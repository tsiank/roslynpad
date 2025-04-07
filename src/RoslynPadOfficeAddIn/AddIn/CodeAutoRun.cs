using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using OfficeSharp.SettingsUI;


namespace OfficeSharp;

internal static class CodeAutoRun
{
    private static JsonSerializerOptions JsonOptions => new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    internal static async Task AutoRunCode()
    {
        var (autoRunMacro, activeDirs) = CheckOfficeSharpMacroAddInConfig();

        if (activeDirs.Count == 0)
        {
            Console.WriteLine("No active scripting to run");
            return;
        }

        var tasks = activeDirs.Select(async folder =>
        {
            var mainFxCodeFile = Path.Combine(folder, "Entry.csx");
            var mainCoreCodeFile = Path.Combine(folder, "EntryC.csx");

            string mainCode;
            bool isDotNet;

            if (File.Exists(mainFxCodeFile))
            {
                mainCode = await IOUtilities.ReadAllTextAsync(mainFxCodeFile).ConfigureAwait(true);
                isDotNet = false;
            }
            else if (File.Exists(mainCoreCodeFile))
            {
                mainCode = await IOUtilities.ReadAllTextAsync(mainCoreCodeFile).ConfigureAwait(true);
                isDotNet = true;
            }
            else
            {
                Console.WriteLine($"No found Entry.csx in {folder}");
                return;
            }

            try
            {
                await CSharpScriptingRunHelper.RunInMemory(isDotNet, mainCode, folder, [folder]).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Folder {folder} error: {ex.Message}");
            }
        });

        var syncContext = SynchronizationContext.Current;

        var xlApp = ExcelDnaUtil.Application as Excel.Application ?? (Excel.Application)Marshal.GetActiveObject("Excel.Application");
        var oldStatusBar = xlApp.StatusBar;

        await Task.WhenAll(tasks).ConfigureAwait(true);

        ExcelAsyncUtil.QueueAsMacro(() =>
        {
            xlApp.StatusBar = "C# run successfully！";
        });

        // Wait 3 seconds, then restore the original status bar
        await Task.Delay(3000).ConfigureAwait(true);
        ExcelAsyncUtil.QueueAsMacro(() =>
        {
            xlApp.StatusBar = false;
        });

    }

    internal static (bool, List<string>) CheckOfficeSharpMacroAddInConfig()
    {
        bool autoRunMacro = false;
        List<string> activeDirectories = [];

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

            if (File.Exists(jsonPath))
            {
                try
                {
                    // 读取现有配置
                    var existingJson = File.ReadAllText(jsonPath);
                    var existingConfig = JsonSerializer.Deserialize<AddInConfig>(existingJson);

                    autoRunMacro = existingConfig!.AutoRunMacro;
                    var addInList = existingConfig.AddInList;
                    if (addInList != null && addInList.Count > 0)
                    {
                        activeDirectories = addInList
                                            .Where(a => a.Value == true)
                                            .Select(a => Path.Combine(existingConfig.ExcelMacroAddinPath, a.Key))
                                            .ToList();
                    }

                }
                catch (JsonException)
                {
                    // 如果现有文件不是有效的JSON，重新创建它
                    var jsonContent = JsonSerializer.Serialize(config, JsonOptions);
                    File.WriteAllText(jsonPath, jsonContent);
                    Console.WriteLine($"Recreated configuration file: {jsonPath}");
                }
            }
            else
            {
                // 创建新的配置文件
                var jsonContent = JsonSerializer.Serialize(config, JsonOptions);
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

        return (autoRunMacro, activeDirectories);
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
