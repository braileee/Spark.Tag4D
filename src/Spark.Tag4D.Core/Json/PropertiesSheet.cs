using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Core.Json
{
    public sealed class PropertiesSheet : OneSheetBase
    {
        public string PropertySetNameColumn { get; set; }
        public string PropertyNameColumn { get; set; }
        public string DataTypeColumn { get; set; }

        public PropertiesSheet()
        {
            PropertySetNameColumn = string.Empty;
            PropertyNameColumn = string.Empty;
            DataTypeColumn = string.Empty;
        }
    }
}
