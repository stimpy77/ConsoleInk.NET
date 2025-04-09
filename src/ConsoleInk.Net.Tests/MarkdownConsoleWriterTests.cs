using Xunit;
using System.IO;
using System;
using ConsoleInk;
using System.Collections.Generic;

namespace ConsoleInk.Tests
{
    public class MarkdownConsoleWriterTests
    {
        // Helper method to simplify testing
        private void AssertRender(string inputMarkdown, string expectedOutput, MarkdownRenderOptions? options = null, bool trimLeadingWhitespace = true)
        {
            options ??= new MarkdownRenderOptions { EnableColors = false }; // Default to colors off if not provided
            var output = new StringWriter();
            var logMessages = new List<string>();
            Action<string> logger = (msg) => logMessages.Add(msg);

            // Use the provided options and add the logger
            using (var writer = new MarkdownConsoleWriter(output, options, logger))
            {
                writer.Write(inputMarkdown);
            }
            var actualOutput = output.ToString();

            // Normalize expected output line endings to LF for consistent comparison
            var normalizedExpectedOutput = expectedOutput.Replace(Environment.NewLine, "\n"); // Normalize expected string

            // Trim leading whitespace from each line if requested (useful for verbatim string literals)
            if (trimLeadingWhitespace)
            {
                var lines = normalizedExpectedOutput.Split('\n');
                var trimmedLines = lines.Select(line => line.TrimStart());
                normalizedExpectedOutput = string.Join("\n", trimmedLines);
            }

            // Normalize actual output to LF
            var normalizedActualOutput = actualOutput.Replace(Environment.NewLine, "\n"); // Normalize actual string

            // Assert using normalized strings
            // Add log output to assertion message for easier debugging
            try
            {
                Assert.Equal(normalizedExpectedOutput, normalizedActualOutput);
            }
            catch (Xunit.Sdk.EqualException ex)
            {
                var logOutput = string.Join(Environment.NewLine, logMessages);
                // Manually format the expected/actual strings as the exception doesn't expose them directly
                throw new Xunit.Sdk.XunitException($"Assert.Equal() Failure:{Environment.NewLine}--- Expected ---{Environment.NewLine}{normalizedExpectedOutput}{Environment.NewLine}--- Actual ---{Environment.NewLine}{normalizedActualOutput}{Environment.NewLine}--- LOG ---{Environment.NewLine}{logOutput}{Environment.NewLine}(Original xUnit message: {ex.Message})");
            }
        }

        [Fact]
        public void Render_SingleParagraph_WrapsCorrectly()
        {
            // Arrange
            var inputMarkdown = "This is a simple paragraph that should wrap correctly based on the console width.";
            var expectedOutput = 
                "This is a simple" + Environment.NewLine +
                "paragraph that" + Environment.NewLine +
                "should wrap" + Environment.NewLine +
                "correctly based on" + Environment.NewLine +
                "the console width.";

            var options = new MarkdownRenderOptions { ConsoleWidth = 20, EnableColors = false };
            var output = new StringWriter();

            // Act
            using (var writer = new MarkdownConsoleWriter(output, options))
            {
                writer.Write(inputMarkdown);
            }

            var actualOutput = output.ToString().TrimEnd(); // Trim trailing newline added by EndParagraph

            // Assert
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Fact]
        public void Render_TwoParagraphs_SeparatedByBlankLine()
        {
            // Arrange
            var inputMarkdown = "First paragraph.\n\nSecond paragraph, slightly longer.";
            var expectedOutput = 
                "First paragraph." + Environment.NewLine + // End of first paragraph
                Environment.NewLine + // Blank line separator
                "Second paragraph," + Environment.NewLine + // Start of second paragraph (wrapped)
                "slightly longer." + Environment.NewLine; // Paragraphs end with newline now

            var options = new MarkdownRenderOptions { ConsoleWidth = 20, EnableColors = false };
            // Use AssertRender which handles StringWriter creation
            AssertRender(inputMarkdown, expectedOutput, options, trimLeadingWhitespace: false);
        }

        [Fact]
        public void Render_IndentedCodeBlock_FourSpaces()
        {
            // Arrange
            var inputMarkdown = 
                "    var x = 1;\n" +
                "    var y = 2;";
            var expectedOutput = 
                "var x = 1;" + Environment.NewLine +
                "var y = 2;" + Environment.NewLine; // Code blocks also end with newline
           
            var options = new MarkdownRenderOptions { EnableColors = false }; // Width doesn't apply to code blocks
            AssertRender(inputMarkdown, expectedOutput, options, trimLeadingWhitespace: false);
        }

        [Fact]
        public void Render_IndentedCodeBlock_Tab()
        {
            // Arrange
            var inputMarkdown = 
                "\tvar x = 1;\n" +
                "\tvar y = 2;";
            var expectedOutput = 
                "var x = 1;" + Environment.NewLine +
                "var y = 2;" + Environment.NewLine; // Code blocks also end with newline
           
            var options = new MarkdownRenderOptions { EnableColors = false }; 
            AssertRender(inputMarkdown, expectedOutput, options, trimLeadingWhitespace: false);
        }

        [Fact]
        public void Render_ParagraphThenCodeBlock()
        {
            // Arrange
            var inputMarkdown = 
                "This is a paragraph.\n" +
                "\n" +
                "    Code line 1\n" +
                "    Code line 2";
            var expectedOutput = 
                "This is a paragraph." + Environment.NewLine +
                Environment.NewLine + // Blank line separation
                "Code line 1" + Environment.NewLine +
                "Code line 2" + Environment.NewLine; // Code blocks end with newline
           
            var options = new MarkdownRenderOptions { ConsoleWidth = 80, EnableColors = false }; 
            AssertRender(inputMarkdown, expectedOutput, options, trimLeadingWhitespace: false);
        }

        [Fact]
        public void Render_CodeBlockThenParagraph()
        {
            // Arrange
            var inputMarkdown = 
                "    Code line 1\n" +
                "    Code line 2\n" +
                "\n" +
                "This is a paragraph."; // Needs blank line to separate
            var expectedOutput = 
                "Code line 1" + Environment.NewLine +
                "Code line 2" + Environment.NewLine + // Code block ends with newline
                Environment.NewLine + // Blank line separation
                "This is a paragraph." + Environment.NewLine; // Paragraph ends with newline
           
            var options = new MarkdownRenderOptions { ConsoleWidth = 80, EnableColors = false }; 
            AssertRender(inputMarkdown, expectedOutput, options, trimLeadingWhitespace: false);
        }

        [Theory]
        [InlineData("# Heading 1", Ansi.Bold, "Heading 1")] // Default H1 Style is Bold
        [InlineData("## Heading 2", Ansi.Underline, "Heading 2")] // Default H2 Style is Underline
        [InlineData("### Heading 3", "", "Heading 3")] // Default H3 Style is empty (no style)
        public void Render_Heading_AppliesCorrectStyle(string markdown, string expectedStyleCode, string expectedText)
        {
            // Arrange
            var options = new MarkdownRenderOptions { EnableColors = true }; // Enable colors
            var output = new StringWriter();
            var expectedOutput = 
                (string.IsNullOrEmpty(expectedStyleCode) ? "" : expectedStyleCode) + 
                expectedText + 
                (string.IsNullOrEmpty(expectedStyleCode) ? "" : Ansi.Reset) + 
                Environment.NewLine + // First newline from heading
                Environment.NewLine;  // Second newline from heading spacing

            // Use AssertRender helper
            AssertRender(markdown, expectedOutput, options, trimLeadingWhitespace: false);
        }

        [Fact]
        public void Render_ParagraphThenHeading()
        {
            // Arrange
            var inputMarkdown = 
                "Just a paragraph.\n" +
                "\n" +
                "# A Heading";
            var options = new MarkdownRenderOptions { EnableColors = true }; 
            var output = new StringWriter();
            var expectedOutput = 
                "Just a paragraph." + Environment.NewLine + // Paragraph ends
                Environment.NewLine + // Separator line
                Ansi.Bold + "A Heading" + Ansi.Reset + Environment.NewLine + Environment.NewLine; // Heading with style and TWO newlines

            // Use AssertRender helper
            AssertRender(inputMarkdown, expectedOutput, options, trimLeadingWhitespace: false);
        }

        [Fact]
        public void Render_HeadingThenParagraph()
        {
            // Arrange
            var inputMarkdown = 
                "## Heading Two\n" +
                "\n" +
                "Followed by a paragraph.";
            var options = new MarkdownRenderOptions { EnableColors = true }; 
            var output = new StringWriter();
            var expectedOutput = 
                Ansi.Underline + "Heading Two" + Ansi.Reset + Environment.NewLine + Environment.NewLine + // Heading ends with TWO newlines
                // Separator newline is now implicitly handled by heading's double newline
                "Followed by a paragraph." + Environment.NewLine; // Paragraph starts and ends with newline

            // Use AssertRender helper
            AssertRender(inputMarkdown, expectedOutput, options, trimLeadingWhitespace: false);
        }

        // === New List Tests ===

        [Fact]
        public void Render_UnorderedList_Simple()
        {
            // Arrange
            var inputMarkdown =
                "* Item 1\n" +
                "- Item 2\n" +
                "+ Item 3";
            var options = new MarkdownRenderOptions { EnableColors = true };
            var output = new StringWriter();
            var nl = Environment.NewLine;
            var bulletColor = Ansi.GetColorCode(options.Theme.ListBulletColor, true);
            var reset = Ansi.Reset;
            var prefix = options.Theme.UnorderedListPrefix;
            var expectedOutput = 
                bulletColor + prefix + reset + " Item 1" + nl + // Add space after prefix
                bulletColor + prefix + reset + " Item 2" + nl + // Add space after prefix
                bulletColor + prefix + reset + " Item 3" + nl; // Add space after prefix

            // Use AssertRender helper
            AssertRender(inputMarkdown, expectedOutput, options, trimLeadingWhitespace: false);
        }

        [Fact]
        public void Render_OrderedList_Simple()
        {
            // Arrange
            var inputMarkdown =
                "1. First\n" +
                "2. Second\n" +
                "3. Third";
            var options = new MarkdownRenderOptions { EnableColors = true };
            var nl = Environment.NewLine;
            var bulletColor = Ansi.GetColorCode(options.Theme.ListBulletColor, true);
            var reset = Ansi.Reset;
            Func<int, string> prefix = i => string.Format(options.Theme.OrderedListPrefixFormat, i);
            var expectedOutput =
                bulletColor + prefix(1) + reset + " First" + nl + // Corrected: Space after prefix
                bulletColor + prefix(2) + reset + " Second" + nl + // Corrected: Space after prefix
                bulletColor + prefix(3) + reset + " Third" + nl; // Corrected: Space after prefix

            // Use AssertRender helper
            AssertRender(inputMarkdown, expectedOutput, options, trimLeadingWhitespace: false);
        }

        [Fact]
        public void Render_List_HandlesIndentation()
        {
             // Arrange
            var inputMarkdown =
                "  * Indented Item"; // 2 spaces indentation
            var options = new MarkdownRenderOptions { EnableColors = true };
            var output = new StringWriter();
            var nl = Environment.NewLine;
            var bulletColor = Ansi.GetColorCode(options.Theme.ListBulletColor, true);
            var reset = Ansi.Reset;
            var prefix = options.Theme.UnorderedListPrefix;
            var expectedOutput = 
                "  " + bulletColor + prefix + reset + " Indented Item" + nl; // Add space after prefix

            // Use AssertRender helper
            AssertRender(inputMarkdown, expectedOutput, options, trimLeadingWhitespace: false);
        }

        [Fact]
        public void Render_ParagraphThenListThenParagraph()
        {
            // Arrange
            var inputMarkdown =
                "Before list.\n" +
                "\n" + // Separator
                "* Item A\n" +
                "* Item B\n" +
                "\n" + // Separator
                "After list.";
            var options = new MarkdownRenderOptions { EnableColors = true };
            var output = new StringWriter();
            var nl = Environment.NewLine;
            var bulletColor = Ansi.GetColorCode(options.Theme.ListBulletColor, true);
            var reset = Ansi.Reset;
            var prefix = options.Theme.UnorderedListPrefix;
            var expectedOutput = 
                "Before list." + nl +
                nl + // Separator
                bulletColor + prefix + reset + " Item A" + nl + // Add space after prefix
                bulletColor + prefix + reset + " Item B" + nl + // Add space after prefix
                nl + // Separator
                "After list." + nl;

            // Use AssertRender helper
            AssertRender(inputMarkdown, expectedOutput, options, trimLeadingWhitespace: false);
        }

        [Fact/*(Skip = "Needs review: Code block indentation and style reset after list item.")*/]
        public void Render_ListThenCodeBlock()
        {
            // Arrange
            var inputMarkdown =
                "1. One\n" +
                "2. Two\n" +
                "\n" +
                "    Code Here"; // This is an indented code block
            var options = new MarkdownRenderOptions { EnableColors = true };
            var nl = Environment.NewLine;
            var bulletColor = Ansi.GetColorCode(options.Theme.ListBulletColor, true);
            var codeStyle = options.Theme.CodeBlockStyle;
            var reset = Ansi.Reset;
            Func<int, string> prefix = i => string.Format(options.Theme.OrderedListPrefixFormat, i);
            var expectedOutput = 
                bulletColor + prefix(1) + reset + " One" + nl + 
                bulletColor + prefix(2) + reset + " Two" + nl + 
                nl + // Separator blank line
                codeStyle + "    Code Here" + nl +
                reset;
                
            // Use AssertRender helper
            AssertRender(inputMarkdown, expectedOutput, options, trimLeadingWhitespace: false);
        }

        [Theory]
        [InlineData(@"This is \*not italic\*.", "This is *not italic*.")]
        [InlineData(@"This is \_not italic\_.", "This is _not italic_.")]
        [InlineData(@"This is \~\~not struck\~\~.", "This is ~~not struck~~.")]
        [InlineData(@"This is \*\*not bold\*\*.", "This is **not bold**.")]
        public void Render_EscapedEmphasis(string markdown, string expectedAnsi)
        {
            // Arrange
            var options = new MarkdownRenderOptions { EnableColors = true };
            var output = new StringWriter();
            // For escaped text, no Reset should be added by the formatter itself, just the final newline from Write
            var expectedOutput = expectedAnsi + Environment.NewLine;

            // Act & Assert using helper
            AssertRender(markdown, expectedOutput, options, trimLeadingWhitespace: false);
        }

        // --- Emphasis Tests ---
        [Theory]
        [InlineData("This is **bold** text.", $"This is {Ansi.Bold}bold{Ansi.BoldOff} text.")]
        [InlineData("This is __bold__ text.", $"This is {Ansi.Bold}bold{Ansi.BoldOff} text.")]
        [InlineData("**Bold** at start.", $"{Ansi.Bold}Bold{Ansi.BoldOff} at start.")]
        [InlineData("End with **bold**.", $"End with {Ansi.Bold}bold{Ansi.BoldOff}.")]
        public void Render_BoldEmphasis(string markdown, string expectedAnsi)
        {
            // Arrange
            var options = new MarkdownRenderOptions { EnableColors = true };
            using var writer = new StringWriter();
            string expectedOutput = expectedAnsi + Environment.NewLine;

            // Act & Assert using helper
            AssertRender(markdown, expectedOutput, options, trimLeadingWhitespace: false);
        }

        [Theory]
        [InlineData("This is *italic* text.", $"This is {Ansi.Italic}italic{Ansi.ItalicOff} text.")]
        [InlineData("This is _italic_ text.", $"This is {Ansi.Italic}italic{Ansi.ItalicOff} text.")]
        [InlineData("*Italic* at start.", $"{Ansi.Italic}Italic{Ansi.ItalicOff} at start.")]
        [InlineData("End with *italic*.", $"End with {Ansi.Italic}italic{Ansi.ItalicOff}.")]
        public void Render_ItalicEmphasis(string markdown, string expectedAnsi)
        {
            // Arrange
            var options = new MarkdownRenderOptions { EnableColors = true };
            using var writer = new StringWriter();
            string expectedOutput = expectedAnsi + Environment.NewLine;

            // Act & Assert using helper
            AssertRender(markdown, expectedOutput, options, trimLeadingWhitespace: false);
        }

        [Theory]
        [InlineData("This is ~~struck~~ text.", $"This is {Ansi.Strikethrough}struck{Ansi.StrikethroughOff} text.")]
        [InlineData("~~Struck~~ at start.", $"{Ansi.Strikethrough}Struck{Ansi.StrikethroughOff} at start.")]
        [InlineData("End with ~~struck~~.", $"End with {Ansi.Strikethrough}struck{Ansi.StrikethroughOff}.")]
        public void Render_StrikethroughEmphasis(string markdown, string expectedAnsi)
        {
            // Arrange
            var options = new MarkdownRenderOptions { EnableColors = true };
            using var writer = new StringWriter();
            string expectedOutput = expectedAnsi + Environment.NewLine;

            // Act & Assert using helper
            AssertRender(markdown, expectedOutput, options, trimLeadingWhitespace: false);
        }

        [Theory]
        [InlineData("This is ***bold italic*** text.", $"This is {Ansi.Bold}{Ansi.Italic}bold italic{Ansi.ItalicOff}{Ansi.BoldOff} text.")]
        [InlineData("This is ___bold italic___ text.", $"This is {Ansi.Bold}{Ansi.Italic}bold italic{Ansi.ItalicOff}{Ansi.BoldOff} text.")]
        // Note: Simple parser might not handle complex nesting perfectly
        [InlineData("**Bold with *italic* inside**", $"{Ansi.Bold}Bold with {Ansi.Italic}italic{Ansi.ItalicOff} inside{Ansi.BoldOff}")]
        public void Render_CombinedAndNestedEmphasis(string markdown, string expectedAnsi)
        {
            // Arrange
            var options = new MarkdownRenderOptions { EnableColors = true };
            using var writer = new StringWriter();
            string expectedOutput = expectedAnsi + Environment.NewLine;

            // Act & Assert using helper
            AssertRender(markdown, expectedOutput, options, trimLeadingWhitespace: false);
        }

        // --- Blockquote Tests ---

        [Fact]
        public void Render_BasicBlockquote()
        {
            var input = "> This is a blockquote.";
            var options = new MarkdownRenderOptions { EnableColors = true }; // Define options
            var expected = $"{Ansi.GetColorCode(options.Theme.BlockquoteColor, true)}{options.Theme.BlockquotePrefix}{Ansi.Reset}This is a blockquote.{Environment.NewLine}"; // Use Env.NewLine
            AssertRender(input, expected, options, trimLeadingWhitespace: false); // Pass options
        }

        [Fact]
        public void Render_MultiLineBlockquote()
        {
            var input = "> First line.\n> Second line.";
            var options = new MarkdownRenderOptions { EnableColors = true }; // Define options
            var nl = Environment.NewLine;
            var expected = $"{Ansi.GetColorCode(options.Theme.BlockquoteColor, true)}{options.Theme.BlockquotePrefix}{Ansi.Reset}First line.{nl}" + 
                           $"{Ansi.GetColorCode(options.Theme.BlockquoteColor, true)}{options.Theme.BlockquotePrefix}{Ansi.Reset}Second line.{nl}"; 
            AssertRender(input, expected, options, trimLeadingWhitespace: false); // Pass options
        }

        [Fact]
        public void Render_BlockquoteWithEmptyLine()
        {
            var input = "> Just this line.";
            var options = new MarkdownRenderOptions { EnableColors = true }; // Define options
            var expected = $"{Ansi.GetColorCode(options.Theme.BlockquoteColor, true)}{options.Theme.BlockquotePrefix}{Ansi.Reset}Just this line.{Environment.NewLine}"; 
            AssertRender(input, expected, options, trimLeadingWhitespace: false); // Pass options
        }

        [Fact]
        public void Render_BlockquoteFollowedByParagraph()
        {
            var input = "> A quote.\n\nThen a paragraph.";
            var options = new MarkdownRenderOptions { EnableColors = true }; // Define options
            var nl = Environment.NewLine;
            var expected = $"{Ansi.GetColorCode(options.Theme.BlockquoteColor, true)}{options.Theme.BlockquotePrefix}{Ansi.Reset}A quote.{nl}" + 
                           nl + // Separator line
                           $"Then a paragraph.{nl}"; 
            AssertRender(input, expected, options, trimLeadingWhitespace: false); // Pass options
        }

        // --- Link Tests ---

        [Fact/*(Skip = "Revisit link parsing logic")*/]
        public void Render_InlineLink()
        {
            var input = "This is [a link](http://example.com).";
            var options = new MarkdownRenderOptions { EnableColors = true };
            var expected = $"This is {options.Theme.LinkTextStyle}a link{Ansi.Reset} ({options.Theme.LinkUrlStyle}http://example.com{Ansi.Reset}).{Environment.NewLine}"; 
            AssertRender(input, expected, options, trimLeadingWhitespace: false);
        }

        [Fact/*(Skip = "Revisit link parsing logic")*/]
        public void Render_InlineLinkWithTitle()
        {
            var input = "Check [this out](http://example.com \"Title\")."; // Title is ignored in output
            var options = new MarkdownRenderOptions { EnableColors = true };
            var expected = $"Check {options.Theme.LinkTextStyle}this out{Ansi.Reset} ({options.Theme.LinkUrlStyle}http://example.com{Ansi.Reset}).{Environment.NewLine}";
            AssertRender(input, expected, options, trimLeadingWhitespace: false);
        }

        // No skip needed - this test involves escape characters, not link rendering logic itself.
        [Fact]
        public void Render_EscapedLinkBrackets()
        {
            var input = "This is \\[not a link\\](http://example.com).";
            var options = new MarkdownRenderOptions { EnableColors = false };
            var expected = "This is [not a link](http://example.com)." + Environment.NewLine; 
            AssertRender(input, expected, options, trimLeadingWhitespace: false);
        }

        [Fact/*(Skip = "Revisit link parsing logic")*/]
        public void Render_ReferenceLink_Full()
        {
            var input = "See [the spec][ref].\n\n[ref]: http://spec.com";
            var options = new MarkdownRenderOptions { EnableColors = false };
            // Since definition comes AFTER usage, streaming parser renders literally.
            // Expect separation newline after paragraph.
            var expected = "See [the spec][ref]." + Environment.NewLine + Environment.NewLine;
            AssertRender(input, expected, options, trimLeadingWhitespace: false);
        }

        [Fact/*(Skip = "Revisit link parsing logic")*/]
        public void Render_ReferenceLink_Collapsed()
        {
            var input = "See [ref][].\n\n[ref]: http://spec.com";
            var options = new MarkdownRenderOptions { EnableColors = false };
            // Since definition comes AFTER usage, streaming parser renders literally.
            // Expect separation newline after paragraph.
            var expected = "See [ref][]." + Environment.NewLine + Environment.NewLine;
            AssertRender(input, expected, options, trimLeadingWhitespace: false);
        }

        [Fact/*(Skip = "Revisit link parsing logic")*/]
        public void Render_ReferenceLink_Shortcut()
        {
            var input = "See [ref].\n\n[ref]: http://spec.com";
            var options = new MarkdownRenderOptions { EnableColors = false };
            // Since definition comes AFTER usage, streaming parser renders literally.
            // Expect separation newline after paragraph.
            var expected = "See [ref]." + Environment.NewLine + Environment.NewLine;
            AssertRender(input, expected, options, trimLeadingWhitespace: false);
        }

        [Fact/*(Skip = "Revisit link parsing logic")*/]
        public void Render_ReferenceLink_CaseInsensitive()
        {
            var input = "See [The Spec].\n\n[the spec]: http://spec.com";
            var options = new MarkdownRenderOptions { EnableColors = false };
            // Since definition comes AFTER usage, streaming parser renders literally.
            // Expect separation newline after paragraph.
            var expected = "See [The Spec]." + Environment.NewLine + Environment.NewLine;
            AssertRender(input, expected, options, trimLeadingWhitespace: false);
        }

        [Fact/*(Skip = "Revisit link parsing logic")*/]
        public void Render_ReferenceLink_MissingDefinition()
        {
            var input = "This [is not][a link]."; // Definition missing
            var options = new MarkdownRenderOptions { EnableColors = false };
            // Definition is missing, so it renders literally.
            var expected = "This [is not][a link]." + Environment.NewLine;
            AssertRender(input, expected, options, trimLeadingWhitespace: false);
        }

        // --- Image Tests ---

        [Fact]
        public void Render_InlineImage()
        {
            var input = "Look: ![Alt text](image.png \"Title\")";
            var options = new MarkdownRenderOptions { EnableColors = true };
            var expected = $"Look: [Image: {Ansi.Faint}Alt text{Ansi.Reset}]{Environment.NewLine}"; // Use Env.NewLine
            AssertRender(input, expected, options, trimLeadingWhitespace: false);
        }

        [Fact]
        public void Render_EscapedImageBrackets()
        {
            var input = "Not an image: \\!\\[Alt text](image.png)";
            var options = new MarkdownRenderOptions { EnableColors = false };
            var expected = "Not an image: ![Alt text](image.png)" + Environment.NewLine; // Use Env.NewLine
            AssertRender(input, expected, options, trimLeadingWhitespace: false);
        }

        // --- HTML Stripping Test ---

        [Fact]
        public void Render_InlineHtmlStripped()
        {
            var input = "Text with <simple>tag</simple> and <br/>.";
            var options = new MarkdownRenderOptions { EnableColors = false };
            var expected = "Text with tag and ." + Environment.NewLine; // Use Env.NewLine
            AssertRender(input, expected, options, trimLeadingWhitespace: false);
        }

        // --- Task List Tests ---

        [Fact]
        public void Render_UnorderedTaskList_Unchecked()
        {
            var input = "- [ ] Task one";
            var options = new MarkdownRenderOptions { EnableColors = true }; // Needed to get theme colors
            var bulletColor = Ansi.GetColorCode(options.Theme.ListBulletColor, true);
            var reset = Ansi.Reset;
            var expected = $"{bulletColor}{options.Theme.TaskListUncheckedMarker}{reset} Task one{Environment.NewLine}"; // Space after reset

            AssertRender(input, expected, options, trimLeadingWhitespace: false); // Pass options
        }

        [Fact]
        public void Render_UnorderedTaskList_Checked()
        {
            var input = "* [x] Task two";
            var options = new MarkdownRenderOptions { EnableColors = true }; // Needed to get theme colors
            var bulletColor = Ansi.GetColorCode(options.Theme.ListBulletColor, true);
            var reset = Ansi.Reset;
            var expected = $"{bulletColor}{options.Theme.TaskListCheckedMarker}{reset} Task two{Environment.NewLine}"; // Space after reset

            AssertRender(input, expected, options, trimLeadingWhitespace: false); // Pass options
        }

        [Fact]
        public void Render_OrderedTaskList_Unchecked()
        {
            // Task lists override ordered list numbering with the checkbox
            var input = "1. [ ] Task one\n2. [ ] Task two";
            var options = new MarkdownRenderOptions { EnableColors = true }; // Needed to get theme colors
            var bulletColor = Ansi.GetColorCode(options.Theme.ListBulletColor, true);
            var reset = Ansi.Reset;
            var expected = $"{bulletColor}{options.Theme.TaskListUncheckedMarker}{reset} Task one{Environment.NewLine}{bulletColor}{options.Theme.TaskListUncheckedMarker}{reset} Task two{Environment.NewLine}"; // Space after reset

            AssertRender(input, expected, options, trimLeadingWhitespace: false); // Pass options
        }

        [Fact]
        public void Render_MixedTaskList()
        {
            var input = "- [ ] Unchecked\n- [X] Checked (caps)"; // Check case-insensitivity
            var options = new MarkdownRenderOptions { EnableColors = true }; // Needed to get theme colors
            var bulletColor = Ansi.GetColorCode(options.Theme.ListBulletColor, true);
            var reset = Ansi.Reset;
            // Use lowercase [x] in expected, as that's what the theme likely produces
            var expected = $"{bulletColor}{options.Theme.TaskListUncheckedMarker}{reset} Unchecked{Environment.NewLine}{bulletColor}{options.Theme.TaskListCheckedMarker}{reset} Checked (caps){Environment.NewLine}"; // Space after reset
            AssertRender(input, expected, options, trimLeadingWhitespace: false); // Pass options
        }

        [Fact]
        public void Render_TaskListWithIndentation()
        {
            var input = "  - [ ] Indented task";
            var options = new MarkdownRenderOptions { EnableColors = true }; // Needed to get theme colors
            var bulletColor = Ansi.GetColorCode(options.Theme.ListBulletColor, true);
            var reset = Ansi.Reset;
            var expected = $"  {bulletColor}{options.Theme.TaskListUncheckedMarker}{reset} Indented task{Environment.NewLine}"; // Space after reset

            AssertRender(input, expected, options, trimLeadingWhitespace: false); // Pass options
        }

        // --- Table Tests ---

        [Fact/*(Skip = "Revisit table parsing/rendering logic")*/]
        public void Render_SimpleTable()
        {
            // Arrange
            var inputMarkdown =
                "Header 1 | Header 2\n" +
                "-------- | --------\n" +
                "Row 1 Cell 1 | Row 1 Cell 2\n" +
                "Row 2 Cell 1 | Row 2 Cell 2";
            var options = new MarkdownRenderOptions { EnableColors = false };
            var nl = Environment.NewLine;
            // Expected GFM output requires padding based on content and header, min 3 dashes for separator
            // Header 1 (8) > -------- (8) => Width 8
            // Header 2 (8) > -------- (8) => Width 8
            // Row 1 Cell 1 (12) -> Max Width 12
            // Row 1 Cell 2 (12) -> Max Width 12
            var expectedOutput = 
                "| Header 1     | Header 2     |" + nl + // Pad to 12
                "| ------------ | ------------ |" + nl + // Separator uses 12 dashes with padding spaces
                "| Row 1 Cell 1 | Row 1 Cell 2 |" + nl +
                "| Row 2 Cell 1 | Row 2 Cell 2 |" + nl;

            // Act & Assert using helper
            AssertRender(inputMarkdown, expectedOutput, options, trimLeadingWhitespace: false);
        }

        [Fact]
        public void Render_TableWithAlignment()
        {
            var input = """
Left | Center | Right
:---|:---:|--:
L | C | R
Long Left | Longer Center | Long Right
""";
            // Widths: L(9)+2=11, C(13)+2=15, R(10)+2=12. Final widths: 9, 13, 10
            var expected = """
| Left      |    Center     |      Right |
| :-------- | :-----------: | ---------: |
| L         |       C       |          R |
| Long Left | Longer Center | Long Right |

""";
            AssertRender(input, expected, trimLeadingWhitespace: false);
        }

        [Fact]
        public void Render_TableWithMissingCells()
        {
            var input = """
Col A | Col B | Col C
-|-
One | Two
Four | | Six
""";
            // Widths: A(5)+2=7, B(5)+2=7, C(5)+2=7. Final Widths: 5, 5, 5
            var expected = """
| Col A | Col B | Col C |
| ----- | ----- | ----- |
| One   | Two   |       |
| Four  |       | Six   |

""";
            AssertRender(input, expected, trimLeadingWhitespace: false);
        }

        // TODO: Add test for table containing inline markdown (e.g., bold text)

        // --- Combination Tests (Can be added later) ---

    }
}
