using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ExcelDna.Integration;
using ExcelDna.Integration.CustomUI;
using System.Windows.Controls;

using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Interop;
using System.Windows.Threading;

using Excel = Microsoft.Office.Interop.Excel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Mono.Cecil.Cil;

using RoslynPad.OfficeAddInEdtitor;
using RoslynPad.UI;
using Microsoft.CodeAnalysis;

namespace RoslynPad;

[ComVisible(true)]
public class MyRibbon : ExcelRibbon
{
    public required App app;

    //public excel.Application Excel = ExcelDnaUtil.Application as excel.Application;

    public override string GetCustomUI(string RibbonID)
    {
        return RibbonResources.Ribbon;
    }

    public override object LoadImage(string imageId)
    {
        return RibbonResources.ResourceManager.GetObject(imageId);
    }

    public void onload(IRibbonUI ribbonUI)
    {

    }

    public void OnButtonPressed(IRibbonControl control)
    {
        Thread thread = new Thread(() =>
        {
            app = new App();
            app.InitializeComponent();
            app.MainWindow.Closed += (a, b) => { app.MainWindow.Dispatcher.InvokeShutdown(); };
            Dispatcher.Run();
        })
        { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        
    }

    [STAThread]
    public void OnButtonPressedSingle(IRibbonControl control)
    {
        app = new App();
        app.InitializeComponent();
        app.Run();
    }


    public void OnButtonPressedCTP(IRibbonControl control)
    {

        //CTPManager.ShowCTP();
    }


    public async void OnButtonRunScript(IRibbonControl control)
    {
        var dataContext = (MainViewModel)app.MainWindow.DataContext;
        var code = await dataContext!.CurrentOpenDocument!.LoadTextAsync();

        ExcelAsyncExtensions.RunCSharpScript(code);
    }

}
