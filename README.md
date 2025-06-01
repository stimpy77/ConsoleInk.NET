# ConsoleInk.NET

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/stimpy77/ConsoleInk.NET)
[![NuGet](https://img.shields.io/nuget/v/ConsoleInk.Net.svg)](https://www.nuget.org/packages/ConsoleInk.Net/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

**ConsoleInk.NET** is a lightweight, zero-dependency .NET library for rendering Markdown text directly into ANSI-formatted output suitable for modern console applications, PowerShell, and CI environments. It focuses on streaming processing, enabling efficient rendering of Markdown content as it arrives.

- **Targets:** .NET Standard 2.0 (C# 10.0 features required)
- **NuGet Package:** [ConsoleInk.Net](https://www.nuget.org/packages/ConsoleInk.Net/)
- **PowerShell Module:** Included (`ConsoleInk.PowerShell`) for native PowerShell cmdlets
- **Latest Version:** 0.1.4
- **Compatible with:** .NET Core, .NET Framework, PowerShell 7+, Windows PowerShell 5.1+

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
Install-Package ConsoleInk.Net -Version 0.1.4 # Adjust version if needed
```

### .NET CLI

```bash
dotnet add package ConsoleInk.Net --version 0.1.4 # Adjust version if needed
```

### PowerShell Module (`ConsoleInk.PowerShell`)

A native PowerShell module is included! Use `ConvertTo-Markdown` and `Show-Markdown` cmdlets for Markdown rendering directly in the console, with full support for pipeline input, file input, themes, width, color options, and hyperlinks.

**All debugging logs and manual type checks have been removed**—the module now provides clean, user-friendly output. If `Import-Module` succeeds, all cmdlets will work as shown in the demo. Troubleshooting is only needed if you see a true error message.

See [samples/PowerShell/Demo-Module.ps1](samples/PowerShell/Demo-Module.ps1) for a comprehensive, feature-rich example covering:
- Pipeline input
- File input
- Theme selection (Default/Monochrome)
- Width selection
- Hyperlink rendering (OSC 8)
- Error handling

**Quick Start:**

```powershell
# Import the module (after building the project)
Import-Module "./src/powershell-module/ConsoleInk.PowerShell.psd1" -Force

# Render Markdown from a string
"# Hello from PowerShell!`n*This is a test*" | ConvertTo-Markdown -Theme Default

# Render Markdown from a file
ConvertTo-Markdown -Path ./README.md -Theme Monochrome

# Show-Markdown is an alias for ConvertTo-Markdown
"# Demo" | Show-Markdown -Width 60
```

**Cmdlets:**
- `ConvertTo-Markdown` (main)
- `Show-Markdown` (alias, same parameters)

Supports pipeline and file input, width, theme selection, color toggling, and hyperlinks. See the [Demo-Module.ps1](samples/PowerShell/Demo-Module.ps1) for a full-featured demonstration.

**Tip:** The .NET namespace for all types is `ConsoleInk` (e.g., `[ConsoleInk.MarkdownRenderOptions]`).

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

### PowerShell Usage

#### Option 1: PowerShell Module (Recommended)

Use the official module for native cmdlets. See above for details and [Demo-Module.ps1](samples/PowerShell/Demo-Module.ps1).

#### Option 2: Direct DLL Loading

You can also use ConsoleInk.Net directly from PowerShell by loading the compiled DLL. See [samples/PowerShell/Demo.ps1](samples/PowerShell/Demo.ps1) for a working example.

**Steps:**
1.  **Build the Library:** Ensure `ConsoleInk.Net.dll` exists (e.g., run `dotnet build src/ConsoleInk.sln`).
2.  **Run the Script:**
```powershell
pwsh -File ./samples/PowerShell/Demo.ps1
```
You can specify `-LibPath` if you want to use a custom build directory.

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
    *   The PowerShell module will be available in `src/powershell-module/` after build. Import the module using its `.psd1` manifest.

3.  **Run tests:**
    ```bash
    dotnet test src/ConsoleInk.sln
    ```

## Version Compatibility

- **Library:** .NET Standard 2.0 (requires C# 10.0 features; ensure your build environment supports this)
- **Demo & PowerShell Module:** .NET Standard 2.0
- **Tests:** May target newer .NET for compatibility with latest test SDKs
- **PowerShell:** Compatible with PowerShell 7+ and Windows PowerShell 5.1 (when run with .NET Standard 2.0 DLL)

## Release Notes

- **0.1.4** (2025-06-01):
    - Full .NET Standard 2.0 migration for library, demo, and PowerShell module
    - PowerShell module (`ConsoleInk.PowerShell`) released: `ConvertTo-Markdown` and `Show-Markdown` cmdlets, pipeline/file input, themes, color
    - All build/test/demo projects updated for compatibility and C# 10.0 features
    - Type-mismatch and build errors resolved for .NET Standard
    - Documentation updated for new module and usage

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
 
