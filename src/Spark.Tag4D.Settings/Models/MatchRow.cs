using System.Collections.Generic;

namespace Spark.Tag4D.Settings.Models
{
    public class MatchRow
    {
        public List<MatchCriterion> Criteria { get; set; }
            = new();

        public string TargetValue { get; set; }
            = string.Empty;
    }
}