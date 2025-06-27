using DebugCompiler.UI.Core.Interfaces;
using DebugCompiler.UI.Core.Singletons;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DebugCompiler.UI.Core.Controls
{
    public partial class CTitleBar : UserControl, IThemeableControl
    {
        private bool _disableDrag;
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        public bool DisableDrag
        {
            get => _disableDrag;
            set => _disableDrag = value;
        }

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        public CTitleBar()
        {
            InitializeComponent();
            InitializeEventHandlers();
            UIThemeManager.RegisterControl(this);
        }

        private void InitializeEventHandlers()
        {
            MouseDown += MouseDown_Drag;
            UIThemeManager.ThemeChanged += OnThemeChanged_Implementation;

            if (TitleLabel != null)
            {
                TitleLabel.MouseDown += MouseDown_Drag;
            }

            if (ExitButton != null)
            {
                ExitButton.Click += ExitButton_Click;
            }
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            ParentForm?.Close();
        }

        private void MouseDown_Drag(object sender, MouseEventArgs e)
        {
            if (ParentForm == null || DisableDrag || e.Button != MouseButtons.Left)
                return;

            ReleaseCapture();
            SendMessage(ParentForm.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }

        public void SetExitButtonVisible(bool isVisible)
        {
            if (ExitButton != null && ExitButton.Visible != isVisible)
            {
                ExitButton.Visible = isVisible;
            }
        }

        public void ApplyTheme(UIThemeInfo theme)
        {
            if (IsDisposed || !IsHandleCreated || theme.Equals(default(UIThemeInfo)))
                return;

            try
            {
                this.BackColor = theme.AccentColor;

                // Ensure title text is always visible by using contrasting color
                Color textColor = GetContrastingTextColor(theme.AccentColor);

                this.ForeColor = textColor;

                if (TitleLabel != null)
                {
                    TitleLabel.ForeColor = textColor;
                }

                if (ExitButton != null)
                {
                    ExitButton.BackColor = theme.AccentColor;
                    ExitButton.ForeColor = textColor;
                    ExitButton.FlatAppearance.MouseOverBackColor = ControlPaint.Light(theme.AccentColor, 0.2f);
                    ExitButton.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(theme.AccentColor, 0.2f);
                }
            }
            catch { /* Handle errors */ }
        }

        private Color GetContrastingTextColor(Color backgroundColor)
        {
            // Calculate luminance to determine if background is light or dark
            double luminance = (0.299 * backgroundColor.R + 0.587 * backgroundColor.G + 0.114 * backgroundColor.B) / 255;
            return luminance > 0.5 ? Color.Black : Color.White;
        }

        private void OnThemeChanged_Implementation(UIThemeInfo theme)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<UIThemeInfo>(ApplyTheme), theme);
                return;
            }
            ApplyTheme(theme);
        }

        public IEnumerable<Control> GetThemedControls()
        {
            if (TitleLabel != null)
                yield return TitleLabel;

            if (ExitButton != null)
                yield return ExitButton;
        }
    }
}