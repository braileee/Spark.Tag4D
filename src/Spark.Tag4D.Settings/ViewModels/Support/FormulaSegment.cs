using Prism.Mvvm;

namespace Spark.Tag4D.Settings.ViewModels.Support
{
    public class FormulaSegment : BindableBase
    {
        private string _prefix = string.Empty;
        public string Prefix
        {
            get => _prefix;
            set => SetProperty(ref _prefix, value);
        }

        private string _selectedProperty = string.Empty;
        public string SelectedProperty
        {
            get => _selectedProperty;
            set => SetProperty(ref _selectedProperty, value);
        }

        private string _suffix = string.Empty;
        public string Suffix
        {
            get => _suffix;
            set => SetProperty(ref _suffix, value);
        }

        public ComposedProperty Parent { get; set; }
    }
}