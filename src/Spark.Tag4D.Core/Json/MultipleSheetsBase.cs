using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Core.Json
{
    public abstract class MultipleSheetBase : SheetBase
    {
        public List<string> SheetNames { get; set; }

        protected MultipleSheetBase()
        {
            SheetNames = new List<string>();
        }
    }
}
