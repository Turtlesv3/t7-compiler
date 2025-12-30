using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Enum.Poison;
using ReaLTaiizor.Forms;

namespace T7CompilerGUI.Forms.Dialogs
{
    partial class SearchResultsDialog
    {
        private System.ComponentModel.IContainer components = null;

        private PoisonLabel lblResultsCount;
        private ReaLTaiizor.Controls.PoisonListView lvwResults;
        private PoisonButton btnGoTo;
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
            this.lblResultsCount = new ReaLTaiizor.Controls.PoisonLabel();
            this.lvwResults = new ReaLTaiizor.Controls.PoisonListView();
            this.btnGoTo = new ReaLTaiizor.Controls.PoisonButton();
            this.btnCancel = new ReaLTaiizor.Controls.PoisonButton();
            this.SuspendLayout();
            // 
            // lblResultsCount
            // 
            this.lblResultsCount.Location = new System.Drawing.Point(23, 53);
            this.lblResultsCount.Name = "lblResultsCount";
            this.lblResultsCount.Size = new System.Drawing.Size(550, 20);
            this.lblResultsCount.TabIndex = 0;
            this.lblResultsCount.Text = "Found 0 result(s)";
            this.lblResultsCount.UseStyleColors = true;
            // 
            // lvwResults
            // 
            this.lvwResults.FullRowSelect = true;
            this.lvwResults.GridLines = true;
            this.lvwResults.HideSelection = false;
            this.lvwResults.Location = new System.Drawing.Point(23, 76);
            this.lvwResults.MultiSelect = false;
            this.lvwResults.Name = "lvwResults";
            this.lvwResults.Size = new System.Drawing.Size(550, 277);
            this.lvwResults.TabIndex = 1;
            this.lvwResults.UseCompatibleStateImageBehavior = false;
            this.lvwResults.UseStyleColors = true;
            this.lvwResults.View = System.Windows.Forms.View.Details;
            this.lvwResults.DoubleClick += new System.EventHandler(this.LvwResults_DoubleClick);
            // 
            // btnGoTo
            // 
            this.btnGoTo.Location = new System.Drawing.Point(418, 360);
            this.btnGoTo.Name = "btnGoTo";
            this.btnGoTo.Size = new System.Drawing.Size(75, 30);
            this.btnGoTo.TabIndex = 2;
            this.btnGoTo.Text = "Go To";
            this.btnGoTo.UseSelectable = true;
            this.btnGoTo.UseStyleColors = true;
            this.btnGoTo.Click += new System.EventHandler(this.BtnGoTo_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(498, 360);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 30);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseSelectable = true;
            this.btnCancel.UseStyleColors = true;
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // SearchResultsDialog
            // 
            this.AcceptButton = this.btnGoTo;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(600, 410);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnGoTo);
            this.Controls.Add(this.lvwResults);
            this.Controls.Add(this.lblResultsCount);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SearchResultsDialog";
            this.PoisonBorderStyle = ReaLTaiizor.Enum.Poison.FormBorderStyle.FixedSingle;
            this.ShadowType = ReaLTaiizor.Enum.Poison.FormShadowType.DropShadow;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Search Results";
            this.ResumeLayout(false);

        }
    }
}

