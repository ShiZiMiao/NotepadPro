using NotepadPro.UI;

namespace NotepadPro
{
    /// <summary>
    /// Sidebar container with a small switcher between the folder workspace
    /// panel and the recent files panel.
    /// </summary>
    public class SidebarPanel : Panel
    {
        private readonly Panel _header;
        private readonly Button _btnFolder;
        private readonly Button _btnRecent;
        public readonly FolderWorkspacePanel FolderPanel;
        public readonly RecentFilesPanel RecentPanel;

        public event Action<string>? OpenFileRequested;
        public event Action? FolderOpenRequested;

        public SidebarPanel()
        {
            Width = 240;

            _header = new Panel { Dock = DockStyle.Top, Height = 30 };
            _btnFolder = new Button
            {
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Left,
                Width = 120,
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = "folder"
            };
            _btnRecent = new Button
            {
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = "recent"
            };
            _btnFolder.FlatAppearance.BorderSize = 0;
            _btnRecent.FlatAppearance.BorderSize = 0;
            _btnFolder.Click += (_, _) => ShowPanel(true);
            _btnRecent.Click += (_, _) => ShowPanel(false);
            _header.Controls.Add(_btnRecent);
            _header.Controls.Add(_btnFolder);

            FolderPanel = new FolderWorkspacePanel { Dock = DockStyle.Fill };
            RecentPanel = new RecentFilesPanel { Dock = DockStyle.Fill };
            FolderPanel.OpenFileRequested += p => OpenFileRequested?.Invoke(p);
            FolderPanel.FolderOpenRequested += () => FolderOpenRequested?.Invoke();
            RecentPanel.OpenFileRequested += p => OpenFileRequested?.Invoke(p);

            Controls.Add(FolderPanel);
            Controls.Add(RecentPanel);
            Controls.Add(_header);
            ShowPanel(true);

        }

        private void ShowPanel(bool folder)
        {
            FolderPanel.Visible = folder;
            RecentPanel.Visible = !folder;
            ApplyPanelColors(folder);
        }

        private void ApplyPanelColors(bool folder)
        {
            _btnFolder.BackColor = folder ? ThemeManager.Current.TabActiveBg : ThemeManager.Current.SidebarBg;
            _btnRecent.BackColor = folder ? ThemeManager.Current.SidebarBg : ThemeManager.Current.TabActiveBg;
            _btnFolder.ForeColor = ThemeManager.Current.SidebarText;
            _btnRecent.ForeColor = ThemeManager.Current.SidebarText;
        }

        /// <summary>Shows the folder workspace panel.</summary>
        public void ShowPanelFolder() => ShowPanel(true);

        /// <summary>Shows the recent files panel.</summary>
        public void ShowPanelRecent() => ShowPanel(false);

        /// <summary>Re-applies localized strings to switcher buttons and children.</summary>
        public void RefreshTexts()
        {
            _btnFolder.Text = L.Get("panel.folder");
            _btnRecent.Text = L.Get("panel.recent");
            FolderPanel.RefreshTexts();
            RecentPanel.RefreshTexts();
        }

        /// <summary>Shows the folder panel and raises the open-folder request.</summary>
        public void TriggerOpenFolder() => FolderPanel.TriggerOpenFolder();

        public void RefreshRecent() => RecentPanel.Reload();

        private void RefreshTheme()
        {
            void Apply()
            {
                BackColor = ThemeManager.Current.SidebarBg;
                _header.BackColor = ThemeManager.Current.SidebarBg;
                ApplyPanelColors(FolderPanel.Visible);
            }
            if (IsHandleCreated) BeginInvoke(new Action(Apply));
            else Apply();
        }
    }

    /// <summary>Browse a folder tree and open files from it.</summary>
    public class FolderWorkspacePanel : ThemedPanel
    {
        private readonly ToolStrip _toolbar;
        private readonly TreeView _tree;
        private readonly Label _empty;
        private readonly ImageList _icons;
        private FileSystemWatcher? _watcher;
        private System.Windows.Forms.Timer? _watchDebounce;
        private string? _rootPath;

        public event Action<string>? OpenFileRequested;
        public event Action? FolderOpenRequested;

        public FolderWorkspacePanel()
        {
            _icons = new ImageList { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
            _icons.Images.Add("folder", MakeFolderIcon(false));
            _icons.Images.Add("folderopen", MakeFolderIcon(true));
            _icons.Images.Add("file", MakeFileIcon());

            _toolbar = new ToolStrip
            {
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                ImageScalingSize = new Size(16, 16)
            };
            var btnOpen = new ToolStripButton { Text = L.Get("folder.open"), DisplayStyle = ToolStripItemDisplayStyle.Text };
            var btnRefresh = new ToolStripButton { Text = L.Get("folder.refresh"), DisplayStyle = ToolStripItemDisplayStyle.Text };
            var btnCollapse = new ToolStripButton { Text = L.Get("folder.collapse"), DisplayStyle = ToolStripItemDisplayStyle.Text };
            btnOpen.Click += (_, _) => TriggerOpenFolder();
            btnRefresh.Click += (_, _) => Reload();
            btnCollapse.Click += (_, _) => _tree.CollapseAll();
            _toolbar.Items.AddRange(new ToolStripItem[] { btnOpen, new ToolStripSeparator(), btnRefresh, btnCollapse });

            _tree = new TreeView
            {
                Dock = DockStyle.Fill,
                ImageList = _icons,
                ShowLines = false,
                HideSelection = false,
                BorderStyle = BorderStyle.None
            };
            _tree.BeforeExpand += BeforeExpand;
            _tree.NodeMouseDoubleClick += NodeDoubleClick;
            _tree.KeyDown += TreeKeyDown;

            var menu = new ContextMenuStrip();
            var miOpen = new ToolStripMenuItem(L.Get("folder.ctx.open"));
            var miExplorer = new ToolStripMenuItem(L.Get("folder.ctx.explorer"));
            var miCopy = new ToolStripMenuItem(L.Get("folder.ctx.copypath"));
            miOpen.Click += (_, _) => { if (_tree.SelectedNode?.Tag is string p) Activate(p); };
            miExplorer.Click += (_, _) => Reveal(_tree.SelectedNode?.Tag as string);
            miCopy.Click += (_, _) =>
            {
                if (_tree.SelectedNode?.Tag is string p) try { Clipboard.SetText(p); } catch { }
            };
            menu.Items.AddRange(new ToolStripItem[] { miOpen, miExplorer, miCopy });
            menu.Opening += (_, e) =>
            {
                bool hasSel = _tree.SelectedNode?.Tag is string;
                miOpen.Enabled = hasSel;
                miExplorer.Enabled = hasSel;
                miCopy.Enabled = hasSel;
            };
            _tree.ContextMenuStrip = menu;

            _empty = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = L.Get("folder.empty")
            };

            Controls.Add(_tree);
            Controls.Add(_empty);
            Controls.Add(_toolbar);

            _watchDebounce = new System.Windows.Forms.Timer { Interval = 600 };
            _watchDebounce.Tick += (_, _) => { _watchDebounce.Stop(); Reload(); };

        }

        /// <summary>Raises the open-folder request so MainForm shows the folder browser.</summary>
        public void TriggerOpenFolder() => FolderOpenRequested?.Invoke();

        /// <summary>Re-applies localized strings.</summary>
        public void RefreshTexts()
        {
            _toolbar.Items[0].Text = L.Get("folder.open");
            _toolbar.Items[2].Text = L.Get("folder.refresh");
            _toolbar.Items[3].Text = L.Get("folder.collapse");
            _empty.Text = L.Get("folder.empty");
            Invalidate();
        }

        public string? RootPath => _rootPath;

        public void OpenFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                MessageBox.Show(L.Get("msg.folderinvalid"), L.Get("msg.open.failed.title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _rootPath = Path.GetFullPath(path);
            SetupWatcher();
            Reload();
        }

        public void Reload()
        {
            _tree.BeginUpdate();
            try
            {
                var expanded = CollectExpandedPaths();
                _tree.Nodes.Clear();
                _empty.Visible = _rootPath == null;
                _tree.Visible = _rootPath != null;
                if (_rootPath == null) return;

                var root = MakeNode(new DirectoryInfo(_rootPath));
                if (root != null)
                {
                    _tree.Nodes.Add(root);
                    Populate(root);
                    RestoreExpanded(root, expanded);
                }
            }
            finally
            {
                _tree.EndUpdate();
            }
        }

        private void SetupWatcher()
        {
            _watcher?.Dispose();
            if (_rootPath == null) return;
            try
            {
                _watcher = new FileSystemWatcher(_rootPath)
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite
                };
                _watcher.Created += (_, _) => _watchDebounce?.Start();
                _watcher.Deleted += (_, _) => _watchDebounce?.Start();
                _watcher.Renamed += (_, _) => _watchDebounce?.Start();
                _watcher.Changed += (_, _) => _watchDebounce?.Start();
                _watcher.EnableRaisingEvents = true;
            }
            catch
            {
                _watcher = null; // Some folders deny watching; refresh stays manual.
            }
        }

        private HashSet<string> CollectExpandedPaths()
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Walk(_tree.Nodes);
            void Walk(TreeNodeCollection nodes)
            {
                foreach (TreeNode n in nodes)
                {
                    if (n.IsExpanded && n.Tag is string p) result.Add(p);
                    Walk(n.Nodes);
                }
            }
            return result;
        }

        private void RestoreExpanded(TreeNode root, HashSet<string> expanded)
        {
            Walk(root);
            void Walk(TreeNode node)
            {
                if (node.Tag is string p && expanded.Contains(p))
                {
                    node.Expand();
                }
                foreach (TreeNode child in node.Nodes) Walk(child);
            }
        }

        private void BeforeExpand(object? sender, TreeViewCancelEventArgs e)
        {
            if (e.Node?.Tag is DirectoryInfo) Populate(e.Node);
        }

        private void Populate(TreeNode node)
        {
            if (node.Nodes.Count > 0 && node.Nodes[0].Name != "__placeholder__") return;
            node.Nodes.Clear();
            if (node.Tag is not DirectoryInfo dir) return;

            try
            {
                foreach (var child in dir.EnumerateDirectories())
                {
                    if ((child.Attributes & FileAttributes.Hidden) != 0) continue;
                    var childNode = MakeNode(child);
                    if (childNode != null) node.Nodes.Add(childNode);
                }
                foreach (var file in dir.EnumerateFiles())
                {
                    if ((file.Attributes & FileAttributes.Hidden) != 0) continue;
                    var fileNode = new TreeNode(file.Name)
                    {
                        Tag = file.FullName,
                        ImageKey = "file",
                        SelectedImageKey = "file"
                    };
                    node.Nodes.Add(fileNode);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Leave the node empty when the OS denies access.
            }
            catch (IOException)
            {
            }
        }

        private TreeNode? MakeNode(FileSystemInfo info)
        {
            var node = new TreeNode(info.Name)
            {
                Tag = info,
                ImageKey = "folder",
                SelectedImageKey = "folderopen"
            };
            // Placeholder so the expand arrow shows even before loading.
            node.Nodes.Add(new TreeNode { Name = "__placeholder__" });
            return node;
        }

        private void NodeDoubleClick(object? sender, TreeNodeMouseClickEventArgs e) => Activate(e.Node?.Tag);

        private void TreeKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) Activate(_tree.SelectedNode?.Tag);
        }

        private void Activate(object? tag)
        {
            if (tag is string path && File.Exists(path))
                OpenFileRequested?.Invoke(path);
        }

        private static void Reveal(string? path)
        {
            if (path == null) return;
            try
            {
                if (File.Exists(path))
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\"");
                else if (Directory.Exists(path))
                    System.Diagnostics.Process.Start("explorer.exe", $"\"{path}\"");
            }
            catch
            {
                // Explorer launch is best-effort.
            }
        }

        protected override void OnThemeApplied()
        {
            _tree.BackColor = ThemeManager.Current.SidebarBg;
            _tree.ForeColor = ThemeManager.Current.SidebarText;
            _empty.BackColor = ThemeManager.Current.SidebarBg;
            _empty.ForeColor = ThemeManager.Current.SidebarHeader;
            _toolbar.BackColor = ThemeManager.Current.SidebarBg;
            _toolbar.ForeColor = ThemeManager.Current.SidebarText;
            foreach (ToolStripItem item in _toolbar.Items)
                item.ForeColor = ThemeManager.Current.SidebarText;
        }

        private static Bitmap MakeFolderIcon(bool open)
        {
            var bmp = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.Transparent);
            var tabColor = Color.FromArgb(255, 190, 150, 60);
            var bodyColor = Color.FromArgb(255, 230, 190, 90);
            int y = open ? 5 : 4;
            g.FillRectangle(new SolidBrush(tabColor), 2, y, 5, 3);
            g.FillRectangle(new SolidBrush(bodyColor), 2, y + 3, open ? 12 : 12, open ? 6 : 8);
            g.DrawRectangle(new Pen(Color.FromArgb(120, 90, 40)), 2, y + 3, 11, open ? 6 : 8);
            return bmp;
        }

        private static Bitmap MakeFileIcon()
        {
            var bmp = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.Transparent);
            var paper = new SolidBrush(Color.FromArgb(240, 244, 250));
            var ink = new Pen(Color.FromArgb(140, 150, 165));
            g.FillRectangle(paper, 4, 1, 8, 14);
            g.DrawRectangle(ink, 4, 1, 8, 14);
            for (int i = 4; i <= 10; i += 3)
                g.DrawLine(new Pen(Color.FromArgb(170, 180, 195)), 6, i, 10, i);
            return bmp;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _watcher?.Dispose();
                _watchDebounce?.Dispose();
                _icons.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>Recently opened files list.</summary>
    public class RecentFilesPanel : ThemedPanel
    {
        private readonly ToolStrip _toolbar;
        private readonly ListBox _list;
        private readonly Label _empty;
        private RecentFilesStore? _store;

        public event Action<string>? OpenFileRequested;

        public RecentFilesPanel()
        {
            _toolbar = new ToolStrip
            {
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden
            };
            var btnClear = new ToolStripButton { Text = L.Get("recent.ctx.clear"), DisplayStyle = ToolStripItemDisplayStyle.Text };
            btnClear.Click += (_, _) => _store?.Clear();
            _toolbar.Items.Add(btnClear);

            _list = new ThemedListBox { Dock = DockStyle.Fill };
            _list.DoubleClick += (_, _) =>
            {
                if (_list.SelectedItem is string p && File.Exists(p))
                    OpenFileRequested?.Invoke(p);
                else if (_list.SelectedItem is string missing)
                {
                    _store?.Remove(missing);
                    Reload();
                }
            };

            var menu = new ContextMenuStrip();
            var miOpen = new ToolStripMenuItem(L.Get("folder.ctx.open"));
            var miRemove = new ToolStripMenuItem(L.Get("recent.ctx.remove"));
            var miClear = new ToolStripMenuItem(L.Get("recent.ctx.clear"));
            miOpen.Click += (_, _) => { if (_list.SelectedItem is string p) OpenFileRequested?.Invoke(p); };
            miRemove.Click += (_, _) =>
            {
                if (_list.SelectedItem is string p) _store?.Remove(p);
                Reload();
            };
            miClear.Click += (_, _) => { _store?.Clear(); };
            menu.Items.AddRange(new ToolStripItem[] { miOpen, miRemove, miClear });
            _list.ContextMenuStrip = menu;

            _empty = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = L.Get("recent.empty")
            };

            Controls.Add(_list);
            Controls.Add(_empty);
            Controls.Add(_toolbar);

        }

        public void Bind(RecentFilesStore store)
        {
            _store = store;
            store.Changed += Reload;
            Reload();
        }

        /// <summary>Re-applies localized strings.</summary>
        public void RefreshTexts()
        {
            _toolbar.Items[0].Text = L.Get("recent.ctx.clear");
            _empty.Text = L.Get("recent.empty");
            Invalidate();
        }

        public void Reload()
        {
            var files = _store?.Files ?? (IReadOnlyList<string>)Array.Empty<string>();
            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (var f in files) _list.Items.Add(f);
            _list.EndUpdate();
            _empty.Visible = files.Count == 0;
            _list.Visible = files.Count > 0;
        }

        protected override void OnThemeApplied()
        {
            _list.BackColor = ThemeManager.Current.SidebarBg;
            _list.ForeColor = ThemeManager.Current.SidebarText;
            _empty.BackColor = ThemeManager.Current.SidebarBg;
            _empty.ForeColor = ThemeManager.Current.SidebarHeader;
            _toolbar.BackColor = ThemeManager.Current.SidebarBg;
            foreach (ToolStripItem item in _toolbar.Items)
                item.ForeColor = ThemeManager.Current.SidebarText;
        }
    }
}
