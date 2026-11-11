using Spark.Tag4D.Core.Json;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Settings.Models;
using Spark.Tag4D.Settings.Services;
using Spark.Tag4D.Settings.ViewModels.Support;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Xml.Linq;

namespace Spark.Tag4D.Settings.ViewModels
{
    public class MatchesViewModel : BindableBase
    {
        private readonly MatchExcelService _matchExcelService;
        private readonly IDialogService _dialogService;
        private readonly SettingsRoot _settingsRoot;

        private string? _currentFilePath;
        private bool _isDirty;

        public MatchesViewModel(
            SettingsRoot settingsRoot)
        {
            _matchExcelService = new MatchExcelService();
            _dialogService = new DialogService();
            _settingsRoot = settingsRoot;

            OpenCommand =
                new DelegateCommand(Open);

            SaveCommand =
                new DelegateCommand(Save);

            AddMatchTableCommand =
                new DelegateCommand(AddMatchTable);

            RemoveMatchTableCommand =
                new DelegateCommand<MatchTableViewModel>(
                    RemoveMatchTable);

            string excelFilePath = ExcelDataLoader.LoadOrPromptExcelFilePath(settingsRoot.ExcelSettings.FileLinkSettings);
            AvailableProperties = ExcelDataLoader.GetPropertyNames(excelFilePath, settingsRoot.ExcelSettings.PropertiesSheet);
            Load(excelFilePath);
        }

        public ObservableCollection<MatchTableViewModel>
            MatchTables
        { get; }
            = new();

        public DelegateCommand OpenCommand { get; }

        public DelegateCommand SaveCommand { get; }

        public DelegateCommand AddMatchTableCommand { get; }

        public DelegateCommand<MatchTableViewModel>
            RemoveMatchTableCommand
        { get; }

        public bool IsDirty
        {
            get => _isDirty;
            set => SetProperty(ref _isDirty, value);
        }
        public List<string> AvailableProperties { get; } = new List<string>();

        public void MarkDirty()
        {
            IsDirty = true;
        }

        private void Open()
        {
            if (IsDirty)
            {
                var result =
                    _dialogService.ShowSaveChanges();

                if (result == MessageBoxResult.Yes)
                {
                    Save();
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            var dialog = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            Load(dialog.FileName);
        }

        private void Load(string filePath)
        {
            MatchTables.Clear();

            _currentFilePath = filePath;

            var tables =
                _matchExcelService.Load(
                    filePath,
                    _settingsRoot);

            foreach (var table in tables)
            {
                var tableVm =
                    new MatchTableViewModel(MarkDirty)
                    {
                        SheetPrefix =
                            table.SheetPrefix,

                        TargetProperty =
                            table.TargetProperty,
                    };

                // Available properties

                foreach (var property in table.AvailableProperties)
                {
                    tableVm.AvailableProperties.Add(
                        property);
                }

                // Source property headers

                foreach (var property in table.SourceProperties)
                {
                    tableVm.SourceProperties.Add(
                        new MatchPropertyViewModel(
                            MarkDirty)
                        {
                            PropertyName = property,
                            AvailableProperties = tableVm.AvailableProperties,
                        });
                }

                // Rows

                foreach (var row in table.Rows)
                {
                    var rowVm =
                        new MatchRowViewModel(
                            MarkDirty)
                        {
                            TargetValue =
                                row.TargetValue
                        };

                    foreach (var criterion in row.Criteria)
                    {
                        rowVm.Criteria.Add(
                            new MatchCriterionViewModel(
                                MarkDirty)
                            {
                                PropertyName =
                                    criterion.PropertyName,

                                Value =
                                    criterion.Value
                            });
                    }

                    tableVm.Rows.Add(rowVm);
                }

                MatchTables.Add(tableVm);
            }

            IsDirty = false;
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(_currentFilePath))
            {
                return;
            }

            _matchExcelService.Save(
                _currentFilePath,
                MatchTables.Select(
                    table => table.ToModel()));

            IsDirty = false;

            _dialogService.ShowInformation(
                "Match tables saved successfully.");
        }

        private void AddMatchTable()
        {
            MatchTableViewModel matchTableViewModel = new(
                    MarkDirty)
            {
                SheetPrefix = $"New Match {MatchTables.Count + 1}",
                AvailableProperties = new ObservableCollection<string>(AvailableProperties),
            };

            matchTableViewModel.TargetProperty = AvailableProperties.FirstOrDefault();
            matchTableViewModel.SourceProperties.Add(new MatchPropertyViewModel(MarkDirty, AvailableProperties.FirstOrDefault())
            {
                AvailableProperties = new ObservableCollection<string>(AvailableProperties),
            });

            MatchTables.Add(matchTableViewModel);

            MarkDirty();
        }

        private void RemoveMatchTable(
            MatchTableViewModel? table)
        {
            if (table == null)
            {
                return;
            }

            MatchTables.Remove(table);

            MarkDirty();
        }

        public bool CanClose()
        {
            if (!IsDirty)
            {
                return true;
            }

            var result =
                _dialogService.ShowSaveChanges();

            switch (result)
            {
                case MessageBoxResult.Yes:

                    Save();
                    return true;

                case MessageBoxResult.No:

                    return true;

                case MessageBoxResult.Cancel:

                    return false;

                default:

                    return false;
            }
        }
    }
}