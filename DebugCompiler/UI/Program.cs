using DebugCompiler.UI.Core.Singletons;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using DebugCompiler.Properties;

namespace DebugCompiler
{
    public static class Program
    {
        // Track if we're in console mode
        private static bool _isConsoleMode = false;

        // For cleanup tracking
        private static readonly List<IDisposable> _disposables = new List<IDisposable>();
        private static readonly List<IntPtr> _handlesToClose = new List<IntPtr>();
        private static readonly List<Process> _childProcesses = new List<Process>();

        [STAThread]
        static void Main(string[] args)
        {
            // Setup unhandled exception handlers
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;

            try
            {
                if (args.Contains("--console"))
                {
                    _isConsoleMode = true;
                    var consoleArgs = args.Where(a => a != "--console").ToArray();
                    Environment.ExitCode = RunConsoleMode(consoleArgs);
                }
                else
                {
                    RunGuiMode();
                }
            }
            finally
            {
                PerformCleanup();
            }
        }

        private static int RunConsoleMode(string[] args)
        {
            try
            {
                using (var rootInstance = new Root())
                {
                    // Track for cleanup
                    RegisterForCleanup(rootInstance);

                    return Root.RunCommandLine(args);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Fatal error: {ex}");
                return -1;
            }
        }

        private static void RunGuiMode()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Initialize theme manager FIRST
            InitializeThemeManager();

            // Then create the form
            var mainForm = new MainForm1();
            RegisterForCleanup(mainForm);
            Application.Run(mainForm);
        }

        private static void InitializeThemeManager()
        {
            try
            {
                string savedThemeName = UIThemeManager.LoadTheme();
                UIThemeInfo theme;

                if (string.IsNullOrWhiteSpace(savedThemeName))
                {
                    theme = UIThemeInfo.Dark;
                }
                else
                {
                    theme = UIThemeInfo.GetThemeByName(savedThemeName);
                    if (theme.Equals(default(UIThemeInfo)))
                        theme = UIThemeInfo.Dark;
                }

                // Critical: Initialize before any forms are created
                UIThemeManager.InitializeWithTheme(theme);

                // Verify theme application
                Debug.WriteLine($"Theme initialized: {UIThemeManager.CurrentTheme.Name}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Theme initialization failed: {ex.Message}");
                UIThemeManager.InitializeWithTheme(UIThemeInfo.Dark);
            }
        }

        private static void PerformCleanup()
        {
            // Clean up in reverse order of registration
            foreach (var disposable in _disposables.Reverse<IDisposable>())
            {
                try
                {
                    disposable?.Dispose();
                }
                catch { /* Suppress cleanup errors */ }
            }

            // Close any remaining handles
            foreach (var handle in _handlesToClose)
            {
                if (handle != IntPtr.Zero)
                {
                    try
                    {
                        Kernel32.CloseHandle(handle);
                    }
                    catch { /* Suppress */ }
                }
            }

            // Kill any child processes we spawned
            foreach (var process in _childProcesses)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
                catch { /* Suppress */ }
            }

            // Additional COM cleanup if needed
            try
            {
                while (Marshal.ReleaseComObject(new object()) > 0) { }
            }
            catch { /* Suppress */ }

            // If we're in GUI mode, ensure all forms are closed
            if (!_isConsoleMode)
            {
                try
                {
                    foreach (Form form in Application.OpenForms)
                    {
                        form?.Close();
                        form?.Dispose();
                    }
                }
                catch { /* Suppress */ }
            }
        }

        private static void RegisterForCleanup(IDisposable disposable)
        {
            _disposables.Add(disposable);
        }

        private static void RegisterHandle(IntPtr handle)
        {
            _handlesToClose.Add(handle);
        }

        public static void RegisterChildProcess(Process process)
        {
            _childProcesses.Add(process);
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            HandleFatalError(e.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleFatalError(e.ExceptionObject as Exception);
        }

        private static void HandleFatalError(Exception ex)
        {
            try
            {
                string errorMsg = $"Fatal error: {ex?.ToString() ?? "Unknown error"}";

                if (_isConsoleMode)
                {
                    Console.Error.WriteLine(errorMsg);
                }
                else
                {
                    MessageBox.Show(errorMsg, "Fatal Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                Environment.ExitCode = -1;
                PerformCleanup();
                Environment.Exit(Environment.ExitCode);
            }
        }

        // Native methods for handle cleanup
        private static class Kernel32
        {
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CloseHandle(IntPtr hObject);
        }
    }
}