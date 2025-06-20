// MainForm1.cs
using Refract.UI.Core.Interfaces;
using Refract.UI.Core.Singletons;
using Refract.UI.Core.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using TreyarchCompiler.Enums;
using SMC.UI.Core.Controls; // Add this line

namespace DebugCompiler
{
    public partial class MainForm1 : CBorderedForm, IThemeableControl, IResizableForm
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
        private UIThemeInfo _currentTheme;
        private readonly TextWriter _originalOut = Console.Out;
        private readonly object _outputLock = new object();

        public MainForm1()
        {
            // InitializeComponent must be first to create all controls
            InitializeComponent();

            // Initialize ControlContents if null (shouldn't be needed if designer is set up properly)
            if (ControlContents == null)
            {
                ControlContents = new Panel();
                ControlContents.Dock = DockStyle.Fill;
                this.Controls.Add(ControlContents);
            }

            // Add MainContainer to ControlContents if not already added
            if (!ControlContents.Controls.Contains(MainContainer))
            {
                MainContainer.Dock = DockStyle.Fill;
                ControlContents.Controls.Add(MainContainer);
            }

            // Add menu items
            var fileMenu = new ToolStripMenuItem("File");
            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => this.Close();
            fileMenu.DropDownItems.Add(exitItem);

            // Add status label
            var statusLabel = new ToolStripStatusLabel();
            statusLabel.Text = "Ready";

            // Theme setup
            UIThemeManager.OnThemeChanged(this, OnThemeChanged_Implementation);
            this.SetThemeAware();
            UIThemeManager.SetTheme("Dark"); // Force initial theme

            // Initialize custom components after all controls are ready
            InitializeCustomComponents();

            // Compiler setup
            compilerRoot = new Root();
            compilerRoot.OnLogMessage += (msg) => SafeAppendText(msg + "\n");
            compilerRoot.OnError += (err) => SafeAppendText("[ERROR] " + err + "\n");

            CheckRequiredFiles();
            this.Text = $"T7/T8 Compiler v{GetVersion()} - by Serious -GUI by DoubleG ;)";
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
                        txtOutput.AppendText(text);
                        txtOutput.ScrollToCaret();
                    }
                    finally
                    {
                        txtOutput.ResumeLayout();
                    }

                    if (text.Contains("[ERROR]"))
                    {
                        Color original = txtOutput.ForeColor;
                        txtOutput.ForeColor = Color.Red;
                        Task.Delay(500).ContinueWith(_ =>
                        {
                            txtOutput.Invoke((Action)(() => txtOutput.ForeColor = original));
                        });
                    }
                }
            }
        }

        public override IEnumerable<Control> GetThemedControls()
        {
            // First return controls from base class
            foreach (var control in base.GetThemedControls())
            {
                yield return control;
            }

            // Then return your additional controls
            yield return MainContainer;
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
            yield return chkNoUpdate;
            yield return cmbGame;
        }

        private void OnThemeChanged_Implementation(UIThemeInfo currentTheme)
        {
            _currentTheme = currentTheme;

            // Apply theme to all controls
            this.BackColor = currentTheme.BackColor;
            this.ForeColor = currentTheme.TextColor;

            // Custom control theming
            txtOutput.BackColor = Color.Black;
            txtOutput.ForeColor = Color.Lime;

            // Special button theming
            btnResetParseTree.BackColor = currentTheme.ButtonActive;
            btnResetParseTree.FlatAppearance.BorderColor = currentTheme.AccentColor;
            btnResetParseTree.ForeColor = currentTheme.TextColor;
        }

        public void WndProc_Implementation(ref Message m)
        {
            base.WndProc(ref m);
        }

        private void InitializeCustomComponents()
        {
            // Add null check at start
            if (cmbGame == null)
            {
                cmbGame = new ComboBox();
                cmbGame.BackColor = Color.FromArgb(50, 50, 50);
                cmbGame.ForeColor = Color.White;
                cmbGame.FlatStyle = FlatStyle.Flat;
                OptionsPanel.Controls.Add(cmbGame);
            }

            var gameDisplayNames = new Dictionary<string, string>
    {
        {"t6", "Call of Duty: Black Ops 2"},
        {"t7", "Call of Duty: Black Ops 3"},
        {"t8", "Call of Duty: Black Ops 4"}
    };

            // Get all enum names and map them to display names
            var gameNames = Enum.GetNames(typeof(Games))
                               .Select(name => gameDisplayNames.TryGetValue(name.ToLower(), out var displayName)
                                                  ? displayName
                                                  : name)
                               .ToArray();

            cmbGame.Items.AddRange(gameNames);
            cmbGame.SelectedIndex = 1;

            // Initialize cmbHotMode here
            cmbHotMode.Items.AddRange(new[] { "GSC", "CSC" });
            cmbHotMode.SelectedIndex = 0;
            cmbHotMode.Enabled = false;

            // Tooltips for buttons
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

            // New comprehensive tooltip for compile button
            resetToolTip.SetToolTip(btnCompile,
                "Compile Script\n\n" +
                "Requirements:\n" +
                "- Select a .gsc file or folder first\n\n" +
                "Compile Options:\n" +
                "- Full Build: Complete Build+Inject\n" +
                "- Compile Only: Skip injection");

            // Tooltips for checkboxes
            resetToolTip.SetToolTip(chkNoUpdate,
                "No Update\n\n" +
                "Skips all program version update checks\n" +
                "Faster compilation but may miss important updates");

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

        private async void BtnResetParseTree_Click(object sender, EventArgs e)
        {
            await Task.Run(() => ClearOutput());

            var confirm = MessageBox.Show(
                "WARNING: Resetting parse tree while in-game may cause crashes!\n\n" +
                "Are you sure you want to reset the GSC parse tree?",
                "Confirm Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

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
            // Create a context menu for the button
            var menu = new ContextMenuStrip();

            // Add file selection option
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

            // Add folder selection option
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

            // Show the menu below the button
            menu.Show(btnBrowse, new Point(0, btnBrowse.Height));
        }

        private async void BtnCompile_Click(object sender, EventArgs e)
        {
            ClearOutput();

            // Enhanced input validation
            if (string.IsNullOrWhiteSpace(txtScriptPath.Text))
            {
                CErrorDialog.Show("Error", "Please select a script file or folder first", true);
                return;
            }

            // Verify the path exists
            bool isFile = File.Exists(txtScriptPath.Text);
            bool isDir = Directory.Exists(txtScriptPath.Text);

            if (!isFile && !isDir)
            {
                SafeAppendText($"[ERROR] Path does not exist: {txtScriptPath.Text}\n");
                return;
            }

            try
            {
                Cursor.Current = Cursors.WaitCursor;
                btnCompile.Enabled = false;

                // Debug logging
                SafeAppendText($"[DEBUG] Starting compilation...\n");
                SafeAppendText($"[DEBUG] Target: {(isFile ? "File" : "Folder")} {txtScriptPath.Text}\n");
                SafeAppendText($"[DEBUG] Game: {cmbGame.Text}\n");

                var args = new List<string> { txtScriptPath.Text, cmbGame.Text };
                var opts = new List<string>();

                if (chkBuild.Checked)
                {
                    opts.Add("--build");
                    SafeAppendText("[FULL BUILD] Enabled\n");
                }

                if (isDir)
                {
                    opts.Add("--batch");
                    SafeAppendText("[BATCH MODE] Compiling all scripts in folder\n");
                }

                // Add other options...
                if (chkNoUpdate.Checked) opts.Add("--noupdate");
                if (chkCompileOnly.Checked) opts.Add("--compile");
                if (chkHotLoad.Checked)
                {
                    opts.Add("--hot");
                    opts.Add(cmbHotMode.SelectedIndex == 0 ? "gsc" : "csc");
                }
                if (chkNoRuntime.Checked) opts.Add("--noruntime");

                // Log final command
                SafeAppendText($"[DEBUG] Command: compilerRoot {string.Join(" ", args.Concat(opts))}\n");

                int result = await Task.Run(() =>
                {
                    try
                    {
                        return compilerRoot.ExecuteCommandLine(args.Concat(opts).ToArray());
                    }
                    catch (Exception ex)
                    {
                        SafeAppendText($"[COMPILER ERROR] {ex.Message}\n");
                        return -1;
                    }
                });

                if (result == 0)
                {
                    SafeAppendText("\nCOMPILATION SUCCESSFUL\n");

                    // Handle output path
                    string outputPath;
                    if (isDir)
                    {
                        outputPath = Path.Combine(txtScriptPath.Text, "bin", "compiled.gscc");
                    }
                    else
                    {
                        outputPath = Path.ChangeExtension(txtScriptPath.Text, ".gscc");
                    }

                    if (File.Exists(outputPath))
                    {
                        SafeAppendText($"Output file: {outputPath}\n");

                        if (!chkCompileOnly.Checked && !chkBuild.Checked)
                        {
                            await Task.Delay(300);
                            await InjectCompiledScript(outputPath);
                        }
                    }
                    else
                    {
                        SafeAppendText("[WARNING] No output file was generated\n");
                    }
                }
                else
                {
                    SafeAppendText("\nCOMPILATION FAILED\n");
                    SafeAppendText($"[ERROR] Exit code: {result}\n");
                }
            }
            catch (Exception ex)
            {
                SafeAppendText($"[SYSTEM ERROR] {ex.Message}\n");
                SafeAppendText($"[STACK TRACE] {ex.StackTrace}\n");
            }
            finally
            {
                Cursor.Current = Cursors.Default;
                btnCompile.Enabled = true;
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

                // Save original console streams
                TextReader originalInput = Console.In;
                TextWriter originalOutput = Console.Out;
                TextWriter originalError = Console.Error;

                try
                {
                    // Redirect all console streams
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
                    // Restore original console streams
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
            await Task.Run(() => ClearOutput());

            if (string.IsNullOrWhiteSpace(txtScriptPath.Text))
            {
                resetToolTip.SetToolTip(btnInject,
            "Please select a script file first!\n\n" +
            "Use the Browse button to select a .gsc/.gscc file");
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

            try
            {
                btnInject.Enabled = false;
                Cursor = Cursors.WaitCursor;
                SafeAppendText($"[{DateTime.Now:HH:mm:ss}] Starting injection...\n");

                _lastInjectedScript = Path.GetFileName(txtScriptPath.Text);
                _lastGameMode = cmbGame.Text;
                _lastInjectionTime = DateTime.Now;

                var args = new List<string> { txtScriptPath.Text, cmbGame.Text };
                var opts = new List<string> { "--inject" };

                if (chkHotLoad.Checked)
                {
                    opts.Add("--hot");
                    opts.Add(cmbHotMode.SelectedIndex == 0 ? "gsc" : "csc");
                }
                if (chkNoRuntime.Checked) opts.Add("--noruntime");

                int result = await Task.Run(() => compilerRoot.ExecuteCommandLine(args.Concat(opts).ToArray()));

                if (result != 0 || txtOutput.Text.Contains("[ERROR] No game process found"))
                {
                    SafeAppendText("\nINJECTION FAILED\n");
                    UpdateResetButton(false);
                }
                else
                {
                    SafeAppendText("\nINJECTION SUCCESSFUL\n");
                    UpdateResetButton(true);
                }
            }
            catch (Exception ex)
            {
                SafeAppendText($"[ERROR] {ex.Message}\n");
                CErrorDialog.Show("Injection Error", $"An error occurred during injection:\n{ex.Message}", true);
            }
            finally
            {
                btnInject.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        private void MainForm1_Load(object sender, EventArgs e)
        {
            if (_currentTheme.Equals(default(UIThemeInfo)))
            {
                _currentTheme = UIThemeInfo.Default();
                OnThemeChanged_Implementation(_currentTheme);
            }
        }

        private void CmbHotMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Optional: Add any specific handling for hot mode changes
        }

        private void chkHotLoad_CheckedChanged(object sender, EventArgs e)
        {
            cmbHotMode.Enabled = chkHotLoad.Checked;

            if (chkHotLoad.Checked)
            {
                SafeAppendText("Hot Load enabled. Mode: " + cmbHotMode.Text + "\n");
            }
            else
            {
                SafeAppendText("Hot Load disabled\n");
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
            string processName = cmbGame.Text switch
            {
                "Call of Duty: Black Ops 3" => "BlackOps3",
                "Call of Duty: Black Ops 4" => "BlackOps4",
                _ => cmbGame.Text.Replace(" ", "").ToLower()
            };
            return Process.GetProcessesByName(processName).Length > 0;
        }

    }
}