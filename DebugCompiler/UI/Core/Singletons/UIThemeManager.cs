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
    public static class UIThemeManager
    {
        public static UIThemeInfo CurrentTheme { get; private set; } = UIThemeInfo.Dark;
        private static readonly HashSet<Control> ThemedControls = new();
        private static readonly Dictionary<Control, Action<UIThemeInfo>> CustomControlHandlers = new();

        public static event Action<UIThemeInfo> ThemeChanged;

        public static void SetTheme(UIThemeInfo theme)
        {
            CurrentTheme = theme;
            ApplyThemeToAllControls();
            ThemeChanged?.Invoke(theme);
        }

        public static void SetTheme(string themeName)
        {
            var theme = UIThemeInfo.AvailableThemes
                .FirstOrDefault(t => t.Name.Equals(themeName, StringComparison.OrdinalIgnoreCase));
            if (theme.Name != null) SetTheme(theme);
        }

        public static void RegisterChildControls(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                RegisterControl(child);
                if (child.Controls.Count > 0)
                {
                    RegisterChildControls(child);
                }
            }
        }


        private static void ApplyThemeToAllControls()
        {
            foreach (var control in ThemedControls.ToArray())
            {
                if (control.IsDisposed)
                {
                    ThemedControls.Remove(control);
                    continue;
                }
                ApplyThemeToControl(control);
            }
        }

        private static void ApplyThemeToControl(Control control)
        {
            control.SuspendLayout();
            try
            {
                if (control is IThemeableControl themeable)
                {
                    themeable.ApplyTheme(CurrentTheme);
                }
                else
                {
                    ApplyDefaultTheme(control);
                }

                if (CustomControlHandlers.TryGetValue(control, out var handler))
                {
                    handler(CurrentTheme);
                }
            }
            finally
            {
                control.ResumeLayout();
            }
        }

        private static void ApplyDefaultTheme(Control control)
        {
            control.SuspendLayout();
            try
            {
                // Handle Form first
                if (control is Form form)
                {
                    form.BackColor = CurrentTheme.BackColor;
                    form.ForeColor = CurrentTheme.TextColor;
                    return;
                }

                // Handle specific controls with explicit type checks
                if (control is GroupBox groupBox)
                {
                    groupBox.Paint -= ThemedGroupBoxPaint;
                    groupBox.Paint += ThemedGroupBoxPaint;
                    groupBox.ForeColor = CurrentTheme.TextColor;
                    return;
                }

                if (control is Button button)
                {
                    button.BackColor = CurrentTheme.ButtonBackColor;
                    button.ForeColor = CurrentTheme.TextColor;
                    button.FlatStyle = CurrentTheme.ButtonFlatStyle;
                    button.FlatAppearance.BorderColor = CurrentTheme.BorderColor;
                    button.FlatAppearance.MouseOverBackColor = CurrentTheme.ButtonHoverColor;
                    button.FlatAppearance.MouseDownBackColor = CurrentTheme.ButtonActiveColor;
                    return;
                }

                // Handle TextBox and RichTextBox separately
                if (control is TextBox textBox)
                {
                    textBox.BackColor = CurrentTheme.TextBoxBackColor;
                    textBox.ForeColor = CurrentTheme.TextColor;
                    textBox.BorderStyle = CurrentTheme.TextBoxBorderStyle;
                    return;
                }

                if (control is RichTextBox richTextBox)
                {
                    richTextBox.BackColor = CurrentTheme.TextBoxBackColor;
                    richTextBox.ForeColor = CurrentTheme.TextColor;
                    richTextBox.BorderStyle = CurrentTheme.TextBoxBorderStyle;
                    return;
                }

                if (control is Label label)
                {
                    label.ForeColor = CurrentTheme.TextColor;
                    return;
                }

                if (control is Panel panel)
                {
                    panel.BackColor = CurrentTheme.ControlBackColor;
                    return;
                }

                if (control is ComboBox comboBox)
                {
                    comboBox.BackColor = CurrentTheme.TextBoxBackColor;
                    comboBox.ForeColor = CurrentTheme.TextColor;
                    comboBox.FlatStyle = CurrentTheme.ButtonFlatStyle;
                    return;
                }

                if (control is UserControl userControl)
                {
                    userControl.BackColor = CurrentTheme.BackColor;
                    userControl.ForeColor = CurrentTheme.TextColor;
                    return;
                }

                // Handle DataGridView if needed
                if (control is DataGridView dataGridView)
                {
                    dataGridView.BackgroundColor = CurrentTheme.BackColor;
                    dataGridView.ForeColor = CurrentTheme.TextColor;
                    dataGridView.GridColor = CurrentTheme.GridLineColor;
                    return;
                }
            }
            finally
            {
                control.ResumeLayout();
            }
        }

        public static void RegisterControl(Control control)
        {
            if (control != null && !ThemedControls.Contains(control))
            {
                ThemedControls.Add(control);
                control.Disposed += (s, e) => ThemedControls.Remove(control);
                ApplyThemeToControl(control);
            }
        }

        private static void ThemedGroupBoxPaint(object sender, PaintEventArgs e)
        {
            if (sender is GroupBox box)
            {
                using (var textBrush = new SolidBrush(CurrentTheme.TextColor))
                using (var borderPen = new Pen(CurrentTheme.BorderColor))
                {
                    SizeF strSize = e.Graphics.MeasureString(box.Text, box.Font);
                    Rectangle rect = new Rectangle(
                        box.ClientRectangle.X,
                        box.ClientRectangle.Y + (int)(strSize.Height / 2),
                        box.ClientRectangle.Width - 1,
                        box.ClientRectangle.Height - (int)(strSize.Height / 2) - 1);

                    // Clear background
                    e.Graphics.Clear(CurrentTheme.BackColor);

                    // Draw text
                    e.Graphics.DrawString(box.Text, box.Font, textBrush, box.Padding.Left, 0);

                    // Draw border
                    // Left
                    e.Graphics.DrawLine(borderPen, rect.Location, new Point(rect.X, rect.Y + rect.Height));
                    // Right
                    e.Graphics.DrawLine(borderPen,
                        new Point(rect.X + rect.Width, rect.Y),
                        new Point(rect.X + rect.Width, rect.Y + rect.Height));
                    // Bottom
                    e.Graphics.DrawLine(borderPen,
                        new Point(rect.X, rect.Y + rect.Height),
                        new Point(rect.X + rect.Width, rect.Y + rect.Height));
                    // Top left
                    e.Graphics.DrawLine(borderPen,
                        new Point(rect.X, rect.Y),
                        new Point(rect.X + box.Padding.Left, rect.Y));
                    // Top right
                    e.Graphics.DrawLine(borderPen,
                        new Point(rect.X + box.Padding.Left + (int)strSize.Width, rect.Y),
                        new Point(rect.X + rect.Width, rect.Y));
                }
            }
        }

    }
}
