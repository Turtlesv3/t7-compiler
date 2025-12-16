using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Enum.Poison;

namespace T7CompilerGUI.Forms.Dialogs
{
    partial class AdvancedSettingsDialog
    {
        private System.ComponentModel.IContainer components = null;
        
        // Control declarations - these will be editable in the designer
        private PoisonPanel mainPanel;
        private PoisonLabel lblInfo;
        internal System.Windows.Forms.PropertyGrid propertyGrid;
        private PoisonPanel buttonPanel;
        internal PoisonButton btnClose;
        
        // Constants for form sizing and positioning
        private const int DIALOG_WIDTH = 650;
        private const int DIALOG_HEIGHT = 550;
        private const int DIALOG_MIN_WIDTH = 550;
        private const int DIALOG_MIN_HEIGHT = 450;
        private const int MAIN_PANEL_PADDING_LEFT = 20;
        private const int MAIN_PANEL_PADDING_TOP = 20;
        private const int MAIN_PANEL_PADDING_RIGHT = 30;
        private const int MAIN_PANEL_PADDING_BOTTOM = 60;
        private const int INFO_LABEL_X = 20;
        private const int INFO_LABEL_Y = 20;
        private const int INFO_LABEL_WIDTH = 580;
        private const int INFO_LABEL_HEIGHT = 20;
        private const int PROPERTY_GRID_X = 20;
        private const int PROPERTY_GRID_Y = 50;
        private const int PROPERTY_GRID_WIDTH = 580;
        private const int PROPERTY_GRID_HEIGHT = 420;
        private const int BUTTON_PANEL_HEIGHT = 50;
        private const int BUTTON_PANEL_PADDING_LEFT = 10;
        private const int BUTTON_PANEL_PADDING_TOP = 10;
        private const int BUTTON_PANEL_PADDING_RIGHT = 20;
        private const int BUTTON_PANEL_PADDING_BOTTOM = 10;
        private const int BUTTON_WIDTH = 100;
        private const int BUTTON_HEIGHT = 35;
        private const int BUTTON_TOP_OFFSET = 5;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Use PoisonFormHelper for standardized cleanup (disposes tracked resources)
                ReaLTaiizor.Extension.Poison.PoisonFormHelper.CleanupForm(this);
                
                if ((components != null))
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.mainPanel = new ReaLTaiizor.Controls.PoisonPanel();
            this.propertyGrid = new System.Windows.Forms.PropertyGrid();
            this.lblInfo = new ReaLTaiizor.Controls.PoisonLabel();
            this.buttonPanel = new ReaLTaiizor.Controls.PoisonPanel();
            this.btnClose = new ReaLTaiizor.Controls.PoisonButton();
            this.mainPanel.SuspendLayout();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainPanel
            // 
            this.mainPanel.Controls.Add(this.propertyGrid);
            this.mainPanel.Controls.Add(this.lblInfo);
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.HorizontalScrollbarBarColor = true;
            this.mainPanel.HorizontalScrollbarHighlightOnWheel = false;
            this.mainPanel.HorizontalScrollbarSize = 10;
            this.mainPanel.Location = new System.Drawing.Point(20, 60);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Padding = new System.Windows.Forms.Padding(20, 20, 30, 60);
            this.mainPanel.Size = new System.Drawing.Size(610, 425);
            this.mainPanel.TabIndex = 0;
            this.mainPanel.UseStyleColors = true;
            this.mainPanel.VerticalScrollbarBarColor = true;
            this.mainPanel.VerticalScrollbarHighlightOnWheel = false;
            this.mainPanel.VerticalScrollbarSize = 10;
            // 
            // propertyGrid
            // 
            this.propertyGrid.Location = new System.Drawing.Point(20, 50);
            this.propertyGrid.Name = "propertyGrid";
            this.propertyGrid.Size = new System.Drawing.Size(557, 337);
            this.propertyGrid.TabIndex = 1;
            // 
            // lblInfo
            // 
            this.lblInfo.Location = new System.Drawing.Point(20, 20);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(580, 20);
            this.lblInfo.TabIndex = 0;
            this.lblInfo.Text = "Edit application properties directly. Changes may require restart.";
            this.lblInfo.UseStyleColors = true;
            // 
            // buttonPanel
            // 
            this.buttonPanel.Controls.Add(this.btnClose);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.buttonPanel.HorizontalScrollbarBarColor = true;
            this.buttonPanel.HorizontalScrollbarHighlightOnWheel = false;
            this.buttonPanel.HorizontalScrollbarSize = 10;
            this.buttonPanel.Location = new System.Drawing.Point(20, 485);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Padding = new System.Windows.Forms.Padding(10, 10, 20, 10);
            this.buttonPanel.Size = new System.Drawing.Size(610, 45);
            this.buttonPanel.TabIndex = 2;
            this.buttonPanel.VerticalScrollbarBarColor = true;
            this.buttonPanel.VerticalScrollbarHighlightOnWheel = false;
            this.buttonPanel.VerticalScrollbarSize = 10;
            // 
            // btnClose
            // 
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnClose.Location = new System.Drawing.Point(500, 6);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(100, 35);
            this.btnClose.TabIndex = 0;
            this.btnClose.Text = "Close";
            this.btnClose.UseSelectable = true;
            this.btnClose.UseStyleColors = true;
            // 
            // AdvancedSettingsDialog
            // 
            this.ClientSize = new System.Drawing.Size(650, 550);
            this.Controls.Add(this.mainPanel);
            this.Controls.Add(this.buttonPanel);
            this.MinimumSize = new System.Drawing.Size(550, 450);
            this.Name = "AdvancedSettingsDialog";
            this.PoisonBorderStyle = ReaLTaiizor.Enum.Poison.FormBorderStyle.FixedSingle;
            this.ShadowType = ReaLTaiizor.Enum.Poison.FormShadowType.DropShadow;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Advanced Settings";
            this.mainPanel.ResumeLayout(false);
            this.buttonPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}

