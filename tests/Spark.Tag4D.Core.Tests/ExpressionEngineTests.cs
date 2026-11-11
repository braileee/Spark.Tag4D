using Spark.Tag4D.Core.Expressions;
using Xunit;

namespace Spark.Tag4D.Core.Tests
{
    public class ExpressionEngineTests
    {
        [Theory]
        [InlineData("EAR.EXC", ".Split('.')[0]", "EAR")]
        [InlineData("EAR.EXC", ".Split('.')[1]", "EXC")]
        [InlineData("  abc ", ".Trim().ToUpper()", "ABC")]
        [InlineData("abcdef", ".Substring(2, 3)", "cde")]
        [InlineData("abcdef", ".Left(2)", "ab")]
        [InlineData("abcdef", ".Right(2)", "ef")]
        [InlineData("a-b", ".Replace('-', '_')", "a_b")]
        [InlineData("7", ".PadLeft(3, '0')", "007")]
        [InlineData("abc", "", "abc")]
        public void Evaluates_text_chains(string input, string expression, string expected)
        {
            Assert.Equal(expected, ExpressionEngine.Evaluate(input, expression));
        }

        [Fact]
        public void Equality_shortcuts_yield_booleans()
        {
            Assert.Equal(true, ExpressionEngine.Evaluate("tmp", "==TMP"));
            Assert.Equal(false, ExpressionEngine.Evaluate("PERM", "==TMP"));
            Assert.Equal(true, ExpressionEngine.Evaluate("PERM", "!=TMP"));
        }

        [Fact]
        public void Length_and_predicates_are_typed()
        {
            Assert.Equal(3, ExpressionEngine.Evaluate("abc", ".Length"));
            Assert.Equal(true, ExpressionEngine.Evaluate("abc", ".StartsWith('A')"));
        }

        [Theory]
        [InlineData(".Split('.')")]
        [InlineData(".Nope()")]
        [InlineData(".Split('.')[9]")]
        [InlineData(".Substring(x)")]
        [InlineData("Split")]
        [InlineData(".Replace('a)")]
        public void Rejects_invalid_expressions(string expression)
        {
            Assert.Throws<ExpressionException>(() => ExpressionEngine.Evaluate("a.b", expression));
        }

        [Fact]
        public void TryEvaluate_reports_errors_without_throwing()
        {
            Assert.False(ExpressionEngine.TryEvaluate("a", ".Boom()", out object? result, out string? error));
            Assert.Null(result);
            Assert.Contains("Boom", error);
        }

        [Fact]
        public void Cannot_execute_arbitrary_code()
        {
            Assert.Throws<ExpressionException>(() =>
                ExpressionEngine.Evaluate("x", ".GetType().Assembly.GetType('System.Environment').Exit(1)"));
        }
    }
}
