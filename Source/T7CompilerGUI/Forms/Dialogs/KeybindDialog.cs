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
using ReaLTaiizorExt = ReaLTaiizor.Extension.Poison;

namespace T7CompilerGUI.Forms.Dialogs
{
    public partial class KeybindDialog : PoisonForm
    {
        private PoisonStyleManager styleManager;
        private Forms.MainForm parentForm; // Reference to parent form for rainbow access
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
            return parentForm != null ? parentForm.currentRainbowColor : ReaLTaiizor.Drawing.Poison.PoisonPaint.GetStyleColor(ReaLTaiizor.Enum.Poison.ColorStyle.Red);
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
        
        public KeybindDialog(PoisonStyleManager styleManager, Dictionary<string, KeybindInfo> currentKeybinds, Forms.MainForm parentForm)
        {
            this.styleManager = styleManager;
            this.parentForm = parentForm;
            this.keybinds = new Dictionary<string, KeybindInfo>(currentKeybinds);
            this.keybindRows = new Dictionary<string, KeybindRow>();
            
            // Initialize designer-generated controls
            InitializeComponent();
            
            // Load form icon
            T7CompilerGUI.Helpers.FormIconHelper.LoadFormIcon(this);
            
            // PoisonForm already sets ControlStyles in its constructor (OptimizedDoubleBuffer, ResizeRedraw, etc.)
            // We don't need to override them - PoisonForm handles smooth resizing internally
            
            SetupControls();
            InitializeKeybinds();
            
            this.Load += KeybindDialog_Load;
            
            // Setup rainbow update timer if parent form is available
            SetupRainbowUpdateTimer();
            
            // Subscribe to StyleManager updates to keep in sync
            if (styleManager != null)
            {
                // When StyleManager updates, sync all controls
                this.Shown += (s, e) => SyncAllControlsWithStyleManager();
                
                // Also sync periodically to catch any changes using tracked timer
                syncTimer = ReaLTaiizorExt.PoisonFormHelper.CreateTrackedTimer(this, 100, (s, e) => SyncAllControlsWithStyleManager());
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
            // This method just wires up event handlers if needed
            
            // Button event handlers are now wired up in Designer
            // Button positions are set using Anchor property in Designer
        }
        
        // StyleManager automatically propagates to all controls with Style=Default and Theme=Default
        
        private void InitializeKeybinds()
        {
            // Use the designer-created panel
            if (panel == null) return;
            
            panel.Controls.Clear();
            keybindRows.Clear();
            
            // Use constants from Designer file for sizes and positions
            int yPos = KEYBIND_ROW_START_Y;
            
            foreach (var kvp in keybinds.OrderBy(k => k.Value.Action))
            {
                var row = new KeybindRow { info = kvp.Value };
                
                // Action label - use enhanced helper
                row.lblAction = ReaLTaiizorExt.PoisonControlHelper.CreateLabel(
                    kvp.Value.Action + ":",
                    styleManager,
                    true,
                    ContentAlignment.MiddleLeft);
                row.lblAction.Location = new Point(KEYBIND_LABEL_X, yPos);
                row.lblAction.Size = new Size(KEYBIND_LABEL_WIDTH, KEYBIND_LABEL_HEIGHT);
                row.lblAction.AutoSize = false;
                
                // Keybind textbox (read-only display) - use enhanced helper
                row.txtKeybind = ReaLTaiizorExt.PoisonControlHelper.CreateTextBox(
                    kvp.Value.ToString(),
                    styleManager,
                    true);
                row.txtKeybind.Location = new Point(KEYBIND_TEXTBOX_X, yPos);
                row.txtKeybind.Size = new Size(KEYBIND_TEXTBOX_WIDTH, KEYBIND_TEXTBOX_HEIGHT);
                row.txtKeybind.ReadOnly = true;
                row.txtKeybind.WaterMark = "Click 'Change' to set";
                
                // Change button - use enhanced helper for better styling and effects
                row.btnChange = ReaLTaiizorExt.PoisonControlHelper.CreateButton(
                    "Change",
                    styleManager,
                    (s, e) => 
                    {
                        StartCapturingKeybind(row);
                    },
                    true);
                row.btnChange.Location = new Point(KEYBIND_BUTTON_X, yPos);
                row.btnChange.Size = new Size(KEYBIND_BUTTON_WIDTH, KEYBIND_BUTTON_HEIGHT);
                
                panel.Controls.Add(row.lblAction);
                panel.Controls.Add(row.txtKeybind);
                panel.Controls.Add(row.btnChange);
                
                keybindRows[kvp.Value.Action] = row;
                
                // Setup hover and pressed effects
                SetupButtonHoverEffects(row.btnChange);
                
                yPos += KEYBIND_ROW_HEIGHT + KEYBIND_ROW_SPACING;
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
            this.BackColor = PoisonPaint.BackColor.Form(styleManager.Theme);
            
            // Update all panels and controls
            UpdateAllControlsRecursive(this);
            
            // Force a refresh
            this.Invalidate();
            this.Update();
        }
        
        private void SyncAllControlsWithStyleManager()
        {
            if (styleManager == null || this.IsDisposed || !this.IsHandleCreated) return;
            
            try
            {
                // Use helper to sync all controls - handles Theme, Style, UseStyleColors automatically
                ReaLTaiizorExt.PoisonControlHelper.ApplyStyleManager(this, styleManager);
            }
            catch
            {
                // Silently ignore errors during sync (form might be disposing)
            }
        }
        
        private void UpdateAllControlsRecursive(Control parent)
        {
            if (parent == null || styleManager == null) return;
            
            // Update panels background
            if (parent is PoisonPanel panel)
            {
                panel.BackColor = PoisonPaint.BackColor.Form(styleManager.Theme);
            }
            
            // Recursively update all child controls
            foreach (Control ctrl in parent.Controls)
            {
                UpdateAllControlsRecursive(ctrl);
            }
        }
        
        private void SetupButtonEffects()
        {
            // Use PoisonFormHelper for standardized form initialization (includes SetupAllButtonEffectsRecursive)
            // This is called after controls are set up, so InitializeForm can be called here
            if (styleManager != null)
            {
                ReaLTaiizorExt.PoisonFormHelper.InitializeForm(this, styleManager);
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
        
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            keybinds = GetDefaultKeybinds();
            InitializeKeybinds();
        }
        
        private void BtnClear_Click(object sender, EventArgs e)
        {
            foreach (var kvp in keybinds.ToList())
            {
                keybinds[kvp.Key] = new KeybindInfo(kvp.Value.Action, Keys.None, false, false, false);
            }
            InitializeKeybinds();
        }
        
        
        public static Dictionary<string, KeybindInfo> GetDefaultKeybinds()
        {
            return new Dictionary<string, KeybindInfo>
            {
                { "Compile", new KeybindInfo("Compile", Keys.F3) },
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
                // Use PoisonFormHelper for standardized cleanup (disposes tracked resources)
                ReaLTaiizorExt.PoisonFormHelper.CleanupForm(this);
                
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

