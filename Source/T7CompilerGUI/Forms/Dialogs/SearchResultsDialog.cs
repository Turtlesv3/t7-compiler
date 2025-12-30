using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Drawing.Poison;
using ReaLTaiizor.Extension.Poison;
using ReaLTaiizorExt = ReaLTaiizor.Extension.Poison;

namespace T7CompilerGUI.Forms.Dialogs
{
    public partial class SearchResultsDialog : PoisonForm
    {
        public SearchResult SelectedResult { get; private set; }

        private PoisonStyleManager styleManager;
        private List<SearchResult> results;
        private string projectPath;

        public class SearchResult
        {
            public string FilePath { get; set; }
            public int LineNumber { get; set; }
            public int ColumnNumber { get; set; }
            public string LineText { get; set; }
            public int MatchStart { get; set; }
            public int MatchLength { get; set; }
            
            public string DisplayText
            {
                get
                {
                    string fileName = Path.GetFileName(FilePath);
                    string linePreview = LineText.Trim();
                    if (linePreview.Length > 80)
                        linePreview = linePreview.Substring(0, 77) + "...";
                    return $"{fileName}:{LineNumber} - {linePreview}";
                }
            }
            
            public override string ToString()
            {
                return DisplayText;
            }
        }

        public SearchResultsDialog(List<SearchResult> results, PoisonStyleManager styleManager = null, string projectPath = null)
        {
            this.results = results ?? new List<SearchResult>();
            this.styleManager = styleManager;
            this.projectPath = projectPath;
            
            InitializeComponent();
            
            // Load form icon
            T7CompilerGUI.Helpers.FormIconHelper.LoadFormIcon(this);
            
            if (styleManager != null)
            {
                // Use PoisonFormHelper for standardized form initialization
                // This handles StyleManager application, button effects, and form icon loading
                ReaLTaiizor.Extension.Poison.PoisonFormHelper.InitializeForm(this, styleManager);
                // Setup as modal dialog and center on parent
                ReaLTaiizor.Extension.Poison.PoisonFormHelper.SetupAsDialog(this, styleManager);
            }
            
            SetupListView();
            PopulateResults();
        }
        
        private void SetupListView()
        {
            if (lvwResults == null) return;
            
            // Clear existing columns
            lvwResults.Columns.Clear();
            
            // Add columns
            lvwResults.Columns.Add("File", 150);
            lvwResults.Columns.Add("Line", 60);
            lvwResults.Columns.Add("Preview", 340);
            
            // Apply StyleManager if available
            if (styleManager != null)
            {
                ReaLTaiizorExt.PoisonControlHelper.ApplyStyleManager(lvwResults, styleManager);
            }
        }
        
        private void PopulateResults()
        {
            if (lvwResults == null)
                return;
                
            lvwResults.Items.Clear();
            
            foreach (var result in results)
            {
                ListViewItem item = new ListViewItem(Path.GetFileName(result.FilePath));
                item.SubItems.Add(result.LineNumber.ToString());
                string preview = result.LineText.Trim();
                if (preview.Length > 60)
                    preview = preview.Substring(0, 57) + "...";
                item.SubItems.Add(preview);
                item.Tag = result;
                lvwResults.Items.Add(item);
            }
            
            lblResultsCount.Text = $"Found {results.Count} result(s)";
            
            if (lvwResults.Items.Count > 0)
            {
                lvwResults.Items[0].Selected = true;
                lvwResults.Items[0].Focused = true;
            }
        }
        
        private void BtnGoTo_Click(object sender, EventArgs e)
        {
            if (lvwResults.SelectedItems.Count > 0 && lvwResults.SelectedItems[0].Tag is SearchResult result)
            {
                SelectedResult = result;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
        
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        
        private void LvwResults_DoubleClick(object sender, EventArgs e)
        {
            BtnGoTo_Click(sender, e);
        }
    }
}

