using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Enum.Poison;
using ReaLTaiizor.Forms;

namespace T7CompilerGUI.Forms.Dialogs
{
    partial class SearchDialog
    {
        private System.ComponentModel.IContainer components = null;

        private PoisonLabel lblSearch;
        private PoisonTextBox txtSearch;
        private PoisonCheckBox chkMatchCase;
        private PoisonCheckBox chkWholeWord;
        private PoisonButton btnFind;
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
            this.chkMatchCase = new ReaLTaiizor.Controls.PoisonCheckBox();
            this.chkWholeWord = new ReaLTaiizor.Controls.PoisonCheckBox();
            this.btnFind = new ReaLTaiizor.Controls.PoisonButton();
            this.btnCancel = new ReaLTaiizor.Controls.PoisonButton();
            this.SuspendLayout();
            // 
            // lblSearch
            // 
            this.lblSearch.Location = new System.Drawing.Point(23, 50);
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
            this.txtSearch.Location = new System.Drawing.Point(23, 75);
            this.txtSearch.MaxLength = 32767;
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.PasswordChar = '\0';
            this.txtSearch.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.txtSearch.SelectedText = "";
            this.txtSearch.SelectionLength = 0;
            this.txtSearch.SelectionStart = 0;
            this.txtSearch.ShortcutsEnabled = true;
            this.txtSearch.Size = new System.Drawing.Size(350, 23);
            this.txtSearch.TabIndex = 1;
            this.txtSearch.UseSelectable = true;
            this.txtSearch.UseStyleColors = true;
            this.txtSearch.WaterMarkColor = System.Drawing.Color.FromArgb(((int)(((byte)(109)))), ((int)(((byte)(109)))), ((int)(((byte)(109)))));
            this.txtSearch.WaterMarkFont = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Pixel);
            // 
            // chkMatchCase
            // 
            this.chkMatchCase.Location = new System.Drawing.Point(23, 105);
            this.chkMatchCase.Name = "chkMatchCase";
            this.chkMatchCase.Size = new System.Drawing.Size(150, 20);
            this.chkMatchCase.TabIndex = 2;
            this.chkMatchCase.Text = "Match case";
            this.chkMatchCase.UseSelectable = true;
            this.chkMatchCase.UseStyleColors = true;
            // 
            // chkWholeWord
            // 
            this.chkWholeWord.Location = new System.Drawing.Point(183, 105);
            this.chkWholeWord.Name = "chkWholeWord";
            this.chkWholeWord.Size = new System.Drawing.Size(150, 20);
            this.chkWholeWord.TabIndex = 3;
            this.chkWholeWord.Text = "Whole word";
            this.chkWholeWord.UseSelectable = true;
            this.chkWholeWord.UseStyleColors = true;
            // 
            // btnFind
            // 
            this.btnFind.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnFind.Location = new System.Drawing.Point(213, 140);
            this.btnFind.Name = "btnFind";
            this.btnFind.Size = new System.Drawing.Size(75, 30);
            this.btnFind.TabIndex = 4;
            this.btnFind.Text = "Find";
            this.btnFind.UseSelectable = true;
            this.btnFind.UseStyleColors = true;
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(298, 140);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 30);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseSelectable = true;
            this.btnCancel.UseStyleColors = true;
            // 
            // SearchDialog
            // 
            this.AcceptButton = this.btnFind;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(392, 191);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnFind);
            this.Controls.Add(this.chkWholeWord);
            this.Controls.Add(this.chkMatchCase);
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(this.lblSearch);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SearchDialog";
            this.PoisonBorderStyle = ReaLTaiizor.Enum.Poison.FormBorderStyle.FixedSingle;
            this.ShadowType = ReaLTaiizor.Enum.Poison.FormShadowType.DropShadow;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Search";
            this.ResumeLayout(false);

        }
    }
}

