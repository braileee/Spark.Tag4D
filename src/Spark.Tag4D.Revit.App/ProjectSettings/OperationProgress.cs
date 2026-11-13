using System;

namespace Spark.Tag4D.Revit.App.ProjectSettings
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

        public bool HasErrors { get; private set; }

        public void Message(string text, LogLevel level = LogLevel.Info)
        {
            if (level == LogLevel.Error)
            {
                HasErrors = true;
            }

            OnMessage?.Invoke(text, level);
        }

        public void Report(int done, int total) => OnProgress?.Invoke(done, total);

        public void Completed() => OnCompleted?.Invoke();
    }
}
