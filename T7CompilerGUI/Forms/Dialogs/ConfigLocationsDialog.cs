using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Enum.Poison;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Interface.Poison;
using ReaLTaiizor.Drawing.Poison;
using T7CompilerGUI.Controls;
using T7CompilerGUI.Helpers;

namespace T7CompilerGUI.Forms.Dialogs
{
    public partial class ConfigLocationsDialog : PoisonForm
    {
        
        private PoisonStyleManager styleManager;
        private Forms.MainForm parentForm; // Reference to parent form for rainbow access
        private System.Windows.Forms.Timer rainbowUpdateTimer; // Timer to update UI when rainbow is active
        private System.Windows.Forms.Timer syncTimer; // Timer to sync with StyleManager
        
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
        
        public string ConfigPath { get; private set; }
        public string TempPath { get; private set; }
        public string DllExtractPath { get; private set; }
        public string AppDataRoaming { get; private set; }
        
        public ConfigLocationsDialog(PoisonStyleManager styleManager, string currentConfigPath, string currentTempPath, string currentDllExtractPath, string currentAppDataRoaming = null) : this(styleManager, currentConfigPath, currentTempPath, currentDllExtractPath, currentAppDataRoaming, null)
        {
        }
        
        public ConfigLocationsDialog(PoisonStyleManager styleManager, string currentConfigPath, string currentTempPath, string currentDllExtractPath, string currentAppDataRoaming, Forms.MainForm parentForm)
        {
            this.styleManager = styleManager;
            this.parentForm = parentForm;
            this.ConfigPath = currentConfigPath;
            this.TempPath = currentTempPath;
            this.DllExtractPath = currentDllExtractPath;
            this.AppDataRoaming = currentAppDataRoaming ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            
            // Initialize designer-generated controls
            InitializeComponent();
            
            // PoisonForm already sets ControlStyles in its constructor (OptimizedDoubleBuffer, ResizeRedraw, etc.)
            // We don't need to override them - PoisonForm handles smooth resizing internally
            
            SetupControls();
            LoadCurrentPaths();
            
            this.Load += ConfigLocationsDialog_Load;
            
            // Setup hover and pressed effects for buttons
            SetupButtonEffects();
            
            // Setup rainbow update timer if parent form is available
            SetupRainbowUpdateTimer();
            
            // Subscribe to StyleManager updates to keep in sync
            if (styleManager != null)
            {
                // When dialog is shown, sync all controls
                this.Shown += (s, e) => SyncAllControlsWithStyleManager();
                
                // Also sync periodically to catch any changes
                syncTimer = new System.Windows.Forms.Timer();
                syncTimer.Interval = 100; // Check every 100ms
                syncTimer.Tick += (s, e) => 
                {
                    if (!this.IsDisposed && this.IsHandleCreated)
                    {
                        SyncAllControlsWithStyleManager();
                    }
                };
                syncTimer.Start();
            }
        }
        
        private void SetupRainbowUpdateTimer()
        {
            if (parentForm == null) return;
            
            // Create a timer to invalidate buttons when rainbow is active
            rainbowUpdateTimer = new System.Windows.Forms.Timer();
            rainbowUpdateTimer.Interval = 16; // ~60 FPS to match rainbow animation
            rainbowUpdateTimer.Tick += (s, e) =>
            {
                if (IsRainbowStyleActive())
                {
                    // Invalidate all buttons to update rainbow colors
                    InvalidateAllButtons(this);
                }
            };
            
            // Always start the timer - it will only invalidate when rainbow is active
            rainbowUpdateTimer.Start();
        }
        
        private void InvalidateAllButtons(Control parent)
        {
            foreach (Control ctrl in parent.Controls)
            {
                if (ctrl is PoisonButton)
                {
                    ctrl.Invalidate();
                }
                
                if (ctrl.HasChildren)
                {
                    InvalidateAllButtons(ctrl);
                }
            }
        }
        
        private void SetupControls()
        {
            // Controls are now created in InitializeComponent (Designer file)
            // This method just wires up event handlers and sets dynamic properties
            
            if (styleManager != null)
            {
                this.StyleManager = styleManager;
            }
            
            // Null check all controls before wiring up events
            if (btnBrowseConfig != null)
                btnBrowseConfig.Click += BtnBrowseConfig_Click;
            if (btnBrowseTemp != null)
                btnBrowseTemp.Click += BtnBrowseTemp_Click;
            if (btnBrowseDll != null)
                btnBrowseDll.Click += BtnBrowseDll_Click;
            if (btnBrowseAppDataRoaming != null)
                btnBrowseAppDataRoaming.Click += BtnBrowseAppDataRoaming_Click;
            if (btnOK != null)
                btnOK.Click += BtnOK_Click;
            if (btnCancel != null)
                btnCancel.Click += (s, e) => 
                {
                    ResetButtonState(s);
                    this.DialogResult = DialogResult.Cancel;
                };
            
            // Position buttons correctly after panel is added (accounting for padding)
            if (buttonPanel != null)
            {
                buttonPanel.Layout += (s, e) =>
                {
                    if (btnOK != null && btnCancel != null)
                    {
                        int rightPadding = buttonPanel.Padding.Right;
                        int buttonSpacing = 10;
                        int dialogButtonWidth = 100;
                        
                        btnOK.Location = new Point(buttonPanel.Width - rightPadding - dialogButtonWidth, 10);
                        btnCancel.Location = new Point(buttonPanel.Width - rightPadding - dialogButtonWidth - dialogButtonWidth - buttonSpacing, 10);
                    }
                };
            }
            
            // Setup button hover effects after controls are created using centralized helper
            if (btnBrowseConfig != null)
                PoisonControlHelper.SetupButtonEffects(btnBrowseConfig);
            if (btnBrowseTemp != null)
                PoisonControlHelper.SetupButtonEffects(btnBrowseTemp);
            if (btnBrowseDll != null)
                PoisonControlHelper.SetupButtonEffects(btnBrowseDll);
            if (btnBrowseAppDataRoaming != null)
                PoisonControlHelper.SetupButtonEffects(btnBrowseAppDataRoaming);
            if (btnOK != null)
                PoisonControlHelper.SetupButtonEffects(btnOK);
            if (btnCancel != null)
                PoisonControlHelper.SetupButtonEffects(btnCancel);
        }
        
        /// <summary>
        /// Shortens a path by replacing common user directories with environment variables
        /// </summary>
        private string ShortenPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            
            try
            {
                // Get common environment paths
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                
                // Replace with environment variables (case-insensitive)
                if (path.StartsWith(userProfile, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(userProfile))
                {
                    return "%USERPROFILE%" + path.Substring(userProfile.Length);
                }
                if (path.StartsWith(appData, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(appData))
                {
                    return "%APPDATA%" + path.Substring(appData.Length);
                }
                if (path.StartsWith(localAppData, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(localAppData))
                {
                    return "%LOCALAPPDATA%" + path.Substring(localAppData.Length);
                }
                if (path.StartsWith(documents, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(documents))
                {
                    return "%USERPROFILE%\\Documents" + path.Substring(documents.Length);
                }
                if (path.StartsWith(desktop, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(desktop))
                {
                    return "%USERPROFILE%\\Desktop" + path.Substring(desktop.Length);
                }
                if (path.StartsWith(programFiles, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(programFiles))
                {
                    return "%ProgramFiles%" + path.Substring(programFiles.Length);
                }
                if (path.StartsWith(programFilesX86, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(programFilesX86))
                {
                    return "%ProgramFiles(x86)%" + path.Substring(programFilesX86.Length);
                }
            }
            catch
            {
                // If anything fails, return original path
            }
            
            return path;
        }
        
        /// <summary>
        /// Expands environment variables in a path back to full path
        /// </summary>
        private string ExpandPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            
            try
            {
                return Environment.ExpandEnvironmentVariables(path);
            }
            catch
            {
                return path;
            }
        }
        
        // ConnectControlsToStyleManager() removed - not needed with StyleManager pattern
        // StyleManager automatically propagates to all controls with Style=Default and Theme=Default
        
        private void ConfigLocationsDialog_Load(object sender, EventArgs e)
        {
            // Ensure form and panels use theme background color
            // Only update if form is fully loaded and styleManager is available
            if (styleManager != null && this.IsHandleCreated)
            {
                UpdateThemeBackgrounds();
            }
        }
        
        private void UpdateThemeBackgrounds()
        {
            if (styleManager == null || this.IsDisposed || !this.IsHandleCreated) return;
            
            try
            {
                // Set form background color based on theme
                this.BackColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Form(styleManager.Theme);
                
                // Update all panels and controls
                UpdateAllControlsRecursive(this);
                
                // Force a refresh
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    this.Invalidate();
                    this.Update();
                }
            }
            catch
            {
                // Silently ignore errors during theme update (form might be disposing)
            }
        }
        
        private void SyncAllControlsWithStyleManager()
        {
            if (styleManager == null || this.IsDisposed || !this.IsHandleCreated) return;
            
            try
            {
                // Sync form properties
                if (this.StyleManager != styleManager)
                {
                    this.StyleManager = styleManager;
                }
                if (this.Theme != styleManager.Theme)
                {
                    this.Theme = styleManager.Theme;
                }
                if (this.Style != styleManager.Style)
                {
                    this.Style = styleManager.Style;
                }
                
                // Sync all controls recursively
                SyncControlsRecursive(this);
                
                // Update theme backgrounds
                UpdateThemeBackgrounds();
            }
            catch
            {
                // Silently ignore errors during sync (form might be disposing)
            }
        }
        
        private void SyncControlsRecursive(Control parent)
        {
            if (parent == null || styleManager == null) return;
            
            // Check if Controls collection is available
            if (parent.Controls == null) return;
            
            // Create a copy of the collection to avoid modification during iteration
            Control[] controls = new Control[parent.Controls.Count];
            parent.Controls.CopyTo(controls, 0);
            
            foreach (Control ctrl in controls)
            {
                if (ctrl == null) continue;
                
                // Sync IPoisonControl controls
                if (ctrl is IPoisonControl poisonCtrl)
                {
                    if (poisonCtrl.StyleManager != styleManager)
                    {
                        poisonCtrl.StyleManager = styleManager;
                    }
                    
                    // Ensure Style and Theme are Default to follow StyleManager
                    var styleProp = ctrl.GetType().GetProperty("Style");
                    var themeProp = ctrl.GetType().GetProperty("Theme");
                    
                    if (styleProp != null && styleProp.CanWrite)
                    {
                        var currentStyle = styleProp.GetValue(ctrl);
                        if (currentStyle == null || currentStyle.ToString() != "Default")
                        {
                            styleProp.SetValue(ctrl, ColorStyle.Default);
                        }
                    }
                    
                    if (themeProp != null && themeProp.CanWrite)
                    {
                        var currentTheme = themeProp.GetValue(ctrl);
                        if (currentTheme == null || currentTheme.ToString() != "Default")
                        {
                            themeProp.SetValue(ctrl, ThemeStyle.Default);
                        }
                    }
                    
                    // Ensure UseStyleColors is enabled
                    var useStyleColorsProp = ctrl.GetType().GetProperty("UseStyleColors");
                    if (useStyleColorsProp != null && useStyleColorsProp.CanWrite)
                    {
                        useStyleColorsProp.SetValue(ctrl, true);
                    }
                    
                    ctrl.Invalidate();
                }
                
                // Sync IPoisonComponent controls (like menus)
                if (ctrl is IPoisonComponent poisonComponent)
                {
                    if (poisonComponent.StyleManager != styleManager)
                    {
                        poisonComponent.StyleManager = styleManager;
                    }
                    ctrl.Invalidate();
                }
                
                // Recursively sync child controls
                if (ctrl.HasChildren)
                {
                    SyncControlsRecursive(ctrl);
                }
            }
        }
        
        private void UpdateAllControlsRecursive(Control parent)
        {
            if (parent == null || styleManager == null) return;
            
            // Check if Controls collection is available
            if (parent.Controls == null) return;
            
            // Update panels background
            if (parent is PoisonPanel panel)
            {
                panel.BackColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Form(styleManager.Theme);
            }
            
            // Create a copy of the collection to avoid modification during iteration
            Control[] controls = new Control[parent.Controls.Count];
            parent.Controls.CopyTo(controls, 0);
            
            // Recursively update all child controls
            foreach (Control ctrl in controls)
            {
                if (ctrl != null)
                {
                    UpdateAllControlsRecursive(ctrl);
                }
            }
        }
        
        private void SetupButtonEffects()
        {
            // Setup hover/pressed effects for all buttons in the form
            SetupButtonHoverEffectsRecursive(this);
        }
        
        private void SetupButtonHoverEffectsRecursive(Control parent)
        {
            // Use centralized helper for basic button effects
            PoisonControlHelper.SetupButtonEffectsRecursive(parent);
            
            // Then apply custom rainbow effects if needed
            foreach (Control ctrl in parent.Controls)
            {
                if (ctrl is PoisonButton btn)
                {
                    // Apply custom rainbow effects on top of basic effects
                    SetupButtonHoverEffects(btn);
                }
                
                if (ctrl.HasChildren)
                {
                    SetupButtonHoverEffectsRecursive(ctrl);
                }
            }
        }
        
        private void SetupButtonHoverEffects(PoisonButton poisonBtn)
        {
            if (poisonBtn == null) return;
            
            // Get reflection info for internal state fields
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
                    styleColor = GetCurrentRainbowColor();
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
            
            // Subscribe to CustomPaintForeground to draw rainbow border and text
            poisonBtn.CustomPaintForeground += (s, e) =>
            {
                if (IsRainbowStyleActive())
                {
                    Color rainbowColor = GetCurrentRainbowColor();
                    
                    // Draw rainbow border
                    using (Pen rainbowPen = new Pen(rainbowColor, 1))
                    {
                        Rectangle borderRect = poisonBtn.ClientRectangle;
                        borderRect.Width -= 1;
                        borderRect.Height -= 1;
                        e.Graphics.DrawRectangle(rainbowPen, borderRect);
                    }
                    
                    // Draw rainbow text
                    if (!string.IsNullOrEmpty(poisonBtn.Text))
                    {
                        var fontSizeProp = poisonBtn.GetType().GetProperty("FontSize");
                        var fontWeightProp = poisonBtn.GetType().GetProperty("FontWeight");
                        Font buttonFont = ReaLTaiizor.Extension.Poison.PoisonFonts.Button(
                            fontSizeProp != null ? (ReaLTaiizor.Extension.Poison.PoisonButtonSize)fontSizeProp.GetValue(poisonBtn) : ReaLTaiizor.Extension.Poison.PoisonButtonSize.Medium,
                            fontWeightProp != null ? (ReaLTaiizor.Extension.Poison.PoisonButtonWeight)fontWeightProp.GetValue(poisonBtn) : ReaLTaiizor.Extension.Poison.PoisonButtonWeight.Regular
                        );
                        
                        // Get background color to erase original text
                        Color backColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Button.Normal(poisonBtn.Theme);
                        if (!poisonBtn.Enabled)
                        {
                            backColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Button.Disabled(poisonBtn.Theme);
                        }
                        
                        // Erase original text by filling text area
                        Size textSize = TextRenderer.MeasureText(e.Graphics, poisonBtn.Text, buttonFont, poisonBtn.ClientRectangle.Size, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        Rectangle eraseRect = new Rectangle(
                            (poisonBtn.Width - textSize.Width) / 2 - 2,
                            (poisonBtn.Height - textSize.Height) / 2 - 1,
                            textSize.Width + 4,
                            textSize.Height + 2
                        );
                        using (SolidBrush backBrush = new SolidBrush(backColor))
                        {
                            e.Graphics.FillRectangle(backBrush, eraseRect);
                        }
                        
                        // Draw rainbow text
                        TextRenderer.DrawText(e.Graphics, poisonBtn.Text, buttonFont, 
                            poisonBtn.ClientRectangle, rainbowColor, 
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                }
            };
            
            // Wire up mouse events to ensure proper state reset
            poisonBtn.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Left && isPressedField != null)
                {
                    isPressedField.SetValue(poisonBtn, false);
                    poisonBtn.Invalidate();
                }
            };
            
            poisonBtn.MouseLeave += (s, e) =>
            {
                if (isHoveredField != null)
                    isHoveredField.SetValue(poisonBtn, false);
                if (isPressedField != null)
                    isPressedField.SetValue(poisonBtn, false);
                poisonBtn.Invalidate();
            };
            
            poisonBtn.Click += (s, e) =>
            {
                if (isHoveredField != null)
                    isHoveredField.SetValue(poisonBtn, false);
                if (isPressedField != null)
                    isPressedField.SetValue(poisonBtn, false);
                poisonBtn.Invalidate();
            };
            
            // Add hover effect handlers
            poisonBtn.MouseEnter += (s, e) => 
            {
                if (poisonBtn.Enabled)
                {
                    poisonBtn.Cursor = Cursors.Hand;
                    poisonBtn.Invalidate();
                }
            };
        }
        
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // Clean up timers
            if (rainbowUpdateTimer != null)
            {
                rainbowUpdateTimer.Stop();
                rainbowUpdateTimer.Dispose();
                rainbowUpdateTimer = null;
            }
            
            if (syncTimer != null)
            {
                syncTimer.Stop();
                syncTimer.Dispose();
                syncTimer = null;
            }
            
            base.OnFormClosed(e);
        }
        
        private void ResetButtonState(object sender)
        {
            // Use centralized helper
            PoisonControlHelper.ResetButtonState(sender);
        }
        
        private void LoadCurrentPaths()
        {
            // Null check controls before accessing
            if (txtConfigPath != null)
                txtConfigPath.Text = ShortenPath(ConfigPath ?? "");
            if (txtTempPath != null)
                txtTempPath.Text = ShortenPath(TempPath ?? Path.GetTempPath());
            if (txtDllExtractPath != null)
                txtDllExtractPath.Text = ShortenPath(DllExtractPath ?? "");
            if (txtAppDataRoaming != null)
                txtAppDataRoaming.Text = ShortenPath(AppDataRoaming ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        }
        
        private void BtnBrowseAppDataRoaming_Click(object sender, EventArgs e)
        {
            ResetButtonState(sender);
            if (txtAppDataRoaming == null) return;
            
            try
            {
                string appDataRoamingPath = ExpandPath(txtAppDataRoaming.Text);
                if (Directory.Exists(appDataRoamingPath))
                {
                    Process.Start("explorer.exe", appDataRoamingPath);
                }
            }
            catch (Exception ex)
            {
                PoisonMessageBox.Show(this, $"Error opening AppData (Roaming) folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void BtnBrowseConfig_Click(object sender, EventArgs e)
        {
            ResetButtonState(sender);
            if (txtConfigPath == null) return;
            
            try
            {
                string configPath = ExpandPath(txtConfigPath.Text);
                string configDir = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(configDir) && Directory.Exists(configDir))
                {
                    Process.Start("explorer.exe", configDir);
                }
                else if (File.Exists(configPath))
                {
                    Process.Start("explorer.exe", $"/select,\"{configPath}\"");
                }
            }
            catch (Exception ex)
            {
                PoisonMessageBox.Show(this, $"Error opening config folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void BtnBrowseTemp_Click(object sender, EventArgs e)
        {
            ResetButtonState(sender);
            if (txtTempPath == null) return;
            
            string initialPath = ExpandPath(txtTempPath.Text);
            string folder = ShowModernFolderDialog("Select temporary file location", initialPath);
            if (!string.IsNullOrEmpty(folder))
            {
                txtTempPath.Text = ShortenPath(folder);
            }
        }
        
        private void BtnBrowseDll_Click(object sender, EventArgs e)
        {
            ResetButtonState(sender);
            if (txtDllExtractPath == null) return;
            
            string initialPath = ExpandPath(txtDllExtractPath.Text);
            string folder = ShowModernFolderDialog("Select DLL extract location", initialPath);
            if (!string.IsNullOrEmpty(folder))
            {
                txtDllExtractPath.Text = ShortenPath(folder);
            }
        }
        
        private string ShowModernFolderDialog(string title, string initialPath = null)
        {
            return ModernFolderDialog.Show(this, title, initialPath);
        }
        
        
        private void BtnOK_Click(object sender, EventArgs e)
        {
            ResetButtonState(sender);
            
            // Expand paths before validation and saving
            string tempPath = txtTempPath != null ? ExpandPath(txtTempPath.Text) : "";
            string dllExtractPath = txtDllExtractPath != null ? ExpandPath(txtDllExtractPath.Text) : "";
            
            // Validate paths
            if (!string.IsNullOrWhiteSpace(tempPath) && !Directory.Exists(tempPath))
            {
                try
                {
                    Directory.CreateDirectory(tempPath);
                }
                catch (Exception ex)
                {
                    PoisonMessageBox.Show(this, $"Cannot create temp directory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            
            if (!string.IsNullOrWhiteSpace(dllExtractPath) && !Directory.Exists(dllExtractPath))
            {
                try
                {
                    Directory.CreateDirectory(dllExtractPath);
                }
                catch (Exception ex)
                {
                    PoisonMessageBox.Show(this, $"Cannot create DLL extract directory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            
            // Save expanded paths (full paths, not shortened)
            TempPath = tempPath;
            DllExtractPath = dllExtractPath;
            
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose components (from Designer.cs)
                if (components != null)
                {
                    components.Dispose();
                }
                
                // Dispose rainbow update timer
                if (rainbowUpdateTimer != null)
                {
                    rainbowUpdateTimer.Stop();
                    rainbowUpdateTimer.Dispose();
                    rainbowUpdateTimer = null;
                }
                
                // Dispose sync timer
                if (syncTimer != null)
                {
                    syncTimer.Stop();
                    syncTimer.Dispose();
                    syncTimer = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}

