using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Spark.Tag4D.Core.Configuration;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Settings.ViewModels;
using Spark.Tag4D.Settings.Views;
using Spark.Tag4D.Revit.App.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace Spark.Tag4D.Revit.App.Commands
{
    [Transaction(TransactionMode.Manual)]

    public class ParametersSettingsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var view = new SettingsView { DataContext = new SettingsViewModel() };
                view.ShowDialog();
            }
            catch (Exception exception)
            {
                TaskDialog.Show("Spark.Tag4D", $"The settings could not be opened: {exception.Message}");
                return Result.Failed;
            }

            return Result.Succeeded;
        }
    }
}
