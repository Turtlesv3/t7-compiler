using System;
using System.Windows.Forms;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Manager;

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
            
            this.Text = title;
            lblPrompt.Text = prompt;
            
            if (styleManager != null)
            {
                this.StyleManager = styleManager;
            }
            
            txtInput.Focus();
        }
    }
}

