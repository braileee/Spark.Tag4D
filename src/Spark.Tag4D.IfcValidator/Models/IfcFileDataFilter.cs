using Spark.Tag4D.Core.Expressions;
using Spark.Tag4D.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Tag4D.IfcValidator.Models
{
    public class IfcFileDataFilter
    {
        public IfcFileDataFilter(List<IfcFile> ifcFiles, List<PropertySetItem> propertySetItems, List<PicklistGroup> picklistGroups)
        {
            IfcFiles = ifcFiles;
            PropertySetItems = propertySetItems;
        }

        public List<IfcFile> IfcFiles { get; } = new List<IfcFile>();
        public List<PropertySetItem> PropertySetItems { get; } = new List<PropertySetItem>();


        public List<IfcFile> GetElementsWithAnyRequiredProperty()
        {
            List<IfcFile> filteredIfcFiles = new List<IfcFile>();

            foreach (IfcFile ifcFile in IfcFiles)
            {
                IfcFile filteredIfcFile = new IfcFile
                {
                    FilePath = ifcFile.FilePath,
                    IfcElements = new List<IfcElement>()
                };

                foreach (IfcElement ifcElement in ifcFile.IfcElements)
                {
                    IfcElement ifcElementUpdated = new IfcElement
                    {
                        Guid = ifcElement.Guid,
                        IfcEntity = ifcElement.IfcEntity,
                        IfcProperties = new List<IfcProperty>(),
                        Layer = ifcElement.Layer,
                        Tag = ifcElement.Tag
                    };

                    foreach (IfcProperty ifcProperty in ifcElement.IfcProperties)
                    {
                        PropertySetItem propertySetItem = PropertySetItems.FirstOrDefault(propertySetItem => propertySetItem.PropertySetName == ifcProperty.PropertySetName);

                        if (propertySetItem == null)
                        {
                            continue;
                        }

                        PropertyItem propertyItem = propertySetItem.PropertyItems.FirstOrDefault(item => item.PropertyName == ifcProperty.PropertyName);

                        if (propertyItem == null)
                        {
                            continue;
                        }

                        ifcElementUpdated.IfcProperties.Add(ifcProperty);
                    }

                    if (ifcElementUpdated.IfcProperties.Any())
                    {
                        filteredIfcFile.IfcElements.Add(ifcElementUpdated);
                    }
                }

                if (filteredIfcFile.IfcElements.Any())
                {
                    filteredIfcFiles.Add(filteredIfcFile);
                }
            }

            return filteredIfcFiles;
        }

        public List<IfcFile> GetElementsWithEmptyValues(List<IfcFile> filteredIfcFiles)
        {
            List<IfcFile> emptyPropertyFiles = new List<IfcFile>();

            foreach (IfcFile ifcFile in filteredIfcFiles)
            {
                IfcFile filteredIfcFile = new IfcFile
                {
                    FilePath = ifcFile.FilePath,
                    IfcElements = new List<IfcElement>()
                };

                foreach (IfcElement ifcElement in ifcFile.IfcElements)
                {
                    IfcElement ifcElementUpdated = new IfcElement
                    {
                        Guid = ifcElement.Guid,
                        IfcEntity = ifcElement.IfcEntity,
                        IfcProperties = new List<IfcProperty>(),
                        Layer = ifcElement.Layer,
                        Tag = ifcElement.Tag
                    };

                    foreach (IfcProperty ifcProperty in ifcElement.IfcProperties)
                    {
                        PropertySetItem propertySetItem = PropertySetItems.FirstOrDefault(propertySetItem => propertySetItem.PropertySetName == ifcProperty.PropertySetName);

                        if (propertySetItem == null)
                        {
                            continue;
                        }

                        PropertyItem propertyItem = propertySetItem.PropertyItems.FirstOrDefault(item => item.PropertyName == ifcProperty.PropertyName);

                        if (propertyItem == null)
                        {
                            continue;
                        }

                        if (string.IsNullOrEmpty(ifcProperty?.Value?.ToString()))
                        {
                            ifcElementUpdated.IfcProperties.Add(ifcProperty);
                        }
                    }

                    if (ifcElementUpdated.IfcProperties.Count > 0)
                    {
                        filteredIfcFile.IfcElements.Add(ifcElementUpdated);
                    }
                }

                emptyPropertyFiles.Add(filteredIfcFile);
            }

            return emptyPropertyFiles;
        }

        public List<IfcFile> GetPicklistCheck(List<IfcFile> ifcFiles, List<PicklistGroup> picklistGroups)
        {
            List<IfcFile> picklistCheckFiles = new List<IfcFile>();

            foreach (IfcFile ifcFile in ifcFiles)
            {
                IfcFile filteredIfcFile = new IfcFile
                {
                    FilePath = ifcFile.FilePath,
                    IfcElements = new List<IfcElement>()
                };

                foreach (IfcElement ifcElement in ifcFile.IfcElements)
                {
                    string guid = ifcElement?.Guid.Value?.ToString();

                    IfcElement ifcElementUpdated = new IfcElement
                    {
                        Guid = guid,
                        IfcEntity = ifcElement.IfcEntity,
                        IfcProperties = new List<IfcProperty>(),
                        Layer = ifcElement.Layer,
                        Tag = ifcElement.Tag
                    };

                    foreach (IfcProperty ifcProperty in ifcElement.IfcProperties)
                    {
                        string ifcPropertyName = ifcProperty.PropertyName;

                        PicklistGroup picklistGroup = picklistGroups.FirstOrDefault(item => item.GroupName == ifcProperty.PropertyName);

                        if (picklistGroup == null)
                        {
                            continue;
                        }

                        PropertySetItem propertySetItem = PropertySetItems.FirstOrDefault(propertySetItem => propertySetItem.PropertySetName == ifcProperty.PropertySetName);

                        if (propertySetItem == null)
                        {
                            continue;
                        }

                        PropertyItem propertyItem = propertySetItem.PropertyItems.FirstOrDefault(item => item.PropertyName == ifcProperty.PropertyName);

                        if (propertyItem == null)
                        {
                            continue;
                        }

                        IfcProperty ifcPropertyUpdated = new IfcProperty
                        {
                            IsValueFromPicklist = false,
                            PropertyName = ifcProperty.PropertyName,
                            PropertySetName = ifcProperty.PropertySetName,
                            Value = ifcProperty.Value,
                        };

                        string value = ifcPropertyUpdated?.Value?.ToString();

                        ifcPropertyUpdated.IsValueFromPicklist = picklistGroup.Values.Contains(value);

                        if (!ifcPropertyUpdated.IsValueFromPicklist)
                        {
                            ifcElementUpdated.IfcProperties.Add(ifcPropertyUpdated);
                        }
                    }

                    if (ifcElementUpdated.IfcProperties.Count > 0)
                    {
                        filteredIfcFile.IfcElements.Add(ifcElementUpdated);
                    }
                }

                picklistCheckFiles.Add(filteredIfcFile);
            }

            return picklistCheckFiles;
        }

        /// <summary>
        /// Finds target properties whose value contradicts the match tables: when every source value of a
        /// match row is present on the element, the target property may only hold one of the allowed values.
        /// </summary>
        public List<IfcFile> GetNonMatchData(List<IfcFile> ifcFiles, List<PropertyValueMatch> propertyValueMatches)
        {
            List<IfcFile> updatedFiles = new List<IfcFile>();

            foreach (IfcFile ifcFile in ifcFiles)
            {
                IfcFile filteredIfcFile = new IfcFile
                {
                    FilePath = ifcFile.FilePath,
                    IfcElements = new List<IfcElement>()
                };

                foreach (IfcElement ifcElement in ifcFile.IfcElements)
                {
                    IfcElement ifcElementUpdated = new IfcElement
                    {
                        Guid = ifcElement.Guid,
                        IfcEntity = ifcElement.IfcEntity,
                        IfcProperties = new List<IfcProperty>(),
                        Layer = ifcElement.Layer,
                        Tag = ifcElement.Tag,
                    };

                    Dictionary<string, string> valuesByName = new Dictionary<string, string>();
                    foreach (IfcProperty property in ifcElement.IfcProperties)
                    {
                        if (!valuesByName.ContainsKey(property.PropertyName))
                        {
                            valuesByName[property.PropertyName] = property.Value?.ToString();
                        }
                    }

                    foreach (IGrouping<string, PropertyValueMatch> targetGroup in propertyValueMatches.GroupBy(match => match.PropertyNameTarget))
                    {
                        List<PropertyValueMatch> applicable = targetGroup
                            .Where(match => match.PropertyNameAndValuesSource.All(source =>
                                valuesByName.TryGetValue(source.Key, out string value) && value == source.Value))
                            .ToList();

                        if (applicable.Count == 0)
                        {
                            continue;
                        }

                        IfcProperty targetProperty = ifcElement.IfcProperties.FirstOrDefault(item => item.PropertyName == targetGroup.Key);
                        if (targetProperty == null)
                        {
                            continue;
                        }

                        string targetValue = targetProperty.Value?.ToString();
                        if (!applicable.Any(match => match.PropertyValueTarget == targetValue))
                        {
                            ifcElementUpdated.IfcProperties.Add(new IfcProperty
                            {
                                PropertyName = targetProperty.PropertyName,
                                PropertySetName = targetProperty.PropertySetName,
                                Value = targetProperty.Value,
                            });
                        }
                    }

                    if (ifcElementUpdated.IfcProperties.Count > 0)
                    {
                        filteredIfcFile.IfcElements.Add(ifcElementUpdated);
                    }
                }

                updatedFiles.Add(filteredIfcFile);
            }

            return updatedFiles;
        }

        public List<IfcFile> GetWrongExpressions(List<IfcFile> ifcFiles, List<ExpressionRule> expressions)
        {
            List<IfcFile> updatedFiles = new List<IfcFile>();

            foreach (IfcFile ifcFile in ifcFiles)
            {
                IfcFile filteredIfcFile = new IfcFile
                {
                    FilePath = ifcFile.FilePath,
                    IfcElements = new List<IfcElement>()
                };

                foreach (IfcElement ifcElement in ifcFile.IfcElements)
                {
                    IfcElement ifcElementUpdated = new IfcElement
                    {
                        Guid = ifcElement.Guid,
                        IfcEntity = ifcElement.IfcEntity,
                        IfcProperties = new List<IfcProperty>(),
                        Layer = ifcElement.Layer,
                        Tag = ifcElement.Tag,
                    };

                    foreach (IfcProperty sourceProperty in ifcElement.IfcProperties)
                    {
                        string assignedSourceValue = sourceProperty?.Value?.ToString();

                        List<ExpressionRule> expressionItems = expressions.Where(expression => expression.SourcePropertyName == sourceProperty.PropertyName).ToList();

                        foreach (ExpressionRule expression in expressionItems)
                        {
                            IfcProperty targetProperty = ifcElement.IfcProperties.FirstOrDefault(item => item.PropertyName == expression.TargetPropertyName);

                            if (targetProperty == null)
                            {
                                continue;
                            }

                            string targetPropertyName = targetProperty.PropertyName;

                            string targetValue = targetProperty?.Value?.ToString();

                            // A rule that cannot be evaluated counts as a finding: the target cannot be proven correct.
                            string evaluatedValueString = ExpressionEngine.TryEvaluate(assignedSourceValue, expression.Function, out object evaluatedValue, out _)
                                ? evaluatedValue?.ToString()
                                : null;

                            if (!string.Equals(targetValue, evaluatedValueString, StringComparison.OrdinalIgnoreCase))
                            {
                                ifcElementUpdated.IfcProperties.Add(sourceProperty);
                                ifcElementUpdated.IfcProperties.Add(targetProperty);
                            }
                        }
                    }

                    if (ifcElementUpdated.IfcProperties.Count > 0)
                    {
                        filteredIfcFile.IfcElements.Add(ifcElementUpdated);
                    }
                }

                updatedFiles.Add(filteredIfcFile);
            }

            return updatedFiles;
        }

        public List<IfcFile> GetWrongLayerMappings(List<IfcFile> filteredIfcFiles, List<LayerMappingItem> layerMappingItems)
        {
            List<IfcFile> updatedFiles = new List<IfcFile>();

            foreach (IfcFile ifcFile in filteredIfcFiles)
            {
                IfcFile updatedFile = new IfcFile
                {
                    IfcElements = new List<IfcElement>(),
                    FilePath = ifcFile.FilePath,
                };

                foreach (IfcElement ifcElement in ifcFile.IfcElements)
                {
                    IfcElement updatedElement = new IfcElement
                    {
                        Guid = ifcElement.Guid,
                        IfcEntity = ifcElement.IfcEntity,
                        IfcProperties = new List<IfcProperty>(),
                        Layer = ifcElement.Layer,
                        Tag = ifcElement.Tag,
                    };

                    string layer = ifcElement.Layer;

                    LayerMappingItem layerMappingItem = layerMappingItems.FirstOrDefault(item => item.LayerName == layer);

                    if (layerMappingItem == null)
                    {
                        continue;
                    }

                    foreach (KeyValuePair<string, string> keyValuePair in layerMappingItem.PropertiesWithValues)
                    {
                        IfcProperty targetProperty = ifcElement.IfcProperties.FirstOrDefault(item => item.PropertyName == keyValuePair.Key);

                        if (targetProperty == null)
                        {
                            continue;
                        }

                        if (!targetProperty.Value.Equals(keyValuePair.Value))
                        {
                            updatedElement.IfcProperties.Add(targetProperty);
                        }
                    }
                }

                updatedFiles.Add(updatedFile);
            }

            return updatedFiles;
        }

        public List<IfcFile> GetElementsWithMissingProperties(List<IfcFile> ifcFiles, List<PropertySetItem> propertySetItems)
        {
            List<IfcFile> missingPropertiesFilesData = new List<IfcFile>();

            List<string> requiredPropertySetNames = propertySetItems.Select(item => item.PropertySetName).ToList();
            List<string> requiredPropertyNames = propertySetItems.SelectMany(item => item.PropertyItems).Select(item => item.PropertyName).ToList();

            foreach (IfcFile ifcFile in IfcFiles)
            {
                IfcFile filteredIfcFile = new IfcFile
                {
                    FilePath = ifcFile.FilePath,
                    IfcElements = new List<IfcElement>()
                };

                foreach (IfcElement ifcElement in ifcFile.IfcElements)
                {
                    string guid = ifcElement.Guid.ToString();

                    IfcElement ifcElementUpdated = new IfcElement
                    {
                        Guid = ifcElement.Guid,
                        IfcEntity = ifcElement.IfcEntity,
                        IfcProperties = new List<IfcProperty>(),
                        Layer = ifcElement.Layer,
                        Tag = ifcElement.Tag
                    };

                    foreach (PropertySetItem propertySetItem in propertySetItems)
                    {
                        foreach (PropertyItem propertyItem in propertySetItem.PropertyItems)
                        {
                            if (!ifcElement.IfcProperties.Any(ifcProperty => ifcProperty.PropertySetName == propertySetItem.PropertySetName && ifcProperty.PropertyName == propertyItem.PropertyName))
                            {
                                IfcProperty emptyProperty = new IfcProperty() { PropertySetName = propertySetItem.PropertySetName, PropertyName = propertyItem.PropertyName };
                                ifcElementUpdated.IfcProperties.Add(emptyProperty);
                            }
                        }
                    }

                    filteredIfcFile.IfcElements.Add(ifcElementUpdated);
                }

                missingPropertiesFilesData.Add(filteredIfcFile);
            }

            return missingPropertiesFilesData;
        }

        public List<IfcFile> GetWrongComposedData(List<IfcFile> ifcFiles, List<ComposedPropertyItem> composedPropertyItems)
        {
            List<IfcFile> ifcFilesWithWrongData = new List<IfcFile>();

            foreach (IfcFile ifcFile in ifcFiles)
            {
                IfcFile ifcFileWithWrongData = new IfcFile
                {
                    FilePath = ifcFile.FilePath,
                    IfcElements = new List<IfcElement>(),
                };


                foreach (IfcElement ifcElement in ifcFile.IfcElements)
                {
                    string tag = ifcElement.Tag;

                    IfcElement ifcElementWithWrongData = new IfcElement
                    {
                        Guid = ifcElement.Guid,
                        IfcEntity = ifcElement.IfcEntity,
                        Layer = ifcElement.Layer,
                        Tag = tag,
                        IfcProperties = new List<IfcProperty>(),
                    };

                    foreach (ComposedPropertyItem composedPropertyItem in composedPropertyItems)
                    {
                        List<string> sourceParameterNames = ComposedItemEvaluator.GetPropertyNames(composedPropertyItem.Formula);
                        string targetParameterName = composedPropertyItem.ComposedPropertyName;

                        IfcProperty targetProperty = ifcElement.IfcProperties.FirstOrDefault(item => item.PropertyName == targetParameterName);

                        if (targetProperty == null)
                        {
                            continue;
                        }

                        List<IfcProperty> sourceProperties = ifcElement.IfcProperties.Where(item => sourceParameterNames.Contains(item.PropertyName)).ToList();

                        string composedValue = ComposedItemEvaluator.Resolve(composedPropertyItem, sourceProperties.ToDictionary(item => item.PropertyName, item => item.Value?.ToString()));

                        if (targetProperty.Value?.ToString() != composedValue)
                        {
                            if (!ifcElementWithWrongData.IfcProperties.Any(property => property.PropertySetName == targetProperty.PropertySetName && property.PropertyName == targetProperty.PropertyName))
                            {
                                ifcElementWithWrongData.IfcProperties.Add(targetProperty);
                            }
                        }
                    }

                    if (ifcElementWithWrongData.IfcProperties.Any())
                    {
                        ifcFileWithWrongData.IfcElements.Add(ifcElementWithWrongData);
                    }
                }

                if (ifcFileWithWrongData.IfcElements.Any())
                {
                    ifcFilesWithWrongData.Add(ifcFileWithWrongData);
                }
            }

            return ifcFilesWithWrongData;
        }
    }
}
