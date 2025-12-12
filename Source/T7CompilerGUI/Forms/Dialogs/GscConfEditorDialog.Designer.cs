using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Enum.Poison;
using ReaLTaiizor.Extension.Poison;

namespace T7CompilerGUI.Forms.Dialogs
{
    partial class GscConfEditorDialog
    {
        private System.ComponentModel.IContainer components = null;
        
        // Control declarations - these will be editable in the designer
        private PoisonPanel mainPanel;
        private PoisonLabel lblFilePath;
        private PoisonLabel lblModes;
        private PoisonRadioButton rdoMP;
        private PoisonRadioButton rdoZM;
        private PoisonRadioButton rdoSP;
        private PoisonLabel lblOtherSymbols;
        private System.Windows.Forms.CheckedListBox lstOtherSymbols;
        private PoisonLabel lblInfo;
        private PoisonPanel buttonPanel;
        private PoisonButton btnCancel;
        private PoisonButton btnOK;

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
            this.mainPanel = new ReaLTaiizor.Controls.PoisonPanel();
            this.lblFilePath = new ReaLTaiizor.Controls.PoisonLabel();
            this.lblModes = new ReaLTaiizor.Controls.PoisonLabel();
            this.rdoMP = new ReaLTaiizor.Controls.PoisonRadioButton();
            this.rdoZM = new ReaLTaiizor.Controls.PoisonRadioButton();
            this.rdoSP = new ReaLTaiizor.Controls.PoisonRadioButton();
            this.lblOtherSymbols = new ReaLTaiizor.Controls.PoisonLabel();
            this.lstOtherSymbols = new System.Windows.Forms.CheckedListBox();
            this.lblInfo = new ReaLTaiizor.Controls.PoisonLabel();
            this.buttonPanel = new ReaLTaiizor.Controls.PoisonPanel();
            this.btnCancel = new ReaLTaiizor.Controls.PoisonButton();
            this.btnOK = new ReaLTaiizor.Controls.PoisonButton();
            this.mainPanel.SuspendLayout();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainPanel
            // 
            this.mainPanel.AutoScroll = true;
            this.mainPanel.Controls.Add(this.lblFilePath);
            this.mainPanel.Controls.Add(this.lblModes);
            this.mainPanel.Controls.Add(this.rdoMP);
            this.mainPanel.Controls.Add(this.rdoZM);
            this.mainPanel.Controls.Add(this.rdoSP);
            this.mainPanel.Controls.Add(this.lblOtherSymbols);
            this.mainPanel.Controls.Add(this.lstOtherSymbols);
            this.mainPanel.Controls.Add(this.lblInfo);
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.HorizontalScrollbar = true;
            this.mainPanel.HorizontalScrollbarBarColor = true;
            this.mainPanel.HorizontalScrollbarHighlightOnWheel = false;
            this.mainPanel.HorizontalScrollbarSize = 10;
            this.mainPanel.Location = new System.Drawing.Point(20, 60);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Padding = new System.Windows.Forms.Padding(20, 15, 20, 20);
            this.mainPanel.Size = new System.Drawing.Size(293, 304);
            this.mainPanel.TabIndex = 0;
            this.mainPanel.VerticalScrollbar = true;
            this.mainPanel.VerticalScrollbarBarColor = true;
            this.mainPanel.VerticalScrollbarHighlightOnWheel = false;
            this.mainPanel.VerticalScrollbarSize = 10;
            // 
            // lblFilePath
            // 
            this.lblFilePath.AutoSize = true;
            this.lblFilePath.FontSize = ReaLTaiizor.Extension.Poison.PoisonLabelSize.Small;
            this.lblFilePath.FontWeight = ReaLTaiizor.Extension.Poison.PoisonLabelWeight.Regular;
            this.lblFilePath.Location = new System.Drawing.Point(23, 10);
            this.lblFilePath.Name = "lblFilePath";
            this.lblFilePath.Size = new System.Drawing.Size(67, 15);
            this.lblFilePath.TabIndex = 2;
            this.lblFilePath.Text = "Config File:";
            this.lblFilePath.UseStyleColors = true;
            // 
            // lblModes
            // 
            this.lblModes.AutoSize = true;
            this.lblModes.FontWeight = ReaLTaiizor.Extension.Poison.PoisonLabelWeight.Bold;
            this.lblModes.Location = new System.Drawing.Point(23, 28);
            this.lblModes.Name = "lblModes";
            this.lblModes.Size = new System.Drawing.Size(180, 19);
            this.lblModes.TabIndex = 3;
            this.lblModes.Text = "Game Mode (Select One):";
            this.lblModes.UseStyleColors = true;
            // 
            // rdoMP
            // 
            this.rdoMP.AutoSize = true;
            this.rdoMP.Location = new System.Drawing.Point(40, 50);
            this.rdoMP.Name = "rdoMP";
            this.rdoMP.Size = new System.Drawing.Size(112, 15);
            this.rdoMP.TabIndex = 4;
            this.rdoMP.Text = "Multiplayer (MP)";
            this.rdoMP.UseSelectable = true;
            // 
            // rdoZM
            // 
            this.rdoZM.AutoSize = true;
            this.rdoZM.Location = new System.Drawing.Point(40, 71);
            this.rdoZM.Name = "rdoZM";
            this.rdoZM.Size = new System.Drawing.Size(98, 15);
            this.rdoZM.TabIndex = 5;
            this.rdoZM.Text = "Zombies (ZM)";
            this.rdoZM.UseSelectable = true;
            // 
            // rdoSP
            // 
            this.rdoSP.AutoSize = true;
            this.rdoSP.Location = new System.Drawing.Point(40, 92);
            this.rdoSP.Name = "rdoSP";
            this.rdoSP.Size = new System.Drawing.Size(180, 15);
            this.rdoSP.TabIndex = 6;
            this.rdoSP.Text = "Single Player / Campaign (SP)";
            this.rdoSP.UseSelectable = true;
            // 
            // lblOtherSymbols
            // 
            this.lblOtherSymbols.AutoSize = true;
            this.lblOtherSymbols.FontWeight = ReaLTaiizor.Extension.Poison.PoisonLabelWeight.Bold;
            this.lblOtherSymbols.Location = new System.Drawing.Point(23, 110);
            this.lblOtherSymbols.Name = "lblOtherSymbols";
            this.lblOtherSymbols.Size = new System.Drawing.Size(144, 19);
            this.lblOtherSymbols.TabIndex = 7;
            this.lblOtherSymbols.Text = "Additional Symbols:";
            this.lblOtherSymbols.UseStyleColors = true;
            // 
            // lstOtherSymbols
            // 
            this.lstOtherSymbols.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lstOtherSymbols.CheckOnClick = true;
            this.lstOtherSymbols.Location = new System.Drawing.Point(23, 132);
            this.lstOtherSymbols.Name = "lstOtherSymbols";
            this.lstOtherSymbols.Size = new System.Drawing.Size(247, 137);
            this.lstOtherSymbols.TabIndex = 8;
            // 
            // lblInfo
            // 
            this.lblInfo.FontSize = ReaLTaiizor.Extension.Poison.PoisonLabelSize.Small;
            this.lblInfo.FontWeight = ReaLTaiizor.Extension.Poison.PoisonLabelWeight.Regular;
            this.lblInfo.Location = new System.Drawing.Point(4, 272);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(272, 32);
            this.lblInfo.TabIndex = 9;
            this.lblInfo.Text = "Select a game mode and any additional symbols to include in your build.";
            this.lblInfo.UseStyleColors = true;
            this.lblInfo.WrapToLine = true;
            // 
            // buttonPanel
            // 
            this.buttonPanel.Controls.Add(this.btnCancel);
            this.buttonPanel.Controls.Add(this.btnOK);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.buttonPanel.HorizontalScrollbarBarColor = true;
            this.buttonPanel.HorizontalScrollbarHighlightOnWheel = false;
            this.buttonPanel.HorizontalScrollbarSize = 10;
            this.buttonPanel.Location = new System.Drawing.Point(20, 364);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Padding = new System.Windows.Forms.Padding(10);
            this.buttonPanel.Size = new System.Drawing.Size(293, 47);
            this.buttonPanel.TabIndex = 1;
            this.buttonPanel.VerticalScrollbarBarColor = true;
            this.buttonPanel.VerticalScrollbarHighlightOnWheel = false;
            this.buttonPanel.VerticalScrollbarSize = 10;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.Location = new System.Drawing.Point(23, 6);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 30);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseSelectable = true;
            this.btnCancel.UseStyleColors = true;
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Location = new System.Drawing.Point(170, 6);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(100, 30);
            this.btnOK.TabIndex = 3;
            this.btnOK.Text = "Save";
            this.btnOK.UseSelectable = true;
            this.btnOK.UseStyleColors = true;
            this.btnOK.Click += new System.EventHandler(this.BtnOK_Click);
            // 
            // GscConfEditorDialog
            // 
            this.ClientSize = new System.Drawing.Size(333, 431);
            this.Controls.Add(this.mainPanel);
            this.Controls.Add(this.buttonPanel);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(333, 431);
            this.Name = "GscConfEditorDialog";
            this.PoisonBorderStyle = ReaLTaiizor.Enum.Poison.FormBorderStyle.FixedSingle;
            this.ShadowType = ReaLTaiizor.Enum.Poison.FormShadowType.DropShadow;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "GSC Configuration Editor";
            this.mainPanel.ResumeLayout(false);
            this.mainPanel.PerformLayout();
            this.buttonPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}

