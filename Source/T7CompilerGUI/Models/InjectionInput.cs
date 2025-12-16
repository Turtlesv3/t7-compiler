using System;
using T7CompilerGUI.Forms;

namespace T7CompilerGUI.Models
{
    /// <summary>
    /// Input parameters for injection
    /// </summary>
    public class InjectionInput
    {
        /// <summary>
        /// Path to the compiled file to inject
        /// </summary>
        public string InjectFile { get; set; }

        /// <summary>
        /// Replace path (e.g., scripts/shared/duplicaterender_mgr.gsc)
        /// </summary>
        public string ReplacePath { get; set; }

        /// <summary>
        /// Game (T7/BO3 or T8/BO4)
        /// </summary>
        public TreyarchCompiler.Enums.Games Game { get; set; }

        /// <summary>
        /// No runtime flag
        /// </summary>
        public bool NoRuntime { get; set; }

        /// <summary>
        /// Hot reload mode
        /// </summary>
        public HotMode HotMode { get; set; }
    }
}

