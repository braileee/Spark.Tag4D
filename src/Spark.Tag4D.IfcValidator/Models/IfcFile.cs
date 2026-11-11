using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.IfcValidator.Models
{
    public class IfcFile
    {
        public string FilePath { get; set; }

        public List<IfcElement> IfcElements { get; set; } = new List<IfcElement>();
    }
}
