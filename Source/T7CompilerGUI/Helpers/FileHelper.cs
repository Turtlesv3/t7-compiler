using System.IO;

namespace T7CompilerGUI.Helpers
{
    internal static class FileHelper
    {
        public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive = true)
        {
            // Get information about the source directory
            var dir = new DirectoryInfo(sourceDir);

            if (!dir.Exists)
                return;

            DirectoryInfo[] dirs = dir.GetDirectories();

            Directory.CreateDirectory(destinationDir);

            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }

        public static long DirSize(string path)
        {
            long size = 0;
            
            if (!Directory.Exists(path))
                return 0;

            DirectoryInfo INFO = new DirectoryInfo(path);
            
            // Add file sizes
            FileInfo[] fis = INFO.GetFiles();
            foreach (FileInfo fi in fis)
            {
                size += fi.Length;
            }
            
            // Add subdirectory sizes
            DirectoryInfo[] dis = INFO.GetDirectories();
            foreach (DirectoryInfo di in dis)
            {
                size += DirSize(di.FullName);
            }
            
            return size;
        }
    }
}

