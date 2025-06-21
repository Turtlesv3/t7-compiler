using DebugCompiler.UI.Core.Controls;
using DebugCompiler.UI.Core.Interfaces;
using DebugCompiler.UI.Core.Singletons;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using TreyarchCompiler.Enums;
using System.Text;


namespace DebugCompiler
{
    public partial class MainForm1 : Form, IThemeableControl
    {
        private readonly Root compilerRoot;
        private readonly ToolTip resetToolTip = new ToolTip
        {
            AutoPopDelay = 5000,
            InitialDelay = 500,
            ReshowDelay = 500,
            ShowAlways = true
        };

        // State tracking fields
        private DateTime _lastInjectionTime;
        private string _lastInjectedScript;
        private string _lastGameMode;
        private readonly TextWriter _originalOut = Console.Out;
        private readonly object _outputLock = new object();
        private bool _isInternalUpdate = false;
        private List<string> _currentOptions = new List<string>();

        // New fields for enhanced functionality
        private CancellationTokenSource _compilationCts;
        private readonly Color _errorColor = Color.FromArgb(255, 100, 100);
        private readonly Color _warningColor = Color.FromArgb(255, 203, 107);
        private readonly Color _successColor = Color.FromArgb(100, 255, 100);
        private readonly Color _infoColor = Color.FromArgb(100, 200, 255);
        private volatile bool _isCompiling = false; // Add volatile for thread safety
        private volatile bool _isInjecting = false;

        private ToolStripMenuItem _themeMenu;
        internal static class NativeMethods
        {
            [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
            private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

            public static void SetDarkScrollBars(IntPtr handle)
            {
                try
                {
                    if (UIThemeManager.CurrentTheme.IsDarkTheme)
                    {
                        SetWindowTheme(handle, "DarkMode_Explorer", null);
                    }
                    else
                    {
                        SetWindowTheme(handle, "Explorer", null);
                    }
                }
                catch (EntryPointNotFoundException)
                {
                    // Fallback if API not available (Windows 7 or older)
                    // We'll just use default scrollbars
                }
            }
        }

        private void InitializeThemeMenu()
        {
            _themeMenu = new ToolStripMenuItem("Themes");

            foreach (var theme in UIThemeInfo.AvailableThemes)
            {
                var item = new ToolStripMenuItem(theme.Name)
                {
                    Tag = theme.Name,
                };
                item.Click += (s, e) => UIThemeManager.SetTheme(theme.Name);
                _themeMenu.DropDownItems.Add(item);
            }

            // Add to existing menu or create new
            if (MainMenuStrip == null)
            {
                var menuStrip = new MenuStrip();
                menuStrip.GripStyle = ToolStripGripStyle.Hidden;
                menuStrip.Visible = false; // Start hidden
                menuStrip.Items.Add(_themeMenu);
                Controls.Add(menuStrip);
                MainMenuStrip = menuStrip;
            }
            else
            {
                MainMenuStrip.Items.Add(_themeMenu);
            }
        }

        public void ShowThemeMenu(bool show)
        {
            if (MainMenuStrip != null)
            {
                MainMenuStrip.Visible = show;
                if (show)
                {
                    MainMenuStrip.Location = new Point(
                        this.ClientSize.Width - 120,
                        0);
                    MainMenuStrip.BringToFront();
                }
            }
        }

        public MainForm1()
        {
            InitializeComponent();

            // Add menu items
            var fileMenu = new ToolStripMenuItem("File");
            var exitItem = new ToolStripMenuItem("Exit");
            fileMenu.DropDownItems.Add(exitItem);

            // Add status label
            var statusLabel = new ToolStripStatusLabel();
            statusLabel.Text = "Ready";

            // Theme setup
            InitializeThemeMenu();
            ShowThemeMenu(false);
            UIThemeManager.RegisterControl(this);
            UIThemeManager.ThemeChanged += OnThemeChanged_Implementation;
            MaximizeBox = true;
            MinimizeBox = true;

            UIThemeManager.ThemeChanged += (theme) =>
            {
                btnInject.Invalidate();
                btnCompile.Invalidate();
                btnBrowse.Invalidate();
            };

            // Initial theme apply
            this.HandleCreated += (s, e) => {
                ApplyTheme(UIThemeManager.CurrentTheme);
            };

            // Toggle with Ctrl+T
            this.KeyPreview = true;
            this.KeyDown += (s, e) => {
                if (e.Control && e.KeyCode == Keys.T)
                    ShowThemeMenu(!MainMenuStrip.Visible);
            };

            txtScriptPath.TextChanged += TxtScriptPath_TextChanged;
            UpdateInjectButtonState(); // Initial button state

            // Initialize components
            InitializeCustomComponents();
            InitializeOutputColors();

            // Compiler setup
            compilerRoot = new Root();
            compilerRoot.OnLogMessage += (msg) => SafeAppendText(msg + "\n");
            compilerRoot.OnError += (err) => SafeAppendText("[ERROR] " + err + "\n");

            CheckRequiredFiles();
            this.Text = $"T7/T8 Compiler v{GetVersion()} - by Serious -GUI by DoubleG ;)";
        }

        private void InitializeOutputColors()
        {
            txtOutput.ForeColor = Color.FromKnownColor(KnownColor.WindowText);
            txtOutput.BackColor = Color.FromKnownColor(KnownColor.Window);
        }

        public void ApplyTheme(UIThemeInfo theme)
        {

            if (IsDisposed || Disposing || !IsHandleCreated)
                return;

            if (InvokeRequired)
            {
                // Only invoke if handle is created
                if (IsHandleCreated)
                {
                    Invoke(new Action<UIThemeInfo>(ApplyTheme), theme);
                }
                return;
            }

            this.SuspendLayout();
            try
            {
                // Apply to form
                this.BackColor = theme.BackColor;
                this.ForeColor = theme.TextColor;

                // Apply to all controls recursively
                ApplyThemeToControls(this.Controls, theme);

                // Special handling for RichTextBox
                txtOutput.BackColor = theme.TextBoxBackColor;
                txtOutput.ForeColor = theme.TextColor;
                txtOutput.BorderStyle = theme.TextBoxBorderStyle;

                // Force redraw
                this.Invalidate(true);
            }
            finally
            {
                this.ResumeLayout(true);
            }
        }

        private void ApplyThemeToControls(Control.ControlCollection controls, UIThemeInfo theme)
        {
            foreach (Control control in controls)
            {
                if (control is Button button && (button == btnInject || button == btnCompile || button == btnBrowse))
                {
                    // Special theming for action buttons
                    button.BackColor = theme.AccentColor;
                    button.ForeColor = Color.White; // High contrast text
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = theme.AccentColor;
                    button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(theme.AccentColor, 0.2f);
                    button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(theme.AccentColor, 0.2f);
                    button.Font = new Font(button.Font, FontStyle.Bold);
                }
                else if (control is Button standardButton)
                {
                    // Standard button theming
                    standardButton.BackColor = theme.ButtonBackColor;
                    standardButton.ForeColor = theme.TextColor;
                    standardButton.FlatStyle = theme.ButtonFlatStyle;
                    standardButton.FlatAppearance.BorderColor = theme.BorderColor;
                    standardButton.FlatAppearance.MouseOverBackColor = theme.ButtonHoverColor;
                    standardButton.FlatAppearance.MouseDownBackColor = theme.ButtonActiveColor;
                }
                else if (control is RichTextBox rtb)
                {
                    rtb.BackColor = theme.TextBoxBackColor;
                    rtb.ForeColor = theme.TextColor;
                    rtb.BorderStyle = theme.TextBoxBorderStyle;
                    NativeMethods.SetDarkScrollBars(rtb.Handle);
                }
                else if (control is TextBox txt)
                {
                    txt.BackColor = theme.TextBoxBackColor;
                    txt.ForeColor = theme.TextColor;
                    txt.BorderStyle = theme.TextBoxBorderStyle;
                }
                else if (control is ComboBox cmb)
                {
                    cmb.BackColor = theme.TextBoxBackColor;
                    cmb.ForeColor = theme.TextColor;
                    cmb.FlatStyle = theme.ButtonFlatStyle;
                }
                else if (control is Label || control is CheckBox || control is RadioButton)
                {
                    control.ForeColor = theme.TextColor;
                }
                else if (control is Panel || control is GroupBox)
                {
                    control.BackColor = theme.ControlBackColor;
                    control.ForeColor = theme.TextColor;
                }
                else if (control is ToolStrip toolStrip)
                {
                    toolStrip.BackColor = theme.BackColor;
                    toolStrip.ForeColor = theme.TextColor;
                }

                // Recursively apply to children
                if (control.HasChildren)
                {
                    ApplyThemeToControls(control.Controls, theme);
                }
            }
        }

        private void SafeAppendText(string text)
        {
            if (txtOutput.InvokeRequired)
            {
                txtOutput.Invoke(new Action<string>(SafeAppendText), text);
            }
            else
            {
                lock (_outputLock)
                {
                    txtOutput.SuspendLayout();
                    try
                    {
                        // Determine color based on message content
                        Color color = txtOutput.ForeColor; // Default color
                        if (text.Contains("[ERROR]")) color = _errorColor;
                        else if (text.Contains("[WARNING]")) color = _warningColor;
                        else if (text.Contains("SUCCESS")) color = _successColor;
                        else if (text.Contains("[INFO]") || text.Contains("[CONFIG]")) color = _infoColor;

                        // Save current selection
                        int start = txtOutput.TextLength;
                        txtOutput.AppendText(text);
                        int end = txtOutput.TextLength;

                        // Apply color
                        txtOutput.Select(start, end - start);
                        txtOutput.SelectionColor = color;
                        txtOutput.SelectionLength = 0; // Clear selection
                        txtOutput.ScrollToCaret();
                    }
                    finally
                    {
                        txtOutput.ResumeLayout();
                    }
                }
            }
        }

        private async Task AppendColoredTextAsync(string text, Color color)
        {
            if (txtOutput.InvokeRequired)
            {
                await Task.Run(() => txtOutput.Invoke(new Action<string, Color>(AppendColoredTextSync), text, color));
            }
            else
            {
                AppendColoredTextSync(text, color);
            }
        }

        private void AppendColoredTextSync(string text, Color color)
        {
            lock (_outputLock)
            {
                txtOutput.SuspendLayout();
                try
                {
                    int start = txtOutput.TextLength;
                    txtOutput.AppendText(text);
                    int end = txtOutput.TextLength;

                    txtOutput.Select(start, end - start);
                    txtOutput.SelectionColor = color;
                    txtOutput.SelectionLength = 0;
                    txtOutput.ScrollToCaret();
                }
                finally
                {
                    txtOutput.ResumeLayout();
                }
            }
        }

        public IEnumerable<Control> GetThemedControls()
        {
            yield return InnerForm;
            yield return txtOutput;
            yield return btnResetParseTree;
            yield return btnInject;
            yield return btnCompile;
            yield return btnBrowse;
            yield return txtScriptPath;
            yield return chkNoRuntime;
            yield return cmbHotMode;
            yield return chkHotLoad;
            yield return chkCompileOnly;
            yield return chkBuild;
            yield return cmbGame;
        }

        private void OnThemeChanged_Implementation(UIThemeInfo currentTheme)
        {

            if (IsDisposed || Disposing || !IsHandleCreated)
                return;

            if (InvokeRequired)
            {
                if (IsHandleCreated)
                {
                    Invoke(new Action<UIThemeInfo>(OnThemeChanged_Implementation), currentTheme);
                }
                return;
            }

            // Only proceed if the theme is actually different
            if (this.BackColor != currentTheme.BackColor ||
                this.ForeColor != currentTheme.TextColor)
            {
                ApplyTheme(currentTheme);
            }

            // Special case for tooltips
            resetToolTip.BackColor = currentTheme.BackColor;
            resetToolTip.ForeColor = currentTheme.TextColor;

            // For ComboBox items (if needed)
            if (cmbGame is not null)
            {
                cmbGame.Invalidate();
            }
            btnInject.Invalidate();
            btnCompile.Invalidate();
            btnBrowse.Invalidate();
            btnResetParseTree.Invalidate();
        }

        public void WndProc_Implementation(ref Message m)
        {
            base.WndProc(ref m);
        }

        private void InitializeCustomComponents()
        {
            var gameDisplayNames = new Dictionary<string, string>
            {
                {"t6", "Call of Duty: Black Ops 2"},
                {"t7", "Call of Duty: Black Ops 3"},
                {"t8", "Call of Duty: Black Ops 4"}
            };

            var gameNames = Enum.GetNames(typeof(Games))
                .Select(name => gameDisplayNames.TryGetValue(name.ToLower(), out var displayName)
                        ? displayName
                        : name)
                .ToArray();

            cmbGame.Items.AddRange(gameNames);
            cmbGame.SelectedIndex = 1;

            cmbHotMode.Items.AddRange(new[] { "GSC", "CSC" });
            cmbHotMode.SelectedIndex = 0;
            cmbHotMode.Enabled = false;

            resetToolTip.SetToolTip(btnResetParseTree,
                "Reset GSC Parse Tree\n\n" +
                $"Last Injection: {_lastInjectedScript ?? "None"}\n" +
                $"Game: {_lastGameMode ?? "None"}\n" +
                $"Time: {_lastInjectionTime:HH:mm:ss}\n\n" +
                "WARNING: May cause crashes if done while in-game!");

            resetToolTip.SetToolTip(btnInject,
                "Inject Compiled Script\n\n" +
                "Requirements:\n" +
                "- Valid .gsc/gscc file selected\n" +
                "- Game process running\n" +
                "- Proper compiler setup");

            resetToolTip.SetToolTip(btnCompile,
                "Compile Script\n\n" +
                "Requirements:\n" +
                "- Select a .gsc file or folder first\n\n" +
                "Compile Options:\n" +
                "- Full Build: Complete Build+Inject\n" +
                "- Compile Only: Skip injection");

            resetToolTip.SetToolTip(chkBuild,
                "Full Build\n\n" +
                "Performs a complete rebuild of all scripts\n" +
                "Slower but ensures everything is fresh");

            resetToolTip.SetToolTip(chkCompileOnly,
                "Compile Only\n\n" +
                "Compiles without injecting into the game\n" +
                "Useful for testing compilation only");

            resetToolTip.SetToolTip(chkHotLoad,
                "Hot Load\n\n" +
                "Enables runtime script reloading\n" +
                "Requires game support for hot loading");

            resetToolTip.SetToolTip(chkNoRuntime,
                "No Runtime\n\n" +
                "Disables runtime type checking\n" +
                "Faster but less safe compilation");

            resetToolTip.SetToolTip(cmbHotMode,
                "Hot Load Mode\n\n" +
                "GSC: Standard script hot loading\n" +
                "CSC: Client-side script hot loading");
        }

        private string GetVersion()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("DebugCompiler.version"))
                {
                    if (stream != null)
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            return reader.ReadToEnd().Trim();
                        }
                    }
                }
            }
            catch
            {
                return "Unknown";
            }
            return "Unknown";
        }

        private void CheckRequiredFiles()
        {
            string[] requiredDlls = { "t7cinternal.dll", "xdrpc.dll", "t8cinternal.dll" };
            bool allExist = true;
            string missingFiles = "";

            foreach (var dll in requiredDlls)
            {
                string dllPath = Path.Combine(Application.StartupPath, dll);
                if (!File.Exists(dllPath))
                {
                    missingFiles += $"\n- {dllPath}";
                    allExist = false;
                }
            }

            if (!allExist)
            {
                string message = $"Missing required DLLs:{missingFiles}\n\n" +
                                "Please ensure these files are in the same directory as the compiler.\n" +
                                "They should be automatically copied from the 'lib' folder during build.";

                MessageBox.Show(message, "Missing Required Files",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);

                btnInject.Enabled = false;
                btnCompile.Enabled = false;
            }
        }

        private void UpdateUIState()
        {
            if (IsDisposed || Disposing) return;

            if (InvokeRequired)
            {
                Invoke(new Action(UpdateUIState));
                return;
            }

            btnCompile.Text = _isCompiling ? "Cancel" : "Compile";
            btnCompile.Enabled = true;
            btnResetParseTree.Enabled = !_isCompiling && !_isInjecting;
            btnBrowse.Enabled = !_isCompiling && !_isInjecting;

            chkBuild.Enabled = !_isCompiling && !_isInjecting;
            chkCompileOnly.Enabled = !_isCompiling && !_isInjecting;
            chkHotLoad.Enabled = !_isCompiling && !_isInjecting;
            chkNoRuntime.Enabled = !_isCompiling && !_isInjecting;
            cmbHotMode.Enabled = !_isCompiling && !_isInjecting && chkHotLoad.Checked;
            cmbGame.Enabled = !_isCompiling && !_isInjecting;

            btnInject.Refresh();
            btnCompile.Refresh();
        }

        private async Task<bool> ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(txtScriptPath.Text))
            {
                await AppendColoredTextAsync("[ERROR] Please select a script file or folder\n", _errorColor);
                UpdateUIState(); // Explicitly update state after validation failure
                return false;
            }

            if (!File.Exists(txtScriptPath.Text) && !Directory.Exists(txtScriptPath.Text))
            {
                await AppendColoredTextAsync($"[ERROR] Path does not exist: {txtScriptPath.Text}\n", _errorColor);
                UpdateUIState(); // Explicitly update state after validation failure
                return false;
            }

            return true;
        }

        private async void BtnCompile_Click(object sender, EventArgs e)
        {
            if (_isCompiling)
            {
                _compilationCts?.Cancel();
                return;
            }

            _isCompiling = true;
            UpdateUIState();
            ClearOutput();
            _compilationCts = new CancellationTokenSource();

            try
            {
                if (!await ValidateInputs())
                {
                    return;
                }

                var token = _compilationCts.Token;
                await AppendColoredTextAsync($"[{DateTime.Now:HH:mm:ss}] Starting compilation...\n", _infoColor);

                var args = BuildCompilerArguments();
                await LogCompilerConfiguration(args);

                int result = await Task.Run(() =>
                {
                    try
                    {
                        return compilerRoot.ExecuteCommandLine(args.ToArray());
                    }
                    catch (OperationCanceledException)
                    {
                        return -2; // Special code for cancellation
                    }
                    catch (Exception ex)
                    {
                        SafeAppendText($"[COMPILER ERROR] {ex.Message}\n");
                        return -1;
                    }
                }, token);

                await HandleCompilationResult(result);
            }
            catch (OperationCanceledException)
            {
                await AppendColoredTextAsync("[INFO] Compilation was cancelled\n", _infoColor);
            }
            catch (Exception ex)
            {
                await AppendColoredTextAsync($"[SYSTEM ERROR] {ex.Message}\n", _errorColor);
            }
            finally
            {
                _isCompiling = false;
                UpdateUIState();
                _compilationCts?.Dispose();
                _compilationCts = null;
                UpdateInjectButtonState();
            }
        }

        private List<string> BuildCompilerArguments()
        {
            var args = new List<string> { txtScriptPath.Text, cmbGame.Text };

            if (chkBuild.Checked) args.Add("--build");
            if (Directory.Exists(txtScriptPath.Text)) args.Add("--batch");
            if (chkCompileOnly.Checked) args.Add("--compile");

            if (chkHotLoad.Checked)
            {
                args.Add("--hot");
                args.Add(cmbHotMode.SelectedIndex == 0 ? "gsc" : "csc");
            }

            if (chkNoRuntime.Checked) args.Add("--noruntime");

            return args;
        }

        private async Task LogCompilerConfiguration(List<string> args)
        {
            await AppendColoredTextAsync("[CONFIG] Compiler Options:\n", _infoColor);
            await AppendColoredTextAsync($"- Target: {(File.Exists(txtScriptPath.Text) ? "File" : "Folder")} {txtScriptPath.Text}\n", _infoColor);
            await AppendColoredTextAsync($"- Game: {cmbGame.Text}\n", _infoColor);

            if (chkBuild.Checked) await AppendColoredTextAsync("- Full Build: Enabled\n", _infoColor);
            if (Directory.Exists(txtScriptPath.Text)) await AppendColoredTextAsync("- Batch Mode: Enabled\n", _infoColor);
            if (chkCompileOnly.Checked) await AppendColoredTextAsync("- Compile Only: Enabled\n", _infoColor);
            if (chkHotLoad.Checked) await AppendColoredTextAsync($"- Hot Load: {cmbHotMode.SelectedItem}\n", _infoColor);
            if (chkNoRuntime.Checked) await AppendColoredTextAsync("- No Runtime: Enabled\n", _infoColor);
        }

        private async Task HandleCompilationResult(int result)
        {
            switch (result)
            {
                case 0:
                    await AppendColoredTextAsync("\nCOMPILATION SUCCESSFUL\n", _successColor);
                    await HandleSuccessfulCompilation();
                    break;
                case -2:
                    await AppendColoredTextAsync("\nCOMPILATION CANCELLED\n", _warningColor);
                    break;
                default:
                    await AppendColoredTextAsync("\nCOMPILATION FAILED\n", _errorColor);
                    await AppendColoredTextAsync($"[ERROR] Exit code: {result}\n", _errorColor);
                    break;
            }
        }

        private async Task HandleSuccessfulCompilation()
        {
            string outputPath = GetOutputPath();

            if (File.Exists(outputPath))
            {
                await AppendColoredTextAsync($"Output file: {outputPath}\n", _infoColor);

                if (!chkCompileOnly.Checked && !chkBuild.Checked)
                {
                    await Task.Delay(300);
                    await InjectCompiledScript(outputPath);
                }
            }
            else
            {
                await AppendColoredTextAsync("[WARNING] No output file was generated\n", _warningColor);
            }
        }

        private string GetOutputPath()
        {
            return Directory.Exists(txtScriptPath.Text)
                ? Path.Combine(txtScriptPath.Text, "bin", "compiled.gscc")
                : Path.ChangeExtension(txtScriptPath.Text, ".gscc");
        }

        private async void BtnResetParseTree_Click(object sender, EventArgs e)
        {
            var confirm = MessageBox.Show(
                "WARNING: Resetting parse tree while in-game may cause crashes!\n\n" +
                "Are you sure you want to reset the GSC parse tree?",
                "Confirm Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            await Task.Run(() => ClearOutput());

            try
            {
                btnResetParseTree.Enabled = false;
                Cursor = Cursors.WaitCursor;
                SafeAppendText($"[{DateTime.Now:HH:mm:ss}] Resetting GSC parse tree...\n");

                await Task.Run(() => compilerRoot.PublicFreeActiveScript(true));

                SafeAppendText($"[{DateTime.Now:HH:mm:ss}] Reset completed. Reload map for full cleanup.\n");
            }
            catch (Exception ex)
            {
                SafeAppendText($"[ERROR] {ex.Message}\n");
                MessageBox.Show(ex.Message, "Reset Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UpdateResetButton(false);
                Cursor = Cursors.Default;
            }
        }

        private void UpdateResetButton(bool visible)
        {
            if (btnResetParseTree.InvokeRequired)
            {
                btnResetParseTree.Invoke(new Action<bool>(UpdateResetButton), visible);
            }
            else
            {
                btnResetParseTree.Visible = visible;
                btnResetParseTree.Enabled = visible;

                if (visible)
                {
                    resetToolTip.SetToolTip(btnResetParseTree,
                        $"Last Injected: {_lastInjectedScript}\n" +
                        $"Game: {_lastGameMode}\n" +
                        $"Time: {_lastInjectionTime:HH:mm:ss}");
                }
            }
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            var menu = new ContextMenuStrip();

            var fileItem = new ToolStripMenuItem("Select File...");
            fileItem.Click += (s, args) => {
                using (var openDialog = new OpenFileDialog())
                {
                    openDialog.Filter = "GSC Files (*.gsc, *.gscc)|*.gsc;*.gscc|All Files (*.*)|*.*";
                    if (openDialog.ShowDialog() == DialogResult.OK)
                    {
                        txtScriptPath.Text = openDialog.FileName;
                        btnResetParseTree.Visible = false;
                    }
                }
            };
            menu.Items.Add(fileItem);

            var folderItem = new ToolStripMenuItem("Select Folder...");
            folderItem.Click += (s, args) => {
                using (var folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "Select folder containing GSC scripts";
                    folderDialog.ShowNewFolderButton = false;

                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        txtScriptPath.Text = folderDialog.SelectedPath;
                        btnResetParseTree.Visible = false;
                    }
                }
            };
            menu.Items.Add(folderItem);

            menu.Show(btnBrowse, new Point(0, btnBrowse.Height));
        }

        private void UpdateCompilerOptions()
        {
            _currentOptions.Clear();

            if (chkNoRuntime.Checked) _currentOptions.Add("--noruntime");
            if (chkHotLoad.Checked)
            {
                _currentOptions.Add("--hot");
                _currentOptions.Add(cmbHotMode.SelectedIndex == 0 ? "gsc" : "csc");
            }
            if (chkBuild.Checked) _currentOptions.Add("--build");
            if (chkCompileOnly.Checked) _currentOptions.Add("--compile");
        }

        private void ChkNoRuntime_CheckedChanged(object sender, EventArgs e)
        {
            if (_isInternalUpdate) return;

            ClearOutput();

            try
            {
                _isInternalUpdate = true;

                if (chkNoRuntime.Checked)
                {
                    if (!chkHotLoad.Checked)
                    {
                        chkHotLoad.Checked = true;
                        cmbHotMode.Enabled = true;
                        SafeAppendText("[CONFIG] Enabled: No Runtime (auto-enabled Hot Load)\n");
                        cmbHotMode.SelectedIndex = 0;
                    }
                    else
                    {
                        SafeAppendText("[CONFIG] Enabled: No Runtime\n");
                    }
                }
                else
                {
                    SafeAppendText("[CONFIG] Disabled: No Runtime\n");
                }
                UpdateCompilerOptions();
            }
            finally
            {
                _isInternalUpdate = false;
            }
        }

        private void ChkHotLoad_CheckedChanged(object sender, EventArgs e)
        {
            if (_isInternalUpdate) return;

            ClearOutput();
            cmbHotMode.Enabled = chkHotLoad.Checked;

            try
            {
                _isInternalUpdate = true;

                if (!chkHotLoad.Checked)
                {
                    if (chkNoRuntime.Checked)
                    {
                        chkNoRuntime.Checked = false;
                        SafeAppendText("[CONFIG] Disabled: Hot Load (auto-disabled No Runtime)\n");
                    }
                    else
                    {
                        SafeAppendText("[CONFIG] Disabled: Hot Load\n");
                    }
                }
                else
                {
                    SafeAppendText("[CONFIG] Enabled: Hot Load\n");
                }
                UpdateCompilerOptions();
            }
            finally
            {
                _isInternalUpdate = false;
            }
        }

        private void CmbHotMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isInternalUpdate || !chkHotLoad.Checked) return;

            ClearOutput();
            SafeAppendText($"[CONFIG] Hot Load Mode: {(cmbHotMode.SelectedIndex == 0 ? "gsc" : "csc")}\n");
            UpdateCompilerOptions();
        }

        private void ChkBuild_CheckedChanged(object sender, EventArgs e)
        {
            if (_isInternalUpdate) return;

            ClearOutput();
            try
            {
                _isInternalUpdate = true;
                if (chkBuild.Checked)
                {
                    chkCompileOnly.Checked = false;
                    SafeAppendText("[CONFIG] Enabled: Full Build\n");
                }
                UpdateCompilerOptions();
            }
            finally
            {
                _isInternalUpdate = false;
            }
        }

        private void ChkCompileOnly_CheckedChanged(object sender, EventArgs e)
        {
            if (_isInternalUpdate) return;

            ClearOutput();
            try
            {
                _isInternalUpdate = true;
                if (chkCompileOnly.Checked)
                {
                    chkBuild.Checked = false;
                    SafeAppendText("[CONFIG] Enabled: Compile Only\n");
                }
                UpdateCompilerOptions();
            }
            finally
            {
                _isInternalUpdate = false;
            }
        }

        private void TxtScriptPath_TextChanged(object sender, EventArgs e)
        {
            UpdateInjectButtonState();
            UpdateUIState();
        }

        private void UpdateInjectButtonState()
        {
            if (IsDisposed || Disposing) return;

            bool shouldEnable = !_isCompiling &&
                               !_isInjecting &&
                               !string.IsNullOrEmpty(txtScriptPath.Text) &&
                               File.Exists(txtScriptPath.Text);

            Action updateAction = () => {
                if (btnInject.Enabled != shouldEnable)
                {
                    btnInject.Enabled = shouldEnable;
                    btnInject.Invalidate();
                    btnInject.Update();

                    // Only show error dialog if we're disabling the button and it wasn't already disabled
                    if (!shouldEnable && btnInject.Enabled)
                    {
                        string reason = "";
                        if (_isCompiling) reason += "• Compilation in progress\n";
                        if (_isInjecting) reason += "• Injection in progress\n";
                        if (string.IsNullOrEmpty(txtScriptPath.Text)) reason += "• No script selected\n";
                        else if (!File.Exists(txtScriptPath.Text)) reason += "• Selected file doesn't exist\n";

                        // Use CErrorDialog instead of tooltip
                        if (!string.IsNullOrEmpty(reason))
                        {
                            CErrorDialog.Show("Injection Unavailable",
                                            "Cannot inject because:\n\n" + reason,
                                            true);
                        }
                    }
                }
            };

            if (btnInject.InvokeRequired)
            {
                try
                {
                    btnInject.BeginInvoke(updateAction);
                }
                catch (ObjectDisposedException)
                {
                    // Silently handle if control is disposed during invoke
                }
            }
            else
            {
                updateAction();
            }
        }

        private void ClearOutput()
        {
            if (txtOutput.InvokeRequired)
            {
                txtOutput.Invoke(new Action(ClearOutput));
            }
            else
            {
                lock (_outputLock)
                {
                    txtOutput.SuspendLayout();
                    txtOutput.Clear();
                    txtOutput.ResumeLayout();
                }
            }
        }

        private bool IsGameRunning()
        {
            try
            {
                string processName = cmbGame.Text switch
                {
                    "Call of Duty: Black Ops 3" => "BlackOps3",
                    "Call of Duty: Black Ops 4" => "BlackOps4",
                    _ => cmbGame.Text.Replace(" ", "").ToLower()
                };
                return Process.GetProcessesByName(processName).Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private async Task InjectCompiledScript(string outputFile)
        {
            try
            {
                if (!IsGameRunning())
                {
                    SafeAppendText("[INFO] Game not running - injection skipped\n");
                    return;
                }

                SafeAppendText($"Injecting {Path.GetFileName(outputFile)}...\n");

                var args = new List<string> { outputFile, cmbGame.Text };
                var opts = new List<string> { "--inject" };

                if (chkHotLoad.Checked)
                {
                    opts.Add("--hot");
                    opts.Add(cmbHotMode.SelectedIndex == 0 ? "gsc" : "csc");
                }
                if (chkNoRuntime.Checked) opts.Add("--noruntime");

                TextReader originalInput = Console.In;
                TextWriter originalOutput = Console.Out;
                TextWriter originalError = Console.Error;

                try
                {
                    Console.SetIn(new StreamReader(Stream.Null));
                    Console.SetOut(TextWriter.Null);
                    Console.SetError(TextWriter.Null);

                    int result = await Task.Run(() =>
                        compilerRoot.ExecuteCommandLine(args.Concat(opts).ToArray()));

                    if (result == 0)
                    {
                        SafeAppendText("\nINJECTION SUCCESSFUL\n");
                        _lastInjectedScript = Path.GetFileName(outputFile);
                        _lastGameMode = cmbGame.Text;
                        _lastInjectionTime = DateTime.Now;
                        UpdateResetButton(true);
                    }
                    else
                    {
                        SafeAppendText("\nINJECTION FAILED\n");
                    }
                }
                finally
                {
                    Console.SetIn(originalInput);
                    Console.SetOut(originalOutput);
                    Console.SetError(originalError);
                }
            }
            catch (Exception ex)
            {
                SafeAppendText($"[INJECTION ERROR] {ex.Message}\n");
            }
        }

        private async void BtnInject_Click(object sender, EventArgs e)
        {
            if (_isCompiling || _isInjecting)
            {
                await AppendColoredTextAsync("[INFO] Operation already in progress\n", _infoColor);
                return;
            }

            _isInjecting = true;
            UpdateUIState();

            try
            {
                await ClearOutputAsync();

                if (string.IsNullOrWhiteSpace(txtScriptPath.Text))
                {
                    CErrorDialog.Show("Error", "Please select a script file first", true);
                    btnBrowse.Focus();
                    return;
                }

                if (Directory.Exists(txtScriptPath.Text))
                {
                    CErrorDialog.Show("Error", "Cannot inject a folder. Please select a single script file.", true);
                    return;
                }

                if (!IsGameRunning())
                {
                    CErrorDialog.Show("Game Not Running",
                           $"{cmbGame.Text} is not running. Please start the game first.",
                           true);
                    return;
                }

                Cursor = Cursors.WaitCursor;
                await AppendColoredTextAsync($"[{DateTime.Now:HH:mm:ss}] Starting injection...\n", _infoColor);

                var args = new List<string> { txtScriptPath.Text, cmbGame.Text };
                var opts = new List<string> { "--inject" };

                if (chkHotLoad.Checked)
                {
                    opts.Add("--hot");
                    opts.Add(cmbHotMode.SelectedIndex == 0 ? "gsc" : "csc");
                }
                if (chkNoRuntime.Checked) opts.Add("--noruntime");

                int injectionResult = await Task.Run(() => compilerRoot.ExecuteCommandLine(args.Concat(opts).ToArray()));

                if (injectionResult != 0 || txtOutput.Text.Contains("[ERROR] No game process found"))
                {
                    await AppendColoredTextAsync("\nINJECTION FAILED\n", _errorColor);
                    CErrorDialog.Show("Injection Failed", "The injection process failed. Check the output for details.", true);
                    UpdateResetButton(false);
                }
                else
                {
                    await AppendColoredTextAsync("\nINJECTION SUCCESSFUL\n", _successColor);
                    _lastInjectedScript = Path.GetFileName(txtScriptPath.Text);
                    _lastGameMode = cmbGame.Text;
                    _lastInjectionTime = DateTime.Now;
                    UpdateResetButton(true);
                }
            }
            catch (Exception ex)
            {
                await AppendColoredTextAsync($"[ERROR] {ex.Message}\n", _errorColor);
                CErrorDialog.Show("Injection Error", $"An error occurred during injection:\n{ex.Message}", true);
            }
            finally
            {
                _isInjecting = false;
                Cursor = Cursors.Default;
                UpdateUIState();
                UpdateInjectButtonState();
            }
        }

        private async Task ClearOutputAsync()
        {
            if (txtOutput.InvokeRequired)
            {
                await (Task)txtOutput.Invoke(new Func<Task>(ClearOutputAsync));
            }
            else
            {
                lock (_outputLock)
                {
                    txtOutput.Clear();
                }
            }
        }
    }
}