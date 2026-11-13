using Spark.Tag4D.Core.Models;
using System.Collections.Generic;

namespace Spark.Tag4D.Revit.App.Models
{
    public enum ParametersOperation
    {
        Load,
        Delete,
        /// <summary>Deletes <see cref="ParametersOperationOptions.PropertySetItemsToDelete"/>, then creates <see cref="ParametersOperationOptions.PropertySetItems"/>.</summary>
        Synchronize
    }

    public class ParametersOperationOptions
    {
        public ParametersOperation Operation { get; set; }

        public List<PropertySetItem> PropertySetItems { get; set; } = new List<PropertySetItem>();

        public List<PropertySetItem> PropertySetItemsToDelete { get; set; } = new List<PropertySetItem>();

        public OperationProgress Progress { get; set; }
    }
}
