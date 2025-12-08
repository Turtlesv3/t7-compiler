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

#pragma warning disable IDE1006 // Naming rule violation - Windows Forms event handlers use controlName_EventName convention

namespace T7CompilerGUI
{
    public partial class MainForm : PoisonForm
    {
        #region Constants and Windows API Declarations
        
        // Windows API for modern folder picker
        private const uint FOS_PICKFOLDERS = 0x00000020;
        private const uint SIGDN_FILESYSPATH = 0x80058000;
        
        // Windows API for modern folder picker
        [ComImport]
        [ClassInterface(ClassInterfaceType.None)]
        [TypeLibType(TypeLibTypeFlags.FCanCreate)]
        [Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
        private class FileOpenDialogRCW { }

        [ComImport]
        [Guid("42F85136-DB7E-439C-85F1-E4075D135FC8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog
        {
            [PreserveSig]
            uint Show([In] IntPtr hwndParent);
            [PreserveSig]
            uint SetOptions(uint fos); // Returns HRESULT, but we'll check it
            [PreserveSig]
            uint GetOptions(out uint pfos);
            void SetFolder(IShellItem psi);
            [PreserveSig]
            void SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            void GetResult(out IShellItem ppsi);
        }

        [ComImport]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(IntPtr pbc, [In] ref Guid bhid, [In] ref Guid riid, out IntPtr ppv);
            void GetParent(out IShellItem ppsi);
            [PreserveSig]
            void GetDisplayName([In] uint sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
            void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
            void Compare(IShellItem psi, uint hint, out int piOrder);
        }

        [ComImport]
        [Guid("B63EA76D-1F85-456F-A19C-48159EFA858B")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItemArray
        {
            void BindToHandler();
            void GetPropertyStore();
            void GetPropertyDescriptionList();
            void GetAttributes();
            void GetCount(out uint pdwNumItems);
            void GetItemAt(uint dwIndex, out IShellItem ppsi);
            void EnumItems();
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern uint SHParseDisplayName([MarshalAs(UnmanagedType.LPWStr)] string pszName, IntPtr pbc, out IntPtr ppidl, uint sfgaoIn, out uint psfgaoOut);

        [DllImport("shell32.dll", PreserveSig = false)]
        private static extern uint SHCreateItemFromIDList(IntPtr pidl, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);
        
        #endregion
        
        #region Private Fields
        
        // UI Theme and Style Management
        // poisonStyleManager and poisonStyleExtender are now declared in MainForm.Designer.cs (matching Form20 pattern)
        
        // Game Status Detection
        private bool lastT7Status = false;
        private bool lastT8Status = false;
        private bool isUpdatingGameStatus = false; // Prevent concurrent updates
        private System.Windows.Forms.Timer gameStatusTimer;
        
        // Game Process Names (optimized static arrays)
        private static readonly string[] T7ProcessNames = { "blackops3", "BlackOps3" };
        private static readonly string[] T8ProcessNames = { "blackops4", "BlackOps4" };
        
        // Injection State Tracking
        private int InjectedBuffSize;
        private PointerEx llpModifiedSPTStruct = 0;
        private PointerEx llpOriginalBuffer;
        private int OriginalSourceChecksum;
        private int OriginalPID = 0;
        private T7SPT InjectedScript;
        private Games LastGameInjected = Games.T7;
        private bool isParseTreeReset = false;
        
        // Compilation and Injection State
        private bool isCompiling = false;
        private bool isInjecting = false;
        
        // UI Selection State
        private int currentPlatformIndex = 0; // 0 = PC, 1 = PS4
        private int currentGameIndex = 0; // 0 = T7, 1 = T8
        private int currentGscConfModeIndex = 0; // 0 = MP, 1 = ZM
        private int currentInjectGameIndex = 0; // 0 = T7, 1 = T8
        private bool isLoadingGscConfMode = false;
        private bool isLoadingSettings = false;
        
        // Build and Path Management
        private string buildFolder = Path.Combine(Application.StartupPath, "build");
        
        // Recent Projects
        private const int MAX_RECENT_PROJECTS = 10;
        
        // Keyboard Shortcuts
        private Dictionary<string, KeybindDialog.KeybindInfo> keybinds = null;
        
        // Settings values (backing fields for SettingsPanel)
        private bool autoSaveSettings = true;
        private string defaultOutputPath = "";
        private bool restoreWindowState = true;
        
        #endregion
        
        #region Windows API Interfaces (Modern Folder Dialog)
        
        // Interfaces are defined in the "Constants and Windows API Declarations" region above
        
        #endregion
        
        #region Helper Methods (Folder Dialog and Path Utilities)
        
        private string ShowModernFolderDialog(string title, string initialPath = null)
        {
            try
            {
                var dialog = (IFileOpenDialog)new FileOpenDialogRCW();
                
                // Set folder picker mode FIRST - this is critical for folder selection
                // FOS_PICKFOLDERS = 0x00000020 - forces folder selection mode (no file name field)
                uint hr = dialog.SetOptions(FOS_PICKFOLDERS);
                
                // Check HRESULT - 0 (S_OK) means success
                if (hr != 0)
                {
                    // If SetOptions fails, fall back to FolderBrowserDialog
                    throw new InvalidOperationException($"Failed to set folder picker options (HRESULT: 0x{hr:X8})");
                }
                
                // Verify the options were set correctly
                if (dialog.GetOptions(out uint verifyOptions) == 0)
                {
                    if ((verifyOptions & FOS_PICKFOLDERS) == 0)
                    {
                        // Folder picker flag not set, fall back
                        throw new InvalidOperationException("Folder picker flag was not set correctly");
                    }
                }
                
                dialog.SetTitle(title);

                // Normalize initialPath - ensure it's a directory path, not a file path
                if (!string.IsNullOrWhiteSpace(initialPath))
                {
                    // If it's a file, get its directory
                    if (File.Exists(initialPath))
                    {
                        initialPath = Path.GetDirectoryName(initialPath);
                    }
                    
                    // Only proceed if we have a valid directory path
                if (!string.IsNullOrWhiteSpace(initialPath) && Directory.Exists(initialPath))
                {
                    try
                    {
                        IntPtr pidl = IntPtr.Zero;
                            uint parseResult = SHParseDisplayName(initialPath, IntPtr.Zero, out pidl, 0, out _);
                            if (parseResult == 0 && pidl != IntPtr.Zero)
                        {
                            try
                            {
                                if (SHCreateItemFromIDList(pidl, typeof(IShellItem).GUID, out IShellItem folder) == 0)
                                {
                                    dialog.SetFolder(folder);
                                }
                            }
                            finally
                            {
                                if (pidl != IntPtr.Zero)
                                    Marshal.FreeCoTaskMem(pidl);
                            }
                        }
                    }
                    catch { }
                    }
                }

                uint result = dialog.Show(this.Handle);
                if (result == 0) // S_OK
                {
                    dialog.GetResult(out IShellItem item);
                    item.GetDisplayName(SIGDN_FILESYSPATH, out string path);
                    return path;
                }
            }
            catch (Exception ex)
            {
                // Modern folder dialog failed - fallback to traditional FolderBrowserDialog
                // FolderBrowserDialog does NOT have "File name:" field - it's a proper folder picker
                System.Diagnostics.Debug.WriteLine($"Modern folder dialog failed: {ex.Message}. Falling back to FolderBrowserDialog.");
                
                using (var dialog = new FolderBrowserDialog())
                {
                    dialog.Description = title;
                    dialog.ShowNewFolderButton = true; // Allow creating new folders
                    
                    // Normalize initialPath for fallback dialog too
                    if (!string.IsNullOrWhiteSpace(initialPath))
                    {
                        // If it's a file, get its directory
                        if (File.Exists(initialPath))
                        {
                            initialPath = Path.GetDirectoryName(initialPath);
                        }
                        
                        // Only set if it's a valid directory
                    if (!string.IsNullOrWhiteSpace(initialPath) && Directory.Exists(initialPath))
                        {
                        dialog.SelectedPath = initialPath;
                        }
                    }
                    
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                        return dialog.SelectedPath;
                }
            }

            return null;
        }

        /// <summary>
        /// Helper function to shorten file paths to show just 1-2 directories before filename
        /// </summary>
        private string GetShortPath(string fullPath)
        {
            try
            {
                string fileName = Path.GetFileName(fullPath);
                string directory = Path.GetDirectoryName(fullPath);
                if (string.IsNullOrEmpty(directory))
                    return fileName;

                string[] parts = directory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                int partCount = parts.Length;
                
                if (partCount <= 2)
                {
                    // If 2 or fewer directories, show all
                    return Path.Combine(directory, fileName);
                }
                else
                {
                    // Show last 2 directories before filename
                    string dir1 = parts[partCount - 2];
                    string dir2 = parts[partCount - 1];
                    return Path.Combine("...", dir1, dir2, fileName);
                }
            }
            catch
            {
                // Fallback to just filename if anything goes wrong
                return Path.GetFileName(fullPath);
            }
        }
        
        #endregion
        
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
            
            // PoisonForm already sets ControlStyles in its constructor (OptimizedDoubleBuffer, ResizeRedraw, etc.)
            // We don't need to override them - PoisonForm handles smooth resizing internally
            
            // Set form border style and shadow type to match Form20 (Poison demo)
            this.PoisonBorderStyle = ReaLTaiizor.Enum.Poison.FormBorderStyle.FixedSingle;
            this.ShadowType = ReaLTaiizor.Enum.Poison.FormShadowType.AeroShadow;
            
            // Initialize build folder - ensure it exists
            InitializeBuildFolder();
            
            // StyleManager is configured in Designer - no initialization needed (matching demo pattern)
            
            // Set default output file path (after buildFolder is initialized)
            string defaultOutput = Path.Combine(buildFolder, "compiled.gscc");
            txtOutputFile.Text = defaultOutput;
            
            // Setup dropdown buttons (platform, game, inject game)
            SetupDropdownButtons();
            
            // Setup dropdown menu (gsc conf mode - keep as dropdown)
            SetupGscConfModeMenu();
            
            // Wire up event handlers for dropdown menu
            WireUpMenuEvents();
            
            // StyleManager handles all styling automatically (configured in Designer, matching demo pattern)
            
            // Load all settings from unified config file FIRST (replaces all individual load methods)
            // This ensures keybinds are loaded before SetupKeyboardShortcuts
            LoadAllSettings();
            
            // Setup new features (after settings are loaded)
            SetupKeyboardShortcuts();
            
            // Setup tooltips AFTER StyleManager is initialized and settings are loaded
            // This ensures tooltips use the correct theme/style from the start
            SetupToolTips();
            
            // Verify injection state after loading (if injection state was loaded)
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
                            txtLog.AppendText($"Loaded previous injection state (PID: {OriginalPID}, Game: {LastGameInjected}). Reset is available.\r\n");
                        }
                    }
                    else
                    {
                        // Process exited - clear state
                        txtLog.AppendText($"Previous injection state found, but game process (PID: {OriginalPID}) is no longer running. State cleared.\r\n");
                        ClearInjectionState();
                    }
                }
                catch (ArgumentException)
                {
                    // Process not found - clear state
                    txtLog.AppendText($"Previous injection state found, but game process (PID: {OriginalPID}) no longer exists. State cleared.\r\n");
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
            
            // StyleManager handles all updates automatically (matching demo pattern)
            UpdateAboutTab();
            
            // Wire up Load event to ensure form background is set
            this.Load += MainForm_Load;
            
            // Setup main form context menu (right-click on title bar/form)
            SetupMainFormContextMenu();
            
            // Initialize reset parse tree button state
            UpdateResetParseTreeButton();
            
            // Update UI state
            UpdateUI();
            
            // Setup game status label to be non-interactive
            SetupGameStatusLabel();
            
            // Handle resize for panelLog height adjustment
            this.SizeChanged += MainForm_SizeChanged;
            this.Shown += MainForm_Shown;
            
            // Enable simple tab content dragging (only TabPage, not all controls)
            EnableTabContentDragging();
            
            // Enable dragging from tab headers (tab names)
            EnableTabHeaderDragging();
            
            // Enable dragging from text panel controls
            EnablePanelDragging();
            
            // Enable dragging from game status label area
            EnableGameStatusDragging();
        }
        
        private void MainForm_Load(object sender, EventArgs e)
        {
            // Ensure form and tab panels use theme background color
            if (poisonStyleManager != null)
            {
                this.BackColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Form(poisonStyleManager.Theme);
                
                // Apply theme colors to txtLog (RichTextBox doesn't automatically inherit from StyleManager)
                ChangeTheme(poisonStyleManager.Theme);
            }
        }
        
        private void MainForm_Shown(object sender, EventArgs e)
        {
            // Adjust panelLog height after form is shown to ensure correct initial layout
            AdjustPanelLogHeight();
            
            // Delay the start of game status monitoring to avoid Win32Exception on startup
            System.Windows.Forms.Timer delayTimer = new System.Windows.Forms.Timer
            {
                Interval = 3000 // 3 seconds delay
            };
            delayTimer.Tick += (s, args) =>
            {
                delayTimer.Stop();
                delayTimer.Dispose();
                InitializeGameStatusMonitoring();
            };
            delayTimer.Start();
        }
        
        #endregion
        
        #region Theme and Style Management
        
        /// <summary>
        /// Adjusts panelLog height to ensure it doesn't overlap with panelLogControls
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
            // Adjust panelLog height after size change
            AdjustPanelLogHeight();
        }
        
        /// <summary>
        /// Enables simple form dragging from tab content (TabPage only, not all controls)
        /// This is a minimal approach - only TabPage controls can trigger dragging
        /// </summary>
        private void EnableTabContentDragging()
        {
            // Add MouseDown handler to TabPage controls only
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
                // Check if clicking on tab header area (not on tab content)
                if (sender is System.Windows.Forms.TabControl)
                {
                    // Only drag if clicking in the tab header area (top portion of TabControl)
                    // Tab headers are typically in the first ~30 pixels of the TabControl
                    if (e.Y < 30)
                    {
                        // Use Windows API to simulate title bar drag
                        const int WM_NCLBUTTONDOWN = 0xA1;
                        const int HT_CAPTION = 0x2;
                        ReleaseCapture();
                        SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                    }
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
        /// Handles mouse down on Label controls - makes label areas draggable
        /// </summary>
        private void Label_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Use Windows API to simulate title bar drag
                const int WM_NCLBUTTONDOWN = 0xA1;
                const int HT_CAPTION = 0x2;
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
                const int WM_NCLBUTTONDOWN = 0xA1;
                const int HT_CAPTION = 0x2;
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
                    const int WM_NCLBUTTONDOWN = 0xA1;
                    const int HT_CAPTION = 0x2;
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
                        const int WM_NCLBUTTONDOWN = 0xA1;
                        const int HT_CAPTION = 0x2;
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                    }
                }
            }
        }
        
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        
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
                using (var dialog = new KeybindDialog(poisonStyleManager, keybinds ?? KeybindDialog.GetDefaultKeybinds(), this))
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
                    
                    using (var dialog = new ConfigLocationsDialog(poisonStyleManager, configPath, tempPath, dllExtractPath, appDataRoaming, this))
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
            
            // Ensure menu updates with rainbow colors when opened
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
        /// Shows the Advanced Settings dialog with PropertyGrid (similar to Form20's Legacy tab)
        /// </summary>
        private void ShowAdvancedSettingsDialog()
        {
            // Create a simple dialog with PropertyGrid, matching Form20's Legacy tab pattern
            using (var dialog = new PoisonForm())
            {
                dialog.Text = "Advanced Settings";
                dialog.Size = new Size(650, 550);
                dialog.MinimumSize = new Size(550, 450);
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.ShadowType = FormShadowType.DropShadow;
                dialog.Theme = poisonStyleManager?.Theme ?? ThemeStyle.Dark;
                dialog.Style = poisonStyleManager?.Style ?? ColorStyle.Red;
                dialog.PoisonBorderStyle = ReaLTaiizor.Enum.Poison.FormBorderStyle.FixedSingle;
                dialog.StyleManager = poisonStyleManager;
                
                // Main panel with padding to prevent controls from being cut off
                var mainPanel = new PoisonPanel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(20, 20, 30, 60), // Extra right and bottom padding
                    Style = ColorStyle.Default,
                    Theme = ThemeStyle.Default
                };
                
                // Info label
                var lblInfo = new PoisonLabel
                {
                    Text = "Edit application properties directly. Changes may require restart.",
                    Location = new Point(20, 20),
                    Size = new Size(580, 20),
                    UseStyleColors = true
                };
                
                // PropertyGrid (like Form20's Legacy tab)
                var propertyGrid = new System.Windows.Forms.PropertyGrid
                {
                    Location = new Point(20, 50),
                    Size = new Size(580, 420),
                    SelectedObject = this, // Allow editing MainForm properties
                    ToolbarVisible = true,
                    HelpVisible = true
                };
                
                // Apply Poison theme to PropertyGrid (same as Form20)
                poisonStyleExtender?.SetApplyPoisonTheme(propertyGrid, true);
                
                // Button panel at bottom
                var buttonPanel = new PoisonPanel
                {
                    Dock = DockStyle.Bottom,
                    Height = 50,
                    Padding = new Padding(10, 10, 20, 10), // Extra right padding
                    Style = ColorStyle.Default,
                    Theme = ThemeStyle.Default
                };
                
                // Close button
                var btnClose = new PoisonButton
                {
                    Text = "Close",
                    Size = new Size(100, 35),
                    Location = new Point(10, 5), // Will be positioned correctly after panel is added
                    DialogResult = DialogResult.OK,
                    Style = ColorStyle.Default,
                    Theme = ThemeStyle.Default,
                    UseStyleColors = true
                };
                btnClose.Click += (s, e) => ResetButtonState(s);
                
                // Position button correctly after panel is added (accounting for padding)
                buttonPanel.Layout += (s, e) =>
                {
                    int rightPadding = buttonPanel.Padding.Right;
                    int buttonWidth = 100;
                    btnClose.Location = new Point(buttonPanel.Width - rightPadding - buttonWidth, 5);
                };
                
                // Setup rainbow effects for button
                btnClose.CustomPaintForeground += (sender, e) =>
                {
                    if (poisonStyleManager != null && IsRainbowStyleActive())
                    {
                        Color rainbowColor = currentRainbowColor;
                        
                        // Draw rainbow border
                        using (Pen rainbowPen = new Pen(rainbowColor, 1))
                        {
                            Rectangle borderRect = btnClose.ClientRectangle;
                            borderRect.Width -= 1;
                            borderRect.Height -= 1;
                            e.Graphics.DrawRectangle(rainbowPen, borderRect);
                        }
                        
                        // Draw rainbow text
                        if (!string.IsNullOrEmpty(btnClose.Text))
                        {
                            var fontSizeProp = btnClose.GetType().GetProperty("FontSize");
                            var fontWeightProp = btnClose.GetType().GetProperty("FontWeight");
                            Font buttonFont = ReaLTaiizor.Extension.Poison.PoisonFonts.Button(
                                fontSizeProp != null ? (ReaLTaiizor.Extension.Poison.PoisonButtonSize)fontSizeProp.GetValue(btnClose) : ReaLTaiizor.Extension.Poison.PoisonButtonSize.Medium,
                                fontWeightProp != null ? (ReaLTaiizor.Extension.Poison.PoisonButtonWeight)fontWeightProp.GetValue(btnClose) : ReaLTaiizor.Extension.Poison.PoisonButtonWeight.Regular
                            );
                            
                            // Get background color to erase original text
                            Color backColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Button.Normal(btnClose.Theme);
                            if (!btnClose.Enabled)
                            {
                                backColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Button.Disabled(btnClose.Theme);
                            }
                            
                            // Erase original text by filling text area
                            Size textSize = TextRenderer.MeasureText(e.Graphics, btnClose.Text, buttonFont, btnClose.ClientRectangle.Size, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                            Rectangle eraseRect = new Rectangle(
                                (btnClose.Width - textSize.Width) / 2 - 2,
                                (btnClose.Height - textSize.Height) / 2 - 1,
                                textSize.Width + 4,
                                textSize.Height + 2
                            );
                            using (SolidBrush backBrush = new SolidBrush(backColor))
                            {
                                e.Graphics.FillRectangle(backBrush, eraseRect);
                            }
                            
                            // Draw rainbow text
                            TextRenderer.DrawText(e.Graphics, btnClose.Text, buttonFont, 
                                btnClose.ClientRectangle, rainbowColor, 
                                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        }
                    }
                };
                
                buttonPanel.Controls.Add(btnClose);
                mainPanel.Controls.Add(lblInfo);
                mainPanel.Controls.Add(propertyGrid);
                dialog.Controls.Add(mainPanel);
                dialog.Controls.Add(buttonPanel);
                
                // Sync dialog with StyleManager periodically
                var                 syncTimer = new System.Windows.Forms.Timer
                {
                    Interval = 100 // Check every 100ms
                };
                syncTimer.Tick += (s, e) =>
                {
                    if (poisonStyleManager != null)
                    {
                        dialog.StyleManager = poisonStyleManager;
                        dialog.Theme = poisonStyleManager.Theme;
                        dialog.Style = poisonStyleManager.Style;
                        
                        // Sync all controls
                        SyncAdvancedDialogControls(dialog);
                    }
                };
                syncTimer.Start();
                
                // Rainbow update timer for button
                var rainbowTimer = new System.Windows.Forms.Timer
                {
                    Interval = 16 // ~60 FPS to match rainbow animation
                };
                rainbowTimer.Tick += (s, e) =>
                {
                    if (IsRainbowStyleActive())
                    {
                        btnClose.Invalidate();
                    }
                };
                rainbowTimer.Start();
                
                dialog.FormClosed += (s, e) =>
                {
                    syncTimer.Stop();
                    syncTimer.Dispose();
                    rainbowTimer.Stop();
                    rainbowTimer.Dispose();
                };
                
                dialog.ShowDialog(this);
            }
        }
        
        private void SyncAdvancedDialogControls(Control parent)
        {
            if (parent == null || poisonStyleManager == null) return;
            
            foreach (Control ctrl in parent.Controls)
            {
                // Sync IPoisonControl controls
                if (ctrl is IPoisonControl poisonCtrl)
                {
                    if (poisonCtrl.StyleManager != poisonStyleManager)
                    {
                        poisonCtrl.StyleManager = poisonStyleManager;
                    }
                    
                    var styleProp = ctrl.GetType().GetProperty("Style");
                    var themeProp = ctrl.GetType().GetProperty("Theme");
                    
                    if (styleProp != null && styleProp.CanWrite)
                    {
                        styleProp.SetValue(ctrl, ColorStyle.Default);
                    }
                    if (themeProp != null && themeProp.CanWrite)
                    {
                        themeProp.SetValue(ctrl, ThemeStyle.Default);
                    }
                    
                    var useStyleColorsProp = ctrl.GetType().GetProperty("UseStyleColors");
                    if (useStyleColorsProp != null && useStyleColorsProp.CanWrite)
                    {
                        useStyleColorsProp.SetValue(ctrl, true);
                    }
                    
                    ctrl.Invalidate();
                }
                
                // Sync IPoisonComponent controls
                if (ctrl is IPoisonComponent poisonComponent)
                {
                    if (poisonComponent.StyleManager != poisonStyleManager)
                    {
                        poisonComponent.StyleManager = poisonStyleManager;
                    }
                    ctrl.Invalidate();
                }
                
                // Recursively sync child controls
                if (ctrl.HasChildren)
                {
                    SyncAdvancedDialogControls(ctrl);
                }
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
                // Set custom Consolas font for game status label
                statusLabel.UseCustomFont = true;
                statusLabel.Font = new System.Drawing.Font("Consolas", 9F);
                
                // PoisonLabel is naturally non-interactive and non-selectable
                // Don't use style colors - we want green/red for running/not running
                statusLabel.UseStyleColors = false;
                
                // Don't connect to StyleManager - we want fixed green/red colors
                // Colors will be set directly in UpdateGameStatus based on running status
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
                const int WM_NCLBUTTONDOWN = 0xA1;
                const int HT_CAPTION = 0x2;
                        ReleaseCapture();
                        SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
        
        // WndProc override removed - scrollbar hiding is now handled by NoScrollRichTextBox class
        
        /// <summary>
        /// Initializes the game status monitoring timer and label - optimized for smooth operation
        /// </summary>
        private void InitializeGameStatusMonitoring()
        {
            // Create timer to check game status every 2.5 seconds (slightly longer to reduce overhead)
            gameStatusTimer = new System.Windows.Forms.Timer
            {
                Interval = 2500 // 2.5 seconds - smooth operation with less overhead
            };
            gameStatusTimer.Tick += GameStatusTimer_Tick;
            
            // Start the timer (delay is handled in MainForm_Shown)
            try
            {
                // Now start the game status timer
                if (gameStatusTimer != null && !gameStatusTimer.Enabled && !this.IsDisposed)
                {
                    gameStatusTimer.Start();
                    // First status update (on UI thread)
                    this.BeginInvoke(new Action(() => UpdateGameStatus()));
                }
            }
            catch
            {
                // Silently ignore any exceptions
            }
        }
        
        /// <summary>
        /// Timer tick event to periodically check game status - optimized to prevent overlapping calls
        /// </summary>
        private void GameStatusTimer_Tick(object sender, EventArgs e)
        {
            // Prevent overlapping updates for smooth operation
            if (isUpdatingGameStatus)
                return;
            
            // Skip update if form is minimized or disposed (save resources)
            if (this.IsDisposed || this.WindowState == FormWindowState.Minimized)
                return;
            
            // Use BeginInvoke to avoid blocking the timer thread
            this.BeginInvoke(new Action(() =>
            {
                try
                {
                    // Double-check after invoke (form might have been disposed)
                    if (this.IsDisposed || this.Disposing)
                        return;
                    
                    isUpdatingGameStatus = true;
                    UpdateGameStatus();
                }
                finally
                {
                    isUpdatingGameStatus = false;
                }
            }));
        }
        
        /// <summary>
        /// Updates the game status indicator - optimized for smooth operation
        /// </summary>
        private void UpdateGameStatus()
        {
            try
            {
                // Early exit checks - avoid unnecessary work
                if (lblGameStatus == null || lblGameStatus.IsDisposed)
                    return;
                
                if (this.IsDisposed || this.Disposing)
                    return;
                
                // Ensure we're on the UI thread - use BeginInvoke to avoid blocking
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(UpdateGameStatus));
                    return;
                }
                
                // Check game status (optimized methods)
                bool t7Running = IsGameRunning(Games.T7);
                bool t8Running = IsGameRunning(Games.T8);
                
                // Check if status changed
                bool statusChanged = (t7Running != lastT7Status || t8Running != lastT8Status);
                bool isFirstUpdate = (lastT7Status == false && lastT8Status == false);
                
                    // Only update UI if status changed or it's the first update
                if (isFirstUpdate || statusChanged)
                {
                    // Update status tracking
                    lastT7Status = t7Running;
                    lastT8Status = t8Running;
                    
                    // Update reset parse tree button when game status changes
                    UpdateResetParseTreeButton();
                    
                    // Build status text and set colors based on running status
                    if (lblGameStatus is ReaLTaiizor.Controls.PoisonLabel statusLabel)
                    {
                        // Build status text with status indicators
                        string bo3Status = t7Running ? "Running" : "Not Running";
                        string bo4Status = t8Running ? "Running" : "Not Running";
                        statusLabel.Text = $"BO3: {bo3Status} | BO4: {bo4Status}";
                        
                        // Set color based on running status - green if any game is running, red if none
                        if (t7Running || t8Running)
                        {
                            statusLabel.ForeColor = Color.FromArgb(0, 200, 0); // Green
                        }
                        else
                        {
                            statusLabel.ForeColor = Color.FromArgb(200, 0, 0); // Red
                        }
                    }
                    
                    // Only update UI when status changes (not every tick)
                    // This reduces unnecessary UI refreshes for smoother operation
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
            // Setup labels to use style colors - PoisonStyleManager handles this automatically
            // This makes labels use the current Style color for their text
            if (lblProjectFolder != null) lblProjectFolder.UseStyleColors = true;
            if (lblOutputFile != null) lblOutputFile.UseStyleColors = true;
            if (lblOpcodeMasking != null) lblOpcodeMasking.UseStyleColors = true;
            if (lblSaveOpcodeMap != null) lblSaveOpcodeMap.UseStyleColors = true;
            if (lblPlatform != null) lblPlatform.UseStyleColors = true;
            if (lblGame != null) lblGame.UseStyleColors = true;
            if (lblInjectFile != null) lblInjectFile.UseStyleColors = true;
            if (lblNoRuntime != null) lblNoRuntime.UseStyleColors = true;
            if (lblInjectPath != null) lblInjectPath.UseStyleColors = true;
            if (lblInjectGame != null) lblInjectGame.UseStyleColors = true;
            if (lblLogSearch != null) lblLogSearch.UseStyleColors = true;
            // Settings panel labels are handled by SettingsPanel
            if (lblVersion != null) lblVersion.UseStyleColors = true;
            if (lblAbout != null) lblAbout.UseStyleColors = true;
            // lblGameStatus uses fixed green/red colors (not style colors)
            // Colors are set in UpdateGameStatus based on running status
            
            // All controls are automatically connected to StyleManager via Designer (matching demo pattern)
            // No manual connection or updates needed - StyleManager handles everything
        }
        
        // UpdateUITheme() removed - functionality moved into ChangeTheme() to avoid duplication
        
        private void UpdateTabPanelsBackground()
        {
            if (poisonStyleManager == null) return;
            
            Color formBackColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Form(poisonStyleManager.Theme);
            
            // Update all tab pages
            if (tabControl != null)
            {
                foreach (System.Windows.Forms.TabPage tabPage in tabControl.TabPages)
                {
                    tabPage.BackColor = formBackColor;
                }
            }
        }
        
        // UpdateAllControlsStyle() removed - functionality moved into ChangeTheme() and ChangeColorStyle()
        
        private void UpdateControlStyle(Control parent, ColorStyle style, ThemeStyle theme, bool forceUpdate = false)
        {
            foreach (Control ctrl in parent.Controls)
            {
                // Update Poison controls
                if (ctrl is IPoisonControl poisonCtrl)
                {
                    // Always ensure StyleManager is connected first
                    if (poisonStyleManager != null)
                    {
                        poisonCtrl.StyleManager = poisonStyleManager;
                    }
                    
                    // Ensure all controls use Style = Default and Theme = Default to follow StyleManager
                    var styleProp = ctrl.GetType().GetProperty("Style");
                    var themeProp = ctrl.GetType().GetProperty("Theme");
                    
                    // Only set to Default if not explicitly set to a specific style (when not forcing update)
                    if (!forceUpdate)
                    {
                        if (styleProp != null && styleProp.CanWrite)
                        {
                            var currentStyle = styleProp.GetValue(ctrl);
                            if (currentStyle == null || currentStyle.ToString() == "Default" || 
                                (currentStyle is ColorStyle styleValue && styleValue == ColorStyle.Default))
                            {
                                styleProp.SetValue(ctrl, ColorStyle.Default);
                            }
                        }
                        
                        if (themeProp != null && themeProp.CanWrite)
                        {
                            var currentTheme = themeProp.GetValue(ctrl);
                            if (currentTheme == null || currentTheme.ToString() == "Default" || 
                                (currentTheme is ThemeStyle themeValue && themeValue == ThemeStyle.Default))
                            {
                                themeProp.SetValue(ctrl, ThemeStyle.Default);
                            }
                        }
                    }
                    
                    // For buttons, ensure UseStyleColors is enabled so they respect Style
                    if (ctrl is ReaLTaiizor.Controls.PoisonButton poisonBtn)
                    {
                        poisonBtn.UseStyleColors = true;
                    }
                    
                    // For dropdown buttons, ensure UseStyleColors is enabled so they respect Style
                    if (ctrl is ReaLTaiizor.Controls.PoisonDropDownButton poisonDropDownBtn)
                    {
                        poisonDropDownBtn.UseStyleColors = true;
                    }
                    
                    // For labels, ensure UseStyleColors is enabled
                    if (ctrl is ReaLTaiizor.Controls.PoisonLabel)
                    {
                        var useStyleColorsProp = ctrl.GetType().GetProperty("UseStyleColors");
                        if (useStyleColorsProp?.CanWrite == true)
                        {
                            useStyleColorsProp.SetValue(ctrl, true);
                        }
                    }
                    
                    // Force update style and theme if forceUpdate is true, otherwise check for Default
                    if (forceUpdate)
                    {
                        // Force update regardless of current value
                        try
                        {
                            var stylePropInner = ctrl.GetType().GetProperty("Style");
                            if (stylePropInner != null && stylePropInner.CanWrite)
                            {
                                stylePropInner.SetValue(ctrl, style);
                            }
                        }
                        catch { }
                        
                        try
                        {
                            var themePropInner = ctrl.GetType().GetProperty("Theme");
                            if (themePropInner != null && themePropInner.CanWrite)
                            {
                                themePropInner.SetValue(ctrl, theme);
                            }
                        }
                        catch { }
                    }
                    else
                    {
                        // Only update if control doesn't have a specific style set (inherits from StyleManager)
                        var stylePropInner = ctrl.GetType().GetProperty("Style");
                        var currentStyle = stylePropInner?.GetValue(ctrl);
                        
                        // If style is Default or matches current, update it
                        if (currentStyle == null || currentStyle.ToString() == "Default" || 
                            (currentStyle is ColorStyle currentStyleEnum && currentStyleEnum == style))
                        {
                            poisonCtrl.Style = style;
                        }
                        
                        var themePropInner = ctrl.GetType().GetProperty("Theme");
                        var currentTheme = themePropInner?.GetValue(ctrl);
                        
                        // If theme is Default or matches current, update it
                        if (currentTheme == null || currentTheme.ToString() == "Default" || 
                            (currentTheme is ThemeStyle currentThemeEnum && currentThemeEnum == theme))
                        {
                            poisonCtrl.Theme = theme;
                        }
                    }
                    
                    // Force control to redraw with new style/theme
                    ctrl.Invalidate();
                }
                
                // Update IPoisonComponent controls (like menus)
                if (ctrl is IPoisonComponent poisonComponent)
                {
                    if (poisonStyleManager != null)
                    {
                        poisonComponent.StyleManager = poisonStyleManager;
                    }
                }
                
                // Handle TabControl and TabPages specially
                if (ctrl is System.Windows.Forms.TabControl tabCtrl)
                {
                    foreach (System.Windows.Forms.TabPage tabPage in tabCtrl.TabPages)
                    {
                        UpdateControlStyle(tabPage, style, theme, forceUpdate);
                    }
                }
                
                // Recursively update child controls
                if (ctrl.HasChildren)
                {
                    UpdateControlStyle(ctrl, style, theme, forceUpdate);
                }
            }
        }
        
        private void SetupToolTips()
        {
            if (poisonToolTip == null) return;
            
            // Configure tooltip theme to match current StyleManager settings
            if (poisonStyleManager != null)
            {
                poisonToolTip.Style = poisonStyleManager.Style;
                poisonToolTip.Theme = poisonStyleManager.Theme;
                poisonToolTip.StyleManager = poisonStyleManager;
                
                // Subscribe to Draw event for custom styling with Style color
                poisonToolTip.Draw -= poisonToolTip_Draw; // Remove if already subscribed
                poisonToolTip.Draw += poisonToolTip_Draw; // Add our custom handler
            }
            else
            {
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
            poisonToolTip.SetToolTip(btnGscConfMode, "Select compilation mode (MP for Multiplayer or ZM for Zombies)");
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
            // Only create build folder if user hasn't set a custom default output path
            // This will be checked when actually needed (in AutoPopulateOutputFile)
            // For now, just ensure the path is set, but don't create the directory yet
            try
            {
                // Try to set buildFolder to a valid location, but don't create it yet
                if (!Directory.Exists(Path.GetDirectoryName(buildFolder)))
                {
                    // If parent directory doesn't exist, try alternatives
                    buildFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "T7Compiler", "build");
                }
            }
            catch
            {
                // Fallback to user's Documents folder
                buildFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "T7Compiler", "build");
            }
        }
        
        /// <summary>
        /// Ensures the build folder exists. Only called when actually needed (when no custom output path is set).
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
                // If we can't create in current location, fallback to user's Documents folder
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
        
        private void WireUpMenuEvents()
        {
            // Only wire up gsc conf mode menu (platform/game/inject game now use radio buttons)
            if (gscConfModeMenu != null)
                gscConfModeMenu.ItemClicked += gscConfModeMenu_ItemClicked;
        }
        
        private void SetupDropdownButtons()
        {
            // Setup Platform dropdown button
            SetupPlatformMenu();
            
            // Setup Game dropdown button
            SetupGameMenu();
            
            // Setup Inject Game dropdown button
            SetupInjectGameMenu();
            
            // Initialize Settings Tab Controls
            SetupSettingsTab();
        }
        
        private void SetupPlatformMenu()
        {
            if (platformMenu == null || btnPlatform == null) return;
            
            platformMenu.Items.Clear();
            var pcItem = new ToolStripMenuItem("PC")
            {
                Tag = 0,
                Checked = false // Don't show checkmark - selection is indicated by background color
            };
            var ps4Item = new ToolStripMenuItem("PS4")
            {
                Tag = 1,
                Checked = false // Don't show checkmark - selection is indicated by background color
            };
            
            pcItem.Click += (s, e) => platformMenu_ItemClicked(s, new ToolStripItemClickedEventArgs(pcItem));
            ps4Item.Click += (s, e) => platformMenu_ItemClicked(s, new ToolStripItemClickedEventArgs(ps4Item));
            
            platformMenu.Items.Add(pcItem);
            platformMenu.Items.Add(ps4Item);
            
            // Update button text
            btnPlatform.Text = (currentPlatformIndex == 0) ? "PC" : "PS4";
            
            // Connect to style manager
            if (poisonStyleManager != null)
            {
                btnPlatform.StyleManager = poisonStyleManager;
                EnhanceMenuItems(platformMenu);
            }
        }
        
        private void SetupGameMenu()
        {
            if (gameMenu == null || btnGame == null) return;
            
            gameMenu.Items.Clear();
            var t7Item = new ToolStripMenuItem("T7 (BO3)")
            {
                Tag = 0,
                Checked = false // Don't show checkmark - selection is indicated by background color
            };
            var t8Item = new ToolStripMenuItem("T8 (BO4)")
            {
                Tag = 1,
                Checked = false // Don't show checkmark - selection is indicated by background color
            };
            
            t7Item.Click += (s, e) => gameMenu_ItemClicked(s, new ToolStripItemClickedEventArgs(t7Item));
            t8Item.Click += (s, e) => gameMenu_ItemClicked(s, new ToolStripItemClickedEventArgs(t8Item));
            
            gameMenu.Items.Add(t7Item);
            gameMenu.Items.Add(t8Item);
            
            // Update button text
            btnGame.Text = (currentGameIndex == 0) ? "T7 (BO3)" : "T8 (BO4)";
            
            // Connect to style manager
            if (poisonStyleManager != null)
            {
                btnGame.StyleManager = poisonStyleManager;
                EnhanceMenuItems(gameMenu);
            }
        }
        
        private void SetupSettingsTab()
        {
            // Setup color style menu
            SetupSettingsColorStyleMenu();
            
            // Setup context menu for settings
            SetupSettingsContextMenu();
            
            // Set default theme toggle (Dark = unchecked)
            if (toggleSettingsTheme != null)
            {
                toggleSettingsTheme.Checked = false; // Dark theme (unchecked)
                toggleSettingsTheme.Text = "Dark";
            }
            
            // Connect controls to style manager
            if (poisonStyleManager != null)
            {
                if (btnSettingsColorStyle != null)
                {
                    btnSettingsColorStyle.StyleManager = poisonStyleManager;
                    btnSettingsColorStyle.Text = poisonStyleManager.Style.ToString();
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
                using (var dialog = new KeybindDialog(poisonStyleManager, keybinds ?? KeybindDialog.GetDefaultKeybinds(), this))
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
            
            // Ensure menu updates with rainbow colors when opened
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
                    Checked = false // Don't show checkmark - selection is indicated by background color
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
            }
            
            // Add Rainbow option
            var rainbowItem = new ToolStripMenuItem("Rainbow")
            {
                Tag = "Rainbow",
                Checked = false // Don't show checkmark - selection is indicated by background color
            };
            rainbowItem.Click += (s, e) =>
            {
                ChangeColorStyleToRainbow();
                UpdateSettingsColorStyleButton();
            };
            settingsColorStyleMenu.Items.Add(rainbowItem);
            
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
                if (IsRainbowStyleActive())
                {
                    btnSettingsColorStyle.Text = "Rainbow";
                }
                else
                {
                    btnSettingsColorStyle.Text = poisonStyleManager.Style.ToString();
                }
                
                // Update menu immediately - refresh renderer and invalidate
                if (settingsColorStyleMenu != null)
                {
                    // Update menu Style and Theme immediately
                    settingsColorStyleMenu.Style = poisonStyleManager.Style;
                    settingsColorStyleMenu.Theme = poisonStyleManager.Theme;
                    settingsColorStyleMenu.StyleManager = poisonStyleManager;
                    
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
            var t7Item = new ToolStripMenuItem("T7 (BO3)")
            {
                Tag = 0,
                Checked = false // Don't show checkmark - selection is indicated by background color
            };
            var t8Item = new ToolStripMenuItem("T8 (BO4)")
            {
                Tag = 1,
                Checked = false // Don't show checkmark - selection is indicated by background color
            };
            
            t7Item.Click += (s, e) => injectGameMenu_ItemClicked(s, new ToolStripItemClickedEventArgs(t7Item));
            t8Item.Click += (s, e) => injectGameMenu_ItemClicked(s, new ToolStripItemClickedEventArgs(t8Item));
            
            injectGameMenu.Items.Add(t7Item);
            injectGameMenu.Items.Add(t8Item);
            
            // Update button text
            btnInjectGame.Text = (currentInjectGameIndex == 0) ? "T7 (BO3)" : "T8 (BO4)";
            
            // Connect to style manager
            if (poisonStyleManager != null)
            {
                btnInjectGame.StyleManager = poisonStyleManager;
                EnhanceMenuItems(injectGameMenu);
            }
        }
        
        private void platformMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem is ToolStripMenuItem item && item.Tag is int index)
            {
                currentPlatformIndex = index;
                btnPlatform.Text = item.Text;
                
                // Menu items don't use checkmarks - selection is indicated by background color
                
                // Reset dropdown button pressed state after menu item selection
                ResetButtonState(btnPlatform);
                
                SaveSelectionState();
            }
        }
        
        private void gameMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem is ToolStripMenuItem item && item.Tag is int index)
            {
                currentGameIndex = index;
                btnGame.Text = item.Text;
                
                // Menu items don't use checkmarks - selection is indicated by background color
                
                // Reset dropdown button pressed state after menu item selection
                ResetButtonState(btnGame);
                
                SaveSelectionState();
            }
        }
        
        private void injectGameMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem is ToolStripMenuItem item && item.Tag is int index)
            {
                currentInjectGameIndex = index;
                btnInjectGame.Text = item.Text;
                
                // Menu items don't use checkmarks - selection is indicated by background color
                
                // Reset dropdown button pressed state after menu item selection
                ResetButtonState(btnInjectGame);
                
                SaveSelectionState();
                UpdateUI(); // Check if game is running
            }
        }
                       
        // StyleManager initialization removed - everything is configured in Designer (matching demo pattern)
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
            // Note: Toggles are now PoisonToggle controls - DisplayFocus and font settings are configured in Designer
            // This method is kept for potential future enhancements, but most settings are handled by StyleManager
        }
        
        // Progress spinner setup removed - configured in Designer (matching demo pattern)
        // Spinner automatically inherits from StyleManager when form has StyleManager set
        
        // Setup progress bar rainbow support (only custom functionality needed)
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
            var menus = new[]
            {
                gscConfModeMenu
            };
            
            foreach (var menu in menus)
            {
                if (menu != null && poisonStyleManager != null)
                {
                    // Ensure StyleManager is connected
                    menu.StyleManager = poisonStyleManager;
                    
                    // Ensure Theme and Style are set
                    menu.Theme = poisonStyleManager.Theme;
                    menu.Style = poisonStyleManager.Style;
                    
                    // Update menu items when menu is opened to ensure proper styling
                    menu.Opening += (s, e) =>
                    {
                        if (menu.StyleManager != null)
                        {
                            menu.Style = menu.StyleManager.Style;
                            menu.Theme = menu.StyleManager.Theme;
                            
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
                        // But we can ensure it uses the current style
                        if (menu.StyleManager != null)
                        {
                            menu.Style = menu.StyleManager.Style;
                            menu.Theme = menu.StyleManager.Theme;
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
                return parentForm != null ? parentForm.currentRainbowColor : Color.Red;
            }
            
            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                // Render menu background based on theme
                if (styleManager != null)
                {
                    ThemeStyle theme = styleManager.Theme;
                    Color backColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Form(theme);
                    
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
                // Hide separator lines - don't render them at all
                // This removes the white vertical line
            }
            
            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                // Hide toolstrip border - don't render it
                // This prevents any unwanted borders from showing
            }
            
            protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
            {
                // Hide image margin (the area where checkmarks/icons go) - use theme background
                if (styleManager != null)
                {
                    ThemeStyle theme = styleManager.Theme;
                    Color backColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Form(theme);
                    
                    using (SolidBrush brush = new SolidBrush(backColor))
                    {
                        e.Graphics.FillRectangle(brush, e.AffectedBounds);
                    }
                }
            }
            
            protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
            {
                // Hide checkmarks - don't render them at all
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
                        styleColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.GetStyleColor(currentStyle);
                    }
                    
                    Color menuBackColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Form(theme);
                    
                    // Check if item is hovered (Selected) or is the current active style (for color style menu)
                    bool isActiveStyle = false;
                    if (menuItem.Tag is ColorStyle tagStyle && styleManager != null)
                    {
                        // For color style menu, highlight if this is the current style
                        isActiveStyle = (styleManager.Style == tagStyle && !IsRainbowStyleActive());
                    }
                    else if (menuItem.Tag is string tag && tag == "Rainbow" && IsRainbowStyleActive())
                    {
                        // For rainbow option, highlight if rainbow is active
                        isActiveStyle = true;
                    }
                    
                    if (menuItem.Selected || isActiveStyle)
                    {
                        // Use style color with more opacity for selected/active state (replaces light blue)
                        using (SolidBrush brush = new SolidBrush(Color.FromArgb(150, styleColor)))
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
                    Color foreColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.ForeColor.Label.Normal(theme);
                    
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
                gscConfModeMenu,
                platformMenu,
                gameMenu,
                injectGameMenu,
                settingsColorStyleMenu,
                mainFormContextMenu,
                settingsContextMenu
            };
            
            foreach (var menu in menus)
            {
                if (menu != null)
                {
                    menu.Theme = theme;
                    menu.Style = poisonStyleManager.Style;
                    menu.StyleManager = poisonStyleManager;
                    
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
        }
        
        private void UpdateAllMenusStyle()
        {
            if (poisonStyleManager == null) return;
            
            // Update all dropdown menus and context menus immediately with current style
            var menus = new[]
            {
                gscConfModeMenu,
                platformMenu,
                gameMenu,
                injectGameMenu,
                settingsColorStyleMenu,
                mainFormContextMenu,
                settingsContextMenu
            };
            
            foreach (var menu in menus)
            {
                if (menu != null)
                {
                    // Update menu Style and Theme immediately
                    menu.Style = poisonStyleManager.Style;
                    menu.Theme = poisonStyleManager.Theme;
                    menu.StyleManager = poisonStyleManager;
                    
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
        }
        
        private void UpdateAllMenuRenderers()
        {
            // Update all menu renderers to use the current style from StyleManager
            var menus = new[]
            {
                gscConfModeMenu
            };
            
            foreach (var menu in menus)
            {
                if (menu != null && poisonStyleManager != null)
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
                    
                    menu.Invalidate();
                }
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
                btnResetParseTree, btnLaunchBO3,
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
                    
                    // Enable UseStyleColors so hover colors respect the Style property
                    var useStyleColorsProp = btn.GetType().GetProperty("UseStyleColors");
                    if (useStyleColorsProp != null && useStyleColorsProp.CanWrite)
                    {
                        useStyleColorsProp.SetValue(btn, true, null);
                    }
                    
                    // Ensure StyleManager is connected for proper theme/style updates
                    if (poisonStyleManager != null)
                    {
                        var styleManagerProp = btn.GetType().GetProperty("StyleManager");
                        if (styleManagerProp != null && styleManagerProp.CanWrite)
                        {
                            styleManagerProp.SetValue(btn, poisonStyleManager, null);
                        }
                    }
                    
                    // Subscribe to CustomPaint and wire up mouse events for PoisonButton controls
                    if (btn is ReaLTaiizor.Controls.PoisonButton poisonBtn)
                    {
                        // Get reflection info for internal state fields (reused for both paint and mouse events)
                        var isHoveredField = poisonBtn.GetType().GetField("isHovered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var isPressedField = poisonBtn.GetType().GetField("isPressed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        
                        // Subscribe to CustomPaint to apply style-based hover colors as an overlay
                        poisonBtn.CustomPaint += (s, e) =>
                        {
                            bool isHovered = isHoveredField?.GetValue(poisonBtn) as bool? ?? false;
                            bool isPressed = isPressedField?.GetValue(poisonBtn) as bool? ?? false;
                            
                            // Apply style-based hover colors as a semi-transparent overlay
                            // Use rainbow color if rainbow mode is active, otherwise use style color
                            Color styleColor;
                            if (IsRainbowStyleActive())
                            {
                                styleColor = currentRainbowColor;
                            }
                            else if (poisonBtn.Style != ColorStyle.Default)
                            {
                                styleColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.GetStyleColor(poisonBtn.Style);
                            }
                            else
                            {
                                return; // No color to apply
                            }
                            
                            if (isHovered && !isPressed && poisonBtn.Enabled)
                            {
                                // Use the style/rainbow color with transparency for hover overlay
                                using (SolidBrush brush = new SolidBrush(Color.FromArgb(80, styleColor)))
                                {
                                    e.Graphics.FillRectangle(brush, poisonBtn.ClientRectangle);
                                }
                            }
                            else if (isHovered && isPressed && poisonBtn.Enabled)
                            {
                                // Use a more opaque style/rainbow color for pressed state
                                using (SolidBrush brush = new SolidBrush(Color.FromArgb(120, styleColor)))
                                {
                                    e.Graphics.FillRectangle(brush, poisonBtn.ClientRectangle);
                                }
                            }
                        };
                        
                        // Wire up mouse events to ensure proper state reset
                        poisonBtn.MouseUp += (s, e) =>
                        {
                            // Force button to reset pressed state immediately
                            if (e.Button == MouseButtons.Left && isPressedField != null)
                            {
                                isPressedField.SetValue(poisonBtn, false);
                                poisonBtn.Invalidate();
                            }
                        };
                        
                        poisonBtn.MouseLeave += (s, e) =>
                        {
                            // Ensure button resets when mouse leaves - clear hover and pressed states
                            isHoveredField?.SetValue(poisonBtn, false);
                            isPressedField?.SetValue(poisonBtn, false);
                            
                            poisonBtn.Invalidate();
                        };
                        
                        poisonBtn.Click += (s, e) =>
                        {
                            // Force button to reset after click - ensure all states are cleared immediately
                            isHoveredField?.SetValue(poisonBtn, false);
                            isPressedField?.SetValue(poisonBtn, false);
                            
                                poisonBtn.Invalidate();
                        };
                        
                        // Add hover effect handlers for better feedback
                        poisonBtn.MouseEnter += (s, e) => 
                        {
                            if (poisonBtn.Enabled)
                            {
                                poisonBtn.Cursor = Cursors.Hand;
                                poisonBtn.Invalidate();
                            }
                        };
                    }
                    // Handle PoisonDropDownButton controls similarly
                    else if (btn is ReaLTaiizor.Controls.PoisonDropDownButton poisonDropDown)
                    {
                        // Ensure StyleManager is connected for proper theme/style updates
                        if (poisonStyleManager != null)
                        {
                            poisonDropDown.StyleManager = poisonStyleManager;
                            poisonDropDown.Style = poisonStyleManager.Style;
                            poisonDropDown.Theme = poisonStyleManager.Theme;
                            poisonDropDown.UseStyleColors = true;
                        }
                        
                        // Hide the dropdown arrow by setting ShowSplit to false
                        // This will hide the arrow and make the entire button clickable
                        var showSplitProp = poisonDropDown.GetType().GetProperty("ShowSplit");
                        if (showSplitProp != null && showSplitProp.CanWrite)
                        {
                            // We'll keep ShowSplit true for the menu functionality, but hide the arrow visually
                            // Actually, we need ShowSplit true for the menu to work, so we'll hide the arrow in CustomPaint
                        }
                        
                        // Get reflection info for internal state fields
                        var isHoveredField = poisonDropDown.GetType().GetField("isHovered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var isPressedField = poisonDropDown.GetType().GetField("isPressed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var dropDownRectangleField = poisonDropDown.GetType().GetField("dropDownRectangle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var stateProperty = poisonDropDown.GetType().GetProperty("State", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var setButtonDrawStateMethod = poisonDropDown.GetType().GetMethod("SetButtonDrawState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        
                        // Use Paint event to hide arrow - this fires AFTER all painting including the arrow
                        // PoisonDropDownButton.OnPaint calls base.OnPaint first, then draws the arrow
                        // So we need to use Paint event (not CustomPaintForeground) to draw over the arrow
                        poisonDropDown.Paint += (s, e) =>
                        {
                            bool isHovered = isHoveredField?.GetValue(poisonDropDown) as bool? ?? false;
                            bool isPressed = isPressedField?.GetValue(poisonDropDown) as bool? ?? false;
                            
                            // Get current style color (use StyleManager if available, otherwise use button's Style)
                            ColorStyle currentStyle = poisonDropDown.StyleManager != null ? poisonDropDown.StyleManager.Style : poisonDropDown.Style;
                            
                            // Hide the dropdown arrow by drawing over it with the button background
                            // The arrow is drawn in the rightmost 18 pixels (SplitSectionWidth)
                            int splitSectionWidth = 18;
                            Rectangle arrowRect = new Rectangle(poisonDropDown.Width - splitSectionWidth, 0, splitSectionWidth, poisonDropDown.Height);
                            
                            // Get the button's actual background color
                            // First try to get it from the button's BackColor, then parent, then form, then theme
                            ThemeStyle theme = poisonDropDown.Theme;
                            Color backColor = poisonDropDown.BackColor;
                            
                            // If BackColor is transparent or default, get from parent or form
                            if (backColor == Color.Transparent || backColor.A < 255 || backColor == SystemColors.Control)
                            {
                                if (poisonDropDown.Parent != null)
                                {
                                    backColor = poisonDropDown.Parent.BackColor;
                                }
                                else if (poisonDropDown.FindForm() != null)
                                {
                                    backColor = poisonDropDown.FindForm().BackColor;
                                }
                                else
                                {
                                    // Fallback to theme color based on state
                                    if (!poisonDropDown.Enabled)
                                    {
                                        backColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Button.Disabled(theme);
                                    }
                                    else if (isPressed)
                                    {
                                        backColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Button.Press(theme);
                                    }
                                    else if (isHovered)
                                    {
                                        backColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Button.Hover(theme);
                                    }
                                    else
                                    {
                                        backColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Button.Normal(theme);
                                    }
                                }
                            }
                            
                            // Draw over the arrow area with background color to hide it
                            // Use a slightly larger rectangle to ensure complete coverage
                            Rectangle hideRect = new Rectangle(poisonDropDown.Width - splitSectionWidth - 1, 0, splitSectionWidth + 2, poisonDropDown.Height);
                            using (SolidBrush brush = new SolidBrush(backColor))
                            {
                                e.Graphics.FillRectangle(brush, hideRect);
                            }
                            
                            // Also hide the split line (vertical line separating arrow from button)
                            if (poisonDropDown.RightToLeft == RightToLeft.Yes)
                            {
                                // Draw over left side split line
                                using (SolidBrush brush = new SolidBrush(backColor))
                                {
                                    e.Graphics.FillRectangle(brush, new Rectangle(0, 0, splitSectionWidth + 3, poisonDropDown.Height));
                                }
                            }
                            else
                            {
                                // Draw over right side split line (the vertical line before the arrow)
                                // Extend a bit to ensure the line is fully covered
                                using (SolidBrush brush = new SolidBrush(backColor))
                                {
                                    e.Graphics.FillRectangle(brush, new Rectangle(poisonDropDown.Width - splitSectionWidth - 3, 0, 3, poisonDropDown.Height));
                                }
                            }
                            
                            // Apply style-based hover colors as a semi-transparent overlay (on top of everything)
                            // Use rainbow color if rainbow mode is active, otherwise use style color
                            Color hoverColor;
                            if (IsRainbowStyleActive())
                            {
                                hoverColor = currentRainbowColor;
                            }
                            else if (currentStyle != ColorStyle.Default)
                            {
                                hoverColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.GetStyleColor(currentStyle);
                            }
                            else
                            {
                                return; // No color to apply
                            }
                            
                            if (isHovered && !isPressed && poisonDropDown.Enabled)
                            {
                                // Use the style/rainbow color with transparency for hover overlay
                                using (SolidBrush brush = new SolidBrush(Color.FromArgb(80, hoverColor)))
                                {
                                    e.Graphics.FillRectangle(brush, poisonDropDown.ClientRectangle);
                                }
                            }
                            else if (isHovered && isPressed && poisonDropDown.Enabled)
                            {
                                // Use a more opaque style/rainbow color for pressed state
                                using (SolidBrush brush = new SolidBrush(Color.FromArgb(120, hoverColor)))
                                {
                                    e.Graphics.FillRectangle(brush, poisonDropDown.ClientRectangle);
                                }
                            }
                        };
                        
                        // Wire up mouse events to make entire button clickable
                        // Since MouseDown event fires after base OnMouseDown, we show the menu here for any click
                        // This ensures the entire button area opens the menu, not just the arrow area
                        poisonDropDown.MouseDown += (s, e) =>
                        {
                            if (e.Button == MouseButtons.Left && poisonDropDown.ClientRectangle.Contains(e.Location))
                            {
                                // Show menu for any click on the button (not just arrow area)
                                if (poisonDropDown.SplitMenuStrip != null && !poisonDropDown.SplitMenuStrip.Visible)
                                {
                                    // Use reflection to call ShowContextMenuStrip method (same as base OnMouseDown does)
                                    var showContextMenuStripMethod = poisonDropDown.GetType().GetMethod("ShowContextMenuStrip", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                    if (showContextMenuStripMethod != null)
                                    {
                                        showContextMenuStripMethod.Invoke(poisonDropDown, null);
                                    }
                                    else
                                    {
                                        // Fallback to direct show
                                        poisonDropDown.SplitMenuStrip.Show(poisonDropDown, new Point(0, poisonDropDown.Height));
                                    }
                                    
                                    // Immediately reset pressed state after showing menu
                                    if (setButtonDrawStateMethod != null)
                                    {
                                        setButtonDrawStateMethod.Invoke(poisonDropDown, null);
                                    }
                                    else if (stateProperty != null && stateProperty.CanWrite)
                                    {
                                        // Fallback: set State to Normal
                                        var normalState = Enum.Parse(stateProperty.PropertyType, "Normal");
                                        stateProperty.SetValue(poisonDropDown, normalState);
                                    }
                                    
                                    isPressedField?.SetValue(poisonDropDown, false);
                                    
                                    poisonDropDown.Invalidate();
                                }
                            }
                        };
                        
                        // Reset button state when menu closes (handles both button click and arrow click)
                        if (poisonDropDown.SplitMenuStrip != null)
                        {
                            poisonDropDown.SplitMenuStrip.Closed += (s, e) =>
                            {
                                ResetButtonState(poisonDropDown);
                            };
                        }
                        
                        poisonDropDown.MouseUp += (s, e) =>
                        {
                            if (e.Button == MouseButtons.Left)
                            {
                                // Reset pressed state on mouse up
                                ResetButtonState(poisonDropDown);
                                
                                // Show the menu if SplitMenuStrip is set and not already visible
                                if (poisonDropDown.SplitMenuStrip != null && !poisonDropDown.SplitMenuStrip.Visible)
                                {
                                    // Show the menu
                                    poisonDropDown.SplitMenuStrip.Show(poisonDropDown, new Point(0, poisonDropDown.Height));
                                }
                                
                                // Reset state after menu is shown
                                poisonDropDown.BeginInvoke(new Action(() =>
                                {
                                    isHoveredField?.SetValue(poisonDropDown, false);
                                    isPressedField?.SetValue(poisonDropDown, false);
                                    poisonDropDown.Invalidate();
                                    poisonDropDown.Update();
                                    poisonDropDown.Refresh();
                                }));
                            }
                        };
                        
                        poisonDropDown.MouseLeave += (s, e) =>
                        {
                            // Only reset if menu is not showing
                            if (poisonDropDown.SplitMenuStrip == null || !poisonDropDown.SplitMenuStrip.Visible)
                            {
                                // Ensure button resets when mouse leaves - clear hover and pressed states
                                isHoveredField?.SetValue(poisonDropDown, false);
                                isPressedField?.SetValue(poisonDropDown, false);
                                
                                poisonDropDown.Invalidate();
                                poisonDropDown.Update();
                                poisonDropDown.Refresh();
                                Application.DoEvents();
                            }
                        };
                        
                        // Handle menu closing to reset state
                        if (poisonDropDown.SplitMenuStrip != null)
                        {
                            poisonDropDown.SplitMenuStrip.Closed += (s, e) =>
                            {
                                // Reset button state when menu closes using SetButtonDrawState
                                if (setButtonDrawStateMethod != null)
                                {
                                    setButtonDrawStateMethod.Invoke(poisonDropDown, null);
                                }
                                else if (stateProperty != null && stateProperty.CanWrite)
                                {
                                    // Fallback: set State to Normal
                                    var normalState = Enum.Parse(stateProperty.PropertyType, "Normal");
                                    stateProperty.SetValue(poisonDropDown, normalState);
                                }
                                
                                isHoveredField?.SetValue(poisonDropDown, false);
                                isPressedField?.SetValue(poisonDropDown, false);
                                
                                poisonDropDown.Invalidate();
                            };
                        }
                        
                        poisonDropDown.Click += (s, e) =>
                        {
                            // Show menu if not already visible (backup in case MouseUp didn't trigger)
                            if (poisonDropDown.SplitMenuStrip != null && !poisonDropDown.SplitMenuStrip.Visible)
                            {
                                poisonDropDown.SplitMenuStrip.Show(poisonDropDown, new Point(0, poisonDropDown.Height));
                            }
                            
                            // Reset state after click
                            isHoveredField?.SetValue(poisonDropDown, false);
                            isPressedField?.SetValue(poisonDropDown, false);
                                poisonDropDown.Invalidate();
                        };
                        
                        // Add hover effect handlers for better feedback
                        poisonDropDown.MouseEnter += (s, e) => 
                        {
                            if (poisonDropDown.Enabled)
                            {
                                poisonDropDown.Cursor = Cursors.Hand;
                                poisonDropDown.Invalidate();
                            }
                        };
                    }
                    else
                    {
                        // Fallback for non-PoisonButton controls
                        btn.MouseUp += (s, e) =>
                        {
                            if (e.Button == MouseButtons.Left)
                            {
                                btn.Invalidate();
                                btn.Update();
                            }
                        };
                        
                        btn.MouseLeave += (s, e) =>
                        {
                            btn.Invalidate();
                            btn.Update();
                        };
                        
                        // Add hover effect handlers for better feedback
                        btn.MouseEnter += (s, e) => 
                        {
                            if (btn.Enabled)
                            {
                                btn.Cursor = Cursors.Hand;
                            }
                        };
                        
                        btn.MouseLeave += (s, e) => 
                        {
                            btn.Cursor = Cursors.Default;
                        };
                    }
                }
            }
            
            // Also enhance dropdown buttons separately (they're not in importantButtons array)
            var dropdownButtons = new[]
            {
                btnGscConfMode, btnPlatform, btnGame, btnInjectGame
            };
            
            foreach (var dropDown in dropdownButtons)
            {
                if (dropDown != null)
                {
                    // Ensure UseStyleColors and StyleManager are set
                    var useStyleColorsProp = dropDown.GetType().GetProperty("UseStyleColors");
                    if (useStyleColorsProp != null && useStyleColorsProp.CanWrite)
                    {
                        useStyleColorsProp.SetValue(dropDown, true, null);
                    }
                    
                    if (poisonStyleManager != null)
                    {
                        var styleManagerProp = dropDown.GetType().GetProperty("StyleManager");
                        if (styleManagerProp != null && styleManagerProp.CanWrite)
                        {
                            styleManagerProp.SetValue(dropDown, poisonStyleManager, null);
                        }
                    }
                    
                    // Get reflection info for internal state fields
                    var isHoveredField = dropDown.GetType().GetField("isHovered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var isPressedField = dropDown.GetType().GetField("isPressed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    // Subscribe to CustomPaint to apply style-based hover colors as an overlay
                    dropDown.CustomPaint += (s, e) =>
                    {
                        bool isHovered = isHoveredField?.GetValue(dropDown) as bool? ?? false;
                        bool isPressed = isPressedField?.GetValue(dropDown) as bool? ?? false;
                        
                        // Apply style-based hover colors as a semi-transparent overlay
                        if (isHovered && !isPressed && dropDown.Enabled && dropDown.Style != ColorStyle.Default)
                        {
                            // Use the style color with transparency for hover overlay
                            Color styleColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.GetStyleColor(dropDown.Style);
                            using (SolidBrush brush = new SolidBrush(Color.FromArgb(80, styleColor)))
                            {
                                e.Graphics.FillRectangle(brush, dropDown.ClientRectangle);
                            }
                        }
                        else if (isHovered && isPressed && dropDown.Enabled && dropDown.Style != ColorStyle.Default)
                        {
                            // Use a more opaque style color for pressed state
                            Color styleColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.GetStyleColor(dropDown.Style);
                            using (SolidBrush brush = new SolidBrush(Color.FromArgb(120, styleColor)))
                            {
                                e.Graphics.FillRectangle(brush, dropDown.ClientRectangle);
                            }
                        }
                    };
                    
                    // Wire up mouse events to ensure proper state reset
                    dropDown.MouseUp += (s, e) =>
                    {
                        // Force button to reset pressed state immediately
                        if (e.Button == MouseButtons.Left && isPressedField != null)
                        {
                            isPressedField.SetValue(dropDown, false);
                            dropDown.Invalidate();
                            dropDown.Update();
                            Application.DoEvents();
                        }
                    };
                    
                    dropDown.MouseLeave += (s, e) =>
                    {
                        // Ensure button resets when mouse leaves - clear hover and pressed states
                        isHoveredField?.SetValue(dropDown, false);
                        isPressedField?.SetValue(dropDown, false);
                        
                        dropDown.Invalidate();
                        dropDown.Update();
                        Application.DoEvents();
                    };
                    
                    dropDown.Click += (s, e) =>
                    {
                        // Ensure button resets after click
                        isHoveredField?.SetValue(dropDown, false);
                        isPressedField?.SetValue(dropDown, false);
                        dropDown.Invalidate();
                        dropDown.Update();
                        Application.DoEvents();
                    };
                    
                    // Add hover effect handlers for better feedback
                    dropDown.MouseEnter += (s, e) => 
                    {
                        if (dropDown.Enabled)
                        {
                            dropDown.Cursor = Cursors.Hand;
                            dropDown.Invalidate();
                        }
                    };
                }
            }
        }
        
        private void LoadGscConfMode()
        {
            isLoadingGscConfMode = true;
            try
            {
                if (string.IsNullOrWhiteSpace(txtProjectFolder.Text) || !Directory.Exists(txtProjectFolder.Text))
                {
                    btnGscConfMode.Text = "";
                    btnGscConfMode.Tag = -1;
                    return;
                }

                // Ensure menu is populated before accessing items
                if (gscConfModeMenu != null && gscConfModeMenu.Items.Count == 0)
                {
                    SetupGscConfModeMenu();
                }

                string projectFolder = txtProjectFolder.Text;
                string gscConfPath = Path.Combine(projectFolder, "gsc.conf");
                
                bool hasMP = false;
                bool hasZM = false;
                
                // First, try to read from gsc.conf if it exists
                if (File.Exists(gscConfPath))
                {
                    try
                    {
                        foreach (string line in File.ReadAllLines(gscConfPath))
                        {
                            if (line.Trim().StartsWith("#")) continue;
                            var split = line.Trim().Split('=');
                            if (split.Length < 2) continue;
                            
                            if (split[0].Trim().Equals("symbols", StringComparison.OrdinalIgnoreCase))
                            {
                                string symbols = split[1].Trim();
                                var symbolList = symbols.Split(',').Select(s => s.Trim());
                                
                                hasMP = symbolList.Any(s => s.Equals("MP", StringComparison.OrdinalIgnoreCase));
                                hasZM = symbolList.Any(s => s.Equals("ZM", StringComparison.OrdinalIgnoreCase));
                                break;
                            }
                        }
                    }
                    catch
                    {
                        // If reading gsc.conf fails, fall through to folder scanning
                    }
                }
                
                // If gsc.conf doesn't exist or doesn't have MP/ZM, scan folder structure
                if (!hasMP && !hasZM)
                {
                    try
                    {
                        // Scan for GSC files and check folder structure
                        // Exclude Zombies files if MP is set, exclude Multiplayer files if ZM is set
                        var gscFiles = Directory.GetFiles(projectFolder, "*.gsc", SearchOption.AllDirectories)
                            .Where(f => !f.EndsWith(".gscc", StringComparison.OrdinalIgnoreCase) && 
                                        !f.EndsWith(".stub.gscc", StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        
                        if (gscFiles.Count > 0)
                        {
                            // Check folder structure to determine MP vs ZM
                            int mpFiles = 0;
                            int zmFiles = 0;
                            
                            foreach (string file in gscFiles)
                            {
                                string normalizedPath = file.Replace("\\", "/").ToLower();
                                
                                // Check for Multiplayer folder/files
                                if (normalizedPath.Contains("/multiplayer/") || 
                                    normalizedPath.EndsWith("/multiplayer.gsc") ||
                                    normalizedPath.Contains("\\multiplayer\\"))
                                {
                                    mpFiles++;
                                }
                                
                                // Check for Zombies folder/files
                                if (normalizedPath.Contains("/zombies/") || 
                                    normalizedPath.EndsWith("/zombies.gsc") ||
                                    normalizedPath.Contains("\\zombies\\"))
                                {
                                    zmFiles++;
                                }
                            }
                            
                            // Determine mode based on file counts
                            if (mpFiles > 0 && zmFiles == 0)
                                hasMP = true;
                            else if (zmFiles > 0 && mpFiles == 0)
                                hasZM = true;
                            else if (mpFiles > zmFiles)
                                hasMP = true; // More MP files, default to MP
                            else if (zmFiles > mpFiles)
                                hasZM = true; // More ZM files, default to ZM
                            // If equal or both zero, default to MP below
                        }
                    }
                    catch
                    {
                        // If scanning fails, default to MP
                    }
                }
                // If MP is set in gsc.conf, ensure ZM is not detected (even if ZM files exist)
                else if (hasMP && hasZM)
                {
                    // If both are set, prefer MP (MP takes priority)
                    hasZM = false;
                }
                
                // Set selection based on what was found (MP takes priority if both exist or neither found)
                int selectedIndex = 0; // Default to MP
                if (hasMP && !hasZM)
                    selectedIndex = 0; // MP
                else if (hasZM && !hasMP)
                    selectedIndex = 1; // ZM
                else
                    selectedIndex = 0; // Default to MP if both exist or neither found
                
                currentGscConfModeIndex = selectedIndex;
                btnGscConfMode.Tag = selectedIndex;
                if (gscConfModeMenu != null && gscConfModeMenu.Items.Count > selectedIndex)
                {
                    if (gscConfModeMenu.Items[selectedIndex] is ToolStripMenuItem selectedItem)
                    {
                        btnGscConfMode.Text = selectedItem.Text;
                        foreach (ToolStripMenuItem menuItem in gscConfModeMenu.Items)
                        {
                            menuItem.Checked = (menuItem.Tag is int tag && tag == selectedIndex);
                        }
                    }
                }
            }
            finally
            {
                isLoadingGscConfMode = false;
            }
        }
        
        private void SetupGscConfModeMenu()
        {
            if (gscConfModeMenu == null) return;
            
            gscConfModeMenu.Items.Clear();
            var mpItem = new ToolStripMenuItem("MP")
            {
                Tag = 0,
                Checked = false // Don't show checkmark - selection is indicated by background color
            };
            var zmItem = new ToolStripMenuItem("ZM")
            {
                Tag = 1,
                Checked = false // Don't show checkmark - selection is indicated by background color
            };
            
            gscConfModeMenu.Items.Add(mpItem);
            gscConfModeMenu.Items.Add(zmItem);
            
            // Enhance menu items with proper styling
            if (poisonStyleManager != null)
            {
                EnhanceMenuItems(gscConfModeMenu);
            }
        }
        
        private void gscConfModeMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem is ToolStripMenuItem item && item.Tag is int index)
            {
                currentGscConfModeIndex = index;
                btnGscConfMode.Text = item.Text;
                btnGscConfMode.Tag = index;
                
                // Menu items don't use checkmarks - selection is indicated by background color
                
                // Update gsc.conf file
                UpdateGscConfFile(item.Text);
                
                // Save state
                SaveSelectionState();
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
        
        private void SetCompileState(bool enabled, bool showProgress = false)
        {
            if (btnCompile != null)
        {
            btnCompile.Enabled = enabled;
            }
            isCompiling = !enabled;
            
            // Show/hide progress bar
            if (progressBar != null)
            {
                progressBar.Visible = showProgress && !enabled && !isInjecting;
                if (progressBar.Visible)
                {
                    progressBar.Value = 0; // Reset to 0 when starting
                }
            }
            
            if (progressSpinner != null)
            {
                // Show spinner when compiling and showProgress is true
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
        
        private void SetInjectState(bool enabled, bool showProgress = false)
        {
            if (btnInject != null)
        {
            btnInject.Enabled = enabled;
            }
            isInjecting = !enabled;
            
            // Show/hide progress bar
            if (progressBar != null)
            {
                progressBar.Visible = showProgress && !enabled && !isCompiling;
                if (progressBar.Visible)
                {
                    progressBar.Value = 0; // Reset to 0 when starting
                }
            }
            
            if (progressSpinner != null)
            {
                // Show spinner when injecting and showProgress is true
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
        
        private void SaveSelectionState()
        {
            // Selection state is now saved as part of SaveAllSettings()
            SaveAllSettings();
        }
        
        private void UpdateGscConfFile(string selectedMode)
        {
            if (isLoadingGscConfMode) return; // Don't update file during initial load
            if (string.IsNullOrWhiteSpace(txtProjectFolder.Text) || !Directory.Exists(txtProjectFolder.Text))
            {
                txtLog.AppendText("[ERROR] Please select a project folder first.\r\n");
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
                return;
            }

            string gscConfPath = Path.Combine(txtProjectFolder.Text, "gsc.conf");
            
            try
            {
                List<string> lines = new List<string>();
                bool symbolsLineFound = false;
                
                if (File.Exists(gscConfPath))
                {
                    lines = File.ReadAllLines(gscConfPath).ToList();
                }
                
                // Find and update the symbols line
                for (int i = 0; i < lines.Count; i++)
                {
                    string line = lines[i];
                    if (line.Trim().StartsWith("#")) continue;
                    
                    var split = line.Trim().Split('=');
                    if (split.Length >= 2 && split[0].Trim().Equals("symbols", StringComparison.OrdinalIgnoreCase))
                    {
                        symbolsLineFound = true;
                        string symbols = split[1].Trim();
                        var symbolList = symbols.Split(',').Select(s => s.Trim()).ToList();
                        
                        // Remove both MP and ZM if they exist
                        symbolList.RemoveAll(s => s.Equals("MP", StringComparison.OrdinalIgnoreCase));
                        symbolList.RemoveAll(s => s.Equals("ZM", StringComparison.OrdinalIgnoreCase));
                        
                        // Add the selected mode
                        symbolList.Add(selectedMode);
                        
                        // Rebuild the symbols line
                        lines[i] = $"symbols={string.Join(",", symbolList)}";
                        break;
                    }
                }
                
                // If symbols line not found, add it
                if (!symbolsLineFound)
                {
                    // Default symbols if file is new
                    if (lines.Count == 0)
                    {
                        lines.Add($"symbols=BO3,SYNERGY,{selectedMode},DEBUG,V3CLIENTEDITIN,SynergyModding");
                    }
                    else
                    {
                        // Insert at the beginning
                        lines.Insert(0, $"symbols=BO3,SYNERGY,{selectedMode},DEBUG,V3CLIENTEDITIN,SynergyModding");
                    }
                }
                
                // Write the file
                File.WriteAllLines(gscConfPath, lines);
                txtLog.AppendText($"[INFO] Updated gsc.conf: Set mode to {selectedMode}\r\n");
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"[ERROR] Failed to update gsc.conf: {ex.Message}\r\n");
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
            }
        }
        
        private void btnSelectProjectFolder_Click(object sender, EventArgs e)
        {
            // Ensure we start from a directory, not a file path
            string initialPath = txtProjectFolder.Text;
            if (!string.IsNullOrWhiteSpace(initialPath))
            {
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
            
            string folder = ShowModernFolderDialog("Select GSC Project Folder", initialPath);
            if (!string.IsNullOrEmpty(folder))
            {
                txtProjectFolder.Text = folder;
                AutoPopulateOutputFile();
                UpdateUI();
                LoadGscConfMode(); // Reload gsc.conf mode when project folder changes
            }
            // Explicitly reset button state after dialog closes
            ResetButtonState(sender);
        }

        private void ResetButtonState(object sender)
        {
            // Handle PoisonButton
            if (sender is PoisonButton btn)
            {
                // Use reflection to reset button internal state
                var isHoveredField = btn.GetType().GetField("isHovered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var isPressedField = btn.GetType().GetField("isPressed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                isHoveredField?.SetValue(btn, false);
                isPressedField?.SetValue(btn, false);
                
                btn.Invalidate();
            }
            // Handle PoisonDropDownButton
            else if (sender is PoisonDropDownButton dropDown)
            {
                // Use reflection to reset dropdown button internal state
                var isHoveredField = dropDown.GetType().GetField("isHovered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var isPressedField = dropDown.GetType().GetField("isPressed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                isHoveredField?.SetValue(dropDown, false);
                isPressedField?.SetValue(dropDown, false);
                
                dropDown.Invalidate();
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
                
                // Determine initial path - use directory from current output file if it exists, otherwise use build folder
                string initialDir = buildFolder;
                string initialFileName = "compiled.gscc";
                
                if (!string.IsNullOrWhiteSpace(txtOutputFile.Text) && Path.IsPathRooted(txtOutputFile.Text))
                {
                    string currentDir = Path.GetDirectoryName(txtOutputFile.Text);
                    if (!string.IsNullOrWhiteSpace(currentDir) && Directory.Exists(currentDir))
                    {
                        initialDir = currentDir;
                    }
                    
                    string existingFileName = Path.GetFileName(txtOutputFile.Text);
                    if (!string.IsNullOrWhiteSpace(existingFileName))
                    {
                        initialFileName = existingFileName;
                    }
                }
                
                saveDialog.InitialDirectory = initialDir;
                saveDialog.FileName = initialFileName;
                
                if (saveDialog.ShowDialog(this) == DialogResult.OK)
                {
                    txtOutputFile.Text = saveDialog.FileName;
                    UpdateUI();
                }
            }
            
            // Explicitly reset button state after dialog closes
            ResetButtonState(sender);
        }

        private void UpdateUI()
        {
            bool hasProjectFolder = !string.IsNullOrWhiteSpace(txtProjectFolder.Text);
            bool hasOutputFile = !string.IsNullOrWhiteSpace(txtOutputFile.Text);
            bool folderExists = hasProjectFolder && Directory.Exists(txtProjectFolder.Text);
            
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
            Games targetGame = Games.T7;
            if (currentInjectGameIndex == 1)
                targetGame = Games.T8;
            
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

        private void btnCompile_Click(object sender, EventArgs e)
        {
            // Reset button state immediately
            ResetButtonState(sender);
            
            if (string.IsNullOrWhiteSpace(txtProjectFolder.Text) || string.IsNullOrWhiteSpace(txtOutputFile.Text))
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, "Please select a project folder and output file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!Directory.Exists(txtProjectFolder.Text))
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
            txtLog.AppendText("Starting compilation...\r\n");
            Application.DoEvents();

            try
            {
                // Get game first (needed for BO3/BO4 determination)
                Games game = Games.T7; // DebugCompiler default
                if (GetGameIndex() == 1)
                    game = Games.T8;
                bool isT7 = game == Games.T7;

                // Load conditional symbols from gsc.conf (DebugCompiler ALWAYS reads it if it exists)
                List<string> conditionalSymbols = new List<string>();
                string gscConfPath = Path.Combine(txtProjectFolder.Text, "gsc.conf");
                string scriptLocation = txtProjectFolder.Text; // Default to project folder
                
                if (File.Exists(gscConfPath))
                {
                    // Shorten path to show just 1-2 directories before filename
                    string shortPath = GetShortPath(gscConfPath);
                    txtLog.AppendText($"Found gsc.conf at: {shortPath}\r\n");
                    Application.DoEvents();
                    
                    foreach (string line in File.ReadAllLines(gscConfPath))
                    {
                        if (line.Trim().StartsWith("#")) continue;
                        var split = line.Trim().Split('=');
                        if (split.Length < 2) continue;
                        switch (split[0].Trim().ToLower())
                        {
                            case "symbols":
                                foreach (string token in split[1].Trim().Split(','))
                                {
                                    string trimmedToken = token.Trim();
                                    if (!string.IsNullOrEmpty(trimmedToken))
                                    {
                                        conditionalSymbols.Add(trimmedToken);
                                    }
                                }
                                break;
                            case "scriptlocation":
                                // Support relative or absolute script location (DebugCompiler line 577-579)
                                string loc = split[1].Trim();
                                if (Path.IsPathRooted(loc))
                                    scriptLocation = loc;
                                else
                                    scriptLocation = Path.Combine(txtProjectFolder.Text, loc);
                                txtLog.AppendText($"Using scriptlocation: {scriptLocation}\r\n");
                                break;
                            case "game":
                                // Allow gsc.conf to override game selection (DebugCompiler line 583-588)
                                if (Enum.TryParse<Games>(split[1].Trim(), true, out Games confGame))
                                {
                                    game = confGame;
                                    txtLog.AppendText($"Game set from gsc.conf: {game}\r\n");
                                }
                                break;
                        }
                    }
                }
                else
                {
                    txtLog.AppendText($"No gsc.conf found at: {gscConfPath}\r\n");
                    txtLog.AppendText("Using default symbols only.\r\n");
                    Application.DoEvents();
                }

                // Always add BO3/BO4 AFTER loading gsc.conf (matching DebugCompiler line 645)
                // DebugCompiler ALWAYS adds it without checking for duplicates
                // ConditionalBlocks uses a HashSet internally, so duplicates are handled there
                string boSymbol = isT7 ? "BO3" : "BO4";
                conditionalSymbols.Add(boSymbol);

                // Verify script location exists
                if (!Directory.Exists(scriptLocation))
                {
                    txtLog.AppendText($"ERROR: Script location does not exist: {scriptLocation}\r\n");
                    SetCompileState(true, false);
                    return;
                }

                // Smart folder exclusion based on symbols - determine mode first
                bool hasMP = conditionalSymbols.Any(s => s.Equals("MP", StringComparison.OrdinalIgnoreCase));
                bool hasZM = conditionalSymbols.Any(s => s.Equals("ZM", StringComparison.OrdinalIgnoreCase));
                
                // Load all GSC files with immediate filtering based on symbols
                // This ensures ZM files are never loaded/processed when compiling for MP
                // Match DebugCompiler exactly: Directory.GetFiles(scriptLocation, "*.gsc", SearchOption.AllDirectories)
                // Filter out compiled files (.gscc, .stub.gscc, etc.) - only include source .gsc files
                var allGscFiles = Directory.GetFiles(scriptLocation, "*.gsc", SearchOption.AllDirectories)
                    .Where(f => !f.EndsWith(".gscc", StringComparison.OrdinalIgnoreCase) && 
                                !f.EndsWith(".stub.gscc", StringComparison.OrdinalIgnoreCase));
                
                // Apply folder exclusion immediately during file loading to prevent ZM/MP files from being processed
                // The debug compiler handles this via symbols, so we filter silently without logging
                List<string> gscFiles;
                if (hasMP && !hasZM)
                {
                    // Compiling for MP only - exclude Zombies folder immediately
                    gscFiles = allGscFiles.Where(f => 
                        !f.Replace("\\", "/").ToLower().Contains("/zombies/") &&
                        !f.Replace("\\", "/").ToLower().EndsWith("/zombies.gsc"))
                        .ToList();
                }
                else if (hasZM && !hasMP)
                {
                    // Compiling for ZM only - exclude Multiplayer folder immediately
                    gscFiles = allGscFiles.Where(f => 
                        !f.Replace("\\", "/").ToLower().Contains("/multiplayer/") &&
                        !f.Replace("\\", "/").ToLower().EndsWith("/multiplayer.gsc"))
                        .ToList();
                }
                else
                {
                    // Both or neither set - include all files
                    gscFiles = allGscFiles.ToList();
                }
                
                // Show symbols and file count in one line
                string symbolsStr = conditionalSymbols.Count > 0 ? string.Join(", ", conditionalSymbols) : "none";
                txtLog.AppendText($"Symbols: {symbolsStr} | Files: {gscFiles.Count}\r\n");
                Application.DoEvents();

                if (gscFiles.Count == 0)
                {
                    ReaLTaiizor.Controls.PoisonMessageBox.Show(this, $"No .gsc files found in:\n{scriptLocation}", "No GSC Files", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    SetCompileState(true, false);
                    return;
                }

                // Combine all GSC files - matching DebugCompiler exactly
                StringBuilder sb = new StringBuilder();
                List<SourceTokenDef> sourceTokens = new List<SourceTokenDef>();
                int currentLineCount = 0;  // 0-based like DebugCompiler
                int currentCharCount = 0;

                foreach (string file in gscFiles)
                {
                    var token = new SourceTokenDef
                    {
                        FilePath = file.Replace(scriptLocation, "").Substring(1).Replace("\\", "/"),
                        LineStart = currentLineCount,  // 0-based like DebugCompiler
                        CharStart = currentCharCount,
                        LineMappings = new Dictionary<int, (int CStart, int CEnd)>()
                    };

                    foreach (var line in File.ReadAllLines(file))
                    {
                        // Map BEFORE incrementing (matching DebugCompiler line 629)
                        token.LineMappings[currentLineCount] = (currentCharCount, currentCharCount + line.Length + 1);
                        sb.Append(line);
                        sb.Append("\n");
                        currentLineCount += 1;  // Increment AFTER mapping (matching DebugCompiler line 632)
                        currentCharCount += line.Length + 1; // + \n
                    }

                    token.LineEnd = currentLineCount;
                    token.CharEnd = currentCharCount;
                    sourceTokens.Add(token);
                    
                    // Account for the extra newline between files (matching DebugCompiler line 639)
                    sb.Append("\n");
                    currentLineCount += 1;
                    currentCharCount += 1;
                }

                string source = sb.ToString();
                txtLog.AppendText("Processing conditionals...\r\n");
                Application.DoEvents();

                // Process conditional compilation
                var ppc = new ConditionalBlocks();
                ppc.LoadConditionalTokens(conditionalSymbols);

                List<SourceTokenDef> originalTokens = new List<SourceTokenDef>(sourceTokens);

                try
                {
                    source = ppc.ParseSource(source);
                }
                catch (CBSyntaxException error)
                {
                    // Match DebugCompiler's error handling logic exactly
                    int errorCharPos = error.ErrorPosition;
                    int numLineBreaks = 0; // Track extra newlines between files
                    string errorFile = null;
                    int errorLine = 0;
                    int errorPos = 0;
                    bool found = false;
                    
                    foreach (var stok in sourceTokens)
                    {
                        // Check if error is in this file's range
                        if (errorCharPos >= stok.CharStart && errorCharPos <= stok.CharEnd)
                        {
                            errorFile = stok.FilePath;
                            // Adjust for inserted linebreaks between files (DebugCompiler line 666)
                            int adjustedCharPos = errorCharPos - numLineBreaks;
                            
                            // Find the exact line
                            foreach (var lineMapping in stok.LineMappings)
                            {
                                var (CStart, CEnd) = lineMapping.Value;
                                if (adjustedCharPos >= CStart && adjustedCharPos <= CEnd)
                                {
                                    // Match DebugCompiler: line.Key - stok.LineStart (no +1)
                                    errorLine = lineMapping.Key - stok.LineStart;
                                    errorPos = adjustedCharPos - CStart;
                                    found = true;
                                    break;
                                }
                            }
                            break;
                        }
                        numLineBreaks++; // Account for \n appended after each file
                    }
                    
                    txtLog.AppendText($"\r\n=== PREPROCESSOR SYNTAX ERROR ===\r\n");
                    txtLog.AppendText($"Error: {error.Message}\r\n");
                    
                    if (found && errorFile != null)
                    {
                        txtLog.AppendText($"File: scripts/{errorFile}\r\n");
                        txtLog.AppendText($"Line: {errorLine}, Position: {errorPos}\r\n");
                        
                        // Try to show the actual line content
                        try
                        {
                            string fullPath = Path.Combine(scriptLocation, errorFile.Replace("/", "\\"));
                            if (File.Exists(fullPath))
                            {
                                string[] lines = File.ReadAllLines(fullPath);
                                if (errorLine > 0 && errorLine <= lines.Length)
                                {
                                    txtLog.AppendText($"\r\nLine content:\r\n");
                                    int contextStart = Math.Max(0, errorLine - 3);
                                    int contextEnd = Math.Min(lines.Length, errorLine + 2);
                                    for (int i = contextStart; i < contextEnd; i++)
                                    {
                                        string marker = (i + 1 == errorLine) ? ">>> " : "    ";
                                        txtLog.AppendText($"{marker}{i + 1,4}: {lines[i]}\r\n");
                                    }
                                }
                            }
                        }
                        catch { /* Ignore errors reading file for context */ }
                        
                        txtLog.AppendText($"\r\n");
                        txtLog.AppendText("Check this file for unmatched #ifdef/#ifndef/#else/#endif directives.\r\n");
                    }
                    else
                    {
                        txtLog.AppendText($"Character Position: {errorCharPos}\r\n");
                        txtLog.AppendText($"Total Source Length: {source.Length}\r\n");
                        txtLog.AppendText($"\r\n");
                        txtLog.AppendText("Tip: Check all GSC files for unmatched preprocessor directives.\r\n");
                    }
                    txtLog.AppendText("Each #ifdef or #ifndef must have a corresponding #endif.\r\n");
                    txtLog.AppendText($"\r\n");
                    
                    // Scroll to bottom to show error
                    txtLog.SelectionStart = txtLog.Text.Length;
                    txtLog.ScrollToCaret();
                    
                    SetCompileState(true, false);
                    return;
                }

                // Determine compilation mode from symbols (hasMP/hasZM already defined above)
                Modes compileMode = Modes.MP; // Default to MP
                
                if (hasZM && !hasMP)
                {
                    compileMode = Modes.ZM;
                }
                else if (hasMP)
                {
                    compileMode = Modes.MP;
                }
                // If both are present, default to MP

                // Get platform from UI (game already declared above, line 285-287)
                // Only PC and PS4 now (PC, Steam, XB1 are all the same)
                Platforms platform = Platforms.PC; // Default fallback
                int platformIndex = GetPlatformIndex();
                switch (platformIndex)
                {
                    case 0: platform = Platforms.PC; break; // PC (index 0)
                    case 1: platform = Platforms.PS4; break; // PS4 (index 1)
                    default: platform = Platforms.PC; break;
                }

                bool useMasking = chkOpcodeMasking.Checked;

                // Log platform selection and expected database file
                string expectedDbFile = (platform == Platforms.PS4) ? "T7PS4V2.db" : "t7pcv2.db";
                string expectedDbPath = Path.Combine(exeDir, expectedDbFile);
                bool dbExists = File.Exists(expectedDbPath);
                
                txtLog.AppendText($"Compiling with Platform: {platform}, Game: {game}, Mode: {compileMode}, Masking: {useMasking}\r\n");
                txtLog.AppendText($"Expected database: {expectedDbFile}\r\n");
                txtLog.AppendText($"Database path: {expectedDbPath}\r\n");
                txtLog.AppendText($"Database exists: {dbExists}\r\n");
                if (!dbExists)
                {
                    txtLog.AppendText($"WARNING: {expectedDbFile} not found! Compilation may fail.\r\n");
                }
                Application.DoEvents();

                // Compile - matching DebugCompiler exactly: no try-catch, no path parameter (empty string default)
                // DebugCompiler line 683: code = Compiler.Compile(platform, game, Modes.MP, false, source);
                
                // Update progress bar to show compilation started
                if (progressBar != null)
                {
                    progressBar.Value = 10;
                    progressBar.Invalidate();
                    Application.DoEvents();
                }
                
                CompiledCode code = Compiler.Compile(platform, game, compileMode, useMasking, source);
                
                // Update progress bar to show compilation completed
                if (progressBar != null)
                {
                    progressBar.Value = 90;
                    progressBar.Invalidate();
                    Application.DoEvents();
                }

                if (code == null)
                {
                    txtLog.AppendText($"\r\n=== COMPILER ERROR ===\r\n");
                    txtLog.AppendText($"Compiler returned null result.\r\n\r\n");
                    
                    // Scroll to bottom to show error
                    txtLog.SelectionStart = txtLog.Text.Length;
                    txtLog.ScrollToCaret();
                    
                    SetCompileState(true, false);
                    return;
                }

                if (code.Error != null && !string.IsNullOrWhiteSpace(code.Error))
                {
                    // Enhanced error parsing - handle multiple error formats
                    string errorMsg = code.Error;
                    string errorFile = null;
                    int errorLine = 0;
                    string errorDescription = errorMsg;
                    
                    // Parse error format: "File: scripts/... Line: XX Error: ..."
                    if (errorMsg.Contains("File:") && errorMsg.Contains("Line:"))
                    {
                        int fileIndex = errorMsg.IndexOf("File:");
                        int lineIndex = errorMsg.IndexOf("Line:");
                        int errorIndex = errorMsg.IndexOf("Error:");
                    
                        if (fileIndex >= 0 && lineIndex > fileIndex)
                    {
                            // Extract file name
                            string fileSection = errorMsg.Substring(fileIndex + "File:".Length, lineIndex - fileIndex - "File:".Length).Trim();
                            errorFile = fileSection;
                            
                            // Extract line number
                            if (errorIndex > lineIndex)
                            {
                                string lineSection = errorMsg.Substring(lineIndex + "Line:".Length, errorIndex - lineIndex - "Line:".Length).Trim();
                                if (int.TryParse(lineSection, out int parsedLine))
                                    errorLine = parsedLine;
                                
                                // Extract error description
                                errorDescription = errorMsg.Substring(errorIndex + "Error:".Length).Trim();
                            }
                            else
                            {
                                string lineSection = errorMsg.Substring(lineIndex + "Line:".Length).Trim();
                                if (int.TryParse(lineSection.Split(new[] { '\r', '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries)[0], out int parsedLine))
                                    errorLine = parsedLine;
                            }
                        }
                    }
                    // Parse format: "Syntax error in input script! [line=XXX]"
                    else if (errorMsg.Contains("Syntax error") && errorMsg.Contains("line="))
                    {
                        int lineIndex = errorMsg.IndexOf("line=");
                        int lineStart = lineIndex + "line=".Length;
                        int lineEnd = errorMsg.IndexOf("]", lineStart);
                        if (lineEnd > lineStart)
                        {
                            if (int.TryParse(errorMsg.Substring(lineStart, lineEnd - lineStart), out int lineNum))
                            {
                                errorLine = lineNum;
                                // Find which source file this line belongs to
                                foreach (var stok in sourceTokens)
                                {
                                    // LineStart and LineEnd are now 1-based
                                    if (lineNum >= stok.LineStart && lineNum <= stok.LineEnd)
                                    {
                                        errorFile = stok.FilePath;
                                        // Calculate line within original file (1-based)
                                        errorLine = lineNum - stok.LineStart + 1;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    // Try to extract file from error message if it contains a path
                    else if (errorFile == null)
                    {
                        foreach (var stok in sourceTokens)
                        {
                            if (errorMsg.Contains(stok.FilePath) || errorMsg.Contains(Path.GetFileName(stok.FilePath)))
                            {
                                errorFile = stok.FilePath;
                                break;
                            }
                        }
                    }
                    
                    // Display enhanced error information
                                        txtLog.AppendText($"\r\n=== COMPILATION ERROR ===\r\n");
                    if (!string.IsNullOrEmpty(errorFile))
                    {
                        txtLog.AppendText($"File: {errorFile}\r\n");
                        if (errorLine > 0)
                            txtLog.AppendText($"Line: {errorLine}\r\n");
                    }
                    else if (errorLine > 0)
                    {
                        // Try to find file by line number (LineStart/LineEnd are 1-based)
                        foreach (var stok in sourceTokens)
                        {
                            if (errorLine >= stok.LineStart && errorLine <= stok.LineEnd)
                            {
                                errorFile = stok.FilePath;
                                int actualLine = errorLine - stok.LineStart + 1;
                                txtLog.AppendText($"File: {errorFile}\r\n");
                                txtLog.AppendText($"Line: {actualLine}\r\n");
                                errorLine = actualLine;
                                break;
                            }
                        }
                        
                        // If still not found, show debug info
                        if (string.IsNullOrEmpty(errorFile))
                        {
                            txtLog.AppendText($"Could not map line {errorLine} to source file.\r\n");
                            txtLog.AppendText($"Source token ranges:\r\n");
                            foreach (var stok in sourceTokens.Take(5))
                            {
                                txtLog.AppendText($"  {stok.FilePath}: lines {stok.LineStart}-{stok.LineEnd}\r\n");
                            }
                            if (sourceTokens.Count > 5)
                            {
                                txtLog.AppendText($"  ... and {sourceTokens.Count - 5} more files\r\n");
                                var lastToken = sourceTokens.Last();
                                txtLog.AppendText($"  Last: {lastToken.FilePath}: lines {lastToken.LineStart}-{lastToken.LineEnd}\r\n");
                            }
                        }
                    }
                    
                    txtLog.AppendText($"Error: {errorDescription}\r\n");
                                        
                    // Show context around the error if we have file and line
                    if (!string.IsNullOrEmpty(errorFile) && errorLine > 0)
                    {
                                        try
                                        {
                            string fullPath = Path.Combine(scriptLocation, errorFile.Replace("/", "\\"));
                                            if (File.Exists(fullPath))
                                            {
                                                string[] lines = File.ReadAllLines(fullPath);
                                if (errorLine <= lines.Length)
                                {
                                    int contextStart = Math.Max(0, errorLine - 5);
                                    int contextEnd = Math.Min(lines.Length, errorLine + 3);
                                                
                                    txtLog.AppendText($"\r\nContext around line {errorLine}:\r\n");
                                                for (int i = contextStart; i < contextEnd; i++)
                                                {
                                        string marker = (i + 1 == errorLine) ? ">>> " : "    ";
                                                    txtLog.AppendText($"{marker}{i + 1,4}: {lines[i]}\r\n");
                                                }
                                        
                                    // Provide helpful suggestions based on error type
                                        txtLog.AppendText($"\r\n");
                                    if (errorDescription.Contains("Syntax error"))
                                    {
                                        txtLog.AppendText("Common syntax issues:\r\n");
                                        txtLog.AppendText("  - Missing semicolons (;)\r\n");
                                        txtLog.AppendText("  - Unmatched braces { }\r\n");
                                        txtLog.AppendText("  - Invalid operators (GSC doesn't support && or || - use nested if statements)\r\n");
                                        txtLog.AppendText("  - Incomplete if/for/while statements\r\n");
                                    }
                                    if (errorDescription.Contains("defined more than once") || errorDescription.Contains("duplicate"))
                                    {
                                        txtLog.AppendText("Duplicate definition detected:\r\n");
                                        txtLog.AppendText("  - Check for duplicate function/variable names\r\n");
                                        txtLog.AppendText("  - Remove duplicate #include statements\r\n");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            txtLog.AppendText($"\r\nNote: Could not read file for context: {ex.Message}\r\n");
                        }
                    }
                    
                    txtLog.AppendText($"\r\n");
                    
                    // Scroll to bottom to show error
                    txtLog.SelectionStart = txtLog.Text.Length;
                    txtLog.ScrollToCaret();
                    
                    SetCompileState(true, false);
                    return;
                }

                // Save compiled file to build folder
                // Use the directory from the selected output file path, or fallback to default buildFolder
                string outputDirectory = buildFolder;
                string outputFileName = Path.GetFileName(txtOutputFile.Text);
                
                // If txtOutputFile contains a full path, use its directory
                if (!string.IsNullOrWhiteSpace(txtOutputFile.Text) && Path.IsPathRooted(txtOutputFile.Text))
                {
                    string selectedDir = Path.GetDirectoryName(txtOutputFile.Text);
                    if (!string.IsNullOrWhiteSpace(selectedDir))
                    {
                        outputDirectory = selectedDir;
                    }
                }
                
                // Ensure output directory exists
                if (!Directory.Exists(outputDirectory))
                    Directory.CreateDirectory(outputDirectory);

                // Get output filename - use .gscc extension (or .gsc if RequiresGSI)
                if (string.IsNullOrWhiteSpace(outputFileName))
                    outputFileName = "compiled.gscc";
                
                // Remove extension and add appropriate one (.gsc or .gscc)
                string baseName = Path.GetFileNameWithoutExtension(outputFileName);
                string extension = code.RequiresGSI ? ".gsc" : ".gscc";
                string finalFileName = baseName + extension;
                
                string outputPath = Path.Combine(outputDirectory, finalFileName);
                
                // Update progress bar before writing file
                if (progressBar != null)
                {
                    progressBar.Value = 95;
                    progressBar.Invalidate();
                    Application.DoEvents();
                }
                
                File.WriteAllBytes(outputPath, code.CompiledScript);
                
                // Update progress bar to 100% after successful compilation
                if (progressBar != null)
                {
                    progressBar.Value = 100;
                    progressBar.Invalidate();
                    Application.DoEvents();
                }

                // Save hash table to hashes.txt in output directory
                if (code.HashMap != null && code.HashMap.Count > 0)
                {
                    string hashPath = Path.Combine(outputDirectory, "hashes.txt");
                    
                    
                    StringBuilder hashes = new StringBuilder();
                    hashes.AppendLine("# Hash Table Generated by T7 Compiler GUI");
                    hashes.AppendLine($"# Compilation Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    hashes.AppendLine($"# Source: {txtProjectFolder.Text}");
                    hashes.AppendLine($"# Output: {outputPath}");
                    hashes.AppendLine();
                    hashes.AppendLine($"Total Functions: {code.HashMap.Count}");
                    hashes.AppendLine();
                    
                    // Sort by hash value for easier reading
                    var sortedHashes = code.HashMap.OrderBy(kvp => kvp.Key);
                    foreach (var kvp in sortedHashes)
                    {
                        hashes.AppendLine($"0x{kvp.Key:X8}, {kvp.Value}");
                    }
                    
                    File.WriteAllText(hashPath, hashes.ToString());
                }

                // Save opcode map if enabled and available
                if (chkSaveOpcodeMap.Checked)
                {
                    string omapBaseName = Path.GetFileNameWithoutExtension(finalFileName);
                    string omapPath = Path.Combine(outputDirectory, omapBaseName + ".omap");
                    
                    byte[] opsRaw = null;
                    
                    // Prefer OpcodeMap (from masking) over OpcodeEmissions
                    if (code.OpcodeMap != null && code.OpcodeMap.Length > 0)
                    {
                        opsRaw = code.OpcodeMap;
                    }
                    else if (code.OpcodeEmissions != null && code.OpcodeEmissions.Count > 0)
                    {
                        opsRaw = new byte[code.OpcodeEmissions.Count * 4];
                        for (int i = 0; i < code.OpcodeEmissions.Count; i++)
                        {
                            BitConverter.GetBytes(code.OpcodeEmissions[i]).CopyTo(opsRaw, i * 4);
                        }
                    }
                    
                    if (opsRaw != null && opsRaw.Length > 0)
                    {
                        File.WriteAllBytes(omapPath, opsRaw);
                    }
                }

                // Save stub script if present
                if (code.StubbedScript != null && code.StubScriptData != null)
                {
                    string stubBaseName = Path.GetFileNameWithoutExtension(finalFileName);
                    string stubPath = Path.Combine(outputDirectory, stubBaseName + ".stub.gscc");
                    
                    txtLog.AppendText("Saving stub script...\r\n");
                    Application.DoEvents();
                    
                    File.WriteAllBytes(stubPath, code.StubScriptData);
                    txtLog.AppendText($"Stub script saved: {stubPath}\r\n");
                }

                txtLog.AppendText($"\r\n✓ Compilation successful!\r\n");
                txtLog.AppendText($"  Output: {Path.GetFileName(outputPath)}\r\n");
                txtLog.AppendText($"  Size: {code.CompiledScript.Length:N0} bytes\r\n");
                if (code.HashMap != null)
                    txtLog.AppendText($"  Functions: {code.HashMap.Count}\r\n");

                // Auto-populate inject file field with the compiled output
                txtInjectFile.Text = outputPath;
                UpdateUI();
                
                // Switch to inject tab automatically on successful compilation
                if (tabControl != null && tabInject != null)
                {
                    tabControl.SelectedTab = tabInject;
                    
                    // Clear selection and reset button hover state after tab switch completes
                    // Use BeginInvoke to ensure this happens after the tab switch UI update
                    this.BeginInvoke(new Action(() =>
                    {
                        if (txtInjectFile != null)
                        {
                            txtInjectFile.SelectionStart = txtInjectFile.Text.Length;
                            txtInjectFile.SelectionLength = 0;
                        }
                        
                        // Reset hover state of browse button
                        if (btnSelectInjectFile != null)
                        {
                            var isHoveredField = btnSelectInjectFile.GetType().GetField("isHovered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            var isPressedField = btnSelectInjectFile.GetType().GetField("isPressed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            
                            isHoveredField?.SetValue(btnSelectInjectFile, false);
                            isPressedField?.SetValue(btnSelectInjectFile, false);
                            
                            btnSelectInjectFile.Invalidate();
                            btnSelectInjectFile.Update();
                            btnSelectInjectFile.Refresh();
                        }
                    }));
                }

                // Save last project folder (saved via SaveAllSettings)
                SaveAllSettings();
                
                // Success message already logged to txtLog, no need for MessageBox
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"\r\n=== UNEXPECTED ERROR ===\r\n");
                txtLog.AppendText($"Error: {ex.Message}\r\n");
                if (!string.IsNullOrEmpty(ex.StackTrace))
                    txtLog.AppendText($"\r\nStack Trace:\r\n{ex.StackTrace}\r\n");
                txtLog.AppendText($"\r\n");
                
                // Scroll to bottom to show error
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
            }
            finally
            {
                SetCompileState(true, false);
                UpdateUI();
            }
        }

        private void txtProjectFolder_TextChanged(object sender, EventArgs e)
        {
            // Auto-populate output file if project folder is set and output file is empty or using default
            if (!string.IsNullOrWhiteSpace(txtProjectFolder.Text) && 
                (string.IsNullOrWhiteSpace(txtOutputFile.Text) || txtOutputFile.Text == Path.Combine(buildFolder, "compiled.gscc")))
            {
                AutoPopulateOutputFile();
            }
            UpdateUI();
            // Auto-scan for GSC files and populate combobox when project folder changes
            LoadGscConfMode();
        }

        private void txtOutputFile_TextChanged(object sender, EventArgs e)
        {
            UpdateUI();
        }
        
        private void txtDefaultOutputPath_TextChanged(object sender, EventArgs e)
        {
            // Auto-populate output file if project folder is set and output file is empty or using default
            if (!string.IsNullOrWhiteSpace(txtProjectFolder.Text) && 
                (string.IsNullOrWhiteSpace(txtOutputFile.Text) || txtOutputFile.Text == Path.Combine(buildFolder, "compiled.gscc")))
            {
                AutoPopulateOutputFile();
            }
        }

        private void btnSelectInjectFile_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "GSC Compiled Files (*.gscc)|*.gscc|GSC Injection Files (*.gsc)|*.gsc|All Files (*.*)|*.*";
                dialog.Title = "Select Compiled GSC File to Inject";
                if (!string.IsNullOrWhiteSpace(txtInjectFile.Text) && File.Exists(txtInjectFile.Text))
                    dialog.InitialDirectory = Path.GetDirectoryName(txtInjectFile.Text);
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtInjectFile.Text = dialog.FileName;
                    UpdateUI();
                }
            }
            // Reset button state after dialog closes
            ResetButtonState(sender);
        }

        private void btnInject_Click(object sender, EventArgs e)
        {
            // Reset button state immediately
            ResetButtonState(sender);
            
            // Determine which game we're targeting
            Games game = Games.T7;
            if (currentInjectGameIndex == 1)
                game = Games.T8;
            
            // Check if game is running FIRST - this is required for injection
            if (!IsGameRunning(game))
            {
                txtLog.AppendText("\r\n=== INJECTION ERROR ===\r\n");
                txtLog.AppendText($"Game ({game}) is not running.\r\n");
                txtLog.AppendText("Please start the game before attempting to inject.\r\n\r\n");
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
                SetInjectState(true, false);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(txtInjectFile.Text) || !File.Exists(txtInjectFile.Text))
            {
                txtLog.AppendText("\r\n=== INJECTION ERROR ===\r\n");
                txtLog.AppendText("Please select a valid compiled file to inject.\r\n\r\n");
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtInjectPath.Text))
            {
                txtLog.AppendText("\r\n=== INJECTION ERROR ===\r\n");
                txtLog.AppendText("Please specify a replace path.\r\n\r\n");
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
                return;
            }

            SetInjectState(false, true); // Disable button and show progress
            txtLog.AppendText("\r\n=== INJECTION ===\r\n");
            txtLog.AppendText($"File: {txtInjectFile.Text}\r\n");
            txtLog.AppendText($"Replace Path: {txtInjectPath.Text}\r\n");
            txtLog.AppendText($"Game: {game}\r\n");
            txtLog.AppendText($"No Runtime: {chkNoRuntime.Checked}\r\n\r\n");
            
            // Update progress bar
            if (progressBar != null)
            {
                progressBar.Value = 10;
                progressBar.Invalidate();
            }
            
            Application.DoEvents();

            try
            {
                // Update progress
                if (progressBar != null)
                {
                    progressBar.Value = 30;
                    progressBar.Invalidate();
                    Application.DoEvents();
                }
                
                byte[] buffer = File.ReadAllBytes(txtInjectFile.Text);
                
                // Validate file is a compiled GSC script
                if (buffer.Length < 16)
                {
                    txtLog.AppendText("ERROR: File is too small to be a valid compiled script.\r\n\r\n");
                    SetInjectState(true, false);
                    txtLog.SelectionStart = txtLog.Text.Length;
                    txtLog.ScrollToCaret();
                    return;
                }

                // Check for GSC header or GSIC preamble
                long header = BitConverter.ToInt64(buffer, 0);
                
                if (header != 0x1C000A0D43534780 && header != 0x36000A0D43534780) // T7 and T8 headers
                {
                    string preamble = Encoding.ASCII.GetString(buffer.Take(4).ToArray());
                    if (preamble == "GSIC")
                    {
                        txtLog.AppendText("Detected GSIC file (with detours)\r\n");
                        // GSIC files can be injected but may require special handling
                    }
                    else
                    {
                        txtLog.AppendText($"\r\n=== INJECTION ERROR ===\r\n");
                        txtLog.AppendText("File is not a valid compiled GSC script.\r\n");
                        txtLog.AppendText("Expected GSC header or GSIC preamble.\r\n\r\n");
                        SetInjectState(true, false);
                        txtLog.SelectionStart = txtLog.Text.Length;
                        txtLog.ScrollToCaret();
                        return;
                    }
                }

                // Update progress
                if (progressBar != null)
                {
                    progressBar.Value = 60;
                    progressBar.Invalidate();
                    Application.DoEvents();
                }
                
                // Implement injection
                int result = InjectScript(txtInjectPath.Text, buffer, game, chkNoRuntime.Checked);
                
                // Update progress
                if (progressBar != null)
                {
                    progressBar.Value = 90;
                    progressBar.Invalidate();
                    Application.DoEvents();
                }
                
                if (result == 0)
                {
                    LastGameInjected = game; // Track which game was injected
                    // Save injection state for persistence (after all variables are set)
                    SaveInjectionState(txtInjectPath.Text, game);
                    txtLog.AppendText($"\r\n=== INJECTION SUCCESS ===\r\n");
                    txtLog.AppendText($"Script injected successfully!\r\n");
                    txtLog.AppendText($"Injection state saved. You can reset the parse tree even after closing the app.\r\n\r\n");
                    
                    // Re-enable reset button after successful injection (may need to reset again)
                    isParseTreeReset = false;
                    UpdateResetParseTreeButton();
                }
                else
                {
                    txtLog.AppendText($"\r\n=== INJECTION FAILED ===\r\n");
                    txtLog.AppendText($"Failed to inject script. Error code: 0x{result:X}\r\n\r\n");
                }
                
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"\r\n=== INJECTION ERROR ===\r\n");
                txtLog.AppendText($"Error: {ex.Message}\r\n");
                if (!string.IsNullOrEmpty(ex.StackTrace))
                    txtLog.AppendText($"\r\nStack Trace:\r\n{ex.StackTrace}\r\n");
                txtLog.AppendText($"\r\n");
            }
            finally
            {
                SetInjectState(true, false);
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
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
        private bool IsGameRunning(Games game)
        {
            // Use static arrays to avoid allocations on every call
            string[] processNames = game == Games.T7 ? T7ProcessNames : T8ProcessNames;
            
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
            catch (System.ComponentModel.Win32Exception)
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
        private ProcessEx GetGameProcessEx(Games game)
        {
            try
            {
                // Use static arrays - check most common name first
                string[] processNames = game == Games.T7 ? T7ProcessNames : T8ProcessNames;
                
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
        
        // btnCompile_Click is defined earlier in the file (around line 2871)
        
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

        private int InjectScript(string replacePath, byte[] buffer, Games game, bool noruntime)
        {
            if (game != Games.T7)
            {
                txtLog.AppendText("T8 injection not yet implemented in GUI.\r\n");
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
                        txtLog.AppendText("ERROR: Script is not a valid compiled script.\r\n");
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
                        txtLog.AppendText("ERROR: Script is not a valid compiled script.\r\n");
                        return -1;
                    }
                }

                // Find game process
                ProcessEx bo3 = GetGameProcessEx(Games.T7);
                if (bo3 == null)
                {
                    txtLog.AppendText("ERROR: No game process found for Black Ops III.\r\n");
                    txtLog.AppendText("Make sure the game is running.\r\n");
                    return -1;
                }

                bool IsWindowsStore = !(bo3["GameChat2.dll"] is null);
                bo3.OpenHandle();
                bo3.SetDefaultCallType(ExCallThreadType.XCTT_QUAPC);
                OriginalPID = bo3.BaseProcess.Id;

                PointerEx off = IsWindowsStore ? 0xF3B1330 : 0x9407AB0;
                txtLog.AppendText($"s_assetPool:ScriptParseTree => {bo3["blackops3.exe"][off]}\r\n");
                
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
                        if (name.ToLower().Trim().Replace("\\", "/") == replacePath.ToLower().Trim().Replace("\\", "/"))
                        {
                            found = true;
                            
                            // Cache target info
                            llpModifiedSPTStruct = (ulong)(i * Marshal.SizeOf(typeof(T7SPT))) + sptGlob;
                            llpOriginalBuffer = entry.lpBuffer;
                            OriginalSourceChecksum = bo3.GetValue<int>(llpOriginalBuffer + 0x8);

                            // Patch script into memory
                            entry.lpBuffer = bo3.QuickAlloc(buffer.Length);
                            BitConverter.GetBytes(OriginalSourceChecksum).CopyTo(buffer, 0x8);
                            bo3.SetBytes(entry.lpBuffer, buffer);

                            // Patch spt struct
                            bo3.SetStruct(llpModifiedSPTStruct, entry);

                            // Cache the struct data for uninjection
                            InjectedScript = entry;
                            InjectedBuffSize = buffer.Length;
                            
                            // Reset the parse tree reset flag since we just injected
                            isParseTreeReset = false;

                            if (!noruntime)
                            {
                                try
                                {
                                    string exeFilePath = Assembly.GetExecutingAssembly().Location;
                                    var result = bo3.Call<long>(bo3.GetProcAddress(@"kernel32.dll", @"LoadLibraryA"), Path.Combine(Path.GetDirectoryName(exeFilePath), "t7cinternal.dll"));
                                    txtLog.AppendText($"LoadLibrary Result => {result:X}\r\n");

                                    bo3.Refresh();
                                    if (result <= 0)
                                    {
                                        return (int)result;
                                    }

                                    bo3.Call(bo3.GetProcAddress(@"t7cinternal.dll", @"RemoveDetours"));
                                    if (gsi != null && gsi.Detours.Count > 0)
                                    {
                                        bo3.Call(bo3.GetProcAddress(@"t7cinternal.dll", @"RegisterDetours"), gsi.PackDetours(), gsi.Detours.Count, (long)entry.lpBuffer);
                                        txtLog.AppendText($"Registered {gsi.Detours.Count} detours.\r\n");
                                    }
                                }
                                catch (Exception e)
                                {
                                    txtLog.AppendText($"ERROR loading runtime: {e.Message}\r\n");
                                    return 3;
                                }
                            }

                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        txtLog.AppendText($"ERROR processing entry: {e.Message}\r\n");
                        continue;
                    }
                }

                if (!found)
                {
                    txtLog.AppendText($"ERROR: Script path '{replacePath}' not found in game's script table.\r\n");
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

                return 0;
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"ERROR during injection: {ex.Message}\r\n");
                if (ex.InnerException != null)
                    txtLog.AppendText($"Inner: {ex.InnerException.Message}\r\n");
                return -1;
            }
        }

        private void btnResetParseTree_Click(object sender, EventArgs e)
        {
            // Reset button state immediately
            ResetButtonState(sender);
            
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
                txtLog.AppendText("\r\n=== RESETTING SCRIPT PARSE TREE ===\r\n");
                Application.DoEvents();

                bool success = FreeActiveScript();

                if (success)
                {
                    txtLog.AppendText("Script parse tree has been reset.\r\n\r\n");
                    isParseTreeReset = true;
                    
                    // Update button state (will disable it)
                    UpdateResetParseTreeButton();
                }
                else
                {
                    txtLog.AppendText("Reset failed - see errors above.\r\n\r\n");
                }

                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"\r\n=== ERROR ===\r\n");
                txtLog.AppendText($"Error resetting parse tree: {ex.Message}\r\n\r\n");
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
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
            // Reset button state immediately
            ResetButtonState(sender);
            
            try
            {
                // BO3 Steam App ID is 311210
                System.Diagnostics.Process.Start("steam://rungameid/311210");
                txtLog.AppendText("Launching Black Ops 3 via Steam...\r\n");
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"Failed to launch BO3: {ex.Message}\r\n");
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, $"Failed to launch Black Ops 3:\n{ex.Message}\n\nMake sure Steam is installed.", 
                    "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Explicitly reset button state after operation completes
                if (sender is Control btn)
                {
                    btn.Invalidate();
                    btn.Update();
                    btn.Refresh();
                }
            }
        }

        private bool FreeActiveScript()
        {
            try
            {
                switch (LastGameInjected)
                {
                    case Games.T7:
                        return FreeT7Script();
                    case Games.T8:
                        // T8 reset not implemented yet
                        txtLog.AppendText("T8 script reset not yet implemented.\r\n");
                        return false;
                    default:
                        txtLog.AppendText("No game injection state found.\r\n");
                        return false;
                }
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"Error in FreeActiveScript: {ex.Message}\r\n");
                throw;
            }
        }

        private bool FreeT7Script()
        {
            ProcessEx bo3 = GetGameProcessEx(Games.T7);
            if (bo3 == null)
            {
                txtLog.AppendText("ERROR: No game process found for Black Ops III.\r\n");
                // Game is not running - clear injection state since there's nothing to reset
                if (OriginalPID != 0 || llpModifiedSPTStruct != 0)
                {
                    txtLog.AppendText("Game is not running. Injection state has been automatically cleared.\r\n");
                    ClearInjectionState();
                }
                return false;
            }

            // If we don't have injection state loaded, try to load it
            if (llpModifiedSPTStruct == 0 && OriginalPID == 0)
            {
                txtLog.AppendText("No in-memory injection state found. Attempting to load saved injection state...\r\n");
                LoadInjectionState();
                
                // Check again after loading
                if (llpModifiedSPTStruct == 0 && OriginalPID == 0)
                {
                    txtLog.AppendText("No saved injection state found or state file is invalid.\r\n");
                    return false;
                }
            }

            // Verify process ID matches
            if (OriginalPID != 0 && bo3.BaseProcess.Id != OriginalPID)
            {
                // Process ID changed - this means the old game closed and a new instance started
                // There's nothing to reset since the old process is gone
                txtLog.AppendText($"Game process ID changed (was {OriginalPID}, now {bo3.BaseProcess.Id}).\r\n");
                txtLog.AppendText("The previous game instance has closed. Injection state has been automatically cleared.\r\n");
                ClearInjectionState();
                return false;
            }
            else if (OriginalPID == 0)
            {
                OriginalPID = bo3.BaseProcess.Id; // Set current PID
            }

            if (llpModifiedSPTStruct == 0) 
            {
                txtLog.AppendText("No script injection state found. Cannot reset.\r\n");
                txtLog.AppendText("If you injected a script, make sure the game is still running.\r\n");
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
                        txtLog.AppendText("Detours removed.\r\n");
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
                txtLog.AppendText($"ERROR during reset: {ex.Message}\r\n");
                return false; // Failed
            }
        }

        private void SaveInjectionState(string _replacePath, Games _game)
        {
            try
            {
                // Verify we have valid state to save
                if (llpModifiedSPTStruct == 0 || OriginalPID == 0)
                {
                    txtLog.AppendText($"WARNING: Cannot save injection state - missing required data (SPT: {llpModifiedSPTStruct:X}, PID: {OriginalPID})\r\n");
                    return;
                }

                // Save to unified config file (no config folder needed)
                SaveAllSettings();
                txtLog.AppendText($"Injection state saved to unified config file.\r\n");
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"ERROR saving injection state: {ex.Message}\r\n");
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
                            
                            // Switch to inject tab if we're not already on it
                            if (tabControl.SelectedTab != tabInject)
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
                poisonToolTip.Theme = poisonStyleManager.Theme;
                poisonToolTip.Style = poisonStyleManager.Style;
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
            Color backColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Form(displayTheme);
            // Use rainbow color if active, otherwise use style color for border
            Color borderColor = IsRainbowStyleActive() ? currentRainbowColor : ReaLTaiizor.Drawing.Poison.PoisonPaint.GetStyleColor(poisonStyleManager.Style);
            Color foreColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.ForeColor.Label.Normal(displayTheme);
            
            // For better contrast, slightly adjust background if needed
            if (displayTheme == ThemeStyle.Dark)
            {
                // Slightly lighter background for dark theme tooltips
                backColor = Color.FromArgb(Math.Min(255, backColor.R + 10), Math.Min(255, backColor.G + 10), Math.Min(255, backColor.B + 10));
            }
            else
            {
                // Slightly darker background for light theme tooltips
                backColor = Color.FromArgb(Math.Max(0, backColor.R - 10), Math.Max(0, backColor.G - 10), Math.Max(0, backColor.B - 10));
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
            // Reset button state immediately
            ResetButtonState(sender);
            
            txtLog?.Clear();
        }
        
        private void btnLogCopy_Click(object sender, EventArgs e)
        {
            // Reset button state immediately
            ResetButtonState(sender);
            
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
                    // Reset button state before processing
                    ResetButtonState(sender);
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
            
            // Reset button state after dialog closes
            ResetButtonState(sender);
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
                txtLog.SelectionBackColor = Color.Yellow;
                txtLog.SelectionColor = Color.Black;
                
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
            txtLog.AppendText(message);
            int end = txtLog.Text.Length;
            
            // Apply color based on level
            txtLog.Select(start, end - start);
            switch (level)
            {
                case LogLevel.Error:
                    txtLog.SelectionColor = Color.FromArgb(255, 100, 100); // Red
                    break;
                case LogLevel.Warning:
                    txtLog.SelectionColor = Color.FromArgb(255, 200, 100); // Orange/Yellow
                    break;
                case LogLevel.Success:
                    txtLog.SelectionColor = Color.FromArgb(100, 255, 100); // Green
                    break;
                default:
                    txtLog.SelectionColor = txtLog.ForeColor; // Default
                    break;
            }
            txtLog.DeselectAll();
            
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }
        
        #endregion
        
        #region Settings Tab
        
        // Settings are now handled by SettingsPanel
        
        internal bool IsRainbowStyleActive()
        {
            // Check if rainbow timer is running
            return rainbowColorTimer != null && rainbowColorTimer.Enabled;
        }
        
        private System.Windows.Forms.Timer rainbowColorTimer;
        private float rainbowHue = 0f;
        internal Color currentRainbowColor = Color.Red; // Made internal so StyleBasedMenuRenderer can access it
        
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
            
            // Start rainbow animation
            if (rainbowColorTimer == null)
            {
                rainbowColorTimer = new System.Windows.Forms.Timer
                {
                    Interval = 16 // ~60 FPS for smooth animation
                };
                rainbowColorTimer.Tick += RainbowColorTimer_Tick;
            }
            
            // Initialize rainbow color - start from red (hue 0) to match previous theme if it was red
            // But ensure it cycles through all colors smoothly
            currentRainbowColor = HslToRgb(0f, 1.0f, 0.5f);
            rainbowHue = 0f;
            
            // Ensure the style manager doesn't interfere - we want rainbow to completely override
            // Keep the Style property set so controls know to use style colors, but rainbow will override in paint events
            
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
            
            // Apply rainbow to this control if it's a Poison control
            if (parent is IPoisonControl poisonCtrl)
            {
                // Ensure StyleManager is connected (important for proper updates)
                if (poisonStyleManager != null)
                {
                    poisonCtrl.StyleManager = poisonStyleManager;
                }
                
                // For controls that support Style property, we need to force them to use rainbow
                // We'll do this by subscribing to their paint events
                // NOTE: We keep Style=Default so StyleManager can still manage the control
                // The paint handlers will override the visual appearance when rainbow is active
                if (poisonCtrl is ReaLTaiizor.Controls.PoisonLabel poisonLabel)
                {
                    // Labels use UseStyleColors - ensure it's enabled for rainbow
                    poisonLabel.UseStyleColors = true;
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
                                backColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Form(poisonLabel.Theme);
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
                    poisonToggle.UseStyleColors = true;
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
                                Color backColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Form(poisonToggle.Theme);
                                
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
                    poisonPanel.UseStyleColors = true;
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
                    poisonTextBox.UseStyleColors = true;
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
                    poisonButton.UseStyleColors = true;
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
                    poisonDropDown.UseStyleColors = true;
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
                                Color backColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Button.Normal(poisonDropDown.Theme);
                                if (!poisonDropDown.Enabled)
                                {
                                    backColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Button.Disabled(poisonDropDown.Theme);
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
            }
            
            // Recursively apply to all child controls
            foreach (Control child in parent.Controls)
            {
                ApplyRainbowToAllControls(child);
            }
        }
        
        private void TabControl_CustomPaintForeground(object sender, ReaLTaiizor.Drawing.Poison.PoisonPaintEventArgs e)
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
        
        private void RainbowColorTimer_Tick(object sender, EventArgs e)
        {
            if (poisonStyleManager == null || !IsRainbowStyleActive()) return;
            
            try
            {
                // Cycle through hue values (0-360) smoothly
                rainbowHue = (rainbowHue + 1.5f) % 360f;
                
                // Convert HSL to RGB (full saturation, medium lightness for vibrant colors)
                currentRainbowColor = HslToRgb(rainbowHue, 1.0f, 0.5f);
                
                // Force all controls to repaint with the new rainbow color
                // Use BeginInvoke to update on UI thread
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        UpdateRainbowUI();
                    }));
                }
                else
                {
                    UpdateRainbowUI();
                }
            }
            catch (Exception ex)
            {
                // Silently handle errors to prevent timer crashes
                System.Diagnostics.Debug.WriteLine($"Rainbow timer error: {ex.Message}");
            }
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
                gscConfModeMenu,
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
                // Ensure StyleManager is connected
                if (poisonStyleManager != null)
                {
                    poisonCtrl.StyleManager = poisonStyleManager;
                }
                
                // Ensure all controls use Style = Default to follow StyleManager
                var styleProp = parent.GetType().GetProperty("Style");
                if (styleProp != null && styleProp.CanWrite)
                {
                    styleProp.SetValue(parent, ColorStyle.Default);
                }
                
                // Ensure UseStyleColors is enabled for controls that support it
                var useStyleColorsProp = parent.GetType().GetProperty("UseStyleColors");
                if (useStyleColorsProp != null && useStyleColorsProp.CanWrite)
                {
                    useStyleColorsProp.SetValue(parent, true);
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
                        // Send WM_PAINT message to force repaint (0x000F = WM_PAINT)
                        SendMessage(handle, 0x000F, 0, 0);
                    }
                }
                
                // Also try to find the actual tooltip window by class name "tooltips_class32"
                IntPtr tooltipWindow = FindWindow("tooltips_class32", null);
                if (tooltipWindow != IntPtr.Zero)
                {
                    SendMessage(tooltipWindow, 0x000F, 0, 0); // WM_PAINT
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
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        
        [DllImport("user32.dll")]
        private static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);
        
        [DllImport("user32.dll")]
        private static extern bool UpdateWindow(IntPtr hWnd);
        
        private void InvalidateAllControls(Control parent)
        {
            if (parent == null) return;
            
            // Invalidate this control
            parent.Invalidate();
            
            // For Poison controls, also ensure StyleManager is connected
            if (parent is IPoisonControl poisonCtrl && poisonStyleManager != null)
            {
                poisonCtrl.StyleManager = poisonStyleManager;
            }
            
            // For Poison components (like menus), ensure StyleManager is connected
            if (parent is IPoisonComponent poisonComponent && poisonStyleManager != null)
            {
                poisonComponent.StyleManager = poisonStyleManager;
            }
            
            // Recursively invalidate all child controls
            foreach (Control child in parent.Controls)
            {
                InvalidateAllControls(child);
            }
        }
        
        private void ChangeTheme(ThemeStyle theme)
        {
            if (poisonStyleManager == null) return;
            
            // Match demo approach: Just set StyleManager.Theme and let it propagate automatically
            // All controls connected to StyleManager will update instantly
            poisonStyleManager.Theme = theme;
            
            // Set form background color based on theme
            this.BackColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Form(theme);
            
            // Update PoisonStyleExtender theme (needed for non-Poison controls like RichTextBox)
                if (poisonStyleExtender != null)
                {
                poisonStyleExtender.Theme = theme;
                    poisonStyleExtender.StyleManager = poisonStyleManager;
                }
                
            // Update RichTextBox colors based on theme
            if (txtLog != null)
            {
                bool isDark = theme == ThemeStyle.Dark;
                if (isDark)
                {
                    txtLog.BackColor = Color.FromArgb(30, 30, 30);
                    txtLog.ForeColor = Color.FromArgb(224, 224, 224);
                }
                else
                {
                    txtLog.BackColor = Color.FromArgb(255, 255, 255);
                    txtLog.ForeColor = Color.FromArgb(30, 30, 30);
                }
            }
            
            // Update tooltip theme
                if (poisonToolTip != null)
                {
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
            
            // Save settings (only if not loading)
            if (!isLoadingSettings)
            {
                SaveSettings();
            }
        }
        
        // RefreshAllControls() removed - not needed with StyleManager pattern
        // StyleManager.Update() handles all control refreshes automatically
        
        private void ChangeColorStyle(ColorStyle style)
        {
            if (poisonStyleManager == null) return;
            
            // Stop rainbow animation if active and remove tab handler
            bool wasRainbowActive = rainbowColorTimer != null && rainbowColorTimer.Enabled;
            if (wasRainbowActive)
            {
                rainbowColorTimer.Stop();
                rainbowColorTimer.Dispose();
                rainbowColorTimer = null;
                
                // Remove tab control rainbow handler when rainbow is disabled
                if (tabControl != null)
                {
                    tabControl.CustomPaintForeground -= TabControl_CustomPaintForeground;
                }
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
                
            // Explicitly update and invalidate theme toggle and color style button
            if (toggleSettingsTheme != null)
            {
                toggleSettingsTheme.StyleManager = poisonStyleManager;
                var styleProp = toggleSettingsTheme.GetType().GetProperty("Style");
                if (styleProp != null && styleProp.CanWrite)
                {
                    styleProp.SetValue(toggleSettingsTheme, style);
                }
                var useStyleColorsProp = toggleSettingsTheme.GetType().GetProperty("UseStyleColors");
                if (useStyleColorsProp != null && useStyleColorsProp.CanWrite)
                {
                    useStyleColorsProp.SetValue(toggleSettingsTheme, true);
                }
                toggleSettingsTheme.Invalidate();
            }
            
            if (btnSettingsColorStyle != null)
            {
                btnSettingsColorStyle.StyleManager = poisonStyleManager;
                var styleProp = btnSettingsColorStyle.GetType().GetProperty("Style");
                if (styleProp != null && styleProp.CanWrite)
                {
                    styleProp.SetValue(btnSettingsColorStyle, style);
                }
                var useStyleColorsProp = btnSettingsColorStyle.GetType().GetProperty("UseStyleColors");
                if (useStyleColorsProp != null && useStyleColorsProp.CanWrite)
                {
                    useStyleColorsProp.SetValue(btnSettingsColorStyle, true);
                }
                btnSettingsColorStyle.Invalidate();
            }
            
            // Force all controls to refresh with new style
            InvalidateAllControls(this);
            
            // Save settings (only if not loading)
            if (!isLoadingSettings)
            {
                SaveSettings();
            }
        }
        
        private void ResetControlsToStyleManager(Control parent, ColorStyle style)
        {
            if (parent == null || poisonStyleManager == null) return;
            
            // Reset Poison controls to follow StyleManager
            if (parent is IPoisonControl poisonCtrl)
            {
                // Ensure StyleManager is connected
                poisonCtrl.StyleManager = poisonStyleManager;
                
                // Ensure UseStyleColors is enabled so controls follow StyleManager
                var useStyleColorsProp = parent.GetType().GetProperty("UseStyleColors");
                if (useStyleColorsProp != null && useStyleColorsProp.CanWrite)
                {
                    useStyleColorsProp.SetValue(parent, true);
                }
                
                // Set Style to match StyleManager (not Default, but the actual style)
                var styleProp = parent.GetType().GetProperty("Style");
                if (styleProp != null && styleProp.CanWrite)
                {
                    styleProp.SetValue(parent, style);
                }
            }
            
            // Recursively process child controls
            foreach (Control child in parent.Controls)
            {
                ResetControlsToStyleManager(child, style);
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
                    if (!string.IsNullOrWhiteSpace(txtProjectFolder.Text) && Directory.Exists(txtProjectFolder.Text))
                    {
                        txtOutputFile.Text = Path.Combine(txtProjectFolder.Text, defaultPath);
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
        
        #region About Tab
        
        private void UpdateAboutTab()
        {
            if (lblVersion != null)
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                lblVersion.Text = $"T7 GSC Compiler v{version.Major}.{version.Minor}.{version.Build}";
            }
            
            if (lblAbout != null)
            {
                lblAbout.Text = "A powerful GSC compiler for Call of Duty: Black Ops 3/4\n\n" +
                               "Features:\n" +
                               "• Compile GSC scripts for PC and PS4\n" +
                               "• Support for T7 (BO3) and T8 (BO4)\n" +
                               "• Multiplayer and Zombies modes\n" +
                               "• Opcode masking and mapping\n" +
                               "• Script injection capabilities\n\n" +
                               "Created with ReaLTaiizor UI Framework";
            }
        }
        
        #endregion
        
        #region Keyboard Shortcuts
        
        private void SetupKeyboardShortcuts()
        {
            // Initialize with default keybinds if not loaded from settings
            if (keybinds == null || keybinds.Count == 0)
            {
                keybinds = KeybindDialog.GetDefaultKeybinds();
                // Save defaults if auto-save is enabled
                if (autoSaveSettings)
                {
                    SaveAllSettings();
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
            // Reset button state immediately
            ResetButtonState(sender);
            
            // Menu will show automatically via SplitMenuStrip
        }
        
        private void txtSettingsDefaultOutputPath_TextChanged(object sender, EventArgs e)
        {
            if (txtSettingsDefaultOutputPath != null)
            {
                defaultOutputPath = txtSettingsDefaultOutputPath.Text;
                SaveAllSettings();
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
            
            string folder = ShowModernFolderDialog("Select default output folder", initialPath);
            if (!string.IsNullOrEmpty(folder) && txtSettingsDefaultOutputPath != null)
            {
                txtSettingsDefaultOutputPath.Text = folder;
            }
            
            // Reset button state after dialog closes
            ResetButtonState(sender);
        }
        
        // Auto save and restore window state are now always enabled - toggles removed
        
        private void btnSettingsOpenConfigFolder_Click(object sender, EventArgs e)
        {
            // Reset button state immediately
            ResetButtonState(sender);
            
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
                        // Note: Config path is read-only (in Application.StartupPath)
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
            // Reset button state immediately
            ResetButtonState(sender);
            
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
            else if (autoSaveSettings)
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
                    keybinds = KeybindDialog.GetDefaultKeybinds();
                    return;
                }
                
                XDocument config = XDocument.Load(configPath);
                XElement keybindsElem = config.Root?.Element("Keybinds");
                if (keybindsElem == null)
                {
                    // No keybinds in config - use defaults
                    keybinds = KeybindDialog.GetDefaultKeybinds();
                    return;
                }
                
                keybinds = new Dictionary<string, KeybindDialog.KeybindInfo>();
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
                            keybinds[action] = new KeybindDialog.KeybindInfo(action, key, ctrl, shift, alt);
                            hasValidKeybinds = true;
                        }
                    }
                }
                
                // If no valid keybinds were loaded, use defaults
                if (!hasValidKeybinds || keybinds.Count == 0)
                {
                    keybinds = KeybindDialog.GetDefaultKeybinds();
                }
            }
            catch
            {
                // If loading fails, use defaults
                keybinds = KeybindDialog.GetDefaultKeybinds();
            }
        }
        
        #endregion
        
        #region Recent Projects
        
        private List<string> recentProjects = new List<string>();
        
        private void AddToRecentProjects(string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
                return;
            
            // Remove if already exists
            recentProjects.Remove(projectPath);
            
            // Add to front
            recentProjects.Insert(0, projectPath);
            
            // Limit to MAX_RECENT_PROJECTS
            if (recentProjects.Count > MAX_RECENT_PROJECTS)
                recentProjects.RemoveAt(recentProjects.Count - 1);
            
            SaveRecentProjects();
        }
        
        private void SaveRecentProjects()
        {
            // Recent projects are now saved as part of SaveAllSettings()
            SaveAllSettings();
        }
        
        
        #endregion
        
        #region Unified Config System
        
        private const string CONFIG_FILE_NAME = "T7CompilerGUI.config";
        
        private string GetConfigPath()
        {
            return Path.Combine(Application.StartupPath, CONFIG_FILE_NAME);
        }
        
        private void SaveAllSettings()
        {
            try
            {
                // Don't save during initial load
                if (isLoadingSettings) return;
                
                if (!autoSaveSettings) return;
                
                string configPath = GetConfigPath();
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
                            new XElement("LastProjectFolder", txtProjectFolder != null ? txtProjectFolder.Text : "")
                        ),
                        new XComment("=== Selection State ==="),
                        new XComment("PlatformIndex: 0 = PC, 1 = PS4"),
                        new XComment("GameIndex: 0 = T7 (BO3), 1 = T8 (BO4)"),
                        new XComment("GscConfModeIndex: 0 = MP, 1 = ZM, 2 = Both"),
                        new XComment("InjectGameIndex: 0 = T7 (BO3), 1 = T8 (BO4)"),
                        new XElement("Selections",
                            new XElement("PlatformIndex", currentPlatformIndex.ToString()),
                            new XElement("GameIndex", currentGameIndex.ToString()),
                            new XElement("GscConfModeIndex", currentGscConfModeIndex.ToString()),
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
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                if (txtLog != null)
                {
                    AppendLog($"Error saving config: {ex.Message}\r\n", LogLevel.Warning);
                }
            }
        }
        
        private void LoadAllSettings()
        {
            isLoadingSettings = true;
            try
            {
                string configPath = GetConfigPath();
                if (!File.Exists(configPath))
                {
                    // Try to migrate from old files
                    MigrateOldConfigFiles();
                    if (!File.Exists(configPath))
                    {
                        isLoadingSettings = false;
                        return;
                    }
                }
                
                XDocument config = XDocument.Load(configPath);
                XElement root = config.Element("T7CompilerConfig");
                if (root == null) return;
                
                // Load Application Settings
                XElement appSettings = root.Element("Application");
                if (appSettings != null)
                {
                    // Theme
                    XElement themeElem = appSettings.Element("Theme");
                    if (themeElem != null && Enum.TryParse<ThemeStyle>(themeElem.Value, out ThemeStyle theme) && poisonStyleManager != null)
                    {
                        poisonStyleManager.Theme = theme;
                        this.Theme = theme;
                        if (poisonStyleExtender != null)
                        {
                            poisonStyleExtender.Theme = theme;
                            poisonStyleExtender.StyleManager = poisonStyleManager;
                        }
                        // StyleManager.Update() handles all control updates automatically (matching demo pattern)
                        poisonStyleManager.Update();
                        if (toggleSettingsTheme != null)
                        {
                            toggleSettingsTheme.Checked = (theme == ThemeStyle.Light);
                            toggleSettingsTheme.Text = toggleSettingsTheme.Checked ? "Light" : "Dark";
                        }
                    }
                    
                    // Color Style
                    XElement styleElem = appSettings.Element("ColorStyle");
                    if (styleElem != null && poisonStyleManager != null)
                    {
                        if (styleElem.Value == "Rainbow")
                        {
                            ChangeColorStyleToRainbow();
                        }
                        else if (Enum.TryParse<ColorStyle>(styleElem.Value, out ColorStyle style))
                        {
                            poisonStyleManager.Style = style;
                            this.Style = style;
                            if (poisonStyleExtender != null)
                            {
                                poisonStyleExtender.StyleManager = poisonStyleManager;
                            }
                            // StyleManager.Update() handles all control updates automatically (matching demo pattern)
                            poisonStyleManager.Update();
                            UpdateSettingsColorStyleButton();
                        }
                        
                        // Update all menu renderers with the loaded style
                        UpdateAllMenuRenderers();
                    }
                    else
                    {
                        // Even if no style was loaded from config, ensure renderers are set up with current style
                        // This handles the case where config doesn't have a style element yet
                        UpdateAllMenuRenderers();
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
                        // Auto save is always enabled
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
                        // Auto-populate output file after project folder is loaded
                        AutoPopulateOutputFile();
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
                    
                    XElement gscConfIdx = selections.Element("GscConfModeIndex");
                    if (gscConfIdx != null && int.TryParse(gscConfIdx.Value, out int gcIdx) && gcIdx >= 0 && gcIdx <= 2)
                    {
                        currentGscConfModeIndex = gcIdx;
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
                                
                                XElement tabIdx = windowState.Element("SelectedTab");
                                if (tabIdx != null && int.TryParse(tabIdx.Value, out int tabIndex) && tabControl != null && tabIndex >= 0 && tabIndex < tabControl.TabCount)
                                {
                                    tabControl.SelectedIndex = tabIndex;
                                }
                            }
                        }
                    }
                }
                
                // Load Recent Projects
                XElement recentProjectsElem = root.Element("RecentProjects");
                if (recentProjectsElem != null)
                {
                    recentProjects = recentProjectsElem.Elements("Project")
                        .Select(e => e.Value)
                        .Where(p => Directory.Exists(p))
                        .Take(MAX_RECENT_PROJECTS)
                        .ToList();
                }
                
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
                        
                        if (Enum.TryParse<Games>(gameElem.Value, out Games game))
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
                }
            }
            finally
            {
                isLoadingSettings = false;
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
                    // Optionally delete old files after migration
                    // (commented out for safety - uncomment if desired)
                    /*
                    try { if (File.Exists(oldSettingsPath)) File.Delete(oldSettingsPath); } catch { }
                    try { if (File.Exists(oldSelectionsPath)) File.Delete(oldSelectionsPath); } catch { }
                    try { if (File.Exists(oldWindowStatePath)) File.Delete(oldWindowStatePath); } catch { }
                    try { if (File.Exists(oldRecentPath)) File.Delete(oldRecentPath); } catch { }
                    try { if (File.Exists(oldInjectionPath)) File.Delete(oldInjectionPath); } catch { }
                    try { Directory.Delete(Path.Combine(buildFolder, "config"), true); } catch { }
                    */
                }
            }
            catch { }
        }
        
        // Legacy methods - now call unified system
        private void SaveSettings()
        {
            SaveAllSettings();
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
            currentGscConfModeIndex = 0;
            currentInjectGameIndex = 0;
            SetupPlatformMenu();
            SetupGameMenu();
            SetupInjectGameMenu();
            SetupGscConfModeMenu();
            
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
            
            // Stop rainbow color timer
            if (rainbowColorTimer != null && rainbowColorTimer.Enabled)
            {
                rainbowColorTimer.Stop();
                rainbowColorTimer.Dispose();
            }
            
            // Save all settings to unified config file
            SaveAllSettings();
            base.OnFormClosing(e);
        }
        
        #endregion
    }
}


