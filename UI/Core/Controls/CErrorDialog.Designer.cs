namespace SMC.UI.Core.Controls
{
    partial class CErrorDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.RichTextBox ErrorRTB;
        private System.Windows.Forms.Button AcceptButton;

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
            this.ErrorRTB = new System.Windows.Forms.RichTextBox();
            this.AcceptButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // ErrorRTB
            // 
            this.ErrorRTB.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ErrorRTB.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.ErrorRTB.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.ErrorRTB.ForeColor = System.Drawing.Color.White;
            this.ErrorRTB.Location = new System.Drawing.Point(12, 12);
            this.ErrorRTB.Name = "ErrorRTB";
            this.ErrorRTB.ReadOnly = true;
            this.ErrorRTB.Size = new System.Drawing.Size(360, 120);
            this.ErrorRTB.TabIndex = 0;
            this.ErrorRTB.Text = "";
            // 
            // AcceptButton
            // 
            this.AcceptButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.AcceptButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.AcceptButton.Location = new System.Drawing.Point(297, 138);
            this.AcceptButton.Name = "AcceptButton";
            this.AcceptButton.Size = new System.Drawing.Size(75, 23);
            this.AcceptButton.TabIndex = 1;
            this.AcceptButton.Text = "OK";
            this.AcceptButton.UseVisualStyleBackColor = true;
            this.AcceptButton.Click += new System.EventHandler(this.AcceptButton_Click);
            // 
            // CErrorDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.ClientSize = new System.Drawing.Size(384, 173);
            this.Controls.Add(this.AcceptButton);
            this.Controls.Add(this.ErrorRTB);
            this.ForeColor = System.Drawing.Color.White;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CErrorDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Error";
            this.ResumeLayout(false);

        }
    }
}