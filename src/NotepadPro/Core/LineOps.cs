using System.Text;
using ScintillaNET;

namespace NotepadPro
{
    /// <summary>
    /// Line and text operations built on plain text manipulation so they work
    /// uniformly across lexers (duplicate/delete/move line, case, comment
    /// toggling, indentation conversion, trailing whitespace trim).
    /// </summary>
    public static class LineOps
    {
        private static string EolOf(Scintilla sci) => sci.EolMode switch
        {
            Eol.Lf => "\n",
            Eol.Cr => "\r",
            _ => "\r\n"
        };

        public static void DuplicateLine(Scintilla sci)
        {
            int line = sci.CurrentLine;
            var lineObj = sci.Lines[line];
            string text = lineObj.Text.TrimEnd('\r', '\n');
            int insertAt = lineObj.EndPosition;
            sci.InsertText(insertAt, text + EolOf(sci));
            sci.CurrentPosition = sci.CurrentPosition + (lineObj.EndPosition - lineObj.Position);
            sci.ScrollCaret();
        }

        public static void DeleteLine(Scintilla sci)
        {
            int first = sci.LineFromPosition(sci.SelectionStart);
            int last = sci.LineFromPosition(sci.SelectionEnd);
            int start = sci.Lines[first].Position;
            int end = last + 1 < sci.Lines.Count
                ? sci.Lines[last + 1].Position
                : sci.Lines[last].EndPosition;
            if (start == 0 && end >= sci.TextLength)
            {
                sci.ClearAll();
                return;
            }
            sci.SetSel(start, end);
            sci.Clear();
        }

        public static void MoveLinesUp(Scintilla sci)
        {
            int first = sci.LineFromPosition(sci.SelectionStart);
            if (first == 0) return;
            int last = sci.LineFromPosition(sci.SelectionEnd);
            MoveBlock(sci, first, last, down: false);
        }

        public static void MoveLinesDown(Scintilla sci)
        {
            int last = sci.LineFromPosition(sci.SelectionEnd);
            if (last + 1 >= sci.Lines.Count) return;
            int first = sci.LineFromPosition(sci.SelectionStart);
            MoveBlock(sci, first, last, down: true);
        }

        /// <summary>Moves the line block [first..last] by one line via a single atomic edit.</summary>
        private static void MoveBlock(Scintilla sci, int first, int last, bool down)
        {
            string eol = EolOf(sci);
            int blockStart = sci.Lines[first].Position;
            var lastLine = sci.Lines[last];
            bool blockAtEof = last + 1 >= sci.Lines.Count;
            int blockEnd = blockAtEof ? lastLine.EndPosition : sci.Lines[last + 1].Position;

            sci.BeginUndoAction();
            try
            {
                if (!down)
                {
                    // Swap "line above" with the block: [above..blockEnd] -> block + above.
                    var above = sci.Lines[first - 1];
                    string aboveText = sci.GetTextRange(above.Position, sci.Lines[first].Position);
                    string blockText = sci.GetTextRange(blockStart, blockEnd);
                    sci.SetSel(above.Position, blockEnd);
                    sci.ReplaceSelection(blockText + aboveText);
                    sci.SetSel(above.Position, above.Position + blockText.Length);
                }
                else
                {
                    // Swap the block with "line below": [blockStart..belowEnd] -> below + block.
                    var below = sci.Lines[last + 1];
                    bool belowAtEof = last + 2 >= sci.Lines.Count;
                    int belowEnd = belowAtEof ? below.EndPosition : sci.Lines[last + 2].Position;
                    string belowText = sci.GetTextRange(below.Position, belowEnd);
                    string blockText = sci.GetTextRange(blockStart, blockEnd);

                    // When the block sits at EOF without a trailing EOL, borrow one
                    // from the line below so the rotation stays well-formed.
                    string replacement;
                    if (blockAtEof && !sci.GetTextRange(blockStart, blockEnd).EndsWith(eol))
                    {
                        // block + below  =>  below + eol + block
                        string belowBody = belowText.TrimEnd('\r', '\n');
                        replacement = belowBody + eol + blockText;
                    }
                    else
                    {
                        replacement = belowText + blockText;
                    }
                    sci.SetSel(blockStart, belowEnd);
                    sci.ReplaceSelection(replacement);
                    sci.SetSel(blockStart + belowText.Length, blockStart + belowText.Length + blockText.Length);
                }
                sci.ScrollCaret();
            }
            finally
            {
                sci.EndUndoAction();
            }
        }

        public static void ToUpper(Scintilla sci)
        {
            if (sci.SelectedText.Length == 0) return;
            sci.ReplaceSelection(sci.SelectedText.ToUpper());
        }

        public static void ToLower(Scintilla sci)
        {
            if (sci.SelectedText.Length == 0) return;
            sci.ReplaceSelection(sci.SelectedText.ToLower());
        }

        /// <summary>Toggles a line comment prefix over the selected line range.</summary>
        public static void ToggleLineComment(Scintilla sci, string prefix)
        {
            if (string.IsNullOrEmpty(prefix)) return;
            int first = sci.LineFromPosition(sci.SelectionStart);
            int last = sci.LineFromPosition(sci.SelectionEnd);
            bool allCommented = true;
            for (int i = first; i <= last; i++)
            {
                if (!GetLineText(sci, i).TrimStart().StartsWith(prefix))
                {
                    allCommented = false;
                    break;
                }
            }

            sci.BeginUndoAction();
            try
            {
                for (int i = first; i <= last; i++)
                {
                    var line = sci.Lines[i];
                    string text = line.Text.TrimEnd('\r', '\n');
                    if (allCommented)
                    {
                        int idx = text.IndexOf(prefix, StringComparison.Ordinal);
                        if (idx >= 0)
                        {
                            // Remove prefix plus one optional space.
                            int removeLen = prefix.Length;
                            if (idx + removeLen < text.Length && text[idx + removeLen] == ' ') removeLen++;
                            sci.SetSel(line.Position + idx, line.Position + idx + removeLen);
                            sci.Clear();
                        }
                    }
                    else if (text.Trim().Length > 0)
                    {
                        sci.InsertText(line.Position, prefix + " ");
                    }
                    else
                    {
                        sci.InsertText(line.Position, prefix);
                    }
                }
            }
            finally
            {
                sci.EndUndoAction();
            }
        }

        /// <summary>Wraps the selection (or inserts empty markers) in a block comment.</summary>
        public static void ToggleBlockComment(Scintilla sci, string open, string close)
        {
            if (string.IsNullOrEmpty(open)) return;
            if (sci.SelectedText.Length > 0)
            {
                sci.BeginUndoAction();
                try
                {
                    int start = sci.SelectionStart, end = sci.SelectionEnd;
                    sci.SetSel(end, end);
                    sci.InsertText(end, close);
                    sci.SetSel(start, start);
                    sci.InsertText(start, open);
                }
                finally
                {
                    sci.EndUndoAction();
                }
            }
            else
            {
                int pos = sci.CurrentPosition;
                sci.InsertText(pos, open + close);
                sci.CurrentPosition = pos + open.Length;
            }
        }

        private static string GetLineText(Scintilla sci, int line) =>
            sci.Lines[line].Text.TrimEnd('\r', '\n');

        /// <summary>Converts leading tabs to spaces (or spaces to tabs when useTabs).</summary>
        public static void ConvertIndentation(Scintilla sci, bool toSpaces, int tabWidth)
        {
            sci.BeginUndoAction();
            try
            {
                for (int i = 0; i < sci.Lines.Count; i++)
                {
                    var line = sci.Lines[i];
                    string text = line.Text;
                    if (text.Length == 0) continue;

                    // Measure the leading whitespace run.
                    int lead = 0;
                    while (lead < text.Length && (text[lead] == '\t' || text[lead] == ' ')) lead++;
                    if (lead == 0) continue;
                    string leading = text[..lead];
                    bool hasTab = leading.Contains('\t');
                    bool hasSpaces = leading.Contains(' ');
                    if (toSpaces && !hasTab) continue;
                    if (!toSpaces && !hasSpaces && !hasTab) continue;

                    string converted = ExpandOrContract(leading, toSpaces, tabWidth);
                    if (converted == leading) continue;
                    sci.SetSel(line.Position, line.Position + lead);
                    sci.ReplaceSelection(converted);
                }
            }
            finally
            {
                sci.EndUndoAction();
            }
        }

        private static string ExpandOrContract(string leading, bool toSpaces, int tabWidth)
        {
            if (toSpaces)
            {
                var sb = new StringBuilder();
                int col = 0;
                foreach (char c in leading)
                {
                    if (c == '\t')
                    {
                        int next = (col / tabWidth + 1) * tabWidth;
                        sb.Append(' ', next - col);
                        col = next;
                    }
                    else
                    {
                        sb.Append(c);
                        col++;
                    }
                }
                return sb.ToString();
            }

            // Spaces to tabs: only collapse runs that align on tab stops.
            var sbT = new StringBuilder();
            int colT = 0, runStart = -1;
            for (int i = 0; i <= leading.Length; i++)
            {
                bool isSpace = i < leading.Length && leading[i] == ' ';
                if (isSpace)
                {
                    if (runStart < 0) runStart = i;
                    colT++;
                }
                else
                {
                    if (runStart >= 0)
                    {
                        int tabs = colT / tabWidth;
                        int rem = colT % tabWidth;
                        sbT.Append('\t', tabs);
                        sbT.Append(' ', rem);
                        runStart = -1;
                    }
                    if (i < leading.Length)
                    {
                        sbT.Append(leading[i]); // a tab inside the leading run
                        colT++;
                    }
                }
            }
            return sbT.ToString();
        }

        /// <summary>Removes trailing whitespace from every line.</summary>
        public static void TrimTrailingWhitespace(Scintilla sci)
        {
            string eol = EolOf(sci);
            sci.BeginUndoAction();
            try
            {
                for (int i = 0; i < sci.Lines.Count; i++)
                {
                    var line = sci.Lines[i];
                    string text = line.Text;
                    int end = text.Length;
                    while (end > 0 && (text[end - 1] == ' ' || text[end - 1] == '\t')) end--;
                    int contentEnd = line.Position + end;
                    int lineEnd = line.EndPosition;
                    if (contentEnd < lineEnd)
                    {
                        sci.SetSel(contentEnd, lineEnd);
                        sci.Clear();
                    }
                }
            }
            finally
            {
                sci.EndUndoAction();
            }
        }
    }
}
