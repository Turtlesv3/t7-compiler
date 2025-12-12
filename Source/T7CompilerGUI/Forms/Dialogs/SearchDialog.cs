using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Drawing.Poison;

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
            
            // Apply to label
            if (lblSearch != null)
            {
                lblSearch.StyleManager = styleManager;
                lblSearch.UseStyleColors = true;
            }
            
            // Apply to text box
            if (txtSearch != null)
            {
                txtSearch.StyleManager = styleManager;
                txtSearch.UseStyleColors = true;
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
            
            // Apply to buttons
            if (btnFind != null)
            {
                btnFind.StyleManager = styleManager;
                btnFind.UseStyleColors = true;
            }
            if (btnCancel != null)
            {
                btnCancel.StyleManager = styleManager;
                btnCancel.UseStyleColors = true;
            }
        }
    }
}

