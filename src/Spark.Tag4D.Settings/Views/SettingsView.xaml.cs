using Spark.Tag4D.Settings.ViewModels;
using System;
using System.Windows;

namespace Spark.Tag4D.Settings.Views
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : Window
    {
        public SettingsView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is SettingsViewModel oldViewModel)
            {
                oldViewModel.CloseRequested -= OnCloseRequested;
            }

            if (e.NewValue is SettingsViewModel newViewModel)
            {
                newViewModel.CloseRequested += OnCloseRequested;
            }
        }

        // Cancel closes the window through the normal path, so the unsaved-changes question is asked in one place.
        private void OnCloseRequested(object sender, EventArgs e) => Close();

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SettingsViewModel viewModel = DataContext as SettingsViewModel;

            if (viewModel == null || !viewModel.IsDirty)
            {
                return;
            }

            MessageBoxResult answer = MessageBox.Show(
                this,
                $"There are unsaved changes in: {viewModel.DirtySummary}.{Environment.NewLine}{Environment.NewLine}Save them before closing?",
                "Spark.Tag4D Settings",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

            if (answer == MessageBoxResult.Cancel || (answer == MessageBoxResult.Yes && !viewModel.SaveAll()))
            {
                e.Cancel = true;
            }
        }
    }
}
