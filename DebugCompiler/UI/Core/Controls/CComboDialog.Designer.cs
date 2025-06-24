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
    partial class CComboDialog
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
            if (disposing)
            {
                UIThemeManager.ThemeChanged -= OnThemeChanged;
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.InnerForm = new DebugCompiler.UI.Core.Controls.CBorderedForm();
            this.cComboBox1 = new DebugCompiler.UI.Core.Controls.CComboBox();
            this.AcceptButton = new System.Windows.Forms.Button();
            this.InnerForm.ControlContents.SuspendLayout();
            this.SuspendLayout();
            // 
            // InnerForm
            // 
            this.InnerForm.BackColor = System.Drawing.Color.DodgerBlue;
            // 
            // InnerForm.ControlContents
            // 
            this.InnerForm.ControlContents.Controls.Add(this.cComboBox1);
            this.InnerForm.ControlContents.Controls.Add(this.AcceptButton);
            this.InnerForm.ControlContents.Dock = System.Windows.Forms.DockStyle.Fill;
            this.InnerForm.ControlContents.Enabled = true;
            this.InnerForm.ControlContents.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.InnerForm.ControlContents.Location = new System.Drawing.Point(0, 32);
            this.InnerForm.ControlContents.Name = "ControlContents";
            this.InnerForm.ControlContents.Size = new System.Drawing.Size(202, 37);
            this.InnerForm.ControlContents.TabIndex = 1;
            this.InnerForm.ControlContents.Visible = true;
            this.InnerForm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.InnerForm.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.InnerForm.Location = new System.Drawing.Point(0, 0);
            this.InnerForm.Name = "InnerForm";
            this.InnerForm.Size = new System.Drawing.Size(206, 73);
            this.InnerForm.TabIndex = 0;
            this.InnerForm.TitleBarTitle = "Combo Dialog";
            this.InnerForm.UseTitleBar = true;
            this.InnerForm.Load += new System.EventHandler(this.InnerForm_Load);
            // 
            // cComboBox1
            // 
            this.cComboBox1.FormattingEnabled = true;
            this.cComboBox1.Location = new System.Drawing.Point(6, 6);
            this.cComboBox1.Name = "cComboBox1";
            this.cComboBox1.Size = new System.Drawing.Size(120, 25);
            this.cComboBox1.TabIndex = 2;
            this.cComboBox1.SelectedIndexChanged += new System.EventHandler(this.CComboBox1_SelectedIndexChanged);
            // 
            // AcceptButton
            // 
            this.AcceptButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.AcceptButton.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AcceptButton.Location = new System.Drawing.Point(132, 6);
            this.AcceptButton.Name = "AcceptButton";
            this.AcceptButton.Size = new System.Drawing.Size(66, 25);
            this.AcceptButton.TabIndex = 1;
            this.AcceptButton.Text = "Select";
            this.AcceptButton.UseVisualStyleBackColor = true;
            this.AcceptButton.Click += new System.EventHandler(this.AcceptButton_Click);
            // 
            // CComboDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(206, 73);
            this.Controls.Add(this.InnerForm);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "CComboDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Error Dialog";
            this.InnerForm.ControlContents.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private DebugCompiler.UI.Core.Controls.CBorderedForm InnerForm;
        private new System.Windows.Forms.Button AcceptButton;
        private DebugCompiler.UI.Core.Controls.CComboBox cComboBox1;
    }
}