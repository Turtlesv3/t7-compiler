using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DebugCompiler.UI.Core.Helpers
{
    public static class AppSettings
    {
        private static string _lastScriptDirectory;
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DebugCompiler");

        private static readonly string SettingsFilePath = Path.Combine(AppDataFolder, "settings.ini");
        private static readonly string MenusFilePath = Path.Combine(AppDataFolder, "saved_menus.ini");

        static AppSettings()
        {
            // Ensure directory exists
            if (!Directory.Exists(AppDataFolder))
            {
                Directory.CreateDirectory(AppDataFolder);
            }
            LoadSettings();
        }

        public static string LastScriptDirectory
        {
            get => _lastScriptDirectory;
            set
            {
                if (!string.IsNullOrEmpty(value) && Directory.Exists(value))
                {
                    _lastScriptDirectory = value;
                    SaveSettings();
                }
            }
        }

        public static List<string> SavedMenus
        {
            get
            {
                try
                {
                    if (File.Exists(MenusFilePath))
                    {
                        return File.ReadAllLines(MenusFilePath)
                                   .Where(line => !string.IsNullOrWhiteSpace(line))
                                   .ToList();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading saved menus: {ex.Message}");
                }
                return new List<string>();
            }
        }

        public static void AddSuccessfullyProcessedFile(string filePath)
        {
            try
            {
                var processedFiles = SavedMenus;
                if (!processedFiles.Contains(filePath))
                {
                    processedFiles.Insert(0, filePath);
                    // Keep only the last 20 successful files
                    File.WriteAllLines(MenusFilePath, processedFiles.Take(20));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving processed file: {ex.Message}");
            }
        }

        public static List<string> GetSuccessfullyProcessedFiles()
        {
            return SavedMenus.Where(f => File.Exists(f)).ToList();
        }

        public static void AddSavedMenu(string menuPath)
        {
            try
            {
                var menus = SavedMenus;
                if (!menus.Contains(menuPath))
                {
                    menus.Insert(0, menuPath);
                    File.WriteAllLines(MenusFilePath, menus.Take(10)); // Keep last 10 menus
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving menu: {ex.Message}");
            }
        }

        private static void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string savedPath = File.ReadAllText(SettingsFilePath);
                    if (Directory.Exists(savedPath))
                    {
                        _lastScriptDirectory = savedPath;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading settings: {ex.Message}");
            }
        }

        private static void SaveSettings()
        {
            try
            {
                File.WriteAllText(SettingsFilePath, _lastScriptDirectory ?? string.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
    }
}