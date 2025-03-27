using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Excel=Microsoft.Office.Interop.Excel;

namespace OfficeMacroExt;

public static class RangeExtensions
{
    /// <summary>
    /// 获取Range的最后有效行号
    /// </summary>
    public static int LastRow(this Excel.Range range)
    {
        try
        {
            Excel.Range startCell = (Excel.Range)range.Cells[1, 1];
            Excel.Range lastCell = (Excel.Range)startCell.End[Excel.XlDirection.xlDown];

            int lastRow = lastCell.Row;
            int firstRow = startCell.Row;

            if (lastRow == firstRow && lastCell.Value == null)
            {
                return firstRow;
            }

            if (lastRow == 1048576)
            {
                Excel.Range checkRange = startCell.EntireColumn;
                lastCell = ((Excel.Range)checkRange.Cells[checkRange.Rows.Count, 1]).End[Excel.XlDirection.xlUp];
                lastRow = lastCell.Row;
            }

            return lastRow;
        }
        catch (Exception)
        {
            return range.Row;
        }
    }

    /// <summary>
    /// 获取Range的最后有效列号
    /// </summary>
    public static int LastColumn(this Excel.Range range)
    {
        try
        {
            Excel.Range startCell = (Excel.Range)range.Cells[1, 1];
            Excel.Range lastCell = (Excel.Range)startCell.End[Excel.XlDirection.xlToRight];

            int lastCol = lastCell.Column;
            int firstCol = startCell.Column;

            if (lastCol == firstCol && lastCell.Value == null)
            {
                return firstCol;
            }

            if (lastCol == 16384)
            {
                Excel.Range checkRange = (Excel.Range)startCell.EntireRow;
                lastCell =  ((Excel.Range)checkRange.Cells[1, checkRange.Columns.Count]).End[Excel.XlDirection.xlToLeft];
                lastCol = lastCell.Column;
            }

            return lastCol;
        }
        catch (Exception)
        {
            return range.Column;
        }
    }
}
