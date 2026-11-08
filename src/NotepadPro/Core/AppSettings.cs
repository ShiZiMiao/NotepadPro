using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NotepadPro
{
    public enum AppTheme
    {
        Light = 0,
        Dark = 1
    }

    public enum UiLanguage
    {
        Auto = 0,
        ZhCn = 1,
        EnUs = 2
    }

    /// <summary>
    /// Persistent application settings stored as JSON in %APPDATA%\NotepadPro.
    /// </summary>
    public class AppSettings
    {
        public string FontName { get; set; } = "Consolas";
        public int FontSize { get; set; } = 11;
        public int TabWidth { get; set; } = 4;
        public bool UseSpaces { get; set; } = true;
        public bool BackspaceUnindents { get; set; } = true;
        public AppTheme Theme { get; set; } = AppTheme.Light;
        public UiLanguage Language { get; set; } = UiLanguage.Auto;
        public bool WrapLines { get; set; } = false;
        public bool ShowWhitespace { get; set; } = false;
        public bool ShowEol { get; set; } = false;
        public bool ShowFoldMargin { get; set; } = true;
        public bool RememberSession { get; set; } = true;
        public bool ShowFolderPanel { get; set; } = false;
        public bool ShowRecentPanel { get; set; } = false;
        public int MaxRecentFiles { get; set; } = 15;
        public string? LastFolder { get; set; }
        public int WindowLeft { get; set; } = -1;
        public int WindowTop { get; set; } = -1;
        public int WindowWidth { get; set; } = 1100;
        public int WindowHeight { get; set; } = 760;
        public bool WindowMaximized { get; set; } = false;

        /// <summary>
        /// Overrides the data directory for tests. When set, Directory returns
        /// this path instead of %APPDATA%\NotepadPro.
        /// </summary>
        public static string? DirectoryOverride { get; set; }

        [JsonIgnore]
        public static string Directory => DirectoryOverride ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NotepadPro");

        [JsonIgnore]
        public static string FilePath => Path.Combine(Directory, "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath));
                    if (loaded != null) return loaded;
                }
            }
            catch
            {
                // Corrupt settings fall back to defaults.
            }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                System.IO.Directory.CreateDirectory(Directory);
                File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                }));
            }
            catch
            {
                // Settings persistence is best-effort.
            }
        }
    }
}
