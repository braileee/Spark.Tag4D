using Spark.Tag4D.Core.Enums;
using Spark.Tag4D.Core.Models;
using NPOI.HPSF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using Xbim.Ifc2x3.MeasureResource;

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

                    foreach (IfcProperty ifcProperty in ifcElement.IfcProperties)
                    {
                        string assignedSourceValue = ifcProperty?.Value?.ToString();

                        List<PropertyValueMatch> sourcePropertyValueMatches = propertyValueMatches.Where(item => item.PropertyNameSource == ifcProperty.PropertyName && item.PropertyValueSource == assignedSourceValue).ToList();

                        if (sourcePropertyValueMatches.Count == 0)
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


                        IfcProperty ifcPropertyTarget = ifcElement.IfcProperties.FirstOrDefault(item => item.PropertyName == sourcePropertyValueMatches[0].PropertyNameTarget);
                        string assignedTargetProperty = ifcPropertyTarget?.Value?.ToString();

                        IfcProperty ifcPropertyUpdated = new IfcProperty
                        {
                            PropertyName = ifcPropertyTarget.PropertyName,
                            PropertySetName = ifcPropertyTarget.PropertySetName,
                            Value = ifcPropertyTarget.Value,
                        };

                        List<string> sourceValues = sourcePropertyValueMatches.Select(item => item.PropertyValueSource).ToList();
                        List<string> targetValues = sourcePropertyValueMatches.Select(item => item.PropertyValueTarget).ToList();

                        if (!targetValues.Contains(assignedTargetProperty))
                        {
                            ifcElementUpdated.IfcProperties.Add(ifcPropertyUpdated);
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

                            object evaluatedValue = ExpressionEvaluator.Evaluate(expression, sourceProperty?.Value);
                            string evaluatedValueString = evaluatedValue?.ToString();

                            if (targetValue != evaluatedValueString)
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
