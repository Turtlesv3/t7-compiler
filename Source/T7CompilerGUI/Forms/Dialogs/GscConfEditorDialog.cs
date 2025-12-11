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
using T7CompilerGUI.Controls;

namespace T7CompilerGUI.Forms.Dialogs
{
    public partial class GscConfEditorDialog : PoisonForm
    {
        private PoisonStyleManager styleManager;
        private Forms.MainForm parentForm;
        private string gscConfPath;
        private System.Windows.Forms.Timer syncTimer; // Timer to sync with StyleManager
        
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
            
            // Scan project for symbols before setting up controls
            ScanProjectForSymbols();
            SetupControls();
            LoadGscConf();
            PoisonControlHelper.SetupAllButtonEffectsRecursive(this);
            
            // Subscribe to StyleManager updates to keep in sync
            if (styleManager != null)
            {
                // Sync on form shown
                this.Shown += (s, e) => SyncAllControlsWithStyleManager();
                
                // Setup timer to periodically sync with StyleManager (for rainbow theme updates)
                syncTimer = new System.Windows.Forms.Timer
                {
                    Interval = 100 // Check every 100ms
                };
                syncTimer.Tick += (s, e) => SyncAllControlsWithStyleManager();
                syncTimer.Start();
            }
        }
        
        private void SetupControls()
        {
            // Controls are now created in InitializeComponent (Designer file)
            // This method just wires up event handlers and sets dynamic properties
            
            if (styleManager != null)
            {
                this.StyleManager = styleManager;
            }
            
            // Set dynamic text properties
            lblFilePath.Text = $"Config File: {Path.GetFileName(gscConfPath)}";
            
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
            
            // availableDefs already includes both symbols from #ifdef/#ifndef and namespaces
            // So we just use availableDefs directly (it's already a combined list)
            List<string> allSymbols = availableDefs.OrderBy(s => s).ToList();
            
            // Update label text with count
            lblOtherSymbols.Text = $"Additional Symbols (from project) - Found {allSymbols.Count}:";
            
            // Apply Poison theme colors to the listbox
            if (styleManager != null)
            {
                lstOtherSymbols.BackColor = PoisonPaint.BackColor.Form(styleManager.Theme);
                lstOtherSymbols.ForeColor = PoisonPaint.ForeColor.Label.Normal(styleManager.Theme);
            }
            
            lstOtherSymbols.Items.AddRange(allSymbols.ToArray());
            lstOtherSymbols.ItemCheck += (s, e) => { HasChanges = true; };
            
            // Button event handlers are now wired up in Designer
            // Position buttons using Anchor property set in Designer
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
                                    // Also exclude SERIOUS as it's always included
                                    string upperSymbol = symbolName.ToUpper();
                                    if (upperSymbol != "MP" && upperSymbol != "ZM" && upperSymbol != "SP" &&
                                        upperSymbol != "BO3" && upperSymbol != "BO4" && upperSymbol != "SERIOUS")
                                    {
                                        // Only add if it's not a constant definition (like mANIM_NONE, mBG_COLOR, etc.)
                                        if (!constantDefines.Contains(symbolName))
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
                
                // Check the symbols in the listbox - check if they're in gsc.conf
                // This works like the code editor: symbols in gsc.conf are checked/enabled
                for (int i = 0; i < lstOtherSymbols.Items.Count; i++)
                {
                    string item = lstOtherSymbols.Items[i].ToString();
                    // Check if this symbol is in gsc.conf (case-insensitive comparison)
                    bool isChecked = symbolsFromConf.Contains(item);
                    lstOtherSymbols.SetItemChecked(i, isChecked);
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
            PoisonControlHelper.ResetButtonState(sender);
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
                
                // Add other symbols from checked listbox (including DEBUG, killstreaks, serious, XBOX, etc.)
                // Preserve original case from the listbox items
                foreach (string item in lstOtherSymbols.CheckedItems)
                    {
                    string trimmed = item.ToString().Trim();
                        if (!string.IsNullOrEmpty(trimmed) && 
                            !symbols.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
                        {
                        symbols.Add(trimmed); // Preserve original case
                    }
                }
                
                // Always add BO3/BO4 and SERIOUS (these are always included like in the code editor)
                // Use the game symbol we loaded from gsc.conf, or default to BO3
                string gameSymbol = !string.IsNullOrEmpty(originalGameSymbol) ? originalGameSymbol : "BO3";
                
                // Add game symbol and SERIOUS (always included, like in code editor)
                if (!symbols.Contains(gameSymbol, StringComparer.OrdinalIgnoreCase))
                {
                    symbols.Add(gameSymbol);
                }
                if (!symbols.Contains("SERIOUS", StringComparer.OrdinalIgnoreCase))
                {
                    symbols.Add("SERIOUS");
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
            if (styleManager == null) return;
            
            // Sync form properties
            if (this.StyleManager != styleManager)
            {
                this.StyleManager = styleManager;
            }
            if (this.Theme != styleManager.Theme)
            {
                this.Theme = styleManager.Theme;
            }
            if (this.Style != styleManager.Style)
            {
                this.Style = styleManager.Style;
            }
            
            // Sync all controls recursively
            SyncControlsRecursive(this);
            
            // Update listbox colors
            if (lstOtherSymbols != null && styleManager != null)
            {
                lstOtherSymbols.BackColor = PoisonPaint.BackColor.Form(styleManager.Theme);
                lstOtherSymbols.ForeColor = PoisonPaint.ForeColor.Label.Normal(styleManager.Theme);
            }
        }
        
        private void SyncControlsRecursive(Control parent)
        {
            if (parent == null || styleManager == null) return;
            
            foreach (Control ctrl in parent.Controls)
            {
                // Sync IPoisonControl controls
                if (ctrl is ReaLTaiizor.Interface.Poison.IPoisonControl poisonCtrl)
                {
                    if (poisonCtrl.StyleManager != styleManager)
                    {
                        poisonCtrl.StyleManager = styleManager;
                    }
                    
                    // Ensure Style and Theme are Default to follow StyleManager
                    var styleProp = ctrl.GetType().GetProperty("Style");
                    var themeProp = ctrl.GetType().GetProperty("Theme");
                    
                    if (styleProp != null && styleProp.CanWrite)
                    {
                        var currentStyle = styleProp.GetValue(ctrl);
                        if (currentStyle == null || currentStyle.ToString() != "Default")
                        {
                            styleProp.SetValue(ctrl, ReaLTaiizor.Enum.Poison.ColorStyle.Default);
                        }
                    }
                    
                    if (themeProp != null && themeProp.CanWrite)
                    {
                        var currentTheme = themeProp.GetValue(ctrl);
                        if (currentTheme == null || currentTheme.ToString() != "Default")
                        {
                            themeProp.SetValue(ctrl, ReaLTaiizor.Enum.Poison.ThemeStyle.Default);
                        }
                    }
                    
                    // Ensure UseStyleColors is enabled
                    var useStyleColorsProp = ctrl.GetType().GetProperty("UseStyleColors");
                    if (useStyleColorsProp != null && useStyleColorsProp.CanWrite)
                    {
                        useStyleColorsProp.SetValue(ctrl, true);
                    }
                    
                    ctrl.Invalidate();
                }
                
                // Sync IPoisonComponent controls (like menus)
                if (ctrl is ReaLTaiizor.Interface.Poison.IPoisonComponent poisonComponent)
                {
                    if (poisonComponent.StyleManager != styleManager)
                    {
                        poisonComponent.StyleManager = styleManager;
                    }
                    ctrl.Invalidate();
                }
                
                // Recursively sync child controls
                if (ctrl.HasChildren)
                {
                    SyncControlsRecursive(ctrl);
                }
            }
        }
        
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // Stop and dispose sync timer
            if (syncTimer != null)
            {
                syncTimer.Stop();
                syncTimer.Dispose();
                syncTimer = null;
            }
            
            base.OnFormClosed(e);
        }
    }
}

