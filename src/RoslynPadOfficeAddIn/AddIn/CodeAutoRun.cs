using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using OfficeMacroExt;
using System.Text.Json;
using RoslynPad.Build;
using Excel = Microsoft.Office.Interop.Excel;
using ExcelDna.Integration;
using System.Text.Json.Serialization;
using RoslynPad.UI;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace OfficeSharp;

internal static class CodeAutoRun
{
    internal static async Task AutoRunCode()
    {
        var excelMacroCodeFilePath = GetScriptingPath();

        var activedFolder = Directory.GetDirectories(excelMacroCodeFilePath)
                                        .Where(d => Path.GetFileName(d)
                                        .StartsWith("1_"))
                                        .ToList();

        var tasks = activedFolder.Select(async folder =>
        {
            var mainFxCodeFile = Path.Combine(folder, "Program.csx");
            var mainCoreCodeFile = Path.Combine(folder, "ProgramC.csx");

            string mainCode;
            bool isDotNet;

            if (File.Exists(mainFxCodeFile))
            {
                mainCode = await IOUtilities.ReadAllTextAsync(mainFxCodeFile);
                isDotNet = false;    
            }
            else if(File.Exists(mainCoreCodeFile))
            {
                mainCode = await IOUtilities.ReadAllTextAsync(mainCoreCodeFile);
                isDotNet = true;
            }
            else
            {
                Console.WriteLine($"文件夹 {folder} 中未找到 Program.csx 文件");
                return;
            }

            try
            {
                await CSharpScriptingRunHelper.RunInMemory(isDotNet, mainCode, folder, [folder]);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理文件夹 {folder} 时出错: {ex.Message}");
            }
        });

        var syncContext = SynchronizationContext.Current;

        var xlApp = ExcelDnaUtil.Application as Excel.Application ?? (Excel.Application)Marshal.GetActiveObject("Excel.Application");
        var oldStatusBar = xlApp.StatusBar;

        await Task.WhenAll(tasks);

        ExcelAsyncUtil.QueueAsMacro(() =>
        {
            xlApp.StatusBar = "C#脚本成功运行！";
        });

        // Wait 3 seconds, then restore the original status bar
        await Task.Delay(3000);
        ExcelAsyncUtil.QueueAsMacro(() =>
        {
            xlApp.StatusBar = false;
        });

    }

    private static string GetScriptingPath()
    {

        var documentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var configPath = Path.Combine(documentPath, "OfficeSharpConfig");
        var configFile = Path.Combine(configPath, "OfficeSharpMacroAddIn.json");

        var json = File.ReadAllText(configFile);

        using JsonDocument doc = JsonDocument.Parse(json);
        var codeFileRootPath = doc.RootElement
            .GetProperty("excelmacroAddinPath")
            .GetString();

        var excelMacroCodeFilePath = Path.Combine(codeFileRootPath);
        return excelMacroCodeFilePath;

    }

    internal static void CheckConfig()
    {
        try
        {
            var documentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var configPath = Path.Combine(documentPath, "OfficeSharpConfig");
            var jsonPath = Path.Combine(configPath, "OfficeSharpMacroAddIn.json");

            var excelMacroPath = Path.Combine(documentPath, "OfficeSharpMacroAddIn", "ExcelMacroAddIn");
            //var wordMacroPath = Path.Combine(documentPath, "OfficeSharpMacroAddIn", "WordMacroAddIn");
            //var pptMacroPath = Path.Combine(documentPath, "OfficeSharpMacroAddIn", "PPTMacroAddIn");
            //var outlookMacroPath = Path.Combine(documentPath, "OfficeSharpMacroAddIn", "OutlookMacroAddIn");
            //var accessMacroPath = Path.Combine(documentPath, "OfficeSharpMacroAddIn", "AccessMacroAddIn");

            // 创建配置对象
            var config = new { excelmacroAddinPath = excelMacroPath };

            // 确保配置目录存在
            if (!Directory.Exists(configPath))
            {
                Directory.CreateDirectory(configPath);
                Console.WriteLine($"Created configuration directory: {configPath}");
            }

            // 处理json文件
            if (File.Exists(jsonPath))
            {
                //try
                //{
                //    // 读取现有配置
                //    var existingJson = File.ReadAllText(jsonPath);
                //    var existingConfig = JsonSerializer.Deserialize<Dictionary<string, string>>(existingJson);
                //}
                //catch (JsonException)
                //{
                //    // 如果现有文件不是有效的JSON，重新创建它
                //    var jsonContent = JsonSerializer.Serialize(config, new JsonSerializerOptions
                //    {
                //        WriteIndented = true
                //    });
                //    File.WriteAllText(jsonPath, jsonContent);
                //    Console.WriteLine($"Recreated configuration file: {jsonPath}");
                //}
            }

            if (!File.Exists(jsonPath))
            {
                // 创建新的配置文件
                var jsonContent = JsonSerializer.Serialize(config, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(jsonPath, jsonContent);
                Console.WriteLine($"Created configuration file: {jsonPath}");
            }

            // 确保宏目录存在
            if (!Directory.Exists(excelMacroPath))
            {
                Directory.CreateDirectory(excelMacroPath);
                Console.WriteLine($"Created macro directory: {excelMacroPath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    internal static string GetExcelMacroPath()
    {
        var documentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var configPath = Path.Combine(documentPath, "OfficeSharpConfig");
        var jsonPath = Path.Combine(configPath, "OfficeSharpMacroAddIn.json");

        var excelMacroPath = Path.Combine(documentPath, "OfficeSharpMacroAddIn", "ExcelMacroAddIn");

        return excelMacroPath;
    }

}
