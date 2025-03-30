using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ExcelDna.Integration;
using ExcelDna.IntelliSense;
using ExcelDna.Registration;
using OfficeMacroExt;
using RoslynPad.Build;

namespace OfficeSharp;

public class ExcelAddIn : IExcelAddIn
{
    public void AutoOpen()
    {
        // force using TLS 1.2 or greater, NuGet need
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

        var autoRunMacro = CodeAutoRun.CheckOfficeSharpMacroAddInConfig();
        if (autoRunMacro)
        {
            _ = RunStartupTasksAsync();
        }

        ExcelRegistration
           .GetExcelFunctions()
           .ProcessParamsRegistrations()
           .RegisterFunctions();

        IntelliSenseServer.Install();
    }

    public void AutoClose()
    {
        IntelliSenseServer.Uninstall();
    }

    internal static async Task RunStartupTasksAsync()
    {
        try
        {
            await CodeAutoRun.AutoRunCode();
        }
        catch (Exception ex)
        {
            // Log the error (e.g., to a file or console) since this runs in the background
            ExcelAsyncUtil.QueueAsMacro(() =>
            {
                ((Microsoft.Office.Interop.Excel.Application)ExcelDnaUtil.Application).StatusBar = $"启动时出错: {ex.Message}";
            });
            Console.WriteLine($"AutoRunCode failed: {ex.Message}");
        }
    }
}
