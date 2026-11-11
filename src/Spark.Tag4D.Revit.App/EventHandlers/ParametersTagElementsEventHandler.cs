using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Revit.Core;
using Spark.Tag4D.Revit.Core.Models;
using Spark.Tag4D.Revit.App.Models;
using Spark.Tag4D.Revit.App.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Tag4D.Revit.App.EventHandlers
{
    public class ParametersTagElementsEventHandler : RevitEventWrapper<ParametersTagElementsOptions>
    {
        public override void Execute(UIApplication app, ParametersTagElementsOptions options)
        {
            try
            {
                using (TransactionGroup transactionGroup = new TransactionGroup(app.ActiveUIDocument.Document, "Elements values update"))
                {
                    transactionGroup.Start();


                    using (Transaction transaction = new Transaction(app.ActiveUIDocument.Document, "Parameters values change"))
                    {
                        transaction.Start();

                        if (!options.UpdateAllValues)
                        {
                            foreach (PropertyField field in options.ChangedFields)
                            {
                                foreach (var element in options.Elements)
                                {
                                    Parameter parameter = element.LookupParameter(field.Name);

                                    if (parameter == null)
                                    {
                                        continue;
                                    }

                                    parameter.TryParseAndSet(field?.Value?.ToString());
                                }
                            }
                        }

                        transaction.Commit();
                    }

                    using (Transaction transaction = new Transaction(app.ActiveUIDocument.Document, "Apply exact matches"))
                    {
                        transaction.Start();
                        ApplyExactMatches(options);
                        transaction.Commit();
                    }

                    using (Transaction transaction = new Transaction(app.ActiveUIDocument.Document, "Apply expressions"))
                    {
                        transaction.Start();
                        ApplyExpressions(options);
                        transaction.Commit();
                    }

                    using (Transaction transaction = new Transaction(app.ActiveUIDocument.Document, "Apply composed"))
                    {
                        transaction.Start();
                        ApplyComposed(options);
                        transaction.Commit();
                    }

                    transactionGroup.Assimilate();
                }

                FieldsRefresh fieldsRefresh = new FieldsRefresh(options.Fields.ToList(), options.Elements);
                fieldsRefresh.Start();
            }
            catch (Exception exception)
            {
            }
        }

        private void ApplyComposed(ParametersTagElementsOptions options)
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

                        Dictionary<string, string> propertyAndValuesToCompose = fieldsToCompose.ToDictionary(item => item.Name, item => element?.LookupParameter(item.Name)?.AsValueString());

                        string value = ComposedItemEvaluator.Resolve(composedItem, propertyAndValuesToCompose);

                        composingField.Value = value;

                        Parameter parameter = element.LookupParameter(composingField.Name);

                        parameter.TryParseAndSet(value);
                    }
                }
            }
        }

        private void ApplyExactMatches(ParametersTagElementsOptions options)
        {
            foreach (PropertyField changedField in options.ChangedFields)
            {
                foreach (Element element in options.Elements)
                {
                    string value = element.LookupParameter(changedField.Name)?.AsValueString() ?? string.Empty;

                    PropertyValueMatch propertyValueMatch = options.PropertyValueExactMatches.FirstOrDefault(item => item.PropertyNameAndValuesSource.ContainsKey(changedField.Name) && item.PropertyNameAndValuesSource.Values.Contains(value));

                    if (propertyValueMatch == null)
                    {
                        continue;
                    }

                    var field = options.Fields.FirstOrDefault(item => item.Name == propertyValueMatch.PropertyNameTarget);

                    field.Value = propertyValueMatch.PropertyValueTarget;

                    Parameter parameter = element.LookupParameter(field.Name);

                    if (parameter != null)
                    {
                        parameter.TryParseAndSet(propertyValueMatch.PropertyValueTarget);
                    }
                }
            }
        }

        private void ApplyExpressions(ParametersTagElementsOptions options)
        {
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

                        try
                        {
                            object value = element.LookupParameter(changedField.Name)?.GetValueAsObject();

                            targetField.Value = ExpressionEvaluator.Evaluate(value.ToString(), expression.Function);
                            targetField.CanBeEdited = false;
                            targetField.IsReadOnly = true;

                            Parameter parameter = element.LookupParameter(targetField.Name);

                            if (parameter != null)
                            {
                                parameter.TryParseAndSet(targetField.Value.ToString());
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }
            }
        }
    }
}
