using System;

namespace ConsoleInk
{
    /// <summary>
    /// Defines the color and styling theme for Markdown rendering.
    /// </summary>
    public class ConsoleTheme
    {
        // --- Static Theme Instances ---

        private static readonly Lazy<ConsoleTheme> _defaultTheme = new Lazy<ConsoleTheme>(() => new ConsoleTheme());
        private static readonly Lazy<ConsoleTheme> _monochromeTheme = new Lazy<ConsoleTheme>(CreateMonochromeTheme);

        /// <summary>
        /// Gets the default theme with standard console colors.
        /// </summary>
        public static ConsoleTheme Default => _defaultTheme.Value;

        /// <summary>
        /// Gets a theme that uses no color, relying only on default foreground/background.
        /// </summary>
        public static ConsoleTheme Monochrome => _monochromeTheme.Value;

        // --- Theme Properties ---

        public ConsoleColor? Heading1Color { get; set; } = ConsoleColor.Yellow;
        public string Heading1Style { get; set; } = Ansi.Bold;
        public ConsoleColor? Heading2Color { get; set; } = ConsoleColor.Cyan;
        public string Heading2Style { get; set; } = Ansi.Underline;
        public ConsoleColor? Heading3Color { get; set; } = ConsoleColor.Green;
        public string Heading3Style { get; set; } = ""; // No default style for H3
        public ConsoleColor? Heading4Color { get; set; } = ConsoleColor.Magenta;
        public ConsoleColor? Heading5Color { get; set; } = ConsoleColor.Blue;
        public ConsoleColor? Heading6Color { get; set; } = ConsoleColor.DarkYellow;

        public ConsoleColor? LinkForegroundColor { get; set; } = ConsoleColor.Blue;
        public string LinkTextStyle { get; set; } = Ansi.Underline + Ansi.FgBlue;
        public string LinkUrlStyle { get; set; } = Ansi.Faint;
        public ConsoleColor? LinkBackgroundColor { get; set; } = null;
        public bool LinkUnderline { get; set; } = true;

        public ConsoleColor? CodeForegroundColor { get; set; } = ConsoleColor.Gray;
        public string CodeBlockStyle { get; set; } = Ansi.FgBrightBlack;
        public ConsoleColor? CodeBackgroundColor { get; set; } = null; // Default background often looks best

        public ConsoleColor? BlockquoteColor { get; set; } = ConsoleColor.DarkGray;
        public ConsoleColor? HorizontalRuleColor { get; set; } = ConsoleColor.DarkGray;
        public ConsoleColor? ListBulletColor { get; set; } = ConsoleColor.Yellow; // Color for *, -, 1. etc.
        public string BlockquotePrefix { get; set; } = "| ";
        public char HorizontalRuleChar { get; set; } = '-';
        public string UnorderedListPrefix { get; set; } = "* ";
        public string OrderedListPrefixFormat { get; set; } = "{0}. "; // {0} is the number

        public string InlineCodeStyle { get; set; } = Ansi.Faint; // Dimmed text for inline code

        /// <summary>
        /// Gets or sets the style for the blockquote marker (e.g., '>').
        /// Defaults to Dim styling.
        /// </summary>
        public string BlockquoteMarkerStyle { get; set; } = Ansi.Faint;

        /// <summary>
        /// Gets or sets the marker character(s) used for blockquotes.
        /// Defaults to "> ".
        /// </summary>
        public string BlockquoteMarker { get; set; } = "> ";

        /// <summary>
        /// Gets or sets the style applied to the alt text of an image.
        /// Defaults to Dim styling.
        /// </summary>
        public string ImageAltTextStyle { get; set; } = Ansi.Faint;

        /// <summary>
        /// Gets or sets the prefix added before the alt text of an image.
        /// Defaults to "[Image: ".
        /// </summary>
        public string ImagePrefix { get; set; } = "[Image: ";

        /// <summary>
        /// Gets or sets the suffix added after the alt text of an image.
        /// Defaults to "]".
        /// </summary>
        public string ImageSuffix { get; set; } = "]";

        /// <summary>
        /// Gets or sets the marker string used for an unchecked task list item.
        /// Defaults to "[ ] ".
        /// </summary>
        public string TaskListUncheckedMarker { get; set; } = "[ ] ";

        /// <summary>
        /// Gets or sets the marker string used for a checked task list item.
        /// Defaults to "[x] ".
        /// </summary>
        public string TaskListCheckedMarker { get; set; } = "[x] ";

        // --- Emphasis Styles ---
        public string BoldStyle { get; set; } = Ansi.Bold;
        public string ItalicStyle { get; set; } = Ansi.Italic;
        public string StrikethroughStyle { get; set; } = Ansi.Strikethrough;

        // TODO: Add more theme properties

        /// <summary>
        /// Creates a monochrome theme instance.
        /// </summary>
        private static ConsoleTheme CreateMonochromeTheme()
        {
            // Use null for colors to indicate default console foreground/background
            return new ConsoleTheme
            {
                Heading1Color = null,
                Heading2Color = null,
                Heading3Color = null,
                Heading4Color = null,
                Heading5Color = null,
                Heading6Color = null,
                LinkForegroundColor = null,
                LinkBackgroundColor = null,
                LinkUnderline = false, // Underlining might not render well without color
                LinkTextStyle = "",
                LinkUrlStyle = "",
                CodeForegroundColor = null,
                CodeBackgroundColor = null,
                CodeBlockStyle = "",
                BlockquoteColor = null,
                HorizontalRuleColor = null,
                ListBulletColor = null,
                BlockquotePrefix = "| ",
                HorizontalRuleChar = '-',
                UnorderedListPrefix = "* ",
                OrderedListPrefixFormat = "{0}. ",
                BoldStyle = "",
                ItalicStyle = "",
                StrikethroughStyle = "",
            };
        }
    }
}
