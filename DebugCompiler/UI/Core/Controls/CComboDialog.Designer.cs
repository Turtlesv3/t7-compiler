namespace SMC.UI.Core.Controls
{
    partial class CComboDialog
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
            this.InnerForm = new Refract.UI.Core.Controls.CBorderedForm();
            this.SelectButton = new System.Windows.Forms.Button();
            this.cComboBox1 = new SMC.UI.Core.Controls.CComboBox();
            this.SuspendLayout();

            // InnerForm
            this.InnerForm.BackColor = System.Drawing.Color.DodgerBlue;
            this.InnerForm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.InnerForm.Location = new System.Drawing.Point(0, 0);
            this.InnerForm.Name = "InnerForm";
            this.InnerForm.Size = new System.Drawing.Size(273, 73);
            this.InnerForm.TabIndex = 0;
            this.InnerForm.TitleBarTitle = "Combo Dialog";
            this.InnerForm.UseTitleBar = true;

            // SelectButton
            this.SelectButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.SelectButton.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.SelectButton.Location = new System.Drawing.Point(200, 6);
            this.SelectButton.Name = "SelectButton";
            this.SelectButton.Size = new System.Drawing.Size(63, 25);
            this.SelectButton.TabIndex = 1;
            this.SelectButton.Text = "Select";
            this.SelectButton.UseVisualStyleBackColor = true;

            // cComboBox1
            this.cComboBox1.FormattingEnabled = true;
            this.cComboBox1.Location = new System.Drawing.Point(6, 6);
            this.cComboBox1.Name = "cComboBox1";
            this.cComboBox1.Size = new System.Drawing.Size(188, 25);
            this.cComboBox1.TabIndex = 2;

            // CComboDialog
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(273, 73);
            this.Controls.Add(this.InnerForm);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "CComboDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Combo Dialog";
            this.ResumeLayout(false);
        }

        private Refract.UI.Core.Controls.CBorderedForm InnerForm;
        private System.Windows.Forms.Button SelectButton;
        private CComboBox cComboBox1;
    }
}