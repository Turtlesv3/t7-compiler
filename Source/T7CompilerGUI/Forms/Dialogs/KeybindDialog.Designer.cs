using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Enum.Poison;

namespace T7CompilerGUI.Forms.Dialogs
{
    partial class KeybindDialog
    {
        protected System.ComponentModel.IContainer components = null;
        
        // Control declarations - these will be editable in the designer
        private PoisonPanel panel;
        private PoisonPanel buttonPanel;
        private PoisonButton btnReset;
        private PoisonButton btnClear;
        private PoisonButton btnCancel;
        private PoisonButton btnOK;

        private void InitializeComponent()
        {
            this.panel = new ReaLTaiizor.Controls.PoisonPanel();
            this.buttonPanel = new ReaLTaiizor.Controls.PoisonPanel();
            this.btnReset = new ReaLTaiizor.Controls.PoisonButton();
            this.btnClear = new ReaLTaiizor.Controls.PoisonButton();
            this.btnCancel = new ReaLTaiizor.Controls.PoisonButton();
            this.btnOK = new ReaLTaiizor.Controls.PoisonButton();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel
            // 
            this.panel.AutoScroll = true;
            this.panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel.HorizontalScrollbar = true;
            this.panel.HorizontalScrollbarBarColor = true;
            this.panel.HorizontalScrollbarHighlightOnWheel = false;
            this.panel.HorizontalScrollbarSize = 10;
            this.panel.Location = new System.Drawing.Point(20, 60);
            this.panel.Name = "panel";
            this.panel.Padding = new System.Windows.Forms.Padding(20, 20, 30, 20);
            this.panel.Size = new System.Drawing.Size(534, 399);
            this.panel.TabIndex = 0;
            this.panel.VerticalScrollbar = true;
            this.panel.VerticalScrollbarBarColor = true;
            this.panel.VerticalScrollbarHighlightOnWheel = false;
            this.panel.VerticalScrollbarSize = 10;
            // 
            // buttonPanel
            // 
            this.buttonPanel.Controls.Add(this.btnReset);
            this.buttonPanel.Controls.Add(this.btnClear);
            this.buttonPanel.Controls.Add(this.btnCancel);
            this.buttonPanel.Controls.Add(this.btnOK);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.buttonPanel.HorizontalScrollbarBarColor = true;
            this.buttonPanel.HorizontalScrollbarHighlightOnWheel = false;
            this.buttonPanel.HorizontalScrollbarSize = 10;
            this.buttonPanel.Location = new System.Drawing.Point(20, 459);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Padding = new System.Windows.Forms.Padding(10, 10, 20, 10);
            this.buttonPanel.Size = new System.Drawing.Size(534, 50);
            this.buttonPanel.TabIndex = 1;
            this.buttonPanel.VerticalScrollbarBarColor = true;
            this.buttonPanel.VerticalScrollbarHighlightOnWheel = false;
            this.buttonPanel.VerticalScrollbarSize = 10;
            // 
            // btnReset
            // 
            this.btnReset.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnReset.Location = new System.Drawing.Point(10, 10);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(122, 30);
            this.btnReset.TabIndex = 2;
            this.btnReset.Text = "Reset to Defaults";
            this.btnReset.UseSelectable = true;
            this.btnReset.UseStyleColors = true;
            this.btnReset.Click += new System.EventHandler(this.BtnReset_Click);
            // 
            // btnClear
            // 
            this.btnClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClear.Location = new System.Drawing.Point(138, 10);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(100, 30);
            this.btnClear.TabIndex = 3;
            this.btnClear.Text = "Clear All";
            this.btnClear.UseSelectable = true;
            this.btnClear.UseStyleColors = true;
            this.btnClear.Click += new System.EventHandler(this.BtnClear_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.Location = new System.Drawing.Point(316, 10);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 30);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseSelectable = true;
            this.btnCancel.UseStyleColors = true;
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Location = new System.Drawing.Point(422, 10);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(100, 30);
            this.btnOK.TabIndex = 5;
            this.btnOK.Text = "OK";
            this.btnOK.UseSelectable = true;
            this.btnOK.UseStyleColors = true;
            this.btnOK.Click += new System.EventHandler(this.BtnOK_Click);
            // 
            // KeybindDialog
            // 
            this.ClientSize = new System.Drawing.Size(574, 529);
            this.Controls.Add(this.panel);
            this.Controls.Add(this.buttonPanel);
            this.MinimumSize = new System.Drawing.Size(500, 400);
            this.Name = "KeybindDialog";
            this.PoisonBorderStyle = ReaLTaiizor.Enum.Poison.FormBorderStyle.FixedSingle;
            this.ShadowType = ReaLTaiizor.Enum.Poison.FormShadowType.DropShadow;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Keyboard Shortcuts";
            this.buttonPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}

