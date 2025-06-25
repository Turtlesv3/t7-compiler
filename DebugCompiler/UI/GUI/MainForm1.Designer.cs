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

namespace DebugCompiler
{
    public partial class MainForm1
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
                _processWatcher?.Stop();
                _processWatcher?.Dispose();
                // ... other disposals
            }
            base.Dispose(disposing);
        }

        private class CustomToolStripRenderer : ToolStripProfessionalRenderer
        {
            private readonly UIThemeInfo _theme;

            public CustomToolStripRenderer(UIThemeInfo theme) : base(new ThemeColors(theme))
            {
                _theme = theme;
            }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                if (e.Item.Selected)
                {
                    using (var brush = new SolidBrush(_theme.ButtonHoverColor))
                    {
                        e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
                    }
                }
                else
                {
                    base.OnRenderMenuItemBackground(e);
                }
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = _theme.MenuTextColor;
                base.OnRenderItemText(e);
            }

            private class ThemeColors : ProfessionalColorTable
            {
                private readonly UIThemeInfo _theme;

                public ThemeColors(UIThemeInfo theme)
                {
                    _theme = theme;
                }

                public override Color MenuItemSelected => _theme.ButtonHoverColor;
                public override Color MenuItemBorder => _theme.BorderColor;
                public override Color MenuBorder => _theme.BorderColor;
                public override Color MenuItemSelectedGradientBegin => _theme.ButtonHoverColor;
                public override Color MenuItemSelectedGradientEnd => _theme.ButtonHoverColor;
                public override Color MenuItemPressedGradientBegin => _theme.ButtonActiveColor;
                public override Color MenuItemPressedGradientEnd => _theme.ButtonActiveColor;
                public override Color ToolStripDropDownBackground => _theme.MenuBackColor;

                // These are the correct color properties for menu items
                public override Color ImageMarginGradientBegin => _theme.MenuBackColor;
                public override Color ImageMarginGradientMiddle => _theme.MenuBackColor;
                public override Color ImageMarginGradientEnd => _theme.MenuBackColor;
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
            this.txtOutput = new System.Windows.Forms.RichTextBox();
            this.btnResetParseTree = new System.Windows.Forms.Button();
            this.btnInject = new System.Windows.Forms.Button();
            this.btnCompile = new System.Windows.Forms.Button();
            this.chkBuild = new System.Windows.Forms.CheckBox();
            this.chkCompileOnly = new System.Windows.Forms.CheckBox();
            this.chkHotLoad = new System.Windows.Forms.CheckBox();
            this.chkNoRuntime = new System.Windows.Forms.CheckBox();
            this.cmbHotMode = new System.Windows.Forms.ComboBox();
            this.cmbGame = new System.Windows.Forms.ComboBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.txtScriptPath = new System.Windows.Forms.RichTextBox();
            this.btnSavedMenus = new System.Windows.Forms.Button();
            this.InnerForm.ControlContents.SuspendLayout();
            this.SuspendLayout();
            // 
            // InnerForm
            // 
            this.InnerForm.BackColor = System.Drawing.Color.MediumPurple;
            // 
            // InnerForm.ControlContents
            // 
            this.InnerForm.ControlContents.Controls.Add(this.txtOutput);
            this.InnerForm.ControlContents.Controls.Add(this.btnResetParseTree);
            this.InnerForm.ControlContents.Controls.Add(this.btnInject);
            this.InnerForm.ControlContents.Controls.Add(this.btnCompile);
            this.InnerForm.ControlContents.Controls.Add(this.chkBuild);
            this.InnerForm.ControlContents.Controls.Add(this.chkCompileOnly);
            this.InnerForm.ControlContents.Controls.Add(this.chkHotLoad);
            this.InnerForm.ControlContents.Controls.Add(this.chkNoRuntime);
            this.InnerForm.ControlContents.Controls.Add(this.cmbHotMode);
            this.InnerForm.ControlContents.Controls.Add(this.cmbGame);
            this.InnerForm.ControlContents.Controls.Add(this.btnBrowse);
            this.InnerForm.ControlContents.Controls.Add(this.txtScriptPath);
            this.InnerForm.ControlContents.Controls.Add(this.btnSavedMenus);
            this.InnerForm.ControlContents.Dock = System.Windows.Forms.DockStyle.Fill;
            this.InnerForm.ControlContents.Location = new System.Drawing.Point(5, 37);
            this.InnerForm.ControlContents.Name = "ControlContents";
            this.InnerForm.ControlContents.Size = new System.Drawing.Size(752, 406);
            this.InnerForm.ControlContents.TabIndex = 1;
            this.InnerForm.DialogResult = System.Windows.Forms.DialogResult.None;
            this.InnerForm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.InnerForm.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.InnerForm.Location = new System.Drawing.Point(0, 0);
            this.InnerForm.Name = "InnerForm";
            this.InnerForm.Size = new System.Drawing.Size(762, 448);
            this.InnerForm.TabIndex = 0;
            this.InnerForm.TitleBarTitle = "Serious\'s T7/T8 Compiler GUI by DoubleG";
            // 
            // txtOutput
            // 
            this.txtOutput.BackColor = System.Drawing.Color.DimGray;
            this.txtOutput.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtOutput.ForeColor = System.Drawing.Color.MediumPurple;
            this.txtOutput.HideSelection = false;
            this.txtOutput.Location = new System.Drawing.Point(123, 125);
            this.txtOutput.Name = "txtOutput";
            this.txtOutput.ReadOnly = true;
            this.txtOutput.Size = new System.Drawing.Size(627, 255);
            this.txtOutput.TabIndex = 0;
            this.txtOutput.Text = "";
            this.txtOutput.WordWrap = false;
            // 
            // btnResetParseTree
            // 
            this.btnResetParseTree.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnResetParseTree.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.btnResetParseTree.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnResetParseTree.FlatAppearance.BorderColor = System.Drawing.Color.DodgerBlue;
            this.btnResetParseTree.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnResetParseTree.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.btnResetParseTree.ForeColor = System.Drawing.Color.White;
            this.btnResetParseTree.Location = new System.Drawing.Point(473, 78);
            this.btnResetParseTree.Name = "btnResetParseTree";
            this.btnResetParseTree.Size = new System.Drawing.Size(191, 30);
            this.btnResetParseTree.TabIndex = 1;
            this.btnResetParseTree.Text = "Reset GSC Parasetree";
            this.btnResetParseTree.UseVisualStyleBackColor = false;
            this.btnResetParseTree.Visible = false;
            this.btnResetParseTree.Click += new System.EventHandler(this.BtnResetParseTree_Click);
            // 
            // btnInject
            // 
            this.btnInject.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnInject.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.btnInject.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnInject.FlatAppearance.BorderColor = System.Drawing.Color.DodgerBlue;
            this.btnInject.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnInject.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.btnInject.ForeColor = System.Drawing.Color.White;
            this.btnInject.Location = new System.Drawing.Point(12, 125);
            this.btnInject.Name = "btnInject";
            this.btnInject.Size = new System.Drawing.Size(100, 30);
            this.btnInject.TabIndex = 2;
            this.btnInject.Text = "Inject";
            this.btnInject.UseVisualStyleBackColor = false;
            this.btnInject.Click += new System.EventHandler(this.BtnInject_Click);
            // 
            // btnCompile
            // 
            this.btnCompile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCompile.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.btnCompile.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCompile.FlatAppearance.BorderColor = System.Drawing.Color.DodgerBlue;
            this.btnCompile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCompile.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.btnCompile.ForeColor = System.Drawing.Color.White;
            this.btnCompile.Location = new System.Drawing.Point(12, 350);
            this.btnCompile.Name = "btnCompile";
            this.btnCompile.Size = new System.Drawing.Size(100, 30);
            this.btnCompile.TabIndex = 3;
            this.btnCompile.Text = "Compile";
            this.btnCompile.UseVisualStyleBackColor = false;
            this.btnCompile.Click += new System.EventHandler(this.BtnCompile_Click);
            // 
            // chkBuild
            // 
            this.chkBuild.AutoSize = true;
            this.chkBuild.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.chkBuild.ForeColor = System.Drawing.Color.White;
            this.chkBuild.Location = new System.Drawing.Point(12, 323);
            this.chkBuild.Name = "chkBuild";
            this.chkBuild.Size = new System.Drawing.Size(78, 21);
            this.chkBuild.TabIndex = 5;
            this.chkBuild.Text = "Full Build";
            this.chkBuild.UseVisualStyleBackColor = true;
            this.chkBuild.CheckedChanged += new System.EventHandler(this.ChkBuild_CheckedChanged);
            // 
            // chkCompileOnly
            // 
            this.chkCompileOnly.AutoSize = true;
            this.chkCompileOnly.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.chkCompileOnly.ForeColor = System.Drawing.Color.White;
            this.chkCompileOnly.Location = new System.Drawing.Point(12, 296);
            this.chkCompileOnly.Name = "chkCompileOnly";
            this.chkCompileOnly.Size = new System.Drawing.Size(105, 21);
            this.chkCompileOnly.TabIndex = 6;
            this.chkCompileOnly.Text = "Compile Only";
            this.chkCompileOnly.UseVisualStyleBackColor = true;
            this.chkCompileOnly.CheckedChanged += new System.EventHandler(this.ChkCompileOnly_CheckedChanged);
            // 
            // chkHotLoad
            // 
            this.chkHotLoad.AutoSize = true;
            this.chkHotLoad.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.chkHotLoad.ForeColor = System.Drawing.Color.White;
            this.chkHotLoad.Location = new System.Drawing.Point(12, 103);
            this.chkHotLoad.Name = "chkHotLoad";
            this.chkHotLoad.Size = new System.Drawing.Size(81, 21);
            this.chkHotLoad.TabIndex = 7;
            this.chkHotLoad.Text = "Hot Load";
            this.chkHotLoad.UseVisualStyleBackColor = true;
            this.chkHotLoad.CheckedChanged += new System.EventHandler(this.ChkHotLoad_CheckedChanged);
            // 
            // chkNoRuntime
            // 
            this.chkNoRuntime.AutoSize = true;
            this.chkNoRuntime.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.chkNoRuntime.ForeColor = System.Drawing.Color.White;
            this.chkNoRuntime.Location = new System.Drawing.Point(12, 161);
            this.chkNoRuntime.Name = "chkNoRuntime";
            this.chkNoRuntime.Size = new System.Drawing.Size(96, 21);
            this.chkNoRuntime.TabIndex = 8;
            this.chkNoRuntime.Text = "No Runtime";
            this.chkNoRuntime.UseVisualStyleBackColor = true;
            this.chkNoRuntime.CheckedChanged += new System.EventHandler(this.ChkNoRuntime_CheckedChanged);
            // 
            // cmbHotMode
            // 
            this.cmbHotMode.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbHotMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.cmbHotMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbHotMode.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbHotMode.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.cmbHotMode.ForeColor = System.Drawing.Color.White;
            this.cmbHotMode.FormattingEnabled = true;
            this.cmbHotMode.Location = new System.Drawing.Point(12, 72);
            this.cmbHotMode.Name = "cmbHotMode";
            this.cmbHotMode.Size = new System.Drawing.Size(55, 25);
            this.cmbHotMode.TabIndex = 9;
            this.cmbHotMode.SelectedIndexChanged += new System.EventHandler(this.CmbHotMode_SelectedIndexChanged);
            // 
            // cmbGame
            // 
            this.cmbGame.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbGame.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.cmbGame.DropDownStyle = System.Windows.Forms.ComboBoxStyle.Simple;
            this.cmbGame.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbGame.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.cmbGame.ForeColor = System.Drawing.Color.White;
            this.cmbGame.FormattingEnabled = true;
            this.cmbGame.Location = new System.Drawing.Point(7, 30);
            this.cmbGame.Name = "cmbGame";
            this.cmbGame.Size = new System.Drawing.Size(133, 25);
            this.cmbGame.TabIndex = 10;
            this.cmbGame.SelectedIndexChanged += new System.EventHandler(this.CmbGame_SelectedIndexChanged);
            // 
            // btnBrowse
            // 
            this.btnBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowse.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.btnBrowse.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBrowse.FlatAppearance.BorderColor = System.Drawing.Color.DodgerBlue;
            this.btnBrowse.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowse.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.btnBrowse.ForeColor = System.Drawing.Color.White;
            this.btnBrowse.Location = new System.Drawing.Point(670, 30);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(80, 25);
            this.btnBrowse.TabIndex = 11;
            this.btnBrowse.Text = "Browse...";
            this.btnBrowse.UseVisualStyleBackColor = false;
            this.btnBrowse.Click += new System.EventHandler(this.BtnBrowse_Click);
            // 
            // txtScriptPath
            // 
            this.txtScriptPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtScriptPath.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.txtScriptPath.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtScriptPath.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtScriptPath.ForeColor = System.Drawing.Color.White;
            this.txtScriptPath.Location = new System.Drawing.Point(151, 30);
            this.txtScriptPath.Name = "txtScriptPath";
            this.txtScriptPath.Size = new System.Drawing.Size(513, 25);
            this.txtScriptPath.TabIndex = 12;
            this.txtScriptPath.Text = "";
            this.txtScriptPath.TextChanged += new System.EventHandler(this.TxtScriptPath_TextChanged);
            // 
            // btnSavedMenus
            // 
            this.btnSavedMenus.Location = new System.Drawing.Point(670, 78);
            this.btnSavedMenus.Name = "btnSavedMenus";
            this.btnSavedMenus.Size = new System.Drawing.Size(80, 30);
            this.btnSavedMenus.TabIndex = 10;
            this.btnSavedMenus.Text = "Saved Menus";
            this.btnSavedMenus.UseVisualStyleBackColor = true;
            this.btnSavedMenus.Click += new System.EventHandler(this.BtnSavedMenus_Click);
            // 
            // MainForm1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.MediumPurple;
            this.ClientSize = new System.Drawing.Size(762, 448);
            this.Controls.Add(this.InnerForm);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.KeyPreview = true;
            this.Name = "MainForm1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "T7/T8 Compiler";
            this.InnerForm.ControlContents.ResumeLayout(false);
            this.InnerForm.ControlContents.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private DebugCompiler.UI.Core.Controls.CBorderedForm InnerForm;
        private System.Windows.Forms.RichTextBox txtScriptPath;
        private System.Windows.Forms.RichTextBox txtOutput;
        private System.Windows.Forms.Button btnResetParseTree;
        private System.Windows.Forms.Button btnCompile;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.Button btnInject;
        private System.Windows.Forms.Button btnSavedMenus;
        private System.Windows.Forms.CheckBox chkBuild;
        private System.Windows.Forms.CheckBox chkCompileOnly;
        private System.Windows.Forms.CheckBox chkHotLoad;
        private System.Windows.Forms.CheckBox chkNoRuntime;
        private System.Windows.Forms.ComboBox cmbHotMode;
        private System.Windows.Forms.ComboBox cmbGame;
    }
}