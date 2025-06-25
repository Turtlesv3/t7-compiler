using DebugCompiler.UI.Core.Controls;
using DebugCompiler.UI.Core.Interfaces;
using DebugCompiler.UI.Core.Singletons;
using DebugCompiler.UI.Core.Helpers;
using Microsoft.Test.Xbox.XDRPC;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DebugCompiler.Properties;

public static class ControlExtensions
{
    public static void SafeInvoke(this Control control, Action<Control> action)
    {
        if (control != null && !control.IsDisposed)
        {
            if (control.InvokeRequired)
            {
                control.Invoke(new Action(() => action(control)));
            }
            else
            {
                action(control);
            }
        }
    }
}