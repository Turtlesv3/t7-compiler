// IMPROVED REWRITES FOR CodeEditorForm.cs
// These are better implementations to replace problematic code sections

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace T7CompilerGUI.Forms
{
    // ============================================================================
    // ISSUE 1: File reading without encoding specification
    // PROBLEM: File.ReadAllText() uses system default encoding, can cause issues
    // ============================================================================
    
    // OLD CODE:
    // string content = File.ReadAllText(filePath);
    
    // IMPROVED:
    private static string ReadFileWithEncoding(string filePath)
    {
        // Try UTF-8 first (most common for code files), fallback to system default
        try
        {
            return File.ReadAllText(filePath, Encoding.UTF8);
        }
        catch (DecoderFallbackException)
        {
            // If UTF-8 fails, try system default encoding
            return File.ReadAllText(filePath, Encoding.Default);
        }
    }
    
    // ============================================================================
    // ISSUE 2: Excessive Application.DoEvents() causing re-entrancy issues
    // PROBLEM: Application.DoEvents() can cause re-entrancy and performance issues
    // ============================================================================
    
    // OLD CODE in SaveAllFiles():
    // Application.DoEvents(); // Called multiple times
    
    // IMPROVED: Use async/await or BackgroundWorker for long operations
    private async Task SaveAllFilesAsync(bool updateTitle = true)
    {
        if (!folderOpened || string.IsNullOrEmpty(projectPath))
        {
            hasChanges = false;
            return;
        }

        try
        {
            string scriptsPath = Path.Combine(projectPath, "scripts");
            if (!Directory.Exists(scriptsPath))
            {
                Directory.CreateDirectory(scriptsPath);
            }

            // Use Task.Run for file I/O to avoid blocking UI thread
            await Task.Run(() =>
            {
                foreach (var kvp in openEditors.ToList()) // ToList() to avoid collection modification
                {
                    if (kvp.Value == null || kvp.Value.IsDisposed)
                        continue;

                    try
                    {
                        string filePath = Path.Combine(scriptsPath, kvp.Key);
                        string contentToSave = kvp.Value.Text;
                        
                        // Write file asynchronously
                        File.WriteAllText(filePath, contentToSave, Encoding.UTF8);
                        
                        // Update on UI thread
                        this.Invoke((MethodInvoker)delegate
                        {
                            fileContents[kvp.Key] = contentToSave;
                            kvp.Value.EmptyUndoBuffer();
                        });
                    }
                    catch (Exception ex)
                    {
                        // Log error properly instead of silent catch
                        System.Diagnostics.Debug.WriteLine($"Error saving file {kvp.Key}: {ex.Message}");
                        // Could show user-friendly error message here
                    }
                }
            });
            
            // Update UI on main thread
            if (updateTitle)
            {
                CheckForUnsavedChanges();
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to save files: {ex.Message}", ex);
        }
    }
    
    // ============================================================================
    // ISSUE 3: Heavy reflection usage in BtnCompile_Click
    // PROBLEM: Reflection is fragile, error-prone, and slow
    // ============================================================================
    
    // OLD CODE: Uses reflection to access MainForm private members
    
    // IMPROVED: Create a public interface or use events
    public interface IMainFormCompile
    {
        void CompileProject(string projectPath, string outputPath, TreyarchCompiler.Enums.Games game, TreyarchCompiler.Enums.Modes mode);
        void ShowCompileTab();
    }
    
    // In MainForm, implement the interface:
    // public partial class MainForm : PoisonForm, IMainFormCompile
    // {
    //     public void CompileProject(string projectPath, string outputPath, ...)
    //     {
    //         // Set fields and compile
    //     }
    // }
    
    // Then in CodeEditorForm:
    private void BtnCompile_Click_Improved(object sender, EventArgs e)
    {
        ResetButtonState(sender);
        
        if (!folderOpened || string.IsNullOrEmpty(projectPath))
        {
            ReaLTaiizor.Controls.PoisonMessageBox.Show(this, 
                "Please open a project folder first.", 
                "Error", 
                MessageBoxButtons.OK, 
                MessageBoxIcon.Warning);
            return;
        }

        try
        {
            SaveAllFiles();
            
            // Find MainForm and use interface
            var mainForm = Application.OpenForms.OfType<IMainFormCompile>().FirstOrDefault();
            if (mainForm != null)
            {
                string outputPath = Path.Combine(projectPath, Path.GetFileName(projectPath) + ".gsc");
                mainForm.ShowCompileTab();
                mainForm.CompileProject(projectPath, outputPath, currentGame, GetGameModeEnum());
            }
            else
            {
                // Fallback: Show message
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this,
                    "MainForm not available. Please use the Compile tab in the main window.",
                    "Compile",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            ReaLTaiizor.Controls.PoisonMessageBox.Show(this,
                $"Error during compilation: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
    
    // ============================================================================
    // ISSUE 4: File watcher without debouncing
    // PROBLEM: File watcher fires multiple times for single file operations
    // ============================================================================
    
    private System.Windows.Forms.Timer fileWatcherDebounceTimer;
    private string pendingFileWatcherPath = null;
    
    private void SetupFileWatcher_Improved()
    {
        fileWatcher = new FileSystemWatcher
        {
            Filter = "*.gsc",
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = false
        };
        
        // Debounce timer - wait 500ms after last change before processing
        fileWatcherDebounceTimer = new System.Windows.Forms.Timer
        {
            Interval = 500,
            Enabled = false
        };
        fileWatcherDebounceTimer.Tick += FileWatcherDebounceTimer_Tick;
        
        fileWatcher.Changed += (s, e) =>
        {
            // Debounce: reset timer on each change
            pendingFileWatcherPath = e.FullPath;
            fileWatcherDebounceTimer.Stop();
            fileWatcherDebounceTimer.Start();
        };
        
        fileWatcher.Created += FileWatcher_Created;
        fileWatcher.Deleted += FileWatcher_Deleted;
        fileWatcher.Renamed += FileWatcher_Renamed;
    }
    
    private void FileWatcherDebounceTimer_Tick(object sender, EventArgs e)
    {
        fileWatcherDebounceTimer.Stop();
        if (!string.IsNullOrEmpty(pendingFileWatcherPath))
        {
            FileWatcher_Changed_Improved(pendingFileWatcherPath);
            pendingFileWatcherPath = null;
        }
    }
    
    private void FileWatcher_Changed_Improved(string filePath)
    {
        if (this.InvokeRequired)
        {
            this.BeginInvoke(new Action<string>(FileWatcher_Changed_Improved), filePath);
            return;
        }

        string filename = Path.GetFileName(filePath);
        if (openEditors.ContainsKey(filename) && currentFileName != filename)
        {
            Scintilla editor = openEditors[filename];
            if (editor != null && !editor.IsDisposed)
            {
                try
                {
                    isLoadingFiles = true;
                    string content = ReadFileWithEncoding(filePath);
                    editor.Text = content;
                    fileContents[filename] = content;
                    editor.EmptyUndoBuffer();
                }
                catch (IOException ex)
                {
                    // File might be locked, retry after delay
                    System.Diagnostics.Debug.WriteLine($"File locked, retrying: {ex.Message}");
                    Task.Delay(100).ContinueWith(_ => FileWatcher_Changed_Improved(filePath));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error reloading file: {ex.Message}");
                }
                finally
                {
                    isLoadingFiles = false;
                }
            }
        }
    }
    
    // ============================================================================
    // ISSUE 5: Simplified thread-safe UI updates
    // PROBLEM: Repeated InvokeRequired checks scattered throughout code
    // ============================================================================
    
    private void InvokeOnUIThread(Action action)
    {
        if (this.InvokeRequired)
        {
            this.BeginInvoke(action);
        }
        else
        {
            action();
        }
    }
    
    // Usage example:
    // OLD: if (this.InvokeRequired) { this.Invoke(...); } else { ... }
    // NEW: InvokeOnUIThread(() => { ... });
    
    // ============================================================================
    // ISSUE 6: Optimized TextChanged handler
    // PROBLEM: TextChanged fires on every keystroke, can be expensive
    // ============================================================================
    
    private System.Windows.Forms.Timer textChangeDebounceTimer;
    private Scintilla pendingTextChangeEditor = null;
    
    private void SetupTextChangedHandler_Improved(Scintilla editor, string filename)
    {
        // Debounce text changes - only check after user stops typing for 300ms
        textChangeDebounceTimer = new System.Windows.Forms.Timer
        {
            Interval = 300,
            Enabled = false
        };
        textChangeDebounceTimer.Tick += (s, e) =>
        {
            textChangeDebounceTimer.Stop();
            if (pendingTextChangeEditor != null && !isLoadingFiles)
            {
                CheckForUnsavedChanges();
                UpdateConditionalCompilationIndicators(pendingTextChangeEditor);
                pendingTextChangeEditor = null;
            }
        };
        
        editor.TextChanged += (s, e) =>
        {
            if (!isLoadingFiles)
            {
                pendingTextChangeEditor = editor;
                textChangeDebounceTimer.Stop();
                textChangeDebounceTimer.Start();
            }
        };
    }
    
    // ============================================================================
    // ISSUE 7: Proper resource disposal
    // PROBLEM: Timers and resources might not be properly disposed
    // ============================================================================
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Dispose timers
            themeCheckTimer?.Stop();
            themeCheckTimer?.Dispose();
            themeCheckTimer = null;
            
            tabUpdateTimer?.Stop();
            tabUpdateTimer?.Dispose();
            // discordUpdateTimer cleanup removed - feature is disabled
            fileWatcherDebounceTimer?.Stop();
            fileWatcherDebounceTimer?.Dispose();
            textChangeDebounceTimer?.Stop();
            textChangeDebounceTimer?.Dispose();
            
            // Dispose file watcher
            if (fileWatcher != null)
            {
                fileWatcher.EnableRaisingEvents = false;
                fileWatcher.Dispose();
            }
            
            // Discord Rich Presence cleanup removed - feature is disabled
            
            // Dispose editors
            foreach (var editor in openEditors.Values)
            {
                if (editor != null && !editor.IsDisposed)
                {
                    editor.Dispose();
                }
            }
            openEditors.Clear();
            editorTabs.Clear();
            fileContents.Clear();
        }
        
        base.Dispose(disposing);
    }
    
    // ============================================================================
    // ISSUE 8: Better error handling instead of empty catch blocks
    // PROBLEM: Silent failures make debugging difficult
    // ============================================================================
    
    // OLD: catch { }
    
    // IMPROVED:
    private void HandleError(string context, Exception ex, bool showToUser = false)
    {
        string errorMsg = $"[{context}] {ex.GetType().Name}: {ex.Message}";
        System.Diagnostics.Debug.WriteLine(errorMsg);
        
        if (ex.InnerException != null)
        {
            System.Diagnostics.Debug.WriteLine($"  Inner: {ex.InnerException.Message}");
        }
        
        if (showToUser)
        {
            InvokeOnUIThread(() =>
            {
                ReaLTaiizor.Controls.PoisonMessageBox.Show(this,
                    $"An error occurred: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            });
        }
    }
    
    // Usage:
    // catch (Exception ex) { HandleError("SaveAllFiles", ex, showToUser: true); }
}

