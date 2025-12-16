namespace T7CompilerGUI.Controls
{
    partial class AvalonEditWrapper
    {
        // Note: components field removed - not needed as no components require disposal tracking

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.elementHost = new System.Windows.Forms.Integration.ElementHost();
            this.textEditor = new ICSharpCode.AvalonEdit.TextEditor();
            this.SuspendLayout();
            // 
            // elementHost
            // 
            this.elementHost.BackColor = System.Drawing.Color.Transparent;
            this.elementHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.elementHost.Location = new System.Drawing.Point(0, 0);
            this.elementHost.Name = "elementHost";
            this.elementHost.Size = new System.Drawing.Size(216, 193);
            this.elementHost.TabIndex = 0;
            this.elementHost.Child = this.textEditor;
            // 
            // AvalonEditWrapper
            // 
            this.Controls.Add(this.elementHost);
            this.Name = "AvalonEditWrapper";
            this.Size = new System.Drawing.Size(216, 193);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Integration.ElementHost elementHost;
        private ICSharpCode.AvalonEdit.TextEditor textEditor;
    }
}
