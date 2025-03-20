using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExcelDna.Integration;
using System.Data;
using System.Data.SQLite;

namespace OfficeMacroExt;

public static class SqliteHelper
{
    public static void InsertDataToSqlite(SQLiteConnection m_dbConnection, bool hasHeader, params object[] tables)
    {
        //var m_dbConnection = new SQLiteConnection("Data Source = :memory:");
        //m_dbConnection.Open();

        var cmd = new SQLiteCommand();
        cmd.Connection = m_dbConnection;

        int i = 0;
        string header;
        string type;

        foreach (object[,] table in tables)
        {
            int rowsCount = table.GetLength(0);
            int colsCount = table.GetLength(1);

            string[] headersTypes = new string[colsCount];
            string[] types = new string[colsCount];

            for (var j = 1; j <= colsCount; j++)
            {
                if (!hasHeader)
                {
                    header = XlApp.NumToColName(j + 1);
                }
                else
                {
                    header = table[1, j].ToString();
                }

                type = "REAL";

                for (var k = 2; k <= rowsCount; k++)
                {
                    if (table[k, j].GetType() == typeof(string))
                    {
                        type = "STRING";
                        break;
                    }
                }

                headersTypes[j-1] = $@"'{header}' {type}";
                types[j-1] = type;
            }

            string fields = string.Join(", ", headersTypes);

            ASCIIEncoding asciiEncoding = new ASCIIEncoding();
            byte[] btNumber = new byte[] { (byte)(i + 65) };
            string tableName = asciiEncoding.GetString(btNumber);

            cmd.CommandText = $"CREATE TABLE {tableName} ({fields})";
            cmd.ExecuteNonQuery();

            StringBuilder values = new StringBuilder();
            for (var k = hasHeader ? 2 : 1; k <= rowsCount; k++)
            {
                string[] tempBrr = new string[colsCount];
                for (var j = 1; j <= colsCount; j++)
                {
                    string temp = table[k, j].ToString();
                    if (table[k, j].GetType() == typeof(ExcelEmpty))
                    {
                        tempBrr[j-1] = "NULL";
                    }
                    else
                    {
                        //if string temp contains "'", then replace to "''"
                        tempBrr[j-1] = types[j-1] == "STRING" ? "'" + temp.Replace("'", "''") + "'" : temp;
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
    }

    internal static string[] InsertDataToSqlite(SQLiteConnection connection, object[,] table, int rowCount, int colCount, bool hasHeader, Type entityType)
    {
        int startRow = hasHeader ? 2 : 1; // 从 1 开始，hasHeader = true 时列名在第 1 行，数据从第 2 行
        var properties = entityType.GetProperties().Where(p => p.Name != "Id").ToArray();

        // 列名来源
        var _headers = hasHeader && rowCount >= 1
            ? Enumerable.Range(1, colCount)
                .Select(col => table[1, col]?.ToString()?.Replace(" ", "_").Replace(".", "_") ?? $"Column{col}")
                .ToArray()
            : Enumerable.Range(1, colCount)
                .Select(col => XlApp.NumToColName(col))
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
        if (clrType == typeof(DateTime)) return "DATETIME";
        if (clrType == typeof(string)) return "TEXT";
        throw new NotSupportedException($"Type {clrType.Name} is not supported.");
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

