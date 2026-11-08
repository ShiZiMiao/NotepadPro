using NotepadPro.Languages;
using NotepadPro.UI;
using ScintillaNET;

namespace NotepadPro
{
    /// <summary>
    /// Main application window: menu bar, tool bar, tab strip, editor container,
    /// sidebar and status bar. Document behavior lives in Core/*.
    /// </summary>
    public class MainForm : Form
    {
        private readonly AppSettings _settings;
        private readonly RecentFilesStore _recent;

        private readonly List<DocumentBuffer> _documents = new();
        private int _activeIndex = -1;

        // Layout
        private readonly MenuStrip _menu = new();
        private readonly ToolStrip _toolbar = new();
        private readonly TabStrip _tabs = new();
        private readonly Panel _editorHost = new();
        private readonly SplitContainer _split = new();
        private SidebarPanel _sidebar;
        private readonly StatusStrip _status = new();

        // Status labels
        private readonly ToolStripStatusLabel _stPath = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        private readonly ToolStripStatusLabel _stLength = new();
        private readonly ToolStripStatusLabel _stLines = new();
        private readonly ToolStripStatusLabel _stPos = new();
        private readonly ToolStripStatusLabel _stSel = new();
        private readonly ToolStripStatusLabel _stEol = new();
        private readonly ToolStripStatusLabel _stEnc = new();
        private readonly ToolStripStatusLabel _stIns = new();
        private readonly ToolStripStatusLabel _stZoom = new();

        // Menus kept for language refresh and state updates
        private readonly List<(ToolStripMenuItem Item, string Key)> _menuKeys = new();
        private ToolStripMenuItem? _miWrap, _miWhitespace, _miEol, _miFoldMargin, _miLineNumber,
            _miPanelFolder, _miPanelRecent, _miFullscreen;
        private ToolStripMenuItem? _miEncodingAnsi, _miEncodingUtf8, _miEncodingUtf8Bom,
            _miEncodingUtf16Le, _miEncodingUtf16Be, _miEolCrLf, _miEolLf, _miEolCr;
        private ToolStripMenuItem? _miRecentPlaceholder;
        private readonly List<ToolStripMenuItem> _languageItems = new();

        private FindReplaceDialog? _findDialog;
        private bool _fullscreen;
        private FormWindowState _preFullscreenState = FormWindowState.Normal;
        private Rectangle _preFullscreenBounds;

        public MainForm(string[] args)
        {
            _settings = AppSettings.Load();
            L.Initialize(_settings);
            ThemeManager.Apply(_settings.Theme);
            _recent = new RecentFilesStore(_settings.MaxRecentFiles);

            Text = "NotepadPro";
            AllowDrop = true;
            KeyPreview = false;
            StartPosition = FormStartPosition.Manual;

            BuildLayout();
            BuildMenus();
            BuildToolbar();
            BuildStatusBar();

            L.LanguageChanged += RefreshTexts;
            ThemeManager.ThemeChanged += OnThemeChanged;
            ThemeManager.ApplyChrome(this);
            RefreshTexts();

            Load += (_, _) =>
            {
                // SplitterDistance must be set after the handle/layout exists.
                try { _split.SplitterDistance = 240; } catch { /* window too narrow */ }
                RestoreWindowPlacement();
                ThemeManager.Apply(_settings.Theme); // re-apply for DWM on final handle
                RestoreSession();
                OpenStartupArguments(args);
                UpdateMenuChecks();
                UpdateUIState();
            };

            DragEnter += OnDragEnter;
            DragDrop += OnDragDrop;
            FormClosing += OnMainFormClosing;
        }

        // ------------------------------------------------------------------
        // Layout
        // ------------------------------------------------------------------

        private void BuildLayout()
        {
            _menu.RenderMode = ToolStripRenderMode.Professional;
            _toolbar.GripStyle = ToolStripGripStyle.Hidden;
            _toolbar.RenderMode = ToolStripRenderMode.Professional;
            _toolbar.Padding = new Padding(4, 1, 4, 1);

            _tabs.Dock = DockStyle.Top;
            _tabs.TabActivated += i => ActivateDocument(i);
            _tabs.TabCloseRequested += i => CloseDocument(i, confirmSave: true);
            _tabs.TabRightClicked += i => ShowTabContextMenu(i);
            _tabs.TabMiddleClicked += item =>
            {
                var idx = _documents.FindIndex(d => ReferenceEquals(d, item.Tag));
                if (idx >= 0) CloseDocument(idx, confirmSave: true);
            };

            _editorHost.Dock = DockStyle.Fill;
            _editorHost.Padding = new Padding(0);
            _editorHost.BackColor = ThemeManager.Current.EditBg;

            _sidebar = new SidebarPanel { Dock = DockStyle.Fill, Visible = false };
            _sidebar.OpenFileRequested += p => OpenFile(p);
            _sidebar.FolderOpenRequested += ChooseWorkspaceFolder;

            _split.Dock = DockStyle.Fill;
            _split.Panel1.Controls.Add(_sidebar);
            _split.Panel2.Controls.Add(_editorHost);
            _split.Panel1Collapsed = true;
            _split.FixedPanel = FixedPanel.Panel1;

            _status.SizingGrip = true;
            _status.Padding = new Padding(6, 1, 6, 1);

            var rightArea = new ToolStripContainer { Dock = DockStyle.Fill };
            rightArea.ContentPanel.Controls.Add(_split);

            // Fill order: status bottom, split fills the rest, tabs above split,
            // toolbar and menu on top.
            Controls.Add(rightArea);
            Controls.Add(_status);
            Controls.Add(_tabs);
            Controls.Add(_toolbar);
            Controls.Add(_menu);
            MainMenuStrip = _menu;
        }

        // ------------------------------------------------------------------
        // Menus
        // ------------------------------------------------------------------

        private ToolStripMenuItem AddMenuItem(ToolStripDropDownItem parent, string key,
            EventHandler? onClick = null, Keys shortcut = Keys.None)
        {
            var item = new ToolStripMenuItem(L.Get(key));
            if (shortcut != Keys.None) item.ShortcutKeys = shortcut;
            if (onClick != null) item.Click += onClick;
            parent.DropDownItems.Add(item);
            _menuKeys.Add((item, key));
            return item;
        }

        private void BuildMenus()
        {
            var mFile = AddTopMenu("menu.file");
            AddMenuItem(mFile, "menu.file.new", (_, _) => NewDocument(), Keys.Control | Keys.N);
            AddMenuItem(mFile, "menu.file.open", (_, _) => ShowOpenDialog(), Keys.Control | Keys.O);
            AddMenuItem(mFile, "menu.file.openfolder", (_, _) => ChooseWorkspaceFolder());
            mFile.DropDownItems.Add(new ToolStripSeparator());
            AddMenuItem(mFile, "menu.file.reload", (_, _) => ReloadDocument(), Keys.None);
            AddMenuItem(mFile, "menu.file.save", (_, _) => SaveDocument(), Keys.Control | Keys.S);
            AddMenuItem(mFile, "menu.file.saveas", (_, _) => SaveDocumentAs(), Keys.Control | Keys.Alt | Keys.S);
            AddMenuItem(mFile, "menu.file.saveall", (_, _) => SaveAllDocuments(), Keys.Control | Keys.Shift | Keys.S);
            mFile.DropDownItems.Add(new ToolStripSeparator());
            AddMenuItem(mFile, "menu.file.close", (_, _) => CloseDocument(_activeIndex, true), Keys.Control | Keys.W);
            AddMenuItem(mFile, "menu.file.closeall", (_, _) => CloseAllDocuments(), Keys.Control | Keys.Shift | Keys.W);
            mFile.DropDownItems.Add(new ToolStripSeparator());
            var miRecent = AddMenuItem(mFile, "menu.file.recent");
            _miRecentPlaceholder = new ToolStripMenuItem(L.Get("menu.file.recent.empty")) { Enabled = false };
            _miRecentPlaceholder.Font = new Font(_miRecentPlaceholder.Font, FontStyle.Italic);
            miRecent.DropDownItems.Add(_miRecentPlaceholder);
            var miRecentClear = AddMenuItem(miRecent, "menu.file.recent.clear", (_, _) => { _recent.Clear(); });
            _ = miRecentClear;
            mFile.DropDownItems.Add(new ToolStripSeparator());
            AddMenuItem(mFile, "menu.file.exit", (_, _) => Close());

            var mEdit = AddTopMenu("menu.edit");
            AddMenuItem(mEdit, "menu.edit.undo", (_, _) => ActiveEditor?.Undo(), Keys.Control | Keys.Z);
            AddMenuItem(mEdit, "menu.edit.redo", (_, _) => ActiveEditor?.Redo(), Keys.Control | Keys.Y);
            mEdit.DropDownItems.Add(new ToolStripSeparator());
            AddMenuItem(mEdit, "menu.edit.cut", (_, _) => ActiveEditor?.Cut(), Keys.Control | Keys.X);
            AddMenuItem(mEdit, "menu.edit.copy", (_, _) => ActiveEditor?.Copy(), Keys.Control | Keys.C);
            AddMenuItem(mEdit, "menu.edit.paste", (_, _) => ActiveEditor?.Paste(), Keys.Control | Keys.V);
            AddMenuItem(mEdit, "menu.edit.delete", (_, _) => ActiveEditor?.Clear(), Keys.Delete);
            AddMenuItem(mEdit, "menu.edit.selectall", (_, _) => ActiveEditor?.SelectAll(), Keys.Control | Keys.A);
            mEdit.DropDownItems.Add(new ToolStripSeparator());
            AddMenuItem(mEdit, "menu.edit.uppercase", (_, _) => { if (ActiveEditor != null) LineOps.ToUpper(ActiveEditor); }, Keys.Control | Keys.Shift | Keys.U);
            AddMenuItem(mEdit, "menu.edit.lowercase", (_, _) => { if (ActiveEditor != null) LineOps.ToLower(ActiveEditor); }, Keys.Control | Keys.U);
            mEdit.DropDownItems.Add(new ToolStripSeparator());
            AddMenuItem(mEdit, "menu.edit.duplicateline", (_, _) => { if (ActiveEditor != null) LineOps.DuplicateLine(ActiveEditor); }, Keys.Control | Keys.D);
            AddMenuItem(mEdit, "menu.edit.deleteline", (_, _) => { if (ActiveEditor != null) LineOps.DeleteLine(ActiveEditor); }, Keys.Control | Keys.L);
            AddMenuItem(mEdit, "menu.edit.moveup", (_, _) => { if (ActiveEditor != null) LineOps.MoveLinesUp(ActiveEditor); }, Keys.Control | Keys.Shift | Keys.Up);
            AddMenuItem(mEdit, "menu.edit.movedown", (_, _) => { if (ActiveEditor != null) LineOps.MoveLinesDown(ActiveEditor); }, Keys.Control | Keys.Shift | Keys.Down);
            mEdit.DropDownItems.Add(new ToolStripSeparator());
            AddMenuItem(mEdit, "menu.edit.commentline", (_, _) => ToggleLineComment(), Keys.Control | Keys.Q);
            AddMenuItem(mEdit, "menu.edit.blockcomment", (_, _) => ToggleBlockComment(), Keys.Control | Keys.Shift | Keys.Q);
            mEdit.DropDownItems.Add(new ToolStripSeparator());
            AddMenuItem(mEdit, "menu.edit.tabs2spaces", (_, _) => ConvertIndent(true));
            AddMenuItem(mEdit, "menu.edit.spaces2tabs", (_, _) => ConvertIndent(false));
            AddMenuItem(mEdit, "menu.edit.trimtrailing", (_, _) => { if (ActiveEditor != null) LineOps.TrimTrailingWhitespace(ActiveEditor); });

            var mSearch = AddTopMenu("menu.search");
            AddMenuItem(mSearch, "menu.search.find", (_, _) => ShowFind(false), Keys.Control | Keys.F);
            AddMenuItem(mSearch, "menu.search.findnext", (_, _) => FindNextFromMenu(down: true), Keys.F3);
            AddMenuItem(mSearch, "menu.search.findprev", (_, _) => FindNextFromMenu(down: false), Keys.Shift | Keys.F3);
            AddMenuItem(mSearch, "menu.search.findword", (_, _) => ShowFind(true), Keys.Control | Keys.F3);
            AddMenuItem(mSearch, "menu.search.replace", (_, _) => ShowReplace(), Keys.Control | Keys.H);
            AddMenuItem(mSearch, "menu.search.goto", (_, _) => ShowGoToLine(), Keys.Control | Keys.G);

            var mView = AddTopMenu("menu.view");
            _miWrap = AddMenuItem(mView, "menu.view.wrap", (_, _) => ToggleWrap());
            _miWhitespace = AddMenuItem(mView, "menu.view.whitespace", (_, _) => ToggleWhitespace());
            _miEol = AddMenuItem(mView, "menu.view.eol", (_, _) => ToggleEol());
            _miFoldMargin = AddMenuItem(mView, "menu.view.foldmargin", (_, _) => ToggleFoldMargin());
            _miLineNumber = AddMenuItem(mView, "menu.view.linenumber", (_, _) => ToggleLineNumber());
            mView.DropDownItems.Add(new ToolStripSeparator());
            AddMenuItem(mView, "menu.view.zoomin", (_, _) => ActiveEditor?.ZoomIn(), Keys.Control | Keys.Add);
            AddMenuItem(mView, "menu.view.zoomout", (_, _) => ActiveEditor?.ZoomOut(), Keys.Control | Keys.Subtract);
            AddMenuItem(mView, "menu.view.zoomreset", (_, _) => ResetZoom(), Keys.Control | Keys.Divide);
            mView.DropDownItems.Add(new ToolStripSeparator());
            _miPanelFolder = AddMenuItem(mView, "menu.view.panelfolder", (_, _) => ToggleSidebar(showFolder: true));
            _miPanelRecent = AddMenuItem(mView, "menu.view.panelrecent", (_, _) => ToggleSidebar(showFolder: false));
            mView.DropDownItems.Add(new ToolStripSeparator());
            _miFullscreen = AddMenuItem(mView, "menu.view.fullscreen", (_, _) => ToggleFullscreen(), Keys.F11);

            var mEncoding = AddTopMenu("menu.encoding");
            var miConvert = AddMenuItem(mEncoding, "menu.encoding.convertto");
            _miEncodingAnsi = AddMenuItem(miConvert, "encoding.ansi.name", (_, _) => ConvertEncoding(TextEncoding.Ansi));
            _miEncodingUtf8 = AddMenuItem(miConvert, "encoding.utf8.name", (_, _) => ConvertEncoding(TextEncoding.Utf8));
            _miEncodingUtf8Bom = AddMenuItem(miConvert, "encoding.utf8bom.name", (_, _) => ConvertEncoding(TextEncoding.Utf8Bom));
            _miEncodingUtf16Le = AddMenuItem(miConvert, "encoding.utf16lebom.name", (_, _) => ConvertEncoding(TextEncoding.Utf16LeBom));
            _miEncodingUtf16Be = AddMenuItem(miConvert, "encoding.utf16bebom.name", (_, _) => ConvertEncoding(TextEncoding.Utf16BeBom));
            var miEolMenu = AddMenuItem(mEncoding, "menu.encoding.eol");
            _miEolCrLf = AddMenuItem(miEolMenu, "menu.encoding.eol.crlf", (_, _) => ConvertEol(TextEol.CrLf));
            _miEolLf = AddMenuItem(miEolMenu, "menu.encoding.eol.lf", (_, _) => ConvertEol(TextEol.Lf));
            _miEolCr = AddMenuItem(miEolMenu, "menu.encoding.eol.cr", (_, _) => ConvertEol(TextEol.Cr));

            var mLanguage = AddTopMenu("menu.language");
            foreach (var lang in LanguageCatalog.All)
            {
                var mi = new ToolStripMenuItem(lang.DisplayName) { Tag = lang.Id };
                mi.Click += (_, _) => SetDocumentLanguage(lang);
                mLanguage.DropDownItems.Add(mi);
                _languageItems.Add(mi);
                _menuKeys.Add((mi, "!lang"));
            }

            var mTools = AddTopMenu("menu.tools");
            AddMenuItem(mTools, "menu.tools.options", (_, _) => ShowSettings());

            var mHelp = AddTopMenu("menu.help");
            AddMenuItem(mHelp, "menu.help.about", (_, _) => new AboutDialog().ShowDialog(this));

            _recent.Changed += RefreshRecentMenu;
            RefreshRecentMenu();
        }

        private ToolStripMenuItem AddTopMenu(string key)
        {
            var item = new ToolStripMenuItem(L.Get(key));
            _menu.Items.Add(item);
            _menuKeys.Add((item, key));
            return item;
        }

        private void RefreshRecentMenu()
        {
            if (_miRecentPlaceholder?.Owner == null) return;
            var recentMenu = _miRecentPlaceholder.Owner;
            recentMenu.Items.Clear();
            var files = _recent.Files;
            if (files.Count == 0)
            {
                var empty = new ToolStripMenuItem(L.Get("menu.file.recent.empty")) { Enabled = false };
                empty.Font = new Font(empty.Font, FontStyle.Italic);
                recentMenu.Items.Add(empty);
                return;
            }
            foreach (var path in files)
            {
                var item = new ToolStripMenuItem(path);
                item.Click += (_, _) => OpenFile(path);
                recentMenu.Items.Add(item);
            }
            recentMenu.Items.Add(new ToolStripSeparator());
            var clear = new ToolStripMenuItem(L.Get("menu.file.recent.clear"));
            clear.Click += (_, _) => _recent.Clear();
            recentMenu.Items.Add(clear);
        }

        // ------------------------------------------------------------------
        // Toolbar
        // ------------------------------------------------------------------

        private void BuildToolbar()
        {
            ToolStripButton Glyph(string glyph, string tooltipKey, EventHandler onClick, string? text = null)
            {
                var btn = new ToolStripButton
                {
                    Text = text ?? char.ConvertFromUtf32(Convert.ToInt32(glyph, 16)),
                    ToolTipText = L.Get(tooltipKey),
                    DisplayStyle = ToolStripItemDisplayStyle.Text,
                    Font = new Font("Segoe MDL2 Assets", 10f),
                    AutoSize = false,
                    Width = 30,
                    Height = 26,
                    Margin = new Padding(1, 2, 1, 2),
                    Tag = tooltipKey
                };
                btn.Click += onClick;
                return btn;
            }

            _toolbar.Items.Add(Glyph("E7C3", "toolbar.new", (_, _) => NewDocument()));
            _toolbar.Items.Add(Glyph("E8E5", "toolbar.open", (_, _) => ShowOpenDialog()));
            _toolbar.Items.Add(Glyph("E74E", "toolbar.save", (_, _) => SaveDocument()));
            _toolbar.Items.Add(new ToolStripSeparator());
            _toolbar.Items.Add(Glyph("E7A7", "toolbar.undo", (_, _) => ActiveEditor?.Undo()));
            _toolbar.Items.Add(Glyph("E7A6", "toolbar.redo", (_, _) => ActiveEditor?.Redo()));
            _toolbar.Items.Add(new ToolStripSeparator());
            _toolbar.Items.Add(Glyph("E721", "toolbar.find", (_, _) => ShowFind(false)));
            _toolbar.Items.Add(Glyph("E7B3", "toolbar.replace", (_, _) => ShowReplace()));
            _toolbar.Items.Add(new ToolStripSeparator());
            _toolbar.Items.Add(Glyph("E71F", "toolbar.zoomout", (_, _) => ActiveEditor?.ZoomOut()));
            _toolbar.Items.Add(Glyph("E8A3", "toolbar.zoomin", (_, _) => ActiveEditor?.ZoomIn()));
            _toolbar.Items.Add(Glyph("E751", "toolbar.wrap", (_, _) => ToggleWrap()));
            _toolbar.Items.Add(new ToolStripSeparator());
            _toolbar.Items.Add(Glyph("E706", "toolbar.themes", (_, _) =>
            {
                _settings.Theme = ThemeManager.IsDark ? AppTheme.Light : AppTheme.Dark;
                ApplyThemeSetting();
            }));
            _toolbar.Items.Add(Glyph("E8B7", "toolbar.folderpanel", (_, _) => ToggleSidebar(true)));
            _toolbar.Items.Add(Glyph("E81C", "toolbar.recentpanel", (_, _) => ToggleSidebar(false)));
        }

        private void BuildStatusBar()
        {
            _status.Items.AddRange(new ToolStripItem[]
            {
                _stPath, new ToolStripSeparator(),
                _stLength, new ToolStripSeparator(),
                _stLines, new ToolStripSeparator(),
                _stPos, new ToolStripSeparator(),
                _stSel, new ToolStripSeparator(),
                _stEol, new ToolStripSeparator(),
                _stEnc, new ToolStripSeparator(),
                _stIns, new ToolStripSeparator(),
                _stZoom
            });
        }

        // ------------------------------------------------------------------
        // Document management
        // ------------------------------------------------------------------

        private Scintilla? ActiveEditor => _activeIndex >= 0 && _activeIndex < _documents.Count
            ? _documents[_activeIndex].Editor
            : null;

        private DocumentBuffer? ActiveDocument =>
            _activeIndex >= 0 && _activeIndex < _documents.Count ? _documents[_activeIndex] : null;

        private void NewDocument()
        {
            CreateDocument();
            ActivateDocument(_documents.Count - 1);
            UpdateUIState();
        }

        /// <summary>Applies per-document editing behavior (multi-selection, indentation).</summary>
        private void ApplyEditorBehavior(Scintilla sci)
        {
            sci.MultipleSelection = true;
            sci.AdditionalSelectionTyping = true;
            sci.UseTabs = !_settings.UseSpaces;
            sci.TabWidth = _settings.TabWidth;
            sci.IndentWidth = _settings.TabWidth;
            sci.TabIndents = true;
            sci.BackspaceUnindents = _settings.BackspaceUnindents;
        }

        private DocumentBuffer CreateDocument()
        {
            var doc = new DocumentBuffer();
            doc.Language = LanguageCatalog.PlainText;
            doc.DirtyChanged += () =>
            {
                int idx = _documents.IndexOf(doc);
                if (idx >= 0) _tabs.Invalidate();
                UpdateTitle();
                UpdateUIState();
            };
            WireEditor(doc);
            ThemeManager.ApplyToEditor(doc.Editor, doc.Language, _settings);
            ApplyEditorBehavior(doc.Editor);
            _documents.Add(doc);
            _editorHost.Controls.Add(doc.Editor);
            doc.Editor.Visible = false;

            var tab = new TabItem
            {
                Tag = doc,
                TitleProvider = () => doc.Title,
                DirtyProvider = () => doc.IsDirty
            };
            _tabs.Items.Add(tab);
            return doc;
        }

        private void WireEditor(DocumentBuffer doc)
        {
            var sci = doc.Editor;
            sci.Dock = DockStyle.Fill;
            sci.UpdateUI += (_, _) => OnEditorUpdateUI(doc);
            sci.ZoomChanged += (_, _) => UpdateStatusZoom();
            sci.CharAdded += (_, e) => OnCharAdded(doc, e);
            sci.MarginClick += (_, e) =>
            {
                if (e.Margin == 2)
                {
                    var line = sci.Lines[sci.LineFromPosition(e.Position)];
                    line.ToggleFold();
                }
            };
        }

        private void ActivateDocument(int index)
        {
            if (index < 0 || index >= _documents.Count) return;
            if (_activeIndex >= 0 && _activeIndex < _documents.Count)
                _documents[_activeIndex].Editor.Visible = false;

            _activeIndex = index;
            var doc = _documents[index];
            doc.Editor.Visible = true;
            _tabs.ActiveIndex = index;
            doc.Editor.Focus();
            UpdateTitle();
            UpdateUIState();
            UpdateEncodingChecks();
            UpdateLanguageMenu();
        }

        public void OpenFile(string path)
        {
            path = Path.GetFullPath(path);
            var existing = _documents.FindIndex(d =>
                d.FilePath != null && string.Equals(d.FilePath, path, StringComparison.OrdinalIgnoreCase));
            if (existing >= 0)
            {
                ActivateDocument(existing);
                return;
            }

            var doc = CreateDocument();
            try
            {
                doc.LoadFrom(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                _documents.Remove(doc);
                _editorHost.Controls.Remove(doc.Editor);
                doc.Dispose();
                _tabs.Items.RemoveAt(_tabs.Items.Count - 1);
                MessageBox.Show(
                    string.Format(L.Get("msg.open.failed.body"), path, ex.Message),
                    L.Get("msg.open.failed.title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            doc.Language = LanguageDef.Match(path) ?? LanguageCatalog.PlainText;
            doc.ApplyLanguage();
            ThemeManager.ApplyToEditor(doc.Editor, doc.Language, _settings);
            ApplyEditorBehavior(doc.Editor);
            _recent.Add(path);
            ActivateDocument(_documents.Count - 1);
        }

        public void OpenFilesFromArguments(string[] paths)
        {
            foreach (var p in paths)
            {
                if (File.Exists(p)) OpenFile(p);
            }
        }

        private void OpenStartupArguments(string[] args)
        {
            bool opened = false;
            foreach (var a in args)
            {
                if (File.Exists(a))
                {
                    OpenFile(a);
                    opened = true;
                }
            }
            if (!opened && _documents.Count == 0) NewDocument();
        }

        private void RestoreSession()
        {
            if (!_settings.RememberSession) return;
            var session = SessionManager.Load();
            foreach (var entry in session.Files)
            {
                OpenFile(entry.Path);
                if (_documents.Count > 0)
                {
                    var doc = _documents[^1];
                    try
                    {
                        var line = Math.Max(0, Math.Min(entry.Line, doc.Editor.Lines.Count - 1));
                        doc.Editor.CurrentPosition = Math.Min(
                            doc.Editor.Lines[line].Position + entry.Column,
                            doc.Editor.Lines[line].EndPosition);
                        doc.Editor.FirstVisibleLine = Math.Max(0, Math.Min(
                            entry.FirstVisibleLine, Math.Max(0, doc.Editor.Lines.Count - 1)));
                        doc.Editor.SetSel(doc.Editor.CurrentPosition, doc.Editor.CurrentPosition);
                    }
                    catch
                    {
                        // Malformed cursor restore is non-fatal.
                    }
                }
            }
            if (session.Files.Count > 0)
            {
                int idx = Math.Clamp(session.ActiveIndex, 0, _documents.Count - 1);
                ActivateDocument(idx);
            }
        }

        private bool ConfirmSave(DocumentBuffer doc)
        {
            if (!doc.IsDirty) return true;
            var choice = MessageBox.Show(
                string.Format(L.Get("msg.save.prompt"), doc.Title),
                L.Get("msg.closemany.title"),
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            return choice switch
            {
                DialogResult.Yes => SaveDocument(doc),
                DialogResult.No => true,
                _ => false
            };
        }

        private void CloseDocument(int index, bool confirmSave)
        {
            if (index < 0 || index >= _documents.Count) return;
            var doc = _documents[index];
            if (confirmSave && !ConfirmSave(doc)) return;

            _documents.RemoveAt(index);
            _editorHost.Controls.Remove(doc.Editor);
            _tabs.Items.RemoveAt(index);
            doc.Dispose();

            if (_documents.Count == 0)
            {
                _activeIndex = -1;
                NewDocument();
            }
            else
            {
                ActivateDocument(Math.Clamp(index, 0, _documents.Count - 1));
            }
            UpdateUIState();
        }

        private void CloseAllDocuments()
        {
            int dirty = _documents.Count(d => d.IsDirty);
            if (dirty > 1)
            {
                var choice = MessageBox.Show(
                    string.Format(L.Get("msg.closemany.body"), dirty),
                    L.Get("msg.closemany.title"),
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (choice == DialogResult.Cancel) return;
                if (choice == DialogResult.Yes && !SaveAllDocuments()) return;
            }
            while (_documents.Count > 0)
            {
                var doc = _documents[^1];
                if (doc.IsDirty && dirty <= 1 && !ConfirmSave(doc)) return;
                CloseDocument(_documents.Count - 1, confirmSave: false);
            }
        }

        // ------------------------------------------------------------------
        // File actions
        // ------------------------------------------------------------------

        private void ShowOpenDialog()
        {
            string? initialDir = _settings.LastFolder;
            if (initialDir == null)
            {
                var first = _recent.Files.FirstOrDefault();
                initialDir = first != null
                    ? Path.GetDirectoryName(first)
                    : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            using var dlg = new OpenFileDialog
            {
                Title = L.Get("menu.file.open"),
                Filter = BuildFileFilter(),
                InitialDirectory = initialDir,
                Multiselect = true
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            if (dlg.FileName.Length > 0)
                _settings.LastFolder = Path.GetDirectoryName(dlg.FileName);
            foreach (var file in dlg.FileNames) OpenFile(file);
        }

        private string BuildFileFilter()
        {
            var all = string.Join(";",
                LanguageCatalog.All.SelectMany(l => l.Extensions).Distinct().Select(e => "*" + e));
            return $"{L.Get("file.filter.languages")}|{all}|{L.Get("file.filter.allfiles")}|*.*";
        }

        private bool SaveDocument(DocumentBuffer? doc = null)
        {
            doc ??= ActiveDocument;
            if (doc == null) return true;
            if (doc.IsUntitled) return SaveDocumentAs(doc);
            return doc.SaveTo(doc.FilePath!);
        }

        private bool SaveDocumentAs(DocumentBuffer? doc = null)
        {
            doc ??= ActiveDocument;
            if (doc == null) return true;

            using var dlg = new SaveFileDialog
            {
                Title = L.Get("menu.file.saveas"),
                FileName = doc.IsUntitled ? doc.Title : Path.GetFileName(doc.FilePath),
                InitialDirectory = _settings.LastFolder
                    ?? (doc.FilePath != null ? Path.GetDirectoryName(doc.FilePath) : null)
                    ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Filter = BuildFileFilter()
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return false;
            _settings.LastFolder = Path.GetDirectoryName(dlg.FileName);

            // Switch the language to match the new extension.
            doc.Language = LanguageDef.Match(dlg.FileName) ?? LanguageCatalog.PlainText;
            doc.ApplyLanguage();
            ThemeManager.ApplyToEditor(doc.Editor, doc.Language, _settings);

            bool ok = doc.SaveTo(dlg.FileName);
            if (ok)
            {
                _recent.Add(dlg.FileName);
                _tabs.Invalidate();
                UpdateTitle();
                UpdateLanguageMenu();
                UpdateEncodingChecks();
            }
            return ok;
        }

        private bool SaveAllDocuments()
        {
            bool allSaved = true;
            foreach (var doc in _documents.ToList())
            {
                if (doc.IsDirty && !SaveDocument(doc)) allSaved = false;
            }
            UpdateUIState();
            return allSaved;
        }

        private void ReloadDocument()
        {
            var doc = ActiveDocument;
            if (doc?.FilePath == null) return;
            if (doc.IsDirty)
            {
                if (MessageBox.Show(L.Get("msg.reload.body"), L.Get("msg.reload.title"),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            }
            int caret = doc.Editor.CurrentPosition;
            doc.LoadFrom(doc.FilePath);
            doc.Editor.CurrentPosition = Math.Min(caret, doc.Editor.TextLength);
            UpdateUIState();
        }

        // ------------------------------------------------------------------
        // Encoding / EOL actions
        // ------------------------------------------------------------------

        private void ConvertEncoding(TextEncoding encoding)
        {
            var doc = ActiveDocument;
            if (doc == null) return;
            doc.Encoding = encoding;
            UpdateEncodingChecks();
            UpdateUIState();
        }

        private void ConvertEol(TextEol eol)
        {
            var doc = ActiveDocument;
            if (doc == null) return;
            var sci = doc.Editor;
            string text = sci.Text;
            if (EncodingDetector.DetectEol(text) == eol && sci.EolMode == ToEolMode(eol))
            {
                UpdateEncodingChecks();
                return;
            }
            // Single selection edit keeps undo history intact.
            sci.SetSel(0, sci.TextLength);
            sci.ReplaceSelection(EncodingDetector.NormalizeEol(text, eol));
            sci.EolMode = ToEolMode(eol);
            sci.CurrentPosition = 0;
            UpdateEncodingChecks();
            UpdateUIState();
        }

        private static Eol ToEolMode(TextEol eol) => eol switch
        {
            TextEol.Lf => Eol.Lf,
            TextEol.Cr => Eol.Cr,
            _ => Eol.CrLf
        };

        private static TextEol FromEolMode(Eol mode) => mode switch
        {
            Eol.Lf => TextEol.Lf,
            Eol.Cr => TextEol.Cr,
            _ => TextEol.CrLf
        };

        // ------------------------------------------------------------------
        // Edit actions
        // ------------------------------------------------------------------

        private void ToggleLineComment()
        {
            var doc = ActiveDocument;
            if (doc == null) return;
            if (doc.Language.LineComment is { } prefix)
                LineOps.ToggleLineComment(doc.Editor, prefix);
        }

        private void ToggleBlockComment()
        {
            var doc = ActiveDocument;
            if (doc == null) return;
            if (doc.Language.BlockComment is { } bc)
                LineOps.ToggleBlockComment(doc.Editor, bc.Open, bc.Close);
        }

        private void ConvertIndent(bool toSpaces)
        {
            var doc = ActiveDocument;
            if (doc == null) return;
            LineOps.ConvertIndentation(doc.Editor, toSpaces, _settings.TabWidth);
        }

        private void OnCharAdded(DocumentBuffer doc, CharAddedEventArgs e)
        {
            // Simple smart indent: copy the previous line's indentation and
            // extend it after a block opener. CRLF mode fires CharAdded for
            // both '\r' and '\n' - only react once per EOL insertion.
            char c = (char)e.Char;
            var eolMode = doc.Editor.EolMode;
            if (eolMode == Eol.Cr)
            {
                if (c != '\r') return;
            }
            else if (c != '\n')
            {
                return;
            }

            var sci = doc.Editor;
            int line = sci.CurrentLine;
            if (line <= 0) return;
            string prevText = sci.Lines[line - 1].Text.TrimEnd('\r', '\n');
            var indent = prevText.TakeWhile(ch => ch == '\t' || ch == ' ').ToArray();

            string trimmed = prevText.TrimEnd();
            string unit = _settings.UseSpaces ? new string(' ', _settings.TabWidth) : "\t";
            if (trimmed.EndsWith('{') || trimmed.EndsWith('[') || trimmed.EndsWith('(') || trimmed.EndsWith(':'))
                indent = indent.Concat(unit).ToArray();

            if (indent.Length > 0)
                sci.AddText(new string(indent));
        }

        // ------------------------------------------------------------------
        // View actions
        // ------------------------------------------------------------------

        private void ToggleWrap()
        {
            _settings.WrapLines = !_settings.WrapLines;
            ApplyViewSettings();
        }

        private void ToggleWhitespace()
        {
            _settings.ShowWhitespace = !_settings.ShowWhitespace;
            ApplyViewSettings();
        }

        private void ToggleEol()
        {
            _settings.ShowEol = !_settings.ShowEol;
            ApplyViewSettings();
        }

        private void ToggleFoldMargin()
        {
            _settings.ShowFoldMargin = !_settings.ShowFoldMargin;
            ApplyViewSettings();
        }

        private void ToggleLineNumber()
        {
            _settings.ShowFoldMargin = _settings.ShowFoldMargin; // line numbers stay always-on in v1
            UpdateMenuChecks();
        }

        private void ApplyViewSettings()
        {
            foreach (var doc in _documents)
                ThemeManager.ApplyToEditor(doc.Editor, doc.Language, _settings);
            UpdateMenuChecks();
            _settings.Save();
        }

        private void ResetZoom()
        {
            if (ActiveEditor == null) return;
            ActiveEditor.Zoom = 0;
            UpdateStatusZoom();
        }

        private void ToggleSidebar(bool showFolder)
        {
            bool target = showFolder;
            if (_split.Panel1Collapsed == false && _sidebar.FolderPanel.Visible == showFolder)
            {
                _split.Panel1Collapsed = true; // toggling the visible panel closes the sidebar
            }
            else
            {
                _split.Panel1Collapsed = false;
                _sidebar.Visible = true;
                if (showFolder) _sidebar.ShowPanelFolder(); else _sidebar.ShowPanelRecent();
            }
            UpdateMenuChecks();
        }

        private void ToggleFullscreen()
        {
            if (!_fullscreen)
            {
                _preFullscreenState = WindowState;
                _preFullscreenBounds = Bounds;
                _fullscreen = true;
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Maximized;
            }
            else
            {
                _fullscreen = false;
                FormBorderStyle = FormBorderStyle.Sizable;
                WindowState = _preFullscreenState;
                Bounds = _preFullscreenBounds;
            }
            if (_miFullscreen != null) _miFullscreen.Checked = _fullscreen;
        }

        private void ChooseWorkspaceFolder()
        {
            using var dlg = new FolderBrowserDialog
            {
                ShowNewFolderButton = false,
                Description = L.Get("folder.open")
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            _sidebar.Visible = true;
            _sidebar.FolderPanel.OpenFolder(dlg.SelectedPath);
            _split.Panel1Collapsed = false;
            _sidebar.ShowPanelFolder();
            UpdateMenuChecks();
        }

        // ------------------------------------------------------------------
        // Search UI
        // ------------------------------------------------------------------

        private void ShowFind(bool fromSelection)
        {
            EnsureFindDialog();
            if (fromSelection) _findDialog!.ShowFindSelection(this);
            else _findDialog!.ShowFind(this);
        }

        private void ShowReplace()
        {
            EnsureFindDialog();
            _findDialog!.ShowReplace(this);
        }

        private void EnsureFindDialog()
        {
            if (_findDialog == null || _findDialog.IsDisposed)
            {
                _findDialog = new FindReplaceDialog(() => ActiveDocument);
            }
            _findDialog.UpdateTexts();
        }

        private void FindNextFromMenu(bool down)
        {
            var doc = ActiveDocument;
            if (doc == null) return;
            var sci = doc.Editor;
            string needle = sci.SelectedText.Contains('\n') || sci.SelectedText.Contains('\r')
                ? string.Empty
                : sci.SelectedText;

            var options = needle.Length > 0
                ? new FindOptions
                {
                    Needle = needle,
                    WrapAround = true,
                    Down = down,
                    Regex = false,
                    MatchCase = false,
                    WholeWord = false
                }
                : _findDialog?.CurrentOptions(down) ?? new FindOptions { Down = down, WrapAround = true };

            if (options.Needle.Length == 0)
            {
                ShowFind(false);
                return;
            }
            var hit = FindReplaceEngine.FindNext(sci, options, 0, sci.SelectionStart, sci.SelectionEnd);
            if (hit.Found) FindReplaceEngine.SelectMatch(sci, hit);
        }

        private void ShowGoToLine()
        {
            var doc = ActiveDocument;
            if (doc == null) return;
            using var dlg = new GoToLineDialog(doc.Editor.Lines.Count, doc.Editor.CurrentLine + 1);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            int line = Math.Clamp(dlg.TargetLine, 1, doc.Editor.Lines.Count) - 1;
            var pos = doc.Editor.Lines[line].Position;
            doc.Editor.SetSel(pos, pos);
            doc.Editor.ScrollCaret();
            doc.Editor.Focus();
        }

        // ------------------------------------------------------------------
        // Settings / language
        // ------------------------------------------------------------------

        private void ShowSettings()
        {
            using var dlg = new SettingsDialog(_settings);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            bool themeChanged = dlg.Theme != _settings.Theme;
            bool langChanged = dlg.Language != _settings.Language;
            _settings.FontName = dlg.FontName;
            _settings.FontSize = dlg.FontSize;
            _settings.TabWidth = dlg.TabWidth;
            _settings.UseSpaces = dlg.UseSpaces;
            _settings.Theme = dlg.Theme;
            _settings.Language = dlg.Language;
            _settings.RememberSession = dlg.RememberSession;
            _recent.Max = _settings.MaxRecentFiles;
            _settings.Save();

            if (themeChanged) ApplyThemeSetting();
            if (langChanged) L.SetCulture(dlg.Language == UiLanguage.ZhCn ? "zh-CN" : "en-US");

            foreach (var doc in _documents)
                ThemeManager.ApplyToEditor(doc.Editor, doc.Language, _settings);
            _sidebar.RefreshRecent();
            UpdateUIState();
        }

        private void ApplyThemeSetting()
        {
            ThemeManager.Apply(_settings.Theme);
            ThemeManager.ApplyChrome(this);
            foreach (var doc in _documents)
                ThemeManager.ApplyToEditor(doc.Editor, doc.Language, _settings);
            OnThemeChanged();
            _settings.Save();
        }

        private void SetDocumentLanguage(LanguageDef lang)
        {
            var doc = ActiveDocument;
            if (doc == null) return;
            doc.Language = lang;
            doc.ApplyLanguage();
            ThemeManager.ApplyToEditor(doc.Editor, lang, _settings);
            UpdateLanguageMenu();
            UpdateUIState();
        }

        // ------------------------------------------------------------------
        // Tab context menu
        // ------------------------------------------------------------------

        private void ShowTabContextMenu(int index)
        {
            if (index < 0 || index >= _documents.Count) return;
            ActivateDocument(index);
            var doc = _documents[index];

            var menu = new ContextMenuStrip();
            void Add(string key, EventHandler onClick)
            {
                var mi = new ToolStripMenuItem(L.Get(key));
                mi.Click += onClick;
                menu.Items.Add(mi);
            }

            Add("tabctx.close", (_, _) => CloseDocument(index, true));
            Add("tabctx.closeothers", (_, _) =>
            {
                for (int i = _documents.Count - 1; i >= 0; i--)
                {
                    if (i != index) CloseDocument(i, true);
                }
            });
            Add("tabctx.closeall", (_, _) => CloseAllDocuments());
            menu.Items.Add(new ToolStripSeparator());
            Add("tabctx.copypath", (_, _) => { try { Clipboard.SetText(doc.DisplayPath); } catch { } });
            Add("tabctx.explorer", (_, _) =>
            {
                if (doc.FilePath != null)
                {
                    try { System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{doc.FilePath}\""); }
                    catch { }
                }
            });
            menu.Show(_tabs, _tabs.PointToClient(Cursor.Position));
        }

        // ------------------------------------------------------------------
        // Status bar / state refresh
        // ------------------------------------------------------------------

        private void OnEditorUpdateUI(DocumentBuffer doc)
        {
            if (!ReferenceEquals(doc, ActiveDocument)) return;

            // Grow the line-number gutter when the document gains digits.
            int needed = ThemeManager.LineNumberWidth(doc.Editor);
            if (Math.Abs(doc.Editor.Margins[0].Width - needed) > 2)
                doc.Editor.Margins[0].Width = needed;

            // Brace match highlight
            var sci = doc.Editor;
            sci.BraceHighlight(-1, -1);
            int caret = sci.CurrentPosition;
            if (caret > 0 && IsBrace((char)sci.GetCharAt(caret - 1)))
            {
                int match = sci.BraceMatch(caret - 1);
                if (match >= 0) sci.BraceHighlight(caret - 1, match);
            }
            else if (IsBrace((char)sci.GetCharAt(caret)))
            {
                int match = sci.BraceMatch(caret);
                if (match >= 0) sci.BraceHighlight(caret, match);
            }

            UpdateTitle();
            RefreshStatusValues();
        }

        private static bool IsBrace(char c) =>
            c is '(' or ')' or '[' or ']' or '{' or '}';

        private void UpdateTitle()
        {
            var doc = ActiveDocument;
            Text = doc == null
                ? "NotepadPro"
                : $"{(doc.IsDirty ? "*" : "")}{doc.Title} - NotepadPro";
        }

        private void UpdateUIState()
        {
            UpdateTitle();
            RefreshStatusValues();
            UpdateStatusZoom();
            UpdateEncodingChecks();
            UpdateLanguageMenu();
            UpdateMenuChecks();
        }

        private void RefreshStatusValues()
        {
            var doc = ActiveDocument;
            if (doc == null)
            {
                _stPath.Text = L.Get("status.ready");
                _stLength.Text = _stLines.Text = _stPos.Text = _stSel.Text = string.Empty;
                _stEol.Text = _stEnc.Text = _stIns.Text = string.Empty;
                return;
            }
            var sci = doc.Editor;
            _stPath.Text = doc.DisplayPath;
            _stLength.Text = $"{L.Get("status.length")} {sci.TextLength}";
            _stLines.Text = $"{L.Get("status.lines")} {sci.Lines.Count}";
            int line = sci.CurrentLine + 1;
            int col = sci.CurrentPosition - sci.Lines[sci.CurrentLine].Position + 1;
            _stPos.Text = $"{L.Get("status.ln")} {line} {L.Get("status.col")} {col}";
            int selLen = Math.Abs(sci.SelectionEnd - sci.SelectionStart);
            _stSel.Text = selLen > 0 ? $"{L.Get("status.sel")} {selLen}" : string.Empty;
            _stEol.Text = EncodingDetector.EolDisplayName(FromEolMode(sci.EolMode));
            _stEnc.Text = EncodingDetector.DisplayName(doc.Encoding);
            _stIns.Text = sci.Overtype ? L.Get("status.ovr") : L.Get("status.ins");
        }

        private void UpdateStatusZoom()
        {
            var doc = ActiveDocument;
            int zoom = doc?.Editor.Zoom ?? 0;
            _stZoom.Text = string.Format(L.Get("status.zoom"), 100 + zoom * 10);
        }

        private void UpdateEncodingChecks()
        {
            var doc = ActiveDocument;
            var enc = doc?.Encoding ?? TextEncoding.Utf8;
            string key = EncodingDetector.MenuKey(enc);
            if (_miEncodingAnsi != null) _miEncodingAnsi.Checked = key == "ansi";
            if (_miEncodingUtf8 != null) _miEncodingUtf8.Checked = key == "utf8";
            if (_miEncodingUtf8Bom != null) _miEncodingUtf8Bom.Checked = key == "utf8bom";
            if (_miEncodingUtf16Le != null) _miEncodingUtf16Le.Checked = key == "utf16le";
            if (_miEncodingUtf16Be != null) _miEncodingUtf16Be.Checked = key == "utf16be";

            var eol = doc != null ? FromEolMode(doc.Editor.EolMode) : TextEol.CrLf;
            if (_miEolCrLf != null) _miEolCrLf.Checked = eol == TextEol.CrLf;
            if (_miEolLf != null) _miEolLf.Checked = eol == TextEol.Lf;
            if (_miEolCr != null) _miEolCr.Checked = eol == TextEol.Cr;
        }

        private void UpdateLanguageMenu()
        {
            var doc = ActiveDocument;
            foreach (var mi in _languageItems)
                mi.Checked = doc != null && (string)mi.Tag == doc.Language.Id;
        }

        private void UpdateMenuChecks()
        {
            if (_miWrap != null) _miWrap.Checked = _settings.WrapLines;
            if (_miWhitespace != null) _miWhitespace.Checked = _settings.ShowWhitespace;
            if (_miEol != null) _miEol.Checked = _settings.ShowEol;
            if (_miFoldMargin != null) _miFoldMargin.Checked = _settings.ShowFoldMargin;
            if (_miPanelFolder != null) _miPanelFolder.Checked =
                !_split.Panel1Collapsed && _sidebar.FolderPanel.Visible;
            if (_miPanelRecent != null) _miPanelRecent.Checked =
                !_split.Panel1Collapsed && _sidebar.RecentPanel.Visible;
        }

        // ------------------------------------------------------------------
        // Language refresh / theme refresh
        // ------------------------------------------------------------------

        private void RefreshTexts()
        {
            foreach (var (item, key) in _menuKeys)
            {
                if (key == "!lang") continue; // language names are proper nouns
                item.Text = L.Get(key);
            }
            foreach (var item in _toolbar.Items.OfType<ToolStripButton>())
            {
                if (item.Tag is string key) item.ToolTipText = L.Get(key);
            }
            _findDialog?.UpdateTexts();
            _sidebar.RefreshTexts();
            UpdateUIState();
            Invalidate();
        }

        private void OnThemeChanged()
        {
            ThemeManager.ApplyChrome(this);
            _editorHost.BackColor = ThemeManager.Current.EditBg;
            _tabs.Invalidate();
        }

        // ------------------------------------------------------------------
        // Drag & drop, keys, lifecycle
        // ------------------------------------------------------------------

        private void OnDragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Copy;
        }

        private void OnDragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files) return;
            foreach (var f in files)
            {
                if (File.Exists(f)) OpenFile(f);
                else if (Directory.Exists(f)) _sidebar.FolderPanel.OpenFolder(f);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Ctrl+Tab / Ctrl+Shift+Tab cycle documents.
            if (keyData == (Keys.Control | Keys.Tab) && _documents.Count > 1)
            {
                ActivateDocument((_activeIndex + 1) % _documents.Count);
                return true;
            }
            if (keyData == (Keys.Control | Keys.Shift | Keys.Tab) && _documents.Count > 1)
            {
                ActivateDocument((_activeIndex - 1 + _documents.Count) % _documents.Count);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void RestoreWindowPlacement()
        {
            if (_settings.WindowMaximized)
            {
                WindowState = FormWindowState.Maximized;
                return;
            }
            if (_settings.WindowLeft >= -10000 && _settings.WindowTop >= -10000)
                Location = new Point(_settings.WindowLeft, _settings.WindowTop);
            Size = new Size(
                Math.Max(640, _settings.WindowWidth),
                Math.Max(480, _settings.WindowHeight));
        }

        private void SaveWindowPlacement()
        {
            if (WindowState == FormWindowState.Normal)
            {
                _settings.WindowLeft = Location.X;
                _settings.WindowTop = Location.Y;
                _settings.WindowWidth = Size.Width;
                _settings.WindowHeight = Size.Height;
                _settings.WindowMaximized = false;
            }
            else
            {
                _settings.WindowMaximized = WindowState == FormWindowState.Maximized;
            }
        }

        private void OnMainFormClosing(object? sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                int dirty = _documents.Count(d => d.IsDirty);
                if (dirty > 0)
                {
                    var choice = MessageBox.Show(
                        string.Format(L.Get("msg.closemany.body"), dirty),
                        L.Get("msg.closemany.title"),
                        MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (choice == DialogResult.Cancel) { e.Cancel = true; return; }
                    if (choice == DialogResult.Yes && !SaveAllDocuments()) { e.Cancel = true; return; }
                }
            }

            if (_settings.RememberSession)
                SessionManager.Save(_documents, _activeIndex);
            else
                SessionManager.Clear();

            SaveWindowPlacement();
            _settings.Save();
        }
    }
}
