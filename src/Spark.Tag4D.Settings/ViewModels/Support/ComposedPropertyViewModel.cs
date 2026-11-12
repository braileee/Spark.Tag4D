using Spark.Tag4D.Core.Mvvm;
using Spark.Tag4D.Settings.Services;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Spark.Tag4D.Settings.ViewModels.Support
{
    public class ComposedProperty : ObservableObject
    {
        public ComposedProperty()
        {
            Segments.CollectionChanged += OnSegmentsChanged;
        }

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
            = new ObservableCollection<FormulaSegment>();

        /// <summary>The formula as it will be stored, e.g. <c>A-&lt;Level&gt;.&lt;Zone&gt;</c>; follows every edit.</summary>
        public string Preview => FormulaParser.Build(Segments);

        private void OnSegmentsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (FormulaSegment segment in e.OldItems)
                {
                    segment.PropertyChanged -= OnSegmentChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (FormulaSegment segment in e.NewItems)
                {
                    segment.Parent = this;
                    segment.PropertyChanged += OnSegmentChanged;
                }
            }

            OnPropertyChanged(nameof(Preview));
        }

        private void OnSegmentChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Preview));
        }
    }
}
