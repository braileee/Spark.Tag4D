using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Spark.Tag4D.Core.Models
{
    /// <summary>Builds the editable fields of the tagging pane from the workbook data (the same for every host application).</summary>
    public static class PropertyFieldFactory
    {
        /// <summary>
        /// One field per property; properties with a picklist become combo boxes, and properties whose value is produced
        /// by a rule (expression, composed property, exact match) are read-only.
        /// </summary>
        public static ObservableCollection<PropertyField> Create(TagData data)
        {
            return Create(data.Properties, data.PicklistsByProperty, data.Expressions, data.Composed, data.ExactMatches);
        }

        public static ObservableCollection<PropertyField> Create(
            IEnumerable<PropertyItem> properties,
            IDictionary<string, List<string>> picklistsByProperty,
            IEnumerable<ExpressionRule> expressions,
            IEnumerable<ComposedPropertyItem> composed,
            IEnumerable<PropertyValueMatch> exactMatches)
        {
            var calculated = new HashSet<string>(
                expressions.Select(rule => rule.TargetPropertyName)
                    .Concat(composed.Select(item => item.ComposedPropertyName))
                    .Concat(exactMatches.Select(match => match.PropertyNameTarget)));

            var fields = new ObservableCollection<PropertyField>();

            foreach (PropertyItem property in properties)
            {
                picklistsByProperty.TryGetValue(property.PropertyName, out List<string>? values);
                var field = new PropertyField(property.PropertyName, property.DataType, values);

                if (calculated.Contains(property.PropertyName))
                {
                    field.CanBeEdited = false;
                    field.IsReadOnly = true;
                }

                fields.Add(field);
            }

            return fields;
        }
    }
}
