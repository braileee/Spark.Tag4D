using Spark.Tag4D.Core.Configuration;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Settings.Models;
using Spark.Tag4D.Settings.Services;
using Spark.Tag4D.Settings.ViewModels.Support;
using Spark.Tag4D.Core.Mvvm;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Spark.Tag4D.Settings.ViewModels
{
    public class ExactMatchesViewModel : ObservableObject
    {
        private readonly ExactMatchExcelService _excelService;
        private readonly IDialogService _dialogService;

        private string? _currentFilePath;

        public ExactMatchesViewModel(SettingsRoot settingsRoot)
        {
            _excelService = new ExactMatchExcelService();
            _dialogService = new DialogService();

            SaveCommand =
                new RelayCommand(Save);

            AddTableCommand =
                new RelayCommand(AddTable);

            RemoveTableCommand =
                new RelayCommand<ExactMatchTableViewModel>(
                    RemoveTable);
            SettingsRoot = settingsRoot;

            string excelFilePath = ExcelDataLoader.LoadOrPromptExcelFilePath(SettingsRoot.ExcelSettings.FileLinkSettings);

            Load(excelFilePath);
        }

        public ObservableCollection<ExactMatchTableViewModel>
            Tables
        { get; } = new();


        public RelayCommand SaveCommand { get; }

        public RelayCommand AddTableCommand { get; }

        public RelayCommand<ExactMatchTableViewModel> RemoveTableCommand { get; }

        private bool _isDirty;

        public bool IsDirty
        {
            get => _isDirty;
            set
            {
                if (SetProperty(ref _isDirty, value))
                {
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }

        public string WindowTitle =>
            IsDirty
                ? $"Exact Match Editor - {Path.GetFileName(_currentFilePath)} *"
                : $"Exact Match Editor - {Path.GetFileName(_currentFilePath)}";

        public SettingsRoot SettingsRoot { get; }

        public void MarkDirty()
        {
            IsDirty = true;
        }

        private void Load(string path)
        {
            Tables.Clear();

            _currentFilePath = path;

            foreach (ExactMatchTable table in _excelService.Load(path, SettingsRoot))
            {
                var tableVm =
    new ExactMatchTableViewModel(
        table,
        MarkDirty)
    {
        SourceColumnHeader =
            table.SourceColumnHeader,

        TargetColumnHeader =
            table.TargetColumnHeader,
        AvailableColumnNames = new ObservableCollection<string>(table.AvailableColumnNames)
    };

                foreach (var row in table.Rows)
                {
                    tableVm.Rows.Add(
                        new ExactMatchRowViewModel(
                            MarkDirty)
                        {
                            Source = row.Source,
                            Target = row.Target
                        });
                }

                Tables.Add(tableVm);
            }

            IsDirty = false;
        }

        private void Save()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_currentFilePath))
                    return;

                foreach (var table in Tables)
                {
                    if (table.HasDuplicateSources())
                    {
                        MessageBox.Show(
                            $"Table '{table.Name}' contains duplicate sources.",
                            "Validation Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        return;
                    }
                }

                _excelService.Save(
                    _currentFilePath,
                    Tables.Select(t => t.ToModel()));

                IsDirty = false;

                MessageBox.Show("Saved");
            }
            catch (Exception)
            {
                MessageBox.Show($"Error on save to {_currentFilePath}");
            }
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

            Tables.Add(
                new ExactMatchTableViewModel(
                    table,
                    MarkDirty));

            MarkDirty();
        }

        private void RemoveTable(
            ExactMatchTableViewModel table)
        {
            if (table == null)
                return;

            Tables.Remove(table);

            MarkDirty();
        }

        public bool CanClose()
        {
            if (!IsDirty)
                return true;

            var result =
                _dialogService.ShowSaveChanges();

            switch (result)
            {
                case MessageBoxResult.Yes:
                    Save();
                    return true;

                case MessageBoxResult.No:
                    return true;

                default:
                    return false;
            }
        }
    }
}