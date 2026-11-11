using System.Text.Json;

namespace Spark.Tag4D.Core.Configuration
{
    internal static class JsonOptions
    {
        /// <summary>Lenient reading: case-insensitive names, comments and trailing commas allowed.</summary>
        public static readonly JsonSerializerOptions Tolerant = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        public static readonly JsonSerializerOptions Indented = new JsonSerializerOptions
        {
            WriteIndented = true
        };
    }
}
