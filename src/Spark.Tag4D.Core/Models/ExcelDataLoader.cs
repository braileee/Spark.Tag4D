using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Spark.Tag4D.Core.Configuration;
using Spark.Tag4D.Core.Spreadsheets;
using Spark.Tag4D.Core.Utils;

namespace Spark.Tag4D.Core.Models
{
    /// <summary>Reads and writes the sheets of the parameters workbook.</summary>
    public static class ExcelDataLoader
    {
        // ----- Properties ---------------------------------------------------------------------------------------

        public static List<PropertySetItem> LoadPropertySetItems(string filePath, PropertiesSheet settings)
        {
            Worksheet? sheet = Workbook.Open(filePath).FindSheet(settings.SheetName);
            if (sheet == null)
            {
                return new List<PropertySetItem>();
            }

            int columnSet = FindColumn(sheet, settings.HeaderRowIndex, settings.PropertySetNameColumn);
            int columnName = FindColumn(sheet, settings.HeaderRowIndex, settings.PropertyNameColumn);
            int columnType = FindColumn(sheet, settings.HeaderRowIndex, settings.DataTypeColumn);

            var rows = new List<PropertyDataRow>();

            for (int row = settings.HeaderRowIndex + 1; row < sheet.RowCount; row++)
            {
                var item = new PropertyDataRow
                {
                    PropertySetName = Cell(sheet, row, columnSet),
                    PropertyName = Cell(sheet, row, columnName),
                    DataType = Cell(sheet, row, columnType)
                };

                if (!string.IsNullOrWhiteSpace(item.PropertySetName) || !string.IsNullOrWhiteSpace(item.PropertyName))
                {
                    rows.Add(item);
                }
            }

            return rows
                .GroupBy(r => r.PropertySetName)
                .Select(group => new PropertySetItem
                {
                    PropertySetName = group.Key,
                    PropertyItems = group
                        .Select(r => new PropertyItem { PropertyName = r.PropertyName, DataType = r.DataType })
                        .ToList()
                })
                .ToList();
        }

        public static List<string> GetPropertyNames(string filePath, PropertiesSheet settings)
        {
            Worksheet? sheet = Workbook.Open(filePath).FindSheet(settings.SheetName);
            if (sheet == null)
            {
                return new List<string>();
            }

            int columnName = FindColumn(sheet, settings.HeaderRowIndex, settings.PropertyNameColumn);

            return Enumerable.Range(settings.HeaderRowIndex + 1, Math.Max(0, sheet.RowCount - settings.HeaderRowIndex - 1))
                .Select(row => Cell(sheet, row, columnName))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();
        }

        public static void SavePropertySetItems(string filePath, PropertiesSheet settings, List<PropertySetItem> propertySets)
        {
            Workbook workbook = Workbook.Open(filePath);
            Worksheet sheet = workbook.FindSheet(settings.SheetName) ?? workbook.AddSheet(settings.SheetName);
            sheet.Clear();

            sheet.SetRow(settings.HeaderRowIndex, new object[]
            {
                settings.PropertySetNameColumn, settings.PropertyNameColumn, settings.DataTypeColumn
            });

            int row = settings.HeaderRowIndex + 1;
            foreach (PropertySetItem set in propertySets)
            {
                foreach (PropertyItem property in set.PropertyItems)
                {
                    sheet.SetRow(row++, new object[] { set.PropertySetName, property.PropertyName, property.DataType });
                }
            }

            workbook.Save(filePath);
        }

        // ----- Picklists ----------------------------------------------------------------------------------------

        public static List<PicklistGroup> LoadPicklistGroups(string filePath, PicklistSheet settings)
        {
            Worksheet? sheet = Workbook.Open(filePath).FindSheet(settings.SheetName);
            if (sheet == null || sheet.RowCount == 0)
            {
                return new List<PicklistGroup>();
            }

            var groups = new List<(int Column, PicklistGroup Group)>();
            for (int column = 0; column < sheet.GetColumnCount(0); column++)
            {
                string header = Cell(sheet, 0, column);
                if (header.Length > 0)
                {
                    groups.Add((column, new PicklistGroup { GroupName = header }));
                }
            }

            for (int row = 1; row < sheet.RowCount; row++)
            {
                foreach ((int column, PicklistGroup group) in groups)
                {
                    string value = Cell(sheet, row, column);
                    if (value.Length > 0)
                    {
                        group.Values.Add(value);
                    }
                }
            }

            foreach ((int _, PicklistGroup group) in groups)
            {
                group.Values = group.Values.Distinct().OrderBy(v => v, StringComparer.Ordinal).ToList();
            }

            return groups.Select(g => g.Group).ToList();
        }

        public static void SavePicklists(string path, string sheetName, List<PicklistGroup> picklists)
        {
            Workbook workbook = File.Exists(path) ? Workbook.Open(path) : Workbook.Create();

            // Rewrite the sheet from scratch so stale columns do not survive.
            workbook.RemoveSheet(sheetName);
            Worksheet sheet = workbook.AddSheet(sheetName);

            sheet.SetRow(0, picklists.Select(p => (object?)p.GroupName));

            int maxRows = picklists.Count == 0 ? 0 : picklists.Max(p => p.Values.Count);
            for (int index = 0; index < maxRows; index++)
            {
                for (int column = 0; column < picklists.Count; column++)
                {
                    if (index < picklists[column].Values.Count)
                    {
                        sheet.SetValue(index + 1, column, picklists[column].Values[index]);
                    }
                }
            }

            sheet.AutoSizeColumns(picklists.Count);
            workbook.Save(path);
        }

        // ----- Layer mappings -----------------------------------------------------------------------------------

        public static List<LayerMappingItem> ReadLayerMappings(string filePath, LayersMappingSheet settings)
        {
            Worksheet? sheet = Workbook.Open(filePath).FindSheet(settings.SheetName);
            if (sheet == null || sheet.RowCount == 0)
            {
                return new List<LayerMappingItem>();
            }

            var columns = new Dictionary<int, string>();
            int layerColumn = -1;

            for (int column = 0; column < sheet.GetColumnCount(0); column++)
            {
                string header = Cell(sheet, 0, column);
                if (header.Length == 0)
                {
                    continue;
                }

                columns[column] = header;
                if (header == settings.LayerColumnName)
                {
                    layerColumn = column;
                }
            }

            if (layerColumn < 0)
            {
                return new List<LayerMappingItem>();
            }

            var result = new List<LayerMappingItem>();
            for (int row = 1; row < sheet.RowCount; row++)
            {
                string layerName = Cell(sheet, row, layerColumn);
                if (layerName.Length == 0)
                {
                    continue;
                }

                var item = new LayerMappingItem { LayerName = layerName };
                foreach (KeyValuePair<int, string> column in columns.Where(c => c.Key != layerColumn))
                {
                    string value = Cell(sheet, row, column.Key);
                    if (value.Length > 0)
                    {
                        item.PropertiesWithValues[column.Value] = value;
                    }
                }

                result.Add(item);
            }

            return result;
        }

        // ----- Matches ------------------------------------------------------------------------------------------

        public static List<PropertyValueMatch> LoadPropertiesValueMatches(string filePath, PropertyMatchSheet settings)
        {
            var matches = new List<PropertyValueMatch>();
            Workbook workbook = Workbook.Open(filePath);

            foreach (string sheetName in settings.SheetNames)
            {
                Worksheet? sheet = workbook.FindSheet(sheetName);
                if (sheet == null || sheet.GetColumnCount(0) < 2)
                {
                    continue;
                }

                int targetColumn = sheet.GetColumnCount(0) - 1;
                string targetName = Cell(sheet, 0, targetColumn);

                for (int row = 1; row < sheet.RowCount; row++)
                {
                    string targetValue = Cell(sheet, row, targetColumn);
                    if (targetValue.Length == 0)
                    {
                        continue;
                    }

                    var source = new Dictionary<string, string>();
                    for (int column = 0; column < targetColumn; column++)
                    {
                        string value = Cell(sheet, row, column);
                        if (value.Length > 0)
                        {
                            source[Cell(sheet, 0, column)] = value;
                        }
                    }

                    if (source.Count > 0)
                    {
                        matches.Add(new PropertyValueMatch
                        {
                            PropertyNameAndValuesSource = source,
                            PropertyNameTarget = targetName,
                            PropertyValueTarget = targetValue
                        });
                    }
                }
            }

            return matches;
        }

        public static List<PropertyValueMatch> LoadPropertiesExactValueMatches(string filePath, PropertyExactMatchSheet settings)
        {
            var matches = new List<PropertyValueMatch>();
            Workbook workbook = Workbook.Open(filePath);

            foreach (string sheetName in settings.SheetNames)
            {
                Worksheet? sheet = workbook.FindSheet(sheetName);
                if (sheet == null || sheet.RowCount == 0)
                {
                    continue;
                }

                string sourceName = Cell(sheet, 0, 0);
                string targetName = Cell(sheet, 0, 1);

                for (int row = 1; row < sheet.RowCount; row++)
                {
                    string source = Cell(sheet, row, 0);
                    string target = Cell(sheet, row, 1);

                    if (source.Length == 0 || target.Length == 0)
                    {
                        continue;
                    }

                    matches.Add(new PropertyValueMatch
                    {
                        PropertyNameAndValuesSource = new Dictionary<string, string> { { sourceName, source } },
                        PropertyNameTarget = targetName,
                        PropertyValueTarget = target
                    });
                }
            }

            return matches;
        }

        // ----- Expressions --------------------------------------------------------------------------------------

        public static List<ExpressionRule> LoadExpressions(string filePath, ExpressionSheet settings)
        {
            var result = new List<ExpressionRule>();
            if (!File.Exists(filePath))
            {
                return result;
            }

            Worksheet? sheet = Workbook.Open(filePath).FindSheet(settings.SheetName);
            if (sheet == null)
            {
                return result;
            }

            // Columns are located by header so the sheet can be re-ordered freely; the historical
            // fixed layout (source, function, target) is the fallback.
            int source = FindColumnOrDefault(sheet, settings.HeaderRowIndex, settings.SourcePropertyColumnName, 0);
            int function = FindColumnOrDefault(sheet, settings.HeaderRowIndex, settings.FunctionColumnName, 1);
            int target = FindColumnOrDefault(sheet, settings.HeaderRowIndex, settings.TargetPropertyColumnName, 2);

            for (int row = settings.HeaderRowIndex + 1; row < sheet.RowCount; row++)
            {
                if (sheet.IsRowEmpty(row))
                {
                    continue;
                }

                result.Add(new ExpressionRule
                {
                    SourcePropertyName = Cell(sheet, row, source),
                    Function = Cell(sheet, row, function),
                    TargetPropertyName = Cell(sheet, row, target)
                });
            }

            return result;
        }

        public static void SaveRules(string filePath, ExpressionSheet settings, IEnumerable<ExpressionRule> rules)
        {
            Workbook workbook = Workbook.Open(filePath);
            Worksheet sheet = workbook.FindSheet(settings.SheetName) ?? workbook.AddSheet(settings.SheetName);
            sheet.Clear();

            sheet.SetRow(0, new object[] { "Source Property Name", "Function", "Target Property Name" });

            int row = 1;
            foreach (ExpressionRule rule in rules)
            {
                sheet.SetRow(row++, new object[] { rule.SourcePropertyName, rule.Function, rule.TargetPropertyName });
            }

            sheet.AutoSizeColumns(3);
            workbook.Save(filePath);
        }

        // ----- Composed properties ------------------------------------------------------------------------------

        public static List<ComposedPropertyItem> LoadComposed(string excelFilePath, ComposedSheet settings)
        {
            var items = new List<ComposedPropertyItem>();

            Worksheet? sheet = Workbook.Open(excelFilePath).FindSheet(settings.SheetName);
            if (sheet == null)
            {
                return items;
            }

            int nameColumn = FindColumn(sheet, settings.HeaderRowIndex, settings.ComposedPropertyColumnName);
            int formulaColumn = FindColumn(sheet, settings.HeaderRowIndex, settings.FormulaColumnName);

            for (int row = settings.HeaderRowIndex + 1; row < sheet.RowCount; row++)
            {
                string name = Cell(sheet, row, nameColumn);
                string formula = Cell(sheet, row, formulaColumn);

                if (name.Length > 0 && formula.Length > 0)
                {
                    items.Add(new ComposedPropertyItem { ComposedPropertyName = name, Formula = formula });
                }
            }

            return items;
        }

        public static void SaveComposed(string filePath, ComposedSheet settings, IEnumerable<ComposedPropertyItem> properties)
        {
            Workbook workbook = Workbook.Open(filePath);
            Worksheet sheet = workbook.FindSheet(settings.SheetName) ?? workbook.AddSheet(settings.SheetName);
            sheet.Clear();

            sheet.SetRow(0, new object[] { settings.ComposedPropertyColumnName, settings.FormulaColumnName });

            int row = 1;
            foreach (ComposedPropertyItem item in properties)
            {
                sheet.SetRow(row++, new object[] { item.ComposedPropertyName, item.Formula });
            }

            workbook.Save(filePath);
        }

        // ----- File location ------------------------------------------------------------------------------------

        /// <summary>
        /// Resolves the workbook to use: the custom file when it exists, otherwise the one shipped with the product.
        /// The result is remembered in the user settings.
        /// </summary>
        public static string LoadOrPromptExcelFilePath(FileLinkSettings fileLinkSettings)
        {
            string excelFilePath = string.Empty;

            if (File.Exists(fileLinkSettings.CustomExcelFilePath))
            {
                excelFilePath = fileLinkSettings.CustomExcelFilePath;
            }
            else if (fileLinkSettings.LoadDefault)
            {
                excelFilePath = GetDefaultExcelFilePath();
            }

            SavePath(excelFilePath);
            return excelFilePath;
        }

        public static string GetDefaultExcelFilePath()
        {
            return Path.Combine(
                AssemblyUtils.GetFolder(typeof(ExcelDataLoader)),
                Constants.FilesFolder,
                Constants.Tag4DFolder,
                Constants.ExcelSettingsFileName);
        }

        public static void SavePath(string path)
        {
            UserSettings.Current.ExcelFilePath = path;
            UserSettings.Current.Save();
        }

        public static string GetPath() => UserSettings.Current.ExcelFilePath;

        // ----- Helpers ------------------------------------------------------------------------------------------

        private static string Cell(Worksheet sheet, int row, int column)
        {
            return column < 0 ? string.Empty : sheet.GetText(row, column).Trim();
        }

        private static int FindColumn(Worksheet sheet, int headerRow, string headerText)
        {
            for (int column = 0; column < sheet.GetColumnCount(headerRow); column++)
            {
                if (sheet.GetText(headerRow, column).Trim() == headerText)
                {
                    return column;
                }
            }

            return -1;
        }

        private static int FindColumnOrDefault(Worksheet sheet, int headerRow, string headerText, int fallback)
        {
            int column = string.IsNullOrEmpty(headerText) ? -1 : FindColumn(sheet, headerRow, headerText);
            return column >= 0 ? column : fallback;
        }
    }
}
