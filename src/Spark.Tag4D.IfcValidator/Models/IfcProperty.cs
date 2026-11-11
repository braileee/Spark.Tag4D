using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.IfcValidator.Models
{
    public class IfcProperty
    {
        public string PropertySetName { get; set; }
        public string PropertyName { get; set; }
        public object Value { get; set; }
        public bool IsValueFromPicklist { get; internal set; }
    }
}
