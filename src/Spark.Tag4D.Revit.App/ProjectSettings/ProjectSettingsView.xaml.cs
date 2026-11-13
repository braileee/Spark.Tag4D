using System.Collections.Specialized;
using System.Windows;

namespace Spark.Tag4D.Revit.App.ProjectSettings
{
    public partial class ProjectSettingsView : Window
    {
        public ProjectSettingsView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is ProjectSettingsViewModel viewModel)
            {
                // Keep the newest log line visible.
                ((INotifyCollectionChanged)viewModel.Log).CollectionChanged += (s, args) => LogScroll.ScrollToEnd();
            }
        }
    }
}
