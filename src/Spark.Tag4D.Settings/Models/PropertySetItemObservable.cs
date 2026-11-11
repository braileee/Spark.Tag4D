using Spark.Tag4D.Core.Mvvm;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Settings.Models
{
    public class PropertySetItemObservable : ObservableObject
    {

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public ObservableCollection<PropertyItemObservable> Properties { get; set; }
            = new ObservableCollection<PropertyItemObservable>();

    }
}
