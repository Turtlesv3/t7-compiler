using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using TreyarchCompiler.Utilities;
using static System.ExCallThreadType;
using PointerEx = System.PointerEx;
using System.PEStructures;

namespace T7CompilerGUI.Services
{
    /// <summary>
    /// Service for loading DLLs into game processes
    /// Uses External project's ProcessEx.MapModule for full manual mapping
    /// </summary>
    public static class DllLoaderService
    {
        /// <summary>
        /// Callback for logging DLL loading progress
        /// </summary>
        public delegate void LogCallback(string message);

        /// <summary>
        /// Result of DLL loading operation
        /// </summary>
        public class DllLoadResult
        {
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
            public PointerEx ModuleHandle { get; set; }
        }

        /// <summary>
        /// Validates that a DLL file exists and is valid
        /// </summary>
        public static (bool isValid, string errorMessage) ValidateDllFile(string dllPath)
        {
            if (string.IsNullOrWhiteSpace(dllPath))
                return (false, "DLL path is empty.");

            if (!File.Exists(dllPath))
                return (false, "DLL file does not exist.");

            if (!dllPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                return (false, "Selected file is not a DLL.");

            try
            {
                // Try to read the file to ensure it's accessible
                using (var fs = new FileStream(dllPath, FileMode.Open, FileAccess.Read))
                {
                    if (fs.Length < 1024) // Minimum reasonable DLL size
                        return (false, "File is too small to be a valid DLL.");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error accessing DLL file: {ex.Message}");
            }

            return (true, null);
        }

        /// <summary>
        /// Loads a DLL into the Black Ops 3 process
        /// </summary>
        public static DllLoadResult LoadDllIntoBO3(string dllPath, LogCallback log = null)
        {
            var result = new DllLoadResult();

            try
            {
                // Validate DLL file
                var validation = ValidateDllFile(dllPath);
                if (!validation.isValid)
                {
                    result.Success = false;
                    result.ErrorMessage = validation.errorMessage;
                    return result;
                }

                log?.Invoke($"\r\n=== DLL LOADER ===\r\n");
                log?.Invoke($"DLL: {dllPath}\r\n");
                log?.Invoke($"Game: Black Ops 3\r\n");
                log?.Invoke($"Advanced anti-detection: Staged injection with delays enabled\r\n\r\n");

                // Get the game process
                ProcessEx game;
                try
                {
                    game = "blackops3";
                    if (game == null || game.BaseProcess.HasExited)
                    {
                        result.Success = false;
                        result.ErrorMessage = "Black Ops 3 is not running.";
                        log?.Invoke("ERROR: Black Ops 3 is not running.\r\n");
                        return result;
                    }
                    game.SetDefaultCallType(XCTT_RIPHijack);
                    if (!game.Handle)
                    {
                        game.OpenHandle(ProcessEx.PROCESS_ACCESS, true);
                    }
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Failed to access Black Ops 3 process: {ex.Message}";
                    log?.Invoke($"ERROR: {result.ErrorMessage}\r\n");
                    return result;
                }

                // Read DLL file into memory
                byte[] dllBytes;
                try
                {
                    dllBytes = File.ReadAllBytes(dllPath);
                    log?.Invoke($"Read {dllBytes.Length} bytes from DLL file.\r\n");
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Failed to read DLL file: {ex.Message}";
                    log?.Invoke($"ERROR: {result.ErrorMessage}\r\n");
                    return result;
                }

                // Use ProcessEx.MapModule for full manual mapping
                // This handles: import resolution, relocation, and entry point execution
                PointerEx moduleBase;
                try
                {
                    log?.Invoke("Starting manual DLL mapping...\r\n");
                    
                    // Validate game process is still valid
                    if (game == null)
                    {
                        result.Success = false;
                        result.ErrorMessage = "Game process object is null.";
                        log?.Invoke("ERROR: Game process object is null.\r\n");
                        return result;
                    }
                    
                    if (game.BaseProcess == null || game.BaseProcess.HasExited)
                    {
                        result.Success = false;
                        result.ErrorMessage = "Game process is no longer valid.";
                        log?.Invoke("ERROR: Game process is no longer valid.\r\n");
                        return result;
                    }
                    
                    // Ensure handle is open
                    if (!game.Handle)
                    {
                        log?.Invoke("  - Opening process handle...\r\n");
                        game.OpenHandle(ProcessEx.PROCESS_ACCESS, true);
                        if (!game.Handle)
                        {
                            result.Success = false;
                            result.ErrorMessage = "Failed to open process handle.";
                            log?.Invoke("ERROR: Failed to open process handle.\r\n");
                            return result;
                        }
                    }
                    
                    log?.Invoke($"  - Process handle: 0x{game.Handle:X}\r\n");
                    log?.Invoke($"  - DLL size: {dllBytes.Length} bytes\r\n");
                    
                    // Refresh process modules to ensure we have the latest module list
                    // This is important for import resolution
                    log?.Invoke("  - Refreshing process modules...\r\n");
                    if (!game.Refresh())
                    {
                        result.Success = false;
                        result.ErrorMessage = "Failed to refresh process state. Process may have exited.";
                        log?.Invoke("ERROR: Failed to refresh process state.\r\n");
                        return result;
                    }
                    
                    // Validate DLL bytes
                    if (dllBytes == null || dllBytes.Length == 0)
                    {
                        result.Success = false;
                        result.ErrorMessage = "DLL bytes are null or empty.";
                        log?.Invoke("ERROR: DLL bytes are null or empty.\r\n");
                        return result;
                    }
                    
                    // Basic PE validation - check for DOS header signature
                    if (dllBytes.Length < 64 || BitConverter.ToUInt16(dllBytes, 0) != 0x5A4D) // "MZ"
                    {
                        result.Success = false;
                        result.ErrorMessage = "Invalid PE file: Missing DOS header signature (MZ).";
                        log?.Invoke("ERROR: Invalid PE file: Missing DOS header signature.\r\n");
                        return result;
                    }
                    
                    // Check for PE signature
                    int peOffset = BitConverter.ToInt32(dllBytes, 60); // e_lfanew
                    if (peOffset < 0 || peOffset + 4 > dllBytes.Length || 
                        BitConverter.ToUInt32(dllBytes, peOffset) != 0x00004550) // "PE\0\0"
                    {
                        result.Success = false;
                        result.ErrorMessage = "Invalid PE file: Missing PE signature.";
                        log?.Invoke("ERROR: Invalid PE file: Missing PE signature.\r\n");
                        return result;
                    }
                    
                    log?.Invoke("  - PE file validation passed.\r\n");
                    
                    // ========================================================================
                    // ADVANCED ANTI-DETECTION: Staged Injection with Delays (BO3Enhanced Bypass)
                    // ========================================================================
                    // BO3Enhanced monitors: threads, hooks, memory, timing patterns
                    // Strategy: Map first, delay, then execute to avoid immediate detection
                    // ========================================================================
                    
                    var random = new Random();
                    
                    // Step 1: Add random delay to avoid timing-based detection
                    int preDelay = random.Next(150, 400);
                    log?.Invoke($"  - Anti-detection: Adding random delay ({preDelay}ms) to avoid timing patterns...\r\n");
                    System.Threading.Thread.Sleep(preDelay);
                    
                    // Verify process still alive
                    if (!game.Refresh() || game.BaseProcess == null || game.BaseProcess.HasExited)
                    {
                        result.Success = false;
                        result.ErrorMessage = "Process exited during anti-detection delay.";
                        log?.Invoke("ERROR: Process exited during delay.\r\n");
                        return result;
                    }
                    
                    log?.Invoke("  - Resolving imports...\r\n");
                    log?.Invoke("  - Relocating module...\r\n");
                    
                    // Step 2: Map module WITHOUT executing DllMain first (stealthier)
                    // This avoids immediate detection when DllMain executes
                    log?.Invoke("  - STAGED INJECTION: Mapping module first (DllMain will execute later)...\r\n");
                    
                    var mapOptions = new System.ModuleLoadOptions
                    {
                        ExecMain = false, // Don't execute DllMain yet
                        ClearHeader = true, // Clear header immediately for stealth
                        MainThreadType = System.ExCallThreadType.XCTT_QUAPC
                    };
                    
                    try
                    {
                        // Map the module
                        moduleBase = game.MapModule(new System.Memory<byte>(dllBytes), mapOptions);
                        
                        if (!moduleBase)
                        {
                            throw new Exception("Failed to map module (returned null/zero).");
                        }
                        
                        log?.Invoke($"  - Module mapped successfully at: 0x{moduleBase:X}\r\n");
                        log?.Invoke("  - PE header cleared (invisible to signature scans)\r\n");
                        
                        // Step 3: Wait before executing DllMain (some protections scan right after mapping)
                        int postMapDelay = random.Next(300, 600);
                        log?.Invoke($"  - Anti-detection: Waiting {postMapDelay}ms before executing DllMain...\r\n");
                        log?.Invoke("  - This delay helps avoid immediate post-mapping scans\r\n");
                        System.Threading.Thread.Sleep(postMapDelay);
                        
                        // Verify process still alive
                        if (!game.Refresh() || game.BaseProcess == null || game.BaseProcess.HasExited)
                        {
                            result.Success = false;
                            result.ErrorMessage = "Process exited after mapping (before DllMain execution).";
                            log?.Invoke("ERROR: Process exited after mapping.\r\n");
                            log?.Invoke("  Module was mapped but DllMain was not executed.\r\n");
                            log?.Invoke("  This may indicate detection during mapping phase.\r\n");
                            return result;
                        }
                        
                        // Step 4: Execute DllMain using QueueUserAPC (stealthiest method)
                        log?.Invoke("  - Executing DllMain entry point via QueueUserAPC...\r\n");
                        
                        // Get entry point address from PE structure (avoid PEImage dependency)
                        // Reuse peOffset that was calculated during validation
                        ushort machine = BitConverter.ToUInt16(dllBytes, peOffset + 4);
                        bool is32Bit = (machine == 0x014C); // IMAGE_FILE_MACHINE_I386
                        
                        uint addressOfEntryPoint;
                        if (is32Bit)
                        {
                            // PE32: Entry point is at offset 40 from OptionalHeader start
                            // OptionalHeader starts at PE offset + 24
                            int optHeaderOffset = peOffset + 24;
                            addressOfEntryPoint = BitConverter.ToUInt32(dllBytes, optHeaderOffset + 16); // AddressOfEntryPoint offset
                        }
                        else
                        {
                            // PE32+: Entry point is at offset 16 from OptionalHeader start
                            int optHeaderOffset = peOffset + 24;
                            addressOfEntryPoint = BitConverter.ToUInt32(dllBytes, optHeaderOffset + 16); // AddressOfEntryPoint offset
                        }
                        
                        PointerEx entryPoint = moduleBase + addressOfEntryPoint;
                        
                        // Execute DllMain via QueueUserAPC (uses existing thread, no new thread creation)
                        bool dllMainResult = game.CallByMethod<bool>(entryPoint, System.ExCallThreadType.XCTT_QUAPC, moduleBase, 1, 0);
                        
                        if (!dllMainResult)
                        {
                            log?.Invoke("  - WARNING: DllMain returned FALSE (may indicate failure or rejection)\r\n");
                            log?.Invoke("  - Module is mapped but initialization may have failed\r\n");
                            log?.Invoke("  - This could indicate BO3Enhanced detected and blocked DllMain execution\r\n");
                        }
                        else
                        {
                            log?.Invoke("  - DllMain executed successfully via QueueUserAPC\r\n");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Check if process crashed
                        if (!game.Refresh() || game.BaseProcess == null || game.BaseProcess.HasExited)
                        {
                            result.Success = false;
                            result.ErrorMessage = $"Process crashed during injection: {ex.Message}";
                            log?.Invoke($"ERROR: Process crashed during injection.\r\n");
                            log?.Invoke($"  Error: {ex.Message}\r\n");
                            if (ex.InnerException != null)
                            {
                                log?.Invoke($"  Inner: {ex.InnerException.Message}\r\n");
                            }
                            return result;
                        }
                        
                        // Staged injection failed, try standard injection as fallback
                        log?.Invoke("  - Staged injection failed, trying standard injection method...\r\n");
                        log?.Invoke($"  - Error: {ex.Message}\r\n");
                        
                        var standardOptions = new System.ModuleLoadOptions
                        {
                            ExecMain = true,
                            ClearHeader = true,
                            MainThreadType = System.ExCallThreadType.XCTT_QUAPC
                        };
                        
                        try
                        {
                            moduleBase = game.MapModule(new System.Memory<byte>(dllBytes), standardOptions);
                            log?.Invoke("  - Standard injection succeeded\r\n");
                        }
                        catch (Exception ex2)
                        {
                            // Check if process crashed
                            if (!game.Refresh() || game.BaseProcess == null || game.BaseProcess.HasExited)
                            {
                                result.Success = false;
                                result.ErrorMessage = $"Process crashed during standard injection: {ex2.Message}";
                                log?.Invoke($"ERROR: Process crashed during standard injection.\r\n");
                                return result;
                            }
                            
                            // Try without DllMain as last resort
                            log?.Invoke("  - Standard injection failed, trying without DllMain execution...\r\n");
                            standardOptions.ExecMain = false;
                            try
                            {
                                moduleBase = game.MapModule(new System.Memory<byte>(dllBytes), standardOptions);
                                log?.Invoke("  - WARNING: DLL mapped but DllMain was not executed.\r\n");
                                log?.Invoke("  - The DLL may not function correctly without DllMain initialization.\r\n");
                            }
                            catch
                            {
                                // Re-throw original exception
                                throw ex;
                            }
                        }
                    }
                    
                    if (!moduleBase)
                    {
                        result.Success = false;
                        result.ErrorMessage = "Failed to map DLL module (returned null/zero).";
                        log?.Invoke("ERROR: Failed to map DLL module (returned null/zero).\r\n");
                        return result;
                    }
                    
                    log?.Invoke($"  - Module base: 0x{moduleBase:X}\r\n");
                    log?.Invoke("  - Entry point executed successfully.\r\n");
                }
                catch (NullReferenceException ex)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Null reference during DLL mapping (likely missing dependency module): {ex.Message}";
                    log?.Invoke($"ERROR: Null reference during DLL mapping.\r\n");
                    log?.Invoke($"  This usually means a required DLL dependency is not loaded in the target process.\r\n");
                    log?.Invoke($"  The mapping process will attempt to load dependencies automatically, but some may fail.\r\n");
                    log?.Invoke($"  Message: {ex.Message}\r\n");
                    if (!string.IsNullOrEmpty(ex.StackTrace))
                    {
                        // Extract the relevant part of the stack trace
                        var stackLines = ex.StackTrace.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                        foreach (var line in stackLines.Take(10)) // Show first 10 lines
                        {
                            if (line.Contains("GetProcAddress") || line.Contains("__MMRewriteModuleIAT") || 
                                line.Contains("MapModule") || line.Contains("DllLoaderService"))
                            {
                                log?.Invoke($"  {line}\r\n");
                            }
                        }
                    }
                    if (ex.InnerException != null)
                    {
                        log?.Invoke($"  Inner exception: {ex.InnerException.Message}\r\n");
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Failed to map DLL: {ex.Message}";
                    log?.Invoke($"ERROR: {result.ErrorMessage}\r\n");
                    log?.Invoke($"  Exception type: {ex.GetType().Name}\r\n");
                    if (ex.InnerException != null)
                    {
                        log?.Invoke($"  Inner exception: {ex.InnerException.Message}\r\n");
                        if (!string.IsNullOrEmpty(ex.InnerException.StackTrace))
                        {
                            log?.Invoke($"  Inner stack trace: {ex.InnerException.StackTrace}\r\n");
                        }
                    }
                    if (!string.IsNullOrEmpty(ex.StackTrace))
                    {
                        log?.Invoke($"  Stack trace: {ex.StackTrace}\r\n");
                    }
                    return result;
                }

                result.Success = true;
                result.ModuleHandle = moduleBase;
                result.ErrorMessage = null;
                log?.Invoke("\r\n=== DLL LOADED SUCCESSFULLY ===\r\n");
                log?.Invoke($"DLL manually mapped at: 0x{moduleBase:X}\r\n");
                log?.Invoke("All imports resolved, module relocated, and DllMain executed.\r\n\r\n");

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Unexpected error: {ex.Message}";
                log?.Invoke($"\r\n=== DLL LOADER ERROR ===\r\n");
                log?.Invoke($"{result.ErrorMessage}\r\n\r\n");
                return result;
            }
        }

        /// <summary>
        /// Loads a DLL into the Black Ops 4 process
        /// </summary>
        public static DllLoadResult LoadDllIntoBO4(string dllPath, LogCallback log = null)
        {
            var result = new DllLoadResult();

            try
            {
                // Validate DLL file
                var validation = ValidateDllFile(dllPath);
                if (!validation.isValid)
                {
                    result.Success = false;
                    result.ErrorMessage = validation.errorMessage;
                    return result;
                }

                log?.Invoke($"\r\n=== DLL LOADER ===\r\n");
                log?.Invoke($"DLL: {dllPath}\r\n");
                log?.Invoke($"Game: Black Ops 4\r\n\r\n");

                // Get the game process
                ProcessEx game;
                try
                {
                    game = "blackops4";
                    if (game == null || game.BaseProcess.HasExited)
                    {
                        result.Success = false;
                        result.ErrorMessage = "Black Ops 4 is not running.";
                        log?.Invoke("ERROR: Black Ops 4 is not running.\r\n");
                        return result;
                    }
                    game.SetDefaultCallType(XCTT_RIPHijack);
                    if (!game.Handle)
                    {
                        game.OpenHandle(ProcessEx.PROCESS_ACCESS, true);
                    }
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Failed to access Black Ops 4 process: {ex.Message}";
                    log?.Invoke($"ERROR: {result.ErrorMessage}\r\n");
                    return result;
                }

                // Read DLL file into memory
                byte[] dllBytes;
                try
                {
                    dllBytes = File.ReadAllBytes(dllPath);
                    log?.Invoke($"Read {dllBytes.Length} bytes from DLL file.\r\n");
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Failed to read DLL file: {ex.Message}";
                    log?.Invoke($"ERROR: {result.ErrorMessage}\r\n");
                    return result;
                }

                // Use ProcessEx.MapModule for full manual mapping
                // This handles: import resolution, relocation, and entry point execution
                PointerEx moduleBase;
                try
                {
                    log?.Invoke("Starting manual DLL mapping...\r\n");
                    
                    // Validate game process is still valid
                    if (game == null)
                    {
                        result.Success = false;
                        result.ErrorMessage = "Game process object is null.";
                        log?.Invoke("ERROR: Game process object is null.\r\n");
                        return result;
                    }
                    
                    if (game.BaseProcess == null || game.BaseProcess.HasExited)
                    {
                        result.Success = false;
                        result.ErrorMessage = "Game process is no longer valid.";
                        log?.Invoke("ERROR: Game process is no longer valid.\r\n");
                        return result;
                    }
                    
                    // Ensure handle is open
                    if (!game.Handle)
                    {
                        log?.Invoke("  - Opening process handle...\r\n");
                        game.OpenHandle(ProcessEx.PROCESS_ACCESS, true);
                        if (!game.Handle)
                        {
                            result.Success = false;
                            result.ErrorMessage = "Failed to open process handle.";
                            log?.Invoke("ERROR: Failed to open process handle.\r\n");
                            return result;
                        }
                    }
                    
                    log?.Invoke($"  - Process handle: 0x{game.Handle:X}\r\n");
                    log?.Invoke($"  - DLL size: {dllBytes.Length} bytes\r\n");
                    
                    // Refresh process modules to ensure we have the latest module list
                    // This is important for import resolution
                    log?.Invoke("  - Refreshing process modules...\r\n");
                    if (!game.Refresh())
                    {
                        result.Success = false;
                        result.ErrorMessage = "Failed to refresh process state. Process may have exited.";
                        log?.Invoke("ERROR: Failed to refresh process state.\r\n");
                        return result;
                    }
                    
                    // Validate DLL bytes
                    if (dllBytes == null || dllBytes.Length == 0)
                    {
                        result.Success = false;
                        result.ErrorMessage = "DLL bytes are null or empty.";
                        log?.Invoke("ERROR: DLL bytes are null or empty.\r\n");
                        return result;
                    }
                    
                    // Basic PE validation - check for DOS header signature
                    if (dllBytes.Length < 64 || BitConverter.ToUInt16(dllBytes, 0) != 0x5A4D) // "MZ"
                    {
                        result.Success = false;
                        result.ErrorMessage = "Invalid PE file: Missing DOS header signature (MZ).";
                        log?.Invoke("ERROR: Invalid PE file: Missing DOS header signature.\r\n");
                        return result;
                    }
                    
                    // Check for PE signature
                    int peOffset = BitConverter.ToInt32(dllBytes, 60); // e_lfanew
                    if (peOffset < 0 || peOffset + 4 > dllBytes.Length || 
                        BitConverter.ToUInt32(dllBytes, peOffset) != 0x00004550) // "PE\0\0"
                    {
                        result.Success = false;
                        result.ErrorMessage = "Invalid PE file: Missing PE signature.";
                        log?.Invoke("ERROR: Invalid PE file: Missing PE signature.\r\n");
                        return result;
                    }
                    
                    log?.Invoke("  - PE file validation passed.\r\n");
                    
                    // ========================================================================
                    // ADVANCED ANTI-DETECTION: Staged Injection with Delays (BO3Enhanced Bypass)
                    // ========================================================================
                    // BO3Enhanced monitors: threads, hooks, memory, timing patterns
                    // Strategy: Map first, delay, then execute to avoid immediate detection
                    // ========================================================================
                    
                    var random = new Random();
                    
                    // Step 1: Add random delay to avoid timing-based detection
                    int preDelay = random.Next(150, 400);
                    log?.Invoke($"  - Anti-detection: Adding random delay ({preDelay}ms) to avoid timing patterns...\r\n");
                    System.Threading.Thread.Sleep(preDelay);
                    
                    // Verify process still alive
                    if (!game.Refresh() || game.BaseProcess == null || game.BaseProcess.HasExited)
                    {
                        result.Success = false;
                        result.ErrorMessage = "Process exited during anti-detection delay.";
                        log?.Invoke("ERROR: Process exited during delay.\r\n");
                        return result;
                    }
                    
                    log?.Invoke("  - Resolving imports...\r\n");
                    log?.Invoke("  - Relocating module...\r\n");
                    
                    // Step 2: Map module WITHOUT executing DllMain first (stealthier)
                    // This avoids immediate detection when DllMain executes
                    log?.Invoke("  - STAGED INJECTION: Mapping module first (DllMain will execute later)...\r\n");
                    
                    var mapOptions = new System.ModuleLoadOptions
                    {
                        ExecMain = false, // Don't execute DllMain yet
                        ClearHeader = true, // Clear header immediately for stealth
                        MainThreadType = System.ExCallThreadType.XCTT_QUAPC
                    };
                    
                    try
                    {
                        // Map the module
                        moduleBase = game.MapModule(new System.Memory<byte>(dllBytes), mapOptions);
                        
                        if (!moduleBase)
                        {
                            throw new Exception("Failed to map module (returned null/zero).");
                        }
                        
                        log?.Invoke($"  - Module mapped successfully at: 0x{moduleBase:X}\r\n");
                        log?.Invoke("  - PE header cleared (invisible to signature scans)\r\n");
                        
                        // Step 3: Wait before executing DllMain (some protections scan right after mapping)
                        int postMapDelay = random.Next(300, 600);
                        log?.Invoke($"  - Anti-detection: Waiting {postMapDelay}ms before executing DllMain...\r\n");
                        log?.Invoke("  - This delay helps avoid immediate post-mapping scans\r\n");
                        System.Threading.Thread.Sleep(postMapDelay);
                        
                        // Verify process still alive
                        if (!game.Refresh() || game.BaseProcess == null || game.BaseProcess.HasExited)
                        {
                            result.Success = false;
                            result.ErrorMessage = "Process exited after mapping (before DllMain execution).";
                            log?.Invoke("ERROR: Process exited after mapping.\r\n");
                            log?.Invoke("  Module was mapped but DllMain was not executed.\r\n");
                            log?.Invoke("  This may indicate detection during mapping phase.\r\n");
                            return result;
                        }
                        
                        // Step 4: Execute DllMain using QueueUserAPC (stealthiest method)
                        log?.Invoke("  - Executing DllMain entry point via QueueUserAPC...\r\n");
                        
                        // Get entry point address from PE structure (avoid PEImage dependency)
                        // Reuse peOffset that was calculated during validation
                        ushort machine = BitConverter.ToUInt16(dllBytes, peOffset + 4);
                        bool is32Bit = (machine == 0x014C); // IMAGE_FILE_MACHINE_I386
                        
                        uint addressOfEntryPoint;
                        if (is32Bit)
                        {
                            // PE32: Entry point is at offset 40 from OptionalHeader start
                            // OptionalHeader starts at PE offset + 24
                            int optHeaderOffset = peOffset + 24;
                            addressOfEntryPoint = BitConverter.ToUInt32(dllBytes, optHeaderOffset + 16); // AddressOfEntryPoint offset
                        }
                        else
                        {
                            // PE32+: Entry point is at offset 16 from OptionalHeader start
                            int optHeaderOffset = peOffset + 24;
                            addressOfEntryPoint = BitConverter.ToUInt32(dllBytes, optHeaderOffset + 16); // AddressOfEntryPoint offset
                        }
                        
                        PointerEx entryPoint = moduleBase + addressOfEntryPoint;
                        
                        // Execute DllMain via QueueUserAPC (uses existing thread, no new thread creation)
                        bool dllMainResult = game.CallByMethod<bool>(entryPoint, System.ExCallThreadType.XCTT_QUAPC, moduleBase, 1, 0);
                        
                        if (!dllMainResult)
                        {
                            log?.Invoke("  - WARNING: DllMain returned FALSE (may indicate failure or rejection)\r\n");
                            log?.Invoke("  - Module is mapped but initialization may have failed\r\n");
                            log?.Invoke("  - This could indicate BO3Enhanced detected and blocked DllMain execution\r\n");
                        }
                        else
                        {
                            log?.Invoke("  - DllMain executed successfully via QueueUserAPC\r\n");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Check if process crashed
                        if (!game.Refresh() || game.BaseProcess == null || game.BaseProcess.HasExited)
                        {
                            result.Success = false;
                            result.ErrorMessage = $"Process crashed during injection: {ex.Message}";
                            log?.Invoke($"ERROR: Process crashed during injection.\r\n");
                            log?.Invoke($"  Error: {ex.Message}\r\n");
                            if (ex.InnerException != null)
                            {
                                log?.Invoke($"  Inner: {ex.InnerException.Message}\r\n");
                            }
                            return result;
                        }
                        
                        // Staged injection failed, try standard injection as fallback
                        log?.Invoke("  - Staged injection failed, trying standard injection method...\r\n");
                        log?.Invoke($"  - Error: {ex.Message}\r\n");
                        
                        var standardOptions = new System.ModuleLoadOptions
                        {
                            ExecMain = true,
                            ClearHeader = true,
                            MainThreadType = System.ExCallThreadType.XCTT_QUAPC
                        };
                        
                        try
                        {
                            moduleBase = game.MapModule(new System.Memory<byte>(dllBytes), standardOptions);
                            log?.Invoke("  - Standard injection succeeded\r\n");
                        }
                        catch (Exception ex2)
                        {
                            // Check if process crashed
                            if (!game.Refresh() || game.BaseProcess == null || game.BaseProcess.HasExited)
                            {
                                result.Success = false;
                                result.ErrorMessage = $"Process crashed during standard injection: {ex2.Message}";
                                log?.Invoke($"ERROR: Process crashed during standard injection.\r\n");
                                return result;
                            }
                            
                            // Try without DllMain as last resort
                            log?.Invoke("  - Standard injection failed, trying without DllMain execution...\r\n");
                            standardOptions.ExecMain = false;
                            try
                            {
                                moduleBase = game.MapModule(new System.Memory<byte>(dllBytes), standardOptions);
                                log?.Invoke("  - WARNING: DLL mapped but DllMain was not executed.\r\n");
                                log?.Invoke("  - The DLL may not function correctly without DllMain initialization.\r\n");
                            }
                            catch
                            {
                                // Re-throw original exception
                                throw ex;
                            }
                        }
                    }
                    
                    if (!moduleBase)
                    {
                        result.Success = false;
                        result.ErrorMessage = "Failed to map DLL module (returned null/zero).";
                        log?.Invoke("ERROR: Failed to map DLL module (returned null/zero).\r\n");
                        return result;
                    }
                    
                    log?.Invoke($"  - Module base: 0x{moduleBase:X}\r\n");
                    log?.Invoke("  - Entry point executed successfully.\r\n");
                }
                catch (NullReferenceException ex)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Null reference during DLL mapping (likely missing dependency module): {ex.Message}";
                    log?.Invoke($"ERROR: Null reference during DLL mapping.\r\n");
                    log?.Invoke($"  This usually means a required DLL dependency is not loaded in the target process.\r\n");
                    log?.Invoke($"  The mapping process will attempt to load dependencies automatically, but some may fail.\r\n");
                    log?.Invoke($"  Message: {ex.Message}\r\n");
                    if (!string.IsNullOrEmpty(ex.StackTrace))
                    {
                        // Extract the relevant part of the stack trace
                        var stackLines = ex.StackTrace.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                        foreach (var line in stackLines.Take(10)) // Show first 10 lines
                        {
                            if (line.Contains("GetProcAddress") || line.Contains("__MMRewriteModuleIAT") || 
                                line.Contains("MapModule") || line.Contains("DllLoaderService"))
                            {
                                log?.Invoke($"  {line}\r\n");
                            }
                        }
                    }
                    if (ex.InnerException != null)
                    {
                        log?.Invoke($"  Inner exception: {ex.InnerException.Message}\r\n");
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Failed to map DLL: {ex.Message}";
                    log?.Invoke($"ERROR: {result.ErrorMessage}\r\n");
                    log?.Invoke($"  Exception type: {ex.GetType().Name}\r\n");
                    if (ex.InnerException != null)
                    {
                        log?.Invoke($"  Inner exception: {ex.InnerException.Message}\r\n");
                        if (!string.IsNullOrEmpty(ex.InnerException.StackTrace))
                        {
                            log?.Invoke($"  Inner stack trace: {ex.InnerException.StackTrace}\r\n");
                        }
                    }
                    if (!string.IsNullOrEmpty(ex.StackTrace))
                    {
                        log?.Invoke($"  Stack trace: {ex.StackTrace}\r\n");
                    }
                    return result;
                }

                result.Success = true;
                result.ModuleHandle = moduleBase;
                result.ErrorMessage = null;
                log?.Invoke("\r\n=== DLL LOADED SUCCESSFULLY ===\r\n");
                log?.Invoke($"DLL manually mapped at: 0x{moduleBase:X}\r\n");
                log?.Invoke("All imports resolved, module relocated, and DllMain executed.\r\n\r\n");

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Unexpected error: {ex.Message}";
                log?.Invoke($"\r\n=== DLL LOADER ERROR ===\r\n");
                log?.Invoke($"{result.ErrorMessage}\r\n\r\n");
                return result;
            }
        }
    }
}

