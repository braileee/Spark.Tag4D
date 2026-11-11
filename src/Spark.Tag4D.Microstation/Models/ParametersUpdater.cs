using Bentley.DgnPlatformNET.Elements;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Microstation.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Tag4D.Microstation.Models
{
    public class ParametersUpdater
    {
        public ParametersUpdater(List<Element> elements, List<PropertyField> changeFields, List<PropertyField> allFields, List<ExpressionItem> expressionItems, List<ComposedPropertyItem> composedPropertyItems, List<PropertyValueMatch> propertyValueExactMatches)
        {
            Elements = elements;
            ChangedFields = changeFields;
            Fields = allFields;
            ExpressionItems = expressionItems;
            ComposedItems = composedPropertyItems;
            PropertyValueExactMatches = propertyValueExactMatches;
        }

        public List<Element> Elements { get; } = new List<Element>();
        public List<PropertyField> ChangedFields { get; } = new List<PropertyField>();
        public List<PropertyField> Fields { get; } = new List<PropertyField>();
        public List<ExpressionItem> ExpressionItems { get; } = new List<ExpressionItem>();

        public List<ComposedPropertyItem> ComposedItems { get; set; } = new List<ComposedPropertyItem>();
        public List<PropertyValueMatch> PropertyValueExactMatches { get; } = new List<PropertyValueMatch>();

        public List<PropertyField> AdditionalChangedFields { get; set; } = new List<PropertyField>();

        public void Update()
        {
            foreach (PropertyField field in ChangedFields)
            {
                foreach (Element element in Elements)
                {

                    element.SetValue(field.Name, field.Value?.ToString());
                }
            }

            ApplyExactMatches(ChangedFields);
            ApplyExpressions(ChangedFields, AdditionalChangedFields);
            ApplyComposed(ChangedFields, AdditionalChangedFields);
        }

        public void UpdateComposedAndExpressionValues()
        {
            ApplyExactMatches(ChangedFields);
            ApplyExpressions(ChangedFields, AdditionalChangedFields);
            ApplyComposed(ChangedFields, AdditionalChangedFields);
        }

        private void ApplyExactMatches(List<PropertyField> fields)
        {
            foreach (PropertyField changedField in fields)
            {
                foreach (Element element in Elements)
                {
                    string elementValue = element?.GetValue(changedField.Name)?.ToString() ?? string.Empty;

                    //PropertyValueMatch propertyValueMatch = PropertyValueExactMatches.FirstOrDefault(item => item.PropertyNameSource == changedField.Name && item.PropertyValueSource == elementValue);
                    PropertyValueMatch propertyValueMatch = PropertyValueExactMatches.FirstOrDefault(item => item.PropertyNameAndValuesSource.ContainsKey(changedField.Name) && item.PropertyNameAndValuesSource.Values.Contains(elementValue));

                    if (propertyValueMatch == null)
                    {
                        continue;
                    }

                    var field = Fields.FirstOrDefault(item => item.Name == propertyValueMatch.PropertyNameTarget);

                    field.Value = propertyValueMatch.PropertyValueTarget;

                    element.SetValue(field.Name, propertyValueMatch.PropertyValueTarget);
                    AdditionalChangedFields.Add(field);
                }
            }
        }

        private void ApplyComposed(List<PropertyField> fields, List<PropertyField> additionalChangedFields)
        {
            List<PropertyField> allChangedFields = new List<PropertyField>();
            allChangedFields.AddRange(fields);
            allChangedFields.AddRange(additionalChangedFields);

            foreach (PropertyField changedField in allChangedFields)
            {
                foreach (Element element in Elements)
                {
                    foreach (var composedItem in ComposedItems)
                    {
                        List<string> propertyNamesToCompose = ComposedItemEvaluator.GetPropertyNames(composedItem.Formula);

                        if (!propertyNamesToCompose.Contains(changedField.Name))
                        {
                            continue;
                        }

                        PropertyField composingField = Fields.FirstOrDefault(item => item.Name == composedItem.ComposedPropertyName);

                        if (composingField == null)
                        {
                            continue;
                        }

                        List<PropertyField> fieldsToCompose = Fields.Where(item => propertyNamesToCompose.Contains(item.Name)).ToList();

                        Dictionary<string, string> propertyAndValuesToCompose = fieldsToCompose.ToDictionary(item => item.Name, item => element?.GetValue(item.Name)?.ToString());

                        string value = ComposedItemEvaluator.Resolve(composedItem, propertyAndValuesToCompose);

                        composingField.Value = value;

                        element.SetValue(composingField.Name, value);
                    }
                }
            }
        }

        private void ApplyExpressions(List<PropertyField> fields, List<PropertyField> additionalChangedFields)
        {
            List<PropertyField> allChangedFields = new List<PropertyField>();
            allChangedFields.AddRange(fields);
            allChangedFields.AddRange(additionalChangedFields);

            List<string> sourceParameterNames = ExpressionItems.Select(item => item.SourcePropertyName).Distinct().ToList();

            foreach (var changedField in allChangedFields)
            {
                foreach (Element element in Elements)
                {
                    if (!sourceParameterNames.Contains(changedField.Name))
                    {
                        continue;
                    }

                    List<ExpressionItem> expressions = ExpressionItems.Where(item => item.SourcePropertyName == changedField.Name).ToList();

                    foreach (var expression in expressions)
                    {
                        PropertyField targetField = Fields.FirstOrDefault(f => f.Name == expression.TargetPropertyName);

                        if (targetField == null)
                        {
                            continue;
                        }

                        object value = element.GetValue(changedField.Name);

                        targetField.Value = ExpressionEvaluator.Evaluate(expression, value);
                        targetField.CanBeEdited = false;
                        targetField.IsReadOnly = true;

                        element.SetValue(targetField.Name, targetField.Value?.ToString());
                    }
                }
            }
        }

    }
}
