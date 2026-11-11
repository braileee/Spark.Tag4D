using System.Collections.Generic;

namespace Spark.Tag4D.Core.Models
{
    /// <summary>Derives <see cref="TargetPropertyName"/> from <see cref="SourcePropertyName"/> using <see cref="Function"/>.</summary>
    public sealed class ExpressionRule
    {
        public string SourcePropertyName { get; set; } = string.Empty;

        public string TargetPropertyName { get; set; } = string.Empty;

        /// <summary>Expression understood by <see cref="Expressions.ExpressionEngine"/>.</summary>
        public string Function { get; set; } = string.Empty;
    }

    public sealed class LayerMappingItem
    {
        public string LayerName { get; set; } = string.Empty;

        public Dictionary<string, string> PropertiesWithValues { get; set; } = new Dictionary<string, string>();
    }

    public sealed class ComposedPropertyItem
    {
        public string ComposedPropertyName { get; set; } = string.Empty;

        /// <summary>Text with <c>&lt;Property&gt;</c> placeholders.</summary>
        public string Formula { get; set; } = string.Empty;
    }

    public sealed class ComposerPropertyResult
    {
        public bool CanBeComposed { get; set; }

        public string Value { get; set; } = string.Empty;

        public PropertyField? ComposingField { get; set; }
    }

    public sealed class PicklistGroup
    {
        public string GroupName { get; set; } = string.Empty;

        public List<string> Values { get; set; } = new List<string>();
    }

    public sealed class PropertyDataRow
    {
        public string PropertySetName { get; set; } = string.Empty;

        public string PropertyName { get; set; } = string.Empty;

        public string DataType { get; set; } = string.Empty;
    }

    public sealed class PropertyItem
    {
        public string PropertyName { get; set; } = string.Empty;

        public string DataType { get; set; } = string.Empty;
    }

    public sealed class PropertySetItem
    {
        public string PropertySetName { get; set; } = string.Empty;

        public List<PropertyItem> PropertyItems { get; set; } = new List<PropertyItem>();
    }

    /// <summary>When all source values match, the target property may only take <see cref="PropertyValueTarget"/>.</summary>
    public sealed class PropertyValueMatch
    {
        public Dictionary<string, string> PropertyNameAndValuesSource { get; set; } = new Dictionary<string, string>();

        public string PropertyNameTarget { get; set; } = string.Empty;

        public string PropertyValueTarget { get; set; } = string.Empty;
    }
}
