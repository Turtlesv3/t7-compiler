using DebugCompiler.UI.Core.Controls;
using DebugCompiler.UI.Core.Singletons;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace DebugCompiler.UI.Core.Controls
{
    public partial class CComboDialog : ThemedDialog
    {
        private bool _isApplyingTheme = false;
        private Color _borderColor = Color.DimGray;

        [Browsable(true)]
        [Category("Appearance")]
        [DefaultValue(typeof(Color), "DimGray")]
        public Color BorderColor
        {
            get => _borderColor;
            set => _borderColor = value;
        }

        public object SelectedValue { get; private set; }
        public int SelectedIndex { get; private set; }

        public CComboDialog(string title, object[] selectables, int defaultIndex = 0)
        {
            InitializeComponent();
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

        public override void ApplyTheme(UIThemeInfo theme)
        {
            if (_isApplyingTheme || IsDisposed || !IsHandleCreated)
                return;

            _isApplyingTheme = true;
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action<UIThemeInfo>(ApplyTheme), theme);
                    return;
                }

                base.ApplyTheme(theme);
                BorderColor = theme.BorderColor;

                // Theme specific controls
                if (cComboBox1 != null)
                {
                    cComboBox1.BackColor = theme.TextBoxBackColor;
                    cComboBox1.ForeColor = theme.TextColor;
                }

                if (AcceptButton != null)
                {
                    AcceptButton.BackColor = theme.ButtonBackColor;
                    AcceptButton.ForeColor = theme.TextColor;
                }
            }
            finally
            {
                _isApplyingTheme = false;
            }
        }

        public override IEnumerable<Control> GetThemedControls()
        {
            if (InnerForm != null) yield return InnerForm;
            if (cComboBox1 != null) yield return cComboBox1;
            if (AcceptButton != null) yield return AcceptButton;
        }

        private void AcceptButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void CComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectedIndex = cComboBox1.SelectedIndex;
            SelectedValue = SelectedIndex >= 0 ? cComboBox1.Items[SelectedIndex] : null;
        }

        private void InnerForm_Load(object sender, EventArgs e)
        {
            // Empty handler to satisfy designer requirements
        }
    }
}