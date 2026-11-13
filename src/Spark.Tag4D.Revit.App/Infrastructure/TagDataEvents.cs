using System;

namespace Spark.Tag4D.Revit.App.Infrastructure
{
    /// <summary>Tells the panes that the settings or the workbook were changed, so they re-read them (and only then).</summary>
    public static class TagDataEvents
    {
        public static event Action Changed;

        public static void RaiseChanged() => Changed?.Invoke();
    }
}
