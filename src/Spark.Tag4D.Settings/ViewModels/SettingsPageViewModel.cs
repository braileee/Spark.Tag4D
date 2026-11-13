using Spark.Tag4D.Core.Mvvm;

namespace Spark.Tag4D.Settings.ViewModels
{
    /// <summary>
    /// A tab of the settings window that edits one part of the parameters workbook. The window owns the Save and
    /// Cancel buttons; pages only say whether they have unsaved changes and how to write them.
    /// </summary>
    public abstract class SettingsPageViewModel : ObservableObject
    {
        private bool _isDirty;

        /// <summary>Tab header.</summary>
        public abstract string Title { get; }

        /// <summary>Which sheet(s) of the workbook this page reads and writes, shown on the page.</summary>
        public abstract string SheetInfo { get; }

        public bool IsDirty
        {
            get => _isDirty;
            protected set => SetProperty(ref _isDirty, value);
        }

        public void MarkDirty() => IsDirty = true;

        private string _filterText = string.Empty;

        /// <summary>The page search box: pages narrow down what they show to what contains this text.</summary>
        public string FilterText
        {
            get => _filterText;
            set
            {
                if (SetProperty(ref _filterText, value ?? string.Empty))
                {
                    OnFilterChanged(_filterText.Trim());
                }
            }
        }

        /// <summary>Text for the empty search box of the page.</summary>
        public virtual string FilterHint => "Search";

        /// <summary>Called with the trimmed search text (empty = show everything).</summary>
        protected virtual void OnFilterChanged(string text)
        {
        }

        /// <summary>Case-insensitive "contains"; an empty search text matches everything.</summary>
        protected internal static bool Contains(string value, string text) =>
            string.IsNullOrEmpty(text) || (value ?? string.Empty).IndexOf(text, System.StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>
        /// Writes the page back to the workbook. Returns false when nothing was written because the data is invalid
        /// (the page has told the user why); throws when the workbook could not be written.
        /// </summary>
        public abstract bool Save();
    }
}
