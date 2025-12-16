using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Enum.Poison;

namespace T7CompilerGUI.Controls
{
    partial class LaunchGameTaskControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        
        // Control declarations
        internal PoisonPanel mainPanel; // Made internal so static Show method can access it
        private PoisonLabel lblMessage;
        internal PoisonPanel buttonPanel; // Made internal so static Show method can access it
        private PoisonButton btnLaunch;
        private PoisonButton btnDismiss;

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
            this.mainPanel = new ReaLTaiizor.Controls.PoisonPanel();
            this.lblMessage = new ReaLTaiizor.Controls.PoisonLabel();
            this.buttonPanel = new ReaLTaiizor.Controls.PoisonPanel();
            this.btnLaunch = new ReaLTaiizor.Controls.PoisonButton();
            this.btnDismiss = new ReaLTaiizor.Controls.PoisonButton();
            this.mainPanel.SuspendLayout();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainPanel
            // 
            this.mainPanel.Controls.Add(this.lblMessage);
            this.mainPanel.Controls.Add(this.buttonPanel);
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.HorizontalScrollbarBarColor = true;
            this.mainPanel.HorizontalScrollbarHighlightOnWheel = false;
            this.mainPanel.HorizontalScrollbarShowThumbOnly = true;
            this.mainPanel.HorizontalScrollbarSize = 10;
            this.mainPanel.Location = new System.Drawing.Point(0, 0);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Padding = new System.Windows.Forms.Padding(20, 15, 20, 15);
            this.mainPanel.Size = new System.Drawing.Size(344, 253);
            this.mainPanel.TabIndex = 0;
            this.mainPanel.UseStyleColors = true;
            this.mainPanel.VerticalScrollbarBarColor = true;
            this.mainPanel.VerticalScrollbarHighlightOnWheel = false;
            this.mainPanel.VerticalScrollbarShowThumbOnly = true;
            this.mainPanel.VerticalScrollbarSize = 10;
            // 
            // lblMessage
            // 
            this.lblMessage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMessage.ForeColor = System.Drawing.Color.White;
            this.lblMessage.Location = new System.Drawing.Point(20, 15);
            this.lblMessage.Margin = new System.Windows.Forms.Padding(10);
            this.lblMessage.Name = "lblMessage";
            this.lblMessage.Size = new System.Drawing.Size(304, 169);
            this.lblMessage.TabIndex = 0;
            this.lblMessage.Text = "Launch Black Ops 3?\r\n\r\nBlack Ops 3 is not running.\r\nWould you like to launch it?";
            this.lblMessage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblMessage.UseStyleColors = true;
            this.lblMessage.Click += new System.EventHandler(this.lblMessage_Click);
            // 
            // buttonPanel
            // 
            this.buttonPanel.Controls.Add(this.btnLaunch);
            this.buttonPanel.Controls.Add(this.btnDismiss);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.buttonPanel.HorizontalScrollbarBarColor = true;
            this.buttonPanel.HorizontalScrollbarHighlightOnWheel = false;
            this.buttonPanel.HorizontalScrollbarSize = 10;
            this.buttonPanel.Location = new System.Drawing.Point(20, 184);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
            this.buttonPanel.Size = new System.Drawing.Size(304, 54);
            this.buttonPanel.TabIndex = 2;
            this.buttonPanel.UseStyleColors = true;
            this.buttonPanel.VerticalScrollbarBarColor = true;
            this.buttonPanel.VerticalScrollbarHighlightOnWheel = false;
            this.buttonPanel.VerticalScrollbarSize = 10;
            // 
            // btnLaunch
            // 
            this.btnLaunch.Location = new System.Drawing.Point(0, 10);
            this.btnLaunch.Name = "btnLaunch";
            this.btnLaunch.Size = new System.Drawing.Size(120, 30);
            this.btnLaunch.TabIndex = 0;
            this.btnLaunch.Text = "Launch BO3";
            this.btnLaunch.UseSelectable = true;
            this.btnLaunch.UseStyleColors = true;
            this.btnLaunch.Click += new System.EventHandler(this.btnLaunch_Click);
            // 
            // btnDismiss
            // 
            this.btnDismiss.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDismiss.Location = new System.Drawing.Point(184, 10);
            this.btnDismiss.Name = "btnDismiss";
            this.btnDismiss.Size = new System.Drawing.Size(120, 30);
            this.btnDismiss.TabIndex = 1;
            this.btnDismiss.Text = "Dismiss";
            this.btnDismiss.UseSelectable = true;
            this.btnDismiss.UseStyleColors = true;
            this.btnDismiss.Click += new System.EventHandler(this.btnDismiss_Click);
            // 
            // LaunchGameTaskControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.mainPanel);
            this.Name = "LaunchGameTaskControl";
            this.Size = new System.Drawing.Size(344, 253);
            this.mainPanel.ResumeLayout(false);
            this.buttonPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
    }
}
