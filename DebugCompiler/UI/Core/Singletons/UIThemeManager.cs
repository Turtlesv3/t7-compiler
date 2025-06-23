using DebugCompiler.UI.Core.Controls;
using DebugCompiler.UI.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DebugCompiler.UI.Core.Singletons
{
    public static class UIThemeManager
    {
        private static UIThemeInfo _currentTheme = UIThemeInfo.Dark;
        public static UIThemeInfo CurrentTheme
        {
            get => _currentTheme;
            private set => _currentTheme = value;
        }

        private static readonly HashSet<Control> ThemedControls = new();
        private static readonly Dictionary<Control, Action<UIThemeInfo>> CustomControlHandlers = new();

        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DebugCompiler",
            "theme.config");

        public static event Action<UIThemeInfo> ThemeChanged;

        public static void SetTheme(UIThemeInfo theme)
        {
            CurrentTheme = theme;
            ApplyThemeToAllControls();
            SaveTheme(theme.Name);
            ThemeChanged?.Invoke(theme);
        }

        public static void SetTheme(string themeName)
        {
            var theme = UIThemeInfo.GetThemeByName(themeName);
            if (EqualityComparer<UIThemeInfo>.Default.Equals(theme, default(UIThemeInfo)))
                return;

            SetTheme(theme); // Now calls the UIThemeInfo version to ensure consistent behavior
        }

        public static void SaveTheme(string themeName)
        {
            try
            {
                var configDir = Path.GetDirectoryName(ConfigPath);
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }
                File.WriteAllText(ConfigPath, themeName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save theme: {ex.Message}");
            }
        }

        public static string LoadTheme()
        {
            try
            {
                return File.Exists(ConfigPath) ? File.ReadAllText(ConfigPath) : string.Empty;
            }
            catch { return string.Empty; }
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
            if (control.InvokeRequired)
            {
                control.Invoke((Action)(() => ApplyThemeToControl(control)));
                return;
            }

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

                // Special handling for ComboBox to maintain DropDownStyle
                if (control is ComboBox comboBox)
                {
                    if (comboBox.Tag?.ToString() == "ForceDropDownList")
                    {
                        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                    }
                    comboBox.Refresh();
                }
            }
            finally
            {
                control.ResumeLayout(true);
            }
        }

        private static void ApplyDefaultTheme(Control control)
        {
            control.SuspendLayout();
            try
            {
                if (control is Form form)
                {
                    form.BackColor = CurrentTheme.BackColor;
                    form.ForeColor = CurrentTheme.TextColor;
                    return;
                }

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

                    // Special handling to maintain DropDownStyle
                    if (comboBox.Tag?.ToString() == "ForceDropDownList")
                    {
                        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                    }
                    return;
                }

                if (control is UserControl userControl)
                {
                    userControl.BackColor = CurrentTheme.BackColor;
                    userControl.ForeColor = CurrentTheme.TextColor;
                    return;
                }

                if (control is DataGridView dataGridView)
                {
                    dataGridView.BackgroundColor = CurrentTheme.BackColor;
                    dataGridView.ForeColor = CurrentTheme.TextColor;
                    dataGridView.GridColor = CurrentTheme.GridLineColor;
                    return;
                }

                if (control is ToolStrip toolStrip)
                {
                    toolStrip.BackColor = CurrentTheme.BackColor;
                    toolStrip.ForeColor = CurrentTheme.TextColor;
                    return;
                }
            }
            finally
            {
                control.ResumeLayout(true);
            }
        }

        public static void RefreshAllControls()
        {
            foreach (var control in ThemedControls.ToArray())
            {
                if (!control.IsDisposed && control.IsHandleCreated)
                {
                    ApplyThemeToControl(control);
                }
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

        public static void RegisterSpecialComboBox(ComboBox comboBox)
        {
            if (comboBox != null)
            {
                comboBox.Tag = "ForceDropDownList";
                RegisterControl(comboBox);
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

                    e.Graphics.Clear(CurrentTheme.BackColor);
                    e.Graphics.DrawString(box.Text, box.Font, textBrush, box.Padding.Left, 0);

                    e.Graphics.DrawLine(borderPen, rect.Location, new Point(rect.X, rect.Y + rect.Height));
                    e.Graphics.DrawLine(borderPen,
                        new Point(rect.X + rect.Width, rect.Y),
                        new Point(rect.X + rect.Width, rect.Y + rect.Height));
                    e.Graphics.DrawLine(borderPen,
                        new Point(rect.X, rect.Y + rect.Height),
                        new Point(rect.X + rect.Width, rect.Y + rect.Height));
                    e.Graphics.DrawLine(borderPen,
                        new Point(rect.X, rect.Y),
                        new Point(rect.X + box.Padding.Left, rect.Y));
                    e.Graphics.DrawLine(borderPen,
                        new Point(rect.X + box.Padding.Left + (int)strSize.Width, rect.Y),
                        new Point(rect.X + rect.Width, rect.Y));
                }
            }
        }
    }
}