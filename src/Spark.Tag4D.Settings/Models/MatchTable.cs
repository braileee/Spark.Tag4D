using System.Collections.Generic;

namespace Spark.Tag4D.Settings.Models
{
    public class MatchTable
    {
        public string SheetPrefix { get; set; }
            = string.Empty;

        public List<string> SourceProperties { get; set; }
            = new();

        public string TargetProperty { get; set; }
            = string.Empty;

        public List<MatchRow> Rows { get; set; }
            = new();
        public List<string> AvailableProperties { get; set; } = new List<string>();
    }
}