using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Spark.Tag4D.Core.Mvvm
{
    /// <summary>
    /// ICommand for asynchronous work. It is disabled while the task runs (no re-entrancy) and
    /// exceptions are surfaced through <see cref="Failed"/> instead of crashing the UI thread.
    /// </summary>
    public sealed class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _isRunning;

        public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        /// <summary>Raised when the delegate throws.</summary>
        public event EventHandler<Exception>? Failed;

        public bool IsRunning => _isRunning;

        public bool CanExecute(object? parameter) => !_isRunning && (_canExecute?.Invoke() ?? true);

        public async void Execute(object? parameter)
        {
            await ExecuteAsync();
        }

        public async Task ExecuteAsync()
        {
            if (!CanExecute(null))
            {
                return;
            }

            _isRunning = true;
            RaiseCanExecuteChanged();

            try
            {
                await _execute();
            }
            catch (Exception exception)
            {
                Failed?.Invoke(this, exception);
            }
            finally
            {
                _isRunning = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
