using ControlzEx.Standard;
using Spark.Tag4D.Core.Configuration;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.IfcValidator.Models;
using Spark.Tag4D.IfcValidator.Utils;
using Spark.Tag4D.Core.Mvvm;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Spark.Tag4D.IfcValidator.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        public MainViewModel()
        {
            var settingsRoot = SettingsLoader.LoadExistingOrDefault();

            ExcelDataLoader.LoadOrPromptExcelFilePath(settingsRoot.ExcelSettings.FileLinkSettings);

            ExcelFilePath = ExcelDataLoader.GetPath();
            IfcFolderPath = Properties.Settings.Default.IfcFolderPath;
            SettingsFilePath = SettingsLoader.GetPath();

            SelectExcelCommand = new RelayCommand(OnSelectExcelCommand);
            SelectIfcFolderCommand = new RelayCommand(OnSelectIfcFolderCommand);
            SelectSettingsCommand = new RelayCommand(OnSelectSettingsCommand);

            ValidateCommand = new RelayCommand(OnValidateCommand);
        }

        private void OnSelectSettingsCommand()
        {
            string initDirectory = string.IsNullOrEmpty(SettingsFilePath) ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop) : Path.GetDirectoryName(SettingsFilePath);

            string settingsFilePath = FileUtils.GetFilePath(initDirectory, "JSON files (*.json)|*.json|All files (*.*)|*.*");
            if (string.IsNullOrEmpty(settingsFilePath))
            {
                return;
            }
            SettingsFilePath = settingsFilePath;
        }

        private void OnValidateCommand()
        {
            try
            {
                if (ExcelFilePath == null || IfcFolderPath == null)
                {
                    MessageBox.Show("Please select both the Excel file and the IFC folder before validating.", "Error");
                    return;
                }

                SettingsRoot settingsRoot = SettingsLoader.LoadExistingOrDefault();

                if (settingsRoot == null || settingsRoot?.ExcelSettings == null)
                {
                    MessageBox.Show("Failed to load settings. Please check the settings file.", "Error");
                    return;
                }

                List<PropertySetItem> propertySetItems = ExcelDataLoader.LoadPropertySetItems(ExcelFilePath, settingsRoot.ExcelSettings.PropertiesSheet);
                List<PicklistGroup> picklistGroups = ExcelDataLoader.LoadPicklistGroups(excelFilePath, settingsRoot.ExcelSettings.PicklistSheet);
                List<PropertyValueMatch> propertyValueMatches = ExcelDataLoader.LoadPropertiesValueMatches(excelFilePath, settingsRoot.ExcelSettings.PropertyMatchSheet);
                List<ExpressionRule> expressions = ExcelDataLoader.LoadExpressions(excelFilePath, settingsRoot.ExcelSettings.ExpressionSheet);
                List<LayerMappingItem> layerMappingItems = ExcelDataLoader.ReadLayerMappings(excelFilePath, settingsRoot.ExcelSettings.LayersMappingSheet);
                List<ComposedPropertyItem> composedPropertyItems = ExcelDataLoader.LoadComposed(excelFilePath, settingsRoot.ExcelSettings.ComposedSheet);
                List<PropertyValueMatch> exactPropertyValueMatches = ExcelDataLoader.LoadPropertiesExactValueMatches(excelFilePath, settingsRoot.ExcelSettings.PropertyExactMatchSheet);

                List<string> ifcFilePaths = System.IO.Directory.GetFiles(IfcFolderPath, "*.ifc", System.IO.SearchOption.TopDirectoryOnly).ToList();

                string reportFilePath = FileUtils.SaveFileToFolder(Environment.SpecialFolder.Desktop, ".xlsx");

                if (string.IsNullOrEmpty(reportFilePath))
                {
                    return;
                }

                IfcFileDataCollector collector = new IfcFileDataCollector(ifcFilePaths);
                List<IfcFile> ifcFiles = collector.CollectFromFiles();

                ReportWriter reportWriter = new ReportWriter(ifcFiles, reportFilePath, propertySetItems, picklistGroups, propertyValueMatches, expressions, layerMappingItems, composedPropertyItems, exactPropertyValueMatches);
                reportWriter.Create();

                System.Diagnostics.Process.Start(
                                    new ProcessStartInfo
                                    {
                                        FileName = reportFilePath,
                                        UseShellExecute = true,
                                        Verb = "open"
                                    }
                                    );
            }
            catch (Exception exception)
            {
                MessageBox.Show("Unexpected error");
            }
        }

        private void OnSelectIfcFolderCommand()
        {
            string initDirectory = IfcFolderPath ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string ifcFolderPath = FolderUtils.GetFolderPathExtendedWindow(initDirectory);

            if (string.IsNullOrEmpty(ifcFolderPath))
            {
                return;
            }

            IfcFolderPath = ifcFolderPath;
        }

        private void OnSelectExcelCommand()
        {
            string initDirectory = string.IsNullOrEmpty(ExcelFilePath) ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop) : Path.GetDirectoryName(ExcelFilePath);
            string excelFilePath = FileUtils.GetFilePath(initDirectory, "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*");

            if (string.IsNullOrEmpty(excelFilePath))
            {
                return;
            }

            ExcelFilePath = excelFilePath;
        }

        private string excelFilePath;

        public string ExcelFilePath
        {
            get { return excelFilePath; }
            set
            {
                excelFilePath = value;
                Properties.Settings.Default.ExcelFilePath = excelFilePath;
                Properties.Settings.Default.Save();
                OnPropertyChanged();
            }
        }

        private string ifcFolderPath;

        public string IfcFolderPath
        {
            get { return ifcFolderPath; }
            set
            {
                ifcFolderPath = value;
                Properties.Settings.Default.IfcFolderPath = ifcFolderPath;
                Properties.Settings.Default.Save();
                OnPropertyChanged();
            }
        }

        private string settingsFilePath;
        public string SettingsFilePath
        {
            get
            {
                return settingsFilePath;
            }

            set
            {
                settingsFilePath = value;
                Properties.Settings.Default.SettingsFilePath = settingsFilePath;
                Properties.Settings.Default.Save();
                OnPropertyChanged();
            }
        }
        public RelayCommand SelectExcelCommand { get; }
        public RelayCommand SelectIfcFolderCommand { get; }
        public RelayCommand SelectSettingsCommand { get; }
        public RelayCommand ValidateCommand { get; }
    }
}
