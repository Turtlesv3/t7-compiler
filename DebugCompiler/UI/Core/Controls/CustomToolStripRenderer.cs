using DebugCompiler.UI.Core.Interfaces;
using DebugCompiler.UI.Core.Singletons;
using System.Drawing;
using System.Windows.Forms;
using DebugCompiler.Properties;

namespace DebugCompiler.UI.Core.Controls
{
    public class CustomToolStripRenderer : ToolStripProfessionalRenderer
    {
        private readonly UIThemeInfo _theme;

        public CustomToolStripRenderer(UIThemeInfo theme) : base(new ThemeColors(theme))
        {
            _theme = theme;
            this.RoundedEdges = false;
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected)
            {
                using var brush = new SolidBrush(UIThemeManager.CurrentTheme.ButtonHoverColor);
                e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
            }
            else
            {
                base.OnRenderMenuItemBackground(e);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            // Use the theme's text color but ensure it's visible
            Color textColor = e.Item.Selected ?
                GetContrastingTextColor(_theme.ButtonHoverColor) :
                GetContrastingTextColor(_theme.MenuBackColor);

            e.TextColor = textColor;
            base.OnRenderItemText(e);
        }

        private Color GetContrastingTextColor(Color backgroundColor)
        {
            double luminance = (0.299 * backgroundColor.R + 0.587 * backgroundColor.G + 0.114 * backgroundColor.B) / 255;
            return luminance > 0.5 ? Color.Black : Color.White;
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            // Don't render border
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
            public override Color SeparatorDark => _theme.BorderColor;
            public override Color SeparatorLight => _theme.BorderColor;
        }
    }
}