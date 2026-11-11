using Bentley.DgnPlatformNET;
using Bentley.DgnPlatformNET.Elements;
using Bentley.MstnPlatformNET;
using Spark.Tag4D.Core;
using Spark.Tag4D.Core.Json;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Core.Utils;
using Spark.Tag4D.Microstation.Models;
using Spark.Tag4D.Microstation.ViewModels;
using Spark.Tag4D.Microstation.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace Spark.Tag4D.Microstation
{
    [Bentley.MstnPlatformNET.AddInAttribute(MdlTaskID = "Spark.Tag4D.Microstation")]

    public class App : Bentley.MstnPlatformNET.AddIn
    {
        private static App s_instance;
        private DgnModel m_ActiveModel;
        private DgnFile m_ActiveDgnFile;

        internal static App Instance
        {
            get
            {
                return s_instance;
            }
        }

        public static Log Log { get; private set; }
        public SettingsRoot SettingsRoot { get; private set; }
        public List<PropertySetItem> PropertySetItems { get; private set; } = new List<PropertySetItem>();
        public List<PicklistGroup> PicklistGroups { get; private set; } = new List<PicklistGroup>();
        public List<LayerMappingItem> LayerMappingItems { get; private set; } = new List<LayerMappingItem>();
        public List<ExpressionItem> ExpressionItems { get; private set; } = new List<ExpressionItem>();
        public List<PropertyValueMatch> PropertyValueMatches { get; private set; } = new List<PropertyValueMatch>();
        public List<ComposedPropertyItem> ComposedItems { get; private set; } = new List<ComposedPropertyItem>();
        public List<PropertyValueMatch> PropertyValueExactMatches { get; private set; } = new List<PropertyValueMatch>();
        public List<PickList> PickLists { get; private set; } = new List<PickList>();

        public App(System.IntPtr mdlDesc) : base(mdlDesc)
        {
            s_instance = this;
        }

        protected override int Run(string[] commandLine)
        {
            s_instance = this;
            m_ActiveModel = Bentley.MstnPlatformNET.Session.Instance.GetActiveDgnModel();
            m_ActiveDgnFile = Bentley.MstnPlatformNET.Session.Instance.GetActiveDgnFile();

            return 0;
        }

        public void LoadParameters()
        {
            try
            {
                DgnFile dgnFile = Session.Instance.GetActiveDgnFile();

                LoadDgnData();

                LoadPickLists(dgnFile);

                // Generate item types aka custom properties
                string libraryName = nameof(Spark.Tag4D.Microstation);
                ItemTypeCreator itemTypeCreator = new ItemTypeCreator(dgnFile, nameof(Spark.Tag4D.Microstation), PropertySetItems, PickLists);
                List<ItemType> itemTypes = itemTypeCreator.Create();

                DgnModel model = Session.Instance.GetActiveDgnModel();
                ModelElementsCollection elements = model.GetGraphicElements();

                //Attach item types to elements
                ItemTypeAttacher itemTypeAttacher = new ItemTypeAttacher(elements);
                itemTypeAttacher.Attach(libraryName, itemTypes);

                // Map properties
                LayerPropertyMapper layerPropertyMapper = new LayerPropertyMapper(dgnFile, PropertySetItems, LayerMappingItems, elements);
                layerPropertyMapper.Map();
            }
            catch (Exception)
            {
                MessageBox.Show($"Error loading parameters. Please check the settings and excel files.", "Error");
            }
        }

        private void LoadDgnData()
        {
            try
            {
                string assemblyFolder = AssemblyUtils.GetFolder(typeof(App));
                string defaultSettingsPath = System.IO.Path.Combine(assemblyFolder, Constants.FilesFolder, Constants.Tag4DFolder, Constants.JsonSettingsFileName);
                string currentSettingsPath = Properties.Settings.Default.SettingsFilePath;

                if (string.IsNullOrEmpty(currentSettingsPath) || !File.Exists(currentSettingsPath))
                {
                    currentSettingsPath = defaultSettingsPath;
                }

                if (!File.Exists(currentSettingsPath))
                {
                    MessageBox.Show($"Can't load settings file from {defaultSettingsPath}", "Error");
                }

                SettingsRoot = SettingsLoader.Load(currentSettingsPath);
                Properties.Settings.Default.SettingsFilePath = currentSettingsPath;
                Properties.Settings.Default.Save();

                string excelFilePath = ExcelDataLoader.LoadOrPromptExcelFilePath(SettingsRoot.ExcelSettings.FileLinkSettings);

                if (string.IsNullOrEmpty(excelFilePath))
                {
                    MessageBox.Show($"No excel file selected", "Error");
                    return;
                }

                PropertySetItems = ExcelDataLoader.LoadPropertySetItems(excelFilePath, SettingsRoot.ExcelSettings.PropertiesSheet);
                PicklistGroups = ExcelDataLoader.LoadPicklistGroups(excelFilePath, SettingsRoot.ExcelSettings.PicklistSheet);
                LayerMappingItems = ExcelDataLoader.ReadLayerMappings(excelFilePath, SettingsRoot.ExcelSettings.LayersMappingSheet);
                ExpressionItems = ExcelDataLoader.LoadExpressions(excelFilePath, SettingsRoot.ExcelSettings.ExpressionSheet);
                PropertyValueMatches = ExcelDataLoader.LoadPropertiesValueMatches(excelFilePath, SettingsRoot.ExcelSettings.PropertyMatchSheet);
                ComposedItems = ExcelDataLoader.LoadComposed(excelFilePath, SettingsRoot.ExcelSettings.ComposedSheet);
                PropertyValueExactMatches = ExcelDataLoader.LoadPropertiesExactValueMatches(excelFilePath, SettingsRoot.ExcelSettings.PropertyExactMatchSheet);

                var dgnFile = Session.Instance.GetActiveDgnFile();
            }
            catch (Exception)
            {
                MessageBox.Show($"Error loading data from dgn file and excel. Please check the settings and excel files.", "Error");
            }
        }

        private void LoadPickLists(DgnFile dgnFile)
        {
            //Generate picklists in dgn file
            PicklistCreator picklistCreator = new PicklistCreator(dgnFile, PicklistGroups);
            PickLists = picklistCreator.Create();
        }

        public void TagElements()
        {
            try
            {
                DgnFile dgnFile = Session.Instance.GetActiveDgnFile();
                DgnModel dgnModel = Session.Instance.GetActiveDgnModel();

                List<Element> elements = new List<Element>();
                for (uint i = 0; i < SelectionSetManager.NumSelected(); i++)
                {
                    DgnModelRef modelRef = null;
                    Element element = null;
                    SelectionSetManager.GetElement(i, ref element, ref modelRef);
                    elements.Add(element);
                }

                LoadDgnData();

                ParametersTagElementsView parametersTagElementsView = new ParametersTagElementsView();
                parametersTagElementsView.DataContext = new ParametersTagElementsViewModel(dgnFile, SettingsRoot, PropertySetItems, PicklistGroups, LayerMappingItems, ExpressionItems, PropertyValueMatches, elements, ComposedItems, PropertyValueExactMatches);
                parametersTagElementsView.Show();

            }
            catch (Exception)
            {
                MessageBox.Show($"Error tagging elements. Please check the settings and excel files.", "Error");
            }
        }

        public void ShowSettings()
        {
            try
            {
                LoadDgnData();
                ParametersSettingsView settingsView = new ParametersSettingsView();
                settingsView.DataContext = new ParametersSettingsViewModel();
                settingsView.Show();
            }
            catch (Exception)
            {
                MessageBox.Show($"Error showing settings. Please check the settings and excel files.", "Error");
            }
        }
    }
}