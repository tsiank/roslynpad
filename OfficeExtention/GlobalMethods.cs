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

namespace OfficeExtention;

#nullable disable
public class GlobalMethods
{
    public Excel.Application ExcelApp => ExcelDnaUtil.Application as Excel.Application;
    public Excel.Application ExcelApp2 => (Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
    public Excel.Workbook ActiveWorkbook => ExcelApp.ActiveWorkbook;
    public Excel.Worksheet ActiveSheet => (Excel.Worksheet)ExcelApp.ActiveSheet;
    public void MsgBox(string message, string caption="Microsoft Excel") => MessageBox.Show(message, caption);

    private static SQLiteConnection _connection;
    private static string[] _headers;

    public IEnumerable<dynamic> Query(Excel.Range range, bool useColumnName = true)
    {
        object[,] values = (object[,])range.Value;

        _connection = new SQLiteConnection("Data Source = :memory:");
        _connection.Open();

        SqliteHelper.InsertDataToSqlite(_connection, useColumnName, values);

        var results = _connection.Query<dynamic>("SELECT * FROM a");
        _connection.Close();

        return results;
    }

    public IEnumerable<T> Query<T>(Excel.Range range, bool useColumnName = true) where T : class, new()
    {
        object[,] values = (object[,])range.Value;
        int rowCount = values.GetLength(0);
        int colCount = values.GetLength(1);

        _connection = new SQLiteConnection("Data Source = :memory:");
        _connection.Open();

        var entityType = typeof(T);

        _headers = SqliteHelper.InsertDataToSqliteRowMap(_connection, values, rowCount, colCount, useColumnName, entityType);

        //InsertDataToSqlite(_connection, values, rowCount, colCount, useColumnName, entityType);

        var results = _connection.Query<T>($"SELECT * FROM {typeof(T).Name}");
        _connection.Close();

        return results;
    }
}

public static class VBA
{
    public static void MsgBox2(string message, string caption = "Microsoft Excel") => MessageBox.Show(message, caption);
}

public static class Debug
{
    public static void Print(string message) => Console.WriteLine(message);
}

#nullable disable
public static class Xl
{
    public static Excel.Application ExcelApp => ExcelDnaUtil.Application as Excel.Application;
    public static Excel.Workbook ActiveWorkbook => ExcelApp.ActiveWorkbook;
    public static Excel.Worksheet ActiveSheet => (Excel.Worksheet)ExcelApp.ActiveSheet;

    private static SQLiteConnection _connection;
    private static string[] _headers;

    public static IEnumerable<dynamic> Query(Excel.Range range, bool useColumnName=true)
    {
        object[,] values = (object[,])range.Value;
        
        _connection = new SQLiteConnection("Data Source = :memory:");
        _connection.Open();

        SqliteHelper.InsertDataToSqlite(_connection, useColumnName, values);

        var results = _connection.Query<dynamic>("SELECT * FROM a");
        _connection.Close();

        return results;
    }

    public static IEnumerable<T> Query<T>(Excel.Range range, bool useColumnName = true) where T : class, new()
    {
        object[,] values = (object[,])range.Value;
        int rowCount = values.GetLength(0);
        int colCount = values.GetLength(1);

        _connection = new SQLiteConnection("Data Source = :memory:");
        _connection.Open();

        var entityType = typeof(T);

        _headers = SqliteHelper.InsertDataToSqliteRowMap(_connection, values, rowCount, colCount, useColumnName, entityType);

        //InsertDataToSqlite(_connection, values, rowCount, colCount, useColumnName, entityType);

        var results = _connection.Query<T>($"SELECT * FROM {typeof(T).Name}");
        _connection.Close();

        return results;
    }
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

// 中间对象，携带 T 和 Id
public class RangeItem<T>
{
    public T Item { get; }
    public int Id { get; } // 隐含的 Id，对应数据库行号

    public RangeItem(T item, int id)
    {
        Item = item;
        Id = id;
    }
}


// 扩展方法 Format
public static class RangeExtensions
{
    public static IEnumerable<RangeItem<T>> Format<T>(this IEnumerable<RangeItem<T>> source, Action<object> rangeAction) where T : class
    {
        if(source != null)
        {
            foreach (var item in source)
            {
                int relativeRow = item.Id;
                //var rowRange = range.Rows[relativeRow];
                rangeAction(123);
                yield return item;
            }
        }
    }
}
