using Autodesk.Revit.UI;
using System.Windows.Controls;

namespace Spark.Tag4D.Revit.App.Review
{
    /// <summary>Dockable pane: choose the parameters that must have a value and isolate the elements that do not.</summary>
    public partial class ReviewView : Page, IDockablePaneProvider
    {
        public ReviewView()
        {
            InitializeComponent();
        }

        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.FrameworkElement = this;
            data.VisibleByDefault = false;
            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Tabbed,
                TabBehind = DockablePanes.BuiltInDockablePanes.ProjectBrowser
            };
        }
    }
}
