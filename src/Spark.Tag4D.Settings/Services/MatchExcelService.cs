using Spark.Tag4D.Core.Json;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Core.Utils;
using Spark.Tag4D.Settings.Models;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Spark.Tag4D.Settings.Services
{
    public class MatchExcelService
    {
        private const string MatchSuffix = " Match";
        private const string ExactMatchSuffix = " Exact Match";

        public List<MatchTable> Load(
            string filePath,
            SettingsRoot settingsRoot)
        {
            List<string> propertyNames =
                ExcelDataLoader.GetPropertyNames(
                    filePath,
                    settingsRoot.ExcelSettings.PropertiesSheet);

            var tables = new List<MatchTable>();

            using var stream = File.Open(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);

            IWorkbook workbook =
                new XSSFWorkbook(stream);

            for (int i = 0; i < workbook.NumberOfSheets; i++)
            {
                var sheet =
                    workbook.GetSheetAt(i);

                if (!sheet.SheetName.EndsWith(
                        MatchSuffix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (sheet.SheetName.EndsWith(
                        ExactMatchSuffix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var headerRow =
                    sheet.GetRow(0);

                if (headerRow == null)
                {
                    continue;
                }

                int columnCount =
                    headerRow.LastCellNum;

                if (columnCount < 2)
                {
                    continue;
                }

                var table = new MatchTable
                {
                    SheetPrefix =
                        sheet.SheetName.Substring(
                            0,
                            sheet.SheetName.Length -
                            MatchSuffix.Length),

                    AvailableProperties =
                        propertyNames
                };

                // Source properties

                for (int columnIndex = 0;
                     columnIndex < columnCount - 1;
                     columnIndex++)
                {
                    string header =
                        headerRow.GetCell(columnIndex)?
                            .ToString()
                        ?? string.Empty;

                    table.SourceProperties.Add(
                        propertyNames.FirstOrDefault(
                            p => p == header) ?? header);
                }

                // Target property

                string targetHeader =
                    headerRow.GetCell(columnCount - 1)?
                        .ToString()
                    ?? string.Empty;

                table.TargetProperty =
                    propertyNames.FirstOrDefault(
                        p => p == targetHeader)
                    ?? targetHeader;

                int rowIndex = 1;

                while (true)
                {
                    IRow? row =
                        sheet.GetRow(rowIndex);

                    if (row == null)
                    {
                        break;
                    }

                    bool hasData = false;

                    var matchRow =
                        new MatchRow();

                    for (int columnIndex = 0;
                         columnIndex < columnCount - 1;
                         columnIndex++)
                    {
                        string value =
                            row.GetCell(columnIndex)?
                                .ToString()?
                                .Trim()
                            ?? string.Empty;

                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            hasData = true;
                        }

                        matchRow.Criteria.Add(
                            new MatchCriterion
                            {
                                PropertyName =
                                    table.SourceProperties[columnIndex],
                                Value = value
                            });
                    }

                    matchRow.TargetValue =
                        row.GetCell(columnCount - 1)?
                            .ToString()?
                            .Trim()
                        ?? string.Empty;

                    if (!string.IsNullOrWhiteSpace(
                            matchRow.TargetValue))
                    {
                        hasData = true;
                    }

                    if (!hasData)
                    {
                        break;
                    }

                    table.Rows.Add(matchRow);

                    rowIndex++;
                }

                tables.Add(table);
            }

            return tables;
        }

        public void Save(
            string filePath,
            IEnumerable<MatchTable> tables)
        {
            XSSFWorkbook workbook =
                XSSFWorkbookUtils.OpenExcelFile(
                    filePath,
                    FileAccess.ReadWrite);

            var tableList =
                tables.ToList();

            RemoveDeletedMatchSheets(
                workbook,
                tableList);

            foreach (var table in tableList)
            {
                string sheetName =
                    $"{table.SheetPrefix}{MatchSuffix}";

                ISheet? sheet =
                    workbook.GetSheet(sheetName);

                if (sheet == null)
                {
                    sheet =
                        workbook.CreateSheet(
                            sheetName);
                }
                else
                {
                    ClearSheet(sheet);
                }

                // Headers

                var headerRow =
                    sheet.CreateRow(0);

                for (int columnIndex = 0;
                     columnIndex < table.SourceProperties.Count;
                     columnIndex++)
                {
                    headerRow.CreateCell(columnIndex)
                        .SetCellValue(
                            table.SourceProperties[columnIndex]);
                }

                headerRow.CreateCell(
                    table.SourceProperties.Count)
                    .SetCellValue(
                        table.TargetProperty);

                // Data

                int rowIndex = 1;

                foreach (var item in table.Rows)
                {
                    var row =
                        sheet.CreateRow(rowIndex++);

                    for (int columnIndex = 0;
                         columnIndex < item.Criteria.Count;
                         columnIndex++)
                    {
                        row.CreateCell(columnIndex)
                           .SetCellValue(
                               item.Criteria[columnIndex].Value);
                    }

                    row.CreateCell(
                        item.Criteria.Count)
                       .SetCellValue(
                           item.TargetValue);
                }

                for (int i = 0;
                     i < table.SourceProperties.Count + 1;
                     i++)
                {
                    sheet.AutoSizeColumn(i);
                }
            }

            XSSFWorkbookUtils.WriteAndClose(
                workbook,
                filePath);
        }

        private static void ClearSheet(
            ISheet sheet)
        {
            for (int i = sheet.LastRowNum;
                 i >= 0;
                 i--)
            {
                var row =
                    sheet.GetRow(i);

                if (row != null)
                {
                    sheet.RemoveRow(row);
                }
            }
        }

        private static void RemoveDeletedMatchSheets(
            IWorkbook workbook,
            IEnumerable<MatchTable> tables)
        {
            var validSheetNames =
                tables
                    .Select(t =>
                        $"{t.SheetPrefix}{MatchSuffix}")
                    .ToHashSet(
                        StringComparer.OrdinalIgnoreCase);

            for (int i = workbook.NumberOfSheets - 1;
                 i >= 0;
                 i--)
            {
                var sheet =
                    workbook.GetSheetAt(i);

                if (!sheet.SheetName.EndsWith(
                        MatchSuffix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (sheet.SheetName.EndsWith(
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
    }
}