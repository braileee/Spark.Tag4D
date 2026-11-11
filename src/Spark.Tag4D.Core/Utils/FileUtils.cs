using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Spark.Tag4D.IfcValidator.Utils
{
    public static class FileUtils
    {

        public static string GetCurrentAssemblyFilePath()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().Location;
        }

        public static string GetCurrentAssemblyFolder()
        {
            return Path.GetDirectoryName(
                      System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        public static string GetAppDataFolderPath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }

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

        public static List<string> GetMultipleFilePaths(string initialDirectory = "c:\\", string dialogFilter = "All files (*.*)|*.*")
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.InitialDirectory = initialDirectory;
            openFileDialog1.Filter = dialogFilter;
            openFileDialog1.FilterIndex = 0;
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.Multiselect = true;
            var selectedFileNameList = new List<string>();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                selectedFileNameList = openFileDialog1.FileNames.ToList();
            }
            return selectedFileNameList;
        }

        public static string SaveFileToFolder(Environment.SpecialFolder initDirectory, string defaultExtension)
        {
            //EXAMPLE
            //OpenFileDialog openFileDialog1 = new OpenFileDialog();

            //openFileDialog1.InitialDirectory = "c:\\";
            //openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            //openFileDialog1.FilterIndex = 2;
            //openFileDialog1.RestoreDirectory = true;
            var saveDialog = new SaveFileDialog
            {
                OverwritePrompt = true,
                InitialDirectory = Environment.GetFolderPath(initDirectory),
                Filter = "All files (*.*)|*.*",
                //folderDialog.DefaultExt = ".txt";
                DefaultExt = defaultExtension
            };

            string selectedFolderPath = string.Empty;
            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                selectedFolderPath = saveDialog.FileName;
            }
            return selectedFolderPath;
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

        public static void WriteToFile(string filePath, List<string> lines)
        {
            FileInfo fi = new FileInfo(filePath);
            if (!fi.Directory.Exists)
            {
                System.IO.Directory.CreateDirectory(fi.DirectoryName);
            }
            using (StreamWriter outputFile = new StreamWriter(filePath))
            {
                foreach (string line in lines)
                    outputFile.WriteLine(line);
            }
        }

        public static void WriteToFile(string filePath, string value)
        {
            FileInfo fi = new FileInfo(filePath);
            if (!fi.Directory.Exists)
            {
                System.IO.Directory.CreateDirectory(fi.DirectoryName);
            }
            File.WriteAllText(filePath, value);
        }

        public static string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "");
                }
            }
        }

        public static List<string> ReadFile(string filePath)
        {
            var rows = new List<string>();
            using (var reader = new StreamReader($@"{filePath}"))
            {

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    rows.Add(line);
                }
            }
            return rows;
        }
    }
}
