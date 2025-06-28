using DebugCompiler.UI.Core.Interfaces;
using System;
using System.Windows.Forms;
using DebugCompiler.Properties;

namespace DebugCompiler.UI.Core.Singletons
{
    public static class ThemeExtensions
    {
        public static void UnregisterThemeHandler(this Control control, Action<UIThemeInfo> handler)
        {
            UIThemeManager.UnregisterControl(control);
            UIThemeManager.ThemeChanged -= handler;
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

        public static void SafeThemeUpdate(this Control control, Action<UIThemeInfo> action)
        {
            if (control == null || control.IsDisposed) return;

            if (control.InvokeRequired)
            {
                control.BeginInvoke((Action)(() => SafeThemeUpdate(control, action)));
            }
            else
            {
                action(UIThemeManager.CurrentTheme);
                control.Invalidate();
            }
        }

        public static void RecursiveThemeUpdate(this Control parent, UIThemeInfo theme)
        {
            if (parent == null || parent.IsDisposed) return;

            foreach (Control child in parent.Controls)
            {
                if (child is IThemeableControl themeable)
                {
                    themeable.ApplyTheme(theme);
                }
                else
                {
                    UIThemeManager.ApplyDefaultTheme(child);
                }

                if (child.HasChildren)
                {
                    child.RecursiveThemeUpdate(theme);
                }
            }
        }

        public static void RegisterWithTheme(this Control control, Action<UIThemeInfo> themeHandler = null)
        {
            UIThemeManager.RegisterControl(control);
            if (themeHandler != null)
            {
                UIThemeManager.ThemeChanged += themeHandler;
                control.Disposed += (s, e) =>
                    UIThemeManager.ThemeChanged -= themeHandler;
            }
        }
    }
}