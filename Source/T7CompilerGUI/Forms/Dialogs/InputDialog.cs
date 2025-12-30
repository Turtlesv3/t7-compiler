using System;
using System.Windows.Forms;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Extension.Poison;
using ReaLTaiizor.Drawing.Poison;
using ReaLTaiizorExt = ReaLTaiizor.Extension.Poison;

namespace T7CompilerGUI.Forms.Dialogs
{
    public partial class InputDialog : PoisonForm
    {
        public string InputText => txtInput.Text;

        private PoisonStyleManager styleManager;

        public InputDialog(string title, string prompt, PoisonStyleManager styleManager = null)
        {
            this.styleManager = styleManager;
            InitializeComponent();
            
            // Load form icon
            T7CompilerGUI.Helpers.FormIconHelper.LoadFormIcon(this);
            
            this.Text = title;
            lblPrompt.Text = prompt;
            
            if (styleManager != null)
            {
                // Use PoisonFormHelper for standardized form initialization
                // This handles StyleManager application, button effects, and form icon loading
                ReaLTaiizor.Extension.Poison.PoisonFormHelper.InitializeForm(this, styleManager);
                // Setup as modal dialog
                ReaLTaiizor.Extension.Poison.PoisonFormHelper.SetupAsDialog(this, styleManager);
            }
            
            txtInput.Focus();
        }
        
        private void BtnOK_Click(object sender, EventArgs e)
        {
        }
        
        private void BtnCancel_Click(object sender, EventArgs e)
        {
        }
    }
}

