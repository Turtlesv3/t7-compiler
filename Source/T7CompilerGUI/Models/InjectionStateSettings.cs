namespace T7CompilerGUI.Models
{
    /// <summary>
    /// Model representing injection state for persistence
    /// </summary>
    public class InjectionStateSettings
    {
        public string ReplacePath { get; set; } = "";
        public TreyarchCompiler.Enums.Games Game { get; set; } = TreyarchCompiler.Enums.Games.T7;
        public int OriginalPID { get; set; } = 0;
        public ulong ModifiedSPTStruct { get; set; } = 0;
        public ulong OriginalBuffer { get; set; } = 0;
        public int InjectedBuffSize { get; set; } = 0;
        public ulong ScriptName { get; set; } = 0;
        public int ScriptBuffSize { get; set; } = 0;
        public ulong ScriptBuffer { get; set; } = 0;
        
        public bool HasValidState => ModifiedSPTStruct != 0 && OriginalPID != 0;
    }
}

