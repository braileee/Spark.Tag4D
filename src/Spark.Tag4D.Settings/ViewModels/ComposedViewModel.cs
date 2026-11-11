using Spark.Tag4D.Core.Configuration;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Settings.Models;
using Spark.Tag4D.Settings.Services;
using Spark.Tag4D.Settings.ViewModels.Support;
using Spark.Tag4D.Core.Mvvm;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Spark.Tag4D.Settings.ViewModels
{
    public class ComposedViewModel : ObservableObject
    {
        public ObservableCollection<PropertyItem>
            AvailableProperties
        { get; }
            = new();

        public ObservableCollection<ComposedProperty>
            ComposedProperties
        { get; }
            = new();

        public RelayCommand AddComposedPropertyCommand { get; }

        public RelayCommand<ComposedProperty>
            DeleteComposedPropertyCommand
        { get; }

        public RelayCommand<ComposedProperty>
            AddSegmentCommand
        { get; }

        public RelayCommand SaveCommand { get; }
        public SettingsRoot Settings { get; }

        public RelayCommand<FormulaSegment>
    InsertSegmentAfterCommand
        { get; }

        public RelayCommand<FormulaSegment>
            RemoveSegmentCommand
        { get; }

        public ComposedViewModel(SettingsRoot settings)
        {
            Settings = settings;

            AddComposedPropertyCommand =
                new RelayCommand(AddComposedProperty);

            DeleteComposedPropertyCommand =
                new RelayCommand<ComposedProperty>(
                    DeleteComposedProperty);

            AddSegmentCommand =
                new RelayCommand<ComposedProperty>(AddSegment);

            InsertSegmentAfterCommand =
    new RelayCommand<FormulaSegment>(
        InsertSegmentAfter);

            RemoveSegmentCommand =
                new RelayCommand<FormulaSegment>(
                    RemoveSegment);

            SaveCommand =
                new RelayCommand(Save);

            LoadData();
        }

        private void LoadData()
        {
            AvailableProperties.Clear();
            ComposedProperties.Clear();

            string filePath = ExcelDataLoader.LoadOrPromptExcelFilePath(Settings.ExcelSettings.FileLinkSettings);

            System.Collections.Generic.List<string> properties = ExcelDataLoader.GetPropertyNames(filePath, Settings.ExcelSettings.PropertiesSheet);

            foreach (var property in properties)
            {
                AvailableProperties.Add(new PropertyItem { PropertyName = property});
            }

            List<ComposedPropertyItem> composedItems = ExcelDataLoader.LoadComposed(filePath, Settings.ExcelSettings.ComposedSheet);

            List<ComposedProperty> composedProperties = composedItems.Select(item => new ComposedProperty
            {
                Name = item.ComposedPropertyName,
                Formula = item.Formula
            }).ToList();

            foreach (var composed in composedProperties)
            {
                foreach (var segment
                         in FormulaParser.Parse(composed.Formula))
                {
                    segment.Parent = composed;
                    composed.Segments.Add(segment);
                }

                ComposedProperties.Add(composed);
            }
        }

        private void AddComposedProperty()
        {
            var composed = new ComposedProperty
            {
                Name = "NewProperty"
            };

            composed.Segments.Add(
                new FormulaSegment
                {
                    SelectedProperty =
                        AvailableProperties.FirstOrDefault()?.PropertyName ?? "",
                    Parent = composed
                });

            ComposedProperties.Add(composed);
        }

        private void DeleteComposedProperty(
            ComposedProperty property)
        {
            if (property != null)
            {
                ComposedProperties.Remove(property);
            }
        }

        private void AddSegment(
     ComposedProperty property)
        {
            if (property == null)
                return;

            property.Segments.Add(
                new FormulaSegment
                {
                    Parent = property,
                    SelectedProperty =
                        AvailableProperties.FirstOrDefault()?.PropertyName ?? "",
                    Suffix = "."
                });
        }

        private void InsertSegmentAfter(
    FormulaSegment segment)
        {
            if (segment == null)
                return;

            var property = segment.Parent;

            int index =
                property.Segments.IndexOf(segment);

            property.Segments.Insert(
                index + 1,
                new FormulaSegment
                {
                    Parent = property,
                    SelectedProperty =
                        AvailableProperties.FirstOrDefault()?.PropertyName ?? "",
                    Suffix = "."
                });
        }

        private void RemoveSegment(
    FormulaSegment segment)
        {
            if (segment == null)
                return;

            var property = segment.Parent;

            if (property.Segments.Count <= 1)
                return;

            property.Segments.Remove(segment);
        }


        private void Save()
        {
            foreach (var property in ComposedProperties)
            {
                property.Formula =
                    FormulaParser.Build(property.Segments);
            }

            string filePath = ExcelDataLoader.LoadOrPromptExcelFilePath(Settings.ExcelSettings.FileLinkSettings);

            ExcelDataLoader.SaveComposed(filePath, Settings.ExcelSettings.ComposedSheet, ComposedProperties.Select(p => new ComposedPropertyItem
            {
                ComposedPropertyName = p.Name,
                Formula = p.Formula
            }).ToList());
        }
    }
}