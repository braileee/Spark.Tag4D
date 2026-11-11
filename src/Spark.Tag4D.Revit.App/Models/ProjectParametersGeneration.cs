using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Revit.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Spark.Tag4D.Revit.App.Models
{
    public class ProjectParametersGeneration
    {
        public static void CreateProjectParameters(
    Document doc,
    List<PropertySetItem> propertySets)
        {
            var app = doc.Application;

            List<BuiltInCategory> excludedCategories = new List<BuiltInCategory> {
                BuiltInCategory.OST_ProjectInformation,
                BuiltInCategory.OST_HVAC_Zones,
                BuiltInCategory.OST_Materials,
                BuiltInCategory.OST_RvtLinks,
                BuiltInCategory.OST_DetailComponents,
            };

            var categories = doc.GetUsedBoundModelCategories(excludedCategories);

            using (var tx = new Transaction(doc, "Create Project Parameters"))
            {
                tx.Start();

                List<PropertyItem> properties = propertySets.SelectMany(ps => ps.PropertyItems).ToList();

                foreach (var prop in properties)
                {
                    if (ProjectParameterExists(doc, prop.PropertyName))
                    {
                        continue;
                    }

                    var specType = RevitTypeMapper.GetSpecType(prop.DataType);
                    RawCreateProjectParameter(app, prop.PropertyName, specType, true, categories, GroupTypeId.Ifc, true);
                }

                tx.Commit();
            }
        }

        private static bool ProjectParameterExists(Document doc, string name)
        {
            var map = doc.ParameterBindings;
            var it = map.ForwardIterator();
            it.Reset();

            while (it.MoveNext())
            {
                Definition def = it.Key;
                if (def != null && def.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }


        public static void RawCreateProjectParameter(Application app, string name, ForgeTypeId dataType, bool visible, CategorySet cats, ForgeTypeId groupTypeId, bool inst)
        {
            string oriFile = app.SharedParametersFilename;
            string tempFile = Path.GetTempFileName() + ".txt";
            using (File.Create(tempFile)) { }
            app.SharedParametersFilename = tempFile;

            var defOptions = new ExternalDefinitionCreationOptions(name, dataType)
            {
                Visible = visible
            };
            ExternalDefinition def = app.OpenSharedParameterFile().Groups.Create("TemporaryDefintionGroup").Definitions.Create(defOptions) as ExternalDefinition;

            app.SharedParametersFilename = oriFile;
            File.Delete(tempFile);

            Autodesk.Revit.DB.Binding binding = app.Create.NewTypeBinding(cats);
            if (inst) binding = app.Create.NewInstanceBinding(cats);

            BindingMap map = (new UIApplication(app)).ActiveUIDocument.Document.ParameterBindings;
            if (!map.Insert(def, binding, groupTypeId))
            {
                Trace.WriteLine($"Failed to create Project parameter '{name}' :(");
            }
        }
    }
}
