using ConsoleInk;
using System;
using System.IO;
using System.Threading; // Added for Thread.Sleep

namespace ConsoleInk.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("--- Demo using static MarkdownConsole.Render ---");
            Console.WriteLine("Rendering a sample Markdown document all at once:");
            Console.WriteLine("-----------------------------------------------");

            string markdown = @"
# Sample Markdown Document

This is a paragraph demonstrating **bold** and *italic* text.

## Features

- Unordered list item 1
- Unordered list item 2
  - Nested item

1. Ordered list item 1
2. Ordered list item 2

```csharp
// Sample code block
public static void SayHello(string name)
{
    Console.WriteLine($""Hello, {name}!"");
}
```

> Blockquote example.
> Continued blockquote.

---

[Link to Google](https://www.google.com)

`Inline code example`

Check out the ~~strikethrough~~ feature.
";

            // Demo 1: Render entire string at once using the static helper
            // Configure options (optional)
            var options = new MarkdownRenderOptions
            {
                // Attempt to use console width, fallback to 80
                ConsoleWidth = TryGetConsoleWidth(80),
                EnableColors = true, // Use colors if supported
                Theme = ConsoleTheme.Default // Use default theme
            };

            string renderedOutput = MarkdownConsole.Render(markdown, options);
            Console.WriteLine(renderedOutput);

            Console.WriteLine("-----------------------------------------------");
            Console.WriteLine(@"
Press Enter to start the streaming demo...");
            Console.ReadLine();

            Console.WriteLine(@"
--- Demo using streaming MarkdownConsoleWriter ---");
            Console.WriteLine("Rendering the same Markdown document line-by-line (simulated stream):");
            Console.WriteLine("--------------------------------------------------");

            // Demo 2: Use the streaming writer directly to Console.Out
            try
            {
                using (var stringReader = new StringReader(markdown))
                // Pass the same options to the streaming writer
                using (var markdownWriter = new MarkdownConsoleWriter(Console.Out, options))
                {
                    string line;
                    while ((line = stringReader.ReadLine()) != null)
                    {
                        markdownWriter.WriteLine(line);
                        markdownWriter.Flush(); // Flush after each line for immediate effect in demo
                        // Introduce a small delay to simulate streaming input
                        Thread.Sleep(100);
                    }
                    markdownWriter.Complete(); // Signal end of input is crucial for finalizing output
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine();
                Console.WriteLine($"Error during streaming demo: {ex.Message}");
                Console.ResetColor();
            }


            Console.WriteLine(@"
--------------------------------------------------");
            Console.WriteLine("--- Demo Complete ---");
            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();
        }

        // Helper to safely get console width
        private static int TryGetConsoleWidth(int defaultWidth)
        {
            try
            {
                // Check if Console is redirected, WindowWidth might throw an exception
                if (!Console.IsOutputRedirected)
                {
                   return Console.WindowWidth > 0 ? Console.WindowWidth : defaultWidth;
                }
            }
            catch (IOException)
            {
                // Ignore if Console.WindowWidth is not available (e.g., redirected output)
            }
            return defaultWidth;
        }
    }
}
