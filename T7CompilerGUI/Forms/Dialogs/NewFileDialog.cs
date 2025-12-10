using System;
using System.IO;
using System.Windows.Forms;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Controls;

namespace T7CompilerGUI.Forms.Dialogs
{
    public partial class NewFileDialog : PoisonForm
    {
        private PoisonLabel lblFileName;
        private PoisonTextBox txtFileName;
        private PoisonLabel lblFileType;
        private PoisonComboBox cmbFileType;
        private PoisonButton btnCreate;
        private PoisonButton btnCancel;
        
        public string FileName { get; private set; }
        public string FileExtension { get; private set; }
        public string ProjectPath { get; set; }
        
        public NewFileDialog()
        {
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            this.Text = "New File";
            this.Size = new System.Drawing.Size(350, 150);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            
            lblFileName = new PoisonLabel
            {
                Text = "File Name:",
                Location = new System.Drawing.Point(10, 15),
                Size = new System.Drawing.Size(80, 20)
            };
            
            txtFileName = new PoisonTextBox
            {
                Location = new System.Drawing.Point(100, 12),
                Size = new System.Drawing.Size(230, 25)
            };
            txtFileName.KeyDown += TxtFileName_KeyDown;
            
            lblFileType = new PoisonLabel
            {
                Text = "File Type:",
                Location = new System.Drawing.Point(10, 50),
                Size = new System.Drawing.Size(80, 20)
            };
            
            cmbFileType = new PoisonComboBox
            {
                Location = new System.Drawing.Point(100, 47),
                Size = new System.Drawing.Size(230, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbFileType.Items.Add("GSC (.gsc)");
            cmbFileType.Items.Add("Text (.txt)");
            cmbFileType.SelectedIndex = 0;
            
            btnCreate = new PoisonButton
            {
                Text = "Create",
                Location = new System.Drawing.Point(160, 80),
                Size = new System.Drawing.Size(80, 30),
                DialogResult = DialogResult.OK
            };
            btnCreate.Click += BtnCreate_Click;
            
            btnCancel = new PoisonButton
            {
                Text = "Cancel",
                Location = new System.Drawing.Point(250, 80),
                Size = new System.Drawing.Size(80, 30),
                DialogResult = DialogResult.Cancel
            };
            btnCancel.Click += (s, e) => this.Close();
            
            this.Controls.Add(lblFileName);
            this.Controls.Add(txtFileName);
            this.Controls.Add(lblFileType);
            this.Controls.Add(cmbFileType);
            this.Controls.Add(btnCreate);
            this.Controls.Add(btnCancel);
            
            this.AcceptButton = btnCreate;
            this.CancelButton = btnCancel;
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
        
        public static bool ShowNewFileDialog(Form parent, string projectPath, out string fileName, out string fileExtension)
        {
            fileName = null;
            fileExtension = null;
            
            using (var dialog = new NewFileDialog())
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

