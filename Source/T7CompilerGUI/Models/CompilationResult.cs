using System;
using System.Collections.Generic;
using TreyarchCompiler.Utilities;

namespace T7CompilerGUI.Models
{
    /// <summary>
    /// Result of a compilation operation
    /// </summary>
    public class CompilationResult
    {
        /// <summary>
        /// Indicates if compilation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Compiled code (null if compilation failed)
        /// </summary>
        public CompiledCode CompiledCode { get; set; }

        /// <summary>
        /// Error message if compilation failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Error file path (if error is file-specific)
        /// </summary>
        public string ErrorFile { get; set; }

        /// <summary>
        /// Error line number (if error is line-specific)
        /// </summary>
        public int ErrorLine { get; set; }

        /// <summary>
        /// Source tokens for error mapping
        /// </summary>
        public List<Utils.SourceTokenDef> SourceTokens { get; set; }

        /// <summary>
        /// Script location used for compilation
        /// </summary>
        public string ScriptLocation { get; set; }

        /// <summary>
        /// Creates a successful compilation result
        /// </summary>
        public static CompilationResult CreateSuccess(CompiledCode code, List<Utils.SourceTokenDef> sourceTokens, string scriptLocation)
        {
            return new CompilationResult
            {
                Success = true,
                CompiledCode = code,
                SourceTokens = sourceTokens,
                ScriptLocation = scriptLocation
            };
        }

        /// <summary>
        /// Creates a failed compilation result
        /// </summary>
        public static CompilationResult CreateFailure(string errorMessage, string errorFile = null, int errorLine = 0, List<Utils.SourceTokenDef> sourceTokens = null, string scriptLocation = null)
        {
            return new CompilationResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                ErrorFile = errorFile,
                ErrorLine = errorLine,
                SourceTokens = sourceTokens,
                ScriptLocation = scriptLocation
            };
        }
    }
}

