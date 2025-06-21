using DebugCompiler.UI.Core.Interfaces;
using DebugCompiler.UI.Core.Singletons;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace DebugCompiler.UI.Core.Controls
{
    public class CComboBox : ComboBox, IThemeableControl
    {
        private const int WM_PAINT = 0xF;
        private int buttonWidth = SystemInformation.HorizontalScrollBarArrowWidth;
        private bool _isApplyingTheme = false;

        public CComboBox()
        {
            BorderColor = Color.DimGray;
            UIThemeManager.RegisterControl(this);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_PAINT)
            {
                using (var g = Graphics.FromHwnd(Handle))
                {
                    using (var p = new Pen(this.BorderColor, 1))
                    {
                        g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
                        g.DrawRectangle(p, 0, 0, Width - buttonWidth - 1, Height - 1);
                    }
                }
            }
        }

        public void ApplyTheme(UIThemeInfo theme)
        {
            if (_isApplyingTheme || IsDisposed || !IsHandleCreated)
                return;

            _isApplyingTheme = true;
            try
            {
                if (InvokeRequired)
                {
                    if (IsHandleCreated)
                    {
                        Invoke(new Action<UIThemeInfo>(ApplyTheme), theme);
                    }
                    return;
                }

                this.BackColor = theme.TextBoxBackColor;
                this.ForeColor = theme.TextColor;
                this.FlatStyle = theme.ButtonFlatStyle;
                BorderColor = theme.BorderColor;
            }
            finally
            {
                _isApplyingTheme = false;
            }
        }

        public IEnumerable<Control> GetThemedControls()
        {
            yield break; // ComboBox has no child controls to theme
        }

        [Browsable(true)]
        [Category("Appearance")]
        [DefaultValue(typeof(Color), "DimGray")]
        public Color BorderColor { get; set; }
    }
}