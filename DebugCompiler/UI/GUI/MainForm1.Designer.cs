using System.Drawing;

namespace DebugCompiler
{
    partial class MainForm1
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

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.MainContainer = new System.Windows.Forms.Panel();
            this.OutputPanel = new System.Windows.Forms.Panel();
            this.txtOutput = new System.Windows.Forms.TextBox();
            this.ControlPanel = new System.Windows.Forms.Panel();
            this.btnResetParseTree = new System.Windows.Forms.Button();
            this.cmbHotMode = new System.Windows.Forms.ComboBox();
            this.btnInject = new System.Windows.Forms.Button();
            this.OptionsPanel = new System.Windows.Forms.Panel();
            this.chkNoRuntime = new System.Windows.Forms.CheckBox();
            this.btnCompile = new System.Windows.Forms.Button();
            this.chkHotLoad = new System.Windows.Forms.CheckBox();
            this.chkCompileOnly = new System.Windows.Forms.CheckBox();
            this.chkBuild = new System.Windows.Forms.CheckBox();
            this.chkNoUpdate = new System.Windows.Forms.CheckBox();
            this.cmbGame = new System.Windows.Forms.ComboBox();
            this.InputPanel = new System.Windows.Forms.Panel();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.txtScriptPath = new System.Windows.Forms.TextBox();
            this.MainContainer.SuspendLayout();
            this.OutputPanel.SuspendLayout();
            this.ControlPanel.SuspendLayout();
            this.OptionsPanel.SuspendLayout();
            this.InputPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // MainContainer
            // 
            this.MainContainer.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.MainContainer.Controls.Add(this.OutputPanel);
            this.MainContainer.Controls.Add(this.ControlPanel);
            this.MainContainer.Controls.Add(this.OptionsPanel);
            this.MainContainer.Controls.Add(this.InputPanel);
            this.MainContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainContainer.Location = new System.Drawing.Point(0, 0);
            this.MainContainer.Name = "MainContainer";
            this.MainContainer.Size = new System.Drawing.Size(784, 561);
            this.MainContainer.TabIndex = 0;
            // 
            // OutputPanel
            // 
            this.OutputPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.OutputPanel.Controls.Add(this.txtOutput);
            this.OutputPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.OutputPanel.Location = new System.Drawing.Point(0, 198);
            this.OutputPanel.Name = "OutputPanel";
            this.OutputPanel.Size = new System.Drawing.Size(784, 363);
            this.OutputPanel.TabIndex = 3;
            // 
            // txtOutput
            // 
            this.txtOutput.BackColor = System.Drawing.Color.Black;
            this.txtOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtOutput.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtOutput.ForeColor = System.Drawing.Color.Lime;
            this.txtOutput.Location = new System.Drawing.Point(0, 0);
            this.txtOutput.Multiline = true;
            this.txtOutput.Name = "txtOutput";
            this.txtOutput.ReadOnly = true;
            this.txtOutput.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtOutput.Size = new System.Drawing.Size(784, 363);
            this.txtOutput.TabIndex = 0;
            // 
            // ControlPanel
            // 
            this.ControlPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.ControlPanel.Controls.Add(this.btnResetParseTree);
            this.ControlPanel.Controls.Add(this.btnInject);
            this.ControlPanel.Controls.Add(this.btnCompile);
            this.ControlPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.ControlPanel.Location = new System.Drawing.Point(0, 154);
            this.ControlPanel.Name = "ControlPanel";
            this.ControlPanel.Size = new System.Drawing.Size(784, 44);
            this.ControlPanel.TabIndex = 2;
            // 
            // btnResetParseTree
            // 
            this.btnResetParseTree.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.btnResetParseTree.Enabled = false;
            this.btnResetParseTree.FlatAppearance.BorderColor = System.Drawing.Color.DodgerBlue;
            this.btnResetParseTree.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnResetParseTree.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.btnResetParseTree.ForeColor = System.Drawing.Color.White;
            this.btnResetParseTree.Location = new System.Drawing.Point(313, 6);
            this.btnResetParseTree.Name = "btnResetParseTree";
            this.btnResetParseTree.Size = new System.Drawing.Size(150, 30);
            this.btnResetParseTree.TabIndex = 2;
            this.btnResetParseTree.Text = "Reset GSC Parasetree";
            this.btnResetParseTree.UseVisualStyleBackColor = false;
            this.btnResetParseTree.Visible = false;
            this.btnResetParseTree.Click += new System.EventHandler(this.BtnResetParseTree_Click);
            // 
            // cmbHotMode
            // 
            this.cmbHotMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.cmbHotMode.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbHotMode.ForeColor = System.Drawing.Color.White;
            this.cmbHotMode.FormattingEnabled = true;
            this.cmbHotMode.Location = new System.Drawing.Point(313, 62);
            this.cmbHotMode.Name = "cmbHotMode";
            this.cmbHotMode.Size = new System.Drawing.Size(58, 23);
            this.cmbHotMode.TabIndex = 5;
            this.cmbHotMode.SelectedIndexChanged += new System.EventHandler(this.CmbHotMode_SelectedIndexChanged);
            // 
            // btnInject
            // 
            this.btnInject.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.btnInject.FlatAppearance.BorderColor = System.Drawing.Color.DodgerBlue;
            this.btnInject.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnInject.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.btnInject.ForeColor = System.Drawing.Color.White;
            this.btnInject.Location = new System.Drawing.Point(12, 6);
            this.btnInject.Name = "btnInject";
            this.btnInject.Size = new System.Drawing.Size(100, 30);
            this.btnInject.TabIndex = 1;
            this.btnInject.Text = "Inject";
            this.btnInject.UseVisualStyleBackColor = false;
            this.btnInject.Click += new System.EventHandler(this.BtnInject_Click);
            // 
            // OptionsPanel
            // 
            this.OptionsPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.OptionsPanel.Controls.Add(this.chkNoRuntime);
            this.OptionsPanel.Controls.Add(this.cmbHotMode);
            this.OptionsPanel.Controls.Add(this.chkHotLoad);
            this.OptionsPanel.Controls.Add(this.chkCompileOnly);
            this.OptionsPanel.Controls.Add(this.chkBuild);
            this.OptionsPanel.Controls.Add(this.chkNoUpdate);
            this.OptionsPanel.Controls.Add(this.cmbGame);
            this.OptionsPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.OptionsPanel.Location = new System.Drawing.Point(0, 60);
            this.OptionsPanel.Name = "OptionsPanel";
            this.OptionsPanel.Size = new System.Drawing.Size(784, 94);
            this.OptionsPanel.TabIndex = 1;
            // 
            // chkNoRuntime
            // 
            this.chkNoRuntime.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.chkNoRuntime.ForeColor = System.Drawing.Color.White;
            this.chkNoRuntime.Location = new System.Drawing.Point(215, 39);
            this.chkNoRuntime.Name = "chkNoRuntime";
            this.chkNoRuntime.Size = new System.Drawing.Size(92, 20);
            this.chkNoRuntime.TabIndex = 6;
            this.chkNoRuntime.Text = "No Runtime";
            this.chkNoRuntime.UseVisualStyleBackColor = false;
            // 
            // btnCompile
            // 
            this.btnCompile.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.btnCompile.FlatAppearance.BorderColor = System.Drawing.Color.DodgerBlue;
            this.btnCompile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCompile.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.btnCompile.ForeColor = System.Drawing.Color.White;
            this.btnCompile.Location = new System.Drawing.Point(672, 6);
            this.btnCompile.Name = "btnCompile";
            this.btnCompile.Size = new System.Drawing.Size(100, 30);
            this.btnCompile.TabIndex = 0;
            this.btnCompile.Text = "Compile";
            this.btnCompile.UseVisualStyleBackColor = false;
            this.btnCompile.Click += new System.EventHandler(this.BtnCompile_Click);
            // 
            // chkHotLoad
            // 
            this.chkHotLoad.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.chkHotLoad.ForeColor = System.Drawing.Color.White;
            this.chkHotLoad.Location = new System.Drawing.Point(215, 65);
            this.chkHotLoad.Name = "chkHotLoad";
            this.chkHotLoad.Size = new System.Drawing.Size(92, 20);
            this.chkHotLoad.TabIndex = 4;
            this.chkHotLoad.Text = "Hot Load";
            this.chkHotLoad.UseVisualStyleBackColor = false;
            this.chkHotLoad.CheckedChanged += new System.EventHandler(this.chkHotLoad_CheckedChanged);
            // 
            // chkCompileOnly
            // 
            this.chkCompileOnly.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.chkCompileOnly.ForeColor = System.Drawing.Color.White;
            this.chkCompileOnly.Location = new System.Drawing.Point(672, 68);
            this.chkCompileOnly.Name = "chkCompileOnly";
            this.chkCompileOnly.Size = new System.Drawing.Size(100, 20);
            this.chkCompileOnly.TabIndex = 3;
            this.chkCompileOnly.Text = "Compile Only";
            this.chkCompileOnly.UseVisualStyleBackColor = false;
            // 
            // chkBuild
            // 
            this.chkBuild.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.chkBuild.ForeColor = System.Drawing.Color.White;
            this.chkBuild.Location = new System.Drawing.Point(672, 42);
            this.chkBuild.Name = "chkBuild";
            this.chkBuild.Size = new System.Drawing.Size(100, 20);
            this.chkBuild.TabIndex = 2;
            this.chkBuild.Text = "Full Build";
            this.chkBuild.UseVisualStyleBackColor = false;
            // 
            // chkNoUpdate
            // 
            this.chkNoUpdate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.chkNoUpdate.ForeColor = System.Drawing.Color.White;
            this.chkNoUpdate.Location = new System.Drawing.Point(215, 13);
            this.chkNoUpdate.Name = "chkNoUpdate";
            this.chkNoUpdate.Size = new System.Drawing.Size(92, 20);
            this.chkNoUpdate.TabIndex = 1;
            this.chkNoUpdate.Text = "No Update";
            this.chkNoUpdate.UseVisualStyleBackColor = false;
            // 
            // cmbGame
            // 
            this.cmbGame.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.cmbGame.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbGame.ForeColor = System.Drawing.Color.White;
            this.cmbGame.FormattingEnabled = true;
            this.cmbGame.Location = new System.Drawing.Point(20, 36);
            this.cmbGame.Name = "cmbGame";
            this.cmbGame.Size = new System.Drawing.Size(180, 23);
            this.cmbGame.TabIndex = 0;
            // 
            // InputPanel
            // 
            this.InputPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.InputPanel.Controls.Add(this.btnBrowse);
            this.InputPanel.Controls.Add(this.txtScriptPath);
            this.InputPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.InputPanel.Location = new System.Drawing.Point(0, 0);
            this.InputPanel.Name = "InputPanel";
            this.InputPanel.Size = new System.Drawing.Size(784, 60);
            this.InputPanel.TabIndex = 0;
            // 
            // btnBrowse
            // 
            this.btnBrowse.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.btnBrowse.FlatAppearance.BorderColor = System.Drawing.Color.DodgerBlue;
            this.btnBrowse.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowse.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.btnBrowse.ForeColor = System.Drawing.Color.White;
            this.btnBrowse.Location = new System.Drawing.Point(526, 20);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(80, 25);
            this.btnBrowse.TabIndex = 1;
            this.btnBrowse.Text = "Browse...";
            this.btnBrowse.UseVisualStyleBackColor = false;
            this.btnBrowse.Click += new System.EventHandler(this.BtnBrowse_Click);
            // 
            // txtScriptPath
            // 
            this.txtScriptPath.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.txtScriptPath.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtScriptPath.ForeColor = System.Drawing.Color.White;
            this.txtScriptPath.Location = new System.Drawing.Point(20, 20);
            this.txtScriptPath.Name = "txtScriptPath";
            this.txtScriptPath.Size = new System.Drawing.Size(500, 23);
            this.txtScriptPath.TabIndex = 0;
            // 
            // MainForm1
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.MainContainer);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ForeColor = System.Drawing.Color.White;
            this.MinimumSize = new System.Drawing.Size(800, 600);
            this.Name = "MainForm1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "T7/T8 Compiler";
            this.Load += new System.EventHandler(this.MainForm1_Load);
            this.MainContainer.ResumeLayout(false);
            this.OutputPanel.ResumeLayout(false);
            this.OutputPanel.PerformLayout();
            this.ControlPanel.ResumeLayout(false);
            this.OptionsPanel.ResumeLayout(false);
            this.InputPanel.ResumeLayout(false);
            this.InputPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel MainContainer;
        private System.Windows.Forms.Panel OutputPanel;
        private System.Windows.Forms.TextBox txtOutput;
        private System.Windows.Forms.Panel ControlPanel;
        private System.Windows.Forms.Button btnResetParseTree;
        private System.Windows.Forms.Button btnInject;
        private System.Windows.Forms.Button btnCompile;
        private System.Windows.Forms.Panel OptionsPanel;
        private System.Windows.Forms.CheckBox chkNoRuntime;
        private System.Windows.Forms.ComboBox cmbHotMode;
        private System.Windows.Forms.CheckBox chkHotLoad;
        private System.Windows.Forms.CheckBox chkCompileOnly;
        private System.Windows.Forms.CheckBox chkBuild;
        private System.Windows.Forms.CheckBox chkNoUpdate;
        private System.Windows.Forms.ComboBox cmbGame;
        private System.Windows.Forms.Panel InputPanel;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.TextBox txtScriptPath;
    }
}