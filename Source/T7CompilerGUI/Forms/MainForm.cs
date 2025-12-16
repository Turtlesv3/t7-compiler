using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using TreyarchCompiler;
using TreyarchCompiler.Enums;
using TreyarchCompiler.Utilities;
using T7CompilerLib;
using System.Reflection;
using System.Diagnostics;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Enum.Poison;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Extension.Poison;
using ReaLTaiizor.Drawing.Poison;
using ReaLTaiizor.Interface.Poison;
using ReaLTaiizorExt = ReaLTaiizor.Extension.Poison;
using T7CompilerGUI.Forms.Dialogs;
using T7CompilerGUI.Controls;
using T7CompilerGUI.Utils;
using T7CompilerGUI.Models;
using T7CompilerGUI.Services;
using Vanara.PInvoke;
using static Vanara.PInvoke.Shell32;

#pragma warning disable IDE1006 // Naming rule violation - Windows Forms event handlers use controlName_EventName convention

namespace T7CompilerGUI.Forms
{
    // Hot reload mode enum (matching debug compiler)
    public enum HotMode
    {
        None,
        Csc,
        Gsc
    }

    public partial class MainForm : PoisonForm
    {
        // Windows API declarations removed - using ModernFolderDialog helper instead
        
        #region Private Fields
        
        private bool lastT7Status = false;
        private bool lastT8Status = false;
        private bool isUpdatingGameStatus = false;
        private System.Windows.Forms.Timer gameStatusTimer;
        
        private static readonly string[] T7ProcessNames = { "blackops3", "BlackOps3" };
        private static readonly string[] T8ProcessNames = { "blackops4", "BlackOps4" };
        
        private int InjectedBuffSize;
        private PointerEx llpModifiedSPTStruct = 0;
        private PointerEx llpOriginalBuffer;
        private int OriginalSourceChecksum;
        private int OriginalPID = 0;
        private T7SPT InjectedScript;
        private TreyarchCompiler.Enums.Games LastGameInjected = TreyarchCompiler.Enums.Games.T7;
        private bool isParseTreeReset = false;
        private HotMode currentHotMode = HotMode.None;
        
        private bool isCompiling = false;
        private bool isInjecting = false;
        
        private int currentPlatformIndex = 0;
        private int currentGameIndex = 0;
        private int currentInjectGameIndex = 0;
        private List<string> compileSymbolsFromEditor = null; // Symbols from CodeEditorForm
        private bool isLoadingSettings = false;
        private bool isUpdatingProjectFromSelection = false; // Flag to prevent recursive updates during project selection
        private Forms.CodeEditorForm codeEditorForm = null; // Reference to open CodeEditorForm
        
        // Initialize buildFolder to a safe default location (will be properly initialized in InitializeBuildFolder)
        private string buildFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "T7Compiler", "build");
        
        private const int MAX_RECENT_PROJECTS = 10;
        private const int MAX_RECENT_FILES = 10;
        
        private Dictionary<string, Dialogs.KeybindDialog.KeybindInfo> keybinds = null;
        
        private bool autoSaveSettings = true;
        private string defaultOutputPath = "";
        private bool restoreWindowState = true;
        
        #endregion
        
        // Helper methods moved to MainForm.Helpers.cs
        
        #region Paint Override for Rainbow Support
        
        protected override void OnPaint(PaintEventArgs e)
        {
            // Call base paint first to draw the standard form
            base.OnPaint(e);
            
            // Override colors with rainbow if active
            if (IsRainbowStyleActive() && poisonStyleManager != null)
            {
                using (SolidBrush rainbowBrush = new SolidBrush(currentRainbowColor))
                {
                    // Draw rainbow top border (replaces the style-based border)
                    Rectangle topRect = new Rectangle(0, 0, Width, 5);
                    e.Graphics.FillRectangle(rainbowBrush, topRect);
                }
                
                // Draw rainbow side borders if PoisonBorderStyle is set
                if (this.PoisonBorderStyle != ReaLTaiizor.Enum.Poison.FormBorderStyle.None)
                {
                    using (Pen rainbowPen = new Pen(currentRainbowColor))
                    {
                        // Left border
                        e.Graphics.DrawLine(rainbowPen, 0, 5, 0, Height - 1);
                        // Right border
                        e.Graphics.DrawLine(rainbowPen, Width - 1, 5, Width - 1, Height - 1);
                        // Bottom border
                        e.Graphics.DrawLine(rainbowPen, 0, Height - 1, Width - 1, Height - 1);
                    }
                }
            }
        }
        
        private void PanelLog_Paint(object sender, PaintEventArgs e)
        {
            if (IsRainbowStyleActive() && panelLog != null)
            {
                using (Pen rainbowPen = new Pen(currentRainbowColor, 2))
                {
                    // Draw rainbow border around the log panel
                    Rectangle borderRect = panelLog.ClientRectangle;
                    borderRect.Width -= 1;
                    borderRect.Height -= 1;
                    e.Graphics.DrawRectangle(rainbowPen, borderRect);
                    
                    // Draw rainbow border around the log textbox
                    if (txtLog != null && txtLog.Visible && txtLog.Parent == panelLog)
                    {
                        // Get txtLog position relative to panel
                        Point txtLogLocation = panelLog.PointToClient(txtLog.Parent.PointToScreen(txtLog.Location));
                        Rectangle txtLogRect = new Rectangle(txtLogLocation.X, txtLogLocation.Y, txtLog.Width, txtLog.Height);
                        e.Graphics.DrawRectangle(rainbowPen, txtLogRect);
                    }
                }
            }
        }
        
        #endregion
        
        #region Constructor and Initialization
        
        public MainForm()
        {
            InitializeComponent();
            
            // Load form icon - try multiple locations
            T7CompilerGUI.Helpers.FormIconHelper.LoadFormIcon(this);
            
            // PoisonPanel already handles double buffering internally via its constructor
            // No need to manually set ControlStyles - it's a protected method anyway
            
            // PoisonForm already sets ControlStyles in its constructor:
            // - AllPaintingInWmPaint
            // - OptimizedDoubleBuffer (not DoubleBuffer)
            // - ResizeRedraw
            // - UserPaint
            // We don't need to override them - PoisonForm handles it correctly
            // Form properties (PoisonBorderStyle, ShadowType) are now set in Designer
            
            // Ensure form is ready to paint (PoisonForm handles rendering automatically)
            // The form will paint correctly when shown - no need to force it here
            
            // Enable form dragging from anywhere (except interactive controls)
            this.MouseDown += Form_MouseDown;
            
            // Enable dragging from all labels
            EnableLabelDragging();
            
            InitializeBuildFolder();
            
            SetupDropdownButtons();
            
            LoadAllSettings();
            
            // Set up rainbow color callback for PoisonPaint (will be used when Rainbow style is selected)
            PoisonPaint.GetRainbowColor = () => currentRainbowColor;
            
            // Initialize recent projects menus after settings are loaded
            UpdateRecentProjectsMenu();
            UpdateRecentFilesMenu();
            
            // Set text alignment to Right so the end of the path (project folder name) is visible when truncated
            if (txtProjectFolder != null)
            {
                txtProjectFolder.TextAlign = HorizontalAlignment.Right;
            }
            
            // Highlight the current project if one is loaded
            if (!string.IsNullOrWhiteSpace(txtProjectFolder?.Text))
            {
                UpdateRecentProjectsSelection(txtProjectFolder.Text);
            }
            
            // Ensure recent projects button is visible and properly set up with ComboBox behavior
            if (btnRecentProjects != null)
            {
                btnRecentProjects.Visible = true;
                SetupDropDownButtonAsComboBox(btnRecentProjects, recentProjectsMenu);
                
                // Add custom handler for recent projects ComboBox selection
                btnRecentProjects.SelectedIndexChanged += BtnRecentProjects_SelectedIndexChanged;
                
                // Adjust txtProjectFolder when btnRecentProjects size or location changes (to prevent overlap)
                btnRecentProjects.SizeChanged += BtnRecentProjects_SizeChanged;
                btnRecentProjects.LocationChanged += BtnRecentProjects_SizeChanged;
            }
            if (btnRecentFiles != null)
            {
                btnRecentFiles.Visible = true;
                SetupDropDownButtonAsComboBox(btnRecentFiles, recentFilesMenu);
            }
            
            // Output file will be loaded from gsc.conf when a project folder is set
            
            SetupKeyboardShortcuts();
            SetupToolTips();
            
            if (llpModifiedSPTStruct != 0 && OriginalPID != 0)
            {
                try
                {
                    Process process = Process.GetProcessById(OriginalPID);
                    if (process != null && !process.HasExited)
                    {
                        // Game is still running - injection state is valid
                        if (txtLog.Text.Length > 0)
                        {
                            AppendLogText($"Loaded previous injection state (PID: {OriginalPID}, Game: {LastGameInjected}). Reset is available.\r\n");
                        }
                    }
                    else
                    {
                        // Process exited - clear state
                        AppendLogText($"Previous injection state found, but game process (PID: {OriginalPID}) is no longer running. State cleared.\r\n");
                        ClearInjectionState();
                    }
                }
                catch (ArgumentException)
                {
                    // Process not found - clear state
                    AppendLogText($"Previous injection state found, but game process (PID: {OriginalPID}) no longer exists. State cleared.\r\n");
                    ClearInjectionState();
                }
                catch
                {
                    // Other error - clear state
                    ClearInjectionState();
                }
            }
            
            // Settings panel handles its own menus and events
            
            // Update reset parse tree button state after loading injection state
            UpdateResetParseTreeButton();
            
            UpdateAboutTab();
            
            this.Load += MainForm_Load;
            
            // Handle tab selection to clear inject file selection when inject tab is selected
            if (tabControl != null)
            {
                tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
            }
            
            // Setup main form context menu (right-click on title bar/form)
            SetupMainFormContextMenu();
            
            // Initialize reset parse tree button state
            UpdateResetParseTreeButton();
            
            // Update UI state
            UpdateUI();
            
            // Initialize Launch BO3 button state based on current game status
            if (btnLaunchBO3 != null)
            {
                bool t7Running = IsGameRunning(TreyarchCompiler.Enums.Games.T7);
                btnLaunchBO3.Enabled = !t7Running;
                if (poisonToolTip != null)
                {
                    if (t7Running)
                    {
                        poisonToolTip.SetToolTip(btnLaunchBO3, "Black Ops 3 is already running");
                    }
                    else
                    {
                        poisonToolTip.SetToolTip(btnLaunchBO3, "Launch Black Ops 3 (requires Steam)");
                    }
                }
            }
            
            // Initialize Kill BO3 button state based on current game status
            if (btnKillBO3 != null)
            {
                bool t7Running = IsGameRunning(TreyarchCompiler.Enums.Games.T7);
                btnKillBO3.Enabled = t7Running;
                if (poisonToolTip != null)
                {
                    if (t7Running)
                    {
                        poisonToolTip.SetToolTip(btnKillBO3, "Kill Black Ops 3 process");
                    }
                    else
                    {
                        poisonToolTip.SetToolTip(btnKillBO3, "Black Ops 3 is not running");
                    }
                }
            }
            
            // Setup game status label to be non-interactive
            SetupGameStatusLabel();
            
            // Handle resize for panelLog height adjustment
            this.SizeChanged += MainForm_SizeChanged;
            this.Shown += MainForm_Shown;
            
            EnableTabContentDragging();
            EnableTabHeaderDragging();
            
            // Enable dragging from text panel controls
            EnablePanelDragging();
            
            // Enable dragging from game status label area
            EnableGameStatusDragging();
            
            // Setup comprehensive click handlers for all controls
            SetupAllControlClickHandlers();
            
            // Setup UI theme - use PoisonFormHelper for standardized initialization
            SetupUITheme();
            
            // PoisonFormHelper.InitializeForm() is called in SetupUITheme() which handles:
            // - StyleManager application to all controls
            // - Button effects setup
            // - Form icon loading (via helper)
            
            // Configure scrollbars: invisible in log panel
            SetupScrollbarSettings();
        }
        
        /// <summary>
        /// Loads the form icon from various possible locations
        /// </summary>
        private void MainForm_Load(object sender, EventArgs e)
        {
            if (poisonStyleManager != null)
            {
                this.BackColor = PoisonPaint.BackColor.Form(poisonStyleManager.Theme);
                
                ChangeTheme(poisonStyleManager.Theme);
                
                // Force refresh of all controls to ensure Poison borders render correctly
                ReaLTaiizorExt.PoisonControlHelper.RefreshAllControls(this);
            }
        }
        
        private void MainForm_Shown(object sender, EventArgs e)
        {
            AdjustPanelLogHeight();
            
            // Adjust project folder width to account for recent projects button
            AdjustProjectFolderWidth();
            
            // Force full refresh to ensure Poison borders and styling render correctly on first show
            if (poisonStyleManager != null)
            {
                // Refresh all controls using helper
                ReaLTaiizorExt.PoisonControlHelper.RefreshAllControls(this);
                
                // Also force form refresh
                this.Invalidate(true);
                this.Update();
            }
            
            System.Windows.Forms.Timer delayTimer = null;
            delayTimer = ReaLTaiizorExt.PoisonFormHelper.CreateTrackedTimer(this, 3000, (s, args) =>
            {
                delayTimer.Stop();
                InitializeGameStatusMonitoring();
            });
            delayTimer.Start();
        }
        
        #endregion
        
        #region Scrollbar Configuration
        
        /// <summary>
        /// Configures scrollbar settings for various panels
        /// </summary>
        private void SetupScrollbarSettings()
        {
            // Make log panel scrollbars invisible (functional but not visible) using helper
            if (panelLog != null)
            {
                ReaLTaiizorExt.PoisonControlHelper.ConfigureScrollbars(panelLog,
                    showVertical: true,
                    showHorizontal: true,
                    verticalInvisible: true,
                    horizontalInvisible: true);
            }
            
            // Configure txtLog to have invisible scrollbars like code editor left panel
            if (txtLog != null)
            {
                // Enable vertical scrolling but hide scrollbars using Windows API
                txtLog.ScrollBars = RichTextBoxScrollBars.Vertical;
                txtLog.HandleCreated += (s, e) => HideScrollbarCorner(txtLog);
                txtLog.Layout += (s, e) =>
                {
                    if (txtLog.IsHandleCreated)
                        HideScrollbarCorner(txtLog);
                };
            }
        }
        
        // Windows API to hide scrollbar corner for txtLog (RichTextBox)
        [DllImport("user32.dll")]
        private static extern int ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);
        
        private const int SB_HORZ = 0;
        private const int SB_VERT = 1;
        
        private void HideScrollbarCorner(Control control)
        {
            if (control == null || !control.IsHandleCreated) return;
            
            try
            {
                // Hide both scrollbars to hide the corner
                ShowScrollBar(control.Handle, SB_HORZ, false);
                ShowScrollBar(control.Handle, SB_VERT, false);
            }
            catch
            {
                // Ignore errors
            }
        }
        
        #endregion
        
        #region Theme and Style Management
        
        /// <summary>
        /// Adjusts panelLog height to prevent overlap with panelLogControls.
        /// </summary>
        private void AdjustPanelLogHeight()
        {
            if (panelLog != null && panelLogControls != null && lblGameStatus != null)
            {
                // Calculate the maximum allowed bottom position for panelLog
                // It should stop above panelLogControls
                int panelLogControlsTop = panelLogControls.Top;
                int panelLogTop = panelLog.Top;
                int maxPanelLogHeight = panelLogControlsTop - panelLogTop - 5; // 5px padding
                
                if (maxPanelLogHeight > 50) // Minimum height of 50 pixels
                {
                    if (panelLog.Height != maxPanelLogHeight)
                    {
                        panelLog.Height = maxPanelLogHeight;
                    }
                }
            }
        }
        
        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            // Only adjust panelLog height - controls handle their own rendering automatically
            AdjustPanelLogHeight();
            
            // Adjust project folder width when form resizes
            AdjustProjectFolderWidth();
        }
        
        /// <summary>
        /// Handles tab selection changes to clear inject file selection when inject tab is selected
        /// </summary>
        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (tabControl != null && tabControl.SelectedTab == tabInject)
                {
                    // Clear selection when inject tab is selected to prevent text highlighting
                    ClearInjectFileSelection();
                }
            }
            catch
            {
                // Silently ignore errors
            }
        }
        
        /// <summary>
        /// Enables form dragging from tab content.
        /// </summary>
        private void EnableTabContentDragging()
        {
            foreach (System.Windows.Forms.TabPage tabPage in tabControl.Controls)
            {
                tabPage.MouseDown += TabPage_MouseDown;
            }
        }
        
        /// <summary>
        /// Enables form dragging from tab headers (tab names)
        /// </summary>
        private void EnableTabHeaderDragging()
        {
            if (tabControl != null)
            {
                tabControl.MouseDown += TabControl_MouseDown;
            }
        }
        
        /// <summary>
        /// Handles mouse down on TabControl - makes tab headers draggable
        /// </summary>
        private void TabControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (sender is System.Windows.Forms.TabControl)
                {
                    // Only drag if clicking in the tab header area (top portion of TabControl)
                    // Tab headers are typically in the first ~30 pixels of the TabControl
                    if (e.Y < 30)
                    {
                        // Use Windows API to simulate title bar drag
                        ReleaseCapture();
                        SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                    }
                }
            }
        }
        
        /// <summary>
        /// Handles mouse down on the form itself - makes form draggable from anywhere (except interactive controls)
        /// </summary>
        private void Form_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && this.Movable)
            {
                // Check if clicking on an interactive control
                Control controlAtPoint = this.GetChildAtPoint(e.Location, GetChildAtPointSkip.Invisible | GetChildAtPointSkip.Disabled);
                
                // Allow dragging if:
                // 1. No control at this point (clicking on form background)
                // 2. Control is a label, panel, or other non-interactive control
                // 3. Control is a tab page (empty space)
                bool canDrag = false;
                
                if (controlAtPoint == null || controlAtPoint == this)
                {
                    canDrag = true;
                }
                else if (controlAtPoint is PoisonLabel || 
                         controlAtPoint is System.Windows.Forms.Label ||
                         controlAtPoint is PoisonPanel)
                {
                    canDrag = true;
                }
                else if (controlAtPoint is System.Windows.Forms.TabPage tabPage)
                {
                    // For TabPage, check if clicking on empty space (not on child controls)
                    Point tabPoint = tabPage.PointToClient(this.PointToScreen(e.Location));
                    Control childAtPoint = tabPage.GetChildAtPoint(tabPoint, GetChildAtPointSkip.Invisible | GetChildAtPointSkip.Disabled);
                    canDrag = (childAtPoint == null || childAtPoint == tabPage);
                }
                
                if (canDrag)
                {
                    // Use Windows API to simulate title bar drag
                    ReleaseCapture();
                    SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            }
        }
        
        /// <summary>
        /// Enables form dragging from text panel controls (like panelLog and panelLogControls)
        /// </summary>
        private void EnablePanelDragging()
        {
            // Add MouseDown handler to panel controls
            if (panelLog != null)
            {
                panelLog.MouseDown += Panel_MouseDown;
            }
            
            // Enable dragging from log controls panel (clear, copy, save, search area)
            if (panelLogControls != null)
            {
                panelLogControls.MouseDown += Panel_MouseDown;
            }
            
            // Also enable dragging from the log text box itself (when clicking on empty space)
            if (txtLog != null)
            {
                txtLog.MouseDown += TextBox_MouseDown;
            }
            
            // Search controls should behave normally - no dragging interference
        }
        
        /// <summary>
        /// Enables form dragging from all label controls (PoisonLabel and regular Label)
        /// </summary>
        private void EnableLabelDragging()
        {
            // Attach MouseDown handler to all existing labels
            AttachLabelHandlersRecursive(this);
            
            // Also attach to labels added dynamically
            this.ControlAdded += (s, e) =>
            {
                if (e.Control is PoisonLabel || e.Control is System.Windows.Forms.Label)
                {
                    e.Control.MouseDown += Label_MouseDown;
                }
                else if (e.Control.HasChildren)
                {
                    AttachLabelHandlersRecursive(e.Control);
                }
            };
        }
        
        /// <summary>
        /// Recursively attaches MouseDown handler to all labels in a container
        /// </summary>
        private void AttachLabelHandlersRecursive(Control parent)
        {
            if (parent == null) return;
            
            foreach (Control control in parent.Controls)
            {
                if (control is PoisonLabel || control is System.Windows.Forms.Label)
                {
                    // Remove existing handler if any, then add our handler
                    control.MouseDown -= Label_MouseDown;
                    control.MouseDown += Label_MouseDown;
                }
                
                // Recursively process child controls
                if (control.HasChildren)
                {
                    AttachLabelHandlersRecursive(control);
                }
            }
        }
        
        /// <summary>
        /// Handles mouse down on Label controls - makes label areas draggable
        /// </summary>
        private void Label_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && this.Movable)
            {
                // Use Windows API to simulate title bar drag
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
        
        /// <summary>
        /// Handles mouse down on Panel controls - makes panel areas draggable
        /// </summary>
        private void Panel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Use Windows API to simulate title bar drag
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
        
        /// <summary>
        /// Handles mouse down on TextBox controls - makes text box areas draggable when clicking on empty space
        /// </summary>
        private void TextBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Only drag if clicking on empty space in the text box (not on text)
                if (sender is System.Windows.Forms.RichTextBox textBox && string.IsNullOrEmpty(textBox.Text))
                {
                    // Use Windows API to simulate title bar drag
                    ReleaseCapture();
                    SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            }
        }
        
        /// <summary>
        /// Handles mouse down on TabPage - makes tab page content draggable
        /// Uses Windows API for smooth dragging
        /// </summary>
        private void TabPage_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Only drag if clicking directly on the TabPage (empty space), not on child controls
                if (sender is System.Windows.Forms.TabPage tabPage)
                {
                    Point clientPoint = tabPage.PointToClient(Control.MousePosition);
                    Control childAtPoint = tabPage.GetChildAtPoint(clientPoint);
                    
                    // If no child control at this point, or the child is the TabPage itself, allow dragging
                    if (childAtPoint == null || childAtPoint == tabPage)
                    {
                        // Use Windows API to simulate title bar drag
                        ReleaseCapture();
                        SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                    }
                }
            }
        }
        
        // Windows API - ReaLTaiizor's Native classes are internal, so we need our own DllImports
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;
        private const int WM_PAINT = 0x000F;
        
        #endregion
        
        #region Main Form Context Menu
        
        /// <summary>
        /// Sets up the right-click context menu for the main form
        /// Matches Form20's pattern - context menu created in Designer, populated here
        /// </summary>
        private void SetupMainFormContextMenu()
        {
            if (mainFormContextMenu == null) return;
            
            mainFormContextMenu.Items.Clear();
            
            // Keyboard Shortcuts menu item
            ToolStripMenuItem keyboardShortcutsItem = new ToolStripMenuItem("Keyboard Shortcuts...");
            keyboardShortcutsItem.Click += (s, e) =>
            {
                using (var dialog = new Dialogs.KeybindDialog(poisonStyleManager, keybinds ?? Dialogs.KeybindDialog.GetDefaultKeybinds(), this))
                {
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        keybinds = dialog.Keybinds;
                        SaveAllSettings();
                        SetupKeyboardShortcuts();
                    }
                }
            };
            mainFormContextMenu.Items.Add(keyboardShortcutsItem);
            
            // Separator
            mainFormContextMenu.Items.Add(new ToolStripSeparator());
            
            // Application Config menu item
            ToolStripMenuItem applicationConfigItem = new ToolStripMenuItem("Application Config...");
            applicationConfigItem.Click += (s, e) =>
            {
                try
                {
                    string configPath = GetConfigPath();
                    string configDir = Path.GetDirectoryName(configPath);
                    string tempPath = Path.GetTempPath();
                    
                    // Get DLL extract path (where Costura extracts embedded DLLs)
                    string appDataLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string appDataRoaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string dllExtractPath = Path.Combine(appDataLocal, "Costura", Assembly.GetExecutingAssembly().GetName().Name);
                    
                    using (var dialog = new Dialogs.ConfigLocationsDialog(poisonStyleManager, configPath, tempPath, dllExtractPath, appDataRoaming, this))
                    {
                        dialog.ShowDialog(this);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening Application Config dialog: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            mainFormContextMenu.Items.Add(applicationConfigItem);
            
            // Advanced Settings menu item
            ToolStripMenuItem advancedSettingsItem = new ToolStripMenuItem("Advanced Settings...");
            advancedSettingsItem.Click += (s, e) =>
            {
                ShowAdvancedSettingsDialog();
            };
            mainFormContextMenu.Items.Add(advancedSettingsItem);
            
            // Enhance menu items with theme styling
            EnhanceMenuItems(mainFormContextMenu);
            
            mainFormContextMenu.Opening += (s, e) =>
            {
                if (poisonStyleManager != null && mainFormContextMenu.Renderer is StyleBasedMenuRenderer renderer)
                {
                    renderer.UpdateStyleManager(poisonStyleManager);
                }
                mainFormContextMenu.Invalidate();
            };
            
            // Assign context menu to form - show on right-click anywhere on the form
            this.ContextMenuStrip = mainFormContextMenu;
        }
        
        /// <summary>
        /// Shows the Advanced Settings dialog with PropertyGrid
        /// </summary>
        private void ShowAdvancedSettingsDialog()
        {
            using (var dialog = new Dialogs.AdvancedSettingsDialog(
                this,
                this, // Selected object (MainForm)
                poisonStyleManager,
                poisonStyleExtender,
                IsRainbowStyleActive,
                () => currentRainbowColor,
                null,
                ApplyPoisonThemeToPropertyGrid,
                SyncAdvancedDialogControls))
            {
                // Use helper to apply StyleManager - handles Theme, Style, and UseStyleColors automatically
                if (poisonStyleManager != null)
                {
                    ReaLTaiizor.Extension.Poison.PoisonControlHelper.ApplyStyleManager(dialog, poisonStyleManager);
                }
                
                dialog.ShowDialog(this);
            }
        }
        
        /// <summary>
        /// Applies Poison theme colors to PropertyGrid to match StyleManager
        /// </summary>
        private void ApplyPoisonThemeToPropertyGrid(System.Windows.Forms.PropertyGrid propertyGrid, PoisonStyleManager styleManager)
        {
            if (propertyGrid == null || styleManager == null) return;
            
            try
            {
                ThemeStyle theme = styleManager.Theme;
                ColorStyle style = styleManager.Style;
                
                // Get theme colors
                Color backColor = PoisonPaint.BackColor.Form(theme);
                // Use PoisonPaint for foreground color to match theme
                Color foreColor = PoisonPaint.ForeColor.Label.Normal(theme);
                Color categoryForeColor = PoisonPaint.GetStyleColor(style);
                // Use PoisonPaint for line color to match theme
                Color lineColor = PoisonPaint.BorderColor.Button.Normal(theme);
                Color selectedBackColor = PoisonPaint.GetStyleColor(style);
                // Use theme-aware foreground for selected items
                Color selectedForeColor = PoisonPaint.ForeColor.Label.Normal(theme);
                
                // Apply colors to PropertyGrid
                propertyGrid.BackColor = backColor;
                propertyGrid.ForeColor = foreColor;
                propertyGrid.LineColor = lineColor;
                propertyGrid.ViewBackColor = backColor;
                propertyGrid.ViewForeColor = foreColor;
                propertyGrid.HelpBackColor = backColor;
                propertyGrid.HelpForeColor = foreColor;
                propertyGrid.CategoryForeColor = categoryForeColor;
                propertyGrid.CategorySplitterColor = lineColor;
                propertyGrid.CommandsBackColor = backColor;
                propertyGrid.CommandsForeColor = foreColor;
                propertyGrid.CommandsBorderColor = lineColor;
                
                // Selected item colors
                propertyGrid.SelectedItemWithFocusBackColor = selectedBackColor;
                propertyGrid.SelectedItemWithFocusForeColor = selectedForeColor;
                
                // Toolbar colors (if accessible)
                try
                {
                    var toolbarProp = propertyGrid.GetType().GetProperty("ToolStripRenderer");
                    if (toolbarProp != null)
                    {
                        // Try to set toolbar colors
                        var toolbar = propertyGrid.Controls.OfType<System.Windows.Forms.ToolStrip>().FirstOrDefault();
                        if (toolbar != null)
                        {
                            toolbar.BackColor = backColor;
                            toolbar.ForeColor = foreColor;
                        }
                    }
                }
                catch
                {
                    // Toolbar styling is optional
                }
                
                propertyGrid.Invalidate();
            }
            catch
            {
                // If styling fails, PropertyGrid will use default colors
            }
        }
        
        private void SyncAdvancedDialogControls(Control parent)
        {
            // Use PoisonControlHelper to sync all controls with StyleManager
            // This replaces all the manual reflection and property setting code
            if (poisonStyleManager != null)
            {
                ReaLTaiizorExt.PoisonControlHelper.ApplyStyleManager(parent, poisonStyleManager);
            }
        }
        
        #endregion
        
        #region Game Status Detection and Monitoring
        
        /// <summary>
        /// Makes the game status label non-interactive and part of the form
        /// </summary>
        private void SetupGameStatusLabel()
        {
            if (lblGameStatus != null && lblGameStatus is ReaLTaiizor.Controls.PoisonLabel statusLabel)
            {
                // Set custom font (moved from Designer - Designer can't set Font property directly)
                if (statusLabel.UseCustomFont)
                {
                    statusLabel.Font = new System.Drawing.Font("Consolas", 9F);
                }
                // PoisonLabel is naturally non-interactive and non-selectable
                statusLabel.UseStyleColors = false;
            }
        }
        
        /// <summary>
        /// Enables form dragging from the game status label area
        /// </summary>
        private void EnableGameStatusDragging()
        {
            if (lblGameStatus != null)
            {
                lblGameStatus.MouseDown += GameStatusLabel_MouseDown;
            }
        }
        
        /// <summary>
        /// Handles mouse down on game status label - makes the status bar area draggable
        /// </summary>
        private void GameStatusLabel_MouseDown(object sender, MouseEventArgs e)
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        // Use Windows API to simulate title bar drag
                        ReleaseCapture();
                        SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                    }
        }
        
        
        /// <summary>
        /// Initializes the game status monitoring timer and label - optimized for smooth operation
        /// </summary>
        private void InitializeGameStatusMonitoring()
        {
            gameStatusTimer = ReaLTaiizorExt.PoisonFormHelper.CreateTrackedTimer(this, 2500, GameStatusTimer_Tick);
            
            // Start the timer (delay is handled in MainForm_Shown)
            try
            {
                // Now start the game status timer
                if (gameStatusTimer != null && !gameStatusTimer.Enabled && !this.IsDisposed)
                {
                    gameStatusTimer.Start();
                    // First status update (on UI thread) using SafeInvoke
                    ReaLTaiizorExt.PoisonFormHelper.SafeInvoke(this, () => UpdateGameStatus());
                }
            }
            catch
            {
                // Silently ignore any exceptions
            }
        }
        
        /// <summary>
        /// Timer tick event to periodically check game status.
        /// </summary>
        private void GameStatusTimer_Tick(object sender, EventArgs e)
        {
            // Prevent overlapping updates for smooth operation
            if (isUpdatingGameStatus)
                return;
            
            // Skip update if form is minimized or disposed (save resources)
            if (this.IsDisposed || this.WindowState == FormWindowState.Minimized)
                return;
            
            // Use SafeInvoke to avoid blocking the timer thread with error handling
            ReaLTaiizorExt.PoisonFormHelper.SafeInvoke(this, () =>
            {
                if (this.IsDisposed || this.Disposing)
                    return;
                
                isUpdatingGameStatus = true;
                try
                {
                    UpdateGameStatus();
                }
                finally
                {
                    isUpdatingGameStatus = false;
                }
            });
        }
        
        /// <summary>
        /// Updates the game status indicator - optimized for smooth operation
        /// </summary>
        private void UpdateGameStatus()
        {
            try
            {
                if (lblGameStatus == null || lblGameStatus.IsDisposed)
                    return;
                
                if (this.IsDisposed || this.Disposing)
                    return;
                
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(UpdateGameStatus));
                    return;
                }
                
                bool t7Running = IsGameRunning(TreyarchCompiler.Enums.Games.T7);
                bool t8Running = IsGameRunning(TreyarchCompiler.Enums.Games.T8);
                
                bool statusChanged = (t7Running != lastT7Status || t8Running != lastT8Status);
                bool isFirstUpdate = (lastT7Status == false && lastT8Status == false);
                
                if (isFirstUpdate || statusChanged)
                {
                    // Update status tracking
                    lastT7Status = t7Running;
                    lastT8Status = t8Running;
                    
                    // Update reset parse tree button when game status changes
                    UpdateResetParseTreeButton();
                    
                    // Update Launch BO3 button - disable if BO3 is running
                    if (btnLaunchBO3 != null && !btnLaunchBO3.IsDisposed)
                    {
                        btnLaunchBO3.Enabled = !t7Running;
                        if (poisonToolTip != null)
                        {
                            if (t7Running)
                            {
                                poisonToolTip.SetToolTip(btnLaunchBO3, "Black Ops 3 is already running");
                            }
                            else
                            {
                                poisonToolTip.SetToolTip(btnLaunchBO3, "Launch Black Ops 3 (requires Steam)");
                            }
                        }
                    }
                    
                    // Update Kill BO3 button - enable if BO3 is running
                    if (btnKillBO3 != null && !btnKillBO3.IsDisposed)
                    {
                        btnKillBO3.Enabled = t7Running;
                        if (poisonToolTip != null)
                        {
                            if (t7Running)
                            {
                                poisonToolTip.SetToolTip(btnKillBO3, "Kill Black Ops 3 process");
                            }
                            else
                            {
                                poisonToolTip.SetToolTip(btnKillBO3, "Black Ops 3 is not running");
                            }
                        }
                    }
                    
                    // Build status text and set colors based on running status
                    if (lblGameStatus is ReaLTaiizor.Controls.PoisonLabel statusLabel)
                    {
                        // Build status text with status indicators
                        string bo3Status = t7Running ? "Running" : "Not Running";
                        string bo4Status = t8Running ? "Running" : "Not Running";
                        statusLabel.Text = $"BO3: {bo3Status} | BO4: {bo4Status}";
                        
                        // Set color based on running status - green if any game is running, red if none
                        // Use theme-aware colors for status label
                        // Use PoisonPaint for consistent theme colors
                        ThemeStyle theme = poisonStyleManager?.Theme ?? ThemeStyle.Dark;
                        statusLabel.ForeColor = PoisonPaint.ForeColor.Label.Normal(theme);
                    }
                    
                    UpdateUI();
                }
            }
            catch (ObjectDisposedException)
            {
                // Control or form was disposed, stop the timer
                if (gameStatusTimer != null && gameStatusTimer.Enabled)
                {
                    gameStatusTimer.Stop();
                }
            }
            catch (Exception)
            {
                // Silently ignore other exceptions to prevent timer from crashing
            }
        }
        
        private void SetupUITheme()
        {
            // Use PoisonFormHelper for standardized form initialization
            // This handles StyleManager application, button effects, and form icon loading
            if (poisonStyleManager != null)
            {
                ReaLTaiizor.Extension.Poison.PoisonFormHelper.InitializeForm(this, poisonStyleManager);
            }
        }
        
        
        private void UpdateTabPanelsBackground()
        {
            if (poisonStyleManager == null) return;
            
            Color formBackColor = PoisonPaint.BackColor.Form(poisonStyleManager.Theme);
            
            // Update all tab pages
            if (tabControl != null)
            {
                foreach (System.Windows.Forms.TabPage tabPage in tabControl.TabPages)
                {
                    tabPage.BackColor = formBackColor;
                }
            }
        }
        
        
        private void UpdateControlStyle(Control parent, ColorStyle style, ThemeStyle theme, bool forceUpdate = false)
        {
            if (parent == null || poisonStyleManager == null) return;
            
            // Use PoisonControlHelper to apply StyleManager - it handles all the manual setup
            ReaLTaiizorExt.PoisonControlHelper.ApplyStyleManager(parent, poisonStyleManager);
            
            // If forceUpdate is true, use helper method to update theme/style with refresh
            // The helper method already handles recursion automatically
            if (forceUpdate)
            {
                ReaLTaiizorExt.PoisonControlHelper.UpdateThemeAndStyleWithRefresh(parent, theme, style);
            }
        }
        
        private void SetupToolTips()
        {
            if (poisonToolTip == null) return;
            
            // Configure tooltip theme to match current StyleManager settings
            if (poisonStyleManager != null)
            {
                // PoisonToolTip is a component, not a control - set properties directly
                poisonToolTip.Style = poisonStyleManager.Style;
                poisonToolTip.Theme = poisonStyleManager.Theme;
                poisonToolTip.StyleManager = poisonStyleManager;
                
                // Subscribe to Draw event for custom styling with Style color
                poisonToolTip.Draw -= poisonToolTip_Draw; // Remove if already subscribed
                poisonToolTip.Draw += poisonToolTip_Draw; // Add our custom handler
            }
            else
            {
                // Fallback to default theme/style if StyleManager not available
                poisonToolTip.Style = ColorStyle.Red;
                poisonToolTip.Theme = ThemeStyle.Dark;
            }
            
            // Compile tab tooltips
            poisonToolTip.SetToolTip(txtProjectFolder, "Select the folder containing your GSC source files and gsc.conf");
            poisonToolTip.SetToolTip(btnSelectProjectFolder, "Browse for project folder");
            poisonToolTip.SetToolTip(txtOutputFile, "Output path for compiled .gscc file");
            poisonToolTip.SetToolTip(btnSelectOutputFile, "Choose location and name for compiled script");
            poisonToolTip.SetToolTip(chkSaveOpcodeMap, "Save opcode mapping information for debugging");
            poisonToolTip.SetToolTip(chkOpcodeMasking, "Enable opcode masking for obfuscation");
            if (btnPlatform != null) poisonToolTip.SetToolTip(btnPlatform, "Select target platform (PC or PS4)");
            if (btnGame != null) poisonToolTip.SetToolTip(btnGame, "Select target game (T7/BO3 or T8/BO4)");
            poisonToolTip.SetToolTip(btnGscConfMode, "Quick editor for gsc.conf - Select game modes (MP/ZM/SP)");
            poisonToolTip.SetToolTip(btnCompile, "Compile GSC scripts into .gscc file");
            
            // Inject tab tooltips
            poisonToolTip.SetToolTip(txtInjectFile, "Select compiled .gscc file to inject");
            poisonToolTip.SetToolTip(btnSelectInjectFile, "Browse for compiled file to inject");
            poisonToolTip.SetToolTip(txtInjectPath, "Script path in game to replace (e.g., scripts/shared/duplicaterender_mgr.gsc)");
            if (btnInjectGame != null) poisonToolTip.SetToolTip(btnInjectGame, "Select target game for injection");
            poisonToolTip.SetToolTip(chkNoRuntime, "Disable runtime checks during injection");
            poisonToolTip.SetToolTip(btnInject, "Inject compiled script into game");
            poisonToolTip.SetToolTip(btnResetParseTree, "Reset the parse tree cache");
            poisonToolTip.SetToolTip(btnLaunchBO3, "Launch Black Ops 3 (requires Steam)");
            if (btnKillBO3 != null)
                poisonToolTip.SetToolTip(btnKillBO3, "Kill Black Ops 3 process");
            
            // Settings tab tooltips
            
            // Log panel tooltips
            if (btnLogClear != null)
                poisonToolTip.SetToolTip(btnLogClear, "Clear the log output");
            if (btnLogCopy != null)
                poisonToolTip.SetToolTip(btnLogCopy, "Copy log contents to clipboard");
            if (btnLogSave != null)
                poisonToolTip.SetToolTip(btnLogSave, "Save log contents to file");
            if (txtLogSearch != null)
                poisonToolTip.SetToolTip(txtLogSearch, "Search log contents (type to filter)");
            
            // Progress spinner
            poisonToolTip.SetToolTip(progressSpinner, "Operation in progress...");
        }
        
        private void InitializeBuildFolder()
        {
            // Try to use a writable location
            // First, try MyDocuments (most reliable)
            string preferredPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "T7Compiler", "build");
            try
            {
                // Ensure the directory exists and is writable
                if (!Directory.Exists(preferredPath))
                {
                    Directory.CreateDirectory(preferredPath);
                }
                // Test write access by creating a test file
                string testFile = Path.Combine(preferredPath, ".writetest");
                try
                {
                    File.WriteAllText(testFile, "test");
                    File.Delete(testFile);
                    buildFolder = preferredPath;
                    return;
                }
                catch
                {
                    // Not writable, try next option
                }
            }
            catch
            {
                // Failed to create, try next option
            }
            
            // Fallback to temp folder if MyDocuments doesn't work
            try
            {
                string tempPath = Path.Combine(Path.GetTempPath(), "T7Compiler", "build");
                if (!Directory.Exists(tempPath))
                {
                    Directory.CreateDirectory(tempPath);
                }
                buildFolder = tempPath;
            }
            catch
            {
                // Last resort: use application startup directory (more reliable than CurrentDirectory)
                try
                {
                    string startupPath = Application.StartupPath;
                    if (!string.IsNullOrEmpty(startupPath))
                    {
                        string startupBuildPath = Path.Combine(startupPath, "build");
                        buildFolder = startupBuildPath;
                    }
                    else
                    {
                        // Absolute last resort: use temp folder with a unique name
                        buildFolder = Path.Combine(Path.GetTempPath(), "T7Compiler", "build");
                    }
                }
                catch
                {
                    // Absolute last resort: use temp folder
                    buildFolder = Path.Combine(Path.GetTempPath(), "T7Compiler", "build");
                }
            }
        }
        
        /// <summary>
        /// Ensures the build folder exists.
        /// </summary>
        private void EnsureBuildFolderExists()
        {
            try
            {
                if (!Directory.Exists(buildFolder))
                {
                    Directory.CreateDirectory(buildFolder);
                }
            }
            catch
            {
                buildFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "T7Compiler", "build");
                try
                {
                    if (!Directory.Exists(buildFolder))
                        Directory.CreateDirectory(buildFolder);
                }
                catch
                {
                    // Last resort: use temp folder
                    buildFolder = Path.Combine(Path.GetTempPath(), "T7Compiler", "build");
                    if (!Directory.Exists(buildFolder))
                        Directory.CreateDirectory(buildFolder);
                }
            }
        }
        
        /// <summary>
        /// Sets up a PoisonDropDownButton to behave like a ComboBox with Items collection
        /// Also ensures proper hover/pressed effects using enhanced ReaLTaiizor helpers
        /// </summary>
        private void SetupDropDownButtonAsComboBox(PoisonDropDownButton button, ContextMenuStrip menuStrip)
        {
            if (button == null || menuStrip == null) return;
            
            // Enable ComboBox behavior
            button.BehaveLikeComboBox = true;
            
            // Set StyleManager and UseStyleColors for proper theming
            if (poisonStyleManager != null)
            {
                // Use helper to setup button with StyleManager
                ReaLTaiizorExt.PoisonControlHelper.SetupButton(button, poisonStyleManager, true, true);
            }
            
            // Use the enhanced helper to ensure proper hover/pressed effects
            // This now uses the improved ReaLTaiizor helper internally
            ReaLTaiizorExt.PoisonControlHelper.SetupDropDownButtonEffects(button, poisonStyleManager);
            
            // Wire up SelectedIndexChanged event to handle selection
            // Skip for btnRecentProjects - it has its own custom handler (BtnRecentProjects_SelectedIndexChanged)
            if (button != btnRecentProjects)
            {
                button.SelectedIndexChanged += (s, e) =>
                {
                    if (button.SelectedIndex >= 0 && button.SelectedIndex < menuStrip.Items.Count)
                    {
                        var menuItem = menuStrip.Items[button.SelectedIndex] as ToolStripMenuItem;
                        if (menuItem != null)
                        {
                            // Trigger the menu item's click event
                            menuItem.PerformClick();
                        }
                    }
                };
            }
        }
        
        private void SetupDropdownButtons()
        {
            // Setup Platform dropdown button
            SetupPlatformMenu();
            SetupDropDownButtonAsComboBox(btnPlatform, platformMenu);
            
            // Setup Game dropdown button
            SetupGameMenu();
            SetupDropDownButtonAsComboBox(btnGame, gameMenu);
            
            // Setup Inject Game dropdown button
            SetupInjectGameMenu();
            SetupDropDownButtonAsComboBox(btnInjectGame, injectGameMenu);
            
            SetupHotReloadMenu();
            SetupDropDownButtonAsComboBox(btnHotReload, hotReloadMenu);
            
            // Initialize Settings Tab Controls
            SetupSettingsTab();
            if (btnSettingsColorStyle != null && settingsColorStyleMenu != null)
            {
                SetupDropDownButtonAsComboBox(btnSettingsColorStyle, settingsColorStyleMenu);
            }
        }
        
        private void SetupPlatformMenu()
        {
            if (platformMenu == null || btnPlatform == null) return;
            
            platformMenu.Items.Clear();
            if (btnPlatform.BehaveLikeComboBox)
            {
                btnPlatform.ClearItems();
            }
            
            var pcItem = new ToolStripMenuItem("PC")
            {
                Tag = 0,
                Checked = false
            };
            var ps4Item = new ToolStripMenuItem("PS4")
            {
                Tag = 1,
                Checked = false
            };
            
            pcItem.Click += (s, e) => platformMenu_ItemClicked(s, new ToolStripItemClickedEventArgs(pcItem));
            ps4Item.Click += (s, e) => platformMenu_ItemClicked(s, new ToolStripItemClickedEventArgs(ps4Item));
            
            platformMenu.Items.Add(pcItem);
            platformMenu.Items.Add(ps4Item);
            
            // Add to ComboBox items if enabled
            if (btnPlatform.BehaveLikeComboBox)
            {
                btnPlatform.AddItem("PC");
                btnPlatform.AddItem("PS4");
            }
            
            // Update button text and selected index
            btnPlatform.Text = (currentPlatformIndex == 0) ? "PC" : "PS4";
            if (btnPlatform.BehaveLikeComboBox)
            {
                btnPlatform.SelectedIndex = currentPlatformIndex;
            }
            
            // Connect to style manager
            if (poisonStyleManager != null)
            {
                // StyleManager is applied via PoisonControlHelper.SetupAllButtonEffectsRecursive
                ReaLTaiizor.Extension.Poison.PoisonControlHelper.ApplyStyleManager(btnPlatform, poisonStyleManager);
                EnhanceMenuItems(platformMenu);
            }
        }
        
        private void SetupGameMenu()
        {
            if (gameMenu == null || btnGame == null) return;
            
            gameMenu.Items.Clear();
            if (btnGame.BehaveLikeComboBox)
            {
                btnGame.ClearItems();
            }
            
            var t7Item = new ToolStripMenuItem("T7 (BO3)")
            {
                Tag = 0,
                Checked = false
            };
            var t8Item = new ToolStripMenuItem("T8 (BO4)")
            {
                Tag = 1,
                Checked = false
            };
            
            t7Item.Click += (s, e) => gameMenu_ItemClicked(s, new ToolStripItemClickedEventArgs(t7Item));
            t8Item.Click += (s, e) => gameMenu_ItemClicked(s, new ToolStripItemClickedEventArgs(t8Item));
            
            gameMenu.Items.Add(t7Item);
            gameMenu.Items.Add(t8Item);
            
            // Add to ComboBox items if enabled
            if (btnGame.BehaveLikeComboBox)
            {
                btnGame.AddItem("T7 (BO3)");
                btnGame.AddItem("T8 (BO4)");
            }
            
            // Update button text and selected index
            btnGame.Text = (currentGameIndex == 0) ? "T7 (BO3)" : "T8 (BO4)";
            if (btnGame.BehaveLikeComboBox)
            {
                btnGame.SelectedIndex = currentGameIndex;
            }
            
            // Connect to style manager
            if (poisonStyleManager != null)
            {
                // StyleManager is applied via PoisonControlHelper.SetupAllButtonEffectsRecursive
                ReaLTaiizor.Extension.Poison.PoisonControlHelper.ApplyStyleManager(btnGame, poisonStyleManager);
                EnhanceMenuItems(gameMenu);
            }
        }
        
        private void SetupSettingsTab()
        {
            // Setup color style menu
            SetupSettingsColorStyleMenu();
            
            // Setup context menu for settings
            SetupSettingsContextMenu();
            
            if (toggleSettingsTheme != null && !toggleSettingsTheme.Checked && toggleSettingsTheme.Text == "")
            {
                toggleSettingsTheme.Checked = false;
                toggleSettingsTheme.Text = "Dark";
            }
            
            if (poisonStyleManager != null)
            {
                if (btnSettingsColorStyle != null)
                {
                    // StyleManager is applied via helper
                    ReaLTaiizor.Extension.Poison.PoisonControlHelper.ApplyStyleManager(btnSettingsColorStyle, poisonStyleManager);
                    if (string.IsNullOrWhiteSpace(btnSettingsColorStyle.Text))
                    {
                    btnSettingsColorStyle.Text = poisonStyleManager.Style.ToString();
                    }
                    EnhanceMenuItems(settingsColorStyleMenu);
                }
            }
        }
        
        private void SetupSettingsContextMenu()
        {
            if (settingsContextMenu == null) return;
            
            settingsContextMenu.Items.Clear();
            
            // Keyboard Shortcuts menu item
            ToolStripMenuItem keyboardShortcutsItem = new ToolStripMenuItem("Keyboard Shortcuts...");
            keyboardShortcutsItem.Click += (s, e) =>
            {
                using (var dialog = new Dialogs.KeybindDialog(poisonStyleManager, keybinds ?? Dialogs.KeybindDialog.GetDefaultKeybinds(), this))
                {
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        keybinds = dialog.Keybinds;
                        SaveAllSettings();
                        SetupKeyboardShortcuts();
                    }
                }
            };
            settingsContextMenu.Items.Add(keyboardShortcutsItem);
            
            // Application Config menu item
            ToolStripMenuItem applicationConfigItem = new ToolStripMenuItem("Application Config...");
            applicationConfigItem.Click += (s, e) =>
            {
                ShowAdvancedSettingsDialog();
            };
            settingsContextMenu.Items.Add(applicationConfigItem);
            
            // Separator
            settingsContextMenu.Items.Add(new ToolStripSeparator());
            
            // Reset Default menu item
            ToolStripMenuItem resetDefaultItem = new ToolStripMenuItem("Reset Default");
            resetDefaultItem.Click += (s, e) =>
            {
                ResetAllSettingsToDefaults();
            };
            settingsContextMenu.Items.Add(resetDefaultItem);
            
            // Enhance menu items with theme styling
            EnhanceMenuItems(settingsContextMenu);
            
            settingsContextMenu.Opening += (s, e) =>
            {
                if (poisonStyleManager != null && settingsContextMenu.Renderer is StyleBasedMenuRenderer renderer)
                {
                    renderer.UpdateStyleManager(poisonStyleManager);
                }
                settingsContextMenu.Invalidate();
            };
        }
        
        private void SetupSettingsColorStyleMenu()
        {
            if (settingsColorStyleMenu == null || btnSettingsColorStyle == null) return;
            
            settingsColorStyleMenu.Items.Clear();
            if (btnSettingsColorStyle.BehaveLikeComboBox)
            {
                btnSettingsColorStyle.ClearItems();
            }
            
            var styles = new[]
            {
                ColorStyle.Red, ColorStyle.Blue, ColorStyle.Green, ColorStyle.Orange, ColorStyle.Purple, ColorStyle.Teal,
                ColorStyle.Lime, ColorStyle.Brown, ColorStyle.Pink, ColorStyle.Magenta, ColorStyle.Silver, ColorStyle.Yellow
            };
            
            foreach (var style in styles)
            {
                var item = new ToolStripMenuItem(style.ToString())
                {
                    Tag = style,
                    Checked = false
                };
                item.Click += (s, e) =>
                {
                    if (item.Tag is ColorStyle selectedStyle)
                    {
                        ChangeColorStyle(selectedStyle);
                        UpdateSettingsColorStyleButton();
                    }
                };
                settingsColorStyleMenu.Items.Add(item);
                
                // Add to ComboBox items if enabled
                if (btnSettingsColorStyle.BehaveLikeComboBox)
                {
                    btnSettingsColorStyle.AddItem(style.ToString());
                }
            }
            
            // Add Rainbow option
            var rainbowItem = new ToolStripMenuItem("Rainbow")
            {
                Tag = "Rainbow",
                Checked = false
            };
            rainbowItem.Click += (s, e) =>
            {
                ChangeColorStyleToRainbow();
                UpdateSettingsColorStyleButton();
            };
            settingsColorStyleMenu.Items.Add(rainbowItem);
            
            // Add Rainbow to ComboBox items if enabled
            if (btnSettingsColorStyle.BehaveLikeComboBox)
            {
                btnSettingsColorStyle.AddItem("Rainbow");
            }
            
            // Apply custom renderer
            if (poisonStyleManager != null)
            {
                settingsColorStyleMenu.Renderer = new StyleBasedMenuRenderer(poisonStyleManager);
            }
        }
        
        private void UpdateSettingsColorStyleButton()
        {
            if (btnSettingsColorStyle != null && poisonStyleManager != null)
            {
                string text;
                int selectedIndex = -1;
                
                if (IsRainbowStyleActive())
                {
                    text = "Rainbow";
                    selectedIndex = settingsColorStyleMenu != null ? settingsColorStyleMenu.Items.Count - 1 : -1; // Rainbow is last item
                }
                else
                {
                    text = poisonStyleManager.Style.ToString();
                    // Find the index of the current style
                    if (settingsColorStyleMenu != null)
                    {
                        for (int i = 0; i < settingsColorStyleMenu.Items.Count - 1; i++) // Exclude Rainbow
                        {
                            if (settingsColorStyleMenu.Items[i].Tag is ColorStyle style && style == poisonStyleManager.Style)
                            {
                                selectedIndex = i;
                                break;
                            }
                        }
                    }
                }
                
                btnSettingsColorStyle.Text = text;
                
                // Update ComboBox selected index if enabled
                if (btnSettingsColorStyle.BehaveLikeComboBox && selectedIndex >= 0)
                {
                    btnSettingsColorStyle.SelectedIndex = selectedIndex;
                }
                
                // Update menu immediately - refresh renderer and invalidate
                if (settingsColorStyleMenu != null && poisonStyleManager != null)
                {
                    // Use helper to apply StyleManager - handles Theme, Style automatically
                    ReaLTaiizor.Extension.Poison.PoisonControlHelper.ApplyStyleManager(settingsColorStyleMenu, poisonStyleManager);
                    
                    // Update renderer with new style manager
                    if (settingsColorStyleMenu.Renderer is StyleBasedMenuRenderer renderer)
                    {
                        renderer.UpdateStyleManager(poisonStyleManager);
                    }
                    else
                    {
                        settingsColorStyleMenu.Renderer = new StyleBasedMenuRenderer(poisonStyleManager);
                    }
                    
                    // Force menu to refresh
                    settingsColorStyleMenu.Invalidate();
                }
            }
        }
        
        private void SetupInjectGameMenu()
        {
            if (injectGameMenu == null || btnInjectGame == null) return;
            
            injectGameMenu.Items.Clear();
            if (btnInjectGame.BehaveLikeComboBox)
            {
                btnInjectGame.ClearItems();
            }
            
            var t7Item = new ToolStripMenuItem("T7 (BO3)")
            {
                Tag = 0,
                Checked = false
            };
            var t8Item = new ToolStripMenuItem("T8 (BO4)")
            {
                Tag = 1,
                Checked = false
            };
            
            t7Item.Click += (s, e) => injectGameMenu_ItemClicked(s, new ToolStripItemClickedEventArgs(t7Item));
            t8Item.Click += (s, e) => injectGameMenu_ItemClicked(s, new ToolStripItemClickedEventArgs(t8Item));
            
            injectGameMenu.Items.Add(t7Item);
            injectGameMenu.Items.Add(t8Item);
            
            // Add to ComboBox items if enabled
            if (btnInjectGame.BehaveLikeComboBox)
            {
                btnInjectGame.AddItem("T7 (BO3)");
                btnInjectGame.AddItem("T8 (BO4)");
            }
            
            // Update button text and selected index
            btnInjectGame.Text = (currentInjectGameIndex == 0) ? "T7 (BO3)" : "T8 (BO4)";
            if (btnInjectGame.BehaveLikeComboBox)
            {
                btnInjectGame.SelectedIndex = currentInjectGameIndex;
            }
            
            // Connect to style manager
            if (poisonStyleManager != null)
            {
                // Use helper to apply StyleManager - handles Theme, Style, UseStyleColors automatically
                ReaLTaiizor.Extension.Poison.PoisonControlHelper.ApplyStyleManager(btnInjectGame, poisonStyleManager);
                EnhanceMenuItems(injectGameMenu);
            }
        }
        
        private void SetupHotReloadMenu()
        {
            if (hotReloadMenu == null || btnHotReload == null) return;
            
            hotReloadMenu.Items.Clear();
            if (btnHotReload.BehaveLikeComboBox)
            {
                btnHotReload.ClearItems();
            }
            
            var noneItem = new ToolStripMenuItem("None") { Tag = HotMode.None };
            var cscItem = new ToolStripMenuItem("CSC") { Tag = HotMode.Csc };
            var gscItem = new ToolStripMenuItem("GSC") { Tag = HotMode.Gsc };
            
            noneItem.Click += (s, e) => hotReloadMenu_ItemClicked(s, new ToolStripItemClickedEventArgs(noneItem));
            cscItem.Click += (s, e) => hotReloadMenu_ItemClicked(s, new ToolStripItemClickedEventArgs(cscItem));
            gscItem.Click += (s, e) => hotReloadMenu_ItemClicked(s, new ToolStripItemClickedEventArgs(gscItem));
            
            hotReloadMenu.Items.Add(noneItem);
            hotReloadMenu.Items.Add(cscItem);
            hotReloadMenu.Items.Add(gscItem);
            
            // Add to ComboBox items if enabled
            if (btnHotReload.BehaveLikeComboBox)
            {
                btnHotReload.AddItem("None");
                btnHotReload.AddItem("CSC");
                btnHotReload.AddItem("GSC");
            }
            
            UpdateHotReloadButton();
            
            // Connect to style manager
            if (poisonStyleManager != null)
            {
                // Use helper to apply StyleManager - handles Theme, Style, UseStyleColors automatically
                ReaLTaiizor.Extension.Poison.PoisonControlHelper.ApplyStyleManager(btnHotReload, poisonStyleManager);
                EnhanceMenuItems(hotReloadMenu);
            }
        }
        
        private void hotReloadMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem is ToolStripMenuItem item && item.Tag is HotMode mode)
            {
                currentHotMode = mode;
                UpdateHotReloadButton();
                
                // Update ComboBox selected index if enabled
                if (btnHotReload.BehaveLikeComboBox)
                {
                    int index = hotReloadMenu.Items.IndexOf(item);
                    if (index >= 0)
                    {
                        btnHotReload.SelectedIndex = index;
                    }
                }
                
                SaveHotReloadToGscConf();
            }
        }
        
        private void UpdateHotReloadButton()
        {
            if (btnHotReload == null) return;
            
            // Use Designer-set text as base, update based on mode
            string text;
            int selectedIndex = -1;
            switch (currentHotMode)
            {
                case HotMode.None:
                    text = "None";
                    selectedIndex = 0;
                    break;
                case HotMode.Csc:
                    text = "CSC";
                    selectedIndex = 1;
                    break;
                case HotMode.Gsc:
                    text = "GSC";
                    selectedIndex = 2;
                    break;
                default:
                    text = "None";
                    selectedIndex = 0;
                    break;
            }
            
            btnHotReload.Text = text;
            if (btnHotReload.BehaveLikeComboBox && selectedIndex >= 0)
            {
                btnHotReload.SelectedIndex = selectedIndex;
            }
        }
        
        private void SaveHotReloadToGscConf()
        {
            try
            {
                string expandedProjectFolder = Helpers.PathHelper.ExpandPath(txtProjectFolder.Text);
                if (string.IsNullOrEmpty(expandedProjectFolder) || !Directory.Exists(expandedProjectFolder))
                    return;
                
                string gscConfPath = Path.Combine(expandedProjectFolder, "gsc.conf");
                if (!File.Exists(gscConfPath))
                    gscConfPath = "gsc.conf";
                
                if (File.Exists(gscConfPath))
                {
                    var lines = File.ReadAllLines(gscConfPath).ToList();
                    bool found = false;
                    
                    for (int i = 0; i < lines.Count; i++)
                    {
                        if (lines[i].Trim().StartsWith("hot=", StringComparison.OrdinalIgnoreCase))
                        {
                            lines[i] = $"hot={currentHotMode.ToString().ToLower()}";
                            found = true;
                            break;
                        }
                    }
                    
                    if (!found)
                    {
                        lines.Add($"hot={currentHotMode.ToString().ToLower()}");
                    }
                    
                    File.WriteAllLines(gscConfPath, lines);
                }
            }
            catch { }
        }
        
        private void platformMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem is ToolStripMenuItem item && item.Tag is int index)
            {
                currentPlatformIndex = index;
                btnPlatform.Text = item.Text;
                
                // Update ComboBox selected index if enabled
                if (btnPlatform.BehaveLikeComboBox)
                {
                    btnPlatform.SelectedIndex = index;
                }
                
                SaveAllSettings();
            }
        }
        
        private void gameMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem is ToolStripMenuItem item && item.Tag is int index)
            {
                currentGameIndex = index;
                btnGame.Text = item.Text;
                
                // Update ComboBox selected index if enabled
                if (btnGame.BehaveLikeComboBox)
                {
                    btnGame.SelectedIndex = index;
                }
                
                SaveAllSettings();
            }
        }
        
        private void injectGameMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem is ToolStripMenuItem item && item.Tag is int index)
            {
                currentInjectGameIndex = index;
                btnInjectGame.Text = item.Text;
                
                // Update ComboBox selected index if enabled
                if (btnInjectGame.BehaveLikeComboBox)
                {
                    btnInjectGame.SelectedIndex = index;
                }
                
                SaveAllSettings();
                UpdateUI(); // Check if game is running
            }
        }
                       
        // StyleManager automatically propagates to all controls with Style=Default and Theme=Default
        
        private void SetupControlInteractivity()
        {
            // Enable DisplayFocus for better visual feedback on interactive controls
            SetControlDisplayFocus(this);
            
            // Ensure all buttons have proper interactivity
            EnhanceButtonInteractivity();
            
            // Enhance checkboxes with proper font settings
            EnhanceCheckBoxInteractivity();
            
            // Ensure all dropdown menus are properly styled
            SetupMenuStyling();
            
            // Setup progress bar rainbow support
            SetupProgressBarRainbow();
        }
        
        private void EnhanceCheckBoxInteractivity()
        {
        }
        
        // Spinner automatically inherits from StyleManager when form has StyleManager set
        
        private void SetupProgressBarRainbow()
        {
            if (progressBar != null)
            {
                // Subscribe to CustomPaint to apply rainbow color when active
                progressBar.CustomPaint += (s, e) =>
                {
                    if (IsRainbowStyleActive())
                    {
                        // Draw rainbow-colored progress fill
                        if (progressBar.Value > 0)
                        {
                            float percent = (float)progressBar.Value / (float)progressBar.Maximum;
                            int fillWidth = (int)(progressBar.Width * percent);
                            using (SolidBrush brush = new SolidBrush(currentRainbowColor))
                            {
                                e.Graphics.FillRectangle(brush, new Rectangle(0, 0, fillWidth, progressBar.Height));
                            }
                        }
                    }
                };
            }
        }
        
        private void SetupMenuStyling()
        {
            // List of all dropdown menus
            var menus = new ReaLTaiizor.Controls.PoisonContextMenuStrip[]
            {
                platformMenu,
                gameMenu,
                injectGameMenu,
                settingsColorStyleMenu,
                mainFormContextMenu,
                settingsContextMenu
            };
            
            foreach (var menu in menus)
            {
                if (menu != null && poisonStyleManager != null)
                {
                    // Use helper to apply StyleManager - handles Theme, Style automatically
                    ReaLTaiizor.Extension.Poison.PoisonControlHelper.ApplyStyleManager(menu, poisonStyleManager);
                    
                    // Update menu items when menu is opened to ensure proper styling
                    menu.Opening += (s, e) =>
                    {
                        if (menu.StyleManager != null)
                        {
                            // Use helper to sync menu with StyleManager
                            ReaLTaiizor.Extension.Poison.PoisonControlHelper.ApplyStyleManager(menu, menu.StyleManager);
                            
                            // Enhance menu items with hover/press effects
                            EnhanceMenuItems(menu);
                            
                            menu.Invalidate();
                        }
                    };
                    
                    // Also enhance menu items immediately if menu already has items
                    EnhanceMenuItems(menu);
                }
            }
        }
        
        private void EnhanceMenuItems(ReaLTaiizor.Controls.PoisonContextMenuStrip menu)
        {
            if (menu == null || poisonStyleManager == null) return;
            
            // Set up custom renderer for style-based selected color
            menu.Renderer = new StyleBasedMenuRenderer(poisonStyleManager);
            
            foreach (ToolStripItem item in menu.Items)
            {
                if (item is ToolStripMenuItem menuItem)
                {
                    // Ensure menu item uses the current style color for hover/press
                    menuItem.MouseEnter += (s, e) =>
                    {
                        // Menu item hover effect is handled by PoisonContextMenuStrip
                        if (menu.StyleManager != null)
                        {
                            // Use helper to sync menu with its StyleManager
                            ReaLTaiizor.Extension.Poison.PoisonControlHelper.ApplyStyleManager(menu, menu.StyleManager);
                            menu.Invalidate();
                        }
                    };
                    
                    // Ensure proper styling on mouse leave
                    menuItem.MouseLeave += (s, e) =>
                    {
                        menu.Invalidate();
                    };
                    
                    // Ensure proper styling on click
                    menuItem.MouseDown += (s, e) =>
                    {
                        menu.Invalidate();
                    };
                    
                    menuItem.MouseUp += (s, e) =>
                    {
                        menu.Invalidate();
                    };
                }
            }
        }
        
        // Custom renderer for menu items to show style-based selected color
        private class StyleBasedMenuRenderer : ToolStripProfessionalRenderer
        {
            private PoisonStyleManager styleManager;
            private MainForm parentForm; // Reference to parent form for rainbow access
            
            public StyleBasedMenuRenderer(PoisonStyleManager manager) : base()
            {
                styleManager = manager;
                // Get parent form reference for rainbow color access
                if (manager != null && manager.Owner is MainForm form)
                {
                    parentForm = form;
                }
            }
            
            public void UpdateStyleManager(PoisonStyleManager manager)
            {
                styleManager = manager;
                // Update parent form reference
                if (manager != null && manager.Owner is MainForm form)
                {
                    parentForm = form;
                }
            }
            
            // Helper to check if rainbow is active
            private bool IsRainbowStyleActive()
            {
                return parentForm != null && parentForm.IsRainbowStyleActive();
            }
            
            // Helper to get current rainbow color
            private Color GetCurrentRainbowColor()
            {
                return parentForm != null ? parentForm.currentRainbowColor : PoisonPaint.GetStyleColor(ColorStyle.Red);
            }
            
            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                // Render menu background based on theme
                if (styleManager != null)
                {
                    ThemeStyle theme = styleManager.Theme;
                    Color backColor = PoisonPaint.BackColor.Form(theme);
                    
                    using (SolidBrush brush = new SolidBrush(backColor))
                    {
                        e.Graphics.FillRectangle(brush, e.AffectedBounds);
                    }
                }
                else
                {
                    base.OnRenderToolStripBackground(e);
                }
            }
            
            protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
            {
            }
            
            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
            }
            
            protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
            {
                // Hide image margin (the area where checkmarks/icons go) - use theme background
                if (styleManager != null)
                {
                    ThemeStyle theme = styleManager.Theme;
                    Color backColor = PoisonPaint.BackColor.Form(theme);
                    
                    using (SolidBrush brush = new SolidBrush(backColor))
                    {
                        e.Graphics.FillRectangle(brush, e.AffectedBounds);
                    }
                }
            }
            
            protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
            {
                // The selected state is already indicated by the background color
            }
            
            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                if (e.Item is ToolStripMenuItem menuItem)
                {
                    // Get the current style color and theme
                    ColorStyle currentStyle = styleManager != null ? styleManager.Style : ColorStyle.Red;
                    ThemeStyle theme = styleManager != null ? styleManager.Theme : ThemeStyle.Dark;
                    
                    // Use rainbow color if rainbow mode is active, otherwise use style color
                    Color styleColor;
                    if (IsRainbowStyleActive())
                    {
                        styleColor = GetCurrentRainbowColor();
                    }
                    else
                    {
                        styleColor = PoisonPaint.GetStyleColor(currentStyle);
                    }
                    
                    Color menuBackColor = PoisonPaint.BackColor.Form(theme);
                    
                    // Check if item is hovered (Selected) or is the current active style (for color style menu)
                    bool isActiveStyle = false;
                    if (menuItem.Tag is ColorStyle tagStyle && styleManager != null)
                    {
                        isActiveStyle = (styleManager.Style == tagStyle && !IsRainbowStyleActive());
                    }
                    else if (menuItem.Tag is string tag && tag == "Rainbow" && IsRainbowStyleActive())
                    {
                        // For rainbow option, highlight if rainbow is active
                        isActiveStyle = true;
                    }
                    
                    if (menuItem.Selected || isActiveStyle)
                    {
                        // Use style color with more opacity for selected/active state
                        // Blend style color with background for semi-transparent effect
                        Color menuBg = PoisonPaint.BackColor.Form(styleManager.Theme);
                        Color semiTransparentStyle = PoisonPaint.BlendColors(menuBg, styleColor, 0.4);
                        using (SolidBrush brush = new SolidBrush(semiTransparentStyle))
                        {
                            e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
                        }
                        
                        // Draw a subtle border with the style color
                        using (Pen pen = new Pen(styleColor, 1))
                        {
                            Rectangle borderRect = e.Item.ContentRectangle;
                            borderRect.Width -= 1;
                            borderRect.Height -= 1;
                            e.Graphics.DrawRectangle(pen, borderRect);
                        }
                    }
                    else
                    {
                        // Use theme-based background for non-selected items
                        using (SolidBrush brush = new SolidBrush(menuBackColor))
                        {
                            e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
                        }
                    }
                }
                else
                {
                    base.OnRenderMenuItemBackground(e);
                }
            }
            
            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                // Use theme-based text color
                if (styleManager != null)
                {
                    ThemeStyle theme = styleManager.Theme;
                    Color foreColor = PoisonPaint.ForeColor.Label.Normal(theme);
                    
                    // Draw text with theme color
                    TextRenderer.DrawText(e.Graphics, e.Text, e.TextFont, e.TextRectangle, foreColor, 
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.HidePrefix);
                }
                else
                {
                    base.OnRenderItemText(e);
                }
            }
        }
        
        private void UpdateAllMenusTheme(ThemeStyle theme)
        {
            if (poisonStyleManager == null) return;
            
            // Update all dropdown menus and context menus immediately with current theme
            var menus = new[]
            {
                platformMenu,
                gameMenu,
                injectGameMenu,
                settingsColorStyleMenu,
                mainFormContextMenu,
                settingsContextMenu
            };
            
            // Filter out null menus for batch operation
            var validMenus = menus.Where(m => m != null && poisonStyleManager != null).ToList();
            
            if (validMenus.Any())
            {
                // Use batch operation to apply StyleManager to all menus at once
                ReaLTaiizor.Extension.Poison.PoisonControlHelper.ApplyStyleManagerToControls(
                    poisonStyleManager, 
                    validMenus.Cast<Control>().ToArray()
                );
            }
            
            // Update renderers and invalidate individually (renderer logic is menu-specific)
            foreach (var menu in validMenus)
            {
                // Update renderer to use new style
                if (menu.Renderer is StyleBasedMenuRenderer renderer)
                {
                    renderer.UpdateStyleManager(poisonStyleManager);
                }
                else
                {
                    menu.Renderer = new StyleBasedMenuRenderer(poisonStyleManager);
                }
                
                // Force menu to refresh immediately
                menu.Invalidate();
            }
        }
        
        private void UpdateAllMenusStyle()
        {
            if (poisonStyleManager == null) return;
            
            // Update all dropdown menus and context menus immediately with current style
            var menus = new[]
            {
                platformMenu,
                gameMenu,
                injectGameMenu,
                settingsColorStyleMenu,
                mainFormContextMenu,
                settingsContextMenu
            };
            
            // Filter out null menus for batch operation
            var validMenus = menus.Where(m => m != null && poisonStyleManager != null).ToList();
            
            if (validMenus.Any())
            {
                // Use batch operation to apply StyleManager to all menus at once
                ReaLTaiizor.Extension.Poison.PoisonControlHelper.ApplyStyleManagerToControls(
                    poisonStyleManager, 
                    validMenus.Cast<Control>().ToArray()
                );
            }
            
            // Update renderers and invalidate individually (renderer logic is menu-specific)
            foreach (var menu in validMenus)
            {
                // Update renderer to use current style
                if (menu.Renderer is StyleBasedMenuRenderer renderer)
                {
                    renderer.UpdateStyleManager(poisonStyleManager);
                }
                else
                {
                    menu.Renderer = new StyleBasedMenuRenderer(poisonStyleManager);
                }
                
                // Force menu to refresh immediately
                menu.Invalidate();
            }
        }
        
        private void SetControlDisplayFocus(Control parent)
        {
            foreach (Control ctrl in parent.Controls)
            {
                // Enable DisplayFocus for Poison controls that support it
                if (ctrl is IPoisonControl)
                {
                    // Use reflection to set DisplayFocus if the property exists
                    var displayFocusProp = ctrl.GetType().GetProperty("DisplayFocus");
                    if (displayFocusProp?.CanWrite == true)
                    {
                        displayFocusProp.SetValue(ctrl, true, null);
                    }
                }
                
                // Recursively process child controls
                if (ctrl.HasChildren)
                {
                    SetControlDisplayFocus(ctrl);
                }
            }
        }
        
        private void EnhanceButtonInteractivity()
        {
            // List of important buttons that should have enhanced interactivity
            var importantButtons = new[]
            {
                btnCompile, btnInject, btnSelectProjectFolder, btnSelectOutputFile,
                btnSelectInjectFile,
                btnLogClear, btnLogCopy, btnLogSave,
                btnResetParseTree, btnLaunchBO3, btnKillBO3,
                btnSettingsBrowseOutputPath, btnSettingsColorStyle
            };
            
            foreach (var btn in importantButtons)
            {
                if (btn != null)
                {
                    // Ensure buttons are selectable and have proper styling
                    btn.UseSelectable = true;
                    
                    // Enable DisplayFocus for better keyboard navigation feedback
                    var displayFocusProp = btn.GetType().GetProperty("DisplayFocus");
                    if (displayFocusProp != null && displayFocusProp.CanWrite)
                    {
                        displayFocusProp.SetValue(btn, true, null);
                    }
                    
                    // Set appropriate font size and weight for important buttons
                    // Using reflection to set FontSize and FontWeight properties
                    try
                    {
                        var fontSizeProp = btn.GetType().GetProperty("FontSize");
                        var fontWeightProp = btn.GetType().GetProperty("FontWeight");
                        
                        if (fontSizeProp != null && fontSizeProp.CanWrite && fontSizeProp.PropertyType.IsEnum)
                        {
                            // Use Medium size for important buttons
                            var mediumValue = Enum.GetValues(fontSizeProp.PropertyType)
                                .Cast<object>()
                                .FirstOrDefault(v => v.ToString() == "Medium");
                            if (mediumValue != null)
                            {
                                fontSizeProp.SetValue(btn, mediumValue, null);
                            }
                        }
                        
                        if (fontWeightProp != null && fontWeightProp.CanWrite && fontWeightProp.PropertyType.IsEnum)
                        {
                            // Use Bold weight for important buttons
                            var boldValue = Enum.GetValues(fontWeightProp.PropertyType)
                                .Cast<object>()
                                .FirstOrDefault(v => v.ToString() == "Bold");
                            if (boldValue != null)
                            {
                                fontWeightProp.SetValue(btn, boldValue, null);
                            }
                        }
                    }
                    catch
                    {
                        // Ignore errors - font properties may not be available on all button types
                    }
                    
                    // Use helper to apply StyleManager and UseStyleColors
                    if (poisonStyleManager != null)
                    {
                        ReaLTaiizor.Extension.Poison.PoisonControlHelper.ApplyStyleManager(btn, poisonStyleManager);
                    }
                    
                    // Button effects (hover/pressed states, cursor) are handled by SetupAllButtonEffectsRecursive
                    // No need for manual cursor handling or state management here
                    // SetupAllButtonEffectsRecursive already handles:
                    // - Hover/pressed effects
                    // - Cursor changes
                    // - StyleManager connection
                    // - UseStyleColors
                }
            }
            
            // Dropdown buttons are already handled by SetupAllButtonEffectsRecursive
            // No need for separate setup here - all effects (hover, cursor, StyleManager) are handled centrally
        }
        
        private void btnGscConfMode_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtProjectFolder.Text) || !Directory.Exists(Helpers.PathHelper.ExpandPath(txtProjectFolder.Text)))
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, "Please select a project folder first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Always use the current project folder from txtProjectFolder.Text
            // This ensures it works correctly whether project was selected via browse or recent projects
            // txtProjectFolder.Text is updated by both btnSelectProjectFolder_Click and SwitchToProject
            string expandedProjectFolder = Helpers.PathHelper.ExpandPath(txtProjectFolder.Text);
            if (!Directory.Exists(expandedProjectFolder))
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, "Project folder does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            // Build gsc.conf path from current project folder
            // The GscConfEditorDialog will scan the entire project folder for symbols
            string gscConfPath = Path.Combine(expandedProjectFolder, "gsc.conf");
            
            // If file doesn't exist, create it with default content
            if (!File.Exists(gscConfPath))
            {
                try
                {
                    File.WriteAllText(gscConfPath, "symbols=MP\n");
                    AppendLogText($"[INFO] Created gsc.conf at: {gscConfPath}\r\n");
                }
                catch (Exception ex)
                {
                    ReaLTaiizor.Controls.PoisonMessageBox.Show(this, $"Failed to create gsc.conf:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            
            // Open the GSC Configuration Editor dialog
            // The dialog will scan the entire project folder for symbols when it opens
            // ScanProjectForSymbols() is called in the constructor and Shown event to ensure fresh scan
            try
            {
                using (var dialog = new Dialogs.GscConfEditorDialog(StyleManager, gscConfPath, this))
                {
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        AppendLogText($"[INFO] Updated gsc.conf with symbols: {dialog.SelectedSymbols}\r\n");
                        
                        // Reload gsc.conf settings to reflect changes made in the dialog
                        LoadGscConfSettings();
                        
                        // Notify code editor to reload symbols if it's open
                        if (codeEditorForm != null && !codeEditorForm.IsDisposed)
                        {
                            codeEditorForm.ReloadSymbolsFromGscConf();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, $"Failed to open GSC Configuration Editor:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private int GetPlatformIndex()
        {
            return currentPlatformIndex;
        }
        
        private int GetGameIndex()
        {
            return currentGameIndex;
        }
        
        /// <summary>
        /// Helper method to set button state, progress indicators, and operation flags
        /// </summary>
        private void SetButtonState(Control button, bool enabled, bool showProgress, ref bool stateFlag, bool otherOperationActive)
        {
            if (button != null)
            {
                button.Enabled = enabled;
            }
            stateFlag = !enabled;
            
            // Show/hide progress bar
            if (progressBar != null)
            {
                progressBar.Visible = showProgress && !enabled && !otherOperationActive;
                if (progressBar.Visible)
                {
                    progressBar.Value = 0; // Reset to 0 when starting
                }
            }
            
            if (progressSpinner != null)
            {
                // Show spinner when operation is active and showProgress is true
                bool shouldSpin = showProgress && !enabled;
                
                if (shouldSpin)
                {
                    // Make visible first
                    progressSpinner.Visible = true;
                    progressSpinner.BringToFront();
                    
                    // Ensure Value is -1 for indeterminate spinning animation
                    progressSpinner.Value = -1;
                    
                    // Start spinning - this enables the internal timer
                    progressSpinner.Spinning = true;
                    
                    // Force immediate repaint to start animation
                    progressSpinner.Invalidate();
                    progressSpinner.Update();
                    progressSpinner.Refresh();
                    
                    // Allow timer to start
                    Application.DoEvents();
                }
                else
                {
                    // Stop spinning first (disables timer)
                    progressSpinner.Spinning = false;
                    
                    // Then hide
                    progressSpinner.Visible = false;
                    
                    // Clear any pending updates
                    progressSpinner.Invalidate();
                }
            }
        }
        
        private void SetCompileState(bool enabled, bool showProgress = false)
        {
            SetButtonState(btnCompile, enabled, showProgress, ref isCompiling, isInjecting);
        }
        
        private void SetInjectState(bool enabled, bool showProgress = false)
        {
            SetButtonState(btnInject, enabled, showProgress, ref isInjecting, isCompiling);
        }
        
        private void btnSelectProjectFolder_Click(object sender, EventArgs e)
        {
            // Button state is automatically reset by PoisonButton.OnClick
            
            // Ensure we start from a directory, not a file path
            string initialPath = null;
            if (!string.IsNullOrWhiteSpace(txtProjectFolder.Text))
            {
                initialPath = Helpers.PathHelper.ExpandPath(txtProjectFolder.Text);
                if (File.Exists(initialPath))
                {
                    // If it's a file, use its directory
                    initialPath = Path.GetDirectoryName(initialPath);
                }
                else if (!Directory.Exists(initialPath))
                {
                    // If neither file nor directory exists, clear it
                    initialPath = null;
                }
            }
            
            string folder = Helpers.ModernFolderDialog.Show(this, "Select GSC Project Folder", initialPath);
            if (!string.IsNullOrEmpty(folder))
            {
                txtProjectFolder.Text = folder;
                AddToRecentProjects(folder); // Add to recent projects (use full path)
                // Highlight the selected project in recent projects dropdown
                UpdateRecentProjectsSelection(folder);
                // Output file will be loaded from gsc.conf via txtProjectFolder_TextChanged -> LoadGscConfSettings()
                UpdateUI();
            }
        }


        private void btnSelectOutputFile_Click(object sender, EventArgs e)
        {
            // Use SaveFileDialog to allow user to choose location and name for compiled script
            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Title = "Save Compiled Script";
                saveDialog.Filter = "GSC Compiled Files (*.gscc)|*.gscc|All Files (*.*)|*.*";
                saveDialog.FilterIndex = 1;
                saveDialog.DefaultExt = "gscc";
                
                // Determine initial path - priority:
                // 1. Directory from current output file if it exists and is valid
                // 2. Directory from default output path if it's set
                // 3. Build folder as fallback
                string initialDir = null;
                string initialFileName = "compiled.gscc";
                
                // First, try to use current output file path if it's valid
                if (!string.IsNullOrWhiteSpace(txtOutputFile.Text))
                {
                    string outputPath = txtOutputFile.Text.Trim();
                    
                    if (Directory.Exists(outputPath))
                    {
                        // It's a directory, use it directly
                        initialDir = outputPath;
                    }
                    else if (File.Exists(outputPath))
                    {
                        // File exists, get its directory
                        string currentDir = Path.GetDirectoryName(outputPath);
                        if (!string.IsNullOrWhiteSpace(currentDir) && Directory.Exists(currentDir))
                        {
                            initialDir = currentDir;
                            initialFileName = Path.GetFileName(outputPath);
                        }
                    }
                    else if (Path.IsPathRooted(outputPath))
                    {
                        // Path is rooted but file doesn't exist - get directory from path
                        string currentDir = Path.GetDirectoryName(outputPath);
                        if (!string.IsNullOrWhiteSpace(currentDir) && Directory.Exists(currentDir))
                        {
                            initialDir = currentDir;
                            initialFileName = Path.GetFileName(outputPath);
                        }
                    }
                }
                
                // If no valid directory from current output file, use default output path
                if (string.IsNullOrWhiteSpace(initialDir) && !string.IsNullOrWhiteSpace(defaultOutputPath))
                {
                    string defaultPath = defaultOutputPath.Trim();
                    
                    if (Directory.Exists(defaultPath))
                    {
                        initialDir = defaultPath;
                    }
                    else if (File.Exists(defaultPath))
                    {
                        string defaultDir = Path.GetDirectoryName(defaultPath);
                        if (!string.IsNullOrWhiteSpace(defaultDir) && Directory.Exists(defaultDir))
                        {
                            initialDir = defaultDir;
                            initialFileName = Path.GetFileName(defaultPath);
                        }
                    }
                    else if (Path.IsPathRooted(defaultPath))
                    {
                        string defaultDir = Path.GetDirectoryName(defaultPath);
                        if (!string.IsNullOrWhiteSpace(defaultDir) && Directory.Exists(defaultDir))
                        {
                            initialDir = defaultDir;
                            string defaultFileName = Path.GetFileName(defaultPath);
                            if (!string.IsNullOrWhiteSpace(defaultFileName))
                            {
                                initialFileName = defaultFileName;
                            }
                        }
                    }
                }
                
                // Final fallback to build folder
                if (string.IsNullOrWhiteSpace(initialDir) || !Directory.Exists(initialDir))
                {
                    initialDir = buildFolder;
                }
                
                saveDialog.InitialDirectory = initialDir;
                saveDialog.FileName = initialFileName;
                
                if (saveDialog.ShowDialog(this) == DialogResult.OK)
                {
                    txtOutputFile.Text = saveDialog.FileName;
                    UpdateUI();
                }
            }
        }
        

        private void UpdateUI()
        {
            bool hasProjectFolder = !string.IsNullOrWhiteSpace(txtProjectFolder.Text);
            bool hasOutputFile = !string.IsNullOrWhiteSpace(txtOutputFile.Text);
            bool folderExists = hasProjectFolder && Directory.Exists(Helpers.PathHelper.ExpandPath(txtProjectFolder.Text));
            
            // Check if we can compile (all conditions met AND not currently compiling or injecting)
            bool canCompile = hasProjectFolder && 
                             hasOutputFile && 
                             folderExists &&
                             !isCompiling && 
                             !isInjecting; // Don't enable if currently compiling or injecting
            
            // Directly set button state - don't use SetCompileState here to avoid circular dependency
            btnCompile.Enabled = canCompile;
            
            // Update progress spinner (hide it when not compiling or injecting)
            if (progressSpinner != null && !isCompiling && !isInjecting)
            {
                progressSpinner.Visible = false;
                progressSpinner.Spinning = false;
            }
            
            btnGscConfMode.Enabled = hasProjectFolder && folderExists;
            
            // Check if we can inject (all conditions met AND game is running AND not currently compiling or injecting)
            bool hasInjectFile = !string.IsNullOrWhiteSpace(txtInjectFile.Text) && File.Exists(txtInjectFile.Text);
            bool hasInjectPath = !string.IsNullOrWhiteSpace(txtInjectPath.Text);
            
            // Determine which game we're targeting
            TreyarchCompiler.Enums.Games targetGame = TreyarchCompiler.Enums.Games.T7;
            if (currentInjectGameIndex == 1)
                targetGame = TreyarchCompiler.Enums.Games.T8;
            
            // Check if game is running
            bool gameRunning = IsGameRunning(targetGame);
            
            bool canInject = hasInjectFile && 
                            hasInjectPath &&
                            gameRunning &&
                            !isCompiling &&
                            !isInjecting;
            
            btnInject.Enabled = canInject;
            
            // Update tooltip to inform user if game isn't running
            if (poisonToolTip != null)
            {
                if (!gameRunning)
                {
                    poisonToolTip.SetToolTip(btnInject, $"Inject compiled script into game (Game is not running - start {targetGame} first)");
                }
                else if (!hasInjectFile || !hasInjectPath)
                {
                    poisonToolTip.SetToolTip(btnInject, "Inject compiled script into game");
                }
                else
                {
                    poisonToolTip.SetToolTip(btnInject, $"Inject compiled script into game ({targetGame} is running)");
                }
            }
        }

        private void btnCodeEditor_Click(object sender, EventArgs e)
        {
            // Button state is automatically reset by PoisonButton.OnClick
            
            try
            {
                Forms.CodeEditorForm editorForm = new Forms.CodeEditorForm(poisonStyleManager);
                codeEditorForm = editorForm; // Store reference for reloading symbols
                editorForm.FormClosed += (s, args) =>
                {
                    // Bring focus back to MainForm when CodeEditorForm closes
                    this.Activate();
                    this.Focus();
                    this.BringToFront();
                    codeEditorForm = null; // Clear reference when closed
                };
                editorForm.Show();
                editorForm.BringToFront();
                editorForm.Activate();
            }
            catch (Exception ex)
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, 
                    $"Error opening code editor: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}", 
                    "Error", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
            }
        }

        // Public method for CodeEditorForm to use MainForm's compile logic
        public void CompileProject(string projectPath, string outputPath, TreyarchCompiler.Enums.Games game, TreyarchCompiler.Enums.Modes mode, List<string> symbols = null)
        {
            if (string.IsNullOrWhiteSpace(projectPath) || string.IsNullOrWhiteSpace(outputPath))
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, "Please provide a project folder and output file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!Directory.Exists(projectPath))
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, "Project folder does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Set the project folder and output file
            if (txtProjectFolder != null)
                txtProjectFolder.Text = projectPath;
            if (txtOutputFile != null)
                txtOutputFile.Text = outputPath;
            
            // Add to recent projects when project is set from CodeEditorForm
            AddToRecentProjects(projectPath);
            // Highlight the selected project in recent projects dropdown
            UpdateRecentProjectsSelection(projectPath);

            // Set the game index
            if (game == TreyarchCompiler.Enums.Games.T7)
                currentGameIndex = 0; // T7
            else
                currentGameIndex = 1; // T8
            
            // Update the game button text
            if (btnGame != null)
                btnGame.Text = (currentGameIndex == 0) ? "T7 (BO3)" : "T8 (BO4)";

            // Store symbols for btnCompile_Click to use
            compileSymbolsFromEditor = symbols;

            // Show and activate MainForm
            Show();
            BringToFront();
            Activate();

            // Switch to compile tab
            if (tabControl != null && tabCompile != null)
            {
                tabControl.SelectedTab = tabCompile;
            }

            // Trigger compile by calling the button click handler
            btnCompile_Click(btnCompile, EventArgs.Empty);
        }

        // Public method for CodeEditorForm to use MainForm's inject logic
        public void InjectPrecompiledScript(string filePath, TreyarchCompiler.Enums.Games game, string replacePath = null)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, "Please select a valid compiled file to inject.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Set the inject file
            if (txtInjectFile != null)
            {
                txtInjectFile.Text = filePath;
                // Clear selection immediately to prevent text from being highlighted
                ClearInjectFileSelection();
            }

            // Set the replace path if provided
            if (!string.IsNullOrWhiteSpace(replacePath) && txtInjectPath != null)
                txtInjectPath.Text = replacePath;

            // Set the game
            if (game == TreyarchCompiler.Enums.Games.T7)
                currentInjectGameIndex = 0; // T7
            else
                currentInjectGameIndex = 1; // T8

            // Show and activate MainForm
            Show();
            BringToFront();
            Activate();

            // Switch to inject tab only if game is running
            if (tabControl != null && tabInject != null)
            {
                // Use the game parameter that was passed to this method
                // Only switch if game is running
                if (IsGameRunning(game))
                {
                    tabControl.SelectedTab = tabInject;
                    // Trigger inject by calling the button click handler
                    btnInject_Click(btnInject, EventArgs.Empty);
                }
                // Removed auto-switch message - user can manually switch to inject tab when ready
            }
        }

        private void btnCompile_Click(object sender, EventArgs e)
        {
            
            if (string.IsNullOrWhiteSpace(txtProjectFolder.Text) || string.IsNullOrWhiteSpace(txtOutputFile.Text))
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, "Please select a project folder and output file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string expandedProjectFolder = Helpers.PathHelper.ExpandPath(txtProjectFolder.Text);
            if (!Directory.Exists(expandedProjectFolder))
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, "Project folder does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Check for database file (T7PCV2.db or T7PS4V2.db) - only in exe directory
            string exeDir = Application.StartupPath;
            string dbPath = Path.Combine(exeDir, "T7PCV2.db");
            if (!File.Exists(dbPath))
            {
                dbPath = Path.Combine(exeDir, "T7PS4V2.db");
                if (!File.Exists(dbPath))
                {
                    ReaLTaiizor.Controls.PoisonMessageBox.Show(this, $"Database file not found!\n\nChecked location:\n- {exeDir}\n\nPlease ensure T7PCV2.db or T7PS4V2.db exists in the executable directory.", 
                                        "Database Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    return;
                }
            }

            SetCompileState(false, true); // Disable button and show progress
            txtLog.Clear();
            AppendLogText("Starting compilation...\r\n");
            Application.DoEvents();

            // Check if BO3 is running (only for T7/BO3)
            TreyarchCompiler.Enums.Games game = TreyarchCompiler.Enums.Games.T7;
            if (GetGameIndex() == 1)
                game = TreyarchCompiler.Enums.Games.T8;
            bool isT7 = game == TreyarchCompiler.Enums.Games.T7;
            
            // Track compilation success - don't show launch prompt before compilation
            // It will be shown after successful compilation instead

            try
            {

                List<string> conditionalSymbols = new List<string>();
                // Use expandedProjectFolder already declared above
                string scriptLocation = expandedProjectFolder;
                
                // If symbols were provided from CodeEditorForm, use them instead of reading from gsc.conf
                bool useProvidedSymbols = (compileSymbolsFromEditor != null && compileSymbolsFromEditor.Count > 0);
                
                if (useProvidedSymbols)
                {
                    // Use symbols provided from CodeEditorForm (includes mode and custom symbols)
                    conditionalSymbols.AddRange(compileSymbolsFromEditor);
                    AppendLogText($"Using symbols from Code Editor: {string.Join(", ", compileSymbolsFromEditor)}\r\n");
                    Application.DoEvents();
                    // Clear the stored symbols after use
                    compileSymbolsFromEditor = null;
                }
                
                // Check for gsc.conf in project folder first, then current directory (matching debug compiler behavior)
                string gscConfPath = Path.Combine(expandedProjectFolder, "gsc.conf");
                if (!File.Exists(gscConfPath))
                    gscConfPath = "gsc.conf"; // Fallback to current directory (like debug compiler)
                
                // Use GscConfParser to parse the configuration file
                var settings = GscConfParser.Parse(gscConfPath);
                
                if (settings != null)
                {
                    // Show full path
                    AppendLogText($"Found gsc.conf at: {gscConfPath}\r\n");
                    Application.DoEvents();
                    
                    // Only read symbols from gsc.conf if not provided from CodeEditorForm
                    if (!useProvidedSymbols && settings.Symbols.Count > 0)
                    {
                        // Add symbols from gsc.conf (including custom symbols)
                        foreach (string symbol in settings.Symbols)
                        {
                            if (!conditionalSymbols.Contains(symbol, StringComparer.OrdinalIgnoreCase))
                            {
                                conditionalSymbols.Add(symbol);
                            }
                        }
                    }
                    
                    // Apply scriptlocation
                    if (!string.IsNullOrWhiteSpace(settings.ScriptLocation))
                    {
                        scriptLocation = GscConfParser.ResolveScriptLocation(settings, expandedProjectFolder);
                        AppendLogText($"Using scriptlocation: {scriptLocation}\r\n");
                    }
                    
                    // Apply script (injection path)
                    if (!string.IsNullOrWhiteSpace(settings.Script) && txtInjectPath != null)
                    {
                        txtInjectPath.Text = settings.Script;
                        AppendLogText($"Injection path set from gsc.conf: {settings.Script}\r\n");
                    }
                    
                    // Apply file (output filename)
                    if (!string.IsNullOrWhiteSpace(settings.File) && txtOutputFile != null)
                    {
                        string resolvedOutputFile = GscConfParser.ResolveOutputFile(settings, expandedProjectFolder, buildFolder);
                        if (!string.IsNullOrWhiteSpace(resolvedOutputFile))
                        {
                            txtOutputFile.Text = resolvedOutputFile;
                            AppendLogText($"Output file set from gsc.conf: {settings.File}\r\n");
                        }
                    }
                    
                    // Apply noruntime
                    if (settings.NoRuntime.HasValue && chkNoRuntime != null)
                    {
                        chkNoRuntime.Checked = settings.NoRuntime.Value;
                        AppendLogText($"No runtime set from gsc.conf: {settings.NoRuntime.Value}\r\n");
                    }
                    
                    // Apply hot reload mode
                    if (settings.Hot.HasValue)
                    {
                        currentHotMode = settings.Hot.Value;
                        UpdateHotReloadButton();
                        AppendLogText($"Hot reload mode set from gsc.conf: {settings.Hot.Value}\r\n");
                    }
                    
                    // Apply game
                    if (settings.Game.HasValue)
                    {
                        game = settings.Game.Value;
                        AppendLogText($"Game set from gsc.conf: {game}\r\n");
                    }
                }
                else
                {
                    AppendLogText($"No gsc.conf found at: {gscConfPath}\r\n");
                    AppendLogText("Using default symbols only.\r\n");
                    Application.DoEvents();
                }

                // BO3/BO4 symbol will be added after creating ConditionalBlocks (matching debug compiler order)

                if (!Directory.Exists(scriptLocation))
                {
                    AppendLogText($"ERROR: Script location does not exist: {scriptLocation}\r\n");
                    SetCompileState(true, false);
                    return;
                }

                // Use CompilationService to collect GSC files
                var gscFiles = Services.CompilationService.CollectGscFiles(scriptLocation, AppendLogText);
                
                // Show symbols and file count in one line
                string symbolsStr = conditionalSymbols.Count > 0 ? string.Join(", ", conditionalSymbols) : "none";
                AppendLogText($"Symbols: {symbolsStr} | Files: {gscFiles.Count}\r\n");
                Application.DoEvents();

                if (gscFiles.Count == 0)
                {
                    ReaLTaiizor.Controls.PoisonMessageBox.Show(this, $"No .gsc files found in:\n{scriptLocation}", "No GSC Files", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    SetCompileState(true, false);
                    return;
                }

                // Use CompilationService to process source files
                var (source, sourceTokens) = Services.CompilationService.ProcessSourceFiles(gscFiles, scriptLocation, AppendLogText);
                Application.DoEvents();

                // Use CompilationService to process conditionals
                var (processedSource, conditionalError) = Services.CompilationService.ProcessConditionals(source, conditionalSymbols, isT7, AppendLogText);
                
                if (conditionalError != null)
                {
                    // Use CompilationErrorParser to parse and format preprocessor error
                    var error = Services.CompilationErrorParser.ParsePreprocessorError(
                        conditionalError, sourceTokens, scriptLocation, source);
                    Services.CompilationErrorParser.FormatPreprocessorError(error, AppendLogText);
                    
                    SetCompileState(true, false);
                    return;
                }
                
                source = processedSource;
                Application.DoEvents();

                // Match debug compiler: always use Modes.MP and false for masking
                // Mode-specific code is handled by #ifdef directives in the source, not by the compile mode parameter
                Modes compileMode = Modes.MP;
                bool useMasking = false;
                
                Platforms platform = Platforms.PC;
                int platformIndex = GetPlatformIndex();
                switch (platformIndex)
                {
                    case 0: platform = Platforms.PC; break; // PC (index 0)
                    case 1: platform = Platforms.PS4; break; // PS4 (index 1)
                    default: platform = Platforms.PC; break;
                }

                // Log platform selection and expected database file
                string expectedDbFile = (platform == Platforms.PS4) ? "T7PS4V2.db" : "t7pcv2.db";
                string expectedDbPath = Path.Combine(exeDir, expectedDbFile);
                bool dbExists = File.Exists(expectedDbPath);
                
                // Symbols already logged above with file count, no need to log again
                AppendLogText($"Compiling with Platform: {platform}, Game: {game}, Mode: {compileMode}, Masking: {useMasking}\r\n");
                AppendLogText($"Expected database: {expectedDbFile}\r\n");
                AppendLogText($"Database path: {expectedDbPath}\r\n");
                AppendLogText($"Database exists: {dbExists}\r\n");
                if (!dbExists)
                {
                    AppendLogText($"WARNING: {expectedDbFile} not found! Compilation may fail.\r\n");
                }
                Application.DoEvents();

                
                UpdateProgress(10);
                
                CompiledCode code = Compiler.Compile(platform, game, compileMode, useMasking, source);
                
                UpdateProgress(90);

                if (code == null)
                {
                    // Use CompilationErrorParser to format null result error
                    var error = new Models.CompilationError
                    {
                        Type = Models.CompilationErrorType.CompilerNullResult,
                        Message = "Compiler returned null result."
                    };
                    Services.CompilationErrorParser.FormatAndLogError(error, AppendLogText);
                    
                    SetCompileState(true, false);
                    return;
                }

                if (code.Error != null && !string.IsNullOrWhiteSpace(code.Error))
                {
                    // Use CompilationErrorParser to parse and format compiler error
                    var error = Services.CompilationErrorParser.ParseCompilerError(
                        code.Error, sourceTokens, scriptLocation);
                    Services.CompilationErrorParser.FormatAndLogError(error, AppendLogText);
                    
                    SetCompileState(true, false);
                    return;
                }

                // Save compiled file to build folder
                // Use the directory from the selected output file path, or fallback to default buildFolder
                string outputDirectory = buildFolder;
                string outputFileName = Path.GetFileName(txtOutputFile.Text);
                
                // Expand path first to handle %USERPROFILE% and other environment variables
                if (!string.IsNullOrWhiteSpace(txtOutputFile.Text))
                {
                    string expandedOutputFile = Helpers.PathHelper.ExpandPath(txtOutputFile.Text);
                
                    // If txtOutputFile contains a full path, use its directory
                    if (Path.IsPathRooted(expandedOutputFile))
                    {
                        string selectedDir = Path.GetDirectoryName(expandedOutputFile);
                        if (!string.IsNullOrWhiteSpace(selectedDir))
                        {
                            outputDirectory = selectedDir;
                        }
                        // Get filename from expanded path
                        outputFileName = Path.GetFileName(expandedOutputFile);
                    }
                }
                
                // Use CompilationService to ensure output directory exists
                outputDirectory = Services.CompilationService.EnsureOutputDirectory(outputDirectory, buildFolder, AppendLogText);
                if (outputDirectory != buildFolder)
                {
                    // Update buildFolder for future use if it changed
                    buildFolder = outputDirectory;
                }
                
                // Update progress bar before writing file
                UpdateProgress(95);
                
                // Use CompilationService to save compilation output
                Services.CompilationService.SaveCompilationOutput(
                    code,
                    outputDirectory,
                    outputFileName,
                    scriptLocation,
                    expandedProjectFolder,
                    chkSaveOpcodeMap.Checked,
                    AppendLogText);
                
                // Get the output path for UI updates
                string baseName = Path.GetFileNameWithoutExtension(outputFileName);
                if (string.IsNullOrWhiteSpace(baseName))
                    baseName = "compiled";
                string extension = code.RequiresGSI ? ".gsc" : ".gscc";
                string finalFileName = baseName + extension;
                string outputPath = Path.Combine(outputDirectory, finalFileName);
                
                // Update progress bar to 100% after successful compilation
                UpdateProgress(100);

                // Auto-populate inject file field with the compiled output
                txtInjectFile.Text = outputPath;
                // Clear selection immediately to prevent text from being highlighted
                ClearInjectFileSelection();
                AddToRecentFiles(outputPath); // Add to recent files
                UpdateUI();
                
                // Switch to inject tab automatically on successful compilation (only if game is running)
                if (tabControl != null && tabInject != null)
                {
                    // Use the game parameter from CompileProject method
                    // Only switch if game is running
                    if (IsGameRunning(game))
                    {
                        tabControl.SelectedTab = tabInject;
                        
                        // Clear selection and reset button hover state after tab switch completes
                        // Use SafeInvoke to ensure this happens after the tab switch UI update with error handling
                        ReaLTaiizorExt.PoisonFormHelper.SafeInvoke(this, () =>
                        {
                            if (txtInjectFile != null)
                            {
                                txtInjectFile.SelectionStart = txtInjectFile.Text.Length;
                                txtInjectFile.SelectionLength = 0;
                            }
                            
                            // Reset hover state of browse button
                            if (btnSelectInjectFile != null)
                            {
                                btnSelectInjectFile.Invalidate();
                            }
                        });
                    }
                    // Removed auto-switch message - user can manually switch to inject tab when ready
                }

                // Save last project folder (saved via SaveAllSettings)
                SaveAllSettings();
                
                // Show launch prompt AFTER successful compilation (only if BO3 is not running)
                if (isT7 && !IsGameRunning(TreyarchCompiler.Enums.Games.T7))
                {
                    // Show non-interrupting task window to ask if user wants to launch BO3
                    ShowLaunchGameTaskWindow();
                }
                
                // Success message already logged to txtLog, no need for MessageBox
            }
            catch (Exception ex)
            {
                AppendLogText($"\r\n=== UNEXPECTED ERROR ===\r\n");
                AppendLogText($"Error: {ex.Message}\r\n");
                if (!string.IsNullOrEmpty(ex.StackTrace))
                    AppendLogText($"\r\nStack Trace:\r\n{ex.StackTrace}\r\n");
                AppendLogText($"\r\n");
            }
            finally
            {
                SetCompileState(true, false);
                UpdateUI();
                // Reset button state to prevent stuck hover state
                ReaLTaiizorExt.PoisonControlHelper.ResetButtonState(sender as Control);
            }
        }

        private void txtProjectFolder_TextChanged(object sender, EventArgs e)
        {
            // Load gsc.conf settings when project folder changes (including output file)
            // This is called for both:
            // 1. Browse button: btnSelectProjectFolder_Click sets txtProjectFolder.Text
            // 2. Recent projects: SwitchToProject sets txtProjectFolder.Text
            // Both trigger this event which loads gsc.conf from the new project folder
            LoadGscConfSettings();
            
            // Auto-populate output file if needed (same as browse button flow)
            // This ensures output file is set up correctly when project folder changes
            AutoPopulateOutputFile();
            
            // Position caret at the end to show the right side (end) of the path so the project folder name is visible
            if (txtProjectFolder != null && !string.IsNullOrEmpty(txtProjectFolder.Text))
            {
                // Set selection to end of text - this positions the view to show the right side
                // Use BeginInvoke to ensure it happens after the text is fully set
                this.BeginInvoke(new Action(() =>
                {
                    if (txtProjectFolder != null && !string.IsNullOrEmpty(txtProjectFolder.Text))
                    {
                        txtProjectFolder.SelectionStart = txtProjectFolder.Text.Length;
                        txtProjectFolder.SelectionLength = 0;
                    }
                }));
            }
            
            // Don't update selection if we're in the middle of a programmatic update from user selection
            // This prevents interference with the dropdown selection
            if (!isUpdatingProjectFromSelection && !string.IsNullOrWhiteSpace(txtProjectFolder?.Text))
            {
                UpdateRecentProjectsSelection(txtProjectFolder.Text);
            }
            
            // Update UI state (output file already loaded from gsc.conf or auto-populated above)
            UpdateUI();
        }
        
        /// <summary>
        /// Loads settings from gsc.conf file in the current project folder
        /// Always does a fresh read of gsc.conf when a project is loaded
        /// </summary>
        private void LoadGscConfSettings()
        {
            if (string.IsNullOrWhiteSpace(txtProjectFolder?.Text))
                return;
            
            string expandedProjectFolder = Helpers.PathHelper.ExpandPath(txtProjectFolder.Text);
            if (string.IsNullOrEmpty(expandedProjectFolder) || !Directory.Exists(expandedProjectFolder))
                return;
            
            string gscConfPath = Path.Combine(expandedProjectFolder, "gsc.conf");
            if (!File.Exists(gscConfPath))
                return; // No gsc.conf found, use defaults
            
            // Use GscConfParser to parse the configuration file
            var settings = GscConfParser.Parse(gscConfPath);
            if (settings == null)
                return;
            
            try
            {
                // Apply script (injection path)
                if (!string.IsNullOrWhiteSpace(settings.Script) && txtInjectPath != null)
                {
                    txtInjectPath.Text = settings.Script;
                }
                
                // Apply file (output filename) - always load from gsc.conf when project is loaded (fresh read)
                if (!string.IsNullOrWhiteSpace(settings.File) && txtOutputFile != null)
                {
                    string resolvedOutputFile = GscConfParser.ResolveOutputFile(settings, expandedProjectFolder, buildFolder);
                    if (!string.IsNullOrWhiteSpace(resolvedOutputFile))
                    {
                        txtOutputFile.Text = resolvedOutputFile;
                        AppendLogText($"Output file loaded from gsc.conf: {settings.File}\r\n");
                    }
                }
                
                // Apply noruntime
                if (settings.NoRuntime.HasValue && chkNoRuntime != null)
                {
                    chkNoRuntime.Checked = settings.NoRuntime.Value;
                }
                
                // Apply hot reload mode
                if (settings.Hot.HasValue)
                {
                    currentHotMode = settings.Hot.Value;
                    // Update hot reload button text
                    if (btnHotReload != null)
                    {
                        btnHotReload.Text = $"Hot Reload: {settings.Hot.Value}";
                        if (btnHotReload.BehaveLikeComboBox)
                        {
                            int hotIndex = settings.Hot.Value == HotMode.None ? 0 : (settings.Hot.Value == HotMode.Csc ? 1 : 2);
                            btnHotReload.SelectedIndex = hotIndex;
                        }
                    }
                }
                
                // Apply game
                if (settings.Game.HasValue)
                {
                    // Update game index and button text
                    currentGameIndex = (settings.Game.Value == TreyarchCompiler.Enums.Games.T7) ? 0 : 1;
                    if (btnGame != null)
                    {
                        btnGame.Text = (settings.Game.Value == TreyarchCompiler.Enums.Games.T7) ? "T7 (BO3)" : "T8 (BO4)";
                        if (btnGame.BehaveLikeComboBox)
                            btnGame.SelectedIndex = currentGameIndex;
                    }
                }
            }
            catch (Exception ex)
            {
                // Silently fail - gsc.conf loading errors shouldn't break the UI
                System.Diagnostics.Debug.WriteLine($"Error applying gsc.conf settings: {ex.Message}");
            }
        }

        private void txtOutputFile_TextChanged(object sender, EventArgs e)
        {
            UpdateUI();
        }
        
        private void txtOutputFile_DoubleClick(object sender, EventArgs e)
        {
            // Open the file dialog when double-clicking the output file text box
            btnSelectOutputFile_Click(sender, e);
        }
        
        private void txtDefaultOutputPath_TextChanged(object sender, EventArgs e)
        {
            // Don't auto-populate output file - keep it blank until user explicitly sets a path
            // The default output path setting is just a preference, not an auto-population trigger
        }

        private void btnSelectInjectFile_Click(object sender, EventArgs e)
        {
            // Button state is automatically reset by PoisonButton.OnClick
            
            try
            {
                // Check if form is disposed
                if (this.IsDisposed || this.Disposing)
                    return;
                
            // Use standard OpenFileDialog for file selection
            string initialPath = null;
                if (txtInjectFile != null && !string.IsNullOrWhiteSpace(txtInjectFile.Text) && File.Exists(txtInjectFile.Text))
                initialPath = Path.GetDirectoryName(txtInjectFile.Text);
                
            string selectedFile = null;
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Select Compiled GSC File to Inject";
                dialog.Filter = "GSC Compiled Files (*.gscc)|*.gscc|GSC Injection Files (*.gsc)|*.gsc|All Files (*.*)|*.*";
                dialog.FilterIndex = 1;
                dialog.RestoreDirectory = true;
                if (!string.IsNullOrEmpty(initialPath) && Directory.Exists(initialPath))
                {
                    dialog.InitialDirectory = initialPath;
                }
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    selectedFile = dialog.FileName;
                }
            }
            
            if (!string.IsNullOrEmpty(selectedFile))
                {
                    if (txtInjectFile != null && !this.IsDisposed && !this.Disposing)
                {
                txtInjectFile.Text = selectedFile;
                // Clear selection immediately to prevent text from being highlighted
                ClearInjectFileSelection();
                AddToRecentFiles(selectedFile); // Add to recent files
                    UpdateUI();
                }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in btnSelectInjectFile_Click: {ex.Message}");
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this,
                    $"Error opening file dialog: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
            // Button state is automatically reset by PoisonButton.OnClick
                if (!this.IsDisposed && !this.Disposing)
                {
                    // No need to manually reset - OnClick already handles it
                }
            }
        }

        private void btnInject_Click(object sender, EventArgs e)
        {
            // Button state is automatically reset by PoisonButton.OnClick
            
            // Determine which game we're targeting
            TreyarchCompiler.Enums.Games game = TreyarchCompiler.Enums.Games.T7;
            if (currentInjectGameIndex == 1)
                game = TreyarchCompiler.Enums.Games.T8;
            
            // Check if game is running FIRST - this is required for injection
            if (!IsGameRunning(game))
            {
                Services.InjectionService.FormatInjectionError(
                    $"Game ({game}) is not running.\r\nPlease start the game before attempting to inject.", 
                    AppendLogText);
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
                SetInjectState(true, false);
                return;
            }
            
            // Use InjectionService to validate input
            var input = new Models.InjectionInput
            {
                InjectFile = txtInjectFile.Text,
                ReplacePath = txtInjectPath.Text,
                Game = game,
                NoRuntime = chkNoRuntime.Checked,
                HotMode = currentHotMode
            };

            var (isValid, errorMessage) = Services.InjectionService.ValidateInput(input);
            if (!isValid)
            {
                Services.InjectionService.FormatInjectionError(errorMessage, AppendLogText);
                ScrollLogToEnd();
                return;
            }

            SetInjectState(false, true); // Disable button and show progress
            
            // Use InjectionService to log injection start
            Services.InjectionService.LogInjectionStart(input, AppendLogText);
            
            UpdateProgress(10);
            Application.DoEvents();

            try
            {
                UpdateProgress(30);
                
                // Use InjectionService to validate and read file
                var (fileValid, fileError, buffer) = Services.InjectionService.ValidateAndReadFile(input.InjectFile, AppendLogText);
                if (!fileValid)
                {
                    Services.InjectionService.FormatInjectionError(fileError, AppendLogText);
                    SetInjectState(true, false);
                    ScrollLogToEnd();
                    return;
                }

                UpdateProgress(60);
                
                // Implement injection (kept in MainForm due to tight coupling with instance state)
                int result = InjectScript(input.ReplacePath, buffer, input.Game, input.NoRuntime, input.HotMode);
                
                UpdateProgress(90);
                
                // Use InjectionService to format result
                var injectionResult = result == 0
                    ? Models.InjectionResult.CreateSuccess(game, input.ReplacePath)
                    : Models.InjectionResult.CreateFailure(result);
                
                Services.InjectionService.FormatInjectionResult(injectionResult, AppendLogText);
                
                if (result == 0)
                {
                    LastGameInjected = game; // Track which game was injected
                    // Save injection state for persistence (after all variables are set)
                    SaveInjectionState(input.ReplacePath, game);
                    
                    // Re-enable reset button after successful injection (may need to reset again)
                    isParseTreeReset = false;
                    UpdateResetParseTreeButton();
                }
                
            }
            catch (Exception ex)
            {
                Services.InjectionService.FormatInjectionError(
                    $"Error: {ex.Message}\r\n" + 
                    (!string.IsNullOrEmpty(ex.StackTrace) ? $"\r\nStack Trace:\r\n{ex.StackTrace}\r\n" : ""), 
                    AppendLogText);
            }
            finally
            {
                SetInjectState(true, false);
                ScrollLogToEnd();
                // Reset button state to prevent stuck hover state
                ReaLTaiizorExt.PoisonControlHelper.ResetButtonState(sender as Control);
            }
        }

        private void txtInjectFile_TextChanged(object sender, EventArgs e)
        {
            UpdateUI();
        }

        private void txtInjectPath_TextChanged(object sender, EventArgs e)
        {
            UpdateUI();
        }
        
        // Check if the game process is running - optimized for smooth operation
        private bool IsGameRunning(TreyarchCompiler.Enums.Games game)
        {
            // Use static arrays to avoid allocations on every call
            string[] processNames = game == TreyarchCompiler.Enums.Games.T7 ? T7ProcessNames : T8ProcessNames;
            
            // Check most common process name first (usually "blackops3" or "blackops4")
            try
            {
                Process[] processes = Process.GetProcessesByName(processNames[0]);
                if (processes != null && processes.Length > 0)
                {
                    // Found it - dispose and return immediately
                    foreach (Process proc in processes)
                    {
                        try { proc.Dispose(); } catch { }
                    }
                    return true;
                }
            }
            catch (Win32Exception)
            {
                // Access denied - try alternative name
            }
            catch
            {
                // Other exceptions - try alternative name
            }
            
            // If first check failed, try alternative casing (usually not needed, but for compatibility)
            if (processNames.Length > 1)
            {
                try
                {
                    Process[] processes = Process.GetProcessesByName(processNames[1]);
                    if (processes != null && processes.Length > 0)
                    {
                        foreach (Process proc in processes)
                        {
                            try { proc.Dispose(); } catch { }
                        }
                        return true;
                    }
                }
                catch
                {
                    // Ignore exceptions - process not found
                }
            }
            
            return false;
        }
        
        // Get ProcessEx for a game - optimized for smooth operation
        private ProcessEx GetGameProcessEx(TreyarchCompiler.Enums.Games game)
        {
            try
            {
                // Use static arrays - check most common name first
                string[] processNames = game == TreyarchCompiler.Enums.Games.T7 ? T7ProcessNames : T8ProcessNames;
                
                // Try first (most common) process name
                try
                {
                    ProcessEx processEx = processNames[0];
                    if (processEx != null)
                    {
                        return processEx;
                    }
                }
                catch { }
                
                // Try alternative if available
                if (processNames.Length > 1)
                {
                    try
                    {
                        ProcessEx processEx = processNames[1];
                        if (processEx != null)
                        {
                            return processEx;
                        }
                    }
                    catch { }
                }
            }
            catch { }
            
            return null;
        }
        
        #endregion
        
        #region Compilation Logic
        
        
        #endregion
        
        #region Injection Logic
        
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct T7SPT
        {
            public PointerEx llpName;
            public int BuffSize;
            public int Pad;
            public PointerEx lpBuffer;
        }

        private class GSICInfo
        {
            public List<T7ScriptObject.ScriptDetour> Detours = new List<T7ScriptObject.ScriptDetour>();

            public byte[] PackDetours()
            {
                List<byte> data = new List<byte>();
                foreach (var detour in Detours)
                {
                    data.AddRange(detour.Serialize());
                }
                return data.ToArray();
            }
        }

        private int InjectScript(string replacePath, byte[] buffer, TreyarchCompiler.Enums.Games game, bool noruntime, HotMode hot = HotMode.None)
        {
            if (game != TreyarchCompiler.Enums.Games.T7)
            {
                AppendLogText("T8 injection not yet implemented in GUI.\r\n");
                return -1;
            }

            try
            {
                GSICInfo gsi = null;
                
                // Check for GSIC file (with detours)
                if (BitConverter.ToInt64(buffer, 0) != 0x1C000A0D43534780)
                {
                    string preamble = Encoding.ASCII.GetString(buffer.Take(4).ToArray());
                    if (preamble != "GSIC")
                    {
                        AppendLogText("ERROR: Script is not a valid compiled script.\r\n");
                        return -1;
                    }
                    
                    // Parse GSIC detours
                    using (MemoryStream ms = new MemoryStream(buffer))
                    using (BinaryReader reader = new BinaryReader(ms))
                    {
                        T7ScriptObject.GSIFields currentField = T7ScriptObject.GSIFields.Detours;
                        reader.BaseStream.Position += 4;
                        gsi = new GSICInfo();
                        for (int numFields = reader.ReadInt32(); numFields > 0; numFields--)
                        {
                            currentField = (T7ScriptObject.GSIFields)reader.ReadInt32();
                            switch (currentField)
                            {
                                case T7ScriptObject.GSIFields.Detours:
                                    int numdetours = reader.ReadInt32();
                                    for (int j = 0; j < numdetours; j++)
                                    {
                                        T7ScriptObject.ScriptDetour detour = new T7ScriptObject.ScriptDetour();
                                        detour.Deserialize(reader);
                                        gsi.Detours.Add(detour);
                                    }
                                    break;
                            }
                        }
                        buffer = buffer.Skip((int)reader.BaseStream.Position).ToArray();
                    }
                    
                    if (BitConverter.ToInt64(buffer, 0) != 0x1C000A0D43534780)
                    {
                        AppendLogText("ERROR: Script is not a valid compiled script.\r\n");
                        return -1;
                    }
                }

                // Find game process
                ProcessEx bo3 = GetGameProcessEx(TreyarchCompiler.Enums.Games.T7);
                if (bo3 == null)
                {
                    AppendLogText("ERROR: No game process found for Black Ops III.\r\n");
                    AppendLogText("Make sure the game is running.\r\n");
                    return -1;
                }

                bool IsWindowsStore = !(bo3["GameChat2.dll"] is null);
                bo3.OpenHandle();
                bo3.SetDefaultCallType(ExCallThreadType.XCTT_QUAPC);
                OriginalPID = bo3.BaseProcess.Id;

                PointerEx off = IsWindowsStore ? 0xF3B1330 : 0x9407AB0;
                AppendLogText($"s_assetPool:ScriptParseTree => {bo3["blackops3.exe"][off]}\r\n");
                
                var sptGlob = bo3.GetValue<ulong>(bo3["blackops3.exe"][off]);
                var sptCount = bo3.GetValue<int>(bo3["blackops3.exe"][off + 0x14]);
                var SPTEntries = bo3.GetArray<T7SPT>(sptGlob, sptCount);
                
                bool found = false;
                for (int i = 0; i < SPTEntries.Length; i++)
                {
                    var entry = SPTEntries[i];
                    if (!entry.llpName) continue;
                    
                    try
                    {
                        var name = bo3.GetString(entry.llpName);
                        // In hot mode, match any script; otherwise match the replace path
                        if (hot != HotMode.None || name.ToLower().Trim().Replace("\\", "/") == replacePath.ToLower().Trim().Replace("\\", "/"))
                        {
                            found = true;
                            
                            // Cache target info (only for normal injection, not hot reload)
                            if (hot == HotMode.None)
                            {
                            llpModifiedSPTStruct = (ulong)(i * Marshal.SizeOf(typeof(T7SPT))) + sptGlob;
                            llpOriginalBuffer = entry.lpBuffer;
                            OriginalSourceChecksum = bo3.GetValue<int>(llpOriginalBuffer + 0x8);
                            }
                            else
                            {
                                // For hot reload, get checksum from current buffer
                                OriginalSourceChecksum = bo3.GetValue<int>(entry.lpBuffer + 0x8);
                            }

                            // Patch script into memory
                            entry.lpBuffer = bo3.QuickAlloc(buffer.Length);
                            BitConverter.GetBytes(OriginalSourceChecksum).CopyTo(buffer, 0x8);
                            bo3.SetBytes(entry.lpBuffer, buffer);

                            // Patch spt struct (only for normal injection)
                            if (hot == HotMode.None)
                            {
                            bo3.SetStruct(llpModifiedSPTStruct, entry);

                            // Cache the struct data for uninjection
                            InjectedScript = entry;
                            InjectedBuffSize = buffer.Length;
                            }
                            
                            // Reset the parse tree reset flag since we just injected
                            isParseTreeReset = false;

                            if (!noruntime)
                            {
                                try
                                {
                                    string exeFilePath = Assembly.GetExecutingAssembly().Location;
                                    var result = bo3.Call<long>(bo3.GetProcAddress(@"kernel32.dll", @"LoadLibraryA"), Path.Combine(Path.GetDirectoryName(exeFilePath), "t7cinternal.dll"));
                                    AppendLogText($"LoadLibrary Result => {result:X}\r\n");

                                    bo3.Refresh();
                                    if (result <= 0)
                                    {
                                        return (int)result;
                                    }

                                    bo3.Call(bo3.GetProcAddress(@"t7cinternal.dll", @"RemoveDetours"));
                                    if (gsi != null && gsi.Detours.Count > 0)
                                    {
                                        bo3.Call(bo3.GetProcAddress(@"t7cinternal.dll", @"RegisterDetours"), gsi.PackDetours(), gsi.Detours.Count, (long)entry.lpBuffer);
                                        AppendLogText($"Registered {gsi.Detours.Count} detours.\r\n");
                                    }
                                }
                                catch (Exception e)
                                {
                                    AppendLogText($"ERROR loading runtime: {e.Message}\r\n");
                                    return 3;
                                }
                            }
                            
                            // Hot reload logic (matching debug compiler)
                            if (hot != HotMode.None)
                            {
                                try
                                {
                                    string exeFilePath = Assembly.GetExecutingAssembly().Location;
                                    string t7cPath = Path.Combine(Path.GetDirectoryName(exeFilePath), "t7cinternal.dll");
                                    
                                    if (!File.Exists(t7cPath))
                                    {
                                        AppendLogText($"ERROR: t7cinternal.dll not found at {t7cPath}\r\n");
                                        AppendLogText("Hot reload requires t7cinternal.dll to be present.\r\n");
                                        return -1;
                                    }
                                    
                                    // Note: Hot reload requires System.Evasion.ModuleMapper which may not be available
                                    // This is a simplified implementation - full hot reload may need additional dependencies
                                    AppendLogText($"Hot reload mode: {hot}\r\n");
                                    AppendLogText("WARNING: Full hot reload implementation requires ModuleMapper.\r\n");
                                    AppendLogText("Script buffer has been patched, but hotload function call is not yet implemented.\r\n");
                                    AppendLogText("Consider using normal injection (None) for now.\r\n");
                                }
                                catch (Exception e)
                                {
                                    AppendLogText($"ERROR during hot reload: {e.Message}\r\n");
                                    return -1;
                                }
                            }

                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        AppendLogText($"ERROR processing entry: {e.Message}\r\n");
                        continue;
                    }
                }

                if (!found)
                {
                    AppendLogText($"ERROR: Script path '{replacePath}' not found in game's script table.\r\n");
                    // Clear any partial state
                    llpModifiedSPTStruct = 0;
                    InjectedBuffSize = 0;
                    OriginalPID = 0;
                    return -1;
                }

                // Ensure OriginalPID is set (it should be set above, but just in case)
                if (OriginalPID == 0)
                {
                    OriginalPID = bo3.BaseProcess.Id;
                }

                // Auto-reset parse tree after successful injection (matching debug compiler behavior)
                // Only for normal injection (not hot reload), and only if not already reset
                if (hot == HotMode.None && !isParseTreeReset)
                {
                    try
                    {
                        // Use FreeActiveScript which handles both T7 and T8
                        bool resetSuccess = FreeActiveScript();
                        if (resetSuccess)
                        {
                            isParseTreeReset = true;
                            AppendLogText("Parse tree automatically reset after injection.\r\n");
                            UpdateResetParseTreeButton();
                        }
                    }
                    catch (Exception e)
                    {
                        AppendLogText($"WARNING: Could not auto-reset parse tree: {e.Message}\r\n");
                        AppendLogText("You may need to manually reset it using the Reset Tree button.\r\n");
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                AppendLogText($"ERROR during injection: {ex.Message}\r\n");
                if (ex.InnerException != null)
                    AppendLogText($"Inner: {ex.InnerException.Message}\r\n");
                return -1;
            }
        }

        private void btnResetParseTree_Click(object sender, EventArgs e)
        {
            // Button state is automatically reset by PoisonButton.OnClick
            
            // Prevent reattempts if already reset
            if (isParseTreeReset)
            {
                AppendLog("Parse tree is already reset. No action needed.\r\n", LogLevel.Info);
                return;
            }
            
            // Double-check button state (should be disabled, but check anyway)
            if (btnResetParseTree != null && !btnResetParseTree.Enabled)
            {
                return;
            }
            
            try
            {
                AppendLogText("\r\n=== RESETTING SCRIPT PARSE TREE ===\r\n");
                Application.DoEvents();

                bool success = FreeActiveScript();

                if (success)
                {
                    AppendLogText("Script parse tree has been reset.\r\n\r\n");
                    isParseTreeReset = true;
                    
                    // Update button state (will disable it)
                    UpdateResetParseTreeButton();
                }
                else
                {
                    AppendLogText("Reset failed - see errors above.\r\n\r\n");
                }

                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
            }
            catch (Exception ex)
            {
                AppendLogText($"\r\n=== ERROR ===\r\n");
                AppendLogText($"Error resetting parse tree: {ex.Message}\r\n\r\n");
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
            }
            finally
            {
                // Reset button state to prevent stuck hover state
                ReaLTaiizorExt.PoisonControlHelper.ResetButtonState(sender as Control);
            }
        }
        
        private void UpdateResetParseTreeButton()
        {
            if (btnResetParseTree != null)
            {
                // Auto-detect injection state: button is enabled if script is injected AND not reset
                bool isInjected = (llpModifiedSPTStruct != 0 || InjectedBuffSize != 0);
                
                // Check if the game that was injected is still running
                bool gameRunning = false;
                if (isInjected && OriginalPID != 0)
                {
                    try
                    {
                        Process process = Process.GetProcessById(OriginalPID);
                        if (process != null && !process.HasExited)
                        {
                            gameRunning = true;
                        }
                    }
                    catch
                    {
                        // Process not found or error - game is not running
                        gameRunning = false;
                    }
                }
                
                // Button is enabled only if: script is injected AND game is running AND not reset
                bool shouldBeEnabled = isInjected && gameRunning && !isParseTreeReset;
                
                btnResetParseTree.Enabled = shouldBeEnabled;
                
                if (isParseTreeReset)
                {
                    btnResetParseTree.Text = "Reset Tree (Done)";
                    // Update tooltip to reflect disabled state
                    poisonToolTip?.SetToolTip(btnResetParseTree, "Parse tree is already reset. Inject a script to re-enable this button.");
                }
                else if (!isInjected)
                {
                    btnResetParseTree.Text = "Reset Tree";
                    btnResetParseTree.Enabled = false;
                    // Update tooltip to indicate no injection
                    poisonToolTip?.SetToolTip(btnResetParseTree, "No script injected. Inject a script first to enable this button.");
                }
                else if (!gameRunning)
                {
                    btnResetParseTree.Text = "Reset Tree";
                    btnResetParseTree.Enabled = false;
                    // Update tooltip to indicate game is not running
                    poisonToolTip?.SetToolTip(btnResetParseTree, "Game is not running. Start the game and inject a script to enable this button.");
                }
                else
                {
                    btnResetParseTree.Text = "Reset Tree";
                    // Restore original tooltip
                    poisonToolTip?.SetToolTip(btnResetParseTree, "Reset the parse tree cache");
                }
            }
        }

        private void btnLaunchBO3_Click(object sender, EventArgs e)
        {
            try
            {
                // BO3 Steam App ID is 311210
                System.Diagnostics.Process.Start("steam://rungameid/311210");
                AppendLogText("Launching Black Ops 3 via Steam...\r\n");
            }
            catch (Exception ex)
            {
                AppendLogText($"Failed to launch BO3: {ex.Message}\r\n");
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, $"Failed to launch Black Ops 3:\n{ex.Message}\n\nMake sure Steam is installed.", 
                    "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Reset button state to prevent stuck hover state
                ReaLTaiizorExt.PoisonControlHelper.ResetButtonState(sender as Control);
            }
        }
        
        private void btnKillBO3_Click(object sender, EventArgs e)
        {
            try
            {
                bool killed = KillBO3();
                if (killed)
                {
                    AppendLogText("Black Ops 3 process terminated.\r\n");
                    
                    // Update button states after killing process
                    if (btnLaunchBO3 != null && !btnLaunchBO3.IsDisposed)
                    {
                        btnLaunchBO3.Enabled = true;
                        if (poisonToolTip != null)
                        {
                            poisonToolTip.SetToolTip(btnLaunchBO3, "Launch Black Ops 3 (requires Steam)");
                        }
                    }
                    
                    if (btnKillBO3 != null && !btnKillBO3.IsDisposed)
                    {
                        btnKillBO3.Enabled = false;
                        if (poisonToolTip != null)
                        {
                            poisonToolTip.SetToolTip(btnKillBO3, "Black Ops 3 is not running");
                        }
                    }
                    
                    // Update game status label
                    UpdateGameStatus();
                }
                else
                {
                    AppendLogText("Black Ops 3 process not found or could not be terminated.\r\n");
                    ReaLTaiizor.Controls.PoisonMessageBox.Show(this, "Black Ops 3 process not found or could not be terminated.", 
                        "Kill Process", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                AppendLogText($"Failed to kill BO3 process: {ex.Message}\r\n");
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, $"Failed to kill Black Ops 3 process:\n{ex.Message}", 
                    "Kill Process Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Reset button state to prevent stuck hover state
                ReaLTaiizorExt.PoisonControlHelper.ResetButtonState(sender as Control);
            }
        }

        private bool FreeActiveScript()
        {
            try
            {
                switch (LastGameInjected)
                {
                    case TreyarchCompiler.Enums.Games.T7:
                        return FreeT7Script();
                    case TreyarchCompiler.Enums.Games.T8:
                        // T8 reset not implemented yet
                        AppendLogText("T8 script reset not yet implemented.\r\n");
                        return false;
                    default:
                        AppendLogText("No game injection state found.\r\n");
                        return false;
                }
            }
            catch (Exception ex)
            {
                AppendLogText($"Error in FreeActiveScript: {ex.Message}\r\n");
                throw;
            }
        }

        private bool FreeT7Script()
        {
            ProcessEx bo3 = GetGameProcessEx(TreyarchCompiler.Enums.Games.T7);
            if (bo3 == null)
            {
                AppendLogText("ERROR: No game process found for Black Ops III.\r\n");
                // Game is not running - clear injection state since there's nothing to reset
                if (OriginalPID != 0 || llpModifiedSPTStruct != 0)
                {
                    AppendLogText("Game is not running. Injection state has been automatically cleared.\r\n");
                    ClearInjectionState();
                }
                return false;
            }

            // If we don't have injection state loaded, try to load it
            if (llpModifiedSPTStruct == 0 && OriginalPID == 0)
            {
                AppendLogText("No in-memory injection state found. Attempting to load saved injection state...\r\n");
                LoadInjectionState();
                
                // Check again after loading
                if (llpModifiedSPTStruct == 0 && OriginalPID == 0)
                {
                    AppendLogText("No saved injection state found or state file is invalid.\r\n");
                    return false;
                }
            }

            // Verify process ID matches
            if (OriginalPID != 0 && bo3.BaseProcess.Id != OriginalPID)
            {
                // Process ID changed - this means the old game closed and a new instance started
                // There's nothing to reset since the old process is gone
                AppendLogText($"Game process ID changed (was {OriginalPID}, now {bo3.BaseProcess.Id}).\r\n");
                AppendLogText("The previous game instance has closed. Injection state has been automatically cleared.\r\n");
                ClearInjectionState();
                return false;
            }
            else if (OriginalPID == 0)
            {
                OriginalPID = bo3.BaseProcess.Id; // Set current PID
            }

            if (llpModifiedSPTStruct == 0) 
            {
                AppendLogText("No script injection state found. Cannot reset.\r\n");
                AppendLogText("If you injected a script, make sure the game is still running.\r\n");
                return false;
            }

            try
            {
                bo3.OpenHandle();

                // Free allocated space
                ProcessEx.VirtualFreeEx(bo3.Handle, InjectedScript.lpBuffer, (uint)InjectedBuffSize, (int)EnvironmentEx.FreeType.Release);

                // Patch SPT struct back to original
                InjectedScript.lpBuffer = llpOriginalBuffer;
                bo3.SetStruct(llpModifiedSPTStruct, InjectedScript);

                // Reset hooked detours
                try
                {
                    string exeFilePath = Assembly.GetExecutingAssembly().Location;
                    var handle = bo3.Call<IntPtr>(bo3.GetProcAddress(@"kernel32.dll", @"GetModuleHandleA"), "t7cinternal.dll");
                    if (handle != IntPtr.Zero)
                    {
                        bo3.Call(bo3.GetProcAddress(@"t7cinternal.dll", @"RemoveDetours"));
                        AppendLogText("Detours removed.\r\n");
                    }
                }
                catch
                {
                    // Ignore if t7cinternal.dll is not loaded
                }

                bo3.CloseHandle();

                // Reset tracking variables
                llpModifiedSPTStruct = 0;
                InjectedBuffSize = 0;
                OriginalPID = 0;
                
                // Update button state after clearing injection
                UpdateResetParseTreeButton();
                
                // Clear saved injection state
                ClearInjectionState();
                
                return true; // Success
            }
            catch (Exception ex)
            {
                AppendLogText($"ERROR during reset: {ex.Message}\r\n");
                return false; // Failed
            }
        }

        private void SaveInjectionState(string _replacePath, TreyarchCompiler.Enums.Games _game)
        {
            try
            {
                // Verify we have valid state to save
                if (llpModifiedSPTStruct == 0 || OriginalPID == 0)
                {
                    AppendLogText($"WARNING: Cannot save injection state - missing required data (SPT: {llpModifiedSPTStruct:X}, PID: {OriginalPID})\r\n");
                    return;
                }

                // Save to unified config file (no config folder needed)
                SaveAllSettings();
                AppendLogText($"Injection state saved to unified config file.\r\n");
            }
            catch (Exception ex)
            {
                AppendLogText($"ERROR saving injection state: {ex.Message}\r\n");
            }
        }

        private void LoadInjectionState()
        {
            // Injection state is now loaded as part of LoadAllSettings()
            // This method is kept for compatibility but the actual loading happens in LoadAllSettings()
            // Update the reset button state after loading injection state
            if (llpModifiedSPTStruct != 0 || InjectedBuffSize != 0)
            {
                isParseTreeReset = false;
                UpdateResetParseTreeButton();
            }
        }

        private void ClearInjectionState()
        {
            try
            {
                // Clear injection state and save to unified config
                llpModifiedSPTStruct = 0;
                InjectedBuffSize = 0;
                OriginalPID = 0;
                isParseTreeReset = false;
                UpdateResetParseTreeButton(); // Update button state when clearing injection
                SaveAllSettings(); // Update unified config without injection state
            }
            catch
            {
                // Ignore errors clearing state
            }
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            // Check if the dragged data contains file/folder paths
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                
                // Allow if it's a single item (folder or .gsc/.gscc file)
                if (files.Length == 1)
                {
                    string path = files[0];
                    
                    // Check if it's a folder
                    if (Directory.Exists(path))
                    {
                        e.Effect = DragDropEffects.Copy;
                        return;
                    }
                    
                    // Check if it's a .gsc or .gscc file
                    if (File.Exists(path))
                    {
                        string ext = Path.GetExtension(path).ToLower();
                        if (ext == ".gsc" || ext == ".gscc")
                        {
                            e.Effect = DragDropEffects.Copy;
                            return;
                        }
                    }
                }
            }
            
            e.Effect = DragDropEffects.None;
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                
                if (files.Length > 0)
                {
                    string path = files[0];
                    
                    // Check if it's a folder - go to compile tab
                    if (Directory.Exists(path))
                    {
                        string folderPath = path;
                        
                        // Set the project folder
                        txtProjectFolder.Text = folderPath;
                        AddToRecentProjects(folderPath); // Add to recent projects (use full path)
                        
                        // Save the folder for next time (saved via SaveAllSettings)
                        SaveAllSettings();
                        
                        // Auto-scan will be triggered by txtProjectFolder_TextChanged
                        
                        // Switch to compile tab if we're not already on it
                        if (tabControl.SelectedTab != tabCompile)
                        {
                            tabControl.SelectedTab = tabCompile;
                        }
                    }
                    // Check if it's a .gsc or .gscc file - go to inject tab
                    else if (File.Exists(path))
                    {
                        string ext = Path.GetExtension(path).ToLower();
                        if (ext == ".gsc" || ext == ".gscc")
                        {
                            string filePath = path;
                            
                            // Set the inject file
                            txtInjectFile.Text = filePath;
                            // Clear selection immediately to prevent text from being highlighted
                            ClearInjectFileSelection();
                            AddToRecentFiles(filePath); // Add to recent files
                            
                            // Switch to inject tab if we're not already on it (only if game is running)
                            if (tabControl.SelectedTab != tabInject)
                            {
                                // Determine which game we're targeting
                                TreyarchCompiler.Enums.Games game = TreyarchCompiler.Enums.Games.T7;
                                if (currentInjectGameIndex == 1)
                                    game = TreyarchCompiler.Enums.Games.T8;
                                
                                // Only switch if game is running
                                if (IsGameRunning(game))
                                {
                                    tabControl.SelectedTab = tabInject;
                                    
                                    // Clear selection after tab switch completes to prevent highlighting
                                    // Use BeginInvoke to ensure this happens after the tab switch UI update
                                    this.BeginInvoke(new Action(() =>
                                    {
                                        if (txtInjectFile != null)
                                        {
                                            txtInjectFile.SelectionStart = txtInjectFile.Text.Length;
                                            txtInjectFile.SelectionLength = 0;
                                        }
                                    }));
                                }
                            }
                            else
                            {
                                // Already on inject tab, just clear selection
                                txtInjectFile.SelectionStart = txtInjectFile.Text.Length;
                                txtInjectFile.SelectionLength = 0;
                            }
                        }
                    }
                }
            }
        }

        private void tabCompile_Click(object sender, EventArgs e)
        {
            // Tab page click handler - not typically needed as tab selection is handled by tabControl
        }

        private void lblGame_Click(object sender, EventArgs e)
        {

        }

        private void poisonToolTip_Popup(object sender, PopupEventArgs e)
        {
            // Ensure tooltip uses current theme and style when it pops up
            if (poisonToolTip != null && poisonStyleManager != null)
            {
                // PoisonToolTip is a component, not a control - set properties directly
                poisonToolTip.Style = poisonStyleManager.Style;
                poisonToolTip.Theme = poisonStyleManager.Theme;
                poisonToolTip.StyleManager = poisonStyleManager;
            }
        }
        
        private void poisonToolTip_Draw(object sender, DrawToolTipEventArgs e)
        {
            // Custom draw handler to ensure tooltip uses Poison theme colors with Style color border
            if (poisonToolTip == null || poisonStyleManager == null) return;
            
            // Use the actual theme (not inverted) to match the form
            ThemeStyle displayTheme = poisonStyleManager.Theme;
            
            // Get colors using PoisonPaint - match the form's theme
            Color backColor = PoisonPaint.BackColor.Form(displayTheme);
            // Use rainbow color if active, otherwise use style color for border
            Color borderColor = IsRainbowStyleActive() ? currentRainbowColor : PoisonPaint.GetStyleColor(poisonStyleManager.Style);
            Color foreColor = PoisonPaint.ForeColor.Label.Normal(displayTheme);
            
            // For better contrast, slightly adjust background if needed
            // Adjust background color for tooltip using PoisonPaint color manipulation
            if (displayTheme == ThemeStyle.Dark)
            {
                // Slightly lighter background for dark theme tooltips
                backColor = PoisonPaint.LightenColor(backColor, 0.04); // ~10/255 ≈ 0.04
            }
            else
            {
                // Slightly darker background for light theme tooltips
                backColor = PoisonPaint.DarkenColor(backColor, 0.04); // ~10/255 ≈ 0.04
            }
            
            // Draw background
            using (SolidBrush brush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }
            
            // Draw border with style color (thicker border to show style)
            using (Pen pen = new Pen(borderColor, 2))
            {
                e.Graphics.DrawRectangle(pen, new Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1));
            }
            
            // Draw text
            Font font = ReaLTaiizor.Extension.Poison.PoisonFonts.Default(13f);
            TextRenderer.DrawText(e.Graphics, e.ToolTipText, font, e.Bounds, foreColor, 
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
        
        #endregion
        
        #region Log Panel Controls
        
        private void btnLogClear_Click(object sender, EventArgs e)
        {
            txtLog?.Clear();
            // Reset button state to prevent stuck hover state
            ReaLTaiizorExt.PoisonControlHelper.ResetButtonState(sender as Control);
        }
        
        private void btnLogCopy_Click(object sender, EventArgs e)
        {
            if (txtLog != null && !string.IsNullOrEmpty(txtLog.Text))
            {
                try
                {
                    Clipboard.SetText(txtLog.Text);
                }
                catch
                {
                    // Silently fail - no logging
                }
            }
            // Reset button state to prevent stuck hover state
            ReaLTaiizorExt.PoisonControlHelper.ResetButtonState(sender as Control);
        }
        
        private void btnLogSave_Click(object sender, EventArgs e)
        {
            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                saveDialog.FileName = $"T7Compiler_Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                saveDialog.DefaultExt = "txt";
                
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllText(saveDialog.FileName, txtLog.Text);
                        AppendLog($"[INFO] Log saved to: {saveDialog.FileName}\r\n", LogLevel.Info);
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"[ERROR] Failed to save log: {ex.Message}\r\n", LogLevel.Error);
                    }
                }
            }
            // Reset button state to prevent stuck hover state
            ReaLTaiizorExt.PoisonControlHelper.ResetButtonState(sender as Control);
        }
        
        private void txtLogSearch_TextChanged(object sender, EventArgs e)
        {
            if (txtLog == null || string.IsNullOrEmpty(txtLogSearch.Text))
            {
                // Reset all highlighting
                if (txtLog != null)
                {
                    txtLog.SelectAll();
                    txtLog.SelectionBackColor = txtLog.BackColor;
                    txtLog.SelectionColor = txtLog.ForeColor;
                    txtLog.DeselectAll();
                }
                return;
            }
            
            // Search and highlight
            int startIndex = 0;
            string searchText = txtLogSearch.Text;
            txtLog.SelectAll();
            txtLog.SelectionBackColor = txtLog.BackColor;
            txtLog.SelectionColor = txtLog.ForeColor;
            
            while (startIndex < txtLog.Text.Length)
            {
                int index = txtLog.Text.IndexOf(searchText, startIndex, StringComparison.OrdinalIgnoreCase);
                if (index == -1) break;
                
                txtLog.Select(index, searchText.Length);
                // Use theme-aware highlight colors via PoisonPaint
                ThemeStyle currentTheme = poisonStyleManager?.Theme ?? ThemeStyle.Dark;
                // Use Yellow style color for highlight, lightened for better visibility
                Color highlightColor = PoisonPaint.GetStyleColor(ColorStyle.Yellow);
                txtLog.SelectionBackColor = PoisonPaint.LightenColor(highlightColor, 0.3); // Lighten yellow for highlight
                txtLog.SelectionColor = PoisonPaint.GetContrastingTextColor(txtLog.SelectionBackColor); // Ensure readable text
                
                startIndex = index + searchText.Length;
            }
            
            txtLog.DeselectAll();
            txtLog.ScrollToCaret();
        }
        
        private enum LogLevel { Info, Warning, Error, Success }
        
        private void AppendLog(string message, LogLevel level = LogLevel.Info)
        {
            if (txtLog == null) return;
            
            int start = txtLog.Text.Length;
            AppendLogText(message);
            int end = txtLog.Text.Length;
            
            // Apply color based on level using PoisonPaint for theme-aware colors
            txtLog.Select(start, end - start);
            ThemeStyle currentTheme = poisonStyleManager?.Theme ?? ThemeStyle.Dark;
            switch (level)
            {
                case LogLevel.Error:
                    // Red for errors - use PoisonPaint Red style color
                    txtLog.SelectionColor = PoisonPaint.GetStyleColor(ColorStyle.Red);
                    break;
                case LogLevel.Warning:
                    // Orange for warnings - blend Red and Yellow
                    Color red = PoisonPaint.GetStyleColor(ColorStyle.Red);
                    Color yellow = PoisonPaint.GetStyleColor(ColorStyle.Yellow);
                    txtLog.SelectionColor = PoisonPaint.BlendColors(red, yellow, 0.5);
                    break;
                case LogLevel.Success:
                    // Green for success - use PoisonPaint Green style color
                    txtLog.SelectionColor = PoisonPaint.GetStyleColor(ColorStyle.Green);
                    break;
                default:
                    txtLog.SelectionColor = PoisonPaint.ForeColor.Label.Normal(currentTheme); // Default - use theme color
                    break;
            }
            txtLog.DeselectAll();
            
            // Always scroll to bottom when new text is added
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }
        
        /// <summary>
        /// Appends text to log and automatically scrolls to bottom
        /// </summary>
        private void AppendLogText(string message)
        {
            if (txtLog == null) return;
            txtLog.AppendText(message);
            // Auto-scroll to bottom
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }
        
        #endregion
        
        #region Settings Tab
        
        // Settings are now handled by SettingsPanel
        
        internal bool IsRainbowStyleActive()
        {
            // Check if Style is set to Rainbow (new method using ColorStyle.Rainbow) OR if rainbow timer is active (legacy method)
            return (poisonStyleManager != null && poisonStyleManager.Style == ReaLTaiizor.Enum.Poison.ColorStyle.Rainbow) ||
                   (rainbowColorTimer != null && rainbowColorTimer.Enabled);
        }
        
        private System.Windows.Forms.Timer rainbowColorTimer;
        private float rainbowHue = 0f;
        internal Color currentRainbowColor = PoisonPaint.GetStyleColor(ColorStyle.Red); // Made internal so StyleBasedMenuRenderer can access it
        
        // Helper method to convert HSL to RGB
        private Color HslToRgb(float h, float s, float l)
        {
            // Normalize hue to 0-360
            h %= 360f;
            if (h < 0) h += 360f;
            
            // Clamp saturation and lightness to 0-1
            s = Math.Max(0f, Math.Min(1f, s));
            l = Math.Max(0f, Math.Min(1f, l));
            
            float c = (1f - Math.Abs(2f * l - 1f)) * s;
            float x = c * (1f - Math.Abs((h / 60f) % 2f - 1f));
            float m = l - c / 2f;
            
            float r, g, b;
            
            if (h < 60f)
            {
                r = c; g = x; b = 0f;
            }
            else if (h < 120f)
            {
                r = x; g = c; b = 0f;
            }
            else if (h < 180f)
            {
                r = 0f; g = c; b = x;
            }
            else if (h < 240f)
            {
                r = 0f; g = x; b = c;
            }
            else if (h < 300f)
            {
                r = x; g = 0f; b = c;
            }
            else
            {
                r = c; g = 0f; b = x;
            }
            
            return Color.FromArgb(
                (int)((r + m) * 255f),
                (int)((g + m) * 255f),
                (int)((b + m) * 255f)
            );
        }
        
        private void ChangeColorStyleToRainbow()
        {
            if (poisonStyleManager == null) return;
            
            // Set the style to Rainbow so it works directly with ColorStyle.Rainbow
            poisonStyleManager.Style = ReaLTaiizor.Enum.Poison.ColorStyle.Rainbow;
            
            // Set up the callback for PoisonPaint.GetRainbowColor to return current rainbow color
            PoisonPaint.GetRainbowColor = () => currentRainbowColor;
            
            // Start rainbow animation
            if (rainbowColorTimer == null)
            {
                rainbowColorTimer = ReaLTaiizorExt.PoisonFormHelper.CreateTrackedTimer(this, 16, RainbowColorTimer_Tick);
            }
            
            // Initialize rainbow color - start from red (hue 0) to match previous theme if it was red
            // But ensure it cycles through all colors smoothly
            currentRainbowColor = HslToRgb(0f, 1.0f, 0.5f);
            rainbowHue = 0f;
            
            rainbowColorTimer.Start();
            
            // Update all dropdown menus immediately
            UpdateAllMenusStyle();
            
            // Setup rainbow paint for tabs and form UI (this sets up all the paint overrides)
            SetupRainbowUI();
            
            // Update SettingsPanel if it exists
            UpdateSettingsColorStyleButton();
            
            // Force immediate update of all controls to clear any red styling
            this.Invalidate(true);
            this.Update();
            InvalidateAllControls(this);
            
            // Force tab control to repaint immediately with rainbow
            if (tabControl != null)
            {
                tabControl.Invalidate(true);
                tabControl.Update();
            }
            
            // Save setting
            SaveAllSettings();
        }
        
        private void SetupRainbowUI()
        {
            // Setup rainbow for tab control - only change border color
            if (tabControl != null)
            {
                // Keep UseStyleColors enabled so normal styling works
                tabControl.UseStyleColors = true;
                tabControl.Style = ColorStyle.Default;
                
                // Remove existing handlers to avoid duplicates
                tabControl.CustomPaintForeground -= TabControl_CustomPaintForeground;
                // Hook into CustomPaintForeground to change only the selected tab's border color
                tabControl.CustomPaintForeground += TabControl_CustomPaintForeground;
            }
            
            // Setup rainbow for all Poison controls - apply rainbow color to all controls that support Style
            ApplyRainbowToAllControls(this);
        }
        
        private void ApplyRainbowToAllControls(Control parent)
        {
            if (parent == null) return;
            
            // Use helper to find all Poison controls and apply rainbow to them
            var poisonControls = ReaLTaiizorExt.PoisonControlHelper.FindPoisonControls(parent);
            
            // Filter valid controls for batch operation
            var validControls = poisonControls
                .Where(pc => poisonStyleManager != null && pc is Control)
                .Cast<Control>()
                .ToList();
            
            if (validControls.Any())
            {
                // Use batch operation to apply StyleManager to all controls at once
                ReaLTaiizorExt.PoisonControlHelper.ApplyStyleManagerToControls(
                    poisonStyleManager,
                    validControls.ToArray()
                );
            }
            
            // Apply rainbow to each Poison control
            foreach (var poisonCtrl in poisonControls)
            {
                // Apply rainbow to this control if it's a Poison control
                if (poisonCtrl is Control control)
                {
                    // For controls that support Style property, we need to force them to use rainbow
                // We'll do this by subscribing to their paint events
                // The paint handlers will override the visual appearance when rainbow is active
                if (poisonCtrl is ReaLTaiizor.Controls.PoisonLabel poisonLabel)
                {
                    // UseStyleColors already set by ApplyStyleManager above
                    // Keep Style=Default so StyleManager can manage it - paint handler will override visuals
                    
                    // Subscribe to CustomPaintForeground to apply rainbow color to text
                    poisonLabel.CustomPaintForeground += (s, e) =>
                    {
                        if (IsRainbowStyleActive() && !string.IsNullOrEmpty(poisonLabel.Text))
                        {
                            // Get the label's font
                            Font labelFont = poisonLabel.Font;
                            
                            // Get background color for proper text rendering
                            Color backColor = poisonLabel.BackColor;
                            if (!poisonLabel.UseCustomBackColor)
                            {
                                backColor = PoisonPaint.BackColor.Form(poisonLabel.Theme);
                            }
                            
                            // Draw text with rainbow color
                            TextRenderer.DrawText(e.Graphics, poisonLabel.Text, labelFont, 
                                poisonLabel.ClientRectangle, currentRainbowColor, backColor,
                                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                        }
                    };
                }
                else if (poisonCtrl is ReaLTaiizor.Controls.PoisonToggle poisonToggle)
                {
                    // Toggles - apply rainbow to toggle switch and text
                    // UseStyleColors already set by ApplyStyleManager above
                    // Keep Style=Default so StyleManager can manage it - paint handler will override visuals
                    
                    // Subscribe to CustomPaintForeground to draw rainbow toggle switch
                    poisonToggle.CustomPaintForeground += (s, e) =>
                    {
                        if (IsRainbowStyleActive())
                        {
                            // Draw rainbow border for toggle switch
                            using (Pen rainbowPen = new Pen(currentRainbowColor, 1))
                            {
                                Rectangle toggleRect = new Rectangle(
                                    poisonToggle.DisplayStatus ? 30 : 0, 
                                    0, 
                                    poisonToggle.ClientRectangle.Width - (poisonToggle.DisplayStatus ? 31 : 1), 
                                    poisonToggle.ClientRectangle.Height - 1
                                );
                                e.Graphics.DrawRectangle(rainbowPen, toggleRect);
                            }
                            
                            // Draw rainbow fill for checked state
                            if (poisonToggle.Checked)
                            {
                                using (SolidBrush rainbowBrush = new SolidBrush(currentRainbowColor))
                                {
                                    Rectangle fillRect = new Rectangle(
                                        poisonToggle.DisplayStatus ? 32 : 2, 
                                        2, 
                                        poisonToggle.ClientRectangle.Width - (poisonToggle.DisplayStatus ? 34 : 4), 
                                        poisonToggle.ClientRectangle.Height - 4
                                    );
                                    e.Graphics.FillRectangle(rainbowBrush, fillRect);
                                }
                            }
                            
                            // Draw rainbow toggle indicator (the sliding part)
                            using (SolidBrush rainbowBrush = new SolidBrush(currentRainbowColor))
                            {
                                int left = poisonToggle.Checked ? poisonToggle.Width - 10 : (poisonToggle.DisplayStatus ? 30 : 0);
                                Rectangle indicatorRect = new Rectangle(left, 0, 10, poisonToggle.ClientRectangle.Height);
                                e.Graphics.FillRectangle(rainbowBrush, indicatorRect);
                            }
                            
                            // Draw rainbow text for DisplayStatus=true (text on left)
                            if (poisonToggle.DisplayStatus && !string.IsNullOrEmpty(poisonToggle.Text))
                            {
                                var fontSizeProp = poisonToggle.GetType().GetProperty("FontSize");
                                var fontWeightProp = poisonToggle.GetType().GetProperty("FontWeight");
                                Font toggleFont = ReaLTaiizor.Extension.Poison.PoisonFonts.LinkLabel(
                                    fontSizeProp != null ? (ReaLTaiizor.Extension.Poison.PoisonLinkLabelSize)fontSizeProp.GetValue(poisonToggle) : ReaLTaiizor.Extension.Poison.PoisonLinkLabelSize.Small,
                                    fontWeightProp != null ? (ReaLTaiizor.Extension.Poison.PoisonLinkLabelWeight)fontWeightProp.GetValue(poisonToggle) : ReaLTaiizor.Extension.Poison.PoisonLinkLabelWeight.Regular
                                );
                                
                                Rectangle textRect = new Rectangle(0, 0, 30, poisonToggle.ClientRectangle.Height);
                                TextRenderer.DrawText(e.Graphics, poisonToggle.Text, toggleFont, textRect, 
                                    currentRainbowColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                            }
                        }
                    };
                    
                    // For DisplayStatus=false, use Paint event to override text after base control paints
                    if (!poisonToggle.DisplayStatus)
                    {
                        poisonToggle.Paint += (s, e) =>
                        {
                            if (IsRainbowStyleActive() && !string.IsNullOrEmpty(poisonToggle.Text))
                            {
                                var fontSizeProp = poisonToggle.GetType().GetProperty("FontSize");
                                var fontWeightProp = poisonToggle.GetType().GetProperty("FontWeight");
                                Font toggleFont = ReaLTaiizor.Extension.Poison.PoisonFonts.LinkLabel(
                                    fontSizeProp != null ? (ReaLTaiizor.Extension.Poison.PoisonLinkLabelSize)fontSizeProp.GetValue(poisonToggle) : ReaLTaiizor.Extension.Poison.PoisonLinkLabelSize.Small,
                                    fontWeightProp != null ? (ReaLTaiizor.Extension.Poison.PoisonLinkLabelWeight)fontWeightProp.GetValue(poisonToggle) : ReaLTaiizor.Extension.Poison.PoisonLinkLabelWeight.Regular
                                );
                                
                                // Get background color to erase original text
                                Color backColor = PoisonPaint.BackColor.Form(poisonToggle.Theme);
                                
                                // Measure text
                                Size textSize = TextRenderer.MeasureText(e.Graphics, poisonToggle.Text, toggleFont);
                                
                                // Text is on the right side - toggle switch is ~40px, text area starts around 40-45px
                                int toggleSwitchWidth = 40; // Approximate toggle switch width
                                int textStartX = toggleSwitchWidth + 3; // Small padding after toggle
                                
                                // Calculate text position based on actual rendered position
                                // CheckBox draws text aligned left within the content area
                                Rectangle textRect = new Rectangle(textStartX, 0, poisonToggle.ClientRectangle.Width - textStartX, poisonToggle.ClientRectangle.Height);
                                
                                // Erase original text by filling a larger area to ensure coverage
                                Rectangle eraseRect = new Rectangle(
                                    textStartX - 2,
                                    0,
                                    poisonToggle.ClientRectangle.Width - textStartX + 2,
                                    poisonToggle.ClientRectangle.Height
                                );
                                using (SolidBrush backBrush = new SolidBrush(backColor))
                                {
                                    e.Graphics.FillRectangle(backBrush, eraseRect);
                                }
                                
                                // Draw rainbow text
                                TextRenderer.DrawText(e.Graphics, poisonToggle.Text, toggleFont, textRect, 
                                    currentRainbowColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                            }
                        };
                    }
                }
                else if (poisonCtrl is ReaLTaiizor.Controls.PoisonPanel poisonPanel)
                {
                    // Panels - apply rainbow to border
                    // UseStyleColors already set by ApplyStyleManager above
                    // Keep Style=Default so StyleManager can manage it - paint handler will override visuals
                    
                    // Subscribe to paint to draw rainbow border
                    poisonPanel.Paint += (s, e) =>
                    {
                        if (IsRainbowStyleActive() && poisonPanel.BorderStyle != BorderStyle.None)
                        {
                            using (Pen rainbowPen = new Pen(currentRainbowColor, 1))
                            {
                                Rectangle borderRect = poisonPanel.ClientRectangle;
                                borderRect.Width -= 1;
                                borderRect.Height -= 1;
                                e.Graphics.DrawRectangle(rainbowPen, borderRect);
                            }
                        }
                    };
                }
                else if (poisonCtrl is ReaLTaiizor.Controls.PoisonTextBox poisonTextBox)
                {
                    // Textboxes - apply rainbow to border and focus indicator
                    // UseStyleColors already set by ApplyStyleManager above
                    // Keep Style=Default so StyleManager can manage it - paint handler will override visuals
                    
                    // Subscribe to CustomPaintForeground to draw rainbow border
                    poisonTextBox.CustomPaintForeground += (s, e) =>
                    {
                        if (IsRainbowStyleActive())
                        {
                            using (Pen rainbowPen = new Pen(currentRainbowColor, 1))
                            {
                                Rectangle borderRect = poisonTextBox.ClientRectangle;
                                borderRect.Width -= 1;
                                borderRect.Height -= 1;
                                e.Graphics.DrawRectangle(rainbowPen, borderRect);
                            }
                        }
                    };
                }
                else if (poisonCtrl is ReaLTaiizor.Controls.PoisonButton poisonButton)
                {
                    // Buttons - apply rainbow to border and text
                    // UseStyleColors already set by ApplyStyleManager above
                    // Keep Style=Default so StyleManager can manage it - paint handler will override visuals
                    
                    // Subscribe to CustomPaintForeground to draw rainbow border and text
                    poisonButton.CustomPaintForeground += (s, e) =>
                    {
                        if (IsRainbowStyleActive())
                        {
                            // Draw rainbow border
                            using (Pen rainbowPen = new Pen(currentRainbowColor, 1))
                            {
                                Rectangle borderRect = poisonButton.ClientRectangle;
                                borderRect.Width -= 1;
                                borderRect.Height -= 1;
                                e.Graphics.DrawRectangle(rainbowPen, borderRect);
                            }
                            
                            // Draw rainbow text
                            if (!string.IsNullOrEmpty(poisonButton.Text))
                            {
                                var fontSizeProp = poisonButton.GetType().GetProperty("FontSize");
                                var fontWeightProp = poisonButton.GetType().GetProperty("FontWeight");
                                Font buttonFont = ReaLTaiizor.Extension.Poison.PoisonFonts.Button(
                                    fontSizeProp != null ? (ReaLTaiizor.Extension.Poison.PoisonButtonSize)fontSizeProp.GetValue(poisonButton) : ReaLTaiizor.Extension.Poison.PoisonButtonSize.Medium,
                                    fontWeightProp != null ? (ReaLTaiizor.Extension.Poison.PoisonButtonWeight)fontWeightProp.GetValue(poisonButton) : ReaLTaiizor.Extension.Poison.PoisonButtonWeight.Regular
                                );
                                
                                TextRenderer.DrawText(e.Graphics, poisonButton.Text, buttonFont, 
                                    poisonButton.ClientRectangle, currentRainbowColor, 
                                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                            }
                        }
                    };
                }
                else if (poisonCtrl is ReaLTaiizor.Controls.PoisonDropDownButton poisonDropDown)
                {
                    // Dropdown buttons - apply rainbow to border and text
                    // UseStyleColors already set by ApplyStyleManager above
                    // Keep Style=Default so StyleManager can manage it - paint handler will override visuals
                    
                    // Subscribe to CustomPaintForeground to draw rainbow border and text
                    poisonDropDown.CustomPaintForeground += (s, e) =>
                    {
                        if (IsRainbowStyleActive())
                        {
                            // Draw rainbow border
                            using (Pen rainbowPen = new Pen(currentRainbowColor, 1))
                            {
                                Rectangle borderRect = poisonDropDown.ClientRectangle;
                                borderRect.Width -= 1;
                                borderRect.Height -= 1;
                                e.Graphics.DrawRectangle(rainbowPen, borderRect);
                            }
                            
                            // Draw rainbow text - only in the main button area (excluding split/arrow section)
                            if (!string.IsNullOrEmpty(poisonDropDown.Text))
                            {
                                var fontSizeProp = poisonDropDown.GetType().GetProperty("FontSize");
                                var fontWeightProp = poisonDropDown.GetType().GetProperty("FontWeight");
                                Font buttonFont = ReaLTaiizor.Extension.Poison.PoisonFonts.Button(
                                    fontSizeProp != null ? (ReaLTaiizor.Extension.Poison.PoisonButtonSize)fontSizeProp.GetValue(poisonDropDown) : ReaLTaiizor.Extension.Poison.PoisonButtonSize.Medium,
                                    fontWeightProp != null ? (ReaLTaiizor.Extension.Poison.PoisonButtonWeight)fontWeightProp.GetValue(poisonDropDown) : ReaLTaiizor.Extension.Poison.PoisonButtonWeight.Regular
                                );
                                
                                // Get the split section width (18px for dropdown buttons)
                                const int splitSectionWidth = 18;
                                var showSplitProp = poisonDropDown.GetType().GetProperty("ShowSplit");
                                bool showSplit = showSplitProp != null && (bool)(showSplitProp.GetValue(poisonDropDown) ?? false);
                                
                                // Text rectangle excludes the split section
                                Rectangle textRect = poisonDropDown.ClientRectangle;
                                if (showSplit && poisonDropDown.RightToLeft != RightToLeft.Yes)
                                {
                                    textRect.Width -= splitSectionWidth;
                                }
                                else if (showSplit && poisonDropDown.RightToLeft == RightToLeft.Yes)
                                {
                                    textRect.X += splitSectionWidth;
                                    textRect.Width -= splitSectionWidth;
                                }
                                
                                // Get background color to erase original text
                                Color backColor = PoisonPaint.BackColor.Button.Normal(poisonDropDown.Theme);
                                if (!poisonDropDown.Enabled)
                                {
                                    backColor = PoisonPaint.BackColor.Button.Disabled(poisonDropDown.Theme);
                                }
                                
                                // Erase original text by filling the text area with background color
                                using (SolidBrush backBrush = new SolidBrush(backColor))
                                {
                                    Size textSize = TextRenderer.MeasureText(e.Graphics, poisonDropDown.Text, buttonFont, textRect.Size, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                                    Rectangle eraseRect = new Rectangle(
                                        textRect.X + (textRect.Width - textSize.Width) / 2,
                                        textRect.Y + (textRect.Height - textSize.Height) / 2,
                                        textSize.Width,
                                        textSize.Height
                                    );
                                    e.Graphics.FillRectangle(backBrush, eraseRect);
                                }
                                
                                // Draw rainbow text in the main button area
                                TextRenderer.DrawText(e.Graphics, poisonDropDown.Text, buttonFont, 
                                    textRect, currentRainbowColor, 
                                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                            }
                        }
                    };
                }
                } // Close if (poisonCtrl is Control control)
            } // Close foreach (var poisonCtrl in poisonControls)
            
            // Recursively apply to all child controls
            foreach (Control child in parent.Controls)
            {
                ApplyRainbowToAllControls(child);
            }
        }
        
        private void TabControl_CustomPaintForeground(object sender, PoisonPaintEventArgs e)
        {
            if (tabControl == null || !IsRainbowStyleActive()) return;
            
            // Only change the border color for the selected tab when rainbow is active
            if (tabControl.SelectedIndex >= 0 && tabControl.SelectedIndex < tabControl.TabPages.Count)
            {
                int tabBottomBorderHeight = 2;
                Rectangle selectedTabRect = tabControl.GetTabRect(tabControl.SelectedIndex);
                
                // Draw ONLY the selected tab's bottom indicator border with rainbow color
                Rectangle selectedTabBorderRectangle = new Rectangle(
                    selectedTabRect.X + (tabControl.SelectedIndex == 0 ? 2 : 0), 
                    selectedTabRect.Bottom + 2 - tabBottomBorderHeight, 
                    selectedTabRect.Width + (tabControl.SelectedIndex == 0 ? 0 : 2), 
                    tabBottomBorderHeight
                );
                
                // Use rainbow color for border
                using (SolidBrush borderBrush = new SolidBrush(currentRainbowColor))
                {
                    e.Graphics.FillRectangle(borderBrush, selectedTabBorderRectangle);
                }
            }
        }
        
        /// <summary>
        /// Removes rainbow event handlers from all controls recursively.
        /// Note: Anonymous lambda handlers can't be easily removed, but they're safe because
        /// they check IsRainbowStyleActive() before doing anything. This method removes
        /// the named handler (TabControl_CustomPaintForeground) and invalidates controls.
        /// </summary>
        private void RemoveRainbowEventHandlersRecursive(Control parent)
        {
            if (parent == null) return;
            
            // Remove tab control rainbow handler
            if (parent is ReaLTaiizor.Controls.PoisonTabControl tabCtrl)
            {
                tabCtrl.CustomPaintForeground -= TabControl_CustomPaintForeground;
                tabCtrl.Invalidate();
            }
            
            // Recursively process all child controls
            foreach (Control child in parent.Controls)
            {
                RemoveRainbowEventHandlersRecursive(child);
                
                // Invalidate control to force repaint without rainbow
                child.Invalidate();
            }
        }
        
        private void RainbowColorTimer_Tick(object sender, EventArgs e)
        {
            if (poisonStyleManager == null || !IsRainbowStyleActive()) return;
            
            // Use SafeExecute for error handling
            ReaLTaiizorExt.PoisonFormHelper.SafeExecute(() =>
            {
                // Cycle through hue values (0-360) smoothly
                rainbowHue = (rainbowHue + 1.5f) % 360f;
                
                // Convert HSL to RGB (full saturation, medium lightness for vibrant colors)
                currentRainbowColor = HslToRgb(rainbowHue, 1.0f, 0.5f);
                
                // Force all controls to repaint with the new rainbow color
                // Use SafeInvoke to update on UI thread with error handling
                ReaLTaiizorExt.PoisonFormHelper.SafeInvoke(this, () => UpdateRainbowUI());
            },
            (ex) => System.Diagnostics.Debug.WriteLine($"Rainbow timer error: {ex.Message}"));
        }
        
        private void UpdateRainbowUI()
        {
            // Invalidate main form to update borders
            this.Invalidate(true);
            this.Update();
            
            // Force all Poison controls to update their Style property to reflect rainbow
            // This ensures all controls that use Style for colors will show rainbow
            UpdateAllControlsForRainbow(this);
            
            // Invalidate all child controls
            InvalidateAllControls(this);
            
            // Invalidate tab control specifically to update rainbow colors (including tab text)
            if (tabControl != null)
            {
                tabControl.Invalidate(true);
                tabControl.Update();
            }
            
            // Invalidate log panel to update rainbow border around txtLog
            panelLog?.Invalidate();
            
            // Game status label uses fixed green/red colors (not rainbow) - don't invalidate it
            
            // Invalidate theme toggle and color style button to update rainbow colors
            if (toggleSettingsTheme != null)
            {
                toggleSettingsTheme.Invalidate();
                toggleSettingsTheme.Update();
            }
            
            // Invalidate all toggles to update rainbow colors (including chkNoRuntime and settings toggles)
            if (chkNoRuntime != null)
            {
                chkNoRuntime.Invalidate();
                chkNoRuntime.Update();
            }
            
            if (chkSaveOpcodeMap != null)
            {
                chkSaveOpcodeMap.Invalidate();
                chkSaveOpcodeMap.Update();
            }
            
            if (chkOpcodeMasking != null)
            {
                chkOpcodeMasking.Invalidate();
                chkOpcodeMasking.Update();
            }
            
            
            if (btnSettingsColorStyle != null)
            {
                btnSettingsColorStyle.Invalidate();
                btnSettingsColorStyle.Update();
            }
            
            // Force tooltip to repaint if it's active
            UpdateActiveTooltip();
            
            // Invalidate all context menus to update rainbow colors immediately
            InvalidateAllContextMenus();
        }
        
        private void InvalidateAllContextMenus()
        {
            // Invalidate all context menus so they update with rainbow colors immediately
            var contextMenus = new[]
            {
                mainFormContextMenu,
                settingsContextMenu,
                platformMenu,
                gameMenu,
                injectGameMenu,
                settingsColorStyleMenu
            };
            
            foreach (var menu in contextMenus)
            {
                if (menu != null && menu.Visible)
                {
                    // Update renderer to refresh rainbow color
                    if (menu.Renderer is StyleBasedMenuRenderer renderer && poisonStyleManager != null)
                    {
                        renderer.UpdateStyleManager(poisonStyleManager);
                    }
                    
                    // Invalidate the menu to force repaint
                    menu.Invalidate();
                    
                    // Also invalidate all menu items
                    foreach (ToolStripItem item in menu.Items)
                    {
                        item.Invalidate();
                    }
                }
            }
        }
        
        private void UpdateAllControlsForRainbow(Control parent)
        {
            if (parent == null) return;
            
            // Skip game status label - it uses fixed green/red colors, not rainbow
            if (parent == lblGameStatus)
                return;
            
            // Update this control if it's a Poison control
            if (parent is IPoisonControl poisonCtrl)
            {
                // Use helper to apply StyleManager (handles StyleManager, Theme, Style, UseStyleColors)
                if (poisonStyleManager != null)
                {
                    ReaLTaiizorExt.PoisonControlHelper.ApplyStyleManager(parent, poisonStyleManager);
                }
                
                // Force control to invalidate so it repaints with rainbow
                parent.Invalidate();
                
                // For labels, toggles, buttons and dropdowns, force immediate update
                if (poisonCtrl is ReaLTaiizor.Controls.PoisonLabel ||
                    poisonCtrl is ReaLTaiizor.Controls.PoisonToggle ||
                    poisonCtrl is ReaLTaiizor.Controls.PoisonButton || 
                    poisonCtrl is ReaLTaiizor.Controls.PoisonDropDownButton)
                {
                    parent.Update();
                }
            }
            
            // Update IPoisonComponent controls (like menus)
            if (parent is IPoisonComponent poisonComponent)
            {
                if (poisonStyleManager != null)
                {
                    // IPoisonComponent doesn't use ApplyStyleManager, set StyleManager directly
                    poisonComponent.StyleManager = poisonStyleManager;
                }
                parent.Invalidate();
            }
            
            // Handle standard WinForms controls that use StyleExtender (like RichTextBox)
            if (poisonStyleExtender != null && txtLog != null && parent == txtLog)
            {
                // txtLog uses StyleExtender - invalidate it for rainbow border updates
                parent.Invalidate();
            }
            
            // Recursively update all child controls
            foreach (Control child in parent.Controls)
            {
                UpdateAllControlsForRainbow(child);
            }
        }
        
        private void UpdateActiveTooltip()
        {
            if (poisonToolTip == null) return;
            
            // Use reflection to access the internal tooltip window and force it to repaint
            try
            {
                // Find all tooltip windows and invalidate them
                // Tooltips are child windows, so we need to enumerate them
                var tooltipHandleField = poisonToolTip.GetType().GetField("handle", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (tooltipHandleField != null)
                {
                    IntPtr handle = (IntPtr)tooltipHandleField.GetValue(poisonToolTip);
                    if (handle != IntPtr.Zero)
                    {
                        // Send WM_PAINT message to force repaint
                        SendMessage(handle, WM_PAINT, 0, 0);
                    }
                }
                
                // Also try to find the actual tooltip window by class name "tooltips_class32"
                IntPtr tooltipWindow = FindWindow("tooltips_class32", null);
                if (tooltipWindow != IntPtr.Zero)
                {
                    SendMessage(tooltipWindow, WM_PAINT, 0, 0); // WM_PAINT
                    // Force immediate update
                    InvalidateRect(tooltipWindow, IntPtr.Zero, false);
                    UpdateWindow(tooltipWindow);
                }
            }
            catch
            {
                // Silently fail - tooltips will update on next mouse move or hover
            }
        }
        
        // Windows API for tooltip updates
        // Windows API - ReaLTaiizor's Native classes are internal, so we need our own DllImports
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        
        [DllImport("user32.dll")]
        private static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);
        
        [DllImport("user32.dll")]
        private static extern bool UpdateWindow(IntPtr hWnd);
        
        private void InvalidateAllControls(Control parent)
        {
            if (parent == null) return;
            
            // Use helper to refresh all controls (includes invalidation)
            // Also ensures StyleManager is applied to all Poison controls
            if (poisonStyleManager != null)
            {
                ReaLTaiizorExt.PoisonControlHelper.ApplyStyleManager(parent, poisonStyleManager);
            }
            ReaLTaiizorExt.PoisonControlHelper.RefreshAllControls(parent);
        }
        
        private void ChangeTheme(ThemeStyle theme)
        {
            if (poisonStyleManager == null) return;
            
            // Suspend layout to prevent incremental updates
            this.SuspendLayout();
            
            try
            {
                // Match demo approach: Just set StyleManager.Theme and let it propagate automatically
                // All controls connected to StyleManager will update instantly
                poisonStyleManager.Theme = theme;
                
                // Explicitly call Update() to ensure all controls refresh immediately
                // (Setting Theme property calls Update() automatically, but we ensure it here)
                poisonStyleManager.Update();
                
                // Set form background color based on theme
                this.BackColor = PoisonPaint.BackColor.Form(theme);
                
                // Update PoisonStyleExtender theme (needed for non-Poison controls like RichTextBox)
                if (poisonStyleExtender != null && poisonStyleManager != null)
                {
                    // PoisonStyleExtender is a component, not a control - set properties directly
                    poisonStyleExtender.Theme = theme;
                    poisonStyleExtender.StyleManager = poisonStyleManager;
                }
                    
                // Update RichTextBox colors based on theme - use PoisonPaint for consistency
                // Apply full Poison theme styling including selection colors and border
                if (txtLog != null)
                {
                    System.Drawing.Color bgColor = PoisonPaint.BackColor.Form(theme);
                    System.Drawing.Color fgColor = PoisonPaint.ForeColor.Label.Normal(theme);
                    System.Drawing.Color styleColor = PoisonPaint.GetStyleColor(poisonStyleManager.Style);
                    
                    txtLog.BackColor = bgColor;
                    txtLog.ForeColor = fgColor;
                    
                    // Style selection colors to match Poison theme
                    // Use style color for selection background (accent color) - blend with background for semi-transparent effect
                    txtLog.SelectionBackColor = PoisonPaint.BlendColors(bgColor, styleColor, 0.2); // Semi-transparent accent
                    txtLog.SelectionColor = PoisonPaint.GetContrastingTextColor(txtLog.SelectionBackColor); // Ensure readable text
                    
                    // Remove border if possible (RichTextBox doesn't have BorderStyle, but we can hide it)
                    // The parent panel already has proper Poison styling
                }
                
                // Update tooltip theme
                if (poisonToolTip != null && poisonStyleManager != null)
                {
                    // PoisonToolTip is a component, not a control - set properties directly
                    poisonToolTip.Theme = theme;
                    poisonToolTip.StyleManager = poisonStyleManager;
                }
                
                // Update all controls that might not be connected to StyleManager
                UpdateControlStyle(this, poisonStyleManager.Style, theme, forceUpdate: true);
                
                // Update tab panels background
                UpdateTabPanelsBackground();
                
                // Update all dropdown menus immediately
                UpdateAllMenusTheme(theme);
                
                // Update Settings tab theme toggle
                if (toggleSettingsTheme != null)
                {
                    toggleSettingsTheme.Checked = (theme == ThemeStyle.Light);
                    toggleSettingsTheme.Text = toggleSettingsTheme.Checked ? "Light" : "Dark";
                }
                
                // Resume layout
                this.ResumeLayout(false);
                
                // Force refresh of entire form
                this.Invalidate(true);
                this.Update();
                this.Refresh();
            }
            catch
            {
                // Ensure layout is resumed even if an error occurs
                try
                {
                    this.ResumeLayout(false);
                }
                catch
                {
                    // Ignore if form is disposed
                }
            }
            
            // Save settings (only if not loading)
            if (!isLoadingSettings)
            {
                SaveAllSettings();
            }
        }
        
        private void ChangeColorStyle(ColorStyle style)
        {
            if (poisonStyleManager == null) return;
            
            // Stop rainbow animation if active and remove all rainbow handlers
            bool wasRainbowActive = rainbowColorTimer != null && rainbowColorTimer.Enabled;
            if (wasRainbowActive)
            {
                rainbowColorTimer.Stop();
                rainbowColorTimer.Dispose();
                rainbowColorTimer = null;
                
                // Remove all rainbow event handlers from controls
                RemoveRainbowEventHandlersRecursive(this);
            }
            
            // Match demo approach: Just set StyleManager.Style and let it propagate automatically
            // All controls connected to StyleManager will update instantly
            poisonStyleManager.Style = style;
            
            // Force StyleManager to update all controls (critical after disabling rainbow)
            // This ensures all controls get the new style properly
            poisonStyleManager.Update();
            
            // Reset all controls to follow StyleManager properly
            // This is especially important after rainbow was active, as rainbow sets Style=Default
            ResetControlsToStyleManager(this, style);
            
            // Update all dropdown menus immediately
            UpdateAllMenusStyle();
            
            // Update Settings tab color style button text
                UpdateSettingsColorStyleButton();
                
            // Use PoisonControlHelper to update theme toggle and color style button
            if (toggleSettingsTheme != null && poisonStyleManager != null)
            {
                ReaLTaiizor.Extension.Poison.PoisonControlHelper.ApplyStyleManager(toggleSettingsTheme, poisonStyleManager);
                toggleSettingsTheme.Invalidate();
            }
            
            if (btnSettingsColorStyle != null && poisonStyleManager != null)
            {
                ReaLTaiizor.Extension.Poison.PoisonControlHelper.ApplyStyleManager(btnSettingsColorStyle, poisonStyleManager);
                btnSettingsColorStyle.Invalidate();
            }
            
            // Force all controls to refresh with new style
            InvalidateAllControls(this);
            
            // Save settings (only if not loading)
            if (!isLoadingSettings)
            {
                SaveAllSettings();
            }
        }
        
        private void ResetControlsToStyleManager(Control parent, ColorStyle style)
        {
            if (parent == null || poisonStyleManager == null) return;
            
            // Use PoisonControlHelper to apply StyleManager and update theme/style
            // This handles all the manual setup automatically
            ReaLTaiizor.Extension.Poison.PoisonControlHelper.ApplyStyleManager(parent, poisonStyleManager);
            
            // If style needs to be set explicitly (not Default), use helper to update with refresh
            if (style != ColorStyle.Default)
            {
                ReaLTaiizor.Extension.Poison.PoisonControlHelper.UpdateThemeAndStyleWithRefresh(
                    parent, 
                    poisonStyleManager.Theme, 
                    style);
            }
        }
        
        // Default output path selection is now handled by SettingsPanel
        
        /// <summary>
        /// Auto-populates the output file field based on the default output path setting.
        /// Only updates if the output file is empty or using the default value.
        /// Only creates build folder if user hasn't set a custom default output path.
        /// </summary>
        private void AutoPopulateOutputFile()
        {
            // Only auto-populate if output file is empty or using the default build folder path
            string currentOutput = txtOutputFile.Text;
            string defaultBuildOutput = Path.Combine(buildFolder, "compiled.gscc");
            
            if (!string.IsNullOrWhiteSpace(currentOutput) && 
                currentOutput != defaultBuildOutput && 
                File.Exists(currentOutput))
            {
                // User has set a custom output file that exists - don't override
                return;
            }
            
            // Check if default output path is set in settings
            if (txtSettingsDefaultOutputPath != null && !string.IsNullOrWhiteSpace(txtSettingsDefaultOutputPath.Text))
            {
                string defaultPath = txtSettingsDefaultOutputPath.Text.Trim();
                
                // Check if it's a directory or a file path
                if (Directory.Exists(defaultPath))
                {
                    // It's a directory - combine with default filename
                    txtOutputFile.Text = Path.Combine(defaultPath, "compiled.gscc");
                }
                else if (Path.IsPathRooted(defaultPath))
                {
                    // It's a full file path - use it directly
                    txtOutputFile.Text = defaultPath;
                }
                else
                {
                    // Relative path - combine with project folder if available
                    if (!string.IsNullOrWhiteSpace(txtProjectFolder.Text))
                    {
                        string expandedProjectFolder = Helpers.PathHelper.ExpandPath(txtProjectFolder.Text);
                        if (Directory.Exists(expandedProjectFolder))
                        {
                            txtOutputFile.Text = Path.Combine(expandedProjectFolder, defaultPath);
                    }
                    else
                    {
                        // Fallback to build folder (only create if needed)
                        EnsureBuildFolderExists();
                            txtOutputFile.Text = Path.Combine(buildFolder, defaultPath);
                        }
                    }
                    else
                    {
                        // Fallback to build folder (only create if needed)
                        EnsureBuildFolderExists();
                        txtOutputFile.Text = Path.Combine(buildFolder, defaultPath);
                    }
                }
            }
            else
            {
                // No default output path set - only create build folder if user hasn't chosen a custom location
                // This means we only create the build folder when actually needed
                EnsureBuildFolderExists();
                txtOutputFile.Text = defaultBuildOutput;
            }
        }
        
        #endregion
        
        #region Control Click Handler Management
        
        /// <summary>
        /// Sets up click handlers for all clickable controls in the form.
        /// This ensures every control that should respond to clicks has a proper handler.
        /// </summary>
        private void SetupAllControlClickHandlers()
        {
            // Setup handlers for all controls recursively
            SetupControlClickHandlersRecursive(this);
        }
        
        /// <summary>
        /// Recursively sets up click handlers for all controls in a container.
        /// </summary>
        private void SetupControlClickHandlersRecursive(Control parent)
        {
            if (parent == null || parent.IsDisposed)
                return;
            
            foreach (Control control in parent.Controls)
            {
                if (control == null || control.IsDisposed)
                    continue;
                
                // Handle different control types
                if (control is PoisonButton button && !HasClickHandler(button))
                {
                    // Only add generic handler if no specific handler exists
                    button.Click += Control_GenericClick;
                }
                else if (control is PoisonDropDownButton dropDown && !HasClickHandler(dropDown))
                {
                    // DropDownButtons usually have context menus, but we can track clicks
                    dropDown.Click += Control_GenericClick;
                }
                else if (control is PoisonToggle toggle && !HasCheckedChangedHandler(toggle))
                {
                    // Toggles use CheckedChanged, but we can also track clicks
                    toggle.Click += Control_GenericClick;
                }
                else if (control is PoisonLabel label && label.Cursor == Cursors.Hand)
                {
                    // Labels with hand cursor should be clickable
                    if (!HasClickHandler(label))
                    {
                        label.Click += Control_GenericClick;
                    }
                }
                else if (control is PoisonTabPage tabPage && !HasClickHandler(tabPage))
                {
                    // Tab pages can be clicked (though usually handled by TabControl)
                    tabPage.Click += Control_GenericClick;
                }
                else if (control is PoisonPanel panel && !HasClickHandler(panel))
                {
                    // Panels can be clickable if needed
                    // Only add if panel has a specific purpose (like log panel)
                    if (panel == panelLog || panel == panelLogControls)
                    {
                        panel.Click += Control_GenericClick;
                    }
                }
                
                // Recursively process child controls
                if (control.HasChildren)
                {
                    SetupControlClickHandlersRecursive(control);
                }
            }
        }
        
        /// <summary>
        /// Checks if a control already has a click event handler attached.
        /// </summary>
        private bool HasClickHandler(Control control)
        {
            if (control == null)
                return false;
            
            try
            {
                var eventField = control.GetType().GetField("EventClick", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.Static);
                
                if (eventField != null)
                {
                    var eventHandler = eventField.GetValue(control) as EventHandler;
                    return eventHandler != null && eventHandler.GetInvocationList().Length > 0;
                }
            }
            catch
            {
                // If reflection fails, assume handler might exist
            }
            
            // Check for known handlers by control name
            string controlName = control.Name;
            if (string.IsNullOrEmpty(controlName))
                return false;
            
            // List of controls that already have specific handlers
            string[] controlsWithHandlers = {
                "btnSelectProjectFolder", "btnSelectOutputFile", "btnCompile",
                "btnSelectInjectFile", "btnInject", "btnResetParseTree", "btnLaunchBO3", "btnKillBO3",
                "btnLogClear", "btnLogCopy", "btnLogSave", "btnSettingsColorStyle",
                "btnSettingsBrowseOutputPath", "lblGame", "tabCompile"
            };
            
            return Array.Exists(controlsWithHandlers, name => controlName == name);
        }
        
        /// <summary>
        /// Checks if a toggle control has a CheckedChanged handler.
        /// </summary>
        private bool HasCheckedChangedHandler(PoisonToggle toggle)
        {
            if (toggle == null)
                return false;
            
            try
            {
                var eventField = toggle.GetType().GetField("EventCheckedChanged",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Static);
                
                if (eventField != null)
                {
                    var eventHandler = eventField.GetValue(toggle) as EventHandler;
                    return eventHandler != null && eventHandler.GetInvocationList().Length > 0;
                }
            }
            catch
            {
                // If reflection fails, check by name
            }
            
            // Known toggles with handlers
            string[] togglesWithHandlers = { "toggleSettingsTheme" };
            return Array.Exists(togglesWithHandlers, name => toggle.Name == name);
        }
        
        /// <summary>
        /// Generic click handler for controls that don't have specific handlers.
        /// This can be used for debugging, logging, or default behavior.
        /// </summary>
        private void Control_GenericClick(object sender, EventArgs e)
        {
            if (sender is Control control)
            {
                string controlName = control.Name;
                string controlType = control.GetType().Name;
                
                // Log the click for debugging (optional)
                // System.Diagnostics.Debug.WriteLine($"Control clicked: {controlType} - {controlName}");
                
                // Handle specific controls that need default behavior
                switch (controlName)
                {
                    case "lblProjectFolder":
                    case "lblOutputFile":
                    case "lblInjectFile":
                    case "lblInjectPath":
                        // Labels next to text boxes - could focus the text box
                        FocusAssociatedTextBox(control);
                        break;
                    
                    case "lblOpcodeMasking":
                        // Toggle the associated checkbox
                        if (chkOpcodeMasking != null)
                            chkOpcodeMasking.Checked = !chkOpcodeMasking.Checked;
                        break;
                    
                    case "lblSaveOpcodeMap":
                        // Toggle the associated checkbox
                        if (chkSaveOpcodeMap != null)
                            chkSaveOpcodeMap.Checked = !chkSaveOpcodeMap.Checked;
                        break;
                    
                    case "lblNoRuntime":
                        // Toggle the associated checkbox
                        if (chkNoRuntime != null)
                            chkNoRuntime.Checked = !chkNoRuntime.Checked;
                        break;
                    
                    case "lblSettingsTheme":
                        // Toggle the theme toggle
                        if (toggleSettingsTheme != null)
                            toggleSettingsTheme.Checked = !toggleSettingsTheme.Checked;
                        break;
                    
                    case "lblSettingsColorStyle":
                        // Open color style dropdown
                        if (btnSettingsColorStyle != null && btnSettingsColorStyle.Enabled)
                            btnSettingsColorStyle.PerformClick();
                        break;
                    
                    case "lblSettingsDefaultOutputPath":
                        // Focus the output path text box
                        if (txtSettingsDefaultOutputPath != null)
                            txtSettingsDefaultOutputPath.Focus();
                        break;
                    
                    case "lblLogSearch":
                        // Focus the search text box
                        if (txtLogSearch != null)
                            txtLogSearch.Focus();
                        break;
                    
                    case "panelLog":
                        // Clicking log panel could focus the log text box
                        if (txtLog != null)
                            txtLog.Focus();
                        break;
                    
                    default:
                        // For unknown controls, just ensure they're properly handled
                        // This is a fallback - most controls should have specific handlers
                        break;
                }
            }
        }
        
        /// <summary>
        /// Focuses the text box associated with a label control.
        /// </summary>
        private void FocusAssociatedTextBox(Control label)
        {
            if (label == null || label.Parent == null)
                return;
            
            // Find the next text box in the same parent
            Control nextControl = label.Parent.GetNextControl(label, true);
            while (nextControl != null && nextControl != label)
            {
                if (nextControl is PoisonTextBox || nextControl is System.Windows.Forms.TextBox)
                {
                    nextControl.Focus();
                    return;
                }
                nextControl = label.Parent.GetNextControl(nextControl, true);
            }
        }
        
        /// <summary>
        /// Gets all clickable controls in the form for debugging/inspection.
        /// </summary>
        public List<Control> GetAllClickableControls()
        {
            List<Control> clickableControls = new List<Control>();
            CollectClickableControlsRecursive(this, clickableControls);
            return clickableControls;
        }
        
        /// <summary>
        /// Recursively collects all clickable controls.
        /// </summary>
        private void CollectClickableControlsRecursive(Control parent, List<Control> clickableControls)
        {
            if (parent == null || parent.IsDisposed)
                return;
            
            foreach (Control control in parent.Controls)
            {
                if (control == null || control.IsDisposed)
                    continue;
                
                // Add controls that are typically clickable
                if (control is PoisonButton ||
                    control is PoisonDropDownButton ||
                    control is PoisonToggle ||
                    (control is PoisonLabel && control.Cursor == Cursors.Hand) ||
                    control is PoisonTabPage ||
                    (control is PoisonPanel && (control == panelLog || control == panelLogControls)))
                {
                    clickableControls.Add(control);
                }
                
                // Recursively process child controls
                if (control.HasChildren)
                {
                    CollectClickableControlsRecursive(control, clickableControls);
                }
            }
        }
        
        #endregion
        
        #region About Tab
        
        private void UpdateAboutTab()
        {
            // Update version text dynamically (text is set in Designer as default)
            if (lblVersion != null)
            {
                // Try to get version from AssemblyFileVersion first, then fall back to AssemblyVersion
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var fileVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
                string versionString;
                
                if (!string.IsNullOrEmpty(fileVersion.FileVersion))
                {
                    // Use FileVersion (e.g., "3.6.9.0")
                    var versionParts = fileVersion.FileVersion.Split('.');
                    if (versionParts.Length >= 3)
                    {
                        versionString = $"{versionParts[0]}.{versionParts[1]}.{versionParts[2]}";
                    }
                    else
                    {
                        versionString = fileVersion.FileVersion;
                    }
                }
                else
                {
                    // Fall back to AssemblyVersion
                    var version = assembly.GetName().Version;
                    versionString = $"{version.Major}.{version.Minor}.{version.Build}";
                }
                
                lblVersion.Text = $"T7 GSC Compiler v{versionString}";
            }
            
            // About text is now set in Designer, but we can update it here if needed
            // The text is already set in Designer with default content
        }
        
        #endregion
        
        #region Keyboard Shortcuts
        
        private void SetupKeyboardShortcuts()
        {
            // Initialize with default keybinds if not loaded from settings
            if (keybinds == null || keybinds.Count == 0)
            {
                keybinds = Dialogs.KeybindDialog.GetDefaultKeybinds();
                // Save defaults if auto-save is enabled
                if (autoSaveSettings)
                {
                    SaveAllSettings();
                }
            }
            
            // Migrate old Compile keybind from Ctrl+C to F3 if present
            if (keybinds != null && keybinds.ContainsKey("Compile"))
            {
                var compileKeybind = keybinds["Compile"];
                if (compileKeybind.Key == System.Windows.Forms.Keys.C && compileKeybind.Ctrl && !compileKeybind.Shift && !compileKeybind.Alt)
                {
                    // Update old Ctrl+C to F3 and save
                    keybinds["Compile"] = new Dialogs.KeybindDialog.KeybindInfo("Compile", System.Windows.Forms.Keys.F3);
                    if (autoSaveSettings)
                    {
                        SaveAllSettings();
                    }
                }
            }
            
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;
        }
        
        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Handle F5/F6 directly (not through keybinds to avoid duplicates)
            if (e.KeyCode == System.Windows.Forms.Keys.F5 && !e.Control && !e.Shift && !e.Alt)
            {
                if (btnCompile != null && btnCompile.Enabled && !isCompiling)
                {
                    btnCompile_Click(sender, e);
                    e.Handled = true;
                    return;
                }
            }
            else if (e.KeyCode == System.Windows.Forms.Keys.F6 && !e.Control && !e.Shift && !e.Alt)
            {
                if (btnInject != null && btnInject.Enabled && !isInjecting)
                {
                    btnInject_Click(sender, e);
                    e.Handled = true;
                    return;
                }
            }
            
            if (keybinds == null) return;
            
            // Check each keybind
            foreach (var keybind in keybinds.Values)
            {
                if (keybind.Key == e.KeyCode &&
                    keybind.Ctrl == e.Control &&
                    keybind.Shift == e.Shift &&
                    keybind.Alt == e.Alt)
                {
                    ExecuteKeybindAction(keybind.Action, sender, e);
                    e.Handled = true;
                    return;
                }
            }
        }
        
        private void ExecuteKeybindAction(string action, object sender, KeyEventArgs e)
        {
            switch (action)
            {
                case "Compile":
                    if (btnCompile != null && btnCompile.Enabled && !isCompiling)
                    {
                        btnCompile_Click(sender, e);
                    }
                    break;
                case "Inject":
                    if (btnInject != null && btnInject.Enabled && !isInjecting)
                    {
                        btnInject_Click(sender, e);
                    }
                    break;
                case "Open Project":
                    if (btnSelectProjectFolder != null)
                    {
                        btnSelectProjectFolder_Click(sender, e);
                    }
                    break;
                case "Save Log":
                    if (btnLogSave != null)
                    {
                        btnLogSave_Click(sender, e);
                    }
                    break;
                case "Focus Search":
                    txtLogSearch?.Focus();
                    break;
                case "Clear Log":
                    if (btnLogClear != null)
                    {
                        btnLogClear_Click(sender, e);
                    }
                    break;
                case "Cycle Tabs":
                    if (tabControl != null)
                    {
                        int nextIndex = (tabControl.SelectedIndex + 1) % tabControl.TabCount;
                        tabControl.SelectedIndex = nextIndex;
                    }
                    break;
            }
        }
        
        #region Settings Tab Event Handlers
        
        private void toggleSettingsTheme_CheckedChanged(object sender, EventArgs e)
        {
            if (toggleSettingsTheme != null)
            {
                // Toggle: Checked = Light, Unchecked = Dark
                ThemeStyle newTheme = toggleSettingsTheme.Checked ? ThemeStyle.Light : ThemeStyle.Dark;
                ChangeTheme(newTheme);
                
                // Update toggle text
                toggleSettingsTheme.Text = toggleSettingsTheme.Checked ? "Light" : "Dark";
            }
        }
        
        private void btnSettingsColorStyle_Click(object sender, EventArgs e)
        {
            // Color style selection is handled by the dropdown menu items
            // This handler is kept for compatibility but the actual logic is in the menu item handlers
        }
        
        private void txtSettingsDefaultOutputPath_TextChanged(object sender, EventArgs e)
        {
            if (txtSettingsDefaultOutputPath != null)
            {
                defaultOutputPath = txtSettingsDefaultOutputPath.Text;
                SaveAllSettings();
                
                // If this is the first time setting the default output path, update the compile tab output file
                // (only if output file is empty or using default build folder path)
                if (txtOutputFile != null && !string.IsNullOrWhiteSpace(txtSettingsDefaultOutputPath.Text))
                {
                    string currentOutput = txtOutputFile.Text;
                    string defaultBuildOutput = Path.Combine(buildFolder, "compiled.gscc");
                    
                    // Only update if output file is empty or using default build folder path
                    bool shouldUpdate = string.IsNullOrWhiteSpace(currentOutput) || 
                                       currentOutput == defaultBuildOutput ||
                                       (!File.Exists(currentOutput) && currentOutput == defaultBuildOutput);
                    
                    if (shouldUpdate)
                    {
                        string defaultPath = txtSettingsDefaultOutputPath.Text.Trim();
                        
                        if (Directory.Exists(defaultPath))
                        {
                            // It's a directory - combine with default filename
                            txtOutputFile.Text = Path.Combine(defaultPath, "compiled.gscc");
                        }
                        else if (Path.IsPathRooted(defaultPath))
                        {
                            // It's a full file path - use it directly
                            txtOutputFile.Text = defaultPath;
                        }
                        else
                        {
                            // Relative path - combine with project folder if available
                            if (!string.IsNullOrWhiteSpace(txtProjectFolder.Text))
                            {
                                string expandedProjectFolder = Helpers.PathHelper.ExpandPath(txtProjectFolder.Text);
                                if (Directory.Exists(expandedProjectFolder))
                                {
                                    txtOutputFile.Text = Path.Combine(expandedProjectFolder, defaultPath);
                                }
                                else
                                {
                                    // Fallback to build folder
                                    EnsureBuildFolderExists();
                                    txtOutputFile.Text = Path.Combine(buildFolder, defaultPath);
                                }
                            }
                            else
                            {
                                // Fallback to build folder
                                EnsureBuildFolderExists();
                                txtOutputFile.Text = Path.Combine(buildFolder, defaultPath);
                            }
                        }
                    }
                }
            }
        }
        
        private void btnSettingsBrowseOutputPath_Click(object sender, EventArgs e)
        {
            string initialPath = txtSettingsDefaultOutputPath?.Text;
            if (string.IsNullOrWhiteSpace(initialPath))
            {
                initialPath = buildFolder;
            }
            else
            {
                // Ensure we start from a directory, not a file path
                if (File.Exists(initialPath))
                {
                    // If it's a file, use its directory
                    initialPath = Path.GetDirectoryName(initialPath);
                }
                else if (!Directory.Exists(initialPath))
                {
                    // If neither file nor directory exists, use build folder
                    initialPath = buildFolder;
                }
            }
            
            // Check if this is the first time setting the default output path
            // (output file is empty or using default build folder path)
            bool isFirstTime = false;
            if (txtOutputFile != null)
            {
                string currentOutput = txtOutputFile.Text;
                string defaultBuildOutput = Path.Combine(buildFolder, "compiled.gscc");
                
                // Consider it first time if output file is empty or using default build folder path
                isFirstTime = string.IsNullOrWhiteSpace(currentOutput) || 
                             currentOutput == defaultBuildOutput ||
                             (!File.Exists(currentOutput) && currentOutput == defaultBuildOutput);
            }
            
            string folder = Helpers.ModernFolderDialog.Show(this, "Select default output folder", initialPath);
            if (!string.IsNullOrEmpty(folder) && txtSettingsDefaultOutputPath != null)
            {
                txtSettingsDefaultOutputPath.Text = folder;
                defaultOutputPath = folder;
                
                // If this is the first time setting the default output path, update the compile tab output file
                if (isFirstTime && txtOutputFile != null)
                {
                    // Use the same logic as AutoPopulateOutputFile to set the output file
                    string defaultPath = folder.Trim();
                    
                    if (Directory.Exists(defaultPath))
                    {
                        // It's a directory - combine with default filename
                        txtOutputFile.Text = Path.Combine(defaultPath, "compiled.gscc");
                    }
                    else if (Path.IsPathRooted(defaultPath))
                    {
                        // It's a full file path - use it directly
                        txtOutputFile.Text = defaultPath;
                    }
                    else
                    {
                        // Relative path - combine with project folder if available
                        if (!string.IsNullOrWhiteSpace(txtProjectFolder.Text))
                        {
                            string expandedProjectFolder = Helpers.PathHelper.ExpandPath(txtProjectFolder.Text);
                            if (Directory.Exists(expandedProjectFolder))
                            {
                                txtOutputFile.Text = Path.Combine(expandedProjectFolder, defaultPath);
                            }
                            else
                            {
                                // Fallback to build folder
                                EnsureBuildFolderExists();
                                txtOutputFile.Text = Path.Combine(buildFolder, defaultPath);
                            }
                        }
                        else
                        {
                            // Fallback to build folder
                            EnsureBuildFolderExists();
                            txtOutputFile.Text = Path.Combine(buildFolder, defaultPath);
                        }
                    }
                }
            }
        }
        
        // Auto save and restore window state are now always enabled - toggles removed
        
        private void btnSettingsOpenConfigFolder_Click(object sender, EventArgs e)
        {
            try
            {
                string configPath = GetConfigPath();
                string configDir = Path.GetDirectoryName(configPath);
                string tempPath = Path.GetTempPath();
                
                // Get DLL extract path (where Costura extracts embedded DLLs)
                string appDataLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string appDataRoaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string dllExtractPath = Path.Combine(appDataLocal, "Costura", Assembly.GetExecutingAssembly().GetName().Name);
                
                using (var dialog = new ConfigLocationsDialog(poisonStyleManager, configPath, tempPath, dllExtractPath, appDataRoaming, this))
                {
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        // Save the new paths if they were changed
                        // Temp path and DLL extract path could be saved to config if needed
                        // For now, we'll just show the dialog - actual path changes would require
                        // modifying how the application uses these paths
                        
                        AppendLog($"[INFO] Application locations updated.\r\n", LogLevel.Info);
                    }
                }
            }
            catch (Exception ex)
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, $"Error opening locations dialog: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        
        #endregion
        
        private void btnKeybinds_Click(object sender, EventArgs e)
        {
            using (var dialog = new KeybindDialog(poisonStyleManager, keybinds ?? KeybindDialog.GetDefaultKeybinds(), this))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    keybinds = dialog.Keybinds;
                    SaveKeybinds();
                    AppendLog("[INFO] Keyboard shortcuts updated.\r\n", LogLevel.Info);
                }
            }
        }
        
        private void SaveKeybinds()
        {
            // Keybinds are now saved as part of SaveAllSettings()
            // This method is kept for compatibility but triggers the unified save
            if (autoSaveSettings)
            {
                SaveAllSettings();
            }
        }
        
        private void LoadKeybinds()
        {
            try
            {
                string configPath = GetConfigPath();
                if (!File.Exists(configPath))
                {
                    // No config file - use defaults
                    keybinds = Dialogs.KeybindDialog.GetDefaultKeybinds();
                    return;
                }
                
                XDocument config = XDocument.Load(configPath);
                XElement keybindsElem = config.Root?.Element("Keybinds");
                if (keybindsElem == null)
                {
                    // No keybinds in config - use defaults
                    keybinds = Dialogs.KeybindDialog.GetDefaultKeybinds();
                    return;
                }
                
                keybinds = new Dictionary<string, Dialogs.KeybindDialog.KeybindInfo>();
                bool hasValidKeybinds = false;
                
                foreach (var keybindElem in keybindsElem.Elements("Keybind"))
                {
                    string action = keybindElem.Element("Action")?.Value;
                    string keyStr = keybindElem.Element("Key")?.Value;
                    bool ctrl = bool.Parse(keybindElem.Element("Ctrl")?.Value ?? "false");
                    bool shift = bool.Parse(keybindElem.Element("Shift")?.Value ?? "false");
                    bool alt = bool.Parse(keybindElem.Element("Alt")?.Value ?? "false");
                    
                    if (!string.IsNullOrEmpty(action) && !string.IsNullOrEmpty(keyStr))
                    {
                        if (Enum.TryParse<System.Windows.Forms.Keys>(keyStr, out System.Windows.Forms.Keys key))
                        {
                            keybinds[action] = new Dialogs.KeybindDialog.KeybindInfo(action, key, ctrl, shift, alt);
                            hasValidKeybinds = true;
                        }
                    }
                }
                
                // If no valid keybinds were loaded, use defaults
                if (!hasValidKeybinds || keybinds.Count == 0)
                {
                    keybinds = Dialogs.KeybindDialog.GetDefaultKeybinds();
                }
                
                // Migrate old Compile keybind from Ctrl+C to F3
                if (keybinds.ContainsKey("Compile"))
                {
                    var compileKeybind = keybinds["Compile"];
                    if (compileKeybind.Key == System.Windows.Forms.Keys.C && compileKeybind.Ctrl && !compileKeybind.Shift && !compileKeybind.Alt)
                    {
                        // Update old Ctrl+C to F3 and save
                        keybinds["Compile"] = new Dialogs.KeybindDialog.KeybindInfo("Compile", System.Windows.Forms.Keys.F3);
                        // Save the migrated keybind
                        SaveAllSettings();
                    }
                }
            }
            catch
            {
                // If loading fails, use defaults
                keybinds = Dialogs.KeybindDialog.GetDefaultKeybinds();
            }
        }
        
        #endregion
        
        #region Recent Projects
        
        private List<string> recentProjects = new List<string>();
        private List<string> recentFiles = new List<string>();
        
        private void AddToRecentProjects(string projectPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(projectPath))
                    return;
                
                // Just store the path as-is - don't normalize or validate
                // The path from browse dialog is already valid, and we just need to remember it
                string trimmedPath = projectPath.Trim();
                
                // Remove if already exists (case-insensitive comparison)
                recentProjects.RemoveAll(p => string.Equals(p.Trim(), trimmedPath, StringComparison.OrdinalIgnoreCase));
                
                // Add path to front (store as-is)
                recentProjects.Insert(0, trimmedPath);
                
                // Limit to MAX_RECENT_PROJECTS
                if (recentProjects.Count > MAX_RECENT_PROJECTS)
                    recentProjects.RemoveAt(recentProjects.Count - 1);
                
                SaveRecentProjects();
                UpdateRecentProjectsMenu();
                // Highlight the current project in the dropdown (only if not in middle of user selection)
                if (!isUpdatingProjectFromSelection)
                {
                    UpdateRecentProjectsSelection(trimmedPath);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                System.Diagnostics.Debug.WriteLine($"Error in AddToRecentProjects: {ex.Message}");
            }
        }
        
        private void AddToRecentFiles(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return;
            
            // Only add .gsc and .gscc files
            string ext = Path.GetExtension(filePath).ToLower();
            if (ext != ".gsc" && ext != ".gscc")
                return;
            
            // Remove if already exists
            recentFiles.Remove(filePath);
            
            // Add to front
            recentFiles.Insert(0, filePath);
            
            // Limit to MAX_RECENT_FILES
            if (recentFiles.Count > MAX_RECENT_FILES)
                recentFiles.RemoveAt(recentFiles.Count - 1);
            
            SaveRecentProjects();
            UpdateRecentFilesMenu();
        }
        
        private void SaveRecentProjects()
        {
            // Recent projects are now saved as part of SaveAllSettings()
            SaveAllSettings();
        }
        
        private void BtnRecentProjects_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (btnRecentProjects == null || recentProjectsMenu == null) 
                {
                    return;
                }
                
                // Prevent recursive calls when we're programmatically updating the selection
                if (isUpdatingProjectFromSelection) 
                {
                    return;
                }
                
                int selectedIndex = btnRecentProjects.SelectedIndex;
                if (selectedIndex < 0 || selectedIndex >= recentProjectsMenu.Items.Count)
                {
                    return;
                }
                
                var menuItem = recentProjectsMenu.Items[selectedIndex] as ToolStripMenuItem;
                if (menuItem == null || !menuItem.Enabled || menuItem.Text == "(No recent projects)")
                {
                    return;
                }
                
                // Update button text immediately to show the selected project name
                if (!string.IsNullOrEmpty(menuItem.Text))
                {
                    btnRecentProjects.Text = menuItem.Text;
                }
                
                // Get path from Tag - this is set when menu items are created in UpdateRecentProjectsMenu()
                // Tag stores the full project path as a string
                string path = menuItem.Tag as string;
                
                // Fallback: if Tag is not a string or is null, try ToolTipText (also contains the full path)
                if (string.IsNullOrWhiteSpace(path))
                {
                    path = menuItem.ToolTipText;
                }
                
                // Final fallback: use the recentProjects list by index (should match since menu is built from this list)
                if (string.IsNullOrWhiteSpace(path) && selectedIndex >= 0 && selectedIndex < recentProjects.Count)
                {
                    path = recentProjects[selectedIndex];
                }
                
                // Validate the path
                if (string.IsNullOrWhiteSpace(path) || path.Length < 2)
                {
                    AppendLogText($"[ERROR] Could not get valid path from recent projects (index: {selectedIndex})\r\n");
                    return;
                }
                
                // Switch to the selected project
                SwitchToProject(path);
            }
            catch (Exception ex)
            {
                AppendLogText($"[ERROR] Error selecting recent project: {ex.Message}\r\n");
                System.Diagnostics.Debug.WriteLine($"Error in BtnRecentProjects_SelectedIndexChanged: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Adjusts txtProjectFolder width when btnRecentProjects size or location changes to prevent overlap
        /// </summary>
        private void BtnRecentProjects_SizeChanged(object sender, EventArgs e)
        {
            AdjustProjectFolderWidth();
        }
        
        /// <summary>
        /// Clears the text selection in txtInjectFile to prevent highlighting when switching to inject tab
        /// </summary>
        private void ClearInjectFileSelection()
        {
            try
            {
                if (txtInjectFile != null && !txtInjectFile.IsDisposed)
                {
                    // Use BeginInvoke to ensure this happens after any focus/selection changes
                    this.BeginInvoke(new Action(() =>
                    {
                        if (txtInjectFile != null && !txtInjectFile.IsDisposed)
                        {
                            // Move selection to end with zero length to clear any highlighting
                            txtInjectFile.SelectionStart = txtInjectFile.Text?.Length ?? 0;
                            txtInjectFile.SelectionLength = 0;
                        }
                    }));
                }
            }
            catch
            {
                // Silently ignore errors
            }
        }
        
        /// <summary>
        /// Adjusts txtProjectFolder width to prevent overlap with btnRecentProjects
        /// </summary>
        private void AdjustProjectFolderWidth()
        {
            try
            {
                if (btnRecentProjects == null || txtProjectFolder == null)
                    return;
                
                // Calculate the gap we want between txtProjectFolder and btnRecentProjects (in pixels)
                const int gap = 5;
                
                // Get the left edge of btnRecentProjects
                int btnRecentProjectsLeft = btnRecentProjects.Location.X;
                
                // Calculate the maximum width for txtProjectFolder
                int txtProjectFolderLeft = txtProjectFolder.Location.X;
                int maxWidth = btnRecentProjectsLeft - txtProjectFolderLeft - gap;
                
                // Ensure minimum width (at least 100 pixels)
                if (maxWidth < 100)
                    maxWidth = 100;
                
                // Only update if the width actually needs to change (avoid unnecessary updates)
                if (Math.Abs(txtProjectFolder.Width - maxWidth) <= 1)
                    return;
                
                // Suspend layout on parent to prevent flickering
                Control parent = txtProjectFolder.Parent;
                if (parent != null)
                    parent.SuspendLayout();
                
                try
                {
                    // Since txtProjectFolder is anchored Top|Left|Right, we need to temporarily
                    // remove the Right anchor, set the width, then restore the anchor
                    AnchorStyles originalAnchor = txtProjectFolder.Anchor;
                    
                    // Remove Right anchor temporarily
                    txtProjectFolder.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                    
                    // Set the new width
                    txtProjectFolder.Width = maxWidth;
                    
                    // Restore the Right anchor
                    txtProjectFolder.Anchor = originalAnchor;
                }
                finally
                {
                    if (parent != null)
                        parent.ResumeLayout(true);
                }
            }
            catch (Exception ex)
            {
                // Silently handle errors - don't crash the UI
                System.Diagnostics.Debug.WriteLine($"Error in AdjustProjectFolderWidth: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Switches to the specified project folder - does the same thing as browse button
        /// </summary>
        private void SwitchToProject(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return;
                
                string pathToUse = path.Trim();
                string expandedPath = Helpers.PathHelper.ExpandPath(pathToUse);
                
                // Check if directory exists
                if (!Directory.Exists(expandedPath) && !Directory.Exists(pathToUse))
                {
                    ReaLTaiizor.Controls.PoisonMessageBox.Show(this, 
                        $"The selected project folder no longer exists:\n{pathToUse}", 
                        "Folder Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    // Remove invalid path from list
                    recentProjects.RemoveAll(p => string.Equals(p.Trim(), pathToUse, StringComparison.OrdinalIgnoreCase));
                    SaveRecentProjects();
                    UpdateRecentProjectsMenu();
                    return;
                }
                
                // Use the path that exists
                if (!Directory.Exists(expandedPath))
                    expandedPath = pathToUse;
                
                // Do exactly what browse button does - ensure ALL steps happen:
                // 1. Set project folder (triggers txtProjectFolder_TextChanged which loads gsc.conf and updates UI)
                if (txtProjectFolder != null)
                {
                    isUpdatingProjectFromSelection = true;
                    try
                    {
                        txtProjectFolder.Text = expandedPath;
                    }
                    finally
                    {
                        isUpdatingProjectFromSelection = false;
                    }
                }
                
                // ALWAYS explicitly call these methods to ensure they happen even if TextChanged doesn't fire
                // (TextChanged won't fire if the text is already set to the same value)
                LoadGscConfSettings();
                AutoPopulateOutputFile();
                
                // 2. Add to recent projects
                AddToRecentProjects(expandedPath);
                
                // 3. Highlight the selected project in recent projects dropdown
                UpdateRecentProjectsSelection(expandedPath);
                
                // 4. Update UI (txtProjectFolder_TextChanged also calls this, but ensure it's called)
                UpdateUI();
                
                // 5. Notify CodeEditorForm safely - OpenFolder will trigger fresh scan for ifdefs
                try
                {
                    if (codeEditorForm != null && !codeEditorForm.IsDisposed && codeEditorForm.IsHandleCreated)
                    {
                        // OpenFolder calls LoadSymbolsFromGscConf which calls ScanProjectForSymbols()
                        // This ensures a fresh scan of ifdefs when switching projects
                        codeEditorForm.OpenFolder(expandedPath);
                    }
                }
                catch
                {
                    // Ignore CodeEditorForm errors - don't let it crash the main form
                }
                
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                System.Diagnostics.Debug.WriteLine($"Error in SwitchToProject: {ex.Message}");
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, 
                    $"Error switching project: {ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void UpdateRecentProjectsMenu()
        {
            if (btnRecentProjects == null || recentProjectsMenu == null) return;
            
            // Save current selection before clearing menu
            int savedSelectedIndex = -1;
            string savedSelectedPath = null;
            if (btnRecentProjects.BehaveLikeComboBox && btnRecentProjects.SelectedIndex >= 0 && 
                btnRecentProjects.SelectedIndex < recentProjectsMenu.Items.Count)
            {
                savedSelectedIndex = btnRecentProjects.SelectedIndex;
                var savedItem = recentProjectsMenu.Items[savedSelectedIndex] as ToolStripMenuItem;
                if (savedItem != null && savedItem.Tag != null)
                {
                    savedSelectedPath = savedItem.Tag.ToString();
                }
            }
            
            // Clear existing items
            recentProjectsMenu.Items.Clear();
            if (btnRecentProjects.BehaveLikeComboBox)
            {
                btnRecentProjects.ClearItems();
            }
            
            // Filter to only existing directories - but keep original paths in the list
            // Just check if they exist, don't modify them
            var validProjects = recentProjects
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Where(p => {
                    try
                    {
                        string expanded = Helpers.PathHelper.ExpandPath(p.Trim());
                        return Directory.Exists(expanded) || Directory.Exists(p.Trim());
                    }
                    catch
                    {
                        return false;
                    }
                })
                .ToList();
            recentProjects = validProjects; // Update the list, but keep original paths
            
            if (validProjects.Count == 0)
            {
                ToolStripMenuItem noItems = new ToolStripMenuItem("(No recent projects)");
                noItems.Enabled = false;
                recentProjectsMenu.Items.Add(noItems);
                btnRecentProjects.Enabled = false;
                if (btnRecentProjects.BehaveLikeComboBox)
                {
                    btnRecentProjects.Text = "Recent";
                }
            }
            else
            {
                btnRecentProjects.Enabled = true;
                int newSelectedIndex = -1;
                
                foreach (string projectPath in validProjects)
                {
                    string displayName = Path.GetFileName(projectPath);
                    if (string.IsNullOrEmpty(displayName))
                        displayName = projectPath;
                    
                    ToolStripMenuItem item = new ToolStripMenuItem(displayName);
                    item.ToolTipText = projectPath;
                    item.Tag = projectPath;
                    item.Click += (s, e) =>
                    {
                        string path = ((ToolStripMenuItem)s).Tag.ToString();
                        SwitchToProject(path);
                    };
                    recentProjectsMenu.Items.Add(item);
                    
                    // Add to ComboBox items if enabled
                    if (btnRecentProjects.BehaveLikeComboBox)
                    {
                        btnRecentProjects.AddItem(displayName);
                        
                        // Check if this is the saved selected path
                        if (savedSelectedPath != null && string.Equals(projectPath, savedSelectedPath, StringComparison.OrdinalIgnoreCase))
                        {
                            newSelectedIndex = recentProjectsMenu.Items.Count - 1;
                        }
                    }
                }
                
                // Restore selection if valid and not in middle of user selection
                // Set flag to prevent triggering SelectedIndexChanged when restoring
                if (btnRecentProjects.BehaveLikeComboBox && !isUpdatingProjectFromSelection)
                {
                    isUpdatingProjectFromSelection = true;
                    try
                    {
                        if (newSelectedIndex >= 0 && newSelectedIndex < btnRecentProjects.Items.Count)
                        {
                            // Restore by matching path
                            btnRecentProjects.SelectedIndex = newSelectedIndex;
                        }
                        else if (savedSelectedIndex >= 0 && savedSelectedIndex < btnRecentProjects.Items.Count && savedSelectedIndex < validProjects.Count)
                        {
                            // Fallback to index if path match failed, but only if index is still valid
                            btnRecentProjects.SelectedIndex = savedSelectedIndex;
                        }
                    }
                    finally
                    {
                        isUpdatingProjectFromSelection = false;
                    }
                }
                
                // Enhance menu items with Poison styling
                if (poisonStyleManager != null)
                {
                    EnhanceMenuItems(recentProjectsMenu);
                }
            }
        }
        
        /// <summary>
        /// Updates the recent projects dropdown to highlight the currently loaded project
        /// </summary>
        private void UpdateRecentProjectsSelection(string currentProjectPath)
        {
            if (btnRecentProjects == null || !btnRecentProjects.BehaveLikeComboBox || string.IsNullOrEmpty(currentProjectPath))
                return;
            
            // Prevent triggering SelectedIndexChanged when we're updating programmatically
            isUpdatingProjectFromSelection = true;
            try
            {
                // Simple comparison - just compare paths directly (case-insensitive)
                string currentTrimmed = currentProjectPath.Trim();
                
                // Find the index by matching the path in the menu items (not the recentProjects list)
                // This ensures we match against what's actually displayed in the menu
                int index = -1;
                for (int i = 0; i < recentProjectsMenu.Items.Count; i++)
                {
                    var item = recentProjectsMenu.Items[i] as ToolStripMenuItem;
                    if (item != null && item.Tag != null)
                    {
                        string itemPath = item.Tag.ToString();
                        if (string.Equals(itemPath.Trim(), currentTrimmed, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(Helpers.PathHelper.ExpandPath(itemPath.Trim()), Helpers.PathHelper.ExpandPath(currentTrimmed), StringComparison.OrdinalIgnoreCase))
                        {
                            index = i;
                            break;
                        }
                    }
                }
                
                if (index >= 0 && index < btnRecentProjects.Items.Count)
                {
                    // Set the selected index to highlight the current project
                    btnRecentProjects.SelectedIndex = index;
                    
                    // Explicitly update the button text to show the selected project name
                    // This ensures the text is accurate even if the ComboBox behavior doesn't update it immediately
                    var selectedItem = recentProjectsMenu.Items[index] as ToolStripMenuItem;
                    if (selectedItem != null && !string.IsNullOrEmpty(selectedItem.Text))
                    {
                        btnRecentProjects.Text = selectedItem.Text;
                    }
                }
                else
                {
                    // Current project not in list, clear selection and reset to default text
                    btnRecentProjects.SelectedIndex = -1;
                    btnRecentProjects.Text = "Recent";
                }
            }
            finally
            {
                isUpdatingProjectFromSelection = false;
            }
        }
        
        private void UpdateRecentFilesMenu()
        {
            if (btnRecentFiles == null || recentFilesMenu == null) return;
            
            // Clear existing items
            recentFilesMenu.Items.Clear();
            if (btnRecentFiles.BehaveLikeComboBox)
            {
                btnRecentFiles.ClearItems();
            }
            
            // Filter to only existing files
            var validFiles = recentFiles.Where(f => File.Exists(f)).ToList();
            recentFiles = validFiles; // Update the list
            
            if (validFiles.Count == 0)
            {
                ToolStripMenuItem noItems = new ToolStripMenuItem("(No recent files)");
                noItems.Enabled = false;
                recentFilesMenu.Items.Add(noItems);
                btnRecentFiles.Enabled = false;
                if (btnRecentFiles.BehaveLikeComboBox)
                {
                    btnRecentFiles.Text = "Recent";
                }
            }
            else
            {
                btnRecentFiles.Enabled = true;
                foreach (string filePath in validFiles)
                {
                    string displayName = Path.GetFileName(filePath);
                    string directory = Path.GetDirectoryName(filePath);
                    
                    ToolStripMenuItem item = new ToolStripMenuItem(displayName);
                    item.ToolTipText = filePath;
                    item.Tag = filePath;
                    item.Click += (s, e) =>
                    {
                        string path = ((ToolStripMenuItem)s).Tag.ToString();
                        if (File.Exists(path))
                        {
                            txtInjectFile.Text = path;
                            // Clear selection immediately to prevent text from being highlighted
                            ClearInjectFileSelection();
                            UpdateUI();
                            // Update button text if using ComboBox behavior
                            if (btnRecentFiles.BehaveLikeComboBox)
                            {
                                btnRecentFiles.SelectedItem = displayName;
                            }
                        }
                        else
                        {
                            ReaLTaiizor.Controls.PoisonMessageBox.Show(this, 
                                "The selected file no longer exists.", 
                                "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            // Remove from list
                            recentFiles.Remove(path);
                            UpdateRecentFilesMenu();
                        }
                    };
                    recentFilesMenu.Items.Add(item);
                    
                    // Add to ComboBox items if enabled
                    if (btnRecentFiles.BehaveLikeComboBox)
                    {
                        btnRecentFiles.AddItem(displayName);
                    }
                }
                
                // Enhance menu items with Poison styling
                if (poisonStyleManager != null)
                {
                    EnhanceMenuItems(recentFilesMenu);
                }
            }
        }
        
        
        #endregion
        
        #region Unified Config System
        
        private const string CONFIG_FILE_NAME = "T7CompilerGUI.conf";
        
        private string GetConfigPath()
        {
            string startupPath = Application.StartupPath;
            
            // Check if startup path is writable, if not use AppData
            try
            {
                string testFile = Path.Combine(startupPath, ".writable_test");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return Path.Combine(startupPath, CONFIG_FILE_NAME);
        }
            catch
            {
                // Startup path is not writable, use AppData instead
                string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "T7CompilerGUI");
                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath);
                }
                return Path.Combine(appDataPath, CONFIG_FILE_NAME);
            }
        }
        
        private XDocument BuildConfigDocument()
        {
                XDocument config = new XDocument(
                    new XComment("T7CompilerGUI Configuration File"),
                    new XComment("This file stores all application settings. You can edit it manually if needed."),
                    new XComment("WARNING: Editing this file while the application is running may cause your changes to be overwritten."),
                    new XElement("T7CompilerConfig",
                        new XComment("=== Application Settings ==="),
                        new XComment("Theme: Dark or Light"),
                        new XComment("ColorStyle: Red, Blue, Green, Orange, Yellow, Purple, Teal, Lime, Brown, Pink, Magenta, Silver, Gold"),
                        new XElement("Application",
                            new XElement("Theme", poisonStyleManager != null ? poisonStyleManager.Theme.ToString() : "Dark"),
                            new XElement("ColorStyle", poisonStyleManager != null ? (IsRainbowStyleActive() ? "Rainbow" : poisonStyleManager.Style.ToString()) : "Red"),
                            new XElement("DefaultOutputPath", txtSettingsDefaultOutputPath != null ? txtSettingsDefaultOutputPath.Text : (defaultOutputPath ?? "")),
                            new XElement("AutoSaveSettings", autoSaveSettings ? "1" : "0"),
                            new XElement("RestoreWindowState", restoreWindowState ? "1" : "0"),
                            new XElement("LastProjectFolder", txtProjectFolder != null ? txtProjectFolder.Text : ""),
                            new XElement("CodeEditorLastProject", "")
                        ),
                        new XComment("=== Selection State ==="),
                        new XComment("PlatformIndex: 0 = PC, 1 = PS4"),
                        new XComment("GameIndex: 0 = T7 (BO3), 1 = T8 (BO4)"),
                        new XComment("InjectGameIndex: 0 = T7 (BO3), 1 = T8 (BO4)"),
                        new XElement("Selections",
                            new XElement("PlatformIndex", currentPlatformIndex.ToString()),
                            new XElement("GameIndex", currentGameIndex.ToString()),
                            new XElement("InjectGameIndex", currentInjectGameIndex.ToString())
                        ),
                        new XComment("=== Window State ==="),
                        new XComment("Stored only if 'RestoreWindowState' is enabled. Contains window position, size, and selected tab."),
                        new XComment("=== Recent Projects ==="),
                        new XComment("List of recently used project folders (max 10)")
                    )
                );
                
                // Conditionally add WindowState element if restoreWindowState is enabled
                if (restoreWindowState && config.Root != null)
                {
                    config.Root.Add(new XElement("WindowState",
                            new XElement("State", this.WindowState.ToString()),
                            new XElement("Location",
                                new XElement("X", this.WindowState == FormWindowState.Normal ? this.Location.X : this.RestoreBounds.X),
                                new XElement("Y", this.WindowState == FormWindowState.Normal ? this.Location.Y : this.RestoreBounds.Y)
                            ),
                        new XElement("Size",
                            new XElement("Width", this.WindowState == FormWindowState.Normal ? this.Size.Width : this.RestoreBounds.Width),
                            new XElement("Height", this.WindowState == FormWindowState.Normal ? this.Size.Height : this.RestoreBounds.Height)
                        ),
                        new XElement("SelectedTab", tabControl != null ? tabControl.SelectedIndex.ToString() : "0")
                    ));
                }
                
                // Conditionally add RecentProjects element if needed
                if (recentProjects != null && recentProjects.Count > 0 && config.Root != null)
                {
                    config.Root.Add(new XElement("RecentProjects",
                        recentProjects.Select(p => new XElement("Project", p))
                    ));
                }
                
                // Conditionally add RecentFiles element if needed
                if (recentFiles != null && recentFiles.Count > 0 && config.Root != null)
                {
                    config.Root.Add(new XElement("RecentFiles",
                        recentFiles.Select(f => new XElement("File", f))
                    ));
                }
                
                // Conditionally add Keybinds element if needed
                if (keybinds != null && keybinds.Count > 0 && config.Root != null)
                {
                    config.Root.Add(new XElement("Keybinds",
                            keybinds.Values.Select(kb => new XElement("Keybind",
                                new XElement("Action", kb.Action),
                                new XElement("Key", kb.Key.ToString()),
                                new XElement("Ctrl", kb.Ctrl),
                                new XElement("Shift", kb.Shift),
                                new XElement("Alt", kb.Alt)
                            ))
                    ));
                }
                
                // Conditionally add Injection element if needed
                if (llpModifiedSPTStruct != 0 && OriginalPID != 0 && config.Root != null)
                {
                    config.Root.Add(new XElement("Injection",
                                new XElement("ReplacePath", txtInjectPath != null ? txtInjectPath.Text : ""),
                                new XElement("Game", LastGameInjected.ToString()),
                                new XElement("OriginalPID", OriginalPID.ToString()),
                                new XElement("ModifiedSPTStruct", ((ulong)llpModifiedSPTStruct).ToString()),
                                new XElement("OriginalBuffer", ((ulong)llpOriginalBuffer).ToString()),
                                new XElement("InjectedBuffSize", InjectedBuffSize.ToString()),
                                new XElement("ScriptName", ((ulong)InjectedScript.llpName).ToString()),
                                new XElement("ScriptBuffSize", InjectedScript.BuffSize.ToString()),
                                new XElement("ScriptBuffer", ((ulong)InjectedScript.lpBuffer).ToString())
                    ));
                }
                
            return config;
        }
        
        private bool SaveConfigToPath(XDocument config, string configPath)
        {
            try
            {
                // Ensure the directory exists
                string configDir = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(configDir) && !Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }
                
                // Save with proper formatting (indented, human-readable)
                XmlWriterSettings settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
                    NewLineChars = "\r\n",
                    NewLineHandling = NewLineHandling.Replace,
                    OmitXmlDeclaration = false,
                    Encoding = Encoding.UTF8
                };
                
                using (XmlWriter writer = XmlWriter.Create(configPath, settings))
                {
                    config.Save(writer);
                }
                
                // Verify the file was actually created
                if (File.Exists(configPath))
                {
                    return true;
                }
                else
                {
                    if (txtLog != null)
                    {
                        AppendLog($"Warning: Config file was not created at: {configPath}\r\n", LogLevel.Warning);
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Error saving config to {configPath}: {ex.Message}\r\n", LogLevel.Warning);
                return false;
            }
        }
        
        private void SaveAllSettings()
        {
            try
            {
                // Don't save during initial load
                if (isLoadingSettings) return;
                
                if (!autoSaveSettings) return;
                
                // Build the config document
                XDocument config = BuildConfigDocument();
                
                // Try to save to the primary location
                string configPath = GetConfigPath();
                if (SaveConfigToPath(config, configPath))
                {
                    return; // Success!
                }
                
                // If primary location failed, try AppData fallback
                AppendLog($"Failed to save to startup path. Trying AppData location...\r\n", LogLevel.Warning);
                
                string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "T7CompilerGUI");
                string fallbackConfigPath = Path.Combine(appDataPath, CONFIG_FILE_NAME);
                
                if (SaveConfigToPath(config, fallbackConfigPath))
                {
                    AppendLog($"Config saved to AppData location: {fallbackConfigPath}\r\n", LogLevel.Info);
                }
                else
                {
                    AppendLog($"Error: Failed to save config to both primary and fallback locations.\r\n", LogLevel.Error);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                AppendLog($"Error saving config: {ex.Message}\r\nStack: {ex.StackTrace}\r\n", LogLevel.Warning);
            }
        }
        
        private void LoadAllSettings()
        {
            isLoadingSettings = true;
            try
            {
                // Ensure StyleManager is initialized before loading settings
                if (poisonStyleManager == null)
                {
                    AppendLog("Warning: StyleManager not initialized. Settings may not load correctly.\r\n", LogLevel.Warning);
                }
                
                string configPath = GetConfigPath();
                
                // If config doesn't exist at primary location, check AppData fallback
                if (!File.Exists(configPath))
                {
                    string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "T7CompilerGUI");
                    string fallbackConfigPath = Path.Combine(appDataPath, CONFIG_FILE_NAME);
                    
                    if (File.Exists(fallbackConfigPath))
                    {
                        configPath = fallbackConfigPath;
                        AppendLog($"Loading config from AppData location: {configPath}\r\n", LogLevel.Info);
                    }
                    else
                {
                    // Try to migrate from old files
                    MigrateOldConfigFiles();
                    if (!File.Exists(configPath))
                    {
                            // Check fallback again after migration
                            if (File.Exists(fallbackConfigPath))
                            {
                                configPath = fallbackConfigPath;
                            }
                            else
                            {
                                // No config file exists - apply defaults and create one
                                AppendLog("No config file found. Using default settings and creating new config file.\r\n", LogLevel.Info);
                                
                                // Apply default settings
                                ApplyDefaultSettings();
                                
                                // Create initial config file with defaults
                                SaveAllSettings();
                                
                        isLoadingSettings = false;
                        return;
                    }
                }
                    }
                }
                
                // Load the config file
                XDocument config;
                try
                {
                    config = XDocument.Load(configPath);
                }
                catch (Exception ex)
                {
                    if (txtLog != null)
                    {
                        AppendLog($"Error loading config file: {ex.Message}. Using default settings.\r\n", LogLevel.Warning);
                    }
                    ApplyDefaultSettings();
                    isLoadingSettings = false;
                    return;
                }
                
                XElement root = config.Element("T7CompilerConfig");
                if (root == null)
                {
                    AppendLog("Config file has invalid structure. Using default settings.\r\n", LogLevel.Warning);
                    ApplyDefaultSettings();
                    isLoadingSettings = false;
                    return;
                }
                
                // Load Application Settings
                XElement appSettings = root.Element("Application");
                if (appSettings != null)
                {
                    // Theme
                    XElement themeElem = appSettings.Element("Theme");
                    if (themeElem != null)
                    {
                        if (Enum.TryParse<ThemeStyle>(themeElem.Value, out ThemeStyle theme))
                        {
                            if (poisonStyleManager != null)
                    {
                        // Set StyleManager theme first (source of truth)
                        poisonStyleManager.Theme = theme;
                        
                        // Use helper to update form and all controls with the new theme
                        ReaLTaiizorExt.PoisonControlHelper.UpdateThemeAndStyleWithRefresh(
                            this, 
                            theme, 
                            poisonStyleManager.Style);
                        
                        // Update components (not controls) directly
                        if (poisonStyleExtender != null)
                        {
                            poisonStyleExtender.Theme = theme;
                            poisonStyleExtender.StyleManager = poisonStyleManager;
                        }
                        poisonStyleManager.Update();
                        if (toggleSettingsTheme != null)
                        {
                            toggleSettingsTheme.Checked = (theme == ThemeStyle.Light);
                            toggleSettingsTheme.Text = toggleSettingsTheme.Checked ? "Light" : "Dark";
                                }
                                
                                AppendLog($"Loaded theme: {theme}\r\n", LogLevel.Info);
                            }
                            else
                            {
                                AppendLog($"Warning: Theme '{theme}' found in config but StyleManager is not initialized.\r\n", LogLevel.Warning);
                            }
                        }
                        else
                        {
                            AppendLog($"Warning: Invalid theme value '{themeElem.Value}' in config. Using default.\r\n", LogLevel.Warning);
                        }
                    }
                    
                    // Color Style
                    XElement styleElem = appSettings.Element("ColorStyle");
                    if (styleElem != null)
                    {
                        if (poisonStyleManager != null)
                    {
                        if (styleElem.Value == "Rainbow")
                        {
                            ChangeColorStyleToRainbow();
                            AppendLog("Loaded color style: Rainbow\r\n", LogLevel.Info);
                        }
                        else if (Enum.TryParse<ColorStyle>(styleElem.Value, out ColorStyle style))
                        {
                            // Set StyleManager style first (source of truth)
                            poisonStyleManager.Style = style;
                            
                            // Use helper to update form and all controls with the new style
                            ReaLTaiizorExt.PoisonControlHelper.UpdateThemeAndStyleWithRefresh(
                                this, 
                                poisonStyleManager.Theme, 
                                style);
                            
                            // Update components (not controls) directly
                            if (poisonStyleExtender != null)
                            {
                                poisonStyleExtender.StyleManager = poisonStyleManager;
                            }
                            poisonStyleManager.Update();
                            UpdateSettingsColorStyleButton();
                            AppendLog($"Loaded color style: {style}\r\n", LogLevel.Info);
                        }
                        else
                        {
                            AppendLog($"Warning: Invalid color style value '{styleElem.Value}' in config. Using default.\r\n", LogLevel.Warning);
                        }
                    }
                    else
                    {
                        AppendLog($"Warning: Color style '{styleElem.Value}' found in config but StyleManager is not initialized.\r\n", LogLevel.Warning);
                    }
                    }
                    else
                    {
                    }
                    
                    // Default Output Path
                    XElement defaultPathElem = appSettings.Element("DefaultOutputPath");
                    if (defaultPathElem != null && !string.IsNullOrWhiteSpace(defaultPathElem.Value))
                    {
                        defaultOutputPath = defaultPathElem.Value;
                        if (txtSettingsDefaultOutputPath != null)
                        {
                            txtSettingsDefaultOutputPath.Text = defaultPathElem.Value;
                        }
                    }
                    else if (txtSettingsDefaultOutputPath != null && string.IsNullOrWhiteSpace(txtSettingsDefaultOutputPath.Text))
                    {
                        // If no default output path is saved, populate with the default build folder path
                        string defaultOutput = Path.Combine(buildFolder, "compiled.gscc");
                        txtSettingsDefaultOutputPath.Text = defaultOutput;
                        defaultOutputPath = defaultOutput;
                    }
                    
                    // Auto Save Settings
                    XElement autoSaveElem = appSettings.Element("AutoSaveSettings");
                    if (autoSaveElem != null)
                    {
                        autoSaveSettings = (autoSaveElem.Value == "1");
                    }
                    else
                    {
                        // If not found in config, default to true (auto-save is always enabled)
                        autoSaveSettings = true;
                    }
                    
                    // Restore Window State
                    XElement restoreStateElem = appSettings.Element("RestoreWindowState");
                    if (restoreStateElem != null)
                    {
                        restoreWindowState = (restoreStateElem.Value == "1");
                        // Restore window state is always enabled
                    }
                    
                    // Last Project Folder
                    XElement lastProjectElem = appSettings.Element("LastProjectFolder");
                    if (lastProjectElem != null && txtProjectFolder != null && Directory.Exists(lastProjectElem.Value))
                    {
                        txtProjectFolder.Text = lastProjectElem.Value;
                        // Don't auto-populate output file - keep it blank until user explicitly sets a path
                    }
                }
                
                // Load Selection State
                XElement selections = root.Element("Selections");
                if (selections != null)
                {
                    XElement platformIdx = selections.Element("PlatformIndex");
                    if (platformIdx != null && int.TryParse(platformIdx.Value, out int pIdx) && pIdx >= 0 && pIdx <= 1)
                    {
                        currentPlatformIndex = pIdx;
                    }
                    
                    XElement gameIdx = selections.Element("GameIndex");
                    if (gameIdx != null && int.TryParse(gameIdx.Value, out int gIdx) && gIdx >= 0 && gIdx <= 1)
                    {
                        currentGameIndex = gIdx;
                    }
                    
                    XElement injectGameIdx = selections.Element("InjectGameIndex");
                    if (injectGameIdx != null && int.TryParse(injectGameIdx.Value, out int igIdx) && igIdx >= 0 && igIdx <= 1)
                    {
                        currentInjectGameIndex = igIdx;
                    }
                    
                    // Update dropdown buttons after loading
                    SetupPlatformMenu();
                    SetupGameMenu();
                    SetupInjectGameMenu();
                }
                
                // Load Window State
                XElement windowState = root.Element("WindowState");
                if (windowState != null && restoreWindowState)
                {
                    XElement stateElem = windowState.Element("State");
                    if (stateElem != null && Enum.TryParse<FormWindowState>(stateElem.Value, out FormWindowState state))
                    {
                        XElement location = windowState.Element("Location");
                        XElement size = windowState.Element("Size");
                        if (location != null && size != null)
                        {
                            if (int.TryParse(location.Element("X")?.Value, out int x) &&
                                int.TryParse(location.Element("Y")?.Value, out int y) &&
                                int.TryParse(size.Element("Width")?.Value, out int width) &&
                                int.TryParse(size.Element("Height")?.Value, out int height))
                            {
                                if (width > 0 && height > 0 && width <= Screen.PrimaryScreen.WorkingArea.Width * 2 && height <= Screen.PrimaryScreen.WorkingArea.Height * 2)
                                {
                                    this.Location = new Point(x, y);
                                    this.Size = new Size(width, height);
                                }
                                this.WindowState = state;
                                
                                // Restore selected tab AFTER window state is set
                                // This ensures the Designer's default (SelectedIndex = 0) is overridden
                                XElement tabIdx = windowState.Element("SelectedTab");
                                if (tabIdx != null && int.TryParse(tabIdx.Value, out int tabIndex) && tabControl != null && tabIndex >= 0 && tabIndex < tabControl.TabCount)
                                {
                                    tabControl.SelectedIndex = tabIndex;
                                    if (txtLog != null)
                                    {
                                        AppendLog($"Restored selected tab: {tabIndex}\r\n", LogLevel.Info);
                                    }
                                }
                                }
                            }
                        }
                    }
                else if (restoreWindowState && tabControl != null)
                {
                    // Even if window state wasn't saved, ensure we don't override with Designer default
                    // The Designer sets SelectedIndex = 0, but we want to respect any programmatic setting
                    // This is a fallback to ensure tab selection isn't lost
                }
                
                // Load Recent Projects - store paths as-is, don't validate or normalize
                XElement recentProjectsElem = root.Element("RecentProjects");
                if (recentProjectsElem != null)
                {
                    recentProjects = recentProjectsElem.Elements("Project")
                        .Select(e => e.Value)
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .Select(p => p.Trim()) // Just trim, don't expand or normalize
                        .Take(MAX_RECENT_PROJECTS)
                        .ToList();
                }
                
                // Load Recent Files
                XElement recentFilesElem = root.Element("RecentFiles");
                if (recentFilesElem != null)
                {
                    recentFiles = recentFilesElem.Elements("File")
                        .Select(e => e.Value)
                        .Where(f => File.Exists(f))
                        .Take(MAX_RECENT_FILES)
                        .ToList();
                }
                
                // Update menus after loading
                UpdateRecentProjectsMenu();
                UpdateRecentFilesMenu();
                
                // Load Keyboard Shortcuts
                LoadKeybinds();
                
                // Load Injection State
                XElement injection = root.Element("Injection");
                if (injection != null)
                {
                    XElement replacePathElem = injection.Element("ReplacePath");
                    XElement gameElem = injection.Element("Game");
                    XElement pidElem = injection.Element("OriginalPID");
                    XElement sptElem = injection.Element("ModifiedSPTStruct");
                    XElement origBufElem = injection.Element("OriginalBuffer");
                    XElement buffSizeElem = injection.Element("InjectedBuffSize");
                    XElement nameElem = injection.Element("ScriptName");
                    XElement scriptBuffSizeElem = injection.Element("ScriptBuffSize");
                    XElement scriptBufElem = injection.Element("ScriptBuffer");
                    
                    if (replacePathElem != null && gameElem != null && pidElem != null && sptElem != null &&
                        int.TryParse(pidElem.Value, out int pid) && ulong.TryParse(sptElem.Value, out ulong sptStruct))
                    {
                        OriginalPID = pid;
                        llpModifiedSPTStruct = sptStruct;
                        
                        if (ulong.TryParse(origBufElem?.Value, out ulong origBuf))
                            llpOriginalBuffer = origBuf;
                        if (int.TryParse(buffSizeElem?.Value, out int buffSize))
                            InjectedBuffSize = buffSize;
                        if (ulong.TryParse(nameElem?.Value, out ulong name))
                            InjectedScript.llpName = name;
                        if (int.TryParse(scriptBuffSizeElem?.Value, out int scriptBuffSize))
                            InjectedScript.BuffSize = scriptBuffSize;
                        if (ulong.TryParse(scriptBufElem?.Value, out ulong scriptBuf))
                            InjectedScript.lpBuffer = scriptBuf;
                        
                        if (Enum.TryParse<TreyarchCompiler.Enums.Games>(gameElem.Value, out TreyarchCompiler.Enums.Games game))
                            LastGameInjected = game;
                        if (replacePathElem != null && txtInjectPath != null)
                            txtInjectPath.Text = replacePathElem.Value;
                        
                        isParseTreeReset = false;
                        UpdateResetParseTreeButton();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                if (txtLog != null)
                {
                    AppendLog($"Error loading config: {ex.Message}\r\n", LogLevel.Warning);
                    AppendLog($"Stack trace: {ex.StackTrace}\r\n", LogLevel.Warning);
                }
                
                // Apply default settings if loading failed
                ApplyDefaultSettings();
            }
            finally
            {
                isLoadingSettings = false;
            }
        }
        
        private void ApplyDefaultSettings()
        {
            try
            {
                // Apply default theme and style if StyleManager is available
                if (poisonStyleManager != null)
                {
                    poisonStyleManager.Theme = ThemeStyle.Dark;
                    this.Theme = ThemeStyle.Dark;
                    poisonStyleManager.Style = ColorStyle.Red;
                    this.Style = ColorStyle.Red;
                    
                    if (poisonStyleExtender != null && poisonStyleManager != null)
                    {
                        // PoisonStyleExtender is a component, not a control - set properties directly
                        poisonStyleExtender.Theme = ThemeStyle.Dark;
                        poisonStyleExtender.StyleManager = poisonStyleManager;
                    }
                    
                    poisonStyleManager.Update();
                    
                    if (toggleSettingsTheme != null)
                    {
                        toggleSettingsTheme.Checked = false;
                        toggleSettingsTheme.Text = "Dark";
                    }
                    
                    UpdateSettingsColorStyleButton();
                }
                
                // Set default auto-save
                autoSaveSettings = true;
                restoreWindowState = true;
                
                // Set default output path
                if (txtSettingsDefaultOutputPath != null && string.IsNullOrWhiteSpace(txtSettingsDefaultOutputPath.Text))
                {
                    string defaultOutput = Path.Combine(buildFolder, "compiled.gscc");
                    txtSettingsDefaultOutputPath.Text = defaultOutput;
                    defaultOutputPath = defaultOutput;
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Error applying default settings: {ex.Message}\r\n", LogLevel.Warning);
            }
        }
        
        private void MigrateOldConfigFiles()
        {
            try
            {
                // Migrate from old separate .txt files to unified config
                bool hasData = false;
                XDocument config = new XDocument(new XElement("T7CompilerConfig"));
                XElement root = config.Element("T7CompilerConfig");
                
                // Migrate t7compiler_settings.txt
                string oldSettingsPath = Path.Combine(Application.StartupPath, "t7compiler_settings.txt");
                if (File.Exists(oldSettingsPath))
                {
                    hasData = true;
                    using (var reader = new StreamReader(oldSettingsPath))
                    {
                        root.Add(new XElement("Application",
                            new XElement("Theme", reader.ReadLine() ?? "Dark"),
                            new XElement("ColorStyle", reader.ReadLine() ?? "Red"),
                            new XElement("DefaultOutputPath", reader.ReadLine() ?? ""),
                            new XElement("AutoSaveSettings", reader.ReadLine() ?? "0"),
                            new XElement("RestoreWindowState", reader.ReadLine() ?? "0")
                        ));
                    }
                }
                
                // Migrate t7compiler_selections.txt
                string oldSelectionsPath = Path.Combine(Application.StartupPath, "t7compiler_selections.txt");
                if (File.Exists(oldSelectionsPath))
                {
                    hasData = true;
                    using (var reader = new StreamReader(oldSelectionsPath))
                    {
                        root.Add(new XElement("Selections",
                            new XElement("PlatformIndex", reader.ReadLine() ?? "0"),
                            new XElement("GameIndex", reader.ReadLine() ?? "0"),
                            new XElement("GscConfModeIndex", reader.ReadLine() ?? "0"),
                            new XElement("InjectGameIndex", reader.ReadLine() ?? "0")
                        ));
                    }
                }
                
                // Migrate window_state.txt
                string oldWindowStatePath = Path.Combine(Application.StartupPath, "window_state.txt");
                if (File.Exists(oldWindowStatePath))
                {
                    hasData = true;
                    using (var reader = new StreamReader(oldWindowStatePath))
                    {
                        string state = reader.ReadLine() ?? "Normal";
                        int x = int.TryParse(reader.ReadLine(), out int xVal) ? xVal : 100;
                        int y = int.TryParse(reader.ReadLine(), out int yVal) ? yVal : 100;
                        int width = int.TryParse(reader.ReadLine(), out int wVal) ? wVal : 800;
                        int height = int.TryParse(reader.ReadLine(), out int hVal) ? hVal : 600;
                        string tabIdx = reader.ReadLine() ?? "0";
                        
                        root.Add(new XElement("WindowState",
                            new XElement("State", state),
                            new XElement("Location", new XElement("X", x), new XElement("Y", y)),
                            new XElement("Size", new XElement("Width", width), new XElement("Height", height)),
                            new XElement("SelectedTab", tabIdx)
                        ));
                    }
                }
                
                // Migrate recent_projects.txt
                string oldRecentPath = Path.Combine(Application.StartupPath, "recent_projects.txt");
                if (File.Exists(oldRecentPath))
                {
                    hasData = true;
                    var projects = File.ReadAllLines(oldRecentPath)
                        .Where(p => Directory.Exists(p))
                        .Take(MAX_RECENT_PROJECTS);
                    root.Add(new XElement("RecentProjects",
                        projects.Select(p => new XElement("Project", p))
                    ));
                }
                
                // Migrate injection state (from config folder or old location)
                string oldInjectionPath = Path.Combine(buildFolder, "config", "t7compiler_injection_state.txt");
                if (!File.Exists(oldInjectionPath))
                {
                    oldInjectionPath = Path.Combine(Application.StartupPath, "t7compiler_injection_state.txt");
                }
                if (File.Exists(oldInjectionPath))
                {
                    hasData = true;
                    using (var reader = new StreamReader(oldInjectionPath))
                    {
                        root.Add(new XElement("Injection",
                            new XElement("ReplacePath", reader.ReadLine() ?? ""),
                            new XElement("Game", reader.ReadLine() ?? "T7"),
                            new XElement("OriginalPID", reader.ReadLine() ?? "0"),
                            new XElement("ModifiedSPTStruct", reader.ReadLine() ?? "0"),
                            new XElement("OriginalBuffer", reader.ReadLine() ?? "0"),
                            new XElement("InjectedBuffSize", reader.ReadLine() ?? "0"),
                            new XElement("ScriptName", reader.ReadLine() ?? "0"),
                            new XElement("ScriptBuffSize", reader.ReadLine() ?? "0"),
                            new XElement("ScriptBuffer", reader.ReadLine() ?? "0")
                        ));
                    }
                }
                
                if (hasData)
                {
                    config.Save(GetConfigPath());
                }
            }
            catch { }
        }
        
        #endregion
        
        #region Window State Persistence
        
        private void ChkRestoreWindowState_CheckedChanged(object sender, EventArgs e)
        {
            // Save settings to persist the checkbox state
            if (!isLoadingSettings)
            {
                SaveAllSettings();
            }
        }
        
        private void ResetAllSettingsToDefaults()
        {
            // Reset theme and style
            if (poisonStyleManager != null)
            {
                poisonStyleManager.Theme = ThemeStyle.Dark;
                poisonStyleManager.Style = ColorStyle.Red;
                this.Theme = ThemeStyle.Dark;
                this.Style = ColorStyle.Red;
                ChangeTheme(ThemeStyle.Dark);
                ChangeColorStyle(ColorStyle.Red);
            }
            
            // Reset Settings tab controls
            if (toggleSettingsTheme != null)
            {
                toggleSettingsTheme.Checked = false; // Dark theme (unchecked)
                toggleSettingsTheme.Text = "Dark";
            }
            if (btnSettingsColorStyle != null && poisonStyleManager != null)
            {
                poisonStyleManager.Style = ColorStyle.Red;
                btnSettingsColorStyle.Text = "Red";
            }
            if (txtSettingsDefaultOutputPath != null)
                txtSettingsDefaultOutputPath.Text = Path.Combine(buildFolder, "compiled.gscc");
            // Auto save and restore window state are always enabled
            
            UpdateSettingsColorStyleButton();
            
            // Reset selection state
            currentPlatformIndex = 0;
            currentGameIndex = 0;
            currentInjectGameIndex = 0;
            SetupPlatformMenu();
            SetupGameMenu();
            SetupInjectGameMenu();
            
            // Reset other settings
            defaultOutputPath = Path.Combine(buildFolder, "compiled.gscc");
            autoSaveSettings = true;
            restoreWindowState = true;
            
            // Reset keybinds to defaults
            keybinds = KeybindDialog.GetDefaultKeybinds();
            
            // Save all settings
            SaveAllSettings();
            
            AppendLog("[INFO] All settings have been reset to defaults.\r\n", LogLevel.Info);
        }
        
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Stop the game status timer to prevent accessing disposed controls
            if (gameStatusTimer != null && gameStatusTimer.Enabled)
            {
                gameStatusTimer.Stop();
                gameStatusTimer.Dispose();
            }
            
            // Stop and dispose rainbow color timer
            if (rainbowColorTimer != null)
            {
                if (rainbowColorTimer.Enabled)
                rainbowColorTimer.Stop();
                rainbowColorTimer.Dispose();
                rainbowColorTimer = null;
            }
            
            // Remove rainbow event handlers
            RemoveRainbowEventHandlersRecursive(this);
            
            // Save all settings to unified config file
            SaveAllSettings();
            
            // Use PoisonFormHelper for standardized cleanup (disposes tracked resources)
            ReaLTaiizorExt.PoisonFormHelper.CleanupForm(this);
            
            base.OnFormClosing(e);
        }
        
        private void txtLog_MouseDown(object sender, MouseEventArgs e)
        {
            // Prevent dragging text out of the window
            // When user clicks, clear any existing selection to prevent drag operation
            if (e.Button == MouseButtons.Left)
            {
                // If there's a selection, clear it to prevent drag
                if (txtLog.SelectionLength > 0)
                {
                    int caretPos = txtLog.SelectionStart;
                    txtLog.SelectionLength = 0;
                    txtLog.SelectionStart = caretPos;
                }
            }
        }
        
        private void txtLog_QueryContinueDrag(object sender, QueryContinueDragEventArgs e)
        {
            // Cancel any drag operations to prevent dragging text out of the window
            e.Action = DragAction.Cancel;
        }
        
        /// <summary>
        /// Shows a non-interrupting task window asking if user wants to launch BO3
        /// Positioned above the log area
        /// </summary>
        private void ShowLaunchGameTaskWindow()
        {
            try
            {
                // Use the static Show method from LaunchGameTaskControl
                // All configuration is handled within the control
                T7CompilerGUI.Controls.LaunchGameTaskControl.Show(
                    this,
                    () =>
                    {
                        // Launch action
                        LaunchBO3();
                        // Switch to inject tab after launching
                        if (tabControl != null && tabInject != null)
                        {
                            tabControl.SelectedTab = tabInject;
                        }
                    },
                    () =>
                    {
                        // Dismiss action (just close, no action needed)
                    },
                    CalculateTaskWindowPosition // Pass position calculator
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing launch game task window: {ex.Message}");
            }
        }

        /// <summary>
        /// Calculates the position for the task window inside the log area
        /// </summary>
        private Point CalculateTaskWindowPosition()
        {
            try
            {
                // Match the size used in LaunchGameTaskControl.Show() for consistency
                int taskWindowWidth = 400;
                int taskWindowHeight = 180; // Size to fit all content including buttons
                
                // Find log panel or textbox to position relative to it
                Control logControl = null;
                if (txtLog != null && txtLog.Visible)
                {
                    logControl = txtLog;
                }
                else if (panelLog != null && panelLog.Visible)
                {
                    logControl = panelLog;
                }
                
                if (logControl != null && logControl.Visible)
                {
                    // Position inside the log area, centered or right-aligned
                    Point logControlScreenPos = logControl.PointToScreen(Point.Empty);
                    
                    // Position inside log area, right-aligned with some margin from edges
                    int x = logControlScreenPos.X + logControl.Width - taskWindowWidth - 10;
                    int y = logControlScreenPos.Y + 10; // Top of log area with small margin
                    
                    // Ensure it's within the log control bounds
                    if (x < logControlScreenPos.X + 10)
                        x = logControlScreenPos.X + 10;
                    if (y + taskWindowHeight > logControlScreenPos.Y + logControl.Height - 10)
                        y = logControlScreenPos.Y + logControl.Height - taskWindowHeight - 10;
                    
                    // Ensure it's within screen bounds
                    Screen currentScreen = Screen.FromControl(this);
                    if (x < currentScreen.WorkingArea.Left)
                        x = currentScreen.WorkingArea.Left + 10;
                    if (y < currentScreen.WorkingArea.Top)
                        y = currentScreen.WorkingArea.Top + 10;
                    
                    return new Point(x, y);
                }
                else
                {
                    // Fallback: position in bottom-right of form
                    Point formScreenPos = this.PointToScreen(Point.Empty);
                    int x = formScreenPos.X + this.Width - taskWindowWidth - 20;
                    int y = formScreenPos.Y + this.Height - taskWindowHeight - 50;
                    
                    return new Point(x, y);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating task window position: {ex.Message}");
                // Fallback to bottom-right
                Point formScreenPos = this.PointToScreen(Point.Empty);
                return new Point(formScreenPos.X + this.Width - 370, formScreenPos.Y + this.Height - 170);
            }
        }

        /// <summary>
        /// Launches Black Ops 3 via Steam
        /// </summary>
        private void LaunchBO3()
        {
            try
            {
                // BO3 Steam App ID is 311210
                Process.Start("steam://rungameid/311210");
                
                // Show status message in log
                if (txtLog != null)
                {
                    AppendLogText("Launching Black Ops 3 via Steam...\r\n");
                }
            }
            catch (Exception ex)
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(
                    this, 
                    $"Failed to launch Black Ops 3:\n{ex.Message}\n\nMake sure Steam is installed.", 
                    "Launch Error", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// Kills all Black Ops 3 processes
        /// </summary>
        private bool KillBO3()
        {
            bool killed = false;
            
            // Try to kill all BO3 processes
            foreach (string processName in T7ProcessNames)
            {
                try
                {
                    Process[] processes = Process.GetProcessesByName(processName);
                    if (processes != null && processes.Length > 0)
                    {
                        foreach (Process proc in processes)
                        {
                            try
                            {
                                proc.Kill();
                                proc.WaitForExit(5000); // Wait up to 5 seconds for process to exit
                                killed = true;
                            }
                            catch (Exception ex)
                            {
                                // Log but continue trying other processes
                                if (txtLog != null)
                                {
                                    AppendLogText($"Warning: Could not kill process {proc.ProcessName} (PID: {proc.Id}): {ex.Message}\r\n");
                                }
                            }
                            finally
                            {
                                try { proc.Dispose(); } catch { }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log but continue trying other process names
                    if (txtLog != null)
                    {
                        AppendLogText($"Warning: Error checking for process {processName}: {ex.Message}\r\n");
                    }
                }
            }
            
            return killed;
        }
        
        #endregion
        
        #region Compiler Actions (moved from CompilerActions.cs)
        
        /// <summary>
        /// Gets the compiler installation path. Checks T7COMPILER_PATH environment variable first, 
        /// then checks for compiler in executable directory, then defaults to Documents\T7Compiler
        /// </summary>
        public static string GetCompilerPath()
        {
            string envPath = Environment.GetEnvironmentVariable("T7COMPILER_PATH");
            if (!string.IsNullOrEmpty(envPath) && Directory.Exists(envPath))
            {
                return envPath;
            }
            
            // Check if compiler exists in the same directory as the executable
            string executingDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrEmpty(executingDir))
            {
                string localCompilerPath = Path.Combine(executingDir, "t7compiler");
                if (Directory.Exists(localCompilerPath))
                {
                    return localCompilerPath;
                }
            }
            
            // Fallback: Use Documents folder instead of hardcoded C:\ drive
            // This is more portable and doesn't require admin privileges
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (!string.IsNullOrEmpty(documentsPath))
            {
                return Path.Combine(documentsPath, "T7Compiler");
            }
            
            // Last resort fallback (should rarely be needed)
            return Path.Combine(Path.GetTempPath(), "T7Compiler");
        }

        /// <summary>
        /// Gets the GUI installation path. Checks T7GUI_PATH environment variable first, 
        /// then uses the executable directory
        /// </summary>
        public static string GetGuiPath()
        {
            string envPath = Environment.GetEnvironmentVariable("T7GUI_PATH");
            if (!string.IsNullOrEmpty(envPath) && Directory.Exists(envPath))
            {
                return envPath;
            }
            
            // Default to executable directory (same as CodeEditorForm.GetGuiPath())
            string executingDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrEmpty(executingDir))
            {
                return executingDir;
            }
            
            // Fallback: Use ApplicationData if executable directory is unavailable
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!string.IsNullOrEmpty(appDataPath))
            {
                return Path.Combine(appDataPath, "T7CompilerGUI");
            }
            
            // Last resort fallback (should rarely be needed)
            return Path.Combine(Path.GetTempPath(), "T7CompilerGUI");
        }

        /// <summary>
        /// Checks if the compiler is installed
        /// </summary>
        public static bool IsCompilerInstalled()
        {
            return Directory.Exists(GetCompilerPath());
        }

        /// <summary>
        /// Kills all processes with the specified name
        /// </summary>
        private static void KillProcessesByName(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            foreach (var process in processes)
            {
                try
                {
                    process.Kill();
                }
                catch { }
            }
        }

        /// <summary>
        /// Installs or updates the T7 compiler from the specified URL
        /// </summary>
        public static void InstallCompiler(string url = @"https://gsc.dev/t7c_package")
        {
            try
            {
                // Kill any running compiler or game processes
                KillProcessesByName("debugcompiler");
                KillProcessesByName("blackops3");
                KillProcessesByName("blackops4");

                var usertemp = Path.GetTempPath();
                var installertemp = Path.Combine(usertemp, "installer_temp");
                var extractpath = Path.Combine(usertemp, "update_t7.zip");
                var compileFolder = GetCompilerPath();

                // Clean up old temp files
                if (Directory.Exists(extractpath))
                    Directory.Delete(extractpath, true);

                if (Directory.Exists(installertemp))
                    Directory.Delete(installertemp, true);

                // Download the compiler package
                using (System.Net.WebClient client = new System.Net.WebClient())
                {
                    client.DownloadFile(url, extractpath);
                }

                // Extract the package
                System.IO.Compression.ZipFile.ExtractToDirectory(extractpath, installertemp);

                // Only install if compiler doesn't exist (matching T7-Compiler-UI-main behavior)
                if (Directory.Exists(compileFolder))
                {
                    // Cleanup and return early if already installed
                    Directory.Delete(installertemp, true);
                    File.Delete(extractpath);
                    return;
                }

                Helpers.FileHelper.CopyDirectory(Path.Combine(installertemp, "t7compiler"), compileFolder, true);
                
                // Copy default project if it exists
                string defaultProjectSource = Path.Combine(installertemp, "defaultproject");
                if (Directory.Exists(defaultProjectSource))
                {
                    Helpers.FileHelper.CopyDirectory(defaultProjectSource, Path.Combine(compileFolder, "defaultproject"), true);
                }

                // Copy Default Menu folder to compiler installation if it exists
                string defaultMenuSource = Path.Combine(installertemp, "Default Menu");
                if (Directory.Exists(defaultMenuSource))
                {
                    Helpers.FileHelper.CopyDirectory(defaultMenuSource, Path.Combine(compileFolder, "Default Menu"), true);
                }

                // Also copy to GUI installation if it exists
                string guiFolder = GetGuiPath();
                if (Directory.Exists(guiFolder))
                {
                    // Copy Default Menu to GUI folder
                    if (Directory.Exists(defaultMenuSource))
                    {
                        Helpers.FileHelper.CopyDirectory(defaultMenuSource, Path.Combine(guiFolder, "Default Menu"), true);
                    }
                }

                // Cleanup
                Directory.Delete(installertemp, true);
                File.Delete(extractpath);

                MessageBox.Show("Compiler Updated/Installed", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error installing compiler: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Gets the game string from Games enum
        /// </summary>
        public static string GetGameString(TreyarchCompiler.Enums.Games game)
        {
            switch (game)
            {
                case TreyarchCompiler.Enums.Games.T7:
                    return "bo3";
                case TreyarchCompiler.Enums.Games.T8:
                    return "bo4";
                default:
                    return "bo3";
            }
        }

        /// <summary>
        /// Gets the game mode string from Modes enum
        /// </summary>
        public static string GetGameModeString(TreyarchCompiler.Enums.Modes mode)
        {
            switch (mode)
            {
                case TreyarchCompiler.Enums.Modes.SP:
                    return "sp";
                case TreyarchCompiler.Enums.Modes.MP:
                    return "mp";
                case TreyarchCompiler.Enums.Modes.ZM:
                    return "zm";
                default:
                    return "zm";
            }
        }
        
        #endregion
    }
}



