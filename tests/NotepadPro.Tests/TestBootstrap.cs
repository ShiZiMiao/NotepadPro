using System.Runtime.CompilerServices;
using System.Text;

namespace NotepadPro.Tests
{
    /// <summary>
    /// Runs once before any test: registers the ANSI code pages (GBK) so the
    /// encoding tests behave exactly like the running application.
    /// </summary>
    internal static class TestBootstrap
    {
        [ModuleInitializer]
        internal static void Init()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
    }
}
