using System;

namespace Spark.Tag4D.Revit.App.Models
{
    public enum LogLevel
    {
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>Lets a long running Revit operation report back to the window that started it.</summary>
    public sealed class OperationProgress
    {
        public Action<string, LogLevel> OnMessage { get; set; }

        public Action<int, int> OnProgress { get; set; }

        public Action OnCompleted { get; set; }

        public void Message(string text, LogLevel level = LogLevel.Info) => OnMessage?.Invoke(text, level);

        public void Report(int done, int total) => OnProgress?.Invoke(done, total);

        public void Completed() => OnCompleted?.Invoke();
    }
}
