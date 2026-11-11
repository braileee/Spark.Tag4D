using Spark.Tag4D.Core.Configuration;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.UI.Dialogs;
using Spark.Tag4D.Core.Mvvm;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Settings.ViewModels
{
    public class PathsViewModel : ObservableObject
    {
        public PathsViewModel()
        {
            SettingsFilePath = LoadSettings();
            LoadSettingsCommand = new RelayCommand(OnLoadSettingsCommand);
            ExcelFilePath = ExcelDataLoader.LoadOrPromptExcelFilePath(SettingsRoot.ExcelSettings.FileLinkSettings);
            LoadExcelCommand = new RelayCommand(OnLoadExcelCommand);
        }

        private void OnLoadExcelCommand()
        {
            string excelFilePath = FileDialogs.OpenFile(FileDialogs.ExcelFilter);

            if (string.IsNullOrEmpty(excelFilePath) || !File.Exists(excelFilePath))
            {
                return;
            }

            ExcelFilePath = excelFilePath;
        }

        private void OnLoadSettingsCommand()
        {
            string settingsFilePath = FileDialogs.OpenFile(FileDialogs.JsonFilter);

            if (string.IsNullOrEmpty(settingsFilePath) || !File.Exists(settingsFilePath))
            {
                return;
            }

            SettingsFilePath = settingsFilePath;
        }

        private string LoadSettings()
        {
            SettingsRoot = SettingsLoader.LoadExistingOrDefault();

            if (SettingsRoot == null)
            {
                return "N/A";
            }

            return SettingsLoader.GetPath();
        }

        private string settingsFilePath;
        private string excelFilePath;

        public string SettingsFilePath
        {
            get { return settingsFilePath; }
            set
            {
                settingsFilePath = value;
                SettingsLoader.SavePath(value);
                OnPropertyChanged();
            }
        }

        public RelayCommand LoadSettingsCommand { get; }
        public string ExcelFilePath
        {
            get
            {
                return excelFilePath;
            }
            set
            {
                excelFilePath = value;
                ExcelDataLoader.SavePath(value);
                OnPropertyChanged();
            }
        }

        public RelayCommand LoadExcelCommand { get; }
        public SettingsRoot SettingsRoot { get; private set; }

        public event EventHandler CloseRequest;

        public void RaiseCloseRequest()
        {
            CloseRequest?.Invoke(this, EventArgs.Empty);
        }


    }
}
