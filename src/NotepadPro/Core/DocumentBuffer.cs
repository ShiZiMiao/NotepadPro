using NotepadPro.Languages;
using ScintillaNET;

namespace NotepadPro
{
    /// <summary>
    /// One open document: a Scintilla editor plus its file, encoding, EOL,
    /// language and dirty state. Each document owns its own editor control so
    /// undo history, selection and scroll position survive tab switching.
    /// </summary>
    public class DocumentBuffer : IDisposable
    {
        private static int _untitledCounter;

        public Scintilla Editor { get; }
        public string? FilePath { get; set; }
        public TextEncoding Encoding { get; set; } = TextEncoding.Utf8;
        public bool IsDirty { get; private set; }
        public LanguageDef Language { get; set; }

        /// <summary>Raised when the dirty state changes (update tab and title).</summary>
        public event Action? DirtyChanged;

        public DocumentBuffer()
        {
            Editor = new Scintilla();
            IsDirty = false;
            Language = LanguageCatalog.PlainText;
            _untitledCounter++;
            UntitledNumber = _untitledCounter;

            Editor.SavePointLeft += (_, _) => SetDirty(true);
            Editor.SavePointReached += (_, _) => SetDirty(false);
        }

        /// <summary>Sequence number for "new 1"-style titles.</summary>
        public int UntitledNumber { get; }

        public bool IsUntitled => FilePath == null;

        public string Title
        {
            get
            {
                if (FilePath != null) return Path.GetFileName(FilePath);
                return L.Get("tab.untitled") + UntitledNumber;
            }
        }

        /// <summary>Full display path, or the pseudo name for untitled buffers.</summary>
        public string DisplayPath => FilePath ?? Title;

        public void SetDirty(bool dirty)
        {
            if (IsDirty == dirty) return;
            IsDirty = dirty;
            DirtyChanged?.Invoke();
        }

        /// <summary>Reads the file, detects encoding/EOL, fills the editor.</summary>
        public void LoadFrom(string path)
        {
            FilePath = path;
            byte[] bytes = File.ReadAllBytes(path);
            (Encoding, var text) = EncodingDetector.Decode(bytes);

            Editor.ReadOnly = false;
            Editor.Text = text;
            Editor.SetSavePoint();
            Editor.EmptyUndoBuffer();
            Editor.EolMode = EncodingDetector.DetectEol(text) switch
            {
                TextEol.Lf => Eol.Lf,
                TextEol.Cr => Eol.Cr,
                _ => Eol.CrLf
            };
            SetDirty(false);
        }

        /// <summary>Writes the buffer with its recorded encoding; returns false when the write failed.</summary>
        public bool SaveTo(string path)
        {
            try
            {
                var text = Editor.Text;
                File.WriteAllBytes(path, EncodingDetector.Encode(text, Encoding));
                FilePath = path;
                Editor.SetSavePoint();
                SetDirty(false);
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show(
                    string.Format(L.Get("msg.save.failed.body"), path, ex.Message),
                    L.Get("msg.save.failed.title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>Replaces the buffer text (encoding conversion path), keeping the caret at home.</summary>
        public void ReplaceText(string text)
        {
            var caret = Editor.CurrentPosition;
            var firstVisible = Editor.FirstVisibleLine;
            Editor.Text = text;
            Editor.SetSavePoint();
            SetDirty(false);
            Editor.CurrentPosition = Math.Min(caret, Editor.TextLength);
            Editor.FirstVisibleLine = firstVisible;
        }

        /// <summary>Applies the language definition to the editor (lexer + keywords).</summary>
        public void ApplyLanguage()
        {
            Language.Apply(Editor);
        }

        public void Dispose()
        {
            Editor.Dispose();
        }
    }
}
