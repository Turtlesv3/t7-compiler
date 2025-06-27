using DebugCompiler.UI.Core.Interfaces;
using DebugCompiler.UI.Core.Singletons;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace DebugCompiler.UI.Core.Controls
{
    [Designer(typeof(FormDocumentDesigner))]
    [DesignerCategory("Form")]
    public class ThemedDialog : Form, IThemeableControl
    {
        public ThemedDialog()
        {
            // Skip theme registration in design mode
            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
            {
                UIThemeManager.RegisterControl(this);
                UIThemeManager.ThemeChanged += OnThemeChanged;
            }

            // Basic styling that works in designer
            if (DesignMode)
            {
                this.BackColor = SystemColors.Window;
                this.ForeColor = SystemColors.WindowText;
                this.Font = SystemFonts.DialogFont;
            }
        }

        public virtual void ApplyTheme(UIThemeInfo theme)
        {
            if (IsDisposed || DesignMode || theme == null) return;

            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action<UIThemeInfo>(ApplyTheme), theme);
                    return;
                }

                this.SuspendLayout();
                this.BackColor = theme.BackColor;
                this.ForeColor = theme.TextColor;

                foreach (Control control in GetThemedControls())
                {
                    if (control is IThemeableControl themedControl)
                        themedControl.ApplyTheme(theme);
                }
            }
            finally
            {
                this.ResumeLayout(true);
            }
        }

        public virtual IEnumerable<Control> GetThemedControls()
        {
            foreach (Control control in Controls)
                yield return control;
        }

        protected virtual void OnThemeChanged(UIThemeInfo theme)
        {
            if (!DesignMode && IsHandleCreated && !IsDisposed)
                ApplyTheme(theme);
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            if (!DesignMode)
                UIThemeManager.RegisterControl(e.Control);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !DesignMode)
            {
                UIThemeManager.ThemeChanged -= OnThemeChanged;
            }
            base.Dispose(disposing);
        }
    }

    // Custom designer class to properly handle the base form
    public class FormDocumentDesigner : DocumentDesigner
    {
        public FormDocumentDesigner()
        {
            this.AutoResizeHandles = true;
        }

        protected override void PreFilterProperties(System.Collections.IDictionary properties)
        {
            base.PreFilterProperties(properties);
            // Ensure all properties are visible in designer
            properties["BackColor"] = TypeDescriptor.CreateProperty(
                typeof(FormDocumentDesigner),
                (PropertyDescriptor)properties["BackColor"],
                new Attribute[0]);
            properties["ForeColor"] = TypeDescriptor.CreateProperty(
                typeof(FormDocumentDesigner),
                (PropertyDescriptor)properties["ForeColor"],
                new Attribute[0]);
        }
    }
}