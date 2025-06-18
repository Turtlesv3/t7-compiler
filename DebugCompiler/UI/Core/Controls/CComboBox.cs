using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SMC.UI.Core.Controls
{
    /// <summary>
    /// A ComboBox control with customizable border color
    /// </summary>
    public class CComboBox : ComboBox
    {
        private const int WM_PAINT = 0xF;
        private readonly int _buttonWidth = SystemInformation.HorizontalScrollBarArrowWidth;
        private Color _borderColor = Color.DimGray;

        /// <summary>
        /// Gets or sets the border color of the control
        /// </summary>
        [Browsable(true)]
        [Category("Appearance")]
        [DefaultValue(typeof(Color), "DimGray")]
        public Color BorderColor
        {
            get => _borderColor;
            set
            {
                _borderColor = value;
                Invalidate();
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_PAINT && DropDownStyle != ComboBoxStyle.Simple)
            {
                using (var g = Graphics.FromHwnd(Handle))
                using (var p = new Pen(BorderColor, 1))
                {
                    // Draw main border
                    g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);

                    // Draw divider between text and dropdown button
                    g.DrawLine(p,
                        Width - _buttonWidth - 1, 1,
                        Width - _buttonWidth - 1, Height - 2);
                }
            }
        }

        public CComboBox()
        {
            FlatStyle = FlatStyle.Flat;
        }
    }
}