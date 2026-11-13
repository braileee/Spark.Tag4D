using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Revit.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Spark.Tag4D.Revit.App.ProjectSettings
{
    /// <summary>Creates and removes the project parameters that back the IFC properties of the workbook.</summary>
    public static class ProjectParametersGeneration
    {
        public static void CreateProjectParameters(Document doc, List<PropertySetItem> propertySets, OperationProgress progress)
        {
            List<BuiltInCategory> excludedCategories = new List<BuiltInCategory>
            {
                BuiltInCategory.OST_ProjectInformation,
                BuiltInCategory.OST_HVAC_Zones,
                BuiltInCategory.OST_Materials,
                BuiltInCategory.OST_RvtLinks,
                BuiltInCategory.OST_DetailComponents,
            };

            List<PropertyItem> properties = propertySets.SelectMany(ps => ps.PropertyItems).ToList();
            progress.Message($"Loading {properties.Count} parameters into '{doc.Title}'...");

            CategorySet categories = doc.GetUsedBoundModelCategories(excludedCategories);
            progress.Message($"Binding to {categories.Size} model categories used in the project.");

            int created = 0, skipped = 0, failed = 0, done = 0;

            using (Transaction tx = new Transaction(doc, "Create Project Parameters"))
            {
                tx.Start();
                HashSet<string> existing = GetExistingNames(doc);

                foreach (PropertyItem property in properties)
                {
                    done++;
                    progress.Report(done, properties.Count);

                    if (existing.Contains(property.PropertyName))
                    {
                        skipped++;
                        progress.Message($"{property.PropertyName} - already exists, skipped.");
                        continue;
                    }

                    try
                    {
                        ForgeTypeId specType = RevitTypeMapper.GetSpecType(property.DataType);
                        if (RawCreateProjectParameter(doc, property.PropertyName, specType, true, categories, GroupTypeId.Ifc, true))
                        {
                            created++;
                            existing.Add(property.PropertyName);
                            progress.Message($"{property.PropertyName} - created ({property.DataType}).", LogLevel.Success);
                        }
                        else
                        {
                            failed++;
                            progress.Message($"{property.PropertyName} - Revit refused to create the parameter.", LogLevel.Error);
                        }
                    }
                    catch (Exception exception)
                    {
                        failed++;
                        progress.Message($"{property.PropertyName} - {exception.Message}", LogLevel.Error);
                    }
                }

                tx.Commit();
            }

            progress.Message(
                $"Done: {created} created, {skipped} already existed, {failed} failed.",
                failed > 0 ? LogLevel.Warning : LogLevel.Success);
        }

        public static void DeleteProjectParameters(Document doc, List<PropertySetItem> propertySets, OperationProgress progress)
        {
            HashSet<string> names = new HashSet<string>(
                propertySets.SelectMany(ps => ps.PropertyItems).Select(p => p.PropertyName),
                StringComparer.OrdinalIgnoreCase);

            progress.Message($"Removing the workbook parameters from '{doc.Title}'...");

            List<Definition> toRemove = new List<Definition>();
            DefinitionBindingMapIterator iterator = doc.ParameterBindings.ForwardIterator();
            iterator.Reset();
            while (iterator.MoveNext())
            {
                Definition definition = iterator.Key;
                if (definition != null && names.Contains(definition.Name))
                {
                    toRemove.Add(definition);
                }
            }

            if (toRemove.Count == 0)
            {
                progress.Message("None of the workbook parameters exist in this project.", LogLevel.Warning);
                return;
            }

            int removed = 0, failed = 0, done = 0;

            using (Transaction tx = new Transaction(doc, "Delete Project Parameters"))
            {
                tx.Start();

                foreach (Definition definition in toRemove)
                {
                    done++;
                    progress.Report(done, toRemove.Count);

                    if (doc.ParameterBindings.Remove(definition))
                    {
                        removed++;
                        progress.Message($"{definition.Name} - removed.", LogLevel.Success);
                    }
                    else
                    {
                        failed++;
                        progress.Message($"{definition.Name} - could not be removed.", LogLevel.Error);
                    }
                }

                tx.Commit();
            }

            progress.Message(
                $"Done: {removed} removed, {failed} failed.",
                failed > 0 ? LogLevel.Warning : LogLevel.Success);
        }

        private static HashSet<string> GetExistingNames(Document doc)
        {
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            DefinitionBindingMapIterator iterator = doc.ParameterBindings.ForwardIterator();
            iterator.Reset();

            while (iterator.MoveNext())
            {
                if (iterator.Key != null)
                {
                    names.Add(iterator.Key.Name);
                }
            }

            return names;
        }

        private static bool RawCreateProjectParameter(Document doc, string name, ForgeTypeId dataType, bool visible, CategorySet cats, ForgeTypeId groupTypeId, bool instance)
        {
            Application app = doc.Application;
            string originalFile = app.SharedParametersFilename;
            string tempFile = Path.GetTempFileName() + ".txt";
            using (File.Create(tempFile)) { }
            app.SharedParametersFilename = tempFile;

            ExternalDefinition definition;
            try
            {
                ExternalDefinitionCreationOptions options = new ExternalDefinitionCreationOptions(name, dataType) { Visible = visible };
                definition = app.OpenSharedParameterFile().Groups.Create("TemporaryDefintionGroup").Definitions.Create(options) as ExternalDefinition;
            }
            finally
            {
                app.SharedParametersFilename = originalFile;
                File.Delete(tempFile);
            }

            Autodesk.Revit.DB.Binding binding = instance ? (Autodesk.Revit.DB.Binding)app.Create.NewInstanceBinding(cats) : app.Create.NewTypeBinding(cats);
            return doc.ParameterBindings.Insert(definition, binding, groupTypeId);
        }
    }
}
