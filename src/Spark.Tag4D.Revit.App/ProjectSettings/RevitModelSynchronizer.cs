using Spark.Tag4D.Settings.Services;
using System;

namespace Spark.Tag4D.Revit.App.ProjectSettings
{
    /// <summary>Mirrors changes of the Properties sheet in the active Revit project (through the external event).</summary>
    internal sealed class RevitModelSynchronizer : IModelSynchronizer
    {
        private readonly ParametersOperationEventHandler handler;
        private readonly Action<string, LogLevel> log;

        public RevitModelSynchronizer(ParametersOperationEventHandler handler, Action<string, LogLevel> log)
        {
            this.handler = handler;
            this.log = log;
        }

        public string ModelName => "Revit project";

        public void Apply(ModelSyncChange change, Action<bool> completed)
        {
            log("Properties changed - updating the Revit project.", LogLevel.Info);
            Run(change.Added, change.Removed, completed);
        }

        public void Rollback(ModelSyncChange change, Action<bool> completed)
        {
            log("Rolling the Revit project back.", LogLevel.Info);
            Run(change.Removed, change.Added, completed);
        }

        private void Run(System.Collections.Generic.List<Spark.Tag4D.Core.Models.PropertySetItem> create,
                         System.Collections.Generic.List<Spark.Tag4D.Core.Models.PropertySetItem> delete,
                         Action<bool> completed)
        {
            var progress = new OperationProgress { OnMessage = log };
            progress.OnCompleted = () => completed(!progress.HasErrors);

            handler.Raise(new ParametersOperationOptions
            {
                Operation = ParametersOperation.Synchronize,
                PropertySetItems = create,
                PropertySetItemsToDelete = delete,
                Progress = progress,
            });
        }
    }
}
