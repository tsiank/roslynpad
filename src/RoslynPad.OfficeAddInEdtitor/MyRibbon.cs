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

//using RoslynPad;

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
        //var ExcelHWND = new IntPtr(Excel.Hwnd);
        Thread thread = new Thread(() =>
        {
            app = new App();
            app.InitializeComponent();
                
            var mainWindow = new MainWindow();
            app.MainWindow = mainWindow;

            //var Helper = new WindowInteropHelper(UI);
            //Helper.Owner = ExcelHWND;

            //mainWindow.Show();
            mainWindow.Closed += (a, b) => { mainWindow.Dispatcher.InvokeShutdown(); };
            Dispatcher.Run();
        })
        { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
    }

    [STAThread]
    public void OnButtonPressedSingle(IRibbonControl control)
    {
        // 初始化应用程序
        var app = new App();
        app.InitializeComponent();

        var mainWindow = new MainWindow();
        app.MainWindow = mainWindow;
        //mainWindow.Show();
        app.Run();
    }

    public void OnButtonPressedCTP(IRibbonControl control)
    {

        //CTPManager.ShowCTP();
    }
}
