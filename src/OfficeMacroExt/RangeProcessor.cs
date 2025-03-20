using Excel = Microsoft.Office.Interop.Excel;
using System;

namespace OfficeMacroExt;

public static class RangeProcessor
{
    // 处理 Range 对象或字符串输入
    public static Excel.Range RangeCheck(Excel.Range range)
    {
        // 判断范围形式并处理
        string address = range.Address[false, false, Excel.XlReferenceStyle.xlA1]; // 获取地址，如 "A2" 或 "C:D"
        string[] parts = address.Split(':');

        if (parts.Length == 1) // 单单元格，例如 "A2"
        {
            if (IsSingleCell(parts[0]))
            {
                Excel.Range startCell = range.Worksheet.Range[parts[0]];
                if (startCell.Value == null || string.IsNullOrEmpty(startCell.Value.ToString().Trim()))
                {
                    // 如果起点为空，向下找第一个非空单元格
                    int row = startCell.Row;
                    int col = startCell.Column;
                    int maxRows = startCell.Worksheet.Rows.Count;
                    while (row <= maxRows)
                    {
                        Excel.Range cell = (Excel.Range)startCell.Worksheet.Cells[row, col];
                        if (cell.Value != null && !string.IsNullOrEmpty(cell.Value.ToString().Trim()))
                        {
                            return cell.CurrentRegion;
                        }
                        row++;
                    }
                    return startCell; // 若无数据，返回原单元格
                }
                return startCell.CurrentRegion;
            }
            throw new ArgumentException("无效的单单元格格式");
        }
        else if (parts.Length == 2) // 范围形式
        {
            if (IsColumnRange(parts[0], parts[1])) // 列范围，例如 "C:D"
            {
                //Excel.Range fullRange = range.Worksheet.Range[parts[0] + "1:" + parts[1] + range.Worksheet.Rows.Count];
                //Excel.Range usedRange = range.Worksheet.UsedRange;
                //return range.Worksheet.Application.Intersect(fullRange, usedRange) ?? fullRange; // 若无交集返回全范围

                Excel.Range fullRange = range.Worksheet.Range[parts[0] + "1:" + parts[1] + range.Worksheet.Rows.Count];
                Excel.Range usedRange = range.Worksheet.UsedRange;
                return range.Worksheet.Application.Intersect(fullRange, usedRange) ?? fullRange; // 若无交集返回全范围
            }
            else if (IsFullRange(parts[0], parts[1])) // 完整范围，例如 "A2:D3"
            {
                return range; // 直接返回原范围
            }
            throw new ArgumentException("无效的范围格式");
        }
        throw new ArgumentException("无法解析的范围格式");
    }

    // 判断是否为单单元格（例如 "A2"）
    private static bool IsSingleCell(string input)
    {
        if (string.IsNullOrEmpty(input)) return false;
        int letterCount = 0, digitCount = 0;
        foreach (char c in input)
        {
            if (char.IsLetter(c)) letterCount++;
            else if (char.IsDigit(c)) digitCount++;
            else return false;
        }
        return letterCount > 0 && digitCount > 0;
    }

    // 判断是否为列范围（例如 "C:D"）
    private static bool IsColumnRange(string start, string end)
    {
        return start.All(char.IsLetter) && end.All(char.IsLetter);
    }

    // 判断是否为完整范围（例如 "A2:D3"）
    private static bool IsFullRange(string start, string end)
    {
        return IsSingleCell(start) && IsSingleCell(end);
    }

    // 可选：计算最大行
    public static int GetMaxRow(Excel.Range rng)
    {
        if (rng == null) return 0;

        object[,] data = (object[,])rng.Value;
        int rowCount = data.GetLength(0);
        int colCount = data.GetLength(1);
        int[] lastRows = new int[colCount];

        System.Threading.Tasks.Parallel.For(0, colCount, col =>
        {
            lastRows[col] = 0;
            for (int row = rowCount; row >= 1; row--)
            {
                object value = data[row, col + 1];
                if (value != null && !string.IsNullOrEmpty(value.ToString().Trim()))
                {
                    lastRows[col] = row + rng.Row - 1; // 加上偏移
                    break;
                }
            }
        });

        return lastRows.Max();
    }
}

// 测试代码
public class Test
{
    public static void TestMethon()
    {
        // 测试 Range 对象输入
        Excel.Range range1 = RangeProcessor.RangeCheck(XlApp.ActiveSheet.Range["A2"]);
        Console.WriteLine("Range[\"A2\"] -> " + range1.Address);

        Excel.Range range2 = RangeProcessor.RangeCheck(XlApp.ActiveSheet.Range["C:D"]);
        Console.WriteLine("Range[\"C:D\"] -> " + range2.Address);

        Excel.Range range3 = RangeProcessor.RangeCheck(XlApp.ActiveSheet.Range["A2:D3"]);
        Console.WriteLine("Range[\"A2:D3\"] -> " + range3.Address);

        // 测试最大行
        int maxRow = RangeProcessor.GetMaxRow(XlApp.ActiveSheet.Range["A2"]);
        Console.WriteLine("Max row for Range[\"A2\"]: " + maxRow);
    }
}
