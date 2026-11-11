using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Revit.Core
{
    public static class ParameterExtensions
    {
        public static void TryParseAndSet(this Parameter parameter, string value)
        {

            if (parameter == null)
            {
                return;
            }

            if (value == null)
            {
                value = string.Empty;
            }

            switch (parameter.StorageType)
            {
                case StorageType.None:
                    break;
                case StorageType.Integer:
                    if (int.TryParse(value, out int intValue))
                    {
                        parameter.Set(intValue);
                    }
                    else if (bool.TryParse(value, out bool boolValue))
                    {
                        int boolValueInt = boolValue ? 1 : 0;
                        parameter.Set(boolValueInt);
                    }
                    break;
                case StorageType.Double:
                    if (double.TryParse(value, out double doubleValue))
                    {
                        parameter.Set(doubleValue);
                    }
                    break;
                case StorageType.String:
                    if (value != null)
                    {
                        parameter.Set(value);
                    }
                    break;
                case StorageType.ElementId:
                    break;
                default:
                    break;
            }
        }

        public static object GetValueAsObject(this Parameter parameter)
        {
            if (parameter == null)
            {
                return null;
            }

            switch (parameter.StorageType)
            {
                case StorageType.None:
                    break;
                case StorageType.Integer:
                    return parameter.AsInteger();
                case StorageType.Double:
                    return parameter.AsDouble();
                case StorageType.String:
                    return parameter.AsValueString();
                case StorageType.ElementId:
                    break;
                default:
                    break;
            }

            return null;
        }
    }
}
