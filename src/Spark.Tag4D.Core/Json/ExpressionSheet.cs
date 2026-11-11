using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Core.Json
{
    public sealed class ExpressionSheet : OneSheetBase
    {
        public string SourcePropertyColumnName { get; set; }
        public string TargetPropertyColumnName { get; set; }
        public string FunctionColumnName { get; set; }
        public string ValueColumnName { get; set; }

        public ExpressionSheet()
        {
            SourcePropertyColumnName = string.Empty;
            TargetPropertyColumnName = string.Empty;
            FunctionColumnName = string.Empty;
            ValueColumnName = string.Empty;
        }
    }
}
