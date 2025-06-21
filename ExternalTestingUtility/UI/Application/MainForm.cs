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

namespace t7c_installer
{
    public partial class MainForm : Form, IThemeableControl
    {
        public MainForm()
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
            yield return UpdateButton;
            yield return ConvertProj;
            yield return InstallVSCExt;
            yield return JoinDiscord;
            yield return CreateDefaultProject;
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

        private void Button1_Click(object sender, EventArgs e)
        {
        }

        private void JoinDiscord_Click(object sender, EventArgs e)
        {
            Process.Start("https://gsc.dev/s/discord");
        }

        private void UpdateButton_Click(object sender, EventArgs e)
        {
            // First try to find update.zip in the same directory
            string localUpdate = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "update.zip");

            if (File.Exists(localUpdate))
            {
                try
                {
                    Program.InstallUpdateFromLocal(localUpdate);
                    CErrorDialog.Show("Success!", "Compiler installed successfully from local update.zip", true);
                    return;
                }
                catch (Exception)
                {
                    // Fall through to manual selection if automatic fails
                }
            }

            // If not found, prompt user to select it
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Update Package|update.zip",
                Title = "Select update.zip file",
                InitialDirectory = Path.GetDirectoryName(Application.ExecutablePath)
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Program.InstallUpdateFromLocal(openFileDialog.FileName);
                    CErrorDialog.Show("Success!", "Compiler installed successfully", true);
                }
                catch (Exception ex)
                {
                    CErrorDialog.Show("Error", $"Failed to install: {ex.Message}", true);
                }
            }
        }

        private void InstallVSCExt_Click(object sender, EventArgs e)
        {
            Program.UpdateVSCExtension();
            CErrorDialog.Show("Success!", "Extension installed successfully", true);
        }

        private void CreateDefaultProject_Click(object sender, EventArgs e)
        {
            var box = new CComboDialog("Game for Project", new string[] { "Black Ops III", "Black Ops 4" });
            if (box.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            var game = (box.SelectedValue.ToString() == "Black Ops 4") ? "T8" : "T7";
            FolderBrowserDialog fbd = new FolderBrowserDialog
            {
                ShowNewFolderButton = true,
                Description = "Select a folder to copy the default project to"
            };
            if (fbd.ShowDialog() != DialogResult.OK) return;
            Program.CopyDefaultProject(fbd.SelectedPath, game);
            Process.Start(fbd.SelectedPath);
            CErrorDialog.Show("Success!", "Project installed successfully", true);
        }

        private void ConvertProj_Click(object sender, EventArgs e)
        {
            Visible = false;
            new ImportDialog().ShowDialog();
            Visible = true;
        }
    }
}
