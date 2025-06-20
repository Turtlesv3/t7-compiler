namespace Refract.UI.Core.Controls
{
    partial class CBorderedForm
    {
        private System.ComponentModel.IContainer components = null;
        protected System.Windows.Forms.Panel MainPanel;
        protected CTitleBar TitleBar;

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
            this.MainPanel = new System.Windows.Forms.Panel();
            this.ControlContents = new System.Windows.Forms.Panel();
            this.TitleBar = new Refract.UI.Core.Controls.CTitleBar();
            this.MainPanel.SuspendLayout();
            this.SuspendLayout();

            // MainPanel
            this.MainPanel.Controls.Add(this.ControlContents);
            this.MainPanel.Controls.Add(this.TitleBar);
            this.MainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainPanel.Location = new System.Drawing.Point(2, 2);
            this.MainPanel.Name = "MainPanel";
            this.MainPanel.Size = new System.Drawing.Size(800, 600);
            this.MainPanel.TabIndex = 0;

            // ControlContents
            this.ControlContents.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ControlContents.Location = new System.Drawing.Point(0, 32);
            this.ControlContents.Name = "ControlContents";
            this.ControlContents.Size = new System.Drawing.Size(800, 568);
            this.ControlContents.TabIndex = 1;

            // TitleBar
            this.TitleBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(36)))));
            this.TitleBar.DisableDrag = false;
            this.TitleBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.TitleBar.Location = new System.Drawing.Point(0, 0);
            this.TitleBar.Name = "TitleBar";
            this.TitleBar.Size = new System.Drawing.Size(800, 32);
            this.TitleBar.TabIndex = 0;

            // CBorderedForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.DodgerBlue;
            this.ClientSize = new System.Drawing.Size(804, 604);
            this.Controls.Add(this.MainPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "CBorderedForm";
            this.MainPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}