using System.IO;
using System.Text.Json;

namespace NotepadPro
{
    /// <summary>Recently opened files, newest first, persisted to %APPDATA%.</summary>
    public class RecentFilesStore
    {
        private readonly string _filePath;
        private List<string> _files = new();
        private int _max;

        /// <summary>Raised after the list changes (refresh menus and panel).</summary>
        public event Action? Changed;

        public RecentFilesStore(int max)
        {
            _filePath = Path.Combine(AppSettings.Directory, "recent.json");
            _max = Math.Max(1, max);
            Load();
        }

        public int Max
        {
            get => _max;
            set
            {
                _max = Math.Max(1, value);
                Trim();
                Save();
            }
        }

        public IReadOnlyList<string> Files => _files;

        public void Add(string path)
        {
            path = Path.GetFullPath(path);
            _files.Remove(path);
            _files.Insert(0, path);
            Trim();
            Save();
            Changed?.Invoke();
        }

        public void Remove(string path)
        {
            if (_files.Remove(path))
            {
                Save();
                Changed?.Invoke();
            }
        }

        public void Clear()
        {
            _files.Clear();
            Save();
            Changed?.Invoke();
        }

        private void Trim() => _files = _files.Take(_max).ToList();

        private void Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var loaded = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(_filePath));
                    if (loaded != null)
                        _files = loaded.Where(File.Exists).ToList();
                }
            }
            catch
            {
                _files = new List<string>();
            }
        }

        private void Save()
        {
            try
            {
                Directory.CreateDirectory(AppSettings.Directory);
                File.WriteAllText(_filePath, JsonSerializer.Serialize(_files));
            }
            catch
            {
                // best-effort
            }
        }
    }
}
