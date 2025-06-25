using DebugCompiler.UI.Core.Controls;
using DebugCompiler.UI.Core.Interfaces;
using DebugCompiler.UI.Core.Singletons;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DebugCompiler
{
    public partial class ImportDialog : Form, IThemeableControl
    {
        public string SelectedFilePath { get; private set; }
        private Form _parentForm;

        public ImportDialog()
        {
            InitializeComponent();

            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
            {
                InitializeRuntimeComponents();
            }
        }

        private void InitializeRuntimeComponents()
        {
            // Initialize theme
            UIThemeManager.RegisterControl(this);
            ApplyTheme(UIThemeManager.CurrentTheme);

            // Set up the inner form
            InnerForm.SetExitButtonVisible(false);
            InnerForm.SetDraggable(true);

            // Initialize list view columns
            listView1.Columns.Add("File", 200);
            listView1.Columns.Add("Size", 80);
            listView1.Columns.Add("Modified", 150);

            // Event handlers
            listView1.DoubleClick += ListView1_DoubleClick;
            btnSelect.Click += BtnSelect_Click;
            btnCancel.Click += BtnCancel_Click;
        }

        public void ApplyTheme(UIThemeInfo theme)
        {
            if (theme.Equals(default(UIThemeInfo)) || IsDisposed) return;

            try
            {
                this.SuspendLayout();

                // Apply to main form
                this.BackColor = theme.BackColor;
                this.ForeColor = theme.TextColor;

                // Apply to inner bordered form
                if (this.InnerForm != null && !this.InnerForm.IsDisposed)
                {
                    this.InnerForm.BackColor = theme.AccentColor;
                    this.InnerForm.ForeColor = theme.TextColor;
                }

                // Apply to list view
                if (listView1 != null && !listView1.IsDisposed)
                {
                    listView1.BackColor = theme.TextBoxBackColor;
                    listView1.ForeColor = theme.TextColor;
                    listView1.BorderStyle = theme.TextBoxBorderStyle;
                }

                // Apply to buttons
                var buttons = new[] { btnSelect, btnCancel };
                foreach (var btn in buttons.Where(b => b != null && !b.IsDisposed))
                {
                    btn.BackColor = theme.AccentColor;
                    btn.ForeColor = Color.White;
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderColor = theme.AccentColor;
                    btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(theme.AccentColor, 0.2f);
                    btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(theme.AccentColor, 0.2f);
                    btn.Font = new Font(btn.Font, FontStyle.Bold);
                }
            }
            finally
            {
                this.ResumeLayout(true);
            }
        }

        public IEnumerable<Control> GetThemedControls()
        {
            yield return InnerForm;
            yield return listView1;
            yield return buttonPanel;
            yield return btnSelect;
            yield return btnCancel;
        }

        public void LoadFiles(IEnumerable<string> filePaths)
        {
            listView1.BeginUpdate();
            try
            {
                listView1.Items.Clear();
                foreach (var filePath in filePaths.Where(File.Exists))
                {
                    var fileInfo = new FileInfo(filePath);
                    var item = new ListViewItem(fileInfo.Name)
                    {
                        Tag = filePath
                    };
                    item.SubItems.Add(FormatFileSize(fileInfo.Length));
                    item.SubItems.Add(fileInfo.LastWriteTime.ToString("g"));
                    listView1.Items.Add(item);
                }

                if (listView1.Items.Count > 0)
                {
                    listView1.Items[0].Selected = true;
                }
            }
            finally
            {
                listView1.EndUpdate();
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            while (bytes >= 1024 && order < sizes.Length - 1)
            {
                order++;
                bytes /= 1024;
            }
            return $"{bytes:0.##} {sizes[order]}";
        }

        private void BtnSelect_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                SelectedFilePath = listView1.SelectedItems[0].Tag.ToString();
                DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show("Please select a file first.", "Selection Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void ListView1_DoubleClick(object sender, EventArgs e)
        {
            BtnSelect_Click(sender, e);
        }

        public new DialogResult ShowDialog()
        {
            return ShowDialog(null);
        }

        public DialogResult ShowDialog(IWin32Window owner)
        {
            try
            {
                // Set dialog properties
                this.FormBorderStyle = FormBorderStyle.None;
                this.ShowInTaskbar = false;
                this.StartPosition = owner != null ? FormStartPosition.CenterParent : FormStartPosition.CenterScreen;

                // Create a proper modal dialog
                if (owner is Form ownerForm)
                {
                    this.Size = new Size(
                        Math.Min(600, ownerForm.Width - 40),
                        Math.Min(400, ownerForm.Height - 40)
                    );
                }
                else
                {
                    this.Size = new Size(500, 300);
                }

                // Show as modal dialog
                return base.ShowDialog(owner);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing import dialog: {ex.Message}");
                return DialogResult.Abort;
            }
        }

        private void CloseDialog()
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}