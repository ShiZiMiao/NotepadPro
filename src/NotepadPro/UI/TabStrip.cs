namespace NotepadPro.UI
{
    /// <summary>A tab item rendered by <see cref="TabStrip"/>.</summary>
    public class TabItem
    {
        public required object Tag { get; init; }
        public Func<string>? TitleProvider { get; set; }
        public Func<bool>? DirtyProvider { get; set; }

        public string Title => TitleProvider?.Invoke() ?? string.Empty;
        public bool Dirty => DirtyProvider?.Invoke() ?? false;
    }

    /// <summary>
    /// Owner-drawn tab strip for document tabs: active tab, hover highlight,
    /// per-tab close button, modified marker and a right-click hook.
    /// </summary>
    public class TabStrip : Control
    {
        private const int TabMinWidth = 90;
        private const int TabMaxWidth = 240;
        private const int CloseBoxSize = 14;
        private const int CloseHitWidth = 20;

        private readonly List<TabItem> _items = new();
        private int _activeIndex = -1;
        private int _hoverIndex = -1;
        private bool _hoverOverClose;
        private readonly ToolTip _toolTip = new();
        private int _lastToolTipIndex = -1;

        public event Action<int>? TabActivated;
        public event Action<int>? TabCloseRequested;
        public event Action<int>? TabRightClicked;
        public event Action<TabItem>? TabMiddleClicked;

        public TabStrip()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.ResizeRedraw
                     | ControlStyles.UserPaint
                     | ControlStyles.Selectable, true);
            Height = 28;
            Cursor = Cursors.Arrow;
        }

        public IList<TabItem> Items => _items;

        public int ActiveIndex
        {
            get => _activeIndex;
            set
            {
                _activeIndex = value;
                Invalidate();
            }
        }

        private ThemeColors T => ThemeManager.Current;

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            g.Clear(T.TabBar);

            if (_items.Count == 0) return;

            int tabWidth = Math.Max(TabMinWidth,
                Math.Min(TabMaxWidth, (Width - 8) / _items.Count));
            int x = 2;
            var font = Font;
            using var textBrushActive = new SolidBrush(T.TabActiveText);
            using var textBrushInactive = new SolidBrush(T.TabInactiveText);
            using var textBrushHover = new SolidBrush(T.TabHoverText);
            using var dirtyBrush = new SolidBrush(T.TabDirty);
            using var closePen = new Pen(T.TabInactiveText);
            using var closePenHover = new Pen(T.TabCloseHover, 1.6f);

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                bool active = i == _activeIndex;
                bool hover = i == _hoverIndex;

                var rect = new Rectangle(x, active ? 2 : 4, tabWidth - 4, Height - (active ? 4 : 6));

                using (var bg = new SolidBrush(active ? T.TabActiveBg
                           : hover ? T.TabHoverBg : T.TabInactiveBg))
                {
                    g.FillRectangle(bg, rect);
                }
                if (active)
                {
                    using var pen = new Pen(T.TabBorder);
                    g.DrawRectangle(pen, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
                    // Accent line on top of the active tab
                    using var accent = new Pen(T.MenuHighlightBg, 2f);
                    g.DrawLine(accent, rect.X + 1, rect.Y + 1, rect.Right - 2, rect.Y + 1);
                }

                // Text (elide middle)
                string title = item.Title;
                int textX = rect.X + 8;
                int textWidth = rect.Width - CloseHitWidth - 14 - (item.Dirty ? 10 : 0);
                string shown = Elide(g, title, font, textWidth);
                var color = active ? textBrushActive : hover ? textBrushHover : textBrushInactive;
                TextRenderer.DrawText(g, shown, font, new Point(textX, rect.Y),
                    active ? T.TabActiveText : hover ? T.TabHoverText : T.TabInactiveText);

                // Dirty dot replaces the close glyph until hovered
                if (item.Dirty && !(hover && _hoverOverClose && i == _hoverIndex))
                {
                    g.FillEllipse(dirtyBrush, rect.Right - CloseHitWidth + 3, rect.Y + rect.Height / 2 - 3, 6, 6);
                }

                // Close glyph
                if (i == _hoverIndex)
                {
                    int cx = rect.Right - CloseHitWidth / 2;
                    int cy = rect.Y + rect.Height / 2;
                    int r = CloseBoxSize / 2 - 3;
                    var pen = _hoverOverClose ? closePenHover : closePen;
                    g.DrawLine(pen, cx - r, cy - r, cx + r, cy + r);
                    g.DrawLine(pen, cx - r, cy + r, cx + r, cy - r);
                }

                x += tabWidth;
                if (x > Width) break;
            }
        }

        private static string Elide(Graphics g, string text, Font font, int width)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var size = TextRenderer.MeasureText(text, font);
            if (size.Width <= width) return text;
            // Binary search the largest fitting prefix
            int lo = 0, hi = text.Length;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                var s = TextRenderer.MeasureText(text[..mid] + "…", font);
                if (s.Width <= width) lo = mid; else hi = mid - 1;
            }
            return text[..lo] + "…";
        }

        private int HitTest(int x, out bool overClose)
        {
            overClose = false;
            if (_items.Count == 0) return -1;
            int tabWidth = Math.Max(TabMinWidth,
                Math.Min(TabMaxWidth, (Width - 8) / _items.Count));
            int index = (x - 2) / tabWidth;
            if (index < 0 || index >= _items.Count) return -1;

            int tabRight = 2 + (index + 1) * tabWidth - 4;
            overClose = x >= tabRight - CloseHitWidth;
            return index;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int index = HitTest(e.X, out bool overClose);
            if (index != _hoverIndex || overClose != _hoverOverClose)
            {
                _hoverIndex = index;
                _hoverOverClose = overClose;
                Invalidate();
                if (index != _lastToolTipIndex)
                {
                    _lastToolTipIndex = index;
                    _toolTip.SetToolTip(this, index >= 0 && index < _items.Count
                        ? _items[index].Title : string.Empty);
                }
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverIndex = -1;
            _hoverOverClose = false;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            int index = HitTest(e.X, out bool overClose);
            if (index < 0) return;

            switch (e.Button)
            {
                case MouseButtons.Left:
                    if (overClose)
                    {
                        TabCloseRequested?.Invoke(index);
                    }
                    else if (index != _activeIndex)
                    {
                        ActiveIndex = index;
                        TabActivated?.Invoke(index);
                    }
                    break;
                case MouseButtons.Middle:
                    TabMiddleClicked?.Invoke(_items[index]);
                    break;
                case MouseButtons.Right:
                    TabRightClicked?.Invoke(index);
                    break;
            }
        }

        /// <summary>Wheel over the tab strip cycles documents.</summary>
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (_items.Count == 0) return;
            int delta = Math.Sign(e.Delta);
            int next = (_activeIndex - delta + _items.Count) % _items.Count;
            ActiveIndex = next;
            TabActivated?.Invoke(next);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _toolTip.Dispose();
            base.Dispose(disposing);
        }
    }
}
