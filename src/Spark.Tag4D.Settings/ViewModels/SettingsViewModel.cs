using Spark.Tag4D.Core.Configuration;
using Spark.Tag4D.Core.Mvvm;
using Spark.Tag4D.Settings.Services;
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

            ModelSyncChange change = PropertiesViewModel.TakePendingChange();
            if (change != null && !change.IsEmpty && ModelSynchronizer != null)
            {
                SynchronizeModel(change);
            }

            return true;
        }

        /// <summary>
        /// Set by the host add-in when the properties of the workbook are mirrored in an open model. After a save that
        /// added or removed properties the model is updated, the user is told, and can roll everything back.
        /// </summary>
        public IModelSynchronizer ModelSynchronizer { get; set; }

        private void SynchronizeModel(ModelSyncChange change)
        {
            StatusText = $"Updating the {ModelSynchronizer.ModelName}...";

            ModelSynchronizer.Apply(change, success =>
            {
                if (!success)
                {
                    StatusText = $"The {ModelSynchronizer.ModelName} was not fully updated.";
                    MessageBox.Show(
                        $"The properties were saved, but the {ModelSynchronizer.ModelName} could not be fully updated. See the activity log of the Parameters window.",
                        "Spark.Tag4D Settings",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                StatusText = $"The {ModelSynchronizer.ModelName} was updated: {change.AddedCount} added, {change.RemovedCount} removed.";

                MessageBoxResult answer = MessageBox.Show(
                    Describe(change)
                    + $"{Environment.NewLine}Do you want to roll these changes back?{Environment.NewLine}"
                    + "Yes - restore the previous Properties sheet and the model.  No - keep the changes.",
                    "Spark.Tag4D - model updated",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information,
                    MessageBoxResult.No);

                if (answer == MessageBoxResult.Yes)
                {
                    RollBack(change);
                }
            });
        }

        private void RollBack(ModelSyncChange change)
        {
            ModelSynchronizer.Rollback(change, success =>
            {
                if (!success)
                {
                    StatusText = "The rollback was not completed.";
                    MessageBox.Show(
                        $"The {ModelSynchronizer.ModelName} could not be fully restored, so the Properties sheet was left as saved. See the activity log.",
                        "Spark.Tag4D Settings",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                PropertiesViewModel.RevertLastSave();
                StatusText = "The changes were rolled back.";
                OnDirtyChanged();
            });
        }

        private string Describe(ModelSyncChange change)
        {
            var text = new System.Text.StringBuilder();
            text.AppendLine($"The {ModelSynchronizer.ModelName} was updated to match the saved properties.");
            text.AppendLine();
            AppendNames(text, "Added", change.Added);
            AppendNames(text, "Removed", change.Removed);

            if (change.RemovedCount > 0)
            {
                text.AppendLine("Values stored in the removed parameters are deleted; a rollback re-creates them empty.");
            }

            return text.ToString();
        }

        private static void AppendNames(System.Text.StringBuilder text, string title, List<Spark.Tag4D.Core.Models.PropertySetItem> sets)
        {
            var names = sets.SelectMany(set => set.PropertyItems).Select(item => item.PropertyName).ToList();
            if (names.Count == 0)
            {
                return;
            }

            text.Append($"{title} ({names.Count}): {string.Join(", ", names.Take(8))}");
            text.AppendLine(names.Count > 8 ? ", ..." : string.Empty);
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
