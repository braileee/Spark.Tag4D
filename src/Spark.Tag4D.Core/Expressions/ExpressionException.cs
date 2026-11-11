using System;

namespace Spark.Tag4D.Core.Expressions
{
    /// <summary>Raised when an expression cannot be parsed or evaluated.</summary>
    public sealed class ExpressionException : Exception
    {
        public ExpressionException(string message)
            : base(message)
        {
        }
    }
}
