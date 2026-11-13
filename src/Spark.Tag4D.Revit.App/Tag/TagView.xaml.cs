using Autodesk.Revit.UI;
using System;
using System.Windows.Controls;

namespace Spark.Tag4D.Revit.App.Tag
{
    /// <summary>
    /// Interaction logic for TagView.xaml
    /// </summary>
    public partial class TagView : Page, IDisposable, IDockablePaneProvider
    {
        public TagView()
        {
            InitializeComponent();
        }

        // Text values are applied when the field is left; Enter applies them right away.
        private void TextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter && sender is System.Windows.Controls.TextBox textBox)
            {
                textBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();
                e.Handled = true;
            }
        }

        private void OptionsButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            OptionsPopup.IsOpen = !OptionsPopup.IsOpen;
        }

        private void OptionsAction_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            OptionsPopup.IsOpen = false;
        }

        public void Dispose()
        {
        }

        public void SetupDockablePane(DockablePaneProviderData data)
        {
            // wpf object with pane's interface
            data.FrameworkElement = this;
            data.VisibleByDefault = false;
            // initial state position
            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Tabbed,
                TabBehind = DockablePanes.BuiltInDockablePanes.ProjectBrowser
            };
        }
    }
}
