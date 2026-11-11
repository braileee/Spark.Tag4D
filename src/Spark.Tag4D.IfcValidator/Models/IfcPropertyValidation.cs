using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.IfcValidator.Models
{
    public class IfcPropertyValidation
    {
        public IfcProperty IfcProperty { get; set; }


        public bool IsValueEmpty
        {
            get
            {
                return IfcProperty.Value == null || (IfcProperty.Value is string str && string.IsNullOrEmpty(str));
            }
        }
    }
}
