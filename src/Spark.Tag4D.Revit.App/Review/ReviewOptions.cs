using System;
using System.Collections.Generic;

namespace Spark.Tag4D.Revit.App.Review
{
    public enum ReviewOperation
    {
        /// <summary>Isolate the elements with a missing value in the Tag 4D 3D view.</summary>
        Show,

        /// <summary>Remove the isolation again.</summary>
        Reset
    }

    public class ReviewOptions
    {
        public ReviewOperation Operation { get; set; }

        public List<string> PropertyNames { get; set; } = new List<string>();

        /// <summary>Receives a one line result (count of elements, or what went wrong).</summary>
        public Action<string> OnResult { get; set; }
    }
}
