using ExcelDna.Integration;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OfficeSharp;

public class FuncRegistration
{
    private List<string> currentFunctions;

    private Dictionary<object, List<string>> workbooks;

    public FuncRegistration()
    {
        currentFunctions = new List<string>();
        workbooks = new Dictionary<object, List<string>>();
    }

    public void RegisterMethods(List<MethodInfo> m)
    {
        UpdateFunctions(currentFunctions, m);
        ExcelIntegration.RegisterMethods(m);
    }


    public void RegisterWorkbookMethods(object workbook, List<MethodInfo> m)
    {
        if (!workbooks.TryGetValue(workbook, out var value))
        {
            value = new List<string>();
        }
        UpdateFunctions(value, m);
        workbooks[workbook] = value;
        ExcelIntegration.RegisterMethods(m);
    }

    private static void UpdateFunctions(List<string> functions, List<MethodInfo> m)
    {
        foreach (var function in functions)
        {
            Unregister(function);
        }
        functions.Clear();
        foreach (var item in m)
        {
            functions.Add(item.Name);
        }
    }

    private static void Unregister(string functionName)
    {
        var obj = XlCall.Excel(XlCall.xlfEvaluate, functionName);
        XlCall.Excel(XlCall.xlfSetName, functionName);
        XlCall.Excel(XlCall.xlfUnregister, obj);
        var obj2 = XlCall.Excel(XlCall.xlGetName);
        var obj3 = XlCall.Excel(XlCall.xlfRegister, obj2, "xlAutoRemove", "I", functionName, ExcelMissing.Value, 2);
        XlCall.Excel(XlCall.xlfSetName, functionName);
        XlCall.Excel(XlCall.xlfUnregister, obj3);
    }

}


internal class FunctionExamples
{
    public void Reg()
    {
        var type = typeof(UDFAndCommand);
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);

        ExcelIntegration.RegisterMethods(methods.ToList());
    }

    public class UDFAndCommand
    {
        [ExcelFunction]
        public static string SayHello(string name)
        {
            return "Hello " + name + "!";
        }

        [ExcelCommand]
        public static void DisplayHello()
        {
            MessageBox.Show("hi");
        }

    }
}
