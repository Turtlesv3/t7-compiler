using System;
using System.Collections.Generic;
using ReaLTaiizor.Enum.Poison;

namespace T7CompilerGUI.Models
{
    /// <summary>
    /// Model representing application-level settings
    /// </summary>
    public class ApplicationSettings
    {
        public ThemeStyle Theme { get; set; } = ThemeStyle.Dark;
        public string ColorStyle { get; set; } = "Red"; // Can be enum name or "Rainbow"
        public string DefaultOutputPath { get; set; } = "";
        public bool AutoSaveSettings { get; set; } = true;
        public bool RestoreWindowState { get; set; } = true;
        public string LastProjectFolder { get; set; } = "";
    }
}

