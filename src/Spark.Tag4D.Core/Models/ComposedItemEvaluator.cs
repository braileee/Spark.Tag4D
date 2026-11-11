using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Spark.Tag4D.Core.Models
{
    public static class ComposedItemEvaluator
    {
        public static List<string> GetPropertyNames(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return new List<string>();

            Regex angleTokenRegex = new Regex("<([^<>]+)>", RegexOptions.Compiled);

            return angleTokenRegex
                .Matches(expression)
                .Cast<Match>()                     // Required for .NET 4.8
                .Select(m => m.Groups[1].Value.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }


        public static string Resolve(ComposedPropertyItem composedPropertyItem, Dictionary<string, string> propertyNamesWithValues)
        {
            if (string.IsNullOrEmpty(composedPropertyItem.Formula))
                return composedPropertyItem.Formula;

            Regex placeholderRegex = new Regex(@"\<([^<>]+)\>", RegexOptions.Compiled);

            return placeholderRegex.Replace(composedPropertyItem.Formula, match =>
            {
                string key = match.Groups[1].Value;

                // Try to get the value (case-insensitive)
                if (propertyNamesWithValues.TryGetValue(key, out string value))
                    return value;

                // Try case-insensitive matching
                foreach (var kv in propertyNamesWithValues)
                {
                    if (string.Equals(kv.Key, key, System.StringComparison.OrdinalIgnoreCase))
                        return kv.Value;
                }

                // If not found, keep original <Property>
                return match.Value;
            });
        }

        public static ComposerPropertyResult GetComposedValue(PropertyField changedField, List<PropertyField> allFields, ComposedPropertyItem composedItem)
        {
            List<string> propertyNamesToCompose = ComposedItemEvaluator.GetPropertyNames(composedItem.Formula);

            if (!propertyNamesToCompose.Contains(changedField.Name))
            {
                return new ComposerPropertyResult
                {
                    CanBeComposed = false
                };
            }

            PropertyField composingField = allFields.FirstOrDefault(item => item.Name == composedItem.ComposedPropertyName);

            if (composingField == null)
            {
                return new ComposerPropertyResult
                {
                    CanBeComposed = false,
                };
            }

            List<PropertyField> fieldsToCompose = allFields.Where(item => propertyNamesToCompose.Contains(item.Name)).ToList();

            Dictionary<string, string> propertyAndValuesToCompose = fieldsToCompose.ToDictionary(item => item.Name, item => item?.Value?.ToString());

            string value = ComposedItemEvaluator.Resolve(composedItem, propertyAndValuesToCompose);
            return new ComposerPropertyResult
            {
                CanBeComposed = true,
                ComposingField = composingField,
                Value = value
            };
        }
    }
}
