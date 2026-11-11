using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Revit.Core
{
    public static class CellExtensions
    {
        public static object GetCellValue(this ICell cell)
        {
            if (cell == null)
                return null;

            switch (cell.CellType)
            {
                case CellType.Numeric:
                    return cell.NumericCellValue;

                case CellType.String:
                    return cell.StringCellValue;

                case CellType.Boolean:
                    return cell.BooleanCellValue;

                case CellType.Formula:
                    switch (cell.CachedFormulaResultType)
                    {
                        case CellType.Numeric:
                            return cell.NumericCellValue;

                        case CellType.String:
                            return cell.StringCellValue;

                        case CellType.Boolean:
                            return cell.BooleanCellValue;

                        default:
                            return null;
                    }

                default:
                    return null;
            }
        }

        public static string GetCellValueAsString(this ICell cell)
        {
            return GetCellValue(cell)?.ToString();
        }

    }
}
