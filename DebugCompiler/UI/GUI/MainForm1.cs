// MainForm1.cs
using Refract.UI.Core.Interfaces;
using Refract.UI.Core.Singletons;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using TreyarchCompiler.Enums;

namespace DebugCompiler
{
    public partial class MainForm1 : Form, IThemeableControl, IResizableForm
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
        private TextWriter _originalOut = Console.Out;

        public MainForm1()
        {
            InitializeComponent();
            UIThemeManager.OnThemeChanged(this, OnThemeChanged_Implementation);
            this.SetThemeAware();
            InitializeCustomComponents();
            compilerRoot = new Root();
            compilerRoot.OnLogMessage += (msg) => SafeAppendText(msg + "\n");
            compilerRoot.OnError += (err) => SafeAppendText("[ERROR] " + err + "\n");
            CheckRequiredFiles();
            this.Text = $"T7/T8 Debug Compiler v{GetVersion()} - by Serious";
        }

        private void SafeAppendText(string text)
        {
            if (txtOutput.InvokeRequired)
            {
                txtOutput.Invoke(new Action<string>(SafeAppendText), text);
            }
            else
            {
                txtOutput.AppendText(text);
            }
        }

        public IEnumerable<Control> GetThemedControls()
        {
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
            txtOutput.BackColor = Color.Black;
            txtOutput.ForeColor = Color.Lime;

            // Enhanced theming for reset button
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
            // Initialize combo boxes
            cmbGame.Items.AddRange(Enum.GetNames(typeof(Games)));
            cmbGame.SelectedIndex = 0;

            cmbHotMode.Items.AddRange(new[] { "GSC", "CSC" });
            cmbHotMode.SelectedIndex = 0;
            cmbHotMode.Enabled = false;

            // Tooltips
            resetToolTip.SetToolTip(btnResetParseTree,
                "Reset GSC Parse Tree\n\n" +
                $"Last Injection: {_lastInjectedScript ?? "None"}\n" +
                $"Game: {_lastGameMode ?? "None"}\n" +
                $"Time: {_lastInjectionTime:HH:mm:ss}\n\n" +
                "WARNING: May cause crashes if done while in-game!");

            resetToolTip.SetToolTip(btnInject,
                "Inject Compiled Script\n\n" +
                "Requirements:\n" +
                "- Valid .gsc file selected\n" +
                "- Game process running\n" +
                "- Proper compiler setup");

            // Event handlers
            chkHotLoad.CheckedChanged += (s, e) => cmbHotMode.Enabled = chkHotLoad.Checked;
            btnBrowse.Click += BtnBrowse_Click;
            btnCompile.Click += BtnCompile_Click;
            btnInject.Click += BtnInject_Click;
            btnResetParseTree.Click += BtnResetParseTree_Click;
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
                MessageBox.Show(ex.Message, "Reset Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UpdateResetButton(false); // Hide after reset
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
            using (var openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "GSC Files (*.gsc, *.gscc)|*.gsc;*.gscc|All Files (*.*)|*.*";
                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    txtScriptPath.Text = openDialog.FileName;
                    btnResetParseTree.Visible = false;
                }
            }
        }

        private async void BtnCompile_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtScriptPath.Text))
            {
                MessageBox.Show("Please select a script file first", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                Cursor.Current = Cursors.WaitCursor;
                btnCompile.Enabled = false;

                var args = new List<string> { txtScriptPath.Text, cmbGame.Text };
                var opts = new List<string>();

                if (chkNoUpdate.Checked) opts.Add("--noupdate");
                if (chkBuild.Checked) opts.Add("--build");
                if (chkCompileOnly.Checked) opts.Add("--compile");
                if (chkHotLoad.Checked) opts.Add("--hot");
                if (chkNoRuntime.Checked) opts.Add("--noruntime");

                await Task.Run(() =>
                {
                    int result = compilerRoot.ExecuteCommandLine(args.Concat(opts).ToArray());
                    SafeAppendText(result == 0 ? "\nCOMPILATION SUCCESSFUL\n" : "\nCOMPILATION FAILED\n");
                });
            }
            catch (Exception ex)
            {
                SafeAppendText($"[ERROR] {ex.Message}\n");
                MessageBox.Show(ex.Message, "Compilation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
                btnCompile.Enabled = true;
            }
        }

        private async void BtnInject_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtScriptPath.Text))
            {
                MessageBox.Show("Please select a script file first", "Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                btnInject.Enabled = false;
                Cursor = Cursors.WaitCursor;

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

                if (result == 0)
                {
                    SafeAppendText("\nINJECTION SUCCESSFUL\n");
                    UpdateResetButton(true); // New method to handle button state
                }
                else
                {
                    SafeAppendText("\nINJECTION FAILED\n");
                    UpdateResetButton(false);
                }
            }
            catch (Exception ex)
            {
                SafeAppendText($"[ERROR] {ex.Message}\n");
                MessageBox.Show(ex.Message, "Injection Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
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
    }
}