using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ExcelDna.Integration;
using Excel = Microsoft.Office.Interop.Excel;

namespace OfficeExtention;

public class GlobalMethods
{
    public Excel.Application ExcelApp => ExcelDnaUtil.Application as Excel.Application;
    public Excel.Application ExcelApp2 => (Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
    public Excel.Workbook ActiveWorkbook => ExcelApp.ActiveWorkbook;
    public Excel.Worksheet ActiveSheet => (Excel.Worksheet)ExcelApp.ActiveSheet;
    public void MsgBox(string message, string caption="Microsoft Excel") => MessageBox.Show(message, caption);
}

public static class VBA
{
    public static void MsgBox(string message, string caption = "Microsoft Excel") => MessageBox.Show(message, caption);
}

public static class Debug
{
    public static void Print(string message) => Console.WriteLine(message);
}

public static class Xl
{
    public static Excel.Application ExcelApp => ExcelDnaUtil.Application as Excel.Application;
    public static Excel.Workbook ActiveWorkbook => ExcelApp.ActiveWorkbook;
    public static Excel.Worksheet ActiveSheet => (Excel.Worksheet)ExcelApp.ActiveSheet;
}

public static class XlApp
{
    private static Excel.Application _excelApp;
    static XlApp()
    {
        _excelApp = (Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
    }
    public static Excel.Application Application => _excelApp;
    public static Excel.Workbook ActiveWorkbook => _excelApp.ActiveWorkbook;
    public static Excel.Worksheet ActiveSheet => (Excel.Worksheet)_excelApp.ActiveSheet;
}
