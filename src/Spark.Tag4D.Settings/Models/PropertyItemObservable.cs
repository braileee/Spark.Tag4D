using Spark.Tag4D.Core.Mvvm;


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Settings.Models
{
    public class PropertyItemObservable : ObservableObject
    {
        private string _propertyName;
        public string PropertyName
        {
            get => _propertyName;
            set => SetProperty(ref _propertyName, value);
        }

        private string _dataType;
        public string DataType
        {
            get => _dataType;
            set => SetProperty(ref _dataType, value);
        }
    }
}
