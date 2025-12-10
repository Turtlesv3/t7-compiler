using SMC.UI.Core.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace t7c_installer
{
    static class Program
    {
#if DEBUG
        private const bool NoErrorHandling = false;
#endif
        // GitHub releases URL: https://github.com/Turtlesv3/t7-compiler/releases/tag/GG
        // Direct download format for GitHub releases: /releases/download/tag/filename
        private static string PackageURL = "https://github.com/Turtlesv3/t7-compiler/releases/download/GG/update.zip";
        internal static bool IsUpdating = false;
        private const string InstallRoot = @"C:\";
        private static string UpdateTempFilename => Path.Combine(Path.GetTempPath(), "t7c_update.zip");
        private static string UpdateTempDirname => Path.Combine(Path.GetTempPath(), "t7c_temp");
        
        /// <summary>
        /// Gets the local update.zip path (next to the installer executable)
        /// </summary>
        private static string GetLocalUpdateZipPath()
        {
            string installerLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            return Path.Combine(installerLocation, "update.zip");
        }
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (!Debugger.IsAttached
#if DEBUG
                && !NoErrorHandling
#endif
                )

            {
                Application.ThreadException += new ThreadExceptionEventHandler(HandleUIThreadExceptions);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(HandleCurrentDomainExceptions);
            }

            try
            {
                if (args.Length > 0)
                {
                    switch (args[0].Trim().ToLower())
                    {
                        case "--install_silent":
                            InstallUpdate();
                            CErrorDialog.Show("Compiler Updated!", $"Your t7 compiler installation was just updated. You may proceed with your compilation action.", true);
                            return;

                        case "--deploy":
                            string compilerDirectory = args[1];
                            string defaultProjectDirectory = args[2];
                            string solutionDirectory = args[3];
                            DeployCompiler(compilerDirectory, defaultProjectDirectory, solutionDirectory);
                            return;
                    }
                }
            }
            catch(Exception e)
            {
                if(IsUpdating)
                {
                    CErrorDialog.Show("Error Updating!", $"Failed to install: {e}", true);
                }
                else
                {
                    Console.WriteLine(e.ToString());
                }
                Environment.Exit(1);
            }
            Application.Run(new MainForm());
        }

        static void DeployCompiler(string compilerDirectory, string defaultProjectDirectory, string solutionDirectory)
        {
            string installer = Assembly.GetEntryAssembly().Location;

            // clear __depot
            string depot = Path.Combine(solutionDirectory, "__depot");
            string build = Path.Combine(depot, "build");
            
            if (Directory.Exists(depot)) Directory.Delete(depot, true);

            // create directories for build
            Directory.CreateDirectory(depot);
            Directory.CreateDirectory(build);

            // pack compiler into __depot/build/t7compiler (already created by DebugCompiler build)
            string compilerSource = Path.Combine(solutionDirectory, "__depot", "build", "t7compiler");
            string compilerTarget = Path.Combine(build, "t7compiler");
            if (Directory.Exists(compilerSource))
            {
                DirectoryCopy(compilerSource, compilerTarget, true);
            }
            else
            {
                // Fallback: copy from compiler directory if depot folder doesn't exist
                Directory.CreateDirectory(compilerTarget);
                foreach(var file in Directory.GetFiles(compilerDirectory))
                {
                    File.Copy(file, Path.Combine(compilerTarget, Path.GetFileName(file)), true);
                }
            }

            // copy this utility to the output folder for reuse later
            File.Copy(installer, Path.Combine(compilerTarget, Path.GetFileName(installer)));

            // pack default project into __depot/build/defaultproject
            string dprojTarget = Path.Combine(build, "defaultproject");
            Directory.CreateDirectory(dprojTarget);
            DirectoryCopy(defaultProjectDirectory, dprojTarget, true);

            // pack GUI into __depot/build/t7gui (already created by T7CompilerGUI build)
            string guiSource = Path.Combine(solutionDirectory, "__depot", "build", "t7gui");
            string guiTarget = Path.Combine(build, "t7gui");
            if (Directory.Exists(guiSource))
            {
                DirectoryCopy(guiSource, guiTarget, true);
            }

            // pack vsix into __depot/build/
            var files = Directory.GetFiles(solutionDirectory, "*.vsix");
            if(files.Length > 0)
            {
                File.Copy(files[files.Length - 1], Path.Combine(build, Path.GetFileName(files[files.Length - 1])));
            }

            ZipFile.CreateFromDirectory(build, Path.Combine(depot, "update.zip"));
            File.Copy(installer, Path.Combine(depot, Path.GetFileName(installer)));
        }

        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                // Overwrite existing files
                file.CopyTo(tempPath, true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }

        public static void FetchUpdateContents()
        {
            // Force delete existing zip file and temp directory with retry logic
            if (File.Exists(UpdateTempFilename))
            {
                try
                {
                    File.Delete(UpdateTempFilename);
                }
                catch
                {
                    // Retry after a short delay
                    System.Threading.Thread.Sleep(100);
                    try { File.Delete(UpdateTempFilename); }
                    catch { /* Ignore if still locked */ }
                }
            }
            
            if (Directory.Exists(UpdateTempDirname))
            {
                try
                {
                    // Delete all files in the directory first
                    foreach (var file in Directory.GetFiles(UpdateTempDirname, "*", SearchOption.AllDirectories))
                    {
                        try { File.Delete(file); }
                        catch { /* Ignore locked files */ }
                    }
                    Directory.Delete(UpdateTempDirname, true);
                }
                catch
                {
                    // Retry after a short delay
                    System.Threading.Thread.Sleep(200);
                    try
                    {
                        foreach (var file in Directory.GetFiles(UpdateTempDirname, "*", SearchOption.AllDirectories))
                        {
                            try { File.Delete(file); }
                            catch { /* Ignore locked files */ }
                        }
                        Directory.Delete(UpdateTempDirname, true);
                    }
                    catch { /* Ignore if still locked - will be overwritten anyway */ }
                }
            }
            
            // Ensure temp directory exists (it was just deleted, so create fresh)
            Directory.CreateDirectory(UpdateTempDirname);
            
            // First, try to use local update.zip (next to installer executable)
            string localUpdateZip = GetLocalUpdateZipPath();
            string zipFileToUse = null;
            
            if (File.Exists(localUpdateZip))
            {
                // Use local update.zip file
                zipFileToUse = localUpdateZip;
            }
            else
            {
                // Fallback: Download from remote URL
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(PackageURL, UpdateTempFilename);
                }
                
                // Verify zip file was downloaded
                if (!File.Exists(UpdateTempFilename))
                {
                    throw new FileNotFoundException($"Failed to find local update.zip at {localUpdateZip} and failed to download from {PackageURL}");
                }
                
                zipFileToUse = UpdateTempFilename;
            }
            
            // Extract the zip file to temp directory
            // Directory was already deleted above, so this will extract fresh
            ZipFile.ExtractToDirectory(zipFileToUse, UpdateTempDirname);
        }

        public static void InstallUpdate()
        {
            if (IsUpdating) return;
            IsUpdating = true;

            // kill all running instances of the compiler
            foreach (var proc in Process.GetProcessesByName("debugcompiler"))
            {
                proc.Kill();
                System.Threading.Thread.Sleep(100);
            }

            // kill all running instances of the GUI
            foreach (var proc in Process.GetProcessesByName("T7CompilerGUI"))
            {
                proc.Kill();
                System.Threading.Thread.Sleep(100);
            }

            // cache update contents
            FetchUpdateContents();

            // kill all running instances of the compiler
            foreach (var proc in Process.GetProcessesByName("debugcompiler"))
            {
                proc.Kill();
                System.Threading.Thread.Sleep(100);
            }

            // kill all running instances of the GUI
            foreach (var proc in Process.GetProcessesByName("T7CompilerGUI"))
            {
                proc.Kill();
                System.Threading.Thread.Sleep(100);
            }

            foreach (var proc in Process.GetProcessesByName("blackops3"))
            {
                proc.Kill();
                System.Threading.Thread.Sleep(100);
            }

            foreach (var proc in Process.GetProcessesByName("blackops4"))
            {
                proc.Kill();
                System.Threading.Thread.Sleep(100);
            }

            // purge old installation
            if (Directory.Exists(Path.Combine(InstallRoot, "t7compiler")))
            {
                Directory.Delete(Path.Combine(InstallRoot, "t7compiler"), true);
            }

            // copy t7compiler folder with ALL contents (includes all files and subdirectories)
            string compilerSource = Path.Combine(UpdateTempDirname, "t7compiler");
            string compilerTarget = Path.Combine(InstallRoot, "t7compiler");
            if (Directory.Exists(compilerSource))
            {
                // Verify source has files before copying
                var compilerFiles = Directory.GetFiles(compilerSource);
                
                DirectoryCopy(compilerSource, compilerTarget, true);
                
                // Verify installation - check if exe and db files were copied
                var installedFiles = Directory.GetFiles(compilerTarget);
                if (installedFiles.Length == 0 && compilerFiles.Length > 0)
                {
                    // Files exist in source but weren't copied - something went wrong
                    throw new Exception($"Failed to copy t7compiler files. Source had {compilerFiles.Length} files but target has {installedFiles.Length} files.");
                }
            }
            else
            {
                throw new DirectoryNotFoundException($"t7compiler folder not found in update package at: {compilerSource}");
            }

            // copy defaultproject to t7compiler folder (if it exists separately in the zip)
            // This ensures defaultproject is available even if it wasn't included in t7compiler folder
            string defaultProjectSource = Path.Combine(UpdateTempDirname, "defaultproject");
            if (Directory.Exists(defaultProjectSource))
            {
                string defaultProjectTargetCompiler = Path.Combine(InstallRoot, "t7compiler", "defaultproject");
                if (Directory.Exists(defaultProjectTargetCompiler))
                {
                    Directory.Delete(defaultProjectTargetCompiler, true);
                }
                DirectoryCopy(defaultProjectSource, defaultProjectTargetCompiler, true);
            }

            // copy t7gui folder with ALL contents (includes all files and subdirectories)
            string guiSource = Path.Combine(UpdateTempDirname, "t7gui");
            string guiTarget = Path.Combine(InstallRoot, "t7gui");
            if (Directory.Exists(guiSource))
            {
                // Verify source has files before copying
                var guiFiles = Directory.GetFiles(guiSource);
                var guiDirs = Directory.GetDirectories(guiSource);
                
                if (Directory.Exists(guiTarget))
                    Directory.Delete(guiTarget, true);
                    
                // Copy all contents (files and subdirectories)
                DirectoryCopy(guiSource, guiTarget, true);
                
                // Verify installation - check if exe and db files were copied
                var installedFiles = Directory.GetFiles(guiTarget);
                if (installedFiles.Length == 0 && guiFiles.Length > 0)
                {
                    // Files exist in source but weren't copied - something went wrong
                    throw new Exception($"Failed to copy t7gui files. Source had {guiFiles.Length} files but target has {installedFiles.Length} files.");
                }
            }
            else
            {
                throw new DirectoryNotFoundException($"t7gui folder not found in update package at: {guiSource}");
            }

            // copy defaultproject to t7gui folder as well (if it exists separately in the zip)
            // This ensures defaultproject is available in both locations
            if (Directory.Exists(defaultProjectSource))
            {
                string defaultProjectTargetGui = Path.Combine(InstallRoot, "t7gui", "defaultproject");
                if (Directory.Exists(defaultProjectTargetGui))
                {
                    Directory.Delete(defaultProjectTargetGui, true);
                }
                DirectoryCopy(defaultProjectSource, defaultProjectTargetGui, true);
            }

            // Note: VSIX extension installation is now handled separately by InstallVSCExt button
            
            // Cleanup: Delete temp files and folders after successful installation
            try
            {
                if (File.Exists(UpdateTempFilename))
                {
                    File.Delete(UpdateTempFilename);
                }
                if (Directory.Exists(UpdateTempDirname))
                {
                    // Delete all files first
                    foreach (var file in Directory.GetFiles(UpdateTempDirname, "*", SearchOption.AllDirectories))
                    {
                        try { File.Delete(file); }
                        catch { /* Ignore locked files */ }
                    }
                    Directory.Delete(UpdateTempDirname, true);
                }

                // Cleanup: Delete any update.zip files from Downloads folder
                try
                {
                    string downloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                    if (Directory.Exists(downloadsFolder))
                    {
                        var updateZipFiles = Directory.GetFiles(downloadsFolder, "update.zip", SearchOption.TopDirectoryOnly);
                        foreach (var zipFile in updateZipFiles)
                        {
                            try
                            {
                                File.Delete(zipFile);
                            }
                            catch
                            {
                                // Ignore if file is locked or can't be deleted
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore cleanup errors for Downloads folder
                }
            }
            catch
            {
                // Ignore cleanup errors - temp files will be cleaned up on next run or by system
            }
            
            IsUpdating = false;
        }

        public static void UpdateVSCExtension()
        {
            if (IsUpdating) return;
            IsUpdating = true;

            // cache update contents
            FetchUpdateContents();

            // Install the vsc extension
            InstallVSCExtensionsCached();
            IsUpdating = false;
        }

        public static void CopyDefaultProject(string path, string gameExt, bool noAppend = false)
        {
            if(!noAppend)
            {
                path = Path.Combine(path, "Default Project");
            }
            // First check t7compiler folder
            if (Directory.Exists(Path.Combine(InstallRoot, "t7compiler", "defaultproject", gameExt)))
            {
                // copy default project
                DirectoryCopy(Path.Combine(InstallRoot, "t7compiler", "defaultproject", gameExt), path, true);
                return;
            }
            // Then check t7gui folder
            if (Directory.Exists(Path.Combine(InstallRoot, "t7gui", "defaultproject", gameExt)))
            {
                // copy default project from GUI folder
                DirectoryCopy(Path.Combine(InstallRoot, "t7gui", "defaultproject", gameExt), path, true);
                return;
            }
            if (Directory.Exists(Path.Combine(UpdateTempDirname, "defaultproject", gameExt)))
            {
                // restore default project to both locations
                DirectoryCopy(Path.Combine(UpdateTempDirname, "defaultproject"), Path.Combine(InstallRoot, "t7compiler", "defaultproject"), true);
                DirectoryCopy(Path.Combine(UpdateTempDirname, "defaultproject"), Path.Combine(InstallRoot, "t7gui", "defaultproject"), true);

                // copy default project
                DirectoryCopy(Path.Combine(UpdateTempDirname, "defaultproject", gameExt), path, true);
                return;
            }

            if (IsUpdating) return;
            IsUpdating = true;

            // cache update contents
            FetchUpdateContents();
            IsUpdating = false;

            // try again
            CopyDefaultProject(path, gameExt, noAppend);
        }

        internal static void NoExcept(Action a)
        {
            try
            {
                a();
            }
            catch { }
        }

        private static void InstallVSCExtensionsCached()
        {
            foreach (var file in Directory.GetFiles(UpdateTempDirname, "*.vsix"))
            {
                InstallExtension(file);
            }
        }

        private static void InstallExtension(string path)
        {
            using (Process proc = Process.Start(new ProcessStartInfo()
            {
                FileName = "code",
                Arguments = $"--install-extension \"{path}\"",
                UseShellExecute = true,
            }))
            {
                proc.WaitForExit();
            }
        }

        private static void HandleUIThreadExceptions(object sender, ThreadExceptionEventArgs args)
        {
            try
            {
                CErrorDialog.Show("Error", args.Exception.Message, true);
                IsUpdating = false;
                return;
            }
            catch
            {
                try
                {
                    MessageBox.Show("Fatal internal exception...", "Error", MessageBoxButtons.OK);
                }
                finally
                {
                    Application.Exit();
                }
            }
        }

        private static void HandleCurrentDomainExceptions(object sender, UnhandledExceptionEventArgs args)
        {
            try
            {
                CErrorDialog.Show("Error", ((Exception)args.ExceptionObject).Message, true);
                IsUpdating = false;
                return;
            }
            catch
            {
                try
                {
                    MessageBox.Show("Fatal internal exception...", "Error", MessageBoxButtons.OK);
                }
                finally
                {
                    Application.Exit();
                }
            }
        }
    }
}
