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

namespace RoslynPad;

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
            var mainCodeFile = Path.Combine(folder, "Main.csx");
            if (File.Exists(mainCodeFile))
            {
                try
                {
                    var mainCode = await IOUtilities.ReadAllTextAsync(mainCodeFile);
                    await CSharpScriptingRunHelper.RunInMemory(mainCode, folder, [folder]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"处理文件夹 {folder} 时出错: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"文件夹 {folder} 中未找到 Main.csx 文件");
            }
        });

        var syncContext = SynchronizationContext.Current;

        var oldStatusBar = ExcelDnaUtil.Application.StatusBar;

        await Task.WhenAll(tasks);

        ExcelAsyncUtil.QueueAsMacro(() =>
        {
            ExcelDnaUtil.Application.StatusBar = "C#脚本成功运行！";
        });

        // Wait 3 seconds, then restore the original status bar
        await Task.Delay(3000);
        ExcelAsyncUtil.QueueAsMacro(() =>
        {
            ExcelDnaUtil.Application.StatusBar = false;
        });

    }

    private static string GetScriptingPath()
    {

        var documentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var configPath = Path.Combine(documentPath, "RoslynPad");
        var configFile = Path.Combine(configPath, "OfficeMacroAddIn.json");

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
        var documentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var configPath = Path.Combine(documentPath, "RoslynPad");
        var jsonPath = Path.Combine(configPath, "OfficeMacroAddIn.json");
        
        var excelMacroPath = Path.Combine(documentPath, "OfficeMacroAddIn", "ExcelMacroAddIn");
        //var wordMacroPath = Path.Combine(documentPath, "OfficeMacroAddIn", "WordMacroAddIn");
        //var pptMacroPath = Path.Combine(documentPath, "OfficeMacroAddIn", "PPTMacroAddIn");
        //var outlookMacroPath = Path.Combine(documentPath, "OfficeMacroAddIn", "OutlookMacroAddIn");
        //var accessMacroPath = Path.Combine(documentPath, "OfficeMacroAddIn", "AccessMacroAddIn");

        try
        {
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
}
