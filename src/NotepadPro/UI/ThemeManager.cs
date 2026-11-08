using System.Runtime.InteropServices;
using NotepadPro.Languages;
using ScintillaNET;

namespace NotepadPro.UI
{
    /// <summary>All colors used by one theme, plus per-role syntax colors.</summary>
    public class ThemeColors
    {
        // Chrome
        public Color Window = SystemColors.Control;        // form background
        public Color WindowText = SystemColors.ControlText;
        public Color MenuBg, MenuText, MenuHighlightBg, MenuHighlightText, MenuBorder;
        public Color ToolbarBg, ToolbarBorder;
        public Color StatusBg, StatusText;

        // Tabs
        public Color TabBar, TabActiveBg, TabActiveText, TabInactiveBg, TabInactiveText,
                     TabHoverBg, TabHoverText, TabBorder, TabCloseHover, TabDirty;

        // Sidebar
        public Color SidebarBg, SidebarText, SidebarBorder, SidebarHeader, TreeHighlightBg;

        // Editor chrome
        public Color EditBg, EditFg, Caret, CurrentLine, SelectionBg, SelectionFg,
                     LineNumberBg, LineNumberFg, WhitespaceFg, IndentGuide,
                     FoldMarginBg, FoldMarginHl, FoldLine, BraceMatch, BraceBad;

        public Dictionary<StyleRole, (Color Color, RoleFlags Flags)> Syntax = new();

        public static ThemeColors Light()
        {
            var t = new ThemeColors
            {
                Window = Color.FromArgb(240, 240, 240),
                WindowText = Color.FromArgb(26, 26, 26),
                MenuBg = Color.FromArgb(245, 246, 248),
                MenuText = Color.FromArgb(26, 26, 26),
                MenuHighlightBg = Color.FromArgb(0, 120, 215),
                MenuHighlightText = Color.White,
                MenuBorder = Color.FromArgb(200, 202, 208),
                ToolbarBg = Color.FromArgb(243, 244, 246),
                ToolbarBorder = Color.FromArgb(214, 216, 220),
                StatusBg = Color.FromArgb(0, 120, 215),
                StatusText = Color.White,

                TabBar = Color.FromArgb(230, 232, 236),
                TabActiveBg = Color.White,
                TabActiveText = Color.FromArgb(20, 20, 20),
                TabInactiveBg = Color.FromArgb(214, 216, 222),
                TabInactiveText = Color.FromArgb(70, 70, 70),
                TabHoverBg = Color.FromArgb(224, 226, 232),
                TabHoverText = Color.FromArgb(30, 30, 30),
                TabBorder = Color.FromArgb(205, 207, 214),
                TabCloseHover = Color.FromArgb(0, 120, 215),
                TabDirty = Color.FromArgb(214, 60, 60),

                SidebarBg = Color.FromArgb(245, 246, 248),
                SidebarText = Color.FromArgb(40, 40, 40),
                SidebarBorder = Color.FromArgb(210, 212, 218),
                SidebarHeader = Color.FromArgb(120, 124, 132),
                TreeHighlightBg = Color.FromArgb(213, 233, 255),

                EditBg = Color.White,
                EditFg = Color.FromArgb(30, 30, 30),
                Caret = Color.Black,
                CurrentLine = Color.FromArgb(238, 243, 250),
                SelectionBg = Color.FromArgb(173, 214, 255),
                SelectionFg = Color.FromArgb(0, 0, 0),
                LineNumberBg = Color.FromArgb(240, 241, 244),
                LineNumberFg = Color.FromArgb(120, 124, 132),
                WhitespaceFg = Color.FromArgb(190, 194, 200),
                IndentGuide = Color.FromArgb(215, 218, 224),
                FoldMarginBg = Color.FromArgb(240, 241, 244),
                FoldMarginHl = Color.FromArgb(255, 255, 255),
                FoldLine = Color.FromArgb(160, 164, 172),
                BraceMatch = Color.FromArgb(0, 120, 215),
                BraceBad = Color.FromArgb(214, 60, 60),
            };

            t.Syntax = new Dictionary<StyleRole, (Color, RoleFlags)>
            {
                [StyleRole.Comment] = (Color.FromArgb(106, 135, 89), RoleFlags.None),
                [StyleRole.DocComment] = (Color.FromArgb(106, 135, 89), RoleFlags.Italic),
                [StyleRole.Keyword] = (Color.FromArgb(0, 0, 255), RoleFlags.None),
                [StyleRole.SecondaryKeyword] = (Color.FromArgb(43, 105, 163), RoleFlags.None),
                [StyleRole.String] = (Color.FromArgb(163, 21, 21), RoleFlags.None),
                [StyleRole.Character] = (Color.FromArgb(163, 21, 21), RoleFlags.None),
                [StyleRole.Number] = (Color.FromArgb(9, 134, 88), RoleFlags.None),
                [StyleRole.Operator] = (Color.FromArgb(50, 50, 50), RoleFlags.None),
                [StyleRole.Identifier] = (Color.FromArgb(30, 30, 30), RoleFlags.None),
                [StyleRole.Preprocessor] = (Color.FromArgb(135, 90, 20), RoleFlags.None),
                [StyleRole.Directive] = (Color.FromArgb(135, 90, 20), RoleFlags.None),
                [StyleRole.Regex] = (Color.FromArgb(129, 30, 130), RoleFlags.None),
                [StyleRole.Error] = (Color.FromArgb(214, 60, 60), RoleFlags.None),
                [StyleRole.MarkupTag] = (Color.FromArgb(43, 105, 163), RoleFlags.None),
                [StyleRole.MarkupAttribute] = (Color.FromArgb(135, 90, 20), RoleFlags.None),
                [StyleRole.MarkupEntity] = (Color.FromArgb(129, 30, 130), RoleFlags.None),
                [StyleRole.Embedded] = (Color.FromArgb(90, 40, 140), RoleFlags.None),
                [StyleRole.ClassName] = (Color.FromArgb(43, 105, 163), RoleFlags.Bold),
                [StyleRole.FunctionName] = (Color.FromArgb(121, 94, 38), RoleFlags.Bold),
                [StyleRole.Variable] = (Color.FromArgb(30, 30, 30), RoleFlags.None),
                [StyleRole.VariableName] = (Color.FromArgb(129, 30, 130), RoleFlags.None),
                [StyleRole.PropertyKey] = (Color.FromArgb(43, 105, 163), RoleFlags.None),
                [StyleRole.Section] = (Color.FromArgb(135, 90, 20), RoleFlags.Bold),
                [StyleRole.Symbol] = (Color.FromArgb(9, 134, 88), RoleFlags.None),
                [StyleRole.Header] = (Color.FromArgb(0, 90, 158), RoleFlags.Bold),
                [StyleRole.Bold] = (Color.FromArgb(30, 30, 30), RoleFlags.Bold),
                [StyleRole.Italic] = (Color.FromArgb(30, 30, 30), RoleFlags.Italic),
                [StyleRole.Link] = (Color.FromArgb(0, 102, 180), RoleFlags.None),
                [StyleRole.CodeInline] = (Color.FromArgb(163, 21, 21), RoleFlags.None),
                [StyleRole.ListMarker] = (Color.FromArgb(0, 120, 215), RoleFlags.Bold),
                [StyleRole.BlockQuote] = (Color.FromArgb(100, 110, 120), RoleFlags.Italic),
                [StyleRole.Strikeout] = (Color.FromArgb(110, 110, 110), RoleFlags.Strikeout),
                [StyleRole.HRule] = (Color.FromArgb(120, 124, 132), RoleFlags.None),
                [StyleRole.Cmdlet] = (Color.FromArgb(43, 105, 163), RoleFlags.None),
            };
            return t;
        }

        public static ThemeColors Dark()
        {
            var t = new ThemeColors
            {
                Window = Color.FromArgb(32, 32, 34),
                WindowText = Color.FromArgb(220, 222, 226),
                MenuBg = Color.FromArgb(38, 38, 42),
                MenuText = Color.FromArgb(220, 222, 226),
                MenuHighlightBg = Color.FromArgb(51, 102, 168),
                MenuHighlightText = Color.White,
                MenuBorder = Color.FromArgb(62, 62, 68),
                ToolbarBg = Color.FromArgb(36, 36, 40),
                ToolbarBorder = Color.FromArgb(56, 56, 62),
                StatusBg = Color.FromArgb(24, 24, 28),
                StatusText = Color.FromArgb(190, 194, 202),

                TabBar = Color.FromArgb(30, 30, 34),
                TabActiveBg = Color.FromArgb(52, 52, 58),
                TabActiveText = Color.FromArgb(235, 237, 240),
                TabInactiveBg = Color.FromArgb(40, 40, 44),
                TabInactiveText = Color.FromArgb(150, 154, 162),
                TabHoverBg = Color.FromArgb(46, 46, 52),
                TabHoverText = Color.FromArgb(200, 204, 210),
                TabBorder = Color.FromArgb(24, 24, 28),
                TabCloseHover = Color.FromArgb(120, 180, 255),
                TabDirty = Color.FromArgb(232, 108, 108),

                SidebarBg = Color.FromArgb(34, 34, 38),
                SidebarText = Color.FromArgb(200, 204, 212),
                SidebarBorder = Color.FromArgb(52, 52, 58),
                SidebarHeader = Color.FromArgb(130, 134, 144),
                TreeHighlightBg = Color.FromArgb(48, 70, 100),

                EditBg = Color.FromArgb(30, 30, 34),
                EditFg = Color.FromArgb(212, 214, 220),
                Caret = Color.FromArgb(230, 232, 238),
                CurrentLine = Color.FromArgb(40, 42, 50),
                SelectionBg = Color.FromArgb(52, 84, 130),
                SelectionFg = Color.FromArgb(235, 237, 240),
                LineNumberBg = Color.FromArgb(36, 36, 40),
                LineNumberFg = Color.FromArgb(108, 112, 122),
                WhitespaceFg = Color.FromArgb(78, 82, 92),
                IndentGuide = Color.FromArgb(66, 68, 78),
                FoldMarginBg = Color.FromArgb(36, 36, 40),
                FoldMarginHl = Color.FromArgb(46, 46, 52),
                FoldLine = Color.FromArgb(96, 100, 110),
                BraceMatch = Color.FromArgb(120, 180, 255),
                BraceBad = Color.FromArgb(232, 108, 108),
            };

            t.Syntax = new Dictionary<StyleRole, (Color, RoleFlags)>
            {
                [StyleRole.Comment] = (Color.FromArgb(106, 153, 85), RoleFlags.None),
                [StyleRole.DocComment] = (Color.FromArgb(106, 153, 85), RoleFlags.Italic),
                [StyleRole.Keyword] = (Color.FromArgb(86, 156, 214), RoleFlags.None),
                [StyleRole.SecondaryKeyword] = (Color.FromArgb(156, 220, 254), RoleFlags.None),
                [StyleRole.String] = (Color.FromArgb(206, 145, 120), RoleFlags.None),
                [StyleRole.Character] = (Color.FromArgb(206, 145, 120), RoleFlags.None),
                [StyleRole.Number] = (Color.FromArgb(181, 206, 168), RoleFlags.None),
                [StyleRole.Operator] = (Color.FromArgb(212, 214, 220), RoleFlags.None),
                [StyleRole.Identifier] = (Color.FromArgb(212, 214, 220), RoleFlags.None),
                [StyleRole.Preprocessor] = (Color.FromArgb(201, 156, 220), RoleFlags.None),
                [StyleRole.Directive] = (Color.FromArgb(201, 156, 220), RoleFlags.None),
                [StyleRole.Regex] = (Color.FromArgb(212, 154, 212), RoleFlags.None),
                [StyleRole.Error] = (Color.FromArgb(232, 108, 108), RoleFlags.None),
                [StyleRole.MarkupTag] = (Color.FromArgb(86, 156, 214), RoleFlags.None),
                [StyleRole.MarkupAttribute] = (Color.FromArgb(156, 220, 254), RoleFlags.None),
                [StyleRole.MarkupEntity] = (Color.FromArgb(212, 154, 212), RoleFlags.None),
                [StyleRole.Embedded] = (Color.FromArgb(180, 150, 230), RoleFlags.None),
                [StyleRole.ClassName] = (Color.FromArgb(156, 220, 254), RoleFlags.Bold),
                [StyleRole.FunctionName] = (Color.FromArgb(220, 220, 170), RoleFlags.Bold),
                [StyleRole.Variable] = (Color.FromArgb(156, 220, 254), RoleFlags.None),
                [StyleRole.VariableName] = (Color.FromArgb(189, 151, 238), RoleFlags.None),
                [StyleRole.PropertyKey] = (Color.FromArgb(156, 220, 254), RoleFlags.None),
                [StyleRole.Section] = (Color.FromArgb(201, 156, 220), RoleFlags.Bold),
                [StyleRole.Symbol] = (Color.FromArgb(181, 206, 168), RoleFlags.None),
                [StyleRole.Header] = (Color.FromArgb(120, 180, 255), RoleFlags.Bold),
                [StyleRole.Bold] = (Color.FromArgb(235, 237, 240), RoleFlags.Bold),
                [StyleRole.Italic] = (Color.FromArgb(235, 237, 240), RoleFlags.Italic),
                [StyleRole.Link] = (Color.FromArgb(96, 170, 240), RoleFlags.None),
                [StyleRole.CodeInline] = (Color.FromArgb(206, 145, 120), RoleFlags.None),
                [StyleRole.ListMarker] = (Color.FromArgb(120, 180, 255), RoleFlags.Bold),
                [StyleRole.BlockQuote] = (Color.FromArgb(140, 146, 158), RoleFlags.Italic),
                [StyleRole.Strikeout] = (Color.FromArgb(140, 144, 154), RoleFlags.Strikeout),
                [StyleRole.HRule] = (Color.FromArgb(130, 134, 144), RoleFlags.None),
                [StyleRole.Cmdlet] = (Color.FromArgb(156, 220, 254), RoleFlags.None),
            };
            return t;
        }
    }

    /// <summary>
    /// Applies the active theme to forms, toolbars and Scintilla editors, and
    /// switches the Windows title bar between light and dark via DWM.
    /// </summary>
    public static class ThemeManager
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public static ThemeColors Current { get; private set; } = ThemeColors.Light();

        /// <summary>True while the dark palette is active.</summary>
        public static bool IsDark { get; private set; }

        /// <summary>Applies the theme and notifies every open themed control.</summary>
        public static void Apply(AppTheme theme)
        {
            Current = theme == AppTheme.Dark ? ThemeColors.Dark() : ThemeColors.Light();
            IsDark = theme == AppTheme.Dark;

            var dark = theme == AppTheme.Dark ? 1 : 0;
            foreach (Form form in Application.OpenForms)
            {
                DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
            }
            ThemeChanged?.Invoke();
        }

        /// <summary>Raised after the palette changes; subscribers re-apply themselves.</summary>
        public static event Action? ThemeChanged;

        /// <summary>Standard chrome application for a Form.</summary>
        public static void ApplyChrome(Control form)
        {
            form.BackColor = Current.Window;
            form.ForeColor = Current.WindowText;
            ApplyToChildren(form);
        }

        /// <summary>Walks the control tree applying chrome colors.</summary>
        public static void ApplyToChildren(Control root)
        {
            foreach (Control c in root.Controls)
            {
                switch (c)
                {
                    case MenuStrip menu:
                        menu.BackColor = Current.MenuBg;
                        menu.ForeColor = Current.MenuText;
                        menu.Renderer = new ThemedMenuRenderer();
                        ApplyMenuItems(menu.Items);
                        break;
                    case StatusStrip status:
                        status.BackColor = Current.StatusBg;
                        status.ForeColor = Current.StatusText;
                        foreach (ToolStripItem item in status.Items)
                            item.ForeColor = Current.StatusText;
                        break;
                    case ToolStrip strip:
                        strip.BackColor = Current.ToolbarBg;
                        strip.ForeColor = Current.WindowText;
                        strip.Renderer = new ThemedMenuRenderer();
                        foreach (ToolStripItem item in strip.Items)
                            item.ForeColor = Current.WindowText;
                        break;
                    default:
                        if (c is TabStrip or SidebarPanel or ThemedPanel or ThemedListBox)
                        {
                            // Self-theming controls subscribe to ThemeChanged.
                        }
                        else
                        {
                            c.BackColor = Current.Window;
                            c.ForeColor = Current.WindowText;
                        }
                        break;
                }
                ApplyToChildren(c);
            }
        }

        private static void ApplyMenuItems(ToolStripItemCollection items)
        {
            foreach (ToolStripItem item in items)
            {
                item.ForeColor = Current.MenuText;
                if (item is ToolStripMenuItem { HasDropDownItems: true } menu)
                    ApplyMenuItems(menu.DropDownItems);
            }
        }

        /// <summary>
        /// Applies the full editor appearance (default style, margins, caret,
        /// selection, whitespace, folding) plus the language style map.
        /// </summary>
        public static void ApplyToEditor(Scintilla sci, LanguageDef? language, AppSettings settings)
        {
            sci.StyleResetDefault();
            sci.Styles[ScintillaNET.Style.Default].Font = settings.FontName;
            sci.Styles[ScintillaNET.Style.Default].Size = settings.FontSize;
            sci.Styles[ScintillaNET.Style.Default].BackColor = Current.EditBg;
            sci.Styles[ScintillaNET.Style.Default].ForeColor = Current.EditFg;
            sci.StyleClearAll();

            // Margins
            sci.Margins[0].Width = LineNumberWidth(sci);
            sci.Margins[0].Type = MarginType.Number;
            sci.Margins[0].Sensitive = false;

            sci.Margins[1].Width = 4;
            sci.Margins[1].Type = MarginType.Symbol;
            sci.Margins[1].Mask = 0; // no symbols in the small gutter

            bool foldMargin = settings.ShowFoldMargin && (language?.Foldable ?? false);
            sci.Margins[2].Width = foldMargin ? 16 : 0;
            sci.Margins[2].Type = MarginType.Symbol;
            const uint foldMask = 0xFE000000u;
            sci.Margins[2].Mask = foldMargin ? foldMask : 0;
            sci.Margins[2].Sensitive = foldMargin;

            // Fold markers (Scintilla5.NET 7.x exposes no marker color API, so the
            // default two-tone symbols are used and only the margin is themed.)
            sci.Markers[ScintillaNET.Marker.Folder].Symbol = MarkerSymbol.BoxPlus;
            sci.Markers[ScintillaNET.Marker.FolderOpen].Symbol = MarkerSymbol.BoxMinus;
            sci.Markers[ScintillaNET.Marker.FolderEnd].Symbol = MarkerSymbol.BoxPlusConnected;
            sci.Markers[ScintillaNET.Marker.FolderMidTail].Symbol = MarkerSymbol.VLine;
            sci.Markers[ScintillaNET.Marker.FolderOpenMid].Symbol = MarkerSymbol.BoxMinusConnected;

            sci.SetFoldMarginColor(true, Current.FoldMarginBg);
            sci.SetFoldMarginHighlightColor(true, Current.FoldMarginHl);
            sci.FoldLineBackColor = Current.FoldMarginBg;

            // Caret, selection, current line
            sci.CaretForeColor = Current.Caret;
            sci.CaretLineVisible = true;
            sci.CaretLineBackColor = Current.CurrentLine;
            sci.SetSelectionBackColor(true, Current.SelectionBg);
            sci.SetSelectionForeColor(true, Current.SelectionFg);

            // Whitespace / indent guides / EOL
            sci.ViewWhitespace = settings.ShowWhitespace
                ? WhitespaceMode.VisibleAlways : WhitespaceMode.Invisible;
            sci.SetWhitespaceForeColor(true, Current.WhitespaceFg);
            sci.ViewEol = settings.ShowEol;
            sci.IndentationGuides = IndentView.LookBoth;

            // Word wrap
            sci.WrapMode = settings.WrapLines ? WrapMode.Word : WrapMode.None;

            // Brace matching colors
            sci.Styles[ScintillaNET.Style.BraceLight].BackColor = Current.BraceMatch;
            sci.Styles[ScintillaNET.Style.BraceLight].ForeColor = Color.White;
            sci.Styles[ScintillaNET.Style.BraceBad].ForeColor = Current.BraceBad;

            // Indent guide color
            sci.Styles[ScintillaNET.Style.IndentGuide].ForeColor = Current.IndentGuide;
            sci.Styles[ScintillaNET.Style.IndentGuide].BackColor = Current.EditBg;

            // Language-specific styles
            if (language != null)
            {
                var styleMap = language.StyleMap ?? Array.Empty<(int, StyleRole)>();
                foreach (var (styleId, role) in styleMap)
                {
                    if (!Current.Syntax.TryGetValue(role, out var spec)) continue;
                    var style = sci.Styles[styleId];
                    style.ForeColor = spec.Color;
                    style.BackColor = Current.EditBg;
                    style.Bold = spec.Flags.HasFlag(RoleFlags.Bold);
                    style.Italic = spec.Flags.HasFlag(RoleFlags.Italic);
                    if (spec.Flags.HasFlag(RoleFlags.Strikeout)) style.FillLine = false;
                }

                // Enable lexer folding where supported.
                if (language.Foldable)
                {
                    sci.SetProperty("fold", "1");
                    sci.SetProperty("fold.compact", "0");
                    sci.SetProperty("fold.html", "1");
                    sci.SetProperty("fold.comment", "1");
                }
                else
                {
                    sci.SetProperty("fold", "0");
                }
            }

            // Line number style tracks the margin colors.
            sci.Styles[ScintillaNET.Style.LineNumber].ForeColor = Current.LineNumberFg;
            sci.Styles[ScintillaNET.Style.LineNumber].BackColor = Current.LineNumberBg;
        }

        /// <summary>Width in pixels to fit the digit count of the largest line number.</summary>
        public static int LineNumberWidth(Scintilla sci)
        {
            int digits = Math.Max(2, 1 + (int)Math.Log10(Math.Max(1, sci.Lines.Count)));
            return 8 + digits * (sci.Styles[ScintillaNET.Style.Default].Size + 2);
        }

        /// <summary>ToolStripProfessionalRenderer using the theme palette.</summary>
        private sealed class ThemedMenuRenderer : ToolStripProfessionalRenderer
        {
            public ThemedMenuRenderer() : base(new ThemedColorTable()) { }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                var g = e.Graphics;
                var bounds = new Rectangle(Point.Empty, e.Item.Size);
                if (e.Item.Selected || e.Item.Pressed)
                {
                    using var b = new SolidBrush(Current.MenuHighlightBg);
                    g.FillRectangle(b, 1, 1, bounds.Width - 2, bounds.Height - 2);
                }
                else
                {
                    using var b = new SolidBrush(Current.MenuBg);
                    g.FillRectangle(b, bounds);
                }
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                using var pen = new Pen(Current.MenuBorder);
                var bounds = e.ToolStrip.ClientRectangle;
                e.Graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
            }
        }

        private sealed class ThemedColorTable : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground => Current.MenuBg;
            public override Color ImageMarginGradientBegin => Current.MenuBg;
            public override Color ImageMarginGradientMiddle => Current.MenuBg;
            public override Color ImageMarginGradientEnd => Current.MenuBg;
            public override Color MenuBorder => Current.MenuBorder;
            public override Color MenuItemBorder => Current.MenuBg;
            public override Color SeparatorDark => Current.MenuBorder;
            public override Color SeparatorLight => Current.MenuBorder;
            public override Color ToolStripGradientBegin => Current.ToolbarBg;
            public override Color ToolStripGradientMiddle => Current.ToolbarBg;
            public override Color ToolStripGradientEnd => Current.ToolbarBg;
            public override Color ToolStripBorder => Current.ToolbarBorder;
            public override Color GripDark => Current.ToolbarBorder;
            public override Color GripLight => Current.ToolbarBg;
        }
    }

    /// <summary>Base panel participating in theme refresh.</summary>
    public class ThemedPanel : Panel
    {
        public ThemedPanel()
        {
            ThemeManager.ThemeChanged += () =>
            {
                if (IsHandleCreated) BeginInvoke(new Action(() =>
                {
                    BackColor = ThemeManager.Current.SidebarBg;
                    ForeColor = ThemeManager.Current.SidebarText;
                    OnThemeApplied();
                }));
            };
        }

        protected virtual void OnThemeApplied() { }
    }

    /// <summary>List box themed like the sidebar.</summary>
    public class ThemedListBox : ListBox
    {
        public ThemedListBox()
        {
            BorderStyle = System.Windows.Forms.BorderStyle.None;
            ThemeManager.ThemeChanged += () =>
            {
                if (IsHandleCreated) BeginInvoke(new Action(() =>
                {
                    BackColor = ThemeManager.Current.SidebarBg;
                    ForeColor = ThemeManager.Current.SidebarText;
                    Invalidate();
                }));
            };
        }
    }
}
