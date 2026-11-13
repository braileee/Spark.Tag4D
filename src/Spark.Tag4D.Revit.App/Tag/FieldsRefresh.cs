using Spark.Tag4D.Core.Models;
using Autodesk.Revit.DB;
using Spark.Tag4D.Revit.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Revit.App.Tag
{
    public class FieldsRefresh
    {
        public FieldsRefresh(List<PropertyField> fields, List<Element> elements, ParameterCache cache = null)
        {
            Fields = fields;
            Elements = elements;
            this.cache = cache ?? new ParameterCache();
        }

        private readonly ParameterCache cache;

        public List<PropertyField> Fields { get; } = new List<PropertyField>();
        public List<Element> Elements { get; } = new List<Element>();

        public void Start()
        {

            foreach (PropertyField propertyField in Fields)
            {
                EditorType editorType = propertyField.EditorType;
                Element firstElement = Elements.FirstOrDefault();

                string propertyName = propertyField.Name;

                object firstElementValue = cache.Get(firstElement, propertyField.Name)?.GetValueAsObject();
                object firstElementValueString = firstElementValue?.ToString() ?? string.Empty;

                bool allValuesSame = Elements.Where(item => cache.Get(item, propertyField.Name) != null).All(element =>
                {
                    string currentStringValue = cache.Get(element, propertyField.Name)?.GetValueAsObject()?.ToString();
                    currentStringValue = currentStringValue ?? string.Empty;

                    return currentStringValue.Equals(firstElementValueString);
                });

                if (allValuesSame)
                {
                    if (propertyField.EditorType == EditorType.Bool)
                    {
                        propertyField.Value = firstElementValue?.ToString() == "1" ? true : false;
                    }
                    else
                    {
                        propertyField.Value = firstElementValue;
                    }

                    if (propertyField.EditorType == EditorType.Combo)
                    {
                        if (propertyField.LookupValues.Contains(PropertyField.VariesValue))
                        {
                            propertyField.LookupValues.Remove(PropertyField.VariesValue);
                        }
                    }
                }
                else
                {
                    if (propertyField.EditorType == EditorType.Combo)
                    {
                        if (!propertyField.LookupValues.Contains(PropertyField.VariesValue))
                        {
                            propertyField.LookupValues.Add(PropertyField.VariesValue);
                        }
                    }

                    propertyField.Value = PropertyField.VariesValue;
                }

                propertyField.Changed = false;
            }
        }
    }
}
