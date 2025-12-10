using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Enum.Poison;
using ReaLTaiizor.Extension.Poison;

namespace T7CompilerGUI.Forms.Dialogs
{
    partial class ConfigLocationsDialog
    {
        protected System.ComponentModel.IContainer components = null;
        
        // Control declarations - these will be editable in the designer
        private PoisonPanel mainPanel;
        private PoisonLabel lblConfig;
        private PoisonTextBox txtConfigPath;
        private PoisonButton btnBrowseConfig;
        private PoisonLabel lblTemp;
        private PoisonTextBox txtTempPath;
        private PoisonButton btnBrowseTemp;
        private PoisonLabel lblDll;
        private PoisonTextBox txtDllExtractPath;
        private PoisonButton btnBrowseDll;
        private PoisonLabel lblAppDataRoaming;
        private PoisonTextBox txtAppDataRoaming;
        private PoisonButton btnBrowseAppDataRoaming;
        private PoisonLabel lblInfo;
        private PoisonPanel buttonPanel;
        private PoisonButton btnOK;
        private PoisonButton btnCancel;

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.mainPanel = new PoisonPanel();
            this.lblConfig = new PoisonLabel();
            this.txtConfigPath = new PoisonTextBox();
            this.btnBrowseConfig = new PoisonButton();
            this.lblTemp = new PoisonLabel();
            this.txtTempPath = new PoisonTextBox();
            this.btnBrowseTemp = new PoisonButton();
            this.lblDll = new PoisonLabel();
            this.txtDllExtractPath = new PoisonTextBox();
            this.btnBrowseDll = new PoisonButton();
            this.lblAppDataRoaming = new PoisonLabel();
            this.txtAppDataRoaming = new PoisonTextBox();
            this.btnBrowseAppDataRoaming = new PoisonButton();
            this.lblInfo = new PoisonLabel();
            this.buttonPanel = new PoisonPanel();
            this.btnOK = new PoisonButton();
            this.btnCancel = new PoisonButton();
            this.SuspendLayout();
            
            // 
            // mainPanel
            // 
            this.mainPanel.Dock = DockStyle.Fill;
            this.mainPanel.Padding = new Padding(20, 20, 30, 20);
            this.mainPanel.AutoScroll = true;
            this.mainPanel.Style = ColorStyle.Default;
            this.mainPanel.Theme = ThemeStyle.Default;
            
            int yPos = 20;
            const int spacing = 50;
            const int labelWidth = 150;
            const int textBoxWidth = 380;
            const int buttonWidth = 80;
            
            // 
            // lblConfig
            // 
            this.lblConfig.Text = "Config File:";
            this.lblConfig.Location = new Point(20, yPos);
            this.lblConfig.Size = new Size(labelWidth, 23);
            this.lblConfig.AutoSize = false;
            this.lblConfig.Style = ColorStyle.Default;
            this.lblConfig.Theme = ThemeStyle.Default;
            this.lblConfig.UseStyleColors = true;
            
            // 
            // txtConfigPath
            // 
            this.txtConfigPath.Location = new Point(180, yPos);
            this.txtConfigPath.Size = new Size(textBoxWidth, 23);
            this.txtConfigPath.ReadOnly = true;
            this.txtConfigPath.Style = ColorStyle.Default;
            this.txtConfigPath.Theme = ThemeStyle.Default;
            
            // 
            // btnBrowseConfig
            // 
            this.btnBrowseConfig.Text = "Open";
            this.btnBrowseConfig.Location = new Point(570, yPos);
            this.btnBrowseConfig.Size = new Size(buttonWidth, 23);
            this.btnBrowseConfig.Style = ColorStyle.Default;
            this.btnBrowseConfig.Theme = ThemeStyle.Default;
            this.btnBrowseConfig.UseStyleColors = true;
            
            yPos += spacing;
            
            // 
            // lblTemp
            // 
            this.lblTemp.Text = "Temp Location:";
            this.lblTemp.Location = new Point(20, yPos);
            this.lblTemp.Size = new Size(labelWidth, 23);
            this.lblTemp.AutoSize = false;
            this.lblTemp.Style = ColorStyle.Default;
            this.lblTemp.Theme = ThemeStyle.Default;
            this.lblTemp.UseStyleColors = true;
            
            // 
            // txtTempPath
            // 
            this.txtTempPath.Location = new Point(180, yPos);
            this.txtTempPath.Size = new Size(textBoxWidth, 23);
            this.txtTempPath.ReadOnly = false;
            this.txtTempPath.Style = ColorStyle.Default;
            this.txtTempPath.Theme = ThemeStyle.Default;
            
            // 
            // btnBrowseTemp
            // 
            this.btnBrowseTemp.Text = "Browse";
            this.btnBrowseTemp.Location = new Point(570, yPos);
            this.btnBrowseTemp.Size = new Size(buttonWidth, 23);
            this.btnBrowseTemp.Style = ColorStyle.Default;
            this.btnBrowseTemp.Theme = ThemeStyle.Default;
            this.btnBrowseTemp.UseStyleColors = true;
            
            yPos += spacing;
            
            // 
            // lblDll
            // 
            this.lblDll.Text = "DLL Extract Path:";
            this.lblDll.Location = new Point(20, yPos);
            this.lblDll.Size = new Size(labelWidth, 23);
            this.lblDll.AutoSize = false;
            this.lblDll.Style = ColorStyle.Default;
            this.lblDll.Theme = ThemeStyle.Default;
            this.lblDll.UseStyleColors = true;
            
            // 
            // txtDllExtractPath
            // 
            this.txtDllExtractPath.Location = new Point(180, yPos);
            this.txtDllExtractPath.Size = new Size(textBoxWidth, 23);
            this.txtDllExtractPath.ReadOnly = false;
            this.txtDllExtractPath.Style = ColorStyle.Default;
            this.txtDllExtractPath.Theme = ThemeStyle.Default;
            
            // 
            // btnBrowseDll
            // 
            this.btnBrowseDll.Text = "Browse";
            this.btnBrowseDll.Location = new Point(570, yPos);
            this.btnBrowseDll.Size = new Size(buttonWidth, 23);
            this.btnBrowseDll.Style = ColorStyle.Default;
            this.btnBrowseDll.Theme = ThemeStyle.Default;
            this.btnBrowseDll.UseStyleColors = true;
            
            yPos += spacing;
            
            // 
            // lblAppDataRoaming
            // 
            this.lblAppDataRoaming.Text = "AppData (Roaming):";
            this.lblAppDataRoaming.Location = new Point(20, yPos);
            this.lblAppDataRoaming.Size = new Size(labelWidth, 23);
            this.lblAppDataRoaming.AutoSize = false;
            this.lblAppDataRoaming.Style = ColorStyle.Default;
            this.lblAppDataRoaming.Theme = ThemeStyle.Default;
            this.lblAppDataRoaming.UseStyleColors = true;
            
            // 
            // txtAppDataRoaming
            // 
            this.txtAppDataRoaming.Location = new Point(180, yPos);
            this.txtAppDataRoaming.Size = new Size(textBoxWidth, 23);
            this.txtAppDataRoaming.ReadOnly = true;
            this.txtAppDataRoaming.Style = ColorStyle.Default;
            this.txtAppDataRoaming.Theme = ThemeStyle.Default;
            
            // 
            // btnBrowseAppDataRoaming
            // 
            this.btnBrowseAppDataRoaming.Text = "Open";
            this.btnBrowseAppDataRoaming.Location = new Point(570, yPos);
            this.btnBrowseAppDataRoaming.Size = new Size(buttonWidth, 23);
            this.btnBrowseAppDataRoaming.Style = ColorStyle.Default;
            this.btnBrowseAppDataRoaming.Theme = ThemeStyle.Default;
            this.btnBrowseAppDataRoaming.UseStyleColors = true;
            
            // 
            // lblInfo
            // 
            this.lblInfo.Text = "Note: DLL Extract Path is where Costura extracts embedded DLLs at runtime.";
            this.lblInfo.Location = new Point(20, yPos + 40);
            this.lblInfo.Size = new Size(630, 30);
            this.lblInfo.AutoSize = false;
            this.lblInfo.FontSize = PoisonLabelSize.Small;
            this.lblInfo.Style = ColorStyle.Default;
            this.lblInfo.Theme = ThemeStyle.Default;
            this.lblInfo.UseStyleColors = true;
            
            // 
            // buttonPanel
            // 
            this.buttonPanel.Dock = DockStyle.Bottom;
            this.buttonPanel.Height = 50;
            this.buttonPanel.Padding = new Padding(10, 10, 20, 10);
            this.buttonPanel.Style = ColorStyle.Default;
            this.buttonPanel.Theme = ThemeStyle.Default;
            
            // 
            // btnOK
            // 
            this.btnOK.Text = "OK";
            this.btnOK.Size = new Size(100, 30);
            this.btnOK.Location = new Point(10, 10);
            this.btnOK.Style = ColorStyle.Default;
            this.btnOK.Theme = ThemeStyle.Default;
            this.btnOK.UseStyleColors = true;
            
            // 
            // btnCancel
            // 
            this.btnCancel.Text = "Cancel";
            this.btnCancel.Size = new Size(100, 30);
            this.btnCancel.Location = new Point(10, 10);
            this.btnCancel.Style = ColorStyle.Default;
            this.btnCancel.Theme = ThemeStyle.Default;
            this.btnCancel.UseStyleColors = true;
            
            // 
            // ConfigLocationsDialog
            // 
            this.Text = "Application Locations";
            this.Size = new Size(700, 400);
            this.MinimumSize = new Size(600, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.ShadowType = FormShadowType.DropShadow;
            this.PoisonBorderStyle = ReaLTaiizor.Enum.Poison.FormBorderStyle.FixedSingle;
            
            // Add controls to panels
            this.mainPanel.Controls.Add(this.lblConfig);
            this.mainPanel.Controls.Add(this.txtConfigPath);
            this.mainPanel.Controls.Add(this.btnBrowseConfig);
            this.mainPanel.Controls.Add(this.lblTemp);
            this.mainPanel.Controls.Add(this.txtTempPath);
            this.mainPanel.Controls.Add(this.btnBrowseTemp);
            this.mainPanel.Controls.Add(this.lblDll);
            this.mainPanel.Controls.Add(this.txtDllExtractPath);
            this.mainPanel.Controls.Add(this.btnBrowseDll);
            this.mainPanel.Controls.Add(this.lblAppDataRoaming);
            this.mainPanel.Controls.Add(this.txtAppDataRoaming);
            this.mainPanel.Controls.Add(this.btnBrowseAppDataRoaming);
            this.mainPanel.Controls.Add(this.lblInfo);
            
            this.buttonPanel.Controls.Add(this.btnOK);
            this.buttonPanel.Controls.Add(this.btnCancel);
            
            // Add panels to form
            this.Controls.Add(this.mainPanel);
            this.Controls.Add(this.buttonPanel);
            
            this.ResumeLayout(false);
        }
    }
}

