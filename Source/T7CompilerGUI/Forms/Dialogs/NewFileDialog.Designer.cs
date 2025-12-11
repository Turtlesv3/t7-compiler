using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Enum.Poison;
using ReaLTaiizor.Forms;

namespace T7CompilerGUI.Forms.Dialogs
{
    partial class NewFileDialog
    {
        private System.ComponentModel.IContainer components = null;

        private PoisonLabel lblFileName;
        private PoisonTextBox txtFileName;
        private PoisonLabel lblFileType;
        private PoisonComboBox cmbFileType;
        private PoisonButton btnCreate;
        private PoisonButton btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblFileName = new ReaLTaiizor.Controls.PoisonLabel();
            this.txtFileName = new ReaLTaiizor.Controls.PoisonTextBox();
            this.lblFileType = new ReaLTaiizor.Controls.PoisonLabel();
            this.cmbFileType = new ReaLTaiizor.Controls.PoisonComboBox();
            this.btnCreate = new ReaLTaiizor.Controls.PoisonButton();
            this.btnCancel = new ReaLTaiizor.Controls.PoisonButton();
            this.SuspendLayout();
            // 
            // lblFileName
            // 
            this.lblFileName.Location = new System.Drawing.Point(14, 68);
            this.lblFileName.Name = "lblFileName";
            this.lblFileName.Size = new System.Drawing.Size(80, 20);
            this.lblFileName.TabIndex = 0;
            this.lblFileName.Text = "File Name:";
            this.lblFileName.UseStyleColors = true;
            // 
            // txtFileName
            // 
            // 
            // 
            // 
            this.txtFileName.CustomButton.Image = null;
            this.txtFileName.CustomButton.Location = new System.Drawing.Point(206, 1);
            this.txtFileName.CustomButton.Name = "";
            this.txtFileName.CustomButton.Size = new System.Drawing.Size(23, 23);
            this.txtFileName.CustomButton.Style = ReaLTaiizor.Enum.Poison.ColorStyle.Blue;
            this.txtFileName.CustomButton.TabIndex = 1;
            this.txtFileName.CustomButton.Theme = ReaLTaiizor.Enum.Poison.ThemeStyle.Light;
            this.txtFileName.CustomButton.UseSelectable = true;
            this.txtFileName.CustomButton.Visible = false;
            this.txtFileName.Lines = new string[0];
            this.txtFileName.Location = new System.Drawing.Point(100, 63);
            this.txtFileName.MaxLength = 32767;
            this.txtFileName.Name = "txtFileName";
            this.txtFileName.PasswordChar = '\0';
            this.txtFileName.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.txtFileName.SelectedText = "";
            this.txtFileName.SelectionLength = 0;
            this.txtFileName.SelectionStart = 0;
            this.txtFileName.ShortcutsEnabled = true;
            this.txtFileName.Size = new System.Drawing.Size(230, 25);
            this.txtFileName.TabIndex = 1;
            this.txtFileName.UseSelectable = true;
            this.txtFileName.UseStyleColors = true;
            this.txtFileName.WaterMarkColor = System.Drawing.Color.FromArgb(((int)(((byte)(109)))), ((int)(((byte)(109)))), ((int)(((byte)(109)))));
            this.txtFileName.WaterMarkFont = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Pixel);
            this.txtFileName.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TxtFileName_KeyDown);
            // 
            // lblFileType
            // 
            this.lblFileType.Location = new System.Drawing.Point(14, 103);
            this.lblFileType.Name = "lblFileType";
            this.lblFileType.Size = new System.Drawing.Size(80, 20);
            this.lblFileType.TabIndex = 2;
            this.lblFileType.Text = "File Type:";
            this.lblFileType.UseStyleColors = true;
            // 
            // cmbFileType
            // 
            this.cmbFileType.FormattingEnabled = true;
            this.cmbFileType.ItemHeight = 23;
            this.cmbFileType.Items.AddRange(new object[] {
            "GSC (.gsc)",
            "Text (.txt)"});
            this.cmbFileType.Location = new System.Drawing.Point(100, 94);
            this.cmbFileType.Name = "cmbFileType";
            this.cmbFileType.Size = new System.Drawing.Size(230, 29);
            this.cmbFileType.TabIndex = 3;
            this.cmbFileType.UseSelectable = true;
            // 
            // btnCreate
            // 
            this.btnCreate.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnCreate.Location = new System.Drawing.Point(164, 129);
            this.btnCreate.Name = "btnCreate";
            this.btnCreate.Size = new System.Drawing.Size(80, 30);
            this.btnCreate.TabIndex = 4;
            this.btnCreate.Text = "Create";
            this.btnCreate.UseSelectable = true;
            this.btnCreate.UseStyleColors = true;
            this.btnCreate.Click += new System.EventHandler(this.BtnCreate_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(250, 129);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(80, 30);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseSelectable = true;
            this.btnCancel.UseStyleColors = true;
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // NewFileDialog
            // 
            this.AcceptButton = this.btnCreate;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(350, 178);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnCreate);
            this.Controls.Add(this.cmbFileType);
            this.Controls.Add(this.lblFileType);
            this.Controls.Add(this.txtFileName);
            this.Controls.Add(this.lblFileName);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(350, 150);
            this.Name = "NewFileDialog";
            this.PoisonBorderStyle = ReaLTaiizor.Enum.Poison.FormBorderStyle.FixedSingle;
            this.ShadowType = ReaLTaiizor.Enum.Poison.FormShadowType.DropShadow;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "New File";
            this.ResumeLayout(false);

        }
    }
}

