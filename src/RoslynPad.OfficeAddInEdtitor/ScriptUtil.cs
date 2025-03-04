using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ExcelDna.Integration;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using RoslynPad.Roslyn;
using Project = Microsoft.CodeAnalysis.Project;
using System.IO;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Windows;
using Newtonsoft.Json;
using Excel = Microsoft.Office.Interop.Excel;
using ICSharpCode.AvalonEdit.Utils;

namespace RoslynPad.OfficeAddInEdtitor;

public static class ExcelAsyncExtensions
{
    //private static CustomRoslynHost _host;

    private static string _officePath = @"C:\WINDOWS\assembly\GAC_MSIL\Microsoft.Office.Interop.Excel\15.0.0.0__71e9bce111e9429c\Microsoft.Office.Interop.Excel.dll";

    private static MetadataReference ExcelDNAsm => MetadataReference.CreateFromImage(GetAssemblyBytesInMemeory("ExcelDna.Integration"));
    public static MetadataReference ExcelApplicationAsm => MetadataReference.CreateFromFile(_officePath, new MetadataReferenceProperties(embedInteropTypes: true));

    private static Assembly MainAssembly => Assembly.Load("RoslynPad.OfficeAddInEdtitor");
    public static Type ScriptGlobalsType => MainAssembly.GetType("RoslynPad.OfficeAddInEdtitor.GlobalMethods");


    internal static async void RunCSharpScript(string code)
    {
        try
        {
            await ExcelAsyncExtensions.RunMacroAsync(async () =>
            {
                var systemCorePath = typeof(System.Dynamic.DynamicObject).Assembly.Location;
                var csharpPath = typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException).Assembly.Location;
                var systemWindows = typeof(System.Windows.MessageBox).Assembly.Location;
                var systemCollections = typeof(System.Collections.Generic.List<>).Assembly.Location;

                var globalInstance = Activator.CreateInstance(ScriptGlobalsType);

                var options = ScriptOptions.Default
                    .AddReferences(
                         ExcelDNAsm,
                         ExcelApplicationAsm,
                        MetadataReference.CreateFromFile(systemCorePath),
                        MetadataReference.CreateFromFile(csharpPath),
                        MetadataReference.CreateFromFile(systemWindows),
                        MetadataReference.CreateFromFile(systemCollections)
                    )
                    .AddImports(
                    "ExcelDna.Integration",
                    "Microsoft.Office.Interop.Excel",
                    "System",
                    "System.Linq",
                    "System.Collections.Generic",
                    "System.Windows"
                );
               
                await CSharpScript.RunAsync(code, options, globals: globalInstance);
            });
        }
        catch (Exception ex)
        {
            string errorMessage = ex is CompilationErrorException cex
                ? string.Join("\n", cex.Diagnostics)
                : ex.Message;
            MessageBox.Show(errorMessage, "运行时错误");
        }
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

    private static byte[] GetAssemblyBytesInMemeory(string asmName)
    {
        var dnaAsm = Assembly.Load(asmName);
        Type type = dnaAsm.GetType();
        var pi = type.GetMethod("GetRawBytes", BindingFlags.Instance | BindingFlags.NonPublic);
        byte[] assemblyBytes = (byte[])pi.Invoke(dnaAsm, null);
        return assemblyBytes;
    }


}


public class CustomRoslynHost : RoslynHost
{
    public CustomRoslynHost(IEnumerable<Assembly>? additionalAssemblies = null,
        RoslynHostReferences? references = null,
        ImmutableHashSet<string>? disabledDiagnostics = null,
        ImmutableArray<string>? analyzerConfigFiles = null):
        base(additionalAssemblies, references, disabledDiagnostics, analyzerConfigFiles)
    {

    }
    protected override Project CreateProject(Solution solution, DocumentCreationArgs args, CompilationOptions compilationOptions, Project? previousProject = null)
    {
        var name = args.Name ?? "New";
        var path = Path.Combine(args.WorkingDirectory, name);
        var id = ProjectId.CreateNewId(name);

        var parseOptions = ParseOptions.WithKind(args.SourceCodeKind);
        var isScript = args.SourceCodeKind == SourceCodeKind.Script;

        if (isScript)
        {
            compilationOptions = compilationOptions.WithScriptClassName(name);
        }

        var analyzerConfigDocuments = AnalyzerConfigFiles.Where(File.Exists).Select(file => DocumentInfo.Create(
            DocumentId.CreateNewId(id, debugName: file),
            name: file,
            loader: new FileTextLoader(file, defaultEncoding: null),
            filePath: file));

        solution = solution.AddProject(ProjectInfo.Create(
            id,
            VersionStamp.Create(),
            name,
            name,
            LanguageNames.CSharp,
            filePath: path,
            isSubmission: isScript,
            parseOptions: parseOptions,
            hostObjectType: typeof(GlobalMethods),
            compilationOptions: compilationOptions,
            metadataReferences: previousProject != null ? [] : DefaultReferences,
            projectReferences: previousProject != null ? new[] { new ProjectReference(previousProject.Id) } : null)
            .WithAnalyzerConfigDocuments(analyzerConfigDocuments));

        var project = solution.GetProject(id)!;
        project = project.WithMetadataReferences([ExcelAsyncExtensions.ExcelApplicationAsm]);

        if (!isScript && GetUsings(project) is { Length: > 0 } usings)
        {
            project = project.AddDocument("RoslynPadGeneratedUsings", usings).Project;
        }

        return project;

        static string GetUsings(Project project)
        {
            if (project.CompilationOptions is CSharpCompilationOptions options)
            {
                return string.Join(" ", options.Usings.Select(i => $"global using {i};"));
            }

            return string.Empty;
        }
    }
}
