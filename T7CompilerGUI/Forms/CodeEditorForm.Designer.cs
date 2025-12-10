using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Manager;

namespace T7CompilerGUI.Forms
{
    partial class CodeEditorForm
    {
        private System.ComponentModel.IContainer components = null;

        // UI Controls
        private PoisonPanel mainPanel;
        private PoisonPanel fileListPanel;
        private PoisonPanel editorPanel;
        private PoisonPanel buttonPanel;
        private FlowLayoutPanel fileButtonsPanel;
        private ReaLTaiizor.Controls.PoisonTabControl tabControl;
        private PoisonLabel lblProjectPath;
        private PoisonButton btnCompile;
        private MenuStrip mainMenuStrip;
        private ToolStripMenuItem fileMenu;
        private ToolStripMenuItem gameMenu;
        private ToolStripMenuItem modeMenu;
        private ToolStripMenuItem symbolsMenu;
        private ToolStripMenuItem injectPrecompiledMenu;
        private ToolStripMenuItem processesMenu;
        private ToolStripMenuItem t7GameItem;
        private ToolStripMenuItem t8GameItem;
        private ToolStripMenuItem campaignModeItem;
        private ToolStripMenuItem multiplayerModeItem;
        private ToolStripMenuItem zombiesModeItem;
        private PoisonTextBox txtHashInput;
        private PoisonButton btnHashCheck;
        private PoisonLabel lblHashChecker;

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
            this.mainPanel = new ReaLTaiizor.Controls.PoisonPanel();
            this.editorPanel = new ReaLTaiizor.Controls.PoisonPanel();
            this.tabControl = new ReaLTaiizor.Controls.PoisonTabControl();
            this.fileListPanel = new ReaLTaiizor.Controls.PoisonPanel();
            this.fileButtonsPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.lblProjectPath = new ReaLTaiizor.Controls.PoisonLabel();
            this.buttonPanel = new ReaLTaiizor.Controls.PoisonPanel();
            this.btnCompile = new ReaLTaiizor.Controls.PoisonButton();
            this.lblHashChecker = new ReaLTaiizor.Controls.PoisonLabel();
            this.txtHashInput = new ReaLTaiizor.Controls.PoisonTextBox();
            this.btnHashCheck = new ReaLTaiizor.Controls.PoisonButton();
            this.mainMenuStrip = new System.Windows.Forms.MenuStrip();
            this.mainPanel.SuspendLayout();
            this.editorPanel.SuspendLayout();
            this.fileListPanel.SuspendLayout();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainPanel
            // 
            this.mainPanel.Controls.Add(this.editorPanel);
            this.mainPanel.Controls.Add(this.fileListPanel);
            this.mainPanel.Controls.Add(this.buttonPanel);
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.HorizontalScrollbarBarColor = true;
            this.mainPanel.HorizontalScrollbarHighlightOnWheel = false;
            this.mainPanel.HorizontalScrollbarSize = 10;
            this.mainPanel.Location = new System.Drawing.Point(20, 54);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Padding = new System.Windows.Forms.Padding(10);
            this.mainPanel.Size = new System.Drawing.Size(1168, 827);
            this.mainPanel.TabIndex = 0;
            this.mainPanel.VerticalScrollbarBarColor = true;
            this.mainPanel.VerticalScrollbarHighlightOnWheel = false;
            this.mainPanel.VerticalScrollbarSize = 10;
            // 
            // editorPanel
            // 
            this.editorPanel.Controls.Add(this.tabControl);
            this.editorPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.editorPanel.HorizontalScrollbar = false;
            this.editorPanel.HorizontalScrollbarBarColor = true;
            this.editorPanel.HorizontalScrollbarHighlightOnWheel = false;
            this.editorPanel.HorizontalScrollbarSize = 10;
            this.editorPanel.Location = new System.Drawing.Point(190, 30);
            this.editorPanel.Name = "editorPanel";
            this.editorPanel.Padding = new System.Windows.Forms.Padding(5);
            this.editorPanel.Size = new System.Drawing.Size(968, 737);
            this.editorPanel.TabIndex = 2;
            this.editorPanel.VerticalScrollbar = false;
            this.editorPanel.VerticalScrollbarBarColor = true;
            this.editorPanel.VerticalScrollbarHighlightOnWheel = false;
            this.editorPanel.VerticalScrollbarSize = 10;
            // 
            // tabControl
            // 
            this.tabControl.Appearance = System.Windows.Forms.TabAppearance.Normal;
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Location = new System.Drawing.Point(5, 5);
            this.tabControl.Name = "tabControl";
            this.tabControl.Size = new System.Drawing.Size(958, 717);
            this.tabControl.TabIndex = 2;
            this.tabControl.UseSelectable = true;
            this.tabControl.SizeMode = System.Windows.Forms.TabSizeMode.Normal;
            this.tabControl.Multiline = false;
            this.tabControl.SelectedIndexChanged += new System.EventHandler(this.TabControl_SelectedIndexChanged);
            // 
            // fileListPanel
            // 
            this.fileListPanel.Controls.Add(this.fileButtonsPanel);
            this.fileListPanel.Controls.Add(this.lblProjectPath);
            this.fileListPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.fileListPanel.HorizontalScrollbarBarColor = true;
            this.fileListPanel.HorizontalScrollbarHighlightOnWheel = false;
            this.fileListPanel.HorizontalScrollbarSize = 10;
            this.fileListPanel.Location = new System.Drawing.Point(10, 30);
            this.fileListPanel.Name = "fileListPanel";
            this.fileListPanel.Padding = new System.Windows.Forms.Padding(5);
            this.fileListPanel.Size = new System.Drawing.Size(180, 737);
            this.fileListPanel.TabIndex = 3;
            this.fileListPanel.VerticalScrollbarBarColor = true;
            this.fileListPanel.VerticalScrollbarHighlightOnWheel = false;
            this.fileListPanel.VerticalScrollbarSize = 10;
            // 
            // fileButtonsPanel
            // 
            this.fileButtonsPanel.AutoScroll = true;
            this.fileButtonsPanel.BackColor = System.Drawing.Color.Transparent;
            this.fileButtonsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.fileButtonsPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.fileButtonsPanel.Location = new System.Drawing.Point(5, 30);
            this.fileButtonsPanel.Name = "fileButtonsPanel";
            this.fileButtonsPanel.Size = new System.Drawing.Size(170, 692);
            this.fileButtonsPanel.TabIndex = 2;
            this.fileButtonsPanel.WrapContents = false;
            // 
            // lblProjectPath
            // 
            this.lblProjectPath.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblProjectPath.Location = new System.Drawing.Point(5, 5);
            this.lblProjectPath.Name = "lblProjectPath";
            this.lblProjectPath.Size = new System.Drawing.Size(170, 25);
            this.lblProjectPath.TabIndex = 3;
            this.lblProjectPath.Text = "No project opened";
            this.lblProjectPath.UseStyleColors = true;
            // 
            // buttonPanel - Compile button and Hash Checker
            // 
            this.buttonPanel.AutoScroll = false;
            this.buttonPanel.Controls.Add(this.btnHashCheck);
            this.buttonPanel.Controls.Add(this.txtHashInput);
            this.buttonPanel.Controls.Add(this.lblHashChecker);
            this.buttonPanel.Controls.Add(this.btnCompile);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.buttonPanel.HorizontalScrollbar = false;
            this.buttonPanel.HorizontalScrollbarBarColor = true;
            this.buttonPanel.HorizontalScrollbarHighlightOnWheel = false;
            this.buttonPanel.HorizontalScrollbarSize = 10;
            this.buttonPanel.Location = new System.Drawing.Point(10, 777);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Padding = new System.Windows.Forms.Padding(10, 0, 0, 10);
            this.buttonPanel.Size = new System.Drawing.Size(1148, 50);
            this.buttonPanel.TabIndex = 4;
            this.buttonPanel.VerticalScrollbar = false;
            this.buttonPanel.VerticalScrollbarBarColor = true;
            this.buttonPanel.VerticalScrollbarHighlightOnWheel = false;
            this.buttonPanel.VerticalScrollbarSize = 10;
            // 
            // btnCompile (moved to bottom left, matching original Compiler UI)
            // 
            this.btnCompile.Location = new System.Drawing.Point(10, 10);
            this.btnCompile.Name = "btnCompile";
            this.btnCompile.Size = new System.Drawing.Size(160, 30);
            this.btnCompile.TabIndex = 0;
            this.btnCompile.Text = "Compile";
            this.btnCompile.UseSelectable = true;
            this.btnCompile.UseStyleColors = true;
            this.btnCompile.Click += new System.EventHandler(this.BtnCompile_Click);
            // 
            // lblHashChecker
            // 
            this.lblHashChecker.AutoSize = true;
            this.lblHashChecker.Location = new System.Drawing.Point(180, 18);
            this.lblHashChecker.Name = "lblHashChecker";
            this.lblHashChecker.Size = new System.Drawing.Size(80, 19);
            this.lblHashChecker.TabIndex = 1;
            this.lblHashChecker.Text = "Hash Check:";
            this.lblHashChecker.UseStyleColors = true;
            // 
            // txtHashInput
            // 
            this.txtHashInput.Location = new System.Drawing.Point(266, 15);
            this.txtHashInput.Name = "txtHashInput";
            this.txtHashInput.Size = new System.Drawing.Size(150, 23);
            this.txtHashInput.TabIndex = 2;
            this.txtHashInput.UseStyleColors = true;
            this.txtHashInput.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TxtHashInput_KeyDown);
            // 
            // btnHashCheck
            // 
            this.btnHashCheck.Location = new System.Drawing.Point(422, 13);
            this.btnHashCheck.Name = "btnHashCheck";
            this.btnHashCheck.Size = new System.Drawing.Size(100, 25);
            this.btnHashCheck.TabIndex = 3;
            this.btnHashCheck.Text = "Find Function";
            this.btnHashCheck.UseSelectable = true;
            this.btnHashCheck.UseStyleColors = true;
            this.btnHashCheck.Click += new System.EventHandler(this.BtnHashCheck_Click);
            // 
            // mainMenuStrip
            // 
            this.mainMenuStrip.Location = new System.Drawing.Point(20, 30);
            this.mainMenuStrip.Name = "mainMenuStrip";
            this.mainMenuStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.mainMenuStrip.Size = new System.Drawing.Size(1168, 24);
            this.mainMenuStrip.TabIndex = 1;
            // 
            // CodeEditorForm
            // 
            this.ClientSize = new System.Drawing.Size(1208, 901);
            this.Controls.Add(this.mainPanel);
            this.Controls.Add(this.mainMenuStrip);
            this.KeyPreview = true;
            this.MainMenuStrip = this.mainMenuStrip;
            this.MinimumSize = new System.Drawing.Size(800, 600);
            this.Name = "CodeEditorForm";
            this.Text = "Code Editor";
            this.mainPanel.ResumeLayout(false);
            this.editorPanel.ResumeLayout(false);
            this.fileListPanel.ResumeLayout(false);
            this.buttonPanel.ResumeLayout(false);
            this.buttonPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}

