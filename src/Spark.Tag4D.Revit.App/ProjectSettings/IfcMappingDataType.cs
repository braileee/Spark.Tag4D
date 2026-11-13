using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Revit.App.ProjectSettings
{
    public static class IfcMappingDataType
    {
        public static string Get(string dataType)
        {
            if (string.IsNullOrEmpty(dataType))
            {
                return "Text";
            }


            switch (dataType.ToLower())
            {
                case "string":
                    return "Text";

                case "int":
                    return "Integer";

                case "double":
                    return "Real";

                case "bool":
                    return "Boolean";

                default:
                    return "Text";
            }

        }
    }
}
