using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace Spark.Tag4D.Revit.App.Models
{
    public static class RevitTypeMapper
    {
        public static ForgeTypeId GetSpecType(string dataType)
        {
            if (string.IsNullOrEmpty(dataType))
            {
                return SpecTypeId.String.Text;
            }

            ForgeTypeId result;

            switch (dataType.ToLower())
            {
                case "string":
                    result = SpecTypeId.String.Text;
                    break;

                case "int":
                    result = SpecTypeId.Int.Integer;
                    break;

                case "double":
                    result = SpecTypeId.Number;
                    break;

                case "bool":
                    result = SpecTypeId.Boolean.YesNo;
                    break;

                default:
                    result = SpecTypeId.String.Text;
                    break;
            }

            return result;
        }
    }
}

