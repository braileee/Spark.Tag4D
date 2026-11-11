using System;
using System.Collections.Generic;
using System.Linq;
using Spark.Tag4D.Core.Configuration;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Core.Spreadsheets;
using Spark.Tag4D.Settings.Models;

namespace Spark.Tag4D.Settings.Services
{
    /// <summary>Reads and writes the "&lt;name&gt; Exact Match" sheets: two columns, source value to target value.</summary>
    public class ExactMatchExcelService
    {
        private const string ExactMatchSuffix = " Exact Match";

        public List<ExactMatchTable> Load(string filePath, SettingsRoot settingsRoot)
        {
            List<string> columnNames = ExcelDataLoader.GetPropertyNames(filePath, settingsRoot.ExcelSettings.PropertiesSheet);
            Workbook workbook = Workbook.Open(filePath);

            var tables = new List<ExactMatchTable>();

            foreach (Worksheet sheet in workbook.Sheets.Where(IsExactMatchSheet))
            {
                string sourceHeader = sheet.GetText(0, 0);
                string targetHeader = sheet.GetText(0, 1);

                var table = new ExactMatchTable
                {
                    SheetPrefix = sheet.Name.Substring(0, sheet.Name.Length - ExactMatchSuffix.Length),
                    AvailableColumnNames = columnNames,
                    SourceColumnHeader = string.IsNullOrEmpty(sourceHeader) ? "Source" : sourceHeader,
                    TargetColumnHeader = string.IsNullOrEmpty(targetHeader) ? "Target" : targetHeader
                };

                // Data rows run until the first completely empty row.
                for (int row = 1; row < sheet.RowCount && !sheet.IsRowEmpty(row); row++)
                {
                    table.Rows.Add(new ExactMatchRow
                    {
                        Source = sheet.GetText(row, 0).Trim(),
                        Target = sheet.GetText(row, 1).Trim()
                    });
                }

                Validate(table);
                tables.Add(table);
            }

            return tables;
        }

        public void Save(string filePath, IEnumerable<ExactMatchTable> tables)
        {
            Workbook workbook = Workbook.Open(filePath);
            List<ExactMatchTable> tableList = tables.ToList();

            // Validate everything first so an invalid table cannot leave a half-written workbook behind.
            tableList.ForEach(Validate);

            var validNames = new HashSet<string>(
                tableList.Select(t => t.SheetPrefix + ExactMatchSuffix),
                StringComparer.OrdinalIgnoreCase);

            foreach (string stale in workbook.Sheets.Where(IsExactMatchSheet).Select(s => s.Name).Where(n => !validNames.Contains(n)).ToList())
            {
                workbook.RemoveSheet(stale);
            }

            foreach (ExactMatchTable table in tableList)
            {
                string sheetName = table.SheetPrefix + ExactMatchSuffix;
                Worksheet sheet = workbook.FindSheet(sheetName) ?? workbook.AddSheet(sheetName);
                sheet.Clear();

                sheet.SetRow(0, new object[] { table.SourceColumnHeader, table.TargetColumnHeader });

                int rowIndex = 1;
                foreach (ExactMatchRow row in table.Rows)
                {
                    sheet.SetRow(rowIndex++, new object[] { row.Source, row.Target });
                }

                sheet.AutoSizeColumns(2);
            }

            workbook.Save(filePath);
        }

        private static bool IsExactMatchSheet(Worksheet sheet)
        {
            return sheet.Name.EndsWith(ExactMatchSuffix, StringComparison.OrdinalIgnoreCase);
        }

        private static void Validate(ExactMatchTable table)
        {
            List<string> duplicates = table.Rows
                .Where(row => !string.IsNullOrWhiteSpace(row.Source))
                .GroupBy(row => row.Source)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();

            if (duplicates.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Table '{table.Name}' contains duplicate Source values: {string.Join(", ", duplicates)}");
            }
        }
    }
}
