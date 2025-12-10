using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using T7CompilerGUI.Forms;

namespace T7CompilerGUI
{
    static class Program
    {
        #region Console Allocation (for compiler output)
        
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
        #endregion
        
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Allocate console for compiler output (like original T7-Compiler-UI)
            AllocConsole();
#if DEBUG
            ShowWindow(GetConsoleWindow(), 1); // Show console in debug
#else
            ShowWindow(GetConsoleWindow(), 0); // Hide console in release
#endif
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}

