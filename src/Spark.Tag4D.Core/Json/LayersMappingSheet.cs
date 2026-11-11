using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Core.Json
{
    public sealed class LayersMappingSheet : OneSheetBase
    {
        public string LayerColumnName { get; set; }

        public LayersMappingSheet()
        {
            LayerColumnName = string.Empty;
        }
    }
}
