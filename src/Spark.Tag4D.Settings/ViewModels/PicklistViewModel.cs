using Spark.Tag4D.Core.Configuration;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Settings.Models;
using Spark.Tag4D.Core.Mvvm;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;

namespace Spark.Tag4D.Settings.ViewModels
{
    public class PicklistViewModel : SettingsPageViewModel
    {
        public ObservableCollection<PicklistObservable> Picklists { get; }
            = new ObservableCollection<PicklistObservable>();

        public RelayCommand<PicklistObservable> AddItemCommand { get; }
        public RelayCommand<PicklistItemObservable> RemoveItemCommand { get; }
        public SettingsRoot SettingsRoot { get; }
        public RelayCommand AddPicklistCommand { get; }

        public RelayCommand<PicklistObservable> RemovePicklistCommand { get; }

        public override string Title => "Picklists";

        public override string SheetInfo => $"Sheet: {SettingsRoot.ExcelSettings.PicklistSheet.SheetName}";

        private List<string> availablePicklistGroupNames;
        public List<string> AvailablePicklistGroupNames
        {
            get { return availablePicklistGroupNames; }
            set
            {
                availablePicklistGroupNames = value;
                OnPropertyChanged();
            }
        }

        public PicklistViewModel(SettingsRoot settingsRoot)
        {
            SettingsRoot = settingsRoot;
            LoadFromExcel();

            AddItemCommand = new RelayCommand<PicklistObservable>(AddItem);
            RemoveItemCommand = new RelayCommand<PicklistItemObservable>(RemoveItem);
            AddPicklistCommand = new RelayCommand(AddPicklist);
            RemovePicklistCommand = new RelayCommand<PicklistObservable>(OnRemovePicklistCommand);

            foreach (PicklistObservable picklist in Picklists)
            {
                Hook(picklist);
            }

            Picklists.CollectionChanged += (s, e) => MarkDirty();
            IsDirty = false;
        }

        /// <summary>Every edit of a picklist - its name, its values, adding or removing values - makes the page dirty.</summary>
        private void Hook(PicklistObservable picklist)
        {
            picklist.PropertyChanged += (s, e) => MarkDirty();

            foreach (PicklistItemObservable item in picklist.Items)
            {
                item.PropertyChanged += (s, e) => MarkDirty();
            }

            picklist.Items.CollectionChanged += (s, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (PicklistItemObservable item in e.NewItems)
                    {
                        item.PropertyChanged += (sender, args) => MarkDirty();
                    }
                }

                MarkDirty();
            };
        }

        private void OnRemovePicklistCommand(PicklistObservable observable)
        {
            if (observable == null) return;

            if (Picklists.Count <= 1)
                return;

            Picklists.Remove(observable);
        }

        private void AddPicklist()
        {
            var newList = new PicklistObservable
            {
                Name = string.Empty,
                Parent = this
            };

            newList.Items.Add(new PicklistItemObservable { Value = "Value1" });
            Hook(newList);
            Picklists.Add(newList);
        }

        public override bool Save()
        {
            if (Picklists.Any(item => item.HasErrors))
            {
                string errorsInfo = $"The picklists can't be saved:{Environment.NewLine}"
                    + string.Join(Environment.NewLine, Picklists.Where(item => item.HasErrors).Select(item => item.GetErrorsInfo("Name")));
                MessageBox.Show(errorsInfo, "Picklists", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            string path = ExcelDataLoader.LoadOrPromptExcelFilePath(SettingsRoot.ExcelSettings.FileLinkSettings);

            List<PicklistGroup> picklists = Picklists
                .Select(item => new PicklistGroup { GroupName = item.Name, Values = item.Items.Select(value => value.Value).ToList() })
                .ToList();

            ExcelDataLoader.SavePicklists(path, SettingsRoot.ExcelSettings.PicklistSheet.SheetName, picklists);

            IsDirty = false;
            return true;
        }

        public List<PropertySetItem> PropertySetItems { get; private set; } = new List<PropertySetItem>();

        private void LoadFromExcel()
        {
            string path = ExcelDataLoader.LoadOrPromptExcelFilePath(SettingsRoot.ExcelSettings.FileLinkSettings);
            PropertySetItems = ExcelDataLoader.LoadPropertySetItems(path, SettingsRoot.ExcelSettings.PropertiesSheet);
            AvailablePicklistGroupNames = PropertySetItems.SelectMany(item => item.PropertyItems).Select(item => item.PropertyName).Distinct().ToList();

            var table = ExcelDataLoader.LoadPicklistGroups(path, SettingsRoot.ExcelSettings.PicklistSheet);

            foreach (var col in table)
            {
                var picklist = new PicklistObservable
                {
                    Name = AvailablePicklistGroupNames.FirstOrDefault(item => item == col.GroupName),
                    Parent = this
                };

                if (string.IsNullOrEmpty(picklist?.Name))
                {
                    continue;
                }

                foreach (var val in col.Values.Where(v => !string.IsNullOrWhiteSpace(v)))
                {
                    picklist.Items.Add(new PicklistItemObservable { Value = val });
                }

                Picklists.Add(picklist);
            }
        }

        private void AddItem(PicklistObservable list)
        {
            list?.Items.Add(new PicklistItemObservable { Value = "NewValue" });
        }

        private void RemoveItem(PicklistItemObservable item)
        {
            foreach (var list in Picklists)
            {
                if (list.Items.Contains(item))
                {
                    list.Items.Remove(item);
                    break;
                }
            }
        }
    }
}
