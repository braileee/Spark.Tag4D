using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace Spark.Tag4D.Revit.App.Infrastructure
{
    /// <summary>
    /// Element.LookupParameter walks all parameters of the element on every call. The tagging code asks for the same
    /// (element, name) pairs many times, so each answer is looked up once and remembered for the length of one operation.
    /// </summary>
    public sealed class ParameterCache
    {
        private readonly Dictionary<ElementId, Dictionary<string, Parameter>> byElement = new Dictionary<ElementId, Dictionary<string, Parameter>>();

        public Parameter Get(Element element, string name)
        {
            if (element == null || name == null)
            {
                return null;
            }

            if (!byElement.TryGetValue(element.Id, out Dictionary<string, Parameter> parameters))
            {
                byElement[element.Id] = parameters = new Dictionary<string, Parameter>();
            }

            if (!parameters.TryGetValue(name, out Parameter parameter))
            {
                parameters[name] = parameter = element.LookupParameter(name);
            }

            return parameter;
        }
    }
}
