using Spark.Tag4D.Core.Enums;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Core.Models
{
    public class ExpressionEvaluator
    {
        public static object Evaluate(
        string sourceValue,
        string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return sourceValue;

            string escapedValue = sourceValue
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");

            string code = $@"
                        var value = ""{escapedValue}"";
                        return value{expression};
                        ";

            return CSharpScript.EvaluateAsync<object>(code);
        }
    }
}
