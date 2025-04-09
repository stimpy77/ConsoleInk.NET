# ConsoleInk.NET Development State

## Goal

Enhance the `MarkdownConsoleWriter` to:

1. Reliably handle block-level elements (Paragraphs, Headings, Lists, Code Blocks, Tables, Blockquotes) with correct transitions and newline separation according to Markdown conventions. (**DONE**)
2. Implement inline styling for *italic*, **bold**, ~~strikethrough~~ using ANSI escape codes. (**DONE**)
3. Implement remaining core CommonMark features (Links, Images, HTML Stripping). (**DONE**, except reference link limitations)
4. Implement core GFM features (Task Lists, Basic Tables). (**DONE** for simple tables and task lists)

## Accomplished

* **Refactored Block Processing:** Significantly refactored `ProcessLine` and related methods for clearer state transitions, block finalization, and style application through multiple iterations.
* **Integrated List Parsing:** Moved list item detection into `DetermineBlockType`.
* **Centralized Finalization:** Created `FinalizeBlock` for cleanup, modified to return `bool` indicating output and set state flag.
* **Fixed List Numbering & Indentation:** Corrected logic for ordered lists and passed detected indentation to `WriteListItem`. Added missing space after list bullets.
* **Block Separation Logic:** Introduced `_needsSeparationBeforeNextBlock` flag set by `FinalizeBlock` based on `RequiresSeparationAfter`. `ProcessLine` consumes this flag at its start. Logic now correctly handles separation between most block types, including transitions involving blank lines and non-rendering blocks like `LinkDefinition`. (**RESOLVED**)
* **Code Block Handling:** Added styling and indentation trimming for code blocks. Ensured correct newline handling.
* **Implemented Emphasis Parsing:** Added `WriteFormattedParagraph` to handle inline styles.
* **Fixed Emphasis Logic:**
  * Correctly implemented parsing for `*`, `_`, `**`, `__`, `~~` markers.
  * Implemented stack-based logic to handle nested styles (e.g., `**bold *italic* bold**`).
  * Handled combined styles (e.g., `***bold italic***`).
  * Implemented escaping (`\*`, `\_`, `\~`, `\!`, `\[`, `\]`, `\(`, `\)`, `\\`) to allow literal markers.
* **Corrected ANSI Style Closing:** Ensured specific off-codes (`[22m`, `[23m`, `[29m`) are used instead of a generic reset when styles end, unless the style stack is empty.
* **Fixed Paragraph Newlines & Wrapping:** Resolved issue where paragraphs sometimes had extra trailing newlines by removing explicit newline from `WriteParagraph` and relying on separation logic. Improved word wrapping space handling (though warnings exist).
* **Test Alignment:** Adjusted emphasis tests (`Render_BoldEmphasis`, etc.) to expect specific off-codes instead of a final `Ansi.Reset`. Adjusted newline expectations in many tests to use `Environment.NewLine` and reflect logic changes (e.g., double newline for headings).
* **All Emphasis Tests Passing.**
* **Implemented Blockquotes:** Added `Blockquote` block type, parsing (`> `), styling, and tests.
* **Fixed Inline & Reference Links:** Corrected parsing logic in `WriteFormattedParagraph` and state handling in `ProcessLine`/`Complete` to correctly handle inline links, reference link definitions (`[label]: url`), and reference link usage (`[text][label]`, etc.). Acknowledged streaming limitation: reference links render literally if definition appears *after* usage. Corrected newline handling around `LinkDefinition` blocks. All link tests now pass (after adjusting expectations for literal rendering where appropriate and correct separation).
* **Implemented Inline Images:** Added parsing for `![alt](url)` to render styled alt text.
* **Implemented Basic HTML Stripping:** Added simple `<...>` tag stripping in `WriteFormattedParagraph`.
* **Implemented GFM Task Lists:** Added parsing for `- [ ]` and `- [x]` within list items, styling, and tests.
* **Refactored GFM Table State Machine:** Corrected `DetermineBlockType` and `ProcessLine` to properly handle transitions into and continuation of table blocks. Prevented extra newline before table start.
* **Fixed GFM Table Rendering (Simple Case):** Corrected padding logic in `WriteTable`, column width calculation, and separator generation logic. `Render_SimpleTable` now passes. (**RESOLVED** for simple case)
* **Fixed Skipped Test:** Corrected logic and expectations for `Render_ListThenCodeBlock`.
* **Debugging:** Added detailed logging framework (`_logger`, `_log`), used it to diagnose issues. Corrected various test expectations based on logs and refined logic.
* **Test Harness Fix:** Corrected `AssertRender` to handle line endings consistently (normalize both to LF), accept `MarkdownRenderOptions`, and provide detailed log output on failure.
* **Fixed `Write(string)` Multi-line Handling:** Corrected parsing logic to handle input strings containing multiple `\r\n` or `\n` correctly.
* **Fixed Heading Rendering:** Ensured `WriteHeading` was called correctly from `ProcessLine`.
* **Fixed GFM Table Alignment & Missing Cells:** Corrected `ParseAndStoreTableHeaderAndSeparator` to handle missing separator cells, ensuring alignment list matches column count. Corrected `WriteTable` to use actual column alignment for header padding. Adjusted `SplitTableRow` for robustness. Corrected expected output in related tests (`Render_TableWithAlignment`, `Render_TableWithMissingCells`). (**RESOLVED**)
* **Added C# Demo Project:** Created `ConsoleInk.Demo` project with example usage and added it to the solution.
* **Configured NuGet Packaging:** Updated `ConsoleInk.Net.csproj` with metadata, SourceLink, and file includes for NuGet package generation.
* **Added PowerShell Sample Script:** Created `samples/PowerShell/Demo.ps1` demonstrating direct DLL usage.
* **Updated README:** Reflected project structure changes, demo/sample additions, and packaging setup in `README.md`.

## Current Test Status

* **Passing:** 57 / 57
* **Skipped:** 0
* **Failing:** 0

## Remaining Issues / Future Work

* **~~Fix Skipped Table Tests:~~** ~~Address alignment (`Render_TableWithAlignment`) and missing cell handling (`Render_TableWithMissingCells`) in `WriteTable`.~~ (**DONE**)
* **Address Compiler Warnings:** Fix unused `skippedSpace` variable and potential null conversion warnings identified during builds.
* **Recursive Inline Parsing:** Implement recursive parsing within elements like link text (`[**bold**](url)`) and image alt text (`![*alt*](url)`).
* **Refine HTML Handling:** Implement more robust HTML stripping or potentially basic tag support.
* **Refine PowerShell module (`ConsoleInk.PowerShell`).**
* **Add comprehensive documentation and examples.**
