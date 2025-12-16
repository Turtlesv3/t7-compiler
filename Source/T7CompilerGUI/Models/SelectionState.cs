namespace T7CompilerGUI.Models
{
    /// <summary>
    /// Model representing UI selection state (dropdown indices)
    /// </summary>
    public class SelectionState
    {
        public int PlatformIndex { get; set; } = 0;
        public int GameIndex { get; set; } = 0;
        public int InjectGameIndex { get; set; } = 0;
    }
}

