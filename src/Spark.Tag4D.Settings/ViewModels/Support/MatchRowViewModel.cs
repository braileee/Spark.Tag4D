using Spark.Tag4D.Core.Mvvm;

using System;
using System.Collections.ObjectModel;

namespace Spark.Tag4D.Settings.ViewModels.Support
{
    public class MatchRowViewModel : ObservableObject
    {
        private readonly Action _markDirty;

        private string _targetValue = string.Empty;

        public MatchRowViewModel(Action markDirty)
        {
            _markDirty = markDirty;
        }

        public ObservableCollection<MatchCriterionViewModel>
            Criteria
        { get; }
            = new();

        public string TargetValue
        {
            get => _targetValue;
            set
            {
                if (SetProperty(ref _targetValue, value))
                {
                    _markDirty();
                }
            }
        }
    }
}
