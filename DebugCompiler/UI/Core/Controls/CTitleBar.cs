using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel.Design;
using System.Windows.Forms.Design;
using System.Drawing.Design;
using System.Collections;
using DebugCompiler.UI.Core.Singletons;
using DebugCompiler.UI.Core.Interfaces;
using DebugCompiler.UI.Core.Controls;

namespace DebugCompiler.UI.Core.Controls
{
    public partial class CTitleBar : UserControl, IThemeableControl
    {
        public bool DisableDrag;
        public CTitleBar()
        {
            InitializeComponent();
            MouseDown += MouseDown_Drag;
            UIThemeManager.RegisterControl(this);
            UIThemeManager.ThemeChanged += OnThemeChanged_Implementation;
            TitleLabel.MouseDown += MouseDown_Drag;
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            ParentForm?.Close();
        }

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private void MouseDown_Drag(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (ParentForm == null) return;
            if (DisableDrag) return;
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(ParentForm.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        public void SetExitButtonVisible(bool isVisible)
        {
            ExitButton.Visible = isVisible;
        }

        public void ApplyTheme(UIThemeInfo theme)
        {
            if (IsDisposed || !IsHandleCreated) return;

            this.BackColor = theme.AccentColor; // Different color for title bar
            this.ForeColor = theme.TextColor;
            ExitButton.BackColor = theme.AccentColor;
            ExitButton.ForeColor = theme.TextColor;
        }

        private void OnThemeChanged_Implementation(UIThemeInfo theme)
        {
            ApplyTheme(theme);
        }

        public IEnumerable<Control> GetThemedControls()
        {
            yield return ExitButton;
            yield return TitleLabel;
        }
    }
}
