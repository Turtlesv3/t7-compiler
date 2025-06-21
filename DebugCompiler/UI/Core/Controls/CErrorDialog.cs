using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel.Design;
using System.Windows.Forms.Design;
using System.Drawing.Design;
using System.Collections;
using DebugCompiler.UI.Core.Singletons;
using DebugCompiler.UI.Core.Interfaces;
using DebugCompiler.UI.Core.Controls;

namespace DebugCompiler.UI.Core.Controls
{
    public partial class CErrorDialog : Form, IThemeableControl
    {
        public CErrorDialog(string title, string description)
        {
            InitializeComponent();
            UIThemeManager.RegisterControl(this);
            UIThemeManager.ThemeChanged += OnThemeChanged_Implementation;
            MaximizeBox = true;
            MinimizeBox = true;
            Text = title;
            InnerForm.TitleBarTitle = title;
            ErrorRTB.Text = description;
        }

        public void ApplyTheme(UIThemeInfo theme)
        {
            // Implement control-specific theming
            this.BackColor = theme.BackColor;
            this.ForeColor = theme.TextColor;
            // ... other properties
        }

        private void OnThemeChanged_Implementation(UIThemeInfo theme)
        {
            ApplyTheme(theme);
        }

        public IEnumerable<Control> GetThemedControls()
        {
            yield return InnerForm;
            yield return ErrorRTB;
            yield return AcceptButton;
        }

        private void AcceptButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Button1_Click(object sender, EventArgs e)
        {

        }

        public static void Show(string title, string description, bool topMost = false)
        {
            new CErrorDialog(title, description) { TopMost = topMost }.ShowDialog();
        }
    }
}
