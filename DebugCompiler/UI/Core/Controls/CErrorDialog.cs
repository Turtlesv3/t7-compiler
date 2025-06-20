using Refract.UI.Core.Interfaces;
using Refract.UI.Core.Singletons;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SMC.UI.Core.Controls
{
    public partial class CErrorDialog : Form, IThemeableControl
    {
        public string ErrorDescription => ErrorRTB.Text;

        public CErrorDialog(string title, string description)
        {
            InitializeComponent();
            UIThemeManager.OnThemeChanged(this, OnThemeChanged_Implementation);
            this.SetThemeAware();

            Text = title;
            ErrorRTB.Text = description;
            this.StartPosition = FormStartPosition.CenterParent;
        }

        private void OnThemeChanged_Implementation(UIThemeInfo themeData)
        {
            this.BackColor = themeData.BackColor;
            this.ForeColor = themeData.TextColor;
            ErrorRTB.BackColor = themeData.BackColor;
            ErrorRTB.ForeColor = themeData.TextColor;
            OKButton.BackColor = themeData.ButtonActive;
            OKButton.ForeColor = themeData.TextColor;
            OKButton.FlatAppearance.BorderColor = themeData.AccentColor;
        }

        public IEnumerable<Control> GetThemedControls()
        {
            yield return ErrorRTB;
            yield return OKButton;
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        public static void Show(string title, string description, bool topMost = false)
        {
            // Ensure we're on the UI thread
            if (Application.OpenForms.Count > 0)
            {
                var owner = Application.OpenForms[0];
                owner.Invoke((MethodInvoker)delegate
                {
                    using var dialog = new CErrorDialog(title, description) { TopMost = topMost };
                    dialog.ShowDialog(owner);
                });
            }
            else
            {
                using var dialog = new CErrorDialog(title, description) { TopMost = topMost };
                dialog.ShowDialog();
            }
        }

        internal static DialogResult Show(string v1, bool v2)
        {
            throw new NotImplementedException();
        }
    }
}