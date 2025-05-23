# ConsoleInk.NET

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/stimpy77/ConsoleInk.NET) <!-- Placeholder Build Status Badge -->
[![NuGet](https://img.shields.io/nuget/v/ConsoleInk.Net.svg)](https://www.nuget.org/packages/ConsoleInk.Net/) <!-- Placeholder NuGet Badge -->
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

**ConsoleInk.NET** is a lightweight, zero-dependency .NET library for rendering Markdown text directly into ANSI-formatted output suitable for modern console applications. It focuses on streaming processing, enabling efficient rendering of Markdown content as it arrives.

## Features

*   **Streaming Markdown Processing:** Renders Markdown incrementally, perfect for real-time display.
*   **Zero External Dependencies:** Relies only on the .NET Base Class Library (BCL).
*   **ANSI Formatting:** Outputs text with ANSI escape codes for colors and styles (bold, italic, underline, strikethrough).
*   **CommonMark & GFM Support:** Handles standard Markdown syntax including headings, lists (ordered, unordered, task lists), code blocks (indented and fenced), blockquotes, links (inline, reference*), images (inline alt text), and emphasis. Basic GFM table support is included. (*Reference links render literally if definition appears after usage due to streaming nature*).
*   **Theming:** Configurable themes (`Default`, `Monochrome`, or custom) to control output appearance.
*   **HTML Stripping:** Removes HTML tags from the output by default.
*   **Word Wrapping:** Automatically wraps text to fit the specified console width.
*   **.NET Library & PowerShell Usage:** Provides a C# library (`ConsoleInk.Net`) and can be used directly from PowerShell.
*   **Hyperlinks:** Supports true OSC-8 hyperlinks in terminal emulators that support them, with fallback to styled text rendering.
*   **Code Blocks:** Handles both indented and fenced code blocks with proper line preservation, indentation control, and syntax-highlighting style.

## Feature Details

### Hyperlinks

ConsoleInk.NET supports two styles of hyperlink rendering:

1. **True Hyperlinks (OSC-8)**: When `UseHyperlinks = true` (default) and the terminal supports it, clickable links are rendered using the OSC-8 ANSI escape sequence standard.
2. **Styled Text Fallback**: When `UseHyperlinks = false` or in terminals without hyperlink support, links are rendered with styled text showing the URL in parentheses.

```csharp
var options = new MarkdownRenderOptions
{
    UseHyperlinks = true,  // Enable true hyperlinks (default)
    Theme = new ConsoleTheme 
    { 
        LinkTextStyle = Ansi.Underline + Ansi.FgBrightBlue,  // Customize link text appearance
        LinkUrlStyle = Ansi.FgBrightCyan                     // Customize URL text appearance (when not using hyperlinks)
    }
};

// Render markdown with hyperlinks
string markdown = "Visit [GitHub](https://github.com/) for more info.";
MarkdownConsole.Render(markdown, Console.Out, options);
```

### Code Blocks

ConsoleInk.NET supports both indented and fenced code blocks with careful handling of line breaks:

1. **Indented Code Blocks**: Code indented with 4 spaces or a tab.
2. **Fenced Code Blocks**: Code surrounded by triple backticks (```), optionally specifying a language.

Code blocks preserve line breaks, maintain indentation within the block as written in the original markdown, and are rendered with optional syntax highlighting styles.

```csharp
var options = new MarkdownRenderOptions
{
    Theme = new ConsoleTheme 
    { 
        CodeBlockStyle = Ansi.FgBrightBlack  // Light gray code blocks
    }
};

// Example with fenced code block
string markdown = """
# Code Example

```csharp
// This code will be rendered with proper indentation
if (condition)
{
    Console.WriteLine("Indented line");
}
```

Regular text continues after the code block.
""";

MarkdownConsole.Render(markdown, Console.Out, options);
```

## Installation

You can install the library via NuGet.

### NuGet Package Manager

```powershell
Install-Package ConsoleInk.Net -Version 0.1.2 # Adjust version if needed
```

### .NET CLI

```bash
dotnet add package ConsoleInk.Net --version 0.1.2 # Adjust version if needed
```

*(A dedicated PowerShell module `ConsoleInk.PowerShell` is planned but not yet available.)*

## Usage

### C# Library (`ConsoleInk.Net`)

Add a reference to the `ConsoleInk.Net` package or project.

See the `src/ConsoleInk.Demo` project for a runnable example.

```csharp
using ConsoleInk.Net; // Namespace is ConsoleInk
using System.IO;

// --- Batch Rendering ---
string markdown = """
# Hello Console!

This is **ConsoleInk.NET**.
*   Renders Markdown
*   Uses *ANSI* codes
""";

// Configure options (optional)
var options = new MarkdownRenderOptions
{
    ConsoleWidth = 100,
    Theme = ConsoleTheme.Default
};

// Render to a string
string ansiOutput = MarkdownConsole.Render(markdown, options);
Console.WriteLine(ansiOutput);

// Render directly to the console
MarkdownConsole.Render(markdown, Console.Out, options);


// --- Streaming Rendering ---
var inputStream = new StringReader("## Streaming Demo\nContent arrives piece by piece...");

// Use the writer within a 'using' block for automatic disposal and completion
using (var writer = new MarkdownConsoleWriter(Console.Out, options))
{
    char[] buffer = new char[50];
    int bytesRead;
    while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
    {
        writer.Write(buffer, 0, bytesRead);
        // writer.Flush(); // Optional: Flush periodically if needed
        System.Threading.Thread.Sleep(100); // Simulate delay
    }
    // writer.Complete(); // No longer needed! Dispose called implicitly by 'using'.
}
```

To run the included C# demo:

```bash
dotnet run --project src/ConsoleInk.Demo/ConsoleInk.Demo.csproj
```

### PowerShell Usage (Direct DLL Loading)

The library can be used directly from PowerShell by loading the compiled DLL.
See the `samples/PowerShell/Demo.ps1` script for a working example.

**Steps:**

1.  **Build the Library:** Ensure `ConsoleInk.Net.dll` exists (e.g., run `dotnet build src/ConsoleInk.sln`).
2.  **Run the Script:**

```powershell
# Navigate to the repository root or adjust paths in the script

# Using default Debug build path:
pwsh -File ./samples/PowerShell/Demo.ps1

# If you built in Release configuration:
pwsh -File ./samples/PowerShell/Demo.ps1 -LibPath '../src/ConsoleInk.Net/bin/Release/net9.0'
```

*(A dedicated `ConsoleInk.PowerShell` module with cmdlets like `ConvertTo-ConsoleMarkdown` and `Show-Markdown` is planned for easier PowerShell integration.)*

## Building & Testing

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/stimpy77/ConsoleInk.NET.git
    cd ConsoleInk.NET
    ```
2.  **Build the solution (including Library, Demo, and Tests):**
    ```bash
    # Builds all projects referenced by the solution
    dotnet build src/ConsoleInk.sln
    ```
    *   This will compile the `ConsoleInk.Net` library.
    *   If building in `Release` configuration (`dotnet build src/ConsoleInk.sln -c Release`), the NuGet package (`.nupkg`) and symbols package (`.snupkg`) will be generated in `src/ConsoleInk.Net/bin/Release/`.

3.  **Run tests:**
    ```bash
    dotnet test src/ConsoleInk.sln
    ```

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
 
