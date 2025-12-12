using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Drawing.Poison;

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
            
            if (styleManager != null)
            {
                this.StyleManager = styleManager;
                ApplyPoisonStyling();
            }
            
            PopulateResults();
        }
        
        private void PopulateResults()
        {
            if (lstResults == null)
                return;
                
            lstResults.Items.Clear();
            
            foreach (var result in results)
            {
                lstResults.Items.Add(result);
            }
            
            lblResultsCount.Text = $"Found {results.Count} result(s)";
            
            if (lstResults.Items.Count > 0)
            {
                lstResults.SelectedIndex = 0;
            }
        }
        
        private void ApplyPoisonStyling()
        {
            if (styleManager == null) return;
            
            if (lblResultsCount != null)
            {
                lblResultsCount.StyleManager = styleManager;
                lblResultsCount.UseStyleColors = true;
            }
            
            if (lstResults != null && styleManager != null)
            {
                lstResults.BackColor = PoisonPaint.BackColor.Form(styleManager.Theme);
                lstResults.ForeColor = PoisonPaint.ForeColor.Label.Normal(styleManager.Theme);
            }
            
            if (btnGoTo != null)
            {
                btnGoTo.StyleManager = styleManager;
                btnGoTo.UseStyleColors = true;
            }
            
            if (btnCancel != null)
            {
                btnCancel.StyleManager = styleManager;
                btnCancel.UseStyleColors = true;
            }
        }
        
        private void BtnGoTo_Click(object sender, EventArgs e)
        {
            if (lstResults.SelectedItem != null)
            {
                SelectedResult = lstResults.SelectedItem as SearchResult;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
        
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        
        private void LstResults_DoubleClick(object sender, EventArgs e)
        {
            BtnGoTo_Click(sender, e);
        }
    }
}

