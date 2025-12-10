using System;
using System.Drawing;
using System.Windows.Forms;

namespace T7CompilerGUI.Forms
{
    #region Helper Dialogs

    internal class InputDialog : Form
    {
        private System.Windows.Forms.Label label;
        private System.Windows.Forms.TextBox textBox;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        public string InputText => textBox.Text;

        public InputDialog(string title, string prompt)
        {
            this.Text = title;
            this.Size = new Size(400, 150);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            label = new Label
            {
                Text = prompt,
                Location = new Point(10, 10),
                Size = new Size(360, 20)
            };

            textBox = new TextBox
            {
                Location = new Point(10, 35),
                Size = new Size(360, 23)
            };

            okButton = new System.Windows.Forms.Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(210, 70),
                Size = new Size(75, 23)
            };

            cancelButton = new System.Windows.Forms.Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(295, 70),
                Size = new Size(75, 23)
            };

            this.Controls.AddRange(new Control[] { label, textBox, okButton, cancelButton });
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }
    }

    internal class SearchDialog : Form
    {
        private System.Windows.Forms.Label label;
        private System.Windows.Forms.TextBox textBox;
        private System.Windows.Forms.CheckBox chkMatchCase;
        private System.Windows.Forms.CheckBox chkWholeWord;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        
        public string SearchText => textBox.Text;
        public bool MatchCase => chkMatchCase.Checked;
        public bool WholeWord => chkWholeWord.Checked;

        public SearchDialog()
        {
            this.Text = "Search";
            this.Size = new Size(400, 180);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            label = new Label
            {
                Text = "Search for:",
                Location = new Point(10, 10),
                Size = new Size(360, 20)
            };

            textBox = new TextBox
            {
                Location = new Point(10, 35),
                Size = new Size(360, 23)
            };

            chkMatchCase = new System.Windows.Forms.CheckBox
            {
                Text = "Match case",
                Location = new Point(10, 65),
                Size = new Size(150, 20)
            };

            chkWholeWord = new System.Windows.Forms.CheckBox
            {
                Text = "Whole word",
                Location = new Point(170, 65),
                Size = new Size(150, 20)
            };

            okButton = new System.Windows.Forms.Button
            {
                Text = "Find",
                DialogResult = DialogResult.OK,
                Location = new Point(210, 100),
                Size = new Size(75, 23)
            };

            cancelButton = new System.Windows.Forms.Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(295, 100),
                Size = new Size(75, 23)
            };

            this.Controls.AddRange(new Control[] { label, textBox, chkMatchCase, chkWholeWord, okButton, cancelButton });
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
            textBox.Focus();
        }
    }
    
    internal class ReplaceDialog : Form
    {
        private System.Windows.Forms.Label lblSearch;
        private System.Windows.Forms.Label lblReplace;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.TextBox txtReplace;
        private System.Windows.Forms.CheckBox chkMatchCase;
        private System.Windows.Forms.CheckBox chkWholeWord;
        private System.Windows.Forms.CheckBox chkReplaceAll;
        private System.Windows.Forms.Button btnReplace;
        private System.Windows.Forms.Button btnCancel;
        
        public string SearchText => txtSearch.Text;
        public string ReplaceText => txtReplace.Text;
        public bool MatchCase => chkMatchCase.Checked;
        public bool WholeWord => chkWholeWord.Checked;
        public bool ReplaceAll => chkReplaceAll.Checked;

        public ReplaceDialog()
        {
            this.Text = "Replace";
            this.Size = new Size(400, 220);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            lblSearch = new Label
            {
                Text = "Search for:",
                Location = new Point(10, 10),
                Size = new Size(360, 20)
            };

            txtSearch = new TextBox
            {
                Location = new Point(10, 35),
                Size = new Size(360, 23)
            };

            lblReplace = new Label
            {
                Text = "Replace with:",
                Location = new Point(10, 65),
                Size = new Size(360, 20)
            };

            txtReplace = new TextBox
            {
                Location = new Point(10, 90),
                Size = new Size(360, 23)
            };

            chkMatchCase = new System.Windows.Forms.CheckBox
            {
                Text = "Match case",
                Location = new Point(10, 120),
                Size = new Size(150, 20)
            };

            chkWholeWord = new System.Windows.Forms.CheckBox
            {
                Text = "Whole word",
                Location = new Point(170, 120),
                Size = new Size(150, 20)
            };

            chkReplaceAll = new System.Windows.Forms.CheckBox
            {
                Text = "Replace all",
                Location = new Point(10, 145),
                Size = new Size(150, 20)
            };

            btnReplace = new System.Windows.Forms.Button
            {
                Text = "Replace",
                DialogResult = DialogResult.OK,
                Location = new Point(210, 170),
                Size = new Size(75, 23)
            };

            btnCancel = new System.Windows.Forms.Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(295, 170),
                Size = new Size(75, 23)
            };

            this.Controls.AddRange(new Control[] { lblSearch, txtSearch, lblReplace, txtReplace, chkMatchCase, chkWholeWord, chkReplaceAll, btnReplace, btnCancel });
            this.AcceptButton = btnReplace;
            this.CancelButton = btnCancel;
            txtSearch.Focus();
        }
    }

    #endregion
}

