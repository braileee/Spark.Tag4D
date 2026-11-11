using System.Globalization;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Core.Spreadsheets;

namespace Spark.Tag4D.IfcValidator.Models
{
    /// <summary>Runs every validation check and writes the findings to an .xlsx report (one sheet per check).</summary>
    public class ReportWriter
    {
        private static readonly string[] MainHeaders =
        {
            "FilePath", "Guid", "IfcEntity", "Tag", "Layer", "PropertySetName", "PropertyName", "Value"
        };

        private static readonly string[] MissingPropertiesHeaders =
        {
            "FilePath", "Guid", "IfcEntity", "Tag", "Layer", "Missing PropertySetName", "Missing PropertyName", "Value"
        };

        public ReportWriter(TagData data, List<IfcFile> ifcFiles, string reportFilePath)
        {
            Data = data;
            IfcFiles = ifcFiles;
            ReportFilePath = reportFilePath;
        }

        public TagData Data { get; }

        public List<IfcFile> IfcFiles { get; }

        public string ReportFilePath { get; }

        public void Create()
        {
            if (IfcFiles.Count == 0 || string.IsNullOrEmpty(ReportFilePath))
            {
                return;
            }

            var filter = new IfcFileDataFilter(IfcFiles, Data.PropertySets, Data.Picklists);

            // Elements with missing properties of a defined property set.
            List<IfcFile> missingProperties = filter.GetElementsWithMissingProperties(IfcFiles, Data.PropertySets);

            // Elements that carry at least one of the defined properties; the remaining checks run on those.
            List<IfcFile> candidates = filter.GetElementsWithAnyRequiredProperty();

            List<PropertyValueMatch> allMatches = Data.Matches.Concat(Data.ExactMatches).ToList();

            Workbook workbook = Workbook.Create();

            AddSheet(workbook, "Missing properties check", MissingPropertiesHeaders, missingProperties);
            AddSheet(workbook, "Empty property values check", MainHeaders, filter.GetElementsWithEmptyValues(candidates));
            AddSheet(
                workbook,
                "Picklist check",
                MainHeaders.Concat(new[] { "Is From Picklist" }).ToArray(),
                filter.GetPicklistCheck(candidates, Data.Picklists),
                includePicklistFlag: true);
            AddSheet(workbook, "Wrong picklist match check", MainHeaders, filter.GetNonMatchData(candidates, allMatches));
            AddSheet(workbook, "Wrong expressions check", MainHeaders, filter.GetWrongExpressions(candidates, Data.Expressions));
            AddSheet(workbook, "Wrong layer mappings check", MainHeaders, filter.GetWrongLayerMappings(candidates, Data.LayerMappings));
            AddSheet(workbook, "Wrong composed data check", MainHeaders, filter.GetWrongComposedData(candidates, Data.Composed));

            workbook.Save(ReportFilePath);
        }

        private static void AddSheet(Workbook workbook, string name, string[] headers, List<IfcFile> files, bool includePicklistFlag = false)
        {
            Worksheet sheet = workbook.AddSheet(name);

            sheet.SetRow(0, headers);
            sheet.SetRowBold(0, headers.Length);

            int row = 1;

            foreach (IfcFile file in files)
            {
                if (file?.IfcElements == null)
                {
                    continue;
                }

                foreach (IfcElement element in file.IfcElements)
                {
                    List<IfcProperty> properties = element?.IfcProperties;

                    // An element without properties still gets one row so it shows up in the report.
                    if (properties == null || properties.Count == 0)
                    {
                        WriteRow(sheet, row++, file, element, null, includePicklistFlag);
                        continue;
                    }

                    foreach (IfcProperty property in properties)
                    {
                        WriteRow(sheet, row++, file, element, property, includePicklistFlag);
                    }
                }
            }

            sheet.FreezeRows(1);
            sheet.SetAutoFilter(0, 0, Math.Max(row - 1, 0), headers.Length - 1);
            sheet.AutoSizeColumns(headers.Length);
        }

        private static void WriteRow(Worksheet sheet, int row, IfcFile file, IfcElement element, IfcProperty property, bool includePicklistFlag)
        {
            sheet.SetValue(row, 0, file?.FilePath);
            sheet.SetValue(row, 1, element?.Guid.ToString());
            sheet.SetValue(row, 2, element?.IfcEntity);
            sheet.SetValue(row, 3, element?.Tag);
            sheet.SetValue(row, 4, element?.Layer);
            sheet.SetValue(row, 5, property?.PropertySetName);
            sheet.SetValue(row, 6, property?.PropertyName);
            sheet.SetValue(row, 7, ToText(property?.Value));

            if (includePicklistFlag)
            {
                sheet.SetValue(row, 8, property?.IsValueFromPicklist ?? false);
            }
        }

        private static string ToText(object value)
        {
            switch (value)
            {
                case null:
                    return string.Empty;
                case DateTime date:
                    return date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                case IFormattable formattable:
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
                default:
                    return value.ToString() ?? string.Empty;
            }
        }
    }
}
