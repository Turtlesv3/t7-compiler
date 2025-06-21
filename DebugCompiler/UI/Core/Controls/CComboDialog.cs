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
    public partial class CComboDialog : Form, IThemeableControl
    {
        public object SelectedValue { get; private set; }
        public int SelectedIndex { get; private set; }
        public CComboDialog(string title, object[] selectables, int defaultIndex = 0)
        {
            InitializeComponent();
            UIThemeManager.RegisterControl(this);
            UIThemeManager.ThemeChanged += OnThemeChanged_Implementation;
            MaximizeBox = true;
            MinimizeBox = true;
            Text = title;
            InnerForm.TitleBarTitle = title;
            cComboBox1.Items.Clear();
            cComboBox1.Items.AddRange(selectables);
            if (defaultIndex > -1 && defaultIndex < selectables.Length)
            {
                cComboBox1.SelectedIndex = defaultIndex;
            }
        }

        public void ApplyTheme(UIThemeInfo theme)
        {
            this.BackColor = theme.BackColor;
            this.ForeColor = theme.TextColor;

            // Theme child controls
            foreach (Control control in this.Controls)
            {
                if (control is IThemeableControl themeable)
                {
                    themeable.ApplyTheme(theme);
                }
                else if (control is ComboBox combo)
                {
                    combo.BackColor = theme.TextBoxBackColor;
                    combo.ForeColor = theme.TextColor;
                }
            }
        }

        private void OnThemeChanged_Implementation(UIThemeInfo theme)
        {
            ApplyTheme(theme);
        }

        public IEnumerable<Control> GetThemedControls()
        {
            return this.Controls.Cast<Control>();

        }

        private void AcceptButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void CComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectedIndex = cComboBox1.SelectedIndex;
            if (SelectedIndex >= 0 && SelectedIndex < cComboBox1.Items.Count)
            {
                SelectedValue = cComboBox1.Items[SelectedIndex];
            }
            else
            {
                SelectedValue = null;
            }
        }

        private void InnerForm_Load(object sender, EventArgs e)
        {

        }
    }
}
