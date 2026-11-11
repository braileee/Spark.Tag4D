using Spark.Tag4D.Settings.ViewModels.Support;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Spark.Tag4D.Settings.Services
{
    public static class FormulaParser
    {
        public static List<FormulaSegment> Parse(string formula)
        {
            var result = new List<FormulaSegment>();

            var matches = Regex.Matches(formula, @"<([^>]+)>");

            for (int i = 0; i < matches.Count; i++)
            {
                var current = matches[i];

                string prefix = string.Empty;
                string suffix = string.Empty;

                if (i == 0)
                {
                    prefix = formula.Substring(0, current.Index);
                }

                if (i < matches.Count - 1)
                {
                    var next = matches[i + 1];

                    int start = current.Index + current.Length;
                    int length = next.Index - start;

                    suffix = formula.Substring(start, length);
                }
                else
                {
                    int start = current.Index + current.Length;

                    if (start < formula.Length)
                        suffix = formula.Substring(start);
                }

                result.Add(new FormulaSegment
                {
                    Prefix = prefix,
                    SelectedProperty = current.Groups[1].Value,
                    Suffix = suffix
                });
            }

            return result;
        }

        public static string Build(IEnumerable<FormulaSegment> segments)
        {
            var sb = new StringBuilder();

            foreach (var segment in segments)
            {
                sb.Append(segment.Prefix);
                sb.Append($"<{segment.SelectedProperty}>");
                sb.Append(segment.Suffix);
            }

            return sb.ToString();
        }
    }
}