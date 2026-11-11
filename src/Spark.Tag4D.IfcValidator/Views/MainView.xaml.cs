using System.Windows;
using Spark.Tag4D.IfcValidator.ViewModels;

namespace Spark.Tag4D.IfcValidator.Views
{
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
