using Spark.Tag4D.Core.Models;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.IfcValidator.Models
{
    public class ReportWriter
    {
        public ReportWriter(List<IfcFile> ifcFiles, string reportFilePath, List<PropertySetItem> propertySetItems, List<PicklistGroup> picklistGroups, List<PropertyValueMatch> propertyValueMatches, List<ExpressionRule> expressions, List<LayerMappingItem> layerMappingItems, List<ComposedPropertyItem> composedPropertyItems, List<PropertyValueMatch> exactPropertyValueMatches)
        {
            IfcFiles = ifcFiles;
            ReportFilePath = reportFilePath;
            PropertySetItems = propertySetItems;
            PicklistGroups = picklistGroups;
            PropertyValueMatches = propertyValueMatches;
            Expressions = expressions;
            LayerMappingItems = layerMappingItems;
            ComposedPropertyItems = composedPropertyItems;
            ExactPropertyValueMatches = exactPropertyValueMatches;
        }

        public List<IfcFile> IfcFiles { get; } = new List<IfcFile>();
        public string ReportFilePath { get; }
        public List<PropertySetItem> PropertySetItems { get; } = new List<PropertySetItem>();
        public List<PicklistGroup> PicklistGroups { get; } = new List<PicklistGroup>();
        public List<PropertyValueMatch> PropertyValueMatches { get; } = new List<PropertyValueMatch>();
        public List<ExpressionRule> Expressions { get; } = new List<ExpressionRule>();
        public List<LayerMappingItem> LayerMappingItems { get; }
        public List<ComposedPropertyItem> ComposedPropertyItems { get; }
        public List<PropertyValueMatch> ExactPropertyValueMatches { get; }

        public void Create()
        {
            if (IfcFiles.Count == 0)
            {
                return;
            }

            if (string.IsNullOrEmpty(ReportFilePath))
            {
                return;
            }

            IfcFileDataFilter ifcFileDataFilter = new IfcFileDataFilter(IfcFiles, PropertySetItems, PicklistGroups);

            //Elements with missing properties of defined property set
            List<IfcFile> ifcFilesMissingProperties = ifcFileDataFilter.GetElementsWithMissingProperties(IfcFiles, PropertySetItems);

            //Collect elements which contains any of the property needed
            List<IfcFile> filteredIfcFiles = ifcFileDataFilter.GetElementsWithAnyRequiredProperty();

            //Element with properties of property sets but with empty values
            List<IfcFile> ifcFileWithEmptyValues = ifcFileDataFilter.GetElementsWithEmptyValues(filteredIfcFiles);

            // Valid picklist check according to excel file
            List<IfcFile> ifcFilesPicklistCheck = ifcFileDataFilter.GetPicklistCheck(filteredIfcFiles, PicklistGroups);


            List<PropertyValueMatch> allPropertyValueMatches = new List<PropertyValueMatch>();
            allPropertyValueMatches.AddRange(PropertyValueMatches);
            allPropertyValueMatches.AddRange(ExactPropertyValueMatches);

            List<IfcFile> ifcFileNonMatchList = ifcFileDataFilter.GetNonMatchData(filteredIfcFiles, allPropertyValueMatches);
            List<IfcFile> ifcFileExpressions = ifcFileDataFilter.GetWrongExpressions(filteredIfcFiles, Expressions);
            List<IfcFile> wrongMappings = ifcFileDataFilter.GetWrongLayerMappings(filteredIfcFiles, LayerMappingItems);
            List<IfcFile> wrongComposedData = ifcFileDataFilter.GetWrongComposedData(filteredIfcFiles, ComposedPropertyItems);

            IWorkbook workbook = new XSSFWorkbook();

            List<string> mainHeaders =
                        new List<string>
                        {
                            "FilePath", "Guid", "IfcEntity", "Tag", "Layer",
                            "PropertySetName", "PropertyName", "Value"
                        };

            List<string> picklistHeaders = mainHeaders.ToList();
            picklistHeaders.Add("Is From Picklist");

            List<string> missingPropertiesHeaders =
                    new List<string>
                    {
                            "FilePath", "Guid", "IfcEntity", "Tag", "Layer",
                            "Missing PropertySetName", "Missing PropertyName", "Value"
                    };

            ISheet missedPropertiesSheet = workbook.CreateSheet("Missing properties check");
            WriteAllData(missedPropertiesSheet, missingPropertiesHeaders, ifcFilesMissingProperties);

            ISheet emptyValuesSheet = workbook.CreateSheet("Empty property values check");
            WriteAllData(emptyValuesSheet, mainHeaders, ifcFileWithEmptyValues);

            ISheet picklistCheckSheet = workbook.CreateSheet("Picklist check");
            WriteAllDataPicklist(picklistCheckSheet, picklistHeaders, ifcFilesPicklistCheck);

            ISheet nonMatchSheet = workbook.CreateSheet("Wrong picklist match check");
            WriteAllData(nonMatchSheet, mainHeaders, ifcFileNonMatchList);

            ISheet wrongExpressionsShseet = workbook.CreateSheet("Wrong expressions check");
            WriteAllData(wrongExpressionsShseet, mainHeaders, ifcFileExpressions);

            ISheet wrongLayerMappings = workbook.CreateSheet("Wrong layer mappings check");
            WriteAllData(wrongLayerMappings, mainHeaders, wrongMappings);

            ISheet wrongComposedDataSheet = workbook.CreateSheet("Wrong composed data check");
            WriteAllData(wrongComposedDataSheet, mainHeaders, wrongComposedData);

            // --- Save to disk ---
            using (var fs = new FileStream(ReportFilePath, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fs);
            }

            workbook.Close();

        }

        private void WriteAllData(ISheet sheet, List<string> headers, List<IfcFile> ifcFiles)
        {
            // --- Header row ---
            int rowIndex = AddHeaders(sheet, headers);

            foreach (var file in ifcFiles)
            {
                if (file?.IfcElements == null) continue;

                foreach (var element in file.IfcElements)
                {
                    // If element has no properties, still output a row (optional, but often useful)
                    var props = element?.IfcProperties;
                    if (props == null || props.Count == 0)
                    {
                        var row = sheet.CreateRow(rowIndex++);
                        WriteRow(row, file, element, psetName: null, propName: null, value: null);
                        continue;
                    }

                    // One row per property
                    foreach (var prop in props)
                    {
                        var row = sheet.CreateRow(rowIndex++);
                        WriteRow(
                            row,
                            file,
                            element,
                            prop?.PropertySetName,
                            prop?.PropertyName,
                            prop?.Value
                        );
                    }
                }
            }

            FormatSheet(sheet, headers, rowIndex);
        }

        private void WriteAllDataPicklist(ISheet sheet, List<string> headers, List<IfcFile> ifcFiles)
        {
            // --- Header row ---
            int rowIndex = AddHeaders(sheet, headers);

            foreach (var file in ifcFiles)
            {
                if (file?.IfcElements == null) continue;

                foreach (var element in file.IfcElements)
                {
                    // If element has no properties, still output a row (optional, but often useful)
                    var props = element?.IfcProperties;
                    if (props == null || props.Count == 0)
                    {
                        var row = sheet.CreateRow(rowIndex++);
                        WriteRow(row, file, element, psetName: null, propName: null, value: null);
                        continue;
                    }

                    // One row per property
                    foreach (var prop in props)
                    {
                        var row = sheet.CreateRow(rowIndex++);
                        WriteRow(
                            row,
                            file,
                            element,
                            prop?.PropertySetName,
                            prop?.PropertyName,
                            prop?.Value,
                            prop.IsValueFromPicklist
                        );
                    }
                }
            }

            FormatSheet(sheet, headers, rowIndex);
        }

        private static int AddHeaders(ISheet sheet, List<string> headers)
        {
            var headerRow = sheet.CreateRow(0);
            for (int i = 0; i < headers.Count; i++)
            {
                var cell = headerRow.CreateCell(i);
                cell.SetCellValue(headers[i]);
            }

            int rowIndex = 1;
            return rowIndex;
        }

        private static void FormatSheet(ISheet sheet, List<string> headers, int rowIndex)
        {
            // --- Make it readable ---
            sheet.CreateFreezePane(0, 1); // Freeze header row


            int lastRow = rowIndex - 1;           // last row with data
            int lastCol = headers.Count - 1;     // last column index

            // Apply AutoFilter to header + data
            sheet.SetAutoFilter(new NPOI.SS.Util.CellRangeAddress(0, lastRow, 0, lastCol));

            for (int i = 0; i < headers.Count; i++)
                sheet.AutoSizeColumn(i);
        }

        private static void WriteRow(
       IRow row,
       IfcFile file,
       IfcElement element,
       string psetName,
       string propName,
       object value)
        {
            // Column order:
            // 0 FilePath
            // 1 Guid
            // 2 IfcEntity
            // 3 Tag
            // 4 Layer
            // 5 PropertySetName
            // 6 PropertyName
            // 7 Value

            row.CreateCell(0).SetCellValue(file?.FilePath ?? string.Empty);
            row.CreateCell(1).SetCellValue(element?.Guid.ToString() ?? string.Empty);
            row.CreateCell(2).SetCellValue(element?.IfcEntity ?? string.Empty);
            row.CreateCell(3).SetCellValue(element?.Tag ?? string.Empty);
            row.CreateCell(4).SetCellValue(element?.Layer ?? string.Empty);

            row.CreateCell(5).SetCellValue(psetName ?? string.Empty);
            row.CreateCell(6).SetCellValue(propName ?? string.Empty);

            var valueCell = row.CreateCell(7);
            valueCell.SetCellValue(ToExcelString(value));
        }

        private static void WriteRow(
       IRow row,
       IfcFile file,
       IfcElement element,
       string psetName,
       string propName,
       object value,
       bool isFromPicklist)
        {
            // Column order:
            // 0 FilePath
            // 1 Guid
            // 2 IfcEntity
            // 3 Tag
            // 4 Layer
            // 5 PropertySetName
            // 6 PropertyName
            // 7 Value
            WriteRow(row, file, element, psetName, propName, value);
            row.CreateCell(8).SetCellValue(isFromPicklist);
        }

        private static string ToExcelString(object value)
        {
            if (value == null) return string.Empty;

            // If you have IFC types here, you might want to unwrap them.
            // For now we just stringify with some sensible handling.
            switch (value)
            {
                case DateTime dt:
                    return dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                case IFormattable fmt:
                    return fmt.ToString(null, CultureInfo.InvariantCulture);

                default:
                    return value.ToString() ?? string.Empty;
            }
        }
    }
}
