using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Enum.Poison;
using ReaLTaiizor.Manager;
using T7CompilerGUI.Helpers;
using T7CompilerGUI.Games;
using T7CompilerGUI.Controls;
using T7CompilerGUI.Actions;
using T7CompilerGUI.Forms.Dialogs;
using static T7CompilerGUI.Helpers.ModernFolderDialog;
using TreyarchCompiler;
using TreyarchCompiler.Enums;
using ScintillaNET;
using ScintillaStyle = ScintillaNET.Style; // Alias to avoid conflict with ReaLTaiizor
using DiscordRPC;
using System.Diagnostics;
using System.Threading;

namespace T7CompilerGUI.Forms
{
    public partial class CodeEditorForm : PoisonForm
    {
        #region Fields and Properties
        
        private PoisonStyleManager styleManager;
        private string projectPath = string.Empty;
        private bool folderOpened = false;
        private bool hasChanges = false;
        private bool isLoadingFiles = false; // Flag to prevent hasChanges during file loading
        private string selectedTabItem = string.Empty;
        private string currentFileName = string.Empty;
        private TreyarchCompiler.Enums.Games currentGame = TreyarchCompiler.Enums.Games.T7;
        private string currentGameModeStr = "ZM"; // Default to ZM like original
        private string executingDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        
        // Timers
        private System.Windows.Forms.Timer themeCheckTimer;
        private System.Windows.Forms.Timer tabUpdateTimer;
        
        // File Watcher
        private FileSystemWatcher fileWatcher;
        
        // Editor Management
        private Dictionary<string, Scintilla> openEditors = new Dictionary<string, Scintilla>();
        private Dictionary<string, ReaLTaiizor.Controls.PoisonTabPage> editorTabs = new Dictionary<string, ReaLTaiizor.Controls.PoisonTabPage>();
        private Dictionary<string, string> fileContents = new Dictionary<string, string>();
        
        // Hash Checker
        private Dictionary<string, string> hashToFunctionMap = new Dictionary<string, string>(); // Hash -> Function name
        
        // Keyboard Shortcuts
        private Dictionary<string, Dialogs.KeybindDialog.KeybindInfo> codeEditorKeybinds = null;
        
        // UI Controls and menu items (declared in Designer.cs)
        
        #endregion

        #region Constructor and Initialization
        
        public CodeEditorForm(PoisonStyleManager styleManager)
        {
            this.styleManager = styleManager;
            InitializeComponent();
            
            // Load GSC syntax data from GSC.xshd early (before SetupControls)
            LoadGscSyntaxData();
            
            SetupControls();
            SetupKeyboardShortcuts();
            SetupTimers();
            SetupDiscordRichPresence();
            SetupFileWatcher();
            SetupThemeChangeHandling();
            
            // Initialize code editor keybinds
            codeEditorKeybinds = GetDefaultCodeEditorKeybinds();
            
            // Check if compiler is installed, if not install it
            if (!CompilerActions.IsCompilerInstalled())
            {
                CompilerActions.InstallCompiler(@"https://gsc.dev/t7c_package");
            }
            
            // Create and open default project on startup
            CreateDefaultProjectOnStartup();
        }

        private void SetupThemeChangeHandling()
        {
            if (styleManager == null) return;
            
            // Create a timer to periodically check for theme/style changes
            themeCheckTimer = new System.Windows.Forms.Timer
            {
                Interval = 100 // Check every 100ms
            };
            
            ThemeStyle lastTheme = styleManager.Theme;
            ColorStyle lastStyle = styleManager.Style;
            
            themeCheckTimer.Tick += (s, e) => {
                // Check if form is disposed or disposing
                if (this.IsDisposed || this.Disposing || styleManager == null)
                {
                    if (themeCheckTimer != null)
                    {
                        themeCheckTimer.Stop();
                        themeCheckTimer.Dispose();
                        themeCheckTimer = null;
                    }
                    return;
                }
                
                // Check if theme or style has changed
                if (styleManager.Theme != lastTheme || styleManager.Style != lastStyle)
                {
                    lastTheme = styleManager.Theme;
                    lastStyle = styleManager.Style;
                    
                    // Update theme and style on UI thread
                    if (this.InvokeRequired)
                    {
                        try
                        {
                            // Check if form handle is valid before invoking
                            if (!this.IsDisposed && !this.Disposing && this.IsHandleCreated)
                            {
                                this.BeginInvoke(new Action(() => {
                                    if (!this.IsDisposed && !this.Disposing)
                                    {
                                        UpdateThemeAndStyle();
                                    }
                                }));
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            // Form is disposed, stop timer
                            if (themeCheckTimer != null)
                            {
                                themeCheckTimer.Stop();
                                themeCheckTimer.Dispose();
                                themeCheckTimer = null;
                            }
                        }
                        catch (InvalidOperationException)
                        {
                            // Handle is invalid or form is being disposed
                            if (themeCheckTimer != null)
                            {
                                themeCheckTimer.Stop();
                                themeCheckTimer.Dispose();
                                themeCheckTimer = null;
                            }
                        }
                        catch (ArgumentException)
                        {
                            // Handle is invalid
                            if (themeCheckTimer != null)
                            {
                                themeCheckTimer.Stop();
                                themeCheckTimer.Dispose();
                                themeCheckTimer = null;
                            }
                        }
                    }
                    else
                    {
                        if (!this.IsDisposed && !this.Disposing)
                        {
                            UpdateThemeAndStyle();
                        }
                    }
                }
            };
            
            themeCheckTimer.Start();
            
            // Also update on form activation
            this.Activated += (s, e) => {
            if (styleManager != null)
            {
                    UpdateThemeAndStyle();
                }
            };
        }
        
        /// <summary>
        /// Public method to update theme and style - can be called from outside when theme/style changes
        /// </summary>
        public void UpdateThemeAndStyle()
        {
            // Check if form is disposed or disposing
            if (this.IsDisposed || this.Disposing || styleManager == null) return;
            
            try
            {
                // Update form background
                this.BackColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Form(styleManager.Theme);
            
            // Update menu strip
            if (mainMenuStrip != null)
            {
                mainMenuStrip.BackColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Form(styleManager.Theme);
                mainMenuStrip.ForeColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.ForeColor.Label.Normal(styleManager.Theme);
                mainMenuStrip.Renderer = new PoisonMenuStripRenderer(styleManager);
                mainMenuStrip.Invalidate();
            }
            
            // Update all panels
            ApplyPoisonThemeToControls();
            
            // Update panel scrollbars to match theme
            UpdatePanelScrollbars();
            
            // Update project label
            if (lblProjectPath != null)
            {
                lblProjectPath.StyleManager = styleManager;
                lblProjectPath.UseStyleColors = true;
                lblProjectPath.Invalidate();
            }
            
            // Update compile button
            if (btnCompile != null)
            {
                btnCompile.StyleManager = styleManager;
                btnCompile.UseStyleColors = true;
                btnCompile.Invalidate();
            }
            
            // Update hash checker controls
            if (lblHashChecker != null)
            {
                lblHashChecker.StyleManager = styleManager;
                lblHashChecker.UseStyleColors = true;
                lblHashChecker.Invalidate();
            }
            
            if (txtHashInput != null)
            {
                txtHashInput.StyleManager = styleManager;
                txtHashInput.UseStyleColors = true;
                txtHashInput.Invalidate();
            }
            
            if (btnHashCheck != null)
            {
                btnHashCheck.StyleManager = styleManager;
                btnHashCheck.UseStyleColors = true;
                btnHashCheck.Invalidate();
            }
            
            // Update tab control and all tab pages
            if (tabControl != null)
            {
                tabControl.StyleManager = styleManager;
                tabControl.UseStyleColors = true;
                
                // Update all tab pages
                foreach (ReaLTaiizor.Controls.PoisonTabPage tabPage in tabControl.TabPages)
                {
                    if (tabPage != null)
                    {
                        tabPage.StyleManager = styleManager;
                        tabPage.UseStyleColors = true;
                        tabPage.Invalidate();
                    }
                }
                
                tabControl.Invalidate();
            }
            
            // Update file buttons panel background
            if (fileButtonsPanel != null)
            {
                fileButtonsPanel.BackColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Form(styleManager.Theme);
            }
            
            // Update all open code editors
            UpdateAllEditorsTheme();
            
            // Update all file buttons
            UpdateAllFileButtonsTheme();
            
            // Force StyleManager to update all connected controls
            styleManager.Update();
            
                // Force refresh of entire form
                this.Invalidate(true);
                this.Update();
            }
            catch (ObjectDisposedException)
            {
                // Form is disposed, ignore
            }
            catch (InvalidOperationException)
            {
                // Form is being disposed, ignore
            }
        }
        
        private void UpdateAllEditorsTheme()
        {
            if (styleManager == null) return;
            
            foreach (var editor in openEditors.Values)
            {
                if (editor == null) continue;
                
                // Get theme-aware colors
                Color editorBackColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Form(styleManager.Theme);
                Color editorForeColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.ForeColor.Label.Normal(styleManager.Theme);
                Color caretColor = editorForeColor;
                Color caretLineColor;
                Color selectionColor;
                
                if (styleManager.Theme == ReaLTaiizor.Enum.Poison.ThemeStyle.Dark)
                {
                    caretLineColor = Color.FromArgb(
                        Math.Min(255, editorBackColor.R + 20),
                        Math.Min(255, editorBackColor.G + 20),
                        Math.Min(255, editorBackColor.B + 20));
                    selectionColor = Color.FromArgb(
                        Math.Min(255, editorBackColor.R + 40),
                        Math.Min(255, editorBackColor.G + 40),
                        Math.Min(255, editorBackColor.B + 40));
                }
                else
                {
                    caretLineColor = Color.FromArgb(240, 240, 240);
                    selectionColor = Color.FromArgb(200, 200, 200);
                }
                
                // Update editor colors
                editor.Styles[ScintillaStyle.Default].BackColor = editorBackColor;
                editor.Styles[ScintillaStyle.Default].ForeColor = editorForeColor;
                editor.CaretForeColor = caretColor;
                editor.CaretLineBackColor = caretLineColor;
                editor.SetSelectionBackColor(true, selectionColor);
                
                // Update identifier color
                editor.Styles[ScintillaStyle.Cpp.Identifier].ForeColor = editorForeColor;
                
                // Update line number margin background - CRITICAL: Must set both margin BackColor and style BackColor
                // Scintilla requires both the margin BackColor and the LineNumber style BackColor to match
                // Force margin refresh by temporarily disabling and re-enabling it
                int currentWidth = editor.Margins[0].Width;
                MarginType currentType = editor.Margins[0].Type;
                
                // Temporarily hide margin to force refresh
                editor.Margins[0].Width = 0;
                Application.DoEvents();
                
                // Restore margin with new background color
                editor.Margins[0].Width = currentWidth;
                editor.Margins[0].Type = currentType;
                editor.Margins[0].BackColor = editorBackColor; // Set AFTER restoring width
                
                // Update line number text color
                Color lineNumberColor = styleManager.Theme == ReaLTaiizor.Enum.Poison.ThemeStyle.Dark
                    ? Color.FromArgb(150, 150, 150) // Light grey for dark theme
                    : Color.FromArgb(100, 100, 100); // Dark grey for light theme
                
                // Set line number style background and foreground - MUST match margin background
                // Clear any cached style first
                editor.Styles[ScintillaStyle.LineNumber].BackColor = editorBackColor;
                editor.Styles[ScintillaStyle.LineNumber].ForeColor = lineNumberColor;
                
                // Also update the default style background to ensure consistency
                editor.Styles[ScintillaStyle.Default].BackColor = editorBackColor;
                
                // Refresh syntax highlighting to ensure all colors are updated
                SetupGSCSyntaxHighlighting(editor);
                
                // Ensure margins stay disabled after theme update
                DisableExtraMargins(editor);
                
                // Force full editor refresh including margins - use multiple methods to ensure update
                editor.Invalidate();
                editor.Update();
                editor.Refresh();
                
                // Additional forced refresh after a brief delay to catch any delayed updates
                Application.DoEvents();
            }
        }
        
        private void UpdateAllFileButtonsTheme()
        {
            if (styleManager == null) return;
            
            // Update all file buttons in the file list panel
            foreach (Control control in fileButtonsPanel.Controls)
            {
                if (control is PoisonPanel buttonContainer)
                {
                    // Update container panel
                    buttonContainer.StyleManager = styleManager;
                    buttonContainer.UseStyleColors = true;
                    
                    // Update file button and close button
                    foreach (Control child in buttonContainer.Controls)
                    {
                        if (child is PoisonButton btn)
                        {
                            btn.StyleManager = styleManager;
                            btn.UseStyleColors = true;
                            btn.Invalidate();
                        }
                    }
                    
                    buttonContainer.Invalidate();
                }
            }
        }
        
        private void UpdatePanelScrollbars()
        {
            if (styleManager == null) return;
            
            // Update all PoisonPanel scrollbars to match theme
            UpdatePanelScrollbarTheme(mainPanel);
            UpdatePanelScrollbarTheme(editorPanel);
            UpdatePanelScrollbarTheme(fileListPanel);
            UpdatePanelScrollbarTheme(buttonPanel);
        }
        
        private void UpdatePanelScrollbarTheme(ReaLTaiizor.Controls.PoisonPanel panel)
        {
            if (panel == null) return;
            
            // Ensure panel has StyleManager
            panel.StyleManager = styleManager;
            panel.UseStyleColors = true;
            
            // Force scrollbar refresh by invalidating
            panel.Invalidate();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // Theme is already applied in SetupControls() called from constructor
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            
            // Refresh form to ensure proper rendering
            this.Refresh();
            
            // Update theme on show to catch any changes
            if (styleManager != null)
            {
                UpdateThemeAndStyle();
            }
        }
        
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            
            // Update theme when form is activated to catch changes made in other forms
            // This is handled by SetupThemeChangeHandling() timer, but we also update here for immediate feedback
            if (styleManager != null)
            {
                UpdateThemeAndStyle();
            }
        }

        // InitializeComponent is now in CodeEditorForm.Designer.cs

        private void SetupControls()
        {
            // Set Poison form properties (StyleManager and theme-dependent properties set at runtime)
            if (styleManager != null)
            {
                this.StyleManager = styleManager;
                this.PoisonBorderStyle = ReaLTaiizor.Enum.Poison.FormBorderStyle.FixedSingle;
                
                // Apply theme to form
                this.BackColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Form(styleManager.Theme);
                
                // Apply theme to menu strip with custom renderer
                if (mainMenuStrip != null)
                {
                    mainMenuStrip.BackColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Form(styleManager.Theme);
                    mainMenuStrip.ForeColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.ForeColor.Label.Normal(styleManager.Theme);
                    mainMenuStrip.Renderer = new PoisonMenuStripRenderer(styleManager);
                }
            }
            else
            {
                // Default border style even without style manager
                this.PoisonBorderStyle = ReaLTaiizor.Enum.Poison.FormBorderStyle.FixedSingle;
            }
            
            // Setup main menu (menu items created at runtime due to dynamic behavior)
            SetupMainMenu();

            SetupGameMenu();
            SetupGameModeMenu();
            
            // Apply Poison theme to all controls
            ApplyPoisonThemeToControls();
            
            PoisonControlHelper.SetupAllButtonEffectsRecursive(this);
        }
        
        private void ApplyPoisonThemeToControls()
        {
            if (styleManager == null) return;
            
            // Apply theme to panels
            if (mainPanel != null)
            {
                mainPanel.StyleManager = styleManager;
                mainPanel.UseStyleColors = true;
            }
            
            if (fileListPanel != null)
            {
                fileListPanel.StyleManager = styleManager;
                fileListPanel.UseStyleColors = true;
            }
            
            if (editorPanel != null)
            {
                editorPanel.StyleManager = styleManager;
                editorPanel.UseStyleColors = true;
            }
            
            if (buttonPanel != null)
            {
                buttonPanel.StyleManager = styleManager;
                buttonPanel.UseStyleColors = true;
            }
            
            // Apply theme to buttons
            if (btnCompile != null)
            {
                btnCompile.StyleManager = styleManager;
                btnCompile.UseStyleColors = true;
            }
            
            if (btnHashCheck != null)
            {
                btnHashCheck.StyleManager = styleManager;
                btnHashCheck.UseStyleColors = true;
            }
            
            // Apply theme to labels
            if (lblProjectPath != null)
            {
                lblProjectPath.StyleManager = styleManager;
                lblProjectPath.UseStyleColors = true;
            }
            
            if (lblHashChecker != null)
            {
                lblHashChecker.StyleManager = styleManager;
                lblHashChecker.UseStyleColors = true;
            }
            
            // Apply theme to textboxes
            if (txtHashInput != null)
            {
                txtHashInput.StyleManager = styleManager;
                txtHashInput.UseStyleColors = true;
            }
            
            // Apply theme to tab control
            if (tabControl != null)
            {
                tabControl.StyleManager = styleManager;
                tabControl.UseStyleColors = true;
            }
            
            // Apply theme to file buttons panel
            if (fileButtonsPanel != null)
            {
                // FlowLayoutPanel doesn't have StyleManager, so set background color directly
                fileButtonsPanel.BackColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Form(styleManager.Theme);
            }
        }

        private void SetupTimers()
        {
            // Auto-save timer DISABLED - files should only save when user explicitly saves
            // The original had auto-save, but we want manual save only
            // autoSaveTimer = new System.Windows.Forms.Timer();
            // autoSaveTimer.Interval = 200;
            // autoSaveTimer.Tick += (s, e) => ForceSave();
            // autoSaveTimer.Start();

            // Tab update timer (100ms like original)
            tabUpdateTimer = new System.Windows.Forms.Timer();
            tabUpdateTimer.Interval = 100;
            tabUpdateTimer.Tick += (s, e) => UpdateSelectedTabItem();
            tabUpdateTimer.Start();

            // Discord presence update timer DISABLED - not needed for this project
            // discordUpdateTimer = new System.Windows.Forms.Timer();
            // discordUpdateTimer.Interval = 200;
            // discordUpdateTimer.Tick += (s, e) => UpdateDiscordPresence();
            // discordUpdateTimer.Start();
        }

        private void SetupDiscordRichPresence()
        {
            // Discord Rich Presence disabled - not needed for this project
            // If you want to enable it, uncomment the code below
            /*
            try
            {
                discordClient = new DiscordRpcClient(DISCORD_CLIENT_ID);
                discordClient.Initialize();
            }
            catch
            {
                // Discord not available, continue without it
            }
            */
        }

        private void SetupFileWatcher()
        {
            fileWatcher = new FileSystemWatcher();
            fileWatcher.Changed += FileWatcher_Changed;
            fileWatcher.Created += FileWatcher_Created;
            fileWatcher.Deleted += FileWatcher_Deleted;
            fileWatcher.Renamed += FileWatcher_Renamed;
            fileWatcher.EnableRaisingEvents = false;
        }

        #endregion

        #region Menu Setup

        private void SetupMainMenu()
        {
            // File Menu (matches original structure)
            fileMenu = new ToolStripMenuItem("File");
            
            // New Project
            var newProjectItem = new ToolStripMenuItem("New Project");
            newProjectItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.N;
            newProjectItem.ShowShortcutKeys = false; // Hide shortcut text in menu
            newProjectItem.Click += (s, e) => BtnNewProject_Click(sender: null, e: EventArgs.Empty);
            fileMenu.DropDownItems.Add(newProjectItem);
            
            // New File
            var newFileItem = new ToolStripMenuItem("New File");
            newFileItem.ShortcutKeys = Keys.Control | Keys.N;
            newFileItem.ShowShortcutKeys = false; // Hide shortcut text in menu
            newFileItem.Click += (s, e) => BtnNewFile_Click(sender: null, e: EventArgs.Empty);
            fileMenu.DropDownItems.Add(newFileItem);
            
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            
            // Open Folder
            var openFolderItem = new ToolStripMenuItem("Open Folder");
            openFolderItem.ShortcutKeys = Keys.Control | Keys.O;
            openFolderItem.ShowShortcutKeys = false; // Hide shortcut text in menu
            openFolderItem.Click += (s, e) => BtnOpenFolder_Click(sender: null, e: EventArgs.Empty);
            fileMenu.DropDownItems.Add(openFolderItem);
            
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            
            // Save
            var saveItem = new ToolStripMenuItem("Save");
            saveItem.ShortcutKeys = Keys.Control | Keys.S;
            saveItem.ShowShortcutKeys = false; // Hide shortcut text in menu
            saveItem.Click += (s, e) => BtnSave_Click(sender: null, e: EventArgs.Empty);
            fileMenu.DropDownItems.Add(saveItem);
            
            // Save All
            var saveAllItem = new ToolStripMenuItem("Save All");
            saveAllItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.S;
            saveAllItem.ShowShortcutKeys = false; // Hide shortcut text in menu
            saveAllItem.Click += (s, e) => BtnSaveAll_Click(sender: null, e: EventArgs.Empty);
            fileMenu.DropDownItems.Add(saveAllItem);
            
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            
            // Refresh Files
            var refreshItem = new ToolStripMenuItem("Refresh Files");
            refreshItem.ShortcutKeys = Keys.Control | Keys.R;
            refreshItem.ShowShortcutKeys = false; // Hide shortcut text in menu
            refreshItem.Click += (s, e) => BtnRefresh_Click(sender: null, e: EventArgs.Empty);
            fileMenu.DropDownItems.Add(refreshItem);
            
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            
            // Port IL Project
            var portILItem = new ToolStripMenuItem("Port IL Project");
            portILItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.Alt | Keys.O;
            portILItem.ShowShortcutKeys = false; // Hide shortcut text in menu
            portILItem.Click += (s, e) => BtnPortIL_Click(sender: null, e: EventArgs.Empty);
            fileMenu.DropDownItems.Add(portILItem);
            
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            
            // Keyboard Shortcuts
            var shortcutsItem = new ToolStripMenuItem("Shortcuts...");
            shortcutsItem.Click += ShortcutsMenu_Click;
            fileMenu.DropDownItems.Add(shortcutsItem);
            
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            
            // Force Host
            var forceHostItem = new ToolStripMenuItem("Force Host");
            forceHostItem.ToolTipText = "Force Host - Great for setting peoples stats | Click Me Before Finding a public match";
            forceHostItem.Click += ForceHostMenu_Click;
            fileMenu.DropDownItems.Add(forceHostItem);
            
            // Reset Host Dvars
            var resetHostItem = new ToolStripMenuItem("Reset Host Dvars");
            resetHostItem.ToolTipText = "Reset Dvars";
            resetHostItem.Click += ClearHostDvarsMenu_Click;
            fileMenu.DropDownItems.Add(resetHostItem);
            
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            
            // Check For Updates
            var updateItem = new ToolStripMenuItem("Check For Updates");
            updateItem.ToolTipText = "Check For Updates";
            updateItem.Click += UpdateCompilerMenu_Click;
            fileMenu.DropDownItems.Add(updateItem);
            
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            
            // About
            var aboutItem = new ToolStripMenuItem("About");
            aboutItem.Click += AboutMenu_Click;
            fileMenu.DropDownItems.Add(aboutItem);
            
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            
            // Exit
            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => this.Close();
            fileMenu.DropDownItems.Add(exitItem);
            
            // Game Menu (checkable items)
            gameMenu = new ToolStripMenuItem("Game");
            t7GameItem = new ToolStripMenuItem("Black Ops 3");
            t7GameItem.CheckOnClick = true;
            t7GameItem.Checked = true; // Default to T7
            t7GameItem.Click += (s, e) => {
                currentGame = TreyarchCompiler.Enums.Games.T7;
                t7GameItem.Checked = true;
                t8GameItem.Checked = false;
            };
            t8GameItem = new ToolStripMenuItem("Black Ops 4");
            t8GameItem.CheckOnClick = true;
            t8GameItem.Click += (s, e) => {
                currentGame = TreyarchCompiler.Enums.Games.T8;
                t8GameItem.Checked = true;
                t7GameItem.Checked = false;
            };
            gameMenu.DropDownItems.Add(t7GameItem);
            gameMenu.DropDownItems.Add(t8GameItem);
            
            // Mode Menu (checkable items)
            modeMenu = new ToolStripMenuItem("Mode");
            campaignModeItem = new ToolStripMenuItem("Campaign");
            campaignModeItem.CheckOnClick = true;
            campaignModeItem.Click += (s, e) => {
                currentGameModeStr = "SP";
                campaignModeItem.Checked = true;
                multiplayerModeItem.Checked = false;
                zombiesModeItem.Checked = false;
                UpdateConditionalCompilationIndicators();
            };
            multiplayerModeItem = new ToolStripMenuItem("Multiplayer");
            multiplayerModeItem.CheckOnClick = true;
            multiplayerModeItem.Click += (s, e) => {
                currentGameModeStr = "MP";
                multiplayerModeItem.Checked = true;
                campaignModeItem.Checked = false;
                zombiesModeItem.Checked = false;
                UpdateConditionalCompilationIndicators();
            };
            zombiesModeItem = new ToolStripMenuItem("Zombies");
            zombiesModeItem.CheckOnClick = true;
            zombiesModeItem.Checked = true; // Default to Zombies
            zombiesModeItem.Click += (s, e) => {
                currentGameModeStr = "ZM";
                zombiesModeItem.Checked = true;
                campaignModeItem.Checked = false;
                multiplayerModeItem.Checked = false;
                UpdateConditionalCompilationIndicators();
            };
            modeMenu.DropDownItems.Add(campaignModeItem);
            modeMenu.DropDownItems.Add(multiplayerModeItem);
            modeMenu.DropDownItems.Add(zombiesModeItem);
            
            // Inject Precompiled Script Menu (standalone)
            injectPrecompiledMenu = new ToolStripMenuItem("Inject Precompiled Script");
            var injectBO3 = new ToolStripMenuItem("Black Ops 3");
            injectBO3.Click += (s, e) => InjectPrecompiledScript(TreyarchCompiler.Enums.Games.T7);
            var injectBO4 = new ToolStripMenuItem("Black Ops 4");
            injectBO4.Click += (s, e) => InjectPrecompiledScript(TreyarchCompiler.Enums.Games.T8);
            injectPrecompiledMenu.DropDownItems.Add(injectBO3);
            injectPrecompiledMenu.DropDownItems.Add(injectBO4);
            
            // Processes Menu (standalone)
            processesMenu = new ToolStripMenuItem("Processes");
            var killBO3 = new ToolStripMenuItem("Kill Black Ops 3");
            killBO3.ToolTipText = "Kills Game Process";
            killBO3.Click += (s, e) => KillGame("blackops3");
            var killBO4 = new ToolStripMenuItem("Kill Black Ops 4");
            killBO4.ToolTipText = "Kills Game Process";
            killBO4.Click += (s, e) => KillGame("blackops4");
            processesMenu.DropDownItems.Add(killBO3);
            processesMenu.DropDownItems.Add(killBO4);
            
            // Add all menus to menu strip
            mainMenuStrip.Items.Add(fileMenu);
            mainMenuStrip.Items.Add(gameMenu);
            mainMenuStrip.Items.Add(modeMenu);
            mainMenuStrip.Items.Add(injectPrecompiledMenu);
            mainMenuStrip.Items.Add(processesMenu);
        }
        
        private void AboutMenu_Click(object sender, EventArgs e)
        {
            ReaLTaiizor.Controls.PoisonMessageBox.Show(
                this,
                "T7 Compiler GUI\n\nA powerful GSC compiler for Call of Duty: Black Ops 3/4",
                "About",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void SetupGameMenu()
        {
            // Context menu no longer needed since we have Game menu in menu bar
            // This method is kept for potential future use but currently does nothing
        }

        private void SetupGameModeMenu()
        {
            // Context menu no longer needed since we have Mode menu in menu bar
            // This method is kept for potential future use but currently does nothing
        }

        private void ResetButtonState(object sender)
        {
            PoisonControlHelper.ResetButtonState(sender);
        }

        #endregion

        #region Keyboard Shortcuts

        private void SetupKeyboardShortcuts()
        {
            // Remove existing handler if any
            this.KeyDown -= CodeEditorForm_KeyDown;
            this.KeyDown += CodeEditorForm_KeyDown;
        }
        
        private void CodeEditorForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Initialize keybinds if not already loaded
            if (codeEditorKeybinds == null)
            {
                codeEditorKeybinds = GetDefaultCodeEditorKeybinds();
            }
            
            // Check each keybind
            foreach (var kvp in codeEditorKeybinds)
            {
                var keybind = kvp.Value;
                bool ctrlMatch = keybind.Ctrl == e.Control;
                bool shiftMatch = keybind.Shift == e.Shift;
                bool altMatch = keybind.Alt == e.Alt;
                bool keyMatch = keybind.Key == e.KeyCode;
                
                if (ctrlMatch && shiftMatch && altMatch && keyMatch)
                {
                    e.Handled = true;
                    
                    // Execute the corresponding action
                    switch (kvp.Key)
                    {
                        case "New Project":
                            BtnNewProject_Click(null, EventArgs.Empty);
                            break;
                        case "New File":
                            BtnNewFile_Click(null, EventArgs.Empty);
                            break;
                        case "Open Folder":
                            BtnOpenFolder_Click(null, EventArgs.Empty);
                            break;
                        case "Save":
                            SaveCurrentFile();
                            break;
                        case "Save All":
                            SaveAllFiles();
                            break;
                        case "Refresh Files":
                            RefreshFileList(false);
                            break;
                        case "Port IL Project":
                            BtnPortIL_Click(null, EventArgs.Empty);
                            break;
                        case "Compile":
                            BtnCompile_Click(null, EventArgs.Empty);
                            break;
                        case "Find":
                            BtnSearch_Click(null, EventArgs.Empty);
                            break;
                        case "Find Next":
                            FindNext();
                            break;
                        case "Replace":
                            BtnReplace_Click(null, EventArgs.Empty);
                            break;
                    }
                    return;
                }
            }
            
            // Undo/Redo - let Scintilla handle these natively
            // Ctrl+Z for undo, Ctrl+Y or Ctrl+Shift+Z for redo
            if (e.Control && e.KeyCode == Keys.Z && !e.Shift)
            {
                // Let Scintilla handle undo natively - don't mark as handled
                // Scintilla will process this automatically
            }
            else if ((e.Control && e.KeyCode == Keys.Y) || (e.Control && e.Shift && e.KeyCode == Keys.Z))
            {
                // Let Scintilla handle redo natively - don't mark as handled
                // Scintilla will process this automatically
            }
        }
        
        private void ShortcutsMenu_Click(object sender, EventArgs e)
        {
            // Get default keybinds for code editor
            if (codeEditorKeybinds == null)
            {
                codeEditorKeybinds = GetDefaultCodeEditorKeybinds();
            }
            
            // Open keybind dialog
            using (var dialog = new Dialogs.KeybindDialog(styleManager, codeEditorKeybinds))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    codeEditorKeybinds = dialog.Keybinds;
                    // Apply the new keybinds (they're already stored, just need to update menu items)
                    ApplyKeybindsToMenu();
                }
            }
        }
        
        private Dictionary<string, Dialogs.KeybindDialog.KeybindInfo> GetDefaultCodeEditorKeybinds()
        {
            return new Dictionary<string, Dialogs.KeybindDialog.KeybindInfo>
            {
                { "New Project", new Dialogs.KeybindDialog.KeybindInfo("New Project", Keys.N, ctrl: true, shift: true) },
                { "New File", new Dialogs.KeybindDialog.KeybindInfo("New File", Keys.N, ctrl: true) },
                { "Open Folder", new Dialogs.KeybindDialog.KeybindInfo("Open Folder", Keys.O, ctrl: true) },
                { "Save", new Dialogs.KeybindDialog.KeybindInfo("Save", Keys.S, ctrl: true) },
                { "Save All", new Dialogs.KeybindDialog.KeybindInfo("Save All", Keys.S, ctrl: true, shift: true) },
                { "Refresh Files", new Dialogs.KeybindDialog.KeybindInfo("Refresh Files", Keys.R, ctrl: true) },
                { "Port IL Project", new Dialogs.KeybindDialog.KeybindInfo("Port IL Project", Keys.O, ctrl: true, shift: true, alt: true) },
                { "Compile", new Dialogs.KeybindDialog.KeybindInfo("Compile", Keys.F9) },
                { "Find", new Dialogs.KeybindDialog.KeybindInfo("Find", Keys.F, ctrl: true) },
                { "Find Next", new Dialogs.KeybindDialog.KeybindInfo("Find Next", Keys.F3) },
                { "Replace", new Dialogs.KeybindDialog.KeybindInfo("Replace", Keys.H, ctrl: true) }
            };
        }
        
        private void ApplyKeybindsToMenu()
        {
            if (codeEditorKeybinds == null || fileMenu == null)
                return;
            
            // Update menu items with new keybinds
            foreach (ToolStripItem item in fileMenu.DropDownItems)
            {
                if (item is ToolStripMenuItem menuItem && codeEditorKeybinds.ContainsKey(menuItem.Text))
                {
                    var keybind = codeEditorKeybinds[menuItem.Text];
                    Keys shortcut = keybind.Key;
                    if (keybind.Ctrl) shortcut |= Keys.Control;
                    if (keybind.Shift) shortcut |= Keys.Shift;
                    if (keybind.Alt) shortcut |= Keys.Alt;
                    menuItem.ShortcutKeys = shortcut;
                    menuItem.ShowShortcutKeys = false; // Keep shortcuts hidden in menu
                }
            }
            
            // Update keyboard shortcut handlers
            SetupKeyboardShortcuts();
        }

        #endregion

        #region File Operations

        private void BtnNewProject_Click(object sender, EventArgs e)
        {
            ResetButtonState(sender);
            
            string selectedPath = ModernFolderDialog.Show(this, "Select Folder for New Project");
            if (string.IsNullOrEmpty(selectedPath))
                return;

            if (Directory.Exists(selectedPath) && Directory.GetFileSystemEntries(selectedPath).Length > 0)
            {
                var result = ReaLTaiizor.Controls.PoisonMessageBox.Show(
                    this,
                    "The selected folder is not empty. Continue anyway?",
                    "Folder Not Empty",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                
                if (result != DialogResult.Yes)
                    return;
            }

            try
            {
                Directory.CreateDirectory(selectedPath);
                string scriptsPath = Path.Combine(selectedPath, "scripts");
                Directory.CreateDirectory(scriptsPath);

                string mainContent = GetDefaultMainContent();
                string headersContent = GetDefaultHeadersContent();

                File.WriteAllText(Path.Combine(scriptsPath, "main.gsc"), mainContent);
                File.WriteAllText(Path.Combine(scriptsPath, "headers.gsc"), headersContent);

                string gameSymbol = currentGame == TreyarchCompiler.Enums.Games.T7 ? "bo3" : "bo4";
                string gameModeLower = currentGameModeStr.ToLower();
                File.WriteAllText(Path.Combine(selectedPath, "gsc.conf"), 
                    $"symbols={gameSymbol},serious,{gameModeLower}");

                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, "Project created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                OpenFolder(selectedPath);
            }
            catch (Exception ex)
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, $"Error creating project: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnOpenFolder_Click(object sender, EventArgs e)
        {
            ResetButtonState(sender);
            
            string selectedPath = ModernFolderDialog.Show(this, "Select Project Folder");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                OpenFolder(selectedPath);
            }
        }

        private void BtnNewFile_Click(object sender, EventArgs e)
        {
            ResetButtonState(sender);
            
            if (!folderOpened)
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, "Please open a project folder first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // Use NewFileDialog with proper validation (matches original T7-Compiler-UI)
            if (NewFileDialog.ShowNewFileDialog(this, projectPath, out string fileName, out string fileExtension))
            {
                    RefreshFileList(false);
                    
                    // Open the new file
                OpenFileInEditor($"{fileName}.{fileExtension}");
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            ResetButtonState(sender);
            SaveCurrentFile();
        }

        private void BtnSaveAll_Click(object sender, EventArgs e)
        {
            ResetButtonState(sender);
            SaveAllFiles();
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            ResetButtonState(sender);
            RefreshFileList(false);
        }

        private void BtnPortIL_Click(object sender, EventArgs e)
        {
            ResetButtonState(sender);
            
            string ilProjectPath = ModernFolderDialog.Show(this, "Select IL (Infinity Loader) Project Folder");
            if (string.IsNullOrEmpty(ilProjectPath) || !Directory.Exists(ilProjectPath))
                return;

            var ilFiles = Directory.GetFiles(ilProjectPath, "*.il", SearchOption.AllDirectories);
            if (ilFiles.Length == 0)
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, "No .il files found. This doesn't appear to be an IL project.", 
                    "Invalid Project", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string outputPath = ModernFolderDialog.Show(this, "Select Output Folder (must be empty)");
            if (string.IsNullOrEmpty(outputPath))
                return;

            if (Directory.Exists(outputPath) && Directory.GetFileSystemEntries(outputPath).Length > 0)
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, "Output folder must be empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                Directory.CreateDirectory(outputPath);
                string scriptsPath = Path.Combine(outputPath, "scripts");
                Directory.CreateDirectory(scriptsPath);

                FileHelper.CopyDirectory(ilProjectPath, scriptsPath, true);

                foreach (var ilFile in Directory.GetFiles(scriptsPath, "*.il", SearchOption.AllDirectories))
                {
                    File.Delete(ilFile);
                }

                foreach (var gscFile in Directory.GetFiles(scriptsPath, "*.gsc", SearchOption.AllDirectories))
                {
                    string content = File.ReadAllText(gscFile);
                    string lower = content.ToLower();
                    
                    while (lower.Contains("enableonlinematch"))
                    {
                        int index = lower.IndexOf("enableonlinematch");
                        content = content.Substring(0, index) + "getplayers" + content.Substring(index + "enableonlinematch".Length);
                        lower = content.ToLower();
                    }
                    
                    File.WriteAllText(gscFile, content);
                }

                string gameSymbol = currentGame == TreyarchCompiler.Enums.Games.T7 ? "bo3" : "bo4";
                string gameModeLower = currentGameModeStr.ToLower();
                File.WriteAllText(Path.Combine(outputPath, "gsc.conf"), 
                    $"symbols={gameSymbol},serious,{gameModeLower}");

                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, "IL project ported successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                OpenFolder(outputPath);
            }
            catch (Exception ex)
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, $"Error porting IL project: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Store last search parameters for Find Next
        private string lastSearchText = string.Empty;
        private SearchFlags lastSearchFlags = SearchFlags.None;

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            ResetButtonState(sender);
            
            if (tabControl.SelectedTab == null)
                return;
                
            using (var searchDialog = new SearchDialog())
            {
                if (searchDialog.ShowDialog() == DialogResult.OK)
                {
                    lastSearchText = searchDialog.SearchText;
                    lastSearchFlags = SearchFlags.None;
                    if (searchDialog.MatchCase)
                        lastSearchFlags |= SearchFlags.MatchCase;
                    if (searchDialog.WholeWord)
                        lastSearchFlags |= SearchFlags.WholeWord;
                    
                    FindNext();
                }
            }
        }
        
        private void FindNext()
        {
            if (string.IsNullOrEmpty(lastSearchText))
            {
                BtnSearch_Click(null, EventArgs.Empty);
                return;
            }
            
            Scintilla currentEditor = GetCurrentEditor();
            if (currentEditor == null)
                return;
            
            // Start search from current position
                        int startPos = currentEditor.CurrentPosition;
                        int endPos = currentEditor.TextLength;
                        
                        // Set search flags
            currentEditor.SearchFlags = lastSearchFlags;
                        
                        // Set search target
                        currentEditor.TargetStart = startPos;
                        currentEditor.TargetEnd = endPos;
                        
                        // Search for text
            int foundPos = currentEditor.SearchInTarget(lastSearchText);
            
            if (foundPos >= 0)
            {
                currentEditor.SetSelection(currentEditor.TargetStart, currentEditor.TargetEnd);
                currentEditor.ScrollCaret();
            }
            else
            {
                // Wrap around - search from beginning
                currentEditor.TargetStart = 0;
                currentEditor.TargetEnd = startPos;
                foundPos = currentEditor.SearchInTarget(lastSearchText);
                        
                        if (foundPos >= 0)
                        {
                            currentEditor.SetSelection(currentEditor.TargetStart, currentEditor.TargetEnd);
                            currentEditor.ScrollCaret();
                        }
                        else
                        {
                            ReaLTaiizor.Controls.PoisonMessageBox.Show(this, "Text not found.", "Search", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        
        private void BtnReplace_Click(object sender, EventArgs e)
        {
            ResetButtonState(sender);
            
            if (tabControl.SelectedTab == null)
                return;
                
            using (var replaceDialog = new ReplaceDialog())
            {
                if (replaceDialog.ShowDialog() == DialogResult.OK)
                {
                    string searchText = replaceDialog.SearchText;
                    string replaceText = replaceDialog.ReplaceText;
                    bool matchCase = replaceDialog.MatchCase;
                    bool wholeWord = replaceDialog.WholeWord;
                    bool replaceAll = replaceDialog.ReplaceAll;
                    
                    Scintilla currentEditor = GetCurrentEditor();
                    if (currentEditor == null)
                return;

                    // Set search flags
                    SearchFlags searchFlags = SearchFlags.None;
                    if (matchCase)
                        searchFlags |= SearchFlags.MatchCase;
                    if (wholeWord)
                        searchFlags |= SearchFlags.WholeWord;
                    
                    currentEditor.SearchFlags = searchFlags;
                    
                    if (replaceAll)
                    {
                        // Replace all occurrences
                        int replaceCount = 0;
                        currentEditor.TargetStart = 0;
                        currentEditor.TargetEnd = currentEditor.TextLength;
                        
                        while (currentEditor.SearchInTarget(searchText) >= 0)
                        {
                            currentEditor.ReplaceTarget(replaceText);
                            replaceCount++;
                            currentEditor.TargetStart = currentEditor.TargetEnd;
                            currentEditor.TargetEnd = currentEditor.TextLength;
                        }
                        
                        ReaLTaiizor.Controls.PoisonMessageBox.Show(this, 
                            $"Replaced {replaceCount} occurrence(s).", 
                            "Replace", 
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Information);
                    }
                    else
                    {
                        // Replace current selection or find next
                        if (currentEditor.SelectedText == searchText)
                        {
                            currentEditor.ReplaceSelection(replaceText);
                        }
                        else
                        {
                            // Find next and replace
                            int startPos = currentEditor.CurrentPosition;
                            int endPos = currentEditor.TextLength;
                            
                            currentEditor.TargetStart = startPos;
                            currentEditor.TargetEnd = endPos;
                            
                            if (currentEditor.SearchInTarget(searchText) >= 0)
                            {
                                currentEditor.ReplaceTarget(replaceText);
                                currentEditor.SetSelection(currentEditor.TargetStart, currentEditor.TargetEnd);
                                currentEditor.ScrollCaret();
                            }
                            else
                            {
                                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, "Text not found.", "Replace", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Hash Checker

        // Store the last used hashes.txt path
        private string lastHashesFilePath = null;
        
        /// <summary>
        /// Loads and parses the hashes.txt file from the project's compiled folder
        /// </summary>
        private bool LoadHashesFile(bool promptIfNotFound = false)
        {
            hashToFunctionMap.Clear();
            
            if (string.IsNullOrEmpty(projectPath) || !folderOpened)
            {
                if (promptIfNotFound)
                {
                    PromptForHashesFile();
                }
                return false;
            }
            
            // Look for hashes.txt in common locations
            string[] possiblePaths = new[]
            {
                Path.Combine(projectPath, "compiled", "hashes.txt"),
                Path.Combine(projectPath, "build", "hashes.txt"),
                Path.Combine(projectPath, "hashes.txt"),
                Path.Combine(Directory.GetParent(projectPath)?.FullName ?? "", "compiled", "hashes.txt"),
                Path.Combine(Directory.GetParent(projectPath)?.FullName ?? "", "build", "hashes.txt")
            };
            
            // If we have a last used path, check it first
            if (!string.IsNullOrEmpty(lastHashesFilePath) && File.Exists(lastHashesFilePath))
            {
                possiblePaths = new[] { lastHashesFilePath }.Concat(possiblePaths).ToArray();
            }
            
            string hashesPath = null;
            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    hashesPath = path;
                    break;
                }
            }
            
            if (string.IsNullOrEmpty(hashesPath))
            {
                // Try to find hashes.txt recursively
                try
                {
                    string[] foundFiles = Directory.GetFiles(projectPath, "hashes.txt", SearchOption.AllDirectories);
                    if (foundFiles.Length > 0)
                    {
                        hashesPath = foundFiles[0];
                    }
                }
                catch
                {
                    // Ignore errors
                }
            }
            
            if (string.IsNullOrEmpty(hashesPath) || !File.Exists(hashesPath))
            {
                if (promptIfNotFound)
                {
                    PromptForHashesFile();
                    return !string.IsNullOrEmpty(lastHashesFilePath) && File.Exists(lastHashesFilePath);
                }
                return false;
            }
            
            // Store the path for future use
            lastHashesFilePath = hashesPath;
            
            try
            {
                string[] lines = File.ReadAllLines(hashesPath);
                foreach (string line in lines)
                {
                    // Skip comments and empty lines
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                        continue;
                    
                    // Parse format: "0x006CB618, functionname" or "0x006CB618, 0bogsindex..."
                    string[] parts = line.Split(new[] { ',' }, 2);
                    if (parts.Length == 2)
                    {
                        string hash = parts[0].Trim();
                        string functionName = parts[1].Trim();
                        
                        // Skip obfuscated function names (those starting with "0bogs")
                        if (functionName.StartsWith("0bogs", StringComparison.OrdinalIgnoreCase))
                            continue;
                        
                        // Store hash without "0x" prefix for easier lookup
                        string hashKey = hash.Replace("0x", "").Replace("0X", "").ToUpper();
                        if (!hashToFunctionMap.ContainsKey(hashKey))
                        {
                            hashToFunctionMap[hashKey] = functionName;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Silently fail - hash checker is optional
                System.Diagnostics.Debug.WriteLine($"Error loading hashes.txt: {ex.Message}");
                return false;
            }
            
            return true; // Successfully loaded
        }
        
        /// <summary>
        /// Prompts the user to select the hashes.txt file location
        /// </summary>
        private void PromptForHashesFile()
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Hash Files|hashes.txt|All Files|*.*";
                dialog.Title = "Select hashes.txt file";
                dialog.CheckFileExists = true;
                
                // Set initial directory to project path or last used path
                if (!string.IsNullOrEmpty(projectPath) && Directory.Exists(projectPath))
                {
                    dialog.InitialDirectory = projectPath;
                }
                else if (!string.IsNullOrEmpty(lastHashesFilePath))
                {
                    dialog.InitialDirectory = Path.GetDirectoryName(lastHashesFilePath);
                    dialog.FileName = Path.GetFileName(lastHashesFilePath);
                }
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    lastHashesFilePath = dialog.FileName;
                    LoadHashesFile(false); // Reload with the new path
                }
            }
        }
        
        /// <summary>
        /// Looks up a function name from a hash
        /// </summary>
        private string GetFunctionNameFromHash(string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
                return null;
            
            // Normalize hash (remove 0x prefix, uppercase)
            string hashKey = hash.Replace("0x", "").Replace("0X", "").Trim().ToUpper();
            
            if (hashToFunctionMap.ContainsKey(hashKey))
            {
                return hashToFunctionMap[hashKey];
            }
            
            return null;
        }
        
        /// <summary>
        /// Searches for a function in all open files and navigates to it
        /// </summary>
        private bool NavigateToFunction(string functionName)
        {
            if (string.IsNullOrWhiteSpace(functionName))
                return false;
            
            // Try different patterns: functionName(, functionName {, functionName =, etc.
            string[] searchPatterns = new[]
            {
                $@"\b{Regex.Escape(functionName)}\s*\(",
                $@"\b{Regex.Escape(functionName)}\s*{{",
                $@"\b{Regex.Escape(functionName)}\s*=",
                $@"\b{Regex.Escape(functionName)}\s*;",
                $@"\b{Regex.Escape(functionName)}\b"
            };
            
            // Search in all open files
            foreach (var kvp in openEditors)
            {
                string fileName = kvp.Key;
                Scintilla editor = kvp.Value;
                
                if (editor == null)
                    continue;
                
                string content = editor.Text;
                
                // Try each search pattern
                foreach (string pattern in searchPatterns)
                {
                    Match match = Regex.Match(content, pattern, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        // Found it! Navigate to this position
                        int position = match.Index;
                        
                        // Switch to this tab
                        if (editorTabs.ContainsKey(fileName))
                        {
                            tabControl.SelectedTab = editorTabs[fileName];
                        }
                        
                        // Set cursor position and scroll to it
                        editor.GotoPosition(position);
                        editor.SetSelection(position, position + match.Length);
                        editor.ScrollCaret();
                        
                        return true;
                    }
                }
            }
            
            // If not found in open files, try to find and open the file
            if (!string.IsNullOrEmpty(projectPath) && Directory.Exists(projectPath))
            {
                try
                {
                    // Search for .gsc files containing the function
                    string[] gscFiles = Directory.GetFiles(projectPath, "*.gsc", SearchOption.AllDirectories);
                    
                    foreach (string filePath in gscFiles)
                    {
                        try
                        {
                            string content = File.ReadAllText(filePath);
                            
                            // Try each search pattern
                            foreach (string pattern in searchPatterns)
                            {
                                Match match = Regex.Match(content, pattern, RegexOptions.IgnoreCase);
                                if (match.Success)
                                {
                                    // Found it! Open the file
                                    string relativePath = GetRelativePath(projectPath, filePath);
                                    OpenFileInEditor(relativePath);
                                    
                                    // Wait a bit for the file to open, then navigate
                                    Application.DoEvents();
                                    System.Threading.Thread.Sleep(50);
                                    
                                    if (openEditors.ContainsKey(relativePath))
                                    {
                                        Scintilla editor = openEditors[relativePath];
                                        int position = match.Index;
                                        editor.GotoPosition(position);
                                        editor.SetSelection(position, position + match.Length);
                                        editor.ScrollCaret();
                                    }
                                    
                                    return true;
                                }
                            }
                        }
                        catch
                        {
                            // Skip files that can't be read
                            continue;
                        }
                    }
                }
                catch
                {
                    // Ignore errors
                }
            }
            
            return false;
        }
        
        private void BtnHashCheck_Click(object sender, EventArgs e)
        {
            ResetButtonState(sender);
            
            string hashInput = txtHashInput.Text?.Trim();
            if (string.IsNullOrWhiteSpace(hashInput))
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this,
                    "Please enter a hash (e.g., DBC91BB1 or 0xDBC91BB1)",
                    "Hash Checker",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }
            
            // Load hashes if not already loaded, prompt if not found
            if (hashToFunctionMap.Count == 0)
            {
                bool loaded = LoadHashesFile(promptIfNotFound: true);
                if (!loaded)
                {
                    ReaLTaiizor.Controls.PoisonMessageBox.Show(this,
                        "Could not find hashes.txt file. Please select the file location.",
                        "Hash File Not Found",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    PromptForHashesFile();
                    
                    // Try loading again after user selects file
                    if (hashToFunctionMap.Count == 0)
                    {
                        ReaLTaiizor.Controls.PoisonMessageBox.Show(this,
                            "No hash file loaded. Please select hashes.txt to use the hash checker.",
                            "No Hash File",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }
                }
            }
            
            // Lookup function name
            string functionName = GetFunctionNameFromHash(hashInput);
            
            if (string.IsNullOrEmpty(functionName))
            {
                // Offer to reload or select a different file
                var result = ReaLTaiizor.Controls.PoisonMessageBox.Show(this,
                    $"Hash '{hashInput}' not found in hashes.txt.\n\nWould you like to select a different hashes.txt file?",
                    "Hash Not Found",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                
                if (result == DialogResult.Yes)
                {
                    PromptForHashesFile();
                    // Try lookup again
                    functionName = GetFunctionNameFromHash(hashInput);
                    if (string.IsNullOrEmpty(functionName))
                    {
                        ReaLTaiizor.Controls.PoisonMessageBox.Show(this,
                            $"Hash '{hashInput}' still not found in the selected hashes.txt file.",
                            "Hash Not Found",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
            
            // Navigate to function
            bool found = NavigateToFunction(functionName);
            
            if (!found)
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this,
                    $"Function '{functionName}' found in hashes.txt, but could not locate it in the project files.\n\nHash: {hashInput}\nFunction: {functionName}",
                    "Function Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            else
            {
                // Show a brief message
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this,
                    $"Found function: {functionName}\nHash: {hashInput}",
                    "Function Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
        
        private void TxtHashInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                BtnHashCheck_Click(btnHashCheck, EventArgs.Empty);
            }
        }

        #endregion

        #region Folder and File Management

        private void OpenFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return;

            long dirSize = FileHelper.DirSize(path);
            if (dirSize > 5000000) // 5MB
            {
                var result = ReaLTaiizor.Controls.PoisonMessageBox.Show(
                    this,
                    "This appears to be a large folder, continuing will destroy FOLDER STRUCTURE ARE YOU SURE WANT TO CONTINUE?",
                    "WARNING",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                
                if (result != DialogResult.Yes)
                    return;
                    
                var m = ReaLTaiizor.Controls.PoisonMessageBox.Show(
                    this,
                    "Do you really wish to open this folder? IT WILL DESTROY YOUR FOLDER STRUCTURE",
                    "WARNING",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                    
                if (m != DialogResult.Yes)
                    return;
            }

            try
            {
                // Clear existing tabs and editors when opening a new folder
                if (tabControl.TabPages.Count > 0)
                {
                    tabControl.TabPages.Clear();
                    openEditors.Clear();
                    editorTabs.Clear();
                }
                fileButtonsPanel.Controls.Clear();
            }
            catch { }

            // Set project path and folder opened BEFORE calling RefreshFileList
            projectPath = path;
            folderOpened = true;
            CompilerActions.menu = Path.GetFileName(projectPath);
            lblProjectPath.Text = $"Project: {CompilerActions.menu}";

            // Check for IL project
            foreach (string file in Directory.GetFiles(path))
            {
                if (file.Contains(".il"))
                {
                    var a = ReaLTaiizor.Controls.PoisonMessageBox.Show(
                        this,
                        "This could be an Infinity Loader Project. Would you like to port it? (required)",
                        "Error",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (a != DialogResult.Yes)
                    {
                        ReaLTaiizor.Controls.PoisonMessageBox.Show(this, "One project failed to load: (IL PROJECT)", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        CreateDefaultProjectOnStartup();
                        return;
                    }

                    PortILProject(path, "unknown");
                    return;
                }
            }

            // Auto-detect project structure - don't force scripts folder
            // Only move contents if scripts folder exists and has no files, or if no GSC files found in root
            string scriptsPath = Path.Combine(projectPath, "scripts");
            bool hasGscFilesInRoot = Directory.GetFiles(projectPath, "*.gsc", SearchOption.TopDirectoryOnly).Length > 0;
            
            // Only move contents if scripts folder exists and project appears to need organization
            if (Directory.Exists(scriptsPath) && !hasGscFilesInRoot)
            {
            MoveContentsToScripts(scriptsPath);
            }

            // Setup file watcher to watch entire project folder (not just scripts)
            if (fileWatcher != null)
            {
                fileWatcher.Path = projectPath;
                fileWatcher.Filter = "*.gsc";
                fileWatcher.IncludeSubdirectories = true; // Watch subdirectories too
                fileWatcher.EnableRaisingEvents = true;
            }

            // Load symbols from gsc.conf
            LoadSymbolsFromGscConf();
            
            // Load hashes.txt for hash checker
            LoadHashesFile(false);
            
            // Now refresh file list (folderOpened and projectPath are set)
            RefreshFileList(false);
            UpdateTitle();
        }

        private void MoveContentsToScripts(string path)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            if (!dirInfo.Exists)
                Directory.CreateDirectory(path);

            List<string> scripts = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).ToList();

            foreach (string file in scripts)
            {
                FileInfo mFile = new FileInfo(file);
                // to remove name collisions
                if (!new FileInfo(Path.Combine(dirInfo.FullName, mFile.Name)).Exists)
                {
                    try
                    {
                        mFile.MoveTo(Path.Combine(dirInfo.FullName, mFile.Name));
                    }
                    catch
                    {
                        // File might be in use or already in correct location
                    }
                }
            }
        }

        private void RefreshFileList(bool force)
        {
            if (!force && !folderOpened)
                return;

            // Clear existing tabs and editors when refreshing
            if (tabControl.TabPages.Count > 0)
            {
                tabControl.TabPages.Clear();
                openEditors.Clear();
                editorTabs.Clear();
            }
            fileButtonsPanel.Controls.Clear();

            if (!folderOpened || string.IsNullOrEmpty(projectPath))
                return;

            // Auto-detect GSC project: search recursively for .gsc and .txt files anywhere in the project folder
            // This supports various folder structures (scripts/, root, subfolders, etc.)
            string[] gscFiles = Directory.GetFiles(projectPath, "*.gsc", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(projectPath, "*.txt", SearchOption.AllDirectories))
                .Where(f => !f.EndsWith(".gscc", StringComparison.OrdinalIgnoreCase) && 
                            !f.EndsWith(".stub.gscc", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => {
                    // Always put main.gsc first (case-insensitive)
                    string fileName = Path.GetFileName(f);
                    if (fileName.Equals("main.gsc", StringComparison.OrdinalIgnoreCase))
                        return "0" + fileName; // Prefix with 0 to sort first
                    return "1" + fileName; // Other files come after
                })
                .ThenBy(f => Path.GetFileName(f)) // Then sort alphabetically
                .ToArray();

            if (gscFiles == null || gscFiles.Length == 0)
            {
                // No GSC files found - check if scripts folder exists and create it if needed
            string scriptsPath = Path.Combine(projectPath, "scripts");
            if (!Directory.Exists(scriptsPath))
            {
                Directory.CreateDirectory(scriptsPath);
                }
                return;
            }

            // Set loading flag to prevent hasChanges during file loading
            isLoadingFiles = true;

            try
            {
            int i = 0;
            foreach (string gscFile in gscFiles)
            {
                    // Get relative path from projectPath to preserve folder structure
                    string relativePath = GetRelativePath(projectPath, gscFile);
                string filename = Path.GetFileName(gscFile);
                    
                if (filename.EndsWith(".gsc") || filename.EndsWith(".txt"))
                {
                        // Use relative path as the key to preserve folder structure
                        CreateFileButton(relativePath, i);
                        OpenFileInEditor(relativePath);
                    i++;
                }
                }
            }
            finally
            {
                // Always clear the loading flag, even if an exception occurs
                // This ensures user edits will properly set hasChanges
                isLoadingFiles = false;
            }
            
            // Ensure hasChanges is false after loading all files - use CheckForUnsavedChanges to verify
            CheckForUnsavedChanges();
            
            folderOpened = true;
        }

        private void CreateFileButton(string filename, int index)
        {
            // Create container panel for file button and close button - use PoisonPanel for theme consistency
            PoisonPanel buttonContainer = new PoisonPanel
            {
                Height = 23,
                Width = fileButtonsPanel.Width - 10,
                Margin = new Padding(0, 2, 0, 2)
            };
            
            // Apply theme to container panel
            if (styleManager != null)
            {
                buttonContainer.StyleManager = styleManager;
                buttonContainer.UseStyleColors = true;
            }

            // File button
            PoisonButton fileButton = new PoisonButton
            {
                Text = filename.Replace(' ', '_'),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                FlatStyle = FlatStyle.Flat,
                UseStyleColors = true
            };
            
            // Apply theme to file button
            if (styleManager != null)
            {
                fileButton.StyleManager = styleManager;
                fileButton.UseStyleColors = true;
            }
            
            fileButton.Click += (s, e) => {
                OpenFileInEditor(filename);
                ResetButtonState(fileButton);
            };
            fileButton.Tag = index;

            // Close button (X) - use theme-aware colors
            PoisonButton closeButton = new PoisonButton
            {
                Text = "X",
                Size = new Size(20, 23),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            
            // Set theme-aware colors
            if (styleManager != null)
            {
                closeButton.BackColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Button.Normal(styleManager.Theme);
                closeButton.ForeColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.ForeColor.Button.Normal(styleManager.Theme);
            }
            else
            {
                closeButton.BackColor = Color.FromArgb(45, 45, 45);
                closeButton.ForeColor = Color.White;
            }
            
            closeButton.MouseEnter += (s, e) => {
                closeButton.BackColor = Color.Red;
            };
            closeButton.MouseLeave += (s, e) => {
                if (styleManager != null)
                    closeButton.BackColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Button.Normal(styleManager.Theme);
                else
                    closeButton.BackColor = Color.FromArgb(45, 45, 45);
            };
            closeButton.Click += (s, e) => {
                DeleteFile(filename, index);
            };
            closeButton.Name = filename.Replace(".gsc", "").Replace(".txt", "").Replace(' ', '_');

            buttonContainer.Controls.Add(closeButton);
            buttonContainer.Controls.Add(fileButton);
            fileButtonsPanel.Controls.Add(buttonContainer);
        }

        private void DeleteFile(string filename, int index)
        {
            var result = ReaLTaiizor.Controls.PoisonMessageBox.Show(
                this,
                $"Warning: This will delete the file ({filename}) Continue?",
                "Warning",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            // Get file path - filename may be a relative path or simple filename
            string filePath;
            if (filename.Contains(Path.DirectorySeparatorChar) || filename.Contains(Path.AltDirectorySeparatorChar))
            {
                // Relative path - use it directly
                filePath = Path.Combine(projectPath, filename);
            }
            else
            {
                // Simple filename - check scripts folder first, then search
                filePath = Path.Combine(projectPath, "scripts", filename);
                if (!File.Exists(filePath))
                {
                    string[] foundFiles = Directory.GetFiles(projectPath, filename, SearchOption.AllDirectories);
                    if (foundFiles.Length > 0)
                        filePath = foundFiles[0];
                }
            }
            
            if (File.Exists(filePath))
                File.Delete(filePath);

            // Remove from editors
            if (editorTabs.ContainsKey(filename))
            {
                tabControl.TabPages.Remove(editorTabs[filename]);
                editorTabs.Remove(filename);
                openEditors.Remove(filename);
            }

            RefreshFileList(false);
            
            if (tabControl.TabPages.Count > 0)
            {
                if (index > 0 && index <= tabControl.TabPages.Count)
                    tabControl.SelectedIndex = index - 1;
                else
                    tabControl.SelectedIndex = 0;
            }
        }

        #endregion

        #region Editor Management

        private void OpenFileInEditor(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return;

            // Support both relative paths (from RefreshFileList) and simple filenames (for new files)
            // If filename contains path separators, treat it as a relative path from projectPath
            // Otherwise, check scripts folder first (for backward compatibility), then search recursively
            string filePath;
            if (filename.Contains(Path.DirectorySeparatorChar) || filename.Contains(Path.AltDirectorySeparatorChar))
            {
                // Relative path - use it directly
                filePath = Path.Combine(projectPath, filename);
            }
            else
            {
                // Simple filename - check scripts folder first, then search recursively
                filePath = Path.Combine(projectPath, "scripts", filename);
                if (!File.Exists(filePath))
                {
                    // Search recursively for the file
                    string[] foundFiles = Directory.GetFiles(projectPath, filename, SearchOption.AllDirectories);
                    if (foundFiles.Length > 0)
                    {
                        filePath = foundFiles[0];
                        // Update filename to relative path for consistency
                        filename = GetRelativePath(projectPath, filePath);
                    }
                }
            }
            
            if (!File.Exists(filePath))
                return;

            // Check if already open
            if (editorTabs.ContainsKey(filename))
            {
                tabControl.SelectedTab = editorTabs[filename];
                return;
            }

            // Create new tab and editor
            // Use just the filename without path, and ensure it's not too long to prevent overlapping
            string tabText = Path.GetFileName(filename);
            
            // Limit tab text length to prevent overlapping (tabs need space for close button and padding)
            // Typical tab needs ~100-120px width for comfortable display
            const int maxTabTextLength = 18;
            if (tabText.Length > maxTabTextLength)
            {
                string nameWithoutExt = Path.GetFileNameWithoutExtension(tabText);
                string ext = Path.GetExtension(tabText);
                
                // Truncate name part, keep extension
                if (nameWithoutExt.Length > maxTabTextLength - ext.Length - 3)
                {
                    nameWithoutExt = nameWithoutExt.Substring(0, maxTabTextLength - ext.Length - 3) + "...";
                }
                tabText = nameWithoutExt + ext;
            }
            
            ReaLTaiizor.Controls.PoisonTabPage tabPage = new ReaLTaiizor.Controls.PoisonTabPage
            {
                Text = tabText,
                ToolTipText = filename // Show full filename in tooltip
            };
            
            // Ensure tab doesn't wrap text
            tabPage.UseVisualStyleBackColor = false;
            Scintilla editor = CreateScintillaEditor();
            
            // Load file content
            string content = File.ReadAllText(filePath);
            
            // Store original content in fileContents for comparison
            fileContents[filename] = content;
            
            // Track changes (but ignore during file loading)
            EventHandler textChangedHandler = (s, e) => {
                // Only set hasChanges if we're not currently loading files
                if (!isLoadingFiles)
                {
                    // Always check all files for changes when any editor changes
                    // This ensures we catch changes in any open file
                    // Use BeginInvoke to ensure this runs on UI thread
                    if (this.InvokeRequired)
                    {
                        try
                        {
                            if (!this.IsDisposed && !this.Disposing && this.IsHandleCreated)
                            {
                                this.BeginInvoke(new Action(() => {
                                    if (!isLoadingFiles && !this.IsDisposed && !this.Disposing)
                                    {
                                        CheckForUnsavedChanges();
                                        // Update conditional compilation indicators when text changes
                                        UpdateConditionalCompilationIndicators(editor);
                                        // Update syntax highlighting indicators when text changes
                                        UpdateSyntaxHighlightingIndicators(editor);
                                    }
                                }));
                            }
                        }
                        catch (ObjectDisposedException) { }
                        catch (InvalidOperationException) { }
                        catch (ArgumentException) { }
                    }
                    else
                    {
                        CheckForUnsavedChanges();
                        // Update conditional compilation indicators when text changes
                        UpdateConditionalCompilationIndicators(editor);
                        // Update syntax highlighting indicators when text changes
                        UpdateSyntaxHighlightingIndicators(editor);
                    }
                }
            };
            
            // Set loading flag to prevent hasChanges from being set during initialization
            isLoadingFiles = true;
            
            try
            {
                // Temporarily disable events to prevent TextChanged from firing during setup
                editor.TextChanged -= textChangedHandler; // Ensure it's not already attached
                
                // Load text BEFORE attaching event handler to prevent initial load from triggering hasChanges
            editor.Text = content;
            fileContents[filename] = content;

                // Clear undo history after loading (so undo doesn't go back to empty state)
                editor.EmptyUndoBuffer();

                // Setup syntax highlighting BEFORE attaching handler (this might modify text)
            SetupGSCSyntaxHighlighting(editor);
                
                // Ensure margins stay disabled after syntax highlighting setup
                DisableExtraMargins(editor);
                
                // Setup conditional compilation indicators
                SetupConditionalCompilationIndicators(editor);
                
                // Update syntax highlighting indicators (include paths, method calls)
                UpdateSyntaxHighlightingIndicators(editor);
                
                // Ensure margins stay disabled after indicator updates
                DisableExtraMargins(editor);
                
                // Clear undo history again after syntax highlighting (in case it modified text)
                editor.EmptyUndoBuffer();
                
                // Now attach the event handler after everything is set up
                // Make sure it's not already attached
                editor.TextChanged -= textChangedHandler;
                editor.TextChanged += textChangedHandler;
            }
            finally
            {
                // Always clear the loading flag AFTER handler is attached
                // This ensures user edits will properly set hasChanges
                isLoadingFiles = false;
                
                // Process any queued events after clearing the flag
                Application.DoEvents();
            }

            // Add to tab
            tabPage.Controls.Add(editor);
            tabControl.TabPages.Add(tabPage);
            tabControl.SelectedTab = tabPage;

            // Store references
            openEditors[filename] = editor;
            editorTabs[filename] = tabPage;

            currentFileName = filename;
            selectedTabItem = filename;
            
            // Ensure hasChanges is false after loading - use CheckForUnsavedChanges to verify
            CheckForUnsavedChanges();
            
            // Update conditional compilation indicators after file is loaded
            UpdateConditionalCompilationIndicators(editor);
            
            // Update syntax highlighting indicators after file is loaded
            // Apply indicators AFTER lexer has finished coloring to ensure they override
            // In ScintillaNET, TextFore indicators should override lexer colors when applied correctly
            UpdateSyntaxHighlightingIndicators(editor);
            
            // Force Scintilla to refresh the display to ensure indicators are visible
            // This helps ensure that TextFore indicators properly override lexer keyword colors
            // Also refresh scrollbars to prevent white scrollbar artifacts
            editor.Invalidate();
            editor.Update();
            editor.Refresh();
        }

        private Scintilla CreateScintillaEditor()
        {
            Scintilla editor = new Scintilla
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                ScrollWidth = 4000, // Set a reasonable default scroll width
                ScrollWidthTracking = true, // Auto-adjust scroll width based on content
                HScrollBar = true, // Show horizontal scrollbar when needed (line wrap is disabled)
                VScrollBar = true // Show vertical scrollbar only when needed
            };
            editor.StyleResetDefault();
            
            // Disable word wrap - use horizontal scrollbar instead
            editor.WrapMode = WrapMode.None;
            
            // Undo/redo is enabled by default in ScintillaNET
            // We'll use EmptyUndoBuffer() after loading/saving to reset the undo history
            
            DisableExtraMargins(editor);

            // Configure editor - using ScintillaStyle alias to avoid conflict with ReaLTaiizor
            // Use theme-aware colors if style manager is available
            Color editorBackColor = Color.FromArgb(16, 16, 16);
            Color editorForeColor = Color.FromArgb(225, 225, 225);
            Color caretLineColor = Color.FromArgb(30, 30, 30);
            Color selectionColor = Color.FromArgb(50, 50, 50);
            Color caretColor = Color.White;
            
            if (styleManager != null)
            {
                // Adjust for dark/light theme using Poison theme colors
                if (styleManager.Theme == ReaLTaiizor.Enum.Poison.ThemeStyle.Dark)
                {
                    // Dark theme - use Poison dark colors
                    editorBackColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Form(styleManager.Theme);
                    // Use a slightly lighter color for text to ensure readability
                    editorForeColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.ForeColor.Label.Normal(styleManager.Theme);
                    caretColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.ForeColor.Label.Normal(styleManager.Theme);
                    // Slightly lighter background for caret line
                    caretLineColor = Color.FromArgb(
                        Math.Min(255, editorBackColor.R + 20),
                        Math.Min(255, editorBackColor.G + 20),
                        Math.Min(255, editorBackColor.B + 20));
                    selectionColor = Color.FromArgb(
                        Math.Min(255, editorBackColor.R + 40),
                        Math.Min(255, editorBackColor.G + 40),
                        Math.Min(255, editorBackColor.B + 40));
                }
                else
                {
                    // Light theme - use Poison light colors
                    editorBackColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Form(styleManager.Theme);
                    editorForeColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.ForeColor.Label.Normal(styleManager.Theme);
                    caretColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.ForeColor.Label.Normal(styleManager.Theme);
                    caretLineColor = Color.FromArgb(240, 240, 240);
                    selectionColor = Color.FromArgb(200, 200, 200);
                }
            }
            
            editor.Styles[ScintillaStyle.Default].Font = "Consolas";
            editor.Styles[ScintillaStyle.Default].Size = 10;
            editor.Styles[ScintillaStyle.Default].BackColor = editorBackColor;
            editor.Styles[ScintillaStyle.Default].ForeColor = editorForeColor;
            editor.StyleClearAll();

            // Line numbers margin
            editor.Margins[0].Width = 50;
            editor.Margins[0].Type = MarginType.Number;
            editor.Margins[0].Sensitive = false; // Prevent clicking on margin from causing issues
            
            // Set margin background color to match editor background - CRITICAL for theme consistency
            // Set margin properties in correct order to ensure proper rendering
            editor.Margins[0].Type = MarginType.Number;
            editor.Margins[0].Width = 50;
            editor.Margins[0].Sensitive = false;
            editor.Margins[0].BackColor = editorBackColor; // Set background color
            
            // Set line number text color (slightly dimmer than main text)
            Color lineNumberColor = styleManager != null && styleManager.Theme == ReaLTaiizor.Enum.Poison.ThemeStyle.Dark
                ? Color.FromArgb(150, 150, 150) // Light grey for dark theme
                : Color.FromArgb(100, 100, 100); // Dark grey for light theme
            
            // Set line number style background and foreground - BOTH must match margin background
            // This is critical - Scintilla uses the style BackColor for the margin background
            editor.Styles[ScintillaStyle.LineNumber].BackColor = editorBackColor;
            editor.Styles[ScintillaStyle.LineNumber].ForeColor = lineNumberColor;
            
            // Ensure margin mask is set (line numbers don't need mask, but this ensures proper rendering)
            editor.Margins[0].Mask = 0;
            
            DisableExtraMargins(editor);

            // Configure caret - use theme-aware colors
            editor.CaretForeColor = caretColor;
            editor.CaretLineVisible = true;
            editor.CaretLineBackColor = caretLineColor;

            // Configure selection - use theme-aware colors
            editor.SetSelectionBackColor(true, selectionColor);

            // Setup code completion (AutoC)
            SetupCodeCompletion(editor);
            
            // Force initial refresh to prevent white scrollbar artifacts
            // This ensures scrollbars are properly initialized and don't appear white
            editor.Invalidate();
            editor.Update();
            
            // Use a delayed refresh to catch any scrollbar rendering issues after full initialization
            System.Windows.Forms.Timer scrollbarFixTimer = new System.Windows.Forms.Timer();
            scrollbarFixTimer.Interval = 150; // Small delay to ensure editor is fully initialized
            scrollbarFixTimer.Tick += (s, e) =>
            {
                scrollbarFixTimer.Stop();
                scrollbarFixTimer.Dispose();
                
                if (editor != null && !editor.IsDisposed)
                {
                    // Force refresh after initialization to fix any white scrollbar artifacts
                    editor.Invalidate();
                    editor.Update();
                    editor.Refresh();
                }
            };
            scrollbarFixTimer.Start();

            return editor;
        }
        
        // Store completion list as a field so it's accessible in the event handler
        private string gscCompletionList = null;
        
        private void DisableExtraMargins(Scintilla editor)
        {
            // Disable all margins except margin 0 (line numbers) to prevent "custom scrollbar" issues
            // Scintilla has 5 margins by default (0-4), we need to disable 1-4
            for (int i = 1; i <= 4; i++)
            {
                try
                {
                    editor.Margins[i].Width = 0;
                    editor.Margins[i].Type = MarginType.Symbol;
                    editor.Margins[i].Sensitive = false;
                    editor.Margins[i].Mask = 0; // Clear any mask
                    editor.Margins[i].Cursor = MarginCursor.Arrow; // Set cursor to arrow
                }
                catch
                {
                    // Ignore errors if margin doesn't exist
                }
            }
            
            // Also ensure fold margins are disabled
            editor.SetFoldFlags(FoldFlags.LineAfterContracted);
            
            // Disable automatic folding
            editor.AutomaticFold = (AutomaticFold)0;
            
            // Ensure margin 0 (line numbers) is properly configured and doesn't show scrollbar
            try
            {
                editor.Margins[0].Type = MarginType.Number;
                editor.Margins[0].Sensitive = false; // Prevent clicking from causing issues
                editor.Margins[0].Mask = 0; // No markers in line number margin
                editor.Margins[0].Cursor = MarginCursor.Arrow; // Normal cursor
            }
            catch
            {
                // Ignore errors
            }
            
            // Note: We don't need to hide fold markers since we've disabled folding
            // and set all margins (except 0) to width 0, which prevents them from appearing
        }
        
        private void SetupCodeCompletion(Scintilla editor)
        {
            // Build completion list from GSC keywords and built-in functions
            // This will be populated with all keywords from GSC.xshd
            if (gscCompletionList == null)
            {
                gscCompletionList = BuildCompletionList();
            }
            
            // Configure auto-completion using ScintillaNET properties
            editor.AutoCSeparator = ' ';  // Space-separated list
            editor.AutoCMaxHeight = 10;   // Show max 10 items
            editor.AutoCIgnoreCase = true; // Case-insensitive
            editor.AutoCAutoHide = false;  // Don't auto-hide
            editor.AutoCCancelAtStart = false; // Don't cancel at start
            // Note: AutoCFillUps might not be available in this version of ScintillaNET
            // editor.AutoCFillUps = "()[]{}.,;:+-*/=!&|<>?~^%"; // Characters that complete
            editor.AutoCChooseSingle = true; // Auto-select if only one match
            
            // Handle character input to trigger completion
            editor.CharAdded += (s, e) => {
                Scintilla scintilla = s as Scintilla;
                if (scintilla == null) return;
                
                char ch = (char)e.Char;
                int pos = scintilla.CurrentPosition;
                int startPos = scintilla.WordStartPosition(pos, true);
                
                // Trigger completion after typing 2+ characters
                if (char.IsLetter(ch) || ch == '_')
                {
                    string word = scintilla.GetTextRange(startPos, pos);
                    if (word.Length >= 2)
                    {
                        // Show auto-completion with the completion list
                        scintilla.AutoCShow(word.Length, gscCompletionList);
                    }
                }
            };
        }
        
        private string BuildCompletionList()
        {
            // Load GSC syntax data from GSC.xshd if not already loaded
            LoadGscSyntaxData();
            
            // Build completion list from GSC keywords (deduplicated using HashSet)
            var keywords = new HashSet<string>();
            
            // Main keywords from GSC.xshd
            if (gscSyntaxData != null && gscSyntaxData.Keywords.Count > 0)
            {
                keywords.UnionWith(gscSyntaxData.Keywords);
            }
            else
            {
                // Fallback to hardcoded keywords
                keywords.UnionWith("$_ player vararg class object new event var return thread undefined self world classes level game anim if else do while for foreach in waittill waittillmatch waittillframeend switch case default break continue notify endon assert assertmsg constructor destructor autoexec private const isdefined vectorscale waitrealtime profilestart profilestop .size wait waitframe".Split(' '));
            }
            
            // TrueFalse keywords
            if (gscSyntaxData != null && gscSyntaxData.TrueFalse.Count > 0)
            {
                keywords.UnionWith(gscSyntaxData.TrueFalse);
            }
            else
            {
                keywords.UnionWith("true false".Split(' '));
            }
            
            // Command/preprocessor keywords
            if (gscSyntaxData != null && gscSyntaxData.CommandKeywords.Count > 0)
            {
                keywords.UnionWith(gscSyntaxData.CommandKeywords);
            }
            else
            {
                keywords.UnionWith("#using_animtree #animtree #namespace #precache #include fn function callback".Split(' '));
            }
            
            if (gscSyntaxData != null && gscSyntaxData.PreprocessorKeywords.Count > 0)
            {
                keywords.UnionWith(gscSyntaxData.PreprocessorKeywords);
            }
            else
            {
                keywords.UnionWith("#if #elif #else #endif #insert #define #ifdef #ifndef".Split(' '));
            }
            
            // Built-in functions from GSC.xshd (this is the complete list from the file)
            if (gscSyntaxData != null && gscSyntaxData.BuiltInFunctions.Count > 0)
            {
                keywords.UnionWith(gscSyntaxData.BuiltInFunctions);
            }
            else
            {
                // Fallback to a subset of common built-in functions
                keywords.UnionWith("getent getentarray getentbynum getentitynumber getentitytype getentnum getplayers getplayername getplayerspawnid getplayerspeed getplayervehicle getplayercorpse getplayergibdef getplayergravity getplayerlastoutwatertime getorigin getangles setorigin setangles getcentroid getmaxs getmins getabsmaxs getabsmins getowner getparententity getlinkedent getmoverent getgroundent geteye geteyeapprox getaimangles getdistancefromscreencenter gethorizontaloffsetfromscreencenter getlocalplayer getlocalplayers getlocalplayerteam getnonpredictedlocalplayer getactivelocalclients getlobbyclientcount getnumconnectedplayers getnumexpectedplayers getnormalhealth getammocount getcurrentweaponincludingmelee getcurrentgunrank getequippedheroindex getequippedheromode getequippedloadoutitemforhero getequippedshowcaseweaponforhero getloadoutitem getloadoutweapon getloadoutperks getloadoutallocation getloadoutgunsmithvariantindex getloadoutitemref getitemarray getitemattachment getitemattachmentallocationcost getitemgroupforweaponname getitemgroupfromitemindex getbaseweaponitemindex getattachmentnames getattachmentcosmeticvariantforweapon getbuildkitweapon getbuildkitweaponoptions getbuildkitattachmentcosmeticvariantindexes getrandomcompatibleattachmentsforweapon getequippedbodyforhero getequippedbodyindexforhero getequippedheadindexforhero getequippedhelmetforhero getequippedhelmetindexforhero getequippedbodyaccentcolorforhero getequippedhelmetaccentcolorforhero getcharacterindex getcharacterdisplayname getcharacterassetname getcharacterbodymodelcount getcharacterbodymodelcolorcount getcharacterheadrenderoptions getcharacterhelmetmodelcount getcharacterhelmetmodelcolorcount getcharacterhelmetrenderoptions getcharacterhelmethideshead getcharacterbodystyleindex getcharactercustomizationforxuid getcharactermoderenderoptions getbodyrenderoptionspacked getfirstheadofgender getfirstheroofgender getheadgender getherogender getheroes getherobodymodelindices getheroheadmodelindices getherohelmetmodelindices getbodyaccentcolorcountforhero gethelmetaccentcolorcountforhero getallcharacterbodies getallcharacterheads getclassindexfromname getgametypeenumfromname getgametypesetting getshoutcastersetting getcontractname getcontractrequiredcount getcontractrequirements getcontractresetconditions getmissionname getmissionuniqueid getmissionversion getrootmapname getmapatindex getmapfields getmaporder getmapintromovie getmapoutromovie getnextmap getskiptoname getskiptos getcurrenteventid getcurrenteventname getcurrenteventoriginator getcurrenteventtype getcurrenteventtypename geteventpointofinterest getscriptbundle getscriptbundlelist getscriptbundlenames getscriptbundles getscriptmoverarray getlocalclientnumber getlocalclientcount getmaxlocalclients getlocalclientangles getlocalclientpos getlocalclienteyepos getlocalclientfov getlocalclientdriver getlocalgunnerangles getcontrollerposition getcontrollertype getenterbutton getclienttime getrealtime getmillisecondsraw getplaybacktime getcurrentanimscriptedname getanimlength getanimtime getanimframecount getanimcurrframecount getanimforcharacter getanimstatecategory getprimarydeltaanim getcorpseanim getentityanimrate getfootstepstrings getnotetracktimes getnotetracksindelta findanimbyname animhasnotetrack animrelative animscripted animmappingsearch asmsetanimationrate clearanim clearanimlimited camanimscripted endcamanimscripted extracamanimscripted endextracamanimscripted cameraforcedisablescriptcam camerasetlensid camerasetupdatecallback getcamangles getcamanglesbylocalclientnum getcamanimtime getcampos getcamposbylocalclientnum getdynent getdynentarray createdynentandlaunch cleanupspawneddynents getnode getnodearray getnodearraysorted getnodesinradius getnodesinradiussorted getnearestnode getnearestpathpoint getallnodes getpathfindingradius getpathmetric getclosestpointonnavmesh getclosestpointonnavvolume getnavmeshfacenormal getnavmeshtriggersforpoint getentnavmaterial getnodeindexonpath getnodeowner getnoderegion getnexttraversalnodeonpath getothernodeinnegotiationpair getcovernodearray getanynodearray getgrappletargetarray connectpaths disconnectpaths deletepathnode dropnodetofloor drawnode canclaimnode canpath findpath getaiarray getaicount getailimit getaiteamarray getaispeciesarray getaiarchetypearray getactorarray getactorteamarray getactorspawnerarray getactorspawnerteamarray getfreeactorcount getactorweaponoptions getaifxname getaitriggerflags getassignedteam getassignedteamname getenemies getenemyscrambleramount getfriendlyscrambleramount clearnearestenemyscrambler addfriendlyscrambler getinfluenceat getinfluencefacepos getinfluencenumfaces getinfluencerpreset getinfluencertimeoutremaining getbestinfluencepos enableinfluencer addinfluencer addorientedinfluencer addentityinfluencer evsetranges getinterestpoolawareness getinterestpoolvalue addtointerestpool getdamageableentarray dodamage clearplayergravity setplayergravity getlastoutwatertime depthinwater depthofplayerinwater gethealthoverlaytime forcepainon getequipmentheadobjective getcrateheadobjective getretrievableweapons getdroppedweapons getrope getlightcolor getlightintensity getlightradius getlightexponent getlightfovouter getfogsettings getreflectionlocs getreflectionorigin getdecorations getpartname getnumparts getknownlength getmovedelta getmovementtype getmovespeedscale getmaxreversespeed getmaxvehicles getnumfreeentities getbrushmodelcenter getpointinbounds getshootatpos getangledelta getanglefrombits getbitsforangle getnorthyaw getdebugeteye getinkillcam getkillcamentity getmigrationstatus getserverhighestclientfieldversion getclientfieldversion codegetclientfield codegetplayerstateclientfield codegetuimodelclientfield codegetworldclientfield codesetclientfield codesetplayerstateclientfield codesetuimodelclientfield codesetworldclientfield codeincrementclientfield codeincrementplayerstateclientfield codeincrementuimodelclientfield codeincrementworldclientfield getsnapshotindexarray getcountertotal getnumchallengescomplete getnumberofcollectiblesforlevel clearlastupdatedcollectibles delete attach detach detachall attachshieldmodel detachshieldmodel attachweapon getcorpsearray forcedelete cloneandremoveentity wait notify endon thread level game self world undefined isdefined".Split(' '));
            }
            
            return string.Join(" ", keywords);
        }
        
        // SetupFindReplace removed - find/replace is handled via keyboard shortcuts and dialogs

        // GSC syntax highlighting data loaded from GSC.xshd
        private static Helpers.GscSyntaxParser.GscSyntaxData gscSyntaxData = null;
        
        // GSC syntax highlighting color constants (loaded from GSC.xshd)
        // Color names match the <Color name="..."> definitions in GSC.xshd
        private static Color CommentColor => GetGscColor("Comment", Color.FromArgb(77, 166, 66));      // #4DA642 - green
        private static Color StringColor => GetGscColor("String", Color.FromArgb(205, 153, 131));     // #CD9983 - orange/brown
        private static Color KeywordColor => GetGscColor("Keywords", Color.FromArgb(86, 156, 214));     // #569cd6 - blue
        private static Color CommandColor => GetGscColor("CommandKeywords", Color.FromArgb(197, 134, 192));    // #c586c0 - purple
        private static Color NumberColor => GetGscColor("NumberLiteral", Color.FromArgb(162, 206, 159));     // #a2ce9f - light green
        private static Color IncludePathColor => GetGscColor("IncludePath", Color.FromArgb(245, 156, 66)); // #f59c42 - orange/yellow
        private static Color MethodCallColor => GetGscColor("MethodCall", Color.FromArgb(220, 220, 170)); // #DCDCAA - yellow/beige
        private static Color BuiltInFunctionColor => GetGscColor("BuiltInFunctions", Color.FromArgb(86, 156, 214)); // #569cd6 - blue (same as keywords)
        private static Color PreprocessorColor => GetGscColor("PreprocessorKeywords", Color.FromArgb(197, 134, 192)); // #c586c0 - purple (same as CommandKeywords)
        
        /// <summary>
        /// Gets a color from GSC.xshd or returns a default fallback
        /// </summary>
        private static Color GetGscColor(string colorName, Color fallback)
        {
            if (gscSyntaxData?.Colors != null && gscSyntaxData.Colors.ContainsKey(colorName))
                return gscSyntaxData.Colors[colorName];
            return fallback;
        }
        
        /// <summary>
        /// Loads GSC syntax data from GSC.xshd file
        /// </summary>
        private static void LoadGscSyntaxData()
        {
            if (gscSyntaxData != null)
                return; // Already loaded
            
            try
            {
                string xshdPath = Helpers.GscSyntaxParser.GetDefaultGscXshdPath();
                if (!string.IsNullOrEmpty(xshdPath) && File.Exists(xshdPath))
                {
                    gscSyntaxData = Helpers.GscSyntaxParser.ParseGscXshd(xshdPath);
                }
            }
            catch (Exception ex)
            {
                // If parsing fails, use defaults (gscSyntaxData remains null)
                System.Diagnostics.Debug.WriteLine($"Failed to load GSC.xshd: {ex.Message}");
            }
        }
        
        // Indicator indices for custom highlighting
        private const int INCLUDE_PATH_INDICATOR = 1;
        private const int METHOD_CALL_INDICATOR = 2;

        private void SetupGSCSyntaxHighlighting(Scintilla editor)
        {
            // Load GSC syntax data from GSC.xshd if not already loaded
            LoadGscSyntaxData();
            
            editor.Lexer = Lexer.Cpp;
            
            // Clear all keyword lists first to remove Cpp lexer's built-in keywords
            // This ensures only our custom keywords are used
            editor.SetKeywords(0, "");
            editor.SetKeywords(1, "");
            editor.SetKeywords(2, "");
            editor.SetKeywords(3, "");
            editor.SetKeywords(4, "");
            
            // Set comment colors (all comment styles use same color)
            var commentStyles = new[] { ScintillaStyle.Cpp.Comment, ScintillaStyle.Cpp.CommentLine, 
                ScintillaStyle.Cpp.CommentLineDoc, ScintillaStyle.Cpp.CommentDoc, 
                ScintillaStyle.Cpp.CommentDocKeyword, ScintillaStyle.Cpp.CommentDocKeywordError };
            foreach (var style in commentStyles)
                editor.Styles[style].ForeColor = CommentColor;
            
            // Set string colors - ensure all string-related styles use orange/brown (#CD9983)
            // This must match GSC.xshd: <Color name="String" foreground="#CD9983"/>
            var stringStyles = new[] { ScintillaStyle.Cpp.String, ScintillaStyle.Cpp.StringRaw, 
                ScintillaStyle.Cpp.Character, ScintillaStyle.Cpp.StringEol, ScintillaStyle.Cpp.Verbatim };
            foreach (var style in stringStyles)
            {
                editor.Styles[style].ForeColor = StringColor; // #CD9983 - orange/brown
                editor.Styles[style].BackColor = editor.Styles[ScintillaStyle.Default].BackColor;
                // Force the style to use our color (not inherited from default)
                editor.Styles[style].Italic = false;
                editor.Styles[style].Bold = false;
            }
            
            // Keywords (blue #569cd6) - main GSC keywords from GSC.xshd
            // Load from parsed GSC.xshd if available, otherwise use hardcoded fallback
            string keywords = gscSyntaxData != null && gscSyntaxData.Keywords.Count > 0
                ? string.Join(" ", gscSyntaxData.Keywords)
                : "$_ player vararg class object new event var return thread undefined self world classes level game anim if else do while for foreach in waittill waittillmatch waittillframeend switch case default break continue notify endon assert assertmsg constructor destructor autoexec private const isdefined vectorscale waitrealtime profilestart profilestop .size wait waitframe";
            editor.SetKeywords(0, keywords);
            editor.Styles[ScintillaStyle.Cpp.Word].ForeColor = KeywordColor;
            editor.Styles[ScintillaStyle.Cpp.Word].Bold = false;
            
            // TrueFalse keywords (blue #569cd6) - true, false
            // These are included in the main keywords list, so they'll be blue automatically
            
            // CommandKeywords (purple #c586c0) - #include, #namespace, fn, function, callback
            // Load from parsed GSC.xshd if available, otherwise use hardcoded fallback
            string commandKeywords = gscSyntaxData != null && gscSyntaxData.CommandKeywords.Count > 0
                ? string.Join(" ", gscSyntaxData.CommandKeywords)
                : "#using_animtree #animtree #namespace #precache #include fn function callback";
            editor.SetKeywords(1, commandKeywords);
            editor.Styles[ScintillaStyle.Cpp.Word2].ForeColor = CommandColor;
            editor.Styles[ScintillaStyle.Cpp.Word2].Bold = false;
            
            // PreprocessorKeywords (purple #c586c0) - #if, #ifdef, #define, etc.
            // Note: ScintillaNET Cpp lexer automatically recognizes preprocessor directives
            // and styles them using the Preprocessor style, so we don't need to add them as keywords.
            editor.Styles[ScintillaStyle.Cpp.Preprocessor].ForeColor = CommandColor;
            editor.Styles[ScintillaStyle.Cpp.PreprocessorComment].ForeColor = CommentColor;
            
            // Numbers
            editor.Styles[ScintillaStyle.Cpp.Number].ForeColor = NumberColor;
            
            // Operators and identifiers use theme-aware default foreground (white)
            // This ensures identifiers like "system" are white, not blue
            editor.Styles[ScintillaStyle.Cpp.Operator].ForeColor = editor.Styles[ScintillaStyle.Default].ForeColor;
            editor.Styles[ScintillaStyle.Cpp.Identifier].ForeColor = editor.Styles[ScintillaStyle.Default].ForeColor;
            
            // Also ensure that any other identifier-related styles are white
            // This prevents the lexer from coloring "system" as a built-in keyword
            editor.Styles[ScintillaStyle.Cpp.GlobalClass].ForeColor = editor.Styles[ScintillaStyle.Default].ForeColor;
            
            // Setup indicators for include paths and method calls
            SetupSyntaxHighlightingIndicators(editor);
            
            // Note: ScintillaNET automatically applies styling when text is loaded/changed,
            // so we don't need to manually call Colorize/Colourise. The styles are applied
            // immediately when the lexer and styles are set above.
        }
        
        private void SetupSyntaxHighlightingIndicators(Scintilla editor)
        {
            // Include path indicator (orange/yellow #f59c42) - highlights paths after #include
            // Use TextFore style to change text color directly (more visible than boxes)
            editor.Indicators[INCLUDE_PATH_INDICATOR].Style = IndicatorStyle.TextFore;
            editor.Indicators[INCLUDE_PATH_INDICATOR].ForeColor = IncludePathColor;
            editor.Indicators[INCLUDE_PATH_INDICATOR].Alpha = 255;
            editor.Indicators[INCLUDE_PATH_INDICATOR].OutlineAlpha = 255;
            editor.Indicators[INCLUDE_PATH_INDICATOR].Under = false;
            
            // Method call indicator (yellow/beige #DCDCAA) - highlights function calls
            // Use TextFore style to change text color directly - this should override lexer colors
            // In ScintillaNET 3.6.3, TextFore indicators change the text color and should override lexer colors
            editor.Indicators[METHOD_CALL_INDICATOR].Style = IndicatorStyle.TextFore;
            editor.Indicators[METHOD_CALL_INDICATOR].ForeColor = MethodCallColor;
            editor.Indicators[METHOD_CALL_INDICATOR].Alpha = 255;
            editor.Indicators[METHOD_CALL_INDICATOR].OutlineAlpha = 255;
            editor.Indicators[METHOD_CALL_INDICATOR].Under = false;
        }
        
        private void UpdateSyntaxHighlightingIndicators(Scintilla editor)
        {
            if (editor == null) return;
            
            // Clear existing indicators
            editor.IndicatorCurrent = INCLUDE_PATH_INDICATOR;
            editor.IndicatorClearRange(0, editor.TextLength);
            editor.IndicatorCurrent = METHOD_CALL_INDICATOR;
            editor.IndicatorClearRange(0, editor.TextLength);
            
            string text = editor.Text;
            if (string.IsNullOrEmpty(text))
                return;
            
            // Convert character positions to byte positions for ScintillaNET
            System.Text.Encoding encoding = System.Text.Encoding.UTF8;
            
            // Get inactive code block ranges to skip highlighting in inactive code
            HashSet<string> activeSymbols = GetActiveSymbols();
            List<CodeBlock> blocks = ParseConditionalBlocks(text);
            var inactiveRanges = new List<Tuple<int, int>>(); // List of (startByte, endByte) for inactive blocks
            foreach (var block in blocks)
            {
                if (!block.IsActive)
                {
                    int startLine = block.StartLine + 1;
                    if (startLine < editor.Lines.Count)
                    {
                        int startPos = editor.Lines[startLine].Position;
                        int endLine = block.EndLine < editor.Lines.Count ? block.EndLine - 1 : editor.Lines.Count - 1;
                        if (endLine >= startLine)
                        {
                            int endPos = editor.Lines[endLine].EndPosition;
                            inactiveRanges.Add(new Tuple<int, int>(startPos, endPos));
                        }
                    }
                }
            }
            
            // Helper method to check if a byte position is in an inactive code block
            bool IsInInactiveBlock(int bytePos)
            {
                foreach (var range in inactiveRanges)
                {
                    if (bytePos >= range.Item1 && bytePos < range.Item2)
                        return true;
                }
                return false;
            }
            
            // Highlight include paths (from "scripts\" to ";")
            // Pattern matches: #include scripts\...; (highlights the path part)
            // Based on original GSC.xshd: <Span color="IncludePath"><Begin>scripts\\"</Begin><End>;</End></Span>
            var includePathRegex = new System.Text.RegularExpressions.Regex(
                @"#include\s+(scripts\\.*?);",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline);
            
            foreach (System.Text.RegularExpressions.Match match in includePathRegex.Matches(text))
            {
                if (match.Groups.Count > 1 && match.Groups[1].Success)
                {
                    // Get the path group (group 1) - this is what we want to highlight
                    var pathGroup = match.Groups[1];
                    int charStart = pathGroup.Index;
                    int charLength = pathGroup.Length;
                    
                    // Convert character positions to byte positions for ScintillaNET
                    int byteStart = encoding.GetByteCount(text.Substring(0, charStart));
                    int byteLength = encoding.GetByteCount(text.Substring(charStart, charLength));
                    
                    // Ensure we don't exceed editor bounds
                    if (byteStart >= 0 && byteLength > 0 && byteStart + byteLength <= editor.TextLength)
                    {
                        editor.IndicatorCurrent = INCLUDE_PATH_INDICATOR;
                        editor.IndicatorFillRange(byteStart, byteLength);
                    }
                }
            }
            
            // Also highlight #namespace names (similar pattern to include paths)
            // Pattern: #namespace name; or #namespace name, (highlights the name part)
            // Based on original GSC.xshd - namespace names should be highlighted like include paths (orange/yellow)
            var namespaceRegex = new System.Text.RegularExpressions.Regex(
                @"#namespace\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*[;,]", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline);
            
            foreach (System.Text.RegularExpressions.Match match in namespaceRegex.Matches(text))
            {
                if (match.Groups.Count > 1 && match.Groups[1].Success)
                {
                    var nameGroup = match.Groups[1];
                    int charStart = nameGroup.Index;
                    int charLength = nameGroup.Length;
                    
                    int byteStart = encoding.GetByteCount(text.Substring(0, charStart));
                    int byteLength = encoding.GetByteCount(text.Substring(charStart, charLength));
                    
                    if (byteStart >= 0 && byteLength > 0 && byteStart + byteLength <= editor.TextLength)
                    {
                        editor.IndicatorCurrent = INCLUDE_PATH_INDICATOR;
                        editor.IndicatorFillRange(byteStart, byteLength);
                    }
                }
            }
            
            // Highlight method calls (identifiers followed by "(")
            // Based on original GSC.xshd: <Rule color="MethodCall">\b[\d\w_]+(?=\s*\()</Rule>
            // In AvalonEdit, rules are processed AFTER keywords, so keywords are already colored
            // and won't be matched by this rule. We need to match ALL identifiers followed by "("
            // and exclude only those that are keywords.
            
            // Keywords that should NOT be highlighted as method calls (they're already colored by lexer)
            // These are from GSC.xshd: Keywords, CommandKeywords, and BuiltInFunctions lists
            var excludedKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // CommandKeywords (purple) - from GSC.xshd line 174-183
                "fn", "function", "callback", "#using_animtree", "#animtree", "#namespace", "#precache", "#include",
                // PreprocessorKeywords (purple) - from GSC.xshd line 185-194
                "#if", "#elif", "#else", "#endif", "#insert", "#define", "#ifdef", "#ifndef",
                // Regular keywords (blue) - from GSC.xshd line 121-172
                "$_", "player", "vararg", "class", "object", "new", "event", "var", "return", "thread", 
                "undefined", "self", "world", "classes", "level", "game", "anim", "if", "else", "do", 
                "while", "for", "foreach", "in", "waittill", "waittillmatch", "waittillframeend", 
                "switch", "case", "default", "break", "continue", "notify", "endon", "assert", "assertmsg", 
                "constructor", "destructor", "autoexec", "private", "const", "isdefined", "vectorscale", 
                "waitrealtime", "profilestart", "profilestop", ".size", "wait", "waitframe",
                // TrueFalse keywords (blue) - from GSC.xshd line 116-119
                "true", "false"
                // Note: Function definition names (init, on_player_connect, etc.) are NOT excluded
                // because they should be highlighted as method calls (yellow), not as keywords (blue)
            };
            
            // Track which positions we've already highlighted to avoid double-highlighting
            var highlightedRanges = new HashSet<int>();
            
            // First, handle namespace::function patterns (e.g., system::register, callback::on_start_gametype)
            // In these patterns, the function name should be highlighted as a method call, even if it's a keyword
            // Pattern: namespace::function(
            var namespaceFunctionRegex = new System.Text.RegularExpressions.Regex(
                @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s*::\s*([a-zA-Z_][a-zA-Z0-9_]*)\s*\(",
                System.Text.RegularExpressions.RegexOptions.Multiline);
            
            foreach (System.Text.RegularExpressions.Match match in namespaceFunctionRegex.Matches(text))
            {
                if (match.Groups.Count >= 3)
                {
                    // Highlight the function name part (group 2), NOT the namespace
                    // The function name should be highlighted even if it's a keyword
                    // because in this context it's a method call, not a keyword usage
                    int charStart = match.Groups[2].Index; // Start at function name, not namespace
                    int charLength = match.Groups[2].Length;
                    int byteStart = encoding.GetByteCount(text.Substring(0, charStart));
                    int byteLength = encoding.GetByteCount(text.Substring(charStart, charLength));
                    
                    // Skip highlighting if this is in an inactive code block
                    if (IsInInactiveBlock(byteStart))
                        continue;
                    
                    if (byteStart >= 0 && byteLength > 0 && byteStart + byteLength <= editor.TextLength)
                    {
                        editor.IndicatorCurrent = METHOD_CALL_INDICATOR;
                        editor.IndicatorFillRange(byteStart, byteLength);
                        
                        // Mark this range as highlighted
                        for (int i = byteStart; i < byteStart + byteLength; i++)
                            highlightedRanges.Add(i);
                    }
                }
            }
            
            // Match ALL identifiers followed by "(" - this is the MethodCall rule from GSC.xshd
            // Pattern: \b[\d\w_]+(?=\s*\() - matches any identifier followed by (
            var methodCallRegex = new System.Text.RegularExpressions.Regex(
                @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s*\(",
                System.Text.RegularExpressions.RegexOptions.Multiline);
            
            foreach (System.Text.RegularExpressions.Match match in methodCallRegex.Matches(text))
            {
                if (match.Groups.Count >= 2)
                {
                    string identifier = match.Groups[1].Value;
                    
                    // Skip if this is a keyword (already colored by lexer)
                    if (excludedKeywords.Contains(identifier))
                        continue;
                    
                    int charStart = match.Groups[1].Index;
                    int charLength = match.Groups[1].Length;
                    int byteStart = encoding.GetByteCount(text.Substring(0, charStart));
                    int byteLength = encoding.GetByteCount(text.Substring(charStart, charLength));
                    
                    // Skip highlighting if this is in an inactive code block
                    if (IsInInactiveBlock(byteStart))
                        continue;
                    
                    // Check if already highlighted (e.g., by namespace::function pattern)
                    bool alreadyHighlighted = false;
                    for (int i = byteStart; i < byteStart + byteLength; i++)
                    {
                        if (highlightedRanges.Contains(i))
                        {
                            alreadyHighlighted = true;
                            break;
                        }
                    }
                    
                    if (!alreadyHighlighted && byteStart >= 0 && byteLength > 0 && byteStart + byteLength <= editor.TextLength)
                    {
                        editor.IndicatorCurrent = METHOD_CALL_INDICATOR;
                        editor.IndicatorFillRange(byteStart, byteLength);
                        
                        // Mark this range as highlighted
                        for (int i = byteStart; i < byteStart + byteLength; i++)
                            highlightedRanges.Add(i);
                    }
                }
            }
            
            // Also highlight ::function patterns in function arguments (e.g., ::init in callback::on_start_gametype(::init))
            // This matches function references, not calls, but should still be highlighted
            // Note: In this context, even keywords like "init" should be highlighted as function references
            var namespaceFunctionArgRegex = new System.Text.RegularExpressions.Regex(
                @"::([a-zA-Z_][a-zA-Z0-9_]*)\s*[\),]",
                System.Text.RegularExpressions.RegexOptions.Multiline);
            
            foreach (System.Text.RegularExpressions.Match match in namespaceFunctionArgRegex.Matches(text))
            {
                if (match.Groups.Count >= 2)
                {
                    // Highlight ALL function references (::function), even if the function name is a keyword
                    // because in this context it's a function reference, not a keyword usage
                    int charStart = match.Groups[1].Index; // Start at function name after ::
                    int charLength = match.Groups[1].Length;
                    int byteStart = encoding.GetByteCount(text.Substring(0, charStart));
                    int byteLength = encoding.GetByteCount(text.Substring(charStart, charLength));
                    
                    // Check if already highlighted
                    bool alreadyHighlighted = false;
                    for (int i = byteStart; i < byteStart + byteLength; i++)
                    {
                        if (highlightedRanges.Contains(i))
                        {
                            alreadyHighlighted = true;
                            break;
                        }
                    }
                    
                    // Skip highlighting if this is in an inactive code block
                    if (IsInInactiveBlock(byteStart))
                        continue;
                    
                    if (!alreadyHighlighted && byteStart >= 0 && byteLength > 0 && byteStart + byteLength <= editor.TextLength)
                    {
                        editor.IndicatorCurrent = METHOD_CALL_INDICATOR;
                        editor.IndicatorFillRange(byteStart, byteLength);
                        
                        // Mark this range as highlighted
                        for (int i = byteStart; i < byteStart + byteLength; i++)
                            highlightedRanges.Add(i);
                    }
                }
            }
        }

        private void SetupConditionalCompilationIndicators(Scintilla editor)
        {
            // Configure indicator for inactive code blocks (grayed out)
            const int INACTIVE_CODE_INDICATOR = 0;
            editor.Indicators[INACTIVE_CODE_INDICATOR].Style = IndicatorStyle.StraightBox;
            editor.Indicators[INACTIVE_CODE_INDICATOR].Alpha = 150; // Semi-transparent overlay
            editor.Indicators[INACTIVE_CODE_INDICATOR].OutlineAlpha = 150;
            
            // Set indicator color based on theme - this will overlay inactive code
            if (styleManager != null && styleManager.Theme == ReaLTaiizor.Enum.Poison.ThemeStyle.Dark)
            {
                // Dark theme: use a darker gray overlay to dim inactive code
                editor.Indicators[INACTIVE_CODE_INDICATOR].ForeColor = Color.FromArgb(40, 40, 40);
            }
            else
            {
                // Light theme: use a lighter gray overlay to dim inactive code
                editor.Indicators[INACTIVE_CODE_INDICATOR].ForeColor = Color.FromArgb(240, 240, 240);
            }
        }

        private void UpdateConditionalCompilationIndicators()
        {
            // Update indicators for all open editors
            foreach (var editor in openEditors.Values)
            {
                if (editor != null)
                {
                    UpdateConditionalCompilationIndicators(editor);
                }
            }
        }

        private void UpdateConditionalCompilationIndicators(Scintilla editor)
        {
            if (editor == null) return;
            
            const int INACTIVE_CODE_INDICATOR = 0;
            
            // Clear existing indicators
            editor.IndicatorCurrent = INACTIVE_CODE_INDICATOR;
            editor.IndicatorClearRange(0, editor.TextLength);
            
            // Get active symbols from current game mode and gsc.conf
            HashSet<string> activeSymbols = GetActiveSymbols();
            
            // Parse conditional compilation blocks
            string text = editor.Text;
            if (string.IsNullOrEmpty(text))
                return;
            
            List<CodeBlock> blocks = ParseConditionalBlocks(text);
            
            // Apply indicators to inactive blocks
            foreach (var block in blocks)
            {
                if (!block.IsActive)
                {
                    // Check if this is a one-line block (#ifdef ... #endif on same line)
                    if (block.StartLine == block.EndLine)
                    {
                        // One-line block: highlight the content between #ifdef and #endif
                        string line = editor.Lines[block.StartLine].Text;
                        
                        // Find positions of #ifdef/#ifndef and #endif anywhere on the line
                        Regex ifdefRegex = new Regex(@"#if(def|ndef)\s+(\w+)", RegexOptions.IgnoreCase);
                        Regex endifRegex = new Regex(@"#endif\b", RegexOptions.IgnoreCase);
                        
                        Match ifdefMatch = ifdefRegex.Match(line);
                        Match endifMatch = endifRegex.Match(line);
                        
                        if (ifdefMatch.Success && endifMatch.Success)
                        {
                            // Get the line's start position
                            int lineStartPos = editor.Lines[block.StartLine].Position;
                            
                            // Calculate byte positions for the content between #ifdef and #endif
                            System.Text.Encoding encoding = System.Text.Encoding.UTF8;
                            int ifdefEndChar = ifdefMatch.Index + ifdefMatch.Length;
                            int endifStartChar = endifMatch.Index;
                            
                            // Convert character positions to byte positions
                            int ifdefEndByte = encoding.GetByteCount(line.Substring(0, ifdefEndChar));
                            int endifStartByte = encoding.GetByteCount(line.Substring(0, endifStartChar));
                            
                            // Highlight the content between #ifdef and #endif
                            int startPos = lineStartPos + ifdefEndByte;
                            int length = endifStartByte - ifdefEndByte;
                            
                            if (length > 0)
                            {
                                editor.IndicatorCurrent = INACTIVE_CODE_INDICATOR;
                                editor.IndicatorFillRange(startPos, length);
                            }
                        }
                    }
                    else
                    {
                        // Multi-line block: highlight from line after #ifdef to line before #endif
                        int startLine = block.StartLine + 1;
                        if (startLine >= editor.Lines.Count)
                            continue;
                            
                        int startPos = editor.Lines[startLine].Position;
                        int endLine = block.EndLine < editor.Lines.Count ? block.EndLine - 1 : editor.Lines.Count - 1;
                        if (endLine < startLine)
                            continue;
                            
                        int endPos = editor.Lines[endLine].EndPosition;
                        
                        // Apply indicator to inactive code (this will overlay with a semi-transparent box)
                        editor.IndicatorCurrent = INACTIVE_CODE_INDICATOR;
                        editor.IndicatorFillRange(startPos, endPos - startPos);
                    }
                }
            }
        }

        // Track which custom symbols are enabled (user can toggle these)
        private Dictionary<string, bool> customSymbolStates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private List<string> availableCustomSymbols = new List<string>();
        
        private HashSet<string> GetActiveSymbols()
        {
            HashSet<string> symbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            // Add current game mode symbol
            if (!string.IsNullOrEmpty(currentGameModeStr))
            {
                symbols.Add(currentGameModeStr.ToUpper());
            }
            
            // Add game symbol (BO3 or BO4)
            string gameSymbol = currentGame == TreyarchCompiler.Enums.Games.T7 ? "BO3" : "BO4";
            symbols.Add(gameSymbol);
            
            // Add "serious" symbol (always active)
            symbols.Add("SERIOUS");
            
            // Add custom symbols that are enabled AND their parent conditions are met
            foreach (var kvp in customSymbolStates)
            {
                if (kvp.Value) // If symbol is enabled
                {
                    string symbol = kvp.Key.ToUpper();
                    
                    // Check if this symbol has parent conditions (nested #ifdef blocks)
                    if (symbolParentConditions.ContainsKey(symbol) && symbolParentConditions[symbol].Count > 0)
                    {
                        // Symbol is only active if all parent conditions are met
                        // We need to recursively check if parents are active
                        bool allParentsActive = true;
                        foreach (string parentSymbol in symbolParentConditions[symbol])
                        {
                            // Check if parent symbol is active (recursively)
                            bool parentActive = IsSymbolActive(parentSymbol, symbols, new HashSet<string>());
                            if (!parentActive)
                            {
                                allParentsActive = false;
                                break;
                            }
                        }
                        
                        if (allParentsActive)
                        {
                            symbols.Add(symbol);
                        }
                    }
                    else
                    {
                        // No parent conditions, symbol is active if enabled
                        symbols.Add(symbol);
                    }
                }
            }
            
            return symbols;
        }
        
        /// <summary>
        /// Recursively checks if a symbol is active (including parent conditions)
        /// </summary>
        private bool IsSymbolActive(string symbol, HashSet<string> currentActiveSymbols, HashSet<string> visited)
        {
            // Prevent infinite recursion
            if (visited.Contains(symbol))
                return false;
            visited.Add(symbol);
            
            // Check if symbol is in current active symbols (base case - already processed)
            if (currentActiveSymbols.Contains(symbol))
                return true;
            
            // Check if symbol is enabled in customSymbolStates
            if (!customSymbolStates.ContainsKey(symbol) || !customSymbolStates[symbol])
                return false;
            
            // Check parent conditions recursively
            if (symbolParentConditions.ContainsKey(symbol) && symbolParentConditions[symbol].Count > 0)
            {
                foreach (string parentSymbol in symbolParentConditions[symbol])
                {
                    if (!IsSymbolActive(parentSymbol, currentActiveSymbols, visited))
                        return false;
                }
            }
            
            return true;
        }
        
        // Track which symbols are defined inside which #ifdef blocks
        private Dictionary<string, List<string>> symbolParentConditions = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        
        /// <summary>
        /// Loads available symbols from gsc.conf and scans project files for symbols in #ifdef blocks
        /// Also reads and sets the game mode from gsc.conf
        /// </summary>
        private void LoadSymbolsFromGscConf()
        {
            customSymbolStates.Clear();
            availableCustomSymbols.Clear();
            symbolParentConditions.Clear();
            
            if (string.IsNullOrEmpty(projectPath))
                return;
                
            // First, load symbols from gsc.conf
            string gscConfPath = Path.Combine(projectPath, "gsc.conf");
            if (File.Exists(gscConfPath))
            {
                try
                {
                    string[] lines = File.ReadAllLines(gscConfPath);
                    foreach (string line in lines)
                    {
                        if (line.Trim().StartsWith("symbols=", StringComparison.OrdinalIgnoreCase))
                        {
                            string symbolsValue = line.Substring("symbols=".Length).Trim();
                            foreach (string symbol in symbolsValue.Split(','))
                            {
                                string trimmedSymbol = symbol.Trim();
                                if (!string.IsNullOrEmpty(trimmedSymbol))
                                {
                                    string upperSymbol = trimmedSymbol.ToUpper();
                                    
                                    // Check for game mode (MP, ZM, SP) and set currentGameModeStr
                                    if (upperSymbol == "MP" || upperSymbol == "ZM" || upperSymbol == "SP")
                                    {
                                        currentGameModeStr = upperSymbol;
                                    }
                                    // Check for game symbol (BO3, BO4) and set currentGame
                                    else if (upperSymbol == "BO3")
                                    {
                                        currentGame = TreyarchCompiler.Enums.Games.T7;
                                    }
                                    else if (upperSymbol == "BO4")
                                    {
                                        currentGame = TreyarchCompiler.Enums.Games.T8;
                                    }
                                    // Track custom symbols (not game mode, game, or serious)
                                    else if (upperSymbol != "SERIOUS")
                                    {
                                        if (!availableCustomSymbols.Contains(upperSymbol))
                                        {
                                            availableCustomSymbols.Add(upperSymbol);
                                        }
                                        // Symbols in gsc.conf are enabled by default
                                        customSymbolStates[upperSymbol] = true;
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
                catch { }
            }
            
            // Scan project files for symbols in #ifdef blocks
            ScanProjectForSymbols();
            
            // Update the symbols menu
            UpdateSymbolsMenu();
            
            // Update the mode menu to reflect the mode from gsc.conf
            UpdateModeMenu();
        }
        
        /// <summary>
        /// Updates the mode menu checkboxes to reflect the current game mode
        /// </summary>
        private void UpdateModeMenu()
        {
            if (campaignModeItem == null || multiplayerModeItem == null || zombiesModeItem == null)
                return;
            
            // Uncheck all first
            campaignModeItem.Checked = false;
            multiplayerModeItem.Checked = false;
            zombiesModeItem.Checked = false;
            
            // Check the appropriate mode based on currentGameModeStr
            switch (currentGameModeStr.ToUpper())
            {
                case "SP":
                    campaignModeItem.Checked = true;
                    break;
                case "MP":
                    multiplayerModeItem.Checked = true;
                    break;
                case "ZM":
                default:
                    zombiesModeItem.Checked = true;
                    break;
            }
        }
        
        /// <summary>
        /// Scans all project files for symbols used in #ifdef/#ifndef blocks
        /// </summary>
        private void ScanProjectForSymbols()
        {
            if (string.IsNullOrEmpty(projectPath) || !Directory.Exists(projectPath))
                return;
            
            try
            {
                // Regex patterns for scanning project files
                Regex ifdefPattern = new Regex(@"#if(?:def|ndef)\s+(\w+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                Regex constantDefinePattern = new Regex(@"^\s*#define\s+(\w+)\s*=", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                
                HashSet<string> constantDefines = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                
                // Scan all .gsc and .csc files in the project
                foreach (string gscFile in Directory.GetFiles(projectPath, "*.*", SearchOption.AllDirectories)
                    .Where(f => f.EndsWith(".gsc", StringComparison.OrdinalIgnoreCase) || 
                                f.EndsWith(".csc", StringComparison.OrdinalIgnoreCase)))
                {
                    try
                    {
                        string content = File.ReadAllText(gscFile);
                        
                        // First, identify constant definitions to exclude them
                        foreach (Match match in constantDefinePattern.Matches(content))
                        {
                            if (match.Groups.Count > 1)
                            {
                                string constName = match.Groups[1].Value.Trim();
                                if (!string.IsNullOrEmpty(constName))
                                {
                                    constantDefines.Add(constName);
                                }
                            }
                        }
                        
                        // Find all symbols used in #ifdef/#ifndef blocks and track their parent conditions
                        string[] lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                        Stack<KeyValuePair<string, bool>> ifdefStack = new Stack<KeyValuePair<string, bool>>(); // Track nested #ifdef conditions (symbol, isIfndef)
                        
                        for (int i = 0; i < lines.Length; i++)
                        {
                            string line = lines[i];
                            
                            // Check for #ifdef or #ifndef
                            Match ifdefMatch = ifdefPattern.Match(line);
                            if (ifdefMatch.Success && ifdefMatch.Groups.Count > 1)
                            {
                                string symbol = ifdefMatch.Groups[1].Value.Trim().ToUpper();
                                bool isIfndef = line.Trim().StartsWith("#ifndef", StringComparison.OrdinalIgnoreCase);
                                
                                if (!string.IsNullOrEmpty(symbol))
                                {
                                    // Exclude game mode symbols and constant definitions
                                    if (symbol != "MP" && symbol != "ZM" && symbol != "SP" && 
                                        symbol != "BO3" && symbol != "BO4" && symbol != "SERIOUS" &&
                                        !constantDefines.Contains(symbol))
                                    {
                                        // Add symbol to available list
                                        if (!availableCustomSymbols.Contains(symbol))
                                        {
                                            availableCustomSymbols.Add(symbol);
                                        }
                                        
                                        // Track parent conditions (nested #ifdef blocks)
                                        // A symbol is only active if all its parent conditions are met
                                        if (!symbolParentConditions.ContainsKey(symbol))
                                        {
                                            symbolParentConditions[symbol] = new List<string>();
                                        }
                                        
                                        // Add current parent conditions to this symbol
                                        // Only add parents that are not already in the list
                                        foreach (var parent in ifdefStack)
                                        {
                                            if (!symbolParentConditions[symbol].Contains(parent.Key))
                                            {
                                                symbolParentConditions[symbol].Add(parent.Key);
                                            }
                                        }
                                        
                                        // Push this symbol onto the stack (it's now a parent for nested blocks)
                                        ifdefStack.Push(new KeyValuePair<string, bool>(symbol, isIfndef));
                                    }
                                }
                            }
                            
                            // Check for #endif to pop from stack
                            if (Regex.IsMatch(line, @"^\s*#endif\b", RegexOptions.IgnoreCase))
                            {
                                if (ifdefStack.Count > 0)
                                {
                                    ifdefStack.Pop();
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Skip files that can't be read
                        continue;
                    }
                }
            }
            catch { }
        }
        
        /// <summary>
        /// Updates the symbols menu to show all available custom symbols
        /// </summary>
        private void UpdateSymbolsMenu()
        {
            // Remove existing symbol menu items (if any)
            if (symbolsMenu != null)
            {
                symbolsMenu.DropDownItems.Clear();
            }
            else
            {
                // Create symbols menu if it doesn't exist
                symbolsMenu = new ToolStripMenuItem("Symbols");
                // Insert after Mode menu
                int modeIndex = mainMenuStrip.Items.IndexOf(modeMenu);
                if (modeIndex >= 0)
                {
                    mainMenuStrip.Items.Insert(modeIndex + 1, symbolsMenu);
                }
                else
                {
                    mainMenuStrip.Items.Add(symbolsMenu);
                }
            }
            
            // Add menu items for each custom symbol
            foreach (string symbol in availableCustomSymbols.OrderBy(s => s))
            {
                ToolStripMenuItem symbolItem = new ToolStripMenuItem(symbol);
                symbolItem.CheckOnClick = true;
                symbolItem.Checked = customSymbolStates.ContainsKey(symbol) && customSymbolStates[symbol];
                symbolItem.Tag = symbol;
                symbolItem.Click += (s, e) => {
                    ToolStripMenuItem item = s as ToolStripMenuItem;
                    if (item != null && item.Tag != null)
                    {
                        string sym = item.Tag.ToString();
                        customSymbolStates[sym] = item.Checked;
                        UpdateConditionalCompilationIndicators();
                    }
                };
                symbolsMenu.DropDownItems.Add(symbolItem);
            }
            
            // If no custom symbols, add a disabled item
            if (availableCustomSymbols.Count == 0)
            {
                ToolStripMenuItem noSymbolsItem = new ToolStripMenuItem("(No custom symbols)");
                noSymbolsItem.Enabled = false;
                symbolsMenu.DropDownItems.Add(noSymbolsItem);
            }
        }

        private class CodeBlock
        {
            public int StartLine { get; set; }
            public int EndLine { get; set; }
            public bool IsActive { get; set; }
            public string Symbol { get; set; }
            public bool IsIfndef { get; set; }
        }

        private List<CodeBlock> ParseConditionalBlocks(string text)
        {
            List<CodeBlock> blocks = new List<CodeBlock>();
            HashSet<string> activeSymbols = GetActiveSymbols();
            
            string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            Stack<CodeBlock> blockStack = new Stack<CodeBlock>();
            
            // Match #ifdef/#ifndef anywhere on the line (not just at start) - needed for one-line blocks
            Regex ifdefRegex = new Regex(@"#ifdef\s+(\w+)", RegexOptions.IgnoreCase);
            Regex ifndefRegex = new Regex(@"#ifndef\s+(\w+)", RegexOptions.IgnoreCase);
            Regex elseRegex = new Regex(@"^\s*#else\b", RegexOptions.IgnoreCase);
            // Match #endif anywhere on the line (not just at start) - needed for one-line blocks
            Regex endifRegex = new Regex(@"#endif\b", RegexOptions.IgnoreCase);
            
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                
                Match ifdefMatch = ifdefRegex.Match(line);
                Match ifndefMatch = ifndefRegex.Match(line);
                Match elseMatch = elseRegex.Match(line);
                Match endifMatch = endifRegex.Match(line);
                
                // Check for one-line blocks (e.g., "#ifndef MP AddSubmenu(...); #endif" or "#ifdef MP code(); #endif")
                // Look for #endif on the same line after #ifdef/#ifndef
                bool isOneLineBlock = false;
                if ((ifdefMatch.Success || ifndefMatch.Success) && endifMatch.Success)
                {
                    // Check if #endif appears after #ifdef/#ifndef on the same line
                    int ifdefPos = ifdefMatch.Success ? ifdefMatch.Index : ifndefMatch.Index;
                    int endifPos = endifMatch.Index;
                    if (endifPos > ifdefPos)
                    {
                        isOneLineBlock = true;
                    }
                }
                
                if (ifdefMatch.Success)
                {
                    string symbol = ifdefMatch.Groups[1].Value.ToUpper();
                    bool isActive = activeSymbols.Contains(symbol);
                    
                    if (isOneLineBlock)
                    {
                        // One-line block: #ifdef ... #endif on same line
                        // Extract the content between #ifdef and #endif
                        int ifdefEnd = ifdefMatch.Index + ifdefMatch.Length;
                        int endifStart = endifMatch.Index;
                        
                        // Create a block for the content between #ifdef and #endif
                        CodeBlock block = new CodeBlock
                        {
                            StartLine = i,
                            EndLine = i,
                            Symbol = symbol,
                            IsIfndef = false,
                            IsActive = isActive
                        };
                        
                        // Only add if inactive (we only need to grey out inactive code)
                        if (!block.IsActive)
                        {
                            blocks.Add(block);
                        }
                        
                        // Skip processing this line further (don't push to stack)
                        continue;
                    }
                    else
                    {
                        CodeBlock block = new CodeBlock
                        {
                            StartLine = i,
                            Symbol = symbol,
                            IsIfndef = false,
                            IsActive = isActive,
                            EndLine = -1 // Will be set when we find #endif
                        };
                        
                        blockStack.Push(block);
                    }
                }
                else if (ifndefMatch.Success)
                {
                    string symbol = ifndefMatch.Groups[1].Value.ToUpper();
                    bool isActive = !activeSymbols.Contains(symbol); // Inverted for #ifndef
                    
                    if (isOneLineBlock)
                    {
                        // One-line block: #ifndef ... #endif on same line
                        CodeBlock block = new CodeBlock
                        {
                            StartLine = i,
                            EndLine = i,
                            Symbol = symbol,
                            IsIfndef = true,
                            IsActive = isActive
                        };
                        
                        // Only add if inactive (we only need to grey out inactive code)
                        if (!block.IsActive)
                        {
                            blocks.Add(block);
                        }
                        
                        // Skip processing this line further (don't push to stack)
                        continue;
                    }
                    else
                    {
                        CodeBlock block = new CodeBlock
                        {
                            StartLine = i,
                            Symbol = symbol,
                            IsIfndef = true,
                            IsActive = isActive,
                            EndLine = -1
                        };
                        
                        blockStack.Push(block);
                    }
                }
                else if (elseMatch.Success && blockStack.Count > 0)
                {
                    // When we hit #else, we need to mark the previous block section
                    // and create a new block for the #else section
                    CodeBlock currentBlock = blockStack.Peek();
                    
                    // Mark the end of the "if" section (before #else)
                    CodeBlock ifBlock = new CodeBlock
                    {
                        StartLine = currentBlock.StartLine + 1,
                        EndLine = i - 1,
                        IsActive = currentBlock.IsActive,
                        Symbol = currentBlock.Symbol,
                        IsIfndef = currentBlock.IsIfndef
                    };
                    if (ifBlock.EndLine >= ifBlock.StartLine)
                        blocks.Add(ifBlock);
                    
                    // The #else section has the opposite active state
                    currentBlock.StartLine = i + 1; // Start after #else
                    currentBlock.IsActive = !currentBlock.IsActive;
                }
                else if (endifMatch.Success && blockStack.Count > 0)
                {
                    CodeBlock block = blockStack.Pop();
                    if (block.EndLine == -1) // Only set if not already set by #else
                    {
                        block.EndLine = i;
                    }
                    else
                    {
                        // This block was split by #else, so the end is the line before #endif
                        block.EndLine = i - 1;
                    }
                    
                    // Only add if there's actual content (not just the directive lines)
                    if (block.EndLine >= block.StartLine)
                    {
                        blocks.Add(block);
                    }
                }
            }
            
            return blocks;
        }

        private Scintilla GetCurrentEditor()
        {
            if (tabControl.SelectedTab == null)
                return null;

            foreach (var kvp in editorTabs)
            {
                if (kvp.Value == tabControl.SelectedTab)
                {
                    return openEditors[kvp.Key];
                }
            }
            return null;
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateSelectedTabItem();
        }

        private void UpdateSelectedTabItem()
        {
            if (!folderOpened)
                return;

            try
            {
                if (tabControl.SelectedTab != null)
                {
                    selectedTabItem = tabControl.SelectedTab.Text;
                    currentFileName = selectedTabItem;
                    // Recheck for unsaved changes when switching tabs
                    CheckForUnsavedChanges();
                }
            }
            catch { }
        }

        #endregion

        #region Save Operations

        private void SaveCurrentFile()
        {
            if (!folderOpened || string.IsNullOrEmpty(currentFileName))
                return;

            Scintilla editor = GetCurrentEditor();
            if (editor == null)
                return;

            // Get file path - currentFileName may be a relative path or simple filename
            string filePath;
            if (currentFileName.Contains(Path.DirectorySeparatorChar) || currentFileName.Contains(Path.AltDirectorySeparatorChar))
            {
                // Relative path - use it directly
                filePath = Path.Combine(projectPath, currentFileName);
            }
            else
            {
                // Simple filename - check if it exists in scripts folder, otherwise search
                filePath = Path.Combine(projectPath, "scripts", currentFileName);
                if (!File.Exists(filePath))
                {
                    // Search for the file to get its actual location
                    string[] foundFiles = Directory.GetFiles(projectPath, currentFileName, SearchOption.AllDirectories);
                    if (foundFiles.Length > 0)
                        filePath = foundFiles[0];
                }
            }
            
            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            
            string contentToSave = editor.Text;
            File.WriteAllText(filePath, contentToSave);
            
            // Update saved content reference - this is what we compare against for hasChanges
            fileContents[currentFileName] = contentToSave;
            
            // Clear undo history after save (so undo doesn't go back past save point)
            editor.EmptyUndoBuffer();
            
            // Recheck for unsaved changes (this will update hasChanges and title)
            CheckForUnsavedChanges();
        }

        private void SaveAllFiles(bool updateTitle = true)
        {
            if (!folderOpened || string.IsNullOrEmpty(projectPath))
            {
                hasChanges = false; // Reset if no folder opened
                return;
            }

            try
            {
                // Use Application.DoEvents() to prevent UI freezing during save
                int savedCount = 0;
                foreach (var kvp in openEditors)
                {
                    if (kvp.Value == null)
                        continue;

                    try
                    {
                        // kvp.Key may be a relative path or simple filename
                        string filePath;
                        if (kvp.Key.Contains(Path.DirectorySeparatorChar) || kvp.Key.Contains(Path.AltDirectorySeparatorChar))
                        {
                            // Relative path - use it directly
                            filePath = Path.Combine(projectPath, kvp.Key);
                        }
                        else
                        {
                            // Simple filename - check scripts folder first, then search
                            filePath = Path.Combine(projectPath, "scripts", kvp.Key);
                            if (!File.Exists(filePath))
                            {
                                string[] foundFiles = Directory.GetFiles(projectPath, kvp.Key, SearchOption.AllDirectories);
                                if (foundFiles.Length > 0)
                                    filePath = foundFiles[0];
                            }
                        }
                        
                        // Ensure directory exists
                        string dir = Path.GetDirectoryName(filePath);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }
                        
                        string contentToSave = kvp.Value.Text;
                        
                        // Process UI events before each save to keep UI responsive
                        Application.DoEvents();
                        
                        // Use async file write to prevent blocking
                        File.WriteAllText(filePath, contentToSave);
                        
                        // Update saved content reference - this is what we compare against for hasChanges
                        fileContents[kvp.Key] = contentToSave;
                        
                        // Clear undo history after save (so undo doesn't go back past save point)
                        kvp.Value.EmptyUndoBuffer();
                        
                        savedCount++;
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue saving other files
                        System.Diagnostics.Debug.WriteLine($"Error saving file {kvp.Key}: {ex.Message}");
                    }
                }

                // Process final UI events
                Application.DoEvents();

                // Update hasChanges flag directly (all files are now saved)
                hasChanges = false;
                
                // Only update title if requested (not during form closing)
                if (updateTitle)
                {
                    CheckForUnsavedChanges();
                }
            }
            catch (Exception ex)
            {
                // Re-throw to be handled by caller
                throw new Exception($"Failed to save files: {ex.Message}", ex);
            }
        }


        #endregion

        #region Compilation

        private void BtnCompile_Click(object sender, EventArgs e)
        {
            ResetButtonState(sender);
            
            if (!folderOpened || string.IsNullOrEmpty(projectPath))
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, "Please open a project folder first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            this.Cursor = Cursors.WaitCursor;
            btnCompile.Cursor = Cursors.WaitCursor;
            
            try
            {
                SaveAllFiles();
                
                // Use MainForm's compile logic - find MainForm instance and use its public method
                MainForm mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
                if (mainForm != null)
                {
                    // Determine output path
                    string outputPath = Path.Combine(projectPath, Path.GetFileName(projectPath) + ".gsc");
                    
                    // Get active symbols from the editor (includes mode and custom symbols)
                    HashSet<string> activeSymbols = GetActiveSymbols();
                    List<string> symbolsList = activeSymbols.ToList();
                    
                    // Use MainForm's public CompileProject method with symbols from editor
                    mainForm.CompileProject(projectPath, outputPath, currentGame, GetGameModeEnum(), symbolsList);
                }
                else
                {
                    // Fallback: Show error message
                    ReaLTaiizor.Controls.PoisonMessageBox.Show(this, 
                        "MainForm not available. Please use the Compile tab in the main window.", 
                        "Error", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, 
                    $"Error during compilation: {ex.Message}\n\nStack trace: {ex.StackTrace}", 
                    "Error", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnCompile.Cursor = Cursors.Default;
                this.Cursor = Cursors.Default;
            }
        }

        private TreyarchCompiler.Enums.Modes GetGameModeEnum()
        {
            switch (currentGameModeStr.ToLower())
            {
                case "zm":
                    return TreyarchCompiler.Enums.Modes.ZM;
                case "mp":
                    return TreyarchCompiler.Enums.Modes.MP;
                case "sp":
                    return TreyarchCompiler.Enums.Modes.SP;
                default:
                    return TreyarchCompiler.Enums.Modes.ZM;
            }
        }


        #endregion

        #region Default Project Creation

        private void CreateDefaultProjectOnStartup()
        {
            try
            {
                string defaultProjectBasePath;
                
                // Check if running from installed location (C:\t7gui)
                string installedPath = @"C:\t7gui";
                bool isInstalled = executingDir.Equals(installedPath, StringComparison.OrdinalIgnoreCase);
                
                if (isInstalled)
                {
                    // When installed: use C:\t7gui\defaultproject (from installer)
                    defaultProjectBasePath = Path.Combine(installedPath, "defaultproject");
                }
                else
                {
                    // When running from build folder (not installed): use __depot\build\defaultproject
                    // executingDir is typically __depot\build\t7gui, so go up one level to build, then to defaultproject
                    string buildDir = Path.GetDirectoryName(executingDir);
                    defaultProjectBasePath = Path.Combine(buildDir, "defaultproject");
                    
                    // Fallback: if __depot\build\defaultproject doesn't exist, try executingDir\defaultproject
                    if (!Directory.Exists(defaultProjectBasePath))
                    {
                        defaultProjectBasePath = Path.Combine(executingDir, "defaultproject");
                    }
                }
                
                // The installer/build puts defaultproject with T7/T8 subfolders
                // Use the appropriate subfolder based on current game selection
                string gameSubfolder = currentGame == TreyarchCompiler.Enums.Games.T7 ? "T7" : "T8";
                string defaultProjectPath = Path.Combine(defaultProjectBasePath, gameSubfolder);
                
                // Check if the defaultproject folder exists (from installer or build)
                if (Directory.Exists(defaultProjectPath))
                {
                    // Use the existing defaultproject from installer or build
                    OpenFolder(defaultProjectPath);
                    folderOpened = true;
                    return;
                }
                
                // Fallback: If defaultproject doesn't exist, create it using the old method
                // This handles cases where running from build folder without installer
                string scriptsPath = Path.Combine(defaultProjectPath, "scripts");
                
                // Create directories
                Directory.CreateDirectory(defaultProjectPath);
                Directory.CreateDirectory(scriptsPath);
                
                // Get default file paths
                string defaultMainPath = Path.Combine(executingDir, "Defaults", "defaultt7project.main");
                string defaultHeadersPath = Path.Combine(executingDir, "Defaults", "defaultt7project.headers");
                string mainGscPath = Path.Combine(scriptsPath, "main.gsc");
                string headersGscPath = Path.Combine(scriptsPath, "headers.gsc");
                
                // Check if default files exist, if not create them with default content
                if (!File.Exists(defaultMainPath))
                {
                    // Create default main.gsc content
                    string defaultMainContent = GetDefaultMainContent();
                    File.WriteAllText(mainGscPath, defaultMainContent);
                }
                else
                {
                    if (!File.Exists(mainGscPath))
                        File.Copy(defaultMainPath, mainGscPath);
                }
                    
                if (!File.Exists(defaultHeadersPath))
                {
                    // Create default headers.gsc content
                    string defaultHeadersContent = GetDefaultHeadersContent();
                    File.WriteAllText(headersGscPath, defaultHeadersContent);
                }
                else
                {
                    if (!File.Exists(headersGscPath))
                        File.Copy(defaultHeadersPath, headersGscPath);
                }

                // Create gsc.conf if it doesn't exist
                string gscConfPath = Path.Combine(defaultProjectPath, "gsc.conf");
                if (!File.Exists(gscConfPath))
                {
                    string gameSymbol = currentGame == TreyarchCompiler.Enums.Games.T7 ? "bo3" : "bo4";
                    string gameModeLower = currentGameModeStr.ToLower();
                    File.WriteAllText(gscConfPath, $"symbols={gameSymbol},serious,{gameModeLower}");
                }

                // Open the folder
                if (Directory.Exists(defaultProjectPath))
                {
                    OpenFolder(defaultProjectPath);
                    folderOpened = true;
                }
                else
                {
                    throw new DirectoryNotFoundException($"Failed to create default project directory: {defaultProjectPath}");
                }
            }
            catch (Exception ex)
            {
                // Use PoisonMessageBox for consistency with Poison UI
                ReaLTaiizor.Controls.PoisonMessageBox.Show(
                    this,
                    $"Default Project Failed to open.\n\nError: {ex.Message}\n\nYou can create a new project using 'New Project' button.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void PortILProject(string input, string output)
        {
            // Port IL project logic (similar to BtnPortIL_Click but for auto-detection)
            // Implementation similar to BtnPortIL_Click
        }

        #endregion

        #region Discord Rich Presence

        private void UpdateDiscordPresence()
        {
            // Discord Rich Presence disabled - not needed for this project
            // This method is kept for compatibility but does nothing
            return;
        }

        #endregion

        #region File Watcher

        private void FileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            // File was changed externally - reload if not currently editing
            if (this.InvokeRequired)
            {
                // Use BeginInvoke instead of Invoke to avoid TimeoutException
                // BeginInvoke is asynchronous and won't block or timeout
                if (!this.IsDisposed && !this.Disposing && this.IsHandleCreated)
                {
                    try
                    {
                        this.BeginInvoke(new Action(() => FileWatcher_Changed(sender, e)));
                    }
                    catch (ObjectDisposedException) { }
                    catch (InvalidOperationException) { }
                    catch (ArgumentException) { }
                }
                return;
            }

            // Get relative path from projectPath to match openEditors keys
            string relativePath = GetRelativePath(projectPath, e.FullPath);
            string filename = Path.GetFileName(e.FullPath);
            
            // Check both relative path and filename (for backward compatibility)
            string keyToUse = openEditors.ContainsKey(relativePath) ? relativePath : 
                             (openEditors.ContainsKey(filename) ? filename : null);
            
            if (keyToUse != null && currentFileName != keyToUse)
            {
                // Reload file from disk (external change)
                Scintilla editor = openEditors[keyToUse];
                if (editor != null)
                {
                    // Use a background task to avoid blocking the UI thread
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        // Wait a bit for the file to be fully written (FileSystemWatcher can fire too early)
                        System.Threading.Thread.Sleep(100);
                        
                        // Retry reading the file with delays to handle file locks
                        string content = null;
                        int retries = 10; // Increased retries
                        int delay = 100; // Start with 100ms delay
                        
                        for (int i = 0; i < retries; i++)
                        {
                            try
                            {
                                // Try to read the file with FileShare.ReadWrite to allow other processes to write
                                using (FileStream fs = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                                {
                                    using (StreamReader reader = new StreamReader(fs))
                                    {
                                        content = reader.ReadToEnd();
                                        break; // Success, exit retry loop
                                    }
                                }
                            }
                            catch (IOException ioEx)
                            {
                                // File is locked or being used by another process
                                if (i < retries - 1)
                                {
                                    // Wait before retrying, with exponential backoff
                                    System.Threading.Thread.Sleep(delay);
                                    delay = Math.Min(delay * 2, 2000); // Cap at 2 seconds
                                }
                                else
                                {
                                    // Last retry failed, log and give up
                                    System.Diagnostics.Debug.WriteLine($"Failed to reload file {e.FullPath} after {retries} attempts: {ioEx.Message}");
                                    return; // Don't update editor if we can't read the file
                                }
                            }
                            catch (UnauthorizedAccessException)
                            {
                                // File access denied, don't retry
                                System.Diagnostics.Debug.WriteLine($"Access denied to file {e.FullPath}");
                                return;
                            }
                        }
                        
                        if (content != null)
                        {
                            // Update UI on the main thread
                            if (this.InvokeRequired)
                            {
                                if (!this.IsDisposed && !this.Disposing && this.IsHandleCreated)
                                {
                                    try
                                    {
                                        this.BeginInvoke(new Action(() =>
                                        {
                                            if (!this.IsDisposed && !this.Disposing && openEditors.ContainsKey(keyToUse))
                                            {
                                                isLoadingFiles = true;
                                                try
                                                {
                                                    editor.Text = content;
                                                    fileContents[keyToUse] = content;
                                                    // Clear undo history after external reload
                                                    editor.EmptyUndoBuffer();
                                                    // Don't set hasChanges = false here - external changes don't affect our unsaved state
                                                }
                                                finally
                                                {
                                                    isLoadingFiles = false;
                                                }
                                            }
                                        }));
                                    }
                                    catch (ObjectDisposedException) { }
                                    catch (InvalidOperationException) { }
                                    catch (ArgumentException) { }
                                }
                            }
                            else
                            {
                                isLoadingFiles = true;
                                try
                                {
                                    editor.Text = content;
                                    fileContents[keyToUse] = content;
                                    // Clear undo history after external reload
                                    editor.EmptyUndoBuffer();
                                    // Don't set hasChanges = false here - external changes don't affect our unsaved state
                                }
                                finally
                                {
                                    isLoadingFiles = false;
                                }
                            }
                        }
                    });
                }
            }
        }

        private void FileWatcher_Created(object sender, FileSystemEventArgs e)
        {
            if (this.InvokeRequired)
            {
                // Use BeginInvoke instead of Invoke to avoid TimeoutException
                if (!this.IsDisposed && !this.Disposing && this.IsHandleCreated)
                {
                    try
                    {
                        this.BeginInvoke(new Action(() => FileWatcher_Created(sender, e)));
                    }
                    catch (ObjectDisposedException) { }
                    catch (InvalidOperationException) { }
                    catch (ArgumentException) { }
                }
                return;
            }

            RefreshFileList(false);
        }

        private void FileWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            if (this.InvokeRequired)
            {
                // Use BeginInvoke instead of Invoke to avoid TimeoutException
                if (!this.IsDisposed && !this.Disposing && this.IsHandleCreated)
                {
                    try
                    {
                        this.BeginInvoke(new Action(() => FileWatcher_Deleted(sender, e)));
                    }
                    catch (ObjectDisposedException) { }
                    catch (InvalidOperationException) { }
                    catch (ArgumentException) { }
                }
                return;
            }

            // Get relative path from projectPath to match editorTabs keys
            string relativePath = GetRelativePath(projectPath, e.FullPath);
            string filename = Path.GetFileName(e.FullPath);
            
            // Check both relative path and filename (for backward compatibility)
            string keyToUse = editorTabs.ContainsKey(relativePath) ? relativePath : 
                             (editorTabs.ContainsKey(filename) ? filename : null);
            
            if (keyToUse != null)
            {
                tabControl.TabPages.Remove(editorTabs[keyToUse]);
                editorTabs.Remove(keyToUse);
                openEditors.Remove(keyToUse);
                fileContents.Remove(keyToUse);
            }
        }

        private void FileWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            if (this.InvokeRequired)
            {
                // Use BeginInvoke instead of Invoke to avoid TimeoutException
                if (!this.IsDisposed && !this.Disposing && this.IsHandleCreated)
                {
                    try
                    {
                        this.BeginInvoke(new Action(() => FileWatcher_Renamed(sender, e)));
                    }
                    catch (ObjectDisposedException) { }
                    catch (InvalidOperationException) { }
                    catch (ArgumentException) { }
                }
                return;
            }

            RefreshFileList(false);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the relative path from basePath to targetPath (compatible with .NET Framework 4.8)
        /// </summary>
        private string GetRelativePath(string basePath, string targetPath)
        {
            if (string.IsNullOrEmpty(basePath) || string.IsNullOrEmpty(targetPath))
                return targetPath;

            Uri baseUri = new Uri(basePath + Path.DirectorySeparatorChar);
            Uri targetUri = new Uri(targetPath);
            Uri relativeUri = baseUri.MakeRelativeUri(targetUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', Path.DirectorySeparatorChar);
            
            return relativePath;
        }

        private void CheckForUnsavedChanges()
        {
            bool anyChanges = false;
            
            // Check all open editors against their saved content
            foreach (var kvp in openEditors)
            {
                if (kvp.Value != null)
                {
                    try
                    {
                        string currentText = kvp.Value.Text ?? string.Empty;
                        
                        if (fileContents.ContainsKey(kvp.Key))
                        {
                            // Compare against saved content
                            string savedText = fileContents[kvp.Key] ?? string.Empty;
                            if (currentText != savedText)
                            {
                                anyChanges = true;
                                break;
                            }
                        }
                        else
                        {
                            // File was opened but never saved - if it has content, consider it changed
                            if (!string.IsNullOrEmpty(currentText))
                            {
                                anyChanges = true;
                                break;
                            }
                        }
                    }
                    catch
                    {
                        // If we can't read the text, assume no changes to avoid errors
                    }
                }
            }
            
            // Always update hasChanges flag
            bool previousState = hasChanges;
            hasChanges = anyChanges;
            
            // Update title if state changed or if we're on UI thread (for immediate feedback)
            if (previousState != anyChanges || !this.InvokeRequired)
            {
                if (this.InvokeRequired)
                {
                    try
                    {
                        if (!this.IsDisposed && !this.Disposing && this.IsHandleCreated)
                        {
                            // Use BeginInvoke instead of Invoke to avoid TimeoutException
                            if (!this.IsDisposed && !this.Disposing && this.IsHandleCreated)
                            {
                                try
                                {
                                    this.BeginInvoke(new Action(() => UpdateTitle()));
                                }
                                catch (ObjectDisposedException) { }
                                catch (InvalidOperationException) { }
                                catch (ArgumentException) { }
                            }
                        }
                    }
                    catch (ObjectDisposedException) { }
                    catch (InvalidOperationException) { }
                    catch (ArgumentException) { }
                }
                else
                {
                    UpdateTitle();
                }
            }
        }

        private void UpdateTitle()
        {
            string title = "Code Editor";
            if (folderOpened && !string.IsNullOrEmpty(projectPath))
            {
                title += $" - {Path.GetFileName(projectPath)}";
            }
            if (hasChanges)
            {
                title += " *";
            }
            this.Text = title;
        }

        private string GetDefaultMainContent()
        {
            string templatePath = Path.Combine(executingDir, "Defaults");
            
            if (currentGame == TreyarchCompiler.Enums.Games.T8)
            {
                string bo4MainPath = Path.Combine(templatePath, "defaultt8project.main");
                if (File.Exists(bo4MainPath))
                    return File.ReadAllText(bo4MainPath);
                
                return @"init()
{
}

on_player_connect()
{
}

on_player_spawned()
{
	self endon(""spawned_player"");

    while(true)
    {
        self enableInvulnerability();
        self iPrintLnBold(""Injected! Compiler by serious"");
        wait 1;
    }
}";
            }
            else
            {
                string bo3MainPath = Path.Combine(templatePath, "defaultt7project.main");
                if (File.Exists(bo3MainPath))
                    return File.ReadAllText(bo3MainPath);
                
                return @"// Default Project For Black Ops 3.
// If you would like to generate one for Black Ops 4, select Black Ops 4 in the games tab.
// Then create a New Project using File->Create New Project (Ctrl+Shift+N)

#include scripts\codescripts\struct;
#include scripts\shared\callbacks_shared;
#include scripts\shared\clientfield_shared;
#include scripts\shared\math_shared;
#include scripts\shared\system_shared;
#include scripts\shared\util_shared;
#include scripts\shared\hud_util_shared;
#include scripts\shared\hud_message_shared;
#include scripts\shared\hud_shared;
#include scripts\shared\array_shared;

#namespace serious;

autoexec __init__system__()
{
	system::register(""serious"", ::__init__, undefined, undefined);
}

__init__()
{
	callback::on_start_gametype(::init);
	callback::on_connect(::on_player_connect);
	callback::on_spawned(::on_player_spawned);
}";
            }
        }

        private string GetDefaultHeadersContent()
        {
            string templatePath = Path.Combine(executingDir, "Defaults");
            
            if (currentGame == TreyarchCompiler.Enums.Games.T8)
            {
                string bo4HeadersPath = Path.Combine(templatePath, "defaultt8project.headers");
                if (File.Exists(bo4HeadersPath))
                    return File.ReadAllText(bo4HeadersPath);
            }
            else
            {
                string bo3HeadersPath = Path.Combine(templatePath, "defaultt7project.headers");
                if (File.Exists(bo3HeadersPath))
                    return File.ReadAllText(bo3HeadersPath);
            }
            
            return @"// Headers file
// Add your function declarations and includes here";
        }

        #endregion

        #region Additional UI Features from T7-Compiler-UI-main

        private void UpdateCompilerMenu_Click(object sender, EventArgs e)
        {
            var result = ReaLTaiizor.Controls.PoisonMessageBox.Show(
                this,
                "This will delete the current compiler and reinstall it. Continue?",
                "Update Compiler",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            
            if (result == DialogResult.Yes)
            {
                try
                {
                    if (Directory.Exists("c:\\t7compiler"))
                        Directory.Delete("c:\\t7compiler", true);
                    
                    CompilerActions.InstallCompiler(@"https://gsc.dev/t7c_package");
                }
                catch (Exception ex)
                {
                    ReaLTaiizor.Controls.PoisonMessageBox.Show(this, $"Error updating compiler: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ForceHostMenu_Click(object sender, EventArgs e)
        {
            try
            {
                BlackOps3.ApplyHostDvars();
                var result = ReaLTaiizor.Controls.PoisonMessageBox.Show(
                    this,
                    "Host Dvars Set, click OK when you are ready to start",
                    "Info",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Information);
                
                if (result == DialogResult.OK)
                {
                    BlackOps3.ApplyHostDvars();
                    BlackOps3.Cbuf_AddText("lobbylaunchgame");
                }
            }
            catch (Exception ex)
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, $"Error: {ex.Message}\n\nMake sure Black Ops 3 is running.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearHostDvarsMenu_Click(object sender, EventArgs e)
        {
            try
            {
                BlackOps3.ClearHostDVARS();
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, "Host Dvars cleared.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, $"Error: {ex.Message}\n\nMake sure Black Ops 3 is running.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InjectPrecompiledScript(TreyarchCompiler.Enums.Games game)
        {
            string gameName = game == TreyarchCompiler.Enums.Games.T7 ? "Black Ops 3" : "Black Ops 4";
            
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = $"{gameName} - Precompiled Script";
                dialog.Filter = "Compiled Scripts (*.gsc;*.gscc)|*.gsc;*.gscc|All Files (*.*)|*.*";
                dialog.FilterIndex = 1;
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    if (!dialog.FileName.EndsWith(".gsc") && !dialog.FileName.EndsWith(".gscc"))
                    {
                        ReaLTaiizor.Controls.PoisonMessageBox.Show(this, "Select a compiled script to inject.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        InjectPrecompiledScript(game);
                        return;
                    }
                    
                    // Use MainForm's inject logic - find MainForm instance and use its public method
                    MainForm mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
                    if (mainForm != null)
                    {
                        // Determine replace path based on game (default paths from original Compiler UI)
                        string replacePath = null;
                        if (game == TreyarchCompiler.Enums.Games.T7)
                            replacePath = "scripts\\shared\\duplicaterender_mgr.gsc";
                        else
                            replacePath = "scripts\\zm_common\\load.gsc";
                        
                        // Use MainForm's public InjectPrecompiledScript method
                        mainForm.InjectPrecompiledScript(dialog.FileName, game, replacePath);
                    }
                    else
                    {
                        // Fallback: Show error message
                        ReaLTaiizor.Controls.PoisonMessageBox.Show(this, 
                            "MainForm not available. Please use the Inject tab in the main window.", 
                            "Error", 
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void KillGame(string processName)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(processName);
                if (processes.Length == 0)
                {
                    ReaLTaiizor.Controls.PoisonMessageBox.Show(this, $"{processName} is not running.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                var result = ReaLTaiizor.Controls.PoisonMessageBox.Show(
                    this,
                    $"Are you sure you want to kill {processName}?",
                    "Confirm",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    foreach (var process in processes)
                    {
                        try
                        {
                            process.Kill();
                        }
                        catch { }
                    }
                    ReaLTaiizor.Controls.PoisonMessageBox.Show(this, $"{processName} killed successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, $"Error killing process: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Cleanup

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Always check for unsaved changes by comparing all editors against saved content
            // This ensures we catch any changes even if the TextChanged handler didn't fire
            CheckForUnsavedChanges();
            
            if (hasChanges)
            {
                var result = ReaLTaiizor.Controls.PoisonMessageBox.Show(
                    this,
                    "You have unsaved changes. Save before closing?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    // Cancel the close event temporarily so we can save first
                    e.Cancel = true;
                    
                    try
                    {
                        // Ensure message box is fully dismissed and form regains focus
                        this.Activate();
                        this.BringToFront();
                        this.Focus();
                        
                        // Process all pending messages to ensure message box is fully closed
                        Application.DoEvents();
                        System.Threading.Thread.Sleep(50); // Brief pause to ensure message box is dismissed
                        Application.DoEvents();
                        
                        this.Cursor = Cursors.WaitCursor;
                        this.Update();
                        Application.DoEvents();
                        
                        // Save files (don't update title - form is closing)
                        SaveAllFiles(updateTitle: false);
                        
                        // Process events after save
                        Application.DoEvents();
                        
                        this.Cursor = Cursors.Default;
                        this.Update();
                        Application.DoEvents();
                        
                        // Close the form synchronously on the UI thread
                        // Use a timer to close after a brief delay to ensure all UI updates complete
                        var closeTimer = new System.Windows.Forms.Timer { Interval = 50 };
                        closeTimer.Tick += (s, args) => {
                            closeTimer.Stop();
                            closeTimer.Dispose();
                            this.Close();
                        };
                        closeTimer.Start();
                    }
                    catch (Exception ex)
                    {
                        this.Cursor = Cursors.Default;
                        e.Cancel = true; // Keep form open on error
                        
                        // If save fails, ask user if they still want to close
                        var errorResult = ReaLTaiizor.Controls.PoisonMessageBox.Show(
                            this,
                            $"Failed to save files: {ex.Message}\n\nDo you still want to close?",
                            "Save Error",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);
                        
                        if (errorResult == DialogResult.Yes)
                        {
                            // User wants to close anyway - use timer to close
                            var closeTimer = new System.Windows.Forms.Timer { Interval = 50 };
                            closeTimer.Tick += (s, args) => {
                                closeTimer.Stop();
                                closeTimer.Dispose();
                                this.Close();
                            };
                            closeTimer.Start();
                        }
                        // If No, form stays open (e.Cancel = true)
                    }
                    
                    return; // Exit early since we're handling close programmatically
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
                else if (result == DialogResult.No)
                {
                    // User chose not to save, allow form to close
                    // hasChanges will remain true but we're closing anyway
                }
            }

            // Cleanup timers
            // Auto-save timer is disabled (manual save only)
            // if (autoSaveTimer != null)
            // {
            //     autoSaveTimer.Stop();
            //     autoSaveTimer.Dispose();
            // }

            if (themeCheckTimer != null)
            {
                themeCheckTimer.Stop();
                themeCheckTimer.Dispose();
                themeCheckTimer = null;
            }

            if (tabUpdateTimer != null)
            {
                tabUpdateTimer.Stop();
                tabUpdateTimer.Dispose();
            }

            // Discord Rich Presence cleanup removed - feature is disabled

            // Cleanup file watcher
            if (fileWatcher != null)
            {
                fileWatcher.EnableRaisingEvents = false;
                fileWatcher.Dispose();
            }

            base.OnFormClosing(e);
        }

        #endregion
    }
}
