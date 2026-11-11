using System;
using Spark.Tag4D.Core.Mvvm;

namespace Spark.Tag4D.Revit.Core.ViewModels
{
    /// <summary>View-model base that adds window-level requests (close/hide/topmost) to <see cref="ObservableObject"/>.</summary>
    public class ViewModelBase : ObservableObject
    {
        public event EventHandler OnRequestClose;

        public event EventHandler OnRequestHide;

        public event EventHandler OnRequestTopmost;

        protected void RaiseCloseRequest() => OnRequestClose?.Invoke(this, EventArgs.Empty);

        protected void RaiseHideRequest() => OnRequestHide?.Invoke(this, EventArgs.Empty);

        protected void RaiseTopmost() => OnRequestTopmost?.Invoke(this, EventArgs.Empty);
    }
}
