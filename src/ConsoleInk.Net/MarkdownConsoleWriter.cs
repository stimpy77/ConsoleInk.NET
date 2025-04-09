using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;

namespace ConsoleInk
{
    /// <summary>
    /// Represents the type of Markdown block currently being processed.
    /// </summary>
    internal enum MarkdownBlockType
    {
        None, // Initial state or between blocks
        Paragraph,
        Heading1,
        Heading2,
        Heading3,
        UnorderedList, // Represents being within an unordered list item
        OrderedList,   // Represents being within an ordered list item
        CodeBlock, // Placeholder for later
        Blockquote, // Placeholder for later
        LinkDefinition, // A line defining a reference link, e.g., [label]: url "title"
        Table // GFM Table
    }

    /// <summary>
    /// Specifies text alignment within a table column.
    /// </summary>
    internal enum ColumnAlignment
    {
        Left,
        Center,
        Right
    }

    /// <summary>
    /// A TextWriter implementation that processes Markdown input incrementally
    /// and writes formatted output to an underlying TextWriter (typically Console.Out).
    /// </summary>
    public class MarkdownConsoleWriter : TextWriter
    {
        private readonly TextWriter _outputWriter;
        private readonly MarkdownRenderOptions _options;
        private readonly StringBuilder _lineBuffer = new StringBuilder();
        private readonly StringBuilder _paragraphBuffer = new StringBuilder(); // Buffer for the current paragraph being built
        private readonly StringWriter _bufferWriter = new();
        private readonly Stack<string> _activeStyles = new(); // Track currently applied ANSI codes
        private bool _isDisposed = false;

        private MarkdownBlockType _currentBlockType = MarkdownBlockType.None;
        private MarkdownBlockType _lastFinalizedBlockType = MarkdownBlockType.None; // Track the last block type that was finalized
        private bool _lastFinalizedBlockProducedOutput = false; // Track output status of last finalized block
        private int _orderedListCounter;
        private readonly int _maxWidth;
        private readonly Dictionary<string, (string Url, string? Title)> _linkDefinitions = new(StringComparer.OrdinalIgnoreCase); // Store [label] -> (url, title)
        private bool _needsSeparationBeforeNextBlock = false; // NEW: Flag to indicate a separator is needed

        // Table State
        private List<string>? _tableHeaderCells;
        private List<ColumnAlignment>? _tableAlignments;
        private List<string>? _tableSeparatorStrings; // ADDED: Store the raw separator cell strings (e.g., "---", ":--:")
        private List<List<string>>? _tableRows;

        // Logger
        private readonly Action<string>? _logger; // Optional logger delegate

        // Regex to match reference link definitions: [label]: url "optional title" or 'optional title' or (optional title)
        // Allows optional whitespace. Captures: 1=label, 2=url, 4=double-quoted title, 5=single-quoted title, 6=parenthesized title
        private static readonly Regex LinkDefinitionRegex =
            new Regex(
                pattern: @"^\s*\[([^\]]+)\]:\s*(\S+)(?:\s+(?:""([^""]*)""|'([^']*)'|\\(([^\\)]*)\\)))?\s*$",
                options: RegexOptions.Compiled
            );

        /// <summary>
        /// Gets the encoding for this writer (defaults to the output writer's encoding).
        /// </summary>
        public override Encoding Encoding => _outputWriter.Encoding;

        /// <summary>
        /// Initializes a new instance of the <see cref="MarkdownConsoleWriter"/> class.
        /// </summary>
        /// <param name="outputWriter">The underlying TextWriter to write formatted output to.</param>
        /// <param name="options">Optional configuration for rendering.</param>
        /// <param name="logger">Optional action to receive detailed logging messages.</param>
        public MarkdownConsoleWriter(TextWriter outputWriter, MarkdownRenderOptions? options = null, Action<string>? logger = null)
        {
            _outputWriter = outputWriter ?? throw new ArgumentNullException(nameof(outputWriter));
            _options = options ?? new MarkdownRenderOptions();
            _maxWidth = _options.ConsoleWidth > 0 ? _options.ConsoleWidth : 80;
            _logger = logger; // Store the logger
            // Initial state is None
            _log($"Initialized MarkdownConsoleWriter. MaxWidth={_maxWidth}, EnableColors={_options.EnableColors}, StripHtml={_options.StripHtml}, Theme={_options.Theme.GetType().Name}");
        }

        /// <summary>
        /// Helper method to safely invoke the logger.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private void _log(string message)
        {
            _logger?.Invoke($"[CI: {_currentBlockType}] {message}");
        }

        /// <summary>
        /// Writes a single character to the writer.
        /// Characters are buffered line by line for processing.
        /// </summary>
        /// <param name="value">The character to write.</param>
        public override void Write(char value)
        {
            CheckDisposed();
            if (value == '\n')
            {
                var lineToProcess = _lineBuffer.ToString();
                _log($"Write(char): Newline detected. Processing line: \"{lineToProcess}\"");
                ProcessLine(lineToProcess);
                _lineBuffer.Clear();
                // Newline character itself doesn't get written directly;
                // ProcessLine handles block separation and line endings.
            }
            else if (value == '\r')
            {
                _log("Write(char): Carriage return ignored.");
                // Ignore carriage returns often paired with newlines
            }
            else
            {
                _log($"Write(char): Appending '{value}'");
                _lineBuffer.Append(value);
            }
        }

        /// <summary>
        /// Writes a string to the writer.
        /// </summary>
        /// <param name="value">The string to write.</param>
        public override void Write(string? value)
        {
            CheckDisposed();
            if (value == null) return;
            _log($"Write(string): Received: \"{value.Replace("\n", "\\n").Replace("\r", "\\r")}\"");

            // Process string potentially containing multiple lines
            int start = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];

                // Handle line breaks (LF or CRLF)
                if (c == '\n')
                {
                    // Append the segment before the newline (handle potential preceding CR)
                    int length = (i > start && value[i - 1] == '\r') ? i - start - 1 : i - start;
                    if (length > 0)
                    {
                        _lineBuffer.Append(value.Substring(start, length));
                        _log($"Write(string): Appended line segment: \"{_lineBuffer}\"");
                    }
                    
                    // Process the complete line
                    var lineToProcess = _lineBuffer.ToString();
                    _log($"Write(string): Newline found. Processing line: \"{lineToProcess}\"");
                    ProcessLine(lineToProcess);
                    _lineBuffer.Clear();
                    start = i + 1; // Move start past the \n
                }
                // Note: We don't need special handling for \r here, 
                // it's handled when we find the corresponding \n.
                // Standalone \r characters are effectively ignored unless followed by \n.
            }

            // Append any remaining part of the string after the last newline (or the whole string if no newlines)
            if (start < value.Length)
            {
                var remainingPart = value.Substring(start);
                _log($"Write(string): Appending remaining part: \"{remainingPart.Replace("\n", "\\n").Replace("\r", "\\r")}\"");
                _lineBuffer.Append(remainingPart);
            }
            _log($"Write(string): Finished processing input. Current buffer: \"{_lineBuffer}\"");
        }

        /// <summary>
        /// Writes a string followed by a line terminator to the writer.
        /// </summary>
        /// <param name="value">The string to write.</param>
        public override void WriteLine(string? value)
        {
            CheckDisposed();
            _log($"WriteLine(string): Value: \"{value?.Replace("\n", "\\n").Replace("\r", "\\r") ?? "null"}\"");
            Write(value);
            Write('\n'); // This triggers line processing via Write(char)
        }

        /// <summary>
        /// Writes a line terminator to the writer.
        /// </summary>
        public override void WriteLine()
        {
            CheckDisposed();
            _log("WriteLine(): Blank line.");
            Write('\n'); // This triggers line processing via Write(char)
        }

        // Internal helper for writing directly to output, handling potential prefix newline
        private void WriteToOutput(string text, bool appendAnsiReset = false)
        {
            _outputWriter.Write(text);

            // Append reset code if requested and if colors are enabled and any styles were active
            if (appendAnsiReset && _options.EnableColors && _activeStyles.Count > 0)
            {
                _outputWriter.Write(Ansi.Reset); 
                // Note: This simple reset might clear styles needed by outer blocks.
                // More sophisticated state management might be needed later.
                _activeStyles.Clear(); // Assume reset clears everything for now
            }
        }

        /// <summary>
        /// Applies an ANSI style code if colors are enabled.
        /// </summary>
        private void ApplyStyle(string styleCode)
        {
            if (_options.EnableColors && !string.IsNullOrEmpty(styleCode))
            {
                _outputWriter.Write(styleCode);
                _activeStyles.Push(styleCode); // Track applied style
            }
        }

        /// <summary>
        /// Resets the last applied ANSI style if colors are enabled.
        /// More sophisticated reset logic might be needed for nested styles.
        /// </summary>
        private void ResetCurrentStyle() // Might need a parameter for specific style later
        {
            if (_options.EnableColors && _activeStyles.Count > 0)
            {
                _outputWriter.Write(Ansi.Reset); // Simple reset for now
                _activeStyles.Clear(); // Assume reset clears everything
            }
        }

        /// <summary>
        /// Signals that all input has been written and any final processing or cleanup should occur.
        /// </summary>
        public void Complete()
        {
            CheckDisposed();
            _log("Complete(): Entered.");

            if(_lineBuffer.Length > 0)
            {
                var finalLine = _lineBuffer.ToString();
                _log($"Complete(): Processing final buffer content: \"{finalLine}\"");
                ProcessLine(finalLine); // Process final partial line if it exists
                _lineBuffer.Clear(); // Clear after processing
            }

            var lastBlock = _currentBlockType;
            _log($"Complete(): Finalizing last block: {lastBlock}");
            FinalizeBlock(lastBlock); // End the very last block correctly

            _log("Complete(): Flushing output writer.");
            _outputWriter.Flush(); // Force flush to underlying writer
            _log("Complete(): Exiting.");
        }

        /// <summary>
        /// Disposes the writer, ensuring completion.
        /// </summary>
        public new void Dispose()
        {
            Complete();
            GC.SuppressFinalize(this);
        }

        // --- Async Overrides (Delegating to sync methods for simplicity initially) ---

        public override Task WriteAsync(char value)
        {
            Write(value);
            return Task.CompletedTask;
        }

        public override Task WriteAsync(string? value)
        {
            Write(value);
            return Task.CompletedTask;
        }

        public override Task WriteLineAsync(string? value)
        {
            WriteLine(value);
            return Task.CompletedTask;
        }

        public override Task WriteLineAsync()
        {
            WriteLine();
            return Task.CompletedTask;
        }

        public override Task FlushAsync()
        {
            Flush();
            return Task.CompletedTask;
        }

        // --- Dispose Pattern ---

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Ensure completion before disposing
                    Complete(); 
                    // We don't dispose _outputWriter as we don't own it.
                    _bufferWriter.Dispose(); // Dispose the internal StringWriter buffer
                }
                _isDisposed = true;
            }
            // Call base dispose AFTER our logic
            base.Dispose(disposing);
        }

        private void CheckDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        // Centralized method to finalize the content of the current block
        private bool FinalizeBlock(MarkdownBlockType blockType)
        {
            _log($"FinalizeBlock START: BlockType={blockType}");
            bool producedOutput = false;

            switch (blockType)
            {
                case MarkdownBlockType.Paragraph:
                    if (_paragraphBuffer.Length > 0)
                    {
                        var paragraphText = _paragraphBuffer.ToString();
                        _log($"FinalizeBlock: Finalizing Paragraph. Content length: {_paragraphBuffer.Length}. Calling WriteParagraph.");
                        WriteParagraph(paragraphText); // Renders content + its own trailing newline
                        _paragraphBuffer.Clear();
                        producedOutput = true; 
                    }
                    else
                    {
                        _log("FinalizeBlock: Finalizing Paragraph. Buffer empty, no output.");
                    }
                    break;
                case MarkdownBlockType.CodeBlock:
                    _log("FinalizeBlock: Finalizing CodeBlock. Resetting style.");
                    ResetCurrentStyle(); // Reset style
                    // Code block lines write their own newlines. Finalization only resets style.
                    producedOutput = true; // Assume if we were in code block, output happened.
                    break;
                case MarkdownBlockType.UnorderedList:
                case MarkdownBlockType.OrderedList:
                    _log($"FinalizeBlock: Finalizing List (Type: {blockType}). Resetting counter.");
                    _orderedListCounter = 0; // Reset counter
                    // List items write their own newlines. Finalization only resets state.
                    producedOutput = true; // Assume if we were in list, output happened.
                    break;
                case MarkdownBlockType.Heading1:
                case MarkdownBlockType.Heading2:
                case MarkdownBlockType.Heading3:
                    // Headings write their own content and newline in WriteHeading.
                    // Finalization does nothing here, but the transition logic handles separation.
                    _log($"FinalizeBlock: Finalizing Heading (Type: {blockType}). No action needed here.");
                    producedOutput = true; // Heading always produces output.
                    break;
                case MarkdownBlockType.Blockquote:
                     // Blockquote lines write their own newlines.
                     _log("FinalizeBlock: Finalizing Blockquote. No specific action needed here.");
                    producedOutput = true; // Assume if we were in quote, output happened.
                    break;
                case MarkdownBlockType.LinkDefinition:
                    // No output
                    _log("FinalizeBlock: Finalizing LinkDefinition. No output.");
                    producedOutput = false;
                    break;
                case MarkdownBlockType.Table:
                    _log("FinalizeBlock: Finalizing Table. Calling WriteTable.");
                    WriteTable(); // Renders table + its own trailing newline
                    _tableHeaderCells = null;
                    _tableAlignments = null;
                    _tableSeparatorStrings = null;
                    _tableRows = null;
                    producedOutput = true;
                    break;
                case MarkdownBlockType.None:
                    // Nothing to finalize
                    _log("FinalizeBlock: Finalizing None. No action.");
                    producedOutput = false;
                    break;
            }
            _lastFinalizedBlockProducedOutput = producedOutput;
            _lastFinalizedBlockType = blockType;
            _log($"FinalizeBlock: Set LastFinalizedBlockType={_lastFinalizedBlockType}, LastProducedOutput={_lastFinalizedBlockProducedOutput}.");
            
            // Set the flag if separation is needed AFTER this block finishes
            bool needsSep = RequiresSeparationAfter(blockType);
            if (producedOutput && needsSep) 
            {
                _log("FinalizeBlock: Setting _needsSeparationBeforeNextBlock = true.");
                _needsSeparationBeforeNextBlock = true;
            }

            _log($"FinalizeBlock END: Returning {producedOutput}");
            return producedOutput;
        }

        // Helper to check if a block type inherently requires separation AFTER it.
        private bool RequiresSeparationAfter(MarkdownBlockType blockType)
        {
            bool result = blockType switch
            {
                MarkdownBlockType.Paragraph or
                MarkdownBlockType.UnorderedList or
                MarkdownBlockType.OrderedList or
                MarkdownBlockType.CodeBlock or
                MarkdownBlockType.Blockquote or
                MarkdownBlockType.Table or
                MarkdownBlockType.Heading1 or
                MarkdownBlockType.Heading2 or
                MarkdownBlockType.Heading3 => true,
                _ => false
            };
            _log($"RequiresSeparationAfter({blockType}): Returning {result}");
            return result;
        }

        private MarkdownBlockType DetermineBlockType(string line, out string content, out int levelOrIndent)
        {
            _log($"DetermineBlockType START: Line='{line}'");
            content = line; // Default
            levelOrIndent = 0;

            if (string.IsNullOrWhiteSpace(line))
            {
                _log("DetermineBlockType: Line is NullOrWhiteSpace.");
                // If the previous block was not None, finalize it.
                if (_currentBlockType != MarkdownBlockType.None)
                {
                    var blockToFinalize = _currentBlockType;
                    _lastFinalizedBlockType = blockToFinalize;
                    _log($"DetermineBlockType: Blank line detected, current block {blockToFinalize} needs finalization (though ProcessLine usually handles this first). Calling FinalizeBlock.");
                    bool producedOutput = FinalizeBlock(blockToFinalize); // Sets flag internally
                    _currentBlockType = MarkdownBlockType.None;

                    if (producedOutput)
                    {
                        // This WriteLine was likely causing extra newlines. ProcessLine handles separation.
                        // _outputWriter.WriteLine(); 
                        _log("DetermineBlockType: Blank line after finalized block that produced output. (Separation handled by ProcessLine flag)");
                        _lastFinalizedBlockProducedOutput = false; // Reset flag (or should ProcessLine do this?)
                    }
                }
                else
                {
                     _log("DetermineBlockType: Blank line, current block already None.");
                }
                // If previous block was already None (e.g., multiple blank lines), do nothing.
                _log("DetermineBlockType END (Blank Line): Returning None.");
                return MarkdownBlockType.None;
            }

            string trimmedLine = line.TrimStart();
            int indentation = line.Length - trimmedLine.Length;
            levelOrIndent = indentation;
            _log($"DetermineBlockType: Trimmed='{trimmedLine}', Indentation={indentation}");

            // 1. Link Definition (Must be at start of line, no indent allowed for definition itself)
            if (indentation == 0)
            {
                var match = LinkDefinitionRegex.Match(line);
                if (match.Success)
                {
                    string label = match.Groups[1].Value;
                    string url = match.Groups[2].Value;
                    // Combine optional title captures
                    string? title = match.Groups[4].Success ? match.Groups[4].Value :
                                    match.Groups[5].Success ? match.Groups[5].Value :
                                    match.Groups[6].Success ? match.Groups[6].Value :
                                    null;
                    
                    _log($"DetermineBlockType: Matched Link Definition. Label='{label}', Url='{url}', Title='{title ?? "null"}'");
                    // Store the definition (label is case-insensitive key)
                    _linkDefinitions[label] = (url, title);
                    content = string.Empty; // No content for this block type
                    _log("DetermineBlockType END (LinkDefinition): Returning LinkDefinition.");
                    return MarkdownBlockType.LinkDefinition;
                }
            }

            // 2. Heading Check (No indent allowed)
            if (indentation == 0 && trimmedLine.StartsWith("#"))
            {
                int headingLevel = trimmedLine.TakeWhile(c => c == '#').Count();
                if (headingLevel >= 1 && headingLevel <= 3 && trimmedLine.Length > headingLevel && trimmedLine[headingLevel] == ' ')
                {
                    levelOrIndent = headingLevel;
                    content = trimmedLine.Substring(headingLevel + 1).Trim();
                    var headingType = GetHeadingBlockType(headingLevel);
                    _log($"DetermineBlockType: Matched Heading (ATX). Level={headingLevel}, Content='{content}'. Returning {headingType}.");
                    _log($"DetermineBlockType END (Heading): Returning {headingType}.");
                    return headingType;
                }
                 _log("DetermineBlockType: Matched '#' but not a valid Heading format.");
                // else: Falls through to paragraph
            }

            // 3. List Item Check
            // Check for Unordered List
            bool isUnordered = trimmedLine.Length >= 2 && (trimmedLine.StartsWith("* ") || trimmedLine.StartsWith("- ") || trimmedLine.StartsWith("+ "));
            if (isUnordered)
            {
                content = trimmedLine.Substring(2);
                levelOrIndent = indentation; // Store original indentation for list item
                _log($"DetermineBlockType: Matched Unordered List. Indent={levelOrIndent}, Content='{content}'. Returning UnorderedList.");
                _log("DetermineBlockType END (UnorderedList): Returning UnorderedList.");
                return MarkdownBlockType.UnorderedList;
            }
            // Check for Ordered List
            int dotIndex = trimmedLine.IndexOf(". ");
            bool isOrdered = dotIndex > 0 && trimmedLine.Length > dotIndex + 2 && int.TryParse(trimmedLine.Substring(0, dotIndex), out int _);
            if (isOrdered)
            {
                content = trimmedLine.Substring(dotIndex + 2);
                levelOrIndent = indentation; // Store original indentation
                _log($"DetermineBlockType: Matched Ordered List. Indent={levelOrIndent}, Content='{content}'. Returning OrderedList.");
                _log("DetermineBlockType END (OrderedList): Returning OrderedList.");
                return MarkdownBlockType.OrderedList;
            }

            // Check for Blockquote
            if (trimmedLine.StartsWith(">"))
            {
                // Content is after '>' and an optional space
                if (trimmedLine.Length > 1 && trimmedLine[1] == ' ')
                {
                    content = trimmedLine.Substring(2);
                }
                else if (trimmedLine.Length > 1)
                {
                    content = trimmedLine.Substring(1);
                }
                else
                {
                    content = string.Empty; // Just a '>' line
                }
                levelOrIndent = indentation; // Store original indentation
                _log($"DetermineBlockType: Matched Blockquote. Indent={levelOrIndent}, Content='{content}'. Returning Blockquote.");
                _log("DetermineBlockType END (Blockquote): Returning Blockquote.");
                return MarkdownBlockType.Blockquote;
            }

            // 4. Indented Code Check (4 spaces or tab)
            if (indentation >= 4 || (indentation > 0 && line.StartsWith("\t")))
            {
                content = line; // Keep original indentation for code
                levelOrIndent = indentation;
                _log($"DetermineBlockType: Matched Indented Code Block. Indent={levelOrIndent}. Returning CodeBlock.");
                _log("DetermineBlockType END (CodeBlock): Returning CodeBlock.");
                return MarkdownBlockType.CodeBlock;
            }

            // Check for GFM Table Separator (must follow a Paragraph or be the first content line)
            // Allow table detection even if _currentBlockType isn't Paragraph (e.g., after header)
            bool isPotentialSeparator = line.Contains('|') && line.Contains('-') && 
                                      line.Trim().All(c => c == '-' || c == ':' || c == '|' || char.IsWhiteSpace(c));
            if (isPotentialSeparator && (_currentBlockType == MarkdownBlockType.Paragraph || _currentBlockType == MarkdownBlockType.None))
            {
                content = line; // Keep the separator line content for now
                _log($"DetermineBlockType: Matched potential Table Separator (following {_currentBlockType}). Returning Table.");
                _log("DetermineBlockType END (Table Separator): Returning Table.");
                return MarkdownBlockType.Table;
            }

            // Check if we are continuing a table (after the separator has been processed)
            if (_currentBlockType == MarkdownBlockType.Table && line.Contains('|'))
            {
                // Treat this line as part of the ongoing table
                content = line;
                _log("DetermineBlockType: Continuing Table row. Returning Table.");
                _log("DetermineBlockType END (Table Row): Returning Table.");
                return MarkdownBlockType.Table;
            }

            // 5. Paragraph (Default)
            content = line.Trim(); // Paragraphs are trimmed
            _log($"DetermineBlockType: Defaulting to Paragraph. Content='{content}'. Returning Paragraph.");
            _log("DetermineBlockType END (Paragraph): Returning Paragraph.");
            return MarkdownBlockType.Paragraph;
        }

        private void ProcessLine(string line)
        {
            _log($"ProcessLine START: Input='{line}', CurrentBlock={_currentBlockType}, NeedsSeparation={_needsSeparationBeforeNextBlock}");
            // --- PRE-PROCESSING: Add separator if flagged --- 
            if (_needsSeparationBeforeNextBlock)
            {
                _log("ProcessLine: Applying separation newline before processing.");
                _outputWriter.Write(Environment.NewLine);
                _needsSeparationBeforeNextBlock = false;
            }

            // I. Handling Blank Lines
            if (string.IsNullOrWhiteSpace(line))
            {
                _log("ProcessLine: Detected blank line.");
                if (_currentBlockType != MarkdownBlockType.None)
                {
                    var blockToFinalize = _currentBlockType;
                    _log($"ProcessLine: Blank line ending block: {blockToFinalize}. Calling FinalizeBlock.");
                    FinalizeBlock(blockToFinalize); // Finalize content (might set flag)
                    _currentBlockType = MarkdownBlockType.None;
                    _log($"ProcessLine: Blank line processing complete. CurrentBlock set to None.");
                }
                else
                {
                    _log("ProcessLine: Blank line, but already in None state. Doing nothing.");
                }
                // Do nothing else - flag will handle separation before next content
                _log("ProcessLine END (Blank Line)");
                return;
            }

            // II. Determining New Block Type
            _log($"ProcessLine: Determining block type for line: '{line}'");
            MarkdownBlockType newBlockType = DetermineBlockType(line, out string content, out int levelOrIndent);
            _log($"ProcessLine: Determined block type: {newBlockType}, Content: '{content}', Level/Indent: {levelOrIndent}");
            bool isTransitioning = newBlockType != _currentBlockType;
            var blockToEnd = _currentBlockType; // Cache the block type we might be ending
            _log($"ProcessLine: Is Transitioning? {isTransitioning}. Block to potentially end: {blockToEnd}");

            // III. Handling Transitions Between Blocks (Finalize Old Block)
            if (isTransitioning && blockToEnd != MarkdownBlockType.None)
            {
                _log($"ProcessLine: Transition detected from {blockToEnd} to {newBlockType}.");
                // Special case: Don't finalize paragraph yet if transitioning to Table (header needed)
                if (!(blockToEnd == MarkdownBlockType.Paragraph && newBlockType == MarkdownBlockType.Table))
                {
                     _log($"ProcessLine: Finalizing block {blockToEnd} due to transition.");
                     FinalizeBlock(blockToEnd); // Finalizes content, sets _lastFinalizedBlock* vars
                }
                else
                {
                    _log($"ProcessLine: Transitioning from Paragraph to Table. Skipping FinalizeBlock for Paragraph (header needed).");
                    // Pretend paragraph produced output for separation logic below
                     _lastFinalizedBlockProducedOutput = true;
                     _lastFinalizedBlockType = blockToEnd;
                }
            }

            // IV. Add Separating Newline (REMOVED - Handled by flag before line processing)

            // V. Updating State and Applying Style
            _log($"ProcessLine: Updating CurrentBlockType to {newBlockType}.");
            _currentBlockType = newBlockType;

            // Apply style AFTER state update and potential separation newline
            if (newBlockType != MarkdownBlockType.LinkDefinition)
            {
                 _log($"ProcessLine: Applying style for block {newBlockType}.");
                 ApplyStyleForBlock(newBlockType);
            }
            // Reset ordered list counter on transition to list
            if (newBlockType == MarkdownBlockType.OrderedList && isTransitioning)
            {
                 // Check blockToEnd specifically, not _lastFinalizedBlockType which might be from further back
                 if (blockToEnd != MarkdownBlockType.OrderedList)
                 {
                     _log("ProcessLine: Resetting ordered list counter due to transition.");
                     _orderedListCounter = 0;
                 }
            }

            // VI. Processing the Current Line Content
            _log($"ProcessLine: Entering switch for CurrentBlockType: {_currentBlockType}");
            switch (_currentBlockType) 
            {
                case MarkdownBlockType.Heading1:
                case MarkdownBlockType.Heading2:
                case MarkdownBlockType.Heading3:
                    _log($"ProcessLine: Processing Heading (Type: {_currentBlockType}). Content: '{content}', Level: {levelOrIndent}.");
                    WriteHeading(levelOrIndent, content);
                    _lastFinalizedBlockType = _currentBlockType; // Record heading type
                    _lastFinalizedBlockProducedOutput = true; // Heading finished, produced output
                    _currentBlockType = MarkdownBlockType.None;
                    break;

                case MarkdownBlockType.UnorderedList:
                case MarkdownBlockType.OrderedList:
                    _log($"ProcessLine: Processing List Item (Type: {_currentBlockType}). Content: '{content}', Indent: {levelOrIndent}.");
                    WriteListItem(newBlockType, content, levelOrIndent);
                    _lastFinalizedBlockProducedOutput = false; // List item doesn't guarantee block end
                    break;

                case MarkdownBlockType.CodeBlock:
                    string contentLine;
                    bool trimIndentation = _lastFinalizedBlockType != MarkdownBlockType.UnorderedList && 
                                           _lastFinalizedBlockType != MarkdownBlockType.OrderedList;
                    if (trimIndentation && line.StartsWith("    ")) { contentLine = line.Substring(4); }
                    else if (trimIndentation && line.StartsWith("\t")) { contentLine = line.Substring(1); }
                    else { contentLine = line; }
                    _outputWriter.WriteLine(contentLine);
                    _lastFinalizedBlockProducedOutput = false; // Code block continues
                    _log($"ProcessLine: Processing Code Block line. Output: '{contentLine}'");
                    break;

                case MarkdownBlockType.Paragraph:
                    _log($"ProcessLine: Processing Paragraph line. Content: '{content}'");
                    StartOrContinueParagraph(content);
                     _lastFinalizedBlockProducedOutput = false; // Paragraph continues
                    break;

                case MarkdownBlockType.Blockquote:
                    _log($"ProcessLine: Processing Blockquote line. Content: '{content}', Indent: {levelOrIndent}.");
                    WriteBlockquoteLine(content, levelOrIndent);
                     _lastFinalizedBlockProducedOutput = false; // Blockquote continues
                    break;

                case MarkdownBlockType.LinkDefinition: // No output
                     _lastFinalizedBlockProducedOutput = false;
                     _log($"ProcessLine: Processing Link Definition (no output expected).");
                     _needsSeparationBeforeNextBlock = false; // Consume any pending separation newline
                     _log("ProcessLine: Cleared _needsSeparationBeforeNextBlock for LinkDefinition.");
                    break;

                case MarkdownBlockType.Table:
                    _log($"ProcessLine: Processing Table line. Content: '{line}'"); // Log original line for table context
                    // Check if this is the separator line (first time entering Table state for this table)
                    if (_tableHeaderCells == null) 
                    { 
                        _log($"ProcessLine: Table separator line detected. Parsing header and separator.");
                        string headerLine = _paragraphBuffer.ToString().Trim();
                        string separatorLine = line.Trim(); // 'content' might be different, use original 'line'
                        _paragraphBuffer.Clear(); // Clear buffer now header is captured
                        ParseAndStoreTableHeaderAndSeparator(headerLine, separatorLine);
                        _tableRows = new List<List<string>>(); // *** Initialize the table rows list HERE ***
                        // Don't write anything for the separator line itself
                    }
                    else
                    { 
                        _log($"ProcessLine: Table row line detected. Parsing row.");
                        ParseAndStoreTableRow(line); // Use original 'line'
                    }
                     _lastFinalizedBlockProducedOutput = false; // Table continues until finalized
                    break;

                case MarkdownBlockType.None: 
                     _lastFinalizedBlockProducedOutput = false;
                     _log($"ProcessLine: CurrentBlockType is None (should not happen after blank line check?).");
                     break;
            }
            // // Ensure a final newline is written after the last line of the paragraph
            // _outputWriter.WriteLine();
            _log($"ProcessLine END: CurrentBlock={_currentBlockType}, NeedsSeparation={_needsSeparationBeforeNextBlock}, LastFinalized={_lastFinalizedBlockType}, LastProducedOutput={_lastFinalizedBlockProducedOutput}");
        }

        private void StartOrContinueParagraph(string line)
        {
            _log($"StartOrContinueParagraph START: Line='{line}', CurrentBufferLength={_paragraphBuffer.Length}");
            // Always finalize previous block unless we are already in a paragraph
            // This seems redundant as ProcessLine handles transitions. Let's rely on ProcessLine.
            // if (_currentBlockType != MarkdownBlockType.Paragraph)
            // {
            //     // _lastFinalizedBlockType = _currentBlockType; // Set this? Maybe not needed here.
            //     FinalizeBlock(_currentBlockType); // End previous block if any
            //     _currentBlockType = MarkdownBlockType.Paragraph;
            //     _paragraphBuffer.Clear();
            //     _paragraphBuffer.Append(line); // Start new paragraph
            // }
            // else

            // Append to buffer, adding space if needed
            bool needsSpace = _paragraphBuffer.Length > 0 && 
                             !char.IsWhiteSpace(_paragraphBuffer[_paragraphBuffer.Length - 1]) &&
                             (line.Length == 0 || !char.IsWhiteSpace(line[0]));
            
            if (needsSpace)
            {
                 _log("StartOrContinueParagraph: Adding space before appending.");
                _paragraphBuffer.Append(' ');
            }
            _log($"StartOrContinueParagraph: Appending line. New buffer length will be {_paragraphBuffer.Length + line.Length}.");
            _paragraphBuffer.Append(line);
            
            // We don't write the paragraph until it's ended (by blank line, end of doc, or another block type)
            _log("StartOrContinueParagraph END");
        }

        private void WriteHeading(int level, string text)
        {
            string style;
            switch (level)
            {
                case 1: style = _options.Theme.Heading1Style; break; // Don't set _currentBlockType here
                case 2: style = _options.Theme.Heading2Style; break;
                case 3: style = _options.Theme.Heading3Style; break;
                default: style = string.Empty; break; // Should not happen based on check in ProcessLine
            }

            _log($"WriteHeading(L{level}): Applying style '{style}'. Text: \"{text}\"");
            ApplyStyle(style);
            WriteToOutput(text); // Write the text itself
            ResetCurrentStyle(); // Reset after text
            _log($"WriteHeading(L{level}): Writing two Environment.NewLines.");
            _outputWriter.Write(Environment.NewLine); // Headings add their own trailing newline.
            //_outputWriter.Write(" ");  // Prepare for spacing off. // Removed spacing attempt
            _outputWriter.Write(Environment.NewLine); // Headings are spaced off.
            _log($"WriteHeading(L{level}): Exiting.");
        }

        private static MarkdownBlockType GetHeadingBlockType(int level)
        {
            return level switch
            {
                1 => MarkdownBlockType.Heading1,
                2 => MarkdownBlockType.Heading2,
                3 => MarkdownBlockType.Heading3,
                _ => MarkdownBlockType.None // Should not happen if called correctly
            };
        }

        private void WriteListItem(MarkdownBlockType listType, string text, int indentation)
        {
            _log($"WriteListItem START: Type={listType}, Text='{text}', Indentation={indentation}");
            string bullet;
            string textAfterBullet = text; // Default to original text

            // Check for GFM Task List Item - Look for marker *after* potential indentation
            string trimmedText = text.TrimStart();
            int markerOffset = text.Length - trimmedText.Length; // How much whitespace was trimmed?
            _log($"WriteListItem: Checking for Task List. Trimmed='{trimmedText}', Offset={markerOffset}");

            if (trimmedText.StartsWith("[ ] "))
            {
                bullet = _options.Theme.TaskListUncheckedMarker;
                textAfterBullet = text.Substring(markerOffset + 4); 
                _log($"WriteListItem: Detected Unchecked Task. Bullet='{bullet}', TextAfter='{textAfterBullet}'");
            }
            else if (trimmedText.StartsWith("[x] ", StringComparison.OrdinalIgnoreCase))
            {
                bullet = _options.Theme.TaskListCheckedMarker;
                textAfterBullet = text.Substring(markerOffset + 4); 
                _log($"WriteListItem: Detected Checked Task. Bullet='{bullet}', TextAfter='{textAfterBullet}'");
            }
            // If not a task item, use standard list bullets
            else if (listType == MarkdownBlockType.OrderedList)
            {
                _orderedListCounter++; // Increment *before* using
                bullet = string.Format(_options.Theme.OrderedListPrefixFormat, _orderedListCounter);
                _log($"WriteListItem: Ordered List Item. Counter={_orderedListCounter}, Bullet='{bullet}'");
            }
            else // Unordered list
            {
                bullet = _options.Theme.UnorderedListPrefix;
                _log($"WriteListItem: Unordered List Item. Bullet='{bullet}'");
            }

            // Apply indentation 
            string indentString = new string(' ', indentation);
            _log($"WriteListItem: Writing Indentation ('{indentString.Length}' spaces).");
            _outputWriter.Write(indentString);

            // Apply bullet/number color
            string bulletStyle = Ansi.GetColorCode(_options.Theme.ListBulletColor, foreground: true);
            _log($"WriteListItem: Applying bullet style ('{bulletStyle}').");
            ApplyStyle(bulletStyle); 
            WriteToOutput(bullet); 
            ResetCurrentStyle(); 
            _outputWriter.Write(" "); // Add space after bullet/marker

            // Write the list item content
            _log($"WriteListItem: Writing text '{textAfterBullet}'.");
            _outputWriter.Write(textAfterBullet); 
            _log("WriteListItem: Writing Environment.NewLine.");
            _outputWriter.Write(Environment.NewLine); // Use Environment.NewLine

            _currentBlockType = listType; // Should this be set here or ProcessLine?
            _log($"WriteListItem END: CurrentBlockType remains {listType}");
        }

        // Simple word wrapping helper
        private void WriteWrappedText(string text)
        {
            int consoleWidth = _maxWidth;
            int currentPosition = 0;

            while (currentPosition < text.Length)
            {
                int lengthToTake = Math.Min(consoleWidth, text.Length - currentPosition);

                // If we are not at the end of the text, try to wrap at a space
                if (currentPosition + lengthToTake < text.Length)
                {
                    int lastSpace = text.LastIndexOf(' ', currentPosition + lengthToTake - 1, lengthToTake);
                    if (lastSpace > currentPosition) // Found a space to wrap at
                    {
                        lengthToTake = lastSpace - currentPosition;
                    }
                    // else: No space found, we have to break the word (or the chunk fits perfectly)
                }

                string lineToWrite = text.Substring(currentPosition, lengthToTake);
                _outputWriter.Write(lineToWrite); 

                currentPosition += lengthToTake;

                // Skip the space we wrapped at (if any) for the next line
                bool skippedSpace = false;
                if (currentPosition < text.Length && text[currentPosition] == ' ')
                {
                    currentPosition++;
                    skippedSpace = true;
                }

                // Add newline if there's more text to write
                if (currentPosition < text.Length)
                {
                    _outputWriter.WriteLine();
                    // If we did NOT skip a space (meaning the break was mid-word or perfect fit),
                    // and the next char isn't a space, add one for separation.
                    // (This seems overly complex, let's rethink - the issue might be joining in StartOrContinueParagraph)
                }
            }
            // Ensure a final newline is written after the last line of the paragraph
            _outputWriter.WriteLine();
        }

        private void WriteParagraph(string text)
        {
            _log($"WriteParagraph START: Text Length={text.Length}");
            if (!string.IsNullOrEmpty(text))
            {
                WriteFormattedParagraph(text); // Use the formatted writer
            }
            _log("WriteParagraph END (Newline removed)");
        }

        private void WriteFormattedParagraph(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                // _outputWriter.WriteLine(); // REMOVED: Empty paragraph shouldn't write anything itself
                return;
            }

            var outputBuffer = new StringBuilder();
            var styleStack = new Stack<string>(); 

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                bool processed = false;

                // Check for escaped marker
                if (c == '\\' && i + 1 < text.Length) 
                {
                    char nextChar = text[i + 1];
                    // Handle specific escapes for markdown syntax
                    if (nextChar == '*' || nextChar == '_' || nextChar == '~' || nextChar == '[' || nextChar == ']' || nextChar == '(' || nextChar == ')' || nextChar == '\\' || nextChar == '!')
                    {
                        outputBuffer.Append(nextChar);
                        i++; 
                        processed = true; 
                    }
                    else
                    {
                        // Not a special markdown escape, append both backslash and char
                        outputBuffer.Append(c);
                        // Let the next character be processed normally in the next iteration
                    }
                }
                // Check for Inline Image start: ![alt text](url "title")
                else if (c == '!' && i + 1 < text.Length && text[i + 1] == '[')
                {
                    int altTextStart = i + 2;
                    int altTextEnd = text.IndexOf(']', altTextStart);
                    if (altTextEnd > altTextStart && altTextEnd + 1 < text.Length && text[altTextEnd + 1] == '(')
                    {
                        int urlPartEnd = text.IndexOf(')', altTextEnd + 2);
                        if (urlPartEnd > altTextEnd + 1)
                        {
                            string altText = text.Substring(altTextStart, altTextEnd - altTextStart);
                            // URL and Title inside () are ignored for console image rendering

                            // Render the image alt text
                            outputBuffer.Append(_options.Theme.ImagePrefix);
                            outputBuffer.Append(_options.Theme.ImageAltTextStyle);
                            outputBuffer.Append(altText); // TODO: Recursively parse altText for emphasis?
                            outputBuffer.Append(Ansi.Reset); // Reset styles after alt text
                            outputBuffer.Append(_options.Theme.ImageSuffix);

                            i = urlPartEnd; // Move parser index past the entire image markdown
                            processed = true;
                        }
                    }
                    // If not a valid image structure, treat '!' literally (handled by default flow)
                }
                // Check for inline HTML tag start
                else if (c == '<')
                {
                    // Attempt to find the closing >
                    int tagEnd = text.IndexOf('>', i + 1);
                    if (tagEnd > i)
                    {
                        // Simple stripping: Skip everything from < to >
                        i = tagEnd; 
                        processed = true;
                    }
                    // If no closing >, treat '<' literally (handled by default flow)
                }
                // Check for Inline Link start: [text](url "title")
                else if (c == '[') // Check inline link first
                {
                    int linkTextEnd = text.IndexOf(']', i + 1);
                    if (linkTextEnd > i && linkTextEnd + 1 < text.Length && text[linkTextEnd + 1] == '(')
                    {
                        int urlPartEnd = text.IndexOf(')', linkTextEnd + 2);
                        if (urlPartEnd > linkTextEnd + 1)
                        {
                            string linkText = text.Substring(i + 1, linkTextEnd - i - 1);
                            string urlPart = text.Substring(linkTextEnd + 2, urlPartEnd - (linkTextEnd + 2));
                            
                            string url;
                            string title = null;

                            // Basic parsing for url and optional title
                            int firstSpace = urlPart.IndexOf(' ');
                            if (firstSpace != -1 && urlPart.Length > firstSpace + 2 && urlPart[firstSpace + 1] == '"' && urlPart.EndsWith('"'))
                            {
                                url = urlPart.Substring(0, firstSpace);
                                title = urlPart.Substring(firstSpace + 2, urlPart.Length - firstSpace - 3); // Extract title
                            }
                            else
                            {
                                url = urlPart; // No title found
                            }

                            // Render the link
                            outputBuffer.Append(_options.Theme.LinkTextStyle);
                            outputBuffer.Append(linkText); // TODO: Recursively parse linkText for emphasis?
                            outputBuffer.Append(Ansi.Reset); // Reset styles after link text
                            outputBuffer.Append(" (");
                            outputBuffer.Append(_options.Theme.LinkUrlStyle);
                            outputBuffer.Append(url);
                            outputBuffer.Append(Ansi.Reset); // Reset styles after URL
                            outputBuffer.Append(")");
                            // Title is ignored for console rendering for now

                            i = urlPartEnd; // Move parser index past the entire link
                            processed = true;
                        }
                    }
                    // If not a valid inline link structure, treat '[' literally (handled by default flow)
                    // Fallthrough to check reference-style link only if not processed as inline
                }
                // Check for Reference Link start: [text][label], [label][], [label] - ONLY if not processed as inline
                if (!processed && c == '[')
                {
                    int linkTextEnd = text.IndexOf(']', i + 1);
                    if (linkTextEnd > i) // Found closing ']' for link text/label
                    {
                        string firstPart = text.Substring(i + 1, linkTextEnd - i - 1);
                        string? label = null; // Initialize to null
                        string? linkText = null; // Initialize to null
                        int endMarkerIndex = -1; // Track index after the complete link syntax
                        bool possibleRefLinkParsed = false; // Flag if we found a potential ref link structure

                        int nextCharIndex = linkTextEnd + 1;
                        if (nextCharIndex < text.Length && text[nextCharIndex] == '[') // Case: [text][label]
                        {
                            int labelEnd = text.IndexOf(']', nextCharIndex + 1);
                            if (labelEnd > nextCharIndex)
                            {
                                label = text.Substring(nextCharIndex + 1, labelEnd - nextCharIndex - 1);
                                linkText = firstPart;
                                endMarkerIndex = labelEnd; // Consume up to label's closing ']'
                                possibleRefLinkParsed = true;
                            }
                            // else Malformed [text][... missing ] - treat as literal later
                        }
                        else // Not [text][label], check collapsed/shortcut
                        {
                             // Check for empty '[]'
                            if (nextCharIndex < text.Length && text[nextCharIndex] == '[' &&
                                nextCharIndex + 1 < text.Length && text[nextCharIndex + 1] == ']')
                            { // Case: [label][]
                                label = firstPart;
                                linkText = firstPart;
                                endMarkerIndex = nextCharIndex + 1; // Consume up to empty []
                                possibleRefLinkParsed = true;
                            }
                            else
                            { // Case: [label] (Assume it might be a shortcut link)
                                label = firstPart;
                                linkText = firstPart;
                                endMarkerIndex = linkTextEnd; // Consume only up to first closing ']'
                                possibleRefLinkParsed = true;
                            }
                        }

                        // If we successfully identified a potential reference structure AND the label lookup succeeds...
                        if (possibleRefLinkParsed && label != null && linkText != null && _linkDefinitions.TryGetValue(label, out var definition))
                        {
                            // Render the link using the definition
                            outputBuffer.Append(_options.Theme.LinkTextStyle);
                            outputBuffer.Append(linkText); // TODO: Recursive parse?
                            outputBuffer.Append(Ansi.Reset);
                            outputBuffer.Append(" (");
                            outputBuffer.Append(_options.Theme.LinkUrlStyle);
                            outputBuffer.Append(definition.Url);
                            outputBuffer.Append(Ansi.Reset);
                            outputBuffer.Append(")");
                            // definition.Title is ignored
                            processed = true;
                            i = endMarkerIndex; // Advance 'i' ONLY if lookup succeeded and link was rendered
                        }
                        // else: Potential ref link structure but lookup failed, OR structure was malformed.
                        //       Do nothing special here. 'processed' remains false.
                        //       The loop will continue, and the original '[' will be appended by the fallback logic.
                    }
                    // If no closing ']', treat '[' literally (default flow)
                }
                // Check for emphasis markers
                else if (c == '*' || c == '_' || c == '~')
                {
                    int markerCount = 1;
                    while (i + markerCount < text.Length && text[i + markerCount] == c)
                    {
                        markerCount++;
                    }

                    string marker = new string(c, markerCount);
                    string? styleCode = null;
                    string? styleOffCode = null;
                    string? styleType = null; 

                    if (marker == "***" || marker == "___") { styleCode = _options.Theme.BoldStyle + _options.Theme.ItalicStyle; styleOffCode = Ansi.ItalicOff + Ansi.BoldOff; styleType = "bolditalic"; }
                    else if (marker == "**" || marker == "__") { styleCode = _options.Theme.BoldStyle; styleOffCode = Ansi.BoldOff; styleType = "bold"; }
                    else if (marker == "*" || marker == "_") { styleCode = _options.Theme.ItalicStyle; styleOffCode = Ansi.ItalicOff; styleType = "italic"; }
                    else if (marker == "~~") { styleCode = _options.Theme.StrikethroughStyle; styleOffCode = Ansi.StrikethroughOff; styleType = "strike"; }

                    if (styleCode != null && styleOffCode != null) // Ensure we have both codes
                    {
                        if (styleStack.Count > 0 && styleStack.Peek() == styleType)
                        {
                            outputBuffer.Append(styleOffCode); 
                            styleStack.Pop();
                        }
                        else
                        {
                            outputBuffer.Append(styleCode); 
                            if (styleType != null) 
                            {
                                styleStack.Push(styleType);
                            }
                        }
                        i += markerCount - 1; 
                        processed = true;
                    }
                    else if (!processed) 
                    {
                        // If not a recognized style marker combo, append the char(s)
                        outputBuffer.Append(c);
                    }
                }

                if (!processed)
                {
                    outputBuffer.Append(c);
                }
            }

            // Explicitly close any remaining open styles in reverse order
            while(styleStack.Count > 0)
            {
                string styleTypeToClose = styleStack.Pop();
                if (styleTypeToClose == "bolditalic") outputBuffer.Append(Ansi.ItalicOff + Ansi.BoldOff);
                else if (styleTypeToClose == "bold") outputBuffer.Append(Ansi.BoldOff);
                else if (styleTypeToClose == "italic") outputBuffer.Append(Ansi.ItalicOff);
                else if (styleTypeToClose == "strike") outputBuffer.Append(Ansi.StrikethroughOff);
            }

            // Pass the styled (and hopefully correctly closed) string to WriteWrappedText
            WriteWrappedText(outputBuffer.ToString()); 
        }

        private void ResetAllStyles()
        {
            while (_activeStyles.Count > 0)
            {
                ResetCurrentStyle();
            }
        }

        private void ApplyStyleForBlock(MarkdownBlockType blockType)
        {
            switch (blockType)
            {
                case MarkdownBlockType.CodeBlock:
                    ApplyStyle(_options.Theme.CodeBlockStyle);
                    break;
                // Add cases for other block types if they need initial styling
                // case MarkdownBlockType.Heading1: ApplyStyle(...); break;
                default:
                    // No specific style to apply initially for Paragraph, Lists, etc.
                    break;
            }
        }

        /// <summary>
        /// Clears all buffers for this writer and causes any buffered data to be written to the underlying device.
        /// </summary>
        public override void Flush()
        {
            CheckDisposed();
            // Process any remaining line buffer content *before* flushing the underlying writer
            Complete();
            _outputWriter.Flush();
        }

        // New method to write a line within a blockquote
        private void WriteBlockquoteLine(string text, int indentation)
        {
            _log($"WriteBlockquoteLine START: Text='{text}', Indentation={indentation}");
            string prefix = new string(' ', indentation) + _options.Theme.BlockquotePrefix;
            string prefixStyle = Ansi.GetColorCode(_options.Theme.BlockquoteColor, foreground: true);
            
            _log($"WriteBlockquoteLine: Applying prefix style ('{prefixStyle}').");
            ApplyStyle(prefixStyle);
            WriteToOutput(prefix);
            ResetCurrentStyle();

            // Write the actual text content of the blockquote line
            _log($"WriteBlockquoteLine: Writing text '{text}'.");
            WriteToOutput(text);
            _log("WriteBlockquoteLine: Writing Environment.NewLine.");
            _outputWriter.Write(Environment.NewLine); // Blockquote lines end with a newline
            _log("WriteBlockquoteLine END");
        }

        // --- Table Parsing and Rendering --- 

        private List<string> SplitTableRow(string rowLine)
        {
            // Trim surrounding whitespace and potential outer pipes
            string trimmedLine = rowLine.Trim();
            if (trimmedLine.StartsWith('|')) trimmedLine = trimmedLine.Substring(1);
            // Trim trailing pipe only if it exists *after* potential leading pipe removal
            if (trimmedLine.EndsWith('|')) trimmedLine = trimmedLine.Substring(0, trimmedLine.Length - 1);

            // Split by pipe, trim each cell
            return trimmedLine.Split('|').Select(cell => cell.Trim()).ToList();
        }

        private void ParseAndStoreTableHeaderAndSeparator(string headerLine, string separatorLine)
        {
            _log($"ParseAndStoreTableHeaderAndSeparator START: Header='{headerLine}', Separator='{separatorLine}'");
            _tableHeaderCells = SplitTableRow(headerLine);
            List<string> rawSeparatorCells = SplitTableRow(separatorLine); // Parse raw separators first
            int columnCount = _tableHeaderCells.Count;

            // Initialize lists to the correct size
            _tableSeparatorStrings = new List<string>(columnCount);
            _tableAlignments = new List<ColumnAlignment>(columnCount);

            _log($"ParseAndStoreTableHeaderAndSeparator: Parsed {columnCount} header cells.");

            for (int i = 0; i < columnCount; i++)
            {
                // Get raw separator, defaulting to '---' if separator line is shorter than header
                string sep = (i < rawSeparatorCells.Count) ? rawSeparatorCells[i] : "---"; 
                _tableSeparatorStrings.Add(sep); // Add the potentially defaulted separator string

                // Determine alignment based on the raw separator
                string trimmedSep = sep.Trim();
                bool left = trimmedSep.StartsWith(':');
                bool right = trimmedSep.EndsWith(':');
                ColumnAlignment alignment;

                if (left && right)
                {
                    alignment = ColumnAlignment.Center;
                }
                else if (right)
                {
                    alignment = ColumnAlignment.Right;
                }
                else // Default or left-aligned (:)
                {
                    alignment = ColumnAlignment.Left;
                }
                _tableAlignments.Add(alignment); // Add the determined alignment
                _log($"ParseAndStoreTableHeaderAndSeparator: Col {i}: Separator='{sep}', Trimmed='{trimmedSep}', Alignment={alignment}");
            }
            _log($"ParseAndStoreTableHeaderAndSeparator END: Stored {columnCount} alignments and separator strings.");
        }

        private void ParseAndStoreTableRow(string rowLine)
        {
            _log($"ParseAndStoreTableRow START: RowLine='{rowLine}'");
            if (_tableRows == null)
            {
                _log("ParseAndStoreTableRow ERROR: Table rows list is null.");
                return; // Should not happen if called correctly
            }
            List<string> cells = SplitTableRow(rowLine);
             _log($"ParseAndStoreTableRow: Parsed {cells.Count} cells.");
            _tableRows.Add(cells);
            _log($"ParseAndStoreTableRow END: Added row. Total rows: {_tableRows.Count}");
        }

        private string PadAndAlign(string text, int totalWidth, ColumnAlignment alignment)
        {
            int textLength = text.Length;
            int paddingNeeded = totalWidth - textLength;
            if (paddingNeeded <= 0) return text.Substring(0, totalWidth); // Truncate if too long

            switch (alignment)
            {
                case ColumnAlignment.Right:
                    return new string(' ', paddingNeeded) + text;
                case ColumnAlignment.Center:
                    int leftPadding = paddingNeeded / 2;
                    int rightPadding = paddingNeeded - leftPadding;
                    return new string(' ', leftPadding) + text + new string(' ', rightPadding);
                case ColumnAlignment.Left:
                default:
                    return text + new string(' ', paddingNeeded);
            }
        }

        private void WriteTable()
        {
            _log("WriteTable START");
            if (_tableHeaderCells == null || _tableAlignments == null || _tableSeparatorStrings == null || _tableRows == null)
            {
                _log("WriteTable ERROR: Table state is incomplete. Cannot render.");
                return; // Not a valid table state
            }

            int columnCount = _tableHeaderCells.Count;
            _log($"WriteTable: Column Count={columnCount}");

            // Calculate column widths based on header, separator, and content
            var maxWidths = new int[columnCount];
            for (int i = 0; i < columnCount; i++)
            {
                int headerWidth = _tableHeaderCells[i].Length;
                int separatorWidth = _tableSeparatorStrings[i].Trim().Length; // Use the trimmed length for alignment marker check
                int maxContentWidth = 0;
                if (_tableRows.Count > 0)
                {
                    maxContentWidth = _tableRows.Max(row => (i < row.Count) ? row[i].Trim().Length : 0);
                }
                
                // GFM requires at least 3 dashes for separator, influencing minimum width
                maxWidths[i] = Math.Max(Math.Max(headerWidth, maxContentWidth), 3); 
                _log($"WriteTable: Col {i}: HeaderWidth={headerWidth}, SeparatorWidth={separatorWidth}, MaxContentWidth={maxContentWidth}, FinalWidth={maxWidths[i]}");
            }

            // Build the output using a StringBuilder
            var sb = new StringBuilder();
            string cellSeparator = " | ";
            string linePrefix = "| ";
            string lineSuffix = " |";

            // --- Render Header ---
            sb.Append(linePrefix);
            for (int i = 0; i < columnCount; i++)
            {
                string headerText = _tableHeaderCells[i];
                // Use the actual column alignment for padding the header
                var alignment = (i < _tableAlignments.Count) ? _tableAlignments[i] : ColumnAlignment.Left; 
                string paddedHeader = PadAndAlign(headerText, maxWidths[i], alignment); 
                sb.Append(paddedHeader);
                if (i < columnCount - 1) sb.Append(cellSeparator);
                 _log($"WriteTable: Header Col {i}: Text='{headerText}', Padded='{paddedHeader}'");
            }
            sb.Append(lineSuffix);
            sb.Append(Environment.NewLine); // Use Environment.NewLine
            _log($"WriteTable: Rendered Header Line: {sb.ToString().TrimEnd()}");

            // --- DEFENSIVE CHECK --- 
            if (_tableAlignments.Count != columnCount) 
            {
                _log($"WriteTable FATAL: Alignment count ({_tableAlignments.Count}) does not match column count ({columnCount}). Aborting table render.");
                // Write an error message instead of crashing?
                WriteToOutput($"[Table Render Error: Column/Alignment mismatch ({columnCount}/{_tableAlignments.Count})]");
                return;
            }
            // --- Render Separator ---
             var sbSep = new StringBuilder();
             sbSep.Append(linePrefix);
            for (int i = 0; i < columnCount; i++)
            {
                var alignment = _tableAlignments[i];
                int targetWidth = maxWidths[i];
                string separatorString;

                // Base separator is dashes matching the width
                var dashes = new string('-', targetWidth);

                // Apply alignment markers
                if (alignment == ColumnAlignment.Center)
                {
                     // Need to place colons carefully if width allows
                     if (targetWidth >= 2)
                     {
                         var chars = dashes.ToCharArray();
                         chars[0] = ':';
                         chars[targetWidth - 1] = ':';
                         separatorString = new string(chars);
                     }
                     else // Width 1? Just use colon?
                     { 
                         separatorString = ":"; // Or maybe "-"? GFM needs at least 3 dashes total per cell...
                         _log($"WriteTable WARN: Center align with width {targetWidth} might be ambiguous.");
                     }
                }
                else if (alignment == ColumnAlignment.Right)
                {
                     if (targetWidth >= 1)
                     {
                         var chars = dashes.ToCharArray();
                         chars[targetWidth - 1] = ':';
                         separatorString = new string(chars);
                     }
                     else
                     {
                         separatorString = "-"; // Fallback
                         _log($"WriteTable WARN: Right align with width {targetWidth} might be ambiguous.");
                     }
                    
                }
                else // Left alignment (or default)
                {
                    if (targetWidth >= 1 && _tableSeparatorStrings[i].Trim().StartsWith(':'))
                    {
                         var chars = dashes.ToCharArray();
                         chars[0] = ':';
                         separatorString = new string(chars);
                    }
                    else
                    {
                        separatorString = dashes; // Just dashes
                    }
                   
                }

                 sbSep.Append(separatorString);
                if (i < columnCount - 1) sbSep.Append(cellSeparator);
                 _log($"WriteTable: Separator Col {i}: Alignment={alignment}, TargetWidth={targetWidth}, SepString='{separatorString}'");
            }
            sbSep.Append(lineSuffix);
            sbSep.Append(Environment.NewLine); // Use Environment.NewLine
            _log($"WriteTable: Rendered Separator Line: {sbSep.ToString().TrimEnd()}");
            sb.Append(sbSep);

            // --- Render Rows ---
            foreach (var row in _tableRows)
            {
                sb.Append(linePrefix);
                for (int i = 0; i < columnCount; i++)
                {
                    string cellText = (i < row.Count) ? row[i].Trim() : string.Empty;
                    // Safely get alignment, defaulting to Left if index is out of bounds
                    var alignment = (i < _tableAlignments.Count) ? _tableAlignments[i] : ColumnAlignment.Left;
                    string paddedCell = PadAndAlign(cellText, maxWidths[i], alignment);
                    sb.Append(paddedCell);
                    if (i < columnCount - 1) sb.Append(cellSeparator);
                    _log($"WriteTable: Row Col {i}: Text='{cellText}', Padded='{paddedCell}'");
                }
                sb.Append(lineSuffix);
                sb.Append(Environment.NewLine); // Use Environment.NewLine
            }
            _log($"WriteTable: Rendered { _tableRows.Count } Data Rows.");

            // Write the complete table to the output
            WriteToOutput(sb.ToString());
             _log("WriteTable END");
        }
    }
}

