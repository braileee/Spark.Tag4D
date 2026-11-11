using System;

namespace Spark.Tag4D.Core
{
    public static class Constants
    {
        public const string TabName = "Tag4D";
        public const string PanelNameProperties = "Properties";
        public const string PanelNameExport = "Export";
        public static readonly string PanelNameViews = "Views";
        public static readonly string PanelNameIfc = "IFC";
        public const string ParametersTagElementsPaneId = "3363BB74-9044-4601-8BB7-96C7D36A7E74";
        public const string Tag4DFolder = "Tag4DFiles";
        public const string FilesFolder = "Files";
        public const string ExcelSettingsFileName = "IFC parameters.xlsx";
        public const string JsonSettingsFileName = "Settings.json";

        public static readonly string[] DataTypes = { "string", "int", "double", "bool" };
    }
}