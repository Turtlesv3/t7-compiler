﻿using DebugCompiler;
using DebugCompiler.UI.Core.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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