using DebugCompiler.UI.Core.Singletons;

namespace DebugCompiler.UI.Core.Controls
{
    partial class CTitleBar
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }

                MouseDown -= MouseDown_Drag;
                UIThemeManager.ThemeChanged -= OnThemeChanged_Implementation;

                if (TitleLabel != null)
                {
                    TitleLabel.MouseDown -= MouseDown_Drag;
                    UIThemeManager.UnregisterControl(TitleLabel);
                }

                if (MinimizeButton != null)
                {
                    MinimizeButton.Click -= MinimizeButton_Click;
                    UIThemeManager.UnregisterControl(MinimizeButton);
                }

                if (MaximizeButton != null)
                {
                    MaximizeButton.Click -= MaximizeButton_Click;
                    UIThemeManager.UnregisterControl(MaximizeButton);
                }

                if (CloseButton != null)
                {
                    CloseButton.Click -= CloseButton_Click;
                    UIThemeManager.UnregisterControl(CloseButton);
                }
                UIThemeManager.UnregisterControl(this);
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this.TitleLabel = new System.Windows.Forms.Label();
            this.MinimizeButton = new System.Windows.Forms.Button();
            this.MaximizeButton = new System.Windows.Forms.Button();
            this.CloseButton = new System.Windows.Forms.Button();
            this.ExitButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // TitleLabel
            // 
            this.TitleLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.TitleLabel.AutoSize = true;
            this.TitleLabel.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.TitleLabel.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.TitleLabel.Location = new System.Drawing.Point(9, 8);
            this.TitleLabel.Margin = new System.Windows.Forms.Padding(0);
            this.TitleLabel.Name = "TitleLabel";
            this.TitleLabel.Size = new System.Drawing.Size(32, 17);
            this.TitleLabel.TabIndex = 0;
            this.TitleLabel.Text = "Title";
            this.TitleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // MinimizeButton
            // 
            this.MinimizeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.MinimizeButton.FlatAppearance.BorderSize = 0;
            this.MinimizeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.MinimizeButton.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.MinimizeButton.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.MinimizeButton.Location = new System.Drawing.Point(204, 0);
            this.MinimizeButton.Margin = new System.Windows.Forms.Padding(0);
            this.MinimizeButton.Name = "MinimizeButton";
            this.MinimizeButton.Size = new System.Drawing.Size(32, 32);
            this.MinimizeButton.TabIndex = 1;
            this.MinimizeButton.Text = "_";
            this.MinimizeButton.UseVisualStyleBackColor = false;
            this.MinimizeButton.Click += new System.EventHandler(this.MinimizeButton_Click);
            // 
            // MaximizeButton
            // 
            this.MaximizeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.MaximizeButton.FlatAppearance.BorderSize = 0;
            this.MaximizeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.MaximizeButton.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.MaximizeButton.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.MaximizeButton.Location = new System.Drawing.Point(236, 0);
            this.MaximizeButton.Margin = new System.Windows.Forms.Padding(0);
            this.MaximizeButton.Name = "MaximizeButton";
            this.MaximizeButton.Size = new System.Drawing.Size(32, 32);
            this.MaximizeButton.TabIndex = 2;
            this.MaximizeButton.Text = "□";
            this.MaximizeButton.UseVisualStyleBackColor = false;
            this.MaximizeButton.Click += new System.EventHandler(this.MaximizeButton_Click);
            // 
            // CloseButton
            // 
            this.CloseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.CloseButton.FlatAppearance.BorderSize = 0;
            this.CloseButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.CloseButton.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.CloseButton.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.CloseButton.Location = new System.Drawing.Point(268, 0);
            this.CloseButton.Margin = new System.Windows.Forms.Padding(0);
            this.CloseButton.Name = "CloseButton";
            this.CloseButton.Size = new System.Drawing.Size(32, 32);
            this.CloseButton.TabIndex = 3;
            this.CloseButton.Text = "×";
            this.CloseButton.UseVisualStyleBackColor = false;
            this.CloseButton.Click += new System.EventHandler(this.CloseButton_Click);
            // 
            // ExitButton
            // 
            this.ExitButton.Location = new System.Drawing.Point(0, 0);
            this.ExitButton.Name = "ExitButton";
            this.ExitButton.Size = new System.Drawing.Size(75, 23);
            this.ExitButton.TabIndex = 0;
            // 
            // CTitleBar
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(36)))));
            this.Controls.Add(this.CloseButton);
            this.Controls.Add(this.MaximizeButton);
            this.Controls.Add(this.MinimizeButton);
            this.Controls.Add(this.TitleLabel);
            this.Name = "CTitleBar";
            this.Size = new System.Drawing.Size(300, 32);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.Label TitleLabel;
        public System.Windows.Forms.Button MinimizeButton;
        public System.Windows.Forms.Button MaximizeButton;
        public System.Windows.Forms.Button CloseButton;
        public System.Windows.Forms.Button ExitButton;
    }
}