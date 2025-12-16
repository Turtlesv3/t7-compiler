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
using ReaLTaiizor.Extension.Poison;
using ReaLTaiizorExt = ReaLTaiizor.Extension.Poison;
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
            return parentForm != null ? parentForm.currentRainbowColor : ReaLTaiizor.Drawing.Poison.PoisonPaint.GetStyleColor(ReaLTaiizor.Enum.Poison.ColorStyle.Red);
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
            
            // Load form icon
            T7CompilerGUI.Helpers.FormIconHelper.LoadFormIcon(this);
            
            // PoisonForm already sets ControlStyles in its constructor (OptimizedDoubleBuffer, ResizeRedraw, etc.)
            // We don't need to override them - PoisonForm handles smooth resizing internally
            
            LoadCurrentPaths();
            
            this.Load += ConfigLocationsDialog_Load;
            
            // Use PoisonFormHelper for standardized form initialization (includes SetupAllButtonEffectsRecursive)
            if (styleManager != null)
            {
                ReaLTaiizorExt.PoisonFormHelper.InitializeForm(this, styleManager);
            }
            
            // Setup rainbow update timer if parent form is available
            SetupRainbowUpdateTimer();
            
            // Subscribe to StyleManager updates to keep in sync (for rainbow theme updates)
            if (styleManager != null)
            {
                // Sync periodically to catch any changes using tracked timer
                syncTimer = ReaLTaiizorExt.PoisonFormHelper.CreateTrackedTimer(this, 100, (s, e) => 
                {
                    if (!this.IsDisposed && this.IsHandleCreated)
                    {
                        SyncAllControlsWithStyleManager();
                    }
                });
                syncTimer.Start();
            }
        }
        
        private void SetupRainbowUpdateTimer()
        {
            if (parentForm == null) return;
            
            // Create a tracked timer to invalidate buttons when rainbow is active
            rainbowUpdateTimer = ReaLTaiizorExt.PoisonFormHelper.CreateTrackedTimer(this, 16, (s, e) =>
            {
                if (IsRainbowStyleActive())
                {
                    // Invalidate all buttons to update rainbow colors
                    InvalidateAllButtons(this);
                }
            });
            
            // Always start the timer - it will only invalidate when rainbow is active
            rainbowUpdateTimer.Start();
        }
        
        private void InvalidateAllButtons(Control parent)
        {
            // Use helper to refresh all controls (includes invalidation)
            if (parent != null)
            {
                ReaLTaiizorExt.PoisonControlHelper.RefreshAllControls(parent);
            }
        }
        
        private void SetupControls()
        {
            // Controls are now created in InitializeComponent (Designer file)
            // This method just wires up event handlers
            
            // Null check all controls before wiring up events
            if (btnBrowseConfig != null)
                btnBrowseConfig.Click += BtnBrowseConfig_Click;
            if (btnBrowseTemp != null)
                btnBrowseTemp.Click += BtnBrowseTemp_Click;
            if (btnBrowseDll != null)
                btnBrowseDll.Click += BtnBrowseDll_Click;
            if (btnBrowseAppDataRoaming != null)
                btnBrowseAppDataRoaming.Click += BtnBrowseAppDataRoaming_Click;
            
            // Wire up button event handlers (buttons are created in Designer)
            if (btnOK != null)
                btnOK.Click += BtnOK_Click;
            if (btnCancel != null)
                btnCancel.Click += BtnCancel_Click;
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
                this.BackColor = PoisonPaint.BackColor.Form(styleManager.Theme);
                
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
                // Use helper to sync all controls - handles Theme, Style, UseStyleColors automatically
                ReaLTaiizorExt.PoisonControlHelper.ApplyStyleManager(this, styleManager);
                // Use PoisonControlHelper to sync all controls recursively
                // This handles StyleManager, Theme, Style, and UseStyleColors automatically
                ReaLTaiizorExt.PoisonControlHelper.ApplyStyleManager(this, styleManager);
                
                // Update theme backgrounds
                UpdateThemeBackgrounds();
            }
            catch
            {
                // Silently ignore errors during sync (form might be disposing)
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
                panel.BackColor = PoisonPaint.BackColor.Form(styleManager.Theme);
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
            ReaLTaiizor.Extension.Poison.PoisonControlHelper.SetupButtonEffectsRecursive(parent);
            
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
                    styleColor = PoisonPaint.GetStyleColor(poisonBtn.Style);
                }
                else
                {
                    return; // No color to apply
                }
                
                if (isHovered && !isPressed && poisonBtn.Enabled)
                {
                    // Use the style/rainbow color with transparency for hover overlay
                    // Use PoisonPaint to blend style color with background for semi-transparent effect
                    Color bgColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Form(styleManager.Theme);
                    Color semiTransparentStyle = ReaLTaiizor.Drawing.Poison.PoisonPaint.BlendColors(bgColor, styleColor, 0.3);
                    using (SolidBrush brush = new SolidBrush(semiTransparentStyle))
                    {
                        e.Graphics.FillRectangle(brush, poisonBtn.ClientRectangle);
                    }
                }
                else if (isHovered && isPressed && poisonBtn.Enabled)
                {
                    // Use a more opaque style/rainbow color for pressed state
                    // Use PoisonPaint to blend style color with background for more opaque effect
                    Color bgColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Form(styleManager.Theme);
                    Color moreOpaqueStyle = ReaLTaiizor.Drawing.Poison.PoisonPaint.BlendColors(bgColor, styleColor, 0.5);
                    using (SolidBrush brush = new SolidBrush(moreOpaqueStyle))
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
                        Color backColor = PoisonPaint.BackColor.Button.Normal(poisonBtn.Theme);
                        if (!poisonBtn.Enabled)
                        {
                            backColor = PoisonPaint.BackColor.Button.Disabled(poisonBtn.Theme);
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
        
        
        private void LoadCurrentPaths()
        {
            // Null check controls before accessing
            if (txtConfigPath != null)
                txtConfigPath.Text = Helpers.PathHelper.ShortenPath(ConfigPath ?? "");
            if (txtTempPath != null)
                txtTempPath.Text = Helpers.PathHelper.ShortenPath(TempPath ?? Path.GetTempPath());
            if (txtDllExtractPath != null)
                txtDllExtractPath.Text = Helpers.PathHelper.ShortenPath(DllExtractPath ?? "");
            if (txtAppDataRoaming != null)
                txtAppDataRoaming.Text = Helpers.PathHelper.ShortenPath(AppDataRoaming ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        }
        
        private void BtnBrowseAppDataRoaming_Click(object sender, EventArgs e)
        {
            if (txtAppDataRoaming == null) return;
            
            try
            {
                string appDataRoamingPath = Helpers.PathHelper.ExpandPath(txtAppDataRoaming.Text);
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
            if (txtConfigPath == null) return;
            
            try
            {
                string configPath = Helpers.PathHelper.ExpandPath(txtConfigPath.Text);
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
            if (txtTempPath == null) return;
            
            string initialPath = Helpers.PathHelper.ExpandPath(txtTempPath.Text);
            string folder = ModernFolderDialog.Show(this, "Select temporary file location", initialPath);
            if (!string.IsNullOrEmpty(folder))
            {
                txtTempPath.Text = Helpers.PathHelper.ShortenPath(folder);
            }
        }
        
        private void BtnBrowseDll_Click(object sender, EventArgs e)
        {
            if (txtDllExtractPath == null) return;
            
            string initialPath = Helpers.PathHelper.ExpandPath(txtDllExtractPath.Text);
            string folder = ModernFolderDialog.Show(this, "Select DLL extract location", initialPath);
            if (!string.IsNullOrEmpty(folder))
            {
                txtDllExtractPath.Text = Helpers.PathHelper.ShortenPath(folder);
            }
        }
        
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }
        
        private void BtnOK_Click(object sender, EventArgs e)
        {
            string tempPath = txtTempPath != null ? Helpers.PathHelper.ExpandPath(txtTempPath.Text) : "";
            string dllExtractPath = txtDllExtractPath != null ? Helpers.PathHelper.ExpandPath(txtDllExtractPath.Text) : "";
            
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
                // Use PoisonFormHelper for standardized cleanup (disposes tracked resources)
                ReaLTaiizorExt.PoisonFormHelper.CleanupForm(this);
                
                // Dispose timers
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


