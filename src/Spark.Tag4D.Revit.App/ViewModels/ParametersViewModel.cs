using Spark.Tag4D.Core.Configuration;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Core.Mvvm;
using Spark.Tag4D.Revit.App.EventHandlers;
using Spark.Tag4D.Revit.App.Models;
using Spark.Tag4D.Settings.ViewModels;
using Spark.Tag4D.Settings.Views;
using Spark.Tag4D.UI.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace Spark.Tag4D.Revit.App.ViewModels
{
    /// <summary>One line of the activity log.</summary>
    public sealed class LogEntry
    {
        public LogEntry(string message, LogLevel level)
        {
            Time = DateTime.Now.ToString("HH:mm:ss");
            Message = message;
            Level = level;
        }

        public string Time { get; }

        public string Message { get; }

        public LogLevel Level { get; }
    }

    /// <summary>Backs the "Parameters" window: configuration summary, load / delete parameters, IFC mapping file, log.</summary>
    public class ParametersViewModel : ObservableObject
    {
        private readonly Dispatcher dispatcher;
        private readonly ParametersOperationEventHandler operationHandler;

        private string settingsFilePath = string.Empty;
        private string excelFilePath = string.Empty;
        private string contentSummary = string.Empty;
        private bool isBusy;
        private bool isIndeterminate;
        private double progressValue;
        private string statusText = "Ready";

        internal ParametersViewModel(ParametersOperationEventHandler operationHandler)
        {
            this.operationHandler = operationHandler;
            dispatcher = Dispatcher.CurrentDispatcher;

            LoadParametersCommand = new RelayCommand(() => RunOperation(ParametersOperation.Load), () => !IsBusy);
            DeleteParametersCommand = new RelayCommand(ConfirmAndDelete, () => !IsBusy);
            GenerateMappingCommand = new RelayCommand(GenerateMappingFile, () => !IsBusy);
            OpenSettingsCommand = new RelayCommand(OpenSettings, () => !IsBusy);
            ClearLogCommand = new RelayCommand(() => Log.Clear());

            RefreshConfiguration();
        }

        public ObservableCollection<LogEntry> Log { get; } = new ObservableCollection<LogEntry>();

        public string SettingsFilePath
        {
            get => settingsFilePath;
            private set => SetProperty(ref settingsFilePath, value);
        }

        public string ExcelFilePath
        {
            get => excelFilePath;
            private set => SetProperty(ref excelFilePath, value);
        }

        public string ContentSummary
        {
            get => contentSummary;
            private set => SetProperty(ref contentSummary, value);
        }

        public bool IsBusy
        {
            get => isBusy;
            private set
            {
                if (SetProperty(ref isBusy, value))
                {
                    LoadParametersCommand.RaiseCanExecuteChanged();
                    DeleteParametersCommand.RaiseCanExecuteChanged();
                    GenerateMappingCommand.RaiseCanExecuteChanged();
                    OpenSettingsCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsIndeterminate
        {
            get => isIndeterminate;
            private set => SetProperty(ref isIndeterminate, value);
        }

        /// <summary>Progress of the running operation, 0 - 100.</summary>
        public double ProgressValue
        {
            get => progressValue;
            private set => SetProperty(ref progressValue, value);
        }

        public string StatusText
        {
            get => statusText;
            private set => SetProperty(ref statusText, value);
        }

        public RelayCommand LoadParametersCommand { get; }

        public RelayCommand DeleteParametersCommand { get; }

        public RelayCommand GenerateMappingCommand { get; }

        public RelayCommand OpenSettingsCommand { get; }

        public RelayCommand ClearLogCommand { get; }

        /// <summary>Re-reads the settings file and workbook to show what is currently in use.</summary>
        public void RefreshConfiguration()
        {
            try
            {
                TagData data = TagData.Load();
                SettingsFilePath = ActiveSettingsFile();
                ExcelFilePath = data.ExcelFilePath;
                int parameters = data.PropertySets.Sum(set => set.PropertyItems.Count);
                ContentSummary = $"{data.PropertySets.Count} property sets, {parameters} parameters, {data.Picklists.Count} picklists";
            }
            catch (TagDataException exception)
            {
                SettingsFilePath = ActiveSettingsFile();
                ExcelFilePath = "(not available)";
                ContentSummary = string.Empty;
                AddLog(exception.Message, LogLevel.Warning);
            }
        }

        private static string ActiveSettingsFile()
        {
            string path = SettingsLoader.GetPath();
            return string.IsNullOrEmpty(path) || !File.Exists(path) ? SettingsLoader.GetDefaultSettingsFilePath() : path;
        }

        private void OpenSettings()
        {
            try
            {
                new SettingsView { DataContext = new SettingsViewModel(), Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) }.ShowDialog();
            }
            catch (Exception exception)
            {
                AddLog($"The settings could not be opened: {exception.Message}", LogLevel.Error);
                return;
            }

            AddLog("Settings closed - configuration reloaded.");
            RefreshConfiguration();
        }

        private void ConfirmAndDelete()
        {
            MessageBoxResult answer = MessageBox.Show(
                "This removes the workbook parameters from the Revit project, together with the values stored in them.\n\nContinue?",
                "Spark.Tag4D - Delete parameters",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (answer == MessageBoxResult.Yes)
            {
                RunOperation(ParametersOperation.Delete);
            }
        }

        private void RunOperation(ParametersOperation operation)
        {
            TagData data;

            try
            {
                data = TagData.Load();
            }
            catch (TagDataException exception)
            {
                AddLog(exception.Message, LogLevel.Error);
                return;
            }

            SettingsFilePath = ActiveSettingsFile();
            ExcelFilePath = data.ExcelFilePath;

            string title = operation == ParametersOperation.Load ? "Loading parameters" : "Deleting parameters";
            AddLog($"{title} from {Path.GetFileName(data.ExcelFilePath)}");
            StartBusy(title);

            var progress = new OperationProgress
            {
                OnMessage = AddLog,
                OnProgress = ReportProgress,
                OnCompleted = FinishBusy,
            };

            operationHandler.Raise(new ParametersOperationOptions
            {
                Operation = operation,
                PropertySetItems = data.PropertySets,
                Progress = progress,
            });
        }

        private void GenerateMappingFile()
        {
            try
            {
                TagData data = TagData.Load();

                string path = FileDialogs.SaveFile("Text files (*.txt)|*.txt", "IfcMapping.txt");
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                StartBusy("Generating IFC mapping file");
                AddLog($"Generating the IFC mapping file from {Path.GetFileName(data.ExcelFilePath)}");
                Flush();

                string content = new IfcMappingContent(data.PropertySets).ToRevitTxtFormat();
                File.WriteAllText(path, content, Encoding.UTF8);

                int parameters = data.PropertySets.Sum(set => set.PropertyItems.Count);
                AddLog($"{data.PropertySets.Count} property sets and {parameters} parameters written.");
                AddLog($"Saved to {path}", LogLevel.Success);
            }
            catch (TagDataException exception)
            {
                AddLog(exception.Message, LogLevel.Error);
            }
            catch (Exception exception)
            {
                AddLog($"Generating the mapping file failed: {exception.Message}", LogLevel.Error);
            }
            finally
            {
                FinishBusy();
            }
        }

        private void StartBusy(string title)
        {
            IsBusy = true;
            IsIndeterminate = true;
            ProgressValue = 0;
            StatusText = title + "...";
            Flush();
        }

        private void ReportProgress(int done, int total)
        {
            IsIndeterminate = false;
            ProgressValue = total == 0 ? 0 : 100.0 * done / total;
            StatusText = $"{done} of {total}";
            Flush();
        }

        private void FinishBusy()
        {
            IsBusy = false;
            IsIndeterminate = false;
            StatusText = "Ready";
        }

        private void AddLog(string message, LogLevel level = LogLevel.Info)
        {
            Log.Add(new LogEntry(message, level));
            Flush();
        }

        /// <summary>
        /// The operations run on Revit's UI thread, so the window would not repaint until they finish.
        /// Letting the dispatcher render pending changes keeps the progress visible.
        /// </summary>
        private void Flush()
        {
            if (dispatcher.CheckAccess())
            {
                dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));
            }
        }
    }
}
