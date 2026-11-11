using Spark.Tag4D.Core.Configuration;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Settings.Models;
using Spark.Tag4D.Core.Mvvm;


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Spark.Tag4D.Settings.ViewModels
{
    public class PicklistViewModel : ObservableObject
    {
        public ObservableCollection<PicklistObservable> Picklists { get; }
            = new ObservableCollection<PicklistObservable>();

        public RelayCommand<PicklistObservable> AddItemCommand { get; }
        public RelayCommand<PicklistItemObservable> RemoveItemCommand { get; }
        public SettingsRoot SettingsRoot { get; }
        public RelayCommand AddPicklistCommand { get; }

        public RelayCommand<PicklistObservable> RemovePicklistCommand { get; }

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
            SaveCommand = new RelayCommand(Save, CanSave);
            AddPicklistCommand = new RelayCommand(AddPicklist);
            RemovePicklistCommand = new RelayCommand<PicklistObservable>(OnRemovePicklistCommand);

            Picklists.CollectionChanged += (s, e) =>
            {
                IsDirty = true;
            };
        }

        private bool _isDirty;
        public bool IsDirty
        {
            get => _isDirty;
            set
            {
                if (SetProperty(ref _isDirty, value))
                {
                    SaveCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        private bool CanSave()
        {
            return IsDirty && !Picklists.Any(p => p.HasErrors);
        }

        private void OnRemovePicklistCommand(PicklistObservable observable)
        {
            if (observable == null) return;

            if (Picklists.Count <= 1)
                return;

            Picklists.Remove(observable);

            SaveCommand.RaiseCanExecuteChanged();
        }

        private void AddPicklist()
        {
            var newList = new PicklistObservable
            {
                Name = string.Empty,
                Parent = this
            };

            newList.Items.Add(new PicklistItemObservable { Value = "Value1" });

            Picklists.Add(newList);

            newList.PropertyChanged += OnPicklistChanged;
            newList.Items.CollectionChanged += (s, e) => IsDirty = true;

            SaveCommand.RaiseCanExecuteChanged();
        }

        private void OnPicklistChanged(object sender, PropertyChangedEventArgs e)
        {
            IsDirty = true;
            SaveCommand.RaiseCanExecuteChanged();
        }

        private void Save()
        {
            try
            {
                string errorsInfo = string.Empty;

                if (Picklists.Any(item => item.HasErrors))
                {
                    if (string.IsNullOrEmpty(errorsInfo))
                    {
                        errorsInfo += $"Can't be saved{Environment.NewLine}";
                    }

                    errorsInfo += string.Join(Environment.NewLine, Picklists.Where(item => item.HasErrors).Select(item => item.GetErrorsInfo("Name")));
                    MessageBox.Show(errorsInfo, "Error", MessageBoxButton.OK);
                    return;
                }

                string path = ExcelDataLoader.LoadOrPromptExcelFilePath(SettingsRoot.ExcelSettings.FileLinkSettings);

                List<PicklistGroup> picklists = Picklists.Select(item => new PicklistGroup { GroupName = item.Name, Values = item.Items.Select(item => item.Value).ToList() }).ToList();

                ExcelDataLoader.SavePicklists(
                    path,
                    SettingsRoot.ExcelSettings.PicklistSheet.SheetName, // add this in settings
                    picklists
                );

                MessageBox.Show("Picklists saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                IsDirty = false;

                OnPropertyChanged(nameof(IsDirty));
                SaveCommand.RaiseCanExecuteChanged();
            }
            catch (Exception)
            {
                MessageBox.Show("An error occurred while saving the picklists.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public RelayCommand SaveCommand { get; }
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
            IsDirty = true;
            SaveCommand.RaiseCanExecuteChanged();
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

            SaveCommand.RaiseCanExecuteChanged();
        }
    }

}
