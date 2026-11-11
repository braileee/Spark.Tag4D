using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Spark.Tag4D.Core.Configuration;

namespace Spark.Tag4D.Core.Models
{
    /// <summary>Raised when the settings file or parameters workbook cannot be found or read.</summary>
    public sealed class TagDataException : Exception
    {
        public TagDataException(string message)
            : base(message)
        {
        }

        public TagDataException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>Everything defined in the settings file and parameters workbook, loaded in one go.</summary>
    public sealed class TagData
    {
        public TagData(SettingsRoot settings, string excelFilePath)
        {
            Settings = settings;
            ExcelFilePath = excelFilePath;
        }

        public SettingsRoot Settings { get; }

        public string ExcelFilePath { get; }

        public List<PropertySetItem> PropertySets { get; private set; } = new List<PropertySetItem>();

        public List<PicklistGroup> Picklists { get; private set; } = new List<PicklistGroup>();

        public List<LayerMappingItem> LayerMappings { get; private set; } = new List<LayerMappingItem>();

        public List<ExpressionRule> Expressions { get; private set; } = new List<ExpressionRule>();

        public List<PropertyValueMatch> Matches { get; private set; } = new List<PropertyValueMatch>();

        public List<PropertyValueMatch> ExactMatches { get; private set; } = new List<PropertyValueMatch>();

        public List<ComposedPropertyItem> Composed { get; private set; } = new List<ComposedPropertyItem>();

        /// <summary>All property definitions, flattened across property sets.</summary>
        public List<PropertyItem> Properties => PropertySets.SelectMany(set => set.PropertyItems).ToList();

        /// <summary>Picklist values keyed by the property they belong to.</summary>
        public Dictionary<string, List<string>> PicklistsByProperty =>
            Picklists.GroupBy(group => group.GroupName).ToDictionary(group => group.Key, group => group.First().Values);

        /// <summary>
        /// Loads the remembered settings file (or the shipped default) and the workbook it points to.
        /// </summary>
        /// <exception cref="TagDataException">Settings or workbook are missing or unreadable.</exception>
        public static TagData Load()
        {
            SettingsRoot? settings;

            try
            {
                settings = SettingsLoader.LoadExistingOrDefault();
            }
            catch (Exception exception) when (exception is IOException || exception is System.Text.Json.JsonException)
            {
                throw new TagDataException($"The settings file could not be read: {exception.Message}", exception);
            }

            if (settings == null)
            {
                throw new TagDataException("No settings file found.");
            }

            string excelFilePath = ExcelDataLoader.LoadOrPromptExcelFilePath(settings.ExcelSettings.FileLinkSettings);
            return Load(settings, excelFilePath);
        }

        /// <summary>Loads the workbook at <paramref name="excelFilePath"/> using the layout described by <paramref name="settings"/>.</summary>
        public static TagData Load(SettingsRoot settings, string excelFilePath)
        {
            if (string.IsNullOrWhiteSpace(excelFilePath) || !File.Exists(excelFilePath))
            {
                throw new TagDataException("No Excel file selected, or the file does not exist.");
            }

            ExcelSettings excel = settings.ExcelSettings;

            try
            {
                return new TagData(settings, excelFilePath)
                {
                    PropertySets = ExcelDataLoader.LoadPropertySetItems(excelFilePath, excel.PropertiesSheet),
                    Picklists = ExcelDataLoader.LoadPicklistGroups(excelFilePath, excel.PicklistSheet),
                    LayerMappings = ExcelDataLoader.ReadLayerMappings(excelFilePath, excel.LayersMappingSheet),
                    Expressions = ExcelDataLoader.LoadExpressions(excelFilePath, excel.ExpressionSheet),
                    Matches = ExcelDataLoader.LoadPropertiesValueMatches(excelFilePath, excel.PropertyMatchSheet),
                    ExactMatches = ExcelDataLoader.LoadPropertiesExactValueMatches(excelFilePath, excel.PropertyExactMatchSheet),
                    Composed = ExcelDataLoader.LoadComposed(excelFilePath, excel.ComposedSheet)
                };
            }
            catch (Exception exception) when (exception is IOException || exception is InvalidDataException || exception is System.Xml.XmlException)
            {
                throw new TagDataException($"The Excel file could not be read: {exception.Message}", exception);
            }
        }
    }
}
