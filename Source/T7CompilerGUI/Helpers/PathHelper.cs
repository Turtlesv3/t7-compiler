using System;
using System.IO;

namespace T7CompilerGUI.Helpers
{
    /// <summary>
    /// Helper class for path manipulation utilities
    /// </summary>
    internal static class PathHelper
    {
        /// <summary>
        /// Shortens a path by replacing common user directories with environment variables
        /// </summary>
        /// <param name="path">The full path to shorten</param>
        /// <returns>The shortened path with environment variables, or the original path if shortening is not possible</returns>
        public static string ShortenPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            
            try
            {
                // Get common environment paths
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                
                // Replace with environment variables (case-insensitive)
                if (path.StartsWith(userProfile, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(userProfile))
                {
                    return "%USERPROFILE%" + path.Substring(userProfile.Length);
                }
                if (path.StartsWith(appData, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(appData))
                {
                    return "%APPDATA%" + path.Substring(appData.Length);
                }
                if (path.StartsWith(localAppData, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(localAppData))
                {
                    return "%LOCALAPPDATA%" + path.Substring(localAppData.Length);
                }
                if (path.StartsWith(documents, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(documents))
                {
                    return "%USERPROFILE%\\Documents" + path.Substring(documents.Length);
                }
                if (path.StartsWith(desktop, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(desktop))
                {
                    return "%USERPROFILE%\\Desktop" + path.Substring(desktop.Length);
                }
                if (path.StartsWith(programFiles, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(programFiles))
                {
                    return "%ProgramFiles%" + path.Substring(programFiles.Length);
                }
                if (path.StartsWith(programFilesX86, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(programFilesX86))
                {
                    return "%ProgramFiles(x86)%" + path.Substring(programFilesX86.Length);
                }
            }
            catch
            {
                // If anything fails, return original path
            }
            
            return path;
        }
        
        /// <summary>
        /// Expands environment variables in a path back to full path
        /// </summary>
        /// <param name="path">The path with environment variables</param>
        /// <returns>The expanded full path, or the original path if expansion fails</returns>
        public static string ExpandPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            
            try
            {
                return Environment.ExpandEnvironmentVariables(path);
            }
            catch
            {
                return path;
            }
        }
    }
}

