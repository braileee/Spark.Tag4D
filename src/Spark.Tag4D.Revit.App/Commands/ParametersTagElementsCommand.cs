using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Spark.Tag4D.Core;
using Spark.Tag4D.Core.Json;
using Spark.Tag4D.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Spark.Tag4D.Revit.App.Commands
{
    [Transaction(TransactionMode.Manual)]

    public class ParametersTagElementsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                SettingsRoot settingsRoot = SettingsLoader.LoadExistingOrDefault();

                // load excel data
                string excelFilePath = ExcelDataLoader.LoadOrPromptExcelFilePath(settingsRoot.ExcelSettings.FileLinkSettings);

                if (string.IsNullOrEmpty(excelFilePath))
                {
                    MessageBox.Show($"No excel file selected", "Error");
                    return Result.Cancelled;
                }

                if(settingsRoot == null)
                {
                    MessageBox.Show($"No settings file found", "Error");
                    return Result.Cancelled;
                }

                List<PropertySetItem> propertySetItems = ExcelDataLoader.LoadPropertySetItems(excelFilePath, settingsRoot.ExcelSettings.PropertiesSheet);
                List<PicklistGroup> picklistGroups = ExcelDataLoader.LoadPicklistGroups(excelFilePath, settingsRoot.ExcelSettings.PicklistSheet);

                Dictionary<string, List<string>> propertiesWithValues = picklistGroups.ToDictionary(item => item.GroupName, item => item.Values);

                List<PropertyItem> properties = propertySetItems.SelectMany(item => item.PropertyItems).ToList();

                DockablePaneId id = new DockablePaneId(new Guid(Constants.ParametersTagElementsPaneId));
                DockablePane dockableWindow = commandData.Application.GetDockablePane(id);

                if (dockableWindow.IsShown())
                {
                    dockableWindow.Hide();
                }
                else
                {
                    dockableWindow.Show();
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error catched", ex.Message);
            }

            return Result.Succeeded;
        }
    }
}
