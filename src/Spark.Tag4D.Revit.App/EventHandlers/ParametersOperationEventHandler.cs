using Autodesk.Revit.UI;
using Spark.Tag4D.Revit.App.Models;
using Spark.Tag4D.Revit.Core.Models;
using System;

namespace Spark.Tag4D.Revit.App.EventHandlers
{
    /// <summary>Runs the load / delete parameters operations inside a valid Revit API context.</summary>
    internal class ParametersOperationEventHandler : RevitEventWrapper<ParametersOperationOptions>
    {
        public override void Execute(UIApplication app, ParametersOperationOptions options)
        {
            OperationProgress progress = options.Progress;

            try
            {
                var document = app.ActiveUIDocument?.Document;
                if (document == null)
                {
                    progress.Message("Open a Revit project first - there is no active document.", LogLevel.Error);
                    return;
                }

                switch (options.Operation)
                {
                    case ParametersOperation.Load:
                        ProjectParametersGeneration.CreateProjectParameters(document, options.PropertySetItems, progress);
                        break;
                    case ParametersOperation.Synchronize:
                        if (options.PropertySetItemsToDelete.Count > 0)
                        {
                            ProjectParametersGeneration.DeleteProjectParameters(document, options.PropertySetItemsToDelete, progress);
                        }

                        if (options.PropertySetItems.Count > 0)
                        {
                            ProjectParametersGeneration.CreateProjectParameters(document, options.PropertySetItems, progress);
                        }

                        break;
                    case ParametersOperation.Delete:
                        ProjectParametersGeneration.DeleteProjectParameters(document, options.PropertySetItems, progress);
                        break;
                }
            }
            catch (Exception exception)
            {
                progress.Message($"The operation failed: {exception.Message}", LogLevel.Error);
            }
            finally
            {
                progress.Completed();
            }
        }
    }
}
