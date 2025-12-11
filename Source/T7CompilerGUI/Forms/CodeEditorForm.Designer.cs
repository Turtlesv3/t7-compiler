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
        private ReaLTaiizor.Manager.PoisonStyleManager poisonStyleManager;

        // UI Controls
        private PoisonPanel mainPanel;
        private PoisonPanel fileListPanel;
        private PoisonPanel editorPanel;
        private PoisonPanel buttonPanel;
        private FlowLayoutPanel fileButtonsPanel;
        private ReaLTaiizor.Controls.PoisonTabControl tabControl;
        private PoisonLabel lblProjectPath;
        private PoisonButton btnOpenFolder;
        private PoisonButton btnRecentProjects;
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
        // File menu items
        private ToolStripMenuItem newProjectItem;
        private ToolStripMenuItem newFileItem;
        private ToolStripSeparator separator1;
        private ToolStripMenuItem saveItem;
        private ToolStripMenuItem saveAllItem;
        private ToolStripSeparator separator2;
        private ToolStripMenuItem refreshItem;
        private ToolStripSeparator separator3;
        private ToolStripMenuItem portILItem;
        private ToolStripSeparator separator4;
        private ToolStripMenuItem shortcutsItem;
        private ToolStripSeparator separator5;
        private ToolStripMenuItem goToLineItem;
        private ToolStripSeparator separator6;
        private ToolStripMenuItem forceHostItem;
        private ToolStripMenuItem resetHostItem;
        private ToolStripSeparator separator7;
        private ToolStripMenuItem updateItem;
        private ToolStripSeparator separator8;
        private ToolStripMenuItem aboutItem;
        private ToolStripSeparator separator9;
        private ToolStripMenuItem exitItem;
        // Inject Precompiled Script menu items
        private ToolStripMenuItem injectBO3Item;
        private ToolStripMenuItem injectBO4Item;
        // Processes menu items
        private ToolStripMenuItem killBO3Item;
        private ToolStripMenuItem killBO4Item;
        private PoisonTextBox txtHashInput;
        private PoisonButton btnHashCheck;
        private PoisonLabel lblHashChecker;
        private PoisonPanel statusPanel;
        private PoisonLabel statusLabel;

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
            this.components = new System.ComponentModel.Container();
            this.poisonStyleManager = new ReaLTaiizor.Manager.PoisonStyleManager(this.components);
            this.mainPanel = new ReaLTaiizor.Controls.PoisonPanel();
            this.editorPanel = new ReaLTaiizor.Controls.PoisonPanel();
            this.tabControl = new ReaLTaiizor.Controls.PoisonTabControl();
            this.fileListPanel = new ReaLTaiizor.Controls.PoisonPanel();
            this.fileButtonsPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.lblProjectPath = new ReaLTaiizor.Controls.PoisonLabel();
            this.btnOpenFolder = new ReaLTaiizor.Controls.PoisonButton();
            this.btnRecentProjects = new ReaLTaiizor.Controls.PoisonButton();
            this.buttonPanel = new ReaLTaiizor.Controls.PoisonPanel();
            this.btnHashCheck = new ReaLTaiizor.Controls.PoisonButton();
            this.txtHashInput = new ReaLTaiizor.Controls.PoisonTextBox();
            this.lblHashChecker = new ReaLTaiizor.Controls.PoisonLabel();
            this.btnCompile = new ReaLTaiizor.Controls.PoisonButton();
            this.mainMenuStrip = new System.Windows.Forms.MenuStrip();
            this.fileMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.newProjectItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newFileItem = new System.Windows.Forms.ToolStripMenuItem();
            this.separator1 = new System.Windows.Forms.ToolStripSeparator();
            this.saveItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAllItem = new System.Windows.Forms.ToolStripMenuItem();
            this.separator2 = new System.Windows.Forms.ToolStripSeparator();
            this.refreshItem = new System.Windows.Forms.ToolStripMenuItem();
            this.separator3 = new System.Windows.Forms.ToolStripSeparator();
            this.portILItem = new System.Windows.Forms.ToolStripMenuItem();
            this.separator4 = new System.Windows.Forms.ToolStripSeparator();
            this.shortcutsItem = new System.Windows.Forms.ToolStripMenuItem();
            this.separator5 = new System.Windows.Forms.ToolStripSeparator();
            this.goToLineItem = new System.Windows.Forms.ToolStripMenuItem();
            this.separator6 = new System.Windows.Forms.ToolStripSeparator();
            this.forceHostItem = new System.Windows.Forms.ToolStripMenuItem();
            this.resetHostItem = new System.Windows.Forms.ToolStripMenuItem();
            this.separator7 = new System.Windows.Forms.ToolStripSeparator();
            this.updateItem = new System.Windows.Forms.ToolStripMenuItem();
            this.separator8 = new System.Windows.Forms.ToolStripSeparator();
            this.aboutItem = new System.Windows.Forms.ToolStripMenuItem();
            this.separator9 = new System.Windows.Forms.ToolStripSeparator();
            this.exitItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gameMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.t7GameItem = new System.Windows.Forms.ToolStripMenuItem();
            this.t8GameItem = new System.Windows.Forms.ToolStripMenuItem();
            this.modeMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.campaignModeItem = new System.Windows.Forms.ToolStripMenuItem();
            this.multiplayerModeItem = new System.Windows.Forms.ToolStripMenuItem();
            this.zombiesModeItem = new System.Windows.Forms.ToolStripMenuItem();
            this.injectPrecompiledMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.injectBO3Item = new System.Windows.Forms.ToolStripMenuItem();
            this.injectBO4Item = new System.Windows.Forms.ToolStripMenuItem();
            this.processesMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.killBO3Item = new System.Windows.Forms.ToolStripMenuItem();
            this.killBO4Item = new System.Windows.Forms.ToolStripMenuItem();
            this.statusPanel = new ReaLTaiizor.Controls.PoisonPanel();
            this.statusLabel = new ReaLTaiizor.Controls.PoisonLabel();
            ((System.ComponentModel.ISupportInitialize)(this.poisonStyleManager)).BeginInit();
            this.mainPanel.SuspendLayout();
            this.editorPanel.SuspendLayout();
            this.fileListPanel.SuspendLayout();
            this.buttonPanel.SuspendLayout();
            this.mainMenuStrip.SuspendLayout();
            this.statusPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // poisonStyleManager
            // 
            this.poisonStyleManager.Owner = this;
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
            this.mainPanel.Size = new System.Drawing.Size(1217, 753);
            this.mainPanel.TabIndex = 0;
            this.mainPanel.UseStyleColors = true;
            this.mainPanel.VerticalScrollbarBarColor = true;
            this.mainPanel.VerticalScrollbarHighlightOnWheel = false;
            this.mainPanel.VerticalScrollbarSize = 10;
            // 
            // editorPanel
            // 
            this.editorPanel.Controls.Add(this.tabControl);
            this.editorPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.editorPanel.HorizontalScrollbarBarColor = true;
            this.editorPanel.HorizontalScrollbarHighlightOnWheel = false;
            this.editorPanel.HorizontalScrollbarSize = 10;
            this.editorPanel.Location = new System.Drawing.Point(293, 10);
            this.editorPanel.Name = "editorPanel";
            this.editorPanel.Padding = new System.Windows.Forms.Padding(5);
            this.editorPanel.Size = new System.Drawing.Size(914, 683);
            this.editorPanel.TabIndex = 2;
            this.editorPanel.UseStyleColors = true;
            this.editorPanel.VerticalScrollbarBarColor = true;
            this.editorPanel.VerticalScrollbarHighlightOnWheel = false;
            this.editorPanel.VerticalScrollbarSize = 10;
            // 
            // tabControl
            // 
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Location = new System.Drawing.Point(5, 5);
            this.tabControl.Name = "tabControl";
            this.tabControl.Size = new System.Drawing.Size(904, 673);
            this.tabControl.TabIndex = 2;
            this.tabControl.UseSelectable = true;
            this.tabControl.UseStyleColors = true;
            this.tabControl.SelectedIndexChanged += new System.EventHandler(this.TabControl_SelectedIndexChanged);
            this.tabControl.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.TabControl_MouseWheel);
            // 
            // fileListPanel
            // 
            this.fileListPanel.Controls.Add(this.fileButtonsPanel);
            this.fileListPanel.Controls.Add(this.lblProjectPath);
            this.fileListPanel.Controls.Add(this.btnOpenFolder);
            this.fileListPanel.Controls.Add(this.btnRecentProjects);
            this.fileListPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.fileListPanel.HorizontalScrollbarBarColor = true;
            this.fileListPanel.HorizontalScrollbarHighlightOnWheel = false;
            this.fileListPanel.HorizontalScrollbarSize = 10;
            this.fileListPanel.Location = new System.Drawing.Point(10, 10);
            this.fileListPanel.Name = "fileListPanel";
            this.fileListPanel.Padding = new System.Windows.Forms.Padding(5);
            this.fileListPanel.Size = new System.Drawing.Size(283, 683);
            this.fileListPanel.TabIndex = 3;
            this.fileListPanel.UseStyleColors = true;
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
            this.fileButtonsPanel.Location = new System.Drawing.Point(5, 90);
            this.fileButtonsPanel.Margin = new System.Windows.Forms.Padding(0);
            this.fileButtonsPanel.Name = "fileButtonsPanel";
            this.fileButtonsPanel.Size = new System.Drawing.Size(273, 588);
            this.fileButtonsPanel.TabIndex = 2;
            this.fileButtonsPanel.WrapContents = false;
            // 
            // lblProjectPath
            // 
            this.lblProjectPath.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblProjectPath.Location = new System.Drawing.Point(5, 65);
            this.lblProjectPath.Name = "lblProjectPath";
            this.lblProjectPath.Size = new System.Drawing.Size(273, 25);
            this.lblProjectPath.TabIndex = 3;
            this.lblProjectPath.Text = "No project opened";
            this.lblProjectPath.UseStyleColors = true;
            // 
            // btnOpenFolder
            // 
            this.btnOpenFolder.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnOpenFolder.Location = new System.Drawing.Point(5, 35);
            this.btnOpenFolder.Name = "btnOpenFolder";
            this.btnOpenFolder.Size = new System.Drawing.Size(273, 30);
            this.btnOpenFolder.TabIndex = 4;
            this.btnOpenFolder.Text = "Open Folder";
            this.btnOpenFolder.UseSelectable = true;
            this.btnOpenFolder.UseStyleColors = true;
            this.btnOpenFolder.Click += new System.EventHandler(this.BtnOpenFolder_Click);
            // 
            // btnRecentProjects
            // 
            this.btnRecentProjects.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnRecentProjects.Location = new System.Drawing.Point(5, 5);
            this.btnRecentProjects.Name = "btnRecentProjects";
            this.btnRecentProjects.Size = new System.Drawing.Size(273, 30);
            this.btnRecentProjects.TabIndex = 5;
            this.btnRecentProjects.Text = "Recent Projects";
            this.btnRecentProjects.UseSelectable = true;
            this.btnRecentProjects.UseStyleColors = true;
            this.btnRecentProjects.Click += new System.EventHandler(this.BtnRecentProjects_Click);
            // 
            // buttonPanel
            // 
            this.buttonPanel.Controls.Add(this.btnHashCheck);
            this.buttonPanel.Controls.Add(this.txtHashInput);
            this.buttonPanel.Controls.Add(this.lblHashChecker);
            this.buttonPanel.Controls.Add(this.btnCompile);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.buttonPanel.HorizontalScrollbarBarColor = true;
            this.buttonPanel.HorizontalScrollbarHighlightOnWheel = false;
            this.buttonPanel.HorizontalScrollbarSize = 10;
            this.buttonPanel.Location = new System.Drawing.Point(10, 693);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Padding = new System.Windows.Forms.Padding(10, 0, 0, 10);
            this.buttonPanel.Size = new System.Drawing.Size(1197, 50);
            this.buttonPanel.TabIndex = 4;
            this.buttonPanel.UseStyleColors = true;
            this.buttonPanel.VerticalScrollbarBarColor = true;
            this.buttonPanel.VerticalScrollbarHighlightOnWheel = false;
            this.buttonPanel.VerticalScrollbarSize = 10;
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
            // txtHashInput
            // 
            // 
            // 
            // 
            this.txtHashInput.CustomButton.Image = null;
            this.txtHashInput.CustomButton.Location = new System.Drawing.Point(128, 1);
            this.txtHashInput.CustomButton.Name = "";
            this.txtHashInput.CustomButton.Size = new System.Drawing.Size(21, 21);
            this.txtHashInput.CustomButton.Style = ReaLTaiizor.Enum.Poison.ColorStyle.Blue;
            this.txtHashInput.CustomButton.TabIndex = 1;
            this.txtHashInput.CustomButton.Theme = ReaLTaiizor.Enum.Poison.ThemeStyle.Light;
            this.txtHashInput.CustomButton.UseSelectable = true;
            this.txtHashInput.CustomButton.Visible = false;
            this.txtHashInput.Lines = new string[0];
            this.txtHashInput.Location = new System.Drawing.Point(266, 15);
            this.txtHashInput.MaxLength = 32767;
            this.txtHashInput.Name = "txtHashInput";
            this.txtHashInput.PasswordChar = '\0';
            this.txtHashInput.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.txtHashInput.SelectedText = "";
            this.txtHashInput.SelectionLength = 0;
            this.txtHashInput.SelectionStart = 0;
            this.txtHashInput.ShortcutsEnabled = true;
            this.txtHashInput.Size = new System.Drawing.Size(150, 23);
            this.txtHashInput.TabIndex = 2;
            this.txtHashInput.UseSelectable = true;
            this.txtHashInput.UseStyleColors = true;
            this.txtHashInput.WaterMarkColor = System.Drawing.Color.FromArgb(((int)(((byte)(109)))), ((int)(((byte)(109)))), ((int)(((byte)(109)))));
            this.txtHashInput.WaterMarkFont = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Pixel);
            this.txtHashInput.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TxtHashInput_KeyDown);
            // 
            // lblHashChecker
            // 
            this.lblHashChecker.AutoSize = true;
            this.lblHashChecker.Location = new System.Drawing.Point(180, 18);
            this.lblHashChecker.Name = "lblHashChecker";
            this.lblHashChecker.Size = new System.Drawing.Size(79, 19);
            this.lblHashChecker.TabIndex = 1;
            this.lblHashChecker.Text = "Hash Check:";
            this.lblHashChecker.UseStyleColors = true;
            // 
            // btnCompile
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
            // mainMenuStrip
            // 
            this.mainMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileMenu,
            this.gameMenu,
            this.modeMenu,
            this.injectPrecompiledMenu,
            this.processesMenu});
            this.mainMenuStrip.Location = new System.Drawing.Point(20, 30);
            this.mainMenuStrip.Name = "mainMenuStrip";
            this.mainMenuStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.mainMenuStrip.Size = new System.Drawing.Size(1217, 24);
            this.mainMenuStrip.TabIndex = 1;
            // 
            // fileMenu
            // 
            this.fileMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newProjectItem,
            this.newFileItem,
            this.separator1,
            this.saveItem,
            this.saveAllItem,
            this.separator2,
            this.refreshItem,
            this.separator3,
            this.portILItem,
            this.separator4,
            this.shortcutsItem,
            this.separator5,
            this.goToLineItem,
            this.separator6,
            this.forceHostItem,
            this.resetHostItem,
            this.separator7,
            this.updateItem,
            this.separator8,
            this.aboutItem,
            this.separator9,
            this.exitItem});
            this.fileMenu.Name = "fileMenu";
            this.fileMenu.Size = new System.Drawing.Size(37, 20);
            this.fileMenu.Text = "File";
            // 
            // newProjectItem
            // 
            this.newProjectItem.Name = "newProjectItem";
            this.newProjectItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.N)));
            this.newProjectItem.ShowShortcutKeys = false;
            this.newProjectItem.Size = new System.Drawing.Size(173, 22);
            this.newProjectItem.Text = "New Project";
            this.newProjectItem.Click += new System.EventHandler(this.NewProjectItem_Click);
            // 
            // newFileItem
            // 
            this.newFileItem.Name = "newFileItem";
            this.newFileItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
            this.newFileItem.ShowShortcutKeys = false;
            this.newFileItem.Size = new System.Drawing.Size(173, 22);
            this.newFileItem.Text = "New File";
            this.newFileItem.Click += new System.EventHandler(this.NewFileItem_Click);
            // 
            // separator1
            // 
            this.separator1.Name = "separator1";
            this.separator1.Size = new System.Drawing.Size(170, 6);
            // 
            // saveItem
            // 
            this.saveItem.Name = "saveItem";
            this.saveItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.saveItem.ShowShortcutKeys = false;
            this.saveItem.Size = new System.Drawing.Size(173, 22);
            this.saveItem.Text = "Save";
            this.saveItem.Click += new System.EventHandler(this.SaveItem_Click);
            // 
            // saveAllItem
            // 
            this.saveAllItem.Name = "saveAllItem";
            this.saveAllItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.S)));
            this.saveAllItem.ShowShortcutKeys = false;
            this.saveAllItem.Size = new System.Drawing.Size(173, 22);
            this.saveAllItem.Text = "Save All";
            this.saveAllItem.Click += new System.EventHandler(this.SaveAllItem_Click);
            // 
            // separator2
            // 
            this.separator2.Name = "separator2";
            this.separator2.Size = new System.Drawing.Size(170, 6);
            // 
            // refreshItem
            // 
            this.refreshItem.Name = "refreshItem";
            this.refreshItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
            this.refreshItem.ShowShortcutKeys = false;
            this.refreshItem.Size = new System.Drawing.Size(173, 22);
            this.refreshItem.Text = "Refresh Files";
            this.refreshItem.Click += new System.EventHandler(this.RefreshItem_Click);
            // 
            // separator3
            // 
            this.separator3.Name = "separator3";
            this.separator3.Size = new System.Drawing.Size(170, 6);
            // 
            // portILItem
            // 
            this.portILItem.Name = "portILItem";
            this.portILItem.ShortcutKeys = ((System.Windows.Forms.Keys)((((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Alt) 
            | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.O)));
            this.portILItem.ShowShortcutKeys = false;
            this.portILItem.Size = new System.Drawing.Size(173, 22);
            this.portILItem.Text = "Port IL Project";
            this.portILItem.Click += new System.EventHandler(this.PortILItem_Click);
            // 
            // separator4
            // 
            this.separator4.Name = "separator4";
            this.separator4.Size = new System.Drawing.Size(170, 6);
            // 
            // shortcutsItem
            // 
            this.shortcutsItem.Name = "shortcutsItem";
            this.shortcutsItem.Size = new System.Drawing.Size(173, 22);
            this.shortcutsItem.Text = "Shortcuts...";
            this.shortcutsItem.Click += new System.EventHandler(this.ShortcutsItem_Click);
            // 
            // separator5
            // 
            this.separator5.Name = "separator5";
            this.separator5.Size = new System.Drawing.Size(170, 6);
            // 
            // goToLineItem
            // 
            this.goToLineItem.Name = "goToLineItem";
            this.goToLineItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.G)));
            this.goToLineItem.ShowShortcutKeys = false;
            this.goToLineItem.Size = new System.Drawing.Size(173, 22);
            this.goToLineItem.Text = "Go to Line...";
            this.goToLineItem.Click += new System.EventHandler(this.GoToLineItem_Click);
            // 
            // separator6
            // 
            this.separator6.Name = "separator6";
            this.separator6.Size = new System.Drawing.Size(170, 6);
            // 
            // forceHostItem
            // 
            this.forceHostItem.Name = "forceHostItem";
            this.forceHostItem.Size = new System.Drawing.Size(173, 22);
            this.forceHostItem.Text = "Force Host";
            this.forceHostItem.ToolTipText = "Force Host - Great for setting peoples stats | Click Me Before Finding a public m" +
    "atch";
            this.forceHostItem.Click += new System.EventHandler(this.ForceHostItem_Click);
            // 
            // resetHostItem
            // 
            this.resetHostItem.Name = "resetHostItem";
            this.resetHostItem.Size = new System.Drawing.Size(173, 22);
            this.resetHostItem.Text = "Reset Host Dvars";
            this.resetHostItem.ToolTipText = "Reset Dvars";
            this.resetHostItem.Click += new System.EventHandler(this.ResetHostItem_Click);
            // 
            // separator7
            // 
            this.separator7.Name = "separator7";
            this.separator7.Size = new System.Drawing.Size(170, 6);
            // 
            // updateItem
            // 
            this.updateItem.Name = "updateItem";
            this.updateItem.Size = new System.Drawing.Size(173, 22);
            this.updateItem.Text = "Check For Updates";
            this.updateItem.ToolTipText = "Check For Updates";
            this.updateItem.Click += new System.EventHandler(this.UpdateItem_Click);
            // 
            // separator8
            // 
            this.separator8.Name = "separator8";
            this.separator8.Size = new System.Drawing.Size(170, 6);
            // 
            // aboutItem
            // 
            this.aboutItem.Name = "aboutItem";
            this.aboutItem.Size = new System.Drawing.Size(173, 22);
            this.aboutItem.Text = "About";
            this.aboutItem.Click += new System.EventHandler(this.AboutItem_Click);
            // 
            // separator9
            // 
            this.separator9.Name = "separator9";
            this.separator9.Size = new System.Drawing.Size(170, 6);
            // 
            // exitItem
            // 
            this.exitItem.Name = "exitItem";
            this.exitItem.Size = new System.Drawing.Size(173, 22);
            this.exitItem.Text = "Exit";
            this.exitItem.Click += new System.EventHandler(this.ExitItem_Click);
            // 
            // gameMenu
            // 
            this.gameMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.t7GameItem,
            this.t8GameItem});
            this.gameMenu.Name = "gameMenu";
            this.gameMenu.Size = new System.Drawing.Size(50, 20);
            this.gameMenu.Text = "Game";
            // 
            // t7GameItem
            // 
            this.t7GameItem.Checked = true;
            this.t7GameItem.CheckOnClick = true;
            this.t7GameItem.Name = "t7GameItem";
            this.t7GameItem.Size = new System.Drawing.Size(135, 22);
            this.t7GameItem.Text = "Black Ops 3";
            this.t7GameItem.Click += new System.EventHandler(this.T7GameItem_Click);
            // 
            // t8GameItem
            // 
            this.t8GameItem.CheckOnClick = true;
            this.t8GameItem.Name = "t8GameItem";
            this.t8GameItem.Size = new System.Drawing.Size(135, 22);
            this.t8GameItem.Text = "Black Ops 4";
            this.t8GameItem.Click += new System.EventHandler(this.T8GameItem_Click);
            // 
            // modeMenu
            // 
            this.modeMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.campaignModeItem,
            this.multiplayerModeItem,
            this.zombiesModeItem});
            this.modeMenu.Name = "modeMenu";
            this.modeMenu.Size = new System.Drawing.Size(50, 20);
            this.modeMenu.Text = "Mode";
            // 
            // campaignModeItem
            // 
            this.campaignModeItem.CheckOnClick = true;
            this.campaignModeItem.Name = "campaignModeItem";
            this.campaignModeItem.Size = new System.Drawing.Size(134, 22);
            this.campaignModeItem.Text = "Campaign";
            this.campaignModeItem.Click += new System.EventHandler(this.CampaignModeItem_Click);
            // 
            // multiplayerModeItem
            // 
            this.multiplayerModeItem.CheckOnClick = true;
            this.multiplayerModeItem.Name = "multiplayerModeItem";
            this.multiplayerModeItem.Size = new System.Drawing.Size(134, 22);
            this.multiplayerModeItem.Text = "Multiplayer";
            this.multiplayerModeItem.Click += new System.EventHandler(this.MultiplayerModeItem_Click);
            // 
            // zombiesModeItem
            // 
            this.zombiesModeItem.Checked = true;
            this.zombiesModeItem.CheckOnClick = true;
            this.zombiesModeItem.Name = "zombiesModeItem";
            this.zombiesModeItem.Size = new System.Drawing.Size(134, 22);
            this.zombiesModeItem.Text = "Zombies";
            this.zombiesModeItem.Click += new System.EventHandler(this.ZombiesModeItem_Click);
            // 
            // injectPrecompiledMenu
            // 
            this.injectPrecompiledMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.injectBO3Item,
            this.injectBO4Item});
            this.injectPrecompiledMenu.Name = "injectPrecompiledMenu";
            this.injectPrecompiledMenu.Size = new System.Drawing.Size(151, 20);
            this.injectPrecompiledMenu.Text = "Inject Precompiled Script";
            // 
            // injectBO3Item
            // 
            this.injectBO3Item.Name = "injectBO3Item";
            this.injectBO3Item.Size = new System.Drawing.Size(135, 22);
            this.injectBO3Item.Text = "Black Ops 3";
            this.injectBO3Item.Click += new System.EventHandler(this.InjectBO3Item_Click);
            // 
            // injectBO4Item
            // 
            this.injectBO4Item.Name = "injectBO4Item";
            this.injectBO4Item.Size = new System.Drawing.Size(135, 22);
            this.injectBO4Item.Text = "Black Ops 4";
            this.injectBO4Item.Click += new System.EventHandler(this.InjectBO4Item_Click);
            // 
            // processesMenu
            // 
            this.processesMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.killBO3Item,
            this.killBO4Item});
            this.processesMenu.Name = "processesMenu";
            this.processesMenu.Size = new System.Drawing.Size(70, 20);
            this.processesMenu.Text = "Processes";
            // 
            // killBO3Item
            // 
            this.killBO3Item.Name = "killBO3Item";
            this.killBO3Item.Size = new System.Drawing.Size(154, 22);
            this.killBO3Item.Text = "Kill Black Ops 3";
            this.killBO3Item.ToolTipText = "Kills Game Process";
            this.killBO3Item.Click += new System.EventHandler(this.KillBO3Item_Click);
            // 
            // killBO4Item
            // 
            this.killBO4Item.Name = "killBO4Item";
            this.killBO4Item.Size = new System.Drawing.Size(154, 22);
            this.killBO4Item.Text = "Kill Black Ops 4";
            this.killBO4Item.ToolTipText = "Kills Game Process";
            this.killBO4Item.Click += new System.EventHandler(this.KillBO4Item_Click);
            // 
            // statusPanel
            // 
            this.statusPanel.Controls.Add(this.statusLabel);
            this.statusPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.statusPanel.HorizontalScrollbarBarColor = true;
            this.statusPanel.HorizontalScrollbarHighlightOnWheel = false;
            this.statusPanel.HorizontalScrollbarSize = 10;
            this.statusPanel.Location = new System.Drawing.Point(20, 807);
            this.statusPanel.Name = "statusPanel";
            this.statusPanel.Padding = new System.Windows.Forms.Padding(10, 5, 10, 5);
            this.statusPanel.Size = new System.Drawing.Size(1217, 22);
            this.statusPanel.TabIndex = 2;
            this.statusPanel.UseStyleColors = true;
            this.statusPanel.VerticalScrollbarBarColor = true;
            this.statusPanel.VerticalScrollbarHighlightOnWheel = false;
            this.statusPanel.VerticalScrollbarSize = 10;
            // 
            // statusLabel
            // 
            this.statusLabel.AutoSize = true;
            this.statusLabel.Dock = System.Windows.Forms.DockStyle.Left;
            this.statusLabel.Location = new System.Drawing.Point(10, 5);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(45, 19);
            this.statusLabel.TabIndex = 0;
            this.statusLabel.Text = "Ready";
            this.statusLabel.UseStyleColors = true;
            // 
            // CodeEditorForm
            // 
            this.ClientSize = new System.Drawing.Size(1257, 849);
            this.Controls.Add(this.mainPanel);
            this.Controls.Add(this.mainMenuStrip);
            this.Controls.Add(this.statusPanel);
            this.DisplayHeader = false;
            this.KeyPreview = true;
            this.MainMenuStrip = this.mainMenuStrip;
            this.MinimumSize = new System.Drawing.Size(800, 600);
            this.Name = "CodeEditorForm";
            this.Padding = new System.Windows.Forms.Padding(20, 30, 20, 20);
            this.PoisonBorderStyle = ReaLTaiizor.Enum.Poison.FormBorderStyle.FixedSingle;
            this.ShadowType = ReaLTaiizor.Enum.Poison.FormShadowType.AeroShadow;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.StyleManager = this.poisonStyleManager;
            this.Text = "Code Editor";
            ((System.ComponentModel.ISupportInitialize)(this.poisonStyleManager)).EndInit();
            this.mainPanel.ResumeLayout(false);
            this.editorPanel.ResumeLayout(false);
            this.fileListPanel.ResumeLayout(false);
            this.buttonPanel.ResumeLayout(false);
            this.buttonPanel.PerformLayout();
            this.mainMenuStrip.ResumeLayout(false);
            this.mainMenuStrip.PerformLayout();
            this.statusPanel.ResumeLayout(false);
            this.statusPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}

