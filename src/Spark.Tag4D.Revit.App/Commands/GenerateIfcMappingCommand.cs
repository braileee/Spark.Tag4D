using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.UI.Dialogs;
using System.Text;
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
                TagData data = TagData.Load();

                string mappingFilePath = FileDialogs.SaveFile("Text files (*.txt)|*.txt", "IfcMapping.txt");
                if (string.IsNullOrEmpty(mappingFilePath))
                {
                    return Result.Cancelled;
                }

                string mappingData = new IfcMappingContent(data.PropertySets).ToRevitTxtFormat();
                System.IO.File.WriteAllText(mappingFilePath, mappingData, Encoding.UTF8);
            }
            catch (TagDataException exception)
            {
                TaskDialog.Show("Spark.Tag4D", exception.Message);
                return Result.Failed;
            }
            catch (Exception exception)
            {
                TaskDialog.Show("Spark.Tag4D", $"Generating the mapping file failed: {exception.Message}");
                return Result.Failed;
            }

            return Result.Succeeded;
        }
    }
}
