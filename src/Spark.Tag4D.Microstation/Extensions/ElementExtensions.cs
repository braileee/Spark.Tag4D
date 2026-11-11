using Bentley.DgnPlatformNET;
using Bentley.DgnPlatformNET.DgnEC;
using Bentley.DgnPlatformNET.Elements;
using Bentley.ECObjects.Instance;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Microstation.Extensions
{
    public static class ElementExtensions
    {
        public static void SetValue(this Element element, string propertyName, string value)
        {
            Element newElement = element;
            CustomItemHost customItemHost = new CustomItemHost(newElement, true);
            var customItems = customItemHost.CustomItems;

            foreach (IDgnECInstance ecInstance in customItemHost.CustomItems)
            {
                var ecProperty = ecInstance.ClassDefinition.Properties(true).FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase) || 
                                                                                                 p.InvariantDisplayLabel.Equals(propertyName, StringComparison.Ordinal));
                if (ecProperty == null)
                {
                    continue;
                }

                string typeName = ecProperty.Type.Name;

                switch (typeName)
                {
                    case "string":
                        ecInstance.SetString(propertyName, value);
                        break;

                    case "int":
                        if (int.TryParse(value, out int intVal))
                            ecInstance.SetInteger(propertyName, intVal);
                        break;

                    case "long":
                        if (long.TryParse(value, out long longVal))
                            ecInstance.SetLong(propertyName, longVal);
                        break;

                    case "double":
                        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double dblVal))
                            ecInstance.SetDouble(propertyName, dblVal);
                        break;

                    case "boolean":
                        if (bool.TryParse(value, out bool boolVal))
                            ecInstance.SetBoolean(propertyName, boolVal);
                        break;

                    case "dateTime":
                        if (DateTime.TryParse(value, out DateTime dtVal))
                            ecInstance.SetDateTime(propertyName, dtVal);
                        break;

                    default:
                        break;
                }

                ecInstance.WriteChanges();
            }
        }

        public static object GetValue(this Element element, string propertyName)
        {
            if(element == null)
            {
                return null;
            }

            Element newElement = element;
            CustomItemHost customItemHost = new CustomItemHost(newElement, true);
            var customItems = customItemHost.CustomItems;

            IDgnECInstance instance = customItemHost.CustomItems.FirstOrDefault(item => item.ClassDefinition.Properties(true).FirstOrDefault(property => property.Name == propertyName || 
                                                                                                                                                property.InvariantDisplayLabel == propertyName) != null);

            if(instance == null)
            {
                return null;
            }

            var ecProperty = instance.ClassDefinition.Properties(true).FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase) ||
                                                                                           p.InvariantDisplayLabel.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

            if (ecProperty == null)
            {
                return null;
            }

            string typeName = ecProperty.Type.Name;
            object value = null;

            switch (typeName)
            {
                case "string":
                    value = instance.GetString(propertyName);
                    break;

                case "int":
                    value = instance.GetInteger(propertyName);
                    break;

                case "long":
                    value = instance.GetLong(propertyName);
                    break;

                case "double":
                    value = instance.GetDouble(propertyName);
                    break;

                case "boolean":
                    value = instance.GetBoolean(propertyName);
                    break;

                case "dateTime":
                    instance.GetDateTime(propertyName);
                    break;

                default:
                    break;
            }

            return value;
        }
    }
}
