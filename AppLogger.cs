using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Threading;

namespace PcAudioRecorder
{
    public class AppLogger
    {
        private const int MaxVisibleEntries = 80;
        private readonly Dispatcher? _dispatcher;
        private readonly string _logDirectory;

        public static AppLogger? Shared { get; private set; }
        public ObservableCollection<string> Entries { get; } = new ObservableCollection<string>();

        public AppLogger(Dispatcher? dispatcher = null, string? logDirectory = null)
        {
            _dispatcher = dispatcher;
            _logDirectory = logDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PcAudioRecorder",
                "logs");
            Shared = this;
        }

        public void Info(string message) => Write("INFO", message);

        public void Warn(string message, Exception? exception = null) => Write("WARN", message, exception);

        public void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);

        private void Write(string level, string message, Exception? exception = null)
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
            if (exception != null)
                line += $" ({exception.GetType().Name}: {exception.Message})";

            AppendVisibleEntry(line);
            AppendLogFile(line, exception);
        }

        private void AppendVisibleEntry(string line)
        {
            void Add()
            {
                Entries.Insert(0, line);
                while (Entries.Count > MaxVisibleEntries)
                    Entries.RemoveAt(Entries.Count - 1);
            }

            if (_dispatcher?.CheckAccess() == false)
                _dispatcher.BeginInvoke(Add);
            else
                Add();
        }

        private void AppendLogFile(string line, Exception? exception)
        {
            try
            {
                Directory.CreateDirectory(_logDirectory);
                var path = Path.Combine(_logDirectory, $"app-{DateTime.Now:yyyyMMdd}.log");
                var text = exception == null ? line : line + Environment.NewLine + exception;
                File.AppendAllText(path, text + Environment.NewLine);
            }
            catch
            {
                // Logging must not interrupt recording.
            }
        }
    }
}
