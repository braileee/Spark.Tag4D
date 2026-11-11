using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Spark.Tag4D.Core.Json;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Core.Utils;
using Spark.Tag4D.Revit.App.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Spark.Tag4D.Revit.App.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class GenerateIfcMappingCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Load settings
                SettingsRoot settingsRoot = SettingsLoader.LoadExistingOrDefault();

                // load excel data

                string excelFilePath = ExcelDataLoader.LoadOrPromptExcelFilePath(settingsRoot.ExcelSettings.FileLinkSettings);

                if (string.IsNullOrEmpty(excelFilePath))
                {
                    return Result.Cancelled;
                }

                List<PropertySetItem> propertyDefinitions = ExcelDataLoader.LoadPropertySetItems(excelFilePath, settingsRoot.ExcelSettings.PropertiesSheet);

                string mappingFilePath = FilePromptUtils.SaveFileToFolder(Environment.SpecialFolder.Desktop, "IfcMapping", "txt");

                IfcMappingContent ifcMappingContent = new IfcMappingContent(propertyDefinitions);
                string mappingData = ifcMappingContent.ToRevitTxtFormat();

                System.IO.File.WriteAllText(mappingFilePath, mappingData, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error catched on showing start view", ex.Message);
            }

            return Result.Succeeded;
        }
    }
}
