using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using T7CompilerGUI.Models;
using T7CompilerGUI.Forms.Dialogs;

namespace T7CompilerGUI.Services
{
    /// <summary>
    /// Service for managing application settings persistence (file I/O and serialization)
    /// UI update logic remains in MainForm
    /// </summary>
    public static class SettingsManager
    {
        private const string CONFIG_FILE_NAME = "T7CompilerGUI.conf";

        /// <summary>
        /// Gets the path to the config file (primary location or AppData fallback)
        /// </summary>
        public static string GetConfigPath()
        {
            string startupPath = System.Windows.Forms.Application.StartupPath;

            // Check if startup path is writable, if not use AppData
            try
            {
                string testFile = Path.Combine(startupPath, ".writable_test");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return Path.Combine(startupPath, CONFIG_FILE_NAME);
            }
            catch
            {
                // Startup path is not writable, use AppData
                string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "T7CompilerGUI");
                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath);
                }
                return Path.Combine(appDataPath, CONFIG_FILE_NAME);
            }
        }

        /// <summary>
        /// Saves a config document to the specified path
        /// </summary>
        public static bool SaveConfigToPath(XDocument config, string configPath, Action<string> logCallback = null)
        {
            try
            {
                // Ensure the directory exists
                string configDir = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(configDir) && !Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }

                // Save with proper formatting (indented, human-readable)
                XmlWriterSettings settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
                    NewLineChars = "\r\n",
                    NewLineHandling = NewLineHandling.Replace,
                    OmitXmlDeclaration = false,
                    Encoding = Encoding.UTF8
                };

                using (XmlWriter writer = XmlWriter.Create(configPath, settings))
                {
                    config.Save(writer);
                }

                // Verify the file was actually created
                if (File.Exists(configPath))
                {
                    return true;
                }
                else
                {
                    logCallback?.Invoke($"Warning: Config file was not created at: {configPath}\r\n");
                    return false;
                }
            }
            catch (Exception ex)
            {
                logCallback?.Invoke($"Error saving config to {configPath}: {ex.Message}\r\n");
                return false;
            }
        }

        /// <summary>
        /// Saves the config document, trying primary location first, then AppData fallback
        /// </summary>
        public static bool SaveConfig(XDocument config, Action<string> logCallback = null)
        {
            // Try to save to the primary location
            string configPath = GetConfigPath();
            if (SaveConfigToPath(config, configPath, logCallback))
            {
                return true; // Success!
            }

            // If primary location failed, try AppData fallback
            logCallback?.Invoke($"Failed to save to startup path. Trying AppData location...\r\n");

            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "T7CompilerGUI");
            string fallbackConfigPath = Path.Combine(appDataPath, CONFIG_FILE_NAME);

            if (SaveConfigToPath(config, fallbackConfigPath, logCallback))
            {
                logCallback?.Invoke($"Config saved to AppData location: {fallbackConfigPath}\r\n");
                return true;
            }
            else
            {
                logCallback?.Invoke($"Error: Failed to save config to both primary and fallback locations.\r\n");
                return false;
            }
        }

        /// <summary>
        /// Loads the config document from file, or returns null if not found
        /// </summary>
        public static XDocument LoadConfig(Action<string> logCallback = null)
        {
            string configPath = GetConfigPath();

            // If config doesn't exist at primary location, check AppData fallback
            if (!File.Exists(configPath))
            {
                string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "T7CompilerGUI");
                string fallbackConfigPath = Path.Combine(appDataPath, CONFIG_FILE_NAME);

                if (File.Exists(fallbackConfigPath))
                {
                    configPath = fallbackConfigPath;
                    logCallback?.Invoke($"Loading config from AppData location: {configPath}\r\n");
                }
                else
                {
                    return null; // No config file found
                }
            }

            // Load the config file
            try
            {
                return XDocument.Load(configPath);
            }
            catch (Exception ex)
            {
                logCallback?.Invoke($"Error loading config file: {ex.Message}. Using default settings.\r\n");
                return null;
            }
        }

        /// <summary>
        /// Migrates old config files to the unified config format
        /// </summary>
        public static XDocument MigrateOldConfigFiles(string buildFolder, Action<string> logCallback = null)
        {
            try
            {
                bool hasData = false;
                XDocument config = new XDocument(new XElement("T7CompilerConfig"));
                XElement root = config.Element("T7CompilerConfig");

                string startupPath = System.Windows.Forms.Application.StartupPath;

                // Migrate t7compiler_settings.txt
                string oldSettingsPath = Path.Combine(startupPath, "t7compiler_settings.txt");
                if (File.Exists(oldSettingsPath))
                {
                    hasData = true;
                    using (var reader = new StreamReader(oldSettingsPath))
                    {
                        root.Add(new XElement("Application",
                            new XElement("Theme", reader.ReadLine() ?? "Dark"),
                            new XElement("ColorStyle", reader.ReadLine() ?? "Red"),
                            new XElement("DefaultOutputPath", reader.ReadLine() ?? ""),
                            new XElement("AutoSaveSettings", reader.ReadLine() ?? "0"),
                            new XElement("RestoreWindowState", reader.ReadLine() ?? "0")
                        ));
                    }
                }

                // Migrate t7compiler_selections.txt
                string oldSelectionsPath = Path.Combine(startupPath, "t7compiler_selections.txt");
                if (File.Exists(oldSelectionsPath))
                {
                    hasData = true;
                    using (var reader = new StreamReader(oldSelectionsPath))
                    {
                        root.Add(new XElement("Selections",
                            new XElement("PlatformIndex", reader.ReadLine() ?? "0"),
                            new XElement("GameIndex", reader.ReadLine() ?? "0"),
                            new XElement("GscConfModeIndex", reader.ReadLine() ?? "0"),
                            new XElement("InjectGameIndex", reader.ReadLine() ?? "0")
                        ));
                    }
                }

                // Migrate window_state.txt
                string oldWindowStatePath = Path.Combine(startupPath, "window_state.txt");
                if (File.Exists(oldWindowStatePath))
                {
                    hasData = true;
                    using (var reader = new StreamReader(oldWindowStatePath))
                    {
                        string state = reader.ReadLine() ?? "Normal";
                        int x = int.TryParse(reader.ReadLine(), out int xVal) ? xVal : 100;
                        int y = int.TryParse(reader.ReadLine(), out int yVal) ? yVal : 100;
                        int width = int.TryParse(reader.ReadLine(), out int wVal) ? wVal : 800;
                        int height = int.TryParse(reader.ReadLine(), out int hVal) ? hVal : 600;
                        string tabIdx = reader.ReadLine() ?? "0";

                        root.Add(new XElement("WindowState",
                            new XElement("State", state),
                            new XElement("Location", new XElement("X", x), new XElement("Y", y)),
                            new XElement("Size", new XElement("Width", width), new XElement("Height", height)),
                            new XElement("SelectedTab", tabIdx)
                        ));
                    }
                }

                // Migrate recent_projects.txt
                string oldRecentPath = Path.Combine(startupPath, "recent_projects.txt");
                if (File.Exists(oldRecentPath))
                {
                    hasData = true;
                    var projects = File.ReadAllLines(oldRecentPath)
                        .Where(p => Directory.Exists(p))
                        .Take(10); // MAX_RECENT_PROJECTS
                    root.Add(new XElement("RecentProjects",
                        projects.Select(p => new XElement("Project", p))
                    ));
                }

                // Migrate injection state (from config folder or old location)
                string oldInjectionPath = Path.Combine(buildFolder, "config", "t7compiler_injection_state.txt");
                if (!File.Exists(oldInjectionPath))
                {
                    oldInjectionPath = Path.Combine(startupPath, "t7compiler_injection_state.txt");
                }
                if (File.Exists(oldInjectionPath))
                {
                    hasData = true;
                    using (var reader = new StreamReader(oldInjectionPath))
                    {
                        root.Add(new XElement("Injection",
                            new XElement("ReplacePath", reader.ReadLine() ?? ""),
                            new XElement("Game", reader.ReadLine() ?? "T7"),
                            new XElement("OriginalPID", reader.ReadLine() ?? "0"),
                            new XElement("ModifiedSPTStruct", reader.ReadLine() ?? "0"),
                            new XElement("OriginalBuffer", reader.ReadLine() ?? "0"),
                            new XElement("InjectedBuffSize", reader.ReadLine() ?? "0"),
                            new XElement("ScriptName", reader.ReadLine() ?? "0"),
                            new XElement("ScriptBuffSize", reader.ReadLine() ?? "0"),
                            new XElement("ScriptBuffer", reader.ReadLine() ?? "0")
                        ));
                    }
                }

                if (hasData)
                {
                    // Save migrated config
                    string configPath = GetConfigPath();
                    SaveConfigToPath(config, configPath, logCallback);
                    return config;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}

