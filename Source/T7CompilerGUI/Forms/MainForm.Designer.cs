using System.Windows.Forms;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Extension.Poison;
using ReaLTaiizor.Manager;

namespace T7CompilerGUI.Forms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private ReaLTaiizor.Controls.PoisonTabControl tabControl;
        private ReaLTaiizor.Controls.PoisonTabPage tabCompile;
        private ReaLTaiizor.Controls.PoisonTabPage tabInject;

        // Compile tab controls
        private ReaLTaiizor.Controls.PoisonLabel lblProjectFolder;
        private PoisonTextBox txtProjectFolder;
        private PoisonButton btnSelectProjectFolder;
        private ReaLTaiizor.Controls.PoisonDropDownButton btnRecentProjects;
        private ReaLTaiizor.Controls.PoisonDropDownButton btnGscConfMode;
        private ReaLTaiizor.Controls.PoisonLabel lblOutputFile;
        private PoisonTextBox txtOutputFile;
        private PoisonButton btnSelectOutputFile;
        private ReaLTaiizor.Controls.PoisonLabel lblOpcodeMasking;
        private ReaLTaiizor.Controls.PoisonToggle chkOpcodeMasking;
        private ReaLTaiizor.Controls.PoisonLabel lblSaveOpcodeMap;
        private ReaLTaiizor.Controls.PoisonToggle chkSaveOpcodeMap;
        private ReaLTaiizor.Controls.PoisonDropDownButton btnPlatform;
        private ReaLTaiizor.Controls.PoisonContextMenuStrip platformMenu;
        private ReaLTaiizor.Controls.PoisonDropDownButton btnGame;
        private ReaLTaiizor.Controls.PoisonContextMenuStrip gameMenu;
        private PoisonButton btnCompile;
        private PoisonButton btnCodeEditor;

        // Inject tab controls
        private ReaLTaiizor.Controls.PoisonLabel lblInjectFile;
        private PoisonTextBox txtInjectFile;
        private PoisonButton btnSelectInjectFile;
        private ReaLTaiizor.Controls.PoisonDropDownButton btnRecentFiles;
        private ReaLTaiizor.Controls.PoisonLabel lblInjectPath;
        private PoisonTextBox txtInjectPath;
        private ReaLTaiizor.Controls.PoisonLabel lblInjectGame;
        private ReaLTaiizor.Controls.PoisonDropDownButton btnInjectGame;
        private ReaLTaiizor.Controls.PoisonContextMenuStrip injectGameMenu;
        private ReaLTaiizor.Controls.PoisonLabel lblNoRuntime;
        private ReaLTaiizor.Controls.PoisonToggle chkNoRuntime;
        private ReaLTaiizor.Controls.PoisonLabel lblHotReload;
        private ReaLTaiizor.Controls.PoisonDropDownButton btnHotReload;
        private ReaLTaiizor.Controls.PoisonContextMenuStrip hotReloadMenu;
        private PoisonButton btnInject;
        private PoisonButton btnResetParseTree;
        private PoisonButton btnLaunchBO3;
        private ReaLTaiizor.Controls.PoisonPanel panelLog;
        private ReaLTaiizor.Controls.PoisonProgressSpinner progressSpinner;
        private ReaLTaiizor.Controls.PoisonProgressBar progressBar;
        private ReaLTaiizor.Controls.PoisonToolTip poisonToolTip;

        // Log panel controls
        private ReaLTaiizor.Controls.PoisonPanel panelLogControls;
        private PoisonButton btnLogClear;
        private PoisonButton btnLogCopy;
        private PoisonButton btnLogSave;
        private PoisonTextBox txtLogSearch;
        private ReaLTaiizor.Controls.PoisonLabel lblLogSearch;

        // Settings tab
        private ReaLTaiizor.Controls.PoisonTabPage tabSettings;
        private ReaLTaiizor.Controls.PoisonLabel lblSettingsTheme;
        private ReaLTaiizor.Controls.PoisonToggle toggleSettingsTheme;
        private ReaLTaiizor.Controls.PoisonLabel lblSettingsColorStyle;
        private ReaLTaiizor.Controls.PoisonDropDownButton btnSettingsColorStyle;
        private ReaLTaiizor.Controls.PoisonContextMenuStrip settingsColorStyleMenu;
        private ReaLTaiizor.Controls.PoisonContextMenuStrip mainFormContextMenu;
        private ReaLTaiizor.Controls.PoisonContextMenuStrip recentProjectsMenu;
        private ReaLTaiizor.Controls.PoisonContextMenuStrip recentFilesMenu;
        private ReaLTaiizor.Controls.PoisonLabel lblSettingsDefaultOutputPath;
        private ReaLTaiizor.Controls.PoisonTextBox txtSettingsDefaultOutputPath;
        private ReaLTaiizor.Controls.PoisonButton btnSettingsBrowseOutputPath;
        private ReaLTaiizor.Controls.PoisonContextMenuStrip settingsContextMenu;

        // About tab
        private ReaLTaiizor.Controls.PoisonTabPage tabAbout;
        private ReaLTaiizor.Controls.PoisonLabel lblVersion;
        private ReaLTaiizor.Controls.PoisonLabel lblAbout;

        // Game status indicator
        private ReaLTaiizor.Controls.PoisonLabel lblGameStatus;
        // gameStatusTimer is defined in MainForm.cs

        // Style Management
        private ReaLTaiizor.Manager.PoisonStyleManager poisonStyleManager;
        private ReaLTaiizor.Controls.PoisonStyleExtender poisonStyleExtender;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.poisonStyleManager = new ReaLTaiizor.Manager.PoisonStyleManager(this.components);
            this.poisonStyleExtender = new ReaLTaiizor.Controls.PoisonStyleExtender(this.components);
            this.txtLog = new System.Windows.Forms.RichTextBox();
            this.platformMenu = new ReaLTaiizor.Controls.PoisonContextMenuStrip(this.components);
            this.gameMenu = new ReaLTaiizor.Controls.PoisonContextMenuStrip(this.components);
            this.injectGameMenu = new ReaLTaiizor.Controls.PoisonContextMenuStrip(this.components);
            this.settingsColorStyleMenu = new ReaLTaiizor.Controls.PoisonContextMenuStrip(this.components);
            this.mainFormContextMenu = new ReaLTaiizor.Controls.PoisonContextMenuStrip(this.components);
            this.recentProjectsMenu = new ReaLTaiizor.Controls.PoisonContextMenuStrip(this.components);
            this.recentFilesMenu = new ReaLTaiizor.Controls.PoisonContextMenuStrip(this.components);
            this.lblProjectFolder = new ReaLTaiizor.Controls.PoisonLabel();
            this.lblOutputFile = new ReaLTaiizor.Controls.PoisonLabel();
            this.lblOpcodeMasking = new ReaLTaiizor.Controls.PoisonLabel();
            this.chkOpcodeMasking = new ReaLTaiizor.Controls.PoisonToggle();
            this.lblSaveOpcodeMap = new ReaLTaiizor.Controls.PoisonLabel();
            this.chkSaveOpcodeMap = new ReaLTaiizor.Controls.PoisonToggle();
            this.tabControl = new ReaLTaiizor.Controls.PoisonTabControl();
            this.tabCompile = new ReaLTaiizor.Controls.PoisonTabPage();
            this.btnGscConfMode = new ReaLTaiizor.Controls.PoisonDropDownButton();
            this.txtProjectFolder = new ReaLTaiizor.Controls.PoisonTextBox();
            this.btnSelectProjectFolder = new ReaLTaiizor.Controls.PoisonButton();
            this.btnRecentProjects = new ReaLTaiizor.Controls.PoisonDropDownButton();
            this.txtOutputFile = new ReaLTaiizor.Controls.PoisonTextBox();
            this.btnSelectOutputFile = new ReaLTaiizor.Controls.PoisonButton();
            this.btnPlatform = new ReaLTaiizor.Controls.PoisonDropDownButton();
            this.btnGame = new ReaLTaiizor.Controls.PoisonDropDownButton();
            this.btnCompile = new ReaLTaiizor.Controls.PoisonButton();
            this.btnCodeEditor = new ReaLTaiizor.Controls.PoisonButton();
            this.tabInject = new ReaLTaiizor.Controls.PoisonTabPage();
            this.lblInjectFile = new ReaLTaiizor.Controls.PoisonLabel();
            this.lblInjectPath = new ReaLTaiizor.Controls.PoisonLabel();
            this.lblInjectGame = new ReaLTaiizor.Controls.PoisonLabel();
            this.lblNoRuntime = new ReaLTaiizor.Controls.PoisonLabel();
            this.chkNoRuntime = new ReaLTaiizor.Controls.PoisonToggle();
            this.lblHotReload = new ReaLTaiizor.Controls.PoisonLabel();
            this.btnHotReload = new ReaLTaiizor.Controls.PoisonDropDownButton();
            this.hotReloadMenu = new ReaLTaiizor.Controls.PoisonContextMenuStrip(this.components);
            this.txtInjectFile = new ReaLTaiizor.Controls.PoisonTextBox();
            this.btnSelectInjectFile = new ReaLTaiizor.Controls.PoisonButton();
            this.txtInjectPath = new ReaLTaiizor.Controls.PoisonTextBox();
            this.btnInjectGame = new ReaLTaiizor.Controls.PoisonDropDownButton();
            this.btnInject = new ReaLTaiizor.Controls.PoisonButton();
            this.btnResetParseTree = new ReaLTaiizor.Controls.PoisonButton();
            this.btnLaunchBO3 = new ReaLTaiizor.Controls.PoisonButton();
            this.tabSettings = new ReaLTaiizor.Controls.PoisonTabPage();
            this.lblSettingsTheme = new ReaLTaiizor.Controls.PoisonLabel();
            this.toggleSettingsTheme = new ReaLTaiizor.Controls.PoisonToggle();
            this.lblSettingsColorStyle = new ReaLTaiizor.Controls.PoisonLabel();
            this.btnSettingsColorStyle = new ReaLTaiizor.Controls.PoisonDropDownButton();
            this.lblSettingsDefaultOutputPath = new ReaLTaiizor.Controls.PoisonLabel();
            this.txtSettingsDefaultOutputPath = new ReaLTaiizor.Controls.PoisonTextBox();
            this.btnSettingsBrowseOutputPath = new ReaLTaiizor.Controls.PoisonButton();
            this.tabAbout = new ReaLTaiizor.Controls.PoisonTabPage();
            this.lblVersion = new ReaLTaiizor.Controls.PoisonLabel();
            this.lblAbout = new ReaLTaiizor.Controls.PoisonLabel();
            this.btnRecentFiles = new ReaLTaiizor.Controls.PoisonDropDownButton();
            this.settingsContextMenu = new ReaLTaiizor.Controls.PoisonContextMenuStrip(this.components);
            this.panelLog = new ReaLTaiizor.Controls.PoisonPanel();
            this.progressSpinner = new ReaLTaiizor.Controls.PoisonProgressSpinner();
            this.progressBar = new ReaLTaiizor.Controls.PoisonProgressBar();
            this.poisonToolTip = new ReaLTaiizor.Controls.PoisonToolTip();
            this.panelLogControls = new ReaLTaiizor.Controls.PoisonPanel();
            this.btnLogClear = new ReaLTaiizor.Controls.PoisonButton();
            this.btnLogCopy = new ReaLTaiizor.Controls.PoisonButton();
            this.btnLogSave = new ReaLTaiizor.Controls.PoisonButton();
            this.lblLogSearch = new ReaLTaiizor.Controls.PoisonLabel();
            this.txtLogSearch = new ReaLTaiizor.Controls.PoisonTextBox();
            this.lblGameStatus = new ReaLTaiizor.Controls.PoisonLabel();
            ((System.ComponentModel.ISupportInitialize)(this.poisonStyleManager)).BeginInit();
            this.tabControl.SuspendLayout();
            this.tabCompile.SuspendLayout();
            this.tabInject.SuspendLayout();
            this.tabSettings.SuspendLayout();
            this.tabAbout.SuspendLayout();
            this.panelLog.SuspendLayout();
            this.panelLogControls.SuspendLayout();
            this.SuspendLayout();
            // 
            // poisonStyleManager
            // 
            this.poisonStyleManager.Owner = this;
            // 
            // txtLog
            // 
            this.poisonStyleExtender.SetApplyPoisonTheme(this.txtLog, true);
            this.txtLog.AllowDrop = false;
            this.txtLog.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtLog.Location = new System.Drawing.Point(0, 0);
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.txtLog.Size = new System.Drawing.Size(613, 220);
            this.txtLog.TabIndex = 0;
            this.txtLog.Text = "";
            this.txtLog.WordWrap = false;
            this.txtLog.MouseDown += new System.Windows.Forms.MouseEventHandler(this.txtLog_MouseDown);
            this.txtLog.QueryContinueDrag += new System.Windows.Forms.QueryContinueDragEventHandler(this.txtLog_QueryContinueDrag);
            // 
            // platformMenu
            // 
            this.platformMenu.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.platformMenu.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.platformMenu.Name = "platformMenu";
            this.platformMenu.Size = new System.Drawing.Size(61, 4);
            // 
            // gameMenu
            // 
            this.gameMenu.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.gameMenu.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.gameMenu.Name = "gameMenu";
            this.gameMenu.Size = new System.Drawing.Size(61, 4);
            // 
            // injectGameMenu
            // 
            this.injectGameMenu.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.injectGameMenu.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.injectGameMenu.Name = "injectGameMenu";
            this.injectGameMenu.Size = new System.Drawing.Size(61, 4);
            // 
            // settingsColorStyleMenu
            // 
            this.settingsColorStyleMenu.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.settingsColorStyleMenu.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.settingsColorStyleMenu.Name = "settingsColorStyleMenu";
            this.settingsColorStyleMenu.Size = new System.Drawing.Size(61, 4);
            // 
            // mainFormContextMenu
            // 
            this.mainFormContextMenu.Name = "mainFormContextMenu";
            this.mainFormContextMenu.Size = new System.Drawing.Size(61, 4);
            // 
            // recentProjectsMenu
            // 
            this.recentProjectsMenu.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.recentProjectsMenu.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.recentProjectsMenu.Name = "recentProjectsMenu";
            this.recentProjectsMenu.Size = new System.Drawing.Size(61, 4);
            // 
            // recentFilesMenu
            // 
            this.recentFilesMenu.Name = "recentFilesMenu";
            this.recentFilesMenu.Size = new System.Drawing.Size(61, 4);
            // 
            // lblProjectFolder
            // 
            this.lblProjectFolder.AutoSize = true;
            this.lblProjectFolder.Location = new System.Drawing.Point(-4, 8);
            this.lblProjectFolder.Name = "lblProjectFolder";
            this.lblProjectFolder.Size = new System.Drawing.Size(96, 19);
            this.lblProjectFolder.TabIndex = 5;
            this.lblProjectFolder.Text = "Project Folder:";
            this.lblProjectFolder.UseStyleColors = true;
            // 
            // lblOutputFile
            // 
            this.lblOutputFile.AutoSize = true;
            this.lblOutputFile.Location = new System.Drawing.Point(-4, 36);
            this.lblOutputFile.Name = "lblOutputFile";
            this.lblOutputFile.Size = new System.Drawing.Size(77, 19);
            this.lblOutputFile.TabIndex = 8;
            this.lblOutputFile.Text = "Output File:";
            this.lblOutputFile.UseStyleColors = true;
            // 
            // lblOpcodeMasking
            // 
            this.lblOpcodeMasking.AutoSize = true;
            this.lblOpcodeMasking.Location = new System.Drawing.Point(0, 92);
            this.lblOpcodeMasking.Name = "lblOpcodeMasking";
            this.lblOpcodeMasking.Size = new System.Drawing.Size(112, 19);
            this.lblOpcodeMasking.TabIndex = 18;
            this.lblOpcodeMasking.Text = "Opcode Masking:";
            this.lblOpcodeMasking.UseStyleColors = true;
            // 
            // chkOpcodeMasking
            // 
            this.chkOpcodeMasking.AutoSize = true;
            this.chkOpcodeMasking.DisplayStatus = false;
            this.chkOpcodeMasking.Location = new System.Drawing.Point(128, 92);
            this.chkOpcodeMasking.Name = "chkOpcodeMasking";
            this.chkOpcodeMasking.Size = new System.Drawing.Size(50, 19);
            this.chkOpcodeMasking.TabIndex = 19;
            this.chkOpcodeMasking.Text = "Off";
            this.chkOpcodeMasking.UseSelectable = true;
            // 
            // lblSaveOpcodeMap
            // 
            this.lblSaveOpcodeMap.AutoSize = true;
            this.lblSaveOpcodeMap.Location = new System.Drawing.Point(0, 64);
            this.lblSaveOpcodeMap.Name = "lblSaveOpcodeMap";
            this.lblSaveOpcodeMap.Size = new System.Drawing.Size(122, 19);
            this.lblSaveOpcodeMap.TabIndex = 16;
            this.lblSaveOpcodeMap.Text = "Save Opcode Map:";
            this.lblSaveOpcodeMap.UseStyleColors = true;
            // 
            // chkSaveOpcodeMap
            // 
            this.chkSaveOpcodeMap.AutoSize = true;
            this.chkSaveOpcodeMap.Checked = true;
            this.chkSaveOpcodeMap.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkSaveOpcodeMap.DisplayStatus = false;
            this.chkSaveOpcodeMap.Location = new System.Drawing.Point(128, 64);
            this.chkSaveOpcodeMap.Name = "chkSaveOpcodeMap";
            this.chkSaveOpcodeMap.Size = new System.Drawing.Size(50, 19);
            this.chkSaveOpcodeMap.TabIndex = 17;
            this.chkSaveOpcodeMap.Text = "On";
            this.chkSaveOpcodeMap.UseSelectable = true;
            // 
            // tabControl
            // 
            this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl.Controls.Add(this.tabCompile);
            this.tabControl.Controls.Add(this.tabInject);
            this.tabControl.Controls.Add(this.tabSettings);
            this.tabControl.Controls.Add(this.tabAbout);
            this.tabControl.HotTrack = true;
            this.tabControl.Location = new System.Drawing.Point(23, 55);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 1;
            this.tabControl.Size = new System.Drawing.Size(615, 189);
            this.tabControl.TabIndex = 22;
            this.tabControl.UseSelectable = true;
            // 
            // tabCompile
            // 
            this.tabCompile.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.tabCompile.Controls.Add(this.btnGscConfMode);
            this.tabCompile.Controls.Add(this.lblProjectFolder);
            this.tabCompile.Controls.Add(this.txtProjectFolder);
            this.tabCompile.Controls.Add(this.btnSelectProjectFolder);
            this.tabCompile.Controls.Add(this.btnRecentProjects);
            this.tabCompile.Controls.Add(this.lblOutputFile);
            this.tabCompile.Controls.Add(this.txtOutputFile);
            this.tabCompile.Controls.Add(this.btnSelectOutputFile);
            this.tabCompile.Controls.Add(this.lblSaveOpcodeMap);
            this.tabCompile.Controls.Add(this.chkSaveOpcodeMap);
            this.tabCompile.Controls.Add(this.lblOpcodeMasking);
            this.tabCompile.Controls.Add(this.chkOpcodeMasking);
            this.tabCompile.Controls.Add(this.btnPlatform);
            this.tabCompile.Controls.Add(this.btnGame);
            this.tabCompile.Controls.Add(this.btnCompile);
            this.tabCompile.Controls.Add(this.btnCodeEditor);
            this.tabCompile.HorizontalScrollbarBarColor = true;
            this.tabCompile.HorizontalScrollbarHighlightOnWheel = false;
            this.tabCompile.HorizontalScrollbarSize = 3;
            this.tabCompile.Location = new System.Drawing.Point(4, 38);
            this.tabCompile.Name = "tabCompile";
            this.tabCompile.Size = new System.Drawing.Size(607, 147);
            this.tabCompile.TabIndex = 0;
            this.tabCompile.Text = "Compile";
            this.tabCompile.VerticalScrollbarBarColor = true;
            this.tabCompile.VerticalScrollbarHighlightOnWheel = false;
            this.tabCompile.VerticalScrollbarSize = 3;
            this.tabCompile.Click += new System.EventHandler(this.tabCompile_Click);
            // 
            // btnGscConfMode
            // 
            this.btnGscConfMode.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnGscConfMode.AutoSize = true;
            this.btnGscConfMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.btnGscConfMode.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.btnGscConfMode.Highlight = true;
            this.btnGscConfMode.Location = new System.Drawing.Point(383, 121);
            this.btnGscConfMode.Name = "btnGscConfMode";
            this.btnGscConfMode.Size = new System.Drawing.Size(118, 23);
            this.btnGscConfMode.TabIndex = 22;
            this.btnGscConfMode.Text = "GSC Config";
            this.btnGscConfMode.UseSelectable = true;
            this.btnGscConfMode.Click += new System.EventHandler(this.btnGscConfMode_Click);
            // 
            // txtProjectFolder
            // 
            this.txtProjectFolder.AllowDrop = true;
            this.txtProjectFolder.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProjectFolder.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            // 
            // 
            // 
            this.txtProjectFolder.CustomButton.Image = null;
            this.txtProjectFolder.CustomButton.Location = new System.Drawing.Point(294, 2);
            this.txtProjectFolder.CustomButton.Name = "";
            this.txtProjectFolder.CustomButton.Size = new System.Drawing.Size(17, 17);
            this.txtProjectFolder.CustomButton.Style = ReaLTaiizor.Enum.Poison.ColorStyle.Blue;
            this.txtProjectFolder.CustomButton.TabIndex = 1;
            this.txtProjectFolder.CustomButton.Theme = ReaLTaiizor.Enum.Poison.ThemeStyle.Light;
            this.txtProjectFolder.CustomButton.UseSelectable = true;
            this.txtProjectFolder.CustomButton.Visible = false;
            this.txtProjectFolder.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.txtProjectFolder.Lines = new string[0];
            this.txtProjectFolder.Location = new System.Drawing.Point(98, 8);
            this.txtProjectFolder.MaxLength = 32767;
            this.txtProjectFolder.Name = "txtProjectFolder";
            this.txtProjectFolder.PasswordChar = '\0';
            this.txtProjectFolder.ReadOnly = true;
            this.txtProjectFolder.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.txtProjectFolder.SelectedText = "";
            this.txtProjectFolder.SelectionLength = 0;
            this.txtProjectFolder.SelectionStart = 0;
            this.txtProjectFolder.ShortcutsEnabled = true;
            this.txtProjectFolder.Size = new System.Drawing.Size(318, 22);
            this.txtProjectFolder.TabIndex = 6;
            this.txtProjectFolder.UseSelectable = true;
            this.txtProjectFolder.WaterMarkColor = System.Drawing.Color.FromArgb(((int)(((byte)(109)))), ((int)(((byte)(109)))), ((int)(((byte)(109)))));
            this.txtProjectFolder.WaterMarkFont = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Pixel);
            this.txtProjectFolder.TextChanged += new System.EventHandler(this.txtProjectFolder_TextChanged);
            this.txtProjectFolder.DragDrop += new System.Windows.Forms.DragEventHandler(this.MainForm_DragDrop);
            this.txtProjectFolder.DragEnter += new System.Windows.Forms.DragEventHandler(this.MainForm_DragEnter);
            // 
            // btnSelectProjectFolder
            // 
            this.btnSelectProjectFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSelectProjectFolder.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.btnSelectProjectFolder.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.btnSelectProjectFolder.Highlight = true;
            this.btnSelectProjectFolder.Location = new System.Drawing.Point(506, 8);
            this.btnSelectProjectFolder.Name = "btnSelectProjectFolder";
            this.btnSelectProjectFolder.Size = new System.Drawing.Size(88, 22);
            this.btnSelectProjectFolder.TabIndex = 7;
            this.btnSelectProjectFolder.Text = "Browse...";
            this.btnSelectProjectFolder.UseSelectable = true;
            this.btnSelectProjectFolder.Click += new System.EventHandler(this.btnSelectProjectFolder_Click);
            // 
            // btnRecentProjects
            // 
            this.btnRecentProjects.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRecentProjects.AutoSize = true;
            this.btnRecentProjects.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.btnRecentProjects.ContextMenuStrip = this.recentProjectsMenu;
            this.btnRecentProjects.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.btnRecentProjects.Location = new System.Drawing.Point(422, 8);
            this.btnRecentProjects.Name = "btnRecentProjects";
            this.btnRecentProjects.Size = new System.Drawing.Size(79, 23);
            this.btnRecentProjects.SplitMenuStrip = this.recentProjectsMenu;
            this.btnRecentProjects.TabIndex = 8;
            this.btnRecentProjects.Text = "Recent";
            this.btnRecentProjects.UseSelectable = true;
            // 
            // txtOutputFile
            // 
            this.txtOutputFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOutputFile.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            // 
            // 
            // 
            this.txtOutputFile.CustomButton.Image = null;
            this.txtOutputFile.CustomButton.Location = new System.Drawing.Point(379, 2);
            this.txtOutputFile.CustomButton.Name = "";
            this.txtOutputFile.CustomButton.Size = new System.Drawing.Size(17, 17);
            this.txtOutputFile.CustomButton.Style = ReaLTaiizor.Enum.Poison.ColorStyle.Blue;
            this.txtOutputFile.CustomButton.TabIndex = 1;
            this.txtOutputFile.CustomButton.Theme = ReaLTaiizor.Enum.Poison.ThemeStyle.Light;
            this.txtOutputFile.CustomButton.UseSelectable = true;
            this.txtOutputFile.CustomButton.Visible = false;
            this.txtOutputFile.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.txtOutputFile.Lines = new string[0];
            this.txtOutputFile.Location = new System.Drawing.Point(79, 36);
            this.txtOutputFile.MaxLength = 32767;
            this.txtOutputFile.Name = "txtOutputFile";
            this.txtOutputFile.PasswordChar = '\0';
            this.txtOutputFile.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.txtOutputFile.SelectedText = "";
            this.txtOutputFile.SelectionLength = 0;
            this.txtOutputFile.SelectionStart = 0;
            this.txtOutputFile.ShortcutsEnabled = true;
            this.txtOutputFile.Size = new System.Drawing.Size(422, 22);
            this.txtOutputFile.TabIndex = 9;
            this.txtOutputFile.UseSelectable = true;
            this.txtOutputFile.WaterMarkColor = System.Drawing.Color.FromArgb(((int)(((byte)(109)))), ((int)(((byte)(109)))), ((int)(((byte)(109)))));
            this.txtOutputFile.WaterMarkFont = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Pixel);
            this.txtOutputFile.TextChanged += new System.EventHandler(this.txtOutputFile_TextChanged);
            this.txtOutputFile.DoubleClick += new System.EventHandler(this.txtOutputFile_DoubleClick);
            // 
            // btnSelectOutputFile
            // 
            this.btnSelectOutputFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSelectOutputFile.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.btnSelectOutputFile.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.btnSelectOutputFile.Highlight = true;
            this.btnSelectOutputFile.Location = new System.Drawing.Point(507, 36);
            this.btnSelectOutputFile.Name = "btnSelectOutputFile";
            this.btnSelectOutputFile.Size = new System.Drawing.Size(88, 22);
            this.btnSelectOutputFile.TabIndex = 10;
            this.btnSelectOutputFile.Text = "Browse...";
            this.btnSelectOutputFile.UseSelectable = true;
            this.btnSelectOutputFile.Click += new System.EventHandler(this.btnSelectOutputFile_Click);
            // 
            // btnPlatform
            // 
            this.btnPlatform.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnPlatform.AutoSize = true;
            this.btnPlatform.ContextMenuStrip = this.platformMenu;
            this.btnPlatform.Location = new System.Drawing.Point(452, 64);
            this.btnPlatform.Name = "btnPlatform";
            this.btnPlatform.Size = new System.Drawing.Size(49, 23);
            this.btnPlatform.SplitMenuStrip = this.platformMenu;
            this.btnPlatform.TabIndex = 18;
            this.btnPlatform.Text = "PC";
            this.btnPlatform.UseSelectable = true;
            // 
            // btnGame
            // 
            this.btnGame.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnGame.AutoSize = true;
            this.btnGame.ContextMenuStrip = this.gameMenu;
            this.btnGame.Location = new System.Drawing.Point(422, 92);
            this.btnGame.Name = "btnGame";
            this.btnGame.Size = new System.Drawing.Size(79, 23);
            this.btnGame.SplitMenuStrip = this.gameMenu;
            this.btnGame.TabIndex = 20;
            this.btnGame.Text = "T7 (BO3)";
            this.btnGame.UseSelectable = true;
            // 
            // btnCompile
            // 
            this.btnCompile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCompile.Enabled = false;
            this.btnCompile.Highlight = true;
            this.btnCompile.Location = new System.Drawing.Point(507, 64);
            this.btnCompile.Name = "btnCompile";
            this.btnCompile.Size = new System.Drawing.Size(88, 80);
            this.btnCompile.TabIndex = 12;
            this.btnCompile.Text = "Compile";
            this.btnCompile.UseSelectable = true;
            this.btnCompile.UseStyleColors = true;
            this.btnCompile.Click += new System.EventHandler(this.btnCompile_Click);
            // 
            // btnCodeEditor
            // 
            this.btnCodeEditor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCodeEditor.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.btnCodeEditor.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.btnCodeEditor.Highlight = true;
            this.btnCodeEditor.Location = new System.Drawing.Point(344, 64);
            this.btnCodeEditor.Name = "btnCodeEditor";
            this.btnCodeEditor.Size = new System.Drawing.Size(102, 23);
            this.btnCodeEditor.TabIndex = 19;
            this.btnCodeEditor.Text = "Open Code Editor";
            this.btnCodeEditor.UseSelectable = true;
            this.btnCodeEditor.Click += new System.EventHandler(this.btnCodeEditor_Click);
            // 
            // tabInject
            // 
            this.tabInject.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.tabInject.Controls.Add(this.lblInjectFile);
            this.tabInject.Controls.Add(this.lblInjectPath);
            this.tabInject.Controls.Add(this.lblInjectGame);
            this.tabInject.Controls.Add(this.lblNoRuntime);
            this.tabInject.Controls.Add(this.chkNoRuntime);
            this.tabInject.Controls.Add(this.lblHotReload);
            this.tabInject.Controls.Add(this.btnHotReload);
            this.tabInject.Controls.Add(this.txtInjectFile);
            this.tabInject.Controls.Add(this.btnSelectInjectFile);
            this.tabInject.Controls.Add(this.txtInjectPath);
            this.tabInject.Controls.Add(this.btnInjectGame);
            this.tabInject.Controls.Add(this.btnInject);
            this.tabInject.Controls.Add(this.btnResetParseTree);
            this.tabInject.Controls.Add(this.btnLaunchBO3);
            this.tabInject.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            this.tabInject.HorizontalScrollbarBarColor = true;
            this.tabInject.HorizontalScrollbarHighlightOnWheel = false;
            this.tabInject.HorizontalScrollbarSize = 3;
            this.tabInject.Location = new System.Drawing.Point(4, 38);
            this.tabInject.Name = "tabInject";
            this.tabInject.Size = new System.Drawing.Size(607, 147);
            this.tabInject.TabIndex = 1;
            this.tabInject.Text = "Inject";
            this.tabInject.VerticalScrollbarBarColor = true;
            this.tabInject.VerticalScrollbarHighlightOnWheel = false;
            this.tabInject.VerticalScrollbarSize = 3;
            // 
            // lblInjectFile
            // 
            this.lblInjectFile.AutoSize = true;
            this.lblInjectFile.Location = new System.Drawing.Point(-4, 11);
            this.lblInjectFile.Name = "lblInjectFile";
            this.lblInjectFile.Size = new System.Drawing.Size(94, 19);
            this.lblInjectFile.TabIndex = 0;
            this.lblInjectFile.Text = "Compiled File:";
            this.lblInjectFile.UseStyleColors = true;
            // 
            // lblInjectPath
            // 
            this.lblInjectPath.AutoSize = true;
            this.lblInjectPath.Location = new System.Drawing.Point(3, 39);
            this.lblInjectPath.Name = "lblInjectPath";
            this.lblInjectPath.Size = new System.Drawing.Size(87, 19);
            this.lblInjectPath.TabIndex = 3;
            this.lblInjectPath.Text = "Replace Path:";
            this.lblInjectPath.UseStyleColors = true;
            // 
            // lblInjectGame
            // 
            this.lblInjectGame.AutoSize = true;
            this.lblInjectGame.Location = new System.Drawing.Point(43, 67);
            this.lblInjectGame.Name = "lblInjectGame";
            this.lblInjectGame.Size = new System.Drawing.Size(47, 19);
            this.lblInjectGame.TabIndex = 5;
            this.lblInjectGame.Text = "Game:";
            this.lblInjectGame.UseStyleColors = true;
            // 
            // lblNoRuntime
            // 
            this.lblNoRuntime.AutoSize = true;
            this.lblNoRuntime.Location = new System.Drawing.Point(8, 92);
            this.lblNoRuntime.Name = "lblNoRuntime";
            this.lblNoRuntime.Size = new System.Drawing.Size(82, 19);
            this.lblNoRuntime.TabIndex = 7;
            this.lblNoRuntime.Text = "No Runtime:";
            this.lblNoRuntime.UseStyleColors = true;
            // 
            // chkNoRuntime
            // 
            this.chkNoRuntime.AutoSize = true;
            this.chkNoRuntime.DisplayStatus = false;
            this.chkNoRuntime.Location = new System.Drawing.Point(96, 92);
            this.chkNoRuntime.Name = "chkNoRuntime";
            this.chkNoRuntime.Size = new System.Drawing.Size(50, 19);
            this.chkNoRuntime.TabIndex = 8;
            this.chkNoRuntime.Text = "Off";
            this.chkNoRuntime.UseSelectable = true;
            // 
            // lblHotReload
            // 
            this.lblHotReload.AutoSize = true;
            this.lblHotReload.Location = new System.Drawing.Point(160, 92);
            this.lblHotReload.Name = "lblHotReload";
            this.lblHotReload.Size = new System.Drawing.Size(78, 19);
            this.lblHotReload.TabIndex = 9;
            this.lblHotReload.Text = "Hot Reload:";
            this.lblHotReload.UseStyleColors = true;
            // 
            // btnHotReload
            // 
            this.btnHotReload.AutoSize = true;
            this.btnHotReload.ContextMenuStrip = this.hotReloadMenu;
            this.btnHotReload.Location = new System.Drawing.Point(244, 92);
            this.btnHotReload.Name = "btnHotReload";
            this.btnHotReload.Size = new System.Drawing.Size(64, 23);
            this.btnHotReload.SplitMenuStrip = this.hotReloadMenu;
            this.btnHotReload.TabIndex = 10;
            this.btnHotReload.Text = "None";
            this.btnHotReload.UseSelectable = true;
            // 
            // hotReloadMenu
            // 
            this.hotReloadMenu.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.hotReloadMenu.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.hotReloadMenu.Name = "hotReloadMenu";
            this.hotReloadMenu.Size = new System.Drawing.Size(61, 4);
            // 
            // txtInjectFile
            // 
            this.txtInjectFile.AllowDrop = true;
            this.txtInjectFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtInjectFile.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            // 
            // 
            // 
            this.txtInjectFile.CustomButton.Image = null;
            this.txtInjectFile.CustomButton.Location = new System.Drawing.Point(385, 2);
            this.txtInjectFile.CustomButton.Name = "";
            this.txtInjectFile.CustomButton.Size = new System.Drawing.Size(17, 17);
            this.txtInjectFile.CustomButton.Style = ReaLTaiizor.Enum.Poison.ColorStyle.Blue;
            this.txtInjectFile.CustomButton.TabIndex = 1;
            this.txtInjectFile.CustomButton.Theme = ReaLTaiizor.Enum.Poison.ThemeStyle.Light;
            this.txtInjectFile.CustomButton.UseSelectable = true;
            this.txtInjectFile.CustomButton.Visible = false;
            this.txtInjectFile.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.txtInjectFile.Lines = new string[0];
            this.txtInjectFile.Location = new System.Drawing.Point(96, 8);
            this.txtInjectFile.MaxLength = 32767;
            this.txtInjectFile.Name = "txtInjectFile";
            this.txtInjectFile.PasswordChar = '\0';
            this.txtInjectFile.ReadOnly = true;
            this.txtInjectFile.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.txtInjectFile.SelectedText = "";
            this.txtInjectFile.SelectionLength = 0;
            this.txtInjectFile.SelectionStart = 0;
            this.txtInjectFile.ShortcutsEnabled = true;
            this.txtInjectFile.Size = new System.Drawing.Size(405, 22);
            this.txtInjectFile.TabIndex = 1;
            this.txtInjectFile.UseSelectable = true;
            this.txtInjectFile.WaterMarkColor = System.Drawing.Color.FromArgb(((int)(((byte)(109)))), ((int)(((byte)(109)))), ((int)(((byte)(109)))));
            this.txtInjectFile.WaterMarkFont = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Pixel);
            this.txtInjectFile.TextChanged += new System.EventHandler(this.txtInjectFile_TextChanged);
            this.txtInjectFile.DragDrop += new System.Windows.Forms.DragEventHandler(this.MainForm_DragDrop);
            this.txtInjectFile.DragEnter += new System.Windows.Forms.DragEventHandler(this.MainForm_DragEnter);
            // 
            // btnSelectInjectFile
            // 
            this.btnSelectInjectFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSelectInjectFile.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.btnSelectInjectFile.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.btnSelectInjectFile.Highlight = true;
            this.btnSelectInjectFile.Location = new System.Drawing.Point(506, 8);
            this.btnSelectInjectFile.Name = "btnSelectInjectFile";
            this.btnSelectInjectFile.Size = new System.Drawing.Size(88, 22);
            this.btnSelectInjectFile.TabIndex = 2;
            this.btnSelectInjectFile.Text = "Browse...";
            this.btnSelectInjectFile.UseSelectable = true;
            this.btnSelectInjectFile.Click += new System.EventHandler(this.btnSelectInjectFile_Click);
            // 
            // txtInjectPath
            // 
            this.txtInjectPath.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            // 
            // 
            // 
            this.txtInjectPath.CustomButton.Image = null;
            this.txtInjectPath.CustomButton.Location = new System.Drawing.Point(224, 2);
            this.txtInjectPath.CustomButton.Name = "";
            this.txtInjectPath.CustomButton.Size = new System.Drawing.Size(17, 17);
            this.txtInjectPath.CustomButton.Style = ReaLTaiizor.Enum.Poison.ColorStyle.Blue;
            this.txtInjectPath.CustomButton.TabIndex = 1;
            this.txtInjectPath.CustomButton.Theme = ReaLTaiizor.Enum.Poison.ThemeStyle.Light;
            this.txtInjectPath.CustomButton.UseSelectable = true;
            this.txtInjectPath.CustomButton.Visible = false;
            this.txtInjectPath.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.txtInjectPath.Lines = new string[] {
        "scripts/shared/duplicaterender_mgr.gsc"};
            this.txtInjectPath.Location = new System.Drawing.Point(96, 36);
            this.txtInjectPath.MaxLength = 32767;
            this.txtInjectPath.Name = "txtInjectPath";
            this.txtInjectPath.PasswordChar = '\0';
            this.txtInjectPath.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.txtInjectPath.SelectedText = "";
            this.txtInjectPath.SelectionLength = 0;
            this.txtInjectPath.SelectionStart = 0;
            this.txtInjectPath.ShortcutsEnabled = true;
            this.txtInjectPath.Size = new System.Drawing.Size(244, 22);
            this.txtInjectPath.TabIndex = 4;
            this.txtInjectPath.Text = "scripts/shared/duplicaterender_mgr.gsc";
            this.txtInjectPath.UseSelectable = true;
            this.txtInjectPath.WaterMarkColor = System.Drawing.Color.FromArgb(((int)(((byte)(109)))), ((int)(((byte)(109)))), ((int)(((byte)(109)))));
            this.txtInjectPath.WaterMarkFont = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Pixel);
            this.txtInjectPath.TextChanged += new System.EventHandler(this.txtInjectPath_TextChanged);
            // 
            // btnInjectGame
            // 
            this.btnInjectGame.AutoSize = true;
            this.btnInjectGame.ContextMenuStrip = this.injectGameMenu;
            this.btnInjectGame.Location = new System.Drawing.Point(96, 64);
            this.btnInjectGame.Name = "btnInjectGame";
            this.btnInjectGame.Size = new System.Drawing.Size(82, 23);
            this.btnInjectGame.SplitMenuStrip = this.injectGameMenu;
            this.btnInjectGame.TabIndex = 6;
            this.btnInjectGame.Text = "T7 (BO3)";
            this.btnInjectGame.UseSelectable = true;
            // 
            // btnInject
            // 
            this.btnInject.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnInject.Highlight = true;
            this.btnInject.Location = new System.Drawing.Point(507, 36);
            this.btnInject.Name = "btnInject";
            this.btnInject.Size = new System.Drawing.Size(88, 22);
            this.btnInject.TabIndex = 8;
            this.btnInject.Text = "Inject";
            this.btnInject.UseSelectable = true;
            this.btnInject.UseStyleColors = true;
            this.btnInject.Click += new System.EventHandler(this.btnInject_Click);
            // 
            // btnResetParseTree
            // 
            this.btnResetParseTree.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnResetParseTree.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.btnResetParseTree.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.btnResetParseTree.Highlight = true;
            this.btnResetParseTree.Location = new System.Drawing.Point(507, 64);
            this.btnResetParseTree.Name = "btnResetParseTree";
            this.btnResetParseTree.Size = new System.Drawing.Size(88, 22);
            this.btnResetParseTree.TabIndex = 10;
            this.btnResetParseTree.Text = "Reset Tree";
            this.btnResetParseTree.UseSelectable = true;
            this.btnResetParseTree.Click += new System.EventHandler(this.btnResetParseTree_Click);
            // 
            // btnLaunchBO3
            // 
            this.btnLaunchBO3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLaunchBO3.Highlight = true;
            this.btnLaunchBO3.Location = new System.Drawing.Point(507, 92);
            this.btnLaunchBO3.Name = "btnLaunchBO3";
            this.btnLaunchBO3.Size = new System.Drawing.Size(88, 24);
            this.btnLaunchBO3.TabIndex = 11;
            this.btnLaunchBO3.Text = "Launch BO3";
            this.btnLaunchBO3.UseSelectable = true;
            this.btnLaunchBO3.UseStyleColors = true;
            this.btnLaunchBO3.Click += new System.EventHandler(this.btnLaunchBO3_Click);
            // 
            // tabSettings
            // 
            this.tabSettings.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.tabSettings.Controls.Add(this.lblSettingsTheme);
            this.tabSettings.Controls.Add(this.toggleSettingsTheme);
            this.tabSettings.Controls.Add(this.lblSettingsColorStyle);
            this.tabSettings.Controls.Add(this.btnSettingsColorStyle);
            this.tabSettings.Controls.Add(this.lblSettingsDefaultOutputPath);
            this.tabSettings.Controls.Add(this.txtSettingsDefaultOutputPath);
            this.tabSettings.Controls.Add(this.btnSettingsBrowseOutputPath);
            this.tabSettings.HorizontalScrollbarBarColor = true;
            this.tabSettings.HorizontalScrollbarHighlightOnWheel = false;
            this.tabSettings.HorizontalScrollbarSize = 3;
            this.tabSettings.Location = new System.Drawing.Point(4, 38);
            this.tabSettings.Name = "tabSettings";
            this.tabSettings.Size = new System.Drawing.Size(607, 147);
            this.tabSettings.TabIndex = 2;
            this.tabSettings.Text = "Settings";
            this.tabSettings.VerticalScrollbarBarColor = true;
            this.tabSettings.VerticalScrollbarHighlightOnWheel = false;
            this.tabSettings.VerticalScrollbarSize = 3;
            // 
            // lblSettingsTheme
            // 
            this.lblSettingsTheme.AutoSize = true;
            this.lblSettingsTheme.Location = new System.Drawing.Point(25, 11);
            this.lblSettingsTheme.Name = "lblSettingsTheme";
            this.lblSettingsTheme.Size = new System.Drawing.Size(52, 19);
            this.lblSettingsTheme.TabIndex = 1;
            this.lblSettingsTheme.Text = "Theme:";
            this.lblSettingsTheme.UseStyleColors = true;
            // 
            // toggleSettingsTheme
            // 
            this.toggleSettingsTheme.AutoSize = true;
            this.toggleSettingsTheme.Checked = true;
            this.toggleSettingsTheme.CheckState = System.Windows.Forms.CheckState.Checked;
            this.toggleSettingsTheme.Location = new System.Drawing.Point(92, 11);
            this.toggleSettingsTheme.Name = "toggleSettingsTheme";
            this.toggleSettingsTheme.Size = new System.Drawing.Size(80, 19);
            this.toggleSettingsTheme.TabIndex = 2;
            this.toggleSettingsTheme.Text = "Dark";
            this.toggleSettingsTheme.UseSelectable = true;
            this.toggleSettingsTheme.CheckedChanged += new System.EventHandler(this.toggleSettingsTheme_CheckedChanged);
            // 
            // lblSettingsColorStyle
            // 
            this.lblSettingsColorStyle.AutoSize = true;
            this.lblSettingsColorStyle.Location = new System.Drawing.Point(10, 44);
            this.lblSettingsColorStyle.Name = "lblSettingsColorStyle";
            this.lblSettingsColorStyle.Size = new System.Drawing.Size(76, 19);
            this.lblSettingsColorStyle.TabIndex = 4;
            this.lblSettingsColorStyle.Text = "Color Style:";
            this.lblSettingsColorStyle.UseStyleColors = true;
            // 
            // btnSettingsColorStyle
            // 
            this.btnSettingsColorStyle.AutoSize = true;
            this.btnSettingsColorStyle.ContextMenuStrip = this.settingsColorStyleMenu;
            this.btnSettingsColorStyle.Location = new System.Drawing.Point(92, 44);
            this.btnSettingsColorStyle.Name = "btnSettingsColorStyle";
            this.btnSettingsColorStyle.Size = new System.Drawing.Size(83, 23);
            this.btnSettingsColorStyle.SplitMenuStrip = this.settingsColorStyleMenu;
            this.btnSettingsColorStyle.TabIndex = 5;
            this.btnSettingsColorStyle.Text = "Red";
            this.btnSettingsColorStyle.UseSelectable = true;
            this.btnSettingsColorStyle.UseStyleColors = true;
            this.btnSettingsColorStyle.Click += new System.EventHandler(this.btnSettingsColorStyle_Click);
            // 
            // lblSettingsDefaultOutputPath
            // 
            this.lblSettingsDefaultOutputPath.AutoSize = true;
            this.lblSettingsDefaultOutputPath.Location = new System.Drawing.Point(10, 78);
            this.lblSettingsDefaultOutputPath.Name = "lblSettingsDefaultOutputPath";
            this.lblSettingsDefaultOutputPath.Size = new System.Drawing.Size(127, 19);
            this.lblSettingsDefaultOutputPath.TabIndex = 6;
            this.lblSettingsDefaultOutputPath.Text = "Default Output Path:";
            this.lblSettingsDefaultOutputPath.UseStyleColors = true;
            // 
            // txtSettingsDefaultOutputPath
            // 
            this.txtSettingsDefaultOutputPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.txtSettingsDefaultOutputPath.CustomButton.Image = null;
            this.txtSettingsDefaultOutputPath.CustomButton.Location = new System.Drawing.Point(365, 1);
            this.txtSettingsDefaultOutputPath.CustomButton.Name = "";
            this.txtSettingsDefaultOutputPath.CustomButton.Size = new System.Drawing.Size(21, 21);
            this.txtSettingsDefaultOutputPath.CustomButton.Style = ReaLTaiizor.Enum.Poison.ColorStyle.Blue;
            this.txtSettingsDefaultOutputPath.CustomButton.TabIndex = 1;
            this.txtSettingsDefaultOutputPath.CustomButton.Theme = ReaLTaiizor.Enum.Poison.ThemeStyle.Light;
            this.txtSettingsDefaultOutputPath.CustomButton.UseSelectable = true;
            this.txtSettingsDefaultOutputPath.CustomButton.Visible = false;
            this.txtSettingsDefaultOutputPath.Lines = new string[0];
            this.txtSettingsDefaultOutputPath.Location = new System.Drawing.Point(143, 78);
            this.txtSettingsDefaultOutputPath.MaxLength = 32767;
            this.txtSettingsDefaultOutputPath.Name = "txtSettingsDefaultOutputPath";
            this.txtSettingsDefaultOutputPath.PasswordChar = '\0';
            this.txtSettingsDefaultOutputPath.WaterMark = "Select output folder...";
            this.txtSettingsDefaultOutputPath.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.txtSettingsDefaultOutputPath.SelectedText = "";
            this.txtSettingsDefaultOutputPath.SelectionLength = 0;
            this.txtSettingsDefaultOutputPath.SelectionStart = 0;
            this.txtSettingsDefaultOutputPath.ShortcutsEnabled = true;
            this.txtSettingsDefaultOutputPath.Size = new System.Drawing.Size(387, 23);
            this.txtSettingsDefaultOutputPath.TabIndex = 7;
            this.txtSettingsDefaultOutputPath.UseSelectable = true;
            this.txtSettingsDefaultOutputPath.WaterMark = "Select output folder...";
            this.txtSettingsDefaultOutputPath.WaterMarkColor = System.Drawing.Color.FromArgb(((int)(((byte)(109)))), ((int)(((byte)(109)))), ((int)(((byte)(109)))));
            this.txtSettingsDefaultOutputPath.WaterMarkFont = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Pixel);
            this.txtSettingsDefaultOutputPath.TextChanged += new System.EventHandler(this.txtSettingsDefaultOutputPath_TextChanged);
            // 
            // btnSettingsBrowseOutputPath
            // 
            this.btnSettingsBrowseOutputPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSettingsBrowseOutputPath.Location = new System.Drawing.Point(536, 78);
            this.btnSettingsBrowseOutputPath.Name = "btnSettingsBrowseOutputPath";
            this.btnSettingsBrowseOutputPath.Size = new System.Drawing.Size(71, 23);
            this.btnSettingsBrowseOutputPath.TabIndex = 8;
            this.btnSettingsBrowseOutputPath.Text = "Browse...";
            this.btnSettingsBrowseOutputPath.UseSelectable = true;
            this.btnSettingsBrowseOutputPath.UseStyleColors = true;
            this.btnSettingsBrowseOutputPath.Click += new System.EventHandler(this.btnSettingsBrowseOutputPath_Click);
            // 
            // tabAbout
            // 
            this.tabAbout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.tabAbout.Controls.Add(this.lblVersion);
            this.tabAbout.Controls.Add(this.lblAbout);
            this.tabAbout.HorizontalScrollbarBarColor = true;
            this.tabAbout.HorizontalScrollbarHighlightOnWheel = false;
            this.tabAbout.HorizontalScrollbarSize = 3;
            this.tabAbout.Location = new System.Drawing.Point(4, 38);
            this.tabAbout.Name = "tabAbout";
            this.tabAbout.Size = new System.Drawing.Size(607, 147);
            this.tabAbout.TabIndex = 3;
            this.tabAbout.Text = "About";
            this.tabAbout.VerticalScrollbarBarColor = true;
            this.tabAbout.VerticalScrollbarHighlightOnWheel = false;
            this.tabAbout.VerticalScrollbarSize = 3;
            // 
            // lblVersion
            // 
            this.lblVersion.AutoSize = true;
            this.lblVersion.FontSize = ReaLTaiizor.Extension.Poison.PoisonLabelSize.Tall;
            this.lblVersion.FontWeight = ReaLTaiizor.Extension.Poison.PoisonLabelWeight.Bold;
            this.lblVersion.Location = new System.Drawing.Point(11, 9);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(208, 25);
            this.lblVersion.TabIndex = 0;
            this.lblVersion.Text = "T7 GSC Compiler v1.0.0";
            this.lblVersion.UseStyleColors = true;
            // 
            // lblAbout
            // 
            this.lblAbout.Location = new System.Drawing.Point(11, 35);
            this.lblAbout.Name = "lblAbout";
            this.lblAbout.Size = new System.Drawing.Size(580, 112);
            this.lblAbout.TabIndex = 1;
            this.lblAbout.Text = resources.GetString("lblAbout.Text");
            this.lblAbout.UseStyleColors = true;
            // 
            // btnRecentFiles
            // 
            this.btnRecentFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRecentFiles.AutoSize = true;
            this.btnRecentFiles.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.btnRecentFiles.ContextMenuStrip = this.recentFilesMenu;
            this.btnRecentFiles.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.btnRecentFiles.Location = new System.Drawing.Point(420, 8);
            this.btnRecentFiles.Name = "btnRecentFiles";
            this.btnRecentFiles.Size = new System.Drawing.Size(80, 22);
            this.btnRecentFiles.SplitMenuStrip = this.recentFilesMenu;
            this.btnRecentFiles.TabIndex = 3;
            this.btnRecentFiles.Text = "Recent";
            this.btnRecentFiles.UseSelectable = true;
            // 
            // settingsContextMenu
            // 
            this.settingsContextMenu.Name = "settingsContextMenu";
            this.settingsContextMenu.Size = new System.Drawing.Size(61, 4);
            // 
            // panelLog
            // 
            this.panelLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelLog.AutoScroll = true;
            this.panelLog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.panelLog.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelLog.Controls.Add(this.txtLog);
            this.panelLog.HorizontalScrollbar = true;
            this.panelLog.HorizontalScrollbarBarColor = true;
            this.panelLog.HorizontalScrollbarHighlightOnWheel = false;
            this.panelLog.HorizontalScrollbarSize = 10;
            this.panelLog.Location = new System.Drawing.Point(23, 246);
            this.panelLog.Name = "panelLog";
            this.panelLog.Size = new System.Drawing.Size(615, 222);
            this.panelLog.TabIndex = 14;
            this.panelLog.VerticalScrollbar = true;
            this.panelLog.VerticalScrollbarBarColor = true;
            this.panelLog.VerticalScrollbarHighlightOnWheel = false;
            this.panelLog.VerticalScrollbarSize = 10;
            // 
            // progressSpinner
            // 
            this.progressSpinner.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.progressSpinner.Location = new System.Drawing.Point(592, 29);
            this.progressSpinner.Maximum = 100;
            this.progressSpinner.Name = "progressSpinner";
            this.progressSpinner.Size = new System.Drawing.Size(30, 29);
            this.progressSpinner.Spinning = false;
            this.progressSpinner.TabIndex = 23;
            this.progressSpinner.UseSelectable = true;
            this.progressSpinner.Value = -1;
            this.progressSpinner.Visible = false;
            // 
            // progressBar
            // 
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.Location = new System.Drawing.Point(113, 121);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(388, 5);
            this.progressBar.TabIndex = 22;
            this.progressBar.Visible = false;
            // 
            // poisonToolTip
            // 
            this.poisonToolTip.Style = ReaLTaiizor.Enum.Poison.ColorStyle.Default;
            this.poisonToolTip.StyleManager = null;
            this.poisonToolTip.Theme = ReaLTaiizor.Enum.Poison.ThemeStyle.Default;
            this.poisonToolTip.Popup += new System.Windows.Forms.PopupEventHandler(this.poisonToolTip_Popup);
            // 
            // panelLogControls
            // 
            this.panelLogControls.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelLogControls.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.panelLogControls.Controls.Add(this.btnLogClear);
            this.panelLogControls.Controls.Add(this.btnLogCopy);
            this.panelLogControls.Controls.Add(this.btnLogSave);
            this.panelLogControls.Controls.Add(this.lblLogSearch);
            this.panelLogControls.Controls.Add(this.txtLogSearch);
            this.panelLogControls.HorizontalScrollbarBarColor = true;
            this.panelLogControls.HorizontalScrollbarHighlightOnWheel = false;
            this.panelLogControls.HorizontalScrollbarSize = 10;
            this.panelLogControls.Location = new System.Drawing.Point(23, 468);
            this.panelLogControls.Name = "panelLogControls";
            this.panelLogControls.Size = new System.Drawing.Size(615, 35);
            this.panelLogControls.TabIndex = 24;
            this.panelLogControls.VerticalScrollbarBarColor = true;
            this.panelLogControls.VerticalScrollbarHighlightOnWheel = false;
            this.panelLogControls.VerticalScrollbarSize = 10;
            // 
            // btnLogClear
            // 
            this.btnLogClear.Location = new System.Drawing.Point(5, 5);
            this.btnLogClear.Name = "btnLogClear";
            this.btnLogClear.Size = new System.Drawing.Size(75, 25);
            this.btnLogClear.TabIndex = 0;
            this.btnLogClear.Text = "Clear";
            this.btnLogClear.UseSelectable = true;
            this.btnLogClear.Click += new System.EventHandler(this.btnLogClear_Click);
            // 
            // btnLogCopy
            // 
            this.btnLogCopy.Location = new System.Drawing.Point(86, 5);
            this.btnLogCopy.Name = "btnLogCopy";
            this.btnLogCopy.Size = new System.Drawing.Size(75, 25);
            this.btnLogCopy.TabIndex = 1;
            this.btnLogCopy.Text = "Copy";
            this.btnLogCopy.UseSelectable = true;
            this.btnLogCopy.Click += new System.EventHandler(this.btnLogCopy_Click);
            // 
            // btnLogSave
            // 
            this.btnLogSave.Location = new System.Drawing.Point(167, 5);
            this.btnLogSave.Name = "btnLogSave";
            this.btnLogSave.Size = new System.Drawing.Size(75, 25);
            this.btnLogSave.TabIndex = 2;
            this.btnLogSave.Text = "Save";
            this.btnLogSave.UseSelectable = true;
            this.btnLogSave.Click += new System.EventHandler(this.btnLogSave_Click);
            // 
            // lblLogSearch
            // 
            this.lblLogSearch.AutoSize = true;
            this.lblLogSearch.Location = new System.Drawing.Point(250, 8);
            this.lblLogSearch.Name = "lblLogSearch";
            this.lblLogSearch.Size = new System.Drawing.Size(51, 19);
            this.lblLogSearch.TabIndex = 3;
            this.lblLogSearch.Text = "Search:";
            this.lblLogSearch.UseStyleColors = true;
            // 
            // txtLogSearch
            // 
            this.txtLogSearch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.txtLogSearch.CustomButton.Image = null;
            this.txtLogSearch.CustomButton.Location = new System.Drawing.Point(281, 1);
            this.txtLogSearch.CustomButton.Name = "";
            this.txtLogSearch.CustomButton.Size = new System.Drawing.Size(21, 21);
            this.txtLogSearch.CustomButton.Style = ReaLTaiizor.Enum.Poison.ColorStyle.Blue;
            this.txtLogSearch.CustomButton.TabIndex = 1;
            this.txtLogSearch.CustomButton.Theme = ReaLTaiizor.Enum.Poison.ThemeStyle.Light;
            this.txtLogSearch.CustomButton.UseSelectable = true;
            this.txtLogSearch.CustomButton.Visible = false;
            this.txtLogSearch.Lines = new string[0];
            this.txtLogSearch.Location = new System.Drawing.Point(307, 5);
            this.txtLogSearch.MaxLength = 32767;
            this.txtLogSearch.Name = "txtLogSearch";
            this.txtLogSearch.PasswordChar = '\0';
            this.txtLogSearch.WaterMark = "Search log...";
            this.txtLogSearch.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.txtLogSearch.SelectedText = "";
            this.txtLogSearch.SelectionLength = 0;
            this.txtLogSearch.SelectionStart = 0;
            this.txtLogSearch.ShortcutsEnabled = true;
            this.txtLogSearch.Size = new System.Drawing.Size(303, 23);
            this.txtLogSearch.TabIndex = 4;
            this.txtLogSearch.UseSelectable = true;
            this.txtLogSearch.WaterMark = "Search log...";
            this.txtLogSearch.WaterMarkColor = System.Drawing.Color.FromArgb(((int)(((byte)(109)))), ((int)(((byte)(109)))), ((int)(((byte)(109)))));
            this.txtLogSearch.WaterMarkFont = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Pixel);
            this.txtLogSearch.TextChanged += new System.EventHandler(this.txtLogSearch_TextChanged);
            // 
            // lblGameStatus
            // 
            this.lblGameStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblGameStatus.Location = new System.Drawing.Point(23, 505);
            this.lblGameStatus.Name = "lblGameStatus";
            this.lblGameStatus.Size = new System.Drawing.Size(615, 19);
            this.lblGameStatus.TabIndex = 50;
            this.lblGameStatus.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblGameStatus.Text = "BO3: Not Running | BO4: Not Running";
            this.lblGameStatus.UseCustomFont = true;
            this.lblGameStatus.UseStyleColors = true;
            // 
            // MainForm
            // 
            this.AllowDrop = true;
            this.ApplyImageInvert = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(661, 550);
            this.Controls.Add(this.lblGameStatus);
            this.Controls.Add(this.panelLogControls);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.panelLog);
            this.Controls.Add(this.progressSpinner);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.MinimumSize = new System.Drawing.Size(500, 550);
            // Load icon - try multiple locations
            System.Drawing.Icon formIcon = null;
            
            // Try 1: Embedded resource
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                // Try different possible resource names
                var resourceNames = new[] { "T7CompilerGUI.Resources.t7gui.ico", "t7gui.ico", "Resources.t7gui.ico" };
                foreach (var resourceName in resourceNames)
                {
                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            formIcon = new System.Drawing.Icon(stream);
                            break;
                        }
                    }
                }
            }
            catch { }
            
            // Try 2: Resources folder relative to executable
            if (formIcon == null)
            {
                try
                {
                    string iconPath = System.IO.Path.Combine(Application.StartupPath, "Resources", "t7gui.ico");
                    if (System.IO.File.Exists(iconPath))
                    {
                        formIcon = new System.Drawing.Icon(iconPath);
                    }
                }
                catch { }
            }
            
            // Try 3: Resources folder relative to assembly location
            if (formIcon == null)
            {
                try
                {
                    string iconPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Resources", "t7gui.ico");
                    if (System.IO.File.Exists(iconPath))
                    {
                        formIcon = new System.Drawing.Icon(iconPath);
                    }
                }
                catch { }
            }
            
            if (formIcon != null)
            {
                this.Icon = formIcon;
            }
            this.Name = "MainForm";
            this.PoisonBorderStyle = ReaLTaiizor.Enum.Poison.FormBorderStyle.FixedSingle;
            this.ShadowType = ReaLTaiizor.Enum.Poison.FormShadowType.AeroShadow;
            this.StyleManager = this.poisonStyleManager;
            this.Text = "T7 GSC Compiler";
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.MainForm_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.MainForm_DragEnter);
            this.ResizeBegin += new System.EventHandler(this.MainForm_ResizeBegin);
            this.ResizeEnd += new System.EventHandler(this.MainForm_ResizeEnd);
            ((System.ComponentModel.ISupportInitialize)(this.poisonStyleManager)).EndInit();
            this.tabControl.ResumeLayout(false);
            this.tabCompile.ResumeLayout(false);
            this.tabCompile.PerformLayout();
            this.tabInject.ResumeLayout(false);
            this.tabInject.PerformLayout();
            this.tabSettings.ResumeLayout(false);
            this.tabSettings.PerformLayout();
            this.tabAbout.ResumeLayout(false);
            this.tabAbout.PerformLayout();
            this.panelLog.ResumeLayout(false);
            this.panelLogControls.ResumeLayout(false);
            this.panelLogControls.PerformLayout();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.RichTextBox txtLog;
    }
}
