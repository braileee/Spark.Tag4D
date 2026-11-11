using Spark.Tag4D.Core.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using static NPOI.HSSF.UserModel.HeaderFooter;


public enum EditorType
{
    String,
    Double,
    Int,
    Bool,
    Combo
}

public class PropertyField : INotifyPropertyChanged
{
    public string Name { get; }
    public string DataType { get; }

    public EditorType EditorType { get; }

    public ObservableCollection<string> LookupValues { get; set; } = new ObservableCollection<string>();
    public ObservableCollection<string> SourceLookupValues { get; }

    private object _value;
    public object Value
    {
        get => _value;
        set
        {
            _value = value;
            Changed = true;
            OnPropertyChanged();
        }
    }

    private bool changed;

    public bool Changed
    {
        get { return changed; }
        set
        {
            changed = value;
            OnPropertyChanged();
        }
    }

    private bool canBeEdited;

    public bool CanBeEdited
    {
        get { return canBeEdited; }
        set
        {
            canBeEdited = value;
            OnPropertyChanged();
        }
    }

    private bool isReadOnly;

    public bool IsReadOnly
    {
        get { return isReadOnly; }
        set
        {
            isReadOnly = value;
            OnPropertyChanged();
        }
    }

    public PropertyField(
        string name,
        string dataType,
        IEnumerable<string> lookupValues = null)
    {
        Name = name;
        DataType = dataType;

        LookupValues = lookupValues != null
            ? new ObservableCollection<string>(lookupValues)
            : null;

        SourceLookupValues = lookupValues != null
            ? new ObservableCollection<string>(lookupValues)
            : null;

        EditorType = ResolveEditorType(dataType, lookupValues);

        CanBeEdited = true;
        IsReadOnly = false;
    }

    private static EditorType ResolveEditorType(string dataType, IEnumerable<string> lookup)
    {
        if (lookup != null)
            return EditorType.Combo;

        switch (dataType.ToLower())
        {
            case "bool":
                return EditorType.Bool;

            case "int":
                return EditorType.Int;

            case "double":
                return EditorType.Double;

            case "string":
                return EditorType.String;

            default:
                return EditorType.String;
        }
    }

    public static void ReloadChangedFields(
    PropertyField changedField,
    ObservableCollection<PropertyField> fields,
    List<PropertyValueMatch> propertyValueMatches)
    {
        if (changedField is null)
            return;

        // Cache matches that depend on the changed field
        var sourceMatches = propertyValueMatches
            .Where(m => m.PropertyNameAndValuesSource.ContainsKey(changedField.Name))
            .ToList();

        if (sourceMatches.Count == 0)
            return;

        // Use HashSet for O(1) lookups
        var targetPropertyNames = new HashSet<string>(
            sourceMatches.Select(m => m.PropertyNameTarget));

        // Cache dictionary for fast field lookup
        var fieldsByName = fields.ToDictionary(f => f.Name, f => f);

        foreach (var field in fields)
        {
            if (field.LookupValues == null || field.Name == changedField.Name)
                continue;

            // Reset values if empty
            if (field.LookupValues.Count == 0)
            {
                field.LookupValues.AddRange(field.SourceLookupValues);
                continue;
            }

            // Skip non-target fields
            if (!targetPropertyNames.Contains(field.Name))
                continue;

            // Matches that target this field
            var currentFieldMatches = sourceMatches
                .Where(m => m.PropertyNameTarget == field.Name)
                .ToList();

            var lookupFields = new Dictionary<string, string>();
            bool anyMatchValid = false;

            foreach (var match in currentFieldMatches)
            {
                bool isMatch = true;

                foreach (var kvp in match.PropertyNameAndValuesSource)
                {
                    if (!fieldsByName.TryGetValue(kvp.Key, out var sourceField))
                    {
                        isMatch = false;
                        break;
                    }

                    if (!Equals(sourceField.Value?.ToString(), kvp.Value))
                    {
                        isMatch = false;
                        break;
                    }

                    lookupFields[kvp.Key] = kvp.Value;
                }

                if (isMatch)
                    anyMatchValid = true;
            }

            // No valid matches → keep original lookups
            if (!anyMatchValid)
            {
                if (field.LookupValues.Count != field.SourceLookupValues.Count ||
                   !field.LookupValues.SequenceEqual(field.SourceLookupValues))
                {
                    field.LookupValues.Clear();
                    field.LookupValues.AddRange(field.SourceLookupValues.ToList());
                }

                continue;
            }

            // Filter matches by the lookupFields criteria
            var currentMatches = currentFieldMatches
                .Where(m =>
                    m.PropertyNameAndValuesSource.Count == lookupFields.Count &&
                    !m.PropertyNameAndValuesSource.Except(lookupFields).Any())
                .ToList();

            var targetValues = currentMatches
                .Select(m => m.PropertyValueTarget)
                .ToList();

            var lookupValues = (targetValues.Count > 0)
                ? targetValues
                : field.SourceLookupValues.ToList();

            if (lookupValues.Count > 0)
            {
                field.LookupValues.Clear();
                field.LookupValues.AddRange(lookupValues);
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
