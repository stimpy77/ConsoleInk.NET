using System;

namespace ConsoleInk
{
    /// <summary>
    /// Provides options for configuring Markdown rendering behavior.
    /// </summary>
    public class MarkdownRenderOptions
    {
        /// <summary>
        /// Gets or sets the desired console width for line wrapping.
        /// Defaults to Console.WindowWidth if available, otherwise 80.
        /// </summary>
        public int ConsoleWidth { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether ANSI color codes should be used.
        /// Defaults to true.
        /// </summary>
        public bool EnableColors { get; set; } = true;

        /// <summary>
        /// Gets or sets the theme to use for rendering.
        /// Defaults to ConsoleTheme.Default.
        /// </summary>
        public ConsoleTheme Theme { get; set; } = ConsoleTheme.Default;

        /// <summary>
        /// Gets or sets a value indicating whether HTML tags should be stripped from the output.
        /// Defaults to true.
        /// </summary>
        public bool StripHtml { get; set; } = true;

        // TODO: Add more configuration options as needed.

        /// <summary>
        /// Initializes a new instance of the <see cref="MarkdownRenderOptions"/> class with default values.
        /// </summary>
        public MarkdownRenderOptions()
        {
            try
            {
                ConsoleWidth = Console.WindowWidth;
            }
            catch (System.IO.IOException)
            {
                // Handle cases where Console.WindowWidth is unavailable (e.g., redirected output)
                ConsoleWidth = 80;
            }
        }
    }
}
