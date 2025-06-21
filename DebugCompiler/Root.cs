using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TreyarchCompiler;
using T7CompilerLib;
using TreyarchCompiler.Enums;
using Games = TreyarchCompiler.Enums.Games;
using T7CompilerLib.OpCodes;
using XDevkit;
using Microsoft.Test.Xbox.XDRPC;
using TreyarchCompiler.Utilities;
using System.Windows.Forms.VisualStyles;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Reflection;
using System.Net;
using T89CompilerLib;

namespace DebugCompiler
{
    public class Root
    {
        // Events for logging
        public delegate void LogHandler(string message);
        public event LogHandler OnLogMessage;
        public event LogHandler OnError;

        private struct CommandInfo
        {
            internal string CommandName;
            internal CommandHandler Exec;
        }
        private delegate int CommandHandler(string[] args, string[] opts);
        private readonly Dictionary<ConsoleKey, CommandInfo> CommandTable;
        private bool ClearHistory = false;
        private static readonly string UpdatesURL = "https://gsc.dev/t7c_version";
        private static readonly string UpdaterURL = "https://gsc.dev/t7c_updater";
        private static string Motdpath => Path.Combine(Application.StartupPath, "motd");
        private const int motdHrsRemindClear = 4;
        private static string T7ProcessName = "blackops3";

        // Hash tables - ONLY DECLARE THESE ONCE
        private static Dictionary<uint, string> t8_dword;
        private static Dictionary<uint, string> t7_dword;
        private static Dictionary<ulong, string> t8_qword;

        public Root()
        {
            CommandTable = new Dictionary<ConsoleKey, CommandInfo>();
            AddCommand(ConsoleKey.Q, "Quit Program", Cmd_Exit);
            AddCommand(ConsoleKey.H, "Hash String [fnv|fnv64|gsc] <baseline> <prime> [input]", Cmd_HashString);
            AddCommand(ConsoleKey.T, "Toggle Text History", Cmd_ToggleNoClear);
            AddCommand(ConsoleKey.C, "Compile Script [path] <T7|T8>", Cmd_Compile);
            AddCommand(ConsoleKey.I, "Inject Script [path] <T7|T8> <inject path>", Cmd_Inject);

            // Initialize hash tables
            t8_dword = new Dictionary<uint, string>();
            t7_dword = new Dictionary<uint, string>();
            t8_qword = new Dictionary<ulong, string>();

            LoadHashTable();
        }
        public int ExecuteCommandLine(string[] args)
        {
            ParseCmdArgs(args, out string[] arguments, out string[] options);

            if (options.Contains("--build") || options.Contains("--compile"))
                return Cmd_Compile(arguments, options);

            if (arguments.Length > 1 && options.Contains("--inject"))
                return Cmd_Inject(arguments, options);

            return 0;
        }

        public void PublicFreeActiveScript(bool forceReset) => FreeActiveScript();

        public static int RunCommandLine(string[] args)
        {
            // Create instance for command execution
            Root rootInstance = new();

            ParseCmdArgs(args, out string[] arguments, out string[] options);

            // Only show version if we have actual commands
            bool hasCommands = options.Contains("--build") ||
                              options.Contains("--compile") ||
                              options.Contains("--inject");

            if (hasCommands)
            {
                string lv = GetEmbeddedVersion();
                Console.WriteLine($"T7/T8 Compiler version {lv}, by Serious\n");

                // Handle update check unless --noupdate is specified
                if (!options.Contains("--noupdate"))
                {
                    try
                    {
                        Motd(new FileInfo(Motdpath));
                        ulong local_version = ParseVersion(lv);
                        ulong remote_version = 0;
                        Console.WriteLine($"Checking client version... (our version is {local_version:X})");
                        using (WebClient client = new())
                        {
                            string downloadString = client.DownloadString(UpdatesURL);
                            remote_version = ParseVersion(downloadString.ToLower().Trim());
                        }
                        if (local_version < remote_version)
                        {
                            Console.WriteLine("Client out of date, downloading installer...");
                            string filename = Path.Combine(Path.GetTempPath(), "t7c_installer.exe");
                            if (File.Exists(filename)) File.Delete(filename);
                            using (WebClient client = new())
                            {
                                client.DownloadFile(UpdaterURL, filename);
                            }
                            Console.WriteLine("Installing update... Please wait for a confirmation window to pop up before attempting to inject again...");
                            Process.Start(filename, "--install_silent");
                            return 0;
                        }
                    }
                    catch
                    {
                        Console.WriteLine($"Error updating client... ignoring update");
                    }
                }

                // Handle process name overrides
                if (options.Contains("--boiii")) T7ProcessName = "boiii";
                if (options.Contains("--t7x")) T7ProcessName = "t7x";

                // Execute commands
                if (options.Contains("--build") || options.Contains("--compile"))
                    return rootInstance.Cmd_Compile(arguments, options);

                if (arguments.Length > 1 && options.Contains("--inject"))
                    return rootInstance.Cmd_Inject(arguments, options);
            }

            // If no valid commands, show help
            Console.WriteLine("Available commands:");
            Console.WriteLine("--compile [path] <T7|T8> - Compile a script");
            Console.WriteLine("--inject [path] <T7|T8> <inject path> - Inject a compiled script");
            Console.WriteLine("--noupdate - Skip version check");
            Console.WriteLine("--boiii - Use boiii process name");
            Console.WriteLine("--t7x - Use t7x process name");
            Console.WriteLine("\nFor GUI mode, run without --console flag");
            return 1;
        }

        static void Motd(FileInfo motdFileInfo)
        {
            var fi = new FileInfo(Motdpath);
            if (fi.Exists)
            {
                if ((DateTime.Now - fi.LastWriteTimeUtc).TotalMinutes <= 60 * motdHrsRemindClear)
                {
                    return; // we dont want to spam users with artificial delays in the program. Lets be nice and only show the motd once every 4 hours.
                }
            }
            File.WriteAllText(Motdpath, "https://www.youtube.com/anthonything");
            fi = motdFileInfo;
            fi.LastWriteTimeUtc = DateTime.Now;
            Console.WriteLine($"Message of the Day:\n\tEver wanted to shoot your friend with a thundergun?\n\tEver wondered what would happen if you could 1v1 with the origins staffs?\n\tNow you can! Zombie Blood Rush is a Black Ops III zombies mod that lets you kill other players.\n\tYour points are your health. Kill other players and zombies to race to 100K points. Play now: https://steamcommunity.com/sharedfiles/filedetails/?id=2696008055\n\n");
            System.Threading.Thread.Sleep(4000);
        }

        static ulong ParseVersion(string vstr)
        {
            ulong result = 0;
            string[] numbers = vstr.Split('.');
            int index = 0;
            for (int i = 0; i < numbers.Length; i++, index++)
            {
                int real_index = numbers.Length - 1 - i;
                ulong num = ushort.Parse(numbers[real_index]);
                result += num << index * 16;
            }
            return result;
        }

        static string GetEmbeddedVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "DebugCompiler.version";

            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            using StreamReader reader = new(stream);
            return reader.ReadToEnd().Trim().ToLower();
        }

        private ConsoleKey PrintOptions()
        {
            if (ClearHistory && !IsConsoleApp())
                Console.Clear();

            foreach (var kvp in CommandTable)
            {
                Console.WriteLine($"{kvp.Key}: {kvp.Value.CommandName}");
            }

            return IsConsoleApp() ? Console.ReadKey(true).Key : ConsoleKey.Escape;
        }

        private static bool IsConsoleApp()
        {
            return Environment.UserInteractive
                   && Console.OpenStandardInput(1) != Stream.Null
                   && !Console.IsInputRedirected;
        }

        public static IEnumerable<string> ParseArgs(string line, char delimiter, char textQualifier)
        {

            if (line == null)
                yield break;

            else
            {
                bool inString = false;

                StringBuilder token = new();

                for (int i = 0; i < line.Length; i++)
                {
                    char currentChar = line[i];
                    char prevChar;
                    if (i > 0)
                        prevChar = line[i - 1];
                    else
                        prevChar = '\0';

                    char nextChar;
                    if (i + 1 < line.Length)
                        nextChar = line[i + 1];
                    else
                        nextChar = '\0';

                    if (currentChar == textQualifier && (prevChar == '\0' || prevChar == delimiter) && !inString)
                    {
                        inString = true;
                        continue;
                    }

                    if (currentChar == textQualifier && (nextChar == '\0' || nextChar == delimiter) && inString)
                    {
                        inString = false;
                        continue;
                    }

                    if (currentChar == delimiter && !inString)
                    {
                        yield return token.ToString();
                        token = token.Remove(0, token.Length);
                        continue;
                    }

                    token = token.Append(currentChar);

                }

                yield return token.ToString();

            }
        }

        private static void ParseCmdArgs(string[] argv, out string[] arguments, out string[] options)
        {
            List<string> opts = new();
            List<string> args = new();

            foreach (string arg in argv)
            {
                if (arg == null || arg.Length == 0)
                {
                    continue;
                }

                if (arg[0] != '-')
                {
                    args.Add(arg);
                }
                else
                {
                    opts.Add(arg);
                }
            }

            arguments = args.ToArray();
            options = opts.ToArray();
        }

        private void AddCommand(ConsoleKey key, string CmdName = "Unknown Command", CommandHandler cex = null)
        {
            if (CommandTable.ContainsKey(key) || cex == null)
                return;
            CommandTable[key] = new CommandInfo() { CommandName = CmdName, Exec = cex };
        }

        private int Exec(ConsoleKey cmd)
        {
            if (!CommandTable.ContainsKey(cmd))
                return 1;

            Success(CommandTable[cmd].CommandName);
            Console.WriteLine("Enter args (if any):");
            string args = Console.ReadLine().Trim();
            Success(args);

            ParseCmdArgs(ParseArgs(args, ' ', '"').ToArray(), out string[] arguments, out string[] options);
            int ret = CommandTable[cmd].Exec.Invoke(arguments, options);
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(false);
            Console.WriteLine();
            return ret;
        }

        private int Error(string msg = "Error encountered")
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            OnError?.Invoke(msg);  // Changed from Error(msg) to OnError?.Invoke(msg)
            Console.WriteLine(msg);
            Console.ForegroundColor = old;
            return 1;
        }

        private int Success(string msg = "")
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            OnLogMessage?.Invoke(msg);  // Changed from Log(msg) to OnLogMessage?.Invoke(msg)
            Console.WriteLine(msg);
            Console.ForegroundColor = old;
            return 0;
        }

        #region commands

        private static void LoadHashTable(bool force = false)
        {

        }

        private int Cmd_Inject(string[] args, string[] opts)
        {
            if (args.Length < 1)
            {
                return Error("Invalid arguments. Please specify a file to inject.");
            }

            if (!File.Exists(args[0]))
            {
                return Error("Specified file does not exist.");
            }

            Games game = Games.T7;
            if (args.Length > 1)
            {
                if (!Enum.TryParse(args[1], true, out game))
                {
                    game = Games.T7;
                }
            }

            try
            {
                byte[] buffer = File.ReadAllBytes(args[0]);
                var path = args.Length > 2 ? args[2] :
                          game == Games.T7 ? @"scripts/shared/duplicaterender_mgr.gsc" :
                                              @"scripts/zm_common/load.gsc";

                PointerEx injresult = InjectScript(path, buffer, game, Hotmode.none, false);

                // Modified output handling
                string resultMessage = !injresult ?
                    $"\t[{path}]: Injected" :
                    $"\t[{path}]: Failed to Inject ({injresult:X})";

                OnLogMessage?.Invoke(resultMessage);
                Console.WriteLine(resultMessage);

                if (!injresult)
                {
                    string resetMessage = "Press any key to reset gsc parsetree... If in game, you may crash.";
                    OnLogMessage?.Invoke(resetMessage);

                    // Only wait for key press in console mode
                    if (Environment.UserInteractive && Console.OpenStandardInput(1) != Stream.Null)
                    {
                        Console.ReadKey(true);
                        NoExcept(FreeActiveScript);
                        OnLogMessage?.Invoke("\tScript parsetree has been reset");
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                return Error($"Failed to inject: {ex.Message}");
            }
        }

        private int Cmd_DumpEmptySlots(string[] args, string[] opts)
        {
            return -1;
        }

        private int Cmd_migrateMap(string[] args, string[] opts)
        {
            return -1;
        }

        private int Cmd_ExtractStrings(string[] args, string[] opts)
        {
            return -1;
        }

        public unsafe static string DecodeAscii(byte[] buffer, int index = 0)
        {
            fixed (byte* bytes = &buffer[index])
            {
                return new string((sbyte*)bytes);
            }
        }

        private int Cmd_Collect(string[] args, string[] opts)
        {
            return -1;
        }

        private int Cmd_Automap(string[] args, string[] opts)
        {
            return -1;
        }

        private int Cmd_HashString(string[] args, string[] opts)
        {
            if (args.Length != 2 && args.Length != 4)
                return Error("Invalid arguments");

            string input;

            string method = args[0].Trim().ToLower();

            switch (method)
            {
                case "fnv64":
                    ulong fnv64Offset = 14695981039346656037;
                    ulong fnv64Prime = 0x100000001b3;

                    if (args.Length == 2)
                        input = args[1].Replace('"', ' ').Trim();
                    else
                    {
                        if (args.Length != 4)
                            return Error("Invalid arguments");
                        try
                        {
                            fnv64Offset = ulong.Parse(args[1].Trim().ToLower().Replace("0x", ""), NumberStyles.HexNumber);
                            fnv64Prime = ulong.Parse(args[2].Trim().ToLower().Replace("0x", ""), NumberStyles.HexNumber);
                            input = args[3].Replace('"', ' ').Trim();
                        }
                        catch
                        {
                            return Error("Invalid arguments");
                        }
                    }

                    Console.WriteLine(HashFNV1a(Encoding.ASCII.GetBytes(input), fnv64Offset, fnv64Prime).ToString("X8"));
                    return 0;

                case "fnv":
                    uint baseline = 0x4B9ACE2F;
                    uint prime = 0x1000193;

                    if (args.Length == 2)
                        input = args[1].Replace('"', ' ').Trim();
                    else
                    {
                        if (args.Length != 4)
                            return Error("Invalid arguments");
                        try
                        {
                            baseline = uint.Parse(args[1].Trim().ToLower().Replace("0x", ""), NumberStyles.HexNumber);
                            prime = uint.Parse(args[2].Trim().ToLower().Replace("0x", ""), NumberStyles.HexNumber);
                            input = args[3].Replace('"', ' ').Trim();
                        }
                        catch
                        {
                            return Error("Invalid arguments");
                        }
                    }

                    Console.WriteLine(Com_Hash(input, baseline, prime).ToString("X4"));
                    return 0;


                default:
                    return Error($"Invalid method '{method}'");
            }
        }

        private int Cmd_GenerateHashMap(string[] args, string[] opts)
        {
            return -1;
        }

        private int Cmd_Permute(string[] args, string[] opts)
        {
            return -1;
        }

        private int Cmd_StatDump(string[] args, string[] opts)
        {
            return -1;
        }

        private string TryGetHash(string tok)
        {
            if (!long.TryParse(tok, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long resultant))
                return tok;
            if (t8_qword.TryGetValue((ulong)resultant, out string dehashed)) return dehashed;
            return tok;
        }

        private int Cmd_Exit(string[] args, string[] opts)
        {
            Environment.Exit(0);
            return 0;
        }

        private int Cmd_ToggleNoClear(string[] args, string[] opts)
        {
            ClearHistory = !ClearHistory;

            Console.WriteLine($"Console history {(!ClearHistory ? "enabled" : "disabled")}.");

            return 0;
        }

        private int Cmd_MapFileNS(string[] args, string[] opts)
        {
            return -1;
        }

        private int Cmd_IncludeMapper(string[] args, string[] opts)
        {
            return -1;
        }

        private class SourceTokenDef
        {
            public string FilePath;
            public int LineStart;
            public int LineEnd;
            public int CharStart;
            public int CharEnd;
            public Dictionary<int, (int CStart, int CEnd)> LineMappings = new();
        }

        private int Cmd_Compile(string[] args, string[] opts)
        {

            List<string> conditionalSymbols = new();
            string replaceScript = null;
            string scriptLocation = args.Length > 0 ? args[0] : "scripts";

            Platforms platform = Platforms.PC;
            Games game = Games.T7;
            Hotmode hot = Hotmode.none;
            bool noruntime = false;
            bool buildScript = false;
            bool compileOnly = false;

            foreach (string opt in opts)
            {
                if (opt == "--build")
                {
                    buildScript = true;
                }
                else if (opt == "--compile")
                {
                    compileOnly = true;
                }
                else if (opt.Length > 2 && opt[1] == 'D')
                { // -Dsomething
                    conditionalSymbols.Add(opt.Substring(2));
                }
            }

            if (args.Length > 1)
            {
                try
                {
                    game = (Games)Enum.Parse(typeof(Games), args[1], true);
                }
                catch { }
            }

            string outName = "compiled";
            if (File.Exists("gsc.conf"))
            {
                foreach (string line in File.ReadAllLines("gsc.conf"))
                {
                    if (line.Trim().StartsWith("#")) continue;
                    var split = line.Trim().Split('=');
                    if (split.Length < 2) continue;
                    switch (split[0].ToLower().Trim())
                    {
                        case "symbols":
                            foreach (string token in split[1].Trim().Split(','))
                            {
                                conditionalSymbols.Add(token);
                            }
                            break;
                        case "script":
                            replaceScript = split[1].ToLower().Trim().Replace("\\", "/");
                            break;
                        case "scriptlocation":
                            scriptLocation = split[1];
                            break;
                        case "file":
                            outName = split[1].ToLower().Trim();
                            break;
                        case "game":
                            if (!Enum.TryParse(split[1].ToLower().Trim().Replace("\\", "/"), true, out game))
                            {
                                game = Games.T7;
                            }
                            break;
                        case "hot":
                            if (!Enum.TryParse(split[1].ToLower().Trim(), true, out hot))
                            {
                                hot = Hotmode.none;
                            }
                            break;
                        case "noruntime":
                            noruntime = split[1].ToLower().Trim() == "true";
                            break;
                    }
                }
            }

            if (!Directory.Exists(scriptLocation))
                return Error($"Script location is either not a directory or does not exist {args[0]}");

            bool isT7 = game == Games.T7;

            //if (args.Length > 1)
            //{
            //    if(!Enum.TryParse(args[1], true, out game))
            //    {
            //        game = Games.T7;
            //    }
            //}

            string source = "";
            CompiledCode code;
            List<SourceTokenDef> SourceTokens = new();
            StringBuilder sb = new();
            int CurrentLineCount = 0;
            int CurrentCharCount = 0;
            foreach (string f in Directory.GetFiles(scriptLocation, "*.gsc", SearchOption.AllDirectories))
            {
                var CurrentSource = new SourceTokenDef
                {
                    FilePath = f.Replace(scriptLocation, "").Substring(1).Replace("\\", "/"),
                    LineStart = CurrentLineCount,
                    CharStart = CurrentCharCount
                };
                foreach (var line in File.ReadAllLines(f))
                {
                    CurrentSource.LineMappings[CurrentLineCount] = (CurrentCharCount, CurrentCharCount + line.Length + 1);
                    sb.Append(line);
                    sb.Append("\n");
                    CurrentLineCount += 1;
                    CurrentCharCount += line.Length + 1; // + \n
                }
                CurrentSource.LineEnd = CurrentLineCount;
                CurrentSource.CharEnd = CurrentCharCount;
                // Console.WriteLine($"{CurrentSource.FilePath} start {CurrentSource.LineStart} end {CurrentSource.LineEnd}");
                SourceTokens.Add(CurrentSource);
                sb.Append("\n"); // remember that this is here because its going to fuck up irony
            end_loop:;
            }

            replaceScript ??= (isT7 ? @"scripts/shared/duplicaterender_mgr.gsc" : @"scripts/zm_common/load.gsc");
            source = sb.ToString();
            var ppc = new ConditionalBlocks();
            conditionalSymbols.Add(isT7 ? "BO3" : "BO4");
            ppc.LoadConditionalTokens(conditionalSymbols);

            try
            {
                source = ppc.ParseSource(source);
            }
            catch (CBSyntaxException e)
            {
                int errorCharPos = e.ErrorPosition;
                int numLineBreaks = 0;
                foreach (var stok in SourceTokens)
                {
                    do
                    {
                        if (errorCharPos < stok.CharStart || errorCharPos > stok.CharEnd)
                        {
                            break; // havent reached the target index set yet
                        }
                        // now we have the source file we want
                        errorCharPos -= numLineBreaks; // adjust for inserted linebreaks between files
                        foreach (var line in stok.LineMappings)
                        {
                            var (CStart, CEnd) = line.Value;
                            if (errorCharPos < CStart || errorCharPos > CEnd)
                            {
                                continue; // havent found the index we want yet
                            }
                            // found the target line
                            return Error($"{e.Message} in scripts/{stok.FilePath} at line {line.Key - stok.LineStart}, position {errorCharPos - CStart}");
                        }
                    }
                    while (false);
                    numLineBreaks++;
                }
                return Error(e.Message);
            }

            code = Compiler.Compile(platform, game, Modes.MP, false, source);
            if (code.Error != null && code.Error.Length > 0)
            {
                if (code.Error.LastIndexOf("line=") < 0)
                {
                    return Error(code.Error);
                }
                int iStart = code.Error.LastIndexOf("line=") + "line=".Length;
                int iLength = code.Error.LastIndexOf("]") - iStart;
                int line = int.Parse(code.Error.Substring(iStart, iLength));
                // Console.WriteLine(code.Error + " :: " + line);
                foreach (var stok in SourceTokens)
                {
                    do
                    {
                        if (stok.LineStart <= line && stok.LineEnd >= line)
                        {
                            return Error($"Syntax error in scripts/{stok.FilePath} around line {line - stok.LineStart + 1}");
                        }
                    }
                    while (false);
                    line--; // acccount for linebreaks appended to each file
                }
                return Error(code.Error);
            }

            if (code.StubbedScript != null)
            {
                File.WriteAllBytes($"{outName}.stub.gscc", code.StubScriptData);
            }

            string cpath = $"{outName}.{(code.RequiresGSI ? "gsic" : "gscc")}";
            File.WriteAllBytes(cpath, code.CompiledScript);
            string hpath = "hashes.txt";
            StringBuilder hashes = new();
            foreach (var kvp in code.HashMap)
            {
                hashes.AppendLine($"0x{kvp.Key:X}, {kvp.Value}");
            }
            File.WriteAllText(hpath, hashes.ToString());

            if (code.OpcodeEmissions != null)
            {
                byte[] opsRaw = new byte[code.OpcodeEmissions.Count * 4];
                for (int i = 0; i < code.OpcodeEmissions.Count; i++)
                {
                    BitConverter.GetBytes(code.OpcodeEmissions[i]).CopyTo(opsRaw, i * 4);
                }
                File.WriteAllBytes($"{outName}.omap", opsRaw);
            }

            Success(cpath);
            if (compileOnly)
            {
                return Success("Script compiled.");
            }
            else if (buildScript)
            {
                Success("Script compiled. Injecting...");
            }
            else
            {
                Success("Script compiled. Press I to inject or anything else to continue");

                if (Console.ReadKey(true).Key != ConsoleKey.I)
                    return 0;
            }

            byte[] data = code.CompiledScript;

            PointerEx injresult = InjectScript(replaceScript, code.CompiledScript, game, hot, noruntime);
            Console.WriteLine();
            Console.ForegroundColor = !injresult ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"\t[{replaceScript}]: {(!injresult ? "Injected" : $"Failed to Inject ({injresult:X})")}\n");

            if (!injresult && hot == Hotmode.none)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Press any key to reset gsc parsetree... If in game, you are probably going to crash.\n");
                Console.ForegroundColor = ConsoleColor.Yellow;

                Console.ReadKey(true);
                NoExcept(FreeActiveScript);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\tScript parsetree has been reset\n");
                Console.ForegroundColor = ConsoleColor.White;
            }

            return 0;
        }

        private void FreeActiveScript()
        {
            switch (LastGameInjected)
            {
                case Games.T7:
                    NoExcept(FreeT7Script);
                    break;
                case Games.T8:
                    NoExcept(FreeT8Script);
                    break;
            }
        }

        private void NoExcept(Action a)
        {
            try
            {
                a();
            }
            catch { }
        }

        private Games LastGameInjected;
        private PointerEx llpModifiedSPTStruct = 0;
        private PointerEx llpOriginalBuffer;
        private int OriginalSourceChecksum;
        private int InjectedBuffSize;
        private T7SPT InjectedScript;
        private int OriginalPID = 0;
        private int InjectScript(string replacePath, byte[] buffer, Games game, Hotmode hot, bool noruntime)
        {
            LastGameInjected = game;
            switch (game)
            {
                case Games.T7: return InjectT7(replacePath, buffer, hot, noruntime);
                case Games.T8: return InjectT8(replacePath, buffer);
                case Games.T6:
                    break;
                default:
                    break;
            }
            return 1;
        }

        private class GSICInfo
        {
            public List<T7ScriptObject.ScriptDetour> Detours = new();

            public byte[] PackDetours()
            {
                List<byte> data = new();
                foreach (var detour in Detours)
                {
                    data.AddRange(detour.Serialize());
                }
                return data.ToArray();
            }
        }

        private class GSICInfoT8
        {
            public List<T89ScriptObject.ScriptDetour> Detours = new();

            public byte[] PackDetours()
            {
                List<byte> data = new();
                foreach (var detour in Detours)
                {
                    data.AddRange(detour.Serialize());
                }
                return data.ToArray();
            }
        }

        public enum Hotmode { none, csc, gsc }

        private int InjectT7(string replacePath, byte[] buffer, Hotmode hot, bool noruntime)
        {
            NoExcept(FreeT7Script);
            GSICInfo gsi = null;
            if (BitConverter.ToInt64(buffer, 0) != 0x1C000A0D43534780)
            {
                string preamble = Encoding.ASCII.GetString(buffer.Take(4).ToArray());
                if (preamble != "GSIC")
                {
                    return Error("Script is not a valid compiled script. Please use a script compiled for Black Ops III.");
                }
                using (MemoryStream ms = new(buffer))
                using (BinaryReader reader = new(ms))
                {
                    T7ScriptObject.GSIFields currentField = T7ScriptObject.GSIFields.Detours;
                    reader.BaseStream.Position += 4;
                    gsi = new GSICInfo();
                    for (int numFields = reader.ReadInt32(); numFields > 0; numFields--)
                    {
                        currentField = (T7ScriptObject.GSIFields)reader.ReadInt32();
                        switch (currentField)
                        {
                            case T7ScriptObject.GSIFields.Detours:
                                int numdetours = reader.ReadInt32();
                                for (int j = 0; j < numdetours; j++)
                                {
                                    T7ScriptObject.ScriptDetour detour = new();
                                    detour.Deserialize(reader);
                                    gsi.Detours.Add(detour);
                                }
                                break;
                        }
                    }
                    buffer = buffer.Skip((int)reader.BaseStream.Position).ToArray();
                }
                if (BitConverter.ToInt64(buffer, 0) != 0x1C000A0D43534780)
                {
                    return Error("Script is not a valid compiled script. Please use a script compiled for Black Ops III.");
                }
            }
            ProcessEx bo3 = T7ProcessName;
            if (bo3 == null)
            {
                return Error("No game process found for Black Ops III.");
            }
            bool IsWindowsStore = bo3["GameChat2.dll"] is not null;
            bo3.OpenHandle();
            bo3.SetDefaultCallType(ExCallThreadType.XCTT_QUAPC);
            OriginalPID = bo3.BaseProcess.Id;
            PointerEx off = IsWindowsStore ? 0xF3B1330 : 0x9407AB0;
            Console.WriteLine($"s_assetPool:ScriptParseTree => {bo3["blackops3.exe"][off]}");
            var sptGlob = bo3.GetValue<ulong>(bo3["blackops3.exe"][off]);
            var sptCount = bo3.GetValue<int>(bo3["blackops3.exe"][off + 0x14]);
            var SPTEntries = bo3.GetArray<T7SPT>(sptGlob, sptCount);
            for (int i = 0; i < SPTEntries.Length; i++)
            {
                var entry = SPTEntries[i];
                if (!entry.llpName) continue;
                try
                {
                    // find target
                    var name = bo3.GetString(entry.llpName);
                    if (hot != Hotmode.none || name.ToLower().Trim().Replace("\\", "/") == replacePath.ToLower().Trim().Replace("\\", "/"))
                    {
                        // cache target info
                        if (hot == Hotmode.none)
                        {
                            llpModifiedSPTStruct = (ulong)(i * Marshal.SizeOf(typeof(T7SPT))) + sptGlob;
                            llpOriginalBuffer = entry.lpBuffer;
                            OriginalSourceChecksum = bo3.GetValue<int>(llpOriginalBuffer + 0x8);
                        }


                        // patch script into memory
                        entry.lpBuffer = bo3.QuickAlloc(buffer.Length);
                        BitConverter.GetBytes(OriginalSourceChecksum).CopyTo(buffer, 0x8);
                        bo3.SetBytes(entry.lpBuffer, buffer);

                        // patch spt struct
                        if (hot == Hotmode.none)
                        {
                            bo3.SetStruct(llpModifiedSPTStruct, entry);

                            // cache the struct data for uninjection
                            InjectedScript = entry;
                            InjectedBuffSize = buffer.Length;
                        }

                        if (!noruntime)
                        {
                            try
                            {
                                string exeFilePath = Assembly.GetExecutingAssembly().Location;
                                var result = bo3.Call<long>(bo3.GetProcAddress(@"kernel32.dll", @"LoadLibraryA"), Path.Combine(Path.GetDirectoryName(exeFilePath), "t7cinternal.dll"));
                                Console.WriteLine($"LoadLibrary Result => {result:X}");

                                bo3.Refresh();
                                if (result <= 0)
                                {
                                    return (int)result;
                                }

                                bo3.Call<VOID>(bo3.GetProcAddress(@"t7cinternal.dll", @"RemoveDetours"));
                                if (gsi != null)
                                {
                                    // detours
                                    if (gsi.Detours.Count > 0)
                                    {
                                        bo3.Call<VOID>(bo3.GetProcAddress(@"t7cinternal.dll", @"RegisterDetours"), gsi.PackDetours(), gsi.Detours.Count, (long)entry.lpBuffer);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.ToString());
                                return 3;
                            }
                        }

                        if (hot != Hotmode.none)
                        {
                            string exeFilePath = Assembly.GetExecutingAssembly().Location;
                            var pe = new System.PEStructures.PEImage(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(exeFilePath), "t7cinternal.dll")));
                            var targetExport = IsWindowsStore ? "HotloadScript_WinStore" : "HotloadScript_Steam";
                            var targetHigh = IsWindowsStore ? "HotloadScript_WinStore_Trail" : "HotloadScript_Steam_Trail";

                            var internalHndl = System.Evasion.ModuleMapper.MapModuleToMemory(Path.Combine(Path.GetDirectoryName(exeFilePath), "t7cinternal.dll")).ModuleBase;
                            var expLo = (PointerEx)System.Evasion.ModuleMapper.GetExportAddress(internalHndl, targetExport);
                            var expHi = (PointerEx)System.Evasion.ModuleMapper.GetExportAddress(internalHndl, targetHigh);

                            byte[] hot_fn = new byte[expHi - expLo];
                            Marshal.Copy(expLo, hot_fn, 0, expHi - expLo);

                            var hFnHotload = bo3.QuickAlloc(hot_fn.Length, true);
                            bo3.SetBytes(hFnHotload, hot_fn);

                            byte[] error_data = new byte[4];
                            try
                            {
                                bool result = bo3.Call<bool>(hFnHotload, entry.lpBuffer, hot == Hotmode.csc ? 1 : 0, error_data);

                                if (!result)
                                {
                                    int error = BitConverter.ToInt32(error_data, 0);
                                    switch (error)
                                    {
                                        case 1:
                                            Console.WriteLine("HOTLOAD: Invalid script");
                                            break;
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Successfully hotloaded script!");
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.ToString());
                            }
                        }

                        return 0;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    continue;
                }
            }
            bo3.CloseHandle();
            return 2;
        }

        private T8InjectCache InjectCache;

        private int InjectT8(string replacePath, byte[] buffer)
        {
            NoExcept(FreeT8Script);
            GSICInfoT8 gsi = null;
            if (BitConverter.ToInt64(buffer, 0) != 0x36000A0D43534780)
            {
                string preamble = Encoding.ASCII.GetString(buffer.Take(4).ToArray());
                if (preamble != "GSIC")
                {
                    return Error("Script is not a valid compiled script. Please use a script compiled for Black Ops IIII.");
                }
                using (MemoryStream ms = new(buffer))
                using (BinaryReader reader = new(ms))
                {
                    T7ScriptObject.GSIFields currentField = T7ScriptObject.GSIFields.Detours;
                    reader.BaseStream.Position += 4;
                    gsi = new GSICInfoT8();
                    for (int numFields = reader.ReadInt32(); numFields > 0; numFields--)
                    {
                        currentField = (T7ScriptObject.GSIFields)reader.ReadInt32();
                        switch (currentField)
                        {
                            case T7ScriptObject.GSIFields.Detours:
                                int numdetours = reader.ReadInt32();
                                for (int j = 0; j < numdetours; j++)
                                {
                                    T89ScriptObject.ScriptDetour detour = new();
                                    detour.Deserialize(reader);
                                    gsi.Detours.Add(detour);
                                }
                                break;
                        }
                    }
                    buffer = buffer.Skip((int)reader.BaseStream.Position).ToArray();
                }
                if (BitConverter.ToInt64(buffer, 0) != 0x36000A0D43534780)
                {
                    return Error("Script is not a valid compiled script. Please use a script compiled for Black Ops 4.");
                }
            }
            ProcessEx bo4 = "blackops4";
            if (bo4 is null)
            {
                return Error("No game process found for Black Ops 4.");
            }

            bo4.OpenHandle();
            OriginalPID = bo4.BaseProcess.Id;
            Console.WriteLine($"s_assetPool:ScriptParseTree => {bo4[0x912ABB0]}");
            var sptGlob = bo4.GetValue<ulong>(bo4[0x912ABB0]);
            var sptCount = bo4.GetValue<int>(bo4[0x912ABB0 + 0x14]);
            var SPTEntries = bo4.GetArray<T8SPT>(sptGlob, sptCount);
            replacePath = replacePath.ToLower().Trim().Replace("\\", "/");
            var surrogateScript = T8s64Hash(replacePath); // script we are hooking
            var targetScript = 0x124CECFF7280BE52; // script we are replacing
            InjectCache.hSurrogate = 0;
            InjectCache.hTarget = 0;

            for (int i = 0; i < SPTEntries.Length; i++)
            {
                var spt = SPTEntries[i];
                if (spt.ScriptName == surrogateScript)
                {
                    InjectCache.Surrogate = spt;
                    InjectCache.hSurrogate = sptGlob + (ulong)(i * Marshal.SizeOf(typeof(T8SPT)));
                }
                if (spt.ScriptName == targetScript)
                {
                    InjectCache.Target = spt;
                    InjectCache.hTarget = sptGlob + (ulong)(i * Marshal.SizeOf(typeof(T8SPT)));
                }
                if (InjectCache.hSurrogate && InjectCache.hTarget)
                {
                    break;
                }
            }

            try
            {
                if (!InjectCache.hSurrogate || !InjectCache.hTarget)
                {
                    return Error("Unable to identify critical injection information. Double check your script path, and try restarting the game. Make sure you are injecting in the pregame lobby.");
                }

                int includeOff = 0x58;
                int tableOff = 0x18;

                // patch include
                byte includeCount = bo4.GetValue<byte>(InjectCache.Surrogate.Buffer + includeOff);
                PointerEx includeTable = InjectCache.Surrogate.Buffer + bo4.GetValue<int>(InjectCache.Surrogate.Buffer + tableOff);
                for (int i = 0; i < includeCount; i++)
                {
                    if (bo4.GetValue<long>(includeTable + i * 8) == targetScript)
                    {
                        goto patchBuff;
                    }
                }
                bo4.SetValue(includeTable + includeCount * 8, targetScript);
                bo4.SetValue(InjectCache.Surrogate.Buffer + includeOff, (byte)(includeCount + 1));

            patchBuff:
                bo4.GetBytes(InjectCache.Target.Buffer + 0x8, 8).CopyTo(buffer, 0x8); // crc32
                InjectCache.hBuffer = bo4.QuickAlloc(buffer.Length); // space
                bo4.SetBytes(InjectCache.hBuffer, buffer); // write to proc
                bo4.SetValue<long>(InjectCache.hTarget + 0x10, InjectCache.hBuffer); // buffer pointer redirect
                InjectCache.Pid = bo4.BaseProcess.Id;
                InjectCache.BufferSize = buffer.Length;
                InjectCache.IsInjected = true;

                try
                {
                    string exeFilePath = Assembly.GetExecutingAssembly().Location;
                    //var result = bo4.Call<long>(bo4.GetProcAddress(@"kernel32.dll", @"LoadLibraryA"), Path.Combine(Path.GetDirectoryName(exeFilePath), "t8cinternal.dll"));
                    //bo4.Refresh();

                    //if (result == 0)
                    //{
                    //    return 4;
                    //}

                    //bo4.Call<VOID>(bo4.GetProcAddress(@"t8cinternal.dll", @"RemoveDetours"));
                    //if (gsi != null)
                    //{
                    //    // detours
                    //    if (gsi.Detours.Count > 0)
                    //    {
                    //        bo4.Call<VOID>(bo4.GetProcAddress(@"t8cinternal.dll", @"RegisterDetours"), gsi.PackDetours(), gsi.Detours.Count, (long)InjectCache.hBuffer);
                    //    }
                    //}
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    return 3;
                }
            }
            catch
            {
                return Error("Unknown error while injecting...");
            }
            finally
            {
                bo4.CloseHandle();
            }

            return 0;
        }

        private void FreeT8Script()
        {
            if (!InjectCache.IsInjected)
            {
                return;
            }

            ProcessEx bo4 = "blackops4";
            if (bo4 is null)
            {
                return;
            }

            if (bo4.BaseProcess.Id != InjectCache.Pid)
            {
                return;
            }

            bo4.OpenHandle();

            try
            {
                // free allocated space
                ProcessEx.VirtualFreeEx(bo4.Handle, InjectCache.hBuffer, (uint)InjectCache.BufferSize, (int)EnvironmentEx.FreeType.Release);

                // Patch spt struct
                bo4.SetStruct(InjectCache.hTarget, InjectCache.Target);

                // Reset hooked detours
                // bo4.Call<VOID>(bo4.GetProcAddress(@"t8cinternal.dll", @"RemoveDetours"));
            }
            finally
            {
                bo4.CloseHandle();
            }
        }

        private void FreeT7Script()
        {
            if (!llpModifiedSPTStruct) return;
            ProcessEx bo3 = T7ProcessName;
            if (bo3 == null) return;
            if (bo3.BaseProcess.Id != OriginalPID) return;
            bo3.OpenHandle();

            // free allocated space
            ProcessEx.VirtualFreeEx(bo3.Handle, InjectedScript.lpBuffer, (uint)InjectedBuffSize, (int)EnvironmentEx.FreeType.Release);

            // Patch spt struct
            InjectedScript.lpBuffer = llpOriginalBuffer;
            bo3.SetStruct(llpModifiedSPTStruct, InjectedScript);

            // Reset hooked detours
            bo3.Call<VOID>(bo3.GetProcAddress(@"t7cinternal.dll", @"RemoveDetours"));

            bo3.CloseHandle();
        }

        private struct T8InjectCache
        {
            public T8SPT Surrogate;
            public T8SPT Target;
            public PointerEx hSurrogate;
            public PointerEx hTarget;
            public PointerEx hBuffer;
            public int BufferSize;
            public int Pid;
            public bool IsInjected;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        struct T7SPT
        {
            public PointerEx llpName;     //00
            public int BuffSize;       //08
            public int Pad;
            public PointerEx lpBuffer;//10
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct T8SPT
        {
            public PointerEx ScriptName;
            public long pad0;
            public PointerEx Buffer;
            public int Size;
            public int Unk0;
        };

        private int Cmd_Dump(string[] args, string[] opts)
        {
            return -1;
        }

        #endregion

        private static readonly HashSet<string> HashIdentifierPrefixes = new() { "script_" };
        public static ulong T8s64Hash(string input)
        {
            input = input.ToLower();

            //if input starts with func_, var_, or hash_, use the provided hash (if possible)
            foreach (string hashprefix in HashIdentifierPrefixes)
            {
                if (input[0] != hashprefix[0] || input.Length <= hashprefix.Length)
                    continue;
                if (!input.StartsWith(hashprefix))
                    continue;
                if (!ulong.TryParse(input.Substring(hashprefix.Length), NumberStyles.HexNumber, default, out ulong result))
                    break;
                return result;
            }

            return 0x7FFFFFFFFFFFFFFF & HashFNV1a(Encoding.ASCII.GetBytes(input));
        }

        uint Com_Hash(string Input, uint IV, uint XORKEY)
        {
            uint hash = IV;

            foreach (char c in Input)
                hash = (char.ToLower(c) ^ hash) * XORKEY;

            hash *= XORKEY;

            return hash;
        }

        public static ulong HashFNV1a(byte[] bytes, ulong fnv64Offset = 14695981039346656037, ulong fnv64Prime = 0x100000001b3)
        {
            ulong hash = fnv64Offset;

            for (var i = 0; i < bytes.Length; i++)
            {
                hash ^= bytes[i];
                hash *= fnv64Prime;
            }

            return hash;
        }

        private ulong FS_HashFileName(string input, ulong hashSize)
        {
            return 0;
        }

        private static void SerializeMetaOps()
        {
        }

        private static readonly Dictionary<byte, ScriptOpCode> XboxCodes = null;

    }
}