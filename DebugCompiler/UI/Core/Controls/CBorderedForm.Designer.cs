// CBorderedForm.Designer.cs
namespace DebugCompiler.UI.Core.Controls
{
    partial class CBorderedForm
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this._controlContents = new System.Windows.Forms.Panel();
            this._titleBar = new DebugCompiler.UI.Core.Controls.CTitleBar();
            this.SuspendLayout();

            // _controlContents
            this._controlContents.BackColor = System.Drawing.Color.Transparent;
            this._controlContents.Dock = System.Windows.Forms.DockStyle.Fill;
            this._controlContents.Location = new System.Drawing.Point(0, 32);
            this._controlContents.Name = "_controlContents";
            this._controlContents.Size = new System.Drawing.Size(300, 268);
            this._controlContents.TabIndex = 0;

            // _titleBar
            this._titleBar.BackColor = System.Drawing.Color.FromArgb(36, 36, 36);
            this._titleBar.Dock = System.Windows.Forms.DockStyle.Top;
            this._titleBar.Location = new System.Drawing.Point(0, 0);
            this._titleBar.Name = "_titleBar";
            this._titleBar.Size = new System.Drawing.Size(300, 32);
            this._titleBar.TabIndex = 1;

            // CBorderedForm
            this.Controls.Add(this._controlContents);
            this.Controls.Add(this._titleBar);
            this.Name = "CBorderedForm";
            this.Size = new System.Drawing.Size(300, 300);
            this.ResumeLayout(false);
        }

        #endregion
    }
}