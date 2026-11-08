namespace NotepadPro.UI
{
    /// <summary>Modal options dialog (font, indentation, theme, language, session).</summary>
    public class SettingsDialog : Form
    {
        public string FontName { get; private set; } = "Consolas";
        public int FontSize { get; private set; } = 11;
        public int TabWidth { get; private set; } = 4;
        public bool UseSpaces { get; private set; } = true;
        public AppTheme Theme { get; private set; } = AppTheme.Light;
        public UiLanguage Language { get; private set; } = UiLanguage.Auto;
        public bool RememberSession { get; private set; } = true;

        private readonly ComboBox _fontCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        private readonly NumericUpDown _size = new() { Minimum = 6, Maximum = 48, Width = 60 };
        private readonly NumericUpDown _tabWidth = new() { Minimum = 1, Maximum = 16, Width = 60 };
        private readonly CheckBox _useSpaces = new() { AutoSize = true };
        private readonly ComboBox _theme = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        private readonly ComboBox _language = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        private readonly CheckBox _remember = new() { AutoSize = true };

        public SettingsDialog(AppSettings settings)
        {
            Text = L.Get("settings.title");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(420, 250);
            Font = new Font("Segoe UI", 9f);

            foreach (var family in FontFamily.Families)
            {
                if (family.IsStyleAvailable(FontStyle.Regular)) _fontCombo.Items.Add(family.Name);
            }
            _fontCombo.SelectedItem = settings.FontName;
            if (_fontCombo.SelectedIndex < 0) _fontCombo.Text = settings.FontName;
            _size.Value = Math.Clamp(settings.FontSize, 6, 48);
            _tabWidth.Value = Math.Clamp(settings.TabWidth, 1, 16);
            _useSpaces.Checked = settings.UseSpaces;
            _theme.Items.AddRange(new object[] { L.Get("settings.theme.light"), L.Get("settings.theme.dark") });
            _theme.SelectedIndex = settings.Theme == AppTheme.Dark ? 1 : 0;
            _language.Items.AddRange(new object[]
            {
                L.Get("settings.language.auto"), "中文（简体）", "English"
            });
            _language.SelectedIndex = (int)settings.Language;
            _remember.Checked = settings.RememberSession;

            int y = 18;
            void Row(Control label, Control input)
            {
                label.Location = new Point(18, y + 3);
                input.Location = new Point(150, y);
                Controls.Add(label);
                Controls.Add(input);
                y += 32;
            }

            Row(new Label { Text = L.Get("settings.font"), AutoSize = true }, _fontCombo);
            Row(new Label { Text = L.Get("settings.fontsize"), AutoSize = true }, _size);
            Row(new Label { Text = L.Get("settings.tabwidth"), AutoSize = true }, _tabWidth);
            _useSpaces.Location = new Point(150, y);
            Controls.Add(_useSpaces);
            y += 32;
            Row(new Label { Text = L.Get("settings.theme"), AutoSize = true }, _theme);
            Row(new Label { Text = L.Get("settings.language"), AutoSize = true }, _language);
            _remember.Location = new Point(150, y);
            Controls.Add(_remember);
            y += 36;

            var ok = new Button
            {
                Text = L.Get("dialog.ok"),
                DialogResult = DialogResult.OK,
                Size = new Size(88, 26),
                Location = new Point(ClientSize.Width - 190, y)
            };
            var cancel = new Button
            {
                Text = L.Get("dialog.cancel"),
                DialogResult = DialogResult.Cancel,
                Size = new Size(88, 26),
                Location = new Point(ClientSize.Width - 96, y)
            };
            Controls.Add(ok);
            Controls.Add(cancel);
            AcceptButton = ok;
            CancelButton = cancel;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && DialogResult != DialogResult.OK)
            {
                base.OnFormClosing(e);
                return;
            }
            if (DialogResult == DialogResult.OK)
            {
                FontName = _fontCombo.SelectedItem as string ?? _fontCombo.Text;
                FontSize = (int)_size.Value;
                TabWidth = (int)_tabWidth.Value;
                UseSpaces = _useSpaces.Checked;
                Theme = _theme.SelectedIndex == 1 ? AppTheme.Dark : AppTheme.Light;
                Language = (UiLanguage)Math.Max(0, _language.SelectedIndex);
                RememberSession = _remember.Checked;
            }
            base.OnFormClosing(e);
        }
    }
}
