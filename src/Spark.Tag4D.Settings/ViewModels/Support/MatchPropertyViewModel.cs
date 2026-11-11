using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Settings.ViewModels.Support
{
    public class MatchPropertyViewModel : BindableBase
    {
        private readonly Action _markDirty;

        private string _propertyName;

        public MatchPropertyViewModel(
            Action markDirty,
            string propertyName = "")
        {
            _markDirty = markDirty;
            _propertyName = propertyName;
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

        public ObservableCollection<string> AvailableProperties { get; set; } = new ObservableCollection<string>();
    }
}
