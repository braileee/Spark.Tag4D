using System;
using System.Threading.Tasks;
using Spark.Tag4D.Core.Mvvm;
using Xunit;

namespace Spark.Tag4D.Core.Tests
{
    public class MvvmTests
    {
        private sealed class Sample : ObservableObject
        {
            private string _name = string.Empty;

            public string Name
            {
                get => _name;
                set => SetProperty(ref _name, value);
            }
        }

        [Fact]
        public void SetProperty_raises_only_on_change()
        {
            var sample = new Sample();
            int raised = 0;
            string? last = null;
            sample.PropertyChanged += (_, e) => { raised++; last = e.PropertyName; };

            sample.Name = "a";
            sample.Name = "a";

            Assert.Equal(1, raised);
            Assert.Equal(nameof(Sample.Name), last);
        }

        [Fact]
        public void RelayCommand_honours_can_execute()
        {
            bool allowed = false;
            int calls = 0;
            var command = new RelayCommand(() => calls++, () => allowed);

            Assert.False(command.CanExecute(null));
            allowed = true;
            Assert.True(command.CanExecute(null));
            command.Execute(null);
            Assert.Equal(1, calls);
        }

        [Fact]
        public void Typed_RelayCommand_passes_the_parameter()
        {
            string? received = null;
            var command = new RelayCommand<string>(value => received = value);

            command.Execute("x");

            Assert.Equal("x", received);
        }

        [Fact]
        public async Task AsyncRelayCommand_blocks_reentrancy_and_reports_failures()
        {
            var gate = new TaskCompletionSource<bool>();
            int runs = 0;
            var command = new AsyncRelayCommand(async () => { runs++; await gate.Task; });

            Task first = command.ExecuteAsync();
            Assert.False(command.CanExecute(null));
            await command.ExecuteAsync(); // ignored while running
            gate.SetResult(true);
            await first;

            Assert.Equal(1, runs);
            Assert.True(command.CanExecute(null));

            Exception? failure = null;
            var failing = new AsyncRelayCommand(() => throw new InvalidOperationException("boom"));
            failing.Failed += (_, e) => failure = e;
            await failing.ExecuteAsync();

            Assert.IsType<InvalidOperationException>(failure);
            Assert.True(failing.CanExecute(null));
        }
    }
}
