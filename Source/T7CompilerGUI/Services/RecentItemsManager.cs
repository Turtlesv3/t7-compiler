using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using T7CompilerGUI.Helpers;

namespace T7CompilerGUI.Services
{
    /// <summary>
    /// Service for managing recent projects and files
    /// </summary>
    public static class RecentItemsManager
    {
        private const int MAX_RECENT_PROJECTS = 10;
        private const int MAX_RECENT_FILES = 10;

        /// <summary>
        /// Adds a project path to the recent projects list
        /// </summary>
        public static void AddProject(List<string> recentProjects, string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath))
                return;

            string trimmedPath = projectPath.Trim();

            // Remove if already exists (case-insensitive comparison)
            recentProjects.RemoveAll(p => string.Equals(p.Trim(), trimmedPath, StringComparison.OrdinalIgnoreCase));

            // Add path to front
            recentProjects.Insert(0, trimmedPath);

            // Limit to MAX_RECENT_PROJECTS
            if (recentProjects.Count > MAX_RECENT_PROJECTS)
                recentProjects.RemoveAt(recentProjects.Count - 1);
        }

        /// <summary>
        /// Adds a file path to the recent files list (only .gsc and .gscc files)
        /// </summary>
        public static void AddFile(List<string> recentFiles, string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return;

            // Only add .gsc and .gscc files
            string ext = Path.GetExtension(filePath).ToLower();
            if (ext != ".gsc" && ext != ".gscc")
                return;

            // Remove if already exists
            recentFiles.Remove(filePath);

            // Add to front
            recentFiles.Insert(0, filePath);

            // Limit to MAX_RECENT_FILES
            if (recentFiles.Count > MAX_RECENT_FILES)
                recentFiles.RemoveAt(recentFiles.Count - 1);
        }

        /// <summary>
        /// Filters recent projects to only include existing directories
        /// </summary>
        public static List<string> FilterValidProjects(List<string> recentProjects)
        {
            return recentProjects
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Where(p =>
                {
                    try
                    {
                        string expanded = PathHelper.ExpandPath(p.Trim());
                        return Directory.Exists(expanded) || Directory.Exists(p.Trim());
                    }
                    catch
                    {
                        return false;
                    }
                })
                .ToList();
        }

        /// <summary>
        /// Filters recent files to only include existing files
        /// </summary>
        public static List<string> FilterValidFiles(List<string> recentFiles)
        {
            return recentFiles.Where(f => File.Exists(f)).ToList();
        }

        /// <summary>
        /// Removes a project from the recent projects list
        /// </summary>
        public static void RemoveProject(List<string> recentProjects, string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath))
                return;

            recentProjects.RemoveAll(p => string.Equals(p.Trim(), projectPath.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Removes a file from the recent files list
        /// </summary>
        public static void RemoveFile(List<string> recentFiles, string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            recentFiles.Remove(filePath);
        }

        /// <summary>
        /// Gets the display name for a project path (folder name)
        /// </summary>
        public static string GetProjectDisplayName(string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath))
                return "";

            string displayName = Path.GetFileName(projectPath);
            if (string.IsNullOrEmpty(displayName))
                displayName = projectPath;

            return displayName;
        }

        /// <summary>
        /// Gets the display name for a file path (file name)
        /// </summary>
        public static string GetFileDisplayName(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return "";

            return Path.GetFileName(filePath);
        }
    }
}

