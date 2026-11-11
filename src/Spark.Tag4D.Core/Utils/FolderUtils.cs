using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Spark.Tag4D.IfcValidator.Utils
{
    public class FolderUtils
    {
        public static string GetFolderPath(Environment.SpecialFolder initDirectory)
        {
            var folderDialog = new FolderBrowserDialog();
            folderDialog.RootFolder = initDirectory;
            string selectedFolderPath = string.Empty;
            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                selectedFolderPath = folderDialog.SelectedPath;
            }
            return selectedFolderPath;
        }

        public static string GetFolderPathExtendedWindow(Environment.SpecialFolder initDirectory)
        {
           return GetFolderPathExtendedWindow(Environment.GetFolderPath(initDirectory));
        }

        public static string GetFolderPathExtendedWindow(string initDirectory)
        {
            var saveDialog = new SaveFileDialog();
            saveDialog.OverwritePrompt = true;
            saveDialog.InitialDirectory = initDirectory;
            saveDialog.Filter = "All files (*.*)|*.*";
            saveDialog.FileName = "Selected a folder";

            string selectedFolderPath = string.Empty;
            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                selectedFolderPath = Path.GetDirectoryName(saveDialog.FileName);
            }
            return selectedFolderPath;
        }

        public static bool CreateDirectoryIfNotExists(string path)
        {

            bool exists = Directory.Exists(path);

            if (!exists)
            {
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
