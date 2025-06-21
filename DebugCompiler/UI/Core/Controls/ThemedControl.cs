using DebugCompiler.UI.Core.Interfaces;
using DebugCompiler.UI.Core.Singletons;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace DebugCompiler.UI.Core.Controls
{
    public class ThemedControl : UserControl, IThemeableControl
    {
        public ThemedControl()
        {
            UIThemeManager.RegisterControl(this);
            UIThemeManager.ThemeChanged += OnThemeChanged;
        }

        public virtual void ApplyTheme(UIThemeInfo theme)
        {
            this.BackColor = theme.ControlBackColor;
            this.ForeColor = theme.TextColor;

            foreach (Control control in GetThemedControls())
            {
                if (control is IThemeableControl themedControl)
                {
                    themedControl.ApplyTheme(theme);
                }
            }
        }

        public virtual IEnumerable<Control> GetThemedControls()
        {
            foreach (Control control in Controls)
            {
                yield return control;
            }
        }

        protected virtual void OnThemeChanged(UIThemeInfo theme)
        {
            ApplyTheme(theme);
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            UIThemeManager.RegisterControl(e.Control);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UIThemeManager.ThemeChanged -= OnThemeChanged;
            }
            base.Dispose(disposing);
        }
    }
}