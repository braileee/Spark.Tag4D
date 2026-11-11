using Spark.Tag4D.Core.Enums;
using Spark.Tag4D.Core.Json;
using Spark.Tag4D.Core.Utils;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Core.Utils;
using NPOI.SS.UserModel;
using NPOI.Util.Collections;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Core.Models
{
    public static class ExcelDataLoader
    {
        public static List<PropertySetItem> LoadPropertySetItems(string filePath, PropertiesSheet settings)
        {
            var rows = new List<PropertyDataRow>();

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = new XSSFWorkbook(fs);
                ISheet sheet = workbook.GetSheet(settings.SheetName);

                if (sheet == null)
                {
                    return new List<PropertySetItem>();
                }

                // Assuming first row is header
                int headerRowIndex = settings.HeaderRowIndex;
                IRow headerRow = sheet.GetRow(headerRowIndex);

                int colSet = headerRow.Cells.FindIndex(c => c.StringCellValue == settings.PropertySetNameColumn);
                int colName = headerRow.Cells.FindIndex(c => c.StringCellValue == settings.PropertyNameColumn);
                int colType = headerRow.Cells.FindIndex(c => c.StringCellValue == settings.DataTypeColumn);

                for (int i = headerRowIndex + 1; i <= sheet.LastRowNum; i++)
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;

                    var item = new PropertyDataRow
                    {
                        PropertySetName = row.GetCell(colSet)?.ToString()?.Trim(),
                        PropertyName = row.GetCell(colName)?.ToString()?.Trim(),
                        DataType = row.GetCell(colType)?.ToString()?.Trim()
                    };

                    // Skip empty rows
                    if (string.IsNullOrWhiteSpace(item.PropertySetName) &&
                        string.IsNullOrWhiteSpace(item.PropertyName))
                        continue;

                    rows.Add(item);
                }
            }


            var propertySets = rows.GroupBy(r => r.PropertySetName).Select(g => new PropertySetItem
            {
                PropertySetName = g.Key,
                PropertyItems = g
                                        .Select(r => new PropertyItem
                                        {
                                            PropertyName = r.PropertyName,
                                            DataType = r.DataType
                                        })
                                        .ToList()
            })
                                .ToList();

            return propertySets;

        }

        public static List<string> GetPropertyNames(string filePath, PropertiesSheet propertiesSheet)
        {
            var propertyNames = new List<string>();

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = new XSSFWorkbook(fs);
                ISheet sheet = workbook.GetSheet(propertiesSheet.SheetName);

                if (sheet == null)
                    return propertyNames;

                int headerRowIndex = propertiesSheet.HeaderRowIndex;
                IRow headerRow = sheet.GetRow(headerRowIndex);

                int colName = headerRow.Cells.FindIndex(c => c.StringCellValue == propertiesSheet.PropertyNameColumn);

                for (int i = headerRowIndex + 1; i <= sheet.LastRowNum; i++)
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;

                    var propertyName = row.GetCell(colName)?.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(propertyName))
                        propertyNames.Add(propertyName);
                }
            }

            return propertyNames.OrderBy(item => item).ToList();
        }

        public static void SavePropertySetItems(string filePath, PropertiesSheet settings, List<PropertySetItem> propertySets)
        {
            IWorkbook workbook;

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                workbook = new XSSFWorkbook(fs);
            }

            var sheet = workbook.GetSheet(settings.SheetName) ?? workbook.CreateSheet(settings.SheetName);

            // Clear sheet
            for (int i = sheet.LastRowNum; i >= 0; i--)
            {
                var row = sheet.GetRow(i);
                if (row != null)
                    sheet.RemoveRow(row);
            }

            int headerRowIndex = settings.HeaderRowIndex;
            var headerRow = sheet.CreateRow(headerRowIndex);

            headerRow.CreateCell(0).SetCellValue(settings.PropertySetNameColumn);
            headerRow.CreateCell(1).SetCellValue(settings.PropertyNameColumn);
            headerRow.CreateCell(2).SetCellValue(settings.DataTypeColumn);

            int rowIndex = headerRowIndex + 1;

            foreach (var set in propertySets)
            {
                foreach (var prop in set.PropertyItems)
                {
                    var row = sheet.CreateRow(rowIndex++);

                    row.CreateCell(0).SetCellValue(set.PropertySetName);
                    row.CreateCell(1).SetCellValue(prop.PropertyName);
                    row.CreateCell(2).SetCellValue(prop.DataType);
                }
            }

            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fs);
            }
        }

        public static List<PicklistGroup> LoadPicklistGroups(string filePath, PicklistSheet settings)
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = new XSSFWorkbook(fs);

                ISheet sheet = workbook.GetSheet(settings.SheetName);
                if (sheet == null)
                    return new List<PicklistGroup>();

                // --- Header row ---
                IRow headerRow = sheet.GetRow(0);
                if (headerRow == null)
                    return new List<PicklistGroup>();

                int columnCount = headerRow.LastCellNum;

                // --- Prepare groups ---
                var groups = new Dictionary<int, PicklistGroup>();

                for (int c = 0; c < columnCount; c++)
                {
                    string header = headerRow.GetCell(c)?.ToString()?.Trim();
                    if (string.IsNullOrWhiteSpace(header))
                        continue;

                    groups[c] = new PicklistGroup
                    {
                        GroupName = header
                    };
                }

                // --- Read data rows ---
                for (int r = 1; r <= sheet.LastRowNum; r++)
                {
                    IRow row = sheet.GetRow(r);
                    if (row == null)
                        continue;

                    foreach (var kvp in groups)
                    {
                        int colIndex = kvp.Key;
                        PicklistGroup group = kvp.Value;

                        string value = row.GetCell(colIndex)?.ToString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(value))
                            group.Values.Add(value);
                    }
                }

                // --- Deduplicate & sort ---
                foreach (var group in groups.Values)
                {
                    group.Values = group.Values
                        .Distinct()
                        .OrderBy(v => v)
                        .ToList();
                }

                return groups.Values.ToList();
            }

        }

        public static void SavePicklists(
    string path,
    string sheetName,
    List<PicklistGroup> picklists)
        {
            IWorkbook workbook;

            if (File.Exists(path))
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                workbook = new XSSFWorkbook(fs);
            }
            else
            {
                workbook = new XSSFWorkbook();
            }

            // Remove sheet if exists
            var existing = workbook.GetSheet(sheetName);
            if (existing != null)
            {
                int index = workbook.GetSheetIndex(existing);
                workbook.RemoveSheetAt(index);
            }

            var sheet = workbook.CreateSheet(sheetName);

            // ✅ Header row
            var header = sheet.CreateRow(0);

            for (int col = 0; col < picklists.Count; col++)
            {
                header.CreateCell(col).SetCellValue(picklists[col].GroupName);
            }

            // ✅ Find max row count
            int maxRows = picklists.Max(p => p.Values.Count);

            // ✅ Fill rows
            for (int rowIndex = 0; rowIndex < maxRows; rowIndex++)
            {
                var row = sheet.CreateRow(rowIndex + 1);

                for (int col = 0; col < picklists.Count; col++)
                {
                    var list = picklists[col];
                    if (rowIndex < list.Values.Count)
                    {
                        row.CreateCell(col).SetCellValue(list.Values[rowIndex]);
                    }
                }
            }

            // ✅ Autosize
            for (int col = 0; col < picklists.Count; col++)
            {
                sheet.AutoSizeColumn(col);
            }

            // ✅ Save file
            using var writeFs = new FileStream(path, FileMode.Create, FileAccess.Write);
            workbook.Write(writeFs);
        }

        public static List<LayerMappingItem> ReadLayerMappings(
                                           string filePath,
                                          LayersMappingSheet settings)
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = new XSSFWorkbook(fs);

                ISheet sheet = workbook.GetSheet(settings.SheetName);
                if (sheet == null)
                    return new List<LayerMappingItem>();

                IRow headerRow = sheet.GetRow(0);
                if (headerRow == null)
                    return new List<LayerMappingItem>();

                int columnCount = headerRow.LastCellNum;

                // Map column index → column name
                var columns = new Dictionary<int, string>();
                int layerNameColIndex = -1;

                for (int c = 0; c < columnCount; c++)
                {
                    string header = headerRow.GetCell(c)?.ToString()?.Trim();
                    if (string.IsNullOrWhiteSpace(header))
                        continue;

                    columns[c] = header;

                    if (header == settings.LayerColumnName)
                        layerNameColIndex = c;
                }

                if (layerNameColIndex == -1)
                    return new List<LayerMappingItem>();

                var result = new List<LayerMappingItem>();

                // Read rows
                for (int r = 1; r <= sheet.LastRowNum; r++)
                {
                    IRow row = sheet.GetRow(r);
                    if (row == null)
                        continue;

                    string layerName = row.GetCell(layerNameColIndex)?.ToString()?.Trim();
                    if (string.IsNullOrWhiteSpace(layerName))
                        continue;

                    var item = new LayerMappingItem
                    {
                        LayerName = layerName
                    };

                    foreach (var col in columns)
                    {
                        if (col.Key == layerNameColIndex)
                            continue;

                        string value = row.GetCell(col.Key)?.ToString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(value))
                            item.PropertiesWithValues[col.Value] = value;
                    }

                    result.Add(item);
                }

                return result;
            }
        }

        public static List<PropertyValueMatch> LoadPropertiesValueMatches(
    string filePath,
    PropertyMatchSheet settings)
        {
            var propertyValueMatches = new List<PropertyValueMatch>();

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = new XSSFWorkbook(fs);

                foreach (string sheetName in settings.SheetNames)
                {
                    ISheet sheet = workbook.GetSheet(sheetName);
                    if (sheet == null)
                        continue;

                    // ---- READ HEADER ----
                    IRow header = sheet.GetRow(0);
                    if (header == null)
                        continue;

                    int lastColIndex = header.LastCellNum - 1;

                    // All source column names (0..last-1)
                    List<string> sourcePropertyNames = new List<string>();
                    for (int col = 0; col < lastColIndex; col++)
                    {
                        sourcePropertyNames.Add(header.GetCell(col)?.ToString());
                    }

                    // Target name = last column
                    string propertyNameTarget = header.GetCell(lastColIndex)?.ToString();

                    // ---- READ ROWS ----
                    for (int i = 1; i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null)
                            continue;

                        string targetValue = row.GetCell(lastColIndex)?.ToString();
                        if (string.IsNullOrWhiteSpace(targetValue))
                            continue;

                        // Build dictionary of all source columns that have values
                        var sourceDict = new Dictionary<string, string>();

                        for (int col = 0; col < lastColIndex; col++)
                        {
                            string valueSource = row.GetCell(col)?.ToString();

                            if (!string.IsNullOrWhiteSpace(valueSource))
                                sourceDict[sourcePropertyNames[col]] = valueSource;
                        }

                        if (sourceDict.Count == 0)
                            continue;

                        propertyValueMatches.Add(new PropertyValueMatch
                        {
                            PropertyNameAndValuesSource = sourceDict,
                            PropertyNameTarget = propertyNameTarget,
                            PropertyValueTarget = targetValue
                        });
                    }
                }
            }

            return propertyValueMatches;
        }

        public static string LoadOrPromptExcelFilePath(FileLinkSettings fileLinkSettings)
        {
            string excelFilePath = string.Empty;

            if (File.Exists(fileLinkSettings.CustomExcelFilePath))
            {
                excelFilePath = fileLinkSettings.CustomExcelFilePath;
            }
            else
            {
                if (fileLinkSettings.LoadDefault)
                {
                    string assemblyFolder = AssemblyUtils.GetFolder(typeof(ExcelDataLoader));
                    excelFilePath = System.IO.Path.Combine(assemblyFolder, Constants.FilesFolder, Constants.Tag4DFolder, Constants.ExcelSettingsFileName);
                }
            }

            Properties.Settings.Default.ExcelFilePath = excelFilePath;
            Properties.Settings.Default.Save();

            return excelFilePath;
        }

        public static List<ExpressionRule> LoadExpressions(string filePath, ExpressionSheet settings)
        {
            var result = new List<ExpressionRule>();

            if (!File.Exists(filePath))
                return result;

            using var stream = File.OpenRead(filePath);

            IWorkbook workbook = new XSSFWorkbook(stream);

            var sheet = workbook.GetSheet(settings.SheetName);

            if (sheet == null)
                return result;

            for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);

                if (row == null)
                    continue;

                result.Add(new ExpressionRule
                {
                    SourcePropertyName = row.GetCell(0)?.ToString() ?? string.Empty,
                    Function = row.GetCell(1)?.ToString() ?? string.Empty,
                    TargetPropertyName = row.GetCell(2)?.ToString() ?? string.Empty
                });
            }

            return result;
        }

        public static void SaveRules(string filePath, ExpressionSheet settings, IEnumerable<ExpressionRule> rules)
        {
            IWorkbook workbook = XSSFWorkbookUtils.OpenExcelFile(filePath, FileAccess.ReadWrite);

            var sheet = workbook.GetSheet(settings.SheetName)
                        ?? workbook.CreateSheet(settings.SheetName);

            // Clear existing data
            for (int i = sheet.LastRowNum; i >= 0; i--)
            {
                var row = sheet.GetRow(i);

                if (row != null)
                {
                    sheet.RemoveRow(row);
                }
            }

            // Header
            var headerRow = sheet.CreateRow(0);

            headerRow.CreateCell(0).SetCellValue("Source Property Name");
            headerRow.CreateCell(1).SetCellValue("Function");
            headerRow.CreateCell(2).SetCellValue("Target Property Name");

            int rowIndex = 1;

            // Data
            foreach (var rule in rules)
            {
                var row = sheet.CreateRow(rowIndex++);

                row.CreateCell(0).SetCellValue(rule.SourcePropertyName);
                row.CreateCell(1).SetCellValue(rule.Function);
                row.CreateCell(2).SetCellValue(rule.TargetPropertyName);
            }

            sheet.AutoSizeColumn(0);
            sheet.AutoSizeColumn(1);
            sheet.AutoSizeColumn(2);

            using (var fs = new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.Write))
            {
                workbook.Write(fs);
            }
        }


        public static List<ComposedPropertyItem> LoadComposed(string excelFilePath, ComposedSheet settings)
        {
            var composedItems = new List<ComposedPropertyItem>();
            using (var fs = new FileStream(excelFilePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = new XSSFWorkbook(fs);
                ISheet sheet = workbook.GetSheet(settings.SheetName);

                if (sheet == null)
                {
                    return composedItems;
                }

                int headerRowIndex = settings.HeaderRowIndex;
                IRow headerRow = sheet.GetRow(headerRowIndex);
                int cellIndexName = headerRow.Cells.FindIndex(c => c.StringCellValue == settings.ComposedPropertyColumnName);
                int cellIndexFormula = headerRow.Cells.FindIndex(c => c.StringCellValue == settings.FormulaColumnName);

                for (int i = headerRowIndex + 1; i <= sheet.LastRowNum; i++)
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null)
                    {
                        continue;
                    }

                    string valuePropertyName = row.GetCell(cellIndexName)?.ToString()?.Trim();
                    string valueFormula = row.GetCell(cellIndexFormula)?.ToString()?.Trim();

                    if (string.IsNullOrEmpty(valuePropertyName) || string.IsNullOrEmpty(valueFormula))
                    {
                        continue;
                    }

                    composedItems.Add(new ComposedPropertyItem
                    {
                        ComposedPropertyName = valuePropertyName,
                        Formula = valueFormula
                    });
                }
            }
            return composedItems;
        }

        public static void SaveComposed(
       string filePath,  ComposedSheet settings,
       IEnumerable<ComposedPropertyItem> properties)
        {
            XSSFWorkbook workbook;

            using (var read = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                workbook = new XSSFWorkbook(read);
            }

            var sheet = workbook.GetSheet(settings.SheetName);

            while (sheet.LastRowNum > 0)
            {
                sheet.RemoveRow(sheet.GetRow(sheet.LastRowNum));
                sheet.ShiftRows(
                    sheet.LastRowNum,
                    sheet.LastRowNum,
                    -1);
            }

            var header = sheet.GetRow(0) ?? sheet.CreateRow(0);

            header.CreateCell(0).SetCellValue(settings.ComposedPropertyColumnName);
            header.CreateCell(1).SetCellValue(settings.FormulaColumnName);

            int rowIndex = 1;

            foreach (var item in properties)
            {
                var row = sheet.CreateRow(rowIndex++);

                row.CreateCell(0).SetCellValue(item.ComposedPropertyName);
                row.CreateCell(1).SetCellValue(item.Formula);
            }

            using var write = File.Create(filePath);

            workbook.Write(write);
        }

        public static List<PropertyValueMatch> LoadPropertiesExactValueMatches(string filePath, PropertyExactMatchSheet settings)
        {
            List<PropertyValueMatch> propertyValueMatches = new List<PropertyValueMatch>();

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = new XSSFWorkbook(fs);

                List<string> sheetNames = settings.SheetNames;

                foreach (string sheetName in sheetNames)
                {
                    ISheet sheet = workbook.GetSheet(sheetName);
                    if (sheet == null)
                    {
                        continue;
                    }

                    // Read header row
                    IRow header = sheet.GetRow(0);
                    string propertyNameSource = header.GetCell(0).ToString();
                    string propertyNameTarget = header.GetCell(1).ToString();

                    // Iterate rows
                    for (int i = 1; i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;

                        string valueSource = row.GetCell(0)?.ToString();
                        string valueTarget = row.GetCell(1)?.ToString();

                        if (string.IsNullOrWhiteSpace(valueSource) || string.IsNullOrWhiteSpace(valueTarget))
                            continue;

                        propertyValueMatches.Add(new PropertyValueMatch
                        {
                            PropertyNameAndValuesSource = new Dictionary<string, string> { { propertyNameSource, valueSource } },
                            PropertyNameTarget = propertyNameTarget,
                            PropertyValueTarget = valueTarget
                        });
                    }
                }
            }

            return propertyValueMatches;
        }

        public static void SavePath(string path)
        {
            Properties.Settings.Default.ExcelFilePath = path;
            Properties.Settings.Default.Save();
        }

        public static string GetPath()
        {
            return Properties.Settings.Default.ExcelFilePath;
        }
    }
}
