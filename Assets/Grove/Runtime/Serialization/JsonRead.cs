using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Grove.Domain.Serialization
{
    /// <summary>
    /// Small JSON reader for Grove catalogs. Domain stays free of UnityEngine and Newtonsoft.
    /// Supports objects, arrays, strings, numbers, bools, and null.
    /// </summary>
    internal static class JsonRead
    {
        public static JsonNode Parse(string json)
        {
            if (json is null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            var parser = new Parser(json);
            var node = parser.ParseValue();
            parser.SkipWs();
            if (!parser.Eof)
            {
                throw parser.Error("Trailing JSON content.");
            }

            return node;
        }

        private sealed class Parser
        {
            private readonly string _text;
            private int _i;

            public Parser(string text)
            {
                _text = text;
                if (_text.Length > 0 && _text[0] == '\uFEFF')
                {
                    _i = 1;
                }
            }

            public bool Eof => _i >= _text.Length;

            public JsonNode ParseValue()
            {
                SkipWs();
                if (Eof)
                {
                    throw Error("Unexpected end of JSON.");
                }

                var c = _text[_i];
                return c switch
                {
                    '{' => ParseObject(),
                    '[' => ParseArray(),
                    '"' => new JsonNode.Str(ParseString()),
                    't' => ParseLiteral("true", JsonNode.Bool.True),
                    'f' => ParseLiteral("false", JsonNode.Bool.False),
                    'n' => ParseLiteral("null", JsonNode.Null.Instance),
                    '-' or >= '0' and <= '9' => ParseNumber(),
                    _ => throw Error($"Unexpected character '{c}'.")
                };
            }

            private JsonNode ParseObject()
            {
                Expect('{');
                var obj = new JsonNode.Obj();
                SkipWs();
                if (Try('}'))
                {
                    return obj;
                }

                while (true)
                {
                    SkipWs();
                    if (Eof || _text[_i] != '"')
                    {
                        throw Error("Expected object key.");
                    }

                    var key = ParseString();
                    SkipWs();
                    Expect(':');
                    var value = ParseValue();
                    if (!obj.Fields.TryAdd(key, value))
                    {
                        throw Error($"Duplicate key '{key}'.");
                    }

                    SkipWs();
                    if (Try('}'))
                    {
                        return obj;
                    }

                    Expect(',');
                }
            }

            private JsonNode ParseArray()
            {
                Expect('[');
                var arr = new JsonNode.Arr();
                SkipWs();
                if (Try(']'))
                {
                    return arr;
                }

                while (true)
                {
                    arr.Items.Add(ParseValue());
                    SkipWs();
                    if (Try(']'))
                    {
                        return arr;
                    }

                    Expect(',');
                }
            }

            private string ParseString()
            {
                Expect('"');
                var sb = new StringBuilder();
                while (!Eof)
                {
                    var c = _text[_i++];
                    if (c == '"')
                    {
                        return sb.ToString();
                    }

                    if (c == '\\')
                    {
                        if (Eof)
                        {
                            throw Error("Unterminated string escape.");
                        }

                        var e = _text[_i++];
                        sb.Append(e switch
                        {
                            '"' => '"',
                            '\\' => '\\',
                            '/' => '/',
                            'b' => '\b',
                            'f' => '\f',
                            'n' => '\n',
                            'r' => '\r',
                            't' => '\t',
                            'u' => ParseUnicode(),
                            _ => throw Error($"Invalid escape '\\{e}'.")
                        });
                        continue;
                    }

                    if (c < 0x20)
                    {
                        throw Error("Unescaped control character in string.");
                    }

                    sb.Append(c);
                }

                throw Error("Unterminated string.");
            }

            private char ParseUnicode()
            {
                if (_i + 4 > _text.Length)
                {
                    throw Error("Invalid \\u escape.");
                }

                var hex = _text.Substring(_i, 4);
                _i += 4;
                if (!ushort.TryParse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var code))
                {
                    throw Error($"Invalid \\u escape '{hex}'.");
                }

                return (char)code;
            }

            private JsonNode ParseNumber()
            {
                var start = _i;
                if (Try('-'))
                {
                    // sign
                }

                if (Eof)
                {
                    throw Error("Invalid number.");
                }

                if (_text[_i] == '0')
                {
                    _i++;
                }
                else if (_text[_i] is >= '1' and <= '9')
                {
                    while (!Eof && _text[_i] is >= '0' and <= '9')
                    {
                        _i++;
                    }
                }
                else
                {
                    throw Error("Invalid number.");
                }

                if (!Eof && _text[_i] == '.')
                {
                    _i++;
                    var frac = _i;
                    while (!Eof && _text[_i] is >= '0' and <= '9')
                    {
                        _i++;
                    }

                    if (_i == frac)
                    {
                        throw Error("Invalid number fraction.");
                    }
                }

                if (!Eof && _text[_i] is 'e' or 'E')
                {
                    _i++;
                    if (!Eof && _text[_i] is '+' or '-')
                    {
                        _i++;
                    }

                    var exp = _i;
                    while (!Eof && _text[_i] is >= '0' and <= '9')
                    {
                        _i++;
                    }

                    if (_i == exp)
                    {
                        throw Error("Invalid number exponent.");
                    }
                }

                var slice = _text.Substring(start, _i - start);
                if (!double.TryParse(slice, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    throw Error($"Invalid number '{slice}'.");
                }

                return new JsonNode.Num(value);
            }

            private JsonNode ParseLiteral(string literal, JsonNode node)
            {
                if (_i + literal.Length > _text.Length ||
                    string.CompareOrdinal(_text, _i, literal, 0, literal.Length) != 0)
                {
                    throw Error($"Expected '{literal}'.");
                }

                _i += literal.Length;
                return node;
            }

            public void SkipWs()
            {
                while (!Eof)
                {
                    var c = _text[_i];
                    if (c is ' ' or '\t' or '\n' or '\r')
                    {
                        _i++;
                        continue;
                    }

                    break;
                }
            }

            private void Expect(char c)
            {
                SkipWs();
                if (Eof || _text[_i] != c)
                {
                    throw Error($"Expected '{c}'.");
                }

                _i++;
            }

            private bool Try(char c)
            {
                if (!Eof && _text[_i] == c)
                {
                    _i++;
                    return true;
                }

                return false;
            }

            public FormatException Error(string message)
            {
                var line = 1;
                var col = 1;
                for (var i = 0; i < _i && i < _text.Length; i++)
                {
                    if (_text[i] == '\n')
                    {
                        line++;
                        col = 1;
                    }
                    else
                    {
                        col++;
                    }
                }

                return new FormatException($"{message} At line {line}, column {col}.");
            }
        }
    }

    internal abstract class JsonNode
    {
        internal sealed class Obj : JsonNode
        {
            public Dictionary<string, JsonNode> Fields { get; } = new(StringComparer.Ordinal);

            public bool TryGet(string key, out JsonNode node) => Fields.TryGetValue(key, out node!);
        }

        internal sealed class Arr : JsonNode
        {
            public List<JsonNode> Items { get; } = new();
        }

        internal sealed class Str : JsonNode
        {
            public Str(string value) => Value = value;

            public string Value { get; }
        }

        internal sealed class Num : JsonNode
        {
            public Num(double value) => Value = value;

            public double Value { get; }

            public int AsInt()
            {
                if (Math.Abs(Value - Math.Round(Value)) > 1e-9)
                {
                    throw new FormatException($"Expected integer, got {Value}.");
                }

                return Convert.ToInt32(Value);
            }
        }

        internal sealed class Bool : JsonNode
        {
            public static readonly Bool True = new(true);
            public static readonly Bool False = new(false);

            public Bool(bool value) => Value = value;

            public bool Value { get; }
        }

        internal sealed class Null : JsonNode
        {
            public static readonly Null Instance = new();
        }
    }
}
