using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Revit.Core.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void RaiseCloseRequest()
        {
            OnRequestClose?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler OnRequestClose;

        protected void RaiseHideRequest()
        {
            OnRequestHide?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler OnRequestHide;

        protected void RaiseTopmost()
        {
            OnRequestTopmost?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler OnRequestTopmost;
    }
}
