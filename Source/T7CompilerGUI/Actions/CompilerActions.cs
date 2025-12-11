using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using T7CompilerGUI.Helpers;
using T7CompilerGUI.Games;
using TreyarchCompiler.Enums;
using TreyarchCompiler.Utilities;
// External library types (ProcessEx, PointerEx, etc.) are in the System namespace

namespace T7CompilerGUI.Actions
{
    /// <summary>
    /// Compiler actions for installing and managing the T7 compiler
    /// </summary>
    public static class CompilerActions
    {
        // Static variables matching T7-Compiler-UI-main
        public static string menu = string.Empty;
        private static bool haveWeInjected = false;
        
        /// <summary>
        /// Gets the compiler installation path. Checks T7COMPILER_PATH environment variable first, then defaults to C:\t7compiler
        /// </summary>
        private static string GetCompilerPath()
        {
            string envPath = Environment.GetEnvironmentVariable("T7COMPILER_PATH");
            if (!string.IsNullOrEmpty(envPath) && Directory.Exists(envPath))
            {
                return envPath;
            }
            // Default installation path (can be overridden via environment variable)
            return @"C:\t7compiler";
        }

        /// <summary>
        /// Gets the GUI installation path. Checks T7GUI_PATH environment variable first, then defaults to C:\t7gui
        /// </summary>
        private static string GetGuiPath()
        {
            string envPath = Environment.GetEnvironmentVariable("T7GUI_PATH");
            if (!string.IsNullOrEmpty(envPath) && Directory.Exists(envPath))
            {
                return envPath;
            }
            // Default installation path (can be overridden via environment variable)
            return @"C:\t7gui";
        }
        /// <summary>
        /// Installs or updates the T7 compiler from the specified URL
        /// </summary>
        public static void InstallCompiler(string url = @"https://gsc.dev/t7c_package")
        {
            try
            {
                // Kill any running compiler or game processes
                KillProcessesByName("debugcompiler");
                KillProcessesByName("blackops3");
                KillProcessesByName("blackops4");

                var usertemp = Path.GetTempPath();
                var installertemp = Path.Combine(usertemp, "installer_temp");
                var extractpath = Path.Combine(usertemp, "update_t7.zip");
                var compileFolder = GetCompilerPath();

                // Clean up old temp files
                if (Directory.Exists(extractpath))
                    Directory.Delete(extractpath, true);

                if (Directory.Exists(installertemp))
                    Directory.Delete(installertemp, true);

                // Download the compiler package
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(url, extractpath);
                }

                // Extract the package
                System.IO.Compression.ZipFile.ExtractToDirectory(extractpath, installertemp);

                // Only install if compiler doesn't exist (matching T7-Compiler-UI-main behavior)
                if (Directory.Exists(compileFolder))
                {
                    // Cleanup and return early if already installed
                    Directory.Delete(installertemp, true);
                    File.Delete(extractpath);
                    return;
                }

                FileHelper.CopyDirectory(Path.Combine(installertemp, "t7compiler"), compileFolder, true);
                
                // Copy default project if it exists
                string defaultProjectSource = Path.Combine(installertemp, "defaultproject");
                if (Directory.Exists(defaultProjectSource))
                {
                    FileHelper.CopyDirectory(defaultProjectSource, Path.Combine(compileFolder, "defaultproject"), true);
                }

                // Copy Default Menu folder to compiler installation if it exists
                string defaultMenuSource = Path.Combine(installertemp, "Default Menu");
                if (Directory.Exists(defaultMenuSource))
                {
                    FileHelper.CopyDirectory(defaultMenuSource, Path.Combine(compileFolder, "Default Menu"), true);
                }

                // Also copy to GUI installation if it exists
                string guiFolder = GetGuiPath();
                if (Directory.Exists(guiFolder))
                {
                    // Copy Default Menu to GUI folder
                    if (Directory.Exists(defaultMenuSource))
                    {
                        FileHelper.CopyDirectory(defaultMenuSource, Path.Combine(guiFolder, "Default Menu"), true);
                    }
                }

                // Cleanup
                Directory.Delete(installertemp, true);
                File.Delete(extractpath);

                MessageBox.Show("Compiler Updated/Installed", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error installing compiler: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Checks if the compiler is installed
        /// </summary>
        public static bool IsCompilerInstalled()
        {
            return Directory.Exists(GetCompilerPath());
        }

        /// <summary>
        /// Gets the game string from Games enum
        /// </summary>
        public static string GetGame(TreyarchCompiler.Enums.Games game)
        {
            switch (game)
            {
                case TreyarchCompiler.Enums.Games.T7:
                    return "bo3";
                case TreyarchCompiler.Enums.Games.T8:
                    return "bo4";
                default:
                    return "bo3";
            }
        }

        /// <summary>
        /// Gets the game mode string from Modes enum
        /// </summary>
        public static string GetGameMode(TreyarchCompiler.Enums.Modes mode)
        {
            switch (mode)
            {
                case TreyarchCompiler.Enums.Modes.SP:
                    return "sp";
                case TreyarchCompiler.Enums.Modes.MP:
                    return "mp";
                case TreyarchCompiler.Enums.Modes.ZM:
                    return "zm";
                default:
                    return "zm";
            }
        }

        /// <summary>
        /// Compiles and injects a script into the game
        /// </summary>
        public static void CompileScript(string path, TreyarchCompiler.Enums.Games game, TreyarchCompiler.Enums.Modes gamemode, bool folderselected)
        {
            haveWeInjected = false;
            File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "injection.log"), "");
            
            if (!folderselected || !Directory.Exists(path))
                return;

            string compilerPath = GetCompilerPath();
            if (!Directory.Exists(compilerPath))
            {
                MessageBox.Show("Compiler Is not installed. Cannot continue", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string[] symbols = { "symbols=", "", "serious", "" };
            symbols[1] = GetGame(game);
            symbols[3] = GetGameMode(gamemode);

            if (!File.Exists(Path.Combine(path, "gsc.conf")))
            {
                File.Create(Path.Combine(path, "gsc.conf")).Close();
            }

            string sp = "scripts\\core_common\\load_shared.gsc";
            string mp = "scripts\\mp_common\\bb.gsc";
            string zm = "scripts\\zm_common\\load.gsc";

            string bo4symbols = null;
            if (symbols[3] == "sp")
                bo4symbols = sp;
            else if (symbols[3] == "mp")
                bo4symbols = mp;
            else if (symbols[3] == "zm")
                bo4symbols = zm;

            // 0 = symbols, 1 = game, 2 = serious, 3 = gamemode | symbols=game,serious,gamemode
            switch (symbols[1])
            {
                case "bo3":
                    try
                    {
                        File.WriteAllText(Path.Combine(path, "gsc.conf"), $"{symbols[0]}{symbols[1]},{symbols[2]},{symbols[3]}");
                    }
                    catch
                    {
                        return;
                    }
                    break;
                case "bo4":
                    try
                    {
                        File.WriteAllText(Path.Combine(path, "gsc.conf"), $"game=t8\nscript={bo4symbols}");
                    }
                    catch
                    {
                        return;
                    }
                    break;
            }

            string compilerExe = Path.Combine(compilerPath, "debugcompiler.exe");
            // Use relative path if compiler is in standard location, otherwise use full path
            string compilerCommand = compilerPath.Equals(@"C:\t7compiler", StringComparison.OrdinalIgnoreCase) 
                ? @"C:\t7compiler\debugcompiler" 
                : $"\"{compilerExe}\"";
            File.WriteAllText(Path.Combine(path, "compile.bat"), $"cd /d {path.Replace("/", "\\")}\n{compilerCommand} --build");
            
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(path, "compile.bat"),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process proc = Process.Start(startInfo);
            proc.OutputDataReceived += Proc_OutputDataReceived;
            proc.BeginOutputReadLine();
            proc.WaitForExit();

            string logPath = Path.Combine(Environment.CurrentDirectory, "injection.log");
            var finalresult = Regex.Replace(File.ReadAllText(logPath), @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);

            if (haveWeInjected)
            {
                bool bo3found = Process.GetProcessesByName("blackops3").Length > 0;
                bool bo4found = Process.GetProcessesByName("blackops4").Length > 0;

                if (bo3found)
                {
                    WindowHelper.BringProcessToFront(Process.GetProcessesByName("blackops3"));
                    BlackOps3.Popup($"^2{menu}\n^7Injected Successfully - ^9{GetGameMode(gamemode)}");
                }

                if (bo4found)
                {
                    WindowHelper.BringProcessToFront(Process.GetProcessesByName("blackops4"));
                    BlackOps4.Popup($"^2{menu}\n^7Injected Successfully - ^9{GetGameMode(gamemode)}");
                }
            }
            else
            {
                MessageBox.Show(finalresult, "Result", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private static void Proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                Console.WriteLine(e.Data);
                string logPath = Path.Combine(Environment.CurrentDirectory, "injection.log");
                File.AppendAllText(logPath, e.Data + Environment.NewLine);
                
                if (e.Data.Contains("If in game, you are probably going to crash."))
                {
                    try
                    {
                        ProcessEx dbc = "debugcompiler";
                        dbc.BaseProcess.Kill();
                        haveWeInjected = true;
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Kills all processes with the specified name
        /// </summary>
        private static void KillProcessesByName(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            foreach (var process in processes)
            {
                try
                {
                    process.Kill();
                }
                catch { }
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct T7SPT
        {
            public PointerEx llpName;
            public int BuffSize;
            public int Pad;
            public PointerEx lpBuffer;
        }

        /// <summary>
        /// Gets the game process using ProcessEx
        /// </summary>
        private static ProcessEx GetGameProcessEx(TreyarchCompiler.Enums.Games game)
        {
            try
            {
                string[] processNames = game == TreyarchCompiler.Enums.Games.T7 
                    ? new[] { "blackops3", "BlackOps3" }
                    : new[] { "blackops4", "BlackOps4" };
                
                foreach (string processName in processNames)
                {
                    try
                    {
                        ProcessEx processEx = processName;
                        if (processEx != null)
                        {
                            return processEx;
                        }
                    }
                    catch { }
                }
            }
            catch { }
            
            return null;
        }

        /// <summary>
        /// Injects a precompiled script into the game using direct process injection (same as MainForm)
        /// </summary>
        public static void InjectScript(string scriptPath, TreyarchCompiler.Enums.Games game)
        {
            try
            {
                // Validate file exists
                if (!File.Exists(scriptPath))
                {
                    MessageBox.Show("Script file not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Read script file
                byte[] buffer = File.ReadAllBytes(scriptPath);
                
                // Validate file is a compiled GSC script
                if (buffer.Length < 16)
                {
                    MessageBox.Show("File is too small to be a valid compiled script.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Check for GSC header
                long header = BitConverter.ToInt64(buffer, 0);
                if (header != 0x1C000A0D43534780 && header != 0x36000A0D43534780) // T7 and T8 headers
                {
                    string preamble = Encoding.ASCII.GetString(buffer.Take(4).ToArray());
                    if (preamble != "GSIC")
                    {
                        MessageBox.Show("File is not a valid compiled GSC script.\nExpected GSC header or GSIC preamble.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                // Only T7 is currently supported for direct injection
                if (game != TreyarchCompiler.Enums.Games.T7)
                {
                    MessageBox.Show("T8 injection not yet implemented. Please use the MainForm inject tab for T8.", "Not Supported", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Get game process
                ProcessEx bo3 = GetGameProcessEx(game);
                if (bo3 == null)
                {
                    MessageBox.Show("No game process found.\nMake sure Black Ops 3 is running.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                bo3.OpenHandle();
                bo3.SetDefaultCallType(ExCallThreadType.XCTT_QUAPC);

                // Determine if Windows Store version
                bool IsWindowsStore = !(bo3["GameChat2.dll"] is null);
                PointerEx off = IsWindowsStore ? 0xF3B1330 : 0x9407AB0;
                
                var sptGlob = bo3.GetValue<ulong>(bo3["blackops3.exe"][off]);
                var sptCount = bo3.GetValue<int>(bo3["blackops3.exe"][off + 0x14]);
                var SPTEntries = bo3.GetArray<T7SPT>(sptGlob, sptCount);
                
                // Default injection path (can be customized)
                string replacePath = "scripts/shared/duplicaterender_mgr.gsc";
                bool found = false;
                
                for (int i = 0; i < SPTEntries.Length; i++)
                {
                    var entry = SPTEntries[i];
                    if (!entry.llpName) continue;
                    
                    try
                    {
                        var name = bo3.GetString(entry.llpName);
                        if (name.ToLower().Trim().Replace("\\", "/") == replacePath.ToLower().Trim().Replace("\\", "/"))
                        {
                            found = true;
                            
                            // Get original checksum
                            int originalChecksum = bo3.GetValue<int>(entry.lpBuffer + 0x8);
                            
                            // Patch script into memory
                            entry.lpBuffer = bo3.QuickAlloc(buffer.Length);
                            BitConverter.GetBytes(originalChecksum).CopyTo(buffer, 0x8);
                            bo3.SetBytes(entry.lpBuffer, buffer);
                            
                            // Patch spt struct
                            ulong llpModifiedSPTStruct = (ulong)(i * Marshal.SizeOf(typeof(T7SPT))) + sptGlob;
                            bo3.SetStruct(llpModifiedSPTStruct, entry);
                            
                            break;
                        }
                    }
                    catch { }
                }

                if (!found)
                {
                    MessageBox.Show($"Script path '{replacePath}' not found in game's script table.\nMake sure the game is fully loaded.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Show success message
                WindowHelper.BringProcessToFront(Process.GetProcessesByName("blackops3"));
                BlackOps3.Popup("^7Injected Successfully - ^9Precompiled Script");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during injection: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

