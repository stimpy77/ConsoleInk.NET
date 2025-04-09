using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleInk
{
    /// <summary>
    /// Provides common ANSI escape code constants for terminal formatting.
    /// Reference: https://en.wikipedia.org/wiki/ANSI_escape_code#SGR_(Select_Graphic_Rendition)_parameters
    /// </summary>
    public static class Ansi
    {
        private const string Esc = "\x1B"; // Or "\u001B"
        private const string Csi = Esc + "[";

        // --- General ---
        public const string Reset = Csi + "0m";

        // --- Styles ---
        public const string Bold = Csi + "1m";
        public const string Faint = Csi + "2m"; // Not widely supported
        public const string Italic = Csi + "3m"; // Not widely supported
        public const string Underline = Csi + "4m";
        public const string SlowBlink = Csi + "5m"; // Not widely supported
        public const string RapidBlink = Csi + "6m"; // Not widely supported
        public const string ReverseVideo = Csi + "7m";
        public const string Conceal = Csi + "8m"; // Not widely supported
        public const string Strikethrough = Csi + "9m"; // Not widely supported

        public const string BoldOff = Csi + "22m"; // Neither bold nor faint
        public const string ItalicOff = Csi + "23m";
        public const string UnderlineOff = Csi + "24m";
        public const string BlinkOff = Csi + "25m";
        public const string ReverseVideoOff = Csi + "27m";
        public const string ConcealOff = Csi + "28m";
        public const string StrikethroughOff = Csi + "29m";


        // --- Foreground Colors (3-bit / 4-bit) ---
        public const string FgBlack = Csi + "30m";
        public const string FgRed = Csi + "31m";
        public const string FgGreen = Csi + "32m";
        public const string FgYellow = Csi + "33m";
        public const string FgBlue = Csi + "34m";
        public const string FgMagenta = Csi + "35m";
        public const string FgCyan = Csi + "36m";
        public const string FgWhite = Csi + "37m";
        public const string FgDefault = Csi + "39m";

        // --- Bright Foreground Colors (3-bit / 4-bit) ---
        public const string FgBrightBlack = Csi + "90m"; // Gray
        public const string FgBrightRed = Csi + "91m";
        public const string FgBrightGreen = Csi + "92m";
        public const string FgBrightYellow = Csi + "93m";
        public const string FgBrightBlue = Csi + "94m";
        public const string FgBrightMagenta = Csi + "95m";
        public const string FgBrightCyan = Csi + "96m";
        public const string FgBrightWhite = Csi + "97m";


        // --- Background Colors (3-bit / 4-bit) ---
        public const string BgBlack = Csi + "40m";
        public const string BgRed = Csi + "41m";
        public const string BgGreen = Csi + "42m";
        public const string BgYellow = Csi + "43m";
        public const string BgBlue = Csi + "44m";
        public const string BgMagenta = Csi + "45m";
        public const string BgCyan = Csi + "46m";
        public const string BgWhite = Csi + "47m";
        public const string BgDefault = Csi + "49m";

        // --- Bright Background Colors (3-bit / 4-bit) ---
        public const string BgBrightBlack = Csi + "100m"; // Gray
        public const string BgBrightRed = Csi + "101m";
        public const string BgBrightGreen = Csi + "102m";
        public const string BgBrightYellow = Csi + "103m";
        public const string BgBrightBlue = Csi + "104m";
        public const string BgBrightMagenta = Csi + "105m";
        public const string BgBrightCyan = Csi + "106m";
        public const string BgBrightWhite = Csi + "107m";


        // --- 256 Colors (8-bit) ---
        // Format: CSI 38;5;{code}m (Foreground)
        // Format: CSI 48;5;{code}m (Background)
        public static string FgColor256(byte code) => $"{Csi}38;5;{code}m";
        public static string BgColor256(byte code) => $"{Csi}48;5;{code}m";

        // --- True Color (24-bit) ---
        // Format: CSI 38;2;{r};{g};{b}m (Foreground)
        // Format: CSI 48;2;{r};{g};{b}m (Background)
        public static string FgColorTrue(byte r, byte g, byte b) => $"{Csi}38;2;{r};{g};{b}m";
        public static string BgColorTrue(byte r, byte g, byte b) => $"{Csi}48;2;{r};{g};{b}m";

        /// <summary>
        /// Gets the ANSI escape sequence for a standard System.ConsoleColor.
        /// </summary>
        /// <param name="color">The ConsoleColor to get the code for.</param>
        /// <param name="foreground">True for foreground color, false for background color.</param>
        /// <returns>The ANSI escape sequence string, or an empty string if color is null.</returns>
        public static string GetColorCode(ConsoleColor? color, bool foreground = true)
        {
            if (!color.HasValue) return string.Empty;

            return (color.Value, foreground) switch
            {
                // Foreground
                (ConsoleColor.Black,       true) => FgBlack,
                (ConsoleColor.DarkBlue,    true) => FgBlue,
                (ConsoleColor.DarkGreen,   true) => FgGreen,
                (ConsoleColor.DarkCyan,    true) => FgCyan,
                (ConsoleColor.DarkRed,     true) => FgRed,
                (ConsoleColor.DarkMagenta, true) => FgMagenta,
                (ConsoleColor.DarkYellow,  true) => FgYellow,       // Often rendered as Brown
                (ConsoleColor.Gray,        true) => FgWhite,        // Standard White
                (ConsoleColor.DarkGray,    true) => FgBrightBlack,  // Bright Black is Dark Gray
                (ConsoleColor.Blue,        true) => FgBrightBlue,
                (ConsoleColor.Green,       true) => FgBrightGreen,
                (ConsoleColor.Cyan,        true) => FgBrightCyan,
                (ConsoleColor.Red,         true) => FgBrightRed,
                (ConsoleColor.Magenta,    true) => FgBrightMagenta,
                (ConsoleColor.Yellow,      true) => FgBrightYellow,
                (ConsoleColor.White,       true) => FgBrightWhite,

                // Background
                (ConsoleColor.Black,       false) => BgBlack,
                (ConsoleColor.DarkBlue,    false) => BgBlue,
                (ConsoleColor.DarkGreen,   false) => BgGreen,
                (ConsoleColor.DarkCyan,    false) => BgCyan,
                (ConsoleColor.DarkRed,     false) => BgRed,
                (ConsoleColor.DarkMagenta, false) => BgMagenta,
                (ConsoleColor.DarkYellow,  false) => BgYellow,
                (ConsoleColor.Gray,        false) => BgWhite,
                (ConsoleColor.DarkGray,    false) => BgBrightBlack,
                (ConsoleColor.Blue,        false) => BgBrightBlue,
                (ConsoleColor.Green,       false) => BgBrightGreen,
                (ConsoleColor.Cyan,        false) => BgBrightCyan,
                (ConsoleColor.Red,         false) => BgBrightRed,
                (ConsoleColor.Magenta,    false) => BgBrightMagenta,
                (ConsoleColor.Yellow,      false) => BgBrightYellow,
                (ConsoleColor.White,       false) => BgBrightWhite,

                _ => string.Empty // Should not happen for valid ConsoleColor values
            };
        }
    }
}
