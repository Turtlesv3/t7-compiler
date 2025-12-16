using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using T7CompilerGUI.Models;
using T7CompilerGUI.Utils;
using TreyarchCompiler;
using TreyarchCompiler.Enums;
using TreyarchCompiler.Utilities;

namespace T7CompilerGUI.Services
{
    /// <summary>
    /// Service for handling GSC compilation workflow
    /// </summary>
    public static class CompilationService
    {
        /// <summary>
        /// Callback for logging compilation progress
        /// </summary>
        public delegate void LogCallback(string message);

        /// <summary>
        /// Collects all GSC files from the script location
        /// </summary>
        public static List<string> CollectGscFiles(string scriptLocation, LogCallback log = null)
        {
            if (string.IsNullOrWhiteSpace(scriptLocation) || !Directory.Exists(scriptLocation))
            {
                log?.Invoke($"ERROR: Script location does not exist: {scriptLocation}\r\n");
                return new List<string>();
            }

            // Match debug compiler: get all .gsc files without filtering
            // The debug compiler doesn't filter files - it includes all .gsc files
            // Conditional compilation (#ifdef MP, #ifdef ZM) handles mode-specific code
            var gscFiles = Directory.GetFiles(scriptLocation, "*.gsc", SearchOption.AllDirectories)
                .Where(f => !f.EndsWith(".gscc", StringComparison.OrdinalIgnoreCase) && 
                            !f.EndsWith(".stub.gscc", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Don't log symbols here - MainForm will log them with the actual symbols being used
            return gscFiles;
        }

        /// <summary>
        /// Processes source files and creates source tokens
        /// </summary>
        public static (string source, List<SourceTokenDef> sourceTokens) ProcessSourceFiles(
            List<string> gscFiles, 
            string scriptLocation, 
            LogCallback log = null)
        {
            if (gscFiles == null || gscFiles.Count == 0)
            {
                return (string.Empty, new List<SourceTokenDef>());
            }

            StringBuilder sb = new StringBuilder();
            List<SourceTokenDef> sourceTokens = new List<SourceTokenDef>();
            int currentLineCount = 0;
            int currentCharCount = 0;

            foreach (string file in gscFiles)
            {
                var token = new SourceTokenDef
                {
                    FilePath = file.Replace(scriptLocation, "").Substring(1).Replace("\\", "/"),
                    LineStart = currentLineCount,
                    CharStart = currentCharCount,
                    LineMappings = new Dictionary<int, (int CStart, int CEnd)>()
                };

                foreach (var line in File.ReadAllLines(file))
                {
                    token.LineMappings[currentLineCount] = (currentCharCount, currentCharCount + line.Length + 1);
                    sb.Append(line);
                    sb.Append("\n");
                    currentLineCount += 1;
                    currentCharCount += line.Length + 1; // + \n
                }

                token.LineEnd = currentLineCount;
                token.CharEnd = currentCharCount;
                sourceTokens.Add(token);
                
                sb.Append("\n");
                currentLineCount += 1;
                currentCharCount += 1;
            }

            string source = sb.ToString();
            log?.Invoke("Processing conditionals...\r\n");

            return (source, sourceTokens);
        }

        /// <summary>
        /// Processes conditional compilation directives
        /// </summary>
        public static (string processedSource, CBSyntaxException error) ProcessConditionals(
            string source, 
            List<string> symbols, 
            bool isT7,
            LogCallback log = null)
        {
            if (string.IsNullOrEmpty(source))
            {
                return (source, null);
            }

            // Process conditional compilation (matching debug compiler order)
            var ppc = new ConditionalBlocks();
            
            // Add BO3/BO4 symbol AFTER creating ConditionalBlocks but BEFORE LoadConditionalTokens (matching debug compiler)
            // Only add if not already present (CodeEditorForm may have already included it)
            string boSymbol = isT7 ? "BO3" : "BO4";
            if (!symbols.Contains(boSymbol, StringComparer.OrdinalIgnoreCase))
            {
                symbols.Add(boSymbol);
            }
            
            ppc.LoadConditionalTokens(symbols);

            try
            {
                string processedSource = ppc.ParseSource(source);
                return (processedSource, null);
            }
            catch (CBSyntaxException error)
            {
                return (null, error);
            }
        }

        /// <summary>
        /// Invokes the compiler with the given parameters
        /// </summary>
        public static CompiledCode InvokeCompiler(
            TreyarchCompiler.Enums.Platforms platform,
            TreyarchCompiler.Enums.Games game,
            TreyarchCompiler.Enums.Modes mode,
            bool useMasking,
            string source,
            LogCallback log = null)
        {
            if (string.IsNullOrEmpty(source))
            {
                log?.Invoke("ERROR: Source code is empty.\r\n");
                return null;
            }

            log?.Invoke($"Compiling with Platform: {platform}, Game: {game}, Mode: {mode}, Masking: {useMasking}\r\n");

            try
            {
                CompiledCode code = Compiler.Compile(platform, game, mode, useMasking, source);
                return code;
            }
            catch (Exception ex)
            {
                log?.Invoke($"Compiler exception: {ex.Message}\r\n");
                return null;
            }
        }

        /// <summary>
        /// Ensures the output directory exists, creating it if necessary with fallback handling
        /// </summary>
        public static string EnsureOutputDirectory(string outputDirectory, string buildFolder, LogCallback log = null)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory))
                outputDirectory = buildFolder;

            try
            {
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }
                return outputDirectory;
            }
            catch (UnauthorizedAccessException)
            {
                // If we can't write to the selected directory, fall back to a safe location
                log?.Invoke($"Warning: Cannot write to '{outputDirectory}'. Using fallback location.\r\n");
                string fallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "T7Compiler", "build");
                try
                {
                    if (!Directory.Exists(fallback))
                    {
                        Directory.CreateDirectory(fallback);
                    }
                    return fallback;
                }
                catch
                {
                    // Last resort: use temp folder
                    fallback = Path.Combine(Path.GetTempPath(), "T7Compiler", "build");
                    if (!Directory.Exists(fallback))
                    {
                        Directory.CreateDirectory(fallback);
                    }
                    return fallback;
                }
            }
            catch (Exception ex)
            {
                // For any other exception, try fallback locations
                log?.Invoke($"Warning: Error creating directory '{outputDirectory}': {ex.Message}. Using fallback location.\r\n");
                string fallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "T7Compiler", "build");
                try
                {
                    if (!Directory.Exists(fallback))
                    {
                        Directory.CreateDirectory(fallback);
                    }
                    return fallback;
                }
                catch
                {
                    // Last resort: use temp folder
                    fallback = Path.Combine(Path.GetTempPath(), "T7Compiler", "build");
                    if (!Directory.Exists(fallback))
                    {
                        Directory.CreateDirectory(fallback);
                    }
                    return fallback;
                }
            }
        }

        /// <summary>
        /// Saves the compiled output and related files
        /// </summary>
        public static void SaveCompilationOutput(
            CompiledCode code,
            string outputDirectory,
            string baseFileName,
            string scriptLocation,
            string projectFolder,
            bool saveOpcodeMap,
            LogCallback log = null)
        {
            if (code == null || code.CompiledScript == null)
            {
                log?.Invoke("ERROR: No compiled code to save.\r\n");
                return;
            }

            // Get output filename - use .gscc extension (or .gsc if RequiresGSI)
            if (string.IsNullOrWhiteSpace(baseFileName))
                baseFileName = "compiled";
            
            // Remove extension and add appropriate one (.gsc or .gscc)
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(baseFileName);
            string extension = code.RequiresGSI ? ".gsc" : ".gscc";
            string finalFileName = fileNameWithoutExt + extension;
            
            string outputPath = Path.Combine(outputDirectory, finalFileName);
            
            // Write compiled script
            File.WriteAllBytes(outputPath, code.CompiledScript);
            log?.Invoke($"✓ Compilation successful!\r\n");
            log?.Invoke($"  Output: {Path.GetFileName(outputPath)}\r\n");
            log?.Invoke($"  Size: {code.CompiledScript.Length:N0} bytes\r\n");
            if (code.HashMap != null)
                log?.Invoke($"  Functions: {code.HashMap.Count}\r\n");

            // Save hash table to hashes.txt in output directory
            if (code.HashMap != null && code.HashMap.Count > 0)
            {
                string hashPath = Path.Combine(outputDirectory, "hashes.txt");
                
                StringBuilder hashes = new StringBuilder();
                hashes.AppendLine("# Hash Table Generated by T7 Compiler GUI");
                hashes.AppendLine($"# Compilation Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                // Expand paths to hide %USERPROFILE% in output
                string expandedSourcePath = Helpers.PathHelper.ExpandPath(projectFolder);
                string expandedOutputPath = Helpers.PathHelper.ExpandPath(outputPath);
                hashes.AppendLine($"# Source: {expandedSourcePath}");
                hashes.AppendLine($"# Output: {expandedOutputPath}");
                hashes.AppendLine();
                hashes.AppendLine($"Total Functions: {code.HashMap.Count}");
                hashes.AppendLine();
                
                // Sort by hash value for easier reading
                var sortedHashes = code.HashMap.OrderBy(kvp => kvp.Key);
                foreach (var kvp in sortedHashes)
                {
                    hashes.AppendLine($"0x{kvp.Key:X8}, {kvp.Value}");
                }
                
                File.WriteAllText(hashPath, hashes.ToString());
            }

            // Save opcode map if enabled and available
            if (saveOpcodeMap)
            {
                string omapBaseName = Path.GetFileNameWithoutExtension(finalFileName);
                string omapPath = Path.Combine(outputDirectory, omapBaseName + ".omap");
                
                byte[] opsRaw = null;
                
                // Prefer OpcodeMap (from masking) over OpcodeEmissions
                if (code.OpcodeMap != null && code.OpcodeMap.Length > 0)
                {
                    opsRaw = code.OpcodeMap;
                }
                else if (code.OpcodeEmissions != null && code.OpcodeEmissions.Count > 0)
                {
                    opsRaw = new byte[code.OpcodeEmissions.Count * 4];
                    for (int i = 0; i < code.OpcodeEmissions.Count; i++)
                    {
                        BitConverter.GetBytes(code.OpcodeEmissions[i]).CopyTo(opsRaw, i * 4);
                    }
                }
                
                if (opsRaw != null && opsRaw.Length > 0)
                {
                    File.WriteAllBytes(omapPath, opsRaw);
                }
            }

            // Save stub script if present
            if (code.StubbedScript != null && code.StubScriptData != null)
            {
                string stubBaseName = Path.GetFileNameWithoutExtension(finalFileName);
                string stubPath = Path.Combine(outputDirectory, stubBaseName + ".stub.gscc");
                
                log?.Invoke("Saving stub script...\r\n");
                File.WriteAllBytes(stubPath, code.StubScriptData);
                log?.Invoke($"Stub script saved: {stubPath}\r\n");
            }
        }
    }
}

