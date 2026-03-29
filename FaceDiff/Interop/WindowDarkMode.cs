using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace FaceDiff.Interop
{
    internal static class WindowDarkMode
    {
        private const int DwmwaUseImmersiveDarkMode = 20;
        private const int DwmwaUseImmersiveDarkModeLegacy = 19;

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public static void TryApplyDarkTitleBar(Window window)
        {
            if (window == null)
                return;

            var helper = new WindowInteropHelper(window);
            nint hwnd = helper.EnsureHandle();
            if (hwnd == 0)
                return;

            int useDark = 1;
            int size = Marshal.SizeOf<int>();
            var h = (IntPtr)hwnd;
            if (DwmSetWindowAttribute(h, DwmwaUseImmersiveDarkMode, ref useDark, size) != 0)
                DwmSetWindowAttribute(h, DwmwaUseImmersiveDarkModeLegacy, ref useDark, size);
        }
    }
}
