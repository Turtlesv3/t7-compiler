using System;
using System.Windows.Forms;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Extension.Poison;
using ReaLTaiizor.Drawing.Poison;
using ReaLTaiizorExt = ReaLTaiizor.Extension.Poison;

namespace T7CompilerGUI.Forms.Dialogs
{
    public partial class ReplaceDialog : PoisonForm
    {
        public string SearchText => txtSearch.Text;
        public string ReplaceText => txtReplace.Text;
        public bool MatchCase => chkMatchCase.Checked;
        public bool WholeWord => chkWholeWord.Checked;
        public bool ReplaceAll => chkReplaceAll.Checked;

        private PoisonStyleManager styleManager;

        public ReplaceDialog(PoisonStyleManager styleManager = null)
        {
            this.styleManager = styleManager;
            InitializeComponent();
            
            // Load form icon
            T7CompilerGUI.Helpers.FormIconHelper.LoadFormIcon(this);
            
            if (styleManager != null)
            {
                // Use PoisonFormHelper for standardized form initialization
                // This handles StyleManager application, button effects, and form icon loading
                ReaLTaiizor.Extension.Poison.PoisonFormHelper.InitializeForm(this, styleManager);
            }
            
            txtSearch.Focus();
        }
        
        private void BtnReplace_Click(object sender, EventArgs e)
        {
        }
        
        private void BtnCancel_Click(object sender, EventArgs e)
        {
        }
    }
}

