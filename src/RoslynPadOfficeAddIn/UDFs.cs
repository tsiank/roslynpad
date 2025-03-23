using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ExcelDna.Integration;
using Microsoft.Office.Interop.Excel;
using Microsoft.Vbe.Interop;
using Excel = Microsoft.Office.Interop.Excel;

#nullable disable

namespace RoslynPad;

public static class UDFs
{
    [ExcelFunction(Description = "Execute SQL query on Excel ranges (first table required, tables named a, b, c, ...)")]
    public static object[,] SQL(
       [ExcelArgument("range area", AllowReference = true, Name = "Range")] object table,
       [ExcelArgument("sql statement")] string query,
       [ExcelArgument("true or false, use first row header, default true")] object hasHeader,
       [ExcelArgument("true or false, return first row header, defalut false")] object returnHeader,
       [ExcelArgument("more range area", AllowReference = true)] params object[] tables)
    {
        bool hasHeader2 = true;
        if (hasHeader is ExcelMissing or null)
        {
            hasHeader2 = true;
        }
        else if (hasHeader is bool bl)
        {
            hasHeader2 = bl;
        }

        bool returnHeader2 = true;
        if (returnHeader is ExcelMissing or null)
        {
            returnHeader2 = true;
        }
        else if (returnHeader is bool bl)
        {
            returnHeader2 = bl;
        }


        if (returnHeader is ExcelMissing or null)
        {
            returnHeader = false;
        }

        try
        {
            // 获取 Excel Application 对象
            Excel.Application app = ExcelDnaUtil.Application;

            // 验证第一个表（必须参数）
            if (!(table is ExcelReference firstTable))
                return new object[,] { { "First table must be a valid range" } };

            // 收集所有表
            var allTables = new List<ExcelReference> { firstTable };
            allTables.AddRange(tables.Where(t => t is ExcelReference).Cast<ExcelReference>());

            if (allTables.Count == 0)
                return new object[,] { { "No valid tables provided" } };

            // 解析查询语句中的字段类型
            var (standardQuery, fieldTypes) = ParseQuery(query, allTables.Count);

            // 使用 SQLite 内存数据库
            using (var connection = new SQLiteConnection("Data Source=:memory:"))
            {
                connection.Open();

                // 创建并填充表，表名使用 a, b, c, ...
                for (int i = 0; i < allTables.Count; i++)
                {
                    string tableName = GetTableName(i);
                    CreateAndPopulateTable(connection, tableName, allTables[i], hasHeader2, app, fieldTypes);
                }

                // 执行查询
                using (var command = new SQLiteCommand(standardQuery, connection))
                using (var reader = command.ExecuteReader())
                {
                    // 获取结果列数和行数
                    int colCount = reader.FieldCount;
                    var results = new List<object[]>();
                    while (reader.Read())
                    {
                        var row = new object[colCount];
                        for (int j = 0; j < colCount; j++)
                        {
                            row[j] = reader[j];
                        }
                        results.Add(row);
                    }

                    // 处理返回结果
                    if (results.Count == 0) return new object[,] { { "No results" } };
                    int rowOffset = returnHeader2 ? 1 : 0;
                    var resultArray = new object[results.Count + rowOffset, colCount];

                    // 添加表头（如果需要）
                    if (returnHeader2)
                    {
                        for (int j = 0; j < colCount; j++)
                        {
                            resultArray[0, j] = reader.GetName(j);
                        }
                    }

                    // 添加数据
                    for (int i = 0; i < results.Count; i++)
                    {
                        for (int j = 0; j < colCount; j++)
                        {
                            resultArray[i + rowOffset, j] = results[i][j];
                        }
                    }

                    return resultArray;
                }
            }
        }
        catch (Exception ex)
        {
            return new object[,] { { $"Error: {ex.Message}" } };
        }
    }

    // 解析查询语句，返回标准 SQL 和字段类型字典
    // 使用大括号解析类型
    // 使用更宽松的正则匹配大括号
    private static (string standardQuery, Dictionary<string, (string sqliteType, string originalType)> fieldTypes) ParseQuery(string query, int tableCount)
    {
        var fieldTypes = new Dictionary<string, (string sqliteType, string originalType)>(StringComparer.OrdinalIgnoreCase);
        string standardQuery = query;

        // 匹配 {字段名:类型} 或 {字段名}
        var pattern = new Regex(@"\{([^:}]+)(?::([^}]+))?\}");
        var matches = pattern.Matches(query);

        foreach (Match match in matches)
        {
            string fieldName = match.Groups[1].Value.Trim();
            string originalType = match.Groups[2].Success ? match.Groups[2].Value.Trim().ToLower() : null;

            if (originalType != null)
            {
                string sqliteType = MapToSqliteType(originalType);
                if (sqliteType != null)
                {
                    if (!fieldName.Contains(".") && tableCount > 1)
                    {
                        throw new ArgumentException($"Field '{fieldName}' without table prefix is ambiguous in multi-table query.");
                    }
                    fieldTypes[fieldName] = (sqliteType, originalType);
                }
            }

            // 替换为不带类型和大括号的字段名
            string replacement = fieldName;
            if (match.Index > 0 && query[match.Index - 1] == '(') // 函数内的字段
            {
                string funcPart = query.Substring(0, match.Index - 1).TrimEnd().Split(' ').Last();
                standardQuery = standardQuery.Replace($"{funcPart}({match.Value})", $"{funcPart}({fieldName})");
            }
            else
            {
                standardQuery = standardQuery.Replace(match.Value, fieldName);
            }
        }

        return (standardQuery, fieldTypes);
    }


    // 将用户指定的类型映射为 SQLite 类型
    private static string MapToSqliteType(string type)
    {
        switch (type.ToLower())
        {
            case "s":
            case "str": return "TEXT";

            case "i":
            case "int": return "INTEGER";

            case "r":
            case "real": return "REAL";

            case "dt":
            case "datetime": return "TEXT";

            default: return null; // 未识别的类型，忽略
        }
    }

    // 生成表名：a, b, c, ...
    private static string GetTableName(int index)
    {
        if (index < 26)
        {
            return ((char)('a' + index)).ToString();
        }
        else
        {
            int first = index / 26 - 1;
            int second = index % 26;
            return ((char)('a' + first)).ToString() + ((char)('a' + second)).ToString();
        }
    }

    // 创建并填充 SQLite 表
    private static void CreateAndPopulateTable(SQLiteConnection connection, 
                                                string tableName, 
                                                ExcelReference excelRef, 
                                                bool hasHeader, 
                                                Excel.Application app,
                                                Dictionary<string, (string sqliteType, string originalType)> fieldTypes)
    {
        // 获取 Range 对象
        Excel.Range range = GetRangeFromReference(excelRef, app);
        object[,] values = range.Value as object[,] ?? throw new ArgumentException($"Range {tableName} contains no data.");

        int rowCount = values.GetLength(0);
        int colCount = values.GetLength(1);

        // 推断列类型和名称
        var columnInfos = InferColumnTypes(values, hasHeader, colCount, rowCount, tableName, fieldTypes);
        CreateDynamicTable(connection, tableName, columnInfos);

        // 插入数据
        int startRow = hasHeader ? 2 : 1;
        //for (int i = startRow; i <= rowCount; i++)
        //{
        //    var rowData = new Dictionary<string, object>();
        //    for (int j = 1; j <= colCount; j++)
        //    {
        //        object value = values[i, j]; // 修正为 1-based 索引
        //        string colName = columnInfos[j - 1].Name;
        //        rowData[colName] = ConvertValue(value, columnInfos[j - 1].Type);
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
                        //string sqliteType = columnInfos[col - 1].Type;
                        //string originalType = fieldTypes.ContainsKey($"{tableName}.{colName}") ? fieldTypes[$"{tableName}.{colName}"].originalType :
                        //                      fieldTypes.ContainsKey(colName) ? fieldTypes[colName].originalType : null;

                        //var valueConverted = ConvertValue(value, sqliteType, originalType); 似乎不需要数据类型转换

                        paramNames.Add(paramName);
                        cmd.Parameters.AddWithValue(paramName, value ?? DBNull.Value);
                    }

                    cmd.CommandText = $"INSERT INTO {tableName} ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", paramNames)})";
                    cmd.ExecuteNonQuery();
                }
            }
            transaction.Commit();
        }

        Marshal.ReleaseComObject(range);
    }

    // 根据 ExcelReference 获取 Range
    private static Excel.Range GetRangeFromReference(ExcelReference xlRef, Excel.Application app)
    {
        string sheetName = (string)XlCall.Excel(XlCall.xlSheetNm, xlRef);
        int index = sheetName.LastIndexOf("]");
        sheetName = sheetName.Substring(index + 1);
        Worksheet ws = (Worksheet)app.Sheets[sheetName];
        Excel.Range target = app.Range[ws.Cells[xlRef.RowFirst + 1, xlRef.ColumnFirst + 1], ws.Cells[xlRef.RowLast + 1, xlRef.ColumnLast + 1]];

        string address = target.Address[false, false, Excel.XlReferenceStyle.xlA1];

        // 检查是否为整列或单单元格
        if (address.Contains(":") && char.IsLetter(address[^1])) // 例如 "A:B"
        {
            return app.Range["A1"].CurrentRegion;
        }
        else if (!address.Contains(":")) // 例如 "A1"
        {
            return app.Range[address].CurrentRegion;
        }
        else // 例如 "A1:B4"
        {
            return target;
        }
    }

    // 推断列类型
    private static List<ColumnInfo> InferColumnTypes(object[,] values, 
                                                    bool hasHeader, 
                                                    int colCount, 
                                                    int rowCount, 
                                                    string tableName,
                                                    Dictionary<string, (string sqliteType, string originalType)> fieldTypes)
    {
        var columnInfos = new List<ColumnInfo>();
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
            string colName = hasHeader && headers != null && headers[j - 1] != null
                ? headers[j - 1].ToString().Trim()
                : ((char)('A' + j - 1)).ToString();

            string fullColName = $"{tableName}.{colName}";
            string sqliteType;

            if (fieldTypes.ContainsKey(fullColName))
            {
                sqliteType = fieldTypes[fullColName].sqliteType;
            }
            else if (fieldTypes.ContainsKey(colName))
            {
                sqliteType = fieldTypes[colName].sqliteType;
            }
            else
            {
                sqliteType = InferSqliteType(values, j, Math.Min(100, rowCount - (hasHeader ? 1 : 0)), hasHeader ? 2 : 1);
            }

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
        string createTableSql = $"CREATE TABLE IF NOT EXISTS {tableName} (Id INTEGER PRIMARY KEY, {columns})";
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
    private static object ConvertValue(object value, string sqliteType, string originalType = null)
    {
        if (value == null) return DBNull.Value;
        string valStr = value.ToString().Trim();

        if (originalType != null && originalType.ToLower() == "datetime" && sqliteType == "TEXT")
        {
            if (DateTime.TryParse(valStr, out DateTime dt))
            {
                return dt.ToString("yyyy-MM-dd HH:mm:ss");
            }
            return DBNull.Value;
        }

        switch (sqliteType)
        {
            case "INTEGER":
                if (valStr.ToUpper() == "TRUE") return 1;
                if (valStr.ToUpper() == "FALSE") return 0;
                return int.TryParse(valStr, out int i) ? i : DBNull.Value;
            case "REAL":
                return double.TryParse(valStr, out double d) ? d : DBNull.Value;
            case "TEXT":
            default:
                return valStr;
        }
    }
}

    // 列信息类
internal record ColumnInfo
{
    public string Name { get; set; }
    public string Type { get; set; }
}
