using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Revit.App.EventHandlers;
using Spark.Tag4D.Revit.App.Models;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Spark.Tag4D.Revit.App.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class ParametersExcelToRevitCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                TagData data = TagData.Load();

                new ParametersExcelToRevitEventHandler().Raise(new ParametersExcelToRevitOptions
                {
                    PropertySetItems = data.PropertySets
                });
            }
            catch (TagDataException exception)
            {
                TaskDialog.Show("Spark.Tag4D", exception.Message);
                return Result.Cancelled;
            }
            catch (Exception exception)
            {
                TaskDialog.Show("Spark.Tag4D", $"Importing the parameters failed: {exception.Message}");
                return Result.Failed;
            }

            return Result.Succeeded;
        }
    }
}
