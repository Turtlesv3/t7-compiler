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
            this.panel.Size = new System.Drawing.Size(526, 335);
            this.panel.Style = ReaLTaiizor.Enum.Poison.ColorStyle.Default;
            this.panel.TabIndex = 0;
            this.panel.Theme = ReaLTaiizor.Enum.Poison.ThemeStyle.Default;
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
            this.buttonPanel.Location = new System.Drawing.Point(20, 395);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Padding = new System.Windows.Forms.Padding(10, 10, 20, 10);
            this.buttonPanel.Size = new System.Drawing.Size(526, 50);
            this.buttonPanel.Style = ReaLTaiizor.Enum.Poison.ColorStyle.Default;
            this.buttonPanel.TabIndex = 1;
            this.buttonPanel.Theme = ReaLTaiizor.Enum.Poison.ThemeStyle.Default;
            this.buttonPanel.VerticalScrollbarBarColor = true;
            this.buttonPanel.VerticalScrollbarHighlightOnWheel = false;
            this.buttonPanel.VerticalScrollbarSize = 10;
            // 
            // btnReset
            // 
            this.btnReset.Location = new System.Drawing.Point(10, 10);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(122, 30);
            this.btnReset.Style = ReaLTaiizor.Enum.Poison.ColorStyle.Default;
            this.btnReset.TabIndex = 2;
            this.btnReset.Text = "Reset to Defaults";
            this.btnReset.Theme = ReaLTaiizor.Enum.Poison.ThemeStyle.Default;
            this.btnReset.UseSelectable = true;
            this.btnReset.UseStyleColors = true;
            // 
            // btnClear
            // 
            this.btnClear.Location = new System.Drawing.Point(10, 10);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(100, 30);
            this.btnClear.Style = ReaLTaiizor.Enum.Poison.ColorStyle.Default;
            this.btnClear.TabIndex = 3;
            this.btnClear.Text = "Clear All";
            this.btnClear.Theme = ReaLTaiizor.Enum.Poison.ThemeStyle.Default;
            this.btnClear.UseSelectable = true;
            this.btnClear.UseStyleColors = true;
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(10, 10);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 30);
            this.btnCancel.Style = ReaLTaiizor.Enum.Poison.ColorStyle.Default;
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.Theme = ReaLTaiizor.Enum.Poison.ThemeStyle.Default;
            this.btnCancel.UseSelectable = true;
            this.btnCancel.UseStyleColors = true;
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(10, 10);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(100, 30);
            this.btnOK.Style = ReaLTaiizor.Enum.Poison.ColorStyle.Default;
            this.btnOK.TabIndex = 5;
            this.btnOK.Text = "OK";
            this.btnOK.Theme = ReaLTaiizor.Enum.Poison.ThemeStyle.Default;
            this.btnOK.UseSelectable = true;
            this.btnOK.UseStyleColors = true;
            // 
            // KeybindDialog
            // 
            this.ClientSize = new System.Drawing.Size(566, 465);
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

