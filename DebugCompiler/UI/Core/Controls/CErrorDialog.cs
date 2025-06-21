using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DebugCompiler.UI.Core.Interfaces;
using DebugCompiler.UI.Core.Singletons;

namespace DebugCompiler.UI.Core.Controls
{
    public partial class CErrorDialog : ThemedDialog
    {
        // Theme debugging switch
        private const bool ThemeDebugging = true;

        public CErrorDialog(string title, string description)
        {
            try
            {
                if (ThemeDebugging) Debug.WriteLine($"Initializing CErrorDialog: {title}");

                InitializeComponent(); // Must be first

                // Safe initialization with null checks
                Text = title ?? "Error";
                MaximizeBox = MinimizeBox = true;

                if (InnerForm != null)
                {
                    InnerForm.TitleBarTitle = title;
                    if (ThemeDebugging) Debug.WriteLine($"Set InnerForm title: {title}");
                }

                if (ErrorRTB != null)
                {
                    ErrorRTB.Text = description ?? string.Empty;
                    if (ThemeDebugging) Debug.WriteLine($"Set ErrorRTB text: {description?.Length ?? 0} chars");
                }

                // Force early theme application
                if (!DesignMode && !UIThemeManager.CurrentTheme.Equals(default(UIThemeInfo)))
                {
                    ApplyTheme(UIThemeManager.CurrentTheme);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing CErrorDialog: {ex}");
                throw new InvalidOperationException("Failed to initialize error dialog", ex);
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // Final theme verification
            if (!DesignMode)
            {
                if (ThemeDebugging) Debug.WriteLine("OnShown - Applying theme");
                ApplyThemeWithRefresh(UIThemeManager.CurrentTheme);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // Custom cleanup code here
            base.OnFormClosed(e);
        }

        public override void ApplyTheme(UIThemeInfo theme)
        {
            if (IsDisposed || DesignMode || theme.Equals(default(UIThemeInfo)))
                return;

            try
            {
                if (ThemeDebugging) Debug.WriteLine($"Begin ApplyTheme: {theme.Name}");

                base.ApplyTheme(theme); // Critical base call

                // Theme all registered controls
                foreach (var control in GetThemedControls())
                {
                    ThemeControl(control, theme);
                }

                // Additional theming for known controls
                ThemeControl(ErrorRTB, theme);
                ThemeControl(AcceptButton, theme);

                if (ThemeDebugging) Debug.WriteLine($"Applied theme: {theme.Name}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ApplyTheme: {ex}");
            }
            finally
            {
                Invalidate(true);
            }
        }

        private void ApplyThemeWithRefresh(UIThemeInfo theme)
        {
            ApplyTheme(theme);
            Refresh();
            Update();
        }

        private void ThemeControl(Control control, UIThemeInfo theme)
        {
            if (control?.IsDisposed != false) return;

            try
            {
                switch (control)
                {
                    case IThemeableControl themedControl:
                        themedControl.ApplyTheme(theme);
                        break;
                    case RichTextBox rtb:
                        rtb.BackColor = theme.TextBoxBackColor;
                        rtb.ForeColor = theme.TextColor;
                        rtb.BorderStyle = theme.TextBoxBorderStyle;
                        break;
                    case Button btn:
                        btn.BackColor = theme.ButtonBackColor;
                        btn.ForeColor = theme.TextColor;
                        btn.FlatStyle = theme.ButtonFlatStyle;
                        break;
                    case Panel panel:
                        panel.BackColor = theme.ControlBackColor;
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error theming {control.Name}: {ex}");
            }
        }

        public override IEnumerable<Control> GetThemedControls()
        {
            if (InnerForm != null) yield return InnerForm;
            if (ErrorRTB != null) yield return ErrorRTB;
            if (AcceptButton != null) yield return AcceptButton;

            // Add any additional controls that need theming
            foreach (Control ctrl in Controls)
            {
                if (ctrl != InnerForm && ctrl != ErrorRTB && ctrl != AcceptButton)
                    yield return ctrl;
            }
        }

        private void AcceptButton_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error closing dialog: {ex}");
            }
        }

        public static void Show(string title, string description, bool topMost = false)
        {
            try
            {
                if (Application.OpenForms.Count > 0)
                {
                    var form = Application.OpenForms[0];
                    if (form.InvokeRequired)
                    {
                        form.Invoke((Action)(() => ShowDialog(title, description, topMost)));
                    }
                    else
                    {
                        ShowDialog(title, description, topMost);
                    }
                }
                else
                {
                    ShowDialog(title, description, topMost);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing dialog: {ex}");
                MessageBox.Show(description, title,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static void ShowDialog(string title, string description, bool topMost)
        {
            using var dialog = new CErrorDialog(title, description) { TopMost = topMost };
            dialog.ApplyThemeWithRefresh(UIThemeManager.CurrentTheme);
            dialog.ShowDialog();
        }
    }
}