using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Spark.Tag4D.Core.Models
{
    /// <summary>Resolves composed properties such as <c>&lt;LocationRoot&gt;.&lt;SubLocation&gt;</c>.</summary>
    public static class ComposedItemEvaluator
    {
        private static readonly Regex Placeholder = new Regex("<([^<>]+)>", RegexOptions.Compiled);

        /// <summary>Names of the properties referenced by a formula.</summary>
        public static List<string> GetPropertyNames(string? expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                return new List<string>();
            }

            return Placeholder.Matches(expression)
                .Cast<Match>()
                .Select(match => match.Groups[1].Value.Trim())
                .Where(name => name.Length > 0)
                .ToList();
        }

        /// <summary>Substitutes placeholders; unknown placeholders are left untouched.</summary>
        public static string Resolve(ComposedPropertyItem item, IDictionary<string, string?> values)
        {
            if (string.IsNullOrEmpty(item.Formula))
            {
                return item.Formula;
            }

            return Placeholder.Replace(item.Formula, match =>
            {
                string key = match.Groups[1].Value;

                if (values.TryGetValue(key, out string? exact))
                {
                    return exact ?? string.Empty;
                }

                foreach (KeyValuePair<string, string?> pair in values)
                {
                    if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
                    {
                        return pair.Value ?? string.Empty;
                    }
                }

                return match.Value;
            });
        }

        /// <summary>Computes the composed value after <paramref name="changedField"/> was edited.</summary>
        public static ComposerPropertyResult GetComposedValue(PropertyField changedField, IList<PropertyField> allFields, ComposedPropertyItem composedItem)
        {
            List<string> referenced = GetPropertyNames(composedItem.Formula);

            if (!referenced.Contains(changedField.Name))
            {
                return new ComposerPropertyResult { CanBeComposed = false };
            }

            PropertyField? composingField = allFields.FirstOrDefault(field => field.Name == composedItem.ComposedPropertyName);
            if (composingField == null)
            {
                return new ComposerPropertyResult { CanBeComposed = false };
            }

            Dictionary<string, string?> values = allFields
                .Where(field => referenced.Contains(field.Name))
                .GroupBy(field => field.Name)
                .ToDictionary(group => group.Key, group => group.First().Value?.ToString());

            return new ComposerPropertyResult
            {
                CanBeComposed = true,
                ComposingField = composingField,
                Value = Resolve(composedItem, values)
            };
        }
    }
}
