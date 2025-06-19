namespace SMC.UI.Core.Controls
{
    partial class CThemedTextbox
    {
        private System.ComponentModel.IContainer components = null;

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
            components = new System.ComponentModel.Container();
            this.SuspendLayout();

            // TextBox properties
            this.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.ForeColor = System.Drawing.Color.White;
            this.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);

            this.ResumeLayout(false);
        }
    }
}