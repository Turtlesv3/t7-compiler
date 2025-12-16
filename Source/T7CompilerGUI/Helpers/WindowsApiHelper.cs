using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace T7CompilerGUI.Helpers
{
    /// <summary>
    /// Minimal Windows API helper for functions not exposed by ReaLTaiizor's internal WinApi class
    /// Consolidates Windows API calls to avoid duplication
    /// </summary>
    internal static class WindowsApiHelper
    {
        #region Constants

        public const int SB_HORZ = 0;
        public const int SB_VERT = 1;
        public const int SB_BOTH = 3;

        public const int WM_NCLBUTTONDOWN = 0x00A1;
        public const int WM_PAINT = 0x000F;
        public const int HT_CAPTION = 0x2;

        public const int SW_RESTORE = 9;
        public const int SW_SHOW = 5;

        #endregion

        #region User32.dll Functions

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32", CharSet = CharSet.Auto)]
        public static extern bool InvalidateRect(IntPtr hwnd, ref Rectangle rect, bool bErase);

        [DllImport("user32", CharSet = CharSet.Auto)]
        public static extern bool UpdateWindow(IntPtr hwnd);

        #endregion

        #region Helper Methods

        /// <summary>
        /// Hides the scrollbar corner by hiding both scrollbars
        /// </summary>
        public static void HideScrollbarCorner(IntPtr hWnd)
        {
            if (hWnd != IntPtr.Zero)
            {
                ShowScrollBar(hWnd, SB_HORZ, false);
                ShowScrollBar(hWnd, SB_VERT, false);
            }
        }

        /// <summary>
        /// Simulates a title bar drag to allow window dragging
        /// </summary>
        public static void DragWindow(IntPtr hWnd)
        {
            ReleaseCapture();
            SendMessage(hWnd, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }

        #endregion
    }
}

