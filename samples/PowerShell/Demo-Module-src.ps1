# Demo: Using the ConsoleInk.PowerShell module
# Requires: ConsoleInk.PowerShell module is built and DLL is present in lib/

# Import the module (forces reload)
Import-Module "$PSScriptRoot/../../src/powershell-module/ConsoleInk.PowerShell.psd1" -Force

Write-Host "--- ConsoleInk PowerShell Module Feature Demo ---" -ForegroundColor Cyan

# --- Configuration & Console Width ---
$consoleWidth = 80 # Default
try {
    if ($Host.Name -eq 'ConsoleHost' -and (-not $Host.UI.RawUI.IsContentRedirected)) {
        $consoleWidth = $Host.UI.RawUI.WindowSize.Width
    }
} catch {
    Write-Warning "Could not determine console width. Using default: $consoleWidth"
}

# --- Sample Markdown ---
$markdown = @"
# Sample Markdown in PowerShell

Demonstrating **ConsoleInk.PowerShell** from a `$PSVersionTable.PSVersion` script.

## Features Used

*   Using `ConvertTo-Markdown` and `Show-Markdown` cmdlets.
*   Pipeline, file, and streaming input.
*   Configuring theme and width.
*   Hyperlink rendering (OSC 8).

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

# --- Demo 1: Static Render with ConvertTo-Markdown ---
Write-Host "`n--- Demo 1: Static render with ConvertTo-Markdown ---" -ForegroundColor Green
try {
    $renderedOutput = $markdown | ConvertTo-Markdown -Theme Default -Width $consoleWidth
    Write-Host $renderedOutput
} catch {
    Write-Error "Error during static rendering: $($_.Exception.Message)"
}
Write-Host "`n-------------------------------------------------"
Read-Host -Prompt "Press Enter to start the hyperlink demo..."

# --- Demo 2: Hyperlink Demo ---
Write-Host "`n--- Demo 2: Hyperlink rendering (OSC 8, if supported) ---" -ForegroundColor Green
$hyperlinkMarkdown = @"
# Hyperlink Feature Demo

Here are some **clickable links** that use the OSC 8 hyperlink protocol.

## Standard Link
* [Microsoft Docs](https://learn.microsoft.com/powershell)

## Reference Style Link
* [PowerShell Gallery][PSGallery]

[PSGallery]: https://www.powershellgallery.com
"@
try {
    $hyperlinkOutput = $hyperlinkMarkdown | ConvertTo-Markdown -Theme Default -Width $consoleWidth
    Write-Host $hyperlinkOutput
    Write-Host "Note: Hyperlinks are rendered using OSC 8 escape sequences if supported by your terminal (e.g., Windows Terminal)." -ForegroundColor Yellow
} catch {
    Write-Error "Error during hyperlink rendering: $($_.Exception.Message)"
}
Write-Host "`n-------------------------------------------------"
Read-Host -Prompt "Press Enter to start the streaming demo..."

# --- Demo 3: Streaming/Line-by-Line Demo ---
Write-Host "`n--- Demo 3: Streaming/line-by-line rendering ---" -ForegroundColor Green
Write-Host "Rendering line-by-line (simulated stream):`n"
try {
    $lines = $markdown -split "`n"
    foreach ($line in $lines) {
        $line | ConvertTo-Markdown -Theme Default -Width $consoleWidth | Write-Host
        Start-Sleep -Milliseconds 100
    }
} catch {
    Write-Error "Error during streaming rendering: $($_.Exception.Message)"
}
Write-Host "`n-------------------------------------------------"
Read-Host -Prompt "Press Enter to start the file input demo..."

# --- Demo 4: File Input (Monochrome Theme) ---
Write-Host "`n--- Demo 4: File input with Monochrome theme ---" -ForegroundColor Green
$tmpFile = [System.IO.Path]::GetTempFileName()
try {
    Set-Content -Path $tmpFile -Value $markdown -Encoding UTF8
    ConvertTo-Markdown -Path $tmpFile -Theme Monochrome -Width $consoleWidth | Write-Host
} catch {
    Write-Error "Error during file input rendering: $($_.Exception.Message)"
} finally {
    Remove-Item $tmpFile -ErrorAction SilentlyContinue
}
Write-Host "`n-------------------------------------------------"
Read-Host -Prompt "Press Enter to start the Show-Markdown demo..."

# --- Demo 5: Show-Markdown with Width Selection ---
Write-Host "`n--- Demo 5: Show-Markdown alias with custom width (60) ---" -ForegroundColor Green
$markdown | Show-Markdown -Width 60
Write-Host "`n-------------------------------------------------"
Read-Host -Prompt "Press Enter to start the error handling demo..."

# --- Demo 6: Error Handling Demo ---
Write-Host "`n--- Demo 6: Error handling (attempt to render non-existent file) ---" -ForegroundColor Green
try {
    ConvertTo-Markdown -Path 'Z:\this\file\does\not\exist.md'
} catch {
    Write-Host "Caught error as expected: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n--- End of ConsoleInk PowerShell module feature demo ---" -ForegroundColor Cyan
Read-Host -Prompt "Press Enter to exit."
