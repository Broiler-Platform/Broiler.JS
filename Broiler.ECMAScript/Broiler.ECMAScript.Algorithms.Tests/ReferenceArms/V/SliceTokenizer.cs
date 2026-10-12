// REFERENCE ARM V: a test-only copy of Broiler.VM's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-3-001 (docs/evidence/sjl-3-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.VM at 0087e8d7 (bundle sjl-0-001's base):
//   src/Broiler.VM.Profile.JavaScript.Compiler/SliceTokenizer.cs
//     blob f42cd2691199a9c8bdf92c0f27d76d7831ec8a64: ReadNumericLiteral, FinishNumeric, BigIntShapeRefusal, RefuseNumeric, SeparatorsAreBetweenDigits, IsPlainDigit, IsRadixDigit, IsDecimalPart, DigitValue
//
// Changes from the source, none of which changes a result:
//   - the namespace; ReadNumericLiteral made internal so the arm can call it;
//   - the generated assurance header and the per-member assurance annotations removed;
//   - the tokenizer's state (source, index), its Refuse sink and the token's record are scaffolding written for the arm below the copied members, holding what the engine's tokenizer held;
//   - IsIdentifierStartAt, which consults the engine's generated identifier table, is replaced as its comment says, which agrees with the engine's table on the population's ASCII texts.

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.V;

/// <summary>The compiler tokenizer's numeric literals: their scanning, refusals and value loops.</summary>
internal sealed class SliceTokenizer
{
    // ---- The scaffolding the copied members need, written for the arm (not copied).

    private readonly string source;
    private int index;

    internal SliceTokenizer(string source) => this.source = source;

    /// <summary>Where scanning stopped: the literal's end.</summary>
    internal int Index => index;

    /// <summary>The first refusal the copied members reported, or null.</summary>
    internal string? Refusal { get; private set; }

    private void Refuse(SliceSourceDiagnosticCode code, string what, int line, int column) => Refusal ??= what;

    /// <summary>
    /// The engine's test asks the generated identifier-start table, JsUnicodeLexical, an engine type
    /// this copy does not take. It is replaced by ASCII letters, '$' and '_' plus
    /// char.IsLetter for the rest; the population's literal texts are ASCII, where the two agree.
    /// </summary>
    private bool IsIdentifierStartAt(int at)
    {
        var c = source[at];
        return c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '$' or '_' || (c > 0x7F && char.IsLetter(c));
    }

    /// <summary>
    /// Reads a numeric literal, recording whether its shape is a legacy octal.
    /// </summary>
    /// <remarks>
    /// The legacy-octal fact is <b>recorded and not ruled on</b>. Whether it is an error depends
    /// on strictness, strictness is the validator's, and a tokenizer that knew about strictness
    /// would be the ambient parse state this component removed.
    /// </remarks>
    internal SliceToken ReadNumericLiteral(int startLine, int startColumn, bool sawNewline)
    {
        var start = index;
        var legacyOctal = false;
        double value;

        if (source[index] == '0' && index + 1 < source.Length &&
            (source[index + 1] is 'x' or 'X' or 'o' or 'O' or 'b' or 'B'))
        {
            var radix = source[index + 1] switch { 'x' or 'X' => 16, 'o' or 'O' => 8, _ => 2 };
            index += 2;
            var digitsStart = index;

            while (index < source.Length && IsRadixDigit(source[index], radix))
            {
                index++;
            }

            if (index == digitsStart)
            {
                return RefuseNumeric(start, startLine, startColumn, "a radix prefix with no digits");
            }

            if (!SeparatorsAreBetweenDigits(
                    System.MemoryExtensions.AsSpan(source, digitsStart, index - digitsStart), radix))
            {
                return RefuseNumeric(
                    start, startLine, startColumn, "a numeric separator that is not between two digits");
            }

            value = 0;

            for (var at = digitsStart; at < index; at++)
            {
                if (source[at] == '_')
                {
                    continue;
                }

                value = (value * radix) + DigitValue(source[at]);
            }
        }
        else
        {
            // A leading zero followed only by decimal digits is a legacy octal when every digit is
            // an octal one, and a decimal literal otherwise - 08 is 8, and 010 is 8. The shape is
            // recorded either way; the value follows the shape.
            if (source[index] == '0' && index + 1 < source.Length && char.IsAsciiDigit(source[index + 1]))
            {
                var scan = index + 1;
                var octal = true;

                while (scan < source.Length && char.IsAsciiDigit(source[scan]))
                {
                    if (source[scan] is '8' or '9')
                    {
                        octal = false;
                    }

                    scan++;
                }

                if (octal && (scan >= source.Length || (source[scan] != '.' && source[scan] is not ('e' or 'E'))))
                {
                    legacyOctal = true;
                    index++;
                    var digitsStart = index;

                    while (index < source.Length && char.IsAsciiDigit(source[index]))
                    {
                        index++;
                    }

                    // A LEGACY OCTAL MAY NOT CARRY A SEPARATOR AT ALL, which is the one place the
                    // rule is about the literal's KIND rather than about where the underscore sits:
                    // `0_1` is not a legacy octal with a separator in it, it is a syntax error, and
                    // the language says so in order not to grow the shape it is trying to retire.
                    if (index < source.Length && source[index] == '_')
                    {
                        return RefuseNumeric(
                            start, startLine, startColumn,
                            "a numeric separator in a legacy octal literal");
                    }

                    value = 0;

                    for (var at = digitsStart; at < index; at++)
                    {
                        value = (value * 8) + (source[at] - '0');
                    }

                    return FinishNumeric(start, value, legacyOctal, startLine, startColumn, sawNewline);
                }

                // A ZERO-LED LITERAL WITH AN 8 OR A 9 IS A NonOctalDecimalIntegerLiteral: decimal
                // in value, and as forbidden in strict code as a legacy octal (12.9.3.1). It is
                // flagged the same way so the parser refuses it there; until 2026-10-03 `08` was
                // admitted in strict code (JSC-253).
                if (!octal)
                {
                    legacyOctal = true;
                }
            }

            while (index < source.Length && IsDecimalPart(source[index]))
            {
                index++;
            }

            if (index < source.Length && source[index] == '.')
            {
                index++;

                while (index < source.Length && IsDecimalPart(source[index]))
                {
                    index++;
                }
            }

            if (index < source.Length && source[index] is 'e' or 'E')
            {
                index++;

                if (index < source.Length && source[index] is '+' or '-')
                {
                    index++;
                }

                var exponentStart = index;

                while (index < source.Length && IsDecimalPart(source[index]))
                {
                    index++;
                }

                if (index == exponentStart)
                {
                    return RefuseNumeric(start, startLine, startColumn, "an exponent with no digits");
                }
            }

            var text = source[start..index];

            if (text.Contains('_', System.StringComparison.Ordinal))
            {
                // `0_1` IS NOT A DECIMAL WITH A SEPARATOR IN IT. A literal beginning with a zero
                // and continuing with a digit is the legacy octal shape, and the language refuses a
                // separator there outright rather than growing a form it is trying to retire - so
                // the test is on the SHAPE and is made before the placement rule below, which
                // `0_1` would otherwise satisfy.
                if (text.Length > 1 && text[0] == '0' && (char.IsAsciiDigit(text[1]) || text[1] == '_'))
                {
                    return RefuseNumeric(
                        start, startLine, startColumn,
                        "a numeric separator in a legacy octal literal");
                }

                if (!SeparatorsAreBetweenDigits(text, 10))
                {
                    return RefuseNumeric(
                        start, startLine, startColumn,
                        "a numeric separator that is not between two digits");
                }

                text = text.Replace("_", string.Empty, System.StringComparison.Ordinal);
            }

            if (!double.TryParse(
                    text,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out value))
            {
                return RefuseNumeric(start, startLine, startColumn, "a numeric literal that is not a number");
            }
        }

        return FinishNumeric(start, value, legacyOctal, startLine, startColumn, sawNewline);
    }

    private SliceToken FinishNumeric(
        int start, double value, bool legacyOctal, int startLine, int startColumn, bool sawNewline)
    {
        // A trailing `n` makes this a BigInt, which is a value kind this manifest has no
        // representation for rather than a malformed number. The suffix is consumed and left on
        // the token's raw text; the parser turns it into a construct and the census counts it.
        if (index < source.Length && source[index] == 'n')
        {
            // ONLY AN INTEGER SPELLING TAKES THE SUFFIX, and the rest are syntax errors of the
            // language rather than BigInts nobody implemented (JSeal B02): `1.5n` and `1e3n` are
            // not integers by spelling, and `01n` and `08n` begin with the zero a BigInt may not
            // have. They are refused here, at the literal, whichever manifest is asked for - a
            // malformed literal is malformed with or without the BigInt gate.
            var shape = BigIntShapeRefusal(start);

            if (shape is not null)
            {
                index++;
                return RefuseNumeric(start, startLine, startColumn, shape);
            }

            index++;

            if (index < source.Length && (IsIdentifierStartAt(index) || char.IsAsciiDigit(source[index])))
            {
                return RefuseNumeric(start, startLine, startColumn, "a numeric literal touching an identifier");
            }

            return new SliceToken(
                SliceTokenKind.NumericLiteral, source[start..index], value, string.Empty,
                startLine, startColumn, sawNewline, legacyOctal);
        }

        // A numeric literal may not be followed immediately by an identifier start: `3in` is not
        // `3 in`, it is an error, and a tokenizer that split it would hand the parser a program
        // the language does not have.
        if (index < source.Length && (IsIdentifierStartAt(index) || char.IsAsciiDigit(source[index])))
        {
            return RefuseNumeric(start, startLine, startColumn, "a numeric literal touching an identifier");
        }

        return new SliceToken(
            SliceTokenKind.NumericLiteral,
            source[start..index],
            value,
            string.Empty,
            startLine,
            startColumn,
            sawNewline,
            legacyOctal);
    }

    /// <summary>
    /// Why the numeric text from <paramref name="start"/> cannot take a BigInt suffix, or null when
    /// it can.
    /// </summary>
    /// <remarks>
    /// The grammar's BigIntLiteralSuffix follows a NonDecimalIntegerLiteral or a
    /// DecimalIntegerLiteral, and a DecimalIntegerLiteral is <c>0</c> or begins with a non-zero
    /// digit. Everything else - a fraction, an exponent, a legacy octal, a leading zero - is
    /// refused.
    /// </remarks>
    private string? BigIntShapeRefusal(int start)
    {
        if (index - start > 1 && source[start] == '0' && source[start + 1] is 'x' or 'X' or 'o' or 'O' or 'b' or 'B')
        {
            return null;
        }

        for (var at = start; at < index; at++)
        {
            if (source[at] is '.' or 'e' or 'E')
            {
                return "a BigInt literal that is not an integer";
            }
        }

        return index - start > 1 && source[start] == '0'
            ? "a BigInt literal with a leading zero"
            : null;
    }

    private SliceToken RefuseNumeric(int start, int startLine, int startColumn, string what)
    {
        Refuse(SliceSourceDiagnosticCode.MalformedNumericLiteral, what, startLine, startColumn);

        return new SliceToken(
            SliceTokenKind.NumericLiteral, source[start..index], 0, string.Empty,
            startLine, startColumn, false, false);
    }

    /// <summary>Whether every separator in a numeric literal sits between two digits.</summary>
    /// <remarks>
    /// <b>THE SEPARATOR IS NOT A CHARACTER THE LITERAL MAY CONTAIN, it is a character that may sit
    /// BETWEEN TWO DIGITS.</b> `1__0`, `1_`, `_1`, `0x_1` and `1_.5` are each a syntax error, and
    /// this tokenizer stripped every underscore and parsed what was left - so all five were numbers.
    /// A literal a person is most likely to write by accident is exactly the one the rule exists to
    /// catch.
    /// </remarks>
    private static bool SeparatorsAreBetweenDigits(System.ReadOnlySpan<char> text, int radix)
    {
        for (var at = 0; at < text.Length; at++)
        {
            if (text[at] != '_')
            {
                continue;
            }

            if (at == 0 || at + 1 >= text.Length)
            {
                return false;
            }

            if (!IsPlainDigit(text[at - 1], radix) || !IsPlainDigit(text[at + 1], radix))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>A digit of the radix, which is what a separator has to sit between.</summary>
    private static bool IsPlainDigit(char c, int radix) => radix switch
    {
        2 => c is '0' or '1',
        8 => c is >= '0' and <= '7',
        16 => char.IsAsciiHexDigit(c),
        _ => char.IsAsciiDigit(c),
    };

    private static bool IsRadixDigit(char c, int radix) => c == '_' || radix switch
    {
        16 => char.IsAsciiHexDigit(c),
        8 => c is >= '0' and <= '7',
        _ => c is '0' or '1',
    };

    /// <summary>Whether <paramref name="c"/> continues a decimal run, separators included.</summary>
    /// <remarks>
    /// <b>The separator is a spelling and not a value.</b> <c>1_000_000</c> and <c>1000000</c> are
    /// the same number, so the scan admits <c>_</c> and the conversion drops it. The census found
    /// 2,362 test262 files refused as malformed numbers, and this is most of them.
    /// </remarks>
    private static bool IsDecimalPart(char c) => char.IsAsciiDigit(c) || c == '_';

    private static int DigitValue(char c) =>
        char.IsAsciiDigit(c) ? c - '0' : (char.ToLowerInvariant(c) - 'a') + 10;
}

/// <summary>The fields of the engine's token the copied members fill.</summary>
internal sealed record SliceToken(
    SliceTokenKind Kind, string RawText, double NumericValue, string StringValue,
    int Line, int Column, bool NewlineBefore, bool LegacyOctal);

/// <summary>The one token kind the copied members produce.</summary>
internal enum SliceTokenKind
{
    NumericLiteral,
}

/// <summary>The one diagnostic code the copied members report.</summary>
internal enum SliceSourceDiagnosticCode
{
    MalformedNumericLiteral,
}
