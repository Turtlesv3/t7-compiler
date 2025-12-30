using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Drawing.Poison;
using ReaLTaiizor.Enum.Poison;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Extension.Poison;
using ReaLTaiizorExt = ReaLTaiizor.Extension.Poison;
using T7CompilerGUI.Controls;

namespace T7CompilerGUI.Forms.Dialogs
{
    public partial class GscConfEditorDialog : PoisonForm
    {
        private PoisonStyleManager styleManager;
        private Forms.MainForm parentForm;
        private string gscConfPath;
        private System.Windows.Forms.Timer syncTimer; // Timer to sync with StyleManager
        private System.Windows.Forms.Timer rainbowTimer; // Timer to update UI when rainbow is active
        
        private List<string> availableDefs;
        private List<string> availableNamespaces;
        private string originalGameModeSymbol = null; // Preserve original case of game mode symbol from gsc.conf
        private string originalGameSymbol = null; // Preserve original case of game symbol (BO3/BO4) from gsc.conf
        
        public bool HasChanges { get; private set; }
        public string SelectedSymbols { get; private set; }
        
        public GscConfEditorDialog(PoisonStyleManager styleManager, string gscConfPath, Forms.MainForm parentForm = null)
        {
            this.styleManager = styleManager;
            this.parentForm = parentForm;
            this.gscConfPath = gscConfPath;
            this.HasChanges = false;
            this.availableDefs = new List<string>(); // Actually stores symbols from #ifdef/#ifndef
            this.availableNamespaces = new List<string>();
            
            // Initialize designer-generated controls
            InitializeComponent();
            
            // Load form icon
            T7CompilerGUI.Helpers.FormIconHelper.LoadFormIcon(this);
            
            // Scan project for symbols before setting up controls
            ScanProjectForSymbols();
            SetupControls();
            
            // Load config immediately after controls are set up (before form is shown)
            // This ensures symbols are displayed when dialog opens
            LoadGscConf();
            
            // Use PoisonFormHelper for standardized form initialization (includes SetupAllButtonEffectsRecursive)
            if (styleManager != null)
            {
                ReaLTaiizorExt.PoisonFormHelper.InitializeForm(this, styleManager);
                // Setup as modal dialog and center on parent
                ReaLTaiizorExt.PoisonFormHelper.SetupAsDialog(this, styleManager);
                if (parentForm != null)
                {
                    ReaLTaiizorExt.PoisonFormHelper.CenterForm(this, parentForm);
                }
            }
            
            // Subscribe to StyleManager updates to keep in sync
            if (styleManager != null)
            {
                // Sync on form shown and force layout update
                // Also rescan and reload config to ensure it's up-to-date with current project
                this.Shown += (s, e) => 
                {
                    // Rescan project in case it changed since dialog was created
                    ScanProjectForSymbols();
                    SetupControls();
                    // Reload config to refresh symbol states
                    LoadGscConf();
                    
                    // Sync controls with StyleManager
                    SyncAllControlsWithStyleManager();
                    // Force layout update to ensure proper display
                    this.PerformLayout();
                    this.Invalidate();
                    this.Update();
                };
                
                // Setup tracked timer to periodically sync with StyleManager (for rainbow theme updates)
                syncTimer = ReaLTaiizorExt.PoisonFormHelper.CreateTrackedTimer(this, 100, (s, e) => SyncAllControlsWithStyleManager());
                syncTimer.Start();
                
                // Setup tracked rainbow update timer - updates all controls when rainbow is active
                rainbowTimer = ReaLTaiizorExt.PoisonFormHelper.CreateTrackedTimer(this, 16, (s, e) =>
                {
                    if (IsRainbowStyleActive())
                    {
                        // Invalidate all controls recursively to update rainbow colors
                        ReaLTaiizorExt.PoisonControlHelper.RefreshAllControls(this);
                        
                        // Also invalidate the form itself for border updates
                        this.Invalidate(true);
                        this.Update();
                    }
                });
                rainbowTimer.Start();
            }
            else
            {
                // Even without style manager, ensure proper layout on show
                this.Shown += (s, e) => 
                {
                    this.PerformLayout();
                    this.Invalidate();
                    this.Update();
                };
            }
        }
        
        private void SetupControls()
        {
            // Controls are now created in InitializeComponent (Designer file)
            // This method just wires up event handlers and sets dynamic properties
            // StyleManager is applied by PoisonFormHelper.InitializeForm() in constructor
            
            // Setup DataGridView for symbols
            SetupSymbolsDataGridView();
            
            // Set dynamic text properties - preserve Designer text and append dynamic content
            // Get base text from Designer and append filename
            string filePathBaseText = lblFilePath.Text;
            // Remove any previously appended filename (in case SetupControls is called multiple times)
            if (filePathBaseText.Contains(":"))
            {
                int colonIndex = filePathBaseText.LastIndexOf(':');
                filePathBaseText = filePathBaseText.Substring(0, colonIndex + 1);
            }
            lblFilePath.Text = $"{filePathBaseText.TrimEnd()} {Path.GetFileName(gscConfPath)}";
            
            // Wire up radio button event handlers for mutual exclusivity
            rdoMP.CheckedChanged += (s, e) =>
            {
                if (rdoMP.Checked)
                {
                    rdoZM.Checked = false;
                    rdoSP.Checked = false;
                }
                HasChanges = true;
            };
            rdoZM.CheckedChanged += (s, e) =>
            {
                if (rdoZM.Checked)
                {
                    rdoMP.Checked = false;
                    rdoSP.Checked = false;
                }
                HasChanges = true;
            };
            rdoSP.CheckedChanged += (s, e) =>
            {
                if (rdoSP.Checked)
                {
                    rdoMP.Checked = false;
                    rdoZM.Checked = false;
                }
                HasChanges = true;
            };
            
            // availableDefs contains only symbols from #ifdef/#ifndef (namespaces are excluded during scanning)
            // Filter out any remaining namespaces or "namespace" symbol just to be safe
            List<string> allSymbols = availableDefs
                .Where(s => !availableNamespaces.Contains(s, StringComparer.OrdinalIgnoreCase) &&
                           s.ToUpper() != "NAMESPACE")
                .OrderBy(s => s)
                .ToList();
            
            // Update label text with count - preserve Designer text and append count
            // Get base text from Designer, removing any previously appended count
            string symbolsBaseText = lblOtherSymbols.Text;
            // Remove any previously appended count (in case SetupControls is called multiple times)
            if (symbolsBaseText.Contains(" - Found "))
            {
                int foundIndex = symbolsBaseText.IndexOf(" - Found ");
                symbolsBaseText = symbolsBaseText.Substring(0, foundIndex);
            }
            // Append count to the Designer text (preserves whatever text the user set in Designer)
            lblOtherSymbols.Text = $"{symbolsBaseText.TrimEnd()} - Found {allSymbols.Count}:";
            
            // Don't set BackColor/ForeColor here - let StyleManager handle it via SyncAllControlsWithStyleManager
            // The DataGridView colors are set in SyncAllControlsWithStyleManager which is called periodically
            
            // Clear DataGridView before adding items to prevent duplicates
            dgvOtherSymbols.Rows.Clear();
            
            // Add symbols to DataGridView (symbol first, then checkbox)
            foreach (string symbol in allSymbols)
            {
                dgvOtherSymbols.Rows.Add(symbol, false); // symbol name first, unchecked by default
            }
            
            // Button event handlers are now wired up in Designer
            // Position buttons using Anchor property set in Designer
        }
        
        private void SetupSymbolsDataGridView()
        {
            if (dgvOtherSymbols == null) return;
            
            // Clear existing columns
            dgvOtherSymbols.Columns.Clear();
            
            // Add symbol name column first (left side)
            DataGridViewTextBoxColumn symbolColumn = new DataGridViewTextBoxColumn
            {
                Name = "Symbol",
                HeaderText = "Symbol",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };
            dgvOtherSymbols.Columns.Add(symbolColumn);
            
            // Add checkbox column second (right side)
            DataGridViewCheckBoxColumn checkColumn = new DataGridViewCheckBoxColumn
            {
                Name = "Checked",
                HeaderText = "",
                Width = 50,
                ReadOnly = false
            };
            dgvOtherSymbols.Columns.Add(checkColumn);
            
            // Wire up cell value changed event
            dgvOtherSymbols.CellValueChanged += DgvOtherSymbols_CellValueChanged;
            dgvOtherSymbols.CurrentCellDirtyStateChanged += DgvOtherSymbols_CurrentCellDirtyStateChanged;
            
            // Apply StyleManager if available
            if (styleManager != null)
            {
                ReaLTaiizorExt.PoisonControlHelper.ApplyStyleManager(dgvOtherSymbols, styleManager);
            }
        }
        
        private void DgvOtherSymbols_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            // Commit checkbox changes immediately
            if (dgvOtherSymbols.IsCurrentCellDirty && dgvOtherSymbols.CurrentCell is DataGridViewCheckBoxCell)
            {
                dgvOtherSymbols.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }
        
        private void DgvOtherSymbols_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 1 && e.RowIndex >= 0) // Checkbox column is now second (index 1)
            {
                HasChanges = true;
            }
        }
        
        private void ScanProjectForSymbols()
        {
            try
            {
                // Get project folder from gsc.conf path
                string projectFolder = Path.GetDirectoryName(gscConfPath);
                if (string.IsNullOrEmpty(projectFolder) || !Directory.Exists(projectFolder))
                    return;
                
                // Regex patterns for scanning project files
                // #namespace namespace_name; (can be on same line or different line)
                Regex namespacePattern = new Regex(@"#namespace\s+(\w+)\s*;", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                // #ifdef SYMBOL or #ifndef SYMBOL (these are the actual symbols used for conditional compilation)
                Regex ifdefPattern = new Regex(@"#if(?:def|ndef)\s+(\w+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                
                // Pattern to identify constant definitions (like #define mANIM_NONE = 0;)
                // These should be excluded as they're not conditional compilation symbols
                Regex constantDefinePattern = new Regex(@"^\s*#define\s+(\w+)\s*=", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                
                HashSet<string> symbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                HashSet<string> namespaces = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                HashSet<string> constantDefines = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                
                // Scan all .gsc and .csc files in the project
                foreach (string gscFile in Directory.GetFiles(projectFolder, "*.*", SearchOption.AllDirectories)
                    .Where(f => f.EndsWith(".gsc", StringComparison.OrdinalIgnoreCase) || 
                                f.EndsWith(".csc", StringComparison.OrdinalIgnoreCase)))
                {
                    try
                    {
                        string content = File.ReadAllText(gscFile);
                        
                        // First, identify constant definitions to exclude them
                        foreach (Match match in constantDefinePattern.Matches(content))
                        {
                            if (match.Groups.Count > 1)
                            {
                                string constName = match.Groups[1].Value.Trim();
                                if (!string.IsNullOrEmpty(constName))
                                {
                                    constantDefines.Add(constName);
                                }
                            }
                        }
                        
                        // Find all #namespace declarations
                        foreach (Match match in namespacePattern.Matches(content))
                        {
                            if (match.Groups.Count > 1)
                            {
                                string nsName = match.Groups[1].Value.Trim();
                                if (!string.IsNullOrEmpty(nsName))
                                    namespaces.Add(nsName);
                            }
                        }
                        
                        // Find all symbols used in #ifdef/#ifndef blocks (these are the actual conditional compilation symbols)
                        foreach (Match match in ifdefPattern.Matches(content))
                        {
                            if (match.Groups.Count > 1)
                            {
                                string symbolName = match.Groups[1].Value.Trim();
                                if (!string.IsNullOrEmpty(symbolName))
                                {
                                    // Exclude game mode symbols (MP, ZM, SP) and game symbols (BO3, BO4) as they're handled separately
                                    // Also exclude namespace (if found) as it's always included
                                    // Exclude "namespace" - namespaces are not conditional compilation symbols, even if they appear in ifdef blocks
                                    string upperSymbol = symbolName.ToUpper();
                                    bool isNamespace = availableNamespaces.Any(ns => ns.Equals(symbolName, StringComparison.OrdinalIgnoreCase));
                                    if (upperSymbol != "MP" && upperSymbol != "ZM" && upperSymbol != "SP" &&
                                        upperSymbol != "BO3" && upperSymbol != "BO4" && !isNamespace &&
                                        upperSymbol != "NAMESPACE")
                                    {
                                        // Only add if it's not a constant definition (like mANIM_NONE, mBG_COLOR, etc.)
                                        // Also exclude if it's a namespace (namespaces are not conditional compilation symbols)
                                        if (!constantDefines.Contains(symbolName) && 
                                            !namespaces.Contains(symbolName, StringComparer.OrdinalIgnoreCase))
                                        {
                                            symbols.Add(symbolName);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Skip files that can't be read
                        continue;
                    }
                }
                
                // Only include actual conditional compilation symbols from #ifdef/#ifndef blocks
                // Namespaces are NOT conditional compilation symbols and should not be included
                availableDefs = symbols.OrderBy(s => s).ToList();
                availableNamespaces = namespaces.OrderBy(n => n).ToList();
            }
            catch
            {
                // If scanning fails, continue with empty lists
                availableDefs = new List<string>();
                availableNamespaces = new List<string>();
            }
        }
        
        private void LoadGscConf()
        {
            if (!File.Exists(gscConfPath))
            {
                // Default to MP if file doesn't exist
                rdoMP.Checked = true;
                return;
            }
            
            try
            {
                HashSet<string> symbolsFromConf = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                originalGameModeSymbol = null; // Reset to preserve original case of game mode symbol
                originalGameSymbol = null; // Reset to preserve original case of game symbol
                
                foreach (string line in File.ReadAllLines(gscConfPath))
                {
                    if (line.Trim().StartsWith("#")) continue;
                    var split = line.Trim().Split('=');
                    if (split.Length < 2) continue;
                    
                    if (split[0].Trim().Equals("symbols", StringComparison.OrdinalIgnoreCase))
                    {
                        string symbolsValue = split[1].Trim();
                        foreach (string symbol in symbolsValue.Split(','))
                        {
                            string trimmedSymbol = symbol.Trim();
                            if (string.IsNullOrEmpty(trimmedSymbol)) continue;
                            
                            // Add to set for later checking (preserve original case in a separate list)
                            symbolsFromConf.Add(trimmedSymbol);
                            
                            // Preserve original case, but use uppercase for comparison
                            switch (trimmedSymbol.ToUpper())
                            {
                                case "MP":
                                    rdoMP.Checked = true;
                                    originalGameModeSymbol = trimmedSymbol; // Preserve original case (could be "mp", "MP", "Mp", etc.)
                                    break;
                                case "ZM":
                                    rdoZM.Checked = true;
                                    originalGameModeSymbol = trimmedSymbol; // Preserve original case
                                    break;
                                case "SP":
                                    rdoSP.Checked = true;
                                    originalGameModeSymbol = trimmedSymbol; // Preserve original case
                                    break;
                                case "BO3":
                                case "BO4":
                                    originalGameSymbol = trimmedSymbol; // Preserve original case of game symbol
                                    break;
                            }
                        }
                    }
                }
                
                // Check the symbols in the DataGridView - check if they're in gsc.conf
                // This works like the code editor: symbols in gsc.conf are checked/enabled
                // Ensure DataGridView is ready before trying to check items
                if (dgvOtherSymbols != null && dgvOtherSymbols.Rows.Count > 0)
                {
                    foreach (DataGridViewRow row in dgvOtherSymbols.Rows)
                    {
                        if (row.Cells["Symbol"].Value != null)
                        {
                            string symbol = row.Cells["Symbol"].Value.ToString();
                            // Check if this symbol is in gsc.conf (case-insensitive comparison)
                            // symbolsFromConf uses case-insensitive comparison, so this will work correctly
                            bool isChecked = symbolsFromConf.Any(s => s.Equals(symbol, StringComparison.OrdinalIgnoreCase));
                            row.Cells["Checked"].Value = isChecked;
                        }
                    }
                }
                
                HasChanges = false; // Reset after loading
            }
            catch (Exception ex)
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, 
                    $"Failed to load gsc.conf:\n{ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        
        private void BtnOK_Click(object sender, EventArgs e)
        {
            try
            {
                // Validate that exactly one game mode is selected
                if (!rdoMP.Checked && !rdoZM.Checked && !rdoSP.Checked)
                {
                    ReaLTaiizor.Controls.PoisonMessageBox.Show(this, 
                        "Please select exactly one game mode (MP, ZM, or SP).", 
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // Build symbols list - preserve original case from gsc.conf if available
                List<string> symbols = new List<string>();
                
                // Use original case from file if available, otherwise use standard uppercase
                // This allows lowercase symbols like "mp", "zm", "sp" to be preserved
                if (rdoMP.Checked)
                {
                    // Check if we loaded an original case from the file
                    if (!string.IsNullOrEmpty(originalGameModeSymbol) && originalGameModeSymbol.ToUpper() == "MP")
                        symbols.Add(originalGameModeSymbol); // Preserve original case (could be "mp", "MP", etc.)
                    else
                        symbols.Add("MP"); // Default to uppercase if not loaded from file
                }
                else if (rdoZM.Checked)
                {
                    if (!string.IsNullOrEmpty(originalGameModeSymbol) && originalGameModeSymbol.ToUpper() == "ZM")
                        symbols.Add(originalGameModeSymbol); // Preserve original case
                    else
                        symbols.Add("ZM"); // Default to uppercase
                }
                else if (rdoSP.Checked)
                {
                    if (!string.IsNullOrEmpty(originalGameModeSymbol) && originalGameModeSymbol.ToUpper() == "SP")
                        symbols.Add(originalGameModeSymbol); // Preserve original case
                    else
                        symbols.Add("SP"); // Default to uppercase
                }
                
                // Add other symbols from checked DataGridView rows (including DEBUG, killstreaks, serious, XBOX, etc.)
                // Preserve original case from the DataGridView items
                foreach (DataGridViewRow row in dgvOtherSymbols.Rows)
                {
                    if (row.Cells["Checked"].Value is bool isChecked && isChecked && row.Cells["Symbol"].Value != null)
                    {
                        string trimmed = row.Cells["Symbol"].Value.ToString().Trim();
                        if (!string.IsNullOrEmpty(trimmed) && 
                            !symbols.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
                        {
                            symbols.Add(trimmed); // Preserve original case
                        }
                    }
                }
                
                // Always add BO3/BO4 and SERIOUS (these are always included like in the code editor)
                // Use the game symbol we loaded from gsc.conf, or default to BO3
                string gameSymbol = !string.IsNullOrEmpty(originalGameSymbol) ? originalGameSymbol : "BO3";
                
                // Add game symbol and namespace (if found, always included, like in code editor)
                if (!symbols.Contains(gameSymbol, StringComparer.OrdinalIgnoreCase))
                {
                    symbols.Add(gameSymbol);
                }
                // Add namespace from source files (first one found)
                if (availableNamespaces.Count > 0)
                {
                    string namespaceSymbol = availableNamespaces[0]; // Use first namespace found
                    if (!symbols.Contains(namespaceSymbol, StringComparer.OrdinalIgnoreCase))
                    {
                        symbols.Add(namespaceSymbol);
                    }
                }
                
                // Read existing file to preserve other settings
                List<string> lines = new List<string>();
                bool symbolsLineFound = false;
                
                if (File.Exists(gscConfPath))
                {
                    foreach (string line in File.ReadAllLines(gscConfPath))
                    {
                        if (line.Trim().StartsWith("#"))
                        {
                            lines.Add(line);
                        }
                        else
                        {
                            var split = line.Trim().Split('=');
                            if (split.Length >= 2 && split[0].Trim().Equals("symbols", StringComparison.OrdinalIgnoreCase))
                            {
                                lines.Add($"symbols={string.Join(",", symbols)}");
                                symbolsLineFound = true;
                            }
                            else if (!string.IsNullOrWhiteSpace(line))
                            {
                                lines.Add(line);
                            }
                        }
                    }
                }
                
                if (!symbolsLineFound)
                {
                    lines.Insert(0, $"symbols={string.Join(",", symbols)}");
                }
                
                // Write to file
                File.WriteAllLines(gscConfPath, lines);
                
                SelectedSymbols = string.Join(",", symbols);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this, 
                    $"Failed to save gsc.conf:\n{ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void SyncAllControlsWithStyleManager()
        {
            if (styleManager == null || this.IsDisposed || !this.IsHandleCreated) return;
            
            try
            {
                // Use PoisonControlHelper to sync all controls recursively
                // This handles StyleManager, Theme, Style, and UseStyleColors automatically
                ReaLTaiizorExt.PoisonControlHelper.ApplyStyleManager(this, styleManager);
                
                // Update DataGridView StyleManager (PoisonDataGridView supports StyleManager automatically)
                if (dgvOtherSymbols != null && styleManager != null)
                {
                    ReaLTaiizorExt.PoisonControlHelper.ApplyStyleManager(dgvOtherSymbols, styleManager);
                }
            }
            catch
            {
                // Silently ignore errors during sync (form might be disposing)
            }
        }
        
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
        }
        
        private bool IsRainbowStyleActive()
        {
            return parentForm != null && parentForm.IsRainbowStyleActive();
        }
        
    }
}

