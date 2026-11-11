using System;
using System.IO;

namespace Spark.Tag4D.Core.Models
{
    /// <summary>Simple thread-safe logger that writes to the console and a time-stamped file.</summary>
    public sealed class Log
    {
        private readonly string _logFilePath;
        private readonly object _gate = new object();

        public Log(string logFolderPath, string logFilePrefix)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            _logFilePath = Path.Combine(logFolderPath, $"{logFilePrefix}_{timestamp}.log");
        }

        public void Information(string message) => Write("INFO", message);

        public void Warning(string message) => Write("WARNING", message);

        public void Error(string message) => Write("ERROR", message);

        public void LogError(string message, Exception exception)
        {
            Write("ERROR", $"{message}{Environment.NewLine}{exception}");
        }

        private void Write(string level, string message)
        {
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
            Console.WriteLine(line);

            lock (_gate)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath)!);
                    File.AppendAllText(_logFilePath, line + Environment.NewLine);
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                    Console.Error.WriteLine($"[Logger] Could not write to '{_logFilePath}': {exception.Message}");
                }
            }
        }
    }
}
