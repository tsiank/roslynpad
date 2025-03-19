using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ExcelDna.Integration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;


namespace OfficeMacroExt;
#nullable enable
public class StandardResult
{
    public bool Success { get; set; }

    public Stream? StandardInput { get; set; }
    public StreamReader? StandardOutput { get; set; }
    public StreamReader? StandardError { get; set; }
}


public static class CSharpScriptingRunHelper
{
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = false };

    private static string _vbeAsmPath = @"C:\Windows\assembly\GAC_MSIL\Microsoft.Vbe.Interop\15.0.0.0__71e9bce111e9429c\Microsoft.Vbe.Interop.dll";

    private static string _officeAsmPath = @"C:\Windows\assembly\GAC_MSIL\office\15.0.0.0__71e9bce111e9429c\OFFICE.DLL";

    private static string _excelAsmPath = @"C:\WINDOWS\assembly\GAC_MSIL\Microsoft.Office.Interop.Excel\15.0.0.0__71e9bce111e9429c\Microsoft.Office.Interop.Excel.dll";

    public static MetadataReference VBEAsm =>
        MetadataReference.CreateFromFile(_vbeAsmPath, new MetadataReferenceProperties(embedInteropTypes: false));

    public static MetadataReference OfficepAsm =>
    MetadataReference.CreateFromFile(_officeAsmPath, new MetadataReferenceProperties(embedInteropTypes: false));

    public static MetadataReference ExcelDNAsm => MetadataReference.CreateFromImage(GetAssemblyBytesInMemeory("ExcelDna.Integration"));
    public static MetadataReference ExcelAppAsm =>
        MetadataReference.CreateFromFile(_excelAsmPath, new MetadataReferenceProperties(embedInteropTypes: false));


    public static async Task<StandardResult> RunInMemory(string code)
    {
        var originalConsoleOut = Console.Out;
        var originalConsoleError = Console.Error;

        bool success;
        StreamReader stdOutReader;
        StreamReader stdErrorReader;
        
        var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        var stdOutStream = new MemoryStream();
        var stdOutWriter = new StreamWriter(stdOutStream, utf8NoBom, 1024, leaveOpen: true) { AutoFlush = true };

        var stdErrorStream = new MemoryStream();
        var stdErrorWriter = new StreamWriter(stdErrorStream, utf8NoBom, 1024, leaveOpen: true) { AutoFlush = true };
        
        // 自定义 TextWriter 来格式化输出
        var formattedOutWriter = new FormattedTextWriter(stdOutWriter, "o");
        var formattedErrorWriter = new FormattedTextWriter(stdErrorWriter, "e");

        //var jsonOutWriter = new ResultObject() { Type = "System.String", Value = stdOutWriter };
        Console.SetOut(formattedOutWriter);
        Console.SetError(formattedErrorWriter);

        try
        {
            await RunMacroAsync(async () =>
            {
                var options = ScriptOptions.Default
                    .AddReferences(
                        typeof(MethodInfo).Assembly,
                        typeof(Index).Assembly,
                        typeof(System.Console).Assembly,
                        typeof(System.Dynamic.DynamicObject).Assembly,
                        typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException).Assembly,
                        typeof(System.Windows.MessageBox).Assembly,
                        typeof(System.Collections.Generic.List<>).Assembly,
                        typeof(OfficeMacroExt.XlApp).Assembly
                        )
                    .AddReferences(
                        ExcelDNAsm,
                        ExcelAppAsm
                    )
                    .AddImports(
                    "ExcelDna.Integration",
                    "Microsoft.Office.Interop.Excel",
                    "System",
                    "System.Linq",
                    "System.Reflection",
                    "System.Collections.Generic",
                    "System.Windows",
                    "OfficeMacroExt"
                );

                //var funcRegsCode = @"Assembly assembly = Assembly.Load(""RoslynPad"");
                //                    var types = assembly.GetTypes();
                //var methodsList = new List<MethodInfo>();
                //foreach (var type in types)
                //{
                //    MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
                //    methodsList.AddRange(methods.ToList());
                //}

                //ExcelIntegration.RegisterMethods(methodsList); ";

                //暂用UDF类表示自定义函数和命令

                //var funcRegsCode = @"if(Type.GetType(""UDF"") != null)
                //                     {
                //                        Type type = typeof(UDF);
                //                        MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
                //                        ExcelIntegration.RegisterMethods(methods.ToList()); 
                //                    }";


                if (code.Contains("class UDF"))
                {
                    var funcRegsCode = @" var type = typeof(UDF);
                                            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
                                            ExcelIntegration.RegisterMethods(methods.ToList());";
                    code += funcRegsCode;
                }

                await CSharpScript.RunAsync(code, options);
            });
            success = true;
        }
        catch (Exception ex)
        {
            string errorMessage = ex is CompilationErrorException cex
                ? string.Join("\n", cex.Diagnostics)
                : ex.Message;
            // 将异常格式化为 JSON 并写入 StandardError
            var errorJson = JsonSerializer.Serialize(new
            {
                h = "Exception",
                v = ex.Message,
                t = ex.GetType().FullName,
                x = false
            }, _jsonOptions);

            MessageBox.Show(ex.Message, "错误提示");
            await Console.Error.WriteLineAsync($"{{{errorJson}}}"); // 注意这里不加 "e" 前缀，因为 FormattedTextWriter 已处理
            success = false;
        }
        finally
        {
            Console.SetOut(originalConsoleOut);
            Console.SetError(originalConsoleError);
        }

        await stdOutWriter.FlushAsync();
        stdOutStream.Position = 0;

        await stdErrorWriter.FlushAsync();
        stdErrorStream.Position = 0;

        stdOutReader = new StreamReader(stdOutStream, Encoding.UTF8);
        stdErrorReader = new StreamReader(stdErrorStream, Encoding.UTF8);

        return new StandardResult
        {
            Success = success,
            StandardOutput = stdOutReader,
            StandardError = stdErrorReader
        };
    }

    public static byte[] GetAssemblyBytesInMemeory(string asmName)
    {
        var dnaAsm = Assembly.Load(asmName);
        Type type = dnaAsm.GetType();
        var pi = type.GetMethod("GetRawBytes", BindingFlags.Instance | BindingFlags.NonPublic);
        byte[] assemblyBytes = (byte[])pi.Invoke(dnaAsm, null);
        return assemblyBytes;
    }


    public static Task RunMacroAsync(Func<Task> asyncAction)
    {
        var tcs = new TaskCompletionSource<bool>();

        ExcelAsyncUtil.QueueAsMacro(async () =>
        {
            try
            {
                await asyncAction();
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        return tcs.Task;
    }

    // 自定义 TextWriter 来添加前缀并格式化为包含 Name、Value、Type 的 JSON
    private class FormattedTextWriter : TextWriter
    {
        private readonly StreamWriter _baseWriter;
        private readonly string _prefix;

        public FormattedTextWriter(StreamWriter baseWriter, string prefix)
        {
            _baseWriter = baseWriter;
            _prefix = prefix;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void WriteLine(string value)
        {
            // 将输出格式化为包含 Name、Value、Type 的 JSON
            var json = JsonSerializer.Serialize(new
            {
                h = _prefix == "o" ? "" : "",
                v = value,
                t = value?.GetType().FullName ?? "System.String",
                x = "true"
            }, new JsonSerializerOptions { WriteIndented = false });
            _baseWriter.WriteLine($"{_prefix}:{json}");
        }

        public override void Write(string value)
        {
            // Write 不添加换行符
            var json = JsonSerializer.Serialize(new
            {
                h = _prefix == "o" ? "Output" : "Error",
                v = value,
                t = value?.GetType().FullName ?? "System.String",
                x = "true"
            }, new JsonSerializerOptions { WriteIndented = false });
            _baseWriter.Write($"{_prefix}:{json}");
        }

        // 重写其他 WriteLine 方法，确保所有输出都被格式化
        public override void WriteLine(object value)
        {
            // 将输出格式化为包含 Name、Value、Type 的 JSON
            var json = JsonSerializer.Serialize(new
            {
                h = _prefix == "o" ? "" : "",
                v = value,
                t = value?.GetType().FullName ?? "System.String",
                x = "true"
            }, new JsonSerializerOptions { WriteIndented = false });
            _baseWriter.WriteLine($"{_prefix}:{json}");
        }

        public override void Write(object value)
        {
            // Write 不添加换行符
            var json = JsonSerializer.Serialize(new
            {
                h = _prefix == "o" ? "Output" : "Error",
                v = value,
                t = value?.GetType().FullName ?? "System.String",
                x = "true"
            }, new JsonSerializerOptions { WriteIndented = false });
            _baseWriter.Write($"{_prefix}:{json}");
        }
    }


    private static async Task ExecuteAssemblyAsync0(string assemblyPath, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            var fi = new FileInfo(assemblyPath);
            var dn = fi.DirectoryName;
            var asm = fi.Name;

            var rasm = Path.Combine(new FileInfo(assemblyPath).DirectoryName, "bin", asm);
            var runtimeAsm = Path.Combine(new FileInfo(assemblyPath).DirectoryName, "bin", "RoslynPad.Runtime.dll");
            Assembly.LoadFrom(runtimeAsm);
            Assembly assembly = Assembly.LoadFrom(rasm);

            MethodInfo mainMethod = assembly.EntryPoint;
            // 检查 Main 的签名，通常是 static void Main() 或 static void Main(string[] args)
            object[]? parameters = mainMethod.GetParameters().Length == 0 ? null : new object[] { new string[0] };
            var ret = mainMethod.Invoke(null, parameters);

            //var runMethods = targetType.GetMethods();
            //runMethod.Invoke(null, null); // 调用静态方法
            return;

        });



    }
}
