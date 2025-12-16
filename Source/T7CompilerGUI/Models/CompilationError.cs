using System;
using System.Collections.Generic;
using T7CompilerGUI.Utils;

namespace T7CompilerGUI.Models
{
    /// <summary>
    /// Represents a compilation error with context information
    /// </summary>
    public class CompilationError
    {
        /// <summary>
        /// Error message/description
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Source file where the error occurred (relative path)
        /// </summary>
        public string File { get; set; }

        /// <summary>
        /// Line number where the error occurred (1-based)
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// Character position within the line (0-based)
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// Character position in the full source (for preprocessor errors)
        /// </summary>
        public int CharacterPosition { get; set; }

        /// <summary>
        /// Source tokens for error mapping
        /// </summary>
        public List<SourceTokenDef> SourceTokens { get; set; }

        /// <summary>
        /// Script location for resolving file paths
        /// </summary>
        public string ScriptLocation { get; set; }

        /// <summary>
        /// Error type
        /// </summary>
        public CompilationErrorType Type { get; set; }

        /// <summary>
        /// Full source code (for context extraction)
        /// </summary>
        public string Source { get; set; }
    }

    /// <summary>
    /// Types of compilation errors
    /// </summary>
    public enum CompilationErrorType
    {
        PreprocessorSyntax,
        CompilerError,
        CompilerNullResult,
        Unknown
    }
}

