using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Spark.Tag4D.Core.Mvvm;

namespace Spark.Tag4D.Core.Models
{
    public enum EditorType
    {
        String,
        Double,
        Int,
        Bool,
        Combo
    }

    /// <summary>An editable property shown in the tagging pane.</summary>
    public sealed class PropertyField : ObservableObject
    {
        private object? _value;
        private bool _changed;
        private bool _canBeEdited = true;
        private bool _isReadOnly;

        public PropertyField(string name, string dataType, IEnumerable<string>? lookupValues = null)
        {
            Name = name;
            DataType = dataType;

            if (lookupValues != null)
            {
                List<string> values = lookupValues.ToList();
                LookupValues = new ObservableCollection<string>(values);
                SourceLookupValues = new ObservableCollection<string>(values);
            }

            EditorType = ResolveEditorType(dataType, lookupValues);
        }

        public string Name { get; }

        public string DataType { get; }

        public EditorType EditorType { get; }

        /// <summary>Values currently offered by the combo box (narrowed down by matching rules).</summary>
        public ObservableCollection<string>? LookupValues { get; }

        /// <summary>The complete, unfiltered picklist.</summary>
        public ObservableCollection<string>? SourceLookupValues { get; }

        public object? Value
        {
            get => _value;
            set
            {
                _value = value;
                Changed = true;
                OnPropertyChanged();
            }
        }

        public bool Changed
        {
            get => _changed;
            set => SetProperty(ref _changed, value);
        }

        public bool CanBeEdited
        {
            get => _canBeEdited;
            set => SetProperty(ref _canBeEdited, value);
        }

        public bool IsReadOnly
        {
            get => _isReadOnly;
            set => SetProperty(ref _isReadOnly, value);
        }

        /// <summary>
        /// Narrows the picklists of the target fields after <paramref name="changedField"/> was edited,
        /// according to the match tables.
        /// </summary>
        public static void ReloadChangedFields(
            PropertyField? changedField,
            ObservableCollection<PropertyField> fields,
            List<PropertyValueMatch> propertyValueMatches)
        {
            if (changedField is null)
            {
                return;
            }

            List<PropertyValueMatch> sourceMatches = propertyValueMatches
                .Where(match => match.PropertyNameAndValuesSource.ContainsKey(changedField.Name))
                .ToList();

            if (sourceMatches.Count == 0)
            {
                return;
            }

            var targetPropertyNames = new HashSet<string>(sourceMatches.Select(match => match.PropertyNameTarget));
            Dictionary<string, PropertyField> fieldsByName = fields.GroupBy(f => f.Name).ToDictionary(g => g.Key, g => g.First());

            foreach (PropertyField field in fields)
            {
                if (field.LookupValues == null || field.SourceLookupValues == null || field.Name == changedField.Name)
                {
                    continue;
                }

                if (field.LookupValues.Count == 0)
                {
                    field.LookupValues.AddRange(field.SourceLookupValues);
                    continue;
                }

                if (!targetPropertyNames.Contains(field.Name))
                {
                    continue;
                }

                field.NarrowLookup(sourceMatches.Where(match => match.PropertyNameTarget == field.Name).ToList(), fieldsByName);
            }
        }

        private void NarrowLookup(List<PropertyValueMatch> matches, Dictionary<string, PropertyField> fieldsByName)
        {
            var criteria = new Dictionary<string, string>();
            bool anyMatchValid = false;

            foreach (PropertyValueMatch match in matches)
            {
                bool isMatch = true;

                foreach (KeyValuePair<string, string> source in match.PropertyNameAndValuesSource)
                {
                    if (!fieldsByName.TryGetValue(source.Key, out PropertyField? sourceField)
                        || !string.Equals(sourceField.Value?.ToString(), source.Value, StringComparison.Ordinal))
                    {
                        isMatch = false;
                        break;
                    }

                    criteria[source.Key] = source.Value;
                }

                anyMatchValid |= isMatch;
            }

            if (!anyMatchValid)
            {
                if (!LookupValues!.SequenceEqual(SourceLookupValues!))
                {
                    Replace(LookupValues!, SourceLookupValues!.ToList());
                }

                return;
            }

            List<string> targetValues = matches
                .Where(match => match.PropertyNameAndValuesSource.Count == criteria.Count
                                && !match.PropertyNameAndValuesSource.Except(criteria).Any())
                .Select(match => match.PropertyValueTarget)
                .ToList();

            List<string> lookup = targetValues.Count > 0 ? targetValues : SourceLookupValues!.ToList();
            if (lookup.Count > 0)
            {
                Replace(LookupValues!, lookup);
            }
        }

        private static void Replace(ObservableCollection<string> target, IEnumerable<string> values)
        {
            target.Clear();
            target.AddRange(values);
        }

        private static EditorType ResolveEditorType(string dataType, IEnumerable<string>? lookup)
        {
            if (lookup != null)
            {
                return EditorType.Combo;
            }

            switch ((dataType ?? string.Empty).ToLowerInvariant())
            {
                case "bool":
                    return EditorType.Bool;
                case "int":
                    return EditorType.Int;
                case "double":
                    return EditorType.Double;
                default:
                    return EditorType.String;
            }
        }
    }
}
