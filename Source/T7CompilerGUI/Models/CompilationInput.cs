using System.Collections.Generic;

namespace T7CompilerGUI.Models
{
    /// <summary>
    /// Input parameters for compilation
    /// </summary>
    public class CompilationInput
    {
        /// <summary>
        /// Project folder path
        /// </summary>
        public string ProjectFolder { get; set; }

        /// <summary>
        /// Script location path (can be different from project folder)
        /// </summary>
        public string ScriptLocation { get; set; }

        /// <summary>
        /// Output file path
        /// </summary>
        public string OutputFile { get; set; }

        /// <summary>
        /// Build folder for default output location
        /// </summary>
        public string BuildFolder { get; set; }

        /// <summary>
        /// Conditional compilation symbols
        /// </summary>
        public List<string> Symbols { get; set; } = new List<string>();

        /// <summary>
        /// Platform (PC, PS4, etc.)
        /// </summary>
        public TreyarchCompiler.Enums.Platforms Platform { get; set; }

        /// <summary>
        /// Game (T7/BO3 or T8/BO4)
        /// </summary>
        public TreyarchCompiler.Enums.Games Game { get; set; }

        /// <summary>
        /// Compilation mode
        /// </summary>
        public TreyarchCompiler.Enums.Modes Mode { get; set; }

        /// <summary>
        /// Use masking flag
        /// </summary>
        public bool UseMasking { get; set; }

        /// <summary>
        /// Database file path
        /// </summary>
        public string DatabasePath { get; set; }
    }
}

