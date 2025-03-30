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

using RoslynPad.UI;
using Microsoft.CodeAnalysis;
using AvalonDock.Layout;
using AvalonDock;
using System.Windows.Media;
using System.Windows;
using OfficeSharp.SettingsUI;
using System.IO;
using Microsoft.Office.Interop.Excel;
using System.Diagnostics;


namespace OfficeSharp;

[ComVisible(true)]
public class MyRibbon : ExcelRibbon
{
    private static App? _IDEApp;

    private CTPMainWindow? _ctpMainWindow;
    private static MainWindow? _mainWindow;

    private static Thread? _windowThread;
    private static readonly object _lock = new object();

    public override string GetCustomUI(string RibbonID)
    {
        return RibbonResources.Ribbon;
    }

    public override object LoadImage(string imageId)
    {
        return RibbonResources.ResourceManager.GetObject(imageId);
    }

    public void Onload(IRibbonUI ribbonUI)
    {
        lock (_lock)
        {
            if (_IDEApp == null)
            {
                _IDEApp = new App();
                _IDEApp.InitializeComponent();
                _IDEApp.ShutdownMode = ShutdownMode.OnExplicitShutdown; // 手动控制关闭
            }
        }
    }

    // 清理资源
    public void ExitCTPOrIDE(IRibbonUI ribbonUI)
    {
        lock (_lock)
        {
            if (_windowThread != null && _windowThread.IsAlive)
            {
                _mainWindow?.Dispatcher.Invoke(() =>
                {
                    _mainWindow?.Close();
                });
            }
            if (_IDEApp != null)
            {
                _IDEApp.Dispatcher.Invoke(() =>
                {
                    _IDEApp.Shutdown();
                });
            }
            _windowThread = null;
            _IDEApp = null;
            _mainWindow = null;
        }

        CTPManager.DeleteCTP();
    }

    public void OnButtonPressedIDE(IRibbonControl control)
    {
        lock (_lock)
        {
            // 如果窗口不存在或线程已停止，创建新窗口和线程
            if (_windowThread == null || !_windowThread.IsAlive || _mainWindow == null || _mainWindow.Dispatcher.HasShutdownFinished)
            {
                CreateNewWindowThread();
            }
            else if (!_mainWindow.IsVisible)
            {
                try
                {
                    _mainWindow.Dispatcher.Invoke(() =>
                    {
                        _mainWindow.Show();
                        _mainWindow.Activate();
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"UI failed to show: {ex.Message}");
                    CreateNewWindowThread(); // 如果失败，重新创建
                }
            }
            else
            {
                try
                {
                    _mainWindow.Dispatcher.Invoke(() =>
                    {
                        _mainWindow.Activate();
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Activate UI failed: {ex.Message}");
                }
            }
        }
    }

    private void CreateNewWindowThread()
    {
        lock (_lock)
        {
            if (_windowThread != null && _windowThread.IsAlive)
            {
                _mainWindow?.Dispatcher.Invoke(() =>
                {
                    _mainWindow.Close();
                });
            }

            _windowThread = new Thread(() =>
            {
                try
                {
                    _mainWindow = new MainWindow();
                    _mainWindow.Closed += (s, e) =>
                    {
                        _mainWindow = null; // 清理引用
                    };
                    _mainWindow.Show();
                    _mainWindow.Activate();
                    Dispatcher.Run(); // 运行独立的消息循环
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"UI thread initialization failed: {ex.Message}");
                }
            });

            _windowThread.SetApartmentState(ApartmentState.STA);
            _windowThread.IsBackground = true;
            _windowThread.Start();
        }
    }


    public void OnButtonPressedSIDE(IRibbonControl control)
    {
        var appExePath = Path.Combine(AppContext.BaseDirectory, "OfficeSharp.exe");

        ProcessStartInfo processStartInfo = new ProcessStartInfo
        {
            FileName = appExePath,
            UseShellExecute = false
        };

        try
        {
            using (Process process = Process.Start(processStartInfo))
            {
                Console.WriteLine("OfficeSharp.exe is running.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Running OfficeSharp.exe error: {ex.Message}");
        }
    }

    public void OnButtonPressedCTP(IRibbonControl control)
    {
        _ctpMainWindow = CTPManager.ShowCTP();
    }

    public void OnButtonSetting(IRibbonControl control)
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.ShowDialog();
    }

    public void OnAddInManage(IRibbonControl control)
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.SettingsTabControl.SelectedIndex = 1;
        settingsWindow.ShowDialog();
    }
}

