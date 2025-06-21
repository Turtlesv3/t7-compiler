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

namespace DebugCompiler.UI.Core.Singletons
{
    public struct UIThemeInfo
    {
        public string Name { get; set; }
        public bool IsDarkTheme { get; set; }

        // Core Colors
        public Color BackColor { get; set; }
        public Color ControlBackColor { get; set; }
        public Color TextColor { get; set; }
        public Color AccentColor { get; set; }

        // Component Colors
        public Color TextBoxBackColor { get; set; }
        public Color ButtonBackColor { get; set; }
        public Color ButtonHoverColor { get; set; }
        public Color ButtonActiveColor { get; set; }
        public Color BorderColor { get; set; }
        public Color DisabledTextColor { get; set; }
        public Color HighlightColor { get; set; }
        public Color MenuBackColor { get; set; }
        public Color MenuTextColor { get; set; }
        public Color GridLineColor { get; set; }

        // Styles
        public BorderStyle TextBoxBorderStyle { get; set; }
        public FlatStyle ButtonFlatStyle { get; set; }
        public FontStyle HeaderFontStyle { get; set; }

        // Built-in Themes
        public static UIThemeInfo Dark => new()
        {
            Name = "Dark",
            IsDarkTheme = true,
            BackColor = Color.FromArgb(28, 28, 28),
            TextColor = Color.WhiteSmoke,
            TextBoxBackColor = Color.FromArgb(35, 35, 35),
            ButtonBackColor = Color.FromArgb(50, 50, 50),
            ControlBackColor = Color.FromArgb(40, 40, 40),
            ButtonActiveColor = Color.DodgerBlue,
            ButtonHoverColor = Color.FromArgb(70, 70, 70),
            BorderColor = Color.FromArgb(60, 60, 60),
            TextBoxBorderStyle = BorderStyle.FixedSingle,
            AccentColor = Color.FromArgb(0, 122, 204),
            ButtonFlatStyle = FlatStyle.Flat
        };

        public static UIThemeInfo Light => new()
        {
            Name = "Light",
            IsDarkTheme = false,
            BackColor = SystemColors.Control,
            ControlBackColor = SystemColors.ControlLight,
            TextColor = SystemColors.ControlText,
            TextBoxBackColor = SystemColors.Window,
            ButtonBackColor = SystemColors.Control,
            ButtonActiveColor = SystemColors.Highlight,
            ButtonHoverColor = SystemColors.ControlLight,
            BorderColor = SystemColors.ControlDark,
            TextBoxBorderStyle = BorderStyle.Fixed3D,
            AccentColor = Color.FromArgb(0, 90, 158),
            ButtonFlatStyle = FlatStyle.Standard
        };

        public static UIThemeInfo Dracula => new()
        {
            Name = "Dracula",
            IsDarkTheme = true,
            BackColor = Color.FromArgb(40, 42, 54),
            ControlBackColor = Color.FromArgb(68, 71, 90),
            TextColor = Color.FromArgb(248, 248, 242),
            AccentColor = Color.FromArgb(189, 147, 249),
            TextBoxBackColor = Color.FromArgb(68, 71, 90),
            ButtonBackColor = Color.FromArgb(98, 114, 164),
            ButtonHoverColor = Color.FromArgb(139, 233, 253),
            ButtonActiveColor = Color.FromArgb(255, 121, 198),
            BorderColor = Color.FromArgb(139, 233, 253),
            DisabledTextColor = Color.FromArgb(150, 152, 165),
            HighlightColor = Color.FromArgb(255, 184, 108),
            MenuBackColor = Color.FromArgb(68, 71, 90),
            MenuTextColor = Color.FromArgb(248, 248, 242),
            GridLineColor = Color.FromArgb(139, 233, 253),
            TextBoxBorderStyle = BorderStyle.FixedSingle,
            ButtonFlatStyle = FlatStyle.Flat,
            HeaderFontStyle = FontStyle.Bold
        };

        public static UIThemeInfo SolarizedDark => new()
        {
            Name = "Solarized Dark",
            IsDarkTheme = true,
            BackColor = Color.FromArgb(0, 43, 54),
            ControlBackColor = Color.FromArgb(7, 54, 66),
            TextColor = Color.FromArgb(131, 148, 150),
            AccentColor = Color.FromArgb(38, 139, 210),
            TextBoxBackColor = Color.FromArgb(7, 54, 66),
            ButtonBackColor = Color.FromArgb(0, 43, 54),
            ButtonHoverColor = Color.FromArgb(203, 75, 22),
            ButtonActiveColor = Color.FromArgb(220, 50, 47),
            BorderColor = Color.FromArgb(88, 110, 117),
            DisabledTextColor = Color.FromArgb(101, 123, 131),
            HighlightColor = Color.FromArgb(181, 137, 0),
            MenuBackColor = Color.FromArgb(7, 54, 66),
            MenuTextColor = Color.FromArgb(131, 148, 150),
            GridLineColor = Color.FromArgb(88, 110, 117),
            TextBoxBorderStyle = BorderStyle.FixedSingle,
            ButtonFlatStyle = FlatStyle.Flat,
            HeaderFontStyle = FontStyle.Italic
        };

        public static UIThemeInfo Nord => new()
        {
            Name = "Nord",
            IsDarkTheme = true,
            BackColor = Color.FromArgb(46, 52, 64),
            ControlBackColor = Color.FromArgb(59, 66, 82),
            TextColor = Color.FromArgb(236, 239, 244),
            AccentColor = Color.FromArgb(129, 161, 193),
            TextBoxBackColor = Color.FromArgb(67, 76, 94),
            ButtonBackColor = Color.FromArgb(76, 86, 106),
            ButtonHoverColor = Color.FromArgb(143, 188, 187),
            ButtonActiveColor = Color.FromArgb(191, 97, 106),
            BorderColor = Color.FromArgb(76, 86, 106),
            DisabledTextColor = Color.FromArgb(136, 192, 208),
            HighlightColor = Color.FromArgb(163, 190, 140),
            MenuBackColor = Color.FromArgb(59, 66, 82),
            MenuTextColor = Color.FromArgb(236, 239, 244),
            GridLineColor = Color.FromArgb(76, 86, 106),
            TextBoxBorderStyle = BorderStyle.FixedSingle,
            ButtonFlatStyle = FlatStyle.Flat,
            HeaderFontStyle = FontStyle.Bold
        };

        public static UIThemeInfo MaterialDeepPurple => new()
        {
            Name = "Material Deep Purple",
            IsDarkTheme = false,
            BackColor = Color.FromArgb(237, 231, 246),
            ControlBackColor = Color.FromArgb(255, 255, 255),
            TextColor = Color.FromArgb(33, 33, 33),
            AccentColor = Color.FromArgb(103, 58, 183),
            TextBoxBackColor = Color.White,
            ButtonBackColor = Color.FromArgb(103, 58, 183),
            ButtonHoverColor = Color.FromArgb(156, 39, 176),
            ButtonActiveColor = Color.FromArgb(233, 30, 99),
            BorderColor = Color.FromArgb(224, 224, 224),
            DisabledTextColor = Color.FromArgb(158, 158, 158),
            HighlightColor = Color.FromArgb(255, 152, 0),
            MenuBackColor = Color.White,
            MenuTextColor = Color.FromArgb(33, 33, 33),
            GridLineColor = Color.FromArgb(224, 224, 224),
            TextBoxBorderStyle = BorderStyle.FixedSingle,
            ButtonFlatStyle = FlatStyle.Flat,
            HeaderFontStyle = FontStyle.Bold
        };

        public static UIThemeInfo Cyberpunk => new()
        {
            Name = "Cyberpunk",
            IsDarkTheme = true,
            BackColor = Color.FromArgb(15, 5, 30),
            ControlBackColor = Color.FromArgb(30, 10, 60),
            TextColor = Color.FromArgb(255, 40, 180),
            AccentColor = Color.FromArgb(0, 255, 255),
            TextBoxBackColor = Color.FromArgb(30, 10, 60),
            ButtonBackColor = Color.FromArgb(80, 0, 100),
            ButtonHoverColor = Color.FromArgb(255, 0, 150),
            ButtonActiveColor = Color.FromArgb(255, 200, 0),
            BorderColor = Color.FromArgb(0, 255, 255),
            DisabledTextColor = Color.FromArgb(100, 40, 120),
            HighlightColor = Color.FromArgb(255, 200, 0),
            MenuBackColor = Color.FromArgb(30, 10, 60),
            MenuTextColor = Color.FromArgb(255, 40, 180),
            GridLineColor = Color.FromArgb(0, 255, 255),
            TextBoxBorderStyle = BorderStyle.FixedSingle,
            ButtonFlatStyle = FlatStyle.Flat,
            HeaderFontStyle = FontStyle.Bold
        };
        // Add more themes as needed
        public static IEnumerable<UIThemeInfo> AvailableThemes => new[]
        {
            Dark,
            Light,
            Dracula,
            SolarizedDark,
            Nord,
            MaterialDeepPurple,
            Cyberpunk
        };

        public Color GetDisabledControlColor() =>
            Color.FromArgb((this.ControlBackColor.R + this.BackColor.R) / 2,
                          (this.ControlBackColor.G + this.BackColor.G) / 2,
                          (this.ControlBackColor.B + this.BackColor.B) / 2);
    }
}