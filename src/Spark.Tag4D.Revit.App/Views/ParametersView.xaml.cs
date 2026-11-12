using Spark.Tag4D.Revit.App.ViewModels;
using System.Collections.Specialized;
using System.Windows;

namespace Spark.Tag4D.Revit.App.Views
{
    public partial class ParametersView : Window
    {
        public ParametersView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is ParametersViewModel viewModel)
            {
                // Keep the newest log line visible.
                ((INotifyCollectionChanged)viewModel.Log).CollectionChanged += (s, args) => LogScroll.ScrollToEnd();
            }
        }
    }
}
