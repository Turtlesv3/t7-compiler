using Refract.UI.Core.Interfaces;
using Refract.UI.Core.Singletons;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Refract.UI.Core.Controls
{
    /// <summary>
    /// Custom title bar control for bordered forms
    /// </summary>
    public partial class CTitleBar : UserControl, IThemeableControl
    {
        /// <summary>
        /// Gets or sets whether dragging is disabled
        /// </summary>
        public bool DisableDrag { get; set; }

        public CTitleBar()
        {
            InitializeComponent();
            MouseDown += MouseDown_Drag;
            TitleLabel.MouseDown += MouseDown_Drag;

            UIThemeManager.RegisterCustomThemeHandler(typeof(CTitleBar), ApplyThemeCustomType_Implementation);
            UIThemeManager.OnThemeChanged(this, ApplyThemeCustom_Implementation);
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            ParentForm?.Close();
        }

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        private void MouseDown_Drag(object sender, MouseEventArgs e)
        {
            if (ParentForm == null || DisableDrag || e.Button != MouseButtons.Left)
                return;

            ReleaseCapture();
            SendMessage(ParentForm.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }

        /// <summary>
        /// Sets the visibility of the exit button
        /// </summary>
        public void SetExitButtonVisible(bool isVisible)
        {
            ExitButton.Visible = isVisible;
        }

        private void ApplyThemeCustomType_Implementation(UIThemeInfo themeData)
        {
            BackColor = themeData.TitleBarColor;
            TitleLabel.ForeColor = themeData.TextColor;
        }

        private void ApplyThemeCustom_Implementation(UIThemeInfo themeData)
        {
            ExitButton.BackColor = themeData.LightBackColor;
            ExitButton.ForeColor = themeData.TextColor;
            ExitButton.FlatAppearance.MouseOverBackColor = themeData.ButtonHoverColor;
        }

        public IEnumerable<Control> GetThemedControls()
        {
            yield return ExitButton;
            yield return TitleLabel;
        }
    }
}