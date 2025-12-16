using System.Collections.Generic;
using T7CompilerGUI.Forms.Dialogs;

namespace T7CompilerGUI.Models
{
    /// <summary>
    /// Complete configuration data model
    /// </summary>
    public class ConfigData
    {
        public ApplicationSettings Application { get; set; } = new ApplicationSettings();
        public SelectionState Selections { get; set; } = new SelectionState();
        public WindowStateSettings WindowState { get; set; } = new WindowStateSettings();
        public List<string> RecentProjects { get; set; } = new List<string>();
        public List<string> RecentFiles { get; set; } = new List<string>();
        public Dictionary<string, KeybindDialog.KeybindInfo> Keybinds { get; set; } = new Dictionary<string, KeybindDialog.KeybindInfo>();
        public InjectionStateSettings Injection { get; set; } = new InjectionStateSettings();
    }
}

