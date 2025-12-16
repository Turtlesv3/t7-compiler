using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using T7CompilerGUI.Models;
using T7CompilerGUI.Forms;

namespace T7CompilerGUI.Services
{
    /// <summary>
    /// Service for parsing gsc.conf configuration files
    /// </summary>
    public static class GscConfParser
    {
        /// <summary>
        /// Parses a gsc.conf file and returns structured settings
        /// </summary>
        /// <param name="gscConfPath">Path to the gsc.conf file</param>
        /// <returns>GscConfSettings object with parsed values, or null if file doesn't exist or parsing fails</returns>
        public static GscConfSettings Parse(string gscConfPath)
        {
            if (string.IsNullOrWhiteSpace(gscConfPath) || !File.Exists(gscConfPath))
                return null;

            var settings = new GscConfSettings();

            try
            {
                foreach (string line in File.ReadAllLines(gscConfPath))
                {
                    // Skip comments and empty lines
                    string trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                        continue;

                    // Split on '=' to get key and value
                    var split = trimmedLine.Split(new[] { '=' }, 2);
                    if (split.Length < 2)
                        continue;

                    string key = split[0].Trim().ToLower();
                    string value = split[1].Trim();

                    ParseSetting(settings, key, value);
                }

                return settings.HasSettings ? settings : null;
            }
            catch (Exception ex)
            {
                // Log error but don't throw - return null to indicate parsing failed
                System.Diagnostics.Debug.WriteLine($"Error parsing gsc.conf: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parses a single setting line
        /// </summary>
        private static void ParseSetting(GscConfSettings settings, string key, string value)
        {
            switch (key)
            {
                case "symbols":
                    // Split by comma and trim each token
                    foreach (string token in value.Split(','))
                    {
                        string trimmedToken = token.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmedToken) && 
                            !settings.Symbols.Contains(trimmedToken, StringComparer.OrdinalIgnoreCase))
                        {
                            settings.Symbols.Add(trimmedToken);
                        }
                    }
                    break;

                case "scriptlocation":
                    settings.ScriptLocation = value;
                    break;

                case "script":
                    // Normalize path separators
                    settings.Script = value.Replace("\\", "/");
                    break;

                case "file":
                    // Store as lowercase to match debug compiler behavior
                    settings.File = value.ToLower();
                    break;

                case "noruntime":
                    if (bool.TryParse(value, out bool noRuntime))
                    {
                        settings.NoRuntime = noRuntime;
                    }
                    else
                    {
                        // Also handle "true"/"false" strings (case-insensitive)
                        settings.NoRuntime = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                    }
                    break;

                case "hot":
                    if (Enum.TryParse<HotMode>(value, true, out HotMode hotMode))
                    {
                        settings.Hot = hotMode;
                    }
                    break;

                case "game":
                    if (Enum.TryParse<TreyarchCompiler.Enums.Games>(value, true, out TreyarchCompiler.Enums.Games game))
                    {
                        settings.Game = game;
                    }
                    break;
            }
        }

        /// <summary>
        /// Resolves the script location path (handles relative and absolute paths)
        /// </summary>
        /// <param name="settings">Parsed settings</param>
        /// <param name="projectFolder">Base project folder for resolving relative paths</param>
        /// <returns>Resolved script location path, or projectFolder if not specified</returns>
        public static string ResolveScriptLocation(GscConfSettings settings, string projectFolder)
        {
            if (settings == null || string.IsNullOrWhiteSpace(settings.ScriptLocation))
                return projectFolder;

            string scriptLocation = settings.ScriptLocation;

            // If absolute path, use as-is
            if (Path.IsPathRooted(scriptLocation))
                return scriptLocation;

            // Otherwise, combine with project folder
            return Path.Combine(projectFolder, scriptLocation);
        }

        /// <summary>
        /// Resolves the output file path (handles filename, relative, and absolute paths)
        /// </summary>
        /// <param name="settings">Parsed settings</param>
        /// <param name="projectFolder">Base project folder for resolving relative paths</param>
        /// <param name="buildFolder">Default build folder for filenames only</param>
        /// <returns>Resolved output file path, or null if not specified</returns>
        public static string ResolveOutputFile(GscConfSettings settings, string projectFolder, string buildFolder)
        {
            if (settings == null || string.IsNullOrWhiteSpace(settings.File))
                return null;

            string confOutputFile = settings.File;

            // If it's just a filename (no directory separators), use buildFolder
            if (!Path.IsPathRooted(confOutputFile) && 
                !confOutputFile.Contains("\\") && 
                !confOutputFile.Contains("/"))
            {
                return Path.Combine(buildFolder, confOutputFile);
            }

            // If relative path, combine with project folder or buildFolder
            if (!Path.IsPathRooted(confOutputFile))
            {
                string baseDir = !string.IsNullOrWhiteSpace(projectFolder) ? projectFolder : buildFolder;
                return Path.Combine(baseDir, confOutputFile);
            }

            // Absolute path - use as-is
            return confOutputFile;
        }
    }
}

