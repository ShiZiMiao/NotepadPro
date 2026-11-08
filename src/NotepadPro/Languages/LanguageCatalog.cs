namespace NotepadPro.Languages
{
    /// <summary>
    /// The built-in language catalog. Keyword lists are standard language
    /// token sets compiled for NotepadPro.
    /// </summary>
    public static class LanguageCatalog
    {
        private static readonly Dictionary<string, LanguageDef> _byId = new();
        private static IReadOnlyList<LanguageDef>? _all;

        // Shared style maps. Declared BEFORE the language definitions because
        // static field initializers run in textual order - the definitions
        // below reference these arrays while they are being initialized.
        private static readonly (int, StyleRole)[] CFamilyStyles =
        {
            ((int)ScintillaNET.Style.Cpp.Default, StyleRole.Default),
            ((int)ScintillaNET.Style.Cpp.Comment, StyleRole.Comment),
            ((int)ScintillaNET.Style.Cpp.CommentLine, StyleRole.Comment),
            ((int)ScintillaNET.Style.Cpp.CommentDoc, StyleRole.DocComment),
            ((int)ScintillaNET.Style.Cpp.CommentLineDoc, StyleRole.DocComment),
            ((int)ScintillaNET.Style.Cpp.CommentDocKeyword, StyleRole.DocComment),
            ((int)ScintillaNET.Style.Cpp.CommentDocKeywordError, StyleRole.Error),
            ((int)ScintillaNET.Style.Cpp.PreprocessorComment, StyleRole.Comment),
            ((int)ScintillaNET.Style.Cpp.PreprocessorCommentDoc, StyleRole.Comment),
            ((int)ScintillaNET.Style.Cpp.TaskMarker, StyleRole.Comment),
            ((int)ScintillaNET.Style.Cpp.Number, StyleRole.Number),
            ((int)ScintillaNET.Style.Cpp.Word, StyleRole.Keyword),
            ((int)ScintillaNET.Style.Cpp.Word2, StyleRole.SecondaryKeyword),
            ((int)ScintillaNET.Style.Cpp.GlobalClass, StyleRole.SecondaryKeyword),
            ((int)ScintillaNET.Style.Cpp.String, StyleRole.String),
            ((int)ScintillaNET.Style.Cpp.Character, StyleRole.Character),
            ((int)ScintillaNET.Style.Cpp.StringEol, StyleRole.Error),
            ((int)ScintillaNET.Style.Cpp.Verbatim, StyleRole.String),
            ((int)ScintillaNET.Style.Cpp.StringRaw, StyleRole.String),
            ((int)ScintillaNET.Style.Cpp.TripleVerbatim, StyleRole.String),
            ((int)ScintillaNET.Style.Cpp.HashQuotedString, StyleRole.String),
            ((int)ScintillaNET.Style.Cpp.UserLiteral, StyleRole.String),
            ((int)ScintillaNET.Style.Cpp.EscapeSequence, StyleRole.Character),
            ((int)ScintillaNET.Style.Cpp.Preprocessor, StyleRole.Preprocessor),
            ((int)ScintillaNET.Style.Cpp.Operator, StyleRole.Operator),
            ((int)ScintillaNET.Style.Cpp.Identifier, StyleRole.Identifier),
            ((int)ScintillaNET.Style.Cpp.Regex, StyleRole.Regex),
            ((int)ScintillaNET.Style.Cpp.Uuid, StyleRole.String),
        };

        private static readonly (int, StyleRole)[] HtmlStyles =
        {
            ((int)ScintillaNET.Style.Html.Default, StyleRole.Default),
            ((int)ScintillaNET.Style.Html.Tag, StyleRole.MarkupTag),
            ((int)ScintillaNET.Style.Html.TagUnknown, StyleRole.Error),
            ((int)ScintillaNET.Style.Html.TagEnd, StyleRole.MarkupTag),
            ((int)ScintillaNET.Style.Html.XmlStart, StyleRole.MarkupTag),
            ((int)ScintillaNET.Style.Html.XmlEnd, StyleRole.MarkupTag),
            ((int)ScintillaNET.Style.Html.Attribute, StyleRole.MarkupAttribute),
            ((int)ScintillaNET.Style.Html.AttributeUnknown, StyleRole.Error),
            ((int)ScintillaNET.Style.Html.Number, StyleRole.Number),
            ((int)ScintillaNET.Style.Html.DoubleString, StyleRole.String),
            ((int)ScintillaNET.Style.Html.SingleString, StyleRole.String),
            ((int)ScintillaNET.Style.Html.Value, StyleRole.String),
            ((int)ScintillaNET.Style.Html.Other, StyleRole.Identifier),
            ((int)ScintillaNET.Style.Html.Comment, StyleRole.Comment),
            ((int)ScintillaNET.Style.Html.XcComment, StyleRole.Comment),
            ((int)ScintillaNET.Style.Html.Entity, StyleRole.MarkupEntity),
            ((int)ScintillaNET.Style.Html.Script, StyleRole.Embedded),
            ((int)ScintillaNET.Style.Html.Asp, StyleRole.Embedded),
            ((int)ScintillaNET.Style.Html.AspAt, StyleRole.Embedded),
            ((int)ScintillaNET.Style.Html.CData, StyleRole.String),
            ((int)ScintillaNET.Style.Html.Question, StyleRole.Operator),
        };

        /// <summary>
        /// Built lazily: the individual language properties below are static
        /// initializers, so enumerating them here must not run before they exist.
        /// </summary>
        public static IReadOnlyList<LanguageDef> All
        {
            get
            {
                if (_all == null)
                {
                    _all = new List<LanguageDef>
                    {
                        PlainText,
                        Cpp, CSharp, Java, Go, Rust,
                        JavaScript, TypeScript,
                        Python, Ruby, Lua, Php,
                        Html, Xml, Css, Json, Sql,
                        Markdown, Batch, PowerShell, Ini, Yaml
                    };
                    foreach (var lang in _all) _byId[lang.Id] = lang;
                }
                return _all;
            }
        }

        public static LanguageDef PlainText { get; } = new()
        {
            Id = "plaintext",
            DisplayName = "Plain Text",
            LexerName = "null",
            Extensions = Array.Empty<string>(),
            StyleMap = Array.Empty<(int, StyleRole)>(),
            Foldable = false
        };

        public static LanguageDef? ById(string id) =>
            _byId.TryGetValue(id, out var lang) ? lang : null;

        // ------------------------------------------------------------------
        // C family (all use the cpp lexer)
        // ------------------------------------------------------------------

        private const string CppLineComment = "//";

        public static LanguageDef Cpp { get; } = new()
        {
            Id = "cpp",
            DisplayName = "C / C++",
            LexerName = "cpp",
            Extensions = new[] { ".c", ".cc", ".cpp", ".cxx", ".h", ".hh", ".hpp", ".hxx", ".ino", ".tcc" },
            KeywordSets = new[]
            {
                // Primary keywords (SCE_C_WORD)
                "alignas alignof and and_eq asm auto break case catch class co_await co_return co_yield " +
                "compl concept const consteval constexpr constinit const_cast continue decltype default " +
                "delete do dynamic_cast else enum explicit export extern false for friend goto if inline " +
                "mutable namespace new noexcept not not_eq nullptr operator or or_eq private protected " +
                "public register reinterpret_cast requires return sizeof static static_assert static_cast " +
                "struct switch template this thread_local throw true try typedef typeid typename union " +
                "using virtual volatile while xor xor_eq override final",
                // Types (SCE_C_WORD2)
                "bool char char8_t char16_t char32_t double float int long short signed unsigned void " +
                "wchar_t size_t ssize_t int8_t int16_t int32_t int64_t uint8_t uint16_t uint32_t uint64_t " +
                "intptr_t uintptr_t ptrdiff_t",
            },
            StyleMap = CFamilyStyles,
            LineComment = CppLineComment,
            BlockComment = ("/*", "*/")
        };

        public static LanguageDef CSharp { get; } = new()
        {
            Id = "csharp",
            DisplayName = "C#",
            LexerName = "cpp",
            Extensions = new[] { ".cs" },
            KeywordSets = new[]
            {
                "abstract as async await base break case catch checked class const continue default " +
                "delegate do else enum event explicit extern false finally fixed for foreach get goto if " +
                "implicit in init interface internal is lock namespace new null operator out override " +
                "params partial private protected public readonly record ref return sealed set sizeof " +
                "stackalloc static struct switch this throw true try typeof unchecked unsafe using value " +
                "var virtual void volatile when where while yield add remove global alias and or not",
                "bool byte char decimal double dynamic float int long nint nuint object sbyte short " +
                "string uint ulong ushort"
            },
            StyleMap = CFamilyStyles,
            LineComment = CppLineComment,
            BlockComment = ("/*", "*/")
        };

        public static LanguageDef Java { get; } = new()
        {
            Id = "java",
            DisplayName = "Java",
            LexerName = "cpp",
            Extensions = new[] { ".java" },
            KeywordSets = new[]
            {
                "abstract assert break case catch class const continue default do else enum extends " +
                "final finally for goto if implements import instanceof interface native new package " +
                "private protected public return static strictfp super switch synchronized this throw " +
                "throws transient try var volatile while record sealed permits yield non-sealed",
                "boolean byte char double float int long short void"
            },
            StyleMap = CFamilyStyles,
            LineComment = CppLineComment,
            BlockComment = ("/*", "*/")
        };

        public static LanguageDef Go { get; } = new()
        {
            Id = "go",
            DisplayName = "Go",
            LexerName = "cpp",
            Extensions = new[] { ".go" },
            KeywordSets = new[]
            {
                "break case chan const continue default defer else fallthrough for func go goto if " +
                "import interface map package range return select struct switch type var",
                "bool byte complex64 complex128 error float32 float64 int int8 int16 int32 int64 rune " +
                "string uint uint8 uint16 uint32 uint64 uintptr any comparable"
            },
            StyleMap = CFamilyStyles,
            LineComment = CppLineComment,
            BlockComment = ("/*", "*/")
        };

        public static LanguageDef Rust { get; } = new()
        {
            Id = "rust",
            DisplayName = "Rust",
            LexerName = "rust",
            Extensions = new[] { ".rs" },
            KeywordSets = new[]
            {
                "as async await break const continue crate dyn else enum extern false fn for if impl " +
                "in let loop match mod move mut pub ref return self Self static struct super trait true " +
                "type unsafe use where while",
                "bool char f32 f64 i8 i16 i32 i64 i128 isize str u8 u16 u32 u64 u128 usize String Vec " +
                "Option Result Box Rc Arc Cow HashMap HashSet BTreeMap BTreeSet"
            },
            StyleMap = new[]
            {
                (ExtraStyles.RustDefault, StyleRole.Default),
                (ExtraStyles.RustCommentBlock, StyleRole.Comment),
                (ExtraStyles.RustCommentLine, StyleRole.Comment),
                (ExtraStyles.RustCommentBlockDoc, StyleRole.DocComment),
                (ExtraStyles.RustCommentLineDoc, StyleRole.DocComment),
                (ExtraStyles.RustString, StyleRole.String),
                (ExtraStyles.RustStringRaw, StyleRole.String),
                (ExtraStyles.RustCharacter, StyleRole.Character),
                (ExtraStyles.RustOperator, StyleRole.Operator),
                (ExtraStyles.RustIdentifier, StyleRole.Identifier),
                (ExtraStyles.RustLifetime, StyleRole.Operator),
                (ExtraStyles.RustMacro, StyleRole.Keyword),
                (ExtraStyles.RustLexError, StyleRole.Error),
                (ExtraStyles.RustKeyword, StyleRole.Keyword),
                (ExtraStyles.RustSecondaryKeyword, StyleRole.SecondaryKeyword),
                (ExtraStyles.RustTertiaryKeyword, StyleRole.SecondaryKeyword),
            },
            LineComment = CppLineComment,
            BlockComment = ("/*", "*/")
        };

        // ------------------------------------------------------------------
        // JavaScript / TypeScript (cpp lexer handles brace/ECMAScript syntax)
        // ------------------------------------------------------------------

        public static LanguageDef JavaScript { get; } = new()
        {
            Id = "javascript",
            DisplayName = "JavaScript",
            LexerName = "cpp",
            Extensions = new[] { ".js", ".jsx", ".mjs", ".cjs" },
            KeywordSets = new[]
            {
                "async await break case catch class const continue debugger default delete do else " +
                "enum export extends false finally for from function get if implements import in " +
                "instanceof interface let new null of private protected public return set static super " +
                "switch this throw true try typeof undefined var void while with yield",
                "boolean number string symbol object any unknown never bigint"
            },
            StyleMap = CFamilyStyles,
            LineComment = CppLineComment,
            BlockComment = ("/*", "*/")
        };

        public static LanguageDef TypeScript { get; } = new()
        {
            Id = "typescript",
            DisplayName = "TypeScript",
            LexerName = "cpp",
            Extensions = new[] { ".ts", ".tsx", ".mts", ".cts" },
            KeywordSets = new[]
            {
                "abstract any as async await boolean break case catch class const constructor continue " +
                "debugger declare default delete do else enum export extends false finally for from " +
                "function get if implements import in infer instanceof interface is keyof let module " +
                "namespace never new null number of object private protected public readonly return set " +
                "static string super switch symbol this throw true try type typeof undefined unique " +
                "unknown var void while with yield",
                "boolean number string symbol object any unknown never bigint"
            },
            StyleMap = CFamilyStyles,
            LineComment = CppLineComment,
            BlockComment = ("/*", "*/")
        };

        // ------------------------------------------------------------------
        // Python / Ruby / Lua / PHP
        // ------------------------------------------------------------------

        public static LanguageDef Python { get; } = new()
        {
            Id = "python",
            DisplayName = "Python",
            LexerName = "python",
            Extensions = new[] { ".py", ".pyw", ".pyi" },
            KeywordSets = new[]
            {
                "and as assert async await break class continue def del elif else except False finally " +
                "for from global if import in is lambda None nonlocal not or pass raise return True try " +
                "while with yield match case soft_keyword",
                "bool bytes dict float frozenset int list object set str tuple complex bytearray type"
            },
            StyleMap = new[]
            {
                ((int)ScintillaNET.Style.Python.Default, StyleRole.Default),
                ((int)ScintillaNET.Style.Python.CommentLine, StyleRole.Comment),
                ((int)ScintillaNET.Style.Python.CommentBlock, StyleRole.Comment),
                ((int)ScintillaNET.Style.Python.Number, StyleRole.Number),
                ((int)ScintillaNET.Style.Python.String, StyleRole.String),
                ((int)ScintillaNET.Style.Python.Character, StyleRole.Character),
                ((int)ScintillaNET.Style.Python.Triple, StyleRole.String),
                ((int)ScintillaNET.Style.Python.TripleDouble, StyleRole.String),
                ((int)ScintillaNET.Style.Python.StringEol, StyleRole.Error),
                ((int)ScintillaNET.Style.Python.Word, StyleRole.Keyword),
                ((int)ScintillaNET.Style.Python.Word2, StyleRole.SecondaryKeyword),
                ((int)ScintillaNET.Style.Python.ClassName, StyleRole.ClassName),
                ((int)ScintillaNET.Style.Python.DefName, StyleRole.FunctionName),
                ((int)ScintillaNET.Style.Python.Operator, StyleRole.Operator),
                ((int)ScintillaNET.Style.Python.Identifier, StyleRole.Identifier),
                ((int)ScintillaNET.Style.Python.Decorator, StyleRole.Directive),
            },
            LineComment = "#"
        };

        public static LanguageDef Ruby { get; } = new()
        {
            Id = "ruby",
            DisplayName = "Ruby",
            LexerName = "ruby",
            Extensions = new[] { ".rb", ".rake", ".gemspec", ".ru" },
            KeywordSets = new[]
            {
                "alias and begin break case class def defined? do else elsif end ensure false for if in " +
                "module next nil not or redo rescue retry return self super then true undef unless until " +
                "when while yield __method__ require require_relative include extend attr_accessor " +
                "attr_reader attr_writer raise new proc lambda puts print p gets loop"
            },
            StyleMap = new[]
            {
                ((int)ScintillaNET.Style.Ruby.Default, StyleRole.Default),
                ((int)ScintillaNET.Style.Ruby.Error, StyleRole.Error),
                ((int)ScintillaNET.Style.Ruby.CommentLine, StyleRole.Comment),
                ((int)ScintillaNET.Style.Ruby.Pod, StyleRole.Comment),
                ((int)ScintillaNET.Style.Ruby.DataSection, StyleRole.Comment),
                ((int)ScintillaNET.Style.Ruby.Number, StyleRole.Number),
                ((int)ScintillaNET.Style.Ruby.Word, StyleRole.Keyword),
                ((int)ScintillaNET.Style.Ruby.String, StyleRole.String),
                ((int)ScintillaNET.Style.Ruby.Character, StyleRole.Character),
                ((int)ScintillaNET.Style.Ruby.ClassName, StyleRole.ClassName),
                ((int)ScintillaNET.Style.Ruby.DefName, StyleRole.FunctionName),
                ((int)ScintillaNET.Style.Ruby.ModuleName, StyleRole.ClassName),
                ((int)ScintillaNET.Style.Ruby.Operator, StyleRole.Operator),
                ((int)ScintillaNET.Style.Ruby.Identifier, StyleRole.Identifier),
                ((int)ScintillaNET.Style.Ruby.Regex, StyleRole.Regex),
                ((int)ScintillaNET.Style.Ruby.Global, StyleRole.VariableName),
                ((int)ScintillaNET.Style.Ruby.Symbol, StyleRole.Symbol),
                ((int)ScintillaNET.Style.Ruby.InstanceVar, StyleRole.VariableName),
                ((int)ScintillaNET.Style.Ruby.ClassVar, StyleRole.VariableName),
                ((int)ScintillaNET.Style.Ruby.BackTicks, StyleRole.String),
                ((int)ScintillaNET.Style.Ruby.HereDelim, StyleRole.Operator),
                ((int)ScintillaNET.Style.Ruby.HereQ, StyleRole.String),
                ((int)ScintillaNET.Style.Ruby.HereQq, StyleRole.String),
                ((int)ScintillaNET.Style.Ruby.HereQx, StyleRole.String),
                ((int)ScintillaNET.Style.Ruby.StringQ, StyleRole.String),
                ((int)ScintillaNET.Style.Ruby.StringQq, StyleRole.String),
                ((int)ScintillaNET.Style.Ruby.StringQx, StyleRole.String),
                ((int)ScintillaNET.Style.Ruby.StringQr, StyleRole.String),
                ((int)ScintillaNET.Style.Ruby.StringQw, StyleRole.String),
            },
            LineComment = "#"
        };

        public static LanguageDef Lua { get; } = new()
        {
            Id = "lua",
            DisplayName = "Lua",
            LexerName = "lua",
            Extensions = new[] { ".lua" },
            KeywordSets = new[]
            {
                "and break do else elseif end false for function goto if in local nil not or repeat " +
                "return then true until while",
                "print type pairs ipairs tostring tonumber require string table math io os coroutine " +
                "debug package select next pcall xpcall error assert setmetatable getmetatable rawget " +
                "rawset rawequal rawlen self"
            },
            StyleMap = new[]
            {
                ((int)ScintillaNET.Style.Lua.Default, StyleRole.Default),
                ((int)ScintillaNET.Style.Lua.Comment, StyleRole.Comment),
                ((int)ScintillaNET.Style.Lua.CommentLine, StyleRole.Comment),
                ((int)ScintillaNET.Style.Lua.CommentDoc, StyleRole.DocComment),
                ((int)ScintillaNET.Style.Lua.Number, StyleRole.Number),
                ((int)ScintillaNET.Style.Lua.Word, StyleRole.Keyword),
                ((int)ScintillaNET.Style.Lua.Word2, StyleRole.SecondaryKeyword),
                ((int)ScintillaNET.Style.Lua.Word3, StyleRole.SecondaryKeyword),
                ((int)ScintillaNET.Style.Lua.Word4, StyleRole.SecondaryKeyword),
                ((int)ScintillaNET.Style.Lua.Word5, StyleRole.SecondaryKeyword),
                ((int)ScintillaNET.Style.Lua.Word6, StyleRole.SecondaryKeyword),
                ((int)ScintillaNET.Style.Lua.Word7, StyleRole.SecondaryKeyword),
                ((int)ScintillaNET.Style.Lua.Word8, StyleRole.SecondaryKeyword),
                ((int)ScintillaNET.Style.Lua.String, StyleRole.String),
                ((int)ScintillaNET.Style.Lua.Character, StyleRole.Character),
                ((int)ScintillaNET.Style.Lua.LiteralString, StyleRole.String),
                ((int)ScintillaNET.Style.Lua.StringEol, StyleRole.Error),
                ((int)ScintillaNET.Style.Lua.Preprocessor, StyleRole.Preprocessor),
                ((int)ScintillaNET.Style.Lua.Operator, StyleRole.Operator),
                ((int)ScintillaNET.Style.Lua.Identifier, StyleRole.Identifier),
                ((int)ScintillaNET.Style.Lua.Label, StyleRole.FunctionName),
            },
            LineComment = "--",
            BlockComment = ("--[[", "]]")
        };

        public static LanguageDef Php { get; } = new()
        {
            Id = "php",
            DisplayName = "PHP",
            LexerName = "hypertext",
            Extensions = new[] { ".php", ".phtml" },
            KeywordSets = new[]
            {
                // Slot 0: HTML elements (hypertext lexer)
                "html head title base link meta style script noscript body section nav article aside " +
                "header footer main address h1 h2 h3 h4 h5 h6 blockquote ol ul li dl dt dd figure " +
                "figcaption div p br hr pre code kbd samp var cite q abbr dfn small strong em b i u s " +
                "mark del ins sub sup ruby rt rp bdi bdo span a img picture audio video source track " +
                "embed iframe object param canvas map area table caption colgroup col tbody thead tfoot " +
                "tr td th form fieldset legend label input button select datalist optgroup option " +
                "textarea output progress meter details summary dialog menu template slot",
                null!, null!, null!,
                // Slot 4: PHP keywords
                "abstract and array as break callable case catch class clone const continue declare " +
                "default do echo else elseif empty enddeclare endfor endforeach endif endswitch endwhile " +
                "enum extends final finally fn for foreach function global goto if implements include " +
                "include_once instanceof insteadof interface isset list match namespace new or print " +
                "private protected public readonly require require_once return static switch throw trait " +
                "try unset use var while xor yield true false null",
            }.Where(k => k != null).Cast<string>().ToArray(),
            StyleMap = HtmlStyles,
            LineComment = "//",
            BlockComment = ("/*", "*/")
        };

        // ------------------------------------------------------------------
        // Markup and data formats
        // ------------------------------------------------------------------

        public static LanguageDef Html { get; } = new()
        {
            Id = "html",
            DisplayName = "HTML",
            LexerName = "hypertext",
            Extensions = new[] { ".html", ".htm", ".xhtml" },
            KeywordSets = new[]
            {
                Php.KeywordSets[0]
            },
            StyleMap = HtmlStyles,
            BlockComment = ("<!--", "-->")
        };

        public static LanguageDef Xml { get; } = new()
        {
            Id = "xml",
            DisplayName = "XML",
            LexerName = "xml",
            Extensions = new[] { ".xml", ".xsl", ".xslt", ".svg", ".xaml", ".csproj", ".vbproj",
                                 ".resx", ".wsdl", ".plist", ".config", ".manifest", ".axml" },
            KeywordSets = Array.Empty<string>(),
            StyleMap = HtmlStyles,
            BlockComment = ("<!--", "-->")
        };

        public static LanguageDef Css { get; } = new()
        {
            Id = "css",
            DisplayName = "CSS",
            LexerName = "css",
            Extensions = new[] { ".css", ".scss", ".less" },
            KeywordSets = Array.Empty<string>(),
            StyleMap = new[]
            {
                ((int)ScintillaNET.Style.Css.Default, StyleRole.Default),
                ((int)ScintillaNET.Style.Css.Tag, StyleRole.MarkupTag),
                ((int)ScintillaNET.Style.Css.Class, StyleRole.ClassName),
                ((int)ScintillaNET.Style.Css.Id, StyleRole.Keyword),
                ((int)ScintillaNET.Style.Css.PseudoClass, StyleRole.Keyword),
                ((int)ScintillaNET.Style.Css.ExtendedPseudoClass, StyleRole.Keyword),
                ((int)ScintillaNET.Style.Css.PseudoElement, StyleRole.Keyword),
                ((int)ScintillaNET.Style.Css.ExtendedPseudoElement, StyleRole.Keyword),
                ((int)ScintillaNET.Style.Css.UnknownPseudoClass, StyleRole.Error),
                ((int)ScintillaNET.Style.Css.Operator, StyleRole.Operator),
                ((int)ScintillaNET.Style.Css.Identifier, StyleRole.Identifier),
                ((int)ScintillaNET.Style.Css.Identifier2, StyleRole.Identifier),
                ((int)ScintillaNET.Style.Css.Identifier3, StyleRole.Identifier),
                ((int)ScintillaNET.Style.Css.UnknownIdentifier, StyleRole.Error),
                ((int)ScintillaNET.Style.Css.ExtendedIdentifier, StyleRole.Identifier),
                ((int)ScintillaNET.Style.Css.Value, StyleRole.Default),
                ((int)ScintillaNET.Style.Css.Comment, StyleRole.Comment),
                ((int)ScintillaNET.Style.Css.Important, StyleRole.Directive),
                ((int)ScintillaNET.Style.Css.Directive, StyleRole.Directive),
                ((int)ScintillaNET.Style.Css.DoubleString, StyleRole.String),
                ((int)ScintillaNET.Style.Css.SingleString, StyleRole.String),
                ((int)ScintillaNET.Style.Css.Attribute, StyleRole.MarkupAttribute),
                ((int)ScintillaNET.Style.Css.Variable, StyleRole.Variable),
                ((int)ScintillaNET.Style.Css.GroupRule, StyleRole.Keyword),
            },
            LineComment = null,
            BlockComment = ("/*", "*/")
        };

        public static LanguageDef Json { get; } = new()
        {
            Id = "json",
            DisplayName = "JSON",
            LexerName = "json",
            Extensions = new[] { ".json", ".jsonc", ".json5" },
            KeywordSets = new[] { "true false null" },
            StyleMap = new[]
            {
                ((int)ScintillaNET.Style.Json.Default, StyleRole.Default),
                ((int)ScintillaNET.Style.Json.Number, StyleRole.Number),
                ((int)ScintillaNET.Style.Json.String, StyleRole.String),
                ((int)ScintillaNET.Style.Json.StringEol, StyleRole.Error),
                ((int)ScintillaNET.Style.Json.PropertyName, StyleRole.PropertyKey),
                ((int)ScintillaNET.Style.Json.EscapeSequence, StyleRole.Character),
                ((int)ScintillaNET.Style.Json.LineComment, StyleRole.Comment),
                ((int)ScintillaNET.Style.Json.BlockComment, StyleRole.Comment),
                ((int)ScintillaNET.Style.Json.Operator, StyleRole.Operator),
                ((int)ScintillaNET.Style.Json.Uri, StyleRole.Link),
                ((int)ScintillaNET.Style.Json.CompactIRI, StyleRole.Link),
                ((int)ScintillaNET.Style.Json.Keyword, StyleRole.Keyword),
                ((int)ScintillaNET.Style.Json.LdKeyword, StyleRole.SecondaryKeyword),
                ((int)ScintillaNET.Style.Json.Error, StyleRole.Error),
            },
            Foldable = false
        };

        public static LanguageDef Sql { get; } = new()
        {
            Id = "sql",
            DisplayName = "SQL",
            LexerName = "sql",
            Extensions = new[] { ".sql" },
            KeywordSets = new[]
            {
                "select from where insert update delete into values create table drop alter add column " +
                "primary key foreign references constraint index view trigger procedure function begin " +
                "end commit rollback transaction grant revoke as on join inner left right outer full " +
                "cross union distinct group by order having limit offset fetch asc desc and or not in " +
                "exists between like is null default unique check cascade case when then else while if " +
                "declare exec execute return cursor open close database schema truncate with recursive " +
                "over partition rows range preceding following unbounded current ascending descending",
                "int integer smallint bigint numeric decimal real double float precision char varchar " +
                "nvarchar text date time timestamp interval boolean bool blob clob binary uuid json " +
                "jsonb serial bigserial smallserial money xml array"
            },
            StyleMap = new[]
            {
                ((int)ScintillaNET.Style.Sql.Default, StyleRole.Default),
                ((int)ScintillaNET.Style.Sql.Comment, StyleRole.Comment),
                ((int)ScintillaNET.Style.Sql.CommentLine, StyleRole.Comment),
                ((int)ScintillaNET.Style.Sql.CommentDoc, StyleRole.DocComment),
                ((int)ScintillaNET.Style.Sql.CommentLineDoc, StyleRole.DocComment),
                ((int)ScintillaNET.Style.Sql.CommentDocKeyword, StyleRole.DocComment),
                ((int)ScintillaNET.Style.Sql.CommentDocKeywordError, StyleRole.Error),
                ((int)ScintillaNET.Style.Sql.SqlPlusComment, StyleRole.Comment),
                ((int)ScintillaNET.Style.Sql.Number, StyleRole.Number),
                ((int)ScintillaNET.Style.Sql.Word, StyleRole.Keyword),
                ((int)ScintillaNET.Style.Sql.Word2, StyleRole.SecondaryKeyword),
                ((int)ScintillaNET.Style.Sql.String, StyleRole.String),
                ((int)ScintillaNET.Style.Sql.Character, StyleRole.Character),
                ((int)ScintillaNET.Style.Sql.Operator, StyleRole.Operator),
                ((int)ScintillaNET.Style.Sql.QOperator, StyleRole.Operator),
                ((int)ScintillaNET.Style.Sql.Identifier, StyleRole.Identifier),
                ((int)ScintillaNET.Style.Sql.QuotedIdentifier, StyleRole.String),
                ((int)ScintillaNET.Style.Sql.SqlPlus, StyleRole.Identifier),
                ((int)ScintillaNET.Style.Sql.SqlPlusPrompt, StyleRole.Identifier),
                ((int)ScintillaNET.Style.Sql.User1, StyleRole.Keyword),
                ((int)ScintillaNET.Style.Sql.User2, StyleRole.Keyword),
                ((int)ScintillaNET.Style.Sql.User3, StyleRole.Keyword),
                ((int)ScintillaNET.Style.Sql.User4, StyleRole.Keyword),
            },
            LineComment = "--",
            BlockComment = ("/*", "*/")
        };

        public static LanguageDef Markdown { get; } = new()
        {
            Id = "markdown",
            DisplayName = "Markdown",
            LexerName = "markdown",
            Extensions = new[] { ".md", ".markdown", ".mkd" },
            KeywordSets = Array.Empty<string>(),
            StyleMap = new[]
            {
                ((int)ScintillaNET.Style.Markdown.Default, StyleRole.Default),
                ((int)ScintillaNET.Style.Markdown.LineBegin, StyleRole.Operator),
                ((int)ScintillaNET.Style.Markdown.Strong1, StyleRole.Bold),
                ((int)ScintillaNET.Style.Markdown.Strong2, StyleRole.Bold),
                ((int)ScintillaNET.Style.Markdown.Em1, StyleRole.Italic),
                ((int)ScintillaNET.Style.Markdown.Em2, StyleRole.Italic),
                ((int)ScintillaNET.Style.Markdown.Header1, StyleRole.Header),
                ((int)ScintillaNET.Style.Markdown.Header2, StyleRole.Header),
                ((int)ScintillaNET.Style.Markdown.Header3, StyleRole.Header),
                ((int)ScintillaNET.Style.Markdown.Header4, StyleRole.Header),
                ((int)ScintillaNET.Style.Markdown.Header5, StyleRole.Header),
                ((int)ScintillaNET.Style.Markdown.Header6, StyleRole.Header),
                ((int)ScintillaNET.Style.Markdown.PreChar, StyleRole.CodeInline),
                ((int)ScintillaNET.Style.Markdown.UListItem, StyleRole.ListMarker),
                ((int)ScintillaNET.Style.Markdown.OListItem, StyleRole.ListMarker),
                ((int)ScintillaNET.Style.Markdown.BlockQuote, StyleRole.BlockQuote),
                ((int)ScintillaNET.Style.Markdown.Strikeout, StyleRole.Strikeout),
                ((int)ScintillaNET.Style.Markdown.HRule, StyleRole.HRule),
                ((int)ScintillaNET.Style.Markdown.Link, StyleRole.Link),
                ((int)ScintillaNET.Style.Markdown.Code, StyleRole.CodeInline),
                ((int)ScintillaNET.Style.Markdown.Code2, StyleRole.CodeInline),
                ((int)ScintillaNET.Style.Markdown.CodeBk, StyleRole.CodeInline),
            },
            Foldable = false
        };

        // ------------------------------------------------------------------
        // Shell / scripting / config
        // ------------------------------------------------------------------

        public static LanguageDef Batch { get; } = new()
        {
            Id = "batch",
            DisplayName = "Batch",
            LexerName = "batch",
            Extensions = new[] { ".bat", ".cmd" },
            KeywordSets = new[]
            {
                "echo set if else for in do goto call exit pause rem cls color title cd md rd del copy " +
                "move ren rename type find findstr sort more ver vol label path prompt shift start " +
                "waitfor errorlevel exist not defined equ neq lss leq gtr geq pushd popd setlocal " +
                "endlocal enabledelayedexpansion enableextensions off on assoc chcp chdir dpath erase " +
                "ftype mklink rmdir subst time date driverquery fc attrib expand reg sc schtasks tree"
            },
            StyleMap = new[]
            {
                ((int)ScintillaNET.Style.Batch.Default, StyleRole.Default),
                ((int)ScintillaNET.Style.Batch.Comment, StyleRole.Comment),
                ((int)ScintillaNET.Style.Batch.Word, StyleRole.Keyword),
                ((int)ScintillaNET.Style.Batch.Label, StyleRole.FunctionName),
                ((int)ScintillaNET.Style.Batch.Hide, StyleRole.Error),
                ((int)ScintillaNET.Style.Batch.Command, StyleRole.Keyword),
                ((int)ScintillaNET.Style.Batch.Identifier, StyleRole.Identifier),
                ((int)ScintillaNET.Style.Batch.Operator, StyleRole.Operator),
            },
            LineComment = "rem",
            Foldable = false
        };

        public static LanguageDef PowerShell { get; } = new()
        {
            Id = "powershell",
            DisplayName = "PowerShell",
            LexerName = "powershell",
            Extensions = new[] { ".ps1", ".psm1", ".psd1" },
            KeywordSets = new[]
            {
                "if elseif else switch foreach for while do until break continue return throw trap try " +
                "catch finally begin process end filter function param in exit class enum hidden static " +
                "base this from data default define dynamicparam"
            },
            StyleMap = new[]
            {
                ((int)ScintillaNET.Style.PowerShell.Default, StyleRole.Default),
                ((int)ScintillaNET.Style.PowerShell.Comment, StyleRole.Comment),
                ((int)ScintillaNET.Style.PowerShell.CommentStream, StyleRole.Comment),
                ((int)ScintillaNET.Style.PowerShell.CommentDocKeyword, StyleRole.DocComment),
                ((int)ScintillaNET.Style.PowerShell.String, StyleRole.String),
                ((int)ScintillaNET.Style.PowerShell.HereString, StyleRole.String),
                ((int)ScintillaNET.Style.PowerShell.Character, StyleRole.Character),
                ((int)ScintillaNET.Style.PowerShell.HereCharacter, StyleRole.Character),
                ((int)ScintillaNET.Style.PowerShell.Number, StyleRole.Number),
                ((int)ScintillaNET.Style.PowerShell.Variable, StyleRole.Variable),
                ((int)ScintillaNET.Style.PowerShell.Operator, StyleRole.Operator),
                ((int)ScintillaNET.Style.PowerShell.Identifier, StyleRole.Identifier),
                ((int)ScintillaNET.Style.PowerShell.Keyword, StyleRole.Keyword),
                ((int)ScintillaNET.Style.PowerShell.Cmdlet, StyleRole.Cmdlet),
                ((int)ScintillaNET.Style.PowerShell.Alias, StyleRole.FunctionName),
                ((int)ScintillaNET.Style.PowerShell.Function, StyleRole.FunctionName),
                ((int)ScintillaNET.Style.PowerShell.User1, StyleRole.SecondaryKeyword),
            },
            LineComment = "#",
            Foldable = true
        };

        public static LanguageDef Ini { get; } = new()
        {
            Id = "ini",
            DisplayName = "INI",
            LexerName = "props",
            Extensions = new[] { ".ini", ".cfg", ".conf", ".properties", ".env", ".gitignore", ".editorconfig" },
            KeywordSets = Array.Empty<string>(),
            StyleMap = new[]
            {
                ((int)ScintillaNET.Style.Properties.Default, StyleRole.Default),
                ((int)ScintillaNET.Style.Properties.Comment, StyleRole.Comment),
                ((int)ScintillaNET.Style.Properties.Section, StyleRole.Section),
                ((int)ScintillaNET.Style.Properties.Assignment, StyleRole.Operator),
                ((int)ScintillaNET.Style.Properties.DefVal, StyleRole.Default),
                ((int)ScintillaNET.Style.Properties.Key, StyleRole.PropertyKey),
            },
            LineComment = "#",
            Foldable = false
        };

        public static LanguageDef Yaml { get; } = new()
        {
            Id = "yaml",
            DisplayName = "YAML",
            LexerName = "yaml",
            Extensions = new[] { ".yml", ".yaml" },
            KeywordSets = new[] { "true false yes no on off null" },
            StyleMap = new[]
            {
                (ExtraStyles.YamlDefault, StyleRole.Default),
                (ExtraStyles.YamlComment, StyleRole.Comment),
                (ExtraStyles.YamlIdentifier, StyleRole.PropertyKey),
                (ExtraStyles.YamlKeyword, StyleRole.Keyword),
                (ExtraStyles.YamlNumber, StyleRole.Number),
                (ExtraStyles.YamlReference, StyleRole.Link),
                (ExtraStyles.YamlDocument, StyleRole.Directive),
                (ExtraStyles.YamlText, StyleRole.Default),
                (ExtraStyles.YamlError, StyleRole.Error),
                (ExtraStyles.YamlOperator, StyleRole.Operator),
                (ExtraStyles.YamlKey, StyleRole.PropertyKey),
            },
            LineComment = "#",
            Foldable = false
        };
    }
}
