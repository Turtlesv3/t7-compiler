using DebugCompiler.UI.Core.Controls;
using DebugCompiler.UI.Core.Interfaces;
using DebugCompiler.UI.Core.Singletons;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System;
using DebugCompiler.Properties;

namespace DebugCompiler.UI.Core.Controls
{
    public class CustomToolStripRenderer : ToolStripProfessionalRenderer
    {
        private readonly UIThemeInfo _theme;

        public CustomToolStripRenderer(UIThemeInfo theme) : base(new ThemeColors(theme))
        {
            _theme = theme;
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected)
            {
                using (var brush = new SolidBrush(_theme.ButtonHoverColor))
                {
                    e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
                }
            }
            else
            {
                base.OnRenderMenuItemBackground(e);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = _theme.MenuTextColor;
            base.OnRenderItemText(e);
        }

        private class ThemeColors : ProfessionalColorTable
        {
            private readonly UIThemeInfo _theme;

            public ThemeColors(UIThemeInfo theme)
            {
                _theme = theme;
            }

            public override Color MenuItemSelected => _theme.ButtonHoverColor;
            public override Color MenuItemBorder => _theme.BorderColor;
            public override Color MenuBorder => _theme.BorderColor;
            public override Color MenuItemSelectedGradientBegin => _theme.ButtonHoverColor;
            public override Color MenuItemSelectedGradientEnd => _theme.ButtonHoverColor;
            public override Color MenuItemPressedGradientBegin => _theme.ButtonActiveColor;
            public override Color MenuItemPressedGradientEnd => _theme.ButtonActiveColor;
            public override Color ToolStripDropDownBackground => _theme.MenuBackColor;
            public override Color ImageMarginGradientBegin => _theme.MenuBackColor;
            public override Color ImageMarginGradientMiddle => _theme.MenuBackColor;
            public override Color ImageMarginGradientEnd => _theme.MenuBackColor;
        }
    }
}