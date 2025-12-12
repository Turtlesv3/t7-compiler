using System;
using System.Windows.Forms;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Manager;

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
            
            if (styleManager != null)
            {
                this.StyleManager = styleManager;
                
                // Apply styling to all controls
                ApplyPoisonStyling();
            }
            
            txtSearch.Focus();
        }
        
        private void ApplyPoisonStyling()
        {
            if (styleManager == null) return;
            
            // Apply to labels
            if (lblSearch != null)
            {
                lblSearch.StyleManager = styleManager;
                lblSearch.UseStyleColors = true;
            }
            if (lblReplace != null)
            {
                lblReplace.StyleManager = styleManager;
                lblReplace.UseStyleColors = true;
            }
            
            // Apply to text boxes
            if (txtSearch != null)
            {
                txtSearch.StyleManager = styleManager;
                txtSearch.UseStyleColors = true;
            }
            if (txtReplace != null)
            {
                txtReplace.StyleManager = styleManager;
                txtReplace.UseStyleColors = true;
            }
            
            // Apply to checkboxes
            if (chkMatchCase != null)
            {
                chkMatchCase.StyleManager = styleManager;
                chkMatchCase.UseStyleColors = true;
            }
            if (chkWholeWord != null)
            {
                chkWholeWord.StyleManager = styleManager;
                chkWholeWord.UseStyleColors = true;
            }
            if (chkReplaceAll != null)
            {
                chkReplaceAll.StyleManager = styleManager;
                chkReplaceAll.UseStyleColors = true;
            }
            
            // Apply to buttons
            if (btnReplace != null)
            {
                btnReplace.StyleManager = styleManager;
                btnReplace.UseStyleColors = true;
            }
            if (btnCancel != null)
            {
                btnCancel.StyleManager = styleManager;
                btnCancel.UseStyleColors = true;
            }
        }
    }
}

