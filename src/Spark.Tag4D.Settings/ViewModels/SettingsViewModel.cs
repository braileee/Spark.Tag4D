using Spark.Tag4D.Core.Json;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Settings.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Settings.ViewModels
{
    public class SettingsViewModel
    {
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
        }

        public SettingsRoot Settings { get; }
        public PathsViewModel PathsViewModel { get; set; }
        public PropertiesViewModel PropertiesViewModel { get; set; }
        public PicklistViewModel PicklistViewModel { get; set; }
        public ExactMatchesViewModel ExactMatchesViewModel { get; set; }
        public MatchesViewModel MatchesViewModel { get; }
        public ExpressionEditorViewModel ExpressionEditorViewModel { get; }
        public ComposedViewModel ComposedViewModel { get; }
    }
}
