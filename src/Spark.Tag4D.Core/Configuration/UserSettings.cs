using System;
using System.IO;
using System.Text.Json;

namespace Spark.Tag4D.Core.Configuration
{
    /// <summary>
    /// Per-user preferences (last used files and folders), stored as JSON in
    /// <c>%APPDATA%\Spark.Tag4D\user-settings.json</c>. Works the same in Revit, MicroStation and the standalone tools,
    /// unlike .NET <c>app.config</c> settings which are not honoured for plug-in assemblies.
    /// </summary>
    public sealed class UserSettings
    {
        private static readonly object Gate = new object();
        private static UserSettings? _current;

        public string SettingsFilePath { get; set; } = string.Empty;

        public string ExcelFilePath { get; set; } = string.Empty;

        public string IfcFolderPath { get; set; } = string.Empty;

        /// <summary>The shared, lazily loaded instance.</summary>
        public static UserSettings Current
        {
            get
            {
                lock (Gate)
                {
                    return _current ??= Load();
                }
            }
        }

        /// <summary>Location of the settings file.</summary>
        public static string FilePath { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Spark.Tag4D",
            "user-settings.json");

        /// <summary>Persists the settings. Failures are swallowed: preferences must never break a command.</summary>
        public void Save()
        {
            lock (Gate)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                    File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions.Indented));
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                }
            }
        }

        /// <summary>Forgets the cached instance so the next access re-reads the file (used by tests).</summary>
        public static void Reset()
        {
            lock (Gate)
            {
                _current = null;
            }
        }

        private static UserSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    return JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(FilePath), JsonOptions.Tolerant)
                           ?? new UserSettings();
                }
            }
            catch (Exception exception) when (exception is IOException || exception is JsonException || exception is UnauthorizedAccessException)
            {
            }

            return new UserSettings();
        }
    }
}
