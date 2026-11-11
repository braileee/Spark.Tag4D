using Spark.Tag4D.Core.Mvvm;

using System;

namespace Spark.Tag4D.Settings.ViewModels.Support
{
    public class MatchCriterionViewModel : ObservableObject
    {
        private readonly Action _markDirty;

        private string _propertyName = string.Empty;
        private string _value = string.Empty;

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
                    _markDirty();
                }
            }
        }
    }
}