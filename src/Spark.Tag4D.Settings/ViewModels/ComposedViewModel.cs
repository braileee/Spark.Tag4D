using Spark.Tag4D.Core.Configuration;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Settings.Services;
using Spark.Tag4D.Settings.ViewModels.Support;
using Spark.Tag4D.Core.Mvvm;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Spark.Tag4D.Settings.ViewModels
{
    public class ComposedViewModel : SettingsPageViewModel
    {
        public ObservableCollection<PropertyItem> AvailableProperties { get; }
            = new ObservableCollection<PropertyItem>();

        public ObservableCollection<ComposedProperty> ComposedProperties { get; }
            = new ObservableCollection<ComposedProperty>();

        public RelayCommand AddComposedPropertyCommand { get; }

        public RelayCommand<ComposedProperty> DeleteComposedPropertyCommand { get; }

        /// <summary>Appends a segment at the end (right) of the formula.</summary>
        public RelayCommand<ComposedProperty> AddSegmentCommand { get; }

        public RelayCommand<FormulaSegment> InsertSegmentAfterCommand { get; }

        public RelayCommand<FormulaSegment> RemoveSegmentCommand { get; }

        public SettingsRoot Settings { get; }

        public override string Title => "Composed Properties";

        public override string SheetInfo => $"Sheet: {Settings.ExcelSettings.ComposedSheet.SheetName}";

        public ComposedViewModel(SettingsRoot settings)
        {
            Settings = settings;

            AddComposedPropertyCommand = new RelayCommand(AddComposedProperty);
            DeleteComposedPropertyCommand = new RelayCommand<ComposedProperty>(DeleteComposedProperty);
            AddSegmentCommand = new RelayCommand<ComposedProperty>(AddSegment);
            InsertSegmentAfterCommand = new RelayCommand<FormulaSegment>(InsertSegmentAfter);
            RemoveSegmentCommand = new RelayCommand<FormulaSegment>(RemoveSegment);

            LoadData();

            foreach (ComposedProperty composed in ComposedProperties)
            {
                Hook(composed);
            }

            ComposedProperties.CollectionChanged += (s, e) => MarkDirty();
            IsDirty = false;
        }

        private void Hook(ComposedProperty composed)
        {
            // The name, and - through Preview - any add, remove, move or edit of a segment.
            composed.PropertyChanged += (s, e) => MarkDirty();
        }

        private void LoadData()
        {
            AvailableProperties.Clear();
            ComposedProperties.Clear();

            string filePath = ExcelDataLoader.LoadOrPromptExcelFilePath(Settings.ExcelSettings.FileLinkSettings);

            List<string> properties = ExcelDataLoader.GetPropertyNames(filePath, Settings.ExcelSettings.PropertiesSheet);

            foreach (var property in properties)
            {
                AvailableProperties.Add(new PropertyItem { PropertyName = property });
            }

            List<ComposedPropertyItem> composedItems = ExcelDataLoader.LoadComposed(filePath, Settings.ExcelSettings.ComposedSheet);

            foreach (ComposedPropertyItem item in composedItems)
            {
                var composed = new ComposedProperty
                {
                    Name = item.ComposedPropertyName,
                    Formula = item.Formula
                };

                foreach (var segment in FormulaParser.Parse(composed.Formula))
                {
                    composed.Segments.Add(segment);
                }

                ComposedProperties.Add(composed);
            }
        }

        private string FirstProperty() => AvailableProperties.FirstOrDefault()?.PropertyName ?? string.Empty;

        private void AddComposedProperty()
        {
            var composed = new ComposedProperty { Name = "NewProperty" };
            composed.Segments.Add(new FormulaSegment { SelectedProperty = FirstProperty() });

            Hook(composed);
            ComposedProperties.Add(composed);
        }

        private void DeleteComposedProperty(ComposedProperty property)
        {
            if (property != null)
            {
                ComposedProperties.Remove(property);
            }
        }

        private void AddSegment(ComposedProperty property)
        {
            if (property == null)
                return;

            property.Segments.Add(new FormulaSegment { SelectedProperty = FirstProperty(), Suffix = "." });
        }

        private void InsertSegmentAfter(FormulaSegment segment)
        {
            if (segment?.Parent == null)
                return;

            ComposedProperty property = segment.Parent;
            int index = property.Segments.IndexOf(segment);

            property.Segments.Insert(index + 1, new FormulaSegment { SelectedProperty = FirstProperty(), Suffix = "." });
        }

        private void RemoveSegment(FormulaSegment segment)
        {
            if (segment?.Parent == null)
                return;

            ComposedProperty property = segment.Parent;

            if (property.Segments.Count <= 1)
                return;

            property.Segments.Remove(segment);
        }

        /// <summary>
        /// Drag and drop: moves <paramref name="segment"/> to position <paramref name="index"/> of
        /// <paramref name="target"/> (which may be another composed property).
        /// </summary>
        public void MoveSegment(FormulaSegment segment, ComposedProperty target, int index)
        {
            ComposedProperty source = segment?.Parent;
            if (source == null || target == null)
                return;

            int oldIndex = source.Segments.IndexOf(segment);

            if (source == target)
            {
                if (index > oldIndex)
                {
                    index--;
                }

                index = System.Math.Max(0, System.Math.Min(index, target.Segments.Count - 1));
                if (index != oldIndex)
                {
                    target.Segments.Move(oldIndex, index);
                }

                return;
            }

            // A formula needs at least one segment, so the last one cannot leave its property.
            if (source.Segments.Count <= 1)
                return;

            source.Segments.Remove(segment);
            index = System.Math.Max(0, System.Math.Min(index, target.Segments.Count));
            target.Segments.Insert(index, segment);
        }

        /// <summary>Drag and drop of a property from the palette: inserts a new segment for it.</summary>
        public void InsertProperty(string propertyName, ComposedProperty target, int index)
        {
            if (target == null || string.IsNullOrEmpty(propertyName))
                return;

            index = System.Math.Max(0, System.Math.Min(index, target.Segments.Count));
            target.Segments.Insert(index, new FormulaSegment { SelectedProperty = propertyName, Suffix = "." });
        }

        public override bool Save()
        {
            foreach (var property in ComposedProperties)
            {
                property.Formula = FormulaParser.Build(property.Segments);
            }

            string filePath = ExcelDataLoader.LoadOrPromptExcelFilePath(Settings.ExcelSettings.FileLinkSettings);

            ExcelDataLoader.SaveComposed(filePath, Settings.ExcelSettings.ComposedSheet, ComposedProperties.Select(p => new ComposedPropertyItem
            {
                ComposedPropertyName = p.Name,
                Formula = p.Formula
            }).ToList());

            IsDirty = false;
            return true;
        }
    }
}
