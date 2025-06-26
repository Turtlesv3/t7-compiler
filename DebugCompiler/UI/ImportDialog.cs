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

        public ImportDialog()
        {
            InitializeComponent();

            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                // Design-time sample data
                listView1.Items.Add(new ListViewItem(new[] { "SampleFile.txt", "128 KB", DateTime.Now.ToString("g") }));
                listView1.Items.Add(new ListViewItem(new[] { "ProjectFile.csproj", "2.5 MB", DateTime.Now.ToString("g") }));
            }
            else
            {
                InitializeRuntimeComponents();
            }
        }

        private void InitializeRuntimeComponents()
        {
            try
            {
                UIThemeManager.RegisterControl(this);
                ApplyTheme(UIThemeManager.CurrentTheme);

                InnerForm.SetExitButtonVisible(false);
                InnerForm.SetDraggable(true);

                // Ensure proper docking
                InnerForm.Dock = DockStyle.Fill;
                InnerForm.ControlContents.Dock = DockStyle.Fill;

                // Event handlers
                listView1.DoubleClick += ListView1_DoubleClick;
                btnSelect.Click += BtnSelect_Click;
                btnCancel.Click += BtnCancel_Click;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Initialization error: {ex.Message}");
            }
        }

        public void ApplyTheme(UIThemeInfo theme)
        {
            if (IsDisposed || DesignMode) return;

            try
            {
                this.SuspendLayout();

                // Only apply theme if we're not in design mode
                if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
                {
                    this.BackColor = theme.BackColor;
                    this.ForeColor = theme.TextColor;

                    if (this.InnerForm != null && !this.InnerForm.IsDisposed)
                    {
                        this.InnerForm.BackColor = theme.AccentColor;
                        this.InnerForm.ForeColor = theme.TextColor;
                    }

                    if (listView1 != null && !listView1.IsDisposed)
                    {
                        listView1.BackColor = theme.TextBoxBackColor;
                        listView1.ForeColor = theme.TextColor;
                        listView1.BorderStyle = theme.TextBoxBorderStyle;
                    }

                    var buttons = new[] { btnSelect, btnCancel };
                    foreach (var btn in buttons.Where(b => b != null && !b.IsDisposed))
                    {
                        btn.BackColor = theme.AccentColor;
                        btn.ForeColor = Color.White;
                        btn.FlatAppearance.BorderColor = theme.AccentColor;
                    }
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
            if (DesignMode) return DialogResult.None;

            try
            {
                // Refresh theme in case it changed
                ApplyTheme(UIThemeManager.CurrentTheme);
                return base.ShowDialog();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing dialog: {ex.Message}");
                return DialogResult.Abort;
            }
        }

        public new DialogResult ShowDialog(IWin32Window owner)
        {
            if (DesignMode) return DialogResult.None;

            try
            {
                ApplyTheme(UIThemeManager.CurrentTheme);
                return base.ShowDialog(owner);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing dialog: {ex.Message}");
                return DialogResult.Abort;
            }
        }
    }
}