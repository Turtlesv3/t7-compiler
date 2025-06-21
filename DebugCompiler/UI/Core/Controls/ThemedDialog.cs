using DebugCompiler.UI.Core.Interfaces;
using DebugCompiler.UI.Core.Singletons;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace DebugCompiler.UI.Core.Controls
{
    public class ThemedDialog : Form, IThemeableControl
    {
        public ThemedDialog()
        {
            UIThemeManager.RegisterControl(this);
            UIThemeManager.ThemeChanged += OnThemeChanged;
        }

        public virtual void ApplyTheme(UIThemeInfo theme)
        {
            this.BackColor = theme.BackColor;
            this.ForeColor = theme.TextColor;

            // Theme all child controls that implement IThemeableControl
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
            // Yield all child controls that need theming
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