using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Enum.Poison;
using ReaLTaiizor.Forms;

namespace T7CompilerGUI.Forms.Dialogs
{
    partial class ReplaceDialog
    {
        private System.ComponentModel.IContainer components = null;

        private PoisonLabel lblSearch;
        private PoisonLabel lblReplace;
        private PoisonTextBox txtSearch;
        private PoisonTextBox txtReplace;
        private PoisonCheckBox chkMatchCase;
        private PoisonCheckBox chkWholeWord;
        private PoisonCheckBox chkReplaceAll;
        private PoisonButton btnReplace;
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
            this.lblSearch = new ReaLTaiizor.Controls.PoisonLabel();
            this.txtSearch = new ReaLTaiizor.Controls.PoisonTextBox();
            this.lblReplace = new ReaLTaiizor.Controls.PoisonLabel();
            this.txtReplace = new ReaLTaiizor.Controls.PoisonTextBox();
            this.chkMatchCase = new ReaLTaiizor.Controls.PoisonCheckBox();
            this.chkWholeWord = new ReaLTaiizor.Controls.PoisonCheckBox();
            this.chkReplaceAll = new ReaLTaiizor.Controls.PoisonCheckBox();
            this.btnReplace = new ReaLTaiizor.Controls.PoisonButton();
            this.btnCancel = new ReaLTaiizor.Controls.PoisonButton();
            this.SuspendLayout();
            // 
            // lblSearch
            // 
            this.lblSearch.Location = new System.Drawing.Point(23, 60);
            this.lblSearch.Name = "lblSearch";
            this.lblSearch.Size = new System.Drawing.Size(360, 20);
            this.lblSearch.TabIndex = 0;
            this.lblSearch.Text = "Search for:";
            this.lblSearch.UseStyleColors = true;
            // 
            // txtSearch
            // 
            // 
            // 
            // 
            this.txtSearch.CustomButton.Image = null;
            this.txtSearch.CustomButton.Location = new System.Drawing.Point(338, 1);
            this.txtSearch.CustomButton.Name = "";
            this.txtSearch.CustomButton.Size = new System.Drawing.Size(21, 21);
            this.txtSearch.CustomButton.Style = ReaLTaiizor.Enum.Poison.ColorStyle.Blue;
            this.txtSearch.CustomButton.TabIndex = 1;
            this.txtSearch.CustomButton.Theme = ReaLTaiizor.Enum.Poison.ThemeStyle.Light;
            this.txtSearch.CustomButton.UseSelectable = true;
            this.txtSearch.CustomButton.Visible = false;
            this.txtSearch.Lines = new string[0];
            this.txtSearch.Location = new System.Drawing.Point(23, 85);
            this.txtSearch.MaxLength = 32767;
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.PasswordChar = '\0';
            this.txtSearch.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.txtSearch.SelectedText = "";
            this.txtSearch.SelectionLength = 0;
            this.txtSearch.SelectionStart = 0;
            this.txtSearch.ShortcutsEnabled = true;
            this.txtSearch.Size = new System.Drawing.Size(360, 23);
            this.txtSearch.TabIndex = 1;
            this.txtSearch.UseSelectable = true;
            this.txtSearch.UseStyleColors = true;
            this.txtSearch.WaterMarkColor = System.Drawing.Color.FromArgb(((int)(((byte)(109)))), ((int)(((byte)(109)))), ((int)(((byte)(109)))));
            this.txtSearch.WaterMarkFont = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Pixel);
            // 
            // lblReplace
            // 
            this.lblReplace.Location = new System.Drawing.Point(23, 115);
            this.lblReplace.Name = "lblReplace";
            this.lblReplace.Size = new System.Drawing.Size(360, 20);
            this.lblReplace.TabIndex = 2;
            this.lblReplace.Text = "Replace with:";
            this.lblReplace.UseStyleColors = true;
            // 
            // txtReplace
            // 
            // 
            // 
            // 
            this.txtReplace.CustomButton.Image = null;
            this.txtReplace.CustomButton.Location = new System.Drawing.Point(338, 1);
            this.txtReplace.CustomButton.Name = "";
            this.txtReplace.CustomButton.Size = new System.Drawing.Size(21, 21);
            this.txtReplace.CustomButton.Style = ReaLTaiizor.Enum.Poison.ColorStyle.Blue;
            this.txtReplace.CustomButton.TabIndex = 2;
            this.txtReplace.CustomButton.Theme = ReaLTaiizor.Enum.Poison.ThemeStyle.Light;
            this.txtReplace.CustomButton.UseSelectable = true;
            this.txtReplace.CustomButton.Visible = false;
            this.txtReplace.Lines = new string[0];
            this.txtReplace.Location = new System.Drawing.Point(23, 140);
            this.txtReplace.MaxLength = 32767;
            this.txtReplace.Name = "txtReplace";
            this.txtReplace.PasswordChar = '\0';
            this.txtReplace.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.txtReplace.SelectedText = "";
            this.txtReplace.SelectionLength = 0;
            this.txtReplace.SelectionStart = 0;
            this.txtReplace.ShortcutsEnabled = true;
            this.txtReplace.Size = new System.Drawing.Size(360, 23);
            this.txtReplace.TabIndex = 3;
            this.txtReplace.UseSelectable = true;
            this.txtReplace.UseStyleColors = true;
            this.txtReplace.WaterMarkColor = System.Drawing.Color.FromArgb(((int)(((byte)(109)))), ((int)(((byte)(109)))), ((int)(((byte)(109)))));
            this.txtReplace.WaterMarkFont = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Pixel);
            // 
            // chkMatchCase
            // 
            this.chkMatchCase.Location = new System.Drawing.Point(23, 170);
            this.chkMatchCase.Name = "chkMatchCase";
            this.chkMatchCase.Size = new System.Drawing.Size(150, 20);
            this.chkMatchCase.TabIndex = 4;
            this.chkMatchCase.Text = "Match case";
            this.chkMatchCase.UseSelectable = true;
            this.chkMatchCase.UseStyleColors = true;
            // 
            // chkWholeWord
            // 
            this.chkWholeWord.Location = new System.Drawing.Point(183, 170);
            this.chkWholeWord.Name = "chkWholeWord";
            this.chkWholeWord.Size = new System.Drawing.Size(150, 20);
            this.chkWholeWord.TabIndex = 5;
            this.chkWholeWord.Text = "Whole word";
            this.chkWholeWord.UseSelectable = true;
            this.chkWholeWord.UseStyleColors = true;
            // 
            // chkReplaceAll
            // 
            this.chkReplaceAll.Location = new System.Drawing.Point(23, 195);
            this.chkReplaceAll.Name = "chkReplaceAll";
            this.chkReplaceAll.Size = new System.Drawing.Size(150, 20);
            this.chkReplaceAll.TabIndex = 6;
            this.chkReplaceAll.Text = "Replace all";
            this.chkReplaceAll.UseSelectable = true;
            this.chkReplaceAll.UseStyleColors = true;
            // 
            // btnReplace
            // 
            this.btnReplace.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnReplace.Location = new System.Drawing.Point(213, 220);
            this.btnReplace.Name = "btnReplace";
            this.btnReplace.Size = new System.Drawing.Size(75, 30);
            this.btnReplace.TabIndex = 7;
            this.btnReplace.Text = "Replace";
            this.btnReplace.UseSelectable = true;
            this.btnReplace.UseStyleColors = true;
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(308, 220);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 30);
            this.btnCancel.TabIndex = 8;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseSelectable = true;
            this.btnCancel.UseStyleColors = true;
            // 
            // ReplaceDialog
            // 
            this.AcceptButton = this.btnReplace;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(404, 280);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnReplace);
            this.Controls.Add(this.chkReplaceAll);
            this.Controls.Add(this.chkWholeWord);
            this.Controls.Add(this.chkMatchCase);
            this.Controls.Add(this.txtReplace);
            this.Controls.Add(this.lblReplace);
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(this.lblSearch);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ReplaceDialog";
            this.PoisonBorderStyle = ReaLTaiizor.Enum.Poison.FormBorderStyle.FixedSingle;
            this.ShadowType = ReaLTaiizor.Enum.Poison.FormShadowType.DropShadow;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Replace";
            this.ResumeLayout(false);

        }
    }
}

