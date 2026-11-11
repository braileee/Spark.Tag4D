using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Spark.Tag4D.Core.Expressions
{
    /// <summary>
    /// Tiny, safe expression language used by the "Expressions" sheet to derive one property from another.
    /// The source property value is the implicit input. Supported forms:
    /// <list type="bullet">
    /// <item><c>==TMP</c> / <c>!=TMP</c> - compare the value (case-insensitive) and yield True/False.</item>
    /// <item><c>.Split('.')[0]</c> - a chain of member calls and indexers, evaluated left to right.</item>
    /// </list>
    /// Chain members: Split, Substring, Left, Right, Trim, TrimStart, TrimEnd, ToUpper, ToLower, Replace,
    /// Remove, PadLeft, PadRight, Length, Contains, StartsWith, EndsWith, IndexOf, Equals.
    /// Nothing is compiled or executed dynamically, so user supplied expressions cannot run arbitrary code.
    /// </summary>
    public static class ExpressionEngine
    {
        /// <summary>Evaluates <paramref name="expression"/> against <paramref name="sourceValue"/>.</summary>
        /// <exception cref="ExpressionException">The expression is malformed or does not apply to the value.</exception>
        public static object Evaluate(string? sourceValue, string? expression)
        {
            string value = sourceValue ?? string.Empty;
            string text = (expression ?? string.Empty).Trim();

            if (text.Length == 0)
            {
                return value;
            }

            if (text.StartsWith("==", StringComparison.Ordinal))
            {
                return Compare(value, text.Substring(2));
            }

            if (text.StartsWith("!=", StringComparison.Ordinal))
            {
                return !Compare(value, text.Substring(2));
            }

            return new ChainEvaluator(text).Run(value);
        }

        /// <summary>Evaluates an expression without throwing; <paramref name="error"/> describes any failure.</summary>
        public static bool TryEvaluate(string? sourceValue, string? expression, out object? result, out string? error)
        {
            try
            {
                result = Evaluate(sourceValue, expression);
                error = null;
                return true;
            }
            catch (ExpressionException exception)
            {
                result = null;
                error = exception.Message;
                return false;
            }
        }

        private static bool Compare(string value, string operand)
        {
            string expected = Unquote(operand.Trim());
            return string.Equals(value.Trim(), expected, StringComparison.OrdinalIgnoreCase);
        }

        private static string Unquote(string text)
        {
            if (text.Length >= 2 && (text[0] == '\'' || text[0] == '"') && text[text.Length - 1] == text[0])
            {
                return text.Substring(1, text.Length - 2);
            }

            return text;
        }

        private sealed class ChainEvaluator
        {
            private readonly string _text;
            private int _position;

            public ChainEvaluator(string text)
            {
                _text = text;
            }

            public object Run(string input)
            {
                object current = input;

                while (_position < _text.Length)
                {
                    SkipWhitespace();
                    char next = _text[_position];

                    if (next == '.')
                    {
                        _position++;
                        current = ApplyMember(current);
                    }
                    else if (next == '[')
                    {
                        current = ApplyIndexer(current);
                    }
                    else
                    {
                        throw Error($"Unexpected '{next}'");
                    }

                    SkipWhitespace();
                }

                if (current is string[] array)
                {
                    throw Error($"Expression yields a list ({array.Length} items); add an index such as [0]");
                }

                return current;
            }

            private object ApplyIndexer(object current)
            {
                _position++; // [
                object index = ReadLiteral();
                SkipWhitespace();
                Expect(']');

                if (!(index is int position))
                {
                    throw Error("Index must be a whole number");
                }

                switch (current)
                {
                    case string[] array:
                        if (position < 0 || position >= array.Length)
                        {
                            throw Error($"Index {position} is outside 0..{array.Length - 1}");
                        }

                        return array[position];
                    case string text:
                        if (position < 0 || position >= text.Length)
                        {
                            throw Error($"Index {position} is outside 0..{text.Length - 1}");
                        }

                        return text[position].ToString();
                    default:
                        throw Error("Indexer can only be applied to text or a list");
                }
            }

            private object ApplyMember(object current)
            {
                string name = ReadIdentifier();
                List<object> args = new List<object>();

                SkipWhitespace();
                if (_position < _text.Length && _text[_position] == '(')
                {
                    _position++;
                    SkipWhitespace();

                    while (_position < _text.Length && _text[_position] != ')')
                    {
                        args.Add(ReadLiteral());
                        SkipWhitespace();

                        if (_position < _text.Length && _text[_position] == ',')
                        {
                            _position++;
                            SkipWhitespace();
                        }
                    }

                    Expect(')');
                }

                if (!(current is string text))
                {
                    throw Error($"'{name}' cannot be applied to a non-text result");
                }

                try
                {
                    return Invoke(text, name.ToLowerInvariant(), args);
                }
                catch (ArgumentException exception)
                {
                    throw Error($"'{name}' failed: {exception.Message}");
                }
            }

            private object Invoke(string text, string name, List<object> args)
            {
                switch (name)
                {
                    case "split":
                        return text.Split(new[] { Text(args, 0) }, args.Count > 1 ? Int(args, 1) : int.MaxValue, StringSplitOptions.None);
                    case "substring":
                        return args.Count > 1 ? text.Substring(Int(args, 0), Int(args, 1)) : text.Substring(Int(args, 0));
                    case "left":
                        return text.Substring(0, Math.Min(Int(args, 0), text.Length));
                    case "right":
                        return text.Substring(Math.Max(0, text.Length - Int(args, 0)));
                    case "trim":
                        return text.Trim();
                    case "trimstart":
                        return text.TrimStart();
                    case "trimend":
                        return text.TrimEnd();
                    case "toupper":
                    case "toupperinvariant":
                        return text.ToUpperInvariant();
                    case "tolower":
                    case "tolowerinvariant":
                        return text.ToLowerInvariant();
                    case "replace":
                        return text.Replace(Text(args, 0), args.Count > 1 ? Text(args, 1) : string.Empty);
                    case "remove":
                        return args.Count > 1 ? text.Remove(Int(args, 0), Int(args, 1)) : text.Remove(Int(args, 0));
                    case "padleft":
                        return text.PadLeft(Int(args, 0), args.Count > 1 ? Text(args, 1)[0] : ' ');
                    case "padright":
                        return text.PadRight(Int(args, 0), args.Count > 1 ? Text(args, 1)[0] : ' ');
                    case "length":
                        return text.Length;
                    case "contains":
                        return text.IndexOf(Text(args, 0), StringComparison.OrdinalIgnoreCase) >= 0;
                    case "startswith":
                        return text.StartsWith(Text(args, 0), StringComparison.OrdinalIgnoreCase);
                    case "endswith":
                        return text.EndsWith(Text(args, 0), StringComparison.OrdinalIgnoreCase);
                    case "indexof":
                        return text.IndexOf(Text(args, 0), StringComparison.OrdinalIgnoreCase);
                    case "equals":
                        return string.Equals(text, Text(args, 0), StringComparison.OrdinalIgnoreCase);
                    default:
                        throw Error($"Unknown member '{name}'");
                }
            }

            private string Text(List<object> args, int index)
            {
                if (index >= args.Count)
                {
                    throw Error("Missing argument");
                }

                return Convert.ToString(args[index], CultureInfo.InvariantCulture) ?? string.Empty;
            }

            private int Int(List<object> args, int index)
            {
                if (index >= args.Count || !(args[index] is int number))
                {
                    throw Error("Expected a whole-number argument");
                }

                return number;
            }

            private object ReadLiteral()
            {
                SkipWhitespace();
                if (_position >= _text.Length)
                {
                    throw Error("Unexpected end of expression");
                }

                char first = _text[_position];

                if (first == '\'' || first == '"')
                {
                    return ReadString(first);
                }

                int start = _position;
                if (first == '-')
                {
                    _position++;
                }

                while (_position < _text.Length && char.IsDigit(_text[_position]))
                {
                    _position++;
                }

                string number = _text.Substring(start, _position - start);
                if (!int.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                {
                    throw Error($"Invalid argument at position {start + 1}");
                }

                return value;
            }

            private string ReadString(char quote)
            {
                _position++; // opening quote
                var builder = new StringBuilder();

                while (_position < _text.Length)
                {
                    char current = _text[_position++];

                    if (current == '\\' && _position < _text.Length)
                    {
                        builder.Append(_text[_position++]);
                    }
                    else if (current == quote)
                    {
                        return builder.ToString();
                    }
                    else
                    {
                        builder.Append(current);
                    }
                }

                throw Error("Unterminated string");
            }

            private string ReadIdentifier()
            {
                int start = _position;
                while (_position < _text.Length && (char.IsLetterOrDigit(_text[_position]) || _text[_position] == '_'))
                {
                    _position++;
                }

                if (start == _position)
                {
                    throw Error("Member name expected after '.'");
                }

                return _text.Substring(start, _position - start);
            }

            private void Expect(char expected)
            {
                if (_position >= _text.Length || _text[_position] != expected)
                {
                    throw Error($"'{expected}' expected");
                }

                _position++;
            }

            private void SkipWhitespace()
            {
                while (_position < _text.Length && char.IsWhiteSpace(_text[_position]))
                {
                    _position++;
                }
            }

            private ExpressionException Error(string message) =>
                new ExpressionException($"{message} (in expression '{_text}')");
        }
    }
}
