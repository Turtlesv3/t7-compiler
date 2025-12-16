using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using T7CompilerGUI.Models;
using T7CompilerGUI.Utils;

namespace T7CompilerGUI.Services
{
    /// <summary>
    /// Service for parsing and formatting compilation errors
    /// </summary>
    public static class CompilationErrorParser
    {
        /// <summary>
        /// Callback for logging error details
        /// </summary>
        public delegate void LogCallback(string message);

        /// <summary>
        /// Parses a preprocessor syntax error (CBSyntaxException)
        /// </summary>
        public static CompilationError ParsePreprocessorError(
            CBSyntaxException error,
            List<SourceTokenDef> sourceTokens,
            string scriptLocation,
            string source)
        {
            if (error == null)
                return null;

            int errorCharPos = error.ErrorPosition;
            int numLineBreaks = 0;
            string errorFile = null;
            int errorLine = 0;
            int errorPos = 0;

            foreach (var stok in sourceTokens)
            {
                // Check if error is in this file's range
                if (errorCharPos >= stok.CharStart && errorCharPos <= stok.CharEnd)
                {
                    errorFile = stok.FilePath;
                    int adjustedCharPos = errorCharPos - numLineBreaks;

                    foreach (var lineMapping in stok.LineMappings)
                    {
                        var (CStart, CEnd) = lineMapping.Value;
                        if (adjustedCharPos >= CStart && adjustedCharPos <= CEnd)
                        {
                            errorLine = lineMapping.Key - stok.LineStart;
                            errorPos = adjustedCharPos - CStart;
                            break;
                        }
                    }
                    break;
                }
                numLineBreaks++;
            }

            return new CompilationError
            {
                Message = error.Message,
                File = errorFile,
                Line = errorLine,
                Position = errorPos,
                CharacterPosition = errorCharPos,
                SourceTokens = sourceTokens,
                ScriptLocation = scriptLocation,
                Type = CompilationErrorType.PreprocessorSyntax,
                Source = source
            };
        }

        /// <summary>
        /// Parses a compiler error message
        /// </summary>
        public static CompilationError ParseCompilerError(
            string errorMessage,
            List<SourceTokenDef> sourceTokens,
            string scriptLocation)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
                return null;

            string errorMsg = errorMessage;
            string errorFile = null;
            int errorLine = 0;
            string errorDescription = errorMsg;

            // Parse error format: "File: scripts/... Line: XX Error: ..."
            if (errorMsg.Contains("File:") && errorMsg.Contains("Line:"))
            {
                int fileIndex = errorMsg.IndexOf("File:");
                int lineIndex = errorMsg.IndexOf("Line:");
                int errorIndex = errorMsg.IndexOf("Error:");

                if (fileIndex >= 0 && lineIndex > fileIndex)
                {
                    // Extract file name
                    string fileSection = errorMsg.Substring(fileIndex + "File:".Length, lineIndex - fileIndex - "File:".Length).Trim();
                    errorFile = fileSection;

                    // Extract line number
                    if (errorIndex > lineIndex)
                    {
                        string lineSection = errorMsg.Substring(lineIndex + "Line:".Length, errorIndex - lineIndex - "Line:".Length).Trim();
                        if (int.TryParse(lineSection, out int parsedLine))
                            errorLine = parsedLine;

                        // Extract error description
                        errorDescription = errorMsg.Substring(errorIndex + "Error:".Length).Trim();
                    }
                    else
                    {
                        string lineSection = errorMsg.Substring(lineIndex + "Line:".Length).Trim();
                        if (int.TryParse(lineSection.Split(new[] { '\r', '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries)[0], out int parsedLine))
                            errorLine = parsedLine;
                    }
                }
            }
            // Parse format: "Syntax error in input script! [line=XXX]"
            else if (errorMsg.Contains("Syntax error") && errorMsg.Contains("line="))
            {
                int lineIndex = errorMsg.IndexOf("line=");
                int lineStart = lineIndex + "line=".Length;
                int lineEnd = errorMsg.IndexOf("]", lineStart);
                if (lineEnd > lineStart)
                {
                    if (int.TryParse(errorMsg.Substring(lineStart, lineEnd - lineStart), out int lineNum))
                    {
                        errorLine = lineNum;
                        // Find which source file this line belongs to
                        foreach (var stok in sourceTokens)
                        {
                            // LineStart and LineEnd are now 1-based
                            if (lineNum >= stok.LineStart && lineNum <= stok.LineEnd)
                            {
                                errorFile = stok.FilePath;
                                // Calculate line within original file (1-based)
                                errorLine = lineNum - stok.LineStart + 1;
                                break;
                            }
                        }
                    }
                }
            }
            // Try to extract file from error message if it contains a path
            else if (errorFile == null)
            {
                foreach (var stok in sourceTokens)
                {
                    if (errorMsg.Contains(stok.FilePath) || errorMsg.Contains(Path.GetFileName(stok.FilePath)))
                    {
                        errorFile = stok.FilePath;
                        break;
                    }
                }
            }

            return new CompilationError
            {
                Message = errorDescription,
                File = errorFile,
                Line = errorLine,
                Position = 0,
                CharacterPosition = 0,
                SourceTokens = sourceTokens,
                ScriptLocation = scriptLocation,
                Type = CompilationErrorType.CompilerError
            };
        }

        /// <summary>
        /// Formats and logs a compilation error with context
        /// </summary>
        public static void FormatAndLogError(CompilationError error, LogCallback log)
        {
            if (error == null || log == null)
                return;

            // Log error header based on type
            switch (error.Type)
            {
                case CompilationErrorType.PreprocessorSyntax:
                    log("\r\n=== PREPROCESSOR SYNTAX ERROR ===\r\n");
                    break;
                case CompilationErrorType.CompilerError:
                    log("\r\n=== COMPILATION ERROR ===\r\n");
                    break;
                case CompilationErrorType.CompilerNullResult:
                    log("\r\n=== COMPILER ERROR ===\r\n");
                    log("Compiler returned null result.\r\n\r\n");
                    return;
                default:
                    log("\r\n=== ERROR ===\r\n");
                    break;
            }

            // Log file and line information
            if (!string.IsNullOrEmpty(error.File))
            {
                log($"File: {error.File}\r\n");
                if (error.Line > 0)
                    log($"Line: {error.Line}\r\n");
            }
            else if (error.Line > 0)
            {
                // Try to find file by line number (LineStart/LineEnd are 1-based)
                string foundFile = null;
                int actualLine = error.Line;
                foreach (var stok in error.SourceTokens)
                {
                    if (error.Line >= stok.LineStart && error.Line <= stok.LineEnd)
                    {
                        foundFile = stok.FilePath;
                        actualLine = error.Line - stok.LineStart + 1;
                        log($"File: {foundFile}\r\n");
                        log($"Line: {actualLine}\r\n");
                        error.Line = actualLine;
                        error.File = foundFile;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(foundFile))
                {
                    log($"Could not map line {error.Line} to source file.\r\n");
                    log("Source token ranges:\r\n");
                    foreach (var stok in error.SourceTokens.Take(5))
                    {
                        log($"  {stok.FilePath}: lines {stok.LineStart}-{stok.LineEnd}\r\n");
                    }
                    if (error.SourceTokens.Count > 5)
                    {
                        log($"  ... and {error.SourceTokens.Count - 5} more files\r\n");
                        var lastToken = error.SourceTokens.Last();
                        log($"  Last: {lastToken.FilePath}: lines {lastToken.LineStart}-{lastToken.LineEnd}\r\n");
                    }
                }
            }

            // Log error message
            log($"Error: {error.Message}\r\n");

            // Show context around the error if we have file and line
            if (!string.IsNullOrEmpty(error.File) && error.Line > 0)
            {
                try
                {
                    string fullPath = Path.Combine(error.ScriptLocation, error.File.Replace("/", "\\"));
                    if (File.Exists(fullPath))
                    {
                        string[] lines = File.ReadAllLines(fullPath);
                        if (error.Line <= lines.Length)
                        {
                            int contextStart = Math.Max(0, error.Line - 5);
                            int contextEnd = Math.Min(lines.Length, error.Line + 3);

                            log($"\r\nContext around line {error.Line}:\r\n");
                            for (int i = contextStart; i < contextEnd; i++)
                            {
                                string marker = (i + 1 == error.Line) ? ">>> " : "    ";
                                log($"{marker}{i + 1,4}: {lines[i]}\r\n");
                            }

                            // Provide helpful suggestions based on error type
                            log("\r\n");
                            if (error.Message.Contains("defined more than once") || error.Message.Contains("duplicate"))
                            {
                                log("Duplicate definition detected:\r\n");
                                log("  - Check for duplicate function/variable names\r\n");
                                log("  - Remove duplicate #include statements\r\n");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    log($"\r\nNote: Could not read file for context: {ex.Message}\r\n");
                }
            }
            else if (error.Type == CompilationErrorType.PreprocessorSyntax)
            {
                // For preprocessor errors without file/line, show character position
                log($"Character Position: {error.CharacterPosition}\r\n");
                if (!string.IsNullOrEmpty(error.Source))
                {
                    log($"Total Source Length: {error.Source.Length}\r\n");
                }
                log("\r\n");
                log("Tip: Check all GSC files for unmatched preprocessor directives.\r\n");
                log("Each #ifdef or #ifndef must have a corresponding #endif.\r\n");
            }

            log("\r\n");
        }

        /// <summary>
        /// Formats a preprocessor error with file context
        /// </summary>
        public static void FormatPreprocessorError(CompilationError error, LogCallback log)
        {
            if (error == null || log == null)
                return;

            log("\r\n=== PREPROCESSOR SYNTAX ERROR ===\r\n");
            log($"Error: {error.Message}\r\n");

            if (!string.IsNullOrEmpty(error.File))
            {
                log($"File: scripts/{error.File}\r\n");
                log($"Line: {error.Line}, Position: {error.Position}\r\n");

                // Try to show the actual line content
                try
                {
                    string fullPath = Path.Combine(error.ScriptLocation, error.File.Replace("/", "\\"));
                    if (File.Exists(fullPath))
                    {
                        string[] lines = File.ReadAllLines(fullPath);
                        if (error.Line > 0 && error.Line <= lines.Length)
                        {
                            log("\r\nLine content:\r\n");
                            int contextStart = Math.Max(0, error.Line - 3);
                            int contextEnd = Math.Min(lines.Length, error.Line + 2);
                            for (int i = contextStart; i < contextEnd; i++)
                            {
                                string marker = (i + 1 == error.Line) ? ">>> " : "    ";
                                log($"{marker}{i + 1,4}: {lines[i]}\r\n");
                            }
                        }
                    }
                }
                catch { /* Ignore errors reading file for context */ }

                log("\r\n");
                log("Check this file for unmatched #ifdef/#ifndef/#else/#endif directives.\r\n");
            }
            else
            {
                log($"Character Position: {error.CharacterPosition}\r\n");
                if (!string.IsNullOrEmpty(error.Source))
                {
                    log($"Total Source Length: {error.Source.Length}\r\n");
                }
                log("\r\n");
                log("Tip: Check all GSC files for unmatched preprocessor directives.\r\n");
            }
            log("Each #ifdef or #ifndef must have a corresponding #endif.\r\n");
            log("\r\n");
        }
    }
}

