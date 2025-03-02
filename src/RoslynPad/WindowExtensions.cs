using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace RoslynPad;

public static partial class WindowExtensions
{
    public static void UseImmersiveDarkMode(this Window window, bool value)
    {
        var hwnd = new WindowInteropHelper(window).EnsureHandle();
        var error = DwmSetWindowAttribute(
            hwnd,
            DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE,
            ref value,
            Marshal.SizeOf<bool>());

        if (error != 0)
        {
            throw new Win32Exception(error);
        }
    }

    //[LibraryImport("dwmapi")]
    //private static partial int DwmSetWindowAttribute(
    //    IntPtr hwnd,
    //    DwmWindowAttribute attribute,
    //    [MarshalAs(UnmanagedType.Bool)] in bool pvAttribute,
    //    int cbAttribute);

    // 替换LibraryImport为DllImport，并移除分部方法声明
    [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        DwmWindowAttribute attribute,
        [MarshalAs(UnmanagedType.Bool)] ref bool pvAttribute, // 移除'in'修饰符，改为ref
        int cbAttribute);

    // 添加一个公共包装方法以简化调用
    public static bool SetDarkMode(IntPtr hwnd, bool enable)
    {
        bool attributeValue = enable;
        return DwmSetWindowAttribute(
            hwnd,
            DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE,
            ref attributeValue,
            Marshal.SizeOf(attributeValue)) == 0;
    }

    private enum DwmWindowAttribute
    {
        DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
    }
}
