using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace T7CompilerGUI.Helpers
{
    public static class WindowHelper
    {
        const int SW_RESTORE = 9;

        [DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr handle);
        
        [DllImport("User32.dll")]
        private static extern bool ShowWindow(IntPtr handle, int nCmdShow);
        
        [DllImport("User32.dll")]
        private static extern bool IsIconic(IntPtr handle);

        public static void BringProcessToFront(Process[] process)
        {
            IntPtr handle = IntPtr.Zero;
            for (int i = 0; i < process.Length; i++)
            {
                if (process[i] != null && !process[i].HasExited)
                {
                    handle = process[i].MainWindowHandle;
                    if (handle != IntPtr.Zero)
                        break;
                }
            }

            if (handle == IntPtr.Zero)
                return;

            if (IsIconic(handle))
            {
                ShowWindow(handle, SW_RESTORE);
            }

            SetForegroundWindow(handle);
        }
    }
}

