using TreyarchCompiler.Enums;
using TreyarchCompiler.Games;
using TreyarchCompiler.Interface;
using TreyarchCompiler.Utilities;

namespace TreyarchCompiler
{
    //NOTE: this class system will no longer work as of bo3, because each platform has unique opcodes.
    public class Compiler
    {
        public static CompiledCode Compile(Platforms platform, Enums.Games game, Modes mode, bool uset8masking, string code, string path = "")
        {
            switch(platform)
            {
                case Platforms.PC:
                case Platforms.Steam:
                case Platforms.XB1:
                    // PC, Steam, and XB1 are all different BO3 PC clients - compile the same way
                    return CompilePC(game, mode, code, path, uset8masking, Platforms.PC)?.Compile();
                
                case Platforms.PS4:
                    // PS4 is the console platform - compile separately
                    return CompilePC(game, mode, code, path, uset8masking, Platforms.PS4)?.Compile();

                case Platforms.Xbox:
                case Platforms.PS3:
                    return CompileConsole(game, mode, code, path)?.Compile();
            }
            return null;
        }

        public static CompiledCode CompileRCE(Platforms platform, Enums.Games game, Modes mode, string code, string address, string path = "")
        {
            return null;
        }

        private static ICompiler CompilePC(Enums.Games game, Modes mode, string code, string path, bool uset8masking, Platforms platform = Platforms.PC)
        {
            switch(game)
            {
                case Enums.Games.T7:
                    return new GSCCompiler(mode, code, path, platform, game, uset8masking);
                case Enums.Games.T8:
                    return new T89Compiler(game, code);
            }
            return null;
        }

        private static ICompiler CompilePS4(Enums.Games game, Modes mode, string code, string path, bool uset8masking)
        {
            // PS4 compilation handled by CompilePC method
            return CompilePC(game, mode, code, path, uset8masking, Platforms.PS4);
        }

        private static ICompiler CompileConsole(Enums.Games game, Modes mode, string code, string path)
        {
            return null;
        }
    }
}