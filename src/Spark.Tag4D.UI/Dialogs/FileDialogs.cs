using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace Spark.Tag4D.UI.Dialogs
{
    /// <summary>Native file and folder pickers. All methods return null/empty when the user cancels.</summary>
    public static class FileDialogs
    {
        public const string ExcelFilter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*";
        public const string JsonFilter = "JSON files (*.json)|*.json|All files (*.*)|*.*";

        public static string OpenFile(string filter, string initialDirectory = null)
        {
            var dialog = new OpenFileDialog
            {
                Filter = filter,
                InitialDirectory = initialDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                RestoreDirectory = true
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public static IReadOnlyList<string> OpenFiles(string filter, string initialDirectory = null)
        {
            var dialog = new OpenFileDialog
            {
                Filter = filter,
                InitialDirectory = initialDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                RestoreDirectory = true,
                Multiselect = true
            };

            return dialog.ShowDialog() == true ? dialog.FileNames.ToList() : new List<string>();
        }

        public static string SaveFile(string filter, string defaultFileName = null, string initialDirectory = null)
        {
            var dialog = new SaveFileDialog
            {
                Filter = filter,
                FileName = defaultFileName ?? string.Empty,
                InitialDirectory = initialDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                OverwritePrompt = true,
                RestoreDirectory = true
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public static string PickFolder(string initialDirectory = null)
        {
            initialDirectory = initialDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

#if NET8_0_OR_GREATER
            var dialog = new OpenFolderDialog { InitialDirectory = initialDirectory };
            return dialog.ShowDialog() == true ? dialog.FolderName : null;
#else
            // .NET Framework has no WPF folder picker: use the classic "pick a file in the folder" trick.
            var dialog = new SaveFileDialog
            {
                InitialDirectory = initialDirectory,
                FileName = "Select this folder",
                Filter = "Folder|*.folder",
                OverwritePrompt = false,
                CheckPathExists = true
            };

            return dialog.ShowDialog() == true ? Path.GetDirectoryName(dialog.FileName) : null;
#endif
        }
    }
}
