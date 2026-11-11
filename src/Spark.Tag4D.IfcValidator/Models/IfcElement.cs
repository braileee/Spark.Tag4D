using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Ifc4.UtilityResource;

namespace Spark.Tag4D.IfcValidator.Models
{
    public class IfcElement
    {
        public List<IfcProperty> IfcProperties { get; set; } = new List<IfcProperty>();
        public IfcGloballyUniqueId Guid { get; internal set; }
        public string IfcEntity { get; internal set; }
        public string Tag { get; internal set; }
        public string Layer { get; internal set; }
    }
}
