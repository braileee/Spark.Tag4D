using Spark.Tag4D.Core.Mvvm;

using System;
using System.Collections.Generic;

namespace Spark.Tag4D.Settings.ViewModels.Support
{
    /// <summary>One cell of a match row: the value a source property must have. Offers the picklist of that property.</summary>
    public class MatchCriterionViewModel : ObservableObject
    {
        private readonly Action _markDirty;

        private string _propertyName = string.Empty;
        private string _value = string.Empty;
        private MatchPropertyViewModel _column;
        private Func<string, IReadOnlyList<string>> _valuesFor;

        public MatchCriterionViewModel(Action markDirty)
        {
            _markDirty = markDirty;
        }

        public string PropertyName
        {
            get => _propertyName;
            set
            {
                if (SetProperty(ref _propertyName, value))
                {
                    _markDirty();
                }
            }
        }

        public string Value
        {
            get => _value;
            set
            {
                if (SetProperty(ref _value, value))
                {
                    OnPropertyChanged(nameof(IsUnknown));
                    _markDirty();
                }
            }
        }

        /// <summary>The picklist values of the property of this column (empty when it has no picklist).</summary>
        public IReadOnlyList<string> Suggestions => _valuesFor?.Invoke(_column?.PropertyName) ?? Array.Empty<string>();

        /// <summary>The value is not in the picklist of its property - it can never be selected in the tagging pane.</summary>
        public bool IsUnknown => PicklistCheck.IsUnknown(Suggestions, Value);

        /// <summary>Links the cell to its column, which decides the property (and so the picklist) of the cell.</summary>
        internal void Attach(MatchPropertyViewModel column, Func<string, IReadOnlyList<string>> valuesFor)
        {
            if (_column != null)
            {
                _column.PropertyChanged -= OnColumnChanged;
            }

            _column = column;
            _valuesFor = valuesFor;

            if (_column != null)
            {
                _column.PropertyChanged += OnColumnChanged;
            }

            Refresh();
        }

        private void OnColumnChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) => Refresh();

        internal void Refresh()
        {
            OnPropertyChanged(nameof(Suggestions));
            OnPropertyChanged(nameof(IsUnknown));
        }
    }

    internal static class PicklistCheck
    {
        /// <summary>A value is unknown when the property has a picklist and the value is filled in but not part of it.</summary>
        public static bool IsUnknown(IReadOnlyList<string> picklist, string value)
        {
            if (picklist == null || picklist.Count == 0 || string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            foreach (string item in picklist)
            {
                if (string.Equals(item, value, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
