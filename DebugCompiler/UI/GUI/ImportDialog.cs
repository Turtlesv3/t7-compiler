using DebugCompiler.UI.Core.Interfaces;
using DebugCompiler.UI.Core.Singletons;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using DebugCompiler.Properties;

namespace DebugCompiler
{
    public partial class ImportDialog : Form, IThemeableControl
    {
        public ImportDialog()
        {
            InitializeComponent();
            UIThemeManager.RegisterControl(this);
            MaximizeBox = true;
            MinimizeBox = true;
        }

        public IEnumerable<Control> GetThemedControls()
        {
            yield return InnerForm;
            yield return SelectImportBtn;
            yield return ImportLabel;
            yield return OutputLabel;
            yield return OutputBtn;
            yield return StartImportButton;
        }

        public void ApplyTheme(UIThemeInfo theme)
        {
            // Implement control-specific theming
            this.BackColor = theme.BackColor;
            this.ForeColor = theme.TextColor;
            // ... other properties
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
