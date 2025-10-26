#Requires -Version 7.2

# NOTE: This script demonstrates direct DLL usage, not the PowerShell module.
# For module usage, see Demo-Module.ps1 in this folder.

<#
.SYNOPSIS
Demonstrates using the ConsoleInk.Net library directly from PowerShell.

.DESCRIPTION
This script loads the ConsoleInk.Net.dll assembly using Add-Type and then
uses its classes (MarkdownConsole, MarkdownConsoleWriter, MarkdownRenderOptions)
to render sample Markdown text to the console.

It shows both batch rendering (using MarkdownConsole.Render) and streaming
rendering (using MarkdownConsoleWriter).

.NOTES
Ensure the ConsoleInk.Net project has been built before running this script,
as it needs to load the DLL from the build output directory.
The relative path to the DLL might need adjustment based on your build configuration
(e.g., Debug/Release) and target framework.

PowerShell 7.2+ is recommended for better compatibility with modern .NET assemblies.
#>

param (
    # Relative path to the directory containing the ConsoleInk.Net.dll
    # Adjust if your build configuration or target framework differs.
    [string]$LibPath = '../../src/ConsoleInk.Net/bin/Debug/netstandard2.0'
)

# --- Configuration ---
Write-Host "--- PowerShell Demo for ConsoleInk.Net ---" -ForegroundColor Cyan

# Construct the full path to the DLL
$dllPath = Join-Path -Path $PSScriptRoot -ChildPath $LibPath -Resolve
$dllFullPath = Join-Path -Path $dllPath -ChildPath 'ConsoleInk.Net.dll'

if (-not (Test-Path $dllFullPath)) {
    Write-Error "ConsoleInk.Net.dll not found at '$dllFullPath'. Please build the 'ConsoleInk.Net' project first (e.g., 'dotnet build ../src/ConsoleInk.sln')."
    exit 1
}

# Load the Assembly
Write-Host "Loading Assembly: $dllFullPath"
try {
    Add-Type -Path $dllFullPath -ErrorAction Stop
} catch {
    Write-Error "Failed to load ConsoleInk.Net assembly. Error: $($_.Exception.Message)"
    exit 1
}

# Sample Markdown Text (using PowerShell Here-String)
$markdown = @"
# Sample Markdown in PowerShell

Demonstrating **ConsoleInk.Net** from a `$PSVersionTable.PSVersion` script.

## Features Used

*   Loading `.NET DLL` via `Add-Type`.
*   Calling static `[ConsoleInk.MarkdownConsole]::Render()`.
*   Using the `[ConsoleInk.MarkdownConsoleWriter]` stream.
*   Configuring `[ConsoleInk.MarkdownRenderOptions]`.

1.  First item
2.  Second item
    *   Nested bullet

```powershell
# PowerShell code block
Get-Process | Sort-Object CPU -Descending | Select-Object -First 5
```

> Quoting works too!

Check `inline code` and ~~strikethrough~~.
[Visit PowerShell Gallery](https://www.powershellgallery.com)
"@

# Get Console Width Safely
$consoleWidth = 80 # Default
try {
    if ($Host.Name -eq 'ConsoleHost' -and (-not $Host.UI.RawUI.IsContentRedirected)) {
        $consoleWidth = $Host.UI.RawUI.WindowSize.Width
    }
} catch { 
    Write-Warning "Could not determine console width. Using default: $consoleWidth" 
}

# --- Demo 1: Static Render Method ---
Write-Host "`n--- Demo using static [MarkdownConsole]::Render ---`n" -ForegroundColor Green

# Prepare Render Options
$renderOptions = [ConsoleInk.MarkdownRenderOptions]::new()
$renderOptions.ConsoleWidth = $consoleWidth
$renderOptions.EnableColors = $true
$renderOptions.Theme = [ConsoleInk.ConsoleTheme]::Default # Or ::Monochrome

# Render the whole markdown string at once
try {
    $renderedOutput = [ConsoleInk.MarkdownConsole]::Render($markdown, $renderOptions)
    Write-Host $renderedOutput
} catch {
    Write-Error "Error during static rendering: $($_.Exception.Message)"
}

Write-Host "`n-------------------------------------------------"
Read-Host -Prompt "Press Enter to start the hyperlink demo..."

# --- Demo 1.5: Hyperlink Demo ---
Write-Host "`n--- Demo showing clickable hyperlinks ---`n" -ForegroundColor Green

# Sample markdown with links
$hyperlinkMarkdown = @"
# Hyperlink Feature Demo

Here are some **clickable links** that use the OSC 8 hyperlink protocol.

## Standard Link
* [Microsoft Docs](https://learn.microsoft.com/powershell)

## Reference Style Link
* [PowerShell Gallery][PSGallery]

[PSGallery]: https://www.powershellgallery.com
"@

# Create options with hyperlinks enabled
$hyperlinkOptions = [ConsoleInk.MarkdownRenderOptions]::new()
$hyperlinkOptions.ConsoleWidth = $consoleWidth
$hyperlinkOptions.EnableColors = $true
$hyperlinkOptions.Theme = [ConsoleInk.ConsoleTheme]::Default
$hyperlinkOptions.UseHyperlinks = $true # Enable hyperlinks!

# Render the hyperlink markdown
try {
    $hyperlinkOutput = [ConsoleInk.MarkdownConsole]::Render($hyperlinkMarkdown, $hyperlinkOptions)
    Write-Host $hyperlinkOutput
    
    Write-Host "Note: Hyperlinks are rendered using OSC 8 escape sequences." -ForegroundColor Yellow
    Write-Host "They are clickable in terminals that support this feature (like Windows Terminal)." -ForegroundColor Yellow
} catch {
    Write-Error "Error during hyperlink rendering: $($_.Exception.Message)"
}

Write-Host "`n-------------------------------------------------"
Read-Host -Prompt "Press Enter to start the streaming demo..."

# --- Demo 2: Streaming Render Method ---
Write-Host "`n--- Demo using streaming [MarkdownConsoleWriter] ---`n" -ForegroundColor Green
Write-Host "Rendering line-by-line (simulated stream):`n"

# Use the streaming writer targeting PowerShell's host output (simulates Console.Out)
# Note: Directing to $Host.UI.Write() might be complex. Using Console.Out is simpler via the library.
# We'll wrap Console.Out. For pure PowerShell host, more work might be needed if Console is unavailable.

$outputWriter = [System.Console]::Out
$markdownWriter = $null
$stringReader = $null

try {
    # Use same options as before
    $markdownWriter = [ConsoleInk.MarkdownConsoleWriter]::new($outputWriter, $renderOptions)
    $stringReader = [System.IO.StringReader]::new($markdown)

    # Read and write line by line
    while (($line = $stringReader.ReadLine()) -ne $null) {
        $markdownWriter.WriteLine($line)
        $markdownWriter.Flush() # Ensure output is written immediately
        Start-Sleep -Milliseconds 100 # Simulate delay
    }

    $markdownWriter.Complete() # IMPORTANT: Signal end of input
    $markdownWriter.Flush()

} catch {
    Write-Error "Error during streaming rendering: $($_.Exception.Message)"
} finally {
    # Dispose disposable objects
    if ($markdownWriter -is [System.IDisposable]) {
        $markdownWriter.Dispose()
    }
    if ($stringReader -is [System.IDisposable]) {
        $stringReader.Dispose()
    }
    # Do NOT dispose Console.Out
}


Write-Host "`n-------------------------------------------------"
Write-Host "--- PowerShell Demo Complete ---`n" -ForegroundColor Cyan
Read-Host -Prompt "Press Enter to exit." 