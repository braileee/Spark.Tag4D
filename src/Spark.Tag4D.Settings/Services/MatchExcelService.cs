using System;
using System.Collections.Generic;
using System.Linq;
using Spark.Tag4D.Core.Configuration;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Core.Spreadsheets;
using Spark.Tag4D.Settings.Models;

namespace Spark.Tag4D.Settings.Services
{
    /// <summary>
    /// Reads and writes the "&lt;name&gt; Match" sheets: every column but the last holds a source property,
    /// the last column holds the target property whose value is derived from them.
    /// </summary>
    public class MatchExcelService
    {
        private const string MatchSuffix = " Match";
        private const string ExactMatchSuffix = " Exact Match";

        public List<MatchTable> Load(string filePath, SettingsRoot settingsRoot)
        {
            List<string> propertyNames = ExcelDataLoader.GetPropertyNames(filePath, settingsRoot.ExcelSettings.PropertiesSheet);
            Workbook workbook = Workbook.Open(filePath);

            var tables = new List<MatchTable>();

            foreach (Worksheet sheet in workbook.Sheets.Where(IsMatchSheet))
            {
                int columnCount = sheet.GetColumnCount(0);
                if (sheet.RowCount == 0 || columnCount < 2)
                {
                    continue;
                }

                var table = new MatchTable
                {
                    SheetPrefix = sheet.Name.Substring(0, sheet.Name.Length - MatchSuffix.Length),
                    AvailableProperties = propertyNames
                };

                for (int column = 0; column < columnCount - 1; column++)
                {
                    table.SourceProperties.Add(Canonical(propertyNames, sheet.GetText(0, column)));
                }

                table.TargetProperty = Canonical(propertyNames, sheet.GetText(0, columnCount - 1));

                // Data rows run until the first completely empty row.
                for (int row = 1; row < sheet.RowCount && !sheet.IsRowEmpty(row); row++)
                {
                    var matchRow = new MatchRow();

                    for (int column = 0; column < columnCount - 1; column++)
                    {
                        matchRow.Criteria.Add(new MatchCriterion
                        {
                            PropertyName = table.SourceProperties[column],
                            Value = sheet.GetText(row, column).Trim()
                        });
                    }

                    matchRow.TargetValue = sheet.GetText(row, columnCount - 1).Trim();
                    table.Rows.Add(matchRow);
                }

                tables.Add(table);
            }

            return tables;
        }

        public void Save(string filePath, IEnumerable<MatchTable> tables)
        {
            Workbook workbook = Workbook.Open(filePath);
            List<MatchTable> tableList = tables.ToList();

            var validNames = new HashSet<string>(
                tableList.Select(t => t.SheetPrefix + MatchSuffix),
                StringComparer.OrdinalIgnoreCase);

            // Drop sheets of tables that were deleted in the editor.
            foreach (string stale in workbook.Sheets.Where(IsMatchSheet).Select(s => s.Name).Where(n => !validNames.Contains(n)).ToList())
            {
                workbook.RemoveSheet(stale);
            }

            foreach (MatchTable table in tableList)
            {
                string sheetName = table.SheetPrefix + MatchSuffix;
                Worksheet sheet = workbook.FindSheet(sheetName) ?? workbook.AddSheet(sheetName);
                sheet.Clear();

                var header = table.SourceProperties.Cast<object>().ToList();
                header.Add(table.TargetProperty);
                sheet.SetRow(0, header);

                int rowIndex = 1;
                foreach (MatchRow row in table.Rows)
                {
                    var values = row.Criteria.Select(c => (object)c.Value).ToList();
                    values.Add(row.TargetValue);
                    sheet.SetRow(rowIndex++, values);
                }

                sheet.AutoSizeColumns(table.SourceProperties.Count + 1);
            }

            workbook.Save(filePath);
        }

        private static bool IsMatchSheet(Worksheet sheet)
        {
            return sheet.Name.EndsWith(MatchSuffix, StringComparison.OrdinalIgnoreCase)
                   && !sheet.Name.EndsWith(ExactMatchSuffix, StringComparison.OrdinalIgnoreCase);
        }

        private static string Canonical(IEnumerable<string> propertyNames, string header)
        {
            return propertyNames.FirstOrDefault(name => name == header) ?? header;
        }
    }
}
