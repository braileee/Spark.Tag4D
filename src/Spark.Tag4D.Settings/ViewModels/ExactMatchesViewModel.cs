using Spark.Tag4D.Core.Models;
﻿using Spark.Tag4D.Core.Configuration;
using Spark.Tag4D.Settings.Models;
using Spark.Tag4D.Settings.Services;
using Spark.Tag4D.Settings.ViewModels.Support;
using Spark.Tag4D.Core.Mvvm;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace Spark.Tag4D.Settings.ViewModels
{
    public class ExactMatchesViewModel : SettingsPageViewModel
    {
        private readonly ExactMatchExcelService _excelService;

        private string _currentFilePath;

        public ExactMatchesViewModel(SettingsRoot settingsRoot)
        {
            _excelService = new ExactMatchExcelService();

            AddTableCommand = new RelayCommand(AddTable);
            RemoveTableCommand = new RelayCommand<ExactMatchTableViewModel>(RemoveTable);
            SettingsRoot = settingsRoot;

            string excelFilePath = ExcelDataLoader.LoadOrPromptExcelFilePath(SettingsRoot.ExcelSettings.FileLinkSettings);

            Load(excelFilePath);
        }

        public ObservableCollection<ExactMatchTableViewModel> Tables { get; } = new ObservableCollection<ExactMatchTableViewModel>();

        public RelayCommand AddTableCommand { get; }

        public RelayCommand<ExactMatchTableViewModel> RemoveTableCommand { get; }

        public override string Title => "Exact Matches";

        public override string SheetInfo => "Sheets: \"<name> Exact Match\"";

        public SettingsRoot SettingsRoot { get; }

        private void Load(string path)
        {
            Tables.Clear();

            _currentFilePath = path;

            foreach (ExactMatchTable table in _excelService.Load(path, SettingsRoot))
            {
                var tableVm = new ExactMatchTableViewModel(table, MarkDirty)
                {
                    SourceColumnHeader = table.SourceColumnHeader,
                    TargetColumnHeader = table.TargetColumnHeader,
                    AvailableColumnNames = new ObservableCollection<string>(table.AvailableColumnNames)
                };

                foreach (var row in table.Rows)
                {
                    tableVm.Rows.Add(new ExactMatchRowViewModel(MarkDirty)
                    {
                        Source = row.Source,
                        Target = row.Target
                    });
                }

                Tables.Add(tableVm);
            }

            IsDirty = false;
        }

        public override bool Save()
        {
            if (string.IsNullOrWhiteSpace(_currentFilePath))
            {
                return true;
            }

            foreach (var table in Tables)
            {
                if (table.HasDuplicateSources())
                {
                    MessageBox.Show(
                        $"Table '{table.WorksheetName}' contains duplicate sources.",
                        "Exact Matches",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return false;
                }
            }

            _excelService.Save(_currentFilePath, Tables.Select(t => t.ToModel()));

            IsDirty = false;
            return true;
        }

        private void AddTable()
        {
            string excelFilePath = ExcelDataLoader.LoadOrPromptExcelFilePath(SettingsRoot.ExcelSettings.FileLinkSettings);

            List<string> columnNames = ExcelDataLoader.GetPropertyNames(excelFilePath, SettingsRoot.ExcelSettings.PropertiesSheet);

            ExactMatchTable table = new ExactMatchTable
            {
                SheetPrefix = $"New Table {Tables.Count + 1}",
                AvailableColumnNames = columnNames
            };

            Tables.Add(new ExactMatchTableViewModel(table, MarkDirty));

            MarkDirty();
        }

        private void RemoveTable(ExactMatchTableViewModel table)
        {
            if (table == null)
                return;

            Tables.Remove(table);

            MarkDirty();
        }
    }
}
