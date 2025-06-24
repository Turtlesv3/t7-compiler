using DebugCompiler.UI.Core.Interfaces;
using System;
using System.Windows.Forms;
using DebugCompiler.Properties;

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