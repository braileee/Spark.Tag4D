using System.Diagnostics;
using System.Windows;
using Spark.Tag4D.Core.Configuration;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Core.Mvvm;
using Spark.Tag4D.IfcValidator.Models;
using Spark.Tag4D.UI.Dialogs;

namespace Spark.Tag4D.IfcValidator.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly UserSettings _userSettings = UserSettings.Current;

        private string _settingsFilePath;
        private string _excelFilePath;
        private string _ifcFolderPath;
        private string _statusMessage = "Choose the files and press Validate.";
        private bool _isBusy;

        public MainViewModel()
        {
            _settingsFilePath = SettingsLoader.GetPath();

            // Make sure a workbook path is resolved even on first start (falls back to the shipped default).
            SettingsRoot settings = SettingsLoader.LoadExistingOrDefault();
            if (settings != null)
            {
                ExcelDataLoader.LoadOrPromptExcelFilePath(settings.ExcelSettings.FileLinkSettings);
            }

            _settingsFilePath = SettingsLoader.GetPath();
            _excelFilePath = ExcelDataLoader.GetPath();
            _ifcFolderPath = _userSettings.IfcFolderPath;

            SelectSettingsCommand = new RelayCommand(SelectSettings);
            SelectExcelCommand = new RelayCommand(SelectExcel);
            SelectIfcFolderCommand = new RelayCommand(SelectIfcFolder);

            ValidateCommand = new AsyncRelayCommand(ValidateAsync, CanValidate);
            ValidateCommand.Failed += (_, exception) => Report($"Validation failed: {exception.Message}", MessageBoxImage.Error);
        }

        public string SettingsFilePath
        {
            get => _settingsFilePath;
            set
            {
                if (SetProperty(ref _settingsFilePath, value))
                {
                    SettingsLoader.SavePath(value);
                }
            }
        }

        public string ExcelFilePath
        {
            get => _excelFilePath;
            set
            {
                if (SetProperty(ref _excelFilePath, value))
                {
                    ExcelDataLoader.SavePath(value);
                    ValidateCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        public string IfcFolderPath
        {
            get => _ifcFolderPath;
            set
            {
                if (SetProperty(ref _ifcFolderPath, value))
                {
                    _userSettings.IfcFolderPath = value;
                    _userSettings.Save();
                    ValidateCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set => SetProperty(ref _isBusy, value);
        }

        public RelayCommand SelectSettingsCommand { get; }

        public RelayCommand SelectExcelCommand { get; }

        public RelayCommand SelectIfcFolderCommand { get; }

        public AsyncRelayCommand ValidateCommand { get; }

        private bool CanValidate() => File.Exists(ExcelFilePath ?? string.Empty) && Directory.Exists(IfcFolderPath ?? string.Empty);

        private void SelectSettings()
        {
            string path = FileDialogs.OpenFile(FileDialogs.JsonFilter, DirectoryOf(SettingsFilePath));
            if (!string.IsNullOrEmpty(path))
            {
                SettingsFilePath = path;
            }
        }

        private void SelectExcel()
        {
            string path = FileDialogs.OpenFile(FileDialogs.ExcelFilter, DirectoryOf(ExcelFilePath));
            if (!string.IsNullOrEmpty(path))
            {
                ExcelFilePath = path;
            }
        }

        private void SelectIfcFolder()
        {
            string path = FileDialogs.PickFolder(IfcFolderPath);
            if (!string.IsNullOrEmpty(path))
            {
                IfcFolderPath = path;
            }
        }

        private async Task ValidateAsync()
        {
            string reportFilePath = FileDialogs.SaveFile("Excel files (*.xlsx)|*.xlsx", "IfcValidationReport.xlsx");
            if (string.IsNullOrEmpty(reportFilePath))
            {
                return;
            }

            string settingsPath = SettingsFilePath;
            string excelPath = ExcelFilePath;
            string ifcFolder = IfcFolderPath;

            IsBusy = true;

            try
            {
                StatusMessage = "Loading rules...";

                // Heavy work (IFC parsing, report generation) runs off the UI thread.
                string summary = await Task.Run(() =>
                {
                    SettingsRoot settings = string.IsNullOrEmpty(settingsPath) || !File.Exists(settingsPath)
                        ? SettingsLoader.LoadExistingOrDefault()
                        : SettingsLoader.Read(settingsPath);

                    if (settings == null)
                    {
                        throw new TagDataException("No settings file found.");
                    }

                    TagData data = TagData.Load(settings, excelPath);

                    string[] ifcFilePaths = Directory.GetFiles(ifcFolder, "*.ifc", SearchOption.TopDirectoryOnly);
                    if (ifcFilePaths.Length == 0)
                    {
                        throw new TagDataException("The selected folder does not contain any .ifc files.");
                    }

                    List<IfcFile> ifcFiles = new IfcFileDataCollector(ifcFilePaths.ToList()).CollectFromFiles();
                    new ReportWriter(data, ifcFiles, reportFilePath).Create();

                    return $"Validated {ifcFilePaths.Length} IFC file(s). Report saved to {reportFilePath}";
                });

                StatusMessage = summary;

                Process.Start(new ProcessStartInfo { FileName = reportFilePath, UseShellExecute = true });
            }
            catch (TagDataException exception)
            {
                Report(exception.Message, MessageBoxImage.Warning);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void Report(string message, MessageBoxImage image)
        {
            StatusMessage = message;
            MessageBox.Show(message, "IFC Validator", MessageBoxButton.OK, image);
        }

        private static string DirectoryOf(string filePath)
        {
            return string.IsNullOrEmpty(filePath) ? null : Path.GetDirectoryName(filePath);
        }
    }
}
