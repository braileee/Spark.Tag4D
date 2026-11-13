using Spark.Tag4D.Core.Models;
﻿using Spark.Tag4D.Core.Configuration;
using Spark.Tag4D.Settings.Services;
using Spark.Tag4D.Settings.ViewModels.Support;
using Spark.Tag4D.Core.Mvvm;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Spark.Tag4D.Settings.ViewModels
{
    public class MatchesViewModel : SettingsPageViewModel
    {
        private readonly MatchExcelService _matchExcelService;
        private readonly SettingsRoot _settingsRoot;

        private string _currentFilePath;

        public MatchesViewModel(SettingsRoot settingsRoot)
        {
            _matchExcelService = new MatchExcelService();
            _settingsRoot = settingsRoot;

            AddMatchTableCommand = new RelayCommand(AddMatchTable);
            RemoveMatchTableCommand = new RelayCommand<MatchTableViewModel>(RemoveMatchTable);

            string excelFilePath = ExcelDataLoader.LoadOrPromptExcelFilePath(settingsRoot.ExcelSettings.FileLinkSettings);
            AvailableProperties = ExcelDataLoader.GetPropertyNames(excelFilePath, settingsRoot.ExcelSettings.PropertiesSheet);
            LoadPicklists(excelFilePath);
            Load(excelFilePath);
        }

        public ObservableCollection<MatchTableViewModel> MatchTables { get; } = new ObservableCollection<MatchTableViewModel>();

        public RelayCommand AddMatchTableCommand { get; }

        public RelayCommand<MatchTableViewModel> RemoveMatchTableCommand { get; }

        public override string Title => "Matches";

        public override string SheetInfo => "Sheets: \"<name> Match\"";

        public override string FilterHint => "Find a value in the match tables";

        protected override void OnFilterChanged(string text)
        {
            foreach (MatchTableViewModel table in MatchTables)
            {
                table.FilterText = text;
            }
        }

        public List<string> AvailableProperties { get; } = new List<string>();

        private Dictionary<string, IReadOnlyList<string>> _picklists = new Dictionary<string, IReadOnlyList<string>>();

        /// <summary>The cells of the tables offer the picklist of their property, as saved in the workbook.</summary>
        private void LoadPicklists(string filePath)
        {
            try
            {
                _picklists = ExcelDataLoader.LoadPicklistGroups(filePath, _settingsRoot.ExcelSettings.PicklistSheet)
                    .GroupBy(group => group.GroupName)
                    .ToDictionary(group => group.Key, group => (IReadOnlyList<string>)group.First().Values);
            }
            catch (System.Exception)
            {
                _picklists = new Dictionary<string, IReadOnlyList<string>>(); // suggestions are a convenience, never a reason to fail
            }
        }

        private IReadOnlyList<string> ValuesFor(string propertyName)
        {
            return propertyName != null && _picklists.TryGetValue(propertyName, out IReadOnlyList<string> values) ? values : System.Array.Empty<string>();
        }

        private void Load(string filePath)
        {
            MatchTables.Clear();

            _currentFilePath = filePath;

            var tables = _matchExcelService.Load(filePath, _settingsRoot);

            foreach (var table in tables)
            {
                var tableVm = new MatchTableViewModel(MarkDirty)
                {
                    SheetPrefix = table.SheetPrefix,
                    TargetProperty = table.TargetProperty,
                };

                foreach (var property in table.AvailableProperties)
                {
                    tableVm.AvailableProperties.Add(property);
                }

                foreach (var property in table.SourceProperties)
                {
                    tableVm.SourceProperties.Add(new MatchPropertyViewModel(MarkDirty)
                    {
                        PropertyName = property,
                        AvailableProperties = tableVm.AvailableProperties,
                    });
                }

                foreach (var row in table.Rows)
                {
                    var rowVm = new MatchRowViewModel(MarkDirty)
                    {
                        TargetValue = row.TargetValue
                    };

                    foreach (var criterion in row.Criteria)
                    {
                        rowVm.Criteria.Add(new MatchCriterionViewModel(MarkDirty)
                        {
                            PropertyName = criterion.PropertyName,
                            Value = criterion.Value
                        });
                    }

                    tableVm.Rows.Add(rowVm);
                }

                tableVm.Attach(ValuesFor);
                MatchTables.Add(tableVm);
            }

            IsDirty = false;
        }

        public override bool Save()
        {
            if (string.IsNullOrWhiteSpace(_currentFilePath))
            {
                return true;
            }

            _matchExcelService.Save(_currentFilePath, MatchTables.Select(table => table.ToModel()));

            IsDirty = false;
            return true;
        }

        private void AddMatchTable()
        {
            MatchTableViewModel matchTableViewModel = new MatchTableViewModel(MarkDirty)
            {
                SheetPrefix = $"New Match {MatchTables.Count + 1}",
                AvailableProperties = new ObservableCollection<string>(AvailableProperties),
            };

            matchTableViewModel.TargetProperty = AvailableProperties.FirstOrDefault();
            matchTableViewModel.SourceProperties.Add(new MatchPropertyViewModel(MarkDirty, AvailableProperties.FirstOrDefault())
            {
                AvailableProperties = new ObservableCollection<string>(AvailableProperties),
            });

            matchTableViewModel.Attach(ValuesFor);
            matchTableViewModel.FilterText = FilterText.Trim();
            MatchTables.Add(matchTableViewModel);

            MarkDirty();
        }

        private void RemoveMatchTable(MatchTableViewModel table)
        {
            if (table == null)
            {
                return;
            }

            MatchTables.Remove(table);

            MarkDirty();
        }
    }
}
