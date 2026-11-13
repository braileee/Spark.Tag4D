using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Spark.Tag4D.Core;
using System;

namespace Spark.Tag4D.Revit.App.Review
{
    /// <summary>Shows or hides the Missing Values pane.</summary>
    [Transaction(TransactionMode.Manual)]
    public class ReviewCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                PaneToggle.Toggle(commandData.Application, Constants.ReviewPaneId, Constants.TagPaneId);
            }
            catch (Exception exception)
            {
                TaskDialog.Show("Spark.Tag4D", $"The Review pane could not be toggled: {exception.Message}");
                return Result.Failed;
            }

            return Result.Succeeded;
        }
    }
}
