using Refract.UI.Core.Interfaces;
using Refract.UI.Core.Singletons;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using SMC.UI.Core.Controls;
using System.IO;

namespace DebugCompiler
{
    public partial class ImportDialog : Form, IThemeableControl, IResizableForm
    {
        private string ImportFolderPath;
        private string OutputFolderPath;

        public ImportDialog()
        {
            InitializeComponent();
            UIThemeManager.OnThemeChanged(this, OnThemeChanged_Implementation);
            this.SetThemeAware();
        }

        public IEnumerable<Control> GetThemedControls()
        {
            yield return InnerForm;
            yield return SelectImportBtn;
            yield return ImportLabel;
            yield return OutputLabel;
            yield return OutputBtn;
            yield return StartImportButton;
        }

        private void OnThemeChanged_Implementation(UIThemeInfo currentTheme)
        {
            InnerForm.BackColor = currentTheme.AccentColor;
            ImportLabel.ForeColor = currentTheme.TextColor;
            OutputLabel.ForeColor = currentTheme.TextColor;
        }

        public void WndProc_Implementation(ref Message m)
        {
            // Handle window messages if needed
            base.WndProc(ref m);
        }

        private void SelectImportButton_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.ShowNewFolderButton = false;
                fbd.Description = "Select a project to import";
                if (fbd.ShowDialog() != DialogResult.OK) return;

                if (!Directory.Exists(fbd.SelectedPath))
                {
                    CErrorDialog.Show("Selection failed!", "Cannot import a project which does not exist", true);
                    return;
                }

                ImportFolderPath = fbd.SelectedPath;
                ImportLabel.Text = ImportFolderPath.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "Error";
            }
        }

        private void OutputBtn_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.ShowNewFolderButton = true;
                fbd.Description = "Select an output folder";
                if (fbd.ShowDialog() != DialogResult.OK) return;

                if (Directory.Exists(fbd.SelectedPath) && !IsDirectoryEmpty(fbd.SelectedPath))
                {
                    MessageBox.Show("You cannot select this folder for output. The folder you select must be empty.",
                                  "DANGER: Folder has contents",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Warning);
                    return;
                }

                OutputFolderPath = fbd.SelectedPath;
                OutputLabel.Text = OutputFolderPath.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "Error";
            }
        }

        private bool IsDirectoryEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }

        private void StartImportButton_Click(object sender, EventArgs e)
        {
            if (ImportFolderPath is null || OutputFolderPath is null)
            {
                CErrorDialog.Show("Conversion Failed!",
                                "You must specify both a folder to import and a folder to output your converted project.",
                                true);
                return;
            }

            if (ImportFolderPath.Contains(OutputFolderPath) || OutputFolderPath.Contains(ImportFolderPath))
            {
                CErrorDialog.Show("Conversion Failed!",
                                "Your import and output folders must be in different directories.",
                                true);
                return;
            }

            try
            {
                // 1. Clear output folder
                if (Directory.Exists(OutputFolderPath))
                {
                    Directory.Delete(OutputFolderPath, true);
                }

                if (File.Exists(Path.Combine(ImportFolderPath, "gsc.conf")))
                {
                    DirectoryCopy(ImportFolderPath, OutputFolderPath, true);
                    CErrorDialog.Show("Success!", "Your project was migrated successfully!", true);
                    return;
                }

                // 2. Install a default project
                CopyDefaultProject(OutputFolderPath, "T7");

                // 3. Clear scripts folder
                string scripts = Path.Combine(OutputFolderPath, "scripts");
                Directory.Delete(scripts, true);

                // 4. Copy project files
                DirectoryCopy(ImportFolderPath, scripts, true);

                // 5. Fix EnableOnlineMatch references
                foreach (var file in Directory.GetFiles(scripts, "*.gsc", SearchOption.AllDirectories))
                {
                    string source = File.ReadAllText(file);
                    string lower = source.ToLower();
                    int index;
                    while ((index = lower.IndexOf("enableonlinematch")) > -1)
                    {
                        source = source.Remove(index, "enableonlinematch".Length).Insert(index, "getplayers");
                        lower = source.ToLower();
                    }
                    File.WriteAllText(file, source);
                }

                // 6. Create gsc.conf
                var config = Directory.GetFiles(scripts, "config.il", SearchOption.AllDirectories).FirstOrDefault();
                HashSet<string> symbols = new HashSet<string> { "BO3", "SERIOUS" };

                if (config != null)
                {
                    string configData = File.ReadAllText(config);
                    int index;
                    if ((index = configData.IndexOf("<Mode>")) > -1)
                    {
                        symbols.Add(new string(configData
                            .Skip(index + "<Mode>".Length)
                            .TakeWhile(c => c != '<')
                            .ToArray()).ToUpper());
                    }
                    if ((index = configData.IndexOf("<Symbols>")) > -1)
                    {
                        foreach (var symbol in new string(configData
                            .Skip(index + "<Symbols>".Length)
                            .TakeWhile(c => c != '<')
                            .ToArray()).ToUpper().Split(';'))
                        {
                            symbols.Add(symbol);
                        }
                    }
                    File.Delete(config);
                }

                File.WriteAllText(Path.Combine(OutputFolderPath, "gsc.conf"), $"symbols={string.Join(",", symbols)}");
                CErrorDialog.Show("Success!", "Your project was migrated successfully!", true);
            }
            catch (Exception ex)
            {
                CErrorDialog.Show("Migration Error", $"An error occurred during migration: {ex.Message}", true);
            }
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {sourceDirName}");
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            Directory.CreateDirectory(destDirName);

            foreach (FileInfo file in dir.GetFiles())
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, false);
            }

            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }

        private static void CopyDefaultProject(string outputPath, string gameType)
        {
            string templatePath = Path.Combine(Application.StartupPath, "Templates", gameType);
            if (Directory.Exists(templatePath))
            {
                DirectoryCopy(templatePath, outputPath, true);
            }
            else
            {
                throw new DirectoryNotFoundException($"Template directory not found for game type: {gameType}");
            }
        }
    }
}