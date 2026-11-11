using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Spark.Tag4D.Core.Utils
{
    public static class FilePromptUtils
    {
        public static string GetFilePath(string initialDirectory = "c:\\",
            string dialogFilter = "txt files (*.txt)|*.txt|All files (*.*)|*.*")
        {
            //EXAMPLE
            //OpenFileDialog openFileDialog1 = new OpenFileDialog();

            //openFileDialog1.InitialDirectory = "c:\\";
            //openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            //openFileDialog1.FilterIndex = 2;
            //openFileDialog1.RestoreDirectory = true;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.InitialDirectory = initialDirectory;
            openFileDialog1.Filter = dialogFilter;
            openFileDialog1.FilterIndex = 0;
            openFileDialog1.RestoreDirectory = true;
            string selectedFileName = string.Empty;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                selectedFileName = openFileDialog1.FileName;
            }
            return selectedFileName;
        }

        public static bool IsFileLocked(string filePath)
        {
            var file = new FileInfo(filePath);
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }

        public static string SaveFileToFolder(Environment.SpecialFolder initDirectory,
            string defaultName, string defaultExtension)
        {
            //EXAMPLE
            //OpenFileDialog openFileDialog1 = new OpenFileDialog();

            //openFileDialog1.InitialDirectory = "c:\\";
            //openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            //openFileDialog1.FilterIndex = 2;
            //openFileDialog1.RestoreDirectory = true;
            var saveDialog = new SaveFileDialog();
            saveDialog.OverwritePrompt = true;
            saveDialog.FileName = defaultName;
            saveDialog.InitialDirectory = Environment.GetFolderPath(initDirectory);
            saveDialog.Filter = "All files (*.*)|*.*";
            //folderDialog.DefaultExt = ".txt";
            saveDialog.DefaultExt = defaultExtension;

            string selectedFolderPath = string.Empty;
            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                selectedFolderPath = saveDialog.FileName;
            }
            return selectedFolderPath;
        }
    }
}
