using DebugCompiler.UI.Core.Controls;
using DebugCompiler.UI.Core.Interfaces;
using DebugCompiler.UI.Core.Singletons;
using DebugCompiler.UI.Core.Helpers;
using Microsoft.Test.Xbox.XDRPC;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Windows.Forms.VisualStyles;
using T7CompilerLib;
using T7CompilerLib.OpCodes;
using T89CompilerLib;
using TreyarchCompiler;
using TreyarchCompiler.Enums;
using XDevkit;
using Games = TreyarchCompiler.Enums.Games;
using DebugCompiler.Properties;

namespace DebugCompiler
{
    public partial class MainForm1 : Form, IThemeableControl
    {
        private readonly Root compilerRoot;
        private readonly ToolTip resetToolTip = new()
        {
            AutoPopDelay = 5000,
            InitialDelay = 500,
            ReshowDelay = 500,
            ShowAlways = true
        };

        // Add to your class fields
        private Label _lblGameStatus;
        private System.Timers.Timer _processWatcher;
        private Games _currentGame;
        private ToolTip toolTip1;
        private ToolStripMenuItem _themeMenu;
        private new MenuStrip MainMenuStrip;

        // State tracking fields
        private DateTime _lastInjectionTime;
        private string _lastInjectedScript;
        private string _lastGameMode;
        private readonly TextWriter _originalOut = Console.Out;
        private readonly object _outputLock = new();
        private bool _isInternalUpdate = false;
        private List<string> _currentOptions = new();

        // New fields for enhanced functionality
        private CancellationTokenSource _compilationCts;
        private readonly Color _errorColor = Color.FromArgb(255, 100, 100);
        private readonly Color _warningColor = Color.FromArgb(255, 203, 107);
        private readonly Color _successColor = Color.FromArgb(100, 255, 100);
        private readonly Color _infoColor = Color.FromArgb(100, 200, 255);
        private volatile bool _isCompiling = false;
        private volatile bool _isInjecting = false;
        private bool _forceStatusRefresh = false;
        private Games _lastRunningGame = Games.None;
        private bool _lastRunningStatus = false;

        // DLL imports
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

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

                // Message label - using fully qualified enum
                var lblMessage = new Label
                {
                    Text = message,
                    Dock = DockStyle.Fill,
                    TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                    Font = new Font(this.Font, FontStyle.Bold)
                };
                mainPanel.Controls.Add(lblMessage, 0, 0);

                // Details textbox
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

                // Buttons panel
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

        public MainForm1()
        {
            // Phase 1: Basic Initialization
            InitializeComponent();

            // Load saved theme before creating controls
            string savedTheme = UIThemeManager.LoadTheme();
            if (!string.IsNullOrEmpty(savedTheme))
            {
                UIThemeManager.SetTheme(savedTheme);
            }

            // Non-UI components
            compilerRoot = new Root();
            toolTip1 = new ToolTip();
            InnerForm.Dock = DockStyle.Fill;
            InnerForm.SetDraggable(true);
            InnerForm.SetExitButtonVisible(true);

            // Redirect console output
            Console.SetOut(new ConsoleOutputWriter(this));
            Console.SetError(new ConsoleOutputWriter(this));

            // Phase 2: Theme System Setup
            UIThemeManager.ThemeChanged += OnThemeChanged;
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;

            // Phase 3: Handle-Created Initialization
            this.HandleCreated += (s, e) =>
            {
                SafeInvoke(() =>
                {
                    InitializeThemeMenu();
                    InitializeGameComboBox();
                    InitializeCustomComponents();

                    // Apply theme after all controls are created
                    ApplyTheme(UIThemeManager.CurrentTheme);

                    // ComboBox Styling
                    cmbHotMode.DropDownStyle = ComboBoxStyle.DropDownList;
                    cmbGame.DropDownStyle = ComboBoxStyle.DropDownList;
                    cmbGame.FlatStyle = FlatStyle.Flat;
                    cmbGame.FlatStyle = FlatStyle.Standard;
                    cmbHotMode.Visible = chkHotLoad.Checked;
                    cmbHotMode.Enabled = chkHotLoad.Checked;

                    // Initial Game Detection
                    var (initialGame, _) = DetectRunningGame();
                    if (initialGame != Games.None)
                    {
                        cmbGame.SelectedItem = cmbGame.Items.Cast<KeyValuePair<Games, string>>()
                            .FirstOrDefault(item => item.Key == initialGame);
                    }

                    // Additional UI Initialization
                    InitializeStatusLabel();
                    InitializeProcessMonitoring();
                    InitializeOutputColors();
                    InitializeButtonStates();
                });
            };

            // Phase 4: Event Subscriptions
            this.Load += (s, e) => SafeInvoke(() =>
            {
                UpdateGameStatus();
                CheckRequiredFiles();
            });

            // Phase 5: Final Configuration
            this.Text = $"T7/T8 Compiler v{GetVersion()} - by Serious -GUI by DoubleG ;)";
            UpdateCompilerOptions();

            // Safe event subscriptions
            SafeInvoke(() =>
            {
                txtScriptPath.TextChanged += TxtScriptPath_TextChanged;
                compilerRoot.OnLogMessage += (msg) => SafeAppendText(msg + "\n");
                compilerRoot.OnError += (err) => SafeAppendText("[ERROR] " + err + "\n");
            });
        }

        // Helper method for safe invocation
        private void SafeInvoke(Action action)
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() =>
                    {
                        if (!IsDisposed && IsHandleCreated)
                        {
                            action();
                        }
                    }));
                }
                else
                {
                    action();
                }
            }
            catch (InvalidOperationException)
            {
                // Handle cases where control is being disposed
            }
        }

        private void OnThemeChanged(UIThemeInfo theme)
        {
            SafeInvoke(() =>
            {
                if (IsDisposed || !IsHandleCreated) return;

                ApplyTheme(theme);
                RefreshComboBoxStyles();
                UpdateGameStatus();

                if (MainMenuStrip != null && !MainMenuStrip.IsDisposed)
                {
                    MainMenuStrip.Visible = false;
                }
            });
        }

        private void InitializeThemeMenu()
        {
            // Clear existing menu safely
            if (MainMenuStrip != null)
            {
                MainMenuStrip.Visible = false;
                Controls.Remove(MainMenuStrip);
                MainMenuStrip.Dispose();
            }

            // Create new menu strip with proper theming (initially hidden)
            MainMenuStrip = new MenuStrip
            {
                Visible = false,  // Hidden on startup
                Renderer = new CustomToolStripRenderer(UIThemeManager.CurrentTheme)
            };

            // Initialize theme menu item
            _themeMenu = new ToolStripMenuItem("Themes");

            // Get distinct themes
            var savedTheme = UIThemeManager.LoadTheme();
            var themes = UIThemeInfo.AvailableThemes
                .GroupBy(t => t.Name)
                .Select(g => g.First())
                .OrderBy(t => t.Name);

            // Add theme items with proper theming
            foreach (var theme in themes)
            {
                var item = new ToolStripMenuItem(theme.Name)
                {
                    Tag = theme,
                    Checked = theme.Name.Equals(savedTheme, StringComparison.OrdinalIgnoreCase),
                    ForeColor = UIThemeManager.CurrentTheme.MenuTextColor
                };

                item.Click += (s, e) =>
                {
                    // Uncheck all items
                    foreach (ToolStripMenuItem menuItem in _themeMenu.DropDownItems)
                    {
                        menuItem.Checked = false;
                    }

                    // Check selected and apply theme
                    item.Checked = true;
                    UIThemeManager.SetTheme(theme);

                    // Update menu renderer with new theme
                    MainMenuStrip.Renderer = new CustomToolStripRenderer(theme);
                };

                _themeMenu.DropDownItems.Add(item);
            }

            // Add drag functionality
            MainMenuStrip.MouseDown += MenuStrip_MouseDown;
            MainMenuStrip.MouseUp += MenuStrip_MouseUp;

            // Add menu to form (still hidden)
            MainMenuStrip.Items.Add(_themeMenu);
            Controls.Add(MainMenuStrip);
            MainMenuStrip.BringToFront();

            // Ensure proper menu theming
            UIThemeManager.ThemeChanged += theme =>
            {
                MainMenuStrip.Renderer = new CustomToolStripRenderer(theme);
            };
        }

        private void MenuStrip_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                MainMenuStrip.Cursor = Cursors.SizeAll;
                ReleaseCapture();
                SendMessage(Handle, 0xA1, 0x2, 0);
            }
        }

        private void MenuStrip_MouseUp(object sender, MouseEventArgs e)
        {
            MainMenuStrip.Cursor = Cursors.Default;
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.T && !e.Alt && !e.Shift)
            {
                e.SuppressKeyPress = true;
                ToggleThemeMenu();
            }
        }

        private void ToggleThemeMenu()
        {
            if (MainMenuStrip == null || MainMenuStrip.IsDisposed)
                return;

            MainMenuStrip.Visible = !MainMenuStrip.Visible;

            if (MainMenuStrip.Visible)
            {
                MainMenuStrip.Location = new Point(
                    ClientSize.Width - MainMenuStrip.Width - 10,
                    10);
                MainMenuStrip.BringToFront();
            }
        }

        public void ShowThemeMenu(bool show)
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;

            SafeInvoke(() =>
            {
                if (MainMenuStrip != null && !MainMenuStrip.IsDisposed)
                {
                    MainMenuStrip.Visible = show;
                    if (show)
                    {
                        MainMenuStrip.Location = new Point(
                            this.ClientSize.Width - MainMenuStrip.Width - 10,
                            10);
                        MainMenuStrip.BringToFront();
                    }
                }
            });
        }

        public void ApplyTheme(UIThemeInfo theme)
        {
            if (IsDisposed || !IsHandleCreated) return;

            SafeInvoke(() =>
            {
                try
                {
                    this.SuspendLayout();

                    // Apply to main form
                    this.BackColor = theme.BackColor;
                    this.ForeColor = theme.TextColor;

                    // Apply to custom border form if exists
                    if (this.InnerForm != null && !this.InnerForm.IsDisposed)
                    {
                        this.InnerForm.BackColor = theme.AccentColor;
                        this.InnerForm.ForeColor = theme.TextColor;
                    }

                    // Apply to all child controls
                    ApplyThemeToControls(this.Controls, theme);

                    // Special handling for status label
                    if (_lblGameStatus != null && !_lblGameStatus.IsDisposed)
                    {
                        _lblGameStatus.BackColor = theme.IsDarkTheme
                            ? Color.FromArgb(40, 40, 40)
                            : Color.FromArgb(240, 240, 240);
                        _lblGameStatus.ForeColor = theme.TextColor;
                    }

                    // Force complete refresh
                    this.Invalidate(true);
                }
                finally
                {
                    this.ResumeLayout(true);
                }
            });
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
                    // Maintain DropDownList style
                    if (cmb == cmbHotMode || cmb == cmbGame)
                    {
                        cmb.DropDownStyle = ComboBoxStyle.DropDownList;
                    }
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

        // Optional helper method for theme preview icons
        private Image CreateThemePreviewImage(UIThemeInfo theme)
        {
            var bmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bmp))
            {
                g.FillRectangle(new SolidBrush(theme.BackColor), 0, 0, 8, 16);
                g.FillRectangle(new SolidBrush(theme.AccentColor), 8, 0, 8, 16);
            }
            return bmp;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Force initial theme application
            ApplyTheme(UIThemeManager.CurrentTheme);

            // Force status update after everything is loaded
            BeginInvoke((MethodInvoker)delegate {
                UpdateGameStatus();
                Refresh();
            });
        }

        private void InitializeOutputColors()
        {
            txtOutput.ForeColor = Color.FromKnownColor(KnownColor.WindowText);
            txtOutput.BackColor = Color.FromKnownColor(KnownColor.Window);
        }

        private void InitializeProcessMonitoring()
        {
            _processWatcher = new System.Timers.Timer(2000);
            _processWatcher.Elapsed += (s, e) => {
                try
                {
                    if (!IsDisposed && IsHandleCreated)
                    {
                        BeginInvoke((MethodInvoker)UpdateGameStatus);
                    }
                }
                catch { /* Handle dispose races */ }
            };
            _processWatcher.Start();
        }

        private void InitializeStatusLabel()
        {
            _lblGameStatus = new Label
            {
                Dock = DockStyle.Bottom,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Height = 25,
                BackColor = Color.FromArgb(40, 40, 40), // Dark default
                ForeColor = Color.Gray, // Default state
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Margin = new Padding(0),
                Padding = new Padding(0),
                UseCompatibleTextRendering = true
            };

            Controls.Add(_lblGameStatus);
            _lblGameStatus.BringToFront();
        }

        private (Games runningGame, bool isRunning) DetectRunningGame()
        {
            try
            {
                foreach (Games game in Enum.GetValues(typeof(Games)))
                {
                    if (game == Games.None) continue;

                    try
                    {
                        var processName = GetProcessNameForGame(game);
                        if (Process.GetProcessesByName(processName).Length > 0)
                        {
                            return (game, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        DisplayWarning($"Failed to check process for {game}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                DisplayError("Game detection failed", "Detection Error", ex);
            }

            return (Games.None, false);
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

            RefreshCombo(cmbGame);
            RefreshCombo(cmbHotMode);
        }

        private void CheckGameProcess()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(CheckGameProcess));
                return;
            }

            try
            {
                var (currentRunningGame, isRunning) = DetectRunningGame();

                // Only update if status changed
                if (isRunning != _lastRunningStatus ||
                    currentRunningGame != _lastRunningGame)
                {
                    _lastRunningStatus = isRunning;
                    _lastRunningGame = currentRunningGame;
                    UpdateGameStatus(); // This will now update the combobox selection
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Game process check failed: {ex.Message}");
            }
        }

        private void UpdateGameStatus()
        {
            if (_lblGameStatus == null || cmbGame == null) return;

            var (runningGame, isRunning) = DetectRunningGame();

            if (isRunning)
            {
                _lblGameStatus.Text = "✓ Running";
                _lblGameStatus.ForeColor = Color.FromArgb(100, 255, 100); // Bright green

                // Find and select the running game in the combobox
                foreach (var item in cmbGame.Items)
                {
                    if (item is KeyValuePair<Games, string> pair && pair.Key == runningGame)
                    {
                        if (!Equals(cmbGame.SelectedItem, item))
                        {
                            cmbGame.SelectedItem = item;
                        }
                        break;
                    }
                }
            }
            else
            {
                _lblGameStatus.Text = "✗ Not Running";
                _lblGameStatus.ForeColor = Color.FromArgb(255, 100, 100); // Bright red
            }

            UpdateInjectButtonState();
        }

        private string GetProcessNameForGame(Games game)
        {
            return game switch
            {
                Games.T6 => "blackops2",
                Games.T7 => "blackops3",
                Games.T8 => "blackops4",
                _ => "blackops3" // Default fallback
            };
        }

        public void RefreshStatus()
        {
            _forceStatusRefresh = true;
            CheckGameProcess();
        }

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

                        // Save current position
                        int start = txtOutput.TextLength;
                        txtOutput.AppendText(text);
                        int end = txtOutput.TextLength;

                        // Apply color if specified
                        if (color.HasValue)
                        {
                            txtOutput.Select(start, end - start);
                            txtOutput.SelectionColor = color.Value;
                            txtOutput.SelectionLength = 0;
                        }

                        // Auto-scroll
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

            if (_lblGameStatus != null)
            {
                _lblGameStatus.BackColor = Color.FromArgb(30, 30, 30); // Keep dark background
                UpdateGameStatus(); // Re-apply status colors
            }

            UIThemeManager.ThemeChanged += theme => {
                ApplyTheme(theme);
                UpdateGameStatus();
            };

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

            cmbHotMode.Items.AddRange(new[] { "GSC", "CSC" });
            cmbHotMode.SelectedIndex = 0;
            cmbHotMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbHotMode.Visible = false;

            // Force style update
            cmbHotMode.FlatStyle = FlatStyle.Standard;
            cmbHotMode.Refresh();

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
                "GSC: Standard script hot loading (Server/Shared)\n" +
                "CSC: Client-side script hot loading\n\n" +
                "Only available when Hot Load is enabled");
        }

        private void InitializeGameComboBox()
        {
            var gameDisplayNames = new Dictionary<Games, string>
            {
                {Games.T6, "Black Ops 2 (T6)"},
                {Games.T7, "Black Ops 3 (T7)"},
                {Games.T8, "Black Ops 4 (T8)"}
            };

            cmbGame.DisplayMember = "Value";
            cmbGame.ValueMember = "Key";
            cmbGame.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbGame.DataSource = Enum.GetValues(typeof(Games))
                .Cast<Games>()
                .Where(g => g != Games.None)
                .Select(g => new KeyValuePair<Games, string>(g, gameDisplayNames[g]))
                .ToList();

            // Modified event handler to prevent recursive updates
            cmbGame.SelectedIndexChanged -= CmbGame_SelectedIndexChanged;
            cmbGame.SelectedIndexChanged += CmbGame_SelectedIndexChanged;

            UIThemeManager.RegisterSpecialComboBox(cmbGame);
            UIThemeManager.RegisterSpecialComboBox(cmbHotMode);
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

            // Update compile button state - disabled by default until valid input
            btnCompile.Text = _isCompiling ? "Cancel" : "Compile";
            btnCompile.Enabled = !_isCompiling && !_isInjecting &&
                                ((chkCompileOnly.Checked && isFolder) ||
                                 (!chkCompileOnly.Checked && (isFolder || isFile)));

            // Update other controls
            btnResetParseTree.Enabled = !_isCompiling && !_isInjecting;
            btnBrowse.Enabled = !_isCompiling && !_isInjecting;
            btnInject.Enabled = !_isCompiling && !_isInjecting && isFile;

            chkBuild.Enabled = !_isCompiling && !_isInjecting;
            chkCompileOnly.Enabled = !_isCompiling && !_isInjecting;
            chkHotLoad.Enabled = !_isCompiling && !_isInjecting;
            chkNoRuntime.Enabled = !_isCompiling && !_isInjecting;
            cmbHotMode.Enabled = !_isCompiling && !_isInjecting && chkHotLoad.Checked;
            cmbHotMode.Visible = chkHotLoad.Checked;
            cmbGame.Enabled = !_isCompiling && !_isInjecting;

            // Force UI refresh
            btnInject.Refresh();
            btnCompile.Refresh();
        }

        private void InitializeButtonStates()
        {
            txtScriptPath.Text = ""; // Clear any default text
            UpdateUIState(); // Set initial button states
        }

        private async Task<bool> ValidateInputs()
        {
            // Check if script path is empty
            if (string.IsNullOrWhiteSpace(txtScriptPath.Text))
            {
                await AppendColoredTextAsync("[ERROR] Please select a script file or folder\n", _errorColor);
                UpdateUIState();
                return false;
            }

            // Check if path exists
            bool pathExists = File.Exists(txtScriptPath.Text) || Directory.Exists(txtScriptPath.Text);
            if (!pathExists)
            {
                await AppendColoredTextAsync($"[ERROR] Path does not exist: {txtScriptPath.Text}\n", _errorColor);
                UpdateUIState();
                return false;
            }

            // Validate file extension if it's a file (not folder)
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

            // Check if game is selected
            if (cmbGame.SelectedIndex < 0)
            {
                await AppendColoredTextAsync("[ERROR] Please select a target game\n", _errorColor);
                UpdateUIState();
                return false;
            }

            // For injection, check if game process is running
            if (!chkCompileOnly.Checked && !IsGameRunning())
            {
                var selectedGame = ((KeyValuePair<Games, string>)cmbGame.SelectedItem).Key;
                string processName = GetProcessNameForGame(selectedGame);

                await AppendColoredTextAsync(
                    $"[ERROR] {cmbGame.Text} is not running (process: {processName}.exe)\n" +
                    "Please start the game first\n",
                    _errorColor);
                UpdateUIState();
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
                        return compilerRoot.ExecuteCommandLine(args.ToArray());
                    }
                    catch (OperationCanceledException)
                    {
                        return -2; // Special code for cancellation
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

                // Consolidated validation using the shared method
                if (!await ValidateInputs())
                    return;

                // Additional injection-specific validation
                if (Directory.Exists(txtScriptPath.Text))
                {
                    DisplayError("Injection requires a single script file, not a folder");
                    return;
                }

                var selectedGame = ((KeyValuePair<Games, string>)cmbGame.SelectedItem).Key;
                string processName = GetProcessNameForGame(selectedGame);

                if (!IsGameRunning(selectedGame))
                {
                    DisplayError($"{cmbGame.Text} is not running",
                                $"Please start {cmbGame.Text} (process: {processName}.exe)");
                    return;
                }

                Cursor = Cursors.WaitCursor;
                DisplayInfo($"Starting injection into {processName}...");

                var args = new List<string> { txtScriptPath.Text, selectedGame.ToString() };
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
                        return compilerRoot.ExecuteCommandLine(args.Concat(opts).ToArray());
                    }
                    catch (Exception ex)
                    {
                        SafeAppendText($"[INJECTION ERROR] {ex.Message}\n", _errorColor);
                        return -1;
                    }
                });

                if (injectionResult != 0)
                {
                    string errorDetails = compilerRoot.GetLastErrorInfo();
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

                    // Update injection history
                    _lastInjectedScript = Path.GetFileName(txtScriptPath.Text);
                    _lastGameMode = cmbGame.Text;
                    _lastInjectionTime = DateTime.Now;
                    UpdateResetButton(true);

                    // Add to recent files (with duplicate prevention)
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

                // Filter duplicates and verify files still exist
                var validRecentFiles = recentFiles
                    .Where(f => File.Exists(f.FilePath))  // Only files that still exist
                    .GroupBy(f => f.FilePath.ToLower())    // Case-insensitive grouping
                    .Select(g => g.OrderByDescending(f => f.Timestamp).First())
                    .OrderByDescending(f => f.Timestamp)
                    .Take(10)  // Limit to 10 most recent
                    .ToList();

                if (!validRecentFiles.Any())
                {
                    MessageBox.Show("No valid recent files found (files may have been moved or deleted).",
                                   "Information",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Information);
                    return;
                }

                using (var importDialog = new ImportDialog())
                {
                    // Just load the file paths - display formatting will need to be handled in ImportDialog
                    importDialog.LoadFiles(validRecentFiles.Select(f => f.FilePath));

                    if (importDialog.ShowDialog(this) == DialogResult.OK &&
                        !string.IsNullOrEmpty(importDialog.SelectedFilePath))
                    {
                        txtScriptPath.Text = importDialog.SelectedFilePath;
                        AppSettings.LastScriptDirectory = Path.GetDirectoryName(importDialog.SelectedFilePath);
                    }
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

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            var menu = new ContextMenuStrip();

            // File selection menu item
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

            // Folder selection menu item
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

        private async void BtnResetParseTree_Click(object sender, EventArgs e)
        {
            // Create confirmation options
            var options = new object[] { "Yes", "No" };

            // Use CComboDialog for confirmation
            using var confirmDialog = new CComboDialog(
                "Confirm Reset",
                options,
                1); // Default to "No"

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

                await Task.Run(() => compilerRoot.PublicFreeActiveScript());

                SafeAppendText($"[{DateTime.Now:HH:mm:ss}] Reset completed. Reload map for full cleanup.\n");
            }
            catch (Exception ex)
            {
                SafeAppendText($"[ERROR] {ex.Message}\n");

                // Use CComboDialog for error display (or could use ImportDialog if preferred)
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

                    // Provide additional error info from compiler
                    var errorInfo = compilerRoot.GetLastErrorInfo();
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

            // Set fixed output path in compiler root directory
            string compilerRootDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string expectedOutputPath = Path.Combine(compilerRootDir, "compiled.gscc");

            await AppendColoredTextAsync($"[INFO] Output will be at: {expectedOutputPath}\n", _infoColor);

            if (File.Exists(expectedOutputPath))
            {
                await AppendColoredTextAsync($"[SUCCESS] Compiled output created: {expectedOutputPath}\n", _successColor);
                await AppendColoredTextAsync($"- Size: {new FileInfo(expectedOutputPath).Length} bytes\n", _infoColor);
                await AppendColoredTextAsync($"- Modified: {File.GetLastWriteTime(expectedOutputPath):yyyy-MM-dd HH:mm:ss}\n", _infoColor);

                // Track successful compilation
                AppSettings.AddSuccessfullyProcessedFile(isFileCompilation ? txtScriptPath.Text : expectedOutputPath);

                if (!chkCompileOnly.Checked && !chkBuild.Checked)
                {
                    await Task.Delay(300); // Small delay before injection
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

        private void ChkNoRuntime_CheckedChanged(object sender, EventArgs e)
        {
            if (_isInternalUpdate) return;

            ClearOutput();
            try
            {
                _isInternalUpdate = true;
                if (chkNoRuntime.Checked) // Only show message when enabled
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

                // Force style refresh when made visible
                if (chkHotLoad.Checked)
                {
                    cmbHotMode.DropDownStyle = ComboBoxStyle.DropDownList;
                    cmbHotMode.FlatStyle = FlatStyle.Flat;
                    cmbHotMode.FlatStyle = FlatStyle.Standard;
                    cmbHotMode.Refresh();
                }

                ClearOutput();
                SafeAppendText($"[CONFIG] Hot Load {(chkHotLoad.Checked ? "Enabled" : "Disabled")}\n");
                UpdateCompilerOptions();
            });
        }

        private void CmbGame_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbGame == null || cmbGame.SelectedIndex < 0 || !IsHandleCreated)
                return;

            try
            {
                if (cmbGame.SelectedItem is KeyValuePair<Games, string> selectedPair)
                {
                    _currentGame = selectedPair.Key;

                    // Safe UI updates
                    if (IsHandleCreated)
                    {
                        BeginInvoke((MethodInvoker)delegate {
                            UpdateGameStatus();
                            UpdateCompilerOptions();
                            UpdateInjectButtonState();

                            // Clear selection highlight
                            cmbGame.SelectionLength = 0;
                        });
                    }

                    // Non-UI operation can run directly
                    CheckGameProcess();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Game selection change error: {ex.Message}");
                SafeAppendText($"[ERROR] Failed to process game selection: {ex.Message}\n");
            }
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
                if (chkBuild.Checked) // Only show message when enabled
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
                if (chkCompileOnly.Checked) // Only show message when enabled
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
            if (IsDisposed || Disposing || btnInject == null || cmbGame == null || txtScriptPath == null)
                return;

            // Get current game states
            var (runningGame, isRunning) = DetectRunningGame();

            // Safely get selected game
            Games selectedGame = Games.None;
            if (cmbGame.SelectedItem is KeyValuePair<Games, string> selectedPair)
            {
                selectedGame = selectedPair.Key;
            }

            // Determine if injection should be enabled
            bool shouldEnable = !_isCompiling &&
                               !_isInjecting &&
                               isRunning &&
                               selectedGame == runningGame &&
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

                        // Show error dialog if disabling from enabled state
                        if (!shouldEnable && btnInject.Enabled)
                        {
                            string reason = "";
                            if (_isCompiling) reason += "• Compilation in progress\n";
                            if (_isInjecting) reason += "• Injection in progress\n";
                            if (!isRunning) reason += "• Game is not running\n";
                            else if (selectedGame != runningGame)
                                reason += $"• Wrong game selected (Running: {runningGame})\n";
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
            if (cmbGame.SelectedIndex < 0) return false;

            var game = (Games)cmbGame.SelectedIndex;
            var processName = GetProcessNameForGame(game);

            try
            {
                return Process.GetProcessesByName(processName).Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private bool IsGameRunning(Games game)
        {
            var processName = game switch
            {
                Games.T6 => "blackops2",
                Games.T7 => "blackops3",
                Games.T8 => "blackops4",
                _ => string.Empty
            };

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

        private async Task InjectCompiledScript(string outputFile)
        {
            try
            {
                if (!IsGameRunning())
                {
                    await AppendColoredTextAsync("[INFO] Game not running - injection skipped\n", _infoColor);
                    return;
                }

                var game = (Games)cmbGame.SelectedIndex;
                var processName = GetProcessNameForGame(game);

                await AppendColoredTextAsync($"[{DateTime.Now:HH:mm:ss}] Injecting into {processName}...\n", _infoColor);

                var args = new List<string> { outputFile, cmbGame.Text };
                var opts = new List<string> { "--inject" };

                // Handle Hotload independently
                if (chkHotLoad.Checked && cmbHotMode.SelectedItem != null)
                {
                    opts.Add("--hot");
                    opts.Add(cmbHotMode.SelectedIndex == 0 ? "gsc" : "csc");
                    await AppendColoredTextAsync($"[CONFIG] Hotload enabled ({cmbHotMode.SelectedItem})\n", _infoColor);
                }

                // Handle No Runtime independently
                if (chkNoRuntime.Checked)
                {
                    opts.Add("--noruntime");
                    await AppendColoredTextAsync("[CONFIG] Runtime checks disabled\n", _infoColor);
                }

                int result = await Task.Run(() => compilerRoot.ExecuteCommandLine(args.Concat(opts).ToArray()));

                // Get detailed error/output from compiler
                string errorInfo = compilerRoot.GetLastErrorInfo();
                string outputInfo = compilerRoot.GetLastOutput();

                if (!string.IsNullOrEmpty(outputInfo))
                {
                    await AppendColoredTextAsync(outputInfo + "\n", _infoColor);
                }

                if (result == 0)
                {
                    await AppendColoredTextAsync("\nINJECTION SUCCESSFUL\n", _successColor);
                    _lastInjectedScript = Path.GetFileName(outputFile);
                    _lastGameMode = cmbGame.Text;
                    _lastInjectionTime = DateTime.Now;
                    UpdateResetButton(true);

                    // Track successful injection
                    AppSettings.AddSuccessfullyProcessedFile(outputFile);

                    // Log injection details
                    await AppendColoredTextAsync($"Injected Script: {_lastInjectedScript}\n", _infoColor);
                    await AppendColoredTextAsync($"Game Mode: {_lastGameMode}\n", _infoColor);
                    await AppendColoredTextAsync($"Time: {_lastInjectionTime:HH:mm:ss}\n", _infoColor);
                }
                else
                {
                    await AppendColoredTextAsync("\nINJECTION FAILED\n", _errorColor);
                    await AppendColoredTextAsync($"Error Code: {result}\n", _errorColor);

                    if (!string.IsNullOrEmpty(errorInfo))
                    {
                        await AppendColoredTextAsync($"[DETAILS] {errorInfo}\n", _errorColor);
                    }

                    // Provide troubleshooting tips based on error code
                    if (result == -1)
                    {
                        await AppendColoredTextAsync("Tip: Verify the game process is accessible\n", _warningColor);
                    }
                }
            }
            catch (Exception ex)
            {
                await AppendColoredTextAsync($"[INJECTION ERROR] {ex.Message}\n", _errorColor);

                // Special handling for common exceptions
                if (ex is UnauthorizedAccessException)
                {
                    await AppendColoredTextAsync("Tip: Run the compiler as administrator\n", _warningColor);
                }
            }
        }

        private class ConsoleOutputRedirect : IDisposable
        {
            private readonly TextReader _originalIn;
            private readonly TextWriter _originalOut;
            private readonly TextWriter _originalErr;

            public ConsoleOutputRedirect()
            {
                _originalIn = Console.In;
                _originalOut = Console.Out;
                _originalErr = Console.Error;

                Console.SetIn(new StreamReader(Stream.Null));
                Console.SetOut(TextWriter.Null);
                Console.SetError(TextWriter.Null);
            }

            public void Dispose()
            {
                Console.SetIn(_originalIn);
                Console.SetOut(_originalOut);
                Console.SetError(_originalErr);
            }
        }

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

        private void DisplayError(string message, string title = "Error", Exception ex = null)
        {
            SafeInvoke(() =>
            {
                SafeAppendText($"[ERROR] {message}\n", _errorColor);

                if (ex != null)
                {
                    string details = $"{ex.Message}\n\nStack Trace:\n{ex.StackTrace}";

                    // For complex errors, show the detailed form
                    if (ex is not OperationCanceledException) // Skip for cancellations
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
                string logPath = Path.Combine(Application.StartupPath, "compiler_errors.log");
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
                SafeAppendText($"[WARNING] {message}\n");
            });
        }

        private void DisplayInfo(string message)
        {
            SafeInvoke(() =>
            {
                SafeAppendText($"[INFO] {message}\n");
            });
        }

        private void DisplaySuccess(string message)
        {
            SafeInvoke(() =>
            {
                SafeAppendText($"[SUCCESS] {message}\n");
            });
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