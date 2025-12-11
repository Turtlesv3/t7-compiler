using System.Collections.Generic;

namespace T7CompilerGUI.Utils
{
    public struct SourceTokenDef
{
    public string FilePath;
    public int LineStart;
    public int LineEnd;
    public int CharStart;
    public int CharEnd;
    public Dictionary<int, (int CStart, int CEnd)> LineMappings;

    public SourceTokenDef(string filePath)
    {
        FilePath = filePath;
        LineStart = 0;
        LineEnd = 0;
        CharStart = 0;
        CharEnd = 0;
        LineMappings = new Dictionary<int, (int CStart, int CEnd)>();
    }
    }
}

