namespace DebugCompiler
{
    partial class MainForm1
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
            this.InnerForm.ControlContents.Dock = System.Windows.Forms.DockStyle.Fill;
            this.InnerForm.ControlContents.Enabled = true;
            this.InnerForm.ControlContents.Location = new System.Drawing.Point(0, 32);
            this.InnerForm.ControlContents.Name = "ControlContents";
            this.InnerForm.ControlContents.Size = new System.Drawing.Size(656, 337);
            this.InnerForm.ControlContents.TabIndex = 1;
            this.InnerForm.ControlContents.Visible = true;
            this.InnerForm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.InnerForm.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.InnerForm.Location = new System.Drawing.Point(0, 0);
            this.InnerForm.Name = "InnerForm";
            this.InnerForm.Size = new System.Drawing.Size(660, 373);
            this.InnerForm.TabIndex = 0;
            this.InnerForm.TitleBarTitle = "Serious\'s T7/T8 Compiler GUI by DoubleG";
            this.InnerForm.UseTitleBar = true;
            // 
            // txtOutput
            // 
            this.txtOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOutput.BackColor = System.Drawing.Color.DimGray;
            this.txtOutput.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtOutput.ForeColor = System.Drawing.Color.MediumPurple;
            this.txtOutput.HideSelection = false;
            this.txtOutput.Location = new System.Drawing.Point(116, 114);
            this.txtOutput.Name = "txtOutput";
            this.txtOutput.ReadOnly = true;
            this.txtOutput.Size = new System.Drawing.Size(530, 213);
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
            this.btnResetParseTree.Location = new System.Drawing.Point(496, 60);
            this.btnResetParseTree.Name = "btnResetParseTree";
            this.btnResetParseTree.Size = new System.Drawing.Size(150, 30);
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
            this.btnInject.Location = new System.Drawing.Point(6, 114);
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
            this.btnCompile.Location = new System.Drawing.Point(6, 259);
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
            this.chkBuild.Location = new System.Drawing.Point(10, 232);
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
            this.chkCompileOnly.Location = new System.Drawing.Point(10, 205);
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
            this.chkHotLoad.Location = new System.Drawing.Point(10, 60);
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
            this.chkNoRuntime.Location = new System.Drawing.Point(10, 87);
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
            this.cmbHotMode.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbHotMode.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.cmbHotMode.ForeColor = System.Drawing.Color.White;
            this.cmbHotMode.FormattingEnabled = true;
            this.cmbHotMode.Location = new System.Drawing.Point(116, 56);
            this.cmbHotMode.Name = "cmbHotMode";
            this.cmbHotMode.Size = new System.Drawing.Size(52, 25);
            this.cmbHotMode.TabIndex = 9;
            this.cmbHotMode.Text = "GSC";
            // 
            // cmbGame
            // 
            this.cmbGame.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbGame.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.cmbGame.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbGame.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.cmbGame.ForeColor = System.Drawing.Color.White;
            this.cmbGame.FormattingEnabled = true;
            this.cmbGame.Location = new System.Drawing.Point(10, 17);
            this.cmbGame.Name = "cmbGame";
            this.cmbGame.Size = new System.Drawing.Size(167, 25);
            this.cmbGame.TabIndex = 10;
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
            this.btnBrowse.Location = new System.Drawing.Point(566, 16);
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
            this.txtScriptPath.Location = new System.Drawing.Point(187, 16);
            this.txtScriptPath.Name = "txtScriptPath";
            this.txtScriptPath.Size = new System.Drawing.Size(366, 25);
            this.txtScriptPath.TabIndex = 12;
            this.txtScriptPath.Text = "";
            // 
            // MainForm1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.MediumPurple;
            this.ClientSize = new System.Drawing.Size(660, 373);
            this.Controls.Add(this.InnerForm);
            this.ForeColor = System.Drawing.SystemColors.ControlText;
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
        private System.Windows.Forms.CheckBox chkBuild;
        private System.Windows.Forms.CheckBox chkCompileOnly;
        private System.Windows.Forms.CheckBox chkHotLoad;
        private System.Windows.Forms.CheckBox chkNoRuntime;
        private System.Windows.Forms.ComboBox cmbHotMode;
        private System.Windows.Forms.ComboBox cmbGame;
    }
}