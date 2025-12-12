using System;
using System.IO;
using System.Windows.Forms;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Manager;

namespace T7CompilerGUI.Forms.Dialogs
{
    public partial class NewFileDialog : PoisonForm
    {
        public string FileName { get; private set; }
        public string FileExtension { get; private set; }
        public string ProjectPath { get; set; }
        
        private PoisonStyleManager styleManager;
        
        public NewFileDialog(PoisonStyleManager styleManager = null)
        {
            this.styleManager = styleManager;
            InitializeComponent();
            
            // Set StyleManager if provided
            if (styleManager != null)
            {
                this.StyleManager = styleManager;
                
                // Apply StyleManager to all Poison controls
                if (lblFileName != null)
                {
                    lblFileName.StyleManager = styleManager;
                    lblFileName.UseStyleColors = true;
                }
                if (txtFileName != null)
                {
                    txtFileName.StyleManager = styleManager;
                    txtFileName.UseStyleColors = true;
                }
                if (lblFileType != null)
                {
                    lblFileType.StyleManager = styleManager;
                    lblFileType.UseStyleColors = true;
                }
                if (cmbFileType != null)
                {
                    cmbFileType.StyleManager = styleManager;
                    cmbFileType.UseStyleColors = true;
                }
                if (btnCreate != null)
                {
                    btnCreate.StyleManager = styleManager;
                    btnCreate.UseStyleColors = true;
                }
                if (btnCancel != null)
                {
                    btnCancel.StyleManager = styleManager;
                    btnCancel.UseStyleColors = true;
                }
            }
            
            // Set default selection after InitializeComponent
            if (cmbFileType != null)
            {
                cmbFileType.SelectedIndex = 0;
            }
        }
        
        private void BtnCancel_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }

        private void TxtFileName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }
        
        private void BtnCreate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFileName.Text))
            {
                MessageBox.Show("Please enter a file name.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // Validate file name for invalid characters
            char[] invalidChars = Path.GetInvalidFileNameChars();
            if (txtFileName.Text.IndexOfAny(invalidChars) >= 0)
            {
                MessageBox.Show("File name contains invalid characters. Please remove special characters, numbers, or spaces.", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            // Check for numbers (like original validation)
            bool hasNumber = false;
            foreach (char c in txtFileName.Text)
            {
                if (char.IsDigit(c))
                {
                    hasNumber = true;
                    break;
                }
            }
            
            if (hasNumber)
            {
                MessageBox.Show("File name cannot contain numbers.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            string extension = cmbFileType.SelectedIndex == 0 ? "gsc" : "txt";
            string fullPath = Path.Combine(ProjectPath, "scripts", $"{txtFileName.Text}.{extension}");
            
            if (File.Exists(fullPath))
            {
                MessageBox.Show($"File already exists ({txtFileName.Text}.{extension})", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            FileName = txtFileName.Text;
            FileExtension = extension;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        
        public static bool ShowNewFileDialog(Form parent, string projectPath, out string fileName, out string fileExtension, ReaLTaiizor.Manager.PoisonStyleManager styleManager = null)
        {
            fileName = null;
            fileExtension = null;
            
            using (var dialog = new NewFileDialog(styleManager))
            {
                dialog.ProjectPath = projectPath;
                if (dialog.ShowDialog(parent) == DialogResult.OK)
                {
                    fileName = dialog.FileName;
                    fileExtension = dialog.FileExtension;
                    
                    // Create the file
                    string fullPath = Path.Combine(projectPath, "scripts", $"{fileName}.{fileExtension}");
                    File.WriteAllText(fullPath, $"// File - {fileName}.{fileExtension}");
                    
                    return true;
                }
            }
            
            return false;
        }
    }
}

