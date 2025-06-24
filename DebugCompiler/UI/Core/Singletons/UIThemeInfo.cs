using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DebugCompiler.UI.Core.Singletons
{
    public struct UIThemeInfo : IEquatable<UIThemeInfo>
    {
        // Theme Identification
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public bool IsDarkTheme { get; set; }
        public string IconResourceName { get; set; }

        // Core Colors
        public Color BackColor { get; set; }
        public Color ForeColor { get; set; }
        public Color ControlBackColor { get; set; }
        public Color TextColor { get; set; }
        public Color AccentColor { get; set; }
        public Color SecondaryAccentColor { get; set; }

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
        public Color HeaderBackColor { get; set; }
        public Color ToolStripBackColor { get; set; }
        public Color StatusBarBackColor { get; set; }

        // Extended Colors
        public Color SuccessColor { get; set; }
        public Color WarningColor { get; set; }
        public Color ErrorColor { get; set; }
        public Color InfoColor { get; set; }

        // Styles
        public BorderStyle TextBoxBorderStyle { get; set; }
        public FlatStyle ButtonFlatStyle { get; set; }
        public FontStyle HeaderFontStyle { get; set; }
        public int BorderWidth { get; set; }
        public int CornerRadius { get; set; }

        // Font Settings
        public string FontFamily { get; set; }
        public float BaseFontSize { get; set; }
        public float HeaderFontSize { get; set; }

        // Theme Metadata
        public string Author { get; set; }
        public string Description { get; set; }
        public Version Version { get; set; }

        // Compatibility property (alias for BackColor)
        public Color WindowBackColor
        {
            get => BackColor;
            set => BackColor = value;
        }

        // Compatibility property (alias for ForeColor)
        public Color WindowForeColor
        {
            get => ForeColor;
            set => ForeColor = value;
        }

        public Color GetColor(string colorKey)
        {
            return colorKey switch
            {
                "@dialog_bg" => this.BackColor,
                "@text_primary" => this.TextColor,
                "@border_color" => this.BorderColor,
                "@button_bg" => this.ButtonBackColor,
                "@textbox_bg" => this.TextBoxBackColor,
                "@accent" => this.AccentColor,
                "@highlight" => this.HighlightColor,
                "@secondary_accent" => this.SecondaryAccentColor,
                "@success" => this.SuccessColor,
                "@warning" => this.WarningColor,
                "@error" => this.ErrorColor,
                "@info" => this.InfoColor,
                _ => throw new ArgumentException($"Unknown color key: {colorKey}")
            };
        }

        public static UIThemeInfo GetThemeByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return default;

            return AvailableThemes.FirstOrDefault(t =>
                t.Name?.Equals(name, StringComparison.OrdinalIgnoreCase) == true ||
                t.DisplayName?.Equals(name, StringComparison.OrdinalIgnoreCase) == true);
        }

        public Color GetDisabledControlColor() =>
            Color.FromArgb(
                (ControlBackColor.R + BackColor.R) / 2,
                (ControlBackColor.G + BackColor.G) / 2,
                (ControlBackColor.B + BackColor.B) / 2
            );

        public bool IsEmpty() => string.IsNullOrEmpty(Name);

        public bool Equals(UIThemeInfo other) =>
            Name == other.Name &&
            Version == other.Version;

        public override bool Equals(object obj) =>
            obj is UIThemeInfo other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (Name?.GetHashCode() ?? 0);
                hash = hash * 23 + Version.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(UIThemeInfo left, UIThemeInfo right) =>
            left.Equals(right);

        public static bool operator !=(UIThemeInfo left, UIThemeInfo right) =>
            !left.Equals(right);

        // Built-in Themes (updated with new properties)
        public static UIThemeInfo Dark => new()
        {
            Name = "Dark",
            DisplayName = "Dark Theme",
            IsDarkTheme = true,
            BackColor = Color.FromArgb(28, 28, 28),
            ForeColor = Color.WhiteSmoke,
            TextColor = Color.WhiteSmoke,
            ControlBackColor = Color.FromArgb(40, 40, 40),
            TextBoxBackColor = Color.FromArgb(35, 35, 35),
            ButtonBackColor = Color.FromArgb(50, 50, 50),
            ButtonHoverColor = Color.FromArgb(70, 70, 70),
            ButtonActiveColor = Color.DodgerBlue,
            BorderColor = Color.FromArgb(60, 60, 60),
            AccentColor = Color.FromArgb(0, 122, 204),
            SecondaryAccentColor = Color.FromArgb(100, 150, 200),
            DisabledTextColor = Color.Gray,
            HighlightColor = Color.FromArgb(255, 184, 108),
            MenuBackColor = Color.FromArgb(40, 40, 40),
            MenuTextColor = Color.WhiteSmoke,
            GridLineColor = Color.FromArgb(60, 60, 60),
            HeaderBackColor = Color.FromArgb(45, 45, 45),
            ToolStripBackColor = Color.FromArgb(50, 50, 50),
            StatusBarBackColor = Color.FromArgb(35, 35, 35),
            SuccessColor = Color.FromArgb(76, 175, 80),
            WarningColor = Color.FromArgb(255, 152, 0),
            ErrorColor = Color.FromArgb(244, 67, 54),
            InfoColor = Color.FromArgb(33, 150, 243),
            TextBoxBorderStyle = BorderStyle.FixedSingle,
            ButtonFlatStyle = FlatStyle.Flat,
            HeaderFontStyle = FontStyle.Bold,
            BorderWidth = 1,
            CornerRadius = 3,
            FontFamily = "Segoe UI",
            BaseFontSize = 9f,
            HeaderFontSize = 10f,
            Author = "System",
            Description = "Default dark theme",
            Version = new Version(1, 0),
            IconResourceName = "theme_dark"
        };

        public static UIThemeInfo Light => new()
        {
            Name = "Light",
            DisplayName = "Light Theme",
            IsDarkTheme = false,
            BackColor = SystemColors.Control,
            ForeColor = SystemColors.ControlText,
            ControlBackColor = SystemColors.ControlLight,
            TextColor = SystemColors.ControlText,
            AccentColor = Color.FromArgb(0, 90, 158),
            SecondaryAccentColor = Color.FromArgb(0, 120, 200),
            TextBoxBackColor = SystemColors.Window,
            ButtonBackColor = SystemColors.Control,
            ButtonHoverColor = SystemColors.ControlLight,
            ButtonActiveColor = SystemColors.Highlight,
            BorderColor = SystemColors.ControlDark,
            DisabledTextColor = SystemColors.GrayText,
            HighlightColor = Color.FromArgb(255, 184, 108),
            MenuBackColor = SystemColors.Menu,
            MenuTextColor = SystemColors.MenuText,
            GridLineColor = SystemColors.ControlDark,
            HeaderBackColor = SystemColors.ControlLightLight,
            ToolStripBackColor = SystemColors.ControlLight,
            StatusBarBackColor = SystemColors.ControlDark,
            SuccessColor = Color.FromArgb(46, 125, 50),
            WarningColor = Color.FromArgb(255, 152, 0),
            ErrorColor = Color.FromArgb(198, 40, 40),
            InfoColor = Color.FromArgb(2, 136, 209),
            TextBoxBorderStyle = BorderStyle.Fixed3D,
            ButtonFlatStyle = FlatStyle.Standard,
            HeaderFontStyle = FontStyle.Regular,
            BorderWidth = 1,
            CornerRadius = 2,
            FontFamily = "Segoe UI",
            BaseFontSize = 9f,
            HeaderFontSize = 10f,
            Author = "System",
            Description = "Default light theme",
            Version = new Version(1, 0),
            IconResourceName = "theme_light"
        };

        public static UIThemeInfo Dracula => new()
        {
            Name = "Dracula",
            DisplayName = "Dracula Theme",
            IsDarkTheme = true,
            BackColor = Color.FromArgb(40, 42, 54),
            ForeColor = Color.FromArgb(248, 248, 242),
            ControlBackColor = Color.FromArgb(68, 71, 90),
            TextColor = Color.FromArgb(248, 248, 242),
            AccentColor = Color.FromArgb(189, 147, 249),
            SecondaryAccentColor = Color.FromArgb(80, 250, 123),
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
            HeaderBackColor = Color.FromArgb(58, 61, 80),
            ToolStripBackColor = Color.FromArgb(78, 81, 100),
            StatusBarBackColor = Color.FromArgb(50, 52, 64),
            SuccessColor = Color.FromArgb(80, 250, 123),
            WarningColor = Color.FromArgb(255, 184, 108),
            ErrorColor = Color.FromArgb(255, 85, 85),
            InfoColor = Color.FromArgb(139, 233, 253),
            TextBoxBorderStyle = BorderStyle.FixedSingle,
            ButtonFlatStyle = FlatStyle.Flat,
            HeaderFontStyle = FontStyle.Bold,
            BorderWidth = 1,
            CornerRadius = 3,
            FontFamily = "Segoe UI",
            BaseFontSize = 9f,
            HeaderFontSize = 10f,
            Author = "Dracula Theme",
            Description = "Official Dracula color scheme",
            Version = new Version(2, 0),
            IconResourceName = "theme_dracula"
        };

        // SolarizedDark with all new properties
        public static UIThemeInfo SolarizedDark => new()
        {
            Name = "Solarized Dark",
            DisplayName = "Solarized Dark",
            IsDarkTheme = true,
            BackColor = Color.FromArgb(0, 43, 54),
            ForeColor = Color.FromArgb(131, 148, 150),
            ControlBackColor = Color.FromArgb(7, 54, 66),
            TextColor = Color.FromArgb(131, 148, 150),
            AccentColor = Color.FromArgb(38, 139, 210),
            SecondaryAccentColor = Color.FromArgb(42, 161, 152),
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
            HeaderBackColor = Color.FromArgb(15, 63, 76),
            ToolStripBackColor = Color.FromArgb(25, 73, 86),
            StatusBarBackColor = Color.FromArgb(5, 53, 64),
            SuccessColor = Color.FromArgb(133, 153, 0),
            WarningColor = Color.FromArgb(181, 137, 0),
            ErrorColor = Color.FromArgb(220, 50, 47),
            InfoColor = Color.FromArgb(38, 139, 210),
            TextBoxBorderStyle = BorderStyle.FixedSingle,
            ButtonFlatStyle = FlatStyle.Flat,
            HeaderFontStyle = FontStyle.Italic,
            BorderWidth = 1,
            CornerRadius = 3,
            FontFamily = "Consolas",
            BaseFontSize = 9f,
            HeaderFontSize = 10f,
            Author = "Ethan Schoonover",
            Description = "Solarized dark color scheme",
            Version = new Version(1, 5),
            IconResourceName = "theme_solarized"
        };

        public static UIThemeInfo Nord => new()
        {
            Name = "Nord",
            DisplayName = "Nord Theme",
            IsDarkTheme = true,
            BackColor = Color.FromArgb(46, 52, 64),
            ForeColor = Color.FromArgb(236, 239, 244),
            ControlBackColor = Color.FromArgb(59, 66, 82),
            TextColor = Color.FromArgb(236, 239, 244),
            AccentColor = Color.FromArgb(129, 161, 193),
            SecondaryAccentColor = Color.FromArgb(143, 188, 187),
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
            HeaderBackColor = Color.FromArgb(69, 76, 92),
            ToolStripBackColor = Color.FromArgb(66, 73, 89),
            StatusBarBackColor = Color.FromArgb(56, 63, 78),
            SuccessColor = Color.FromArgb(163, 190, 140),
            WarningColor = Color.FromArgb(208, 135, 112),
            ErrorColor = Color.FromArgb(191, 97, 106),
            InfoColor = Color.FromArgb(136, 192, 208),
            TextBoxBorderStyle = BorderStyle.FixedSingle,
            ButtonFlatStyle = FlatStyle.Flat,
            HeaderFontStyle = FontStyle.Bold,
            BorderWidth = 1,
            CornerRadius = 4,
            FontFamily = "Segoe UI",
            BaseFontSize = 9f,
            HeaderFontSize = 10.5f,
            Author = "Arctic Ice Studio",
            Description = "Arctic, north-bluish color palette",
            Version = new Version(1, 0),
            IconResourceName = "theme_nord"
        };

        public static UIThemeInfo MaterialDeepPurple => new()
        {
            Name = "MaterialDeepPurple",
            DisplayName = "Material Deep Purple",
            IsDarkTheme = false,
            BackColor = Color.FromArgb(237, 231, 246),
            ForeColor = Color.FromArgb(33, 33, 33),
            ControlBackColor = Color.FromArgb(255, 255, 255),
            TextColor = Color.FromArgb(33, 33, 33),
            AccentColor = Color.FromArgb(103, 58, 183),
            SecondaryAccentColor = Color.FromArgb(156, 39, 176),
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
            HeaderBackColor = Color.FromArgb(225, 219, 242),
            ToolStripBackColor = Color.FromArgb(245, 243, 250),
            StatusBarBackColor = Color.FromArgb(207, 200, 225),
            SuccessColor = Color.FromArgb(76, 175, 80),
            WarningColor = Color.FromArgb(255, 152, 0),
            ErrorColor = Color.FromArgb(244, 67, 54),
            InfoColor = Color.FromArgb(33, 150, 243),
            TextBoxBorderStyle = BorderStyle.FixedSingle,
            ButtonFlatStyle = FlatStyle.Flat,
            HeaderFontStyle = FontStyle.Bold,
            BorderWidth = 1,
            CornerRadius = 4,
            FontFamily = "Roboto",
            BaseFontSize = 9f,
            HeaderFontSize = 10.5f,
            Author = "Google Material Design",
            Description = "Material Design with deep purple accent",
            Version = new Version(2, 0),
            IconResourceName = "theme_material_purple"
        };

        public static UIThemeInfo Cyberpunk => new()
        {
            Name = "Cyberpunk",
            DisplayName = "Cyberpunk Neon",
            IsDarkTheme = true,
            BackColor = Color.FromArgb(15, 5, 30),
            ForeColor = Color.FromArgb(255, 40, 180),
            ControlBackColor = Color.FromArgb(30, 10, 60),
            TextColor = Color.FromArgb(255, 40, 180),
            AccentColor = Color.FromArgb(0, 255, 255),
            SecondaryAccentColor = Color.FromArgb(255, 0, 150),
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
            HeaderBackColor = Color.FromArgb(40, 0, 80),
            ToolStripBackColor = Color.FromArgb(50, 20, 90),
            StatusBarBackColor = Color.FromArgb(20, 0, 50),
            SuccessColor = Color.FromArgb(0, 255, 100),
            WarningColor = Color.FromArgb(255, 150, 0),
            ErrorColor = Color.FromArgb(255, 0, 80),
            InfoColor = Color.FromArgb(0, 200, 255),
            TextBoxBorderStyle = BorderStyle.FixedSingle,
            ButtonFlatStyle = FlatStyle.Flat,
            HeaderFontStyle = FontStyle.Bold,
            BorderWidth = 2,
            CornerRadius = 0,
            FontFamily = "Consolas",
            BaseFontSize = 9f,
            HeaderFontSize = 11f,
            Author = "Cyberpunk 2077",
            Description = "Neon cyberpunk aesthetic theme",
            Version = new Version(2, 0),
            IconResourceName = "theme_cyberpunk"
        };

        public static UIThemeInfo MonokaiPro => new()
        {
            Name = "MonokaiPro",
            DisplayName = "Monokai Pro",
            IsDarkTheme = true,
            BackColor = Color.FromArgb(45, 42, 46),
            ForeColor = Color.FromArgb(248, 248, 242),
            ControlBackColor = Color.FromArgb(57, 53, 58),
            TextColor = Color.FromArgb(248, 248, 242),
            AccentColor = Color.FromArgb(255, 154, 48),
            SecondaryAccentColor = Color.FromArgb(249, 38, 114),
            TextBoxBackColor = Color.FromArgb(57, 53, 58),
            ButtonBackColor = Color.FromArgb(87, 82, 89),
            ButtonHoverColor = Color.FromArgb(121, 113, 124),
            ButtonActiveColor = Color.FromArgb(249, 38, 114),
            BorderColor = Color.FromArgb(87, 82, 89),
            DisabledTextColor = Color.FromArgb(117, 110, 119),
            HighlightColor = Color.FromArgb(166, 226, 46),
            MenuBackColor = Color.FromArgb(57, 53, 58),
            MenuTextColor = Color.FromArgb(248, 248, 242),
            GridLineColor = Color.FromArgb(87, 82, 89),
            HeaderBackColor = Color.FromArgb(67, 63, 68),
            ToolStripBackColor = Color.FromArgb(77, 73, 78),
            StatusBarBackColor = Color.FromArgb(47, 43, 48),
            SuccessColor = Color.FromArgb(166, 226, 46),
            WarningColor = Color.FromArgb(253, 151, 31),
            ErrorColor = Color.FromArgb(249, 38, 114),
            InfoColor = Color.FromArgb(102, 217, 239),
            TextBoxBorderStyle = BorderStyle.FixedSingle,
            ButtonFlatStyle = FlatStyle.Flat,
            HeaderFontStyle = FontStyle.Bold,
            BorderWidth = 1,
            CornerRadius = 3,
            FontFamily = "Fira Code",
            BaseFontSize = 9f,
            HeaderFontSize = 10.5f,
            Author = "Monokai",
            Description = "Professional Monokai color scheme",
            Version = new Version(1, 5),
            IconResourceName = "theme_monokai"
        };

        public static UIThemeInfo Gruvbox => new()
        {
            Name = "Gruvbox",
            DisplayName = "Gruvbox",
            IsDarkTheme = true,
            BackColor = Color.FromArgb(40, 40, 40),
            ForeColor = Color.FromArgb(235, 219, 178),
            ControlBackColor = Color.FromArgb(60, 56, 54),
            TextColor = Color.FromArgb(235, 219, 178),
            AccentColor = Color.FromArgb(254, 128, 25),
            SecondaryAccentColor = Color.FromArgb(184, 187, 38),
            TextBoxBackColor = Color.FromArgb(50, 48, 47),
            ButtonBackColor = Color.FromArgb(80, 73, 69),
            ButtonHoverColor = Color.FromArgb(184, 187, 38),
            ButtonActiveColor = Color.FromArgb(251, 73, 52),
            BorderColor = Color.FromArgb(102, 92, 84),
            DisabledTextColor = Color.FromArgb(146, 131, 116),
            HighlightColor = Color.FromArgb(215, 153, 33),
            MenuBackColor = Color.FromArgb(60, 56, 54),
            MenuTextColor = Color.FromArgb(235, 219, 178),
            GridLineColor = Color.FromArgb(102, 92, 84),
            HeaderBackColor = Color.FromArgb(70, 66, 64),
            ToolStripBackColor = Color.FromArgb(80, 76, 74),
            StatusBarBackColor = Color.FromArgb(50, 46, 44),
            SuccessColor = Color.FromArgb(184, 187, 38),
            WarningColor = Color.FromArgb(215, 153, 33),
            ErrorColor = Color.FromArgb(251, 73, 52),
            InfoColor = Color.FromArgb(131, 165, 152),
            TextBoxBorderStyle = BorderStyle.FixedSingle,
            ButtonFlatStyle = FlatStyle.Flat,
            HeaderFontStyle = FontStyle.Bold,
            BorderWidth = 1,
            CornerRadius = 3,
            FontFamily = "Terminus",
            BaseFontSize = 9f,
            HeaderFontSize = 10.5f,
            Author = "Pavel Pertsev",
            Description = "Retro groove color scheme",
            Version = new Version(1, 0),
            IconResourceName = "theme_gruvbox"
        };

        public static UIThemeInfo OneDark => new()
        {
            Name = "OneDark",
            DisplayName = "One Dark",
            IsDarkTheme = true,
            BackColor = Color.FromArgb(40, 44, 52),
            ForeColor = Color.FromArgb(171, 178, 191),
            ControlBackColor = Color.FromArgb(53, 59, 69),
            TextColor = Color.FromArgb(171, 178, 191),
            AccentColor = Color.FromArgb(97, 175, 239),
            SecondaryAccentColor = Color.FromArgb(152, 195, 121),
            TextBoxBackColor = Color.FromArgb(53, 59, 69),
            ButtonBackColor = Color.FromArgb(56, 63, 78),
            ButtonHoverColor = Color.FromArgb(152, 195, 121),
            ButtonActiveColor = Color.FromArgb(209, 154, 102),
            BorderColor = Color.FromArgb(62, 68, 81),
            DisabledTextColor = Color.FromArgb(92, 99, 112),
            HighlightColor = Color.FromArgb(198, 120, 221),
            MenuBackColor = Color.FromArgb(53, 59, 69),
            MenuTextColor = Color.FromArgb(171, 178, 191),
            GridLineColor = Color.FromArgb(62, 68, 81),
            HeaderBackColor = Color.FromArgb(63, 69, 79),
            ToolStripBackColor = Color.FromArgb(73, 79, 89),
            StatusBarBackColor = Color.FromArgb(43, 49, 59),
            SuccessColor = Color.FromArgb(152, 195, 121),
            WarningColor = Color.FromArgb(209, 154, 102),
            ErrorColor = Color.FromArgb(224, 108, 117),
            InfoColor = Color.FromArgb(86, 182, 194),
            TextBoxBorderStyle = BorderStyle.FixedSingle,
            ButtonFlatStyle = FlatStyle.Flat,
            HeaderFontStyle = FontStyle.Bold,
            BorderWidth = 1,
            CornerRadius = 4,
            FontFamily = "Fira Code",
            BaseFontSize = 9f,
            HeaderFontSize = 10.5f,
            Author = "Atom",
            Description = "Default Atom editor dark theme",
            Version = new Version(1, 0),
            IconResourceName = "theme_one_dark"
        };

        public static UIThemeInfo TokyoNight => new()
        {
            Name = "TokyoNight",
            DisplayName = "Tokyo Night",
            IsDarkTheme = true,
            BackColor = Color.FromArgb(26, 27, 38),
            ForeColor = Color.FromArgb(169, 177, 214),
            ControlBackColor = Color.FromArgb(48, 52, 70),
            TextColor = Color.FromArgb(169, 177, 214),
            AccentColor = Color.FromArgb(122, 162, 247),
            SecondaryAccentColor = Color.FromArgb(180, 142, 173),
            TextBoxBackColor = Color.FromArgb(48, 52, 70),
            ButtonBackColor = Color.FromArgb(65, 72, 104),
            ButtonHoverColor = Color.FromArgb(180, 142, 173),
            ButtonActiveColor = Color.FromArgb(247, 118, 142),
            BorderColor = Color.FromArgb(65, 72, 104),
            DisabledTextColor = Color.FromArgb(92, 95, 119),
            HighlightColor = Color.FromArgb(158, 206, 106),
            MenuBackColor = Color.FromArgb(48, 52, 70),
            MenuTextColor = Color.FromArgb(169, 177, 214),
            GridLineColor = Color.FromArgb(65, 72, 104),
            HeaderBackColor = Color.FromArgb(58, 62, 80),
            ToolStripBackColor = Color.FromArgb(68, 72, 90),
            StatusBarBackColor = Color.FromArgb(36, 40, 58),
            SuccessColor = Color.FromArgb(158, 206, 106),
            WarningColor = Color.FromArgb(224, 175, 104),
            ErrorColor = Color.FromArgb(247, 118, 142),
            InfoColor = Color.FromArgb(122, 162, 247),
            TextBoxBorderStyle = BorderStyle.FixedSingle,
            ButtonFlatStyle = FlatStyle.Flat,
            HeaderFontStyle = FontStyle.Bold,
            BorderWidth = 1,
            CornerRadius = 4,
            FontFamily = "JetBrains Mono",
            BaseFontSize = 9f,
            HeaderFontSize = 10.5f,
            Author = "Folke Lemaitre",
            Description = "Elegant dark theme inspired by Tokyo",
            Version = new Version(1, 0),
            IconResourceName = "theme_tokyo_night"
        };

        public static UIThemeInfo CatppuccinMocha => new()
        {
            Name = "CatppuccinMocha",
            DisplayName = "Catppuccin Mocha",
            IsDarkTheme = true,
            BackColor = Color.FromArgb(30, 30, 46),
            ForeColor = Color.FromArgb(205, 214, 244),
            ControlBackColor = Color.FromArgb(49, 50, 68),
            TextColor = Color.FromArgb(205, 214, 244),
            AccentColor = Color.FromArgb(137, 180, 250),
            SecondaryAccentColor = Color.FromArgb(243, 139, 168),
            TextBoxBackColor = Color.FromArgb(49, 50, 68),
            ButtonBackColor = Color.FromArgb(69, 71, 90),
            ButtonHoverColor = Color.FromArgb(166, 227, 161),
            ButtonActiveColor = Color.FromArgb(243, 139, 168),
            BorderColor = Color.FromArgb(69, 71, 90),
            DisabledTextColor = Color.FromArgb(127, 132, 156),
            HighlightColor = Color.FromArgb(249, 226, 175),
            MenuBackColor = Color.FromArgb(49, 50, 68),
            MenuTextColor = Color.FromArgb(205, 214, 244),
            GridLineColor = Color.FromArgb(69, 71, 90),
            HeaderBackColor = Color.FromArgb(59, 60, 78),
            ToolStripBackColor = Color.FromArgb(69, 70, 88),
            StatusBarBackColor = Color.FromArgb(39, 40, 58),
            SuccessColor = Color.FromArgb(166, 227, 161),
            WarningColor = Color.FromArgb(249, 226, 175),
            ErrorColor = Color.FromArgb(243, 139, 168),
            InfoColor = Color.FromArgb(137, 180, 250),
            TextBoxBorderStyle = BorderStyle.FixedSingle,
            ButtonFlatStyle = FlatStyle.Flat,
            HeaderFontStyle = FontStyle.Bold,
            BorderWidth = 1,
            CornerRadius = 6,
            FontFamily = "Inter",
            BaseFontSize = 9f,
            HeaderFontSize = 10.5f,
            Author = "Catppuccin",
            Description = "Soothing pastel theme for warm coding nights",
            Version = new Version(1, 0),
            IconResourceName = "theme_catppuccin"
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
            Cyberpunk,
            MonokaiPro,
            Gruvbox,
            OneDark,
            TokyoNight,
            CatppuccinMocha
        };

        public Color GetContrastTextColor(Color backColor)
        {
            // Calculate luminance and return appropriate text color
            double luminance = (0.299 * backColor.R + 0.587 * backColor.G + 0.114 * backColor.B) / 255;
            return luminance > 0.5 ? Color.Black : Color.White;
        }

        public Color GetHoverColor(Color baseColor)
        {
            return IsDarkTheme
                ? ControlPaint.Light(baseColor, 0.2f)
                : ControlPaint.Dark(baseColor, 0.1f);
        }

        public Color GetPressedColor(Color baseColor)
        {
            return IsDarkTheme
                ? ControlPaint.Dark(baseColor, 0.3f)
                : ControlPaint.Light(baseColor, 0.3f);
        }
    }
}