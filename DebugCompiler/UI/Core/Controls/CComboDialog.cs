using Refract.UI.Core.Interfaces;
using Refract.UI.Core.Singletons;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SMC.UI.Core.Controls
{
    public partial class CComboDialog : Form, IThemeableControl
    {
        public object SelectedValue { get; private set; }
        public int SelectedIndex { get; private set; }

        public CComboDialog(string title, object[] selectables, int defaultIndex = 0)
        {
            InitializeComponent();
            UIThemeManager.OnThemeChanged(this, OnThemeChanged_Implementation);
            this.SetThemeAware();
            Text = title;
            InnerForm.TitleBarTitle = title;
            cComboBox1.Items.AddRange(selectables);

            if (defaultIndex > -1 && defaultIndex < selectables.Length)
            {
                cComboBox1.SelectedIndex = defaultIndex;
            }

            // Manually add controls to InnerForm's ControlContents
            InnerForm.ControlContents.Controls.Add(cComboBox1);
            InnerForm.ControlContents.Controls.Add(SelectButton);
        }

        private void OnThemeChanged_Implementation(UIThemeInfo themeData) { }

        public IEnumerable<Control> GetThemedControls()
        {
            yield return InnerForm;
            yield return cComboBox1;
            yield return SelectButton;
        }

        private void SelectButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void cComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectedIndex = cComboBox1.SelectedIndex;
            SelectedValue = SelectedIndex >= 0 ? cComboBox1.Items[SelectedIndex] : null;
        }
    }
}