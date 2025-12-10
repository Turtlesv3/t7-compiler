using System;
using TreyarchCompiler.Utilities;
using static System.ExCallThreadType;

namespace T7CompilerGUI.Games
{
    internal static class BlackOps4
    {
        internal static class Constants
        {
            public const ulong OFF_SendUIPopUp = 0x3CD1C60;
            public const ulong OFF_GetLocalClientNum = 0x2893330;
        }

        private static ProcessEx __game;
        internal static ProcessEx Game
        {
            get
            {
                if (__game == null || __game.BaseProcess.HasExited)
                {
                    __game = "blackops4";
                    if (__game == null || __game.BaseProcess.HasExited)
                    {
                        throw new Exception("Black Ops 4 not found.");
                    }
                    __game.SetDefaultCallType(XCTT_RIPHijack);
                }
                if (!__game.Handle)
                {
                    __game.OpenHandle(ProcessEx.PROCESS_ACCESS, true);
                }
                return __game;
            }
        }

        internal static void Popup(string message)
        {
            int clientnum = Game.Call<int>(Game[Constants.OFF_GetLocalClientNum], 0);
            Game.Call(Game[Constants.OFF_SendUIPopUp], clientnum, 0x8, message); // 0x8 == info
        }

        internal enum Gamemodes : int
        {
            MODE_ZOMBIES = 0x0,
            MODE_MULTIPLAYER = 0x1,
            MODE_CAMPAIGN = 0x2,
        };
    }
}

