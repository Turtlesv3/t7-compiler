using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Vanara.PInvoke;
using static Vanara.PInvoke.Shell32;

[ComImport]
[Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ILegacyShellItem
{
    void BindToHandler(IntPtr pbc, [In] ref Guid bhid, [In] ref Guid riid, out IntPtr ppv);
    void GetParent(out ILegacyShellItem ppsi);
    [PreserveSig]
    void GetDisplayName([In] uint sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
    void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
    void Compare(ILegacyShellItem psi, uint hint, out int piOrder);
}

namespace T7CompilerGUI.Helpers
{
    /// <summary>
    /// Helper class for showing modern Windows folder picker dialogs using Vanara.PInvoke
    /// </summary>
    public static class ModernFolderDialog
    {
        /// <summary>
        /// Shows a modern folder picker dialog
        /// </summary>
        /// <param name="owner">Owner window (can be null)</param>
        /// <param name="title">Dialog title</param>
        /// <param name="initialPath">Initial folder path (optional)</param>
        /// <returns>Selected folder path, or null if cancelled</returns>
        public static string Show(IWin32Window owner, string title, string initialPath = null)
        {
            try
            {
                IntPtr handle = owner?.Handle ?? IntPtr.Zero;
                
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
                
                // Fallback to standard folder browser
                using (var fallbackDialog = new FolderBrowserDialog())
                {
                    fallbackDialog.Description = title;
                    fallbackDialog.ShowNewFolderButton = true;
                    
                    if (!string.IsNullOrWhiteSpace(initialPath))
                    {
                        if (File.Exists(initialPath))
                        {
                            initialPath = Path.GetDirectoryName(initialPath);
                        }
                        
                        if (!string.IsNullOrWhiteSpace(initialPath) && Directory.Exists(initialPath))
                        {
                            fallbackDialog.SelectedPath = initialPath;
                        }
                    }
                    
                    if (fallbackDialog.ShowDialog(owner) == DialogResult.OK)
                    {
                        return fallbackDialog.SelectedPath;
                    }
                }
                
                return null;
            }
        }

        // Native methods for path parsing
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern uint SHParseDisplayName([MarshalAs(UnmanagedType.LPWStr)] string pszName, IntPtr pbc, out IntPtr ppidl, uint sfgaoIn, out uint psfgaoOut);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SHCreateItemFromIDList(IntPtr pidl, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, [MarshalAs(UnmanagedType.Interface)] out ILegacyShellItem ppv);

        /// <summary>
        /// Creates an IShellItem from a file system path using Vanara
        /// Note: Vanara's IShellItem and ILegacyShellItem are the same COM interface, so we can cast
        /// </summary>
        private static IShellItem CreateShellItemFromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return null;

            IntPtr pidl = IntPtr.Zero;
            try
            {
                uint parseResult = SHParseDisplayName(path, IntPtr.Zero, out pidl, 0, out _);
                if (parseResult == 0 && pidl != IntPtr.Zero)
                {
                    SHCreateItemFromIDList(pidl, typeof(ILegacyShellItem).GUID, out ILegacyShellItem legacyItem);
                    if (legacyItem != null)
                    {
                        // Cast to Vanara's IShellItem (same COM interface, different managed wrapper)
                        return (IShellItem)(object)legacyItem;
                    }
                }
            }
            catch
            {
                return null;
            }
            finally
            {
                if (pidl != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(pidl);
            }
            return null;
        }
    }
}

