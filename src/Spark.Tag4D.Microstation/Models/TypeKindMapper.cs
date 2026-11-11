using Bentley.DgnPlatformNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Microstation.Models
{
    public static class TypeKindMapper
    {
        public static CustomProperty.TypeKind Get(string typeName)
        {
            switch (typeName)
            {
                case "string":
                    return CustomProperty.TypeKind.String;
                case "int":
                    return CustomProperty.TypeKind.Integer;
                case "double":
                    return CustomProperty.TypeKind.Double;
                case "bool":
                    return CustomProperty.TypeKind.Boolean;
                default:
                    break;
            }

            return CustomProperty.TypeKind.String;
        }
    }
}
