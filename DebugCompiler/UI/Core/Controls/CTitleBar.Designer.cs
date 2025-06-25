using DebugCompiler.UI.Core.Singletons;

namespace DebugCompiler.UI.Core.Controls
{
    public partial class CTitleBar
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Clean up event handlers
                MouseDown -= MouseDown_Drag;
                UIThemeManager.ThemeChanged -= OnThemeChanged_Implementation;

                // Clean up child controls
                if (TitleLabel != null)
                {
                    TitleLabel.MouseDown -= MouseDown_Drag;
                    UIThemeManager.UnregisterControl(TitleLabel);
                }

                if (ExitButton != null)
                {
                    ExitButton.Click -= ExitButton_Click;
                    UIThemeManager.UnregisterControl(ExitButton);
                }

                // Unregister self
                UIThemeManager.UnregisterControl(this);
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this.TitleLabel = new System.Windows.Forms.Label();
            this.ExitButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // TitleLabel
            // 
            this.TitleLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)));
            this.TitleLabel.AutoSize = true;
            this.TitleLabel.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TitleLabel.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.TitleLabel.Location = new System.Drawing.Point(8, 6);
            this.TitleLabel.Margin = new System.Windows.Forms.Padding(0);
            this.TitleLabel.Name = "TitleLabel";
            this.TitleLabel.Size = new System.Drawing.Size(30, 17);
            this.TitleLabel.TabIndex = 0;
            this.TitleLabel.Text = "Title";
            this.TitleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ExitButton
            // 
            this.ExitButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ExitButton.FlatAppearance.BorderSize = 0;
            this.ExitButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.ExitButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.ExitButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ExitButton.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ExitButton.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.ExitButton.Location = new System.Drawing.Point(268, 0);
            this.ExitButton.Margin = new System.Windows.Forms.Padding(0);
            this.ExitButton.Name = "ExitButton";
            this.ExitButton.Size = new System.Drawing.Size(32, 32);
            this.ExitButton.TabIndex = 1;
            this.ExitButton.Text = "×";
            this.ExitButton.UseVisualStyleBackColor = false;
            // 
            // CTitleBar
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(36)))));
            this.Controls.Add(this.ExitButton);
            this.Controls.Add(this.TitleLabel);
            this.Name = "CTitleBar";
            this.Size = new System.Drawing.Size(300, 32);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        internal System.Windows.Forms.Label TitleLabel;
        internal System.Windows.Forms.Button ExitButton;
    }
}