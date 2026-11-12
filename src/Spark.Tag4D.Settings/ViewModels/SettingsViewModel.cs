using Spark.Tag4D.Core.Configuration;
using Spark.Tag4D.Core.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Spark.Tag4D.Settings.ViewModels
{
    public class SettingsViewModel : ObservableObject
    {
        private string _statusText = string.Empty;

        public SettingsViewModel()
        {
            Settings = SettingsLoader.LoadExistingOrDefault();
            PathsViewModel = new PathsViewModel();
            PropertiesViewModel = new PropertiesViewModel(Settings);
            PicklistViewModel = new PicklistViewModel(Settings);
            ExactMatchesViewModel = new ExactMatchesViewModel(Settings);
            MatchesViewModel = new MatchesViewModel(Settings);
            ExpressionEditorViewModel = new ExpressionEditorViewModel(Settings);
            ComposedViewModel = new ComposedViewModel(Settings);

            Pages = new List<SettingsPageViewModel>
            {
                PropertiesViewModel,
                PicklistViewModel,
                ExactMatchesViewModel,
                MatchesViewModel,
                ExpressionEditorViewModel,
                ComposedViewModel,
            };

            foreach (SettingsPageViewModel page in Pages)
            {
                page.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(SettingsPageViewModel.IsDirty))
                    {
                        OnDirtyChanged();
                    }
                };
            }

            SaveCommand = new RelayCommand(() => SaveAll(), () => IsDirty);
            CancelCommand = new RelayCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));
        }

        public SettingsRoot Settings { get; }
        public PathsViewModel PathsViewModel { get; set; }
        public PropertiesViewModel PropertiesViewModel { get; set; }
        public PicklistViewModel PicklistViewModel { get; set; }
        public ExactMatchesViewModel ExactMatchesViewModel { get; set; }
        public MatchesViewModel MatchesViewModel { get; }
        public ExpressionEditorViewModel ExpressionEditorViewModel { get; }
        public ComposedViewModel ComposedViewModel { get; }

        /// <summary>The editable pages, in tab order.</summary>
        public IReadOnlyList<SettingsPageViewModel> Pages { get; }

        public RelayCommand SaveCommand { get; }

        public RelayCommand CancelCommand { get; }

        /// <summary>Raised when the Cancel button is pressed; the window closes (and asks about unsaved changes).</summary>
        public event EventHandler CloseRequested;

        public bool IsDirty => Pages.Any(page => page.IsDirty);

        /// <summary>Titles of the tabs with unsaved changes, for the status line.</summary>
        public string DirtySummary => string.Join(", ", Pages.Where(page => page.IsDirty).Select(page => page.Title));

        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        /// <summary>Saves every tab that has changes. Returns false when something could not be saved.</summary>
        public bool SaveAll()
        {
            foreach (SettingsPageViewModel page in Pages.Where(p => p.IsDirty).ToList())
            {
                try
                {
                    if (!page.Save())
                    {
                        StatusText = $"'{page.Title}' was not saved.";
                        return false;
                    }
                }
                catch (Exception exception)
                {
                    MessageBox.Show(
                        $"The '{page.Title}' tab could not be saved:{Environment.NewLine}{exception.Message}",
                        "Spark.Tag4D Settings",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    StatusText = $"'{page.Title}' could not be saved.";
                    return false;
                }
            }

            StatusText = $"All changes saved at {DateTime.Now:HH:mm:ss}.";
            return true;
        }

        private void OnDirtyChanged()
        {
            OnPropertyChanged(nameof(IsDirty));
            OnPropertyChanged(nameof(DirtySummary));
            SaveCommand.RaiseCanExecuteChanged();

            if (IsDirty)
            {
                StatusText = string.Empty;
            }
        }
    }
}
