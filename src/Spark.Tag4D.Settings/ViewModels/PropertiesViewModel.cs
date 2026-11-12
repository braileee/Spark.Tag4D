using Spark.Tag4D.Core;
using Spark.Tag4D.Core.Configuration;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Settings.Models;
using Spark.Tag4D.Core.Mvvm;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Spark.Tag4D.Settings.ViewModels
{
    public class PropertiesViewModel : SettingsPageViewModel
    {
        public ObservableCollection<PropertySetItemObservable> PropertySets { get; }
       = new ObservableCollection<PropertySetItemObservable>();

        public RelayCommand AddPropertySetCommand { get; }
        public RelayCommand<PropertySetItemObservable> RemovePropertySetCommand { get; }

        public RelayCommand<PropertySetItemObservable> AddPropertyCommand { get; }
        public RelayCommand<PropertyItemObservable> RemovePropertyCommand { get; }
        public SettingsRoot SettingsRoot { get; }

        public override string Title => "Properties";

        public override string SheetInfo => $"Sheet: {SettingsRoot.ExcelSettings.PropertiesSheet.SheetName}";

        public PropertiesViewModel(SettingsRoot settingsRoot)
        {
            SettingsRoot = settingsRoot;

            AddPropertySetCommand = new RelayCommand(AddPropertySet);
            RemovePropertySetCommand = new RelayCommand<PropertySetItemObservable>(RemovePropertySet);

            AddPropertyCommand = new RelayCommand<PropertySetItemObservable>(AddProperty);
            RemovePropertyCommand = new RelayCommand<PropertyItemObservable>(RemoveProperty);

            string excelFilePath = ExcelDataLoader.LoadOrPromptExcelFilePath(settingsRoot.ExcelSettings.FileLinkSettings);
            List<PropertySetItem> propertySetItems = ExcelDataLoader.LoadPropertySetItems(excelFilePath, settingsRoot.ExcelSettings.PropertiesSheet);

            foreach (var item in propertySetItems)
            {
                var set = PropertySets.FirstOrDefault(s => s.Name == item.PropertySetName);
                if (set == null)
                {
                    set = new PropertySetItemObservable { Name = item.PropertySetName };
                    PropertySets.Add(set);
                }
                foreach (var prop in item.PropertyItems)
                {
                    set.Properties.Add(new PropertyItemObservable
                    {
                        PropertyName = prop.PropertyName,
                        DataType = Constants.DataTypes.FirstOrDefault(dataType => dataType == prop.DataType),
                    });
                }
            }

            foreach (var set in PropertySets)
            {
                HookSet(set);
            }

            PropertySets.CollectionChanged += (s, e) => MarkDirty();
            IsDirty = false;
        }

        private void HookSet(PropertySetItemObservable set)
        {
            set.PropertyChanged += (s, e) => MarkDirty();
            set.Properties.CollectionChanged += (s, e) => MarkDirty();

            foreach (var prop in set.Properties)
            {
                prop.PropertyChanged += (s, e) => MarkDirty();
            }
        }

        public override bool Save()
        {
            string excelFilePath = ExcelDataLoader.LoadOrPromptExcelFilePath(SettingsRoot.ExcelSettings.FileLinkSettings);

            ExcelDataLoader.SavePropertySetItems(
                excelFilePath,
                SettingsRoot.ExcelSettings.PropertiesSheet,
                PropertySets.Select(s => new PropertySetItem
                {
                    PropertySetName = s.Name,
                    PropertyItems = s.Properties.Select(p => new PropertyItem
                    {
                        PropertyName = p.PropertyName,
                        DataType = p.DataType
                    }).ToList()
                }).ToList()
            );

            IsDirty = false;
            return true;
        }

        public string[] DataTypes => Constants.DataTypes;

        private void AddPropertySet()
        {
            var set = new PropertySetItemObservable { Name = "NewSet" };
            HookSet(set);
            PropertySets.Add(set);
        }

        private void RemovePropertySet(PropertySetItemObservable set)
        {
            PropertySets.Remove(set);
        }

        private void AddProperty(PropertySetItemObservable set)
        {
            if (set == null) return;

            var prop = new PropertyItemObservable
            {
                PropertyName = "NewProperty",
                DataType = Constants.DataTypes.FirstOrDefault()
            };

            prop.PropertyChanged += (s, e) => MarkDirty();
            set.Properties.Add(prop);
        }

        private void RemoveProperty(PropertyItemObservable item)
        {
            foreach (var set in PropertySets)
            {
                if (set.Properties.Contains(item))
                {
                    set.Properties.Remove(item);
                    break;
                }
            }
        }
    }
}
