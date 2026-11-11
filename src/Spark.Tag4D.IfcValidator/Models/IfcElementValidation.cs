using Spark.Tag4D.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace Spark.Tag4D.IfcValidator.Models
{
    public class IfcElementValidation
    {
        public IfcElement IfcElement { get; set; } = new IfcElement();

        public List<string> GetMissingPropertyNames(List<PropertySetItem> propertySetItems)
        {
            List<string> requiredParameterNames = propertySetItems.SelectMany(p => p.PropertyItems).Select(item => item.PropertyName).ToList();
            List<string> existingParametersNames = IfcElement.IfcProperties.Select(p => p.PropertyName).ToList();
            List<string> missingParameterNames = requiredParameterNames.Except(existingParametersNames).ToList();
            
            return missingParameterNames;
        }

        public List<string> GetPropertyNamesWithMissingValues(List<PropertySetItem> propertySetItems)
        {
            List<string> requiredParameterNames = propertySetItems.SelectMany(p => p.PropertyItems).Select(item => item.PropertyName).ToList();
            List<string> existingParametersNamesWithMissingValues = IfcElement.IfcProperties.Where(p => requiredParameterNames.Contains(p.PropertyName) && string.IsNullOrEmpty(p.Value?.ToString())).Select(p => p.PropertyName).ToList();
            return existingParametersNamesWithMissingValues;
        }

        public Dictionary<string, string> GetInvalidPicklistNameAndValues(List<PicklistGroup> picklistGroups)
        {
            Dictionary<string, string> nonPickListValues = new Dictionary<string, string>();
            foreach (var property in IfcElement.IfcProperties)
            {
                var picklistGroup = picklistGroups.FirstOrDefault(g => g.GroupName == property.PropertyName);
                if (picklistGroup != null && !picklistGroup.Values.Contains(property.Value?.ToString()))
                {
                    nonPickListValues[property.PropertyName] = property.Value?.ToString() ?? string.Empty;
                }
            }
            return nonPickListValues;
        }
    }
}
