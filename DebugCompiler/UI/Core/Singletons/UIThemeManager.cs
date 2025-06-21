using DebugCompiler.UI.Core.Interfaces;
using DebugCompiler.UI.Core.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

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
                switch (control)
                {
                    case Form form:
                        form.BackColor = CurrentTheme.BackColor;
                        form.ForeColor = CurrentTheme.TextColor;
                        break;
                    case GroupBox groupBox:
                        groupBox.Paint -= ThemedGroupBoxPaint;
                        groupBox.Paint += ThemedGroupBoxPaint;
                        groupBox.ForeColor = CurrentTheme.TextColor;
                        break;
                    case Button button:
                        button.BackColor = CurrentTheme.ButtonBackColor;
                        button.ForeColor = CurrentTheme.TextColor;
                        button.FlatStyle = CurrentTheme.ButtonFlatStyle;
                        button.FlatAppearance.BorderColor = CurrentTheme.BorderColor;
                        button.FlatAppearance.MouseOverBackColor = CurrentTheme.ButtonHoverColor;
                        button.FlatAppearance.MouseDownBackColor = CurrentTheme.ButtonActiveColor;
                        break;
                    case TextBoxBase textBox: // Handles TextBox and RichTextBox
                        textBox.BackColor = CurrentTheme.TextBoxBackColor;
                        textBox.ForeColor = CurrentTheme.TextColor;
                        textBox.BorderStyle = CurrentTheme.TextBoxBorderStyle;
                        break;
                    case Label label:
                        label.ForeColor = CurrentTheme.TextColor;
                        break;
                    case Panel panel:
                        panel.BackColor = CurrentTheme.ControlBackColor;
                        break;
                    case ComboBox comboBox:
                        comboBox.BackColor = CurrentTheme.TextBoxBackColor;
                        comboBox.ForeColor = CurrentTheme.TextColor;
                        comboBox.FlatStyle = CurrentTheme.ButtonFlatStyle;
                        break;
                    case UserControl userControl:
                        userControl.BackColor = CurrentTheme.BackColor;
                        userControl.ForeColor = CurrentTheme.TextColor;
                        break;
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
