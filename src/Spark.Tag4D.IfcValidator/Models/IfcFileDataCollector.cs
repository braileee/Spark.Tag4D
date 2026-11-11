
using Spark.Tag4D.IfcValidator.Utils;
using Microsoft.Isam.Esent.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common.Geometry;
using Xbim.Ifc;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.IO.Xml.BsConf;

namespace Spark.Tag4D.IfcValidator.Models
{
    public class IfcFileDataCollector
    {
        public IfcFileDataCollector(List<string> ifcFilePathList)
        {
            IfcFilePathList = ifcFilePathList;
        }

        public List<string> IfcFilePathList { get; } = new List<string>();

        public List<IfcFile> CollectFromFiles()
        {
            List<IfcFile> ifcFiles = new List<IfcFile>();

            foreach (string ifcPath in IfcFilePathList)
            {
                IfcFile ifcFile = CollectFromFile(ifcPath);
                ifcFiles.Add(ifcFile);
            }

            return ifcFiles;
        }

        private IfcFile CollectFromFile(string ifcPath)
        {

            using var model = IfcStore.Open(ifcPath);

            // All objects that can have property sets
            var objects = model.Instances.OfType<IIfcObject>();

            List<IfcElement> ifcElements = new List<IfcElement>();

            foreach (IIfcObject obj in objects)
            {
                string tag = obj is IIfcElement element ? element.Tag : "";

                string layerName = IfcLayerUtils.GetSingleLayerName(obj);

                IfcElement ifcElement = new IfcElement
                {
                    Guid = obj.GlobalId,
                    IfcEntity = obj.ExpressType.Name,
                    Tag = tag,
                    Layer = layerName
                };

                var psetRels = obj.IsDefinedBy.OfType<IIfcRelDefinesByProperties>();

                List<IfcProperty> properties = new List<IfcProperty>();

                foreach (var rel in psetRels)
                {

                    if (rel.RelatingPropertyDefinition is IIfcPropertySet pset)
                    {
                        foreach (var prop in pset.HasProperties)
                        {
                            IfcProperty ifcProperty = ConvertProperty(obj, pset, prop);

                            properties.Add(ifcProperty);
                        }
                    }
                }

                ifcElement.IfcProperties = properties;
                ifcElements.Add(ifcElement);
            }

            IfcFile ifcFile = new IfcFile
            {
                FilePath = ifcPath,
                IfcElements = ifcElements
            };

            return ifcFile;
        }

        private IfcProperty ConvertProperty(
            IIfcObject obj,
            IIfcPropertySet pset,
            IIfcProperty prop)
        {
            string value = ExtractValue(prop);

            return new IfcProperty
            {
                PropertySetName = pset.Name,
                PropertyName = prop.Name,
                Value = value,
            };
        }

        private string ExtractValue(IIfcProperty prop)
        {
            switch (prop)
            {
                case IIfcPropertySingleValue sv:
                    return sv.NominalValue?.Value?.ToString() ?? "";

                case IIfcPropertyEnumeratedValue ev:
                    return string.Join(", ", ev.EnumerationValues
                        .Select(v => v.Value?.ToString()));

                case IIfcPropertyListValue lv:
                    return string.Join(", ", lv.ListValues
                        .Select(v => v.Value?.ToString()));

                case IIfcPropertyTableValue tv:
                    return $"Table: {tv.DefiningValues.Count} items";

                case IIfcComplexProperty cp:
                    return $"Complex ({cp.HasProperties.Count} sub-properties)";

                default:
                    return "";
            }
        }

        internal void PrintAllTheLayers(List<string> ifcFilePaths)
        {
            foreach (string filePath in ifcFilePaths)
            {
                var model = IfcStore.Open(filePath);

                var layers = model.Instances
                           .OfType<IIfcPresentationLayerAssignment>()
                           .Select(l => new
                           {
                               Name = l.Name.ToString() ?? "(Unnamed)",
                               Identifier = l.Identifier?.ToString(),
                               Description = l.Description?.ToString(),
                               IsWithStyle = l is IIfcPresentationLayerWithStyle
                           })
                           .OrderBy(x => x.Name)
                           .ToList();

                Console.WriteLine($"Found {layers.Count} presentation layers.");
                foreach (var l in layers)
                {
                    Console.WriteLine($"- {l.Name}"
                        + (string.IsNullOrWhiteSpace(l.Identifier) ? "" : $" [Id: {l.Identifier}]")
                        + (l.IsWithStyle ? " (WithStyle)" : "")
                        + (string.IsNullOrWhiteSpace(l.Description) ? "" : $" — {l.Description}"));
                }

            }
        }


        public void PrintLayersWithAssignedItemCounts(List<string> ifcPathList)
        {
            foreach (var filePath in ifcPathList)
            {
                using var model = IfcStore.Open(filePath);

                var layers = model.Instances
                    .OfType<IIfcPresentationLayerAssignment>()
                    .Select(l => new
                    {
                        Name = l.Name.ToString() ?? "(Unnamed)",
                        AssignedCount = l.AssignedItems?.Count ?? 0,
                        AssignedItems = l.AssignedItems,
                        Identifier = l.Identifier?.ToString(),
                        IsWithStyle = l is IIfcPresentationLayerWithStyle
                    })
                    .OrderByDescending(x => x.AssignedCount)
                    .ThenBy(x => x.Name)
                    .ToList();

                Console.WriteLine($"Found {layers.Count} presentation layers.");
                foreach (var l in layers)
                {
                    Console.WriteLine($"- {l.Name}  | AssignedItems: {l.AssignedCount}"
                        + (string.IsNullOrWhiteSpace(l.Identifier) ? "" : $" | Id: {l.Identifier}")
                        + (l.IsWithStyle ? " | WithStyle" : ""));
                }
            }
        }
    }
}
