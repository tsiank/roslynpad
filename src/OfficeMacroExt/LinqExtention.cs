using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Excel;
using Excel = Microsoft.Office.Interop.Excel;

#nullable disable

namespace OfficeMacroExt;

//todo
public static class LinqExtensions
{
    public static IEnumerable<Excel.Range> AsRange<T>(this IEnumerable<T> source, Excel.Range rng) where T : class
    {
        if (source != null)
        {
            foreach (var item in source)
            {
                yield return (Excel.Range)item;
            }
        }
    }

    //未实现
    public static IEnumerable<Excel.Range> Format(this IEnumerable<Excel.Range> source, Action<Excel.Range> rangeAction)
    {
        if (source != null)
        {
            foreach (var item in source)
            {
                rangeAction(item);
                yield return item;
            }
        }
    }

    //以下操作对象必须有ID属性

    //对Excel Range对象的操作

    public static IEnumerable<dynamic> Format(this IEnumerable<dynamic> source,
                                        string address,
                                        Action<Excel.Range> formatAction)
    {
        if (source == null)
        {
            yield break;
        }

        var range = XlApp.ActiveSheet.Range[address];
        foreach (var item in source)
        {
            Excel.Range rowRange = (Excel.Range)range.Rows[(int)item.Id + 1];
            formatAction(rowRange);
            yield return item;
        }
    }

    public static IEnumerable<dynamic> Format(this IEnumerable<dynamic> source,
                                            Excel.Range range,
                                            Action<Excel.Range> formatAction)
    {
        if (source == null)
        {
            yield break;
        }

        foreach (var item in source)
        {
            Excel.Range rowRange = (Excel.Range)range.Rows[(int)item.Id + 1];
            formatAction(rowRange);
            yield return item;
        }
    }


    public static IEnumerable<dynamic> Format(this IEnumerable<dynamic> source,
                                        string address,
                                        Action<Excel.Range> formatAction,
                                        bool hasHeader = true
                                        )
    {
        var range = XlApp.ActiveSheet.Range[address];
        foreach (var item in source)
        {
            int offset = hasHeader ? 1 : 0;
            int relativeRow = (int)item.Id + offset;
            if (relativeRow >= 1 && relativeRow <= range.Rows.Count)
            {
                var rowRange = ((Excel.Range)range.Rows[relativeRow]);
                formatAction(rowRange);
            }
            yield return item;
        }
    }


    public static IEnumerable<dynamic> Format(this IEnumerable<dynamic> source,
                                            Excel.Range range,
                                            Action<Excel.Range> formatAction,
                                            bool hasHeader = true
                                            )
    {
        foreach (var item in source)
        {
            int offset = hasHeader ? 1 : 0;
            int relativeRow = (int)item.Id + offset;
            if (relativeRow >= 1 && relativeRow <= range.Rows.Count)
            {
                var rowRange = ((Excel.Range)range.Rows[relativeRow]);
                formatAction(rowRange);
            }
            yield return item;
        }
    }

    public static IEnumerable<T> Format<T>(this IEnumerable<T> source,
                                        string address,
                                        Action<Excel.Range> formatAction) where T : class, IHasId
    {
        if (source == null)
        {
            yield break;
        }

        var range = XlApp.ActiveSheet.Range[address];
        foreach (var item in source)
        {
            Excel.Range rowRange = (Excel.Range)range.Rows[item.Id + 1];
            formatAction(rowRange);
            yield return item;
        }
    }


    public static IEnumerable<T> Format<T>(this IEnumerable<T> source, 
                                            Excel.Range range, 
                                            Action<Excel.Range> formatAction) where T : class, IHasId
    {
        if (source == null)
        {
            yield break;
        }

        foreach (var item in source)
        {
            Excel.Range rowRange = (Excel.Range)range.Rows[item.Id+1];
            formatAction(rowRange);
            yield return item;
        }
    }

    public static IEnumerable<T> Format<T>(this IEnumerable<T> source,
                                        string address,
                                        Action<Excel.Range> formatAction,
                                        bool hasHeader = true
                                        ) where T : class, IHasId
    {
        var range = XlApp.ActiveSheet.Range[address];
        foreach (var item in source)
        {
            int offset = hasHeader ? 1 : 0;
            int relativeRow = item.Id + offset;
            if (relativeRow >= 1 && relativeRow <= range.Rows.Count)
            {
                var rowRange = ((Excel.Range)range.Rows[relativeRow]);
                formatAction(rowRange);
            }
            yield return item;
        }
    }

    public static IEnumerable<T> Format<T>(this IEnumerable<T> source,
                                            Excel.Range range,
                                            Action<Excel.Range> formatAction,
                                            bool hasHeader=true
                                            ) where T : class, IHasId
    {
        foreach (var item in source)
        {
            int offset = hasHeader ? 1 : 0;
            int relativeRow = item.Id + offset;
            if (relativeRow >= 1 && relativeRow <= range.Rows.Count)
            {
                var rowRange = ((Excel.Range)range.Rows[relativeRow]);
                formatAction(rowRange);
            }
            yield return item;
        }
    }


    //更新Range数据
    public static IEnumerable<T> Update<T>(this IEnumerable<T> source,
                                             Excel.Range range,
                                             Action<T> updateAction,
                                             bool hasHeader = true
                                             ) where T : class, IHasId
    {
        var items = source.ToList();
        if (!items.Any())
        {
            return items;
        }

        //Worksheet worksheet = range.Worksheet;
        //Excel.Range usedRange = worksheet.Application.Intersect(range, worksheet.UsedRange) ?? range; // 限制到实际数据范围
        Excel.Range usedRange = range.CurrentRegion;
        int offset = hasHeader ? 1 : 0;

        // 获取列名映射
        Dictionary<string, int> columnMap = GetColumnMap<T>(usedRange, hasHeader);

        // 初始化 updatedValues 为实际数据范围的大小
        object[,] originalValues = usedRange.Value as object[,] ?? new object[usedRange.Rows.Count, usedRange.Columns.Count];
        object[,] updatedValues = new object[usedRange.Rows.Count, usedRange.Columns.Count];
        for (int i = 0; i < usedRange.Rows.Count; i++)
        {
            for (int j = 0; j < usedRange.Columns.Count; j++)
            {
                updatedValues[i, j] = i < originalValues.GetLength(0) && j < originalValues.GetLength(1)
                ? originalValues[i + 1, j + 1] // Excel 数组从 1 开始
                : null;
            }
        }

        // 更新值并记录更改范围
        int? minChangeRow = null;
        int? maxChangeRow = null;

        foreach (var item in items)
        {
            int relativeRow = item.Id + offset - 1; // 数组索引从 0 开始
            if (relativeRow >= 0 && relativeRow < usedRange.Rows.Count)
            {
                T originalItem = CloneItem(item);
                updateAction(item); // 执行赋值操作

                PropertyInfo[] properties = typeof(T).GetProperties();
                foreach (var prop in properties)
                {
                    if (columnMap.TryGetValue(prop.Name, out int colIndex))
                    {
                        object newValue = prop.GetValue(item);
                        object oldValue = prop.GetValue(originalItem);
                        if (!Equals(newValue, oldValue))
                        {
                            updatedValues[relativeRow, colIndex] = newValue;
                            minChangeRow = minChangeRow.HasValue ? Math.Min(minChangeRow.Value, relativeRow) : relativeRow;
                            maxChangeRow = maxChangeRow.HasValue ? Math.Max(maxChangeRow.Value, relativeRow) : relativeRow;
                        }
                    }
                }
            }
        }

        // 批量写入更改
        if (minChangeRow.HasValue && maxChangeRow.HasValue)
        {
            int rowsToUpdate = maxChangeRow.Value - minChangeRow.Value + 1;
            if (rowsToUpdate > 0 && minChangeRow.Value + rowsToUpdate - 1 < usedRange.Rows.Count)
            {
                Excel.Range updateRange = usedRange.Range[
                usedRange.Cells[minChangeRow.Value + 1, 1],
                usedRange.Cells[maxChangeRow.Value + 1, usedRange.Columns.Count]];
                object[,] valuesToWrite = new object[rowsToUpdate, usedRange.Columns.Count];

                for (int i = 0; i < rowsToUpdate; i++)
                {
                    for (int j = 0; j < usedRange.Columns.Count; j++)
                    {
                        valuesToWrite[i, j] = updatedValues[minChangeRow.Value + i, j];
                    }
                }

                updateRange.Value = valuesToWrite;
                Marshal.ReleaseComObject(updateRange);
            }
        }

        Marshal.ReleaseComObject(usedRange);
        return items;
    }

    // 辅助方法：获取列映射
    private static Dictionary<string, int> GetColumnMap<T>(Excel.Range range, bool hasHeader)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (hasHeader)
        {
            object[,] headers = ((Excel.Range)range.Rows[1]).Value as object[,] ?? new object[1, range.Columns.Count];
            for (int j = 1; j <= range.Columns.Count; j++)
            {
                string header = headers[1, j]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(header)) map[header] = j - 1;
            }
        }
        else
        {
            PropertyInfo[] properties = typeof(T).GetProperties();
            for (int j = 0; j < Math.Min(properties.Length, range.Columns.Count); j++)
            {
                map[properties[j].Name] = j;
            }
        }
        return map;
    }

    // 辅助方法：克隆对象以比较变化
    private static T CloneItem<T>(T item) where T : class
    {
        PropertyInfo[] properties = typeof(T).GetProperties();
        T clone = Activator.CreateInstance<T>();
        foreach (var prop in properties)
        {
            if (prop.CanWrite)
            {
                prop.SetValue(clone, prop.GetValue(item));
            }
        }
        return clone;
    }


    //插入Range
    public static IEnumerable<T> Insert<T>(this IEnumerable<T> source, Excel.Range range, Action<Excel.Range> formatAction) where T : class, IHasId
    {
        var items = source.ToList();
        if (!items.Any())
        {
            yield break;
        }

        foreach (var item in source)
        {
            Excel.Range rowRange = (Excel.Range)range.Rows[item.Id + 1];
            formatAction(rowRange);
            yield return item;
        }
    }

    //删除Rang
    public static IEnumerable<T> Delete<T>(this IEnumerable<T> source, Excel.Range range, Action<Excel.Range> formatAction) where T : class, IHasId
    {
        var items = source.ToList();
        if (!items.Any())
        {
            yield break;
        }

        foreach (var item in source)
        {
            Excel.Range rowRange = (Excel.Range)range.Rows[item.Id + 1];
            formatAction(rowRange);
            yield return item;
        }
    }

    //清除数据
    public static IEnumerable<T> Clear<T>(this IEnumerable<T> source, Excel.Range range, Action<Excel.Range> formatAction) where T : class, IHasId
    {
        var items = source.ToList();
        if (!items.Any())
        {
            yield break;
        }

        foreach (var item in source)
        {
            Excel.Range rowRange = (Excel.Range)range.Rows[item.Id + 1];
            formatAction(rowRange);
            yield return item;
        }
    }
}

public interface IHasId
{
    int Id { get; }
}
