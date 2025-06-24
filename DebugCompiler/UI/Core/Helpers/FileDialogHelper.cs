using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace DebugCompiler.UI.Core.Helpers
{
    public static class FileDialogHelper
    {
        public static string BrowseForGSCFile(string initialDirectory = null)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Select GSC File";
                dialog.InitialDirectory = initialDirectory ?? AppSettings.LastScriptDirectory ??
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                dialog.Filter = "GSC Files (*.gsc, *.gscc)|*.gsc;*.gscc|All Files (*.*)|*.*";
                dialog.RestoreDirectory = true;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    AppSettings.LastScriptDirectory = Path.GetDirectoryName(dialog.FileName);
                    return dialog.FileName;
                }
                return string.Empty;
            }
        }

        public static string BrowseForFolder(string description, string initialDirectory = null)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = description;
                dialog.InitialDirectory = initialDirectory ?? AppSettings.LastScriptDirectory ??
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                dialog.ValidateNames = false;
                dialog.CheckFileExists = false;
                dialog.CheckPathExists = true;
                dialog.FileName = "Folder Selection";
                dialog.Filter = "Folders|\n";

                // Handle folder selection
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    // Get the directory from the path
                    string selectedPath = Path.GetDirectoryName(dialog.FileName);

                    // If user selected a folder (not a file), use that directly
                    if (Directory.Exists(dialog.FileName))
                    {
                        selectedPath = dialog.FileName;
                    }

                    if (Directory.Exists(selectedPath))
                    {
                        AppSettings.LastScriptDirectory = selectedPath;
                        return selectedPath;
                    }
                }
                return string.Empty;
            }
        }
    }
}