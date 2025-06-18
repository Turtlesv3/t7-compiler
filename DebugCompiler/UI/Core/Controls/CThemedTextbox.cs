using Refract.UI.Core.Interfaces;
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

namespace SMC.UI.Core.Controls
{
    /// <summary>
    /// A TextBox control with customizable border color
    /// </summary>
    public partial class CThemedTextbox : TextBox
    {
        private const int WM_NCPAINT = 0x85;
        private const uint RDW_INVALIDATE = 0x1;
        private const uint RDW_IUPDATENOW = 0x100;
        private const uint RDW_FRAME = 0x400;

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprc, IntPtr hrgn, uint flags);

        private Color _borderColor = Color.Gray;

        /// <summary>
        /// Gets or sets the border color of the control
        /// </summary>
        [Category("Appearance")]
        [Description("The border color of the control")]
        public Color BorderColor
        {
            get => _borderColor;
            set
            {
                _borderColor = value;
                RedrawWindow(Handle, IntPtr.Zero, IntPtr.Zero, RDW_FRAME | RDW_IUPDATENOW | RDW_INVALIDATE);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCPAINT && BorderColor != Color.Transparent && BorderStyle == BorderStyle.Fixed3D)
            {
                var hdc = GetWindowDC(Handle);
                using (var g = Graphics.FromHdcInternal(hdc))
                using (var borderPen = new Pen(BorderColor))
                using (var backgroundPen = new Pen(BackColor))
                {
                    // Outer border
                    g.DrawRectangle(borderPen, new Rectangle(0, 0, Width - 1, Height - 1));
                    // Inner border
                    g.DrawRectangle(backgroundPen, new Rectangle(1, 1, Width - 3, Height - 3));
                }
                ReleaseDC(Handle, hdc);
            }
            else
            {
                base.WndProc(ref m);
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            RedrawWindow(Handle, IntPtr.Zero, IntPtr.Zero, RDW_FRAME | RDW_IUPDATENOW | RDW_INVALIDATE);
        }

        public CThemedTextbox()
        {
            InitializeComponent();
            BorderStyle = BorderStyle.Fixed3D;
        }
    }
}