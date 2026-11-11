using Bentley.DgnPlatformNET;
using Bentley.DgnPlatformNET.Elements;
using Spark.Tag4D.Core.Configuration;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Microstation.Extensions;
using Spark.Tag4D.Microstation.Models;
using Spark.Tag4D.Core.Mvvm;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace Spark.Tag4D.Microstation.ViewModels
{
    public class ParametersTagElementsViewModel : ObservableObject
    {
        public ParametersTagElementsViewModel(DgnFile dgnFile, SettingsRoot settingsRoot, List<PropertySetItem> propertySetItems, List<PicklistGroup> picklistGroups, List<LayerMappingItem> layerMappingItems, List<ExpressionRule> expressionItems, List<PropertyValueMatch> propertyValueMatches, List<Element> selectedElements, List<ComposedPropertyItem> composedPropertyItems, List<PropertyValueMatch> propertyValueExactMatches)
        {

            CustomProperties = propertySetItems.SelectMany(item => item.PropertyItems).ToList();
            Dictionary<string, List<string>> propertiesWithValues = picklistGroups.ToDictionary(item => item.GroupName, item => item.Values);

            PropertiesWithValues = propertiesWithValues;

            ApplyCommand = new RelayCommand(OnApplyCommand);
            UpdateSelectionCommand = new RelayCommand(OnUpdateSelectionCommand);

            UpdateValuesCommand = new RelayCommand(OnUpdateValuesCommand);

            DgnFile = dgnFile;
            SettingsRoot = settingsRoot;
            PropertySetItems = propertySetItems;
            PicklistGroups = picklistGroups;
            LayerMappingItems = layerMappingItems;
            ExpressionItems = expressionItems;
            PropertyValueMatches = propertyValueMatches;
            SelectedElements = selectedElements;
            SelectedElementsInfo = $"Selected Elements: {SelectedElements.Count}";
            ComposedItems = composedPropertyItems;
            PropertyValueExactMatches = propertyValueExactMatches;
            LoadFields();
        }

        private void OnUpdateValuesCommand()
        {
            try
            {
                ParametersUpdater parametersUpdater = new ParametersUpdater(SelectedElements, Fields.ToList(), Fields.ToList(), ExpressionItems, ComposedItems, PropertyValueExactMatches);
                parametersUpdater.UpdateComposedAndExpressionValues();
                ReportInvalidExpressions(parametersUpdater);

                OnUpdateSelectionCommand();
            }
            catch (Exception)
            {
                MessageBox.Show("Error updating values. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnUpdateSelectionCommand()
        {
            try
            {
                List<Element> elements = new List<Element>();
                for (uint i = 0; i < SelectionSetManager.NumSelected(); i++)
                {
                    DgnModelRef modelRef = null;
                    Element element = null;
                    SelectionSetManager.GetElement(i, ref element, ref modelRef);
                    elements.Add(element);
                }

                SelectedElements = elements;
                SelectedElementsInfo = $"Selected Elements: {SelectedElements.Count}";

                LoadFields();

                string variesValue = "***VARIES***";

                if (SelectedElements.Count == 0)
                {
                    foreach (PropertyField propertyField in Fields)
                    {
                        if (propertyField.EditorType == EditorType.Combo)
                        {
                            if (!propertyField.LookupValues.Contains(null))
                            {
                                propertyField.LookupValues.Add(null);
                            }
                        }

                        propertyField.Value = null;
                    }
                }

                foreach (PropertyField propertyField in Fields)
                {
                    EditorType editorType = propertyField.EditorType;
                    Element firstElement = SelectedElements.FirstOrDefault();

                    if (firstElement == null)
                    {
                        continue;
                    }

                    string propertyName = propertyField.Name;

                    object firstElementValue = firstElement.GetValue(propertyField.Name);

                    if (firstElementValue == null)
                    {
                        continue;
                    }

                    bool allValuesSame = SelectedElements.Where(element => element.GetValue(propertyField.Name) != null)
                                                         .All(element => firstElementValue.Equals(element.GetValue(propertyField.Name)));

                    if (allValuesSame)
                    {
                        propertyField.Value = firstElementValue;
                    }
                    else
                    {
                        if (propertyField.EditorType == EditorType.Combo)
                        {
                            if (!propertyField.LookupValues.Contains(variesValue))
                            {
                                propertyField.LookupValues.Add(variesValue);
                            }
                        }

                        propertyField.Value = variesValue;
                    }

                    propertyField.Changed = false;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Error updating selection. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

            if (changedField == null)
            {
                return;
            }

            PropertyField.ReloadChangedFields(changedField, Fields, PropertyValueMatches);

            //List<PropertyValueMatch> sourceMatches = PropertyValueMatches.Where(item => item.PropertyNameSource == changedField.Name).ToList();

            //if (sourceMatches.Count == 0)
            //{
            //    return;
            //}

            //List<string> targetPropertyNames = sourceMatches.Select(item => item.PropertyNameTarget).Distinct().ToList();
            //List<string> sourcePropertyNames = sourceMatches.Select(item => item.PropertyNameSource).Distinct().ToList();

            //foreach (PropertyField field in Fields)
            //{
            //    if (field.LookupValues == null || field.Name == changedField.Name)
            //    {
            //        continue;
            //    }

            //    if (field.LookupValues.Count == 0)
            //    {
            //        field.LookupValues.AddRange(field.SourceLookupValues);
            //        continue;
            //    }

            //    if (!targetPropertyNames.Contains(field.Name))
            //    {
            //        continue;
            //    }

            //    List<PropertyValueMatch> targetMatches = sourceMatches.Where(item => item.PropertyNameTarget == field.Name).ToList();
            //    List<PropertyValueMatch> sourceMatchValueItems = sourceMatches.Where(item => item.PropertyValueSource == changedField.Value?.ToString()).ToList();
            //    List<string> targetMatchesValues = sourceMatchValueItems.Select(item => item.PropertyValueTarget).ToList();

            //    List<string> lookupValues = targetMatchesValues.Count > 0 ? targetMatchesValues : field.SourceLookupValues.ToList();

            //    if (lookupValues.Count > 0)
            //    {
            //        field.LookupValues.Clear();
            //        field.LookupValues.AddRange(lookupValues);
            //    }
            //}

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

            List<string> expressions = ExpressionItems.Select(item => item.TargetPropertyName).Distinct().ToList();

            ApplyReadOnly(expressions);

            List<string> composedItems = ComposedItems.Select(item => item.ComposedPropertyName).Distinct().ToList();
            ApplyReadOnly(composedItems);

            List<string> propertyValueExactNames = PropertyValueExactMatches.Select(item => item.PropertyNameTarget).Distinct().ToList();
            ApplyReadOnly(propertyValueExactNames);
        }

        private void ApplyReadOnly(List<string> composedItems)
        {
            foreach (var field in Fields)
            {
                if (composedItems.Contains(field.Name))
                {
                    field.CanBeEdited = false;
                    field.IsReadOnly = true;
                }
            }
        }

        private static void ReportInvalidExpressions(ParametersUpdater updater)
        {
            if (updater.InvalidExpressions.Count > 0)
            {
                MessageBox.Show(
                    "Some expressions could not be evaluated:" + Environment.NewLine + string.Join(Environment.NewLine, updater.InvalidExpressions),
                    "Spark.Tag4D",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void OnApplyCommand()
        {
            try
            {
                List<PropertyField> changedFields = Fields.Where(f => f.Changed).ToList();

                ParametersUpdater parametersUpdater = new ParametersUpdater(SelectedElements, changedFields, Fields.ToList(), ExpressionItems, ComposedItems, PropertyValueExactMatches);
                parametersUpdater.Update();
                ReportInvalidExpressions(parametersUpdater);

                OnUpdateSelectionCommand();
            }
            catch (Exception)
            {
                MessageBox.Show("Error applying changes. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public List<PropertyValueMatch> PropertyValueMatches { get; } = new List<PropertyValueMatch>();
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
        public RelayCommand UpdateSelectionCommand { get; }
        public RelayCommand UpdateValuesCommand { get; }
        public RelayCommand SelectElementsCommand { get; }

        private string selectedElementsInfo;
        private ObservableCollection<PropertyField> fields = new ObservableCollection<PropertyField>();
        private List<Element> selectedElements;

        public string SelectedElementsInfo
        {
            get { return selectedElementsInfo; }
            set
            {
                selectedElementsInfo = value;
                OnPropertyChanged();
            }
        }

        public List<ComposedPropertyItem> ComposedItems { get; } = new List<ComposedPropertyItem>();
        public List<PropertyValueMatch> PropertyValueExactMatches { get; }
        public DgnFile DgnFile { get; }
        public SettingsRoot SettingsRoot { get; }
        public List<PropertySetItem> PropertySetItems { get; } = new List<PropertySetItem>();
        public List<PicklistGroup> PicklistGroups { get; } = new List<PicklistGroup>();
        public List<LayerMappingItem> LayerMappingItems { get; } = new List<LayerMappingItem>();
        public List<ExpressionRule> ExpressionItems { get; } = new List<ExpressionRule>();
    }
}
