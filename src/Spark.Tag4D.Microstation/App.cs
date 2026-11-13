using Bentley.DgnPlatformNET;
using Bentley.DgnPlatformNET.Elements;
using Bentley.MstnPlatformNET;
using Spark.Tag4D.Core;
using Spark.Tag4D.Core.Configuration;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Core.Utils;
using Spark.Tag4D.Microstation.Models;
using Spark.Tag4D.Microstation.ViewModels;
using Spark.Tag4D.Microstation.Views;
using Spark.Tag4D.Settings.ViewModels;
using Spark.Tag4D.Settings.Views;
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
        public List<ExpressionRule> ExpressionItems { get; private set; } = new List<ExpressionRule>();
        public List<PropertyValueMatch> PropertyValueMatches { get; private set; } = new List<PropertyValueMatch>();
        public List<ComposedPropertyItem> ComposedItems { get; private set; } = new List<ComposedPropertyItem>();
        public List<PropertyValueMatch> PropertyValueExactMatches { get; private set; } = new List<PropertyValueMatch>();
        public List<PickList> PickLists { get; private set; } = new List<PickList>();

        static App()
        {
            // WPF resolves pack://application:,,,/Spark.Tag4D.UI;component/... by assembly name; make sure the sibling
            // assemblies in the add-in folder can be found even if MS_ADDIN_DEPENDENCYPATH does not cover them.
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string name = new System.Reflection.AssemblyName(args.Name).Name;
                if (!name.StartsWith("Spark.Tag4D", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                string path = Path.Combine(AssemblyUtils.GetFolder(typeof(App)), name + ".dll");
                return File.Exists(path) ? System.Reflection.Assembly.LoadFrom(path) : null;
            };
        }

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
                string libraryName = Constants.ProductName;
                ItemTypeCreator itemTypeCreator = new ItemTypeCreator(dgnFile, libraryName, PropertySetItems, PickLists);
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
            catch (TagDataException exception)
            {
                MessageBox.Show(exception.Message, "Spark.Tag4D", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Error loading parameters: {exception.Message}", "Spark.Tag4D", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDgnData()
        {
            TagData data = TagData.Load();

            SettingsRoot = data.Settings;
            PropertySetItems = data.PropertySets;
            PicklistGroups = data.Picklists;
            LayerMappingItems = data.LayerMappings;
            ExpressionItems = data.Expressions;
            PropertyValueMatches = data.Matches;
            ComposedItems = data.Composed;
            PropertyValueExactMatches = data.ExactMatches;
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

                TagView parametersTagElementsView = new TagView();
                parametersTagElementsView.DataContext = new TagViewModel(dgnFile, SettingsRoot, PropertySetItems, PicklistGroups, LayerMappingItems, ExpressionItems, PropertyValueMatches, elements, ComposedItems, PropertyValueExactMatches);
                parametersTagElementsView.Show();

            }
            catch (TagDataException exception)
            {
                MessageBox.Show(exception.Message, "Spark.Tag4D", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Error tagging elements: {exception.Message}", "Spark.Tag4D", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ShowSettings()
        {
            try
            {
                new SettingsView { DataContext = new SettingsViewModel() }.Show();
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Error showing settings: {exception.Message}", "Spark.Tag4D", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}