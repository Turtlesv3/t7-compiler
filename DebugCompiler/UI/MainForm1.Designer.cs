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
        private System.Windows.Forms.ToolTip toolTip1;
        private new System.Windows.Forms.MenuStrip MainMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem _themeMenu;
        private System.Windows.Forms.Label _lblGameStatus;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                Console.SetOut(_originalOut);

                _processWatcher?.Stop();
                _processWatcher?.Dispose();
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComboBox(ComboBox comboBox, bool visible)
        {
            if (comboBox == null || comboBox.IsDisposed) return;

            comboBox.BeginInvoke((MethodInvoker)(() =>
            {
                comboBox.SuspendLayout();
                try
                {
                    comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                    comboBox.Visible = visible;
                    comboBox.Enabled = visible;

                    // Additional combo box styling
                    comboBox.FlatStyle = FlatStyle.Flat;
                    comboBox.BackColor = UIThemeManager.CurrentTheme.ControlBackColor;
                    comboBox.ForeColor = UIThemeManager.CurrentTheme.TextColor;
                }
                finally
                {
                    comboBox.ResumeLayout();
                }
            }));
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Force initial process check after window is shown
            CheckGameProcess();
        }

        private void UpdateGameStatus()
        {
            if (_lblGameStatus == null) return;

            _lblGameStatus.Text = _lastRunningStatus
                ? $"✓ {_lastDetectedGame} Running"
                : "✗ Game Not Running";

            _lblGameStatus.ForeColor = _lastRunningStatus ? _successColor : _errorColor;
            _lblGameStatus.Visible = true; // Now always visible since it's designer-created
        }

        private void InitializeCustomComponents()
        {
            // Initialize Hot Mode dropdown
            cmbHotMode.Items.Clear();
            cmbHotMode.Items.AddRange(new[] { "GSC", "CSC" });
            cmbHotMode.SelectedIndex = 0; // Select first item by default
            cmbHotMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbHotMode.Visible = chkHotLoad.Checked;

            // Ensure proper theming
            cmbHotMode.BackColor = UIThemeManager.CurrentTheme.ControlBackColor;
            cmbHotMode.ForeColor = UIThemeManager.CurrentTheme.TextColor;

            // Refresh to ensure visibility
            cmbHotMode.Refresh();
        }

        private void InitializeOutputTextBox()
        {
            if (txtOutput == null || txtOutput.IsDisposed) return;

            txtOutput.BeginInvoke((MethodInvoker)(() =>
            {
                txtOutput.BackColor = UIThemeManager.CurrentTheme.ControlBackColor;
                txtOutput.ForeColor = UIThemeManager.CurrentTheme.TextColor;
                txtOutput.Font = new Font("Consolas", 9.75f);
                txtOutput.WordWrap = false;
            }));
        }

        private void SyncTitleBarTheme()
        {
            if (InnerForm?.TitleBar == null) return;

            var theme = UIThemeManager.CurrentTheme;
            InnerForm.TitleBar.BackColor = theme.AccentColor;

            // Force update of child controls
            foreach (var control in InnerForm.TitleBar.GetThemedControls())
            {
                UIThemeManager.EnsureThemeApplied(control);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            PositionThemeMenu(); // Maintain bottom-left position on resize
        }

        private void DelayedRetryInitialization()
        {
            if (IsDisposed) return;

            try
            {
                // Wait for handles to be ready
                if (!IsHandleCreated || !cmbHotMode.IsHandleCreated)
                {
                    BeginInvoke((MethodInvoker)DelayedRetryInitialization);
                    return;
                }

                InitializeUIComponents();
            }
            catch
            {
                // Final fallback
                Task.Delay(1000).ContinueWith(_ =>
                {
                    SafeInvoke(InitializeUIComponents);
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
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
            this.components = new System.ComponentModel.Container();
            this.MainMenuStrip = new System.Windows.Forms.MenuStrip();
            this._themeMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.InnerForm = new DebugCompiler.UI.Core.Controls.CBorderedForm();
            this._lblGameStatus = new System.Windows.Forms.Label();
            this.txtOutput = new System.Windows.Forms.RichTextBox();
            this.btnResetParseTree = new System.Windows.Forms.Button();
            this.btnInject = new System.Windows.Forms.Button();
            this.btnCompile = new System.Windows.Forms.Button();
            this.chkBuild = new System.Windows.Forms.CheckBox();
            this.chkCompileOnly = new System.Windows.Forms.CheckBox();
            this.chkHotLoad = new System.Windows.Forms.CheckBox();
            this.chkNoRuntime = new System.Windows.Forms.CheckBox();
            this.cmbHotMode = new System.Windows.Forms.ComboBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.txtScriptPath = new System.Windows.Forms.RichTextBox();
            this.btnSavedMenus = new System.Windows.Forms.Button();
            this.MainMenuStrip.SuspendLayout();
            this.InnerForm.MainPanel.SuspendLayout();
            this.InnerForm.SuspendLayout();
            this.SuspendLayout();
            // 
            // MainMenuStrip
            // 
            this.MainMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._themeMenu});
            this.MainMenuStrip.Location = new System.Drawing.Point(0, 0);
            this.MainMenuStrip.Name = "MainMenuStrip";
            this.MainMenuStrip.Size = new System.Drawing.Size(768, 24);
            this.MainMenuStrip.TabIndex = 0;
            this.MainMenuStrip.Text = "menuStrip1";
            this.MainMenuStrip.Visible = false;
            this.MainMenuStrip.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.MainMenuStrip_ItemClicked);
            // 
            // _themeMenu
            // 
            this._themeMenu.Name = "_themeMenu";
            this._themeMenu.Size = new System.Drawing.Size(60, 20);
            this._themeMenu.Text = "Themes";
            // 
            // toolTip1
            // 
            this.toolTip1.Popup += new System.Windows.Forms.PopupEventHandler(this.toolTip1_Popup);
            // 
            // InnerForm
            // 
            this.InnerForm.BackColor = System.Drawing.Color.MediumPurple;
            // 
            // InnerForm.ControlContents
            // 
            this.InnerForm.MainPanel.BackColor = System.Drawing.Color.CornflowerBlue;
            this.InnerForm.MainPanel.Controls.Add(this._lblGameStatus);
            this.InnerForm.MainPanel.Controls.Add(this.txtOutput);
            this.InnerForm.MainPanel.Controls.Add(this.btnResetParseTree);
            this.InnerForm.MainPanel.Controls.Add(this.btnInject);
            this.InnerForm.MainPanel.Controls.Add(this.btnCompile);
            this.InnerForm.MainPanel.Controls.Add(this.chkBuild);
            this.InnerForm.MainPanel.Controls.Add(this.chkCompileOnly);
            this.InnerForm.MainPanel.Controls.Add(this.chkHotLoad);
            this.InnerForm.MainPanel.Controls.Add(this.chkNoRuntime);
            this.InnerForm.MainPanel.Controls.Add(this.cmbHotMode);
            this.InnerForm.MainPanel.Controls.Add(this.btnBrowse);
            this.InnerForm.MainPanel.Controls.Add(this.txtScriptPath);
            this.InnerForm.MainPanel.Controls.Add(this.btnSavedMenus);
            this.InnerForm.MainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.InnerForm.MainPanel.Location = new System.Drawing.Point(0, 0);
            this.InnerForm.MainPanel.Name = "ControlContents";
            this.InnerForm.MainPanel.Size = new System.Drawing.Size(768, 438);
            this.InnerForm.MainPanel.TabIndex = 0;
            this.InnerForm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.InnerForm.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.InnerForm.Location = new System.Drawing.Point(0, 0);
            this.InnerForm.Name = "InnerForm";
            this.InnerForm.Size = new System.Drawing.Size(768, 438);
            this.InnerForm.TabIndex = 0;
            this.InnerForm.Title = "Title";
            // 
            // 
            // 
            this.InnerForm.TitleBar.DisableDrag = false;
            this.InnerForm.TitleBar.Location = new System.Drawing.Point(0, 0);
            this.InnerForm.TitleBar.Name = "_titleBar";
            this.InnerForm.TitleBar.TabIndex = 1;
            this.InnerForm.TitleBar.Title = "Title";
            // 
            // _lblGameStatus
            // 
            this._lblGameStatus.BackColor = System.Drawing.Color.Transparent;
            this._lblGameStatus.Font = new System.Drawing.Font("Segoe UI", 9F);
            this._lblGameStatus.Location = new System.Drawing.Point(620, 377);
            this._lblGameStatus.Name = "_lblGameStatus";
            this._lblGameStatus.Size = new System.Drawing.Size(136, 26);
            this._lblGameStatus.TabIndex = 20;
            this._lblGameStatus.Text = "Game Status";
            this._lblGameStatus.Visible = false;
            // 
            // txtOutput
            // 
            this.txtOutput.BackColor = System.Drawing.Color.LavenderBlush;
            this.txtOutput.Font = new System.Drawing.Font("Calibri", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtOutput.ForeColor = System.Drawing.Color.MediumPurple;
            this.txtOutput.HideSelection = false;
            this.txtOutput.Location = new System.Drawing.Point(12, 138);
            this.txtOutput.Name = "txtOutput";
            this.txtOutput.ReadOnly = true;
            this.txtOutput.Size = new System.Drawing.Size(744, 255);
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
            this.btnResetParseTree.Location = new System.Drawing.Point(123, 42);
            this.btnResetParseTree.Name = "btnResetParseTree";
            this.btnResetParseTree.Size = new System.Drawing.Size(516, 81);
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
            this.btnInject.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnInject.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.btnInject.ForeColor = System.Drawing.Color.White;
            this.btnInject.Location = new System.Drawing.Point(648, 42);
            this.btnInject.Name = "btnInject";
            this.btnInject.Size = new System.Drawing.Size(108, 40);
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
            this.btnCompile.Location = new System.Drawing.Point(12, 42);
            this.btnCompile.Name = "btnCompile";
            this.btnCompile.Size = new System.Drawing.Size(105, 35);
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
            this.chkBuild.Location = new System.Drawing.Point(12, 83);
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
            this.chkCompileOnly.Location = new System.Drawing.Point(12, 102);
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
            this.chkHotLoad.Location = new System.Drawing.Point(105, 17);
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
            this.chkNoRuntime.Location = new System.Drawing.Point(12, 17);
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
            this.cmbHotMode.Location = new System.Drawing.Point(192, 11);
            this.cmbHotMode.Name = "cmbHotMode";
            this.cmbHotMode.Size = new System.Drawing.Size(52, 25);
            this.cmbHotMode.TabIndex = 9;
            this.cmbHotMode.SelectedIndexChanged += new System.EventHandler(this.CmbHotMode_SelectedIndexChanged);
            // 
            // btnBrowse
            // 
            this.btnBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowse.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.btnBrowse.Cursor = System.Windows.Forms.Cursors.PanNorth;
            this.btnBrowse.FlatAppearance.BorderColor = System.Drawing.Color.DodgerBlue;
            this.btnBrowse.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowse.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.btnBrowse.ForeColor = System.Drawing.Color.White;
            this.btnBrowse.Location = new System.Drawing.Point(648, 7);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(108, 31);
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
            this.txtScriptPath.Location = new System.Drawing.Point(250, 7);
            this.txtScriptPath.Name = "txtScriptPath";
            this.txtScriptPath.Size = new System.Drawing.Size(389, 31);
            this.txtScriptPath.TabIndex = 12;
            this.txtScriptPath.Text = "";
            this.txtScriptPath.TextChanged += new System.EventHandler(this.TxtScriptPath_TextChanged);
            // 
            // btnSavedMenus
            // 
            this.btnSavedMenus.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSavedMenus.Location = new System.Drawing.Point(648, 88);
            this.btnSavedMenus.Name = "btnSavedMenus";
            this.btnSavedMenus.Size = new System.Drawing.Size(108, 35);
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
            this.ClientSize = new System.Drawing.Size(768, 438);
            this.Controls.Add(this.InnerForm);
            this.Controls.Add(this.MainMenuStrip);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.KeyPreview = true;
            this.Name = "MainForm1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.MainMenuStrip.ResumeLayout(false);
            this.MainMenuStrip.PerformLayout();
            this.InnerForm.MainPanel.ResumeLayout(false);
            this.InnerForm.MainPanel.PerformLayout();
            this.InnerForm.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        

        #endregion
    }
}