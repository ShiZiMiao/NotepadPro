using System.Text.Json;

namespace NotepadPro.Tests
{
    /// <summary>
    /// Persists data away from real %APPDATA%: points AppSettings.Directory at
    /// a sandbox folder for the duration of each test. Tests in this collection
    /// run sequentially because the override is process-wide.
    /// </summary>
    [Collection("AppData")]
    public abstract class AppDataSandbox : IDisposable
    {
        protected AppDataSandbox()
        {
            var temp = Path.Combine(Path.GetTempPath(), "notepadpro-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            AppSettings.DirectoryOverride = temp;
            SandboxPath = temp;
        }

        protected string SandboxPath { get; }

        /// <summary>Creates a real file inside the sandbox for store round-trips.</summary>
        protected string MakeRealFile(string name, string content = "test")
        {
            var path = Path.Combine(SandboxPath, name);
            File.WriteAllText(path, content);
            return path;
        }

        public void Dispose()
        {
            try { Directory.Delete(SandboxPath, recursive: true); } finally { AppSettings.DirectoryOverride = null; }
        }
    }

    [Collection("AppData")]
    public class RecentFilesStoreTests : AppDataSandbox
    {
        [Fact]
        public void Add_MovesToFrontAndDeduplicates()
        {
            var store = new RecentFilesStore(5);
            var a = MakeRealFile("a.txt");
            var b = MakeRealFile("b.txt");
            store.Add(a);
            store.Add(b);
            store.Add(a);
            Assert.Equal(a, store.Files[0]);
            Assert.Equal(b, store.Files[1]);
            Assert.Equal(2, store.Files.Count);
        }

        [Fact]
        public void Add_RespectsMaxCapacity()
        {
            var store = new RecentFilesStore(3);
            for (int i = 0; i < 6; i++)
                store.Add(MakeRealFile($"f{i}.txt"));
            Assert.Equal(3, store.Files.Count);
            Assert.EndsWith("f5.txt", store.Files[0]);
        }

        [Fact]
        public void Persist_ReloadsBetweenInstances()
        {
            var file = MakeRealFile("persisted.txt");
            var first = new RecentFilesStore(5);
            first.Add(file);

            var second = new RecentFilesStore(5);
            Assert.Contains(file, second.Files);
        }

        [Fact]
        public void Remove_And_Clear_Work()
        {
            var store = new RecentFilesStore(5);
            var x = MakeRealFile("x.txt");
            store.Add(x);
            store.Remove(x);
            Assert.Empty(store.Files);
            store.Add(MakeRealFile("y.txt"));
            store.Clear();
            Assert.Empty(store.Files);
        }
    }

    [Collection("AppData")]
    public class SessionManagerTests : AppDataSandbox
    {
        [Fact]
        public void SaveAndLoad_RoundTripsEntries()
        {
            var one = MakeRealFile("one.py");
            var two = MakeRealFile("two.cs");
            var data = new SessionData
            {
                ActiveIndex = 1,
                Files =
                {
                    new SessionEntry { Path = one, Line = 3, Column = 7, FirstVisibleLine = 1 },
                    new SessionEntry { Path = two, Line = 0, Column = 0, FirstVisibleLine = 0 }
                }
            };
            // Write through the same JSON shape SessionManager persists.
            var path = Path.Combine(AppSettings.Directory, "session.json");
            File.WriteAllText(path, JsonSerializer.Serialize(data,
                new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));

            var loaded = SessionManager.Load();
            Assert.Equal(2, loaded.Files.Count);
            Assert.Equal(1, loaded.ActiveIndex);
            Assert.Equal(one, loaded.Files[0].Path);
            Assert.Equal(3, loaded.Files[0].Line);
            Assert.Equal(7, loaded.Files[0].Column);
        }

        [Fact]
        public void Load_SkipsMissingFiles()
        {
            var real = MakeRealFile("real.txt");
            var data = new SessionData
            {
                Files =
                {
                    new SessionEntry { Path = @"Z:\definitely-missing.txt" },
                    new SessionEntry { Path = real }
                }
            };
            var path = Path.Combine(AppSettings.Directory, "session.json");
            File.WriteAllText(path, JsonSerializer.Serialize(data));

            var loaded = SessionManager.Load();
            Assert.Single(loaded.Files);
            Assert.Equal(real, loaded.Files[0].Path);
        }

        [Fact]
        public void Load_WithoutFile_ReturnsEmpty()
        {
            var loaded = SessionManager.Load();
            Assert.Empty(loaded.Files);
        }

        [Fact]
        public void Load_CorruptJson_ReturnsEmpty()
        {
            File.WriteAllText(Path.Combine(AppSettings.Directory, "session.json"), "{not json");
            var loaded = SessionManager.Load();
            Assert.Empty(loaded.Files);
        }
    }

    [Collection("AppData")]
    public class AppSettingsTests : AppDataSandbox
    {
        [Fact]
        public void SaveAndLoad_RoundTripsValues()
        {
            var settings = new AppSettings
            {
                FontName = "Cascadia Mono",
                FontSize = 13,
                Theme = AppTheme.Dark,
                Language = UiLanguage.ZhCn,
                WrapLines = true
            };
            settings.Save();

            var loaded = AppSettings.Load();
            Assert.Equal("Cascadia Mono", loaded.FontName);
            Assert.Equal(13, loaded.FontSize);
            Assert.Equal(AppTheme.Dark, loaded.Theme);
            Assert.Equal(UiLanguage.ZhCn, loaded.Language);
            Assert.True(loaded.WrapLines);
        }

        [Fact]
        public void Load_WithoutFile_ReturnsDefaults()
        {
            var loaded = AppSettings.Load();
            Assert.Equal("Consolas", loaded.FontName);
            Assert.Equal(AppTheme.Light, loaded.Theme);
        }
    }
}
