using Spark.Tag4D.Core.Configuration;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Settings.ViewModels.Support;
using Spark.Tag4D.Core.Mvvm;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace Spark.Tag4D.Settings.ViewModels
{
    public class ExpressionEditorViewModel : SettingsPageViewModel
    {
        public ObservableCollection<ExpressionRuleViewModel> Rules { get; }
            = new ObservableCollection<ExpressionRuleViewModel>();

        public ObservableCollection<string> AvailableProperties { get; }
            = new ObservableCollection<string>();

        public RelayCommand AddRowCommand { get; }

        public RelayCommand<ExpressionRuleViewModel> RemoveRowCommand { get; }

        public string ExcelFilePath { get; }
        public SettingsRoot SettingsRoot { get; set; }

        public override string Title => "Expressions";

        public override string SheetInfo => $"Sheet: {SettingsRoot.ExcelSettings.ExpressionSheet.SheetName}";

        /// <summary>The rules shown, narrowed down by the search text.</summary>
        public ICollectionView RulesView { get; }

        public override string FilterHint => "Find a rule by property or function";

        protected override void OnFilterChanged(string text) => RulesView.Refresh();

        // A rule that is still empty (just added) is never hidden, so it can be filled in.
        private static bool Matches(ExpressionRuleViewModel rule, string text)
        {
            bool empty = string.IsNullOrEmpty(rule.SourcePropertyName) && string.IsNullOrEmpty(rule.Function) && string.IsNullOrEmpty(rule.TargetPropertyName);
            return empty || Contains(rule.SourcePropertyName, text) || Contains(rule.Function, text) || Contains(rule.TargetPropertyName, text);
        }

        public ExpressionEditorViewModel(SettingsRoot settingsRoot)
        {
            SettingsRoot = settingsRoot;
            RulesView = CollectionViewSource.GetDefaultView(Rules);
            RulesView.Filter = item => Matches((ExpressionRuleViewModel)item, FilterText.Trim());

            ExcelFilePath = ExcelDataLoader.LoadOrPromptExcelFilePath(settingsRoot.ExcelSettings.FileLinkSettings);

            List<string> availableProperties = ExcelDataLoader.GetPropertyNames(ExcelFilePath, settingsRoot.ExcelSettings.PropertiesSheet);

            foreach (var property in availableProperties)
            {
                AvailableProperties.Add(property);
            }

            AddRowCommand = new RelayCommand(AddRow);
            RemoveRowCommand = new RelayCommand<ExpressionRuleViewModel>(RemoveRow);

            LoadRules();

            Rules.CollectionChanged += (s, e) => MarkDirty();
            IsDirty = false;
        }

        private void LoadRules()
        {
            Rules.Clear();

            foreach (var rule in ExcelDataLoader.LoadExpressions(ExcelFilePath, SettingsRoot.ExcelSettings.ExpressionSheet))
            {
                Rules.Add(CreateRule(new ExpressionRuleViewModel(rule)));
            }
        }

        private ExpressionRuleViewModel CreateRule(ExpressionRuleViewModel rule)
        {
            rule.PropertyChanged += (s, e) => MarkDirty();
            return rule;
        }

        private void AddRow()
        {
            Rules.Add(CreateRule(new ExpressionRuleViewModel()));
        }

        private void RemoveRow(ExpressionRuleViewModel rule)
        {
            if (rule != null)
            {
                Rules.Remove(rule);
            }
        }

        public override bool Save()
        {
            ExcelDataLoader.SaveRules(ExcelFilePath, SettingsRoot.ExcelSettings.ExpressionSheet, Rules.Select(x => x.ToModel()));
            IsDirty = false;
            return true;
        }
    }
}
