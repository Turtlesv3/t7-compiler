using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Drawing.Poison;
using ReaLTaiizor.Extension.Poison;
using ReaLTaiizorExt = ReaLTaiizor.Extension.Poison;

namespace T7CompilerGUI.Forms.Dialogs
{
    public partial class SearchDialog : PoisonForm
    {
        public string SearchText => txtSearch.Text;
        public bool MatchCase => chkMatchCase.Checked;
        public bool WholeWord => chkWholeWord.Checked;
        public bool SearchAllFiles => true; // Always search all files

        private PoisonStyleManager styleManager;

        public SearchDialog(PoisonStyleManager styleManager = null)
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
        
        private void BtnFind_Click(object sender, EventArgs e)
        {
        }
        
        private void BtnCancel_Click(object sender, EventArgs e)
        {
        }
    }
}

