using Spark.Tag4D.Settings.Models;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Settings.ViewModels.Support
{
    public class ExactMatchTableViewModel : BindableBase
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
                new DelegateCommand(AddRow);

            RemoveRowCommand =
                new DelegateCommand<ExactMatchRowViewModel>(
                    RemoveRow);

            AvailableColumnNames = new ObservableCollection<string>(table.AvailableColumnNames);
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

        public DelegateCommand AddRowCommand { get; }

        public DelegateCommand<ExactMatchRowViewModel>
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
                    RaisePropertyChanged(nameof(WorksheetName));

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
