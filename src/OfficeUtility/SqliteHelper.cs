using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExcelDna.Integration;
using System.Data;
using System.Data.SQLite;
using Excel = Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;
using System.Windows.Controls;

#nullable disable

namespace OfficeMacroExt;

public static class SqliteHelper
{

    public static void InsertDataToSqliteWithoutType(SQLiteConnection connection, object[,] table, bool hasHeader)
    {
        int startRow = hasHeader ? 2 : 1; // 从 1 开始，hasHeader = true 时列名在第 1 行，数据从第 2 行
        string tableName = "a";
        CreateAndPopulateTable(connection, tableName, table, hasHeader);
    }

    // 创建并填充 SQLite 表
    private static void CreateAndPopulateTable(SQLiteConnection connection, string tableName, object[,] values, bool hasHeader)
    {
        int rowCount = values.GetLength(0);
        int colCount = values.GetLength(1);

        // 推断列类型和名称
        var columnInfos = InferColumnTypes(values, hasHeader, colCount, rowCount);
        CreateDynamicTable(connection, tableName, columnInfos);

        // 插入数据
        int startRow = hasHeader ? 2 : 1;
        
        //for (int i = startRow; i <= rowCount; i++)
        //{
        //    var rowData = new Dictionary<string, object>();
        //    for (int j = 1; j <= colCount; j++)
        //    {
        //        object value = values[i, j]; // 从 1 开始索引
        //        string colName = columnInfos[j - 1].Name;
        //        rowData[colName] = value;
        //    }
        //    InsertDynamicItem(connection, tableName, rowData);
        //}

        using (var transaction = connection.BeginTransaction())
        {
            for (int row = startRow; row <= rowCount; row++)
            {
                using (var cmd = new SQLiteCommand(connection))
                {
                    cmd.Parameters.Clear();
                    var columnNames = columnInfos.Select(p => $"[{p.Name}]").ToList();
                    var paramNames = new List<string>();
                    for (int col = 1; col <= colCount; col++)
                    {
                        string paramName = $"@p{col}";
                        var value = col <= colCount ? values[row, col] : null;
                        string colName = columnInfos[col - 1].Name;

                        paramNames.Add(paramName);
                        cmd.Parameters.AddWithValue(paramName, value ?? DBNull.Value);
                    }

                    cmd.CommandText = $"INSERT INTO {tableName} ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", paramNames)})";
                    cmd.ExecuteNonQuery();
                }
            }
            transaction.Commit();
        }

    }

    // 根据 ExcelReference 获取 Range
    private static Excel.Range GetRangeFromReference(ExcelReference excelRef, Excel.Application app)
    {
        string address = app.get_Range(excelRef).Address[false, false, Excel.XlReferenceStyle.xlA1];

        // 检查是否为整列或单单元格
        if (address.Contains(":") && !address.Contains("$")) // 例如 "A:B"
        {
            return app.Range["A1"].CurrentRegion;
        }
        else if (!address.Contains(":")) // 例如 "A1"
        {
            return app.Range[address].CurrentRegion;
        }
        else // 例如 "A1:B4"
        {
            return app.get_Range(excelRef);
        }
    }

    // 推断列类型
    private static List<ColumnInfo> InferColumnTypes(object[,] values, bool hasHeader, int colCount, int rowCount)
    {
        var columnInfos = new List<ColumnInfo>();
        int startRow = hasHeader ? 1 : 0; // 第一行可能是表头
        object[] headers = null;

        if (hasHeader && rowCount >= 1)
        {
            headers = new object[colCount];
            for (int j = 0; j < colCount; j++)
            {
                headers[j] = values[1, j + 1];
            }
        }


        for (int j = 1; j <= colCount; j++)
        {
            string colName = hasHeader && headers != null && rowCount >= 1 && headers[j - 1] != null
                ? headers[j - 1].ToString().Trim()
                : ((char)('A' + j - 1)).ToString();

            string sqliteType = InferSqliteType(values, j, Math.Min(100, rowCount - (hasHeader ? 1 : 0)), hasHeader ? 2 : 1);
            columnInfos.Add(new ColumnInfo { Name = colName, Type = sqliteType });
        }

        return columnInfos;
    }

    // 根据前 N 行数据推断类型
    private static string InferSqliteType(object[,] values, int colIndex, int sampleRows, int startRow)
    {
        bool allNumeric = true;
        bool allInt = true;
        bool allBoolean = true;
        bool hasLongNumber = false;

        for (int i = startRow; i < startRow + sampleRows && i <= values.GetUpperBound(0) + 1; i++)
        {
            object value = values[i, colIndex]; // 不再减 1，直接使用 i 和 colIndex
            if (value == null) continue;

            string valStr = value.ToString().Trim().ToUpper();
            if (string.IsNullOrEmpty(valStr)) continue;

            // 检查布尔值
            if (valStr == "TRUE" || valStr == "FALSE")
            {
                allNumeric = false;
                allInt = false;
            }
            else
            {
                allBoolean = false;

                // 检查是否为数字
                if (!double.TryParse(valStr, out double num))
                {
                    allNumeric = false;
                    allInt = false;
                    break;
                }

                // 检查数字长度（去除小数点和小数部分）
                string intPart = valStr.Split('.')[0];
                if (intPart.Length > 15)
                {
                    hasLongNumber = true;
                    break;
                }

                // 检查是否为整数
                if (!int.TryParse(valStr, out _))
                {
                    allInt = false;
                }
            }
        }

        if (allBoolean) return "INTEGER";                // 布尔值（TRUE/FALSE 映射为 1/0）
        if (hasLongNumber) return "TEXT";                // 超过 15 位数字转为 TEXT
        if (allInt && allNumeric) return "INTEGER";      // 纯整数
        if (allNumeric) return "REAL";                   // 包含浮点数
        return "TEXT";                                   // 默认文本
    }

    // 创建动态表
    private static void CreateDynamicTable(SQLiteConnection connection, string tableName, List<ColumnInfo> columnInfos)
    {
        string columns = string.Join(", ", columnInfos.Select(c => $"{c.Name} {c.Type}"));
        string createTableSql = $"CREATE TABLE IF NOT EXISTS {tableName} ({columns})";
        using (var command = new SQLiteCommand(createTableSql, connection))
        {
            command.ExecuteNonQuery();
        }
    }

    // 插入动态记录
    private static void InsertDynamicItem(SQLiteConnection connection, string tableName, Dictionary<string, object> row)
    {
        string columns = string.Join(", ", row.Keys);
        string parameters = string.Join(", ", row.Keys.Select(k => $"@{k}"));
        string insertSql = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";

        using (var command = new SQLiteCommand(insertSql, connection))
        {
            foreach (var kvp in row)
            {
                command.Parameters.AddWithValue($"@{kvp.Key}", kvp.Value ?? DBNull.Value);
            }
            command.ExecuteNonQuery();
        }
    }

    // 转换值到目标类型
    private static object ConvertValue(object value, string sqliteType)
    {
        if (value == null) return DBNull.Value;
        string valStr = value.ToString().Trim().ToUpper();

        switch (sqliteType)
        {
            case "INTEGER":
                if (valStr == "TRUE") return 1;
                if (valStr == "FALSE") return 0;
                return int.TryParse(valStr, out int i) ? i : DBNull.Value;
            case "REAL":
                return double.TryParse(valStr, out double d) ? d : DBNull.Value;
            case "TEXT":
            default:
                return valStr; // TEXT 类型保留原始字符串
        }
    }

    public static string[] InsertDataToSqlite(SQLiteConnection connection, object[,] table, int rowCount, int colCount, bool hasHeader, Type entityType)
    {
        int startRow = hasHeader ? 2 : 1; // 从 1 开始，hasHeader = true 时列名在第 1 行，数据从第 2 行
        var properties = entityType.GetProperties().Where(p => p.Name != "Id").ToArray();

        // 列名来源
        var _headers = hasHeader && rowCount >= 1
            ? Enumerable.Range(1, colCount)
                .Select(col => table[1, col]?.ToString()?.Replace(" ", "_").Replace(".", "_") ?? $"Column{col}")
                .ToArray()
            : Enumerable.Range(1, colCount)
                .Select(col => NumToColName(col))
                .ToArray();

        if (_headers.Length < properties.Length)
        {
            throw new ArgumentException($"Range has fewer columns ({_headers.Length}) than properties in {entityType.Name} ({properties.Length}).");
        }

        var sqliteTypes = properties.Select(p => MapToSqliteType(p.PropertyType)).ToArray();
        string createTableSql = $"CREATE TABLE {entityType.Name} (Id INTEGER PRIMARY KEY, {string.Join(", ", properties.Zip(sqliteTypes, (p, t) => $"[{p.Name}] {t}"))})";
        using (var cmd = new SQLiteCommand(createTableSql, connection))
        {
            cmd.ExecuteNonQuery();
        }

        using (var transaction = connection.BeginTransaction())
        {
            for (int row = startRow; row <= rowCount; row++)
            {
                using (var cmd = new SQLiteCommand(connection))
                {
                    cmd.Parameters.Clear();
                    var columnNames = properties.Select(p => $"[{p.Name}]").ToList();
                    var paramNames = new List<string>();
                    for (int col = 1; col <= properties.Length; col++)
                    {
                        string paramName = $"@p{col}";
                        var value = col <= colCount ? table[row, col] : null;

                        var columnTypeInfo = properties[col-1];
                        if(columnTypeInfo.PropertyType.Name == "TimeSpan")
                        {
                            value = DateTime.FromOADate((double)value).ToLongTimeString();
                        }
                        
                        paramNames.Add(paramName);
                        cmd.Parameters.AddWithValue(paramName, value ?? DBNull.Value);
                    }

                    cmd.CommandText = $"INSERT INTO {entityType.Name} ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", paramNames)})";
                    cmd.ExecuteNonQuery();
                }
            }
            transaction.Commit();
        }

        return _headers;
    }

    private static string MapToSqliteType(Type clrType)
    {
        if (clrType == typeof(int)) return "INTEGER";
        if (clrType == typeof(double)) return "REAL";
        if (clrType == typeof(string)) return "TEXT";
        if (clrType == typeof(bool)) return "INTEGER";

        if (clrType == typeof(DateTime)) return "TEXT";
        if (clrType == typeof(DateTimeOffset)) return "TEXT";
        if (clrType == typeof(DateTime)) return "TEXT";
        if (clrType == typeof(TimeSpan)) return "TEXT";

        throw new NotSupportedException($"Type {clrType.Name} is not supported.");
    }

    private static string NumToColName(int num)
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


    //private static Type GenerateEntityType(string[] headers, string[] sqliteTypes)
    //{
    //    string modelCode = "public class RangeRecord {\n" +
    //                      "    public int Id { get; set; }\n";
    //    foreach (var (header, sqliteType) in headers.Zip(sqliteTypes, (h, t) => (h, t)))
    //    {
    //        string clrType = sqliteType switch
    //        {
    //            "INTEGER" => "int",
    //            "REAL" => "double",
    //            "DATETIME" => "System.DateTime",
    //            "TEXT" => "string",
    //            _ => "string"
    //        };
    //        modelCode += $"    public {clrType} {header} {{ get; set; }}\n";
    //    }
    //    modelCode += "}";

    //    var syntaxTree = CSharpSyntaxTree.ParseText(modelCode);
    //    var references = new MetadataReference[]
    //    {
    //        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
    //        MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
    //        MetadataReference.CreateFromFile(typeof(SQLiteConnection).Assembly.Location),
    //        MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location)
    //    };

    //    var compilation = CSharpCompilation.Create(
    //        "DynamicModelAssembly",
    //        new[] { syntaxTree },
    //        references,
    //        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    //    using (var ms = new MemoryStream())
    //    {
    //        var result = compilation.Emit(ms);
    //        if (!result.Success)
    //        {
    //            throw new Exception("Model compilation failed: " + string.Join("\n", result.Diagnostics));
    //        }

    //        ms.Seek(0, SeekOrigin.Begin);
    //        var assembly = Assembly.Load(ms.ToArray());
    //        return assembly.GetType("RangeRecord");
    //    }
    //}

    //private static IEnumerable<object> ExecuteQuery(string sql)
    //{
    //    using (var cmd = new SQLiteCommand(sql, _connection))
    //    {
    //        using (var reader = cmd.ExecuteReader())
    //        {
    //            while (reader.Read())
    //            {
    //                var instance = Activator.CreateInstance(_entityType);
    //                for (int i = 0; i < reader.FieldCount; i++)
    //                {
    //                    var prop = _entityType.GetProperty(reader.GetName(i));
    //                    if (prop != null && reader[i] != DBNull.Value)
    //                    {
    //                        prop.SetValue(instance, reader[i]);
    //                    }
    //                }
    //                yield return instance;
    //            }
    //        }
    //    }
    //}

}

// 列信息类
internal record ColumnInfo
{
    public string Name { get; set; }
    public string Type { get; set; }
}

