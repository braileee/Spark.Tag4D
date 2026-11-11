using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Revit.Core
{
    public static class ElementExtensions
    {
        public static XYZ GetLocationOrCurveCenter(this Element element)
        {
            if (element.Location is LocationPoint)
            {
                LocationPoint locationPoint = element.Location as LocationPoint;
                return locationPoint.Point;
            }
            else if (element.Location is LocationCurve)
            {
                LocationCurve locationCurve = element.Location as LocationCurve;
                XYZ point1 = locationCurve.Curve.GetEndPoint(0);
                XYZ point2 = locationCurve.Curve.GetEndPoint(1);

                return (point1 + point2) / 2;
            }

            return null;
        }

        public static XYZ GetBoundingBoxCenter(this Element element)
        {
            BoundingBoxXYZ boundingBoxXYZ = element.get_BoundingBox(null);
            return (boundingBoxXYZ.Min + boundingBoxXYZ.Max) / 2;
        }

        public static string GetParameterString(this Element element, string parameterName)
        {
            Parameter parameter = element.LookupParameter(parameterName);

            if (parameter != null && parameter.HasValue)
            {
                return parameter.AsString() ?? parameter.AsValueString();
            }

            return null;
        }

        public static string GetParameterString(this Element element, BuiltInParameter builtInParameter)
        {
            Parameter parameter = element.get_Parameter(builtInParameter);

            if (parameter != null && parameter.HasValue)
            {
                return parameter.AsString() ?? parameter.AsValueString();
            }

            return null;
        }
    }
}
