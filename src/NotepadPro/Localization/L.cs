using System.Reflection;
using System.Text.Json;

namespace NotepadPro
{
    /// <summary>
    /// Localization engine: flat key/value dictionaries loaded from embedded JSON
    /// resources. Use <see cref="Get"/> (or the <see cref="T(string)"/> helper
    /// class) for every user-visible string, and subscribe to
    /// <see cref="LanguageChanged"/> to re-apply texts when the language switches.
    /// </summary>
    public static class L
    {
        public const string FallbackCulture = "en-US";
        private const string MissingPrefix = "?";

        private static Dictionary<string, string> _strings = new();
        private static Dictionary<string, string> _fallback = new();

        /// <summary>Raised after the active culture has changed.</summary>
        public static event Action? LanguageChanged;

        /// <summary>Culture code of the active language, e.g. "zh-CN".</summary>
        public static string Culture { get; private set; } = FallbackCulture;

        public static void Initialize(AppSettings settings)
        {
            string culture = settings.Language switch
            {
                UiLanguage.ZhCn => "zh-CN",
                UiLanguage.EnUs => FallbackCulture,
                _ => System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("zh")
                        ? "zh-CN" : FallbackCulture
            };
            SetCulture(culture);
        }

        public static void SetCulture(string culture)
        {
            Culture = culture;
            _fallback = LoadResource(FallbackCulture);
            _strings = culture == FallbackCulture
                ? _fallback
                : Merge(_fallback, LoadResource(culture));
            LanguageChanged?.Invoke();
        }

        private static Dictionary<string, string> Merge(
            Dictionary<string, string> primary, Dictionary<string, string> overlay)
        {
            var merged = new Dictionary<string, string>(primary);
            foreach (var kv in overlay) merged[kv.Key] = kv.Value;
            return merged;
        }

        private static Dictionary<string, string> LoadResource(string culture)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var name = $"NotepadPro.Localization.{culture}.json";
            using var stream = assembly.GetManifestResourceStream(name);
            if (stream == null) return new Dictionary<string, string>();
            using var reader = new StreamReader(stream);
            var doc = JsonDocument.Parse(reader.ReadToEnd());
            var result = new Dictionary<string, string>();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                result[prop.Name] = prop.Value.GetString() ?? prop.Name;
            }
            return result;
        }

        public static string Get(string key) => _strings.TryGetValue(key, out var v) ? v
            : _fallback.TryGetValue(key, out var f) ? f
            : MissingPrefix + key;
    }

    /// <summary>Fluent shorthand: <c>Loc.T("key")</c> returns the translated string.</summary>
    public static class Loc
    {
        public static string T(string key) => L.Get(key);
    }
}
