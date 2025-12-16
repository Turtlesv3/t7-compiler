using System;
using System.IO;
using System.Linq;
using System.Text;
using T7CompilerGUI.Models;
using TreyarchCompiler.Enums;

namespace T7CompilerGUI.Services
{
    /// <summary>
    /// Service for handling injection validation and file operations
    /// </summary>
    public static class InjectionService
    {
        /// <summary>
        /// Callback for logging injection progress
        /// </summary>
        public delegate void LogCallback(string message);

        /// <summary>
        /// Validates injection input parameters
        /// </summary>
        public static (bool isValid, string errorMessage) ValidateInput(InjectionInput input)
        {
            if (input == null)
                return (false, "Injection input is null.");

            if (string.IsNullOrWhiteSpace(input.InjectFile))
                return (false, "Please select a valid compiled file to inject.");

            if (!File.Exists(input.InjectFile))
                return (false, "The selected file does not exist.");

            if (string.IsNullOrWhiteSpace(input.ReplacePath))
                return (false, "Please specify a replace path.");

            return (true, null);
        }

        /// <summary>
        /// Validates that a compiled GSC file is valid
        /// </summary>
        public static (bool isValid, string errorMessage, byte[] buffer) ValidateAndReadFile(string filePath, LogCallback log = null)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return (false, "File does not exist.", null);
            }

            try
            {
                byte[] buffer = File.ReadAllBytes(filePath);

                // Validate file is a compiled GSC script
                if (buffer.Length < 16)
                {
                    return (false, "File is too small to be a valid compiled script.", null);
                }

                // Check for GSC header or GSIC preamble
                long header = BitConverter.ToInt64(buffer, 0);
                const long T7_HEADER = 0x1C000A0D43534780;
                const long T8_HEADER = 0x36000A0D43534780;

                if (header != T7_HEADER && header != T8_HEADER)
                {
                    string preamble = Encoding.ASCII.GetString(buffer.Take(4).ToArray());
                    if (preamble == "GSIC")
                    {
                        log?.Invoke("Detected GSIC file (with detours)\r\n");
                        // GSIC files can be injected but may require special handling
                        return (true, null, buffer);
                    }
                    else
                    {
                        return (false, "File is not a valid compiled GSC script. Expected GSC header or GSIC preamble.", null);
                    }
                }

                return (true, null, buffer);
            }
            catch (Exception ex)
            {
                return (false, $"Error reading file: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Formats injection result for logging
        /// </summary>
        public static void FormatInjectionResult(InjectionResult result, LogCallback log)
        {
            if (result == null || log == null)
                return;

            if (result.Success)
            {
                log("\r\n=== INJECTION SUCCESS ===\r\n");
                log("Script injected successfully!\r\n");
                log("Injection state saved. You can reset the parse tree even after closing the app.\r\n\r\n");
            }
            else
            {
                log("\r\n=== INJECTION FAILED ===\r\n");
                if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                {
                    log($"{result.ErrorMessage}\r\n");
                }
                else
                {
                    log($"Failed to inject script. Error code: 0x{result.ErrorCode:X}\r\n");
                }
                log("\r\n");
            }
        }

        /// <summary>
        /// Formats injection error for logging
        /// </summary>
        public static void FormatInjectionError(string errorMessage, LogCallback log)
        {
            if (log == null)
                return;

            log("\r\n=== INJECTION ERROR ===\r\n");
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                log($"{errorMessage}\r\n");
            }
            log("\r\n");
        }

        /// <summary>
        /// Formats injection start information for logging
        /// </summary>
        public static void LogInjectionStart(InjectionInput input, LogCallback log)
        {
            if (input == null || log == null)
                return;

            log("\r\n=== INJECTION ===\r\n");
            log($"File: {input.InjectFile}\r\n");
            log($"Replace Path: {input.ReplacePath}\r\n");
            log($"Game: {input.Game}\r\n");
            log($"No Runtime: {input.NoRuntime}\r\n");
            log($"Hot Reload: {input.HotMode}\r\n\r\n");
        }
    }
}

