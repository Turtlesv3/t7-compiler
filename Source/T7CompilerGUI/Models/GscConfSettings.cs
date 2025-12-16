using System;
using System.Collections.Generic;
using T7CompilerGUI.Forms;
using TreyarchCompiler.Enums;

namespace T7CompilerGUI.Models
{
    /// <summary>
    /// Represents settings loaded from gsc.conf file
    /// </summary>
    public class GscConfSettings
    {
        /// <summary>
        /// List of conditional compilation symbols
        /// </summary>
        public List<string> Symbols { get; set; } = new List<string>();

        /// <summary>
        /// Script location path (relative or absolute)
        /// </summary>
        public string ScriptLocation { get; set; }

        /// <summary>
        /// Injection path (e.g., scripts/shared/duplicaterender_mgr.gsc)
        /// </summary>
        public string Script { get; set; }

        /// <summary>
        /// Output filename (without extension)
        /// </summary>
        public string File { get; set; }

        /// <summary>
        /// No runtime flag
        /// </summary>
        public bool? NoRuntime { get; set; }

        /// <summary>
        /// Hot reload mode
        /// </summary>
        public HotMode? Hot { get; set; }

        /// <summary>
        /// Game (T7/BO3 or T8/BO4)
        /// </summary>
        public TreyarchCompiler.Enums.Games? Game { get; set; }

        /// <summary>
        /// Indicates if any settings were loaded
        /// </summary>
        public bool HasSettings => Symbols.Count > 0 || 
                                   !string.IsNullOrWhiteSpace(ScriptLocation) ||
                                   !string.IsNullOrWhiteSpace(Script) ||
                                   !string.IsNullOrWhiteSpace(File) ||
                                   NoRuntime.HasValue ||
                                   Hot.HasValue ||
                                   Game.HasValue;
    }
}

