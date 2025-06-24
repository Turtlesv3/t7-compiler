using DebugCompiler.UI.Core.Singletons;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using DebugCompiler.Properties;

namespace DebugCompiler.UI.Core.Controls
{
    public partial class CComboDialog : ThemedDialog
    {
        private bool _isApplyingTheme = false;
        private Color _borderColor = Color.DimGray;
        private UIThemeInfo _currentTheme;

        [Browsable(true)]
        [Category("Appearance")]
        [DefaultValue(typeof(Color), "DimGray")]
        public Color BorderColor
        {
            get => _borderColor;
            set
            {
                _borderColor = value;
                Invalidate();
            }
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

            // Register for theme changes
            UIThemeManager.RegisterControl(this);
            UIThemeManager.ThemeChanged += OnThemeChanged;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ApplyTheme(UIThemeManager.CurrentTheme);
        }

        protected override void OnThemeChanged(UIThemeInfo theme)  // Changed to override
        {
            if (!IsDisposed && IsHandleCreated)
            {
                ApplyTheme(theme);
            }
            base.OnThemeChanged(theme);  // Call base implementation
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

                _currentTheme = theme;
                base.ApplyTheme(theme);
                BorderColor = theme.BorderColor;

                // Apply theme to all controls
                BackColor = theme.BackColor;
                ForeColor = theme.TextColor;

                if (cComboBox1 != null)
                {
                    cComboBox1.BackColor = theme.TextBoxBackColor;
                    cComboBox1.ForeColor = theme.TextColor;
                    cComboBox1.FlatStyle = FlatStyle.Flat;
                }

                if (AcceptButton != null)
                {
                    AcceptButton.BackColor = theme.AccentColor;
                    AcceptButton.ForeColor = Color.White;
                    AcceptButton.FlatStyle = FlatStyle.Flat;
                    AcceptButton.FlatAppearance.BorderColor = theme.AccentColor;
                    AcceptButton.FlatAppearance.MouseOverBackColor = ControlPaint.Light(theme.AccentColor, 0.2f);
                    AcceptButton.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(theme.AccentColor, 0.2f);
                }

                if (InnerForm != null)
                {
                    InnerForm.BackColor = theme.BackColor;
                    InnerForm.ForeColor = theme.TextColor;
                }

                Invalidate();
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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Draw border with themed color
            using (var pen = new Pen(BorderColor, 1))
            {
                e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, Width - 1, Height - 1));
            }
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