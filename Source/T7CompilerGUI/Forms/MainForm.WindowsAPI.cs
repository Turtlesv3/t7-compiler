using System;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Shell32;

namespace T7CompilerGUI.Forms
{
    public partial class MainForm
    {
        #region Constants and Windows API Declarations (Legacy - kept for ShowModernFileDialog)
        
        // Legacy constants and interfaces - kept for ShowModernFileDialog which still uses raw COM
        private const uint FOS_PICKFOLDERS = 0x00000020;
        private const uint FOS_FILEMUSTEXIST = 0x00001000;
        private const uint FOS_PATHMUSTEXIST = 0x00000800;
        private const uint SIGDN_FILESYSPATH = 0x80058000;
        
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct COMDLG_FILTERSPEC
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszSpec;
        }
        
        [ComImport]
        [ClassInterface(ClassInterfaceType.None)]
        [TypeLibType(TypeLibTypeFlags.FCanCreate)]
        [Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
        private class FileOpenDialogRCW { }

        [ComImport]
        [Guid("42F85136-DB7E-439C-85F1-E4075D135FC8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ILegacyFileOpenDialog
        {
            [PreserveSig]
            uint Show([In] IntPtr hwndParent);
            [PreserveSig]
            void SetOptions(uint fos);
            void GetOptions(out uint pfos);
            void SetFolder(ILegacyShellItem psi);
            [PreserveSig]
            void SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            void GetResult(out ILegacyShellItem ppsi);
            [PreserveSig]
            void SetFileTypes(uint cFileTypes, [In, MarshalAs(UnmanagedType.LPArray)] COMDLG_FILTERSPEC[] rgFilterSpec);
        }

        [ComImport]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ILegacyShellItem
        {
            void BindToHandler(IntPtr pbc, [In] ref Guid bhid, [In] ref Guid riid, out IntPtr ppv);
            void GetParent(out ILegacyShellItem ppsi);
            [PreserveSig]
            void GetDisplayName([In] uint sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
            void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
            void Compare(ILegacyShellItem psi, uint hint, out int piOrder);
        }

        [ComImport]
        [Guid("B63EA76D-1F85-456F-A19C-48159EFA858B")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ILegacyShellItemArray
        {
            void BindToHandler();
            void GetPropertyStore();
            void GetPropertyDescriptionList();
            void GetAttributes();
            void GetCount(out uint pdwNumItems);
            void GetItemAt(uint dwIndex, out ILegacyShellItem ppsi);
            void EnumItems();
        }

        // Native methods for path parsing
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern uint SHParseDisplayName([MarshalAs(UnmanagedType.LPWStr)] string pszName, IntPtr pbc, out IntPtr ppidl, uint sfgaoIn, out uint psfgaoOut);

        // Legacy version for ShowModernFileDialog (uses ILegacyShellItem)
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SHCreateItemFromIDList(IntPtr pidl, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, [MarshalAs(UnmanagedType.Interface)] out ILegacyShellItem ppv);
        
        // Helper method to create Vanara IShellItem from path
        // Note: Vanara's IShellItem and ILegacyShellItem are the same COM interface, so we can cast
        private static IShellItem CreateShellItemFromPath(string path)
        {
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
            finally
            {
                if (pidl != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(pidl);
            }
            return null;
        }
        
        #endregion
    }
}

