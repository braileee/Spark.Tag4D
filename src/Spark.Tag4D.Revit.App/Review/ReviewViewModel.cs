using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Core.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace Spark.Tag4D.Revit.App.Review
{
    /// <summary>One parameter of the workbook that can be checked for missing values.</summary>
    public sealed class ReviewParameter : ObservableObject
    {
        private bool isChecked;

        public ReviewParameter(string name, Action changed)
        {
            Name = name;
            PropertyChanged += (sender, args) => changed();
        }

        public string Name { get; }

        public bool IsChecked
        {
            get => isChecked;
            set => SetProperty(ref isChecked, value);
        }
    }

    /// <summary>Backs the "Tag 4D Review" pane: pick parameters, isolate the elements that have no value in them.</summary>
    public class ReviewViewModel : ObservableObject
    {
        private readonly ReviewEventHandler handler = new ReviewEventHandler();

        private string searchText = string.Empty;
        private string status = "Check the parameters that must have a value.";

        public ReviewViewModel()
        {
            Parameters = new ObservableCollection<ReviewParameter>();
            ProjectSettingsView = CollectionViewSource.GetDefaultView(Parameters);
            ProjectSettingsView.Filter = item => string.IsNullOrWhiteSpace(searchText)
                || ((ReviewParameter)item).Name.IndexOf(searchText.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;

            ShowCommand = new RelayCommand(() => Run(ReviewOperation.Show), () => CheckedCount > 0);
            ResetCommand = new RelayCommand(() => Run(ReviewOperation.Reset));
            CheckAllCommand = new RelayCommand(() => SetAll(true));
            ClearAllCommand = new RelayCommand(() => SetAll(false));
            ReloadCommand = new RelayCommand(Reload);

            TagDataEvents.Changed += Reload;
            Reload();
        }

        public ObservableCollection<ReviewParameter> Parameters { get; }

        public ICollectionView ProjectSettingsView { get; }

        public RelayCommand ShowCommand { get; }

        public RelayCommand ResetCommand { get; }

        public RelayCommand CheckAllCommand { get; }

        public RelayCommand ClearAllCommand { get; }

        public RelayCommand ReloadCommand { get; }

        public int CheckedCount => Parameters.Count(item => item.IsChecked);

        public string SearchText
        {
            get => searchText;
            set
            {
                if (SetProperty(ref searchText, value))
                {
                    ProjectSettingsView.Refresh();
                }
            }
        }

        public string Status
        {
            get => status;
            private set => SetProperty(ref status, value);
        }

        /// <summary>Re-reads the parameters from the workbook, keeping what was checked.</summary>
        public void Reload()
        {
            var previouslyChecked = new HashSet<string>(Parameters.Where(item => item.IsChecked).Select(item => item.Name));

            try
            {
                TagData data = TagData.Load();

                Parameters.Clear();
                foreach (PropertyItem property in data.Properties)
                {
                    Parameters.Add(new ReviewParameter(property.PropertyName, OnCheckedChanged)
                    {
                        IsChecked = previouslyChecked.Contains(property.PropertyName)
                    });
                }
            }
            catch (TagDataException exception)
            {
                Status = exception.Message;
            }

            OnCheckedChanged();
        }

        private void SetAll(bool value)
        {
            // only what the search shows
            foreach (ReviewParameter item in ProjectSettingsView.Cast<ReviewParameter>().ToList())
            {
                item.IsChecked = value;
            }
        }

        private void OnCheckedChanged()
        {
            OnPropertyChanged(nameof(CheckedCount));
            ShowCommand.RaiseCanExecuteChanged();
        }

        private void Run(ReviewOperation operation)
        {
            Status = operation == ReviewOperation.Show ? "Searching..." : "Resetting...";

            handler.Raise(new ReviewOptions
            {
                Operation = operation,
                PropertyNames = Parameters.Where(item => item.IsChecked).Select(item => item.Name).ToList(),
                OnResult = message => Status = message,
            });
        }
    }
}
