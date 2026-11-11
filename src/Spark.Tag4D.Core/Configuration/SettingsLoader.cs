using System.IO;
using System.Text.Json;
using Spark.Tag4D.Core.Utils;

namespace Spark.Tag4D.Core.Configuration
{
    /// <summary>Loads Settings.json and remembers which file is active.</summary>
    public static class SettingsLoader
    {
        /// <summary>Reads a settings file and remembers it as the active one.</summary>
        public static SettingsRoot Load(string filePath)
        {
            SettingsRoot settings = Read(filePath);
            SavePath(filePath);
            return settings;
        }

        /// <summary>Reads a settings file without changing the remembered path.</summary>
        public static SettingsRoot Read(string filePath)
        {
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<SettingsRoot>(json, JsonOptions.Tolerant) ?? new SettingsRoot();
        }

        /// <summary>Loads the remembered settings file, falling back to the one shipped with the product.</summary>
        public static SettingsRoot? LoadExistingOrDefault()
        {
            string path = GetPath();

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                path = GetDefaultSettingsFilePath();
            }

            return File.Exists(path) ? Load(path) : null;
        }

        public static string GetPath() => UserSettings.Current.SettingsFilePath;

        public static void SavePath(string path)
        {
            UserSettings.Current.SettingsFilePath = path;
            UserSettings.Current.Save();
        }

        public static string GetDefaultSettingsFilePath()
        {
            return Path.Combine(AssemblyUtils.GetFolder(typeof(SettingsLoader)), Constants.FilesFolder, Constants.Tag4DFolder, Constants.JsonSettingsFileName);
        }
    }
}
