using System.Text;
using NotepadPro.Languages;
using Xunit;

namespace NotepadPro.Tests
{
    public class EncodingDetectorTests
    {
        [Fact]
        public void Decode_Utf8Bom_IsDetectedAndStripped()
        {
            var bytes = new byte[] { 0xEF, 0xBB, 0xBF }
                .Concat(Encoding.UTF8.GetBytes("hello")).ToArray();
            var (enc, text) = EncodingDetector.Decode(bytes);
            Assert.Equal(TextEncoding.Utf8Bom, enc);
            Assert.Equal("hello", text);
        }

        [Fact]
        public void Decode_Utf16LeBom_IsDetected()
        {
            var bytes = new byte[] { 0xFF, 0xFE }
                .Concat(Encoding.Unicode.GetBytes("你好")).ToArray();
            var (enc, text) = EncodingDetector.Decode(bytes);
            Assert.Equal(TextEncoding.Utf16LeBom, enc);
            Assert.Equal("你好", text);
        }

        [Fact]
        public void Decode_Utf16BeBom_IsDetected()
        {
            var bytes = new byte[] { 0xFE, 0xFF }
                .Concat(Encoding.BigEndianUnicode.GetBytes("ok")).ToArray();
            var (enc, text) = EncodingDetector.Decode(bytes);
            Assert.Equal(TextEncoding.Utf16BeBom, enc);
            Assert.Equal("ok", text);
        }

        [Fact]
        public void Decode_PlainUtf8Chinese_NoBomNeeded()
        {
            var bytes = Encoding.UTF8.GetBytes("记事本专业版");
            var (enc, text) = EncodingDetector.Decode(bytes);
            Assert.Equal(TextEncoding.Utf8, enc);
            Assert.Equal("记事本专业版", text);
        }

        [Fact]
        public void Decode_GbkBytes_FallsBackToAnsiAndRoundTrips()
        {
            var gbk = Encoding.GetEncoding(936);
            var original = "# 配置文件：服务器地址";
            var bytes = gbk.GetBytes(original);
            var (enc, text) = EncodingDetector.Decode(bytes);
            Assert.Equal(TextEncoding.Ansi, enc);
            Assert.Equal(original, text);
        }

        [Fact]
        public void Decode_EmptyData_ReturnsUtf8Empty()
        {
            var (enc, text) = EncodingDetector.Decode(Array.Empty<byte>());
            Assert.Equal(TextEncoding.Utf8, enc);
            Assert.Equal(string.Empty, text);
        }

        [Fact]
        public void EncodeDecode_RoundTrip_AllMenuEncodings()
        {
            const string sample = "NotepadPro 记事本 v1.0\nline2";
            foreach (var enc in new[]
                     {
                         TextEncoding.Ansi, TextEncoding.Utf8, TextEncoding.Utf8Bom,
                         TextEncoding.Utf16LeBom, TextEncoding.Utf16BeBom
                     })
            {
                var bytes = EncodingDetector.Encode(sample, enc);
                var (detected, text) = EncodingDetector.Decode(bytes);
                Assert.Equal(sample, text);
            }
        }

        [Fact]
        public void MenuKey_RoundTrips()
        {
            foreach (var enc in new[]
                     {
                         TextEncoding.Ansi, TextEncoding.Utf8, TextEncoding.Utf8Bom,
                         TextEncoding.Utf16LeBom, TextEncoding.Utf16BeBom
                     })
            {
                Assert.Equal(enc, EncodingDetector.FromMenuKey(EncodingDetector.MenuKey(enc)));
            }
        }
    }

    public class EolTests
    {
        [Theory]
        [InlineData("a\r\nb\r\nc", TextEol.CrLf)]
        [InlineData("a\nb\nc", TextEol.Lf)]
        [InlineData("a\rb\rc", TextEol.Cr)]
        [InlineData("", TextEol.CrLf)]
        public void DetectEol_FindsDominantEnding(string text, TextEol expected)
        {
            Assert.Equal(expected, EncodingDetector.DetectEol(text));
        }

        [Fact]
        public void NormalizeEol_ConvertsAllEndings()
        {
            const string mixed = "a\r\nb\nc\rd";
            Assert.Equal("a\r\nb\r\nc\r\nd", EncodingDetector.NormalizeEol(mixed, TextEol.CrLf));
            Assert.Equal("a\nb\nc\nd", EncodingDetector.NormalizeEol(mixed, TextEol.Lf));
            Assert.Equal("a\rb\rc\rd", EncodingDetector.NormalizeEol(mixed, TextEol.Cr));
        }

        [Fact]
        public void EolString_MatchesConvention()
        {
            Assert.Equal("\r\n", EncodingDetector.EolString(TextEol.CrLf));
            Assert.Equal("\n", EncodingDetector.EolString(TextEol.Lf));
            Assert.Equal("\r", EncodingDetector.EolString(TextEol.Cr));
        }
    }

    public class LanguageCatalogTests
    {
        [Fact]
        public void All_HasAtLeastTwentyLanguages()
        {
            Assert.True(LanguageCatalog.All.Count >= 20);
        }

        [Theory]
        [InlineData("demo.py", "python")]
        [InlineData("Greeter.cs", "csharp")]
        [InlineData("page.html", "html")]
        [InlineData("data.json", "json")]
        [InlineData("README.md", "markdown")]
        [InlineData("run.bat", "batch")]
        [InlineData("s.ps1", "powershell")]
        [InlineData("m.yaml", "yaml")]
        [InlineData("m.yml", "yaml")]
        [InlineData("a.ini", "ini")]
        [InlineData("m.lua", "lua")]
        [InlineData("m.rb", "ruby")]
        [InlineData("m.rs", "rust")]
        [InlineData("m.go", "go")]
        [InlineData("m.php", "php")]
        [InlineData("m.css", "css")]
        [InlineData("m.sql", "sql")]
        [InlineData("Main.java", "java")]
        [InlineData("app.ts", "typescript")]
        [InlineData("app.js", "javascript")]
        public void Match_RecognizesCommonExtensions(string file, string expectedId)
        {
            var lang = LanguageDef.Match(file);
            Assert.NotNull(lang);
            Assert.Equal(expectedId, lang!.Id);
        }

        [Fact]
        public void Match_UnknownExtension_ReturnsNull()
        {
            Assert.Null(LanguageDef.Match("file.xyz123"));
            Assert.Null(LanguageDef.Match("noextension"));
        }

        [Fact]
        public void EveryLanguage_HasNonNullStyleMapAndLexer()
        {
            foreach (var lang in LanguageCatalog.All)
            {
                Assert.NotNull(lang.StyleMap);
                Assert.False(string.IsNullOrWhiteSpace(lang.LexerName));
                Assert.False(string.IsNullOrWhiteSpace(lang.DisplayName));
            }
        }

        [Fact]
        public void ById_FindsAndReturnsNullSafely()
        {
            Assert.NotNull(LanguageCatalog.ById("python"));
            Assert.Null(LanguageCatalog.ById("does-not-exist"));
        }
    }
}
