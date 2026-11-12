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

        /// <summary>
        /// Writes the page back to the workbook. Returns false when nothing was written because the data is invalid
        /// (the page has told the user why); throws when the workbook could not be written.
        /// </summary>
        public abstract bool Save();
    }
}
