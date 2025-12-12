using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Vanara.PInvoke;
using static Vanara.PInvoke.Shell32;

namespace T7CompilerGUI.Forms
{
    public partial class MainForm
    {
        #region Helper Methods (Folder Dialog and Path Utilities)
        
        private string ShowModernFileDialog(string title, string filter = null, string initialPath = null)
        {
            // Use standard OpenFileDialog directly - more reliable than COM interop
            // The modern COM dialog was causing crashes, so we'll use the standard one
            return ShowModernFileDialogFallback(title, filter, initialPath);
        }
        
        private string ShowModernFileDialogFallback(string title, string filter, string initialPath)
        {
            try
            {
                if (this.IsDisposed || this.Disposing)
                    return null;
                    
                using (var dialog = new OpenFileDialog())
                {
                    dialog.Title = title;
                    if (!string.IsNullOrEmpty(filter))
                        dialog.Filter = filter;
                    
                    // Set icon - dialogs inherit from owner form
                    if (this.Icon != null)
                    {
                        // The dialog will use the owner form's icon automatically
                    }
                    
                    if (!string.IsNullOrWhiteSpace(initialPath))
                    {
                        if (File.Exists(initialPath))
                            dialog.InitialDirectory = Path.GetDirectoryName(initialPath);
                        else if (Directory.Exists(initialPath))
                            dialog.InitialDirectory = initialPath;
                    }
                    
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                        return dialog.FileName;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fallback file dialog failed: {ex.Message}");
            }
            
            return null;
        }
        
        private string ShowModernFolderDialog(string title, string initialPath = null)
        {
            // Ensure we're on the UI thread (COM interop requires this)
            if (this.InvokeRequired)
            {
                string result = null;
                this.Invoke(new Action(() => { result = ShowModernFolderDialogInternal(title, initialPath); }));
                return result;
            }
            
            return ShowModernFolderDialogInternal(title, initialPath);
        }
        
        private string ShowModernFolderDialogInternal(string title, string initialPath = null)
        {
            try
            {
                // Ensure form handle is created before showing dialog
                IntPtr handle = this.Handle;
                if (handle == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Form handle not available");
                }
                
                // Ensure form is not disposed
                if (this.IsDisposed)
                {
                    throw new ObjectDisposedException("MainForm");
                }
                
                // Use Vanara's managed IFileOpenDialog wrapper
                IFileOpenDialog dialog = new IFileOpenDialog();
                try
                {
                    // Set folder picker options using Vanara's FILEOPENDIALOGOPTIONS enum
                    dialog.SetOptions(FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS);
                    
                    // Set title (only if not null/empty)
                    if (!string.IsNullOrEmpty(title))
                    {
                        dialog.SetTitle(title);
                    }

                    // Set initial folder if provided
                    if (!string.IsNullOrWhiteSpace(initialPath))
                    {
                        if (File.Exists(initialPath))
                        {
                            initialPath = Path.GetDirectoryName(initialPath);
                        }
                        
                        if (!string.IsNullOrWhiteSpace(initialPath) && Directory.Exists(initialPath))
                        {
                            try
                            {
                                // Use helper method to create Vanara IShellItem from path
                                var initialFolder = CreateShellItemFromPath(initialPath);
                                if (initialFolder != null)
                                {
                                    dialog.SetFolder(initialFolder);
                                }
                            }
                            catch
                            {
                                // If setting initial folder fails, continue anyway
                            }
                        }
                    }

                    // Show the dialog - Vanara's Show returns HRESULT
                    HRESULT result = dialog.Show(handle);
                    if (result == HRESULT.S_OK)
                    {
                        // Vanara's GetResult returns IShellItem directly
                        IShellItem item = dialog.GetResult();
                        if (item != null)
                        {
                            // Vanara's GetDisplayName returns string directly with SIGDN enum
                            string path = item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH);
                            if (!string.IsNullOrEmpty(path))
                            {
                                return path;
                            }
                        }
                    }
                    // If result != S_OK, user cancelled or error occurred
                    return null;
                }
                catch (COMException comEx)
                {
                    // COM-specific errors
                    System.Diagnostics.Debug.WriteLine($"COM error in folder dialog: {comEx.Message} (HRESULT: {comEx.ErrorCode})");
                    throw;
                }
                finally
                {
                    // Manually release COM object since IFileOpenDialog doesn't implement IDisposable
                    if (dialog != null)
                        Marshal.ReleaseComObject(dialog);
                }
            }
            catch (Exception ex)
            {
                // Log detailed error for debugging
                System.Diagnostics.Debug.WriteLine($"Modern folder dialog failed: {ex.Message}\n{ex.StackTrace}");
                
                // Show user-friendly error message
                MessageBox.Show(
                    $"The modern folder dialog could not be displayed.\n\nError: {ex.Message}\n\nFalling back to standard folder browser.",
                    "Dialog Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                
                using (var dialog = new FolderBrowserDialog())
                {
                    dialog.Description = title;
                    dialog.ShowNewFolderButton = true; // Allow creating new folders
                    
                    if (!string.IsNullOrWhiteSpace(initialPath))
                    {
                        // If it's a file, get its directory
                        if (File.Exists(initialPath))
                        {
                            initialPath = Path.GetDirectoryName(initialPath);
                        }
                        
                        // Only set if it's a valid directory
                        if (!string.IsNullOrWhiteSpace(initialPath) && Directory.Exists(initialPath))
                        {
                            dialog.SelectedPath = initialPath;
                        }
                    }
                    
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                        return dialog.SelectedPath;
                }
            }

            return null;
        }

        /// <summary>
        /// Helper function to shorten file paths to show just 1-2 directories before filename
        /// </summary>
        private string GetShortPath(string fullPath)
        {
            try
            {
                string fileName = Path.GetFileName(fullPath);
                string directory = Path.GetDirectoryName(fullPath);
                if (string.IsNullOrEmpty(directory))
                    return fileName;

                string[] parts = directory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                int partCount = parts.Length;
                
                if (partCount <= 2)
                {
                    // If 2 or fewer directories, show all
                    return Path.Combine(directory, fileName);
                }
                else
                {
                    // Show last 2 directories before filename
                    string dir1 = parts[partCount - 2];
                    string dir2 = parts[partCount - 1];
                    return Path.Combine("...", dir1, dir2, fileName);
                }
            }
            catch
            {
                return Path.GetFileName(fullPath);
            }
        }
        
        #endregion
    }
}

