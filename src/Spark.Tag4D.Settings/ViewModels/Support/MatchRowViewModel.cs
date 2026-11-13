using Spark.Tag4D.Core.Mvvm;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Spark.Tag4D.Settings.ViewModels.Support
{
    /// <summary>One row of a match table: the source values and the target value they select.</summary>
    public class MatchRowViewModel : ObservableObject
    {
        private readonly Action _markDirty;

        private string _targetValue = string.Empty;
        private MatchTableViewModel _table;

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
                    OnPropertyChanged(nameof(IsTargetUnknown));
                    _markDirty();
                }
            }
        }

        /// <summary>The picklist values of the target property of the table.</summary>
        public IReadOnlyList<string> TargetSuggestions => _table?.TargetValues ?? Array.Empty<string>();

        public bool IsTargetUnknown => PicklistCheck.IsUnknown(TargetSuggestions, TargetValue);

        internal void Attach(MatchTableViewModel table)
        {
            _table = table;
            RefreshTarget();
        }

        internal void RefreshTarget()
        {
            OnPropertyChanged(nameof(TargetSuggestions));
            OnPropertyChanged(nameof(IsTargetUnknown));
        }

        /// <summary>A row without any value (just added): it is never hidden by the search, so it can be filled in.</summary>
        internal bool IsEmpty => string.IsNullOrEmpty(TargetValue) && Criteria.All(criterion => string.IsNullOrEmpty(criterion.Value));

        /// <summary>Does any cell of the row contain <paramref name="text"/>?</summary>
        internal bool Contains(string text)
        {
            return (TargetValue ?? string.Empty).IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0
                || Criteria.Any(criterion => (criterion.Value ?? string.Empty).IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
