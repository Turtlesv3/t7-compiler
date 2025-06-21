using DebugCompiler.UI.Core.Interfaces;
using DebugCompiler.UI.Core.Singletons;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel.Design;
using System.Windows.Forms.Design;
using System.Drawing.Design;
using System.Collections;
using DebugCompiler.UI.Core.Controls;

namespace DebugCompiler.UI.Core.Singletons
{
    public static class ThemeExtensions
    {
        public static void RegisterThemeHandler(this Control control, Action<UIThemeInfo> handler)
        {
            UIThemeManager.RegisterControl(control);
            UIThemeManager.ThemeChanged += handler;
        }

        public static void ApplyThemeToChildren(this Control parent, UIThemeInfo theme)
        {
            foreach (Control child in parent.Controls)
            {
                if (child is IThemeableControl themedChild)
                {
                    themedChild.ApplyTheme(theme);
                }
                child.ApplyThemeToChildren(theme);
            }
        }
    }
}