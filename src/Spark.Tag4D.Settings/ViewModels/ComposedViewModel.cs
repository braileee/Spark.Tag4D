using Spark.Tag4D.Core.Json;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Settings.Models;
using Spark.Tag4D.Settings.Services;
using Spark.Tag4D.Settings.ViewModels.Support;
using Prism.Commands;
using Prism.Mvvm;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Spark.Tag4D.Settings.ViewModels
{
    public class ComposedViewModel : BindableBase
    {
        public ObservableCollection<PropertyItem>
            AvailableProperties
        { get; }
            = new();

        public ObservableCollection<ComposedProperty>
            ComposedProperties
        { get; }
            = new();

        public DelegateCommand AddComposedPropertyCommand { get; }

        public DelegateCommand<ComposedProperty>
            DeleteComposedPropertyCommand
        { get; }

        public DelegateCommand<ComposedProperty>
            AddSegmentCommand
        { get; }

        public DelegateCommand SaveCommand { get; }
        public SettingsRoot Settings { get; }

        public DelegateCommand<FormulaSegment>
    InsertSegmentAfterCommand
        { get; }

        public DelegateCommand<FormulaSegment>
            RemoveSegmentCommand
        { get; }

        public ComposedViewModel(SettingsRoot settings)
        {
            Settings = settings;

            AddComposedPropertyCommand =
                new DelegateCommand(AddComposedProperty);

            DeleteComposedPropertyCommand =
                new DelegateCommand<ComposedProperty>(
                    DeleteComposedProperty);

            AddSegmentCommand =
                new DelegateCommand<ComposedProperty>(AddSegment);

            InsertSegmentAfterCommand =
    new DelegateCommand<FormulaSegment>(
        InsertSegmentAfter);

            RemoveSegmentCommand =
                new DelegateCommand<FormulaSegment>(
                    RemoveSegment);

            SaveCommand =
                new DelegateCommand(Save);

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