using Spark.Tag4D.Core;
using Spark.Tag4D.Core.Json;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Settings.Models;
using NPOI.HPSF;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Prism.Commands;
using Prism.Mvvm;
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
    public class PropertiesViewModel : BindableBase
    {
        public ObservableCollection<PropertySetItemObservable> PropertySets { get; }
       = new ObservableCollection<PropertySetItemObservable>();

        public DelegateCommand AddPropertySetCommand { get; }
        public DelegateCommand<PropertySetItemObservable> RemovePropertySetCommand { get; }

        public DelegateCommand<PropertySetItemObservable> AddPropertyCommand { get; }
        public DelegateCommand<PropertyItemObservable> RemovePropertyCommand { get; }
        public SettingsRoot SettingsRoot { get; }


        public DelegateCommand SaveCommand { get; }

        private bool _hasUnsavedChanges;
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetProperty(ref _hasUnsavedChanges, value);
        }

        private readonly string _excelFilePath;


        public PropertiesViewModel(SettingsRoot settingsRoot)
        {
            SettingsRoot = settingsRoot;

            AddPropertySetCommand = new DelegateCommand(AddPropertySet);
            RemovePropertySetCommand = new DelegateCommand<PropertySetItemObservable>(RemovePropertySet);

            AddPropertyCommand = new DelegateCommand<PropertySetItemObservable>(AddProperty);
            RemovePropertyCommand = new DelegateCommand<PropertyItemObservable>(RemoveProperty);

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
                        DataType = Constants.DataTypes.FirstOrDefault(item => item == prop.DataType),
                    });
                }
            }


            foreach (var set in PropertySets)
            {
                HookSet(set);
            }

            PropertySets.CollectionChanged += (s, e) => HasUnsavedChanges = true;


            SaveCommand = new DelegateCommand(Save).ObservesCanExecute(() => HasUnsavedChanges);

        }

        private void HookSet(PropertySetItemObservable set)
        {
            set.PropertyChanged += (s, e) => HasUnsavedChanges = true;

            set.Properties.CollectionChanged += (s, e) => HasUnsavedChanges = true;

            foreach (var prop in set.Properties)
            {
                prop.PropertyChanged += (s, e) => HasUnsavedChanges = true;
            }
        }


        private void Save()
        {
            try
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

                HasUnsavedChanges = false;

                MessageBox.Show("Picklists saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception)
            {
                MessageBox.Show("An error occurred while saving the picklists.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        public string[] DataTypes => Constants.DataTypes;

        private void AddPropertySet()
        {

            var set = new PropertySetItemObservable { Name = "NewSet" };
            HookSet(set);
            PropertySets.Add(set);
            HasUnsavedChanges = true;
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

            prop.PropertyChanged += (s, e) => HasUnsavedChanges = true;

            set.Properties.Add(prop);
            HasUnsavedChanges = true;
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
