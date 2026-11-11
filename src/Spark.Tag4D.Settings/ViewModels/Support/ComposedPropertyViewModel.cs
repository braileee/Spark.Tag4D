using Spark.Tag4D.Core.Mvvm;

using System.Collections.ObjectModel;

namespace Spark.Tag4D.Settings.ViewModels.Support
{
    public class ComposedProperty : ObservableObject
    {
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _formula = string.Empty;
        public string Formula
        {
            get => _formula;
            set => SetProperty(ref _formula, value);
        }

        public ObservableCollection<FormulaSegment> Segments { get; }
            = new();
    }
}