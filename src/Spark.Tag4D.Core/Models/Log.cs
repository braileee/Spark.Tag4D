using System;
using System.IO;

namespace Spark.Tag4D.Core.Models
{
    public class Log
    {
        private readonly string _logFilePath;
        private readonly object _lockObject = new object(); // For thread-safe file writing

        /// <summary>
        /// Initializes a new instance of the CustomLogger class.
        /// A log file will be created with the specified prefix and a timestamp.
        /// </summary>
        /// <param name="logFilePrefix">The prefix for the log file name (e.g., "MyApp").</param>
        public Log(string logFolderPath, string logFilePrefix)
        {
            // Generate the log file name with prefix and current timestamp (including milliseconds)
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            _logFilePath = Path.Combine(logFolderPath, $"{logFilePrefix}_{timestamp}.log");

            try
            {
                // Create a StreamWriter to write to the log file.
                // true for append mode, false for overwrite (we want to overwrite for new instance)
                Console.WriteLine($"[Logger] Log file created at: {_logFilePath}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Logger] ERROR: Could not create log file at '{_logFilePath}'. Exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Information(string message)
        {
            WriteLog("INFO", message);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Warning(string message)
        {
            WriteLog("WARNING", message);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Error(string message)
        {
            WriteLog("ERROR", message);
        }

        /// <summary>
        /// Logs an error message with an associated exception.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">The exception to log.</param>
        public void LogError(string message, Exception ex)
        {
            WriteLog("ERROR", $"{message}{Environment.NewLine}Exception: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}");
        }

        /// <summary>
        /// Writes the log message to the console and the log file.
        /// This method is thread-safe.
        /// </summary>
        /// <param name="level">The log level (e.g., INFO, WARNING, ERROR).</param>
        /// <param name="message">The message content.</param>
        private void WriteLog(string level, string message)
        {
            // Format the log message
            string formattedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";

            // Write to console
            Console.WriteLine(formattedMessage);

            // Write to file (thread-safe)
            lock (_lockObject)
            {
                try
                {
                    File.AppendAllLines(_logFilePath, new[] { formattedMessage });
                }
                catch (ObjectDisposedException)
                {
                    Console.Error.WriteLine($"[Logger] ERROR: Attempted to write to a disposed log file writer.");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Logger] ERROR: Could not write to log file. Exception: {ex.Message}");
                }
            }
        }
    }
}
