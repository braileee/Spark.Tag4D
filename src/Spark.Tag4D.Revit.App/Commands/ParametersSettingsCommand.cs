using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Spark.Tag4D.Core.Json;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Settings.ViewModels;
using Spark.Tag4D.Settings.Views;
using Spark.Tag4D.Revit.App.Models;
using Spark.Tag4D.Revit.App.Views;
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
               
                SettingsRoot settingsRoot = SettingsLoader.LoadExistingOrDefault();
                string excelFilePath = ExcelDataLoader.LoadOrPromptExcelFilePath(settingsRoot.ExcelSettings.FileLinkSettings);

                if (string.IsNullOrEmpty(excelFilePath))
                {
                    MessageBox.Show($"No excel file selected", "Error");
                    return Result.Failed;
                }

                if(settingsRoot == null)
                {
                    MessageBox.Show($"No settings file found", "Error");
                    return Result.Failed;
                }

                SettingsView view = new SettingsView();
                SettingsViewModel viewModel = new SettingsViewModel();
                view.DataContext = viewModel;
                view.ShowDialog();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error catched on showing start view", ex.Message);
            }

            return Result.Succeeded;
        }
    }
}
