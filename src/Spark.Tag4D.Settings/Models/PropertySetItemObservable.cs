using Spark.Tag4D.Core.Mvvm;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace Spark.Tag4D.Settings.Models
{
    public class PropertySetItemObservable : ObservableObject
    {
        public PropertySetItemObservable()
        {
            PropertiesView = CollectionViewSource.GetDefaultView(Properties);
        }

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public ObservableCollection<PropertyItemObservable> Properties { get; set; }
            = new ObservableCollection<PropertyItemObservable>();

        /// <summary>The properties shown, narrowed down while searching.</summary>
        public ICollectionView PropertiesView { get; private set; }

        /// <summary>Does the set match the search text by its name or by one of its properties? New, still empty entries always match.</summary>
        public bool Matches(string text)
        {
            return string.IsNullOrEmpty(text) || string.IsNullOrEmpty(Name) || Has(Name, text)
                || Properties.Any(p => string.IsNullOrEmpty(p.PropertyName) || Has(p.PropertyName, text) || Has(p.DataType, text));
        }

        /// <summary>A search that matches the set name shows the whole set; otherwise only the matching properties.</summary>
        public void ApplyFilter(string text)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(Name) || Has(Name, text))
            {
                PropertiesView.Filter = null;
            }
            else
            {
                PropertiesView.Filter = item =>
                {
                    var property = (PropertyItemObservable)item;
                    return string.IsNullOrEmpty(property.PropertyName) || Has(property.PropertyName, text) || Has(property.DataType, text);
                };
            }
        }

        private static bool Has(string value, string text) =>
            (value ?? string.Empty).IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
