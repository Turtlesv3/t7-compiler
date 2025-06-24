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
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DebugCompiler");

        private static readonly string SettingsFilePath = Path.Combine(AppDataFolder, "settings.ini");
        private static readonly string ProcessedFilesPath = Path.Combine(AppDataFolder, "processed_files.ini");

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

        public static void AddSuccessfullyProcessedFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.WriteLine($"File doesn't exist, not tracking: {filePath}");
                    return;
                }

                var processedFiles = new List<string>();
                if (File.Exists(ProcessedFilesPath))
                {
                    processedFiles = File.ReadAllLines(ProcessedFilesPath)
                        .Where(line => !string.IsNullOrWhiteSpace(line))
                        .ToList();
                }

                // Remove existing entries for this file
                processedFiles.RemoveAll(x => x.StartsWith(filePath + "|"));

                // Add new entry with timestamp
                string entry = $"{filePath}|{DateTime.Now:yyyy-MM-dd HH:mm:ss}|{new FileInfo(filePath).Length}";
                processedFiles.Insert(0, entry);

                // Keep only the last 20 entries
                File.WriteAllLines(ProcessedFilesPath, processedFiles.Take(20));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving processed file: {ex.Message}");
            }
        }

        public static List<ProcessedFileInfo> GetSuccessfullyProcessedFiles()
        {
            try
            {
                if (File.Exists(ProcessedFilesPath))
                {
                    return File.ReadAllLines(ProcessedFilesPath)
                        .Select(line =>
                        {
                            var parts = line.Split('|');
                            return new ProcessedFileInfo
                            {
                                FilePath = parts[0],
                                Timestamp = DateTime.Parse(parts[1]),
                                FileSize = long.Parse(parts[2])
                            };
                        })
                        .Where(x => File.Exists(x.FilePath))
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading processed files: {ex.Message}");
            }
            return new List<ProcessedFileInfo>();
        }

        public static void ClearProcessedFilesHistory()
        {
            try
            {
                if (File.Exists(ProcessedFilesPath))
                {
                    File.Delete(ProcessedFilesPath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error clearing processed files: {ex.Message}");
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

        public class ProcessedFileInfo
        {
            public string FilePath { get; set; }
            public DateTime Timestamp { get; set; }
            public long FileSize { get; set; }

            public string DisplayName => Path.GetFileName(FilePath);
            public string DisplaySize => FormatFileSize(FileSize);
            public string DisplayDate => Timestamp.ToString("yyyy-MM-dd HH:mm");

            private static string FormatFileSize(long bytes)
            {
                string[] sizes = { "B", "KB", "MB", "GB" };
                int order = 0;
                double len = bytes;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len /= 1024;
                }
                return $"{len:0.##} {sizes[order]}";
            }
        }
    }
}