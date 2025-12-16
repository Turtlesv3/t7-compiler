using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Drawing.Poison;
using ReaLTaiizor.Extension.Poison;
using ReaLTaiizorExt = ReaLTaiizor.Extension.Poison;

namespace T7CompilerGUI.Forms.Dialogs
{
    public partial class GoToLineDialog : PoisonForm
    {
        public int SelectedLineNumber { get; private set; } = -1;
        public NavigationItem SelectedNavigationItem { get; private set; }

        private PoisonStyleManager styleManager;
        private List<NavigationItem> navigationItems;
        private int maxLine;
        private int currentLine;

        public class NavigationItem
        {
            public string DisplayText { get; set; }
            public int LineNumber { get; set; }
            public string Type { get; set; } // "function", "label", "ifdef", "endif"
            public int MatchingLineNumber { get; set; } = -1; // For ifdef/endif pairs
            
            public override string ToString()
            {
                return $"{LineNumber,5}: {DisplayText}";
            }
        }

        public GoToLineDialog(List<NavigationItem> navigationItems, int maxLine, int currentLine, PoisonStyleManager styleManager = null)
        {
            this.navigationItems = navigationItems ?? new List<NavigationItem>();
            this.maxLine = maxLine;
            this.currentLine = currentLine;
            this.styleManager = styleManager;
            
            InitializeComponent();
            
            // Load form icon
            T7CompilerGUI.Helpers.FormIconHelper.LoadFormIcon(this);
            
            if (styleManager != null)
            {
                // Use PoisonFormHelper for standardized form initialization
                // This handles StyleManager application, button effects, and form icon loading
                ReaLTaiizor.Extension.Poison.PoisonFormHelper.InitializeForm(this, styleManager);
            }
            
            // Populate combo box
            foreach (var item in this.navigationItems)
            {
                comboNavigate.Items.Add(item);
            }
            
            // Set current line in text box
            txtLineNumber.Text = currentLine.ToString();
            
            // Update label with max line
            lblLineNumber.Text = $"Or enter line number (1-{maxLine}):";
            
            // When combo selection changes, update line number textbox
            comboNavigate.SelectedIndexChanged += (s, e) =>
            {
                if (comboNavigate.SelectedItem != null)
                {
                    var selectedItem = (NavigationItem)comboNavigate.SelectedItem;
                    txtLineNumber.Text = selectedItem.LineNumber.ToString();
                }
            };
            
            comboNavigate.Focus();
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (comboNavigate.SelectedItem != null)
            {
                SelectedNavigationItem = (NavigationItem)comboNavigate.SelectedItem;
                SelectedLineNumber = SelectedNavigationItem.LineNumber;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            // Otherwise try to parse line number
            else if (!string.IsNullOrEmpty(txtLineNumber.Text.Trim()) && 
                     int.TryParse(txtLineNumber.Text.Trim(), out int lineNumber))
            {
                if (lineNumber >= 1 && lineNumber <= maxLine)
                {
                    SelectedLineNumber = lineNumber;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show(this, $"Invalid line number. Please enter a number between 1 and {maxLine}.", "Go to Line", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
        
        private void BtnCancel_Click(object sender, EventArgs e)
        {
        }
        
    }
}

