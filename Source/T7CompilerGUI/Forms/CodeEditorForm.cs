using ReaLTaiizor.Controls;
using ReaLTaiizor.Drawing.Poison;
using ReaLTaiizor.Enum.Poison;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Manager;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;
using T7CompilerGUI.Controls;
using T7CompilerGUI.Forms.Dialogs;
using T7CompilerGUI.Games;
using T7CompilerGUI.Helpers;
using ReaLTaiizorExt = ReaLTaiizor.Extension.Poison;

namespace T7CompilerGUI.Forms
{
    public partial class CodeEditorForm : PoisonForm
    {
        #region Constants

        // UI Constants for dynamically created controls
        private const int FileButtonHeight = 23;
        private const int CloseButtonWidth = 20;
        private const int CloseButtonHeight = 23;
        private const int MaxTabTextLength = 18;
        private const int FileButtonMargin = 2;
        private const int FileButtonPanelPadding = 10;

        // Fallback colors (used when StyleManager is null) - use PoisonPaint for consistency
        private static Color FallbackButtonBackColor(ThemeStyle theme) =>
            PoisonPaint.BackColor.Button.Normal(theme);
        private static Color FallbackButtonForeColor(ThemeStyle theme) =>
            PoisonPaint.ForeColor.Button.Normal(theme);
        private static Color CloseButtonHoverColor(ThemeStyle theme) => PoisonPaint.GetStyleColor(ColorStyle.Red); // Red is universal for close buttons

        #endregion

        #region Fields and Properties

        private PoisonStyleManager styleManager;
        private System.Windows.Forms.Timer rainbowTimer; // Timer to update UI when rainbow is active
        private string projectPath = string.Empty;
        private bool folderOpened = false;
        private bool hasChanges = false;
        private bool isLoadingFiles = false; // Flag to prevent hasChanges during file loading
        private string selectedTabItem = string.Empty;
        private string currentFileName = string.Empty;
        private TreyarchCompiler.Enums.Games currentGame = TreyarchCompiler.Enums.Games.T7;
        private string currentGameModeStr = "ZM"; // Default to ZM like original
        private string projectNamespace = string.Empty; // Namespace read from source files (e.g., "serious")
        private string executingDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        // Tab scrolling debounce
        private DateTime lastTabScrollTime = DateTime.MinValue;
        private const int TabScrollThrottleMs = 10; // Minimum milliseconds between tab changes

        /// <summary>
        /// Gets the GUI installation path. Uses MainForm's method for consistency
        /// </summary>
        private string GetGuiPath()
        {
            return MainForm.GetGuiPath();
        }

        private string GetResourcesPath()
        {
            // First, check in the executable directory (for installed/built versions)
            // Resources folder is copied to t7gui\Resources during build
            string resourcesPath = Path.Combine(executingDir, "Resources");
            if (Directory.Exists(resourcesPath))
            {
                return resourcesPath;
            }

            // Try to find Resources folder relative to solution directory (for development)
            // Solution is typically at: My T7\TreyarchCompiler.sln
            // Resources is at: My T7\Resources
            string currentDir = executingDir;
            for (int i = 0; i < 5; i++) // Limit search depth
            {
                resourcesPath = Path.Combine(currentDir, "Resources");
                if (Directory.Exists(resourcesPath))
                {
                    return resourcesPath;
                }

                // Check parent directory
                string parentDir = Path.GetDirectoryName(currentDir);
                if (string.IsNullOrEmpty(parentDir) || parentDir == currentDir)
                    break;
                currentDir = parentDir;
            }

            // Fallback: return executable directory (will create Resources there if needed)
            return Path.Combine(executingDir, "Resources");
        }

        /// <summary>
        /// Gets the path to the Defaults folder containing template files
        /// </summary>
        private string GetDefaultsPath()
        {
            // First, try in the executable directory (for installed/built versions)
            string defaultsPath = Path.Combine(executingDir, "Defaults");
            if (Directory.Exists(defaultsPath))
            {
                return defaultsPath;
            }

            // Try to find Defaults folder relative to solution directory
            // Defaults is at: Source\T7CompilerGUI\Defaults
            string currentDir = executingDir;
            for (int i = 0; i < 5; i++) // Limit search depth
            {
                // Check if we're in a build folder (bin\Debug, bin\Release, etc.)
                if (currentDir.Contains("bin") || currentDir.Contains("Builds"))
                {
                    // Go up to find Source folder
                    string sourcePath = Path.Combine(currentDir, "..", "..", "Source", "T7CompilerGUI", "Defaults");
                    sourcePath = Path.GetFullPath(sourcePath);
                    if (Directory.Exists(sourcePath))
                    {
                        return sourcePath;
                    }
                }

                // Check current directory
                defaultsPath = Path.Combine(currentDir, "Defaults");
                if (Directory.Exists(defaultsPath))
                {
                    return defaultsPath;
                }

                // Check Source\T7CompilerGUI\Defaults
                defaultsPath = Path.Combine(currentDir, "Source", "T7CompilerGUI", "Defaults");
                if (Directory.Exists(defaultsPath))
                {
                    return defaultsPath;
                }

                // Check parent directory
                string parentDir = Path.GetDirectoryName(currentDir);
                if (string.IsNullOrEmpty(parentDir) || parentDir == currentDir)
                    break;
                currentDir = parentDir;
            }

            // Fallback: try relative to executable
            return Path.Combine(executingDir, "Defaults");
        }

        // Timers
        private System.Windows.Forms.Timer themeCheckTimer;
        private System.Windows.Forms.Timer tabUpdateTimer;

        // File Watcher
        private FileSystemWatcher fileWatcher;

        // Editor Management
        private Dictionary<string, AvalonEditWrapper> openEditors = new Dictionary<string, AvalonEditWrapper>();
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

            // Load form icon
            T7CompilerGUI.Helpers.FormIconHelper.LoadFormIcon(this);

            // PoisonForm already has double buffering enabled via ControlStyles
            // No need to enable it manually

            // Load GSC syntax data from GSC.xshd early (before SetupControls)
            LoadGscSyntaxData();

            SetupControls();
            SetupKeyboardShortcuts();
            SetupTimers();
            SetupFileWatcher();
            SetupThemeChangeHandling();
            // Initialize code editor keybinds
            codeEditorKeybinds = GetDefaultCodeEditorKeybinds();

            // Configure scrollbars for panels and controls
            SetupScrollbarSettings();

            // Check if compiler is installed, if not install it
            if (!MainForm.IsCompilerInstalled())
            {
                MainForm.InstallCompiler(@"https://gsc.dev/t7c_package");
            }

            // Defer default project creation until after form is shown
            // This ensures the form displays even if project creation fails
            this.Shown += (s, e) =>
            {
                try
                {
                    // Check for last project and ask to load it
                    CheckAndLoadLastProject();
                }
                catch (Exception ex)
                {
                    // Log error but don't prevent form from showing
                    System.Diagnostics.Debug.WriteLine($"Failed to check last project: {ex.Message}");
                    // Fallback to default project creation
                    try
                    {
                        CreateDefaultProjectOnStartup();
                    }
                    catch (Exception ex2)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to create default project: {ex2.Message}");
                    }
                }
            };
        }

        private void SetupThemeChangeHandling()
        {
            if (styleManager == null) return;

            ThemeStyle lastTheme = styleManager.Theme;
            ColorStyle lastStyle = styleManager.Style;

            // Create a tracked timer to periodically check for theme/style changes
            themeCheckTimer = ReaLTaiizorExt.PoisonFormHelper.CreateTrackedTimer(this, 100, (s, e) =>
            {
                // Check if form is disposed or disposing
                if (this.IsDisposed || this.Disposing || styleManager == null)
                {
                    if (themeCheckTimer != null)
                    {
                        themeCheckTimer.Stop();
                    }
                    return;
                }

                // Check if theme or style has changed
                if (styleManager.Theme != lastTheme || styleManager.Style != lastStyle)
                {
                    lastTheme = styleManager.Theme;
                    lastStyle = styleManager.Style;

                    // Update theme and style immediately on UI thread
                    // Use SafeInvoke for error handling (handles all exceptions internally)
                    ReaLTaiizorExt.PoisonFormHelper.SafeInvoke(this, () =>
                    {
                        if (!this.IsDisposed && !this.Disposing)
                        {
                            UpdateThemeAndStyle();
                        }
                    });
                }
            });

            themeCheckTimer.Start();

            // Also update on form activation
            this.Activated += (s, e) =>
            {
                if (styleManager != null)
                {
                    UpdateThemeAndStyle();
                }
            };
        }

        /// <summary>
        /// Sets up rainbow animation timer to update UI when rainbow style is active
        /// </summary>
        private void SetupRainbowTimer()
        {
            // Setup rainbow update timer - updates all controls when rainbow is active
            rainbowTimer = ReaLTaiizorExt.PoisonFormHelper.CreateTrackedTimer(this, 16, (s, e) =>
            {
                // Check if form is disposed or disposing
                if (this.IsDisposed || this.Disposing) return;

                // Check if rainbow is active by finding MainForm
                MainForm mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
                if (mainForm != null && mainForm.IsRainbowStyleActive())
                {
                    // Invalidate all controls recursively to update rainbow colors
                    ReaLTaiizorExt.PoisonControlHelper.RefreshAllControls(this);

                    // Also invalidate the form itself for border updates
                    this.Invalidate(true);
                    this.Update();
                }
            });
            rainbowTimer.Start();
        }


        /// <summary>
        /// Public method to update theme and style - can be called from outside when theme/style changes
        /// Uses ReaLTaiizor's proper StyleManager.Update() method for automatic theme propagation
        /// </summary>
        public void UpdateThemeAndStyle()
        {
            // Check if form is disposed or disposing
            if (this.IsDisposed || this.Disposing || styleManager == null) return;

            try
            {
                // Suspend layout to batch all updates - prevents incremental painting
                this.SuspendLayout();

                // Use helper to apply StyleManager to form and all controls
                ReaLTaiizorExt.PoisonControlHelper.ApplyStyleManager(this, styleManager);

                // Ensure Owner is set
                if (styleManager != null)
                {
                    styleManager.Owner = this;
                }

                // Sync Designer's poisonStyleManager with the active styleManager
                if (poisonStyleManager != null)
                {
                    poisonStyleManager.Theme = styleManager.Theme;
                    poisonStyleManager.Style = styleManager.Style;
                }

                // Update form background color to match theme (before StyleManager.Update)
                this.BackColor = PoisonPaint.BackColor.Form(styleManager.Theme);

                // Update menu strip (needs custom renderer, not handled by StyleManager)
                if (mainMenuStrip != null)
                {
                    mainMenuStrip.BackColor = PoisonPaint.BackColor.Form(styleManager.Theme);
                    mainMenuStrip.ForeColor = PoisonPaint.ForeColor.Label.Normal(styleManager.Theme);
                    mainMenuStrip.Renderer = new PoisonMenuStripRenderer(styleManager);
                }

                // Update file buttons panel background (FlowLayoutPanel doesn't support StyleManager)
                if (fileButtonsPanel != null)
                {
                    fileButtonsPanel.BackColor = PoisonPaint.BackColor.Form(styleManager.Theme);
                }

                // Update all open code editors (custom controls) - these need manual theme updates
                UpdateAllEditorsTheme();

                // Update all registered controls first (dynamically created file buttons)
                ReaLTaiizorExt.PoisonControlHelper.UpdateRegisteredControls(styleManager);
                
                // Update all file buttons (dynamically created) - ensure they're connected to StyleManager
                UpdateAllFileButtonsTheme();

                // Update editor panel context menu theme using helper
                if (editorPanelContextMenu != null)
                {
                    ReaLTaiizorExt.PoisonControlHelper.ApplyStyleManager(editorPanelContextMenu, styleManager);
                }

                // Use StyleManager.Update() to automatically update all IPoisonControl and IPoisonComponent controls
                // This recursively calls Refresh() on all controls, so no additional refresh calls are needed
                styleManager.Update();

                // Resume layout - StyleManager.Update() already refreshed all controls
                // No Invalidate/Update/Refresh needed - they cause visible transitions
                this.ResumeLayout(false);
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

            // Update StyleManager on all open editors - this will trigger ApplyPoisonTheme() in each editor
            // All updates are batched - no individual refreshes happen here
            foreach (var editor in openEditors.Values)
            {
                if (editor != null && !editor.IsDisposed)
                {
                    // Setting StyleManager will automatically apply Poison theme colors
                    // This doesn't trigger immediate refresh - StyleManager.Update() handles that
                    editor.StyleManager = styleManager;

                    // Update WPF context menu theme if it exists
                    // WPF controls need manual color updates, but no refresh is needed
                    if (editor.Editor?.TextArea?.ContextMenu != null)
                    {
                        UpdateWpfContextMenuTheme(editor.Editor.TextArea.ContextMenu);
                    }
                }
            }
        }

        private void UpdateWpfContextMenuTheme(System.Windows.Controls.ContextMenu contextMenu)
        {
            if (contextMenu == null || styleManager == null) return;

            try
            {
                // Get theme colors from Poison - match MainForm's PoisonContextMenuStrip
                var backColor = PoisonPaint.BackColor.Form(styleManager.Theme);
                var foreColor = PoisonPaint.ForeColor.Button.Normal(styleManager.Theme);
                var borderColor = PoisonPaint.BorderColor.Button.Normal(styleManager.Theme);
                var styleColor = PoisonPaint.GetStyleColor(styleManager.Style);

                // Convert to WPF colors
                var wpfBackColor = System.Windows.Media.Color.FromArgb(backColor.A, backColor.R, backColor.G, backColor.B);
                var wpfForeColor = System.Windows.Media.Color.FromArgb(foreColor.A, foreColor.R, foreColor.G, foreColor.B);
                var wpfBorderColor = System.Windows.Media.Color.FromArgb(borderColor.A, borderColor.R, borderColor.G, borderColor.B);
                // Use style color with 150 alpha for hover (matches MainForm's StyleBasedMenuRenderer)
                var wpfHoverColor = System.Windows.Media.Color.FromArgb(150,
                    (byte)styleColor.R, (byte)styleColor.G, (byte)styleColor.B);

                contextMenu.Background = new System.Windows.Media.SolidColorBrush(wpfBackColor);
                contextMenu.Foreground = new System.Windows.Media.SolidColorBrush(wpfForeColor);
                contextMenu.BorderBrush = new System.Windows.Media.SolidColorBrush(wpfBorderColor);

                // Update all menu items with proper hover handling
                foreach (var item in contextMenu.Items)
                {
                    if (item is System.Windows.Controls.MenuItem menuItem)
                    {
                        menuItem.Background = new System.Windows.Media.SolidColorBrush(wpfBackColor);
                        menuItem.Foreground = new System.Windows.Media.SolidColorBrush(wpfForeColor);

                        // Remove existing handlers to avoid duplicates
                        menuItem.MouseEnter -= MenuItem_MouseEnter;
                        menuItem.MouseLeave -= MenuItem_MouseLeave;

                        // Add hover handlers
                        menuItem.MouseEnter += MenuItem_MouseEnter;
                        menuItem.MouseLeave += MenuItem_MouseLeave;

                        // Store colors in Tag for use in event handlers
                        menuItem.Tag = new { Normal = wpfBackColor, Hover = wpfHoverColor };
                    }
                    else if (item is System.Windows.Controls.Separator separator)
                    {
                        // Style separator to match theme
                        separator.Background = new System.Windows.Media.SolidColorBrush(wpfBorderColor);
                    }
                }
            }
            catch
            {
                // Ignore errors - context menu might be disposed
            }
        }

        private void MenuItem_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem menuItem && menuItem.Tag != null)
            {
                try
                {
                    dynamic colors = menuItem.Tag;
                    menuItem.Background = new System.Windows.Media.SolidColorBrush(colors.Hover);
                }
                catch
                {
                    // Ignore errors
                }
            }
        }

        private void MenuItem_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem menuItem && menuItem.Tag != null)
            {
                try
                {
                    dynamic colors = menuItem.Tag;
                    menuItem.Background = new System.Windows.Media.SolidColorBrush(colors.Normal);
                }
                catch
                {
                    // Ignore errors
                }
            }
        }

        private void UpdateAllFileButtonsTheme()
        {
            if (styleManager == null || fileButtonsPanel == null) return;

            // Update all file buttons in the file list panel
            // All StyleManager assignments are batched - no individual refreshes happen here
            foreach (Control control in fileButtonsPanel.Controls)
            {
                if (control is PoisonPanel buttonContainer && !buttonContainer.IsDisposed)
                {
                    // Update container panel - StyleManager assignment doesn't trigger immediate refresh
                    // Use PoisonControlHelper to apply StyleManager to container and buttons
                    ReaLTaiizorExt.PoisonControlHelper.ApplyStyleManager(buttonContainer, styleManager);

                    // Update file button and close button (already handled by ApplyStyleManager, but verify)
                    foreach (Control child in buttonContainer.Controls)
                    {
                        if (child is PoisonButton btn && !btn.IsDisposed)
                        {
                            // StyleManager already applied via ApplyStyleManager above

                            // For close buttons (X buttons), update colors to match theme
                            // These are set but won't paint until ResumeLayout and Refresh
                            if (btn.Text == "X")
                            {
                                btn.BackColor = PoisonPaint.BackColor.Button.Normal(styleManager.Theme);
                                btn.ForeColor = PoisonPaint.ForeColor.Button.Normal(styleManager.Theme);
                            }
                        }
                    }
                }
            }
        }


        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // Theme is already applied in SetupControls() called from constructor
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // Update theme on show to catch any changes
            // UpdateThemeAndStyle() handles all rendering - no need for explicit Refresh()
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

        /// <summary>
        /// Removes placeholder tabs that may be added for designer visibility
        /// </summary>
        private void RemovePlaceholderTabs()
        {
            if (tabControl == null) return;

            // Remove any tabs with "Placeholder" in the name
            for (int i = tabControl.TabPages.Count - 1; i >= 0; i--)
            {
                var tab = tabControl.TabPages[i];
                if (tab != null && (tab.Name?.Contains("Placeholder") == true || tab.Text == "Code Editor"))
                {
                    // Dispose editor and tab before removing
                    var tabPageToRemove = tabControl.TabPages[i];
                    string pathToRemove = null;

                    // Find the path for this tab
                    foreach (var kvp in editorTabs)
                    {
                        if (kvp.Value == tabPageToRemove)
                        {
                            pathToRemove = kvp.Key;
                            break;
                        }
                    }

                    // Properly clean up tab and free memory
                    if (pathToRemove != null)
                    {
                        CleanupTabResources(pathToRemove);
                    }

                    // Dispose and remove tab page
                    tabPageToRemove.Dispose();
                    tabControl.TabPages.RemoveAt(i);
                }
            }
        }

        private void SetupControls()
        {
            // Remove any placeholder tabs added by the designer
            RemovePlaceholderTabs();

            // Use helper to apply StyleManager to form and all controls
            if (styleManager != null)
            {
                ReaLTaiizorExt.PoisonControlHelper.ApplyStyleManager(this, styleManager);
                styleManager.Owner = this;

                // Sync the Designer's poisonStyleManager component
                if (poisonStyleManager != null)
                {
                    // Sync the component's properties with the passed-in styleManager
                    poisonStyleManager.Theme = styleManager.Theme;
                    poisonStyleManager.Style = styleManager.Style;
                    poisonStyleManager.Owner = this;
                }

                // Apply theme to menu strip with custom renderer (MenuStrip needs special handling)
                if (mainMenuStrip != null)
                {
                    mainMenuStrip.BackColor = PoisonPaint.BackColor.Form(styleManager.Theme);
                    mainMenuStrip.ForeColor = PoisonPaint.ForeColor.Label.Normal(styleManager.Theme);
                    mainMenuStrip.Renderer = new PoisonMenuStripRenderer(styleManager);
                }

                // Apply theme to file buttons panel (FlowLayoutPanel doesn't support StyleManager)
                if (fileButtonsPanel != null)
                {
                    fileButtonsPanel.BackColor = PoisonPaint.BackColor.Form(styleManager.Theme);
                }

                // Use StyleManager.Update() to automatically apply theme to all Poison controls
                styleManager.Update();
            }

            // Setup menus (menu items created at runtime due to dynamic behavior)
            SetupMainMenu();
            SetupGameMenu();
            SetupGameModeMenu();

            // Hide scrollbar corner control using Windows API (runtime behavior, must stay in code)
            SetupScrollbarCornerHiding();

            // Setup mouse scrolling for file list panel
            SetupFileListMouseScrolling();

            // Setup context menu for editor panel and tab control
            SetupEditorPanelContextMenu();

            ReaLTaiizorExt.PoisonControlHelper.SetupAllButtonEffectsRecursive(this, styleManager);

            // StyleManager.Update() already handles all control refreshes
            // No need for explicit Refresh() call here - it causes unnecessary paint events
        }

        private ReaLTaiizor.Controls.PoisonContextMenuStrip editorPanelContextMenu;

        private void SetupEditorPanelContextMenu()
        {
            // Create context menu for editor panel
            editorPanelContextMenu = new ReaLTaiizor.Controls.PoisonContextMenuStrip(this.components);

            // Use helper to apply StyleManager - handles Theme, Style, and UseStyleColors automatically
            if (styleManager != null)
            {
                ReaLTaiizorExt.PoisonControlHelper.ApplyStyleManager(editorPanelContextMenu, styleManager);
            }
            else
            {
                // Fallback if no StyleManager - use helper to set properties with default theme
                ReaLTaiizorExt.PoisonControlHelper.SetupControl(editorPanelContextMenu, null, true, false);
                ReaLTaiizorExt.PoisonControlHelper.UpdateThemeAndStyle(editorPanelContextMenu,
                    ReaLTaiizor.Enum.Poison.ThemeStyle.Dark, ReaLTaiizor.Enum.Poison.ColorStyle.Blue);
            }

            var goToLineItem = new System.Windows.Forms.ToolStripMenuItem("Go to Line...");
            goToLineItem.Click += (s, e) => GoToLine();
            editorPanelContextMenu.Items.Add(goToLineItem);

            editorPanelContextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            var refreshItem = new System.Windows.Forms.ToolStripMenuItem("Refresh Files");
            refreshItem.Click += (s, e) => RefreshItem_Click(s, e);
            editorPanelContextMenu.Items.Add(refreshItem);

            // Assign to editor panel
            if (editorPanel != null)
            {
                editorPanel.ContextMenuStrip = editorPanelContextMenu;
            }

            // Also assign to tab control
            if (tabControl != null)
            {
                tabControl.ContextMenuStrip = editorPanelContextMenu;
            }
        }

        private void SetupScrollbarCornerHiding()
        {
            // Hide scrollbar corner for panels and tab control
            Action<Control> setupCornerHiding = (control) =>
            {
                if (control == null) return;
                control.HandleCreated += (s, e) => HideScrollbarCorner(control);
                control.Layout += (s, e) =>
                {
                    if (control.IsHandleCreated)
                        HideScrollbarCorner(control);
                };
            };

            setupCornerHiding(editorPanel);
            setupCornerHiding(mainPanel);
            setupCornerHiding(tabControl);

            // Hide tab control scrollbars if it has any
            if (tabControl != null)
            {
                tabControl.HandleCreated += (s, e) => HideScrollbarCorner(tabControl);
                tabControl.Layout += (s, e) =>
                {
                    if (tabControl.IsHandleCreated)
                        HideScrollbarCorner(tabControl);
                };
            }
        }

        /// <summary>
        /// Configures scrollbar settings for panels and controls
        /// </summary>
        private void SetupScrollbarSettings()
        {
            // File buttons panel: use helper to configure invisible scrollbars
            if (fileButtonsPanel != null)
            {
                ReaLTaiizorExt.PoisonControlHelper.ConfigureScrollbars(fileButtonsPanel,
                    showVertical: true,
                    showHorizontal: true,
                    verticalInvisible: true,
                    horizontalInvisible: true);
            }

            // Editor panel: use helper to configure invisible scrollbars
            if (editorPanel != null)
            {
                ReaLTaiizorExt.PoisonControlHelper.ConfigureScrollbars(editorPanel,
                    showVertical: true,
                    showHorizontal: true,
                    verticalInvisible: true,
                    horizontalInvisible: true);
            }

            // Tab pages: scrollbars are configured as invisible when created in CreateTabPage()
            // Tab control itself doesn't have scrollbar properties
        }

        private void SetupFileListMouseScrolling()
        {
            if (fileButtonsPanel == null) return;

            // Now that fileButtonsPanel is a PoisonPanel, we just need to ensure scrollbars are invisible
            // Settings are already configured in Designer and SetupScrollbarSettings()
            // Enable middle mouse button drag scrolling for better UX
            bool isMiddleMouseScrolling = false;
            System.Drawing.Point scrollStartPoint = System.Drawing.Point.Empty;
            int scrollStartY = 0;

            fileButtonsPanel.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Middle)
                {
                    isMiddleMouseScrolling = true;
                    scrollStartPoint = e.Location;
                    scrollStartY = -fileButtonsPanel.AutoScrollPosition.Y;
                    fileButtonsPanel.Cursor = Cursors.Hand;
                }
            };

            fileButtonsPanel.MouseMove += (s, e) =>
            {
                if (isMiddleMouseScrolling)
                {
                    int deltaY = scrollStartPoint.Y - e.Y;
                    int newY = scrollStartY + deltaY;

                    // Calculate max scroll position
                    int maxScroll = Math.Max(0, fileButtonsPanel.DisplayRectangle.Height - fileButtonsPanel.Height);
                    newY = Math.Max(0, Math.Min(newY, maxScroll));

                    // Scroll by setting AutoScrollPosition (negative value)
                    fileButtonsPanel.AutoScrollPosition = new System.Drawing.Point(0, newY);
                }
            };

            fileButtonsPanel.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Middle && isMiddleMouseScrolling)
                {
                    isMiddleMouseScrolling = false;
                    fileButtonsPanel.Cursor = Cursors.Default;
                }
            };

            fileButtonsPanel.MouseLeave += (s, e) =>
            {
                if (isMiddleMouseScrolling)
                {
                    isMiddleMouseScrolling = false;
                    fileButtonsPanel.Cursor = Cursors.Default;
                }
            };
        }

        // Windows API to hide scrollbar corner - ReaLTaiizor's Native classes are internal
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


        private void SetupTimers()
        {
            // Tab update timer (100ms like original)
            tabUpdateTimer = ReaLTaiizorExt.PoisonFormHelper.CreateTrackedTimer(this, 100, (s, e) => UpdateSelectedTabItem());
            tabUpdateTimer.Start();

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
            // Menu items are now created in Designer - event handlers are wired up in Designer.cs
            // This method is kept for potential future use but currently does nothing
        }

        // Menu event handler methods (called from Designer)
        private void NewProjectItem_Click(object sender, EventArgs e)
        {
            BtnNewProject_Click(sender: null, e: EventArgs.Empty);
        }

        private void NewFileItem_Click(object sender, EventArgs e)
        {
            BtnNewFile_Click(sender: null, e: EventArgs.Empty);
        }

        private void SaveItem_Click(object sender, EventArgs e)
        {
            BtnSave_Click(sender: null, e: EventArgs.Empty);
        }

        private void SaveAllItem_Click(object sender, EventArgs e)
        {
            BtnSaveAll_Click(sender: null, e: EventArgs.Empty);
        }

        private void RefreshItem_Click(object sender, EventArgs e)
        {
            BtnRefresh_Click(sender: null, e: EventArgs.Empty);
        }

        private void PortILItem_Click(object sender, EventArgs e)
        {
            BtnPortIL_Click(sender: null, e: EventArgs.Empty);
        }

        private void ShortcutsItem_Click(object sender, EventArgs e)
        {
            ShortcutsMenu_Click(sender, e);
        }

        private void GoToLineItem_Click(object sender, EventArgs e)
        {
            GoToLine();
        }

        private void ForceHostItem_Click(object sender, EventArgs e)
        {
            ForceHostMenu_Click(sender, e);
        }

        private void ResetHostItem_Click(object sender, EventArgs e)
        {
            ClearHostDvarsMenu_Click(sender, e);
        }

        private void ExitItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void T7GameItem_Click(object sender, EventArgs e)
        {
            currentGame = TreyarchCompiler.Enums.Games.T7;
            t7GameItem.Checked = true;
            t8GameItem.Checked = false;
        }

        private void T8GameItem_Click(object sender, EventArgs e)
        {
            currentGame = TreyarchCompiler.Enums.Games.T8;
            t8GameItem.Checked = true;
            t7GameItem.Checked = false;
        }

        private void CampaignModeItem_Click(object sender, EventArgs e)
        {
            currentGameModeStr = "SP";
            campaignModeItem.Checked = true;
            multiplayerModeItem.Checked = false;
            zombiesModeItem.Checked = false;
            // Save mode change to gsc.conf
            SaveModeToGscConf();
            UpdateConditionalCompilationIndicators();
        }

        private void MultiplayerModeItem_Click(object sender, EventArgs e)
        {
            currentGameModeStr = "MP";
            multiplayerModeItem.Checked = true;
            campaignModeItem.Checked = false;
            zombiesModeItem.Checked = false;
            // Save mode change to gsc.conf
            SaveModeToGscConf();
            UpdateConditionalCompilationIndicators();
        }

        private void ZombiesModeItem_Click(object sender, EventArgs e)
        {
            currentGameModeStr = "ZM";
            zombiesModeItem.Checked = true;
            campaignModeItem.Checked = false;
            multiplayerModeItem.Checked = false;
            // Save mode change to gsc.conf
            SaveModeToGscConf();
            UpdateConditionalCompilationIndicators();
        }

        /// <summary>
        /// Saves the current mode and symbols to gsc.conf
        /// </summary>
        private void SaveModeToGscConf()
        {
            if (string.IsNullOrEmpty(projectPath) || !Directory.Exists(projectPath))
                return;

            string gscConfPath = Path.Combine(projectPath, "gsc.conf");
            try
            {
                // Build symbols string from current state
                var symbolsList = new List<string>();

                // Add game symbol (BO3 or BO4)
                if (currentGame == TreyarchCompiler.Enums.Games.T7)
                    symbolsList.Add("BO3");
                else if (currentGame == TreyarchCompiler.Enums.Games.T8)
                    symbolsList.Add("BO4");

                // Add namespace from source files (if found)
                if (!string.IsNullOrEmpty(projectNamespace))
                {
                    symbolsList.Add(projectNamespace);
                }

                // Add game mode (MP, ZM, or SP)
                symbolsList.Add(currentGameModeStr.ToLower());

                // Add enabled custom symbols
                foreach (var symbol in availableCustomSymbols.OrderBy(s => s))
                {
                    string upperSymbol = symbol.ToUpper();
                    if (customSymbolStates.ContainsKey(upperSymbol) && customSymbolStates[upperSymbol])
                    {
                        symbolsList.Add(symbol);
                    }
                }

                string symbolsString = string.Join(",", symbolsList);
                File.WriteAllText(gscConfPath, $"symbols={symbolsString}");
            }
            catch (Exception ex)
            {
                // Log error but don't interrupt user
                System.Diagnostics.Debug.WriteLine($"Error saving gsc.conf: {ex.Message}");
            }
        }

        private void InjectBO3Item_Click(object sender, EventArgs e)
        {
            InjectPrecompiledScript(TreyarchCompiler.Enums.Games.T7);
        }

        private void InjectBO4Item_Click(object sender, EventArgs e)
        {
            InjectPrecompiledScript(TreyarchCompiler.Enums.Games.T8);
        }

        private void KillBO3Item_Click(object sender, EventArgs e)
        {
            KillGame("blackops3");
        }

        private void KillBO4Item_Click(object sender, EventArgs e)
        {
            KillGame("blackops4");
        }

        private void SetupGameMenu()
        {
        }

        private void SetupGameModeMenu()
        {
        }

        // Removed ResetButtonState wrapper - PoisonButton.OnClick now handles state reset automatically
        // If needed for non-PoisonButton controls, use PoisonControlHelper.ResetButtonState directly

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
                        case "Go to Line":
                            GoToLine();
                            break;
                    }
                    return;
                }
            }

            // Zoom shortcuts (Ctrl+Plus, Ctrl+Minus, Ctrl+0)
            if (e.Control)
            {
                if (e.KeyCode == Keys.Add || e.KeyCode == Keys.Oemplus)
                {
                    e.Handled = true;
                    ZoomIn();
                    return;
                }
                else if (e.KeyCode == Keys.Subtract || e.KeyCode == Keys.OemMinus)
                {
                    e.Handled = true;
                    ZoomOut();
                    return;
                }
                else if (e.KeyCode == Keys.D0 || e.KeyCode == Keys.NumPad0)
                {
                    e.Handled = true;
                    ZoomReset();
                    return;
                }
            }

            // Go to Line (Ctrl+G)
            if (e.Control && e.KeyCode == Keys.G)
            {
                e.Handled = true;
                GoToLine();
                return;
            }

            // Undo/Redo - let AvalonEdit handle these natively
            // Ctrl+Z for undo, Ctrl+Y or Ctrl+Shift+Z for redo
            if (e.Control && e.KeyCode == Keys.Z && !e.Shift)
            {
                // Let AvalonEdit handle undo natively - don't mark as handled
                // AvalonEdit will process this automatically
            }
            else if ((e.Control && e.KeyCode == Keys.Y) || (e.Control && e.Shift && e.KeyCode == Keys.Z))
            {
                // Let AvalonEdit handle redo natively - don't mark as handled
                // AvalonEdit will process this automatically
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
                { "Save", new Dialogs.KeybindDialog.KeybindInfo("Save", Keys.S, ctrl: true) },
                { "Save All", new Dialogs.KeybindDialog.KeybindInfo("Save All", Keys.S, ctrl: true, shift: true) },
                { "Refresh Files", new Dialogs.KeybindDialog.KeybindInfo("Refresh Files", Keys.R, ctrl: true) },
                { "Port IL Project", new Dialogs.KeybindDialog.KeybindInfo("Port IL Project", Keys.O, ctrl: true, shift: true, alt: true) },
                { "Compile", new Dialogs.KeybindDialog.KeybindInfo("Compile", Keys.F9) },
                { "Find", new Dialogs.KeybindDialog.KeybindInfo("Find", Keys.F, ctrl: true) },
                { "Find Next", new Dialogs.KeybindDialog.KeybindInfo("Find Next", Keys.F3) },
                { "Replace", new Dialogs.KeybindDialog.KeybindInfo("Replace", Keys.H, ctrl: true) },
                { "Go to Line", new Dialogs.KeybindDialog.KeybindInfo("Go to Line", Keys.G, ctrl: true) }
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
                string functionsPath = Path.Combine(scriptsPath, "functions");
                Directory.CreateDirectory(scriptsPath);
                Directory.CreateDirectory(functionsPath);

                // Get default template files path
                string defaultsPath = GetDefaultsPath();

                if (currentGame == TreyarchCompiler.Enums.Games.T7)
                {
                    // For T7, use multiple template files
                    CreateFileFromTemplate(defaultsPath, "defaultt7project.main", Path.Combine(scriptsPath, "main.gsc"), GetDefaultMainContent());
                    CreateFileFromTemplate(defaultsPath, "defaultt7project.options", Path.Combine(scriptsPath, "options.gsc"), "");
                    CreateFileFromTemplate(defaultsPath, "defaultt7project.util", Path.Combine(scriptsPath, "util.gsc"), "");
                    CreateFileFromTemplate(defaultsPath, "defaultt7project._util", Path.Combine(functionsPath, "_util.gsc"), "");
                    CreateFileFromTemplate(defaultsPath, "defaultt7project.misc", Path.Combine(functionsPath, "misc.gsc"), "");
                    CreateFileFromTemplate(defaultsPath, "defaultt7project.stats", Path.Combine(functionsPath, "stats.gsc"), "");
                    CreateFileFromTemplate(defaultsPath, "defaultt7project.zombies_only", Path.Combine(functionsPath, "zombies_only.gsc"), "");
                }
                else
                {
                    // For T8, use main and headers files
                    CreateFileFromTemplate(defaultsPath, "defaultt8project.main", Path.Combine(scriptsPath, "main.gsc"), GetDefaultMainContent());
                    CreateFileFromTemplate(defaultsPath, "defaultt8project.headers", Path.Combine(scriptsPath, "headers.gsc"), GetDefaultHeadersContent());
                }

                string gameSymbol = currentGame == TreyarchCompiler.Enums.Games.T7 ? "bo3" : "bo4";
                string gameModeLower = currentGameModeStr.ToLower();
                // Read namespace from source files if available, otherwise use empty (will be read when project loads)
                string namespaceSymbol = !string.IsNullOrEmpty(projectNamespace) ? projectNamespace : "";
                string symbolsLine = string.IsNullOrEmpty(namespaceSymbol)
                    ? $"symbols={gameSymbol},{gameModeLower}"
                    : $"symbols={gameSymbol},{namespaceSymbol},{gameModeLower}";
                File.WriteAllText(Path.Combine(selectedPath, "gsc.conf"), symbolsLine);

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
            string selectedPath = ModernFolderDialog.Show(this, "Select Project Folder");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                OpenFolder(selectedPath);
                AddToRecentProjects(selectedPath);
            }
        }

        private void BtnRecentProjects_Click(object sender, EventArgs e)
        {
            ShowRecentProjectsMenu();
        }

        private void AddToRecentProjects(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath) || !Directory.Exists(projectPath))
                return;

            try
            {
                List<string> recentProjects = GetRecentProjects();

                // Remove if already exists (to move to top)
                recentProjects.Remove(projectPath);

                // Add to beginning
                recentProjects.Insert(0, projectPath);

                // Keep only last 10 projects
                if (recentProjects.Count > 10)
                {
                    recentProjects = recentProjects.Take(10).ToList();
                }

                // Save to file
                SaveRecentProjects(recentProjects);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to add to recent projects: {ex.Message}");
            }
        }

        private const string CONFIG_FILE_NAME = "T7CompilerGUI.conf";
        private const int MAX_RECENT_PROJECTS = 10;

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

        private List<string> GetRecentProjects()
        {
            try
            {
                string configPath = GetConfigPath();
                if (File.Exists(configPath))
                {
                    XDocument config = XDocument.Load(configPath);
                    XElement root = config.Root;
                    if (root != null)
                    {
                        XElement recentProjectsElem = root.Element("RecentProjects");
                        if (recentProjectsElem != null)
                        {
                            return recentProjectsElem.Elements("Project")
                                .Select(e => e.Value)
                                .Where(p => !string.IsNullOrEmpty(p) && Directory.Exists(p))
                                .Take(MAX_RECENT_PROJECTS)
                                .ToList();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load recent projects: {ex.Message}");
            }

            return new List<string>();
        }

        private void SaveRecentProjects(List<string> projects)
        {
            try
            {
                string configPath = GetConfigPath();
                XDocument config;

                // Load existing config or create new one
                if (File.Exists(configPath))
                {
                    config = XDocument.Load(configPath);
                }
                else
                {
                    config = new XDocument(
                        new XElement("T7CompilerConfig")
                    );
                }

                XElement root = config.Root;
                if (root == null)
                {
                    root = new XElement("T7CompilerConfig");
                    config.Add(root);
                }

                // Remove existing RecentProjects element if it exists
                XElement existingRecentProjects = root.Element("RecentProjects");
                if (existingRecentProjects != null)
                {
                    existingRecentProjects.Remove();
                }

                // Add new RecentProjects element
                if (projects != null && projects.Count > 0)
                {
                    root.Add(new XElement("RecentProjects",
                        projects.Take(MAX_RECENT_PROJECTS).Select(p => new XElement("Project", p))
                    ));
                }

                // Save the config file
                config.Save(configPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save recent projects: {ex.Message}");
            }
        }

        private void ShowRecentProjectsMenu()
        {
            List<string> recentProjects = GetRecentProjects();

            if (recentProjects.Count == 0)
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(
                    this,
                    "No recent projects found.",
                    "Recent Projects",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // Create context menu using PoisonContextMenuStrip
            // Pass null for Container since we're creating it dynamically (constructor accepts null)
            ReaLTaiizor.Controls.PoisonContextMenuStrip recentMenu = new ReaLTaiizor.Controls.PoisonContextMenuStrip(null);
            if (styleManager != null)
            {
                // Use helper to apply StyleManager - handles Theme, Style, and UseStyleColors automatically
                ReaLTaiizorExt.PoisonControlHelper.ApplyStyleManager(recentMenu, styleManager);
            }
            else
            {
                // Fallback if no StyleManager - use helper to set properties with default theme
                ReaLTaiizorExt.PoisonControlHelper.SetupControl(recentMenu, null, true, false);
                ReaLTaiizorExt.PoisonControlHelper.UpdateThemeAndStyle(recentMenu,
                    ReaLTaiizor.Enum.Poison.ThemeStyle.Dark, ReaLTaiizor.Enum.Poison.ColorStyle.Blue);
            }

            foreach (string projectPath in recentProjects)
            {
                string displayName = Path.GetFileName(projectPath);
                if (string.IsNullOrEmpty(displayName))
                    displayName = projectPath;

                // Truncate if too long
                if (displayName.Length > 50)
                {
                    displayName = displayName.Substring(0, 47) + "...";
                }

                ToolStripMenuItem item = new ToolStripMenuItem(displayName)
                {
                    ToolTipText = projectPath
                };

                string path = projectPath; // Capture for closure
                item.Click += (s, e) =>
                {
                    if (Directory.Exists(path))
                    {
                        OpenFolder(path);
                        AddToRecentProjects(path);
                    }
                    else
                    {
                        ReaLTaiizor.Controls.PoisonMessageBox.Show(
                            this,
                            $"Project path no longer exists:\n{path}",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        // Remove from recent projects
                        List<string> updated = GetRecentProjects();
                        updated.Remove(path);
                        SaveRecentProjects(updated);
                    }
                };

                recentMenu.Items.Add(item);
            }

            // Add separator and clear option
            recentMenu.Items.Add(new ToolStripSeparator());
            ToolStripMenuItem clearItem = new ToolStripMenuItem("Clear Recent Projects");
            clearItem.Click += (s, e) =>
            {
                var result = ReaLTaiizor.Controls.PoisonMessageBox.Show(
                    this,
                    "Clear all recent projects?",
                    "Clear Recent Projects",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    SaveRecentProjects(new List<string>());
                }
            };
            recentMenu.Items.Add(clearItem);

            // Show menu at button location
            Point location = btnRecentProjects.PointToScreen(new Point(0, btnRecentProjects.Height));
            recentMenu.Show(location);
        }

        private void BtnNewFile_Click(object sender, EventArgs e)
        {
            if (!folderOpened)
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, "Please open a project folder first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Use NewFileDialog with proper validation (matches original T7-Compiler-UI)
            if (NewFileDialog.ShowNewFileDialog(this, projectPath, out string fileName, out string fileExtension, styleManager))
            {
                RefreshFileList(false);

                // Open the new file
                OpenFileInEditor($"{fileName}.{fileExtension}");
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            SaveCurrentFile();
        }

        private void BtnSaveAll_Click(object sender, EventArgs e)
        {
            SaveAllFiles();
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            RefreshFileList(false);
        }

        private void BtnPortIL_Click(object sender, EventArgs e)
        {
            string ilProjectPath = ModernFolderDialog.Show(this, "Select IL (Infinity Loader) Project Folder");
            if (string.IsNullOrEmpty(ilProjectPath) || !Directory.Exists(ilProjectPath))
                return;

            // Check if this is an IL project using new detection method
            string detectionError;
            if (!IsILProject(ilProjectPath, out detectionError))
            {
                string errorMessage = "This doesn't appear to be an IL (Infinity Loader) project.\n\nLooking for:\n- .il files\n- system::register(\"infinityloader\")\n- #ifdef IL directives";
                if (!string.IsNullOrEmpty(detectionError))
                {
                    errorMessage += $"\n\nDetection Details:\n{detectionError}";
                }
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this,
                    errorMessage,
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

                // Delete all .il files
                foreach (var ilFile in Directory.GetFiles(scriptsPath, "*.il", SearchOption.AllDirectories))
                {
                    File.Delete(ilFile);
                }

                // Convert IL syntax to T7 syntax in all .gsc files
                foreach (var gscFile in Directory.GetFiles(scriptsPath, "*.gsc", SearchOption.AllDirectories))
                {
                    try
                    {
                        string content = File.ReadAllText(gscFile);
                        string convertedContent = ConvertILToT7(content);
                        File.WriteAllText(gscFile, convertedContent);
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue with other files
                        System.Diagnostics.Debug.WriteLine($"Error converting IL file {gscFile}: {ex.Message}");
                    }
                }

                string gameSymbol = currentGame == TreyarchCompiler.Enums.Games.T7 ? "bo3" : "bo4";
                string gameModeLower = currentGameModeStr.ToLower();
                // Read namespace from source files if available
                string namespaceSymbol = !string.IsNullOrEmpty(projectNamespace) ? projectNamespace : "";
                string symbolsLine = string.IsNullOrEmpty(namespaceSymbol)
                    ? $"symbols={gameSymbol},{gameModeLower}"
                    : $"symbols={gameSymbol},{namespaceSymbol},{gameModeLower}";
                File.WriteAllText(Path.Combine(outputPath, "gsc.conf"), symbolsLine);

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
        private AvalonEditWrapper.SearchFlagsEnum lastSearchFlags = AvalonEditWrapper.SearchFlagsEnum.None;

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab == null)
                return;

            using (var searchDialog = new Dialogs.SearchDialog(styleManager))
            {
                if (searchDialog.ShowDialog(this) == DialogResult.OK)
                {
                    lastSearchText = searchDialog.SearchText;
                    lastSearchFlags = AvalonEditWrapper.SearchFlagsEnum.None;
                    if (searchDialog.MatchCase)
                        lastSearchFlags |= AvalonEditWrapper.SearchFlagsEnum.MatchCase;
                    if (searchDialog.WholeWord)
                        lastSearchFlags |= AvalonEditWrapper.SearchFlagsEnum.WholeWord;

                    // Reset search state to start fresh
                    currentSearchFileIndex = -1;
                    currentSearchPosition = -1;
                    searchableFiles.Clear();

                    // Always search all project files
                    InitializeSearchAllProjectFiles();

                    // Perform search and show results
                    List<Dialogs.SearchResultsDialog.SearchResult> results = PerformSearch(searchDialog.SearchText, lastSearchFlags, true);

                    if (results.Count > 1)
                    {
                        // Show results dialog if multiple matches
                        ShowSearchResults(results);
                    }
                    else if (results.Count == 1)
                    {
                        // Navigate to the single result
                        NavigateToSearchResult(results[0]);
                    }
                    else
                    {
                        // No results found
                        ReaLTaiizor.Controls.PoisonMessageBox.Show(this, "Text not found.", "Search", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void GoToLine()
        {
            AvalonEditWrapper currentEditor = GetCurrentEditor();
            if (currentEditor == null)
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, "No file is currently open.", "Go to Line", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int maxLine = currentEditor.LineCount;

            // Build list of navigation items (functions, labels, ifdefs)
            var navigationItems = BuildNavigationItems(currentEditor);

            // Use the proper GoToLineDialog
            using (var dialog = new Dialogs.GoToLineDialog(navigationItems, maxLine, currentEditor.CurrentLine, styleManager))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    int targetLine = dialog.SelectedLineNumber;

                    // If an ifdef/endif was selected and it has a matching line, navigate to the match
                    if (dialog.SelectedNavigationItem != null &&
                        dialog.SelectedNavigationItem.MatchingLineNumber > 0 &&
                        (dialog.SelectedNavigationItem.Type == "ifdef" || dialog.SelectedNavigationItem.Type == "endif"))
                    {
                        targetLine = dialog.SelectedNavigationItem.MatchingLineNumber;
                    }

                    if (targetLine >= 1 && targetLine <= maxLine)
                    {
                        currentEditor.CurrentLine = targetLine;
                        currentEditor.Focus();
                    }
                    else
                    {
                        ReaLTaiizor.Controls.PoisonMessageBox.Show(this, $"Invalid line number. Please enter a number between 1 and {maxLine}.", "Go to Line", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        // NavigationItem is now defined in GoToLineDialog - use that type instead
        private List<Dialogs.GoToLineDialog.NavigationItem> BuildNavigationItems(AvalonEditWrapper editor)
        {
            var items = new List<Dialogs.GoToLineDialog.NavigationItem>();

            if (editor == null || string.IsNullOrEmpty(editor.Text))
                return items;

            string[] lines = editor.Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            // Dictionary to track ifdef/endif pairs: ifdef line -> endif line
            var ifdefToEndif = new Dictionary<int, int>();
            var endifToIfdef = new Dictionary<int, int>();

            // Stack to track ifdef nesting for matching endif
            var ifdefStack = new Stack<(int line, string symbol, string type)>();

            // First pass: build the ifdef/endif mapping
            for (int i = 0; i < lines.Length; i++)
            {
                int lineNumber = i + 1;
                string line = lines[i].Trim();

                // Match #ifdef, #ifndef
                var ifdefMatch = Regex.Match(line, @"#ifdef\s+(\w+)", RegexOptions.IgnoreCase);
                var ifndefMatch = Regex.Match(line, @"#ifndef\s+(\w+)", RegexOptions.IgnoreCase);
                if (ifdefMatch.Success || ifndefMatch.Success)
                {
                    string symbol = ifdefMatch.Success ? ifdefMatch.Groups[1].Value : ifndefMatch.Groups[1].Value;
                    ifdefStack.Push((lineNumber, symbol, ifdefMatch.Success ? "ifdef" : "ifndef"));
                }

                // Match #endif
                if (Regex.IsMatch(line, @"#endif", RegexOptions.IgnoreCase))
                {
                    if (ifdefStack.Count > 0)
                    {
                        var matchingIfdef = ifdefStack.Pop();
                        ifdefToEndif[matchingIfdef.line] = lineNumber;
                        endifToIfdef[lineNumber] = matchingIfdef.line;
                    }
                }
            }

            // Second pass: build navigation items with matching line numbers
            for (int i = 0; i < lines.Length; i++)
            {
                int lineNumber = i + 1;
                string line = lines[i].Trim();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Match functions: functionName( or functionName {
                var functionMatch = Regex.Match(line, @"^\s*(\w+)\s*[({]", RegexOptions.IgnoreCase);
                if (functionMatch.Success)
                {
                    string functionName = functionMatch.Groups[1].Value;
                    // Skip common keywords that aren't functions
                    if (!IsKeyword(functionName))
                    {
                        items.Add(new Dialogs.GoToLineDialog.NavigationItem
                        {
                            DisplayText = $"function {functionName}()",
                            LineNumber = lineNumber,
                            Type = "function"
                        });
                    }
                }

                // Match labels: labelName:
                var labelMatch = Regex.Match(line, @"^\s*(\w+)\s*:", RegexOptions.IgnoreCase);
                if (labelMatch.Success)
                {
                    string labelName = labelMatch.Groups[1].Value;
                    items.Add(new Dialogs.GoToLineDialog.NavigationItem
                    {
                        DisplayText = $"label {labelName}:",
                        LineNumber = lineNumber,
                        Type = "label"
                    });
                }

                // Match #ifdef, #ifndef
                var ifdefMatch = Regex.Match(line, @"#ifdef\s+(\w+)", RegexOptions.IgnoreCase);
                var ifndefMatch = Regex.Match(line, @"#ifndef\s+(\w+)", RegexOptions.IgnoreCase);
                if (ifdefMatch.Success)
                {
                    string symbol = ifdefMatch.Groups[1].Value;
                    int matchingEndif = ifdefToEndif.ContainsKey(lineNumber) ? ifdefToEndif[lineNumber] : -1;
                    items.Add(new Dialogs.GoToLineDialog.NavigationItem
                    {
                        DisplayText = matchingEndif > 0 ? $"#ifdef {symbol} → line {matchingEndif}" : $"#ifdef {symbol}",
                        LineNumber = lineNumber,
                        Type = "ifdef",
                        MatchingLineNumber = matchingEndif
                    });
                }
                else if (ifndefMatch.Success)
                {
                    string symbol = ifndefMatch.Groups[1].Value;
                    int matchingEndif = ifdefToEndif.ContainsKey(lineNumber) ? ifdefToEndif[lineNumber] : -1;
                    items.Add(new Dialogs.GoToLineDialog.NavigationItem
                    {
                        DisplayText = matchingEndif > 0 ? $"#ifndef {symbol} → line {matchingEndif}" : $"#ifndef {symbol}",
                        LineNumber = lineNumber,
                        Type = "ifdef",
                        MatchingLineNumber = matchingEndif
                    });
                }

                // Match #endif
                if (Regex.IsMatch(line, @"#endif", RegexOptions.IgnoreCase))
                {
                    int matchingIfdef = endifToIfdef.ContainsKey(lineNumber) ? endifToIfdef[lineNumber] : -1;
                    if (matchingIfdef > 0)
                    {
                        items.Add(new Dialogs.GoToLineDialog.NavigationItem
                        {
                            DisplayText = $"#endif → line {matchingIfdef}",
                            LineNumber = lineNumber,
                            Type = "endif",
                            MatchingLineNumber = matchingIfdef
                        });
                    }
                    else
                    {
                        items.Add(new Dialogs.GoToLineDialog.NavigationItem
                        {
                            DisplayText = "#endif",
                            LineNumber = lineNumber,
                            Type = "endif"
                        });
                    }
                }
            }

            return items;
        }

        private bool IsKeyword(string word)
        {
            // Common GSC keywords that shouldn't be treated as functions
            string[] keywords = { "if", "else", "while", "for", "foreach", "switch", "case", "default",
                                 "return", "wait", "waitframe", "waittill", "thread", "self", "level",
                                 "game", "undefined", "true", "false", "var", "const", "private", "new" };
            return Array.IndexOf(keywords, word.ToLower()) >= 0;
        }

        private void ZoomIn()
        {
            AvalonEditWrapper currentEditor = GetCurrentEditor();
            if (currentEditor != null)
            {
                currentEditor.ZoomIn();
            }
        }

        private void ZoomOut()
        {
            AvalonEditWrapper currentEditor = GetCurrentEditor();
            if (currentEditor != null)
            {
                currentEditor.ZoomOut();
            }
        }

        private void ZoomReset()
        {
            AvalonEditWrapper currentEditor = GetCurrentEditor();
            if (currentEditor != null)
            {
                currentEditor.ZoomReset();
            }
        }

        // Track search state across all files
        private int currentSearchFileIndex = -1;
        private int currentSearchPosition = -1;
        private List<string> searchableFiles = new List<string>();

        // Use SearchResultsDialog.SearchResult for consistency

        private void FindNext()
        {
            if (string.IsNullOrEmpty(lastSearchText))
            {
                BtnSearch_Click(null, EventArgs.Empty);
                return;
            }

            // Initialize search across all open files
            if (currentSearchFileIndex < 0 || searchableFiles.Count == 0)
            {
                InitializeSearchAcrossFiles();
            }

            if (searchableFiles.Count == 0)
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, "No files to search.", "Search", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Search in current file from current position
            AvalonEditWrapper currentEditor = GetCurrentEditor();
            if (currentEditor != null && currentSearchFileIndex >= 0 && currentSearchFileIndex < searchableFiles.Count)
            {
                string currentFileName = searchableFiles[currentSearchFileIndex];
                if (openEditors.ContainsKey(currentFileName) && openEditors[currentFileName] == currentEditor)
                {
                    // Continue searching in current file
                    int startPos = currentSearchPosition >= 0 ? currentSearchPosition : currentEditor.CurrentPosition;
                    int endPos = currentEditor.TextLength;

                    currentEditor.SearchFlags = lastSearchFlags;
                    currentEditor.TargetStart = startPos;
                    currentEditor.TargetEnd = endPos;

                    int foundPos = currentEditor.SearchInTarget(lastSearchText);

                    if (foundPos >= 0)
                    {
                        currentEditor.SetSelection(currentEditor.TargetStart, currentEditor.TargetEnd);
                        currentEditor.ScrollCaret();
                        currentEditor.Focus();
                        currentSearchPosition = currentEditor.TargetEnd;
                        return;
                    }
                }
            }

            // Not found in current file, search in next files
            bool found = SearchInNextFile();

            if (!found)
            {
                // Not found anywhere - wrap around and search from beginning
                // Reset search state and try again from the start
                currentSearchFileIndex = -1;
                currentSearchPosition = -1;
                InitializeSearchAcrossFiles();

                // Try one more time from the beginning
                found = SearchInNextFile();

                if (!found)
                {
                    ReaLTaiizor.Controls.PoisonMessageBox.Show(this, "Text not found in any open file.", "Search", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    currentSearchFileIndex = -1;
                    currentSearchPosition = -1;
                }
            }
        }

        private void InitializeSearchAcrossFiles()
        {
            searchableFiles.Clear();

            // Get all open GSC files
            foreach (var kvp in openEditors)
            {
                if (kvp.Key.EndsWith(".gsc", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    searchableFiles.Add(kvp.Key);
                }
            }

            // Start from current file
            AvalonEditWrapper currentEditor = GetCurrentEditor();
            if (currentEditor != null)
            {
                foreach (var kvp in openEditors)
                {
                    if (kvp.Value == currentEditor)
                    {
                        currentSearchFileIndex = searchableFiles.IndexOf(kvp.Key);
                        if (currentSearchFileIndex < 0)
                            currentSearchFileIndex = 0;
                        currentSearchPosition = currentEditor.CurrentPosition;
                        return;
                    }
                }
            }

            currentSearchFileIndex = 0;
            currentSearchPosition = 0;
        }

        private void InitializeSearchAllProjectFiles()
        {
            searchableFiles.Clear();

            if (string.IsNullOrEmpty(projectPath) || !Directory.Exists(projectPath))
                return;

            try
            {
                // Get all .gsc and .csc files from the project
                string[] files = Directory.GetFiles(projectPath, "*.gsc", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    if (!searchableFiles.Contains(file, StringComparer.OrdinalIgnoreCase))
                        searchableFiles.Add(file);
                }

                files = Directory.GetFiles(projectPath, "*.csc", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    if (!searchableFiles.Contains(file, StringComparer.OrdinalIgnoreCase))
                        searchableFiles.Add(file);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing search all project files: {ex.Message}");
            }
        }

        private List<Dialogs.SearchResultsDialog.SearchResult> PerformSearch(string searchText, AvalonEditWrapper.SearchFlagsEnum flags, bool searchAllFiles)
        {
            List<Dialogs.SearchResultsDialog.SearchResult> results = new List<Dialogs.SearchResultsDialog.SearchResult>();

            if (string.IsNullOrEmpty(searchText))
                return results;

            List<string> filesToSearch = new List<string>();

            if (searchAllFiles)
            {
                // Search all project files
                InitializeSearchAllProjectFiles();
                filesToSearch.AddRange(searchableFiles);
            }
            else
            {
                // Search only open files
                foreach (var kvp in openEditors)
                {
                    if (kvp.Key.EndsWith(".gsc", StringComparison.OrdinalIgnoreCase) ||
                        kvp.Key.EndsWith(".csc", StringComparison.OrdinalIgnoreCase) ||
                        kvp.Key.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        filesToSearch.Add(kvp.Key);
                    }
                }
            }

            foreach (string filePath in filesToSearch)
            {
                try
                {
                    string[] lines = File.ReadAllLines(filePath);
                    bool matchCase = (flags & AvalonEditWrapper.SearchFlagsEnum.MatchCase) != 0;
                    bool wholeWord = (flags & AvalonEditWrapper.SearchFlagsEnum.WholeWord) != 0;

                    StringComparison comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i];
                        int searchIndex = 0;

                        while (true)
                        {
                            int index = line.IndexOf(searchText, searchIndex, comparison);
                            if (index < 0)
                                break;

                            // Check whole word if needed
                            if (wholeWord)
                            {
                                bool isWholeWord = true;
                                if (index > 0 && char.IsLetterOrDigit(line[index - 1]))
                                    isWholeWord = false;
                                if (index + searchText.Length < line.Length && char.IsLetterOrDigit(line[index + searchText.Length]))
                                    isWholeWord = false;

                                if (!isWholeWord)
                                {
                                    searchIndex = index + 1;
                                    continue;
                                }
                            }

                            // Found a match
                            results.Add(new Dialogs.SearchResultsDialog.SearchResult
                            {
                                FilePath = filePath,
                                LineNumber = i + 1,
                                ColumnNumber = index + 1,
                                LineText = line,
                                MatchStart = index,
                                MatchLength = searchText.Length
                            });

                            searchIndex = index + searchText.Length;
                        }
                    }
                }
                catch
                {
                    // Skip files that can't be read
                    continue;
                }
            }

            return results;
        }

        private void ShowSearchResults(List<Dialogs.SearchResultsDialog.SearchResult> results)
        {
            using (var resultsDialog = new Dialogs.SearchResultsDialog(results, styleManager, projectPath))
            {
                if (resultsDialog.ShowDialog(this) == DialogResult.OK && resultsDialog.SelectedResult != null)
                {
                    NavigateToSearchResult(resultsDialog.SelectedResult);
                }
            }
        }

        private void NavigateToSearchResult(Dialogs.SearchResultsDialog.SearchResult result)
        {
            // Open the file if not already open
            if (!openEditors.ContainsKey(result.FilePath))
            {
                OpenFileInEditor(result.FilePath);
                // Give it time to load
                Application.DoEvents();
            }

            // Switch to the file's tab
            if (editorTabs.ContainsKey(result.FilePath))
            {
                tabControl.SelectedTab = editorTabs[result.FilePath];
                Application.DoEvents();
            }

            // Navigate to the line and column
            if (openEditors.ContainsKey(result.FilePath))
            {
                AvalonEditWrapper editor = openEditors[result.FilePath];
                editor.CurrentLine = result.LineNumber;
                editor.CurrentColumn = result.ColumnNumber;

                // Select the match
                if (result.LineNumber > 0 && result.LineNumber <= editor.LineCount)
                {
                    int lineStart = editor.Lines[result.LineNumber - 1].Position;
                    int startPos = lineStart + result.MatchStart;
                    int endPos = startPos + result.MatchLength;
                    editor.SetSelection(startPos, endPos);
                    editor.ScrollCaret();
                    editor.Focus();
                }
            }
        }


        private bool SearchInNextFile()
        {
            if (searchableFiles.Count == 0)
                return false;

            // Start from next file (or current file if we haven't started searching yet)
            int startFileIndex = currentSearchFileIndex >= 0 ? currentSearchFileIndex + 1 : 0;

            // If we've wrapped around, start from beginning
            if (startFileIndex >= searchableFiles.Count)
                startFileIndex = 0;

            // Search through all files (wrap around once)
            for (int fileOffset = 0; fileOffset < searchableFiles.Count; fileOffset++)
            {
                int fileIndex = (startFileIndex + fileOffset) % searchableFiles.Count;
                string fileName = searchableFiles[fileIndex];

                // If file is not open, open it for searching
                if (!openEditors.ContainsKey(fileName))
                {
                    if (File.Exists(fileName))
                    {
                        // Open the file in a new tab
                        OpenFileInEditor(fileName);
                        // Give it time to load
                        Application.DoEvents();
                    }
                    else
                    {
                        continue; // File doesn't exist, skip it
                    }
                }

                if (!openEditors.ContainsKey(fileName))
                    continue;

                AvalonEditWrapper editor = openEditors[fileName];
                if (editor == null)
                    continue;

                // Switch to this file's tab BEFORE searching
                if (editorTabs.ContainsKey(fileName))
                {
                    tabControl.SelectedTab = editorTabs[fileName];
                    // Give the tab time to switch
                    Application.DoEvents();
                }

                // Search in this file - start from beginning if it's a new file, or from current position if it's the first file we're checking
                editor.SearchFlags = lastSearchFlags;

                // If this is the first file we're checking and we have a saved position, use it
                // Otherwise start from beginning (or current cursor position if it's the current file)
                if (fileOffset == 0 && fileIndex == currentSearchFileIndex && currentSearchPosition >= 0)
                {
                    // Continue from where we left off in the current file
                    editor.TargetStart = currentSearchPosition;
                }
                else
                {
                    // Start from beginning of file (or current cursor if it's the active tab)
                    if (tabControl.SelectedTab != null && editorTabs.ContainsKey(fileName) && editorTabs[fileName] == tabControl.SelectedTab)
                    {
                        editor.TargetStart = editor.CurrentPosition;
                    }
                    else
                    {
                        editor.TargetStart = 0;
                    }
                }

                editor.TargetEnd = editor.TextLength;

                int foundPos = editor.SearchInTarget(lastSearchText);

                if (foundPos >= 0)
                {
                    // Found it! Select and scroll to it
                    editor.SetSelection(editor.TargetStart, editor.TargetEnd);
                    editor.ScrollCaret();
                    editor.Focus();

                    // Update search state
                    currentSearchFileIndex = fileIndex;
                    currentSearchPosition = editor.TargetEnd;
                    return true;
                }

                // Not found in this file, reset position for next file
                currentSearchPosition = 0;
            }

            return false;
        }

        private void BtnReplace_Click(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab == null)
                return;

            using (var replaceDialog = new Dialogs.ReplaceDialog(styleManager))
            {
                if (replaceDialog.ShowDialog() == DialogResult.OK)
                {
                    string searchText = replaceDialog.SearchText;
                    string replaceText = replaceDialog.ReplaceText;
                    bool matchCase = replaceDialog.MatchCase;
                    bool wholeWord = replaceDialog.WholeWord;
                    bool replaceAll = replaceDialog.ReplaceAll;

                    AvalonEditWrapper currentEditor = GetCurrentEditor();
                    if (currentEditor == null)
                        return;

                    // Set search flags
                    AvalonEditWrapper.SearchFlagsEnum searchFlags = AvalonEditWrapper.SearchFlagsEnum.None;
                    if (matchCase)
                        searchFlags |= AvalonEditWrapper.SearchFlagsEnum.MatchCase;
                    if (wholeWord)
                        searchFlags |= AvalonEditWrapper.SearchFlagsEnum.WholeWord;

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
                AvalonEditWrapper editor = kvp.Value;

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
                                    // Found it! Open the file (use full path)
                                    string fullPath = Path.GetFullPath(filePath);
                                    OpenFileInEditor(fullPath);

                                    // Wait a bit for the file to open, then navigate
                                    Application.DoEvents();
                                    System.Threading.Thread.Sleep(50);

                                    if (openEditors.ContainsKey(fullPath))
                                    {
                                        AvalonEditWrapper editor = openEditors[fullPath];
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

        internal void OpenFolder(string path)
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
                // CRITICAL: Dispose all resources before clearing to prevent memory leaks
                // Dispose all editors and tab pages first
                if (openEditors != null && openEditors.Count > 0)
                {
                    var keysToCleanup = new List<string>(openEditors.Keys);
                    foreach (var key in keysToCleanup)
                    {
                        CleanupTabResources(key);
                    }
                }

                // Dispose all file button controls (they have event handlers that keep references alive)
                if (fileButtonsPanel != null && fileButtonsPanel.Controls.Count > 0)
                {
                    var controlsToDispose = new List<Control>();
                    foreach (Control control in fileButtonsPanel.Controls)
                    {
                        controlsToDispose.Add(control);
                    }
                    fileButtonsPanel.Controls.Clear(); // Clear first to remove from parent
                    foreach (var control in controlsToDispose)
                    {
                        // Dispose container and all child controls (buttons)
                        DisposeControlRecursive(control);
                    }
                }

                // Clear dictionaries (resources already disposed above)
                if (openEditors != null)
                {
                    openEditors.Clear();
                }
                if (editorTabs != null)
                {
                    editorTabs.Clear();
                }
                if (tabControl != null && tabControl.TabPages.Count > 0)
                {
                    tabControl.TabPages.Clear();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing controls in OpenFolder: {ex.Message}");
            }

            // Set project path and folder opened BEFORE calling RefreshFileList
            projectPath = path;
            folderOpened = true;
            string projectName = Path.GetFileName(projectPath);

            // Update UI controls with null checks
            if (lblProjectPath != null)
            {
                lblProjectPath.Text = $"Project: {projectName}";
            }

            // Update status bar to show project path
            if (statusLabel != null)
            {
                statusLabel.Text = GetDefaultStatusText();
            }

            // Add to recent projects
            AddToRecentProjects(path);

            // Skip IL detection for default project (template project, not a real IL project)
            string guiPath = GetGuiPath();
            string defaultProjectBasePath = Path.Combine(guiPath, "defaultproject");
            bool isDefaultProject = path.StartsWith(defaultProjectBasePath, StringComparison.OrdinalIgnoreCase);

            // Skip IL detection for already-ported projects (they have "_ported" in the path)
            bool isPortedProject = path.IndexOf("_ported", StringComparison.OrdinalIgnoreCase) >= 0;

            // Check for IL project (new syntax: system::register("infinityloader" or #ifdef IL)
            // Skip detection for default project template and already-ported projects
            if (!isDefaultProject && !isPortedProject)
            {
                string ilDetectionError;
                if (IsILProject(path, out ilDetectionError))
                {
                    string message = "This could be an Infinity Loader Project. Would you like to port it? (required)";
                    if (!string.IsNullOrEmpty(ilDetectionError))
                    {
                        message += $"\n\nDetection Details:\n{ilDetectionError}";
                    }
                    var a = ReaLTaiizor.Controls.PoisonMessageBox.Show(
                        this,
                        message,
                        "IL Project Detected",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (a != DialogResult.Yes)
                    {
                        // User declined to port - just show error and return, don't create default project again
                        ReaLTaiizor.Controls.PoisonMessageBox.Show(this, "Project loading cancelled. Please select a different project or create a new one.", "Project Not Loaded", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    // Port the project - PortILProject will open the ported version automatically
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

            // Only move files from the project root to scripts folder
            // Preserve subdirectory structure (like functions/, shared/, etc.)
            // This method should only organize files from root, not flatten subdirectories
            DirectoryInfo projectDir = dirInfo.Parent;
            if (projectDir != null && projectDir.Exists)
            {
                // Get files directly in project root (not in subdirectories)
                List<string> rootFiles = Directory.GetFiles(projectDir.FullName, "*.gsc", SearchOption.TopDirectoryOnly)
                    .Concat(Directory.GetFiles(projectDir.FullName, "*.txt", SearchOption.TopDirectoryOnly))
                    .ToList();

                foreach (string file in rootFiles)
                {
                    FileInfo mFile = new FileInfo(file);
                    // Only move files that are directly in project root to scripts folder
                    // Preserve subdirectory structure like scripts/functions/
                    string targetPath = Path.Combine(dirInfo.FullName, mFile.Name);
                    if (!File.Exists(targetPath))
                    {
                        try
                        {
                            mFile.MoveTo(targetPath);
                        }
                        catch
                        {
                            // File might be in use or already in correct location
                        }
                    }
                }
            }
            // Don't move files from scripts subdirectories - preserve structure
        }

        private void RefreshFileList(bool force)
        {
            if (!force && !folderOpened)
                return;

            // INSTANT UNLOAD: Hide panels immediately to prevent visual flicker
            // This makes unloading appear instant, just like loading
            bool wasFileButtonsPanelVisible = fileButtonsPanel != null ? fileButtonsPanel.Visible : false;
            bool wasTabControlVisible = tabControl != null ? tabControl.Visible : false;

            // Suspend painting and layout for both panels simultaneously
            if (fileButtonsPanel != null)
            {
                fileButtonsPanel.SuspendLayout();
                fileButtonsPanel.Visible = false; // Hide instantly - user won't see disposal
            }
            if (tabControl != null)
            {
                tabControl.SuspendLayout();
                tabControl.Visible = false; // Hide instantly - user won't see disposal
            }

            // OPTIMIZED UNLOAD: Batch all disposal operations for maximum speed
            // Clear tab pages first (fast operation, no disposal needed here)
            if (tabControl != null && tabControl.TabPages.Count > 0)
            {
                tabControl.TabPages.Clear(); // Fast clear - tab pages will be disposed separately
            }

            // Batch collect all resources to dispose in one pass (much faster than individual cleanup)
            var editorsToDispose = new List<AvalonEditWrapper>();
            var tabPagesToDispose = new List<ReaLTaiizor.Controls.PoisonTabPage>();
            var fileButtonsToDispose = new List<Control>();

            // Collect all resources in single pass
            if (openEditors != null && openEditors.Count > 0)
            {
                foreach (var kvp in openEditors)
                {
                    if (kvp.Value != null)
                    {
                        editorsToDispose.Add(kvp.Value);
                    }
                    if (editorTabs != null && editorTabs.ContainsKey(kvp.Key))
                    {
                        var tabPage = editorTabs[kvp.Key];
                        if (tabPage != null)
                        {
                            tabPagesToDispose.Add(tabPage);
                        }
                    }
                }
            }

            // Collect file buttons to dispose
            if (fileButtonsPanel != null && fileButtonsPanel.Controls.Count > 0)
            {
                foreach (Control control in fileButtonsPanel.Controls)
                {
                    fileButtonsToDispose.Add(control);
                }
                fileButtonsPanel.Controls.Clear(); // Clear parent reference immediately
            }

            // INSTANT CLEAR: Clear dictionaries immediately (memory cleanup happens instantly)
            // This is fast and prevents any references from being held
            if (openEditors != null)
            {
                openEditors.Clear();
            }
            if (editorTabs != null)
            {
                editorTabs.Clear();
            }
            if (fileContents != null)
            {
                fileContents.Clear(); // Clear file contents to free memory instantly
            }

            // Batch dispose all resources (actual disposal happens here, but references already cleared)
            // Dispose in efficient batches to avoid blocking UI thread
            try
            {
                // Dispose editors (these are the heaviest resources)
                foreach (var editor in editorsToDispose)
                {
                    try
                    {
                        if (editor != null && !editor.IsDisposed)
                        {
                            editor.Dispose();
                        }
                    }
                    catch
                    {
                        // Ignore disposal errors - resource is being cleaned up anyway
                    }
                }

                // Dispose tab pages (lighter, but still important)
                foreach (var tabPage in tabPagesToDispose)
                {
                    try
                    {
                        if (tabPage != null && !tabPage.IsDisposed)
                        {
                            tabPage.Dispose();
                        }
                    }
                    catch
                    {
                        // Ignore disposal errors
                    }
                }

                // Dispose file buttons (lightweight)
                foreach (var control in fileButtonsToDispose)
                {
                    try
                    {
                        if (control != null && !control.IsDisposed)
                        {
                            DisposeControlRecursive(control);
                        }
                    }
                    catch
                    {
                        // Ignore disposal errors
                    }
                }
            }
            catch
            {
                // Ignore any batch disposal errors - we've already cleared references
            }

            // Resume layout for file buttons panel (will be reshown in finally if loading)
            if (fileButtonsPanel != null)
            {
                fileButtonsPanel.ResumeLayout(false);
            }

            if (!folderOpened || string.IsNullOrEmpty(projectPath))
                return;

            // Only move contents to scripts folder when opening a new folder, not on refresh
            // This prevents unnecessary file operations when user just wants to refresh the list
            // MoveContentsToScripts should only be called from OpenFolder, not RefreshFileList
            string scriptsPath = Path.Combine(projectPath, "scripts");

            // Auto-detect GSC project: search recursively for .gsc and .txt files anywhere in the project folder
            // This supports various folder structures (scripts/, root, subfolders, etc.)
            string[] gscFiles = Directory.GetFiles(projectPath, "*.gsc", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(projectPath, "*.txt", SearchOption.AllDirectories))
                .Where(f => !f.EndsWith(".gscc", StringComparison.OrdinalIgnoreCase) &&
                            !f.EndsWith(".stub.gscc", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f =>
                {
                    // Always put main.gsc first (case-insensitive)
                    string fileName = Path.GetFileName(f);
                    if (fileName.Equals("main.gsc", StringComparison.OrdinalIgnoreCase))
                        return "0"; // Prefix with 0 to sort first
                    return "1"; // Other files come after
                })
                .ThenBy(f =>
                {
                    // Sort by folder path (directory) first
                    string directory = Path.GetDirectoryName(f);
                    if (!string.IsNullOrEmpty(projectPath) && directory.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
                    {
                        // Get relative directory path from project root
                        string relativeDir = directory.Substring(projectPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        return relativeDir.ToLowerInvariant(); // Case-insensitive folder sorting
                    }
                    return directory.ToLowerInvariant();
                })
                .ThenBy(f => Path.GetFileName(f).ToLowerInvariant()) // Then sort by filename alphabetically (case-insensitive)
                .ToArray();

            if (gscFiles == null || gscFiles.Length == 0)
            {
                // No GSC files found - check if scripts folder exists and create it if needed
                // scriptsPath is already declared above, so just check and create if needed
                if (!Directory.Exists(scriptsPath))
                {
                    Directory.CreateDirectory(scriptsPath);
                }
                return;
            }

            // Set loading flag to prevent hasChanges during file loading
            isLoadingFiles = true;

            // Hide tab control and file buttons panel during loading to make it look instant
            // (They're already hidden from disposal above, but ensure they stay hidden)
            if (tabControl != null)
            {
                tabControl.SuspendLayout();
                tabControl.Visible = false;
            }
            if (fileButtonsPanel != null)
            {
                fileButtonsPanel.SuspendLayout();
                fileButtonsPanel.Visible = false;
            }

            // Show loading status
            if (statusLabel != null)
            {
                statusLabel.Text = "Loading files...";
            }

            try
            {
                // Prepare file list with full absolute paths
                var fileList = new List<(string fullPath, int index)>();
                int i = 0;
                foreach (string gscFile in gscFiles)
                {
                    string fullPath = Path.GetFullPath(gscFile);
                    string filename = Path.GetFileName(gscFile);

                    if (filename.EndsWith(".gsc") || filename.EndsWith(".txt"))
                    {
                        fileList.Add((fullPath, i));
                        i++;
                    }
                }

                // Create all file buttons first (UI operations must be on UI thread)
                foreach (var (fullPath, index) in fileList)
                {
                    CreateFileButton(fullPath, index);
                }

                // Load all files efficiently - read files in parallel, then create editors on UI thread
                // Use local ConcurrentDictionary for thread-safe parallel reading, then transfer to class member
                var tempFileContents = new ConcurrentDictionary<string, string>();

                // Read all files in parallel (file I/O can be done on background threads)
                System.Threading.Tasks.Parallel.ForEach(fileList, fileInfo =>
                {
                    string filePath = fileInfo.fullPath;
                    if (File.Exists(filePath))
                    {
                        try
                        {
                            string content = File.ReadAllText(filePath);
                            // Store content even if empty - empty files should still show in editor
                            tempFileContents[fileInfo.fullPath] = content ?? "";
                        }
                        catch (Exception ex)
                        {
                            // Log error but still create empty editor for the file
                            System.Diagnostics.Debug.WriteLine($"Failed to read file {filePath}: {ex.Message}");
                            tempFileContents[fileInfo.fullPath] = "";
                        }
                    }
                    else
                    {
                        // File doesn't exist - create empty editor
                        tempFileContents[fileInfo.fullPath] = "";
                    }
                });

                // Create all editors on UI thread (must be on UI thread for WinForms controls)
                // This is fast since file I/O is already done
                // Don't show tabs during loading - build them all first, then show at once
                int fileIndex = 0;
                foreach (var fileInfo in fileList)
                {
                    if (tempFileContents.TryGetValue(fileInfo.fullPath, out string content))
                    {
                        // Ensure content is not null
                        string contentToLoad = content ?? "";
                        OpenFileInEditorWithContent(fileInfo.fullPath, contentToLoad);
                        // Don't call DoEvents during loading - keep tabs hidden
                        fileIndex++;
                    }
                    else
                    {
                        // File was found but couldn't be read - create empty editor
                        OpenFileInEditorWithContent(fileInfo.fullPath, "");
                    }
                }

                // Layout all file buttons after creating them (PoisonPanel doesn't auto-layout like FlowLayoutPanel)
                LayoutFileButtons();

            }
            finally
            {
                // Always show tabs and file buttons panel, and resume layout, even if an exception occurs
                // This makes loading look instant - all tabs and buttons appear at once
                if (tabControl != null)
                {
                    tabControl.ResumeLayout(true);
                    tabControl.Visible = true; // Always show after loading
                }
                if (fileButtonsPanel != null)
                {
                    fileButtonsPanel.ResumeLayout(true);
                    fileButtonsPanel.Visible = true; // Always show after loading
                }

                // Always clear the loading flag, even if an exception occurs
                // This ensures user edits will properly set hasChanges
                isLoadingFiles = false;

                // Update status to show completion
                if (statusLabel != null)
                {
                    statusLabel.Text = GetDefaultStatusText();
                }
            }

            // Update theme for all file buttons (including close buttons) after loading
            // This ensures all buttons have proper theme applied
            if (styleManager != null)
            {
                UpdateAllFileButtonsTheme();

                // Also update editor panel context menu theme
                if (editorPanelContextMenu != null && styleManager != null)
                {
                    // Use helper to apply StyleManager - handles Theme, Style automatically
                    ReaLTaiizorExt.PoisonControlHelper.ApplyStyleManager(editorPanelContextMenu, styleManager);
                }
            }

            // Layout file buttons after theme update
            LayoutFileButtons();

            // Ensure hasChanges is false after loading all files - use CheckForUnsavedChanges to verify
            CheckForUnsavedChanges();

            folderOpened = true;
        }

        private void CreateFileButton(string filePath, int index)
        {
            // Create container panel for file button and close button - use PoisonPanel for theme consistency
            // Use enhanced helper for better styling - CreateStyledPanel already applies StyleManager
            PoisonPanel buttonContainer = ReaLTaiizorExt.PoisonControlHelper.CreateStyledPanel(styleManager, true);
            buttonContainer.Height = FileButtonHeight;
            buttonContainer.Width = fileButtonsPanel.Width - FileButtonPanelPadding;
            buttonContainer.Margin = new Padding(0, FileButtonMargin, 0, FileButtonMargin);

            // Display relative path from project root on the button
            string displayPath;
            if (!string.IsNullOrEmpty(projectPath) && filePath.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
            {
                // Get relative path from project root
                displayPath = filePath.Substring(projectPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                // Replace backslashes with forward slashes for cleaner display
                displayPath = displayPath.Replace('\\', '/');
            }
            else
            {
                // Fallback to just filename if we can't determine relative path
                displayPath = Path.GetFileName(filePath);
            }

            // File button - use enhanced helper for better styling and effects
            PoisonButton fileButton = ReaLTaiizorExt.PoisonControlHelper.CreateButton(
                displayPath,
                styleManager,
                (s, e) =>
                {
                    OpenFileInEditor(filePath);
                },
                true);
            fileButton.Dock = DockStyle.Fill;
            fileButton.TextAlign = ContentAlignment.MiddleLeft;
            fileButton.FlatStyle = FlatStyle.Flat;
            fileButton.Tag = index;

            // Close button (X) - use enhanced helper for better styling and effects
            PoisonButton closeButton = ReaLTaiizorExt.PoisonControlHelper.CreateButton(
                "X",
                styleManager,
                (s, e) =>
                {
                    DeleteFile(filePath, index);
                },
                true);
            closeButton.Size = new Size(CloseButtonWidth, CloseButtonHeight);
            closeButton.Dock = DockStyle.Right;
            closeButton.FlatStyle = FlatStyle.Flat;
            closeButton.UseVisualStyleBackColor = false;

            // Enhanced hover effect for close button (red on hover)
            ThemeStyle currentTheme = styleManager?.Theme ?? ThemeStyle.Dark;
            closeButton.MouseEnter += (s, e) =>
            {
                closeButton.BackColor = CloseButtonHoverColor(currentTheme);
                // Use theme-aware foreground color for close button text
                closeButton.ForeColor = PoisonPaint.ForeColor.Button.Normal(currentTheme);
            };
            closeButton.MouseLeave += (s, e) =>
            {
                // Reset to theme colors
                closeButton.BackColor = PoisonPaint.BackColor.Button.Normal(currentTheme);
                closeButton.ForeColor = PoisonPaint.ForeColor.Button.Normal(currentTheme);
            };
            closeButton.Name = Path.GetFileName(filePath).Replace(".gsc", "").Replace(".txt", "").Replace(' ', '_');
            
            // Register dynamically created buttons for automatic StyleManager updates
            if (styleManager != null)
            {
                ReaLTaiizorExt.PoisonControlHelper.RegisterControl(fileButton, styleManager);
                ReaLTaiizorExt.PoisonControlHelper.RegisterControl(closeButton, styleManager);
            }

            buttonContainer.Controls.Add(closeButton);
            buttonContainer.Controls.Add(fileButton);
            fileButtonsPanel.Controls.Add(buttonContainer);
        }

        /// <summary>
        /// Layouts file buttons vertically in the fileButtonsPanel (PoisonPanel doesn't auto-layout like FlowLayoutPanel)
        /// </summary>
        private void LayoutFileButtons()
        {
            if (fileButtonsPanel == null) return;

            int yPos = 0;
            foreach (Control control in fileButtonsPanel.Controls)
            {
                if (control is PoisonPanel buttonContainer)
                {
                    buttonContainer.Location = new Point(0, yPos);
                    buttonContainer.Width = fileButtonsPanel.ClientSize.Width;
                    yPos += buttonContainer.Height + FileButtonMargin * 2;
                }
            }

            // Update panel's preferred size for scrolling
            fileButtonsPanel.AutoScrollMinSize = new Size(0, yPos);
        }

        private void DeleteFile(string filePath, int index)
        {
            // Ensure we have a full path
            string fullPath = Path.IsPathRooted(filePath) ? Path.GetFullPath(filePath) : Path.Combine(projectPath, filePath);

            var result = ReaLTaiizor.Controls.PoisonMessageBox.Show(
                this,
                $"Warning: This will delete the file ({fullPath}) Continue?",
                "Warning",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            if (File.Exists(fullPath))
                File.Delete(fullPath);

            // Remove from editors using the full path as key
            if (editorTabs.ContainsKey(fullPath))
            {
                // Remove tab page from tab control first
                if (editorTabs.ContainsKey(fullPath))
                {
                    var tabPage = editorTabs[fullPath];
                    tabControl.TabPages.Remove(tabPage);

                    // Properly clean up all resources for this tab
                    CleanupTabResources(fullPath);
                }
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

        /// <summary>
        /// Creates a PoisonTabPage with consistent properties for editor tabs
        /// </summary>
        private ReaLTaiizor.Controls.PoisonTabPage CreateTabPage(string tabText, string tooltipText)
        {
            // Use helper to create tab page with StyleManager, Theme, Style, and UseStyleColors configured
            var tabPage = ReaLTaiizorExt.PoisonControlHelper.CreateTabPage(tabText, styleManager, true);

            // Set custom properties
            tabPage.ToolTipText = tooltipText; // Show full absolute path in tooltip
            tabPage.UseVisualStyleBackColor = false;

            // Use helper to configure invisible scrollbars (AvalonEdit handles its own scrolling)
            ReaLTaiizorExt.PoisonControlHelper.ConfigureTabPageScrollbars(tabPage,
                showVertical: false,
                showHorizontal: false,
                verticalInvisible: true,
                horizontalInvisible: true);

            // Hide scrollbar corner control using Windows API (similar to PoisonPanel)
            tabPage.HandleCreated += (s, e) =>
            {
                if (tabPage.IsHandleCreated)
                    HideScrollbarCorner(tabPage);
            };
            tabPage.Layout += (s, e) =>
            {
                if (tabPage.IsHandleCreated)
                    HideScrollbarCorner(tabPage);
            };

            return tabPage;
        }

        #endregion

        #region Editor Management

        private void OpenFileInEditorWithContent(string filePath, string content)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            // Support both full absolute paths and relative paths
            string fullPath;
            if (Path.IsPathRooted(filePath))
            {
                // Already a full path
                fullPath = Path.GetFullPath(filePath);
            }
            else if (filePath.Contains(Path.DirectorySeparatorChar) || filePath.Contains(Path.AltDirectorySeparatorChar))
            {
                // Relative path - combine with project path
                fullPath = Path.GetFullPath(Path.Combine(projectPath, filePath));
            }
            else
            {
                // Simple filename - check scripts folder first, then search recursively
                fullPath = Path.Combine(projectPath, "scripts", filePath);
                if (!File.Exists(fullPath))
                {
                    // Search recursively for the file
                    string[] foundFiles = Directory.GetFiles(projectPath, filePath, SearchOption.AllDirectories);
                    if (foundFiles.Length > 0)
                    {
                        fullPath = Path.GetFullPath(foundFiles[0]);
                    }
                }
                else
                {
                    fullPath = Path.GetFullPath(fullPath);
                }
            }

            // Check if already open (use full path as key)
            if (editorTabs.ContainsKey(fullPath))
            {
                tabControl.SelectedTab = editorTabs[fullPath];
                return;
            }

            // Create new tab and editor
            string tabText = Path.GetFileName(fullPath);

            // Limit tab text length to prevent overlapping
            if (tabText.Length > MaxTabTextLength)
            {
                string nameWithoutExt = Path.GetFileNameWithoutExtension(tabText);
                string ext = Path.GetExtension(tabText);

                if (nameWithoutExt.Length > MaxTabTextLength - ext.Length - 3)
                {
                    nameWithoutExt = nameWithoutExt.Substring(0, MaxTabTextLength - ext.Length - 3) + "...";
                }
                tabText = nameWithoutExt + ext;
            }

            ReaLTaiizor.Controls.PoisonTabPage tabPage = CreateTabPage(tabText, fullPath);
            AvalonEditWrapper editor = CreateAvalonEditEditor();

            // Store original content in fileContents for comparison (use full path as key)
            fileContents[fullPath] = content;

            // Track changes (but ignore during file loading)
            EventHandler textChangedHandler = (s, e) =>
            {
                if (!isLoadingFiles)
                {
                    if (this.InvokeRequired)
                    {
                        // Use SafeInvoke for error handling (handles all exceptions internally)
                        ReaLTaiizorExt.PoisonFormHelper.SafeInvoke(this, () =>
                        {
                            if (!isLoadingFiles && !this.IsDisposed && !this.Disposing)
                            {
                                CheckForUnsavedChanges();
                                UpdateConditionalCompilationIndicators(editor);
                                UpdateSyntaxHighlightingIndicators(editor);
                            }
                        });
                    }
                    else
                    {
                        CheckForUnsavedChanges();
                        UpdateConditionalCompilationIndicators(editor);
                        UpdateSyntaxHighlightingIndicators(editor);
                    }
                }
            };

            isLoadingFiles = true;

            try
            {
                // Unsubscribe from TextChanged to prevent false change detection during loading
                editor.TextChanged -= textChangedHandler;

                // Load text with provided content (ensure content is not null)
                string contentToSet = content ?? "";

                // Set content and store in fileContents BEFORE setting up editor
                // This ensures fileContents matches what's in the editor
                fileContents[fullPath] = contentToSet;
                editor.Text = contentToSet;

                // Clear undo buffer to prevent false change detection
                editor.EmptyUndoBuffer();

                // Setup editor features
                SetupGSCSyntaxHighlighting(editor);
                DisableExtraMargins(editor);
                SetupConditionalCompilationIndicators(editor);
                UpdateSyntaxHighlightingIndicators(editor);

                // Clear undo buffer again after setup to ensure clean state
                editor.EmptyUndoBuffer();

                // Re-subscribe to TextChanged AFTER everything is set up
                editor.TextChanged += textChangedHandler;
            }
            finally
            {
                // Ensure isLoadingFiles is reset even if an error occurs
                isLoadingFiles = false;
            }

            // Add editor to tab BEFORE adding tab to tabControl
            // This ensures the editor is properly initialized when the tab becomes visible
            tabPage.Controls.Add(editor);

            // Store references BEFORE adding tab (needed for some operations) - use full path as key
            openEditors[fullPath] = editor;
            editorTabs[fullPath] = tabPage;

            // Add tab to tabControl
            tabControl.TabPages.Add(tabPage);
            if (tabControl.TabPages.Count == 1)
            {
                tabControl.SelectedTab = tabPage;
            }

            // Force editor to update/refresh after being added
            editor.Refresh();

            if (tabControl.SelectedTab == null)
            {
                currentFileName = fullPath;
                selectedTabItem = Path.GetFileName(fullPath);
            }

            // Update fileContents to match final editor state (after all setup)
            // This ensures CheckForUnsavedChanges won't detect false changes
            fileContents[fullPath] = editor.Text;

            // Update indicators
            UpdateConditionalCompilationIndicators(editor);
            UpdateSyntaxHighlightingIndicators(editor);

            // Ensure hasChanges is false after loading - check AFTER all setup is complete
            // Use BeginInvoke to ensure this runs after any queued TextChanged events
            this.BeginInvoke(new Action(() =>
            {
                if (!this.IsDisposed && !this.Disposing)
                {
                    CheckForUnsavedChanges();
                }
            }));
        }

        private void OpenFileInEditor(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            // Support both full absolute paths and relative paths
            string fullPath;
            if (Path.IsPathRooted(filePath))
            {
                // Already a full path
                fullPath = Path.GetFullPath(filePath);
            }
            else if (filePath.Contains(Path.DirectorySeparatorChar) || filePath.Contains(Path.AltDirectorySeparatorChar))
            {
                // Relative path - combine with project path
                fullPath = Path.GetFullPath(Path.Combine(projectPath, filePath));
            }
            else
            {
                // Simple filename - check scripts folder first, then search recursively
                fullPath = Path.Combine(projectPath, "scripts", filePath);
                if (!File.Exists(fullPath))
                {
                    // Search recursively for the file
                    string[] foundFiles = Directory.GetFiles(projectPath, filePath, SearchOption.AllDirectories);
                    if (foundFiles.Length > 0)
                    {
                        fullPath = Path.GetFullPath(foundFiles[0]);
                    }
                }
                else
                {
                    fullPath = Path.GetFullPath(fullPath);
                }
            }

            if (!File.Exists(fullPath))
                return;

            // Check if already open (use full path as key)
            if (editorTabs.ContainsKey(fullPath))
            {
                tabControl.SelectedTab = editorTabs[fullPath];
                return;
            }

            // Create new tab and editor
            // Use just the filename without path, and ensure it's not too long to prevent overlapping
            string tabText = Path.GetFileName(fullPath);

            // Limit tab text length to prevent overlapping (tabs need space for close button and padding)
            // Typical tab needs ~100-120px width for comfortable display
            if (tabText.Length > MaxTabTextLength)
            {
                string nameWithoutExt = Path.GetFileNameWithoutExtension(tabText);
                string ext = Path.GetExtension(tabText);

                // Truncate name part, keep extension
                if (nameWithoutExt.Length > MaxTabTextLength - ext.Length - 3)
                {
                    nameWithoutExt = nameWithoutExt.Substring(0, MaxTabTextLength - ext.Length - 3) + "...";
                }
                tabText = nameWithoutExt + ext;
            }

            ReaLTaiizor.Controls.PoisonTabPage tabPage = CreateTabPage(tabText, fullPath);
            AvalonEditWrapper editor = CreateAvalonEditEditor();

            // Load file content
            string content = File.ReadAllText(fullPath);

            // Store original content in fileContents for comparison (use full path as key)
            fileContents[fullPath] = content;

            // Track changes (but ignore during file loading)
            EventHandler textChangedHandler = (s, e) =>
            {
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
                                this.BeginInvoke(new Action(() =>
                                {
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
                // Unsubscribe from TextChanged to prevent false change detection during loading
                editor.TextChanged -= textChangedHandler;

                // Load text and store in fileContents BEFORE setting up editor
                // This ensures fileContents matches what's in the editor
                fileContents[fullPath] = content;
                editor.Text = content;

                // Clear undo buffer to prevent false change detection
                editor.EmptyUndoBuffer();

                // Setup syntax highlighting (this should not modify text, but clear buffer after)
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

            // Store references (use full path as key)
            openEditors[fullPath] = editor;
            editorTabs[fullPath] = tabPage;

            currentFileName = fullPath;
            selectedTabItem = Path.GetFileName(fullPath);

            // Update fileContents to match final editor state (after all setup)
            // This ensures CheckForUnsavedChanges won't detect false changes
            fileContents[fullPath] = editor.Text;

            // Update conditional compilation indicators after file is loaded
            UpdateConditionalCompilationIndicators(editor);

            // Update syntax highlighting indicators after file is loaded
            // Apply indicators AFTER lexer has finished coloring to ensure they override
            // In AvalonEdit, text markers should override syntax highlighting colors when applied correctly
            UpdateSyntaxHighlightingIndicators(editor);

            // Force AvalonEdit to refresh the display to ensure indicators are visible
            // This helps ensure that text markers properly override syntax highlighting colors
            // Also refresh scrollbars to prevent white scrollbar artifacts
            editor.Invalidate();
            editor.Update();
            editor.Refresh();

            // Ensure hasChanges is false after loading - check AFTER all setup is complete
            // Use BeginInvoke to ensure this runs after any queued TextChanged events
            this.BeginInvoke(new Action(() =>
            {
                if (!this.IsDisposed && !this.Disposing)
                {
                    CheckForUnsavedChanges();
                }
            }));
        }

        private AvalonEditWrapper CreateAvalonEditEditor()
        {
            AvalonEditWrapper editor = new AvalonEditWrapper
            {
                Dock = DockStyle.Fill
            };

            // Disable word wrap - use horizontal scrollbar instead
            editor.WordWrap = false;

            // Configure scrollbar settings for thumb-only display (no track, no arrows, just thumb)
            editor.UsePoisonScrollbarTheme = true;
            editor.ScrollbarThumbOpacity = 1.0;

            // Set StyleManager to enable Poison theming
            if (styleManager != null)
            {
                editor.StyleManager = styleManager;
            }
            else
            {
                // Fallback to default dark theme colors if no style manager - use PoisonPaint for consistency
                ThemeStyle fallbackTheme = ThemeStyle.Dark;
                Color editorBackColor = PoisonPaint.BackColor.Form(fallbackTheme);
                Color editorForeColor = PoisonPaint.ForeColor.Label.Normal(fallbackTheme);
                editor.SetColors(editorBackColor, editorForeColor);
            }

            // Load GSC syntax highlighting from embedded GSC.xshd resource
            editor.LoadGscSyntaxHighlighting();

            // Setup code completion
            SetupCodeCompletion(editor);

            // Setup context menu for line numbers
            SetupEditorContextMenu(editor);

            // Disable AvalonEdit's built-in SearchPanel - we use custom search dialog instead
            // Hook into editor to prevent Ctrl+F and F3 from opening SearchPanel
            editor.Editor.TextArea.PreviewKeyDown += (s, e) =>
            {
                // Intercept Ctrl+F to open our custom search dialog instead of SearchPanel
                if (e.Key == System.Windows.Input.Key.F &&
                    (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) != 0)
                {
                    e.Handled = true;
                    // Use BeginInvoke to ensure this runs on the UI thread
                    this.BeginInvoke(new Action(() =>
                    {
                        BtnSearch_Click(null, EventArgs.Empty);
                    }));
                    return;
                }

                // Handle F3 - Find Next across tabs
                if (e.Key == System.Windows.Input.Key.F3)
                {
                    // Check if there's selected text first
                    string selectedText = editor.Editor.SelectedText;
                    if (!string.IsNullOrEmpty(selectedText) && selectedText.Trim().Length > 0 && selectedText.Length < 100)
                    {
                        // Use selected text as search term
                        string trimmedText = selectedText.Trim();
                        lastSearchText = trimmedText;

                        // Trigger cross-tab search
                        e.Handled = true;
                        this.BeginInvoke(new Action(() =>
                        {
                            FindNext();
                        }));
                        return;
                    }

                    // Use last search text if available
                    if (!string.IsNullOrEmpty(lastSearchText))
                    {
                        e.Handled = true;
                        this.BeginInvoke(new Action(() =>
                        {
                            FindNext();
                        }));
                    }
                }
            };

            return editor;
        }

        /// <summary>
        /// Sets up the context menu for the editor (appears when right-clicking on line numbers or text)
        /// </summary>
        private void SetupEditorContextMenu(AvalonEditWrapper editor)
        {
            if (editor?.Editor == null)
                return;

            // Create context menu with useful items
            var contextMenu = new System.Windows.Controls.ContextMenu();

            // Apply Poison theme colors to WPF context menu
            if (styleManager != null)
            {
                // Get theme colors from Poison
                var backColor = PoisonPaint.BackColor.Form(styleManager.Theme);
                var foreColor = PoisonPaint.ForeColor.Label.Normal(styleManager.Theme);
                var borderColor = PoisonPaint.BorderColor.Button.Normal(styleManager.Theme);

                // Convert to WPF colors
                contextMenu.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(backColor.A, backColor.R, backColor.G, backColor.B));
                contextMenu.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(foreColor.A, foreColor.R, foreColor.G, foreColor.B));
                contextMenu.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(borderColor.A, borderColor.R, borderColor.G, borderColor.B));
                contextMenu.BorderThickness = new System.Windows.Thickness(1);
            }
            else
            {
                // Fallback to dark theme colors
                contextMenu.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(45, 45, 48));
                contextMenu.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(200, 200, 200));
                contextMenu.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(100, 100, 100));
                contextMenu.BorderThickness = new System.Windows.Thickness(1);
            }

            // Helper to create styled menu item - matches MainForm's PoisonContextMenuStrip styling
            System.Windows.Controls.MenuItem CreateMenuItem(string header, string inputGestureText, System.Windows.RoutedEventHandler clickHandler)
            {
                var menuItem = new System.Windows.Controls.MenuItem
                {
                    Header = header,
                    InputGestureText = inputGestureText,
                    Padding = new System.Windows.Thickness(8, 4, 8, 4) // Match WinForms menu item padding
                };
                menuItem.Click += clickHandler;

                // Style menu item with Poison theme colors - match MainForm's context menu
                if (styleManager != null)
                {
                    var itemBackColor = PoisonPaint.BackColor.Form(styleManager.Theme);
                    var itemForeColor = PoisonPaint.ForeColor.Button.Normal(styleManager.Theme);
                    // Use style color for hover (like MainForm's StyleBasedMenuRenderer does)
                    var styleColor = PoisonPaint.GetStyleColor(styleManager.Style);
                    var hoverColor = System.Windows.Media.Color.FromArgb(150,
                        (byte)styleColor.R, (byte)styleColor.G, (byte)styleColor.B); // 150 alpha like MainForm

                    menuItem.Background = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromArgb(itemBackColor.A, itemBackColor.R, itemBackColor.G, itemBackColor.B));
                    menuItem.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromArgb(itemForeColor.A, itemForeColor.R, itemForeColor.G, itemForeColor.B));

                    // Handle hover state - use style color with transparency (matches MainForm)
                    menuItem.MouseEnter += (s, e) =>
                    {
                        menuItem.Background = new System.Windows.Media.SolidColorBrush(hoverColor);
                    };
                    menuItem.MouseLeave += (s, e) =>
                    {
                        menuItem.Background = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromArgb(itemBackColor.A, itemBackColor.R, itemBackColor.G, itemBackColor.B));
                    };
                }
                else
                {
                    // Fallback dark theme colors
                    menuItem.Background = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(45, 45, 48));
                    menuItem.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(200, 200, 200));

                    menuItem.MouseEnter += (s, e) =>
                    {
                        menuItem.Background = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromArgb(150, 0, 122, 204)); // Default blue hover
                    };
                    menuItem.MouseLeave += (s, e) =>
                    {
                        menuItem.Background = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(45, 45, 48));
                    };
                }

                return menuItem;
            }

            // Go to Line
            var goToLineItem = CreateMenuItem("Go to Line...", "Ctrl+G", (s, e) => GoToLine());
            contextMenu.Items.Add(goToLineItem);

            // Separator - style it to match Poison theme
            var separator = new System.Windows.Controls.Separator();
            if (styleManager != null)
            {
                var separatorColor = PoisonPaint.BorderColor.Button.Normal(styleManager.Theme);
                separator.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(separatorColor.A, separatorColor.R, separatorColor.G, separatorColor.B));
            }
            else
            {
                separator.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(100, 100, 100));
            }
            contextMenu.Items.Add(separator);

            // Toggle ifdef - find the symbol on the current line and toggle it
            var toggleIfdefItem = CreateMenuItem("Toggle ifdef", null, (s, e) => ToggleIfdefAtCurrentLine(editor));
            contextMenu.Items.Add(toggleIfdefItem);

            // Set context menu on the text editor
            editor.Editor.TextArea.ContextMenu = contextMenu;

            // Store reference to update theme later
            editor.Editor.TextArea.ContextMenu.Tag = editor; // Store editor reference for theme updates

            // Also set context menu on the ElementHost to ensure right-click works everywhere
            if (editor.Controls.Count > 0 && editor.Controls[0] is System.Windows.Forms.Integration.ElementHost elementHost)
            {
                // Create WinForms context menu for the ElementHost wrapper
                var winFormsContextMenu = new ReaLTaiizor.Controls.PoisonContextMenuStrip(this.components);
                // Use helper to apply StyleManager - handles Theme, Style automatically
                if (styleManager != null)
                {
                    ReaLTaiizorExt.PoisonControlHelper.ApplyStyleManager(winFormsContextMenu, styleManager);
                }

                var goToLineWinFormsItem = new System.Windows.Forms.ToolStripMenuItem("Go to Line...");
                goToLineWinFormsItem.Click += (s, e) => GoToLine();
                winFormsContextMenu.Items.Add(goToLineWinFormsItem);

                winFormsContextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

                var toggleIfdefWinFormsItem = new System.Windows.Forms.ToolStripMenuItem("Toggle ifdef");
                toggleIfdefWinFormsItem.Click += (s, e) => ToggleIfdefAtCurrentLine(editor);
                winFormsContextMenu.Items.Add(toggleIfdefWinFormsItem);

                elementHost.ContextMenuStrip = winFormsContextMenu;
            }
        }

        /// <summary>
        /// Toggles the ifdef symbol at the current line
        /// </summary>
        private void ToggleIfdefAtCurrentLine(AvalonEditWrapper editor)
        {
            if (editor?.Editor == null)
                return;

            int currentLine = editor.CurrentLine;
            if (currentLine < 1 || currentLine > editor.LineCount)
                return;

            string lineText = editor.GetLineText(currentLine);
            if (string.IsNullOrWhiteSpace(lineText))
                return;

            // Find #ifdef or #ifndef on this line
            var ifdefMatch = System.Text.RegularExpressions.Regex.Match(lineText, @"#ifdef\s+(\w+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var ifndefMatch = System.Text.RegularExpressions.Regex.Match(lineText, @"#ifndef\s+(\w+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            string symbol = null;
            if (ifdefMatch.Success)
            {
                symbol = ifdefMatch.Groups[1].Value;
            }
            else if (ifndefMatch.Success)
            {
                symbol = ifndefMatch.Groups[1].Value;
            }

            if (string.IsNullOrEmpty(symbol))
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, "No #ifdef or #ifndef found on this line.", "Toggle ifdef", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Toggle the symbol in customSymbolStates
            string upperSymbol = symbol.ToUpper();

            // Skip game mode symbols, namespace, and constant definitions (they can't be toggled)
            bool isNamespace = !string.IsNullOrEmpty(projectNamespace) &&
                               upperSymbol.Equals(projectNamespace.ToUpper(), StringComparison.OrdinalIgnoreCase);
            if (upperSymbol == "MP" || upperSymbol == "ZM" || upperSymbol == "SP" ||
                upperSymbol == "BO3" || upperSymbol == "BO4" || isNamespace)
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, $"Cannot toggle system symbol: {symbol}", "Toggle ifdef", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Toggle the symbol state
            bool currentState = customSymbolStates.ContainsKey(upperSymbol) && customSymbolStates[upperSymbol];
            customSymbolStates[upperSymbol] = !currentState;

            // Save symbol changes to gsc.conf
            SaveModeToGscConf();

            // Update the symbols menu to reflect the new state
            UpdateSymbolsMenu();

            // Update conditional compilation indicators for all editors
            foreach (var kvp in openEditors)
            {
                UpdateConditionalCompilationIndicators(kvp.Value);
            }

            // Show feedback
            string status = !currentState ? "enabled" : "disabled";
            ReaLTaiizor.Controls.PoisonMessageBox.Show(this, $"Symbol '{symbol}' is now {status}.", "Toggle ifdef", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Store completion list as a field so it's accessible in the event handler
        private string gscCompletionList = null;

        private void DisableExtraMargins(AvalonEditWrapper editor)
        {
            // AvalonEdit has built-in line numbers - no need for separate margin configuration
            // This method is kept for compatibility but does nothing for AvalonEdit
        }

        private void SetupCodeCompletion(AvalonEditWrapper editor)
        {
            // AvalonEdit has built-in code completion support
            // We'll set it up to use GSC keywords from GSC.xshd
            if (editor?.Editor == null)
                return;

            // Build completion list from GSC keywords and built-in functions
            if (gscCompletionList == null)
            {
                gscCompletionList = BuildCompletionList();
            }

            // Setup AvalonEdit completion window
            // Note: AvalonEdit has built-in completion support, but we'll use a simplified approach
            // For now, completion is handled by AvalonEdit's built-in mechanisms
            // This can be enhanced later with custom completion data if needed
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
        /// Loads GSC syntax data from embedded GSC.xshd resource
        /// </summary>
        private static void LoadGscSyntaxData()
        {
            if (gscSyntaxData != null)
                return; // Already loaded

            try
            {
                using (Stream xshdStream = Helpers.GscSyntaxParser.GetGscXshdStream())
                {
                    if (xshdStream != null)
                    {
                        gscSyntaxData = Helpers.GscSyntaxParser.ParseGscXshd(xshdStream);
                    }
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

        private void SetupGSCSyntaxHighlighting(AvalonEditWrapper editor)
        {
            // AvalonEdit loads syntax highlighting directly from GSC.xshd
            // This is already done in CreateAvalonEditEditor, so this method
            // just ensures it's loaded and updates conditional compilation highlighting
            if (editor?.Editor == null)
                return;

            // Syntax highlighting is already loaded from GSC.xshd in CreateAvalonEditEditor
            // We just need to update conditional compilation indicators if needed
            // (This will be handled by UpdateConditionalCompilationIndicators)
        }

        private void SetupSyntaxHighlightingIndicators(AvalonEditWrapper editor)
        {
            // AvalonEdit handles syntax highlighting directly from GSC.xshd
            // Include paths and method calls are already highlighted by GSC.xshd rules
            // This method is kept for compatibility but does nothing for AvalonEdit
        }

        private void UpdateSyntaxHighlightingIndicators(AvalonEditWrapper editor)
        {
        }

        private void SetupConditionalCompilationIndicators(AvalonEditWrapper editor)
        {
        }

        private void UpdateConditionalCompilationIndicators()
        {
            // Ensure we're on the UI thread
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateConditionalCompilationIndicators()));
                return;
            }

            // Update indicators for all open editors
            foreach (var editor in openEditors.Values)
            {
                if (editor != null)
                {
                    UpdateConditionalCompilationIndicators(editor);
                }
            }
        }

        private void UpdateConditionalCompilationIndicators(AvalonEditWrapper editor)
        {
            if (editor == null || editor.Editor == null) return;

            // Clear all existing markers
            editor.ClearAllMarkers();

            // Get active symbols from current game mode and gsc.conf
            HashSet<string> activeSymbols = GetActiveSymbols();

            // Parse conditional compilation blocks
            string text = editor.Text;
            if (string.IsNullOrEmpty(text))
                return;

            List<CodeBlock> blocks = ParseConditionalBlocks(text);

            // Apply markers to inactive blocks
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
                            // Get the line's start position (character offset)
                            int lineStartPos = editor.Lines[block.StartLine].Position;

                            // Calculate character positions for the content between #ifdef and #endif
                            int ifdefEndChar = ifdefMatch.Index + ifdefMatch.Length;
                            int endifStartChar = endifMatch.Index;

                            // Highlight the content between #ifdef and #endif
                            int startPos = lineStartPos + ifdefEndChar;
                            int length = endifStartChar - ifdefEndChar;

                            if (length > 0)
                            {
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

                        // Apply marker to inactive code
                        editor.IndicatorFillRange(startPos, endPos - startPos);
                    }
                }
            }

            // Force redraw to show markers
            if (editor.Editor?.TextArea?.TextView != null)
            {
                editor.Editor.TextArea.TextView.Redraw();
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

            // Add namespace symbol (if found in source files)
            if (!string.IsNullOrEmpty(projectNamespace))
            {
                symbols.Add(projectNamespace.ToUpper());
            }

            // Add custom symbols that are enabled
            // Symbols work independently UNLESS they are inside a mode-specific ifdef (MP, ZM, SP)
            // If a symbol is inside a mode ifdef, it requires that mode to be active
            foreach (var kvp in customSymbolStates)
            {
                if (kvp.Value) // If symbol is enabled
                {
                    string symbol = kvp.Key.ToUpper();

                    // Check if this symbol is inside a mode-specific ifdef block
                    if (symbolModeConditions.ContainsKey(symbol) && symbolModeConditions[symbol].Count > 0)
                    {
                        // Symbol is inside one or more mode ifdefs - check if current mode matches
                        string currentMode = currentGameModeStr.ToUpper();
                        bool modeMatches = symbolModeConditions[symbol].Contains(currentMode);

                        if (modeMatches)
                        {
                            // Current mode matches one of the modes this symbol is inside - symbol is active
                            symbols.Add(symbol);
                        }
                        // If mode doesn't match, symbol is not active (even though it's enabled)
                    }
                    else
                    {
                        // Symbol is not inside any mode-specific ifdef - it's active if enabled
                        symbols.Add(symbol);
                    }
                }
            }

            return symbols;
        }
        // Parent conditions are still tracked for reference but don't affect symbol activation

        // Track which symbols are defined inside which #ifdef blocks
        private Dictionary<string, List<string>> symbolParentConditions = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        // Track which symbols are inside mode-specific ifdef blocks (MP, ZM, SP)
        // Key: symbol name, Value: list of mode symbols (MP, ZM, SP) that this symbol appears inside
        private Dictionary<string, List<string>> symbolModeConditions = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Public method to reload symbols from gsc.conf (called when GSC Configuration Editor saves)
        /// </summary>
        public void ReloadSymbolsFromGscConf()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(ReloadSymbolsFromGscConf));
                return;
            }

            LoadSymbolsFromGscConf();
            UpdateSymbolsMenu();
            UpdateModeMenu();

            // Update conditional compilation indicators for all open editors
            foreach (var kvp in openEditors)
            {
                UpdateConditionalCompilationIndicators(kvp.Value);
            }
        }

        /// <summary>
        /// Loads available symbols from gsc.conf and scans project files for symbols in #ifdef blocks
        /// Also reads and sets the game mode from gsc.conf
        /// </summary>
        private void LoadSymbolsFromGscConf()
        {
            customSymbolStates.Clear();
            availableCustomSymbols.Clear();
            symbolParentConditions.Clear();
            symbolModeConditions.Clear();

            if (string.IsNullOrEmpty(projectPath) || !Directory.Exists(projectPath))
                return;

            // First, load symbols from gsc.conf
            string gscConfPath = Path.Combine(projectPath, "gsc.conf");
            if (File.Exists(gscConfPath))
            {
                try
                {
                    // Use FileShare.ReadWrite to allow other processes to read/write while we read
                    string[] lines;
                    using (var fileStream = new FileStream(gscConfPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(fileStream))
                    {
                        lines = reader.ReadToEnd().Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                    }
                    foreach (string line in lines)
                    {
                        // Skip empty lines and comments
                        string trimmedLine = line.Trim();
                        if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#") || trimmedLine.StartsWith("//"))
                            continue;

                        if (trimmedLine.StartsWith("symbols=", StringComparison.OrdinalIgnoreCase))
                        {
                            // Extract symbols value (handle both "symbols=" and "symbols =")
                            int equalsIndex = trimmedLine.IndexOf('=');
                            if (equalsIndex < 0) continue;

                            string symbolsValue = trimmedLine.Substring(equalsIndex + 1).Trim();
                            foreach (string symbol in symbolsValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
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
                                    // Track custom symbols (not game mode, game, or namespace)
                                    else if (string.IsNullOrEmpty(projectNamespace) ||
                                             !upperSymbol.Equals(projectNamespace.ToUpper(), StringComparison.OrdinalIgnoreCase))
                                    {
                                        // Use case-insensitive comparison for checking
                                        // Find existing symbol (case-insensitive) to preserve its exact casing
                                        string existingSymbol = availableCustomSymbols.FirstOrDefault(s => s.Equals(upperSymbol, StringComparison.OrdinalIgnoreCase));

                                        if (existingSymbol == null)
                                        {
                                            // Symbol doesn't exist, add it
                                            availableCustomSymbols.Add(upperSymbol);
                                            // Symbols in gsc.conf are enabled by default
                                            customSymbolStates[upperSymbol] = true;
                                        }
                                        else
                                        {
                                            // Symbol already exists (from previous load or scan), preserve its state
                                            // Use uppercase key consistently (customSymbolStates uses case-insensitive comparison)
                                            // Only set to true if it's not already set (don't overwrite if it was disabled)
                                            if (!customSymbolStates.ContainsKey(upperSymbol))
                                            {
                                                customSymbolStates[upperSymbol] = true;
                                            }
                                            // If it already exists in customSymbolStates, preserve its current state
                                            // Note: We use upperSymbol (uppercase) for the key to ensure consistency
                                        }
                                    }
                                }
                            }
                            break; // Only process first symbols= line
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't show prompt - silently fail to avoid interrupting user
                    System.Diagnostics.Debug.WriteLine($"Error loading gsc.conf: {ex.Message}");
                }
            }

            // Read namespace from source files
            ReadNamespaceFromSourceFiles();

            // Scan project files for symbols in #ifdef blocks
            ScanProjectForSymbols();

            // Update the symbols menu
            UpdateSymbolsMenu();

            // Update the mode menu to reflect the mode from gsc.conf
            UpdateModeMenu();
        }

        /// <summary>
        /// Reads the namespace from source files by scanning for #namespace directives
        /// </summary>
        private void ReadNamespaceFromSourceFiles()
        {
            projectNamespace = string.Empty;

            if (string.IsNullOrEmpty(projectPath) || !Directory.Exists(projectPath))
                return;

            try
            {
                // Regex pattern to match #namespace directives (case-insensitive)
                Regex namespaceRegex = new Regex(@"#namespace\s+(\w+)\s*;", RegexOptions.IgnoreCase);

                // Scan all .gsc and .csc files in the project
                foreach (string gscFile in Directory.GetFiles(projectPath, "*.*", SearchOption.AllDirectories)
                    .Where(f => f.EndsWith(".gsc", StringComparison.OrdinalIgnoreCase) ||
                                f.EndsWith(".csc", StringComparison.OrdinalIgnoreCase)))
                {
                    try
                    {
                        string content = File.ReadAllText(gscFile);
                        Match match = namespaceRegex.Match(content);
                        if (match.Success && match.Groups.Count > 1)
                        {
                            string nsName = match.Groups[1].Value.Trim();
                            if (!string.IsNullOrEmpty(nsName))
                            {
                                // Store the namespace as found (preserve original case)
                                projectNamespace = nsName;
                                break; // Use first namespace found
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
                // Silently fail - namespace will remain empty
            }
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
                // Clear old symbol data for fresh scan when switching projects
                availableCustomSymbols.Clear();
                symbolParentConditions.Clear();
                symbolModeConditions.Clear();

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
                        HashSet<string> currentModeParents = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // Track which mode symbols (MP, ZM, SP) are in the stack

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
                                    // Exclude game mode symbols, namespace, and constant definitions
                                    bool isNamespace = !string.IsNullOrEmpty(projectNamespace) &&
                                                       symbol.Equals(projectNamespace.ToUpper(), StringComparison.OrdinalIgnoreCase);
                                    if (symbol != "MP" && symbol != "ZM" && symbol != "SP" &&
                                        symbol != "BO3" && symbol != "BO4" && !isNamespace &&
                                        !constantDefines.Contains(symbol))
                                    {
                                        // Add symbol to available list (case-insensitive check)
                                        // Find existing symbol (case-insensitive) to preserve its exact casing
                                        string existingSymbol = availableCustomSymbols.FirstOrDefault(s => s.Equals(symbol, StringComparison.OrdinalIgnoreCase));

                                        if (existingSymbol == null)
                                        {
                                            // Symbol doesn't exist, add it
                                            availableCustomSymbols.Add(symbol);
                                            // Initialize symbol state if not already set (from gsc.conf)
                                            // Don't overwrite existing state - only set if it doesn't exist
                                            if (!customSymbolStates.ContainsKey(symbol))
                                            {
                                                customSymbolStates[symbol] = false; // Default to disabled if not in gsc.conf
                                            }
                                        }
                                        else
                                        {
                                            // Symbol already exists (from gsc.conf), use the existing key for state lookup
                                            // Don't modify customSymbolStates - preserve whatever state was set from gsc.conf
                                            // The state is already set correctly from gsc.conf loading
                                        }

                                        // Track parent conditions (nested #ifdef blocks)
                                        // A symbol is only active if all its parent conditions are met
                                        if (!symbolParentConditions.ContainsKey(symbol))
                                        {
                                            symbolParentConditions[symbol] = new List<string>();
                                        }

                                        // Track mode-specific conditions (if symbol is inside MP, ZM, or SP ifdef)
                                        if (!symbolModeConditions.ContainsKey(symbol))
                                        {
                                            symbolModeConditions[symbol] = new List<string>();
                                        }

                                        // Add current parent conditions to this symbol
                                        // Only add parents that are not already in the list
                                        foreach (var parent in ifdefStack)
                                        {
                                            if (!symbolParentConditions[symbol].Contains(parent.Key))
                                            {
                                                symbolParentConditions[symbol].Add(parent.Key);
                                            }

                                            // If parent is a mode symbol (MP, ZM, SP), track it separately
                                            if (parent.Key == "MP" || parent.Key == "ZM" || parent.Key == "SP")
                                            {
                                                if (!symbolModeConditions[symbol].Contains(parent.Key))
                                                {
                                                    symbolModeConditions[symbol].Add(parent.Key);
                                                }
                                            }
                                        }

                                        // Also check currentModeParents (modes in the stack)
                                        foreach (string modeParent in currentModeParents)
                                        {
                                            if (!symbolModeConditions[symbol].Contains(modeParent))
                                            {
                                                symbolModeConditions[symbol].Add(modeParent);
                                            }
                                        }

                                        // Push this symbol onto the stack (it's now a parent for nested blocks)
                                        ifdefStack.Push(new KeyValuePair<string, bool>(symbol, isIfndef));

                                        // If this symbol is a mode symbol, add it to currentModeParents
                                        if (symbol == "MP" || symbol == "ZM" || symbol == "SP")
                                        {
                                            currentModeParents.Add(symbol);
                                        }
                                    }
                                }
                            }

                            // Check for #endif to pop from stack
                            if (Regex.IsMatch(line, @"^\s*#endif\b", RegexOptions.IgnoreCase))
                            {
                                if (ifdefStack.Count > 0)
                                {
                                    var popped = ifdefStack.Pop();
                                    // If we popped a mode symbol, remove it from currentModeParents
                                    if (popped.Key == "MP" || popped.Key == "ZM" || popped.Key == "SP")
                                    {
                                        currentModeParents.Remove(popped.Key);
                                    }
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
                // Use case-insensitive lookup for consistency
                string upperSymbol = symbol.ToUpper();
                symbolItem.Checked = customSymbolStates.ContainsKey(upperSymbol) && customSymbolStates[upperSymbol];
                symbolItem.Tag = upperSymbol; // Store uppercase version for consistency
                symbolItem.Click += (s, e) =>
                {
                    ToolStripMenuItem item = s as ToolStripMenuItem;
                    if (item != null && item.Tag != null)
                    {
                        string sym = item.Tag.ToString().ToUpper(); // Ensure uppercase
                        customSymbolStates[sym] = item.Checked;
                        // Save symbol changes to gsc.conf
                        SaveModeToGscConf();
                        // Force immediate update on UI thread
                        if (this.InvokeRequired)
                        {
                            this.BeginInvoke(new Action(() => UpdateConditionalCompilationIndicators()));
                        }
                        else
                        {
                            UpdateConditionalCompilationIndicators();
                        }
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
                    // Special case: namespace is always active, so #ifndef namespace blocks should always be active (not grayed out)
                    bool isActive;
                    if (!string.IsNullOrEmpty(projectNamespace) &&
                        symbol.Equals(projectNamespace.ToUpper(), StringComparison.OrdinalIgnoreCase))
                    {
                        isActive = true; // Always active - don't gray out
                    }
                    else
                    {
                        // For #ifndef: blank when symbol is disabled, unblank when symbol is enabled
                        // This matches user expectation: if XBOX is disabled, #ifndef XBOX should be blanked
                        isActive = activeSymbols.Contains(symbol);
                    }

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
                    // Only add if inactive (we only need to grey out inactive code)
                    if (ifBlock.EndLine >= ifBlock.StartLine && !ifBlock.IsActive)
                    {
                        blocks.Add(ifBlock);
                    }

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

                    // Only add if there's actual content AND it's inactive (we only grey out inactive code)
                    if (block.EndLine >= block.StartLine && !block.IsActive)
                    {
                        blocks.Add(block);
                    }
                }
            }

            return blocks;
        }

        private AvalonEditWrapper GetCurrentEditor()
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


        private void HandleTabControlMouseWheel(MouseEventArgs e)
        {
            if (tabControl == null || tabControl.TabPages.Count == 0)
                return;

            // Check if tabs are overflowing (selector would be visible)
            // We can detect this by checking if all tabs fit in the visible area
            bool tabsOverflowing = AreTabsOverflowing();

            if (tabsOverflowing)
            {
                // When tabs overflow, let the base TabControl handle scrolling
                // Don't interfere - it will scroll tabs horizontally automatically
                return;
            }

            // Throttle tab scrolling to prevent rapid changes
            DateTime now = DateTime.Now;
            if ((now - lastTabScrollTime).TotalMilliseconds < TabScrollThrottleMs)
                return; // Too soon since last change, ignore this scroll event

            // Scroll through tabs with mouse wheel (change selected tab)
            int currentIndex = tabControl.SelectedIndex;
            if (currentIndex < 0)
                currentIndex = 0;

            int newIndex = currentIndex;

            if (e.Delta > 0)
            {
                // Scroll up - go to previous tab
                if (currentIndex > 0)
                {
                    newIndex = currentIndex - 1;
                }
                else
                {
                    // Wrap to last tab
                    newIndex = tabControl.TabPages.Count - 1;
                }
            }
            else if (e.Delta < 0)
            {
                // Scroll down - go to next tab
                if (currentIndex < tabControl.TabPages.Count - 1)
                {
                    newIndex = currentIndex + 1;
                }
                else
                {
                    // Wrap to first tab
                    newIndex = 0;
                }
            }

            // Only change if index actually changed
            if (newIndex != currentIndex)
            {
                // Change tab immediately (throttling already handled above)
                tabControl.SelectedIndex = newIndex;
                lastTabScrollTime = now;
            }
        }

        // Check if tabs are overflowing (would show selector)
        // This is a simple heuristic: if the total width of all tabs exceeds the control width
        private bool AreTabsOverflowing()
        {
            if (tabControl == null || tabControl.TabPages.Count == 0 || !tabControl.IsHandleCreated)
                return false;

            try
            {
                // Get the width of the tab control's display area
                int controlWidth = tabControl.DisplayRectangle.Width;

                // Estimate total width of all tabs
                int totalTabWidth = 0;
                for (int i = 0; i < tabControl.TabPages.Count; i++)
                {
                    Rectangle tabRect = tabControl.GetTabRect(i);
                    totalTabWidth += tabRect.Width;
                }

                // If total width exceeds control width, tabs are likely overflowing
                return totalTabWidth > controlWidth;
            }
            catch
            {
                // If we can't determine, assume not overflowing
                return false;
            }
        }

        private void TabControl_MouseWheel(object sender, MouseEventArgs e)
        {
            if (tabControl == null || tabControl.TabPages.Count == 0)
                return;

            // Check if tabs are overflowing
            if (AreTabsOverflowing())
            {
                // When tabs overflow, scroll tabs horizontally by changing selected index
                // No throttling needed - allow continuous scrolling
                int currentIndex = tabControl.SelectedIndex;
                if (currentIndex < 0 && tabControl.TabPages.Count > 0)
                    currentIndex = 0;

                int scrollDirection = e.Delta > 0 ? -1 : 1;
                int newIndex = currentIndex + scrollDirection;

                if (newIndex >= 0 && newIndex < tabControl.TabPages.Count)
                {
                    tabControl.SelectedIndex = newIndex;
                }
                else if (newIndex < 0 && tabControl.TabPages.Count > 0)
                {
                    tabControl.SelectedIndex = tabControl.TabPages.Count - 1;
                }
                else if (newIndex >= tabControl.TabPages.Count && tabControl.TabPages.Count > 0)
                {
                    tabControl.SelectedIndex = 0;
                }
                return;
            }

            // When tabs don't overflow, handle tab selection
            // No throttling - allow continuous scrolling
            int currentIdx = tabControl.SelectedIndex;
            if (currentIdx < 0)
                currentIdx = 0;

            int newIdx = currentIdx;

            if (e.Delta > 0)
            {
                // Scroll up - go to previous tab
                if (currentIdx > 0)
                {
                    newIdx = currentIdx - 1;
                }
                else
                {
                    // Wrap to last tab
                    newIdx = tabControl.TabPages.Count - 1;
                }
            }
            else if (e.Delta < 0)
            {
                // Scroll down - go to next tab
                if (currentIdx < tabControl.TabPages.Count - 1)
                {
                    newIdx = currentIdx + 1;
                }
                else
                {
                    // Wrap to first tab
                    newIdx = 0;
                }
            }

            // Only change if index actually changed
            if (newIdx != currentIdx)
            {
                tabControl.SelectedIndex = newIdx;
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            // Check if mouse is over tab control area - if so, handle tab scrolling
            Point mousePos = this.PointToClient(Control.MousePosition);
            if (tabControl != null && tabControl.Bounds.Contains(mousePos))
            {
                // Get the tab control's client coordinates
                Point tabControlMousePos = tabControl.PointToClient(Control.MousePosition);

                // Check if mouse is in the tab area (top ~30 pixels) or if Ctrl key is held
                // This allows scrolling tabs even when mouse is over content if Ctrl is held
                bool isOverTabArea = tabControlMousePos.Y < 30;
                bool ctrlHeld = (Control.ModifierKeys & Keys.Control) != 0;

                if (isOverTabArea || ctrlHeld)
                {
                    // The TabControl_MouseWheel handler will handle it
                    // Just let the event reach the TabControl
                    base.OnMouseWheel(e);
                    return;
                }
            }

            base.OnMouseWheel(e);
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

            AvalonEditWrapper editor = GetCurrentEditor();
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

        /// <summary>
        /// Checks if the game is currently running
        /// </summary>

        #endregion

        #region Default Project Creation

        private void CreateDefaultProjectOnStartup()
        {
            try
            {
                string gameSubfolder = currentGame == TreyarchCompiler.Enums.Games.T7 ? "T7" : "T8";

                // Create default project in the exe directory as "defaultproject"
                string guiPath = GetGuiPath();
                string defaultProjectBasePath = Path.Combine(guiPath, "defaultproject");
                string defaultProjectPath = Path.Combine(defaultProjectBasePath, gameSubfolder);

                // Check if the defaultproject folder exists (from installer or build)
                if (Directory.Exists(defaultProjectPath))
                {
                    // Use the existing defaultproject from installer or build
                    OpenFolder(defaultProjectPath);
                    folderOpened = true;
                    return;
                }

                // Get default template files path
                string defaultsPath = GetDefaultsPath();

                // Create it using the template files
                string scriptsPath = Path.Combine(defaultProjectPath, "scripts");
                string functionsPath = Path.Combine(scriptsPath, "functions");

                // Create directories
                Directory.CreateDirectory(defaultProjectPath);
                Directory.CreateDirectory(scriptsPath);
                Directory.CreateDirectory(functionsPath);

                if (currentGame == TreyarchCompiler.Enums.Games.T7)
                {
                    // For T7, use multiple template files
                    CreateFileFromTemplate(defaultsPath, "defaultt7project.main", Path.Combine(scriptsPath, "main.gsc"), GetDefaultMainContent());
                    CreateFileFromTemplate(defaultsPath, "defaultt7project.options", Path.Combine(scriptsPath, "options.gsc"), "");
                    CreateFileFromTemplate(defaultsPath, "defaultt7project.util", Path.Combine(scriptsPath, "util.gsc"), "");
                    CreateFileFromTemplate(defaultsPath, "defaultt7project._util", Path.Combine(functionsPath, "_util.gsc"), "");
                    CreateFileFromTemplate(defaultsPath, "defaultt7project.misc", Path.Combine(functionsPath, "misc.gsc"), "");
                    CreateFileFromTemplate(defaultsPath, "defaultt7project.stats", Path.Combine(functionsPath, "stats.gsc"), "");
                    CreateFileFromTemplate(defaultsPath, "defaultt7project.zombies_only", Path.Combine(functionsPath, "zombies_only.gsc"), "");
                }
                else
                {
                    // For T8, use main and headers files
                    CreateFileFromTemplate(defaultsPath, "defaultt8project.main", Path.Combine(scriptsPath, "main.gsc"), GetDefaultMainContent());
                    CreateFileFromTemplate(defaultsPath, "defaultt8project.headers", Path.Combine(scriptsPath, "headers.gsc"), GetDefaultHeadersContent());
                }

                // Create gsc.conf if it doesn't exist
                string gscConfPath = Path.Combine(defaultProjectPath, "gsc.conf");
                if (!File.Exists(gscConfPath))
                {
                    string gameSymbol = currentGame == TreyarchCompiler.Enums.Games.T7 ? "bo3" : "bo4";
                    string gameModeLower = currentGameModeStr.ToLower();
                    // Read namespace from source files if available
                    string namespaceSymbol = !string.IsNullOrEmpty(projectNamespace) ? projectNamespace : "";
                    string symbolsLine = string.IsNullOrEmpty(namespaceSymbol)
                        ? $"symbols={gameSymbol},{gameModeLower}"
                        : $"symbols={gameSymbol},{namespaceSymbol},{gameModeLower}";
                    File.WriteAllText(gscConfPath, symbolsLine);
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

        /// <summary>
        /// Checks if a project is an IL (Infinity Loader) project by looking for main.gsc and specific IL markers
        /// Checks for: system::register("infinityloader", ::init, undefined, undefined) inside init() function
        /// Also checks namespace - if it's NOT "duplicate_render", it's an IL project
        /// Searches all subfolders for main.gsc (same as RefreshFileList does)
        /// </summary>
        /// <param name="projectPath">Path to the project folder</param>
        /// <param name="errorDetails">Output parameter containing detailed error information if not an IL project</param>
        /// <returns>True if the project appears to be an IL project, false otherwise</returns>
        private bool IsILProject(string projectPath, out string errorDetails)
        {
            errorDetails = string.Empty;
            StringBuilder errorInfo = new StringBuilder();

            try
            {
                // Step 1: Find main.gsc file - search recursively in all subfolders (same as RefreshFileList)
                string[] mainGscFiles = Directory.GetFiles(projectPath, "main.gsc", SearchOption.AllDirectories);
                if (mainGscFiles.Length == 0)
                {
                    errorInfo.AppendLine("✗ main.gsc file not found in project or any subfolders");
                    errorDetails = errorInfo.ToString();
                    return false;
                }

                // Use the first main.gsc found (typically there should only be one)
                string mainGscPath = mainGscFiles[0];
                string relativePath = mainGscPath.Substring(projectPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                errorInfo.AppendLine($"✓ Found main.gsc: {relativePath}");

                // Step 2: Read and analyze main.gsc
                string content = File.ReadAllText(mainGscPath);
                string lowerContent = content.ToLower();

                // Step 3: Check namespace (for informational purposes only - not used for detection)
                // Namespace alone is not a reliable indicator - we require both system::register and #ifdef IL
                bool foundNamespace = false;
                string namespaceValue = string.Empty;

                // Look for #namespace directive
                System.Text.RegularExpressions.Regex namespaceRegex = new System.Text.RegularExpressions.Regex(@"#namespace\s+(\w+)\s*;", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                System.Text.RegularExpressions.Match namespaceMatch = namespaceRegex.Match(content);
                if (namespaceMatch.Success)
                {
                    namespaceValue = namespaceMatch.Groups[1].Value;
                    foundNamespace = true;
                    errorInfo.AppendLine($"✓ Found namespace: {namespaceValue}");
                }
                else
                {
                    errorInfo.AppendLine("✗ No #namespace directive found");
                }

                // Step 4: Check for BOTH system::register("infinityloader" AND #ifdef IL (both required)
                // The new syntax requires both indicators to be present
                bool foundSystemRegister = false;
                bool foundIfdefIL = false;

                // Look for system::register("infinityloader" anywhere in the file
                // Pattern: system::register("infinityloader" (with flexible spacing)
                System.Text.RegularExpressions.Regex registerRegex = new System.Text.RegularExpressions.Regex(
                    @"system\s*::\s*register\s*\(\s*[""']infinityloader[""']",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (registerRegex.IsMatch(content))
                {
                    foundSystemRegister = true;
                    errorInfo.AppendLine("✓ Found system::register(\"infinityloader\" in file");
                }
                else
                {
                    errorInfo.AppendLine("✗ system::register(\"infinityloader\" not found in file");
                }

                // Check for #ifdef IL directives (new IL syntax indicator)
                // Check for both "#ifdef IL" and "#ifdef\tIL" (with tab)
                if (lowerContent.IndexOf("#ifdef il", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    lowerContent.IndexOf("#ifdef\til", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    foundIfdefIL = true;
                    errorInfo.AppendLine("✓ Found #ifdef IL directive in file");
                }
                else
                {
                    errorInfo.AppendLine("✗ #ifdef IL directive not found in file");
                }

                // BOTH indicators must be present for it to be an IL project
                if (foundSystemRegister && foundIfdefIL)
                {
                    errorInfo.AppendLine("✓ Both IL indicators found - IL project detected");
                    return true;
                }
                else
                {
                    if (!foundSystemRegister && !foundIfdefIL)
                    {
                        errorInfo.AppendLine("✗ Neither IL indicator found - not an IL project");
                    }
                    else if (!foundSystemRegister)
                    {
                        errorInfo.AppendLine("✗ system::register(\"infinityloader\" not found - not an IL project");
                    }
                    else if (!foundIfdefIL)
                    {
                        errorInfo.AppendLine("✗ #ifdef IL directive not found - not an IL project");
                    }

                    if (foundNamespace && namespaceValue.Equals("duplicate_render", StringComparison.OrdinalIgnoreCase))
                    {
                        errorInfo.AppendLine("✗ Namespace is 'duplicate_render' and IL indicators not found - not an IL project");
                    }
                }
            }
            catch (Exception ex)
            {
                // If we can't check, provide error details
                errorInfo.AppendLine($"Error scanning project: {ex.Message}");
                errorDetails = errorInfo.ToString();
                return false;
            }

            errorDetails = errorInfo.ToString();
            return false;
        }

        /// <summary>
        /// Converts IL syntax to T7 syntax in a GSC file
        /// Handles new nested IL syntax:
        /// - #namespace infinityloader; -> #namespace duplicate_render;
        /// - #ifdef RELEASE ... #else ... #ifndef DEBUG ... #else ... #endif ... #endif
        ///   -> autoexec __init__system__() { system::register("duplicate_render", ::__init__, undefined, undefined); }
        /// - #ifdef T7 ... #else ... #ifdef IL ... #else ... #endif ... #endif
        ///   -> Keep #ifdef T7 block, remove entire #else block (which contains IL code)
        /// - #ifdef IL ... #else ... #endif
        ///   -> Keep #else part (T7 code), remove #ifdef IL part
        /// - Removes system::register("infinityloader" calls
        /// - Removes autoexec __init__system__() functions
        /// </summary>
        private string ConvertILToT7(string content)
        {
            // Step 0: Replace #namespace infinityloader; with #namespace duplicate_render;
            content = System.Text.RegularExpressions.Regex.Replace(
                content,
                @"#namespace\s+infinityloader\s*;",
                "#namespace duplicate_render;",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Step 1: Convert #ifdef RELEASE ... #else ... #ifndef DEBUG ... #else ... #endif ... #endif
            // to: autoexec __init__system__() { system::register("duplicate_render", ::__init__, undefined, undefined); }
            // Only match if it contains BUILD definition (to avoid false matches)
            // This pattern matches the nested RELEASE/DEBUG structure with BUILD definition
            content = System.Text.RegularExpressions.Regex.Replace(
                content,
                @"#ifdef\s+RELEASE\s*[\r\n]*(?:[^#]|#(?!else|endif))*?#define\s+BUILD[^\r\n]*[\r\n]*(?:[^#]|#(?!else|endif))*?#else\s*[\r\n]*(?:[^#]|#(?!ifndef|endif))*?#ifndef\s+DEBUG\s*[\r\n]*(?:[^#]|#(?!else|endif))*?#define\s+BUILD[^\r\n]*[\r\n]*(?:[^#]|#(?!else|endif))*?#else\s*[\r\n]*(?:[^#]|#(?!endif))*?#define\s+BUILD[^\r\n]*[\r\n]*(?:[^#]|#(?!endif))*?[\r\n]*?#endif\s*[\r\n]*#endif",
                "autoexec __init__system__()\r\n{\r\n    system::register(\"duplicate_render\", ::__init__, undefined, undefined);\r\n}",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

            // Step 2: Handle nested #ifdef T7 ... #else ... #ifdef IL ... #else ... #endif ... #endif
            // We want to keep the #ifdef T7 block and remove the entire #else block (which contains the IL code)
            // Pattern: #ifdef T7 (keep this) ... #else (remove everything from here) ... #ifdef IL ... #else ... #endif ... #endif
            // Replace with: #ifdef T7 (keep content) ... (remove #else and everything after until matching #endif)
            content = System.Text.RegularExpressions.Regex.Replace(
                content,
                @"(#ifdef\s+T7\s*[\r\n]*(?:[^#]|#(?!else|endif))*?)(#else\s*[\r\n]*(?:[^#]|#(?!endif))*?#ifdef\s+IL\s*[\r\n]*(?:[^#]|#(?!else|endif))*?#else\s*[\r\n]*(?:[^#]|#(?!endif))*?[\r\n]*?#endif\s*[\r\n]*#endif)",
                "$1",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

            // Also handle simpler case: #ifdef T7 ... #else ... #endif (remove #else part if it contains IL indicators)
            // But only if the #else doesn't have nested #ifdef IL (already handled above)
            content = System.Text.RegularExpressions.Regex.Replace(
                content,
                @"(#ifdef\s+T7\s*[\r\n]*(?:[^#]|#(?!else|endif))*?)#else\s*[\r\n]*(?:[^#]|#(?!endif))*?(system\s*::\s*register\s*\(\s*[""']infinityloader[""']|#ifdef\s+IL)(?:[^#]|#(?!endif))*?#endif",
                "$1",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

            // Step 3: Handle standalone #ifdef IL ... #else ... #endif blocks (keep #else part, remove IL part)
            // This regex matches: #ifdef IL ... #else (T7 code) ... #endif
            // and replaces it with just the T7 code from #else
            content = System.Text.RegularExpressions.Regex.Replace(
                content,
                @"#ifdef\s+IL\s*[\r\n]*(?:[^#]|#(?!else|endif))*?#else\s*(?:[\r\n]|//[^\r\n]*[\r\n])*((?:[^#]|#(?!endif))*?)[\r\n]*?#endif",
                "$1",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

            // Step 4: Handle #ifdef IL blocks without #else (remove entire block)
            content = System.Text.RegularExpressions.Regex.Replace(
                content,
                @"#ifdef\s+IL\s*[\r\n]*(?:[^#]|#(?!endif))*?[\r\n]*?#endif",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

            // Step 5: Remove autoexec __init__system__() functions that register infinityloader (IL-specific)
            // This needs to match multiline functions - match from autoexec to closing brace
            // But keep the ones that register "duplicate_render" (we just added those)
            content = System.Text.RegularExpressions.Regex.Replace(
                content,
                @"autoexec\s+__init__system__\s*\([^)]*\)\s*\{[^\}]*system\s*::\s*register\s*\(\s*[""']infinityloader[""'][^\}]*\}",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

            // Step 6: Remove system::register("infinityloader" calls (can be multiline, now in #else blocks)
            // Match the entire call including any whitespace before it
            content = System.Text.RegularExpressions.Regex.Replace(
                content,
                @"\s*system\s*::\s*register\s*\(\s*[""']infinityloader[""'][^;]*;\s*",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

            // Step 7: Remove standalone #ifdef IL lines (if any remain)
            content = System.Text.RegularExpressions.Regex.Replace(
                content,
                @"#ifdef\s+IL\s*[\r\n]+",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Step 8: Remove #else comments that were part of IL blocks (e.g., "// ---------------- T7 / BUILD MODE ----------------")
            content = System.Text.RegularExpressions.Regex.Replace(
                content,
                @"#else\s*//\s*[^\r\n]*(?:T7|BUILD\s+MODE)[^\r\n]*[\r\n]+",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline);

            // Step 9: Replace enableonlinematch with getplayers (old IL syntax compatibility)
            content = System.Text.RegularExpressions.Regex.Replace(
                content,
                @"enableonlinematch",
                "getplayers",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Step 10: Clean up excessive blank lines (more than 2 consecutive)
            content = System.Text.RegularExpressions.Regex.Replace(
                content,
                @"[\r\n]{3,}",
                "\r\n\r\n",
                System.Text.RegularExpressions.RegexOptions.Multiline);

            // Step 11: Clean up trailing whitespace on lines
            content = System.Text.RegularExpressions.Regex.Replace(
                content,
                @"[ \t]+[\r\n]",
                "\r\n",
                System.Text.RegularExpressions.RegexOptions.Multiline);

            return content;
        }

        private void PortILProject(string input, string output)
        {
            // Port IL project logic (aligned with ImportDialog.cs conversion logic)
            try
            {
                // If output is "unknown", create a new folder next to the input
                string outputPath = output;
                if (output == "unknown" || string.IsNullOrEmpty(output))
                {
                    // Create output folder next to input with "_ported" suffix
                    outputPath = input + "_ported";
                    int counter = 1;
                    while (Directory.Exists(outputPath))
                    {
                        outputPath = input + "_ported" + counter;
                        counter++;
                    }
                }

                // Check if gsc.conf already exists - if so, just copy the project as-is (already converted)
                if (File.Exists(Path.Combine(input, "gsc.conf")))
                {
                    Directory.CreateDirectory(outputPath);
                    FileHelper.CopyDirectory(input, outputPath, true);

                    ReaLTaiizor.Controls.PoisonMessageBox.Show(
                        this,
                        $"Project already has gsc.conf - copied as-is to:\n{outputPath}\n\nThe project will now be opened.",
                        "Porting Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    OpenFolder(outputPath);
                    return;
                }

                // Create output directory structure
                Directory.CreateDirectory(outputPath);
                string scriptsPath = Path.Combine(outputPath, "scripts");
                Directory.CreateDirectory(scriptsPath);

                // Copy all files from input to scripts folder
                FileHelper.CopyDirectory(input, scriptsPath, true);

                // Convert IL syntax to T7 syntax in all .gsc files
                // This handles #ifdef IL blocks, system::register calls, enableonlinematch, etc.
                foreach (var gscFile in Directory.GetFiles(scriptsPath, "*.gsc", SearchOption.AllDirectories))
                {
                    try
                    {
                        string content = File.ReadAllText(gscFile);
                        string convertedContent = ConvertILToT7(content);
                        File.WriteAllText(gscFile, convertedContent);
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue with other files
                        System.Diagnostics.Debug.WriteLine($"Error converting IL file {gscFile}: {ex.Message}");
                    }
                }

                // Convert config.il to gsc.conf (aligned with ImportDialog.cs logic)
                HashSet<string> symbols = new HashSet<string>();
                symbols.Add("BO3");  // Always add BO3
                // Add namespace if found (read from source files)
                if (!string.IsNullOrEmpty(projectNamespace))
                {
                    symbols.Add(projectNamespace.ToUpper());
                }

                var configIl = Directory.GetFiles(scriptsPath, "config.il", SearchOption.AllDirectories).FirstOrDefault();
                if (configIl != null)
                {
                    try
                    {
                        string configData = File.ReadAllText(configIl);

                        // Extract <Mode> tag value
                        int modeIndex = configData.IndexOf("<Mode>");
                        if (modeIndex >= 0)
                        {
                            string modeValue = new string(configData
                                .Skip(modeIndex + "<Mode>".Length)
                                .TakeWhile(c => c != '<')
                                .ToArray())
                                .Trim()
                                .ToUpper();

                            if (!string.IsNullOrEmpty(modeValue))
                            {
                                symbols.Add(modeValue);
                            }
                        }

                        // Extract <Symbols> tag values (semicolon-separated)
                        int symbolsIndex = configData.IndexOf("<Symbols>");
                        if (symbolsIndex >= 0)
                        {
                            string symbolsValue = new string(configData
                                .Skip(symbolsIndex + "<Symbols>".Length)
                                .TakeWhile(c => c != '<')
                                .ToArray())
                                .Trim()
                                .ToUpper();

                            if (!string.IsNullOrEmpty(symbolsValue))
                            {
                                foreach (var symbol in symbolsValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                                {
                                    string trimmedSymbol = symbol.Trim();
                                    if (!string.IsNullOrEmpty(trimmedSymbol))
                                    {
                                        symbols.Add(trimmedSymbol);
                                    }
                                }
                            }
                        }

                        // Delete config.il after extracting its data
                        File.Delete(configIl);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing config.il: {ex.Message}");
                    }
                }

                // Delete all remaining .il files (after processing config.il)
                foreach (var ilFile in Directory.GetFiles(scriptsPath, "*.il", SearchOption.AllDirectories))
                {
                    try
                    {
                        File.Delete(ilFile);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error deleting .il file {ilFile}: {ex.Message}");
                    }
                }

                // Create gsc.conf file with extracted symbols
                string symbolsString = string.Join(",", symbols.ToArray());
                File.WriteAllText(Path.Combine(outputPath, "gsc.conf"), $"symbols={symbolsString}");

                // Show success message
                ReaLTaiizor.Controls.PoisonMessageBox.Show(
                    this,
                    $"IL project successfully ported to:\n{outputPath}\n\nThe ported project will now be opened.",
                    "Porting Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                // Open the ported project (will skip IL detection because path contains "_ported")
                OpenFolder(outputPath);
            }
            catch (Exception ex)
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this,
                    $"Error porting IL project: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                CreateDefaultProjectOnStartup();
            }
        }

        #endregion


        #region Status Bar

        private System.Windows.Forms.Timer statusBarTimer;

        /// <summary>
        /// Gets the default status text (Ready + project path if available)
        /// </summary>
        private string GetDefaultStatusText()
        {
            if (!string.IsNullOrEmpty(projectPath) && folderOpened)
            {
                // Replace %USERPROFILE% or actual user profile path with ~
                string displayPath = projectPath;
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                // First expand any environment variables
                displayPath = Environment.ExpandEnvironmentVariables(displayPath);

                // Replace user profile path with ~
                if (!string.IsNullOrEmpty(userProfile) && displayPath.StartsWith(userProfile, StringComparison.OrdinalIgnoreCase))
                {
                    displayPath = "~" + displayPath.Substring(userProfile.Length);
                }
                // Also handle %USERPROFILE% if it wasn't expanded
                else if (displayPath.IndexOf("%USERPROFILE%", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Case-insensitive replace
                    int index = displayPath.IndexOf("%USERPROFILE%", StringComparison.OrdinalIgnoreCase);
                    if (index >= 0)
                    {
                        displayPath = displayPath.Substring(0, index) + "~" + displayPath.Substring(index + "%USERPROFILE%".Length);
                    }
                }

                // Truncate path if too long (max 100 characters for path)
                if (displayPath.Length > 100)
                {
                    displayPath = "..." + displayPath.Substring(displayPath.Length - 97);
                }
                return $"Ready | {displayPath}";
            }
            return "Ready";
        }

        /// <summary>
        /// Shows a message in the status bar for a specified duration
        /// </summary>
        private void ShowStatusMessage(string message, int durationMs = 3000)
        {
            if (statusLabel == null)
                return;

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => ShowStatusMessage(message, durationMs)));
                return;
            }

            statusLabel.Text = message;

            // Clear the timer if it exists
            if (statusBarTimer != null)
            {
                statusBarTimer.Stop();
                statusBarTimer.Dispose();
            }

            // Create a tracked timer to reset the status bar after the duration
            statusBarTimer = ReaLTaiizorExt.PoisonFormHelper.CreateTrackedTimer(this, durationMs, (s, e) =>
            {
                statusBarTimer.Stop();
                statusBarTimer = null;
                if (statusLabel != null && !this.IsDisposed)
                {
                    statusLabel.Text = GetDefaultStatusText();
                }
            });
            statusBarTimer.Start();
        }

        #endregion

        #region File Watcher

        private void FileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            // CRITICAL: Check if form is disposed/disposing before processing to prevent memory leaks
            if (this.IsDisposed || this.Disposing || !this.IsHandleCreated)
                return;

            // File was changed externally - reload if not currently editing
            if (this.InvokeRequired)
            {
                // Use BeginInvoke instead of Invoke to avoid TimeoutException
                // BeginInvoke is asynchronous and won't block or timeout
                try
                {
                    this.BeginInvoke(new Action(() => FileWatcher_Changed(sender, e)));
                }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
                catch (ArgumentException) { }
                return;
            }

            // Use full absolute path to match openEditors keys
            string fullPath = Path.GetFullPath(e.FullPath);
            string filename = Path.GetFileName(e.FullPath);

            // Check both full path and filename (for backward compatibility)
            string keyToUse = openEditors.ContainsKey(fullPath) ? fullPath :
                             (openEditors.ContainsKey(filename) ? filename : null);

            // Reload file if it's open
            // If it's the current file being edited, we'll still reload but show a notification
            if (keyToUse != null)
            {
                // Reload file from disk (external change)
                AvalonEditWrapper editor = openEditors[keyToUse];
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

                                                    // Show notification in status bar
                                                    ShowStatusMessage($"File '{Path.GetFileName(e.FullPath)}' was modified externally and has been reloaded.", 5000);

                                                    // Update conditional compilation indicators after reload
                                                    UpdateConditionalCompilationIndicators(editor);
                                                    UpdateSyntaxHighlightingIndicators(editor);
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

                                    // Show notification in status bar
                                    ShowStatusMessage($"File '{Path.GetFileName(e.FullPath)}' was modified externally and has been reloaded.", 5000);

                                    // Update conditional compilation indicators after reload
                                    UpdateConditionalCompilationIndicators(editor);
                                    UpdateSyntaxHighlightingIndicators(editor);
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
            // CRITICAL: Check if form is disposed/disposing before processing to prevent memory leaks
            if (this.IsDisposed || this.Disposing || !this.IsHandleCreated)
                return;

            if (this.InvokeRequired)
            {
                // Use BeginInvoke instead of Invoke to avoid TimeoutException
                try
                {
                    this.BeginInvoke(new Action(() => FileWatcher_Created(sender, e)));
                }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
                catch (ArgumentException) { }
                return;
            }

            RefreshFileList(false);
        }

        private void FileWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            // CRITICAL: Check if form is disposed/disposing before processing to prevent memory leaks
            if (this.IsDisposed || this.Disposing || !this.IsHandleCreated)
                return;

            if (this.InvokeRequired)
            {
                // Use BeginInvoke instead of Invoke to avoid TimeoutException
                try
                {
                    this.BeginInvoke(new Action(() => FileWatcher_Deleted(sender, e)));
                }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
                catch (ArgumentException) { }
                return;
            }

            // Use full absolute path to match editorTabs keys
            string fullPath = Path.GetFullPath(e.FullPath);
            string filename = Path.GetFileName(e.FullPath);

            // Check both full path and filename (for backward compatibility)
            string keyToUse = editorTabs.ContainsKey(fullPath) ? fullPath :
                             (editorTabs.ContainsKey(filename) ? filename : null);

            if (keyToUse != null)
            {
                // Show notification in status bar
                ShowStatusMessage($"File '{Path.GetFileName(e.FullPath)}' was deleted. Closing tab...", 3000);

                // Dispose and remove tab page first
                var tabPageToRemove = editorTabs[keyToUse];
                tabControl.TabPages.Remove(tabPageToRemove);

                // Properly clean up all resources for this tab
                CleanupTabResources(keyToUse);
            }

            RefreshFileList(false);
        }

        private void FileWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            // CRITICAL: Check if form is disposed/disposing before processing to prevent memory leaks
            if (this.IsDisposed || this.Disposing || !this.IsHandleCreated)
                return;

            if (this.InvokeRequired)
            {
                // Use BeginInvoke instead of Invoke to avoid TimeoutException
                try
                {
                    this.BeginInvoke(new Action(() => FileWatcher_Renamed(sender, e)));
                }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
                catch (ArgumentException) { }
                return;
            }

            // Show notification in status bar
            ShowStatusMessage($"File '{Path.GetFileName(e.OldFullPath)}' was renamed to '{Path.GetFileName(e.FullPath)}'. Refreshing...", 3000);

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

        /// <summary>
        /// Reads an embedded resource file from the Defaults folder
        /// </summary>
        private string ReadEmbeddedTemplate(string resourceName)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string fullResourceName = $"T7CompilerGUI.Defaults.{resourceName}";

                using (Stream stream = assembly.GetManifestResourceStream(fullResourceName))
                {
                    if (stream != null)
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }
            catch
            {
                // If embedded resource not found, return empty string
            }

            return "";
        }

        private string GetDefaultMainContent()
        {
            if (currentGame == TreyarchCompiler.Enums.Games.T8)
            {
                string content = ReadEmbeddedTemplate("defaultt8project.main");
                if (!string.IsNullOrEmpty(content))
                    return content;
            }
            else
            {
                string content = ReadEmbeddedTemplate("defaultt7project.main");
                if (!string.IsNullOrEmpty(content))
                    return content;
            }

            // Fallback: try file system (for development)
            string templatePath = GetDefaultsPath();
            if (currentGame == TreyarchCompiler.Enums.Games.T8)
            {
                string bo4MainPath = Path.Combine(templatePath, "defaultt8project.main");
                if (File.Exists(bo4MainPath))
                    return File.ReadAllText(bo4MainPath);
            }
            else
            {
                string bo3MainPath = Path.Combine(templatePath, "defaultt7project.main");
                if (File.Exists(bo3MainPath))
                    return File.ReadAllText(bo3MainPath);
            }

            return "";
        }

        private string GetDefaultHeadersContent()
        {
            if (currentGame == TreyarchCompiler.Enums.Games.T8)
            {
                string content = ReadEmbeddedTemplate("defaultt8project.headers");
                if (!string.IsNullOrEmpty(content))
                    return content;
            }

            // Fallback: try file system (for development)
            string templatePath = GetDefaultsPath();
            if (currentGame == TreyarchCompiler.Enums.Games.T8)
            {
                string bo4HeadersPath = Path.Combine(templatePath, "defaultt8project.headers");
                if (File.Exists(bo4HeadersPath))
                    return File.ReadAllText(bo4HeadersPath);
            }

            // T7 doesn't use headers file, it uses multiple files (options, util, functions/*)
            return @"// Headers file
// Add your function declarations and includes here";
        }

        /// <summary>
        /// Creates a file from an embedded template resource or file system template, otherwise uses fallback content
        /// Always writes the content to ensure files have the default content
        /// </summary>
        private void CreateFileFromTemplate(string templateDir, string templateFileName, string outputPath, string fallbackContent)
        {
            // Ensure the output directory exists
            string outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            string contentToWrite = "";

            // First, try to read from embedded resources
            string embeddedContent = ReadEmbeddedTemplate(templateFileName);
            if (!string.IsNullOrEmpty(embeddedContent))
            {
                contentToWrite = embeddedContent;
            }
            else
            {
                // Fallback: try file system (for development)
                string templatePath = Path.Combine(templateDir, templateFileName);
                if (File.Exists(templatePath))
                {
                    // Read content from template file
                    contentToWrite = File.ReadAllText(templatePath);
                }
                else
                {
                    // Use fallback content
                    contentToWrite = fallbackContent ?? "";
                }
            }

            // Always write the content (overwrite if file exists to ensure default content is set)
            File.WriteAllText(outputPath, contentToWrite);
        }

        #endregion

        #region Additional UI Features

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
            catch (Exception ex)
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, $"Error killing process: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Properly cleans up all resources for a tab (editor, tab page, file contents)
        /// This ensures memory is freed when tabs are closed
        /// </summary>
        private void CleanupTabResources(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            // Dispose editor and remove from dictionary
            if (openEditors.ContainsKey(filePath))
            {
                var editor = openEditors[filePath];
                if (editor != null && !editor.IsDisposed)
                {
                    // Note: All event handlers (TextChanged, PreviewKeyDown, etc.) are cleaned up
                    // automatically when the editor is disposed. AvalonEditWrapper.Dispose() handles
                    // cleanup of ElementHost, TextEditor, and all WPF resources including event handlers.
                    // The editor's Dispose() method ensures all resources are properly released.
                    editor.Dispose();
                }
                openEditors.Remove(filePath);
            }

            // Remove tab page reference
            if (editorTabs.ContainsKey(filePath))
            {
                var tabPage = editorTabs[filePath];
                if (tabPage != null && !tabPage.IsDisposed)
                {
                    // TabPage will dispose its child controls (including the editor if still attached)
                    tabPage.Dispose();
                }
                editorTabs.Remove(filePath);
            }

            // Remove file contents from memory - this can be large for big files
            if (fileContents.ContainsKey(filePath))
            {
                fileContents.Remove(filePath);
            }
        }

        /// <summary>
        /// Recursively disposes a control and all its child controls to prevent memory leaks
        /// </summary>
        private void DisposeControlRecursive(Control control)
        {
            if (control == null || control.IsDisposed)
                return;

            try
            {
                // Dispose all child controls first
                var children = new List<Control>();
                foreach (Control child in control.Controls)
                {
                    children.Add(child);
                }
                control.Controls.Clear(); // Remove from parent first

                // Dispose children recursively
                foreach (var child in children)
                {
                    DisposeControlRecursive(child);
                }

                // Dispose the control itself
                control.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing control {control.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Clean up resources when form is disposed
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose components first (from designer)
                if (components != null)
                {
                    components.Dispose();
                    components = null;
                }

                // Stop and dispose timers
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
                    tabUpdateTimer = null;
                }

                // Dispose status bar timer
                if (statusBarTimer != null)
                {
                    statusBarTimer.Stop();
                    statusBarTimer.Dispose();
                    statusBarTimer = null;
                }

                // Dispose rainbow timer
                if (rainbowTimer != null)
                {
                    rainbowTimer.Stop();
                    rainbowTimer.Dispose();
                    rainbowTimer = null;
                }

                // Dispose file watcher
                if (fileWatcher != null)
                {
                    fileWatcher.EnableRaisingEvents = false;
                    // Unsubscribe from all events before disposing
                    fileWatcher.Changed -= FileWatcher_Changed;
                    fileWatcher.Created -= FileWatcher_Created;
                    fileWatcher.Deleted -= FileWatcher_Deleted;
                    fileWatcher.Renamed -= FileWatcher_Renamed;
                    fileWatcher.Dispose();
                    fileWatcher = null;
                }

                // Clean up all tabs properly to free memory
                var keysToCleanup = new List<string>(openEditors.Keys);
                foreach (var key in keysToCleanup)
                {
                    CleanupTabResources(key);
                }

                // Ensure dictionaries are cleared (CleanupTabResources should have done this, but double-check)
                openEditors.Clear();
                editorTabs.Clear();
                fileContents.Clear();

                // Clear hash map
                hashToFunctionMap?.Clear();

                // Clear keyboard shortcuts dictionary
                codeEditorKeybinds?.Clear();

                // Unsubscribe from tab control events
                if (tabControl != null)
                {
                    tabControl.SelectedIndexChanged -= TabControl_SelectedIndexChanged;
                }

                // Unsubscribe from form events (lambda handlers will be cleaned up automatically when form is disposed)
                // Note: Lambda event handlers on the form itself don't cause memory leaks as they're disposed with the form

                // Ensure status bar timer is disposed
                if (statusBarTimer != null)
                {
                    statusBarTimer.Stop();
                    statusBarTimer.Dispose();
                    statusBarTimer = null;
                }

                // CRITICAL: Dispose all file button controls when form is disposed
                // This ensures memory is fully released after closing the editor
                if (fileButtonsPanel != null && fileButtonsPanel.Controls.Count > 0)
                {
                    var controlsToDispose = new List<Control>();
                    foreach (Control control in fileButtonsPanel.Controls)
                    {
                        controlsToDispose.Add(control);
                    }
                    fileButtonsPanel.Controls.Clear(); // Clear first to remove from parent
                    foreach (var control in controlsToDispose)
                    {
                        // Dispose container and all child controls (buttons)
                        DisposeControlRecursive(control);
                    }
                }
            }

            base.Dispose(disposing);
        }

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
                        // Use a tracked timer to close after a brief delay to ensure all UI updates complete
                        System.Windows.Forms.Timer closeTimer = null;
                        closeTimer = ReaLTaiizorExt.PoisonFormHelper.CreateTrackedTimer(this, 50, (s, args) =>
                        {
                            try
                            {
                                closeTimer.Stop();
                                this.Close();
                            }
                            catch
                            {
                                // Ignore errors during close
                            }
                        });
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
                            // User wants to close anyway - use tracked timer to close
                            System.Windows.Forms.Timer closeTimer = null;
                            closeTimer = ReaLTaiizorExt.PoisonFormHelper.CreateTrackedTimer(this, 50, (s, args) =>
                            {
                                try
                                {
                                    closeTimer.Stop();
                                    this.Close();
                                }
                                catch
                                {
                                    // Ignore errors during close
                                }
                            });
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

            // Cleanup status bar timer
            if (statusBarTimer != null)
            {
                statusBarTimer.Stop();
                statusBarTimer.Dispose();
            }

            // Save last project path if a project is open
            if (folderOpened && !string.IsNullOrEmpty(projectPath) && Directory.Exists(projectPath))
            {
                SaveLastProjectPath(projectPath);
            }

            // Use PoisonFormHelper for standardized cleanup (disposes tracked resources)
            ReaLTaiizorExt.PoisonFormHelper.CleanupForm(this);
            
            base.OnFormClosing(e);
        }

        /// <summary>
        /// Saves the last project path to config file
        /// </summary>
        private void SaveLastProjectPath(string path)
        {
            try
            {
                // Use the same unified config file as MainForm (T7CompilerGUI.conf)
                string configPath = GetConfigPath();

                XDocument config;
                if (File.Exists(configPath))
                {
                    config = XDocument.Load(configPath);
                }
                else
                {
                    config = new XDocument(new XElement("T7CompilerConfig"));
                }

                XElement root = config.Element("T7CompilerConfig");
                if (root == null)
                {
                    root = new XElement("T7CompilerConfig");
                    config.Add(root);
                }

                XElement appSettings = root.Element("Application");
                if (appSettings == null)
                {
                    appSettings = new XElement("Application");
                    root.Add(appSettings);
                }

                XElement codeEditorLastProject = appSettings.Element("CodeEditorLastProject");
                if (codeEditorLastProject == null)
                {
                    codeEditorLastProject = new XElement("CodeEditorLastProject");
                    appSettings.Add(codeEditorLastProject);
                }

                codeEditorLastProject.Value = path;
                config.Save(configPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save last project path: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the last project path from config file (uses same unified config as MainForm: T7CompilerGUI.conf)
        /// </summary>
        private string GetLastProjectPath()
        {
            try
            {
                // Use the same unified config file as MainForm (T7CompilerGUI.conf)
                string configPath = GetConfigPath();

                if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
                    return null;

                XDocument config = XDocument.Load(configPath);
                XElement root = config.Element("T7CompilerConfig");
                if (root == null)
                    return null;

                XElement appSettings = root.Element("Application");
                if (appSettings == null)
                    return null;

                XElement codeEditorLastProject = appSettings.Element("CodeEditorLastProject");
                if (codeEditorLastProject == null || string.IsNullOrWhiteSpace(codeEditorLastProject.Value))
                    return null;

                string path = codeEditorLastProject.Value.Trim();
                if (Directory.Exists(path))
                    return path;

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load last project path: {ex.Message}");
                return null;
            }
        }


        /// <summary>
        /// Checks for last project and asks user if they want to load it
        /// </summary>
        private void CheckAndLoadLastProject()
        {
            try
            {
                // Check if form is disposed or not fully initialized
                if (this.IsDisposed || !this.IsHandleCreated)
                {
                    // Form not ready, skip loading
                    return;
                }

                // Use a tracked delay timer instead of BeginInvoke to ensure form is fully ready
                System.Windows.Forms.Timer loadTimer = null;
                loadTimer = ReaLTaiizorExt.PoisonFormHelper.CreateTrackedTimer(this, 100, (s, e) =>
                {
                    loadTimer.Stop();

                    try
                    {
                        // Double-check form is still valid
                        if (this.IsDisposed || !this.IsHandleCreated)
                        {
                            return;
                        }

                        string lastProjectPath = GetLastProjectPath();
                        if (string.IsNullOrEmpty(lastProjectPath) || !Directory.Exists(lastProjectPath))
                        {
                            // No last project, create default
                            CreateDefaultProjectOnStartup();
                            return;
                        }

                        // Ask user if they want to load the last project
                        var result = ReaLTaiizor.Controls.PoisonMessageBox.Show(
                            this,
                            $"Do you want to load the last project?\n\n{lastProjectPath}",
                            "Load Last Project?",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (result == DialogResult.Yes)
                        {
                            // Load the last project
                            if (Directory.Exists(lastProjectPath))
                            {
                                OpenFolder(lastProjectPath);
                            }
                            else
                            {
                                // Path no longer exists, create default
                                CreateDefaultProjectOnStartup();
                            }
                        }
                        else
                        {
                            // User declined, create default project
                            CreateDefaultProjectOnStartup();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error in CheckAndLoadLastProject: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                        // Fallback to default project on error
                        try
                        {
                            if (!this.IsDisposed && this.IsHandleCreated)
                            {
                                CreateDefaultProjectOnStartup();
                            }
                        }
                        catch (Exception ex2)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error creating default project: {ex2.Message}");
                        }
                    }
                });

                loadTimer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting up CheckAndLoadLastProject: {ex.Message}");
                // Fallback to default project on error
                try
                {
                    if (!this.IsDisposed && this.IsHandleCreated)
                    {
                        CreateDefaultProjectOnStartup();
                    }
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"Error creating default project: {ex2.Message}");
                }
            }
        }

        #endregion
    }
}

