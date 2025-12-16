using System.Drawing;
using System.Windows.Forms;
using ReaLTaiizor.Enum.Poison;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Drawing.Poison;

namespace T7CompilerGUI.Forms
{
    #region Custom Menu Renderer
    
    /// <summary>
    /// Custom renderer for MenuStrip to use Poison theme colors
    /// </summary>
    public class PoisonMenuStripRenderer : ToolStripProfessionalRenderer
    {
        private PoisonStyleManager styleManager;
        
        public PoisonMenuStripRenderer(PoisonStyleManager styleManager) : base(new PoisonMenuColorTable(styleManager))
        {
            this.styleManager = styleManager;
        }
        
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item is ToolStripMenuItem menuItem && styleManager != null)
            {
                ThemeStyle theme = styleManager.Theme;
                ColorStyle style = styleManager.Style;
                
                Color backColor = PoisonPaint.BackColor.Form(theme);
                Color hoverColor = PoisonPaint.GetStyleColor(style);
                
                if (menuItem.Selected)
                {
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(150, hoverColor)))
                    {
                        e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
                    }
                }
                else
                {
                    using (SolidBrush brush = new SolidBrush(backColor))
                    {
                        e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
                    }
                }
            }
            else
            {
                base.OnRenderMenuItemBackground(e);
            }
        }
        
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (styleManager != null)
            {
                ThemeStyle theme = styleManager.Theme;
                Color foreColor = PoisonPaint.ForeColor.Label.Normal(theme);
                
                TextRenderer.DrawText(e.Graphics, e.Text, e.TextFont, e.TextRectangle, foreColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.HidePrefix);
            }
            else
            {
                base.OnRenderItemText(e);
            }
        }
    }
    
    /// <summary>
    /// Color table for MenuStrip to use Poison theme colors
    /// </summary>
    public class PoisonMenuColorTable : ProfessionalColorTable
    {
        private PoisonStyleManager styleManager;
        
        public PoisonMenuColorTable(PoisonStyleManager styleManager)
        {
            this.styleManager = styleManager;
        }
        
        public override Color MenuStripGradientBegin
        {
            get
            {
                if (styleManager != null)
                    return PoisonPaint.BackColor.Form(styleManager.Theme);
                return base.MenuStripGradientBegin;
            }
        }
        
        public override Color MenuStripGradientEnd
        {
            get
            {
                if (styleManager != null)
                    return PoisonPaint.BackColor.Form(styleManager.Theme);
                return base.MenuStripGradientEnd;
            }
        }
        
        public override Color MenuItemSelected
        {
            get
            {
                if (styleManager != null)
                {
                    Color styleColor = PoisonPaint.GetStyleColor(styleManager.Style);
                    return Color.FromArgb(150, styleColor);
                }
                return base.MenuItemSelected;
            }
        }
        
        public override Color MenuItemBorder
        {
            get
            {
                if (styleManager != null)
                {
                    Color styleColor = PoisonPaint.GetStyleColor(styleManager.Style);
                    return styleColor;
                }
                return base.MenuItemBorder;
            }
        }
        
        public override Color MenuItemPressedGradientBegin
        {
            get
            {
                if (styleManager != null)
                {
                    Color styleColor = PoisonPaint.GetStyleColor(styleManager.Style);
                    return Color.FromArgb(200, styleColor);
                }
                return base.MenuItemPressedGradientBegin;
            }
        }
        
        public override Color MenuItemPressedGradientEnd
        {
            get
            {
                if (styleManager != null)
                {
                    Color styleColor = PoisonPaint.GetStyleColor(styleManager.Style);
                    return Color.FromArgb(200, styleColor);
                }
                return base.MenuItemPressedGradientEnd;
            }
        }
        
        public override Color MenuItemSelectedGradientBegin
        {
            get
            {
                if (styleManager != null)
                {
                    Color styleColor = PoisonPaint.GetStyleColor(styleManager.Style);
                    return Color.FromArgb(150, styleColor);
                }
                return base.MenuItemSelectedGradientBegin;
            }
        }
        
        public override Color MenuItemSelectedGradientEnd
        {
            get
            {
                if (styleManager != null)
                {
                    Color styleColor = PoisonPaint.GetStyleColor(styleManager.Style);
                    return Color.FromArgb(150, styleColor);
                }
                return base.MenuItemSelectedGradientEnd;
            }
        }
        
        public override Color ToolStripDropDownBackground
        {
            get
            {
                if (styleManager != null)
                    return PoisonPaint.BackColor.Form(styleManager.Theme);
                return base.ToolStripDropDownBackground;
            }
        }
        
        public override Color ImageMarginGradientBegin
        {
            get
            {
                if (styleManager != null)
                    return PoisonPaint.BackColor.Form(styleManager.Theme);
                return base.ImageMarginGradientBegin;
            }
        }
        
        public override Color ImageMarginGradientEnd
        {
            get
            {
                if (styleManager != null)
                    return PoisonPaint.BackColor.Form(styleManager.Theme);
                return base.ImageMarginGradientEnd;
            }
        }
        
        public override Color ImageMarginGradientMiddle
        {
            get
            {
                if (styleManager != null)
                    return PoisonPaint.BackColor.Form(styleManager.Theme);
                return base.ImageMarginGradientMiddle;
            }
        }
    }
    
    #endregion
}

