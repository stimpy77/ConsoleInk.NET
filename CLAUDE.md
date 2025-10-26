# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ConsoleInk.NET is a zero-dependency .NET library for rendering Markdown to ANSI-formatted console output. The library targets .NET Standard 2.0 with C# 10.0 features and uses a **streaming-first architecture** with a state machine processor for incremental Markdown rendering.

## Key Architecture Principles

### Streaming-First Design
- Core rendering happens through `MarkdownConsoleWriter`, a `TextWriter` implementation that processes Markdown incrementally
- State machine tracks current context (paragraph, heading, list, code block, etc.) without building a full AST
- Minimal lookahead buffering for resolving ambiguities (e.g., Setext headings, tables)
- Direct output generation to target `TextWriter` as content is processed

### State Management
The `MarkdownConsoleWriter` maintains:
- `_currentBlockType`: Active Markdown block being processed
- `_lastFinalizedBlockType`: Previously completed block (for separation logic)
- `_needsSeparationBeforeNextBlock`: Flag controlling newline insertion between blocks
- `_paragraphBuffer`: Accumulates paragraph text before rendering
- `_lineBuffer`: Buffers input for line-by-line processing
- `_linkDefinitions`: Dictionary of reference-style link definitions
- Table state: `_tableHeaderCells`, `_tableAlignments`, `_tableRows`

### Block Finalization Pattern
When transitioning between Markdown blocks:
1. `FinalizeBlock()` is called to complete the current block
2. Returns boolean indicating if the block produced output
3. Sets `_needsSeparationBeforeNextBlock` flag based on block type
4. `ProcessLine()` consumes this flag to insert appropriate separation

## Development Commands

### Build
**One-step build for everything:**
```bash
dotnet build src/ConsoleInk.sln
```
This builds all projects and automatically copies the ConsoleInk.Net.dll to the PowerShell module location (`src/powershell-module/ConsoleInk/lib/`) via the `ConsoleInk.PowerShell.Build` MSBuild project.

For Release build (generates NuGet packages):
```bash
dotnet build src/ConsoleInk.sln -c Release
```
NuGet package (`.nupkg`) and symbols (`.snupkg`) will be in `src/ConsoleInk.Net/bin/Release/`

**Note:** The PowerShell module DLL is NOT committed to git. It's copied during build via MSBuild targets in `src/powershell-module/ConsoleInk.PowerShell.Build.csproj`.

### Run Tests
```bash
dotnet test src/ConsoleInk.sln
```

Run a specific test:
```bash
dotnet test src/ConsoleInk.Net.Tests/ConsoleInk.Net.Tests.csproj --filter "FullyQualifiedName~Render_BoldEmphasis"
```

### Run Demo
```bash
dotnet run --project src/ConsoleInk.Demo/ConsoleInk.Demo.csproj
```

### PowerShell Testing
Direct DLL usage:
```powershell
pwsh -File ./samples/PowerShell/Demo.ps1
```

## Code Structure

### Core Library (`src/ConsoleInk.Net/`)
- **`MarkdownConsoleWriter.cs`** (90KB): Core state machine and rendering logic
  - `ProcessLine()`: Main state machine dispatcher
  - `DetermineBlockType()`: Identifies Markdown block types from input
  - `FinalizeBlock()`: Completes current block and manages separation
  - `WriteFormattedParagraph()`: Handles inline emphasis, links, images, strikethrough
  - `WriteListItem()`, `WriteHeading()`, `WriteCodeBlock()`, `WriteTable()`: Block-specific renderers
- **`Ansi.cs`**: ANSI escape code constants (colors, styles)
- **`ConsoleTheme.cs`**: Theme definitions (Default, Monochrome) with color/style configuration
- **`MarkdownRenderOptions.cs`**: Configuration options (width, colors, themes, hyperlinks)
- **`MarkdownConsole.cs`**: Static helper for batch rendering

### Tests (`src/ConsoleInk.Net.Tests/`)
- Uses xUnit framework
- Test helper: `AssertRender()` normalizes line endings and provides detailed log output on failure
- Tests verify ANSI output and state transitions for each Markdown feature

### PowerShell Module (`src/powershell-module/ConsoleInk/`)
- Build project: `ConsoleInk.PowerShell.Build.csproj`
- Provides `ConvertTo-Markdown` cmdlet with pipeline support

## Important Implementation Details

### Inline Parsing
`WriteFormattedParagraph()` uses a single-pass parser with:
- Stack-based style tracking (`_activeStyles`) for nested emphasis
- Specific ANSI off-codes (`[22m` for bold, `[23m` for italic, `[29m` for strikethrough) instead of generic reset
- Backslash escaping for literal markers (`\*`, `\_`, `\~`, `\!`, `\[`, `\]`, `\(`, `\)`, `\\`)
- **List items use inline formatting**: `WriteListItem()` calls `WriteFormattedParagraph()` to ensure links, bold, italic, and other inline styles render properly within list items (see line 1042)

### Reference Links Limitation
Due to streaming nature, reference link definitions must appear **before** usage in the input. If a reference link is encountered before its definition, it renders literally. This is documented as a known streaming limitation.

### Table Rendering
Tables require buffering header, separator, and all rows before rendering (non-streaming):
- `DetermineBlockType()` detects table start (header + separator pattern)
- `ProcessLine()` accumulates table rows in `_tableRows`
- `FinalizeBlock()` triggers `WriteTable()` to render complete table with proper alignment and padding

### Block Separation Logic
Controlled by `_needsSeparationBeforeNextBlock` flag:
- Headings, code blocks, lists, tables, blockquotes set this flag on finalization
- Paragraphs only set it if they produced output
- Link definitions never set it (non-rendering)
- Blank lines reset current block but don't force separation
- **Special case for lists**: Consecutive list items of the same list type clear the separation flag to prevent extra newlines between items (see lines 737-751 in `MarkdownConsoleWriter.cs`)

### Test Conventions
- ANSI escape sequences in expected output use constants from `Ansi` class
- Line endings normalized to `\n` in `AssertRender()`
- Emphasis tests expect specific off-codes, not generic `Ansi.Reset`
- Logging framework available via optional `logger` parameter to `MarkdownConsoleWriter`

## Namespace
All public types use namespace `ConsoleInk` (not `ConsoleInk.Net` despite project name). This is intentional for simplicity.

## Debugging Features
- Optional `logger` parameter in `MarkdownConsoleWriter` constructor accepts `Action<string>` for detailed state machine logging
- `_log()` method throughout state machine transitions
- Tests can enable logging via `AssertRender()` helper

## Git Commit Guidelines
- DO NOT add "Co-Authored-By: Claude" or similar co-authorship claims to commit messages
- DO NOT add "Generated with Claude Code" footer to commit messages
- Keep commit messages professional and focused on the technical changes
