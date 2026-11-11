using Spark.Tag4D.Microstation.ViewModels;
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

namespace Spark.Tag4D.Microstation.Views
{
    /// <summary>
    /// Interaction logic for ParametersSettingsView.xaml
    /// </summary>
    public partial class ParametersSettingsView : Window
    {
        public ParametersSettingsView()
        {
            InitializeComponent();

            ParametersSettingsViewModel viewModel = new ParametersSettingsViewModel();
            DataContext = viewModel;

            viewModel.CloseRequest += OnCloseRequest;
        }


        private void OnCloseRequest(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
