using System.Reflection;

namespace NotepadPro.UI
{
    /// <summary>Simple about box.</summary>
    public class AboutDialog : Form
    {
        public AboutDialog()
        {
            Text = L.Get("menu.help.about");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(460, 250);
            Font = new Font("Segoe UI", 9f);

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var title = new Label
            {
                Text = "NotepadPro",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 14)
            };
            var versionLabel = new Label
            {
                Text = string.Format(L.Get("about.version"),
                    $"{version?.Major ?? 1}.{version?.Minor ?? 0}.{version?.Build ?? 0}"),
                AutoSize = true,
                ForeColor = ThemeManager.Current.SidebarHeader,
                Location = new Point(22, 58)
            };
            var tagline = new Label
            {
                Text = L.Get("app.tagline"),
                AutoSize = true,
                ForeColor = ThemeManager.Current.SidebarHeader,
                Location = new Point(22, 78)
            };
            var body = new Label
            {
                Text = L.Get("about.body"),
                AutoSize = false,
                Size = new Size(420, 92),
                Location = new Point(20, 106)
            };
            var license = new Label
            {
                Text = L.Get("app.license"),
                AutoSize = true,
                ForeColor = ThemeManager.Current.SidebarHeader,
                Location = new Point(22, 200)
            };
            var ok = new Button
            {
                Text = L.Get("about.ok"),
                DialogResult = DialogResult.OK,
                Size = new Size(88, 27),
                Location = new Point(352, 216)
            };
            AcceptButton = ok;
            Controls.AddRange(new Control[] { title, versionLabel, tagline, body, license, ok });
        }
    }
}
