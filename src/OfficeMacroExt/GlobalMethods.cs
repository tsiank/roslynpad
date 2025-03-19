using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ExcelDna.Integration;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Excel = Microsoft.Office.Interop.Excel;
using System.Data.SQLite;
using System.Reflection.Emit;
using System.Data.Entity.Infrastructure;
using Dapper;
using Microsoft.Office.Interop.Excel;
using System.Data.Common;

namespace OfficeMacroExt;

#nullable disable
public static class XlApp
{
    public static Excel.Application Application => ExcelDnaUtil.Application as Excel.Application;
    public static Excel.Application Application2 => (Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
    public static Excel.Workbook ActiveWorkbook => Application.ActiveWorkbook;
    public static Excel.Worksheet ActiveSheet => (Excel.Worksheet)Application.ActiveSheet;
    public static Excel.Range ActiveCell => Application.ActiveCell;

    public static ExcelReference Selection => XlCall.Excel(XlCall.xlfSelection) as ExcelReference;

    public static void WriteToSelection<T>(T input) => XlCall.Excel(XlCall.xlSet, Selection, input);
    public static void MsgBox(string message, string caption="Microsoft Excel") => MessageBox.Show(message, caption);

    public static IEnumerable<dynamic> Query(Excel.Range range, bool useColumnName = true)
    {
        object[,] values = (object[,])range.Value;

        var connection = new SQLiteConnection("Data Source = :memory:");
        connection.Open();

        SqliteHelper.InsertDataToSqlite(connection, useColumnName, values);

        var results = connection.Query<dynamic>("SELECT * FROM a");
        connection.Close();

        return results;
    }

    public static IEnumerable<T> Query<T>(Excel.Range range, bool useColumnName = true) where T : class, new()
    {
        object[,] values = (object[,])range.Value;
        int rowCount = values.GetLength(0);
        int colCount = values.GetLength(1);

        var connection = new SQLiteConnection("Data Source = :memory:");
        connection.Open();

        var entityType = typeof(T);

        SqliteHelper.InsertDataToSqliteRowMap(connection, values, rowCount, colCount, useColumnName, entityType);

        var results = connection.Query<T>($"SELECT * FROM {typeof(T).Name}");
        connection.Close();

        return results;
    }
}

public static class Debug
{
    public static void Print(string message) => Console.WriteLine(message);
}
