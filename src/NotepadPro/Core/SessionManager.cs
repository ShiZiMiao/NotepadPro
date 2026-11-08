using System.IO;
using System.Text.Json;

namespace NotepadPro
{
    /// <summary>One restored tab: file path plus view state.</summary>
    public class SessionEntry
    {
        public string Path { get; set; } = string.Empty;
        public int Line { get; set; }
        public int Column { get; set; }
        public int FirstVisibleLine { get; set; }
    }

    public class SessionData
    {
        public List<SessionEntry> Files { get; set; } = new();
        public int ActiveIndex { get; set; }
    }

    /// <summary>Persists the open-tab list between runs ("remember session").</summary>
    public static class SessionManager
    {
        private static string FilePath => Path.Combine(AppSettings.Directory, "session.json");

        public static void Save(IReadOnlyList<DocumentBuffer> documents, int activeIndex)
        {
            var data = new SessionData { ActiveIndex = activeIndex };
            foreach (var doc in documents)
            {
                if (doc.IsUntitled || doc.FilePath == null) continue;
                data.Files.Add(new SessionEntry
                {
                    Path = doc.FilePath,
                    Line = doc.Editor.CurrentLine,
                    Column = doc.Editor.CurrentPosition - doc.Editor.Lines[doc.Editor.CurrentLine].Position,
                    FirstVisibleLine = doc.Editor.FirstVisibleLine
                });
            }
            try
            {
                Directory.CreateDirectory(AppSettings.Directory);
                File.WriteAllText(FilePath, JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                }));
            }
            catch
            {
                // Session persistence is best-effort.
            }
        }

        /// <summary>Loads the last session; entries for missing files are skipped.</summary>
        public static SessionData Load()
        {
            var data = new SessionData();
            try
            {
                if (!File.Exists(FilePath)) return data;
                var loaded = JsonSerializer.Deserialize<SessionData>(File.ReadAllText(FilePath));
                if (loaded == null) return data;
                foreach (var entry in loaded.Files)
                {
                    if (File.Exists(entry.Path)) data.Files.Add(entry);
                }
                data.ActiveIndex = loaded.ActiveIndex;
            }
            catch
            {
                // Corrupt session falls back to empty.
            }
            return data;
        }

        public static void Clear()
        {
            try { if (File.Exists(FilePath)) File.Delete(FilePath); }
            catch { /* best-effort */ }
        }
    }
}
