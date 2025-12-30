using System;
using System.Drawing;
using System.Windows.Forms;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Enum.Poison;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Extension.Poison;
using ReaLTaiizor.Drawing.Poison;
using ReaLTaiizorExt = ReaLTaiizor.Extension.Poison;

namespace T7CompilerGUI.Forms.Dialogs
{
    /// <summary>
    /// Advanced Settings dialog with PropertyGrid for editing MainForm properties
    /// </summary>
    public partial class AdvancedSettingsDialog : PoisonForm
    {
        private Form parentForm;
        private PoisonStyleManager styleManager;
        private PoisonStyleExtender styleExtender;
        // propertyGrid is declared in Designer file
        private System.Windows.Forms.Timer syncTimer;
        private System.Windows.Forms.Timer rainbowTimer;
        private Func<bool> isRainbowStyleActive;
        private Func<Color> getCurrentRainbowColor;
        private Action<Control> resetButtonState;
        private Action<System.Windows.Forms.PropertyGrid, PoisonStyleManager> applyPoisonThemeToPropertyGrid;
        private Action<Form> syncAdvancedDialogControls;

        public AdvancedSettingsDialog(
            Form parentForm,
            object selectedObject,
            PoisonStyleManager styleManager,
            PoisonStyleExtender styleExtender,
            Func<bool> isRainbowStyleActive,
            Func<Color> getCurrentRainbowColor,
            Action<Control> resetButtonState,
            Action<System.Windows.Forms.PropertyGrid, PoisonStyleManager> applyPoisonThemeToPropertyGrid,
            Action<Form> syncAdvancedDialogControls)
        {
            this.parentForm = parentForm;
            this.styleManager = styleManager;
            this.styleExtender = styleExtender;
            this.isRainbowStyleActive = isRainbowStyleActive;
            this.getCurrentRainbowColor = getCurrentRainbowColor;
            this.resetButtonState = resetButtonState;
            this.applyPoisonThemeToPropertyGrid = applyPoisonThemeToPropertyGrid;
            this.syncAdvancedDialogControls = syncAdvancedDialogControls;

            InitializeComponent();
            
            // Load form icon
            T7CompilerGUI.Helpers.FormIconHelper.LoadFormIcon(this);
            
            // Setup button panel layout handler (moved from Designer)
            SetupButtonPanelLayout();
            
            // Set PropertyGrid selected object
            if (selectedObject != null)
            {
                propertyGrid.SelectedObject = selectedObject;
            }
            
            // Apply theme to PropertyGrid
            if (applyPoisonThemeToPropertyGrid != null && styleManager != null)
            {
                applyPoisonThemeToPropertyGrid(propertyGrid, styleManager);
            }
            
            // Apply StyleExtender
            styleExtender?.SetApplyPoisonTheme(propertyGrid, true);
            
            // Setup button click handler
            btnClose.Click += BtnClose_Click;
            
            // Use PoisonFormHelper for standardized form initialization (includes SetupAllButtonEffectsRecursive)
            if (styleManager != null)
            {
                ReaLTaiizorExt.PoisonFormHelper.InitializeForm(this, styleManager);
                // Setup as modal dialog and center on parent
                ReaLTaiizorExt.PoisonFormHelper.SetupAsDialog(this, styleManager);
                if (parentForm != null)
                {
                    ReaLTaiizorExt.PoisonFormHelper.CenterForm(this, parentForm);
                }
            }
            
            // Setup rainbow effects for button
            SetupRainbowButtonEffects();
            
            // Setup timers
            SetupTimers();
        }

        private void SetupButtonPanelLayout()
        {
            // Position button correctly when panel resizes
            buttonPanel.Layout += (s, e) =>
            {
                int rightPadding = buttonPanel.Padding.Right;
                int buttonWidth = 100; // BUTTON_WIDTH constant
                int buttonTopOffset = 5; // BUTTON_TOP_OFFSET constant
                btnClose.Location = new Point(buttonPanel.Width - rightPadding - buttonWidth, buttonTopOffset);
            };
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void SetupRainbowButtonEffects()
        {
            btnClose.CustomPaintForeground += (sender, e) =>
            {
                if (styleManager != null && isRainbowStyleActive != null && isRainbowStyleActive())
                {
                    Color rainbowColor = getCurrentRainbowColor != null ? getCurrentRainbowColor() : ReaLTaiizor.Drawing.Poison.PoisonPaint.GetStyleColor(ReaLTaiizor.Enum.Poison.ColorStyle.White);
                    
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
                        Color backColor = PoisonPaint.BackColor.Button.Normal(btnClose.Theme);
                        if (!btnClose.Enabled)
                        {
                            backColor = PoisonPaint.BackColor.Button.Disabled(btnClose.Theme);
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
        }

        private void SetupTimers()
        {
            // Sync dialog with StyleManager periodically using tracked timer
            syncTimer = ReaLTaiizorExt.PoisonFormHelper.CreateTrackedTimer(this, 100, (s, e) =>
            {
                if (styleManager != null)
                {
                    // Use helper to apply StyleManager - handles Theme, Style, UseStyleColors automatically
                    ReaLTaiizorExt.PoisonControlHelper.ApplyStyleManager(this, styleManager);
                    
                    // Sync all controls
                    syncAdvancedDialogControls?.Invoke(this);
                }
            });
            syncTimer.Start();
            
            // Rainbow update timer - updates all controls when rainbow is active using tracked timer
            rainbowTimer = ReaLTaiizorExt.PoisonFormHelper.CreateTrackedTimer(this, 16, (s, e) =>
            {
                if (isRainbowStyleActive != null && isRainbowStyleActive())
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

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (syncTimer != null)
            {
                syncTimer.Stop();
                syncTimer.Dispose();
            }
            if (rainbowTimer != null)
            {
                rainbowTimer.Stop();
                rainbowTimer.Dispose();
            }
            base.OnFormClosed(e);
        }
        
    }
}

