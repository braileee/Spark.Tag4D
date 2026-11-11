using Spark.Tag4D.Core.Json;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Settings.ViewModels.Support;
using Prism.Commands;
using Prism.Mvvm;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;

namespace Spark.Tag4D.Settings.ViewModels
{
    public class ExpressionEditorViewModel : BindableBase
    {

        public ObservableCollection<ExpressionRuleViewModel> Rules { get; }
            = new();

        public ObservableCollection<string> AvailableProperties { get; }
            = new();

        private ExpressionRuleViewModel? _selectedRule;

        public ExpressionRuleViewModel? SelectedRule
        {
            get => _selectedRule;
            set => SetProperty(ref _selectedRule, value);
        }

        public DelegateCommand AddRowCommand { get; }

        public DelegateCommand DeleteRowCommand { get; }

        public DelegateCommand SaveCommand { get; }
        public string ExcelFilePath { get; }
        public SettingsRoot SettingsRoot { get; set; }

        public ExpressionEditorViewModel(SettingsRoot settingsRoot)
        {
            SettingsRoot = settingsRoot;

            ExcelFilePath = ExcelDataLoader.LoadOrPromptExcelFilePath(settingsRoot.ExcelSettings.FileLinkSettings);

            List<string> availableProperties = ExcelDataLoader.GetPropertyNames(ExcelFilePath, settingsRoot.ExcelSettings.PropertiesSheet);

            foreach (var property in availableProperties)
            {
                AvailableProperties.Add(property);
            }

            AddRowCommand = new DelegateCommand(AddRow);
            DeleteRowCommand = new DelegateCommand(DeleteRow);
            SaveCommand = new DelegateCommand(Save);

            LoadRules();
        }

        private void LoadRules()
        {
            Rules.Clear();

            foreach (var rule in ExcelDataLoader.LoadExpressions(ExcelFilePath, SettingsRoot.ExcelSettings.ExpressionSheet))
            {
                var vm = new ExpressionRuleViewModel(rule);

                Rules.Add(vm);
            }
        }

        private void AddRow()
        {
            var vm = new ExpressionRuleViewModel();

            Rules.Add(vm);
        }

        private void DeleteRow()
        {
            if (SelectedRule != null)
            {
                Rules.Remove(SelectedRule);
            }
        }

        private void Save()
        {
            ExcelDataLoader.SaveRules(ExcelFilePath, SettingsRoot.ExcelSettings.ExpressionSheet, Rules.Select(x => x.ToModel()));
        }
    }
}