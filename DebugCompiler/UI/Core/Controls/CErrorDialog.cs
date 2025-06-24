using DebugCompiler.UI.Core.Singletons;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using DebugCompiler.Properties;

namespace DebugCompiler.UI.Core.Controls
{
    public partial class CErrorDialog : ThemedDialog
    {
        private const bool ThemeDebugging = true;

        public CErrorDialog(string title, string description)
        {
            try
            {
                if (ThemeDebugging) Debug.WriteLine($"Initializing CErrorDialog: {title}");

                InitializeComponent();

                // Set core properties
                Text = title ?? "Error";
                MaximizeBox = MinimizeBox = true;

                // Initialize controls
                if (InnerForm != null)
                {
                    InnerForm.TitleBarTitle = title;
                    InnerForm.BackColor = Color.Transparent; // Critical for proper theming
                }

                if (ErrorRTB != null)
                {
                    ErrorRTB.Text = description ?? string.Empty;
                    ErrorRTB.BorderStyle = BorderStyle.None; // Remove default border
                }

                // Force immediate theme application
                if (!DesignMode)
                {
                    this.SuspendLayout();
                    ApplyThemeWithForce(UIThemeManager.CurrentTheme);
                    this.ResumeLayout(true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing CErrorDialog: {ex}");
                throw;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (ThemeDebugging) Debug.WriteLine("OnShown - Force applying theme");
            ApplyThemeWithForce(UIThemeManager.CurrentTheme);
        }

        public override void ApplyTheme(UIThemeInfo theme)
        {
            if (IsDisposed || DesignMode || theme.Equals(default(UIThemeInfo)))
                return;

            try
            {
                if (ThemeDebugging) Debug.WriteLine($"Begin ApplyTheme: {theme.Name}");

                this.SuspendLayout();

                // Apply to main form
                this.BackColor = theme.BackColor;
                this.ForeColor = theme.TextColor;

                // Apply to InnerForm and its container
                if (InnerForm != null)
                {
                    InnerForm.ForeColor = theme.TextColor;
                    InnerForm.ControlContents.BackColor = theme.BackColor;
                }

                // Apply to RichTextBox
                if (ErrorRTB != null)
                {
                    ErrorRTB.BackColor = theme.TextBoxBackColor;
                    ErrorRTB.ForeColor = theme.TextColor;
                }

                // Apply to Button
                if (AcceptButton != null)
                {
                    AcceptButton.BackColor = theme.ButtonBackColor;
                    AcceptButton.ForeColor = theme.TextColor;
                    AcceptButton.FlatStyle = theme.ButtonFlatStyle;
                }

                if (ThemeDebugging)
                {
                    Debug.WriteLine($"Applied theme - BackColor: {BackColor}");
                    Debug.WriteLine($"ErrorRTB BackColor: {ErrorRTB?.BackColor}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ApplyTheme: {ex}");
            }
            finally
            {
                this.ResumeLayout(true);
                this.Refresh();
            }
        }

        private void ApplyThemeWithForce(UIThemeInfo theme)
        {
            ApplyTheme(theme);

            // Force complete redraw of all elements
            this.Invalidate(true);
            this.Update();

            if (InnerForm != null)
            {
                InnerForm.Invalidate(true);
                InnerForm.Update();
            }

            ErrorRTB?.Invalidate(true);
            AcceptButton?.Invalidate(true);

            Application.DoEvents(); // Ensure immediate repaint
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Ensure background is painted with current theme color
            using (var brush = new SolidBrush(this.BackColor))
            {
                e.Graphics.FillRectangle(brush, this.ClientRectangle);
            }

            base.OnPaintBackground(e);
        }

        private void AcceptButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        public static void Show(string title, string description, bool topMost = false)
        {
            try
            {
                var dialog = new CErrorDialog(title, description) { TopMost = topMost };
                dialog.ApplyThemeWithForce(UIThemeManager.CurrentTheme);
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing dialog: {ex}");
                MessageBox.Show(description, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}