using System.Collections.Generic;

namespace Spark.Tag4D.Settings.Models
{
    public class ExactMatchTable
    {
        public string SheetPrefix { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string SourceColumnHeader { get; set; }

        public string TargetColumnHeader { get; set; } 

        public List<ExactMatchRow> Rows { get; set; } = new();
        public List<string> AvailableColumnNames { get; set; } = new List<string>();
    }
}