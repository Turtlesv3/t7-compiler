using DebugCompiler.UI.Core.Controls;
using DebugCompiler.UI.Core.Interfaces;
using DebugCompiler.UI.Core.Singletons;
using DebugCompiler.UI.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using DebugCompiler.Properties;

namespace DebugCompiler
{
    public partial class ImportDialog : CBorderedForm, IThemeableControl
    {
        public string SelectedFilePath { get; private set; }
        public string SelectedOutputPath { get; private set; }

        // Store the parent form for ShowDialog implementation
        private Form _parentForm;

        public ImportDialog()
        {
            InitializeComponent();
            UIThemeManager.RegisterControl(this);
            ApplyTheme(UIThemeManager.CurrentTheme);

            // Set title bar properties
            this.TitleBarTitle = "Saved Menus";
            this.SetExitHidden(false);
            this.SetDraggable(true);

            // Initialize controls
            InitializeListView();
            InitializeButtons();
        }

        private void InitializeListView()
        {
            listView1.View = View.Details;
            listView1.FullRowSelect = true;
            listView1.MultiSelect = false;
            listView1.HideSelection = false;

            // Add columns
            listView1.Columns.Add("File", 200);
            listView1.Columns.Add("Size", 80);
            listView1.Columns.Add("Modified", 150);
        }

        private void InitializeButtons()
        {
            btnSelect.Text = "Select";
            btnSelect.Click += BtnSelect_Click;
            btnSelect.DialogResult = DialogResult.None;

            btnCancel.Text = "Cancel";
            btnCancel.Click += BtnCancel_Click;
            btnCancel.DialogResult = DialogResult.Cancel;
        }

        private void BtnSelect_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                SelectedFilePath = listView1.SelectedItems[0].Tag.ToString();
                this.DialogResult = DialogResult.OK;
                this.Hide();

                // Close the parent form if it exists
                _parentForm?.Close();
            }
            else
            {
                MessageBox.Show("Please select a file first.", "Selection Required",
                               MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Hide();

            // Close the parent form if it exists
            _parentForm?.Close();
        }

        public new void ApplyTheme(UIThemeInfo theme)
        {
            if (theme.Equals(default(UIThemeInfo))) return;

            base.ApplyTheme(theme);

            // Apply theme to all controls
            this.ControlContents.BackColor = theme.BackColor;
            this.ControlContents.ForeColor = theme.TextColor;

            // ListView theming
            if (listView1 != null)
            {
                listView1.BackColor = theme.TextBoxBackColor;
                listView1.ForeColor = theme.TextColor;
                listView1.BorderStyle = theme.TextBoxBorderStyle;
            }

            // Button theming
            var buttons = new[] { btnSelect, btnCancel };
            foreach (var btn in buttons.Where(b => b != null))
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

        public new IEnumerable<Control> GetThemedControls()
        {
            foreach (var control in base.GetThemedControls())
            {
                yield return control;
            }

            yield return listView1;
            yield return btnSelect;
            yield return btnCancel;
        }

        public void LoadFiles(IEnumerable<string> filePaths)
        {
            listView1.Items.Clear();
            foreach (var filePath in filePaths)
            {
                if (File.Exists(filePath))
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
            }

            // Auto-select first item if available
            if (listView1.Items.Count > 0)
            {
                listView1.Items[0].Selected = true;
                listView1.Select();
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

        private void ListView1_DoubleClick(object sender, EventArgs e)
        {
            BtnSelect_Click(sender, e);
        }

        public DialogResult ShowDialog(Form owner)
        {
            _parentForm = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.CenterParent,
                Size = this.Size,
                Controls = { this }
            };

            this.Dock = DockStyle.Fill;
            this.Visible = true;

            var result = _parentForm.ShowDialog(owner);
            _parentForm = null;
            return result;
        }

        public DialogResult ShowDialog()
        {
            return ShowDialog(null);
        }
    }
}