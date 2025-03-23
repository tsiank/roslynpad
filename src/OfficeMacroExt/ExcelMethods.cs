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
using System.Net;
using System.Data;

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

    //excel列名转数字
    public static int ColNameToNum(string colName)
    {
        int num = 0;

        colName = colName.ToUpper();
        int firstChar = Convert.ToInt32(colName[0]);
        if (firstChar < 91 && firstChar > 64)
        {
            for (var i = colName.Length - 1; i >= 0; i--)
            {
                var power = (int)Math.Pow(26, colName.Length - 1 - i);
                var asc = Convert.ToInt32(colName[i] - 64);
                num += power * asc;
            }
        }
        return num;
    }

    //数字转excel列名
    public static string NumToColName(int num)
    {
        string columnName = "";
        while (num > 0)
        {
            int remainder = (num - 1) % 26;
            columnName = (char)('A' + remainder) + columnName;
            num = (num - 1) / 26;
        }
        return columnName;
    }

    public static IEnumerable<dynamic> Query(string address, bool hasHeader = true)
    {
        var range = XlApp.ActiveSheet.Range[address];

        var results = Query(range, hasHeader);
  
        return results;
    }


    public static IEnumerable<dynamic> Query(Excel.Range range, bool hasHeader = true)
    {
        var validRange = RangeProcessor.RangeCheck(range);

        object[,] values = (object[,])validRange.Value;

        var connection = new SQLiteConnection("Data Source = :memory:");
        connection.Open();

        SqliteHelper.InsertDataToSqliteWithoutType(connection, values, hasHeader);

        var results = connection.Query<dynamic>("SELECT * FROM a");
        connection.Close();

        return results;
    }

    public static IEnumerable<T> Query<T>(string address, bool hasHeader = true) where T : class, new()
    {
        var range = XlApp.ActiveSheet.Range[address];

        var results = Query<T>(range, hasHeader);

        return results;
    }

    public static IEnumerable<T> Query<T>(Excel.Range range, bool hasHeader = true) where T : class, new()
    {
        var validRange = RangeProcessor.RangeCheck(range);

        object[,] values = (object[,])validRange.Value;

        int rowCount = values.GetLength(0);
        int colCount = values.GetLength(1);

        var connection = new SQLiteConnection("Data Source = :memory:");
        connection.Open();

        var entityType = typeof(T);

        SqliteHelper.InsertDataToSqlite(connection, values, rowCount, colCount, hasHeader, entityType);

        // 注册处理器
        SqlMapper.AddTypeHandler(new TimeSpanHandler());
        var results = connection.Query<T>($"SELECT * FROM {typeof(T).Name}");
        connection.Close();

        return results;
    }
}

public class TimeSpanHandler : SqlMapper.TypeHandler<TimeSpan>
{
    public override TimeSpan Parse(object value)
    {
        return TimeSpan.Parse((string)value);
    }

    public override void SetValue(IDbDataParameter parameter, TimeSpan value)
    {
        parameter.Value = value.ToString();
    }
}


public static class Debug
{
    public static void Print(string message) => Console.WriteLine(message);
}
