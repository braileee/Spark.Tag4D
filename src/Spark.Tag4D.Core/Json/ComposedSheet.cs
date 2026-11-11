using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Core.Json
{
    public sealed class ComposedSheet : OneSheetBase
    {
        public string ComposedPropertyColumnName { get; set; }
        public string FormulaColumnName { get; set; }

        public ComposedSheet()
        {
            SheetName = string.Empty;
            ComposedPropertyColumnName = string.Empty;
            FormulaColumnName = string.Empty;
        }
    }
}
