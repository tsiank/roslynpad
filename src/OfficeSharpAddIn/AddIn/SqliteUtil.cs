using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExcelDna.Integration;

namespace OfficeSharp;

public static class SqliteUtil
{
    public static DataTable SqlCommon(string query, bool useColumnName, params object[] tables)
    {
        var m_dbConnection = new SQLiteConnection("Data Source = :memory:");
        m_dbConnection.Open();

        var cmd = new SQLiteCommand();
        cmd.Connection = m_dbConnection;

        var i = 0;
        string header;
        string type;

        var dt = new DataTable();

        foreach (object[,] table in tables)
        {
            var rowsCount = GetLastRow(table);
            //int rowsCount = table.GetLength(0);
            var colsCount = table.GetLength(1);

            var headersTypes = new string[colsCount];
            var types = new string[colsCount];

            for (var j = 0; j < colsCount; j++)
            {
                if (useColumnName)
                {
                    header = NumToColName(j + 1);
                }
                else
                {
                    header = table[0, j].ToString();
                }

                type = "REAL";

                for (var k = 1; k < rowsCount; k++)
                {
                    if (table[k, j].GetType() == typeof(string))
                    {
                        type = "STRING";
                        break;
                    }
                }

                headersTypes[j] = $@"'{header}' {type}";
                types[j] = type;
            }

            var fields = string.Join(", ", headersTypes);

            var asciiEncoding = new ASCIIEncoding();
            var btNumber = new byte[] { (byte)(i + 65) };
            var tableName = asciiEncoding.GetString(btNumber);

            cmd.CommandText = $"CREATE TABLE {tableName} ({fields})";
            cmd.ExecuteNonQuery();

            var values = new StringBuilder();
            for (var k = useColumnName ? 0 : 1; k < rowsCount; k++)
            {
                var tempBrr = new string[colsCount];
                for (var j = 0; j < colsCount; j++)
                {
                    var temp = table[k, j].ToString();
                    if (table[k, j].GetType() == typeof(ExcelEmpty))
                    {
                        tempBrr[j] = "NULL";
                    }
                    else
                    {
                        //if string temp contains "'", then replace to "''"
                        tempBrr[j] = types[j] == "STRING" ? "'" + temp.Replace("'", "''") + "'" : temp;
                    }
                }

                var value = $"({string.Join(", ", tempBrr)}),";
                values.Append(value);
            }

            values.Remove(values.Length - 1, 1);

            cmd.CommandText = $@"INSERT INTO {tableName} VALUES {values}";
            cmd.ExecuteNonQuery();
            i++;

        }

        cmd.CommandText = query;
        //cmd.CommandText = query.Replace("。",".").Replace("，", ",");
        var reader = cmd.ExecuteReader();

        dt.Load(reader);

        m_dbConnection.Close();
        return dt;
    }

    public static int GetLastRow(object[,] arr)
    {
        var lastRow = arr.GetLength(0);
        var lastColumn = arr.GetLength(1);

        if (lastRow < 1048576)
        {
            return lastRow;
        }
        /*
            for (var i = lastRow - 1; i > 0; i--)
            {
                var j = 0;
                while (j < lastColumn)
                {
                    if (arr[i, j].GetType() == typeof(ExcelEmpty))
                    {
                        j++;
                    }
                    else
                    {
                        return i+1;
                    }
                }
            }
        */

        Func<int, int, int> getRow = (m, n) =>
        {
            var ret = 0;
            for (var i = m; i > n; i--)
            {
                var j = 0;
                while (j < lastColumn)
                {
                    if (arr[i, j].GetType() == typeof(ExcelEmpty))
                    {
                        j++;
                    }
                    else
                    {
                        ret = i + 1;
                        goto end;
                    }
                }
            }
        end:
            return ret;
        };

        var rett = new int[8];

        Parallel.Invoke(
            () => rett[0] = getRow(lastRow - 1, 917504),
            () => rett[1] = getRow(917504, 786432),
            () => rett[2] = getRow(786432, 655360),
            () => rett[3] = getRow(655360, 524288),
            () => rett[4] = getRow(524288, 393216),
            () => rett[5] = getRow(393216, 262144),
            () => rett[6] = getRow(262144, 131072),
            () => rett[7] = getRow(131072, 0)
        );

        return rett.Max();
    }

    //数字转excel列名
    public static string NumToColName(int num)
    {
        if (num <= 0)
        {
            return "数字是必须大于0的正整数";
        }

        var asciiEncoding = new ASCIIEncoding();
        var colName = "";
        while (num > 0)
        {
            var btNumber = new byte[] { (byte)((num - 1) % 26 + 65) };
            colName = asciiEncoding.GetString(btNumber) + colName;
            num = (num - 1) / 26;
        }
        return colName;
    }

    //excel列名转数字
    public static int ColNameToNum(string colName)
    {
        var num = 0;

        colName = colName.ToUpper();
        var firstChar = Convert.ToInt32(colName[0]);
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
}
