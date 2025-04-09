# ConsoleInk.NET

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/your-username/ConsoleInk.NET) <!-- Replace with actual build status badge -->
[![NuGet](https://img.shields.io/nuget/v/ConsoleInk.Net.svg)](https://www.nuget.org/packages/ConsoleInk.Net/) <!-- Replace with actual NuGet badge -->
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

**ConsoleInk.NET** is a lightweight, zero-dependency .NET library for rendering Markdown text directly into ANSI-formatted output suitable for modern console applications. It focuses on streaming processing, enabling efficient rendering of Markdown content as it arrives.

## Features

*   **Streaming Markdown Processing:** Renders Markdown incrementally, perfect for real-time display.
*   **Zero External Dependencies:** Relies only on the .NET Base Class Library (BCL).
*   **ANSI Formatting:** Outputs text with ANSI escape codes for colors and styles (bold, italic, underline, strikethrough).
*   **CommonMark & GFM Support:** Handles standard Markdown syntax including headings, lists, code blocks, blockquotes, links, images, emphasis, task lists, and basic tables.
*   **Theming:** Configurable themes (`Default`, `Monochrome`, or custom) to control output appearance.
*   **HTML Stripping:** Removes HTML tags from the output by default.
*   **Word Wrapping:** Automatically wraps text to fit the specified console width.
*   **.NET & PowerShell Integration:** Provides both a C# library (`ConsoleInk.Net`) and a PowerShell module (`ConsoleInk.PowerShell`).

## Installation

### NuGet Package Manager

```powershell
Install-Package ConsoleInk.Net
```

### .NET CLI

```bash
dotnet add package ConsoleInk.Net
```

*(PowerShell module installation instructions TBD)*

## Usage

### C# Library (`ConsoleInk.Net`)

#### Simple Batch Rendering

```csharp
using ConsoleInk;

string markdown = """
# Hello Console!

This is **ConsoleInk.NET**.

*   Renders Markdown
*   Uses *ANSI* codes
*   `Zero` dependencies!

> Look, a blockquote!

```csharp
Console.WriteLine("Code example");
```
""";

// Render to a string
string ansiOutput = MarkdownConsole.Render(markdown);
Console.WriteLine(ansiOutput);

// Render directly to the console
MarkdownConsole.Render(markdown, Console.Out);

// Render with options
var options = new MarkdownRenderOptions
{
    ConsoleWidth = 120,
    Theme = ConsoleTheme.Default // Or ConsoleTheme.Monochrome, or custom
};
MarkdownConsole.Render(markdown, Console.Out, options);
```

#### Streaming Rendering

```csharp
using ConsoleInk;
using System.IO;

// Simulate streaming input
var inputStream = new StringReader("""
## Streaming Demo

Content arrives piece by piece...
...and gets rendered immediately.
""");

var options = new MarkdownRenderOptions { ConsoleWidth = 80 };

// Use MarkdownConsoleWriter with Console.Out
using (var writer = new MarkdownConsoleWriter(Console.Out, options))
{
    char[] buffer = new char[50];
    int bytesRead;
    while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
    {
        writer.Write(buffer, 0, bytesRead);
        // Simulate delay or processing time
        System.Threading.Thread.Sleep(100);
    }
    writer.Complete(); // Signal end of input
}
```

### PowerShell Module (`ConsoleInk.PowerShell`)

*(Examples pending module finalization)*

```powershell
# Example: Convert Markdown string
"## Title
* Item 1
* Item 2" | ConvertTo-ConsoleMarkdown

# Example: Show a Markdown file
Show-Markdown -Path ./README.md

# Example: Convert with options
Get-Content ./document.md | ConvertTo-ConsoleMarkdown -Width 100 -NoColor
```

## Building & Testing

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/your-username/ConsoleInk.NET.git
    cd ConsoleInk.NET
    ```
2.  **Build the solution:**
    ```bash
    dotnet build src/ConsoleInk.sln
    ```
3.  **Run tests:**
    ```bash
    dotnet test src/ConsoleInk.sln
    ```

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
 
