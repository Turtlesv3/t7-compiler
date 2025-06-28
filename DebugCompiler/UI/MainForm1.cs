using DebugCompiler.Properties;
using DebugCompiler.UI.Core.Controls;
using DebugCompiler.UI.Core.Helpers;
using DebugCompiler.UI.Core.Interfaces;
using DebugCompiler.UI.Core.Singletons;
using Microsoft.Test.Xbox.XDRPC;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using T7CompilerLib;
using T89CompilerLib;
using TreyarchCompiler;
using TreyarchCompiler.Enums;
using XDevkit;
using Games = TreyarchCompiler.Enums.Games;

namespace DebugCompiler
{
    public partial class MainForm1 : Form, IThemeableControl
    {
        // Constants
        private const int ProcessCheckInterval = 5000;
        private const int MaxRecentFiles = 10;
        private const string ErrorLogFileName = "compiler_errors.log";
        private const string DefaultTheme = "CatppuccinMocha";

        // Fields
        private Root _compilerRoot;
        private ToolTip _resetToolTip;
        private TextWriter _originalOut;
        private object _outputLock;

        // State tracking
        private DateTime _lastInjectionTime;
        private string _lastInjectedScript;
        private string _lastGameMode = string.Empty;
        private bool _isInternalUpdate;
        private List<string> _currentOptions = new();
        private bool _isCompiling;
        private bool _isInjecting;
        private string _lastDetectedGame = "";
        private bool _lastRunningStatus;
        private System.Timers.Timer _processWatcher;
        private CancellationTokenSource _compilationCts;
        private bool _forceStatusRefresh;
        private bool _handleInitialized = false;
        private bool _isInSafeInvoke = false;

        // Colors
        private readonly Color _errorColor = Color.FromArgb(255, 100, 100);
        private readonly Color _warningColor = Color.FromArgb(255, 203, 107);
        private readonly Color _successColor = Color.FromArgb(100, 255, 100);
        private readonly Color _infoColor = Color.FromArgb(100, 200, 255);

        // DLL imports
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        public MainForm1()
        {
            InitializeComponent();

            // Basic initialization
            _resetToolTip = new ToolTip();
            _outputLock = new object();

            // Safe initialization sequence
            this.HandleCreated += (s, e) =>
            {
                // Theme setup
                UIThemeManager.ThemeChanged += OnThemeChanged;
                ApplyTheme(UIThemeManager.CurrentTheme);

                // Form setup
                InitializeBorderedForm();
                SetFormTitle();

                // Other initialization
                this.Load += MainForm1_Load;
            };
        }

        private void MainForm1_Load(object sender, EventArgs e)
        {
            if (IsDisposed || !IsHandleCreated) return;

            SafeInvoke(() =>
            {
                InitializeApplication();
                InitializeUIComponents();
            });
        }

        #region Initialization

        private void InitializeApplication()
        {
            if (IsDisposed || !IsHandleCreated) return;

            _compilerRoot = new Root();
            InitializeToolTips();

            // Console redirection
            _originalOut = Console.Out;
            Console.SetOut(new ConsoleOutputWriter(this));
            Console.SetError(new ConsoleOutputWriter(this));

            // Theme initialization - now handled in Program.cs before form creation
            UIThemeManager.ThemeChanged += OnThemeChanged;

            // Apply theme immediately
            ApplyTheme(UIThemeManager.CurrentTheme);

            // UI initialization
            InitializeThemeMenu();
            SubscribeToEvents();
            InitializeUIState();
            InitializeCustomComponents();

            // Final UI setup
            BeginInvoke((MethodInvoker)delegate {
                cmbHotMode.DropDownStyle = ComboBoxStyle.DropDownList;
                cmbHotMode.Visible = chkHotLoad.Checked;
                cmbHotMode.Enabled = chkHotLoad.Checked;
            });
        }

        private void InitializeUIComponents()
        {
            if (IsDisposed) return;

            SafeInvoke(() =>
            {
                try
                {
                    // Theme-related components
                    if (MainMenuStrip != null && !MainMenuStrip.IsDisposed)
                    {
                        MainMenuStrip.Renderer = new CustomToolStripRenderer(UIThemeManager.CurrentTheme);
                    }

                    // Initialize ToolTips
                    ConfigureToolTips();

                    // Update all UI states
                    UpdateUIState();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"UI Initialization Error: {ex.Message}");
                    BeginInvoke((MethodInvoker)DelayedRetryInitialization);
                }
            });
        }

        private void InitializeBorderedForm()
        {
            // Configure the bordered form
            InnerForm.SetTitle(this.Text);
            InnerForm.SetExitButtonVisible(true);
            InnerForm.SetDraggable(true);
            InnerForm.Dock = DockStyle.Fill;

            // Ensure proper docking and theming
            this.Controls.Add(InnerForm);
            InnerForm.BringToFront();
        }

        private void SetFormTitle()
        {
            if (InnerForm?.TitleBar != null && !InnerForm.IsDisposed)
            {
                InnerForm.Title = $"T7/T8 Compiler v{GetVersion()} - GUI by G";
            }
        }

        private void InitializeTheme(string savedTheme)
        {
            UIThemeManager.SetTheme(!string.IsNullOrEmpty(savedTheme) ? savedTheme : DefaultTheme);
            ApplyTheme(UIThemeManager.CurrentTheme);
        }

        private void InitializeToolTips()
        {
            _resetToolTip.AutoPopDelay = 5000;
            _resetToolTip.InitialDelay = 500;
            _resetToolTip.ShowAlways = true;
            ConfigureToolTips();
        }

        private void SubscribeToEvents()
        {
            this.KeyDown += MainForm_KeyDown;
            this.Load += OnFormLoad;
            this.HandleCreated += OnHandleCreated;
            txtScriptPath.TextChanged += TxtScriptPath_TextChanged;

            _compilerRoot.OnLogMessage += msg => SafeAppendText(msg + "\n");
            _compilerRoot.OnError += err => SafeAppendText("[ERROR] " + err + "\n");
        }

        private void InitializeUIState()
        {
            this.Text = $"T7/T8 Compiler v{GetVersion()}";
            UpdateCompilerOptions();
            InitializeOutputColors();
            InitializeButtonStates();
        }

        private void OnHandleCreated(object sender, EventArgs e)
        {
            if (_handleInitialized || DesignMode) return;

            try
            {
                _handleInitialized = true;
                base.OnHandleCreated(e);

                // Critical handle-dependent initialization
                Console.SetOut(new ConsoleOutputWriter(this));
                Console.SetError(new ConsoleOutputWriter(this));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Handle creation error: {ex.Message}");
                _handleInitialized = false;
            }
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            base.OnLoad(e);

            // Force complete theme application
            ApplyTheme(UIThemeManager.CurrentTheme);

            UIThemeManager.RegisterControl(this);

            // Initialize components that need the handle
            InitializeCustomComponents();
            InitializeProcessMonitoring();

            // Make sure status label is visible
            if (_lblGameStatus != null)
            {
                _lblGameStatus.Visible = true;
            }

            // Initial checks
            CheckGameProcess();
            CheckRequiredFiles();
        }


        #endregion

        #region UI Management


        private void ConfigureToolTips()
        {
            _resetToolTip.SetToolTip(btnResetParseTree,
                "Reset GSC Parse Tree\n\n" +
                $"Last Injection: {_lastInjectedScript ?? "None"}\n" +
                $"Game: {_lastGameMode ?? "None"}\n" +
                $"Time: {_lastInjectionTime:HH:mm:ss}\n\n" +
                "WARNING: May cause crashes if done while in-game!");

            _resetToolTip.SetToolTip(btnInject,
                "Inject Compiled Script\n\n" +
                "Requirements:\n" +
                "- Valid .gsc/gscc file selected\n" +
                "- Game process running\n" +
                "- Proper compiler setup");

            _resetToolTip.SetToolTip(btnCompile,
                "Compile Script\n\n" +
                "Requirements:\n" +
                "- Select a .gsc file or folder first\n\n" +
                "Compile Options:\n" +
                "- Full Build: Complete Build+Inject\n" +
                "- Compile Only: Skip injection");

            _resetToolTip.SetToolTip(chkBuild,
                "Full Build\n\n" +
                "Performs a complete rebuild of all scripts\n" +
                "Slower but ensures everything is fresh");

            _resetToolTip.SetToolTip(chkCompileOnly,
                "Compile Only\n\n" +
                "Compiles without injecting into the game\n" +
                "Useful for testing compilation only");

            _resetToolTip.SetToolTip(chkHotLoad,
                "Hot Load\n\n" +
                "Enables runtime script reloading\n" +
                "Requires game support for hot loading");

            _resetToolTip.SetToolTip(chkNoRuntime,
                "No Runtime\n\n" +
                "Disables runtime type checking\n" +
                "Faster but less safe compilation");

            _resetToolTip.SetToolTip(cmbHotMode,
                "Hot Load Mode\n\n" +
                "GSC: Standard script hot loading (Server/Shared)\n" +
                "CSC: Client-side script hot loading\n\n" +
                "Only available when Hot Load is enabled");
        }

        private void InitializeProcessMonitoring()
        {
            _processWatcher = new System.Timers.Timer(ProcessCheckInterval);
            _processWatcher.Elapsed += (s, e) =>
            {
                try
                {
                    if (!IsDisposed && IsHandleCreated)
                    {
                        BeginInvoke((MethodInvoker)(() =>
                        {
                            CheckGameProcess();
                            if (_lblGameStatus != null)
                            {
                                _lblGameStatus.Visible = true;
                                _lblGameStatus.BringToFront();
                            }
                        }));
                    }
                }
                catch { /* Handle dispose races */ }
            };
            _processWatcher.Start();

            // Immediate first check
            CheckGameProcess();
        }

        private void InitializeOutputColors()
        {
            txtOutput.ForeColor = Color.FromKnownColor(KnownColor.WindowText);
            txtOutput.BackColor = Color.FromKnownColor(KnownColor.Window);
        }

        private void InitializeButtonStates()
        {
            txtScriptPath.Text = "";
            UpdateUIState();
        }

        #endregion

        #region Theme Management

        public IEnumerable<Control> GetThemedControls()
        {
            // Return all controls that should be themed
            return new Control[]
            {
        InnerForm,
        txtOutput,
        btnResetParseTree,
        btnInject,
        btnCompile,
        chkBuild,
        chkCompileOnly,
        chkHotLoad,
        chkNoRuntime,
        cmbHotMode,
        btnBrowse,
        txtScriptPath,
        btnSavedMenus,
        MainMenuStrip,
        _lblGameStatus
            }.Where(c => c != null);
        }

        public void ApplyTheme(UIThemeInfo theme)
        {
            if (IsDisposed || !IsHandleCreated) return;

            this.SuspendLayout();
            try
            {
                // Apply to main form
                this.BackColor = theme.BackColor;
                this.ForeColor = theme.TextColor;

                // Apply to all child controls
                UIThemeManager.EnsureThemeApplied(InnerForm);

                // Special handling for output textbox
                if (txtOutput != null)
                {
                    txtOutput.BackColor = theme.TextBoxBackColor;
                    txtOutput.ForeColor = theme.TextColor;
                }

                // Force menu refresh
                if (MainMenuStrip != null)
                {
                    MainMenuStrip.Renderer = new CustomToolStripRenderer(theme);
                }

                // Force status label update
                UpdateGameStatus();
            }
            finally
            {
                this.ResumeLayout(true);
                this.Refresh();
            }
        }

        private void ApplyThemeToControlsRecursive(Control parent, UIThemeInfo theme)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is IThemeableControl themeable)
                {
                    themeable.ApplyTheme(theme);
                }
                else
                {
                    UIThemeManager.ApplyDefaultTheme(control);
                }

                if (control.HasChildren)
                {
                    ApplyThemeToControlsRecursive(control, theme);
                }
            }
        }

        private void ApplyThemeToControls(Control.ControlCollection controls, UIThemeInfo theme)
        {
            foreach (Control control in controls)
            {
                if (control is IThemeableControl themeable)
                {
                    themeable.ApplyTheme(theme);
                }
                else
                {
                    UIThemeManager.ApplyDefaultTheme(control);
                }

                if (control.HasChildren)
                {
                    ApplyThemeToControls(control.Controls, theme);
                }
            }
        }

        private void OnThemeChanged(UIThemeInfo theme)
        {
            if (IsDisposed || !IsHandleCreated) return;

            this.BeginInvoke((Action)(() => {
                try
                {
                    // Force refresh all components
                    ApplyTheme(theme);

                    // Explicit title bar update
                    if (InnerForm?.TitleBar != null)
                    {
                        InnerForm.TitleBar.BackColor = theme.AccentColor;
                    }

                    // Rebuild theme menu
                    InitializeThemeMenu();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Theme change error: {ex.Message}");
                }
            }));
        }

        private void RefreshComboBoxStyles()
        {
            static void RefreshCombo(ComboBox comboBox)
            {
                if (comboBox != null && !comboBox.IsDisposed && comboBox.IsHandleCreated)
                {
                    comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                    comboBox.FlatStyle = FlatStyle.Flat;
                    comboBox.FlatStyle = FlatStyle.Standard;
                    comboBox.Refresh();
                }
            }

            RefreshCombo(cmbHotMode);
        }

        public override string Text
        {
            get => base.Text;
            set
            {
                // Only allow setting through InnerForm.Title
                if (InnerForm != null)
                {
                    InnerForm.Title = value;
                }
                else
                {
                    base.Text = value;
                }
            }
        }

        #endregion

        #region Game Process Management

        private (string gameName, bool isRunning) DetectRunningGame()
        {
            try
            {
                if (Process.GetProcessesByName("blackops3").Length > 0)
                    return ("Black Ops 3 (T7)", true);

                if (Process.GetProcessesByName("blackops4").Length > 0)
                    return ("Black Ops 4 (T8)", true);

                if (Process.GetProcessesByName("blackops2").Length > 0)
                    return ("Black Ops 2 (T6)", true);

                return ("", false);
            }
            catch
            {
                return ("", false);
            }
        }

        private void CheckGameProcess()
        {
            if (IsDisposed) return;

            if (InvokeRequired)
            {
                Invoke(new Action(CheckGameProcess));
                return;
            }

            try
            {
                var (currentGame, isRunning) = DetectRunningGame();
                _lastGameMode = currentGame;
                _lastRunningStatus = isRunning;
                _lastDetectedGame = currentGame;

                // Ensure status label is visible and updated
                if (_lblGameStatus == null)
                {
                }

                _lblGameStatus.Visible = true;
                UpdateGameStatus();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Game process check failed: {ex.Message}");
                SafeInvoke(() => {
                    if (_lblGameStatus != null)
                    {
                        _lblGameStatus.Text = "Status check failed";
                        _lblGameStatus.ForeColor = _errorColor;
                        _lblGameStatus.Visible = true;
                    }
                });
            }
        }

        private string GetProcessNameForGame(Games game)
        {
            return game switch
            {
                Games.T6 => "blackops2",
                Games.T7 => "blackops3",
                Games.T8 => "blackops4",
                _ => "blackops3"
            };
        }

        private bool IsGameRunning(Games? game = null)
        {
            if (game != null)
            {
                var processName = GetProcessNameForGame(game.Value);
                if (string.IsNullOrEmpty(processName))
                    return false;

                try
                {
                    return Process.GetProcessesByName(processName).Length > 0;
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                return Process.GetProcessesByName("blackops2").Length > 0 ||
                       Process.GetProcessesByName("blackops3").Length > 0 ||
                       Process.GetProcessesByName("blackops4").Length > 0;
            }
        }

        public void RefreshStatus()
        {
            _forceStatusRefresh = true;
            CheckGameProcess();
        }

        #endregion

        #region Output Management

        private void SafeAppendText(string text, Color? color = null)
        {
            if (string.IsNullOrEmpty(text)) return;

            void Append()
            {
                lock (_outputLock)
                {
                    try
                    {
                        txtOutput.SuspendLayout();
                        int start = txtOutput.TextLength;
                        txtOutput.AppendText(text);
                        int end = txtOutput.TextLength;

                        if (color.HasValue)
                        {
                            txtOutput.Select(start, end - start);
                            txtOutput.SelectionColor = color.Value;
                            txtOutput.SelectionLength = 0;
                        }

                        txtOutput.ScrollToCaret();
                    }
                    finally
                    {
                        txtOutput.ResumeLayout();
                    }
                }
            }

            if (txtOutput.InvokeRequired)
            {
                try
                {
                    txtOutput.BeginInvoke((Action)(() => Append()));
                }
                catch (InvalidOperationException) { /* Handle disposed control */ }
            }
            else
            {
                Append();
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

        #endregion

        #region UI Event Handlers

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.T && !e.Alt && !e.Shift)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                ToggleThemeMenu();
            }
            else if (e.KeyCode == Keys.Escape && MainMenuStrip != null && MainMenuStrip.Visible)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                MainMenuStrip.Visible = false;
            }
        }

        private void ToggleThemeMenu()
        {
            if (MainMenuStrip == null || MainMenuStrip.IsDisposed)
            {
                InitializeThemeMenu();
            }

            // Just toggle visibility
            MainMenuStrip.Visible = !MainMenuStrip.Visible;

            if (MainMenuStrip.Visible)
            {
                PositionThemeMenu(); // Ensure proper position before showing
                MainMenuStrip.BringToFront();
            }
        }

        private void InitializeThemeMenu()
        {
            _themeMenu.DropDownItems.Clear();
            var themes = UIThemeInfo.AvailableThemes
                .GroupBy(t => t.Name)
                .Select(g => g.First())
                .OrderBy(t => t.Name);

            foreach (var theme in themes)
            {
                var item = new ToolStripMenuItem(theme.Name)
                {
                    Tag = theme,
                    Checked = theme.Name.Equals(UIThemeManager.CurrentTheme.Name,
                              StringComparison.OrdinalIgnoreCase)
                };
                item.Click += (s, e) => {
                    foreach (ToolStripMenuItem menuItem in _themeMenu.DropDownItems)
                        menuItem.Checked = false;
                    item.Checked = true;
                    UIThemeManager.SetTheme(theme);
                    MainMenuStrip.Visible = false; // Auto-hide on selection
                };
                _themeMenu.DropDownItems.Add(item);
            }

            // Position menu at fixed bottom left
            PositionThemeMenu();
        }

        private void InitializeThemeSystem()
        {
            // 1. Register all top-level controls
            UIThemeManager.RegisterControlRecursive(this);
            UIThemeManager.RegisterControlRecursive(InnerForm);

            // 2. Special handling for title bar
            if (InnerForm?.TitleBar != null)
            {
                InnerForm.TitleBar.BackColor = UIThemeManager.CurrentTheme.AccentColor;
            }

            // 3. Subscribe to theme changes
            UIThemeManager.ThemeChanged += OnThemeChanged;

            // 4. Force initial theme application
            this.BeginInvoke((Action)(() => {
                ApplyTheme(UIThemeManager.CurrentTheme);
            }));
        }



        private void PositionThemeMenu()
        {
            if (MainMenuStrip != null && !MainMenuStrip.IsDisposed)
            {
                // Fixed position at bottom left
                MainMenuStrip.Location = new Point(10, this.ClientSize.Height - MainMenuStrip.Height - 10);
                MainMenuStrip.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (MainMenuStrip != null && MainMenuStrip.Visible &&
                !MainMenuStrip.Bounds.Contains(e.Location))
            {
                MainMenuStrip.Visible = false;
            }
        }

        #endregion

        #region Button Event Handlers

        private async void BtnCompile_Click(object sender, EventArgs e)
        {
            if (_isCompiling)
            {
                _compilationCts?.Cancel();
                return;
            }

            try
            {
                if (!await ValidateInputs())
                    return;

                _isCompiling = true;
                UpdateUIState();
                ClearOutput();
                _compilationCts = new CancellationTokenSource();

                var args = BuildCompilerArguments();
                await LogCompilerConfiguration(args);

                int result = await Task.Run(() =>
                {
                    try
                    {
                        return _compilerRoot.ExecuteCommandLine(args.ToArray());
                    }
                    catch (OperationCanceledException)
                    {
                        return -2;
                    }
                    catch (Exception ex)
                    {
                        SafeAppendText($"[CRITICAL] Compiler execution failed: {ex.Message}\n", _errorColor);
                        return -1;
                    }
                }, _compilationCts.Token);

                await HandleCompilationResult(result);
            }
            catch (Exception ex)
            {
                DisplayError("Unexpected error during compilation", "Compilation Error", ex);
                LogErrorToFile("Compilation failed", ex);
            }
            finally
            {
                _isCompiling = false;
                UpdateUIState();
                _compilationCts?.Dispose();
                _compilationCts = null;
            }
        }

        private async void BtnInject_Click(object sender, EventArgs e)
        {
            if (_isCompiling || _isInjecting)
            {
                DisplayWarning("Another operation is already in progress");
                return;
            }

            _isInjecting = true;
            UpdateUIState();

            try
            {
                await ClearOutputAsync();

                if (!await ValidateInputs())
                    return;

                if (Directory.Exists(txtScriptPath.Text))
                {
                    DisplayError("Injection requires a single script file, not a folder");
                    return;
                }

                if (!_lastRunningStatus)
                {
                    DisplayError("Game is not running", "Please start the game first");
                    return;
                }

                Cursor = Cursors.WaitCursor;
                DisplayInfo($"Starting injection into {_lastDetectedGame}...");

                var args = new List<string> { txtScriptPath.Text };

                if (_lastDetectedGame.Contains("T7")) args.Add("T7");
                else if (_lastDetectedGame.Contains("T8")) args.Add("T8");
                else if (_lastDetectedGame.Contains("T6")) args.Add("T6");

                var opts = new List<string> { "--inject" };

                if (chkHotLoad.Checked)
                {
                    opts.Add("--hot");
                    opts.Add(cmbHotMode.SelectedIndex == 0 ? "gsc" : "csc");
                }
                if (chkNoRuntime.Checked) opts.Add("--noruntime");

                int injectionResult = await Task.Run(() =>
                {
                    try
                    {
                        return _compilerRoot.ExecuteCommandLine(args.Concat(opts).ToArray());
                    }
                    catch (Exception ex)
                    {
                        SafeAppendText($"[INJECTION ERROR] {ex.Message}\n", _errorColor);
                        return -1;
                    }
                });

                if (injectionResult != 0)
                {
                    string errorDetails = _compilerRoot.GetLastErrorInfo();
                    string fullError = $"Injection failed (code: {injectionResult})";

                    if (!string.IsNullOrEmpty(errorDetails))
                    {
                        fullError += $"\n\nDetails:\n{errorDetails}";
                    }

                    DisplayError(fullError, "Injection Failed");
                }
                else
                {
                    DisplaySuccess("Injection completed successfully");
                    _lastInjectedScript = Path.GetFileName(txtScriptPath.Text);
                    _lastInjectionTime = DateTime.Now;
                    UpdateResetButton(true);
                    AppSettings.AddSuccessfullyProcessedFile(txtScriptPath.Text);
                }
            }
            catch (Exception ex)
            {
                DisplayError("Unexpected error during injection", "Injection Error", ex);
                LogErrorToFile("Injection failed", ex);
            }
            finally
            {
                _isInjecting = false;
                Cursor = Cursors.Default;
                UpdateUIState();
            }
        }

        private async void BtnResetParseTree_Click(object sender, EventArgs e)
        {
            var options = new object[] { "Yes", "No" };
            using var confirmDialog = new CComboDialog(
                "Confirm Reset",
                options,
                1);

            if (confirmDialog.ShowDialog(this) != DialogResult.OK ||
                confirmDialog.SelectedValue?.ToString() != "Yes")
            {
                return;
            }

            await Task.Run(() => ClearOutput());

            try
            {
                btnResetParseTree.Enabled = false;
                Cursor = Cursors.WaitCursor;
                SafeAppendText($"[{DateTime.Now:HH:mm:ss}] Resetting GSC parse tree...\n");

                await Task.Run(() => _compilerRoot.PublicFreeActiveScript());

                SafeAppendText($"[{DateTime.Now:HH:mm:ss}] Reset completed. Reload map for full cleanup.\n");
            }
            catch (Exception ex)
            {
                SafeAppendText($"[ERROR] {ex.Message}\n");

                using var errorDialog = new CComboDialog(
                    "Reset Error",
                    new object[] { ex.Message },
                    0);
                errorDialog.ShowDialog(this);
            }
            finally
            {
                UpdateResetButton(false);
                Cursor = Cursors.Default;
            }
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            var menu = new ContextMenuStrip();

            var fileItem = new ToolStripMenuItem("Select File...");
            fileItem.Click += (s, args) =>
            {
                string filePath = FileDialogHelper.BrowseForGSCFile();
                if (!string.IsNullOrEmpty(filePath))
                {
                    txtScriptPath.Text = filePath;
                    btnResetParseTree.Visible = false;
                }
            };
            menu.Items.Add(fileItem);

            var folderItem = new ToolStripMenuItem("Select Folder...");
            folderItem.Click += (s, args) =>
            {
                string folderPath = FileDialogHelper.BrowseForFolder("Select folder containing GSC scripts");
                if (!string.IsNullOrEmpty(folderPath))
                {
                    txtScriptPath.Text = folderPath;
                    btnResetParseTree.Visible = false;
                }
            };
            menu.Items.Add(folderItem);

            menu.Show(btnBrowse, new Point(0, btnBrowse.Height));
        }

        private void BtnSavedMenus_Click(object sender, EventArgs e)
        {
            try
            {
                var recentFiles = AppSettings.GetSuccessfullyProcessedFiles();

                if (!recentFiles.Any())
                {
                    MessageBox.Show("No recently compiled/injected files found.",
                                   "Information",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Information);
                    return;
                }

                var validRecentFiles = recentFiles
                    .Where(f => File.Exists(f.FilePath))
                    .GroupBy(f => f.FilePath.ToLower())
                    .Select(g => g.OrderByDescending(f => f.Timestamp).First())
                    .OrderByDescending(f => f.Timestamp)
                    .Take(MaxRecentFiles)
                    .ToList();

                if (!validRecentFiles.Any())
                {
                    MessageBox.Show("No valid recent files found (files may have been moved or deleted).",
                                   "Information",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Information);
                    return;
                }

                using var importDialog = new ImportDialog();
                importDialog.LoadFiles(validRecentFiles.Select(f => f.FilePath));

                if (importDialog.ShowDialog(this) == DialogResult.OK &&
                    !string.IsNullOrEmpty(importDialog.SelectedFilePath))
                {
                    txtScriptPath.Text = importDialog.SelectedFilePath;
                    AppSettings.LastScriptDirectory = Path.GetDirectoryName(importDialog.SelectedFilePath);
                }
            }
            catch (Exception ex)
            {
                LogErrorToFile("Error loading saved menus", ex);
                MessageBox.Show($"Could not load recent files: {ex.Message}",
                              "Error",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Checkbox Event Handlers

        private void ChkNoRuntime_CheckedChanged(object sender, EventArgs e)
        {
            if (_isInternalUpdate) return;

            ClearOutput();
            try
            {
                _isInternalUpdate = true;
                if (chkNoRuntime.Checked)
                {
                    SafeAppendText("[CONFIG] Enabled: No Runtime\n");
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

            SafeInvoke(() => {
                cmbHotMode.Visible = chkHotLoad.Checked;
                cmbHotMode.Enabled = chkHotLoad.Checked;

                if (chkHotLoad.Checked && cmbHotMode.Items.Count == 0)
                {
                    // Re-initialize if empty
                    cmbHotMode.Items.AddRange(new[] { "GSC", "CSC" });
                    cmbHotMode.SelectedIndex = 0;
                }

                ClearOutput();
                SafeAppendText($"[CONFIG] Hot Load {(chkHotLoad.Checked ? "Enabled" : "Disabled")}\n");
                UpdateCompilerOptions();
            });
        }

        private void CmbHotMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isInternalUpdate || !chkHotLoad.Checked || !IsHandleCreated)
                return;

            ClearOutput();
            SafeAppendText($"[CONFIG] Hot Load Mode: {(cmbHotMode.SelectedIndex == 0 ? "gsc" : "csc")}\n");
            UpdateCompilerOptions();

            if (IsHandleCreated)
            {
                BeginInvoke((MethodInvoker)delegate {
                    cmbHotMode.SelectionLength = 0;
                    if (cmbHotMode.DropDownStyle == ComboBoxStyle.DropDownList)
                    {
                        cmbHotMode.FlatStyle = FlatStyle.Flat;
                        cmbHotMode.FlatStyle = FlatStyle.Standard;
                    }
                });
            }
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

        #endregion

        #region Helper Methods

        private void SafeInvoke(Action action)
        {
            if (IsDisposed || !IsHandleCreated || _isInSafeInvoke)
                return;

            try
            {
                _isInSafeInvoke = true;

                if (InvokeRequired)
                {
                    BeginInvoke((Action)(() =>
                    {
                        if (!IsDisposed && IsHandleCreated)
                            action();
                    }));
                }
                else
                {
                    action();
                }
            }
            finally
            {
                _isInSafeInvoke = false;
            }
        }

        private string GetVersion()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream("DebugCompiler.version");
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    return reader.ReadToEnd().Trim();
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

            bool isFolder = Directory.Exists(txtScriptPath.Text);
            bool isFile = File.Exists(txtScriptPath.Text) &&
                         (txtScriptPath.Text.EndsWith(".gsc", StringComparison.OrdinalIgnoreCase) ||
                          txtScriptPath.Text.EndsWith(".gscc", StringComparison.OrdinalIgnoreCase));

            btnCompile.Text = _isCompiling ? "Cancel" : "Compile";
            btnCompile.Enabled = !_isCompiling && !_isInjecting &&
                                ((chkCompileOnly.Checked && isFolder) ||
                                 (!chkCompileOnly.Checked && (isFolder || isFile)));

            btnInject.Enabled = !_isCompiling && !_isInjecting && isFile && _lastRunningStatus;
            btnResetParseTree.Enabled = !_isCompiling && !_isInjecting;
            btnBrowse.Enabled = !_isCompiling && !_isInjecting;
            chkBuild.Enabled = !_isCompiling && !_isInjecting;
            chkCompileOnly.Enabled = !_isCompiling && !_isInjecting;
            chkHotLoad.Enabled = !_isCompiling && !_isInjecting;
            chkNoRuntime.Enabled = !_isCompiling && !_isInjecting;
            cmbHotMode.Enabled = !_isCompiling && !_isInjecting && chkHotLoad.Checked;
            cmbHotMode.Visible = chkHotLoad.Checked;

            btnInject.Refresh();
            btnCompile.Refresh();
        }

        private async Task<bool> ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(txtScriptPath.Text))
            {
                await AppendColoredTextAsync("[ERROR] Please select a script file or folder\n", _errorColor);
                UpdateUIState();
                return false;
            }

            bool pathExists = File.Exists(txtScriptPath.Text) || Directory.Exists(txtScriptPath.Text);
            if (!pathExists)
            {
                await AppendColoredTextAsync($"[ERROR] Path does not exist: {txtScriptPath.Text}\n", _errorColor);
                UpdateUIState();
                return false;
            }

            if (File.Exists(txtScriptPath.Text))
            {
                string extension = Path.GetExtension(txtScriptPath.Text).ToLower();
                if (extension != ".gsc" && extension != ".gscc" && extension != ".gsic")
                {
                    await AppendColoredTextAsync(
                        $"[ERROR] Invalid file extension '{extension}'. Must be .gsc, .gscc, or .gsic\n",
                        _errorColor);
                    UpdateUIState();
                    return false;
                }
            }

            return true;
        }

        private List<string> BuildCompilerArguments()
        {
            var args = new List<string> { txtScriptPath.Text };

            if (_lastRunningStatus && !string.IsNullOrEmpty(_lastDetectedGame))
            {
                if (_lastDetectedGame.Contains("T7")) args.Add("T7");
                else if (_lastDetectedGame.Contains("T8")) args.Add("T8");
                else if (_lastDetectedGame.Contains("T6")) args.Add("T6");
            }

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

            // Fixed string interpolation with proper parentheses
            string targetType = File.Exists(txtScriptPath.Text) ? "File" : "Folder";
            await AppendColoredTextAsync($"- Target: {targetType} {txtScriptPath.Text}\n", _infoColor);

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

                    var errorInfo = _compilerRoot.GetLastErrorInfo();
                    if (!string.IsNullOrEmpty(errorInfo))
                        await AppendColoredTextAsync($"[COMPILER ERROR] {errorInfo}\n", _errorColor);
                    break;
            }
        }

        private async Task HandleSuccessfulCompilation()
        {
            bool isFileCompilation = File.Exists(txtScriptPath.Text) &&
                                     (txtScriptPath.Text.EndsWith(".gsc", StringComparison.OrdinalIgnoreCase) ||
                                      txtScriptPath.Text.EndsWith(".gscc", StringComparison.OrdinalIgnoreCase));

            string compilerRootDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string expectedOutputPath = Path.Combine(compilerRootDir, "compiled.gscc");

            await AppendColoredTextAsync($"[INFO] Output will be at: {expectedOutputPath}\n", _infoColor);

            if (File.Exists(expectedOutputPath))
            {
                await AppendColoredTextAsync($"[SUCCESS] Compiled output created: {expectedOutputPath}\n", _successColor);
                await AppendColoredTextAsync($"- Size: {new FileInfo(expectedOutputPath).Length} bytes\n", _infoColor);
                await AppendColoredTextAsync($"- Modified: {File.GetLastWriteTime(expectedOutputPath):yyyy-MM-dd HH:mm:ss}\n", _infoColor);

                AppSettings.AddSuccessfullyProcessedFile(isFileCompilation ? txtScriptPath.Text : expectedOutputPath);

                if (!chkCompileOnly.Checked && !chkBuild.Checked)
                {
                    await Task.Delay(300);
                    await InjectCompiledScript(expectedOutputPath);
                }
            }
            else
            {
                await AppendColoredTextAsync("[ERROR] Compiled output was not created at expected location\n", _errorColor);
            }
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

        private void UpdateInjectButtonState()
        {
            if (IsDisposed || Disposing || btnInject == null || txtScriptPath == null)
                return;

            var (runningGameName, isRunning) = DetectRunningGame();

            bool shouldEnable = !_isCompiling &&
                               !_isInjecting &&
                               isRunning &&
                               !string.IsNullOrEmpty(txtScriptPath.Text) &&
                               File.Exists(txtScriptPath.Text);

            void UpdateButtonState()
            {
                try
                {
                    if (btnInject.Enabled != shouldEnable)
                    {
                        btnInject.Enabled = shouldEnable;
                        btnInject.Invalidate();
                        btnInject.Update();

                        if (!shouldEnable && btnInject.Enabled)
                        {
                            string reason = "";
                            if (_isCompiling) reason += "• Compilation in progress\n";
                            if (_isInjecting) reason += "• Injection in progress\n";
                            if (!isRunning) reason += "• Game is not running\n";
                            if (string.IsNullOrEmpty(txtScriptPath.Text)) reason += "• No script selected\n";
                            else if (!File.Exists(txtScriptPath.Text)) reason += "• Selected file doesn't exist\n";

                            if (!string.IsNullOrEmpty(reason))
                            {
                                CErrorDialog.Show("Injection Unavailable",
                                                "Cannot inject because:\n\n" + reason,
                                                true);
                            }
                        }
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Handle disposed controls gracefully
                }
            }

            if (btnInject.InvokeRequired)
            {
                try
                {
                    btnInject.BeginInvoke((Action)UpdateButtonState);
                }
                catch (InvalidOperationException)
                {
                    // Handle cases where control isn't ready for invocation
                }
            }
            else
            {
                UpdateButtonState();
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
                    _resetToolTip.SetToolTip(btnResetParseTree,
                        $"Last Injected: {_lastInjectedScript ?? "None"}\n" +
                        $"Game: {_lastGameMode ?? "None"}\n" +
                        $"Time: {_lastInjectionTime:HH:mm:ss}");
                }
            }
        }

        private async Task InjectCompiledScript(string outputFile)
        {
            try
            {
                if (!IsGameRunning())
                {
                    await AppendColoredTextAsync("[INFO] Game not running - injection skipped\n", _infoColor);
                    return;
                }

                Games runningGame = Games.None;
                if (Process.GetProcessesByName("blackops3").Length > 0)
                    runningGame = Games.T7;
                else if (Process.GetProcessesByName("blackops4").Length > 0)
                    runningGame = Games.T8;
                else if (Process.GetProcessesByName("blackops2").Length > 0)
                    runningGame = Games.T6;

                await AppendColoredTextAsync($"[{DateTime.Now:HH:mm:ss}] Injecting into {runningGame}...\n", _infoColor);

                var args = new List<string> { outputFile };
                if (runningGame != Games.None)
                {
                    args.Add(runningGame.ToString());
                }

                var opts = new List<string> { "--inject" };

                if (chkHotLoad.Checked && cmbHotMode.SelectedItem != null)
                {
                    opts.Add("--hot");
                    opts.Add(cmbHotMode.SelectedIndex == 0 ? "gsc" : "csc");
                    await AppendColoredTextAsync($"[CONFIG] Hotload enabled ({cmbHotMode.SelectedItem})\n", _infoColor);
                }

                if (chkNoRuntime.Checked)
                {
                    opts.Add("--noruntime");
                    await AppendColoredTextAsync("[CONFIG] Runtime checks disabled\n", _infoColor);
                }

                int result = await Task.Run(() => _compilerRoot.ExecuteCommandLine(args.Concat(opts).ToArray()));

                if (result == 0)
                {
                    await AppendColoredTextAsync("[SUCCESS] Injection completed\n", _successColor);
                }
                else
                {
                    await AppendColoredTextAsync($"[ERROR] Injection failed with code {result}\n", _errorColor);
                }
            }
            catch (Exception ex)
            {
                await AppendColoredTextAsync($"[INJECTION ERROR] {ex.Message}\n", _errorColor);
                if (ex is UnauthorizedAccessException)
                {
                    await AppendColoredTextAsync("Tip: Run the compiler as administrator\n", _warningColor);
                }
            }
        }

        #endregion

        #region Error Handling

        private void DisplayError(string message, string title = "Error", Exception ex = null)
        {
            SafeInvoke(() =>
            {
                SafeAppendText($"[ERROR] {message}\n", _errorColor);

                if (ex != null)
                {
                    string details = $"{ex.Message}\n\nStack Trace:\n{ex.StackTrace}";

                    if (ex is not OperationCanceledException)
                    {
                        var errorForm = new ErrorDetailsForm(
                            title,
                            message,
                            details);

                        errorForm.ShowDialog(this);
                    }
                    else
                    {
                        SafeAppendText($"[DETAILS] {details}\n", Color.DarkGray);
                    }
                }
            });
        }

        private void LogErrorToFile(string message, Exception ex = null)
        {
            try
            {
                string logPath = Path.Combine(Application.StartupPath, ErrorLogFileName);
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n";

                if (ex != null)
                {
                    logMessage += $"Exception: {ex.GetType().Name}\n";
                    logMessage += $"Message: {ex.Message}\n";
                    logMessage += $"Stack Trace:\n{ex.StackTrace}\n\n";
                }

                File.AppendAllText(logPath, logMessage);
            }
            catch { /* Don't fail if logging fails */ }
        }

        private void DisplayWarning(string message)
        {
            SafeInvoke(() =>
            {
                SafeAppendText($"[WARNING] {message}\n", _warningColor);
            });
        }

        private void DisplayInfo(string message)
        {
            SafeInvoke(() =>
            {
                SafeAppendText($"[INFO] {message}\n", _infoColor);
            });
        }

        private void DisplaySuccess(string message)
        {
            SafeInvoke(() =>
            {
                SafeAppendText($"[SUCCESS] {message}\n", _successColor);
            });
        }

        #endregion

        #region Console Output Redirection

        private class ConsoleOutputWriter : TextWriter
        {
            private readonly MainForm1 _form;
            private readonly StringBuilder _buffer = new();

            public ConsoleOutputWriter(MainForm1 form)
            {
                _form = form;
            }

            public override Encoding Encoding => Encoding.UTF8;

            public override void Write(char value)
            {
                _buffer.Append(value);
                if (value == '\n')
                {
                    _form.SafeAppendText(_buffer.ToString());
                    _buffer.Clear();
                }
            }

            public override void Write(string value)
            {
                _form.SafeAppendText(value);
            }
        }

        #endregion

        #region Nested Classes

        internal static class NativeMethods
        {
            [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
            public static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

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
                    // Fallback if API not available
                }
            }
        }

        public class ErrorDetailsForm : Form
        {
            public ErrorDetailsForm(string title, string message, string details)
            {
                this.Text = title;
                this.StartPosition = FormStartPosition.CenterParent;
                this.Size = new Size(600, 400);
                this.MinimizeBox = false;
                this.MaximizeBox = false;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;

                var mainPanel = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 3,
                    Padding = new Padding(10)
                };

                var lblMessage = new Label
                {
                    Text = message,
                    Dock = DockStyle.Fill,
                    TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                    Font = new Font(this.Font, FontStyle.Bold)
                };
                mainPanel.Controls.Add(lblMessage, 0, 0);

                var txtDetails = new RichTextBox
                {
                    Text = details,
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    BackColor = SystemColors.Window,
                    BorderStyle = BorderStyle.FixedSingle,
                    ScrollBars = RichTextBoxScrollBars.Both
                };
                mainPanel.Controls.Add(txtDetails, 0, 1);

                var btnPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.RightToLeft
                };

                var btnCopy = new Button { Text = "Copy Details", Width = 100 };
                btnCopy.Click += (s, e) => Clipboard.SetText(details);

                var btnClose = new Button { Text = "Close", Width = 100 };
                btnClose.Click += (s, e) => this.Close();

                btnPanel.Controls.Add(btnClose);
                btnPanel.Controls.Add(btnCopy);
                mainPanel.Controls.Add(btnPanel, 0, 2);

                this.Controls.Add(mainPanel);
            }
        }

        #endregion

        private void toolTip1_Popup(object sender, PopupEventArgs e)
        {

        }

        private void MainMenuStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }
    }
}