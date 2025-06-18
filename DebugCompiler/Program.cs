using System;
using System.Linq;
using System.Windows.Forms;

namespace DebugCompiler
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Contains("--console"))
            {
                var consoleArgs = args.Where(a => a != "--console").ToArray();
                Environment.Exit(Root.RunCommandLine(consoleArgs));
            }
            else
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm1());
            }
        }
    }
}