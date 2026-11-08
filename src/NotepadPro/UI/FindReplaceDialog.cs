namespace NotepadPro.UI
{
    /// <summary>
    /// Modeless Find/Replace dialog bound to the active document. A single
    /// shared instance keeps find history and options across modes.
    /// </summary>
    public class FindReplaceDialog : Form
    {
        private static FindOptions _options = new();

        private readonly Func<DocumentBuffer?> _activeDocument;
        private readonly ComboBox _txtFind = new() { Width = 260, DropDownStyle = ComboBoxStyle.DropDown };
        private readonly ComboBox _txtReplace = new() { Width = 260, DropDownStyle = ComboBoxStyle.DropDown };
        private readonly CheckBox _chkCase = new() { AutoSize = true };
        private readonly CheckBox _chkWord = new() { AutoSize = true };
        private readonly CheckBox _chkRegex = new() { AutoSize = true };
        private readonly CheckBox _chkWrap = new() { AutoSize = true };
        private readonly CheckBox _chkSelection = new() { AutoSize = true };
        private readonly RadioButton _rbDown = new() { AutoSize = true };
        private readonly RadioButton _rbUp = new() { AutoSize = true };
        private readonly Button _btnNext = new() { AutoSize = true };
        private readonly Button _btnCount = new() { AutoSize = true };
        private readonly Button _btnReplace = new() { AutoSize = true };
        private readonly Button _btnReplaceAll = new() { AutoSize = true };
        private readonly Button _btnClose = new() { AutoSize = true };
        private readonly Label _status = new() { AutoSize = true, Padding = new Padding(2) };
        private readonly TableLayoutPanel _layout;
        private bool _replaceMode;

        public FindReplaceDialog(Func<DocumentBuffer?> activeDocument)
        {
            _activeDocument = activeDocument;

            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(430, 330);
            Font = new Font("Segoe UI", 9f);

            _layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12, 10, 12, 8), ColumnCount = 2 };
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            BuildRows();
            _rbDown.Checked = true; // default search direction: down
            Controls.Add(_layout);

            AcceptButton = _btnNext;
            CancelButton = _btnClose;
            _btnClose.Click += (_, _) => Hide();
            _btnNext.Click += (_, _) => FindNextAction();
            _btnCount.Click += (_, _) => CountAction();
            _btnReplace.Click += (_, _) => ReplaceAction();
            _btnReplaceAll.Click += (_, _) => ReplaceAllAction();
            _chkRegex.CheckedChanged += (_, _) => UpdateHint();

            Shown += (_, _) => { _txtFind.Focus(); _txtFind.SelectAll(); };
            ThemeManager.ThemeChanged += ApplyTheme;
            ApplyTheme();
        }

        private void BuildRows()
        {
            int row = 0;
            void Add(Control c, bool fill = false)
            {
                _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                _layout.Controls.Add(c, fill ? 1 : 0, row);
                if (fill) _layout.SetColumnSpan(c, 1);
                row++;
            }

            var lblFind = new Label { Text = L.Get("find.findwhat"), AutoSize = true, Anchor = AnchorStyles.Left };
            _layout.Controls.Add(lblFind, 0, row);
            _layout.Controls.Add(_txtFind, 1, row);
            _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            row++;

            var lblReplace = new Label { Text = L.Get("find.replacewith"), AutoSize = true, Anchor = AnchorStyles.Left };
            _lblReplace = lblReplace;
            _layout.Controls.Add(lblReplace, 0, row);
            _layout.Controls.Add(_txtReplace, 1, row);
            _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            row++;

            var optionsFlow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, WrapContents = true };
            optionsFlow.Controls.AddRange(new Control[] { _chkCase, _chkWord, _chkRegex, _chkWrap, _chkSelection });
            Add(optionsFlow, fill: true);

            var dirFlow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            dirFlow.Controls.AddRange(new Control[] { _rbDown, _rbUp });
            Add(dirFlow, fill: true);

            var buttonFlow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            buttonFlow.Controls.AddRange(new Control[] { _btnNext, _btnCount, _btnReplace, _btnReplaceAll, _btnClose });
            Add(buttonFlow, fill: true);

            Add(_status, fill: true);
        }

        private Label? _lblReplace;

        /// <summary>Shows the dialog in find-only mode.</summary>
        public void ShowFind(IWin32Window owner)
        {
            _replaceMode = false;
            SetMode();
            ShowMode(owner, find: true);
        }

        /// <summary>Shows the dialog in replace mode.</summary>
        public void ShowReplace(IWin32Window owner)
        {
            _replaceMode = true;
            SetMode();
            ShowMode(owner, find: true);
        }

        /// <summary>Shows the dialog pre-filled from the current selection (quick find).</summary>
        public void ShowFindSelection(IWin32Window owner)
        {
            _replaceMode = false;
            SetMode();
            ShowMode(owner, find: false);
        }

        private void ShowMode(IWin32Window owner, bool find)
        {
            var doc = _activeDocument();
            if (doc != null && find && doc.Editor.SelectedText.Length > 0)
            {
                var sel = doc.Editor.SelectedText;
                if (!sel.Contains('\n') && !sel.Contains('\r'))
                    _txtFind.Text = sel;
                doc.Editor.TargetFromSelection();
            }
            Text = L.Get(_replaceMode ? "find.title.replace" : "find.title.find");
            UpdateTexts();
            if (!Visible) Show(owner);
            else { BringToFront(); Activate(); }
            _txtFind.Focus();
            _txtFind.SelectAll();
        }

        private void SetMode()
        {
            _lblReplace!.Visible = _replaceMode;
            _txtReplace.Visible = _replaceMode;
            _btnReplace.Visible = _replaceMode;
            _btnReplaceAll.Visible = _replaceMode;
            if (_replaceMode)
            {
                _btnReplace.Text = L.Get("find.replace");
                _btnReplaceAll.Text = L.Get("find.replaceall");
            }
        }

        public void UpdateTexts()
        {
            Text = L.Get(_replaceMode ? "find.title.replace" : "find.title.find");
            _chkCase.Text = L.Get("find.matchcase");
            _chkWord.Text = L.Get("find.wholeword");
            _chkRegex.Text = L.Get("find.regex");
            _chkWrap.Text = L.Get("find.wrap");
            _chkSelection.Text = L.Get("find.inselection");
            _rbDown.Text = L.Get("find.down");
            _rbUp.Text = L.Get("find.up");
            _btnNext.Text = L.Get("find.next");
            _btnCount.Text = L.Get("find.count");
            _btnClose.Text = L.Get("find.close");
            _btnReplace.Text = L.Get("find.replace");
            _btnReplaceAll.Text = L.Get("find.replaceall");
            UpdateHint();
            Invalidate();
        }

        private void UpdateHint()
        {
            _status.Text = _chkRegex.Checked ? L.Get("find.regex.hint") : string.Empty;
        }

        private FindOptions CollectOptions()
        {
            _options.Needle = _txtFind.Text;
            _options.MatchCase = _chkCase.Checked;
            _options.WholeWord = _chkWord.Checked;
            _options.Regex = _chkRegex.Checked;
            _options.WrapAround = _chkWrap.Checked;
            _options.Down = _rbDown.Checked;
            _options.InSelection = _chkSelection.Checked;
            if (_txtFind.Text.Length > 0 && !_txtFind.Items.Contains(_txtFind.Text))
                _txtFind.Items.Insert(0, _txtFind.Text);
            return _options;
        }

        private DocumentBuffer? Doc => _activeDocument();

        /// <summary>
        /// The shared find options, for menu-driven Find Next (F3) when the
        /// dialog is closed. <paramref name="down"/> overrides the direction.
        /// </summary>
        public FindOptions CurrentOptions(bool down)
        {
            if (_txtFind.Text.Length > 0) CollectOptions();
            return new FindOptions
            {
                Needle = _options.Needle,
                MatchCase = _options.MatchCase,
                WholeWord = _options.WholeWord,
                Regex = _options.Regex,
                WrapAround = _options.WrapAround,
                Down = down,
                InSelection = false
            };
        }

        private void FindNextAction()
        {
            var doc = Doc;
            if (doc == null) return;
            var opt = CollectOptions();
            var sci = doc.Editor;
            var hit = FindReplaceEngine.FindNext(sci, opt, 0, sci.SelectionStart, sci.SelectionEnd);
            if (!FindReplaceEngine.SelectMatch(sci, hit))
            {
                _status.Text = string.Format(L.Get("find.nomore"), _txtFind.Text);
            }
            else
            {
                _status.Text = string.Empty;
            }
        }

        private void CountAction()
        {
            var doc = Doc;
            if (doc == null) return;
            var count = FindReplaceEngine.CountMatches(doc.Editor, CollectOptions());
            _status.Text = string.Format(L.Get("find.counted"), count);
        }

        private void ReplaceAction()
        {
            var doc = Doc;
            if (doc == null) return;
            var opt = CollectOptions();
            var replaced = FindReplaceEngine.ReplaceCurrent(doc.Editor, opt, _txtReplace.Text);
            if (replaced)
            {
                var sci = doc.Editor;
                var hit = FindReplaceEngine.FindNext(sci, opt, 0, sci.SelectionStart, sci.SelectionEnd);
                if (hit.Found) FindReplaceEngine.SelectMatch(sci, hit);
                else _status.Text = string.Format(L.Get("find.nomore"), _txtFind.Text);
            }
        }

        private void ReplaceAllAction()
        {
            var doc = Doc;
            if (doc == null) return;
            var count = FindReplaceEngine.ReplaceAll(doc.Editor, CollectOptions(), _txtReplace.Text);
            _status.Text = string.Format(L.Get("find.replaced"), count);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Keep editing keys working inside the combo boxes.
            if (keyData == Keys.Escape)
            {
                Hide();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ApplyTheme()
        {
            if (!IsHandleCreated) return;
            var t = ThemeManager.Current;
            BackColor = t.Window;
            ForeColor = t.WindowText;
            foreach (Control c in _layout.Controls)
            {
                if (c is FlowLayoutPanel or TableLayoutPanel)
                {
                    c.BackColor = t.Window;
                    foreach (Control inner in c.Controls)
                    {
                        inner.BackColor = t.Window;
                        inner.ForeColor = t.WindowText;
                    }
                }
                else
                {
                    c.BackColor = t.Window;
                    c.ForeColor = t.WindowText;
                }
            }
            _txtFind.BackColor = t.TabActiveBg;
            _txtFind.ForeColor = t.WindowText;
            _txtReplace.BackColor = t.TabActiveBg;
            _txtReplace.ForeColor = t.WindowText;
            _lblReplace!.BackColor = t.Window;
            _lblReplace.ForeColor = t.WindowText;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ThemeManager.ThemeChanged -= ApplyTheme;
            base.Dispose(disposing);
        }
    }
}
