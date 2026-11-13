using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Spark.Tag4D.Core.Expressions;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Revit.Core;
using Spark.Tag4D.Revit.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Tag4D.Revit.App.Tag
{
    public class TagEventHandler : RevitEventWrapper<TagOptions>
    {
        private ParameterCache cache = new ParameterCache();

        public override void Execute(UIApplication app, TagOptions options)
        {
            options.Syncing?.Invoke(true);
            try
            {
                cache = new ParameterCache();
                Document document = app.ActiveUIDocument.Document;

                // One transaction for the whole update: faster than one per step, and a single entry in Revit's undo list.
                using (Transaction transaction = new Transaction(document, "Elements values update"))
                {
                    transaction.Start();

                    if (!options.UpdateAllValues)
                    {
                        foreach (PropertyField field in options.ChangedFields)
                        {
                            string value = field?.Value?.ToString();

                            foreach (var element in options.Elements)
                            {
                                cache.Get(element, field.Name).TryParseAndSet(value);
                            }
                        }
                    }

                    ApplyExactMatches(options);
                    ApplyExpressions(options);
                    ApplyComposed(options);

                    transaction.Commit();
                }

                new FieldsRefresh(options.Fields.ToList(), options.Elements, cache).Start();
            }
            catch (Exception exception)
            {
                TaskDialog.Show("Spark.Tag4D", $"Updating the elements failed: {exception.Message}");
            }
            finally
            {
                options.Syncing?.Invoke(false);
            }
        }

        private void ApplyComposed(TagOptions options)
        {
            foreach (PropertyField changedField in options.ChangedFields)
            {
                foreach (Element element in options.Elements)
                {
                    foreach (ComposedPropertyItem composedItem in options.ComposedItems)
                    {
                        List<string> propertyNamesToCompose = ComposedItemEvaluator.GetPropertyNames(composedItem.Formula);

                        if (!propertyNamesToCompose.Contains(changedField.Name))
                        {
                            continue;
                        }

                        PropertyField composingField = options.Fields.FirstOrDefault(item => item.Name == composedItem.ComposedPropertyName);

                        if (composingField == null)
                        {
                            continue;
                        }

                        List<PropertyField> fieldsToCompose = options.Fields.Where(item => propertyNamesToCompose.Contains(item.Name)).ToList();

                        Dictionary<string, string> propertyAndValuesToCompose = fieldsToCompose.ToDictionary(item => item.Name, item => cache.Get(element, item.Name)?.AsValueString());

                        string value = ComposedItemEvaluator.Resolve(composedItem, propertyAndValuesToCompose);

                        composingField.Value = value;

                        Parameter parameter = cache.Get(element, composingField.Name);

                        parameter.TryParseAndSet(value);
                    }
                }
            }
        }

        private void ApplyExactMatches(TagOptions options)
        {
            foreach (PropertyField changedField in options.ChangedFields)
            {
                foreach (Element element in options.Elements)
                {
                    string value = cache.Get(element, changedField.Name)?.AsValueString() ?? string.Empty;

                    PropertyValueMatch propertyValueMatch = options.PropertyValueExactMatches.FirstOrDefault(item => item.PropertyNameAndValuesSource.ContainsKey(changedField.Name) && item.PropertyNameAndValuesSource.Values.Contains(value));

                    if (propertyValueMatch == null)
                    {
                        continue;
                    }

                    var field = options.Fields.FirstOrDefault(item => item.Name == propertyValueMatch.PropertyNameTarget);

                    field.Value = propertyValueMatch.PropertyValueTarget;

                    Parameter parameter = cache.Get(element, field.Name);

                    if (parameter != null)
                    {
                        parameter.TryParseAndSet(propertyValueMatch.PropertyValueTarget);
                    }
                }
            }
        }

        private void ApplyExpressions(TagOptions options)
        {
            var invalidRules = new HashSet<string>();
            List<string> sourceParameterNames = options.Expressions.Select(item => item.SourcePropertyName).Distinct().ToList();

            foreach (PropertyField changedField in options.ChangedFields)
            {
                if (!sourceParameterNames.Contains(changedField.Name))
                {
                    continue;
                }

                List<ExpressionRule> relatedExpressions = options.Expressions.Where(item => item.SourcePropertyName == changedField.Name).ToList();

                foreach (Element element in options.Elements)
                {
                    foreach (var expression in relatedExpressions)
                    {
                        PropertyField targetField = options.Fields.FirstOrDefault(f => f.Name == expression.TargetPropertyName);

                        if (targetField == null)
                        {
                            continue;
                        }

                        object value = cache.Get(element, changedField.Name)?.GetValueAsObject();

                        if (!ExpressionEngine.TryEvaluate(value?.ToString(), expression.Function, out object result, out string error))
                        {
                            // A broken rule must not stop the remaining rules from being applied.
                            invalidRules.Add($"{expression.SourcePropertyName} -> {expression.TargetPropertyName}: {error}");
                            continue;
                        }

                        targetField.Value = result;
                        targetField.CanBeEdited = false;
                        targetField.IsReadOnly = true;

                        Parameter parameter = cache.Get(element, targetField.Name);
                        parameter?.TryParseAndSet(result?.ToString());
                    }
                }
            }

            if (invalidRules.Count > 0)
            {
                TaskDialog.Show("Spark.Tag4D", "Some expressions could not be evaluated:\n" + string.Join("\n", invalidRules));
            }
        }
    }
}
