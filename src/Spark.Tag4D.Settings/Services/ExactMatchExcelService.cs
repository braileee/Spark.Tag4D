using Spark.Tag4D.Core.Configuration;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Core.Utils;
using Spark.Tag4D.Settings.Models;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Settings.Services
{
    public class ExactMatchExcelService 
    {
        private const string ExactMatchSuffix = " Exact Match";

        public List<ExactMatchTable> Load(string filePath, SettingsRoot settingsRoot)
        {
            List<string> columnNames = ExcelDataLoader.GetPropertyNames(filePath, settingsRoot.ExcelSettings.PropertiesSheet);

            var tables = new List<ExactMatchTable>();

            using var stream = File.Open(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);

            IWorkbook workbook = new XSSFWorkbook(stream);

            for (int i = 0; i < workbook.NumberOfSheets; i++)
            {
                var sheet = workbook.GetSheetAt(i);

                if (!sheet.SheetName.EndsWith(
                        ExactMatchSuffix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var headerRow = sheet.GetRow(0);

                var table = new ExactMatchTable
                {
                    SheetPrefix =
                    sheet.SheetName.Substring(
                        0,
                        sheet.SheetName.Length -
                        ExactMatchSuffix.Length),

                    SourceColumnHeader = columnNames.FirstOrDefault(item => item ==  headerRow?.GetCell(0)?.ToString()),
                    TargetColumnHeader = columnNames.FirstOrDefault(item => item == headerRow?.GetCell(1)?.ToString()),
                    AvailableColumnNames = columnNames
                };


                table.SourceColumnHeader =
                    headerRow?.GetCell(0)?.ToString()
                    ?? "Source";

                table.TargetColumnHeader =
                    headerRow?.GetCell(1)?.ToString()
                    ?? "Target";

                int rowIndex = 1;

                while (true)
                {
                    var row = sheet.GetRow(rowIndex);

                    if (row == null)
                    {
                        break;
                    }

                    string source =
                        row.GetCell(0)?.ToString()?.Trim()
                        ?? string.Empty;

                    string target =
                        row.GetCell(1)?.ToString()?.Trim()
                        ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(source)
                        && string.IsNullOrWhiteSpace(target))
                    {
                        break;
                    }

                    table.Rows.Add(new ExactMatchRow
                    {
                        Source = source,
                        Target = target
                    });

                    rowIndex++;
                }

                Validate(table);

                tables.Add(table);
            }

            return tables;
        }

        public void Save(
            string filePath,
            IEnumerable<ExactMatchTable> tables)
        {
            XSSFWorkbook workbook = XSSFWorkbookUtils.OpenExcelFile(filePath, FileAccess.ReadWrite);

            var tableList = tables.ToList();

            RemoveDeletedExactMatchSheets(
                workbook,
                tableList);

            foreach (var table in tableList)
            {
                Validate(table);

                string sheetName = $"{table.SheetPrefix}{ExactMatchSuffix}";

                ISheet? sheet =
                    workbook.GetSheet(sheetName);

                if (sheet == null)
                {
                    sheet = workbook.CreateSheet(sheetName);
                }
                else
                {
                    ClearSheet(sheet);
                }

                var headerRow = sheet.CreateRow(0);

                headerRow.CreateCell(0)
                    .SetCellValue(
                        table.SourceColumnHeader);

                headerRow.CreateCell(1)
                    .SetCellValue(
                        table.TargetColumnHeader);

                int rowIndex = 1;

                foreach (var item in table.Rows)
                {
                    var row =
                        sheet.CreateRow(rowIndex++);

                    row.CreateCell(0)
                        .SetCellValue(item.Source);

                    row.CreateCell(1)
                        .SetCellValue(item.Target);
                }

                sheet.AutoSizeColumn(0);
                sheet.AutoSizeColumn(1);
            }

            XSSFWorkbookUtils.WriteAndClose(workbook, filePath);
        }

        private static void ClearSheet(ISheet sheet)
        {
            for (int i = sheet.LastRowNum; i >= 0; i--)
            {
                var row = sheet.GetRow(i);

                if (row != null)
                {
                    sheet.RemoveRow(row);
                }
            }
        }

        private static void RemoveDeletedExactMatchSheets(
            IWorkbook workbook,
            IEnumerable<ExactMatchTable> tables)
        {
            var validSheetNames =
                tables
                    .Select(t =>
    $"{t.SheetPrefix}{ExactMatchSuffix}")
                    .ToHashSet(
                        StringComparer.OrdinalIgnoreCase);

            for (int i = workbook.NumberOfSheets - 1;
                 i >= 0;
                 i--)
            {
                var sheet =
                    workbook.GetSheetAt(i);

                if (!sheet.SheetName.EndsWith(
                        ExactMatchSuffix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!validSheetNames.Contains(
                        sheet.SheetName))
                {
                    workbook.RemoveSheetAt(i);
                }
            }
        }

        private static void Validate(
            ExactMatchTable table)
        {
            var duplicates =
                table.Rows
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(
                            x.Source))
                    .GroupBy(x => x.Source)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

            if (duplicates.Any())
            {
                throw new InvalidOperationException(
                    $"Table '{table.Name}' contains duplicate Source values: " +
                    string.Join(", ", duplicates));
            }
        }
    }
}
