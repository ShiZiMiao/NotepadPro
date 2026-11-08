using NotepadPro;

namespace NotepadPro
{
    internal static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            // Enable ANSI code pages (GBK/GB18030 etc.) on .NET 8.
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            ApplicationConfiguration.Initialize();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) => LogException(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                LogException(e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString()));

            // Single instance: forward file arguments to the running instance.
            if (SingleInstance.TryForwardToRunningInstance(args))
            {
                return 0;
            }

            var pipeServer = SingleInstance.Acquire(out var mutex);
            using (mutex)
            {
                using (pipeServer)
                {
                    var form = new MainForm(args);
                    pipeServer?.Start();
                    Application.Run(form);
                }
            }
            return 0;
        }

        /// <summary>Appends unexpected exceptions to a log file for diagnosis.</summary>
        private static void LogException(Exception ex)
        {
            try
            {
                var dir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NotepadPro");
                System.IO.Directory.CreateDirectory(dir);
                File.AppendAllText(System.IO.Path.Combine(dir, "error.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\r\n\r\n");
            }
            catch
            {
                // Logging must never throw.
            }
        }
    }
}
