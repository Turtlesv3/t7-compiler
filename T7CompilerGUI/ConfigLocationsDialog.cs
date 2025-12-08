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

namespace T7CompilerGUI
{
    public class ConfigLocationsDialog : PoisonForm
    {
        #region Windows API for Modern Folder Dialog
        
        private const uint FOS_PICKFOLDERS = 0x00000020;
        private const uint SIGDN_FILESYSPATH = 0x80058000;
        
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
            void SetOptions(uint fos);
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

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern uint SHParseDisplayName([MarshalAs(UnmanagedType.LPWStr)] string pszName, IntPtr pbc, out IntPtr ppidl, uint sfgaoIn, out uint psfgaoOut);

        [DllImport("shell32.dll", PreserveSig = false)]
        private static extern uint SHCreateItemFromIDList(IntPtr pidl, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);
        
        #endregion
        
        private PoisonStyleManager styleManager;
        private MainForm parentForm; // Reference to parent form for rainbow access
        private PoisonTextBox txtConfigPath;
        private PoisonTextBox txtTempPath;
        private PoisonTextBox txtDllExtractPath;
        private PoisonTextBox txtAppDataRoaming;
        private PoisonButton btnBrowseConfig;
        private PoisonButton btnBrowseTemp;
        private PoisonButton btnBrowseDll;
        private PoisonButton btnBrowseAppDataRoaming;
        private PoisonButton btnOK;
        private PoisonButton btnCancel;
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
        
        public ConfigLocationsDialog(PoisonStyleManager styleManager, string currentConfigPath, string currentTempPath, string currentDllExtractPath, string currentAppDataRoaming, MainForm parentForm)
        {
            this.styleManager = styleManager;
            this.parentForm = parentForm;
            this.ConfigPath = currentConfigPath;
            this.TempPath = currentTempPath;
            this.DllExtractPath = currentDllExtractPath;
            this.AppDataRoaming = currentAppDataRoaming ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            
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
                syncTimer.Tick += (s, e) => SyncAllControlsWithStyleManager();
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
            this.SuspendLayout();
            
            // Form properties - match demo pattern
            this.Text = "Application Locations";
            this.Size = new Size(700, 400);
            this.MinimumSize = new Size(600, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.ShadowType = FormShadowType.DropShadow;
            this.PoisonBorderStyle = ReaLTaiizor.Enum.Poison.FormBorderStyle.FixedSingle;
            
            // Set StyleManager - all controls will inherit automatically (matching demo pattern)
            if (styleManager != null)
            {
                this.StyleManager = styleManager;
            }
            
            // Main panel - controls inherit from StyleManager automatically
            var mainPanel = new PoisonPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 20, 30, 20), // Extra right padding to prevent button cutoff
                AutoScroll = true,
                Style = ColorStyle.Default,
                Theme = ThemeStyle.Default
            };
            
            int yPos = 20;
            const int spacing = 50;
            const int labelWidth = 150;
            const int textBoxWidth = 380;
            const int buttonWidth = 80;
            
            // Config File Path - controls inherit from StyleManager automatically
            var lblConfig = new PoisonLabel
            {
                Text = "Config File:",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 23),
                AutoSize = false,
                Style = ColorStyle.Default,
                Theme = ThemeStyle.Default,
                UseStyleColors = true
            };
            
            txtConfigPath = new PoisonTextBox
            {
                Location = new Point(180, yPos),
                Size = new Size(textBoxWidth, 23),
                ReadOnly = true,
                Style = ColorStyle.Default,
                Theme = ThemeStyle.Default
            };
            
            btnBrowseConfig = new PoisonButton
            {
                Text = "Open",
                Location = new Point(570, yPos),
                Size = new Size(buttonWidth, 23),
                Style = ColorStyle.Default,
                Theme = ThemeStyle.Default,
                UseStyleColors = true
            };
            btnBrowseConfig.Click += BtnBrowseConfig_Click;
            
            yPos += spacing;
            
            // Temp Path - controls inherit from StyleManager automatically
            var lblTemp = new PoisonLabel
            {
                Text = "Temp Location:",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 23),
                AutoSize = false,
                Style = ColorStyle.Default,
                Theme = ThemeStyle.Default,
                UseStyleColors = true
            };
            
            txtTempPath = new PoisonTextBox
            {
                Location = new Point(180, yPos),
                Size = new Size(textBoxWidth, 23),
                ReadOnly = false,
                Style = ColorStyle.Default,
                Theme = ThemeStyle.Default
            };
            
            btnBrowseTemp = new PoisonButton
            {
                Text = "Browse",
                Location = new Point(570, yPos),
                Size = new Size(buttonWidth, 23),
                Style = ColorStyle.Default,
                Theme = ThemeStyle.Default,
                UseStyleColors = true
            };
            btnBrowseTemp.Click += BtnBrowseTemp_Click;
            
            yPos += spacing;
            
            // DLL Extract Path - controls inherit from StyleManager automatically
            var lblDll = new PoisonLabel
            {
                Text = "DLL Extract Path:",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 23),
                AutoSize = false,
                Style = ColorStyle.Default,
                Theme = ThemeStyle.Default,
                UseStyleColors = true
            };
            
            txtDllExtractPath = new PoisonTextBox
            {
                Location = new Point(180, yPos),
                Size = new Size(textBoxWidth, 23),
                ReadOnly = false,
                Style = ColorStyle.Default,
                Theme = ThemeStyle.Default
            };
            
            btnBrowseDll = new PoisonButton
            {
                Text = "Browse",
                Location = new Point(570, yPos),
                Size = new Size(buttonWidth, 23),
                Style = ColorStyle.Default,
                Theme = ThemeStyle.Default,
                UseStyleColors = true
            };
            btnBrowseDll.Click += BtnBrowseDll_Click;
            
            yPos += spacing;
            
            // AppData (Roaming) Path - controls inherit from StyleManager automatically
            var lblAppDataRoaming = new PoisonLabel
            {
                Text = "AppData (Roaming):",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 23),
                AutoSize = false,
                Style = ColorStyle.Default,
                Theme = ThemeStyle.Default,
                UseStyleColors = true
            };
            
            txtAppDataRoaming = new PoisonTextBox
            {
                Location = new Point(180, yPos),
                Size = new Size(textBoxWidth, 23),
                ReadOnly = true,
                Style = ColorStyle.Default,
                Theme = ThemeStyle.Default
            };
            
            btnBrowseAppDataRoaming = new PoisonButton
            {
                Text = "Open",
                Location = new Point(570, yPos),
                Size = new Size(buttonWidth, 23),
                Style = ColorStyle.Default,
                Theme = ThemeStyle.Default,
                UseStyleColors = true
            };
            btnBrowseAppDataRoaming.Click += BtnBrowseAppDataRoaming_Click;
            
            // Info label - controls inherit from StyleManager automatically
            var lblInfo = new PoisonLabel
            {
                Text = "Note: DLL Extract Path is where Costura extracts embedded DLLs at runtime.",
                Location = new Point(20, yPos + 40),
                Size = new Size(630, 30),
                AutoSize = false,
                FontSize = ReaLTaiizor.Extension.Poison.PoisonLabelSize.Small,
                Style = ColorStyle.Default,
                Theme = ThemeStyle.Default,
                UseStyleColors = true
            };
            
            // Buttons panel - controls inherit from StyleManager automatically
            var buttonPanel = new PoisonPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(10, 10, 20, 10), // Extra right padding to prevent button cutoff
                Style = ColorStyle.Default,
                Theme = ThemeStyle.Default
            };
            
            btnOK = new PoisonButton
            {
                Text = "OK",
                Size = new Size(100, 30),
                Location = new Point(10, 10), // Will be positioned correctly after panel is added
                Style = ColorStyle.Default,
                Theme = ThemeStyle.Default,
                UseStyleColors = true
            };
            btnOK.Click += BtnOK_Click;
            
            btnCancel = new PoisonButton
            {
                Text = "Cancel",
                Size = new Size(100, 30),
                Location = new Point(10, 10), // Will be positioned correctly after panel is added
                Style = ColorStyle.Default,
                Theme = ThemeStyle.Default,
                UseStyleColors = true
            };
            btnCancel.Click += (s, e) => 
            {
                ResetButtonState(s);
                this.DialogResult = DialogResult.Cancel;
            };
            
            // Add controls to panels
            mainPanel.Controls.Add(lblConfig);
            mainPanel.Controls.Add(txtConfigPath);
            mainPanel.Controls.Add(btnBrowseConfig);
            mainPanel.Controls.Add(lblTemp);
            mainPanel.Controls.Add(txtTempPath);
            mainPanel.Controls.Add(btnBrowseTemp);
            mainPanel.Controls.Add(lblDll);
            mainPanel.Controls.Add(txtDllExtractPath);
            mainPanel.Controls.Add(btnBrowseDll);
            mainPanel.Controls.Add(lblAppDataRoaming);
            mainPanel.Controls.Add(txtAppDataRoaming);
            mainPanel.Controls.Add(btnBrowseAppDataRoaming);
            mainPanel.Controls.Add(lblInfo);
            
            buttonPanel.Controls.Add(btnOK);
            buttonPanel.Controls.Add(btnCancel);
            
            // Position buttons correctly after panel is added (accounting for padding)
            buttonPanel.Layout += (s, e) =>
            {
                int rightPadding = buttonPanel.Padding.Right;
                int buttonSpacing = 10;
                int dialogButtonWidth = 100;
                
                btnOK.Location = new Point(buttonPanel.Width - rightPadding - dialogButtonWidth, 10);
                btnCancel.Location = new Point(buttonPanel.Width - rightPadding - dialogButtonWidth - dialogButtonWidth - buttonSpacing, 10);
            };
            
            this.Controls.Add(mainPanel);
            this.Controls.Add(buttonPanel);
            
            // StyleManager already set above - all controls with Style=Default and Theme=Default inherit automatically
            // No need for ConnectControlsToStyleManager - StyleManager handles propagation (matching demo pattern)
            
            // Setup button hover effects after controls are created
            SetupButtonHoverEffects(btnBrowseConfig);
            SetupButtonHoverEffects(btnBrowseTemp);
            SetupButtonHoverEffects(btnBrowseDll);
            SetupButtonHoverEffects(btnBrowseAppDataRoaming);
            SetupButtonHoverEffects(btnOK);
            SetupButtonHoverEffects(btnCancel);
            
            this.ResumeLayout(false);
        }
        
        // ConnectControlsToStyleManager() removed - not needed with StyleManager pattern
        // StyleManager automatically propagates to all controls with Style=Default and Theme=Default
        
        private void ConfigLocationsDialog_Load(object sender, EventArgs e)
        {
            // Ensure form and panels use theme background color
            UpdateThemeBackgrounds();
        }
        
        private void UpdateThemeBackgrounds()
        {
            if (styleManager == null) return;
            
            // Set form background color based on theme
            this.BackColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Form(styleManager.Theme);
            
            // Update all panels and controls
            UpdateAllControlsRecursive(this);
            
            // Force a refresh
            this.Invalidate();
            this.Update();
        }
        
        private void SyncAllControlsWithStyleManager()
        {
            if (styleManager == null) return;
            
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
        
        private void SyncControlsRecursive(Control parent)
        {
            if (parent == null || styleManager == null) return;
            
            foreach (Control ctrl in parent.Controls)
            {
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
            
            // Update panels background
            if (parent is PoisonPanel panel)
            {
                panel.BackColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.BackColor.Form(styleManager.Theme);
            }
            
            // Recursively update all child controls
            foreach (Control ctrl in parent.Controls)
            {
                UpdateAllControlsRecursive(ctrl);
            }
        }
        
        private void SetupButtonEffects()
        {
            // Setup hover/pressed effects for all buttons in the form
            SetupButtonHoverEffectsRecursive(this);
        }
        
        private void SetupButtonHoverEffectsRecursive(Control parent)
        {
            foreach (Control ctrl in parent.Controls)
            {
                if (ctrl is PoisonButton btn)
                {
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
            // Handle PoisonButton
            if (sender is PoisonButton btn)
            {
                var isHoveredField = btn.GetType().GetField("isHovered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var isPressedField = btn.GetType().GetField("isPressed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (isHoveredField != null)
                    isHoveredField.SetValue(btn, false);
                if (isPressedField != null)
                    isPressedField.SetValue(btn, false);
                
                btn.Invalidate();
            }
            // Handle PoisonDropDownButton
            else if (sender is PoisonDropDownButton dropDown)
            {
                var isHoveredField = dropDown.GetType().GetField("isHovered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var isPressedField = dropDown.GetType().GetField("isPressed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (isHoveredField != null)
                    isHoveredField.SetValue(dropDown, false);
                if (isPressedField != null)
                    isPressedField.SetValue(dropDown, false);
                
                dropDown.Invalidate();
            }
        }
        
        private void LoadCurrentPaths()
        {
            txtConfigPath.Text = ConfigPath ?? "";
            txtTempPath.Text = TempPath ?? Path.GetTempPath();
            txtDllExtractPath.Text = DllExtractPath ?? "";
            txtAppDataRoaming.Text = AppDataRoaming ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }
        
        private void BtnBrowseAppDataRoaming_Click(object sender, EventArgs e)
        {
            ResetButtonState(sender);
            try
            {
                string appDataRoamingPath = txtAppDataRoaming.Text;
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
            try
            {
                string configDir = Path.GetDirectoryName(txtConfigPath.Text);
                if (Directory.Exists(configDir))
                {
                    Process.Start("explorer.exe", configDir);
                }
                else if (File.Exists(txtConfigPath.Text))
                {
                    Process.Start("explorer.exe", $"/select,\"{txtConfigPath.Text}\"");
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
            string folder = ShowModernFolderDialog("Select temporary file location", txtTempPath.Text);
            if (!string.IsNullOrEmpty(folder))
            {
                txtTempPath.Text = folder;
            }
        }
        
        private void BtnBrowseDll_Click(object sender, EventArgs e)
        {
            ResetButtonState(sender);
            string folder = ShowModernFolderDialog("Select DLL extract location", txtDllExtractPath.Text);
            if (!string.IsNullOrEmpty(folder))
            {
                txtDllExtractPath.Text = folder;
            }
        }
        
        private string ShowModernFolderDialog(string title, string initialPath = null)
        {
            try
            {
                var dialog = (IFileOpenDialog)new FileOpenDialogRCW();
                dialog.SetOptions(FOS_PICKFOLDERS);
                dialog.SetTitle(title);

                if (!string.IsNullOrWhiteSpace(initialPath) && Directory.Exists(initialPath))
                {
                    try
                    {
                        IntPtr pidl = IntPtr.Zero;
                        uint hr = SHParseDisplayName(initialPath, IntPtr.Zero, out pidl, 0, out _);
                        if (hr == 0 && pidl != IntPtr.Zero)
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

                uint result = dialog.Show(this.Handle);
                if (result == 0) // S_OK
                {
                    dialog.GetResult(out IShellItem item);
                    item.GetDisplayName(SIGDN_FILESYSPATH, out string path);
                    return path;
                }
            }
            catch
            {
                // Fallback to traditional folder dialog
                using (var dialog = new FolderBrowserDialog())
                {
                    dialog.Description = title;
                    if (!string.IsNullOrWhiteSpace(initialPath) && Directory.Exists(initialPath))
                        dialog.SelectedPath = initialPath;
                    
                    if (dialog.ShowDialog() == DialogResult.OK)
                        return dialog.SelectedPath;
                }
            }

            return null;
        }
        
        
        private void BtnOK_Click(object sender, EventArgs e)
        {
            ResetButtonState(sender);
            // Validate paths
            if (!string.IsNullOrWhiteSpace(txtTempPath.Text) && !Directory.Exists(txtTempPath.Text))
            {
                try
                {
                    Directory.CreateDirectory(txtTempPath.Text);
                }
                catch (Exception ex)
                {
                    PoisonMessageBox.Show(this, $"Cannot create temp directory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            
            if (!string.IsNullOrWhiteSpace(txtDllExtractPath.Text) && !Directory.Exists(txtDllExtractPath.Text))
            {
                try
                {
                    Directory.CreateDirectory(txtDllExtractPath.Text);
                }
                catch (Exception ex)
                {
                    PoisonMessageBox.Show(this, $"Cannot create DLL extract directory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            
            // Save values
            TempPath = txtTempPath.Text;
            DllExtractPath = txtDllExtractPath.Text;
            
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose rainbow update timer
                if (rainbowUpdateTimer != null)
                {
                    rainbowUpdateTimer.Stop();
                    rainbowUpdateTimer.Dispose();
                    rainbowUpdateTimer = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}

