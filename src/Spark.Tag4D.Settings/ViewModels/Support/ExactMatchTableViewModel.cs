using Spark.Tag4D.Settings.Models;
using Spark.Tag4D.Core.Mvvm;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Data;

namespace Spark.Tag4D.Settings.ViewModels.Support
{
    public class ExactMatchTableViewModel : ObservableObject
    {
        private readonly Action _markDirty;

        private string _name;

        public ExactMatchTableViewModel(
    ExactMatchTable table,
    Action markDirty)
        {
            _sheetPrefix = table.SheetPrefix;
            _markDirty = markDirty;

            AddRowCommand =
                new RelayCommand(AddRow);

            RemoveRowCommand =
                new RelayCommand<ExactMatchRowViewModel>(
                    RemoveRow);

            AvailableColumnNames = new ObservableCollection<string>(table.AvailableColumnNames);

            RowsView = CollectionViewSource.GetDefaultView(Rows);
            RowsView.Filter = MatchesFilter;
            Rows.CollectionChanged += (s, e) => OnPropertyChanged(nameof(RowCountText));
        }

        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                {
                    _markDirty();
                }
            }
        }

        public ObservableCollection<ExactMatchRowViewModel> Rows { get; }
            = new();


        // ----- Search ----------------------------------------------------------------------------------------------

        private string _filterText = string.Empty;

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

        /// <summary>"12 rows", or "3 of 12 rows" while searching.</summary>
        public string RowCountText
        {
            get
            {
                string noun = Rows.Count == 1 ? "row" : "rows";
                return string.IsNullOrWhiteSpace(FilterText)
                    ? $"{Rows.Count} {noun}"
                    : $"{RowsView.Cast<object>().Count()} of {Rows.Count} {noun}";
            }
        }

        // A row without any value (just added) is never hidden, so it can be filled in.
        private bool MatchesFilter(object item)
        {
            if (string.IsNullOrWhiteSpace(_filterText) || !(item is ExactMatchRowViewModel row))
            {
                return true;
            }

            string text = _filterText.Trim();
            return (string.IsNullOrEmpty(row.Source) && string.IsNullOrEmpty(row.Target))
                || (row.Source ?? string.Empty).IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0
                || (row.Target ?? string.Empty).IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        public RelayCommand AddRowCommand { get; }

        public RelayCommand<ExactMatchRowViewModel>
            RemoveRowCommand
        { get; }

        private void AddRow()
        {
            Rows.Add(
                new ExactMatchRowViewModel(_markDirty));

            _markDirty();
        }

        private void RemoveRow(
            ExactMatchRowViewModel row)
        {
            if (row == null)
                return;

            Rows.Remove(row);

            _markDirty();
        }

        public bool HasDuplicateSources()
        {
            return Rows
                .Where(r => !string.IsNullOrWhiteSpace(r.Source))
                .GroupBy(r => r.Source)
                .Any(g => g.Count() > 1);
        }

        public ExactMatchTable ToModel()
        {
            return new ExactMatchTable
            {
                SheetPrefix = SheetPrefix,
                SourceColumnHeader = SourceColumnHeader,
                TargetColumnHeader = TargetColumnHeader,
                Rows = Rows
                    .Select(r => new ExactMatchRow
                    {
                        Source = r.Source,
                        Target = r.Target,
                    })
                    .ToList(),
                AvailableColumnNames = AvailableColumnNames.ToList()
            };
        }

        public ObservableCollection<string> AvailableColumnNames { get; set; } = new ObservableCollection<string>();

        private string _sourceColumnHeader;
        public string SourceColumnHeader
        {
            get => _sourceColumnHeader;
            set
            {
                if (SetProperty(ref _sourceColumnHeader, value))
                    _markDirty();
            }
        }

        private string _targetColumnHeader;
        public string TargetColumnHeader
        {
            get => _targetColumnHeader;
            set
            {
                if (SetProperty(ref _targetColumnHeader, value))
                    _markDirty();
            }
        }

        private string _sheetPrefix;

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

        public string WorksheetName
        {
            get
            {
                return $"{SheetPrefix} Exact Match";
            }
        }
    }
}
