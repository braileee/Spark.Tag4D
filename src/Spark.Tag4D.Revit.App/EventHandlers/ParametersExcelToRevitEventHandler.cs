using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Spark.Tag4D.Revit.Core.Models;
using Spark.Tag4D.Revit.App.Models;
using Spark.Tag4D.Revit.App.ViewModels;
using Spark.Tag4D.Revit.App.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;

namespace Spark.Tag4D.Revit.App.EventHandlers
{
    internal class ParametersExcelToRevitEventHandler : RevitEventWrapper<ParametersExcelToRevitOptions>
    {
        public override void Execute(UIApplication app, ParametersExcelToRevitOptions options)
        {
            try
            {
               ProjectParametersGeneration.CreateProjectParameters(app.ActiveUIDocument.Document, options.PropertySetItems);
            }
            catch (Exception exception)
            {
                TaskDialog.Show("Spark.Tag4D", $"Creating the project parameters failed: {exception.Message}");
            }
        }
    }
}
