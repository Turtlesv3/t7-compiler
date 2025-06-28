using DebugCompiler.UI.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using DebugCompiler.Properties;

namespace DebugCompiler.UI.Core.Singletons
{
#if DEBUG
    [System.ComponentModel.DesignerCategory("Code")]
#endif
    public static class UIThemeManager
    {
        private static bool _isDesignMode = System.Diagnostics.Process.GetCurrentProcess().ProcessName == "devenv";
        private static UIThemeInfo _currentTheme = GetDefaultTheme();
        private static readonly HashSet<Control> ThemedControls = new();
        private static readonly Dictionary<Control, Action<UIThemeInfo>> CustomControlHandlers = new();

        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DebugCompiler",
            "theme.config");

        public static event Action<UIThemeInfo> ThemeChanged;
        public static event Action<UIThemeInfo> DesignTimeThemeChanged;

        public static bool IsDesignMode => _isDesignMode;

        public static UIThemeInfo CurrentTheme
        {
            get => _currentTheme;
            private set
            {
                if (!_currentTheme.Equals(value))
                {
                    _currentTheme = value;
                    if (!IsDesignMode)
                    {
                        ApplyThemeToAllControls();
                        ThemeChanged?.Invoke(value);
                    }
                }
            }
        }

        public static void SimulateDesignTimeTheme(UIThemeInfo theme)
        {
            if (IsDesignMode)
            {
                _currentTheme = theme;
                DesignTimeThemeChanged?.Invoke(theme);
            }
        }

        public static void InitializeWithTheme(UIThemeInfo theme)
        {
            _currentTheme = theme;
            Application.Idle += FirstIdleThemeApplication;
        }

        private static void FirstIdleThemeApplication(object sender, EventArgs e)
        {
            Application.Idle -= FirstIdleThemeApplication;
            ApplyThemeToAllControls();
            Debug.WriteLine($"Theme applied to {ThemedControls.Count} controls on startup");
        }

        public static void EnsureThemed(Control control)
        {
            if (!ThemedControls.Contains(control))
            {
                RegisterControl(control);
                ApplyThemeToControl(control);
            }
        }

        public static void SetTheme(UIThemeInfo theme)
        {
            if (theme == null || theme.Equals(default(UIThemeInfo))) return;
            CurrentTheme = theme;
            SaveTheme(theme.Name);
        }

        private static UIThemeInfo GetDefaultTheme()
        {
            var theme = UIThemeInfo.GetThemeByName("catapatchitomocha");
            return theme != null ? theme : UIThemeInfo.Dark;
        }

        public static void SetTheme(string themeName)
        {
            if (string.IsNullOrWhiteSpace(themeName))
            {
                SetTheme(GetDefaultTheme());
                return;
            }

            UIThemeInfo theme = UIThemeInfo.GetThemeByName(themeName);
            if (theme != null)
            {
                SetTheme(theme);
            }
            else
            {
                SetTheme(GetDefaultTheme());
            }
        }

        public static bool TryGetTheme(string themeName, out UIThemeInfo theme)
        {
            theme = UIThemeInfo.GetThemeByName(themeName);
            return !theme.Equals(default(UIThemeInfo));
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
            if (parent == null) return;

            foreach (Control child in parent.Controls)
            {
                RegisterControl(child);
                if (child.HasChildren)
                {
                    RegisterChildControls(child);
                }
            }
        }

        public static void UnregisterControl(Control control)
        {
            if (control != null)
            {
                ThemedControls.Remove(control);
                CustomControlHandlers.Remove(control);

                if (control is GroupBox groupBox)
                {
                    groupBox.Paint -= ThemedGroupBoxPaint;
                }
            }
        }

        private static void ApplyThemeToAllControls()
        {
            var theme = CurrentTheme;
            foreach (var control in ThemedControls.ToArray())
            {
                if (control.IsDisposed)
                {
                    ThemedControls.Remove(control);
                    continue;
                }

                if (!control.Visible && !(control is ContainerControl))
                {
                    continue;
                }

                ApplyThemeToControl(control);
            }
        }

        private static void ApplyThemeToControl(Control control)
        {
            if (control == null || control.IsDisposed) return;

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

                if (control is ComboBox comboBox && comboBox.Tag?.ToString() == "ForceDropDownList")
                {
                    comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                    comboBox.Refresh();
                }
            }
            finally
            {
                control.ResumeLayout(true);
            }
        }

        public static void ApplyDefaultTheme(Control control)
        {
            if (control == null || control.IsDisposed)
                return;

            control.SuspendLayout();
            try
            {
                control.BackColor = CurrentTheme.ControlBackColor;
                control.ForeColor = CurrentTheme.TextColor;

                if (!control.Enabled)
                {
                    control.ForeColor = CurrentTheme.DisabledTextColor;
                }

                switch (control)
                {
                    case Button button:
                        button.BackColor = CurrentTheme.ButtonBackColor;
                        button.FlatStyle = CurrentTheme.ButtonFlatStyle;
                        button.FlatAppearance.BorderColor = CurrentTheme.BorderColor;
                        button.FlatAppearance.MouseOverBackColor = CurrentTheme.ButtonHoverColor;
                        button.FlatAppearance.MouseDownBackColor = CurrentTheme.ButtonActiveColor;
                        break;

                    case TextBoxBase textBox:
                        textBox.BackColor = CurrentTheme.TextBoxBackColor;
                        textBox.BorderStyle = CurrentTheme.TextBoxBorderStyle;
                        break;

                    case ComboBox comboBox:
                        comboBox.BackColor = CurrentTheme.TextBoxBackColor;
                        comboBox.FlatStyle = CurrentTheme.ButtonFlatStyle;
                        if (comboBox.Tag?.ToString() == "ForceDropDownList")
                        {
                            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                        }
                        break;

                    case DataGridView grid:
                        grid.BackgroundColor = CurrentTheme.BackColor;
                        grid.GridColor = CurrentTheme.GridLineColor;
                        break;
                }

                control.Refresh();
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

                if (CurrentTheme != null)
                {
                    ApplyThemeToControl(control);
                }
            }
        }

        public static void RegisterControlRecursive(Control control)
        {
            if (control == null || control.IsDisposed) return;

            if (control is Button || control is TextBox || control is Panel)
            {
                RegisterControl(control);
                return;
            }

            RegisterControl(control);

            if (control is IThemeableContainer container)
            {
                foreach (var child in container.GetThemeableChildren())
                {
                    RegisterControlRecursive(child);
                }
            }
            else if (control.HasChildren && control.Controls.Count < 20)
            {
                foreach (Control child in control.Controls)
                {
                    RegisterControlRecursive(child);
                }
            }
        }

        public static void EnsureThemeApplied(Control control)
        {
            if (control == null || control.IsDisposed) return;

            if (control.InvokeRequired)
            {
                control.Invoke((Action)(() => EnsureThemeApplied(control)));
                return;
            }

            if (control is IThemeableControl themeable)
            {
                themeable.ApplyTheme(CurrentTheme);
            }
            else
            {
                ApplyDefaultTheme(control);
            }

            ApplyThemeToControl(control);

            foreach (Control child in control.Controls)
            {
                EnsureThemeApplied(child);
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

        public static bool ValidateTheme(UIThemeInfo theme)
        {
            if (theme.IsEmpty()) return false;

            var requiredColors = new[] { theme.BackColor, theme.ForeColor, theme.TextColor };
            if (requiredColors.Any(c => c.IsEmpty || c.A == 0))
            {
                return false;
            }

            return true;
        }
    }
}