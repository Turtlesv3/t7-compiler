using DebugCompiler.Properties;
using DebugCompiler.UI.Core.Interfaces;
using DebugCompiler.UI.Core.Singletons;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace DebugCompiler
{
    public partial class ImportDialog : Form, IThemeableControl
    {

        public System.Windows.Forms.ListView FileListView => listView1;

        public ImportDialog()
        {
            InitializeComponent();
            UIThemeManager.RegisterControl(this);
            MaximizeBox = true;
            MinimizeBox = true;

            // Initialize the ListView if it's not in the designer
            if (listView1 == null)
            {
                listView1 = new System.Windows.Forms.ListView();
                listView1.Name = "listView1";
                listView1.Dock = DockStyle.Fill;
                listView1.View = View.Details;
                this.Controls.Add(listView1);
            }
        }

        public IEnumerable<Control> GetThemedControls()
        {
            yield return InnerForm;
            yield return SelectImportBtn;
            yield return ImportLabel;
            yield return OutputLabel;
            yield return OutputBtn;
            yield return StartImportButton;

            // Add the ListView to themed controls if it exists
            if (listView1 != null)
            {
                yield return listView1;
            }
        }

        public void ApplyTheme(UIThemeInfo theme)
        {
            this.BackColor = theme.BackColor;
            this.ForeColor = theme.TextColor;

            // Apply theme to ListView if it exists
            if (listView1 != null)
            {
                listView1.BackColor = theme.TextBoxBackColor;
                listView1.ForeColor = theme.TextColor;
            }
        }


        private void OnThemeChanged(UIThemeInfo theme)
        {
            ApplyTheme(theme);
        }

        private void RPCTest1_Click(object sender, EventArgs e)
        {

        }

        private void RPCExample2_Click(object sender, EventArgs e)
        {

        }

        private void RPCExample3_Click(object sender, EventArgs e)
        {
        }

        private void ExampleRPC4_Click(object sender, EventArgs e)
        {
        }

        private void Button1_Click(object sender, EventArgs e)
        {
        }

        private void SelectImportButton_Click(object sender, EventArgs e)
        {
        }

        private void OutputBtn_Click(object sender, EventArgs e)
        {
        }

        private void StartImportButton_Click(object sender, EventArgs e)
        {
        }
    }
}
