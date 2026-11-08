namespace NotepadPro.UI
{
    /// <summary>Modal "go to line" dialog.</summary>
    public class GoToLineDialog : Form
    {
        private readonly NumericUpDown _num = new()
        {
            Minimum = 1,
            Maximum = int.MaxValue,
            Width = 160,
            ThousandsSeparator = false
        };

        /// <summary>The chosen 1-based line number after OK.</summary>
        public int TargetLine => (int)_num.Value;

        public GoToLineDialog(int maxLine, int currentLine)
        {
            Text = L.Get("goto.title");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(300, 110);
            Font = new Font("Segoe UI", 9f);
            _num.Maximum = Math.Max(1, maxLine);
            _num.Value = Math.Clamp(currentLine, 1, Math.Max(1, maxLine));

            var label = new Label
            {
                Text = L.Get("goto.label"),
                AutoSize = true,
                Location = new Point(14, 16)
            };
            _num.Location = new Point(18, 42);

            var ok = new Button
            {
                Text = L.Get("dialog.ok"),
                DialogResult = DialogResult.OK,
                Location = new Point(120, 76),
                Size = new Size(80, 26)
            };
            var cancel = new Button
            {
                Text = L.Get("dialog.cancel"),
                DialogResult = DialogResult.Cancel,
                Location = new Point(208, 76),
                Size = new Size(80, 26)
            };
            AcceptButton = ok;
            CancelButton = cancel;
            Controls.AddRange(new Control[] { label, _num, ok, cancel });
        }
    }
}
