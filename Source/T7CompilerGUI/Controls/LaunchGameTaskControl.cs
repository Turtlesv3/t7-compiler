using System;
using System.Drawing;
using System.Windows.Forms;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Enum.Poison;
using ReaLTaiizor.Interface.Poison;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Extension.Poison;

namespace T7CompilerGUI.Controls
{
    /// <summary>
    /// Non-interrupting task window control for prompting to launch BO3
    /// Can be used as a UserControl (for designer) or embedded in PoisonTaskWindow
    /// </summary>
    public partial class LaunchGameTaskControl : PoisonUserControl
    {
        public event EventHandler LaunchClicked;
        public event EventHandler DismissClicked;

        public LaunchGameTaskControl()
        {
            InitializeComponent();
            SetupControlProperties();
            SetupLabelFont();
            SetupStyleColors();
            SetupButtonEffects();
            SetupScrollbars();
        }
        
        /// <summary>
        /// Sets UseStyleColors for controls that support it
        /// Uses PoisonControlHelper for better consistency
        /// </summary>
        private void SetupStyleColors()
        {
            // Set UseStyleColors for this control
            this.UseStyleColors = true;
            
            // Use helper to enable style colors for all controls recursively
            // Note: EnableStyleColorsForControls takes bool first, then params Control[]
            PoisonControlHelper.EnableStyleColorsForControls(true, this);
        }
        
        /// <summary>
        /// Sets up button effects (hover/pressed states) using PoisonControlHelper
        /// </summary>
        private void SetupButtonEffects()
        {
            if (StyleManager != null)
            {
                // Setup effects for all buttons in the control
                PoisonControlHelper.SetupAllButtonEffectsRecursive(this, StyleManager, true);
            }
            
            // Also setup individual buttons if needed
            if (btnLaunch != null)
            {
                PoisonControlHelper.SetupButtonEffects(btnLaunch, StyleManager, true);
            }
            if (btnDismiss != null)
            {
                PoisonControlHelper.SetupButtonEffects(btnDismiss, StyleManager, true);
            }
        }
        
        /// <summary>
        /// Configures scrollbars for panels to show only the thumb
        /// </summary>
        private void SetupScrollbars()
        {
            if (mainPanel != null)
            {
                // Use helper to configure scrollbars
                PoisonControlHelper.ConfigureScrollbars(mainPanel,
                    showVertical: true,
                    showHorizontal: true,
                    verticalThumbOnly: true,
                    horizontalThumbOnly: true);
            }
            
            if (buttonPanel != null)
            {
                // Button panel doesn't need scrollbars
                PoisonControlHelper.ConfigureScrollbars(buttonPanel,
                    showVertical: false,
                    showHorizontal: false);
            }
        }
        
        /// <summary>
        /// Sets up control properties for proper display
        /// </summary>
        private void SetupControlProperties()
        {
            // Control properties - works in both designer and runtime
            this.Dock = DockStyle.Fill;
            this.Size = new Size(400, 180);
            this.AutoSize = false;
            // No need to configure scrollbars here - handled by SetupScrollbars()
        }
        
        /// <summary>
        /// Sets the font for the message label at runtime
        /// </summary>
        private void SetupLabelFont()
        {
            if (lblMessage != null)
            {
                lblMessage.UseCustomFont = true;
                lblMessage.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                // Use theme-aware foreground color for visibility
                var styleManager = this.FindForm() is ReaLTaiizor.Forms.PoisonForm poisonForm && poisonForm.StyleManager != null
                    ? poisonForm.StyleManager
                    : null;
                var theme = styleManager?.Theme ?? ReaLTaiizor.Enum.Poison.ThemeStyle.Dark;
                lblMessage.ForeColor = ReaLTaiizor.Drawing.Poison.PoisonPaint.ForeColor.Label.Normal(theme); // Ensure text is visible
            }
        }

        private void btnLaunch_Click(object sender, EventArgs e)
        {
            LaunchClicked?.Invoke(this, EventArgs.Empty);
        }

        private void btnDismiss_Click(object sender, EventArgs e)
        {
            DismissClicked?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Sets the Poison StyleManager for this control and all child controls
        /// Since we inherit from PoisonUserControl, the StyleManager property is already available
        /// Uses PoisonControlHelper for better consistency
        /// </summary>
        public void SetStyleManager(PoisonStyleManager styleManager)
        {
            // Set StyleManager on this control (inherited from PoisonUserControl)
            this.StyleManager = styleManager;
            
            if (styleManager != null)
            {
                // Use helper to apply StyleManager to all controls recursively
                // This handles Theme, Style, and UseStyleColors automatically
                PoisonControlHelper.ApplyStyleManager(this, styleManager);
                
                // Setup button effects with the new StyleManager
                SetupButtonEffects();
            }
        }

        /// <summary>
        /// Gets the current StyleManager (inherited from PoisonUserControl)
        /// </summary>
        public PoisonStyleManager GetStyleManager()
        {
            return this.StyleManager;
        }

        /// <summary>
        /// Shows the launch game task window with proper configuration
        /// Uses this control directly in PoisonTaskWindow
        /// </summary>
        /// <param name="parentForm">Parent form for theme/style inheritance</param>
        /// <param name="launchAction">Action to execute when Launch is clicked</param>
        /// <param name="dismissAction">Action to execute when Dismiss is clicked</param>
        /// <param name="calculatePosition">Function to calculate window position</param>
        public static void Show(Form parentForm, Action launchAction, Action dismissAction, Func<Point> calculatePosition = null)
        {
            // Create the control instance
            var taskControl = new LaunchGameTaskControl();
            
            // Apply style manager if parent is a PoisonForm
            if (parentForm is IPoisonForm poisonForm && poisonForm.StyleManager != null)
            {
                // SetStyleManager already handles Theme and Style via ApplyStyleManager
                taskControl.SetStyleManager(poisonForm.StyleManager);
            }
            
            // Handle launch button click
            taskControl.LaunchClicked += (s, e) =>
            {
                // Cancel auto-close timer before launching
                PoisonTaskWindow.CancelAutoClose();
                launchAction?.Invoke();
                // Close the task window
                PoisonTaskWindow.ForceClose();
            };
            
            // Handle dismiss button click
            taskControl.DismissClicked += (s, e) =>
            {
                // Cancel auto-close timer before dismissing
                PoisonTaskWindow.CancelAutoClose();
                dismissAction?.Invoke();
                // Close the task window
                PoisonTaskWindow.ForceClose();
            };
            
            // Calculate desired position and size - ensure it fits all content
            Point targetLocation = calculatePosition != null 
                ? calculatePosition() 
                : new Point(Screen.PrimaryScreen.WorkingArea.Right - 400, Screen.PrimaryScreen.WorkingArea.Bottom - 180);
            Size targetSize = new Size(400, 180); // Size for better visibility

            // Show the task window with basic configuration
            // 5 seconds auto-dismiss timeout (secToClose parameter is in seconds)
            PoisonTaskWindow.ShowTaskWindow(
                parentForm, 
                "Launch Black Ops 3?", 
                taskControl, 
                5, // 5 seconds auto-dismiss timeout
                size: targetSize,
                location: targetLocation,
                maximizeBox: false,
                minimizeBox: false,
                resizable: false,
                useCustomSize: true,
                useCustomLocation: true);

            // Configure properties after window is shown using Shown event
            // We'll hook into the task window's Shown event to configure properties
            // Note: This is a user control, not a form, so we can't use CreateTrackedTimer
            // We'll manually dispose it in the tick handler
            System.Windows.Forms.Timer configTimer = new System.Windows.Forms.Timer
            {
                Interval = 50 // Small delay to ensure window is fully shown
            };
            configTimer.Tick += (s, e) =>
            {
                configTimer.Stop();
                configTimer.Dispose();
                
                var taskWindow = PoisonTaskWindow.Instance;
                if (taskWindow != null && !taskWindow.IsDisposed)
                {
                    // Set enhanced properties on the instance
                    taskWindow.CustomPadding = new Padding(0); // No extra padding, control handles it
                    taskWindow.ShowProgressBar = true; // Show progress bar for auto-dismiss
                    taskWindow.ProgressBarHeight = 5; // Standard progress bar height (default)
                    taskWindow.EnableAnimation = false; // Disable animation to prevent positioning issues
                    
                    // Ensure buttons reset state when window appears using helper
                    PoisonControlHelper.ResetAllButtonStates(taskControl);
                    
                    // Ensure all button effects are properly set up
                    if (taskControl.StyleManager != null)
                    {
                        PoisonControlHelper.SetupAllButtonEffectsRecursive(taskControl, taskControl.StyleManager, true);
                    }
                    
                    // Force refresh to update progress bar
                    taskWindow.Invalidate();
                }
            };
            configTimer.Start();
        }

        private void lblMessage_Click(object sender, EventArgs e)
        {

        }
    }
}
