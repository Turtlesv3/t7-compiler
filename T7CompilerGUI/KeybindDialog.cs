using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Enum.Poison;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Drawing.Poison;
using ReaLTaiizor.Interface.Poison;

namespace T7CompilerGUI
{
    public class KeybindDialog : PoisonForm
    {
        private PoisonStyleManager styleManager;
        private MainForm parentForm; // Reference to parent form for rainbow access
        private Dictionary<string, KeybindInfo> keybinds;
        private Dictionary<string, KeybindRow> keybindRows;
        private System.Windows.Forms.Timer rainbowUpdateTimer; // Timer to update UI when rainbow is active
        private System.Windows.Forms.Timer syncTimer; // Timer to sync with StyleManager
        
        public Dictionary<string, KeybindInfo> Keybinds => keybinds;
        
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
        
        public class KeybindInfo
        {
            public string Action { get; set; }
            public Keys Key { get; set; }
            public bool Ctrl { get; set; }
            public bool Shift { get; set; }
            public bool Alt { get; set; }
            
            public KeybindInfo(string action, Keys key, bool ctrl = false, bool shift = false, bool alt = false)
            {
                Action = action;
                Key = key;
                Ctrl = ctrl;
                Shift = shift;
                Alt = alt;
            }
            
            public override string ToString()
            {
                List<string> parts = new List<string>();
                if (Ctrl) parts.Add("Ctrl");
                if (Alt) parts.Add("Alt");
                if (Shift) parts.Add("Shift");
                parts.Add(Key.ToString());
                return string.Join(" + ", parts);
            }
        }
        
        private class KeybindRow
        {
            public PoisonLabel lblAction;
            public PoisonTextBox txtKeybind;
            public PoisonButton btnChange;
            public KeybindInfo info;
            public bool isCapturing = false;
        }
        
        public KeybindDialog(PoisonStyleManager styleManager, Dictionary<string, KeybindInfo> currentKeybinds) : this(styleManager, currentKeybinds, null)
        {
        }
        
        public KeybindDialog(PoisonStyleManager styleManager, Dictionary<string, KeybindInfo> currentKeybinds, MainForm parentForm)
        {
            this.styleManager = styleManager;
            this.parentForm = parentForm;
            this.keybinds = new Dictionary<string, KeybindInfo>(currentKeybinds);
            this.keybindRows = new Dictionary<string, KeybindRow>();
            
            // PoisonForm already sets ControlStyles in its constructor (OptimizedDoubleBuffer, ResizeRedraw, etc.)
            // We don't need to override them - PoisonForm handles smooth resizing internally
            
            SetupControls();
            InitializeKeybinds();
            
            this.Load += KeybindDialog_Load;
            
            // Setup hover and pressed effects for buttons
            SetupButtonEffects();
            
            // Setup rainbow update timer if parent form is available
            SetupRainbowUpdateTimer();
            
            // Subscribe to StyleManager updates to keep in sync
            if (styleManager != null)
            {
                // When StyleManager updates, sync all controls
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
            this.Text = "Keyboard Shortcuts";
            this.Size = new Size(600, 500);
            this.MinimumSize = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.ShadowType = FormShadowType.DropShadow;
            this.PoisonBorderStyle = ReaLTaiizor.Enum.Poison.FormBorderStyle.FixedSingle;
            
            // Set StyleManager - all controls will inherit automatically (matching demo pattern)
            if (styleManager != null)
            {
                this.StyleManager = styleManager;
            }
            
            // Create panel for keybinds - controls inherit from StyleManager automatically
            var panel = new PoisonPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 20, 30, 20), // Extra right padding to prevent button cutoff
                AutoScroll = true,
                Style = ColorStyle.Default,
                Theme = ThemeStyle.Default
            };
            
            // Create buttons panel - controls inherit from StyleManager automatically
            var buttonPanel = new PoisonPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(10, 10, 20, 10), // Extra right padding to prevent button cutoff
                Style = ColorStyle.Default,
                Theme = ThemeStyle.Default
            };
            
            var btnReset = new PoisonButton
            {
                Text = "Reset to Defaults",
                Size = new Size(150, 30),
                Location = new Point(10, 10),
                Style = ColorStyle.Default,
                Theme = ThemeStyle.Default,
                UseStyleColors = true
            };
            btnReset.Click += BtnReset_Click;
            
            var btnCancel = new PoisonButton
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
            
            var btnOK = new PoisonButton
            {
                Text = "OK",
                Size = new Size(100, 30),
                Location = new Point(10, 10), // Will be positioned correctly after panel is added
                Style = ColorStyle.Default,
                Theme = ThemeStyle.Default,
                UseStyleColors = true
            };
            btnOK.Click += (s, e) => 
            {
                ResetButtonState(s);
                this.DialogResult = DialogResult.OK;
            };
            
            buttonPanel.Controls.Add(btnReset);
            buttonPanel.Controls.Add(btnCancel);
            buttonPanel.Controls.Add(btnOK);
            
            // Position buttons correctly after panel is added (accounting for padding)
            buttonPanel.Layout += (s, e) =>
            {
                int rightPadding = buttonPanel.Padding.Right;
                int buttonSpacing = 10;
                int buttonWidth = 100;
                
                btnOK.Location = new Point(buttonPanel.Width - rightPadding - buttonWidth, 10);
                btnCancel.Location = new Point(buttonPanel.Width - rightPadding - buttonWidth - buttonWidth - buttonSpacing, 10);
            };
            
            this.Controls.Add(panel);
            this.Controls.Add(buttonPanel);
            
            // StyleManager already set above - all controls with Style=Default and Theme=Default inherit automatically
            // No need for ConnectControlsToStyleManager - StyleManager handles propagation (matching demo pattern)
            
            this.ResumeLayout(false);
        }
        
        // ConnectControlsToStyleManager() removed - not needed with StyleManager pattern
        // StyleManager automatically propagates to all controls with Style=Default and Theme=Default
        
        private void InitializeKeybinds()
        {
            var panel = this.Controls.OfType<PoisonPanel>().FirstOrDefault(p => p.Dock == DockStyle.Fill);
            if (panel == null) return;
            
            panel.Controls.Clear();
            keybindRows.Clear();
            
            int yPos = 10;
            const int rowHeight = 40;
            const int spacing = 5;
            
            foreach (var kvp in keybinds.OrderBy(k => k.Value.Action))
            {
                var row = new KeybindRow { info = kvp.Value };
                
                // Action label - inherits from StyleManager automatically
                row.lblAction = new PoisonLabel
                {
                    Text = kvp.Value.Action + ":",
                    Location = new Point(10, yPos),
                    Size = new Size(200, 25),
                    AutoSize = false,
                    Style = ColorStyle.Default,
                    Theme = ThemeStyle.Default,
                    UseStyleColors = true
                };
                
                // Keybind textbox (read-only display) - inherits from StyleManager automatically
                row.txtKeybind = new PoisonTextBox
                {
                    Text = kvp.Value.ToString(),
                    Location = new Point(220, yPos),
                    Size = new Size(190, 25),
                    ReadOnly = true,
                    WaterMark = "Click 'Change' to set",
                    Style = ColorStyle.Default,
                    Theme = ThemeStyle.Default
                };
                
                // Change button - inherits from StyleManager automatically
                row.btnChange = new PoisonButton
                {
                    Text = "Change",
                    Location = new Point(420, yPos),
                    Size = new Size(90, 25),
                    Style = ColorStyle.Default,
                    Theme = ThemeStyle.Default,
                    UseStyleColors = true
                };
                row.btnChange.Click += (s, e) => 
                {
                    ResetButtonState(s);
                    StartCapturingKeybind(row);
                };
                
                panel.Controls.Add(row.lblAction);
                panel.Controls.Add(row.txtKeybind);
                panel.Controls.Add(row.btnChange);
                
                keybindRows[kvp.Value.Action] = row;
                
                // Setup hover and pressed effects
                SetupButtonHoverEffects(row.btnChange);
                
                yPos += rowHeight + spacing;
            }
        }
        
        private void KeybindDialog_Load(object sender, EventArgs e)
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
            
            // Rainbow updates are handled by rainbowUpdateTimer
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
        
        private void StartCapturingKeybind(KeybindRow row)
        {
            row.isCapturing = true;
            row.btnChange.Text = "Press Key...";
            row.btnChange.Enabled = false;
            row.txtKeybind.Text = "Press a key combination...";
            row.txtKeybind.Focus();
            
            // Create a temporary key handler
            KeyEventHandler keyHandler = null;
            keyHandler = (s, e) =>
            {
                if (!row.isCapturing) return;
                
                // Don't capture modifier keys alone
                if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey || 
                    e.KeyCode == Keys.Menu || e.KeyCode == Keys.LControlKey || 
                    e.KeyCode == Keys.RControlKey || e.KeyCode == Keys.LShiftKey || 
                    e.KeyCode == Keys.RShiftKey || e.KeyCode == Keys.LMenu || 
                    e.KeyCode == Keys.RMenu)
                {
                    return;
                }
                
                // Capture the key combination
                row.info.Key = e.KeyCode;
                row.info.Ctrl = e.Control;
                row.info.Shift = e.Shift;
                row.info.Alt = e.Alt;
                
                row.txtKeybind.Text = row.info.ToString();
                row.isCapturing = false;
                row.btnChange.Text = "Change";
                row.btnChange.Enabled = true;
                
                // Update keybinds dictionary
                keybinds[row.info.Action] = row.info;
                
                // Remove handler
                this.KeyDown -= keyHandler;
                e.Handled = true;
            };
            
            this.KeyDown += keyHandler;
            this.KeyPreview = true;
        }
        
        private void BtnReset_Click(object sender, EventArgs e)
        {
            ResetButtonState(sender);
            // Reset to default keybinds
            keybinds = GetDefaultKeybinds();
            InitializeKeybinds();
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
        
        public static Dictionary<string, KeybindInfo> GetDefaultKeybinds()
        {
            return new Dictionary<string, KeybindInfo>
            {
                { "Compile", new KeybindInfo("Compile", Keys.C, ctrl: true) },
                { "Inject", new KeybindInfo("Inject", Keys.I, ctrl: true) },
                { "Open Project", new KeybindInfo("Open Project", Keys.O, ctrl: true) },
                { "Save Log", new KeybindInfo("Save Log", Keys.S, ctrl: true, shift: true) },
                { "Focus Search", new KeybindInfo("Focus Search", Keys.F, ctrl: true) },
                { "Clear Log", new KeybindInfo("Clear Log", Keys.L, ctrl: true) },
                { "Cycle Tabs", new KeybindInfo("Cycle Tabs", Keys.T, ctrl: true) }
            };
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

