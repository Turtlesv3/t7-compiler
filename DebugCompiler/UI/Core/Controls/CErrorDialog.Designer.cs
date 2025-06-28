using DebugCompiler.UI.Core.Controls;
using DebugCompiler.UI.Core.Interfaces;
using DebugCompiler.UI.Core.Singletons;
using DebugCompiler.UI.Core.Helpers;
using Microsoft.Test.Xbox.XDRPC;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Windows.Forms.VisualStyles;
using DebugCompiler.Properties;

namespace DebugCompiler.UI.Core.Controls
{
    public partial class CErrorDialog
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
            try
            {
                if (disposing)
                {
                    // Add any additional cleanup you need here
                    components?.Dispose();
                }
                base.Dispose(disposing);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error disposing dialog: {ex}");
            }
        }


        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.InnerForm = new DebugCompiler.UI.Core.Controls.CBorderedForm();
            this.AcceptButton = new System.Windows.Forms.Button();
            this.ErrorRTB = new System.Windows.Forms.RichTextBox();
            this.InnerForm.ControlContents.SuspendLayout();
            this.SuspendLayout();
            // 
            // InnerForm
            // 
            this.InnerForm.BackColor = System.Drawing.Color.Transparent;
            // 
            // InnerForm.ControlContents
            // 
            this.InnerForm.ControlContents.Controls.Add(this.AcceptButton);
            this.InnerForm.ControlContents.Controls.Add(this.ErrorRTB);
            this.InnerForm.ControlContents.Dock = System.Windows.Forms.DockStyle.Fill;
            this.InnerForm.ControlContents.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.InnerForm.ControlContents.Location = new System.Drawing.Point(5, 37);
            this.InnerForm.ControlContents.Name = "ControlContents";
            this.InnerForm.ControlContents.Size = new System.Drawing.Size(222, 137);
            this.InnerForm.ControlContents.TabIndex = 1;
            this.InnerForm.DialogResult = System.Windows.Forms.DialogResult.None;
            this.InnerForm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.InnerForm.ForeColor = System.Drawing.Color.Transparent;
            this.InnerForm.Location = new System.Drawing.Point(0, 0);
            this.InnerForm.Name = "InnerForm";
            this.InnerForm.Size = new System.Drawing.Size(232, 179);
            this.InnerForm.TabIndex = 0;
            this.InnerForm.Title = "Error Dialog";
            // 
            // AcceptButton
            // 
            this.AcceptButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.AcceptButton.ForeColor = System.Drawing.Color.Transparent;
            this.AcceptButton.Location = new System.Drawing.Point(148, 101);
            this.AcceptButton.Name = "AcceptButton";
            this.AcceptButton.Size = new System.Drawing.Size(67, 29);
            this.AcceptButton.TabIndex = 1;
            this.AcceptButton.Text = "NINE";
            this.AcceptButton.UseVisualStyleBackColor = true;
            this.AcceptButton.Click += new System.EventHandler(this.AcceptButton_Click);
            // 
            // ErrorRTB
            // 
            this.ErrorRTB.BackColor = System.Drawing.Color.MediumPurple;
            this.ErrorRTB.DetectUrls = false;
            this.ErrorRTB.Location = new System.Drawing.Point(7, 18);
            this.ErrorRTB.Name = "ErrorRTB";
            this.ErrorRTB.ReadOnly = true;
            this.ErrorRTB.Size = new System.Drawing.Size(208, 77);
            this.ErrorRTB.TabIndex = 0;
            this.ErrorRTB.Text = "Sup Sexy Cunt!";
            // 
            // CErrorDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(232, 179);
            this.Controls.Add(this.InnerForm);
            this.ForeColor = System.Drawing.Color.Transparent;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "CErrorDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Jew Dialog";
            this.InnerForm.ControlContents.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private DebugCompiler.UI.Core.Controls.CBorderedForm InnerForm;
        private System.Windows.Forms.RichTextBox ErrorRTB;
        private new System.Windows.Forms.Button AcceptButton;
    }
}