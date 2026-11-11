using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Core.Models
{
    public class ComposerPropertyResult
    {
        public bool CanBeComposed { get; set; }

        public string Value { get; set; }
        public PropertyField ComposingField { get; internal set; }
    }
}
