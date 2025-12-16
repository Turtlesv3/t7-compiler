using System;

namespace T7CompilerGUI.Models
{
    /// <summary>
    /// Result of an injection operation
    /// </summary>
    public class InjectionResult
    {
        /// <summary>
        /// Indicates if injection was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error code (0 = success, non-zero = failure)
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// Error message if injection failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Game that was injected
        /// </summary>
        public TreyarchCompiler.Enums.Games? Game { get; set; }

        /// <summary>
        /// Replace path used for injection
        /// </summary>
        public string ReplacePath { get; set; }

        /// <summary>
        /// Creates a successful injection result
        /// </summary>
        public static InjectionResult CreateSuccess(TreyarchCompiler.Enums.Games game, string replacePath)
        {
            return new InjectionResult
            {
                Success = true,
                ErrorCode = 0,
                Game = game,
                ReplacePath = replacePath
            };
        }

        /// <summary>
        /// Creates a failed injection result
        /// </summary>
        public static InjectionResult CreateFailure(int errorCode, string errorMessage = null)
        {
            return new InjectionResult
            {
                Success = false,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage ?? $"Injection failed with error code: 0x{errorCode:X}"
            };
        }
    }
}

