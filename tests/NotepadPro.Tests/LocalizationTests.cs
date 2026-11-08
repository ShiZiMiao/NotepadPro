using System.Text.Json;
using NotepadPro;

namespace NotepadPro.Tests
{
    /// <summary>
    /// The localization engine reads embedded resources; these tests cover both
    /// shipped languages and the fallback behavior.
    /// </summary>
    public class LocalizationTests
    {
        [Fact]
        public void SetCulture_Chinese_ReturnsChineseStrings()
        {
            L.SetCulture("zh-CN");
            Assert.Equal("文件(&F)", L.Get("menu.file"));
            Assert.Equal("替换", L.Get("find.title.replace"));
        }

        [Fact]
        public void SetCulture_English_ReturnsEnglishStrings()
        {
            L.SetCulture("en-US");
            Assert.Equal("&File", L.Get("menu.file"));
            Assert.Equal("Replace", L.Get("find.title.replace"));
        }

        [Fact]
        public void UnknownKey_ReturnsKeyMarkedMissing()
        {
            L.SetCulture("en-US");
            Assert.Equal("?no.such.key", L.Get("no.such.key"));
        }

        [Fact]
        public void BothCultures_HaveSameKeySets()
        {
            // Both tables must stay in sync so switching languages never
            // leaves raw keys visible.
            var zh = typeof(L).Assembly;
            using var zhStream = zh.GetManifestResourceStream("NotepadPro.Localization.zh-CN.json")!;
            using var enStream = zh.GetManifestResourceStream("NotepadPro.Localization.en-US.json")!;
            Assert.NotNull(zhStream);
            Assert.NotNull(enStream);

            var zhDoc = JsonDocument.Parse(new StreamReader(zhStream).ReadToEnd());
            var enDoc = JsonDocument.Parse(new StreamReader(enStream).ReadToEnd());
            var zhKeys = zhDoc.RootElement.EnumerateObject().Select(p => p.Name).ToHashSet();
            var enKeys = enDoc.RootElement.EnumerateObject().Select(p => p.Name).ToHashSet();

            var missingInEn = zhKeys.Except(enKeys).ToList();
            var missingInZh = enKeys.Except(zhKeys).ToList();
            Assert.True(missingInEn.Count == 0, "Missing in en-US: " + string.Join(",", missingInEn));
            Assert.True(missingInZh.Count == 0, "Missing in zh-CN: " + string.Join(",", missingInZh));
        }

        [Fact]
        public void LanguageChanged_EventFiresOnSwitch()
        {
            bool fired = false;
            void Handler() => fired = true;
            L.LanguageChanged += Handler;
            try
            {
                L.SetCulture("zh-CN");
                Assert.True(fired);
            }
            finally
            {
                L.LanguageChanged -= Handler;
            }
        }
    }
}
