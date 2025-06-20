using Refract.UI.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Refract.UI.Core.Singletons;
using System.ComponentModel.Design;
using System.Windows.Forms.Design;
using System.Collections;

namespace Refract.UI.Core.Controls
{
    [Designer(typeof(CBorderedFormDesigner))]
    public partial class CBorderedForm : Form, IThemeableControl
    {
        #region Designer Properties
        private bool _useTitleBar = true;

        [Category("Title Bar")]
        [Description("Determines if a title bar should be rendered.")]
        public bool UseTitleBar
        {
            get => _useTitleBar;
            set
            {
                _useTitleBar = value;
                if (TitleBar != null) TitleBar.Visible = value;
                Invalidate();
            }
        }

        [Category("Title Bar")]
        [Description("Determines the title bar's text")]
        public string TitleBarTitle
        {
            get => TitleBar?.TitleLabel?.Text ?? string.Empty;
            set
            {
                if (TitleBar?.TitleLabel != null)
                    TitleBar.TitleLabel.Text = value;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [Browsable(true)]
        public Panel ControlContents { get; protected set; } = new Panel();
        #endregion

        #region Win32 API
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        #endregion

        public CBorderedForm()
        {
            InitializeComponent();
            ControlContents.Dock = DockStyle.Fill;
            MouseDown += MouseDown_Drag;
            if (MainPanel != null) MainPanel.MouseDown += MouseDown_Drag;

            UIThemeManager.RegisterCustomThemeHandler(typeof(CBorderedForm), ApplyThemeCustom_Implementation);
            FormBorderStyle = FormBorderStyle.None;
        }

        private void MouseDown_Drag(object sender, MouseEventArgs e)
        {
            if (ParentForm == null || e.Button != MouseButtons.Left)
                return;

            ReleaseCapture();
            SendMessage(ParentForm.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }

        private void ApplyThemeCustom_Implementation(UIThemeInfo themeData)
        {
            BackColor = themeData.AccentColor;
            if (MainPanel != null) MainPanel.BackColor = themeData.BackColor;
        }

        public virtual IEnumerable<Control> GetThemedControls()
        {
            yield return TitleBar;
            yield return MainPanel;
            yield return ControlContents;
        }

        public void SetExitHidden(bool hidden)
        {
            if (TitleBar != null) TitleBar.SetExitButtonVisible(!hidden);
        }

        public void SetDraggable(bool draggable)
        {
            if (TitleBar != null) TitleBar.DisableDrag = !draggable;
        }
    }
}