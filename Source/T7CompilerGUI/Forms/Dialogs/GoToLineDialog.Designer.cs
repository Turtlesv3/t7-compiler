using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Enum.Poison;
using ReaLTaiizor.Forms;

namespace T7CompilerGUI.Forms.Dialogs
{
    partial class GoToLineDialog
    {
        private System.ComponentModel.IContainer components = null;

        private PoisonLabel lblPrompt;
        private System.Windows.Forms.ComboBox comboNavigate;
        private PoisonLabel lblLineNumber;
        private PoisonTextBox txtLineNumber;
        private PoisonButton btnOK;
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
            this.lblPrompt = new ReaLTaiizor.Controls.PoisonLabel();
            this.comboNavigate = new System.Windows.Forms.ComboBox();
            this.lblLineNumber = new ReaLTaiizor.Controls.PoisonLabel();
            this.txtLineNumber = new ReaLTaiizor.Controls.PoisonTextBox();
            this.btnOK = new ReaLTaiizor.Controls.PoisonButton();
            this.btnCancel = new ReaLTaiizor.Controls.PoisonButton();
            this.SuspendLayout();
            // 
            // lblPrompt
            // 
            this.lblPrompt.Location = new System.Drawing.Point(23, 49);
            this.lblPrompt.Name = "lblPrompt";
            this.lblPrompt.Size = new System.Drawing.Size(85, 20);
            this.lblPrompt.TabIndex = 0;
            this.lblPrompt.Text = "Navigate to:";
            this.lblPrompt.UseStyleColors = true;
            // 
            // comboNavigate
            // 
            this.comboNavigate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboNavigate.FormattingEnabled = true;
            this.comboNavigate.Location = new System.Drawing.Point(23, 72);
            this.comboNavigate.Name = "comboNavigate";
            this.comboNavigate.Size = new System.Drawing.Size(400, 21);
            this.comboNavigate.TabIndex = 1;
            // 
            // lblLineNumber
            // 
            this.lblLineNumber.Location = new System.Drawing.Point(23, 96);
            this.lblLineNumber.Name = "lblLineNumber";
            this.lblLineNumber.Size = new System.Drawing.Size(142, 20);
            this.lblLineNumber.TabIndex = 2;
            this.lblLineNumber.Text = "Or enter line number:";
            this.lblLineNumber.UseStyleColors = true;
            // 
            // txtLineNumber
            // 
            // 
            // 
            // 
            this.txtLineNumber.CustomButton.Image = null;
            this.txtLineNumber.CustomButton.Location = new System.Drawing.Point(378, 1);
            this.txtLineNumber.CustomButton.Name = "";
            this.txtLineNumber.CustomButton.Size = new System.Drawing.Size(21, 21);
            this.txtLineNumber.CustomButton.Style = ReaLTaiizor.Enum.Poison.ColorStyle.Blue;
            this.txtLineNumber.CustomButton.TabIndex = 1;
            this.txtLineNumber.CustomButton.Theme = ReaLTaiizor.Enum.Poison.ThemeStyle.Light;
            this.txtLineNumber.CustomButton.UseSelectable = true;
            this.txtLineNumber.CustomButton.Visible = false;
            this.txtLineNumber.Lines = new string[0];
            this.txtLineNumber.Location = new System.Drawing.Point(23, 119);
            this.txtLineNumber.MaxLength = 32767;
            this.txtLineNumber.Name = "txtLineNumber";
            this.txtLineNumber.PasswordChar = '\0';
            this.txtLineNumber.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.txtLineNumber.SelectedText = "";
            this.txtLineNumber.SelectionLength = 0;
            this.txtLineNumber.SelectionStart = 0;
            this.txtLineNumber.ShortcutsEnabled = true;
            this.txtLineNumber.Size = new System.Drawing.Size(400, 23);
            this.txtLineNumber.TabIndex = 3;
            this.txtLineNumber.UseSelectable = true;
            this.txtLineNumber.UseStyleColors = true;
            this.txtLineNumber.WaterMarkColor = System.Drawing.Color.FromArgb(((int)(((byte)(109)))), ((int)(((byte)(109)))), ((int)(((byte)(109)))));
            this.txtLineNumber.WaterMarkFont = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Pixel);
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(267, 148);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 30);
            this.btnOK.TabIndex = 4;
            this.btnOK.Text = "OK";
            this.btnOK.UseSelectable = true;
            this.btnOK.UseStyleColors = true;
            this.btnOK.Click += new System.EventHandler(this.BtnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(348, 148);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 30);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseSelectable = true;
            this.btnCancel.UseStyleColors = true;
            // 
            // GoToLineDialog
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(440, 217);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.txtLineNumber);
            this.Controls.Add(this.lblLineNumber);
            this.Controls.Add(this.comboNavigate);
            this.Controls.Add(this.lblPrompt);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "GoToLineDialog";
            this.PoisonBorderStyle = ReaLTaiizor.Enum.Poison.FormBorderStyle.FixedSingle;
            this.ShadowType = ReaLTaiizor.Enum.Poison.FormShadowType.DropShadow;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Go to Line";
            this.ResumeLayout(false);

        }
    }
}

