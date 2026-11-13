using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Spark.Tag4D.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Spark.Tag4D.Revit.App.Tag
{
    [Transaction(TransactionMode.Manual)]

    public class TagCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                PaneToggle.Toggle(commandData.Application, Constants.TagPaneId, Constants.ReviewPaneId);
            }
            catch (Exception exception)
            {
                TaskDialog.Show("Spark.Tag4D", $"The tagging pane could not be toggled: {exception.Message}");
                return Result.Failed;
            }

            return Result.Succeeded;
        }
    }
}
