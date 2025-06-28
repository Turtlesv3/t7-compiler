using DebugCompiler.UI.Core.Interfaces;
using DebugCompiler.UI.Core.Singletons;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DebugCompiler.UI.Core.Controls
{
    [Designer("System.Windows.Forms.Design.ParentControlDesigner, System.Design", typeof(System.ComponentModel.Design.IDesigner))]
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
                ExitButton.Click += CloseButton_Click;
            }
        }

        private void MouseDown_Drag(object sender, MouseEventArgs e)
        {
            if (ParentForm == null || DisableDrag || e.Button != MouseButtons.Left)
                return;

            ReleaseCapture();
            SendMessage(ParentForm.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }

        private void MinimizeButton_Click(object sender, EventArgs e)
        {
            if (ParentForm != null)
            {
                ParentForm.WindowState = FormWindowState.Minimized;
            }
        }

        private void MaximizeButton_Click(object sender, EventArgs e)
        {
            if (ParentForm != null)
            {
                ParentForm.WindowState = ParentForm.WindowState == FormWindowState.Maximized
                    ? FormWindowState.Normal
                    : FormWindowState.Maximized;
            }
        }

        public void SetExitButtonVisible(bool isVisible)
        {
            if (ExitButton != null)
            {
                ExitButton.Visible = isVisible;
            }
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            ParentForm?.Close();
        }

        public void ApplyTheme(UIThemeInfo theme)
        {
            if (IsDisposed || !IsHandleCreated || theme.Equals(default(UIThemeInfo)))
                return;

            try
            {
                // Set unified background for entire title bar
                this.BackColor = theme.AccentColor;

                // Apply to title label
                if (TitleLabel != null)
                {
                    TitleLabel.BackColor = theme.AccentColor;
                }

                // Apply to all buttons using a helper method
                ApplyButtonTheme(MinimizeButton, theme);
                ApplyButtonTheme(MaximizeButton, theme);
                ApplyButtonTheme(CloseButton, theme, isCloseButton: true);

                // Force immediate redraw
                this.Invalidate(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TitleBar theme error: {ex.Message}");
            }
        }

        private void ApplyButtonTheme(Button button, UIThemeInfo theme, bool isCloseButton = false)
        {
            if (button != null)
            {
                button.BackColor = theme.AccentColor;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 0;

                if (isCloseButton)
                {
                    button.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 17, 35);
                    button.FlatAppearance.MouseDownBackColor = Color.FromArgb(139, 0, 0);
                }
                else
                {
                    button.FlatAppearance.MouseOverBackColor = theme.GetHoverColor(theme.AccentColor);
                    button.FlatAppearance.MouseDownBackColor = theme.GetPressedColor(theme.AccentColor);
                }
            }
        }

        public string Title
        {
            get => TitleLabel?.Text ?? string.Empty;
            set
            {
                if (TitleLabel != null)
                {
                    TitleLabel.Text = value;
                }
            }
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

            if (MinimizeButton != null)
                yield return MinimizeButton;

            if (MaximizeButton != null)
                yield return MaximizeButton;

            if (CloseButton != null)
                yield return CloseButton;
        }
    }
}