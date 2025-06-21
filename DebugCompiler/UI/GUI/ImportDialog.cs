using Refract.UI.Core.Interfaces;
using Refract.UI.Core.Singletons;
using SMC.UI.Core.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DebugCompiler
{
    public partial class ImportDialog : Form, IThemeableControl
    {
        private string ImportFolderPath;
        private string OutputFolderPath;
        public ImportDialog()
        {
            InitializeComponent();
            UIThemeManager.OnThemeChanged(this, OnThemeChanged_Implementation);
            this.SetThemeAware();
            MaximizeBox = true;
            MinimizeBox = true;
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
        }

        private void RPCTest1_Click(object sender, EventArgs e)
        {

        }

        private void RPCExample2_Click(object sender, EventArgs e)
        {

        }

        private void RPCExample3_Click(object sender, EventArgs e)
        {
        }

        private void ExampleRPC4_Click(object sender, EventArgs e)
        {
        }

        private void button1_Click(object sender, EventArgs e)
        {
        }

        private void SelectImportButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
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

        private void OutputBtn_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.ShowNewFolderButton = true;
            fbd.Description = "Select an output folder";
            if (fbd.ShowDialog() != DialogResult.OK) return;
            if (Directory.Exists(fbd.SelectedPath) && !IsDirectoryEmpty(fbd.SelectedPath))
            {
                MessageBox.Show("You cannot select this folder for output. The folder you select must be empty.", "DANGER: Folder has contents", MessageBoxButtons.OKCancel);
                return;
            }
            OutputFolderPath = fbd.SelectedPath;
            OutputLabel.Text = OutputFolderPath.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "Error";
        }

        private bool IsDirectoryEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }

        private void StartImportButton_Click(object sender, EventArgs e)
        {
            
        }
    }
}
