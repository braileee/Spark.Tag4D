using Spark.Tag4D.Core.Json;
using Spark.Tag4D.Core.Utils;
using Newtonsoft.Json;
using System.IO;

namespace Spark.Tag4D.Core.Models
{
    public static class SettingsLoader
    {
        public static SettingsRoot Load(string filePath)
        {
            string json = File.ReadAllText(filePath);

            SettingsRoot settingsRoot = JsonConvert.DeserializeObject<SettingsRoot>(json);

            Properties.Settings.Default.SettingsFilePath = filePath;
            Properties.Settings.Default.Save();

            return settingsRoot;
        }

        public static SettingsRoot LoadExistingOrDefault()
        {
            string defaultSettingsPath = GetDefaultSettingsFilePath();
            string currentSettingsPath = Properties.Settings.Default.SettingsFilePath;

            if (string.IsNullOrEmpty(currentSettingsPath) || !File.Exists(currentSettingsPath))
            {
                currentSettingsPath = defaultSettingsPath;
            }

            if (!File.Exists(currentSettingsPath))
            {
                return null;
            }

            return Load(currentSettingsPath);
        }

        public static string GetPath()
        {
            return Properties.Settings.Default.SettingsFilePath;
        }

        public static void SavePath(string path)
        {
            Properties.Settings.Default.SettingsFilePath = path;
            Properties.Settings.Default.Save();
        }

        public static string GetDefaultSettingsFilePath()
        {
            string assemblyFolder = AssemblyUtils.GetFolder(typeof(SettingsLoader));
            string defaultSettingsPath = Path.Combine(assemblyFolder, Constants.FilesFolder, Constants.Tag4DFolder, Constants.JsonSettingsFileName);
            return defaultSettingsPath;
        }
    }
}
