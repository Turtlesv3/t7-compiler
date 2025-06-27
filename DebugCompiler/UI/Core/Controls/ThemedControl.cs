using DebugCompiler.UI.Core.Interfaces;
using DebugCompiler.UI.Core.Singletons;
using System.Collections.Generic;
using System.Windows.Forms;
using DebugCompiler.Properties;

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
            if (IsDisposed || !IsHandleCreated) return;

            this.BackColor = theme.ControlBackColor;
            this.ForeColor = theme.TextColor;

            // Let UIThemeManager handle theming of child controls
            UIThemeManager.RegisterChildControls(this);
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
            if (IsDisposed || !IsHandleCreated) return;
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
                UIThemeManager.UnregisterControl(this);
            }
            base.Dispose(disposing);
        }
    }
}