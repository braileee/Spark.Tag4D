using Spark.Tag4D.Core.Models;
using Autodesk.Revit.DB;
using Spark.Tag4D.Revit.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NPOI.HSSF.UserModel.HeaderFooter;

namespace Spark.Tag4D.Revit.App.Services
{
    public class FieldsRefresh
    {
        public FieldsRefresh(List<PropertyField> fields, List<Element> elements )
        {
            Fields = fields;
            Elements = elements;
        }

        public List<PropertyField> Fields { get; } = new List<PropertyField>();
        public List<Element> Elements { get; } = new List<Element>();

        public void Start()
        {
            string variesValue = "***VARIES***";

            foreach (PropertyField propertyField in Fields)
            {
                EditorType editorType = propertyField.EditorType;
                Element firstElement = Elements.FirstOrDefault();

                string propertyName = propertyField.Name;

                object firstElementValue = firstElement?.LookupParameter(propertyField.Name)?.GetValueAsObject();
                object firstElementValueString = firstElementValue?.ToString() ?? string.Empty;

                bool allValuesSame = Elements.Where(item => item.LookupParameter(propertyField.Name) != null).All(element =>
                {
                    string currentStringValue = element.LookupParameter(propertyField.Name)?.GetValueAsObject()?.ToString();
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
                        if (propertyField.LookupValues.Contains(variesValue))
                        {
                            propertyField.LookupValues.Remove(variesValue);
                        }
                    }
                }
                else
                {
                    if (propertyField.EditorType == EditorType.Combo)
                    {
                        if (!propertyField.LookupValues.Contains(variesValue))
                        {
                            propertyField.LookupValues.Add(variesValue);
                        }
                    }

                    propertyField.Value = variesValue;
                }

                propertyField.Changed = false;
            }
        }
    }
}
