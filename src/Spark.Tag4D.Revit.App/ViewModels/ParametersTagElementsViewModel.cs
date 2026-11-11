using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Spark.Tag4D.Core.Configuration;
using Spark.Tag4D.Core.Models;
using NPOI.Util.Collections;
using Spark.Tag4D.Core.Mvvm;

using Spark.Tag4D.Revit.Core;
using Spark.Tag4D.Revit.App.EventHandlers;
using Spark.Tag4D.Revit.App.Models;
using Spark.Tag4D.Revit.App.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using static NPOI.HSSF.UserModel.HeaderFooter;

namespace Spark.Tag4D.Revit.App.ViewModels
{
    public class ParametersTagElementsViewModel : ObservableObject
    {
        private List<Element> selectedElements;

        public ParametersTagElementsViewModel(UIApplication uiapp)
        {
            Uiapp = uiapp;

            // Load settings
            SettingsRoot settingsRoot = SettingsLoader.LoadExistingOrDefault();

            if (settingsRoot == null)
            {
                MessageBox.Show($"No settings file found", "Error");
                return;
            }

            string excelFilePath = ExcelDataLoader.LoadOrPromptExcelFilePath(settingsRoot.ExcelSettings.FileLinkSettings);

            List<PropertySetItem> propertySetItems = ExcelDataLoader.LoadPropertySetItems(excelFilePath, settingsRoot.ExcelSettings.PropertiesSheet);
            List<PicklistGroup> picklistGroups = ExcelDataLoader.LoadPicklistGroups(excelFilePath, settingsRoot.ExcelSettings.PicklistSheet);
            PropertyValueMatches = ExcelDataLoader.LoadPropertiesValueMatches(excelFilePath, settingsRoot.ExcelSettings.PropertyMatchSheet);
            Expressions = ExcelDataLoader.LoadExpressions(excelFilePath, settingsRoot.ExcelSettings.ExpressionSheet);
            ComposedItems = ExcelDataLoader.LoadComposed(excelFilePath, settingsRoot.ExcelSettings.ComposedSheet);
            PropertyValueExactMatches = ExcelDataLoader.LoadPropertiesExactValueMatches(excelFilePath, settingsRoot.ExcelSettings.PropertyExactMatchSheet);

            CustomProperties = propertySetItems.SelectMany(item => item.PropertyItems).ToList();
            Dictionary<string, List<string>> propertiesWithValues = picklistGroups.ToDictionary(item => item.GroupName, item => item.Values);

            PropertiesWithValues = propertiesWithValues;

            LoadFields();

            uiapp.SelectionChanged -= SelectionChanged;
            uiapp.SelectionChanged += SelectionChanged;

            ApplyCommand = new RelayCommand(OnApplyCommand);
            UpdateValuesCommand = new RelayCommand(OnUpdateValuesCommand);

            ParametersTagElementsEventHandler = new ParametersTagElementsEventHandler();
        }

        private void OnUpdateValuesCommand()
        {
            ParametersTagElementsEventHandler.Raise(new ParametersTagElementsOptions
            {
                Elements = SelectedElements,
                ChangedFields = Fields.ToList(),
                Fields = Fields,
                Expressions = Expressions,
                ComposedItems = ComposedItems,
                PropertyValueExactMatches = PropertyValueExactMatches,
                UpdateAllValues = true,
            });
        }

        private void FieldsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (PropertyField item in e.NewItems)
                    item.PropertyChanged += ItemPropertyChanged;

            if (e.OldItems != null)
                foreach (PropertyField item in e.OldItems)
                    item.PropertyChanged -= ItemPropertyChanged;
        }

        private void ItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyField changedField = sender as PropertyField;

            PropertyField.ReloadChangedFields(changedField, Fields, PropertyValueMatches);

            if (Fields == null || Fields.Count == 0)
            {
                LoadFields();
            }
        }

        private void LoadFields()
        {
            Fields = new ObservableCollection<PropertyField>();
            var newFields = new ObservableCollection<PropertyField>(
                            CustomProperties.Select(s =>
                                new PropertyField(
                                    s.PropertyName,
                                    s.DataType,
                                    PropertiesWithValues.TryGetValue(s.PropertyName, out var values)
                                        ? values
                                        : null
                                )
                                )
                             );

            foreach (var newField in newFields)
            {
                Fields.Add(newField);
            }

            List<string> expressionParameters = Expressions.Select(item => item.TargetPropertyName).Distinct().ToList();
            ApplyReadOnly(expressionParameters);

            List<string> composedParameters = ComposedItems.Select(item => item.ComposedPropertyName).Distinct().ToList();
            ApplyReadOnly(composedParameters);

            List<string> propertyValueExactNames = PropertyValueExactMatches.Select(item => item.PropertyNameTarget).Distinct().ToList();
            ApplyReadOnly(propertyValueExactNames);
        }

        private void ApplyReadOnly(List<string> expressionParameters)
        {
            foreach (var field in Fields)
            {
                if (expressionParameters.Contains(field.Name))
                {
                    field.CanBeEdited = false;
                    field.IsReadOnly = true;
                }
            }
        }

        private void OnApplyCommand()
        {
            List<PropertyField> changedFields = Fields.Where(f => f.Changed).ToList();

            ParametersTagElementsEventHandler.Raise(new ParametersTagElementsOptions
            {
                Elements = SelectedElements,
                ChangedFields = changedFields,
                Fields = Fields,
                Expressions = Expressions,
                ComposedItems = ComposedItems,
                PropertyValueExactMatches = PropertyValueExactMatches,
                UpdateAllValues = false,
            });
        }

        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool flowControl = UpdatedFields();
            if (!flowControl)
            {
                return;
            }
        }

        private bool UpdatedFields()
        {
            if (Fields == null || Fields.Count == 0)
            {
                LoadFields();
            }

            if (Uiapp.ActiveUIDocument == null)
            {
                return false;
            }

            var selectedIds = Uiapp.ActiveUIDocument.Selection.GetElementIds();

            SelectedElementsInfo = $"Elements selected: {selectedIds.Count}";

            if (selectedIds.Count == 0)
            {
                SelectedElements = new List<Element>();
                foreach (var field in fields)
                {
                    field.Value = null;
                }
                return false;
            }

            SelectedElements = selectedIds.Select(id => Uiapp.ActiveUIDocument.Document.GetElement(id)).ToList();

            FieldsRefresh fieldsRefresh = new FieldsRefresh(Fields.ToList(), SelectedElements);
            fieldsRefresh.Start();

            return true;
        }

        public UIApplication Uiapp { get; }
        public List<PropertyValueMatch> PropertyValueMatches { get; } = new List<PropertyValueMatch>();
        public List<ExpressionRule> Expressions { get; private set; } = new List<ExpressionRule>();
        public List<ComposedPropertyItem> ComposedItems { get; } = new List<ComposedPropertyItem>();
        public List<PropertyValueMatch> PropertyValueExactMatches { get; } = new List<PropertyValueMatch>();
        public List<PropertyItem> CustomProperties { get; } = new List<PropertyItem>();
        public Dictionary<string, List<string>> PropertiesWithValues { get; } = new Dictionary<string, List<string>>();
        public ObservableCollection<PropertyField> Fields
        {
            get
            {
                return fields;
            }

            set
            {
                if (fields != null)
                {
                    Fields.CollectionChanged -= FieldsCollectionChanged;
                }

                fields = value;

                if (fields != null)
                {
                    Fields.CollectionChanged += FieldsCollectionChanged;
                }

                OnPropertyChanged();
            }
        }
        public RelayCommand ApplyCommand { get; }
        public RelayCommand UpdateValuesCommand { get; }
        public ParametersTagElementsEventHandler ParametersTagElementsEventHandler { get; }

        public List<Element> SelectedElements
        {
            get
            {
                return selectedElements;
            }

            set
            {
                selectedElements = value;
                OnPropertyChanged();
            }
        }

        private string selectedElementsInfo;
        private ObservableCollection<PropertyField> fields = new ObservableCollection<PropertyField>();

        public string SelectedElementsInfo
        {
            get { return selectedElementsInfo; }
            set
            {
                selectedElementsInfo = value;
                OnPropertyChanged();
            }
        }

    }
}
