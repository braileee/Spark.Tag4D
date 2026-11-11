using Autodesk.Revit.UI;
using Spark.Tag4D.Revit.App.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Spark.Tag4D.Revit.App.Views
{
    /// <summary>
    /// Interaction logic for ParametersSettingsView.xaml
    /// </summary>
    public partial class ParametersSettingsView : Window
    {
        public ParametersSettingsView(ExternalCommandData commandData)
        {
            InitializeComponent();

            ParametersSettingsViewModel viewModel = new ParametersSettingsViewModel(commandData.Application);
            DataContext = viewModel;

            viewModel.CloseRequest += OnCloseRequest;
        }

        private void OnCloseRequest(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
