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
            }
            
            txtSearch.Focus();
        }
    }
}

