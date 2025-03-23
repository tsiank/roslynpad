using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using ExcelDna.Integration;
using ExcelDna.Integration.CustomUI;
using UserControl = System.Windows.Controls.UserControl;

#nullable disable

namespace RoslynPad
{
    internal static class CTPManager
    {
        // 使用字典存储每个工作簿的 TaskPane
        private static readonly Dictionary<object, CustomTaskPane> _ctpInstances = new Dictionary<object, CustomTaskPane>();
        private static UserControl _wpfControl;

        private static CustomTaskPane _currentTaskPane;
        
        internal static CustomTaskPane CTP => _currentTaskPane;
        internal static UserControl CTPUserControl => _wpfControl;
        public static CTPMainWindow ShowCTP()
        {
            // 获取当前活动的工作簿
            var excelApp = ExcelDnaUtil.Application as Microsoft.Office.Interop.Excel.Application;
            var currentWorkbook = excelApp?.ActiveWorkbook;

            if (currentWorkbook == null) return null;

            // 检查当前工作簿是否已有 TaskPane
            if (!_ctpInstances.TryGetValue(currentWorkbook, out CustomTaskPane ctp) || ctp == null)
            {
                // 为当前工作簿创建新的 TaskPane
                _wpfControl = new CTPMainWindow();
                ctp = GetCTP(_wpfControl, "Excel宏编辑器");
                _ctpInstances[currentWorkbook] = ctp;
                _currentTaskPane = ctp;

                // 设置初始可见性和宽度
                ctp.Visible = true;
                SetTaskPaneWidthToThirdOfExcelWindow(ctp);

                // 绑定事件
                ctp.DockPositionStateChange += Ctp_DockPositionStateChange;
                ctp.VisibleStateChange += Ctp_VisibleStateChange;
            }
            else
            {
                // 如果已有 TaskPane，则直接显示
                ctp.Visible = true;
            }

            // 清理已关闭的工作簿对应的 TaskPane（可选）
            CleanupClosedWorkbooks(excelApp);
            return (CTPMainWindow)_wpfControl;
        }

        public static void DeleteCTP()
        {
            var excelApp = ExcelDnaUtil.Application as Microsoft.Office.Interop.Excel.Application;
            var currentWorkbook = excelApp?.ActiveWorkbook;

            if (currentWorkbook != null && _ctpInstances.TryGetValue(currentWorkbook, out CustomTaskPane ctp) && ctp != null)
            {
                ctp.Delete();
                _ctpInstances.Remove(currentWorkbook);
            }
        }

        private static CustomTaskPane GetCTP(UserControl wpf, string titleName)
        {
            System.Windows.Forms.UserControl userControl = new System.Windows.Forms.UserControl();
            ElementHost elementHost = new ElementHost
            {
                Child = wpf,
                Dock = DockStyle.Fill
            };
            userControl.Controls.Add(elementHost);
            var ctp = CustomTaskPaneFactory.CreateCustomTaskPane(userControl, titleName);
            return ctp;
        }

        private static async void Ctp_VisibleStateChange(CustomTaskPane customTaskPaneInst)
        {
            if (!customTaskPaneInst.Visible && _wpfControl != null)
            {
                var ctpWindow = (CTPMainWindow)_wpfControl;
                await ctpWindow.RequestClose();
            }
        }

        private static void Ctp_DockPositionStateChange(CustomTaskPane customTaskPaneInst)
        {
            SetTaskPaneWidthToThirdOfExcelWindow(customTaskPaneInst);
        }

        private static void SetTaskPaneWidthToThirdOfExcelWindow(CustomTaskPane taskPane)
        {
            if (taskPane == null) return;

            var excelApp = ExcelDnaUtil.Application as Microsoft.Office.Interop.Excel.Application;
            var excelWindowWidthPoints = excelApp.Width;

            IntPtr excelHwnd = new IntPtr(excelApp.Hwnd);
            GetWindowRect(excelHwnd, out RECT excelRect);
            int excelWindowWidthPixels = excelRect.Width;

            var desiredWidth = (int)(excelWindowWidthPixels / 2.5 + 80);
            taskPane.Width = desiredWidth;
        }

        // 清理已关闭的工作簿对应的 TaskPane
        private static void CleanupClosedWorkbooks(Microsoft.Office.Interop.Excel.Application excelApp)
        {
            var workbooks = excelApp.Workbooks;
            var activeWorkbooks = new HashSet<object>();
            for (int i = 1; i <= workbooks.Count; i++)
            {
                activeWorkbooks.Add(workbooks[i]);
            }

            var closedWorkbooks = _ctpInstances.Keys.Where(wb => !activeWorkbooks.Contains(wb)).ToList();
            foreach (var workbook in closedWorkbooks)
            {
                if (_ctpInstances.TryGetValue(workbook, out var ctp) && ctp != null)
                {
                    ctp.Delete();
                }
                _ctpInstances.Remove(workbook);
            }
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
