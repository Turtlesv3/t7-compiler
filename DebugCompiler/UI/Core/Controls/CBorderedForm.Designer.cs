using DebugCompiler.UI.Core.Singletons;

namespace DebugCompiler.UI.Core.Controls
{
    partial class CBorderedForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                UIThemeManager.ThemeChanged -= OnThemeChanged_Implementation;
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this.MainPanel = new System.Windows.Forms.Panel();
            this.DesignerContents = new System.Windows.Forms.Panel();
            this.TitleBar = new DebugCompiler.UI.Core.Controls.CTitleBar();
            this.MainPanel.SuspendLayout();
            this.SuspendLayout();

            // MainPanel
            this.MainPanel.BackColor = System.Drawing.Color.FromArgb(28, 28, 28);
            this.MainPanel.Controls.Add(this.DesignerContents);
            this.MainPanel.Controls.Add(this.TitleBar);
            this.MainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainPanel.Padding = new System.Windows.Forms.Padding(5);

            // DesignerContents
            this.DesignerContents.BackColor = System.Drawing.Color.Transparent;
            this.DesignerContents.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DesignerContents.Location = new System.Drawing.Point(5, 37);
            this.DesignerContents.Padding = new System.Windows.Forms.Padding(3);

            // TitleBar
            this.TitleBar.BackColor = System.Drawing.Color.FromArgb(36, 36, 36);
            this.TitleBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.TitleBar.Location = new System.Drawing.Point(5, 5);

            // CBorderedForm
            this.Controls.Add(this.MainPanel);
            this.MainPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel MainPanel;
        private CTitleBar TitleBar;
        private System.Windows.Forms.Panel DesignerContents;
    }
}