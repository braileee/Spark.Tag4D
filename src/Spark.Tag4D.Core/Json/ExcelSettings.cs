namespace Spark.Tag4D.Core.Json
{
    public class ExcelSettings
    {

        public PropertiesSheet PropertiesSheet { get; set; }
        public PicklistSheet PicklistSheet { get; set; }
        public LayersMappingSheet LayersMappingSheet { get; set; }
        public PropertyMatchSheet PropertyMatchSheet { get; set; }
        public PropertyExactMatchSheet PropertyExactMatchSheet { get; set; }
        public ExpressionSheet ExpressionSheet { get; set; }
        public ComposedSheet ComposedSheet { get; set; }

        public FileLinkSettings FileLinkSettings { get; set; }

        public ExcelSettings()
        {
            PropertiesSheet = new PropertiesSheet();
            PicklistSheet = new PicklistSheet();
            LayersMappingSheet = new LayersMappingSheet();
            PropertyMatchSheet = new PropertyMatchSheet();
            PropertyExactMatchSheet = new PropertyExactMatchSheet();
            ExpressionSheet = new ExpressionSheet();
            ComposedSheet = new ComposedSheet();
            FileLinkSettings = new FileLinkSettings();
        }
    }
}
