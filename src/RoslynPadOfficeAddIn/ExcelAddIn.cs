using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExcelDna.Integration;
using ExcelDna.Registration;

namespace RoslynPad;

public class ExcelAddIn : IExcelAddIn
{
    public void AutoOpen()
    {
        ExcelRegistration
           .GetExcelFunctions()
           .ProcessParamsRegistrations()
           .RegisterFunctions();
        //MessageBox.Show("fires when the add-in is loaded");
    }

    public void AutoClose()
    {
        //MessageBox.Show("fires when the add-in is unloaded");
    }

    [ExcelFunction(Description = "use sql query with field name")]
    public static object SQLF(string query, params object[] tables)
    {
        try
        {
            DataTable dt = SqliteUtil.SqlCommon(query, false, tables);

            var rows = dt.Rows;
            int rowCount = dt.Rows.Count;
            int colCount = dt.Columns.Count;

            object[,] result = new object[rowCount + 1, colCount];
            int p = 0;
            foreach (DataColumn column in dt.Columns)
            {
                result[0, p] = column.ColumnName;
                p++;
            }

            for (var m = 1; m < rowCount + 1; m++)
            {
                for (var n = 0; n < colCount; n++)
                {
                    result[m, n] = rows[m - 1][n].ToString() == String.Empty ? String.Empty : rows[m - 1][n];
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    [ExcelFunction(Description = "use sql query without field name")]
    public static object SQLT(string query, params object[] tables)
    {
        try
        {
            DataTable dt = SqliteUtil.SqlCommon(query, false, tables);
            var rows = dt.Rows;
            int rowCount = dt.Rows.Count;
            int colCount = dt.Columns.Count;

            object[,] result = new object[rowCount, colCount];
            for (var m = 0; m < rowCount; m++)
            {
                for (var n = 0; n < colCount; n++)
                {
                    result[m, n] = rows[m][n].ToString() == String.Empty ? String.Empty : rows[m][n];
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }

    }

    [ExcelFunction(Description = "use sql query with ColumnName and field name")]
    public static object SQLFA(string query, params object[] tables)
    {
        try
        {
            DataTable dt = SqliteUtil.SqlCommon(query, true, tables);
            var rows = dt.Rows;
            int rowCount = dt.Rows.Count;
            int colCount = dt.Columns.Count;

            object[,] result = new object[rowCount + 1, colCount];
            int p = 0;
            foreach (DataColumn column in dt.Columns)
            {
                result[0, p] = column.ColumnName;
                p++;
            }

            for (var m = 1; m < rowCount + 1; m++)
            {
                for (var n = 0; n < colCount; n++)
                {
                    result[m, n] = rows[m - 1][n].ToString() == String.Empty ? String.Empty : rows[m - 1][n];
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }

    }

    [ExcelFunction(Description = "use sql query with ColumnName but without field name")]
    public static object SQLTA(string query, params object[] tables)
    {
        try
        {
            DataTable dt = SqliteUtil.SqlCommon(query, true, tables);
            var rows = dt.Rows;
            int rowCount = dt.Rows.Count;
            int colCount = dt.Columns.Count;

            object[,] result = new object[rowCount, colCount];
            for (var m = 0; m < rowCount; m++)
            {
                for (var n = 0; n < colCount; n++)
                {
                    result[m, n] = rows[m][n].ToString() == String.Empty ? String.Empty : rows[m][n];
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }

    }
}
