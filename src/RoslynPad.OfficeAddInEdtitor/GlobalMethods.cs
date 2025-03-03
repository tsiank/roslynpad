using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExcelDna.Integration;
using Excel = Microsoft.Office.Interop.Excel;

namespace RoslynPad.OfficeAddInEdtitor;

public class GlobalMethods
{
    public Excel.Application ExcelApp => ExcelDnaUtil.Application as Excel.Application;
    public Excel.Workbook ActiveWorkbook => ExcelApp.ActiveWorkbook;
    public Excel.Worksheet ActiveSheet => (Excel.Worksheet)ExcelApp.ActiveSheet;
}
