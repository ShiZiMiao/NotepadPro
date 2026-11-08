using System.Runtime.InteropServices;
using System.Text;

namespace NotepadPro
{
    /// <summary>Text encodings NotepadPro reads and writes.</summary>
    public enum TextEncoding
    {
        /// <summary>System ANSI code page (GBK on zh-CN Windows), no BOM.</summary>
        Ansi,
        /// <summary>UTF-8 without BOM.</summary>
        Utf8,
        /// <summary>UTF-8 with BOM.</summary>
        Utf8Bom,
        /// <summary>UTF-16 little endian without BOM (detected only).</summary>
        Utf16Le,
        /// <summary>UTF-16 little endian with BOM.</summary>
        Utf16LeBom,
        /// <summary>UTF-16 big endian without BOM (detected only).</summary>
        Utf16Be,
        /// <summary>UTF-16 big endian with BOM.</summary>
        Utf16BeBom
    }

    /// <summary>Line-ending conventions.</summary>
    public enum TextEol
    {
        CrLf,
        Lf,
        Cr
    }

    /// <summary>
    /// Encoding detection and conversion. Detection order: BOM, NUL-byte heuristic
    /// for BOM-less UTF-16, strict UTF-8 validation, then ANSI code page fallback.
    /// </summary>
    public static class EncodingDetector
    {
        private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        [DllImport("kernel32.dll")]
        private static extern int GetACP();

        /// <summary>The system ANSI code page encoding (cached).</summary>
        public static Encoding AnsiEncoding { get; } = ResolveAnsiEncoding();

        private static Encoding ResolveAnsiEncoding()
        {
            try
            {
                int acp = GetACP();
                return Encoding.GetEncoding(acp);
            }
            catch
            {
                try { return Encoding.GetEncoding("GB18030"); }
                catch { return Encoding.Latin1; }
            }
        }

        public static bool HasBom(TextEncoding encoding) => encoding is
            TextEncoding.Utf8Bom or TextEncoding.Utf16LeBom or TextEncoding.Utf16BeBom;

        /// <summary>Decodes bytes and reports the encoding used.</summary>
        public static (TextEncoding Encoding, string Text) Decode(byte[] data)
        {
            if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
                return (TextEncoding.Utf8Bom, Encoding.UTF8.GetString(data, 3, data.Length - 3));

            if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xFE)
                return (TextEncoding.Utf16LeBom, Encoding.Unicode.GetString(data, 2, data.Length - 2));

            if (data.Length >= 2 && data[0] == 0xFE && data[1] == 0xFF)
                return (TextEncoding.Utf16BeBom, Encoding.BigEndianUnicode.GetString(data, 2, data.Length - 2));

            if (data.Length == 0)
                return (TextEncoding.Utf8, string.Empty);

            // NUL-byte heuristic: interleaved zero bytes mean BOM-less UTF-16.
            int nulOdd = 0, nulEven = 0;
            int sample = Math.Min(data.Length, 8192);
            for (int i = 0; i < sample; i++)
            {
                if (data[i] == 0)
                {
                    if ((i & 1) == 0) nulEven++; else nulOdd++;
                }
            }
            if (nulEven > sample / 8 && nulOdd == 0)
                return (TextEncoding.Utf16Be, Encoding.BigEndianUnicode.GetString(data));
            if (nulOdd > sample / 8 && nulEven == 0)
                return (TextEncoding.Utf16Le, Encoding.Unicode.GetString(data));

            // Strict UTF-8; anything invalid falls back to the ANSI code page.
            try
            {
                return (TextEncoding.Utf8, StrictUtf8.GetString(data));
            }
            catch (DecoderFallbackException)
            {
                return (TextEncoding.Ansi, AnsiEncoding.GetString(data));
            }
        }

        /// <summary>Encodes text to bytes using the given encoding (including BOM when required).</summary>
        public static byte[] Encode(string text, TextEncoding encoding)
        {
            switch (encoding)
            {
                case TextEncoding.Ansi:
                    return AnsiEncoding.GetBytes(text);
                case TextEncoding.Utf8:
                    return new UTF8Encoding(false).GetBytes(text);
                case TextEncoding.Utf8Bom:
                    return new UTF8Encoding(true).GetBytes(text);
                case TextEncoding.Utf16Le:
                    return Encoding.Unicode.GetBytes(text);
                case TextEncoding.Utf16LeBom:
                    return Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(text)).ToArray();
                case TextEncoding.Utf16Be:
                    return Encoding.BigEndianUnicode.GetBytes(text);
                case TextEncoding.Utf16BeBom:
                    return Encoding.BigEndianUnicode.GetPreamble()
                        .Concat(Encoding.BigEndianUnicode.GetBytes(text)).ToArray();
                default:
                    return new UTF8Encoding(false).GetBytes(text);
            }
        }

        /// <summary>Localized display name for the status bar and menus.</summary>
        public static string DisplayName(TextEncoding encoding) => encoding switch
        {
            TextEncoding.Ansi => string.Format(L.Get("encoding.ansi.name"), AnsiEncoding.CodePage),
            TextEncoding.Utf8 => L.Get("encoding.utf8.name"),
            TextEncoding.Utf8Bom => L.Get("encoding.utf8bom.name"),
            TextEncoding.Utf16Le => L.Get("encoding.utf16le.name"),
            TextEncoding.Utf16LeBom => L.Get("encoding.utf16lebom.name"),
            TextEncoding.Utf16Be => L.Get("encoding.utf16be.name"),
            TextEncoding.Utf16BeBom => L.Get("encoding.utf16bebom.name"),
            _ => encoding.ToString()
        };

        /// <summary>Menu key for an encoding; BOM-less UTF-16 variants map to their BOM variants.</summary>
        public static string MenuKey(TextEncoding encoding) => encoding switch
        {
            TextEncoding.Utf16Le => "utf16le",
            TextEncoding.Utf16LeBom => "utf16le",
            TextEncoding.Utf16Be => "utf16be",
            TextEncoding.Utf16BeBom => "utf16be",
            TextEncoding.Utf8Bom => "utf8bom",
            TextEncoding.Utf8 => "utf8",
            _ => "ansi"
        };

        public static TextEncoding FromMenuKey(string key) => key switch
        {
            "utf8" => TextEncoding.Utf8,
            "utf8bom" => TextEncoding.Utf8Bom,
            "utf16le" => TextEncoding.Utf16LeBom,
            "utf16be" => TextEncoding.Utf16BeBom,
            _ => TextEncoding.Ansi
        };

        /// <summary>Detects the dominant line ending of a text sample.</summary>
        public static TextEol DetectEol(string text)
        {
            int crlf = 0, lf = 0, cr = 0;
            int sample = Math.Min(text.Length, 65536);
            for (int i = 0; i < sample; i++)
            {
                char c = text[i];
                if (c == '\r')
                {
                    if (i + 1 < sample && text[i + 1] == '\n') { crlf++; i++; }
                    else cr++;
                }
                else if (c == '\n') lf++;
            }
            if (crlf == 0 && lf == 0 && cr == 0) return TextEol.CrLf;
            if (crlf >= lf && crlf >= cr) return TextEol.CrLf;
            if (lf >= cr) return TextEol.Lf;
            return TextEol.Cr;
        }

        public static string EolString(TextEol eol) => eol switch
        {
            TextEol.Lf => "\n",
            TextEol.Cr => "\r",
            _ => "\r\n"
        };

        /// <summary>Normalizes all line endings in a text to the target convention.</summary>
        public static string NormalizeEol(string text, TextEol eol)
        {
            var sb = new StringBuilder(text.Length + 16);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\r')
                {
                    if (i + 1 < text.Length && text[i + 1] == '\n') i++;
                    sb.Append(EolString(eol));
                }
                else if (c == '\n')
                {
                    sb.Append(EolString(eol));
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public static string EolDisplayName(TextEol eol) => eol switch
        {
            TextEol.Lf => L.Get("status.eol.lf"),
            TextEol.Cr => L.Get("status.eol.cr"),
            _ => L.Get("status.eol.crlf")
        };
    }
}
