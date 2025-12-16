using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Enum.Poison;
using ReaLTaiizor.Extension.Poison;
using ReaLTaiizor.Forms;

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
            this.mainPanel = new ReaLTaiizor.Controls.PoisonPanel();
            this.lblConfig = new ReaLTaiizor.Controls.PoisonLabel();
            this.txtConfigPath = new ReaLTaiizor.Controls.PoisonTextBox();
            this.btnBrowseConfig = new ReaLTaiizor.Controls.PoisonButton();
            this.lblTemp = new ReaLTaiizor.Controls.PoisonLabel();
            this.txtTempPath = new ReaLTaiizor.Controls.PoisonTextBox();
            this.btnBrowseTemp = new ReaLTaiizor.Controls.PoisonButton();
            this.lblDll = new ReaLTaiizor.Controls.PoisonLabel();
            this.txtDllExtractPath = new ReaLTaiizor.Controls.PoisonTextBox();
            this.btnBrowseDll = new ReaLTaiizor.Controls.PoisonButton();
            this.lblAppDataRoaming = new ReaLTaiizor.Controls.PoisonLabel();
            this.txtAppDataRoaming = new ReaLTaiizor.Controls.PoisonTextBox();
            this.btnBrowseAppDataRoaming = new ReaLTaiizor.Controls.PoisonButton();
            this.lblInfo = new ReaLTaiizor.Controls.PoisonLabel();
            this.buttonPanel = new ReaLTaiizor.Controls.PoisonPanel();
            this.btnOK = new ReaLTaiizor.Controls.PoisonButton();
            this.btnCancel = new ReaLTaiizor.Controls.PoisonButton();
            this.mainPanel.SuspendLayout();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainPanel
            // 
            this.mainPanel.AutoScroll = true;
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
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.HorizontalScrollbar = true;
            this.mainPanel.HorizontalScrollbarBarColor = true;
            this.mainPanel.HorizontalScrollbarHighlightOnWheel = false;
            this.mainPanel.HorizontalScrollbarSize = 10;
            this.mainPanel.Location = new System.Drawing.Point(20, 60);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Padding = new System.Windows.Forms.Padding(20, 20, 30, 20);
            this.mainPanel.Size = new System.Drawing.Size(660, 177);
            this.mainPanel.TabIndex = 0;
            this.mainPanel.UseStyleColors = true;
            this.mainPanel.VerticalScrollbar = true;
            this.mainPanel.VerticalScrollbarBarColor = true;
            this.mainPanel.VerticalScrollbarHighlightOnWheel = false;
            this.mainPanel.VerticalScrollbarSize = 10;
            // 
            // lblConfig
            // 
            this.lblConfig.Location = new System.Drawing.Point(54, 20);
            this.lblConfig.Name = "lblConfig";
            this.lblConfig.Size = new System.Drawing.Size(78, 23);
            this.lblConfig.TabIndex = 0;
            this.lblConfig.Text = "Config File:";
            this.lblConfig.UseStyleColors = true;
            // 
            // txtConfigPath
            // 
            // 
            // 
            // 
            this.txtConfigPath.CustomButton.Image = null;
            this.txtConfigPath.CustomButton.Location = new System.Drawing.Point(404, 1);
            this.txtConfigPath.CustomButton.Name = "";
            this.txtConfigPath.CustomButton.Size = new System.Drawing.Size(21, 21);
            this.txtConfigPath.CustomButton.Style = ReaLTaiizor.Enum.Poison.ColorStyle.Blue;
            this.txtConfigPath.CustomButton.TabIndex = 1;
            this.txtConfigPath.CustomButton.Theme = ReaLTaiizor.Enum.Poison.ThemeStyle.Light;
            this.txtConfigPath.CustomButton.UseSelectable = true;
            this.txtConfigPath.CustomButton.Visible = false;
            this.txtConfigPath.Lines = new string[0];
            this.txtConfigPath.Location = new System.Drawing.Point(138, 20);
            this.txtConfigPath.MaxLength = 32767;
            this.txtConfigPath.Name = "txtConfigPath";
            this.txtConfigPath.PasswordChar = '\0';
            this.txtConfigPath.ReadOnly = true;
            this.txtConfigPath.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.txtConfigPath.SelectedText = "";
            this.txtConfigPath.SelectionLength = 0;
            this.txtConfigPath.SelectionStart = 0;
            this.txtConfigPath.ShortcutsEnabled = true;
            this.txtConfigPath.Size = new System.Drawing.Size(426, 23);
            this.txtConfigPath.TabIndex = 1;
            this.txtConfigPath.UseSelectable = true;
            this.txtConfigPath.UseStyleColors = true;
            this.txtConfigPath.WaterMarkColor = System.Drawing.Color.FromArgb(((int)(((byte)(109)))), ((int)(((byte)(109)))), ((int)(((byte)(109)))));
            this.txtConfigPath.WaterMarkFont = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Pixel);
            // 
            // btnBrowseConfig
            // 
            this.btnBrowseConfig.Location = new System.Drawing.Point(570, 20);
            this.btnBrowseConfig.Name = "btnBrowseConfig";
            this.btnBrowseConfig.Size = new System.Drawing.Size(80, 23);
            this.btnBrowseConfig.TabIndex = 2;
            this.btnBrowseConfig.Text = "Open";
            this.btnBrowseConfig.UseSelectable = true;
            this.btnBrowseConfig.UseStyleColors = true;
            // 
            // lblTemp
            // 
            this.lblTemp.Location = new System.Drawing.Point(33, 52);
            this.lblTemp.Name = "lblTemp";
            this.lblTemp.Size = new System.Drawing.Size(99, 23);
            this.lblTemp.TabIndex = 3;
            this.lblTemp.Text = "Temp Location:";
            this.lblTemp.UseStyleColors = true;
            // 
            // txtTempPath
            // 
            // 
            // 
            // 
            this.txtTempPath.CustomButton.Image = null;
            this.txtTempPath.CustomButton.Location = new System.Drawing.Point(404, 1);
            this.txtTempPath.CustomButton.Name = "";
            this.txtTempPath.CustomButton.Size = new System.Drawing.Size(21, 21);
            this.txtTempPath.CustomButton.Style = ReaLTaiizor.Enum.Poison.ColorStyle.Blue;
            this.txtTempPath.CustomButton.TabIndex = 1;
            this.txtTempPath.CustomButton.Theme = ReaLTaiizor.Enum.Poison.ThemeStyle.Light;
            this.txtTempPath.CustomButton.UseSelectable = true;
            this.txtTempPath.CustomButton.Visible = false;
            this.txtTempPath.Lines = new string[0];
            this.txtTempPath.Location = new System.Drawing.Point(138, 52);
            this.txtTempPath.MaxLength = 32767;
            this.txtTempPath.Name = "txtTempPath";
            this.txtTempPath.PasswordChar = '\0';
            this.txtTempPath.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.txtTempPath.SelectedText = "";
            this.txtTempPath.SelectionLength = 0;
            this.txtTempPath.SelectionStart = 0;
            this.txtTempPath.ShortcutsEnabled = true;
            this.txtTempPath.Size = new System.Drawing.Size(426, 23);
            this.txtTempPath.TabIndex = 4;
            this.txtTempPath.UseSelectable = true;
            this.txtTempPath.UseStyleColors = true;
            this.txtTempPath.WaterMarkColor = System.Drawing.Color.FromArgb(((int)(((byte)(109)))), ((int)(((byte)(109)))), ((int)(((byte)(109)))));
            this.txtTempPath.WaterMarkFont = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Pixel);
            // 
            // btnBrowseTemp
            // 
            this.btnBrowseTemp.Location = new System.Drawing.Point(570, 52);
            this.btnBrowseTemp.Name = "btnBrowseTemp";
            this.btnBrowseTemp.Size = new System.Drawing.Size(80, 23);
            this.btnBrowseTemp.TabIndex = 5;
            this.btnBrowseTemp.Text = "Browse";
            this.btnBrowseTemp.UseSelectable = true;
            this.btnBrowseTemp.UseStyleColors = true;
            // 
            // lblDll
            // 
            this.lblDll.Location = new System.Drawing.Point(26, 84);
            this.lblDll.Name = "lblDll";
            this.lblDll.Size = new System.Drawing.Size(106, 23);
            this.lblDll.TabIndex = 6;
            this.lblDll.Text = "DLL Extract Path:";
            this.lblDll.UseStyleColors = true;
            // 
            // txtDllExtractPath
            // 
            // 
            // 
            // 
            this.txtDllExtractPath.CustomButton.Image = null;
            this.txtDllExtractPath.CustomButton.Location = new System.Drawing.Point(404, 1);
            this.txtDllExtractPath.CustomButton.Name = "";
            this.txtDllExtractPath.CustomButton.Size = new System.Drawing.Size(21, 21);
            this.txtDllExtractPath.CustomButton.Style = ReaLTaiizor.Enum.Poison.ColorStyle.Blue;
            this.txtDllExtractPath.CustomButton.TabIndex = 1;
            this.txtDllExtractPath.CustomButton.Theme = ReaLTaiizor.Enum.Poison.ThemeStyle.Light;
            this.txtDllExtractPath.CustomButton.UseSelectable = true;
            this.txtDllExtractPath.CustomButton.Visible = false;
            this.txtDllExtractPath.Lines = new string[0];
            this.txtDllExtractPath.Location = new System.Drawing.Point(138, 84);
            this.txtDllExtractPath.MaxLength = 32767;
            this.txtDllExtractPath.Name = "txtDllExtractPath";
            this.txtDllExtractPath.PasswordChar = '\0';
            this.txtDllExtractPath.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.txtDllExtractPath.SelectedText = "";
            this.txtDllExtractPath.SelectionLength = 0;
            this.txtDllExtractPath.SelectionStart = 0;
            this.txtDllExtractPath.ShortcutsEnabled = true;
            this.txtDllExtractPath.Size = new System.Drawing.Size(426, 23);
            this.txtDllExtractPath.TabIndex = 7;
            this.txtDllExtractPath.UseSelectable = true;
            this.txtDllExtractPath.UseStyleColors = true;
            this.txtDllExtractPath.WaterMarkColor = System.Drawing.Color.FromArgb(((int)(((byte)(109)))), ((int)(((byte)(109)))), ((int)(((byte)(109)))));
            this.txtDllExtractPath.WaterMarkFont = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Pixel);
            // 
            // btnBrowseDll
            // 
            this.btnBrowseDll.Location = new System.Drawing.Point(570, 84);
            this.btnBrowseDll.Name = "btnBrowseDll";
            this.btnBrowseDll.Size = new System.Drawing.Size(80, 23);
            this.btnBrowseDll.TabIndex = 8;
            this.btnBrowseDll.Text = "Browse";
            this.btnBrowseDll.UseSelectable = true;
            this.btnBrowseDll.UseStyleColors = true;
            // 
            // lblAppDataRoaming
            // 
            this.lblAppDataRoaming.Location = new System.Drawing.Point(3, 120);
            this.lblAppDataRoaming.Name = "lblAppDataRoaming";
            this.lblAppDataRoaming.Size = new System.Drawing.Size(129, 23);
            this.lblAppDataRoaming.TabIndex = 9;
            this.lblAppDataRoaming.Text = "AppData (Roaming):";
            this.lblAppDataRoaming.UseStyleColors = true;
            // 
            // txtAppDataRoaming
            // 
            // 
            // 
            // 
            this.txtAppDataRoaming.CustomButton.Image = null;
            this.txtAppDataRoaming.CustomButton.Location = new System.Drawing.Point(404, 1);
            this.txtAppDataRoaming.CustomButton.Name = "";
            this.txtAppDataRoaming.CustomButton.Size = new System.Drawing.Size(21, 21);
            this.txtAppDataRoaming.CustomButton.Style = ReaLTaiizor.Enum.Poison.ColorStyle.Blue;
            this.txtAppDataRoaming.CustomButton.TabIndex = 1;
            this.txtAppDataRoaming.CustomButton.Theme = ReaLTaiizor.Enum.Poison.ThemeStyle.Light;
            this.txtAppDataRoaming.CustomButton.UseSelectable = true;
            this.txtAppDataRoaming.CustomButton.Visible = false;
            this.txtAppDataRoaming.Lines = new string[0];
            this.txtAppDataRoaming.Location = new System.Drawing.Point(138, 120);
            this.txtAppDataRoaming.MaxLength = 32767;
            this.txtAppDataRoaming.Name = "txtAppDataRoaming";
            this.txtAppDataRoaming.PasswordChar = '\0';
            this.txtAppDataRoaming.ReadOnly = true;
            this.txtAppDataRoaming.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.txtAppDataRoaming.SelectedText = "";
            this.txtAppDataRoaming.SelectionLength = 0;
            this.txtAppDataRoaming.SelectionStart = 0;
            this.txtAppDataRoaming.ShortcutsEnabled = true;
            this.txtAppDataRoaming.Size = new System.Drawing.Size(426, 23);
            this.txtAppDataRoaming.TabIndex = 10;
            this.txtAppDataRoaming.UseSelectable = true;
            this.txtAppDataRoaming.UseStyleColors = true;
            this.txtAppDataRoaming.WaterMarkColor = System.Drawing.Color.FromArgb(((int)(((byte)(109)))), ((int)(((byte)(109)))), ((int)(((byte)(109)))));
            this.txtAppDataRoaming.WaterMarkFont = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Pixel);
            // 
            // btnBrowseAppDataRoaming
            // 
            this.btnBrowseAppDataRoaming.Location = new System.Drawing.Point(570, 120);
            this.btnBrowseAppDataRoaming.Name = "btnBrowseAppDataRoaming";
            this.btnBrowseAppDataRoaming.Size = new System.Drawing.Size(80, 23);
            this.btnBrowseAppDataRoaming.TabIndex = 11;
            this.btnBrowseAppDataRoaming.Text = "Open";
            this.btnBrowseAppDataRoaming.UseSelectable = true;
            this.btnBrowseAppDataRoaming.UseStyleColors = true;
            // 
            // lblInfo
            // 
            this.lblInfo.FontSize = ReaLTaiizor.Extension.Poison.PoisonLabelSize.Small;
            this.lblInfo.Location = new System.Drawing.Point(20, 146);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(630, 30);
            this.lblInfo.TabIndex = 12;
            this.lblInfo.Text = "Note: DLL Extract Path is where Costura extracts embedded DLLs at runtime.";
            this.lblInfo.UseStyleColors = true;
            // 
            // buttonPanel
            // 
            this.buttonPanel.Controls.Add(this.btnOK);
            this.buttonPanel.Controls.Add(this.btnCancel);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.buttonPanel.HorizontalScrollbarBarColor = true;
            this.buttonPanel.HorizontalScrollbarHighlightOnWheel = false;
            this.buttonPanel.HorizontalScrollbarSize = 10;
            this.buttonPanel.Location = new System.Drawing.Point(20, 237);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Padding = new System.Windows.Forms.Padding(10, 10, 20, 10);
            this.buttonPanel.Size = new System.Drawing.Size(660, 43);
            this.buttonPanel.TabIndex = 1;
            this.buttonPanel.UseStyleColors = true;
            this.buttonPanel.VerticalScrollbarBarColor = true;
            this.buttonPanel.VerticalScrollbarHighlightOnWheel = false;
            this.buttonPanel.VerticalScrollbarSize = 10;
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Location = new System.Drawing.Point(550, 6);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(100, 30);
            this.btnOK.TabIndex = 0;
            this.btnOK.Text = "OK";
            this.btnOK.UseSelectable = true;
            this.btnOK.UseStyleColors = true;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.Location = new System.Drawing.Point(444, 6);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 30);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseSelectable = true;
            this.btnCancel.UseStyleColors = true;
            // 
            // ConfigLocationsDialog
            // 
            this.ClientSize = new System.Drawing.Size(700, 300);
            this.Controls.Add(this.mainPanel);
            this.Controls.Add(this.buttonPanel);
            this.MinimumSize = new System.Drawing.Size(700, 300);
            this.Name = "ConfigLocationsDialog";
            this.PoisonBorderStyle = ReaLTaiizor.Enum.Poison.FormBorderStyle.FixedSingle;
            this.ShadowType = ReaLTaiizor.Enum.Poison.FormShadowType.DropShadow;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Application Locations";
            this.mainPanel.ResumeLayout(false);
            this.buttonPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}

