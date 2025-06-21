using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TreyarchCompiler;
using T7CompilerLib;
using TreyarchCompiler.Enums;
using Games = TreyarchCompiler.Enums.Games;
using T7CompilerLib.OpCodes;
using XDevkit;
using Microsoft.Test.Xbox.XDRPC;
using TreyarchCompiler.Utilities;
using System.Windows.Forms.VisualStyles;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Reflection;
using System.Net;
using T89CompilerLib;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.ComponentModel.Design;
using System.Windows.Forms.Design;
using System.Drawing.Design;
using System.Collections;
using DebugCompiler.UI.Core.Singletons;
using DebugCompiler.UI.Core.Interfaces;
using DebugCompiler.UI.Core.Controls;

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