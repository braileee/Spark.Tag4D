using Spark.Tag4D.Core.Mvvm;
using Spark.Tag4D.Settings.Models;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Settings.ViewModels.Support
{
    public class ExactMatchRowViewModel : ObservableObject
    {
        private readonly Action _markDirty;

        private string _source = string.Empty;
        private string _target = string.Empty;

        public ExactMatchRowViewModel(Action markDirty)
        {
            _markDirty = markDirty;
        }

        public string Source
        {
            get => _source;
            set
            {
                if (SetProperty(ref _source, value))
                {
                    _markDirty();
                }
            }
        }

        public string Target
        {
            get => _target;
            set
            {
                if (SetProperty(ref _target, value))
                {
                    _markDirty();
                }
            }
        }
    }
}
