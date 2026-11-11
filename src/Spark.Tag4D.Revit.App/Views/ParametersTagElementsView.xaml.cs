using Autodesk.Revit.UI;
using System;
using System.Windows.Controls;

namespace Spark.Tag4D.Revit.App.Views
{
    /// <summary>
    /// Interaction logic for ParametersTagElementsView.xaml
    /// </summary>
    public partial class ParametersTagElementsView : Page, IDisposable, IDockablePaneProvider
    {
        public ParametersTagElementsView()
        {
            InitializeComponent();
        }

        public void Dispose()
        {
            this.Dispose();
        }

        public void SetupDockablePane(DockablePaneProviderData data)
        {
            // wpf object with pane's interface
            data.FrameworkElement = this;
            // initial state position
            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Tabbed,
                TabBehind = DockablePanes.BuiltInDockablePanes.ProjectBrowser
            };
        }
    }
}
