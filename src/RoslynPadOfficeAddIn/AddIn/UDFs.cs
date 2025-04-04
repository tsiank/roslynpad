using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ExcelDna.Integration;
using Microsoft.Office.Interop.Excel;
using Excel = Microsoft.Office.Interop.Excel;

#nullable disable

namespace RoslynPad.AddIn;

public static class UDFS
{
    [ExcelFunction(Description = "Execute SQL query on Excel ranges (first range required, ranges named a, b, c, ...)")]
    public static object[,] SQL(
       [ExcelArgument(Description = "needed range", Name = "Range")] object table,
       [ExcelArgument(Description = "sql query statement", Name = "Query")] string query,
       [ExcelArgument(Description = "use first row header, default true", Name = "HasRangeHeader")] object hasRangeHeader,
       [ExcelArgument(Description = "return first row header, defalut true", Name = "HasResultHeader")] object hasResultHeader,
       [ExcelArgument(Description = "more ranges", Name = "Ranges")] params object[] tables)
    {
        var hasRangeHeader2 = hasRangeHeader is ExcelMissing or null || (bool)hasRangeHeader;
        var hasResultHeader2 = hasResultHeader is ExcelMissing or null || (bool)hasResultHeader;

        try
        {
            //var app = ExcelDnaUtil.Application as Application;
            var allTables = new List<object>();

            allTables.Add(table);

            if (tables?.Count() > 0)
            {
                allTables.AddRange(tables);
            }

            var fixedTables = new List<object[,]>();

            foreach (var otable in allTables)
            {
                if (otable is string)
                {
                    return new object[,] { { "Please input full range" } };
                }
                //else if (otable is ExcelReference tableRef)
                //{
                //    var tableValues = GetRangeFromReference(tableRef, app);
                //    fixedTables.Add(tableValues);
                //}
                else if (otable is object[,] tableObj)
                {
                    fixedTables.Add(tableObj);
                }
                else
                {
                    return new object[,] { { "Invalid first table reference" } };
                }

            }

            // 解析查询语句中的字段类型
            var (standardQuery, fieldTypes) = ParseQuery(query, fixedTables.Count);

            using (var connection = new SQLiteConnection("Data Source=:memory:"))
            {
                connection.Open();

                // 创建并填充表，表名使用 a, b, c, ...
                for (var i = 0; i < fixedTables.Count; i++)
                {
                    var tableName = GetTableName(i);
                    CreateAndPopulateTable(connection, tableName, fixedTables[i], hasRangeHeader2, fieldTypes);
                }

                using (var command = new SQLiteCommand(standardQuery, connection))
                using (var reader = command.ExecuteReader())
                {
                    // 获取结果列数和行数
                    var colCount = reader.FieldCount;
                    var results = new List<object[]>();
                    while (reader.Read())
                    {
                        var row = new object[colCount];
                        for (var j = 0; j < colCount; j++)
                        {
                            row[j] = reader[j];
                        }
                        results.Add(row);
                    }

                    // 处理返回结果
                    if (results.Count == 0) return new object[,] { { "No results" } };
                    var rowOffset = hasResultHeader2 ? 1 : 0;
                    var resultArray = new object[results.Count + rowOffset, colCount];

                    // 添加表头（如果需要）
                    if (hasResultHeader2)
                    {
                        for (var j = 0; j < colCount; j++)
                        {
                            resultArray[0, j] = reader.GetName(j);
                        }
                    }

                    // 添加数据
                    for (var i = 0; i < results.Count; i++)
                    {
                        for (var j = 0; j < colCount; j++)
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
        var standardQuery = query;

        // 匹配 {字段名:类型} 或 {字段名}
        var pattern = new Regex(@"\{([^:}]+)(?::([^}]+))?\}");
        var matches = pattern.Matches(query);

        foreach (Match match in matches)
        {
            var fieldName = match.Groups[1].Value.Trim();
            var originalType = match.Groups[2].Success ? match.Groups[2].Value.Trim().ToLower() : null;

            if (originalType != null)
            {
                var sqliteType = MapToSqliteType(originalType);
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
            var replacement = fieldName;
            if (match.Index > 0 && query[match.Index - 1] == '(') // 函数内的字段
            {
                var funcPart = query.Substring(0, match.Index - 1).TrimEnd().Split(' ').Last();
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
            var first = index / 26 - 1;
            var second = index % 26;
            return ((char)('a' + first)).ToString() + ((char)('a' + second)).ToString();
        }
    }

    // 创建并填充 SQLite 表
    private static void CreateAndPopulateTable(SQLiteConnection connection, 
                                                string tableName, 
                                                object[,] values, 
                                                bool hasHeader,
                                                Dictionary<string, (string sqliteType, string originalType)> fieldTypes)
    {

        var rowCount = values.GetLength(0);
        var colCount = values.GetLength(1);
        rowCount = GetLastRow(values, rowCount, colCount);

        // 推断列类型和名称
        var columnInfos = InferColumnTypes(values, hasHeader, colCount, rowCount, tableName, fieldTypes);
        CreateDynamicTable(connection, tableName, columnInfos);

        // 插入数据
        var startRow = hasHeader ? 1 : 0;

        using (var transaction = connection.BeginTransaction())
        {
            for (var row = startRow; row < rowCount; row++)
            {
                using (var cmd = new SQLiteCommand(connection))
                {
                    cmd.Parameters.Clear();
                    var columnNames = columnInfos.Select(p => $"[{p.Name}]").ToList();
                    var paramNames = new List<string>();
                    for (var col = 0; col < colCount; col++)
                    {
                        var paramName = $"@p{col}";
                        var value = col <= colCount ? values[row, col] : null;
                        var colName = columnInfos[col].Name;
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
    }

    // 根据 ExcelReference 获取 Range的values
    private static object[,] GetRangeFromReference(ExcelReference xlRef, Application app)
    {
        var sheetName = (string)XlCall.Excel(XlCall.xlSheetNm, xlRef);
        var index = sheetName.LastIndexOf("]");
        sheetName = sheetName.Substring(index + 1);
        
        var sht = (Worksheet)app.Sheets[sheetName];
        var target = sht.Range[sht.Cells[xlRef.RowFirst + 1, xlRef.ColumnFirst + 1], sht.Cells[xlRef.RowLast + 1, xlRef.ColumnLast + 1]];

        var address = target.Address[false, false, XlReferenceStyle.xlA1];

        // 检查是否为整列或单单元格
        if (address.Contains(":") && char.IsLetter(address.Last())) // 例如 "A:B"
        {
            var parts = address.Split(':');
            var firstRange = sht.Range[parts[0] + "1"];

            var startRow = firstRange.Value != null ? 1 : firstRange.End[XlDirection.xlDown].Row;
            var endRow = sht.Range[parts[1] + sht.Rows.Count.ToString()].End[XlDirection.xlUp].Row;

            return (object[,])sht.Range[$"{parts[0]}{startRow}:{parts[1]}{endRow}"].Value;
        }
        else if (!address.Contains(":")) // 例如 "A1"
        {
            return (object[,])sht.Range[address].CurrentRegion.Value;
        }
        else // 例如 "A1:B4"
        {
            return (object[,])target.Value;
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
            for (var j = 0; j < colCount; j++)
            {
                headers[j] = values[0, j];
            }
        }

        for (var j = 0; j < colCount; j++)
        {
            var colName = hasHeader && headers != null && headers[j] != null
                ? headers[j].ToString().Trim()
                : ((char)('A' + j)).ToString();

            var fullColName = $"{tableName}.{colName}";
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
                sqliteType = InferSqliteType(values, j, Math.Min(100, rowCount - (hasHeader ? 1 : 0)), hasHeader ? 1 : 0);
            }

            columnInfos.Add(new ColumnInfo { Name = colName, Type = sqliteType });
        }

        return columnInfos;
    }


    // 根据前 N 行数据推断类型
    private static string InferSqliteType(object[,] values, int colIndex, int sampleRows, int startRow)
    {
        var allNumeric = true;
        var allInt = true;
        var allBoolean = true;
        var hasLongNumber = false;

        for (var i = startRow; i < startRow + sampleRows && i <= values.GetUpperBound(0); i++)
        {
            var value = values[i, colIndex]; // 不再减 1，直接使用 i 和 colIndex
            if (value == null) continue;

            var valStr = value.ToString().Trim().ToUpper();
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
                if (!double.TryParse(valStr, out var num))
                {
                    allNumeric = false;
                    allInt = false;
                    break;
                }

                // 检查数字长度（去除小数点和小数部分）
                var intPart = valStr.Split('.')[0];
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
        var columns = string.Join(", ", columnInfos.Select(c => $"{c.Name} {c.Type}"));
        var createTableSql = $"CREATE TABLE IF NOT EXISTS {tableName} (Id INTEGER PRIMARY KEY, {columns})";
        using (var command = new SQLiteCommand(createTableSql, connection))
        {
            command.ExecuteNonQuery();
        }
    }

    // 插入动态记录
    private static void InsertDynamicItem(SQLiteConnection connection, string tableName, Dictionary<string, object> row)
    {
        var columns = string.Join(", ", row.Keys);
        var parameters = string.Join(", ", row.Keys.Select(k => $"@{k}"));
        var insertSql = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";

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
        var valStr = value.ToString().Trim();

        if (originalType != null && originalType.ToLower() == "datetime" && sqliteType == "TEXT")
        {
            if (DateTime.TryParse(valStr, out var dt))
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
                return int.TryParse(valStr, out var i) ? i : DBNull.Value;
            case "REAL":
                return double.TryParse(valStr, out var d) ? d : DBNull.Value;
            case "TEXT":
            default:
                return valStr;
        }
    }

    private static int GetLastRow(object[,] arr, int rowCount, int colCount)
    {
        const int PROBE_STEP = 100000; // 探测步长，例如 10 万行
        int threadCount = Environment.ProcessorCount; // 动态线程数

        // 第一阶段：探测数据分布
        List<(int start, int end)> blocksWithData = ProbeDataDistribution(arr, rowCount, colCount, PROBE_STEP);

        if (blocksWithData.Count == 0)
        {
            return -1; // 无数据
        }

        // 第二阶段：并行精查有数据的块
        int[] results = new int[blocksWithData.Count];

        Parallel.For(0, blocksWithData.Count, i =>
        {
            var (start, end) = blocksWithData[i];
            results[i] = FindLastRowInRange(arr, start, end, colCount);
        });

        return results.Max();
    }



    private static (int lastRow, int lastCol) GetLastRowAndColumn(object[,] arr, int rowCount, int colCount)
    {
        const int PROBE_STEP = 100000; // 探测步长，例如 10 万行
        int threadCount = Environment.ProcessorCount; // 动态线程数

        // 第一阶段：探测数据分布
        List<(int start, int end)> blocksWithData = ProbeDataDistribution(arr, rowCount, colCount, PROBE_STEP);

        if (blocksWithData.Count == 0)
        {
            return (-1, -1); // 无数据
        }

        // 第二阶段：并行精查有数据的块
        (int row, int col)[] results = new (int, int)[blocksWithData.Count];

        Parallel.For(0, blocksWithData.Count, i =>
        {
            var (start, end) = blocksWithData[i];
            results[i] = FindLastRowAndColumnInRange(arr, start, end, colCount);
        });

        int maxRow = -1;
        int maxCol = -1;
        foreach (var (row, col) in results)
        {
            if (row > maxRow)
            {
                maxRow = row;
                maxCol = col;
            }
            else if (row == maxRow && col > maxCol)
            {
                maxCol = col;
            }
        }

        return (maxRow, maxCol);
    }

    private static List<(int start, int end)> ProbeDataDistribution(object[,] arr, int rowCount, int colCount, int step)
    {
        List<(int start, int end)> blocks = new List<(int start, int end)>();
        int probeCount = (int)Math.Ceiling((double)rowCount / step);

        for (int i = 0; i < probeCount; i++)
        {
            int probeRow = Math.Min(i * step, rowCount - 1);
            if (HasDataInRow(arr, probeRow, colCount))
            {
                int start = i * step;
                int end = Math.Min(start + step - 1, rowCount - 1);
                blocks.Add((start, end));
            }
        }

        // 如果没有探测到数据，检查最后一行
        if (blocks.Count == 0 && HasDataInRow(arr, rowCount - 1, colCount))
        {
            blocks.Add((rowCount - step, rowCount - 1));
        }

        return blocks;
    }

    private static int FindLastRowInRange(object[,] arr, int start, int end, int colCount)
    {
        int lastRow = -1;

        for (int row = start; row <= end; row++)
        {
            if (HasDataInRow(arr, row, colCount))
            {
                lastRow = row + 1; // 1-based
            }
        }
        return lastRow;
    }


    private static (int lastRow, int lastCol) FindLastRowAndColumnInRange(object[,] arr, int start, int end, int colCount)
    {
        int lastRow = -1;
        int lastCol = -1;

        for (int row = start; row <= end; row++)
        {
            int col = FindLastColumn(arr, row, colCount - 1);
            if (col >= 0)
            {
                lastRow = row + 1; // 1-based
                lastCol = col;
            }
        }
        return (lastRow, lastCol);
    }

    private static bool HasDataInRow(object[,] arr, int row, int colCount)
    {
        if (colCount <= 20) // 少量列线性扫描
        {
            for (int col = 0; col < colCount; col++)
            {
                if (HasData(arr[row, col]))
                {
                    return true;
                }
            }
            return false;
        }
        else // 大量列二分查找
        {
            return FindLastColumn(arr, row, colCount - 1) >= 0;
        }
    }

    private static int FindLastColumn(object[,] arr, int row, int high)
    {
        int low = 0;
        int lastValidCol = -1;

        while (low <= high)
        {
            int mid = low + (high - low) / 2;
            if (HasData(arr[row, mid]))
            {
                lastValidCol = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }
        return lastValidCol;
    }

    private static bool HasData(object value)
    {
        if (value == null || value is ExcelEmpty || value is ExcelError)
        {
            return false;
        }
        if (value is string str && string.IsNullOrEmpty(str))
        {
            return false;
        }
        return true;
    }

    private static string ColumnIndexToLetter(int index)
    {
        string letters = string.Empty;
        while (index > 0)
        {
            int remainder = (index - 1) % 26;
            letters = (char)('A' + remainder) + letters;
            index = (index - remainder - 1) / 26;
        }
        return letters;
    }
}

// 列信息类
internal record ColumnInfo
{
    public string Name { get; set; }
    public string Type { get; set; }
}
