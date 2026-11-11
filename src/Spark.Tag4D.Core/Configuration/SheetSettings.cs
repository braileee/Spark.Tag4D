using System.Collections.Generic;

namespace Spark.Tag4D.Core.Configuration
{
    /// <summary>Common settings of every sheet in the parameters workbook.</summary>
    public abstract class SheetBase
    {
        /// <summary>Zero-based index of the header row.</summary>
        public int HeaderRowIndex { get; set; }
    }

    public abstract class OneSheetBase : SheetBase
    {
        public string SheetName { get; set; } = string.Empty;
    }

    public abstract class MultipleSheetBase : SheetBase
    {
        public List<string> SheetNames { get; set; } = new List<string>();
    }

    public sealed class PropertiesSheet : OneSheetBase
    {
        public string PropertySetNameColumn { get; set; } = string.Empty;

        public string PropertyNameColumn { get; set; } = string.Empty;

        public string DataTypeColumn { get; set; } = string.Empty;
    }

    public sealed class PicklistSheet : OneSheetBase
    {
    }

    public sealed class LayersMappingSheet : OneSheetBase
    {
        public string LayerColumnName { get; set; } = string.Empty;
    }

    public sealed class PropertyMatchSheet : MultipleSheetBase
    {
    }

    public sealed class PropertyExactMatchSheet : MultipleSheetBase
    {
    }

    public sealed class ExpressionSheet : OneSheetBase
    {
        public string SourcePropertyColumnName { get; set; } = string.Empty;

        public string TargetPropertyColumnName { get; set; } = string.Empty;

        public string FunctionColumnName { get; set; } = string.Empty;

        public string ValueColumnName { get; set; } = string.Empty;
    }

    public sealed class ComposedSheet : OneSheetBase
    {
        public string ComposedPropertyColumnName { get; set; } = string.Empty;

        public string FormulaColumnName { get; set; } = string.Empty;
    }

    /// <summary>Where the parameters workbook comes from.</summary>
    public sealed class FileLinkSettings
    {
        public bool LoadDefault { get; set; } = true;

        public string CustomExcelFilePath { get; set; } = string.Empty;
    }
}
