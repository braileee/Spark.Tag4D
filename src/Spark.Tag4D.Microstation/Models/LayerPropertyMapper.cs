using Bentley.DgnPlatformNET;
using Bentley.DgnPlatformNET.DgnEC;
using Bentley.DgnPlatformNET.Elements;
using Bentley.ECObjects.Instance;
using Bentley.ECObjects.Schema;
using Bentley.MstnPlatformNET;
using Spark.Tag4D.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Microstation.Models
{
    public class LayerPropertyMapper
    {
        public LayerPropertyMapper(DgnFile dgnFile, List<PropertySetItem> propertySetItems, List<LayerMappingItem> layerMappingItems, ModelElementsCollection elements)
        {
            DgnFile = dgnFile;
            PropertySetItems = propertySetItems;
            LayerMappingItems = layerMappingItems;
            Elements = elements;
        }

        public DgnFile DgnFile { get; }
        public List<PropertySetItem> PropertySetItems { get; } = new List<PropertySetItem>();
        public List<LayerMappingItem> LayerMappingItems { get; } = new List<LayerMappingItem>();
        public ModelElementsCollection Elements { get; }

        public void Map()
        {
            List<string> propertyNames = PropertySetItems.SelectMany(item => item.PropertyItems).Select(item => item.PropertyName).ToList();

            foreach (Element element in Elements)
            {
                LevelHandle levelHandle = DgnFile.GetLevelCache().GetLevel(element.LevelId);
                LayerMappingItem mappingItem = LayerMappingItems.FirstOrDefault(item => item.LayerName == levelHandle.Name);

                if (mappingItem == null)
                {
                    continue;
                }

                Element newElement = element;
                CustomItemHost customItemHost = new CustomItemHost(newElement, true);
                var customItems = customItemHost.CustomItems;

                foreach (IDgnECInstance ecInstance in customItemHost.CustomItems)
                {
                    foreach (KeyValuePair<string, string> propertyWithValue in mappingItem.PropertiesWithValues)
                    {
                        string propName = propertyWithValue.Key;
                        string rawValue = propertyWithValue.Value;

                        var ecProperty = ecInstance.ClassDefinition.Properties(true).FirstOrDefault(p => p.Name.Equals(propName, StringComparison.OrdinalIgnoreCase));
                        if (ecProperty == null)
                        {
                            continue;
                        }

                        string typeName = ecProperty.Type.Name;

                        switch (typeName)
                        {
                            case "string":
                                ecInstance.SetString(propName, rawValue);
                                break;

                            case "int":
                                if (int.TryParse(rawValue, out int intVal))
                                    ecInstance.SetInteger(propName, intVal);
                                break;

                            case "long":
                                if (long.TryParse(rawValue, out long longVal))
                                    ecInstance.SetLong(propName, longVal);
                                break;

                            case "double":
                                if (double.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double dblVal))
                                    ecInstance.SetDouble(propName, dblVal);
                                break;

                            case "boolean":
                                if (bool.TryParse(rawValue, out bool boolVal))
                                    ecInstance.SetBoolean(propName, boolVal);
                                break;

                            case "dateTime":
                                if (DateTime.TryParse(rawValue, out DateTime dtVal))
                                    ecInstance.SetDateTime(propName, dtVal);
                                break;

                            default:
                                break;
                        }
                    }

                    ecInstance.WriteChanges();
                }
            }
        }
    }
}
