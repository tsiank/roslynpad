using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExcelDna.Integration;
using ExcelDna.Integration.CustomUI;
using Excel = Microsoft.Office.Interop.Excel;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Runtime.InteropServices;

namespace RoslynPad
{
        internal static class CTPManager
        {
            static CustomTaskPane? _ctp;
            static System.Windows.Controls.UserControl? _wpfControl;

        public static CustomTaskPane? CTP => _ctp;
        public static System.Windows.Controls.UserControl? CTPUserControl => _wpfControl;
            public static CustomTaskPane GetCTP(System.Windows.Controls.UserControl wpf, string TitleName)
            {
                System.Windows.Forms.UserControl userControl1 = new System.Windows.Forms.UserControl();
                ElementHost elementHost = new ElementHost();
                elementHost.Child = (UIElement)wpf;
                elementHost.Dock = DockStyle.Fill;
                userControl1.Controls.Add((System.Windows.Forms.Control)elementHost);
                var ctp = CustomTaskPaneFactory.CreateCustomTaskPane((object)userControl1, TitleName);
                return ctp;
            }


            public static void ShowCTP(System.Windows.Controls.UserControl wpfControl)
            {
                _wpfControl = wpfControl;

                if (_ctp == null)
                {
                    _ctp = GetCTP(wpfControl, "C#脚本编辑器");
                    _ctp.Visible = true;

                    SetTaskPaneWidthToThirdOfExcelWindow(_ctp);

                    _ctp.DockPositionStateChange += Ctp_DockPositionStateChange;
                    _ctp.VisibleStateChange += Ctp_VisibleStateChange;
                }
                else
                {

                    _ctp.Visible = true;
                }
            }

            public static void DeleteCTP()
            {
                if (_ctp != null)
                {
                    //ctp.Delete();
                    //ctp = null;
                    //ctp.Visible = false;
                }

            }

            static void Ctp_VisibleStateChange(CustomTaskPane CustomTaskPaneInst)
            {
                if (_ctp != null && _ctp.Visible == false)
                {
                    DeleteCTP();
                }
            }

            static void Ctp_DockPositionStateChange(CustomTaskPane CustomTaskPaneInst)
            {
                SetTaskPaneWidthToThirdOfExcelWindow(CustomTaskPaneInst);
            }



            private static void SetTaskPaneWidthToThirdOfExcelWindow(CustomTaskPane taskPane)
            {
                if (taskPane == null) return;

                var excelApp = ExcelDnaUtil.Application;
                var excelWindowWidthPoints = excelApp.Width;

                IntPtr excelHwnd = new IntPtr(excelApp.Hwnd);
                GetWindowRect(excelHwnd, out RECT excelRect);
                int excelWindowWidthPixels = excelRect.Width;

                var desiredWidth = (int)(excelWindowWidthPixels / 2.5);
                taskPane.Width = desiredWidth;

                //_wpfControl.CodeEditor.Height = taskPane.Height / 1.75 - 80;
            }

            [DllImport("user32.dll")]
            public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

            [StructLayout(LayoutKind.Sequential)]
            public struct RECT
            {
                public int Left;
                public int Top;
                public int Right;
                public int Bottom;
                public int Width => Right - Left;
                public int Height => Bottom - Top;
            }

        }
    }

