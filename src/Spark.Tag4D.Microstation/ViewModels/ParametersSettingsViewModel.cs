using Spark.Tag4D.Core;
using Spark.Tag4D.Core.Utils;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Spark.Tag4D.Microstation.ViewModels
{
    public class ParametersSettingsViewModel : BindableBase
    {
        public ParametersSettingsViewModel()
        {
            SettingsFilePath = LoadSettings();
            LoadSettingsCommand = new DelegateCommand(OnLoadSettingsCommand);

            ExcelFilePath = Properties.Settings.Default.ExcelFilePath;

            LoadExcelCommand = new DelegateCommand(OnLoadExcelCommand);
        }

        private void OnLoadExcelCommand()
        {
            try
            {
                string excelFilePath = FilePromptUtils.GetFilePath(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Excel files (*.xlsx)|*.xlsx");

                if (string.IsNullOrEmpty(excelFilePath) || !File.Exists(excelFilePath))
                {
                    return;
                }

                ExcelFilePath = excelFilePath;
            }
            catch (Exception)
            {
                MessageBox.Show("Error loading Excel file. Please ensure the file is not open in another application and try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnLoadSettingsCommand()
        {
            try
            {
                string settingsFilePath = FilePromptUtils.GetFilePath(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "json files (*.json)|*.json");

                if (string.IsNullOrEmpty(settingsFilePath) || !File.Exists(settingsFilePath))
                {
                    return;
                }

                SettingsFilePath = settingsFilePath;

            }
            catch (Exception)
            {
                MessageBox.Show("Error loading settings file. Please ensure the file is not open in another application and try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string LoadSettings()
        {
            if (string.IsNullOrEmpty(Properties.Settings.Default.SettingsFilePath) || !File.Exists(Properties.Settings.Default.SettingsFilePath))
            {
                string assemblyFolder = AssemblyUtils.GetFolder(typeof(ParametersSettingsViewModel));
                string defaultSettingsPath = System.IO.Path.Combine(assemblyFolder, Constants.FilesFolder, Constants.Tag4DFolder, Constants.JsonSettingsFileName);
            }

            if (System.IO.File.Exists(Properties.Settings.Default.SettingsFilePath))
            {
                return Properties.Settings.Default.SettingsFilePath;
            }
            else
            {
                return "N/A";
            }
        }

        private string settingsFilePath;
        private string excelFilePath;

        public string SettingsFilePath
        {
            get { return settingsFilePath; }
            set
            {
                settingsFilePath = value;
                Properties.Settings.Default.SettingsFilePath = settingsFilePath;
                Properties.Settings.Default.Save();
                RaisePropertyChanged();
            }
        }

        public DelegateCommand LoadSettingsCommand { get; }
        public string ExcelFilePath
        {
            get
            {
                return excelFilePath;
            }
            set
            {
                excelFilePath = value;
                Properties.Settings.Default.ExcelFilePath = excelFilePath;
                Properties.Settings.Default.Save();
                RaisePropertyChanged();
            }
        }

        public DelegateCommand LoadExcelCommand { get; }

        public event EventHandler CloseRequest;

        public void RaiseCloseRequest()
        {
            CloseRequest?.Invoke(this, EventArgs.Empty);
        }
    }
}
