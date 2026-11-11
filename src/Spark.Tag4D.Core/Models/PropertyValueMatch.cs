using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Core.Models
{
    public class PropertyValueMatch
    {

        public Dictionary<string, string> PropertyNameAndValuesSource { get; set; } = new Dictionary<string, string>();

        public string PropertyNameTarget { get; set; }
        public string PropertyValueTarget { get; set; }
    }
}
