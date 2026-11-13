using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Core.Mvvm;

using Spark.Tag4D.Revit.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Xml.Linq;

namespace Spark.Tag4D.Revit.App.Tag
{
    public class TagViewModel : ObservableObject
    {
        private List<Element> selectedElements;

        public TagViewModel(UIApplication uiapp)
        {
            Uiapp = uiapp;

            FixAutomaticValuesCommand = new RelayCommand(OnFixAutomaticValues);
            ApplyCommand = new RelayCommand(ApplyPendingChanges, () => HasPendingChanges);
            TagEventHandler = new TagEventHandler();

            uiapp.SelectionChanged -= SelectionChanged;
            uiapp.SelectionChanged += SelectionChanged;

            // The workbook is read once here, and again only when the settings were changed.
            TagDataEvents.Changed += Reload;
            Reload();
        }

        /// <summary>Re-reads the settings and the workbook.</summary>
        public void Reload()
        {
            TagData data;

            try
            {
                data = TagData.Load();
            }
            catch (TagDataException exception)
            {
                MessageBox.Show(exception.Message, "Spark.Tag4D", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            PropertyValueMatches = data.Matches;
            Expressions = data.Expressions;
            ComposedItems = data.Composed;
            PropertyValueExactMatches = data.ExactMatches;
            CustomProperties = data.Properties;
            PropertiesWithValues = data.PicklistsByProperty;

            LoadFields();

            try
            {
                UpdatedFields();
            }
            catch (Exception)
            {
                // outside of a Revit API context the selection cannot be read; the next selection change fills the values
            }
        }

        /// <summary>Recalculates the composed properties, expressions and exact matches of the selected elements.</summary>
        private void OnFixAutomaticValues()
        {
            TagEventHandler.Raise(new TagOptions
            {
                Elements = SelectedElements,
                ChangedFields = Fields.ToList(),
                Fields = Fields,
                Expressions = Expressions,
                ComposedItems = ComposedItems,
                PropertyValueExactMatches = PropertyValueExactMatches,
                UpdateAllValues = true,
                Syncing = SetSyncing,
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

            if (e.PropertyName == nameof(PropertyField.Value) && !isSyncing && changedField != null)
            {
                changedField.Notice = null; // the user edited it, an earlier "cleared" message no longer applies
            }

            if (e.PropertyName != nameof(PropertyField.Value))
            {
                return; // Changed, IsReadOnly ... do not affect the match tables
            }

            PropertyField.ReloadChangedFields(changedField, Fields, PropertyValueMatches, clearInvalidValues: !isSyncing);

            if (Fields == null || Fields.Count == 0)
            {
                LoadFields();
                return;
            }

            // A value picked or entered by the user is written to the selected elements right away (or collected until
            // Apply is clicked). Values set by the pane itself (refresh on selection change, calculated values) are not edits.
            if (!isSyncing && changedField != null
                && !changedField.IsReadOnly && SelectedElements != null && SelectedElements.Count > 0
                && !Equals(changedField.Value, PropertyField.VariesValue))
            {
                if (ApplyAutomatically)
                {
                    ApplyChangedFields();
                }
                else
                {
                    pending.Add(changedField);
                    RaisePendingChanged();
                }
            }
        }

        // ----- Options: apply automatically or on request ----------------------------------------------------------

        private readonly HashSet<PropertyField> pending = new HashSet<PropertyField>();
        private bool applyAutomatically = true;
        private bool suppressSelectionEvents;

        /// <summary>On (default): every picked or entered value is written to the selected elements at once. Off: values are collected and written by <see cref="ApplyCommand"/>.</summary>
        public bool ApplyAutomatically
        {
            get => applyAutomatically;
            set
            {
                if (!SetProperty(ref applyAutomatically, value))
                {
                    return;
                }

                // Switching back to automatic must not leave collected edits behind.
                if (value && pending.Count > 0)
                {
                    ApplyPendingChanges();
                }

                RaisePendingChanged();
            }
        }

        public RelayCommand ApplyCommand { get; }

        public bool HasPendingChanges => pending.Count > 0;

        /// <summary>Status line under the buttons.</summary>
        public string ApplyInfo
        {
            get
            {
                if (ApplyAutomatically)
                {
                    return "Changes are applied to the selected elements immediately.";
                }

                return pending.Count == 0
                    ? "Changes are applied when you click Apply."
                    : $"{pending.Count} changed {(pending.Count == 1 ? "property is" : "properties are")} not applied yet.";
            }
        }

        private void RaisePendingChanged()
        {
            OnPropertyChanged(nameof(HasPendingChanges));
            OnPropertyChanged(nameof(ApplyInfo));
            ApplyCommand.RaiseCanExecuteChanged();
        }

        private void ApplyPendingChanges()
        {
            if (pending.Count == 0 || SelectedElements == null || SelectedElements.Count == 0)
            {
                pending.Clear();
                RaisePendingChanged();
                return;
            }

            TagEventHandler.Raise(CreateOptions(SelectedElements, pending.ToList(), updateAll: false));
            pending.Clear();
            RaisePendingChanged();
        }

        private TagOptions CreateOptions(List<Element> elements, List<PropertyField> changedFields, bool updateAll)
        {
            return new TagOptions
            {
                Elements = elements,
                ChangedFields = changedFields,
                Fields = Fields,
                Expressions = Expressions,
                ComposedItems = ComposedItems,
                PropertyValueExactMatches = PropertyValueExactMatches,
                UpdateAllValues = updateAll,
                Syncing = SetSyncing,
            };
        }

        /// <summary>
        /// The selection already changed when this is called, and <see cref="SelectedElements"/> still holds the previous
        /// one: ask what to do with the values that were not applied to it. Returns false to keep the old selection.
        /// </summary>
        private bool ResolvePendingChanges()
        {
            MessageBoxResult answer = MessageBox.Show(
                $"{pending.Count} changed {(pending.Count == 1 ? "property was" : "properties were")} not applied to the {SelectedElements.Count} previously selected element(s).\n\n" +
                "Yes - apply them now\nNo - discard them\nCancel - go back to the previous selection",
                "Tag - unapplied changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            switch (answer)
            {
                case MessageBoxResult.Yes:
                    // Runs here and now, inside Revit's event: the pane is about to show the new selection.
                    TagEventHandler.Execute(Uiapp, CreateOptions(SelectedElements, pending.ToList(), updateAll: false));
                    break;

                case MessageBoxResult.Cancel:
                    RestorePreviousSelection();
                    return false;
            }

            pending.Clear();
            RaisePendingChanged();
            return true;
        }

        private void RestorePreviousSelection()
        {
            suppressSelectionEvents = true;
            try
            {
                Uiapp.ActiveUIDocument.Selection.SetElementIds(SelectedElements.Select(element => element.Id).ToList());
            }
            catch (Exception)
            {
                // the elements may be gone; the new selection then simply stays
                pending.Clear();
                RaisePendingChanged();
            }
            finally
            {
                suppressSelectionEvents = false;
            }
        }

        private bool isSyncing;

        private void SetSyncing(bool value) => isSyncing = value;

        private void LoadFields()
        {
            Fields = PropertyFieldFactory.Create(CustomProperties, PropertiesWithValues, Expressions, ComposedItems, PropertyValueExactMatches);
            pending.Clear(); // the fields were replaced
            RaisePendingChanged();
        }

        private void ApplyChangedFields()
        {
            TagEventHandler.Raise(CreateOptions(SelectedElements, Fields.Where(f => f.Changed).ToList(), updateAll: false));
        }

        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (suppressSelectionEvents)
            {
                return;
            }

            if (!ApplyAutomatically && pending.Count > 0 && SelectedElements != null && SelectedElements.Count > 0 && !ResolvePendingChanges())
            {
                return;
            }

            UpdatedFields();
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
                    field.Value = null; // SelectedElements is empty here, so this is never applied
                }
                return false;
            }

            SelectedElements = selectedIds.Select(id => Uiapp.ActiveUIDocument.Document.GetElement(id)).ToList();

            foreach (PropertyField field in Fields)
            {
                field.Notice = null; // a new selection: earlier "cleared" messages are about other elements
            }

            FieldsRefresh fieldsRefresh = new FieldsRefresh(Fields.ToList(), SelectedElements);
            isSyncing = true;
            try
            {
                fieldsRefresh.Start();
            }
            finally
            {
                isSyncing = false;
            }

            return true;
        }

        public UIApplication Uiapp { get; }
        public List<PropertyValueMatch> PropertyValueMatches { get; private set; } = new List<PropertyValueMatch>();
        public List<ExpressionRule> Expressions { get; private set; } = new List<ExpressionRule>();
        public List<ComposedPropertyItem> ComposedItems { get; private set; } = new List<ComposedPropertyItem>();
        public List<PropertyValueMatch> PropertyValueExactMatches { get; private set; } = new List<PropertyValueMatch>();
        public List<PropertyItem> CustomProperties { get; private set; } = new List<PropertyItem>();
        public Dictionary<string, List<string>> PropertiesWithValues { get; private set; } = new Dictionary<string, List<string>>();
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
                    foreach (PropertyField old in fields) old.PropertyChanged -= ItemPropertyChanged;
                }

                fields = value;

                if (fields != null)
                {
                    Fields.CollectionChanged += FieldsCollectionChanged;
                    foreach (PropertyField added in fields) added.PropertyChanged += ItemPropertyChanged;
                }

                FieldsView = fields == null ? null : CollectionViewSource.GetDefaultView(fields);
                if (FieldsView != null)
                {
                    FieldsView.Filter = MatchesSearch;
                }

                OnPropertyChanged(nameof(FieldsView));
                OnPropertyChanged();
            }
        }
        /// <summary>The parameters shown in the pane, narrowed down by <see cref="SearchText"/>.</summary>
        public ICollectionView FieldsView { get; private set; }

        public string SearchText
        {
            get { return searchText; }
            set
            {
                if (SetProperty(ref searchText, value))
                {
                    FieldsView?.Refresh();
                }
            }
        }

        private bool MatchesSearch(object item)
        {
            var field = item as PropertyField;
            return field != null
                && (string.IsNullOrWhiteSpace(searchText) || field.Name.IndexOf(searchText.Trim(), StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public RelayCommand FixAutomaticValuesCommand { get; }
        public TagEventHandler TagEventHandler { get; }

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

        private string searchText = string.Empty;
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
