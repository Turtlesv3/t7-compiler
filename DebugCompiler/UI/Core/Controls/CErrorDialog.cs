using System;
using System.Collections.Generic;
using System.Windows.Forms;
using DebugCompiler.UI.Core.Interfaces;
using DebugCompiler.UI.Core.Singletons;

namespace DebugCompiler.UI.Core.Controls
{
    public partial class CErrorDialog : ThemedDialog
    {
        public CErrorDialog(string title, string description)
        {
            try
            {
                InitializeComponent(); // This must complete first

                // Safe initialization
                if (InnerForm != null)
                {
                    InnerForm.TitleBarTitle = title;
                }

                Text = title;
                MaximizeBox = MinimizeBox = true;

                // Null-check before accessing ErrorRTB
                if (ErrorRTB != null)
                {
                    ErrorRTB.Text = description ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                // Log error or fallback handling
                Console.WriteLine($"Error initializing CErrorDialog: {ex.Message}");
                throw;
            }
        }

        public override void ApplyTheme(UIThemeInfo theme)
        {
            // Check for default struct instead of null
            if (IsDisposed || !IsHandleCreated || theme.Equals(default(UIThemeInfo)))
                return;

            try
            {
                base.ApplyTheme(theme);

                // Safe theming with null checks
                if (ErrorRTB != null)
                {
                    ErrorRTB.BackColor = theme.TextBoxBackColor;
                    ErrorRTB.ForeColor = theme.TextColor;
                    ErrorRTB.BorderStyle = theme.TextBoxBorderStyle;
                }

                if (AcceptButton != null)
                {
                    AcceptButton.BackColor = theme.ButtonBackColor;
                    AcceptButton.ForeColor = theme.TextColor;
                    AcceptButton.FlatStyle = theme.ButtonFlatStyle;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying theme: {ex.Message}");
            }
            finally
            {
                Invalidate(true);
            }
        }

        public override IEnumerable<Control> GetThemedControls()
        {
            if (InnerForm != null) yield return InnerForm;
            if (ErrorRTB != null) yield return ErrorRTB;
            if (AcceptButton != null) yield return AcceptButton;
        }

        private void AcceptButton_Click(object sender, EventArgs e)
        {
            try
            {
                Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error closing dialog: {ex.Message}");
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
                        form.Invoke(new Action(() =>
                            new CErrorDialog(title, description) { TopMost = topMost }.ShowDialog()));
                    }
                    else
                    {
                        new CErrorDialog(title, description) { TopMost = topMost }.ShowDialog();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing error dialog: {ex.Message}");
                // Fallback to MessageBox if our dialog fails
                MessageBox.Show(description, title,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}