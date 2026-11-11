namespace Spark.Tag4D.Core.Configuration
{
    /// <summary>Deserialised content of Settings.json.</summary>
    public sealed class SettingsRoot
    {
        public ExcelSettings ExcelSettings { get; set; } = new ExcelSettings();
    }

    /// <summary>Layout of the parameters workbook: which sheet holds what and how it is laid out.</summary>
    public sealed class ExcelSettings
    {
        public PropertiesSheet PropertiesSheet { get; set; } = new PropertiesSheet();

        public PicklistSheet PicklistSheet { get; set; } = new PicklistSheet();

        public LayersMappingSheet LayersMappingSheet { get; set; } = new LayersMappingSheet();

        public PropertyMatchSheet PropertyMatchSheet { get; set; } = new PropertyMatchSheet();

        public PropertyExactMatchSheet PropertyExactMatchSheet { get; set; } = new PropertyExactMatchSheet();

        public ExpressionSheet ExpressionSheet { get; set; } = new ExpressionSheet();

        public ComposedSheet ComposedSheet { get; set; } = new ComposedSheet();

        public FileLinkSettings FileLinkSettings { get; set; } = new FileLinkSettings();
    }
}
