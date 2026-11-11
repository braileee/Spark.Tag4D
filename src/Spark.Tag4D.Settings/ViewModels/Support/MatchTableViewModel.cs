using Spark.Tag4D.Settings.Models;
using Spark.Tag4D.Core.Mvvm;

using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Spark.Tag4D.Settings.ViewModels.Support
{
    public class MatchTableViewModel : ObservableObject
    {
        private readonly Action _markDirty;

        private string _sheetPrefix = string.Empty;
        private string _targetProperty = string.Empty;

        public MatchTableViewModel(Action markDirty)
        {
            _markDirty = markDirty;

            AddRowCommand =
                new RelayCommand(AddRow);

            RemoveRowCommand =
                new RelayCommand<MatchRowViewModel>(
                    RemoveRow);

            AddSourcePropertyCommand =
                new RelayCommand(AddSourceProperty);

            RemoveSourcePropertyCommand =
                new RelayCommand<MatchPropertyViewModel>(
                    RemoveSourceProperty);
        }

        public string SheetPrefix
        {
            get => _sheetPrefix;
            set
            {
                if (SetProperty(ref _sheetPrefix, value))
                {
                    _markDirty();
                }
            }
        }

        public ObservableCollection<string>
            AvailableProperties
        { get; set; }
            = new();

        public ObservableCollection<MatchPropertyViewModel>
            SourceProperties
        { get; set; }
            = new();

        public ObservableCollection<MatchRowViewModel>
            Rows
        { get; }
            = new();

        public string TargetProperty
        {
            get => _targetProperty;
            set
            {
                if (SetProperty(ref _targetProperty, value))
                {
                    _markDirty();
                }
            }
        }

        public RelayCommand AddRowCommand { get; }

        public RelayCommand<MatchRowViewModel>
            RemoveRowCommand
        { get; }

        public RelayCommand AddSourcePropertyCommand { get; }

        public RelayCommand<MatchPropertyViewModel>
            RemoveSourcePropertyCommand
        { get; }

        private void AddSourceProperty()
        {
            SourceProperties.Add(
                new MatchPropertyViewModel(_markDirty) { AvailableProperties = AvailableProperties});

            foreach (var row in Rows)
            {
                row.Criteria.Add(
                    new MatchCriterionViewModel(_markDirty));
            }

            _markDirty();
        }

        private void RemoveSourceProperty(
            MatchPropertyViewModel property)
        {
            if (property == null)
                return;

            int propertyIndex =
                SourceProperties.IndexOf(property);

            SourceProperties.Remove(property);

            foreach (var row in Rows)
            {
                if (propertyIndex >= 0 &&
                    propertyIndex < row.Criteria.Count)
                {
                    row.Criteria.RemoveAt(
                        propertyIndex);
                }
            }

            _markDirty();
        }

        private void AddRow()
        {
            var row =
                new MatchRowViewModel(_markDirty);

            foreach (var property in SourceProperties)
            {
                row.Criteria.Add(
                    new MatchCriterionViewModel(_markDirty)
                    {
                        PropertyName =
                            property.PropertyName
                    });
            }

            Rows.Add(row);

            _markDirty();
        }

        private void RemoveRow(
            MatchRowViewModel row)
        {
            if (row == null)
                return;

            Rows.Remove(row);

            _markDirty();
        }

        public MatchTable ToModel()
        {
            return new MatchTable
            {
                SheetPrefix = SheetPrefix,

                TargetProperty = TargetProperty,

                AvailableProperties =
                    AvailableProperties.ToList(),

                SourceProperties =
                    SourceProperties
                        .Select(x => x.PropertyName)
                        .ToList(),

                Rows =
                    Rows
                        .Select(r => new MatchRow
                        {
                            TargetValue =
                                r.TargetValue,

                            Criteria =
                                r.Criteria
                                    .Select(c =>
                                        new MatchCriterion
                                        {
                                            PropertyName =
                                                c.PropertyName,

                                            Value =
                                                c.Value
                                        })
                                    .ToList()
                        })
                        .ToList()
            };
        }
    }
}