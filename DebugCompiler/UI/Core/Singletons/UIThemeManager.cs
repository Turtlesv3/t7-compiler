using DebugCompiler.UI.Core.Interfaces;
using SMC.UI.Core.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DebugCompiler.UI.Core.Singletons
{
    internal struct UIThemeInfo
    {
        public string Name { get; set; }
        public bool IsDarkTheme { get; set; }

        // Core Colors
        public Color BackColor { get; set; }
        public Color AccentColor { get; set; }
        public Color TextColor { get; set; }

        // Component Specific
        public Color TitleBarColor { get; set; }
        public Color ControlBackColor { get; set; }
        public Color TextBoxBackColor { get; set; }
        public Color ButtonBackColor { get; set; }
        public Color ButtonHoverColor { get; set; }
        public Color ButtonActiveColor { get; set; }
        public Color TextInactiveColor { get; set; }
        public Color BorderColor { get; set; }

        // Styles
        public FlatStyle ButtonFlatStyle { get; set; }
        public BorderStyle TextBoxBorderStyle { get; set; }

        public static UIThemeInfo DarkTheme => new UIThemeInfo
        {
            Name = "Dark",
            IsDarkTheme = true,
            BackColor = Color.FromArgb(28, 28, 28),
            ControlBackColor = Color.FromArgb(40, 40, 40),
            TextColor = Color.WhiteSmoke,
            AccentColor = Color.DodgerBlue,
            TitleBarColor = Color.FromArgb(36, 36, 36),
            TextBoxBackColor = Color.FromArgb(35, 35, 35),
            ButtonBackColor = Color.FromArgb(50, 50, 50),
            ButtonHoverColor = Color.FromArgb(70, 70, 70),
            ButtonActiveColor = Color.DodgerBlue,
            TextInactiveColor = Color.Gray,
            BorderColor = Color.FromArgb(60, 60, 60),
            ButtonFlatStyle = FlatStyle.Flat,
            TextBoxBorderStyle = BorderStyle.FixedSingle
        };

        public static UIThemeInfo LightTheme => new UIThemeInfo
        {
            Name = "Light",
            IsDarkTheme = false,
            BackColor = SystemColors.Control,
            ControlBackColor = SystemColors.ControlLight,
            TextColor = SystemColors.ControlText,
            AccentColor = Color.DodgerBlue,
            TitleBarColor = SystemColors.ControlDark,
            TextBoxBackColor = SystemColors.Window,
            ButtonBackColor = SystemColors.Control,
            ButtonHoverColor = SystemColors.ControlLight,
            ButtonActiveColor = SystemColors.Highlight,
            TextInactiveColor = SystemColors.GrayText,
            BorderColor = SystemColors.ControlDark,
            ButtonFlatStyle = FlatStyle.Standard,
            TextBoxBorderStyle = BorderStyle.Fixed3D
        };
    }

    internal delegate void ThemeChangedCallback(UIThemeInfo themeData);

    internal static class UIThemeManager
    {
        public static UIThemeInfo CurrentTheme { get; private set; }
        private static readonly HashSet<Control> ThemedControls = new HashSet<Control>();
        private static readonly Dictionary<Type, ThemeChangedCallback> CustomTypeHandlers = new Dictionary<Type, ThemeChangedCallback>();
        private static readonly Dictionary<Control, ThemeChangedCallback> CustomControlHandlers = new Dictionary<Control, ThemeChangedCallback>();

        static UIThemeManager()
        {
            CurrentTheme = UIThemeInfo.DarkTheme; // Default to dark theme
        }

        public static void SetTheme(UIThemeInfo theme)
        {
            CurrentTheme = theme;
            ApplyThemeToAllControls();
        }

        public static void ToggleTheme()
        {
            SetTheme(CurrentTheme.IsDarkTheme ? UIThemeInfo.LightTheme : UIThemeInfo.DarkTheme);
        }

        internal static void SetThemeAware(this IThemeableControl control)
        {
            if (!(control is Control ctrl))
                throw new InvalidOperationException($"Cannot theme control of type '{control.GetType()}' because it is not derived from Control");

            foreach (Control childControl in control.GetThemedControls())
            {
                if (childControl == null) continue;
                if (childControl is IThemeableControl themedControl)
                    themedControl.SetThemeAware();
                else
                    RegisterAndThemeControl(childControl);
            }
            RegisterAndThemeControl(ctrl);
        }

        private static void ApplyThemeToAllControls()
        {
            foreach (var control in ThemedControls.ToArray()) // ToArray to prevent collection modification
            {
                if (control.IsDisposed)
                {
                    ThemedControls.Remove(control);
                    continue;
                }
                ThemeSpecificControl(control);
            }
        }

        private static void ThemeSpecificControl(Control control)
        {
            // Invoke custom type handlers first
            if (CustomTypeHandlers.TryGetValue(control.GetType(), out var typeHandler))
            {
                typeHandler.Invoke(CurrentTheme);
            }

            // Apply default theming
            if (!(control is IThemeableControl))
            {
                ApplyDefaultTheme(control);
            }

            // Invoke custom control handlers
            if (CustomControlHandlers.TryGetValue(control, out var controlHandler))
            {
                controlHandler.Invoke(CurrentTheme);
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

        private static void RegisterAndThemeControl(Control control)
        {
            if (control == null || ThemedControls.Contains(control)) return;

            control.Disposed += ThemedControlDisposed;
            ThemedControls.Add(control);
            ThemeSpecificControl(control);
        }

        private static void ThemedControlDisposed(object sender, EventArgs e)
        {
            if (sender is Control control)
            {
                ThemedControls.Remove(control);
                CustomControlHandlers.Remove(control);
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

        // Extension methods for easier theming
        // Replace the existing extension method with these two versions:

        // For controls
        public static void RegisterCustomThemeHandler<T>(this T control, ThemeChangedCallback callback) where T : Control
        {
            if (control == null) return;

            if (callback == null)
            {
                CustomControlHandlers.Remove(control);
            }
            else
            {
                CustomControlHandlers[control] = callback;
                control.Disposed += (s, e) => CustomControlHandlers.Remove(control);
            }
        }

        // For types (add this new method)
        public static void RegisterCustomThemeHandler(Type type, ThemeChangedCallback callback)
        {
            if (type == null) return;

            if (callback == null)
            {
                CustomTypeHandlers.Remove(type);
            }
            else
            {
                if (CustomTypeHandlers.ContainsKey(type))
                {
                    CustomTypeHandlers[type] += callback;
                }
                else
                {
                    CustomTypeHandlers[type] = callback;
                }
            }
        }
    }
}