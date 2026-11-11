using Spark.Tag4D.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Revit.App.Models
{
    public class ParametersExcelToRevitOptions
    {
        public List<PropertySetItem> PropertySetItems { get; set; } = new List<PropertySetItem>();
    }
}
