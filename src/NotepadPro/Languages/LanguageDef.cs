using ScintillaNET;

namespace NotepadPro.Languages
{
    /// <summary>
    /// Semantic role of a lexer style; themes map each role to a concrete color
    /// and font flags. This indirection keeps all languages themeable at once.
    /// </summary>
    public enum StyleRole
    {
        Default,
        Comment,
        DocComment,
        Keyword,
        SecondaryKeyword,
        String,
        Character,
        Number,
        Operator,
        Identifier,
        Preprocessor,
        Regex,
        Error,
        MarkupTag,
        MarkupAttribute,
        MarkupEntity,
        Embedded,
        ClassName,
        FunctionName,
        Variable,
        PropertyKey,
        Section,
        Header,
        Bold,
        Italic,
        Link,
        CodeInline,
        ListMarker,
        BlockQuote,
        Strikeout,
        HRule,
        Cmdlet,
        VariableName,
        Symbol,
        Directive
    }

    /// <summary>Font decoration for a role (in addition to foreground color).</summary>
    [Flags]
    public enum RoleFlags
    {
        None = 0,
        Bold = 1,
        Italic = 2,
        Strikeout = 4
    }

    /// <summary>
    /// A language definition: lexer name, keyword sets, style-role mapping,
    /// comment syntax and extension matching.
    /// </summary>
    public class LanguageDef
    {
        /// <summary>Internal id, also used as the Language menu tag.</summary>
        public required string Id { get; init; }

        /// <summary>Display name (language names are proper nouns; not localized).</summary>
        public required string DisplayName { get; init; }

        /// <summary>Scintilla 5 lexer name (Lexilla), e.g. "cpp", "hypertext".</summary>
        public required string LexerName { get; init; }

        public required string[] Extensions { get; init; }

        /// <summary>Keyword sets; index matches Scintilla keyword slot 0..8.</summary>
        public string[] KeywordSets { get; init; } = Array.Empty<string>();

        /// <summary>Maps lexer style ids to theme roles.</summary>
        public (int Style, StyleRole Role)[] StyleMap { get; init; } = Array.Empty<(int, StyleRole)>();

        public string? LineComment { get; init; }

        public (string Open, string Close)? BlockComment { get; init; }

        /// <summary>Whether the lexer supports Scintilla fold points.</summary>
        public bool Foldable { get; init; } = true;

        /// <summary>File extensions joined for menus/status, e.g. "*.py; *.pyw".</summary>
        public string ExtensionPattern => string.Join("; ",
            Extensions.Select(e => "*" + e));

        /// <summary>Finds the language for a file path; null when unknown.</summary>
        public static LanguageDef? Match(string path)
        {
            var ext = Path.GetExtension(path);
            if (ext.Length == 0) return null;
            foreach (var lang in LanguageCatalog.All)
            {
                foreach (var candidate in lang.Extensions)
                {
                    if (string.Equals(candidate, ext, StringComparison.OrdinalIgnoreCase))
                        return lang;
                }
            }
            return null;
        }

        /// <summary>Applies lexer + keywords to an editor (styles are applied by ThemeManager).</summary>
        public void Apply(Scintilla sci)
        {
            sci.LexerName = LexerName;
            for (int i = 0; i < KeywordSets.Length && i < 9; i++)
            {
                sci.SetKeywords(i, KeywordSets[i]);
            }
        }
    }

    /// <summary>
    /// Style constants for lexers that the Scintilla5.NET package does not
    /// expose convenience classes for (Rust, YAML). Values follow Scintilla's
    /// published SCE_RUST_* / SCE_YAML_* numbering.
    /// </summary>
    public static class ExtraStyles
    {
        // SCE_RUST_*
        public const int RustDefault = 0;
        public const int RustCommentBlock = 1;
        public const int RustCommentLine = 2;
        public const int RustCommentBlockDoc = 3;
        public const int RustCommentLineDoc = 4;
        public const int RustString = 6;
        public const int RustStringRaw = 7;
        public const int RustCharacter = 8;
        public const int RustOperator = 10;
        public const int RustIdentifier = 11;
        public const int RustLifetime = 12;
        public const int RustMacro = 13;
        public const int RustLexError = 14;
        public const int RustKeyword = 15;
        public const int RustSecondaryKeyword = 16;
        public const int RustTertiaryKeyword = 17;

        // SCE_YAML_*
        public const int YamlDefault = 0;
        public const int YamlComment = 1;
        public const int YamlIdentifier = 2;
        public const int YamlKeyword = 3;
        public const int YamlNumber = 4;
        public const int YamlReference = 5;
        public const int YamlDocument = 6;
        public const int YamlText = 7;
        public const int YamlError = 8;
        public const int YamlOperator = 9;
        public const int YamlKey = 10;
    }
}
