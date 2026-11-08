using ScintillaNET;

namespace NotepadPro
{
    /// <summary>Search/replace options as shown in the Find/Replace dialog.</summary>
    public class FindOptions
    {
        public string Needle { get; set; } = string.Empty;
        public bool MatchCase { get; set; }
        public bool WholeWord { get; set; }
        public bool Regex { get; set; }
        public bool WrapAround { get; set; } = true;
        public bool Down { get; set; } = true;
        public bool InSelection { get; set; }

        public SearchFlags ToFlags() =>
            (MatchCase ? ScintillaNET.SearchFlags.MatchCase : ScintillaNET.SearchFlags.None) |
            (WholeWord ? ScintillaNET.SearchFlags.WholeWord : ScintillaNET.SearchFlags.None) |
            (Regex ? ScintillaNET.SearchFlags.Regex : ScintillaNET.SearchFlags.None);
    }

    public readonly record struct SearchResult(int Start, int End)
    {
        public bool Found => Start >= 0;
        public static readonly SearchResult None = new(-1, -1);
    }

    /// <summary>
    /// Search engine over Scintilla's Target* API. All searches are stateless
    /// so multiple dialogs and quick-find stay consistent.
    /// </summary>
    public static class FindReplaceEngine
    {
        /// <summary>Finds the next match at or after <paramref name="from"/>, honoring wrap-around.</summary>
        public static SearchResult FindNext(Scintilla sci, FindOptions opt, int from, int selStart, int selEnd)
        {
            if (opt.Needle.Length == 0) return SearchResult.None;
            sci.SearchFlags = opt.ToFlags();

            // Start after the current selection so repeated "find next" advances.
            int start = opt.Down ? Math.Max(from, selEnd) : 0;
            int end = opt.Down ? sci.TextLength : Math.Max(0, selStart);

            var hit = SearchRange(sci, opt, start, end);
            if (!hit.Found && opt.WrapAround)
            {
                hit = opt.Down
                    ? SearchRange(sci, opt, 0, Math.Min(sci.TextLength, selStart + opt.Needle.Length))
                    : SearchRange(sci, opt, Math.Max(0, selEnd), sci.TextLength);
            }
            return hit;
        }

        /// <summary>Searches a single range; regex "search" advances by one char on empty matches.</summary>
    private static SearchResult SearchRange(Scintilla sci, FindOptions opt, int start, int end)
    {
        if (start >= end) return SearchResult.None;
        sci.TargetStart = start;
        sci.TargetEnd = end;
        int pos = sci.SearchInTarget(opt.Needle);
        if (pos < 0) return SearchResult.None;
        if (sci.TargetEnd == sci.TargetStart) return SearchResult.None; // empty match guard
        return new SearchResult(sci.TargetStart, sci.TargetEnd);
    }

        /// <summary>Counts matches in the whole document (or current selection).</summary>
        public static int CountMatches(Scintilla sci, FindOptions opt)
        {
            if (opt.Needle.Length == 0) return 0;
            sci.SearchFlags = opt.ToFlags();
            int count = 0;
            int start = opt.InSelection ? sci.SelectionStart : 0;
            int end = opt.InSelection ? sci.SelectionEnd : sci.TextLength;
            sci.TargetStart = start;
            sci.TargetEnd = end;
            while (sci.TargetStart < sci.TargetEnd)
            {
                int pos = sci.SearchInTarget(opt.Needle);
                if (pos < 0) break;
                count++;
                int foundEnd = sci.TargetEnd;
                if (foundEnd == pos)
                {
                    // Empty match: move one character to guarantee progress.
                    if (foundEnd >= end) break;
                    sci.TargetStart = foundEnd + 1;
                }
                else
                {
                    sci.TargetStart = foundEnd;
                }
                sci.TargetEnd = end;
            }
            return count;
        }

        /// <summary>Selects the match and returns true when found.</summary>
        public static bool SelectMatch(Scintilla sci, SearchResult hit)
        {
            if (!hit.Found) return false;
            sci.SetSelection(hit.End, hit.Start);
            sci.ScrollCaret();
            return true;
        }

        /// <summary>Replaces the current selection if it equals a valid match; returns true when replaced.</summary>
        public static bool ReplaceCurrent(Scintilla sci, FindOptions opt, string replacement)
        {
            if (opt.Needle.Length == 0) return false;
            sci.SearchFlags = opt.ToFlags();

            // Verify the current selection matches.
            sci.TargetStart = sci.SelectionStart;
            sci.TargetEnd = sci.SelectionEnd;
            if (sci.SearchInTarget(opt.Needle) < 0 || sci.TargetStart != sci.SelectionStart)
            {
                // Selection is not a match: find the next one instead.
                var next = FindNext(sci, opt, 0, sci.SelectionStart, sci.SelectionEnd);
                if (!next.Found) return false;
                SelectMatch(sci, next);
                return false;
            }

            DoReplace(sci, opt, replacement, sci.SelectionStart, sci.SelectionEnd);
            return true;
        }

        /// <summary>Replaces all matches in the document (or selection); returns the count.</summary>
        public static int ReplaceAll(Scintilla sci, FindOptions opt, string replacement)
        {
            if (opt.Needle.Length == 0) return 0;
            sci.SearchFlags = opt.ToFlags();

            int count = 0;
            int start = opt.InSelection ? sci.SelectionStart : 0;
            int end = opt.InSelection ? sci.SelectionEnd : sci.TextLength;

            sci.BeginUndoAction();
            try
            {
                sci.TargetStart = start;
                sci.TargetEnd = end;
                while (sci.TargetStart <= end)
                {
                    int pos = sci.SearchInTarget(opt.Needle);
                    if (pos < 0) break;

                    int foundStart = sci.TargetStart;
                    int foundEnd = sci.TargetEnd;
                    DoReplace(sci, opt, replacement, foundStart, foundEnd);
                    count++;

                    int delta = (opt.Regex ? replacement.Length : replacement.Length)
                                - (foundEnd - foundStart);
                    end += delta;
                    sci.TargetStart = foundStart + replacement.Length;
                    // Zero-length matches (e.g. regex "^") must not loop forever.
                    if (sci.TargetStart <= foundStart) sci.TargetStart = foundStart + 1;
                    sci.TargetEnd = end;
                    if (sci.TargetStart > sci.TargetEnd) break;
                }
            }
            finally
            {
                sci.EndUndoAction();
            }
            return count;
        }

        private static void DoReplace(Scintilla sci, FindOptions opt, string replacement, int start, int end)
        {
            sci.TargetStart = start;
            sci.TargetEnd = end;
            if (opt.Regex)
                sci.ReplaceTargetRe(replacement);
            else
                sci.ReplaceTarget(replacement);
        }
    }
}

