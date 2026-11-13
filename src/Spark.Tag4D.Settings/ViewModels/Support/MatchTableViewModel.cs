using Spark.Tag4D.Settings.Models;
using Spark.Tag4D.Core.Mvvm;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace Spark.Tag4D.Settings.ViewModels.Support
{
    /// <summary>One "&lt;name&gt; Match" sheet: source property columns, one target property, and the rows that map them.</summary>
    public class MatchTableViewModel : ObservableObject
    {
        private readonly Action _markDirty;

        private string _sheetPrefix = string.Empty;
        private string _targetProperty = string.Empty;
        private string _filterText = string.Empty;
        private Func<string, IReadOnlyList<string>> _valuesFor;

        public MatchTableViewModel(Action markDirty)
        {
            _markDirty = markDirty;

            AddRowCommand = new RelayCommand(AddRow);
            RemoveRowCommand = new RelayCommand<MatchRowViewModel>(RemoveRow);
            AddSourcePropertyCommand = new RelayCommand(AddSourceProperty);
            RemoveSourcePropertyCommand = new RelayCommand<MatchPropertyViewModel>(RemoveSourceProperty);
            

            RowsView = CollectionViewSource.GetDefaultView(Rows);
            RowsView.Filter = MatchesFilter;
            Rows.CollectionChanged += (s, e) => OnPropertyChanged(nameof(RowCountText));
        }

        public string SheetPrefix
        {
            get => _sheetPrefix;
            set
            {
                if (SetProperty(ref _sheetPrefix, value))
                {
                    OnPropertyChanged(nameof(WorksheetName));
                    _markDirty();
                }
            }
        }

        /// <summary>Name of the Excel sheet this table is stored in.</summary>
        public string WorksheetName => $"{SheetPrefix} Match";

        public ObservableCollection<string> AvailableProperties { get; set; } = new();

        public ObservableCollection<MatchPropertyViewModel> SourceProperties { get; set; } = new();

        public ObservableCollection<MatchRowViewModel> Rows { get; } = new();

        public string TargetProperty
        {
            get => _targetProperty;
            set
            {
                if (SetProperty(ref _targetProperty, value))
                {
                    foreach (MatchRowViewModel row in Rows)
                    {
                        row.RefreshTarget();
                    }

                    _markDirty();
                }
            }
        }

        /// <summary>Picklist values of the target property.</summary>
        public IReadOnlyList<string> TargetValues => _valuesFor?.Invoke(TargetProperty) ?? Array.Empty<string>();

        // ----- Filter ----------------------------------------------------------------------------------------------

        /// <summary>The rows shown, narrowed down by <see cref="FilterText"/>.</summary>
        public ICollectionView RowsView { get; }

        public string FilterText
        {
            get => _filterText;
            set
            {
                if (SetProperty(ref _filterText, value ?? string.Empty))
                {
                    RowsView.Refresh();
                    OnPropertyChanged(nameof(RowCountText));
                }
            }
        }

        /// <summary>"12 rows", or "3 of 12 rows" while filtering.</summary>
        public string RowCountText
        {
            get
            {
                int total = Rows.Count;
                string noun = total == 1 ? "row" : "rows";

                if (string.IsNullOrWhiteSpace(FilterText))
                {
                    return $"{total} {noun}";
                }

                return $"{RowsView.Cast<object>().Count()} of {total} {noun}";
            }
        }

        private bool MatchesFilter(object item)
        {
            return string.IsNullOrWhiteSpace(_filterText) || (item is MatchRowViewModel row && (row.IsEmpty || row.Contains(_filterText.Trim())));
        }

        // ----- Commands --------------------------------------------------------------------------------------------

        public RelayCommand AddRowCommand { get; }

        public RelayCommand<MatchRowViewModel> RemoveRowCommand { get; }

        public RelayCommand AddSourcePropertyCommand { get; }

        public RelayCommand<MatchPropertyViewModel> RemoveSourcePropertyCommand { get; }


        /// <summary>
        /// Connects every cell to the picklist of its property. Call after the table was built or its structure changed.
        /// <paramref name="valuesFor"/> returns the picklist values of a property name.
        /// </summary>
        public void Attach(Func<string, IReadOnlyList<string>> valuesFor)
        {
            _valuesFor = valuesFor;
            AttachCells();
        }

        private void AttachCells()
        {
            foreach (MatchRowViewModel row in Rows)
            {
                row.Attach(this);

                for (int index = 0; index < row.Criteria.Count && index < SourceProperties.Count; index++)
                {
                    row.Criteria[index].Attach(SourceProperties[index], _valuesFor);
                }
            }

            OnPropertyChanged(nameof(TargetValues));
        }

        private void AddSourceProperty()
        {
            SourceProperties.Add(new MatchPropertyViewModel(_markDirty) { AvailableProperties = AvailableProperties });

            foreach (MatchRowViewModel row in Rows)
            {
                row.Criteria.Add(new MatchCriterionViewModel(_markDirty));
            }

            AttachCells();
            _markDirty();
        }

        private void RemoveSourceProperty(MatchPropertyViewModel property)
        {
            if (property == null)
            {
                return;
            }

            int propertyIndex = SourceProperties.IndexOf(property);

            SourceProperties.Remove(property);

            foreach (MatchRowViewModel row in Rows)
            {
                if (propertyIndex >= 0 && propertyIndex < row.Criteria.Count)
                {
                    row.Criteria.RemoveAt(propertyIndex);
                }
            }

            AttachCells();
            _markDirty();
        }

        private void AddRow()
        {
            var row = new MatchRowViewModel(_markDirty);

            foreach (MatchPropertyViewModel property in SourceProperties)
            {
                row.Criteria.Add(new MatchCriterionViewModel(_markDirty) { PropertyName = property.PropertyName });
            }

            Rows.Add(row);
            AttachCells();

            _markDirty();
        }

        private void RemoveRow(MatchRowViewModel row)
        {
            if (row == null)
            {
                return;
            }

            Rows.Remove(row);

            _markDirty();
        }

        public MatchTable ToModel()
        {
            return new MatchTable
            {
                SheetPrefix = SheetPrefix,
                TargetProperty = TargetProperty,
                AvailableProperties = AvailableProperties.ToList(),
                SourceProperties = SourceProperties.Select(x => x.PropertyName).ToList(),
                Rows = Rows
                    .Select(r => new MatchRow
                    {
                        TargetValue = r.TargetValue,
                        Criteria = r.Criteria
                            .Select(c => new MatchCriterion { PropertyName = c.PropertyName, Value = c.Value })
                            .ToList(),
                    })
                    .ToList(),
            };
        }
    }
}
