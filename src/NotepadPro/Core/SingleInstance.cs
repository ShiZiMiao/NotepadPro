using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace NotepadPro
{
    /// <summary>
    /// Single-instance support: a named mutex marks the running instance and a
    /// named pipe forwards file arguments from later launches to it.
    /// </summary>
    internal static class SingleInstance
    {
        private const string MutexName = "Local\\NotepadPro.SingleInstanceMutex";
        private const string PipeName = "NotepadPro.OpenPipe";

        private static Mutex? _mutex;

        public static bool TryForwardToRunningInstance(string[] args)
        {
            _mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
            if (createdNew)
            {
                return false; // We are the first instance; keep the mutex.
            }

            // Another instance owns the mutex - forward the file list to it.
            _mutex.Dispose();
            _mutex = null;
            try
            {
                using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                client.Connect(2000);
                var paths = args.Where(a => !a.StartsWith('-')).ToArray();
                using var writer = new StreamWriter(client) { AutoFlush = true };
                writer.WriteLine(JsonSerializer.Serialize(paths));
                return true;
            }
            catch
            {
                // Could not reach the running instance; open a new one.
                return false;
            }
        }

        /// <summary>
        /// Returns the pipe server that listens for forwarded files, or null when
        /// this process is not the owner (should not happen on the normal path).
        /// </summary>
        public static PipeServer? Acquire(out Mutex heldMutex)
        {
            heldMutex = _mutex ??= new Mutex(true, MutexName, out _);
            return new PipeServer();
        }

        /// <summary>Background named-pipe server receiving file paths from later launches.</summary>
        public sealed class PipeServer : IDisposable
        {
            private Thread? _thread;
            private volatile bool _running;

            public void Start()
            {
                _running = true;
                _thread = new Thread(ListenLoop) { IsBackground = true };
                _thread.Start();
            }

            private void ListenLoop()
            {
                while (_running)
                {
                    try
                    {
                        using var server = new NamedPipeServerStream(PipeName, PipeDirection.In);
                        server.WaitForConnection();
                        using var reader = new StreamReader(server);
                        var line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var paths = JsonSerializer.Deserialize<string[]>(line) ?? Array.Empty<string>();
                        var form = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
                        form?.BeginInvoke(new Action(() => form.OpenFilesFromArguments(paths)));
                    }
                    catch
                    {
                        // Client disconnected or pipe broken; keep serving.
                    }
                }
            }

            public void Dispose() => _running = false;
        }
    }
}
