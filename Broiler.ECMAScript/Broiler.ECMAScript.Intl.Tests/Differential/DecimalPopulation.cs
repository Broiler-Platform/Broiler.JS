using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using Broiler.ECMAScript.Conformance;

namespace Broiler.ECMAScript.Intl.Tests.Differential;

/// <summary>One value an Intl operation of sub-area (b) is handed: a Number, a String or a BigInt, as ToPrimitive leaves it.</summary>
internal sealed record DecimalInput(double Number, string? Text, BigInteger? BigInt)
{
    internal static DecimalInput Of(double value) => new(value, null, null);

    internal static DecimalInput Of(string value) => new(0, value, null);

    internal static DecimalInput Of(BigInteger value) => new(0, null, value);

    /// <summary>How a difference's report names the input.</summary>
    internal string Label =>
        Text is { } text ? LocaleSection.Quote(text)
        : BigInt is { } big ? big.ToString(CultureInfo.InvariantCulture) + "n"
        : Number == 0 && double.IsNegative(Number) ? "-0"
        : Number.ToString("R", CultureInfo.InvariantCulture);
}

/// <summary>An options bag of plain values, in the order written: Strings, Numbers and Booleans.</summary>
internal sealed record OptionsBag(IReadOnlyList<KeyValuePair<string, object>> Entries)
{
    internal static OptionsBag Empty { get; } = new([]);

    internal object? this[string key] => Entries.FirstOrDefault(e => e.Key == key).Value;

    internal bool Has(string key) => Entries.Any(e => e.Key == key);

    /// <summary>The bag as a difference's report names it.</summary>
    internal string Label =>
        "{" + string.Join(",", Entries.Select(static e => e.Key + ":" + e.Value switch
        {
            string s => "\"" + s + "\"",
            bool b => b ? "true" : "false",
            double d => d.ToString("R", CultureInfo.InvariantCulture),
            _ => "?",
        })) + "}";
}

/// <summary>
/// The differential check's population for sub-area (b) (Broiler.VM's JSD-0082, decision D22): every Number, String and BigInt
/// input of the Intl vector files and every value and options bag the sub-area's vector scripts write; every value, range and
/// options bag of the <c>numbers</c> and <c>plurals</c> dataset programs; every locale those name and the embedding's own; and
/// seeded random Numbers, numeric Strings, BigInts, ranges and options bags, so many per unit of scale.
/// </summary>
internal sealed class DecimalPopulation
{
    private static readonly string[] DigitKeys =
    [
        "minimumIntegerDigits", "minimumFractionDigits", "maximumFractionDigits", "minimumSignificantDigits", "maximumSignificantDigits",
        "roundingIncrement", "roundingMode", "roundingPriority", "trailingZeroDisplay",
    ];

    private static readonly string[] Modes = ["ceil", "floor", "expand", "trunc", "halfCeil", "halfFloor", "halfExpand", "halfTrunc", "halfEven"];

    private static readonly int[] Increments = [1, 2, 5, 10, 20, 25, 50, 100, 200, 250, 500, 1000, 2000, 2500, 5000];

    internal List<DecimalInput> Values { get; } = [];

    internal List<(DecimalInput Start, DecimalInput End)> Ranges { get; } = [];

    /// <summary>Options bags of <c>Intl.NumberFormat</c>.</summary>
    internal List<OptionsBag> NumberFormatBags { get; } = [];

    /// <summary>Options bags of <c>Intl.PluralRules</c>.</summary>
    internal List<OptionsBag> PluralRulesBags { get; } = [];

    internal List<string> Locales { get; } = [];

    internal static DecimalPopulation Build(Embedding embedding, SeededRandom random, int scale)
    {
        var population = new DecimalPopulation();
        var values = new List<DecimalInput>();
        var numberBags = new List<OptionsBag>();
        var pluralBags = new List<OptionsBag> { OptionsBag.Empty };
        var locales = new List<string>();

        // EVERY INTL VECTOR'S NUMBER, STRING AND BIGINT INPUT, and the values and bags sub-area (b)'s scripts write.
        foreach (var file in new[] { "locales.vectors", "values-and-plurals.vectors", "numbers-and-text.vectors", "date-times.vectors", "durations.vectors" })
        {
            foreach (var vector in Population.IntlVectors(file))
            {
                foreach (var input in LibraryRun.Inputs(vector))
                {
                    switch (input.Kind)
                    {
                        case TypedValueKind.Number:
                            values.Add(DecimalInput.Of(input.Number));
                            break;
                        case TypedValueKind.BigInt:
                            values.Add(DecimalInput.Of(input.Integer));
                            break;
                        case TypedValueKind.String when file == "values-and-plurals.vectors" && input.Chars.Length > 0 && !char.IsAsciiLetter(input.Chars[0]):
                            values.Add(DecimalInput.Of(input.Chars));
                            break;
                    }
                }

                if (file != "values-and-plurals.vectors")
                    continue;

                var script = vector.Script;
                foreach (var service in new[] { "PluralRules", "NumberFormat" })
                {
                    var at = script.IndexOf("new Intl." + service + "(", StringComparison.Ordinal);
                    if (at < 0)
                        continue;

                    var reader = new LiteralReader(script, at + ("new Intl." + service + "(").Length);
                    if (reader.Value() is string locale)
                    {
                        locales.Add(locale);
                        if (reader.Take(",") && reader.Value() is OptionsBag bag)
                            (service == "PluralRules" ? pluralBags : numberBags).Add(bag);
                    }
                }

                values.AddRange(ScriptValues(script));
            }
        }

        // EVERY DATASET VALUE, RANGE AND OPTIONS BAG of the numbers and plurals programs.
        foreach (var (name, program) in Population.DatasetPrograms().Where(static p => p.Name is "numbers" or "plurals"))
        {
            foreach (var item in Array(program, "values"))
            {
                if (Input(item) is { } input)
                    values.Add(input);
            }

            foreach (var item in Array(program, "ranges"))
            {
                if (item is List<object?> pair && pair.Count == 2 && Input(pair[0]) is { } start && Input(pair[1]) is { } end)
                    population.Ranges.Add((start, end));
            }

            foreach (var item in Array(program, name == "plurals" ? "formats" : "cases"))
            {
                if (item is not List<object?> format || format.Count == 0 || format[0] is not string locale)
                    continue;

                locales.Add(locale);
                var bag = format.Count > 1 && format[1] is OptionsBag given ? given : OptionsBag.Empty;
                (name == "plurals" ? pluralBags : numberBags).Add(bag);
            }
        }

        // THE EMBEDDING'S OWN LOCALES.
        locales.AddRange(embedding.Lines(IntlTable.Locales).Select(static l => l.Split('|')[0]));

        // SEEDED RANDOM VALUES, RANGES AND BAGS.
        for (var i = 0; i < 1000 * scale; i++)
            values.Add(RandomValue(random));
        for (var i = 0; i < 150 * scale; i++)
            numberBags.Add(RandomBag(random, numberFormat: true));
        for (var i = 0; i < 75 * scale; i++)
            pluralBags.Add(RandomBag(random, numberFormat: false));

        population.Values.AddRange(values.DistinctBy(static v => v.Label + (v.Text is null ? string.Empty : "s")));
        for (var i = 0; i < 250 * scale; i++)
            population.Ranges.Add((random.Pick(population.Values), random.Pick(population.Values)));
        population.NumberFormatBags.AddRange(numberBags.DistinctBy(static b => b.Label));
        population.PluralRulesBags.AddRange(pluralBags.DistinctBy(static b => b.Label));
        population.Locales.AddRange(locales.Distinct(StringComparer.Ordinal));
        return population;
    }

    /// <summary>The literal arguments a sub-area (b) script hands <c>select</c>, <c>selectRange</c>, <c>format</c> and a list's <c>map</c>.</summary>
    private static IEnumerable<DecimalInput> ScriptValues(string script)
    {
        foreach (var head in new[] { ".select(", ".selectRange(", ".format(", "JSON.stringify([" })
        {
            for (var at = script.IndexOf(head, StringComparison.Ordinal); at >= 0; at = script.IndexOf(head, at + 1, StringComparison.Ordinal))
            {
                var reader = new LiteralReader(script, at + head.Length);
                while (reader.Value() is { } value && Input(value) is { } input)
                {
                    yield return input;
                    if (!reader.Take(","))
                        break;
                }
            }
        }
    }

    private static DecimalInput? Input(object? value) => value switch
    {
        double d => DecimalInput.Of(d),
        string s => DecimalInput.Of(s),
        BigInteger b => DecimalInput.Of(b),
        _ => null,
    };

    /// <summary>The elements of the array a program's <c>var name = [...]</c> writes.</summary>
    private static List<object?> Array(string program, string name)
    {
        var at = program.IndexOf("var " + name + " = [", StringComparison.Ordinal);
        if (at < 0)
            return [];
        var reader = new LiteralReader(program, at + ("var " + name + " = ").Length);
        return reader.Value() as List<object?> ?? [];
    }

    private static DecimalInput RandomValue(SeededRandom random)
    {
        switch (random.Next(10))
        {
            case 0:
            {
                var bits = BitConverter.Int64BitsToDouble((long)random.NextUInt64());
                return DecimalInput.Of(double.IsFinite(bits) ? bits : (double)(random.Next(1000) - 500));
            }

            case 1:
                return DecimalInput.Of((double)(random.Next(2000) - 1000));
            case 2:
            case 3:
            {
                // A short decimal, a tie at its last digit half the time.
                var digits = random.NextString("0123456789", 1, 7) + (random.Next(2) == 0 ? "5" : string.Empty);
                var point = random.Next(digits.Length + 1);
                var text = (random.Next(4) == 0 ? "-" : string.Empty) + digits[..point] + "." + digits[point..];
                return DecimalInput.Of(double.Parse(text.TrimEnd('.') is { Length: > 0 } t && t != "-" ? t : "0", NumberStyles.Float, CultureInfo.InvariantCulture));
            }

            case 4:
            case 5:
                return DecimalInput.Of(RandomNumericText(random));
            case 6:
            {
                var digits = random.NextString("0123456789", 1, 60);
                var value = BigInteger.Parse(digits, CultureInfo.InvariantCulture);
                return DecimalInput.Of(random.Next(3) == 0 ? -value : value);
            }

            case 7:
                return DecimalInput.Of(BigInteger.Pow(10, random.Next(420)) * (random.Next(2) == 0 ? 1 : -1));
            case 8:
                return DecimalInput.Of(Math.Pow(10, random.Next(40) - 20) * (1 + random.Next(9)));
            default:
                return DecimalInput.Of(random.Pick(new[] { 0.0, -0.0, double.NaN, double.PositiveInfinity, double.NegativeInfinity, double.MaxValue, double.Epsilon, 1e21, 0.1 + 0.2 }));
        }
    }

    /// <summary>A String a StringNumericLiteral reader is handed: often one, sometimes almost one.</summary>
    private static string RandomNumericText(SeededRandom random)
    {
        var digits = random.NextString("0123456789", 1, random.Next(20) == 0 ? 900 : 25);
        var text = new StringBuilder();
        if (random.Next(5) == 0)
            text.Append(random.Pick(new[] { " ", "\u00A0", "\uFEFF", "\u2028", "\t\n", "\u0085" }));
        switch (random.Next(8))
        {
            case 0:
                text.Append(random.Pick(new[] { "0x", "0X", "0o", "0b", "-0x" })).Append(random.NextString("0123456789abcdefABCDEF", 1, 30));
                break;
            case 1:
                text.Append(random.Pick(new[] { "Infinity", "-Infinity", "+Infinity", "infinity", "NaN", "", "1_000", "1e", "1n", ".", "-", "e5" }));
                break;
            default:
                text.Append(random.Pick(new[] { "", "", "-", "+" }));
                var point = random.Next(digits.Length + 2);
                text.Append(point < digits.Length ? digits[..point] + "." + digits[point..] : point == digits.Length ? digits + "." : digits);
                if (random.Next(3) == 0)
                    text.Append(random.Pick(new[] { "e", "E" })).Append(random.Pick(new[] { "", "-", "+" })).Append(random.Next(random.Next(6) == 0 ? 2000 : 30).ToString(CultureInfo.InvariantCulture));
                break;
        }

        if (random.Next(5) == 0)
            text.Append(random.Pick(new[] { " ", "\u3000", "\r" }));
        return text.ToString();
    }

    private static OptionsBag RandomBag(SeededRandom random, bool numberFormat)
    {
        var entries = new List<KeyValuePair<string, object>>();

        void Add(string key, object value) => entries.Add(new(key, value));

        if (numberFormat && random.Next(3) == 0)
        {
            var style = random.Pick(new[] { "decimal", "percent", "currency", "currency", "bogus" });
            Add("style", style);
            if (style == "currency" || random.Next(8) == 0)
                Add("currency", random.Pick(new[] { "USD", "EUR", "JPY", "KWD", "bhd", "CLF", "XXX", "US" }));
        }

        if (!numberFormat && random.Next(4) == 0)
            Add("type", random.Pick(new[] { "cardinal", "ordinal", "ordinal", "bogus" }));
        if (random.Next(3) == 0)
            Add("notation", random.Pick(new[] { "standard", "scientific", "engineering", "compact", "compact", "bogus" }));
        if (random.Next(5) == 0)
            Add("compactDisplay", random.Pick(new[] { "short", "long", "bogus" }));

        foreach (var key in DigitKeys)
        {
            if (random.Next(4) != 0)
                continue;

            object value = key switch
            {
                "minimumIntegerDigits" => (double)random.Pick(new[] { 1, 2, 3, 21, 0, 22 }),
                "minimumFractionDigits" or "maximumFractionDigits" => random.Next(12) == 0 ? random.Pick(new object[] { -1.0, 101.0, 2.7, "3", double.NaN }) : (double)random.Next(random.Next(4) == 0 ? 101 : 8),
                "minimumSignificantDigits" or "maximumSignificantDigits" => random.Next(12) == 0 ? random.Pick(new object[] { 0.0, 22.0, 2.5, "4" }) : (double)(1 + random.Next(21)),
                "roundingIncrement" => random.Next(10) == 0 ? random.Pick(new object[] { 3.0, 0.0, 5001.0, 2.5 }) : (double)random.Pick(Increments),
                "roundingMode" => random.Next(10) == 0 ? "up" : random.Pick(Modes),
                "roundingPriority" => random.Pick(new[] { "auto", "morePrecision", "lessPrecision", "most" }),
                _ => random.Pick(new[] { "auto", "stripIfInteger", "strip" }),
            };
            Add(key, value);
        }

        if (numberFormat && random.Next(6) == 0)
            Add("signDisplay", random.Pick(new[] { "auto", "never", "always", "exceptZero", "negative", "bogus" }));
        return new OptionsBag(entries);
    }

    /// <summary>
    /// A reader of the JavaScript literals the vector scripts and the dataset programs write: Numbers (with a sign, an exponent,
    /// <c>NaN</c> and the infinities, the prelude's <c>$f64(hi, lo)</c>), BigInts and their <c>**</c>, Strings in either quote,
    /// <c>undefined</c>, Booleans, arrays and object literals of those. Anything else ends the read with <see langword="null"/>.
    /// </summary>
    private sealed class LiteralReader(string text, int at)
    {
        private int at = at;

        internal bool Take(string literal)
        {
            Skip();
            if (string.CompareOrdinal(text, at, literal, 0, literal.Length) != 0)
                return false;
            at += literal.Length;
            return true;
        }

        internal object? Value()
        {
            Skip();
            if (at >= text.Length)
                return null;

            var c = text[at];
            if (c is '"' or '\'')
                return Quoted(c);
            if (c == '[')
            {
                at++;
                var items = new List<object?>();
                if (Take("]"))
                    return items;
                while (true)
                {
                    var item = Value();
                    if (item is null && !Last(text, "undefined"))
                        return null;
                    items.Add(item);
                    if (Take("]"))
                        return items;
                    if (!Take(","))
                        return null;
                }
            }

            if (c == '{')
            {
                at++;
                var entries = new List<KeyValuePair<string, object>>();
                if (Take("}"))
                    return new OptionsBag(entries);
                while (true)
                {
                    Skip();
                    var begin = at;
                    while (at < text.Length && char.IsAsciiLetterOrDigit(text[at]))
                        at++;
                    var key = text[begin..at];
                    if (key.Length == 0 || !Take(":"))
                        return null;
                    var value = Value();
                    if (value is not (string or double or bool))
                        return null;
                    entries.Add(new(key, value));
                    if (Take("}"))
                        return new OptionsBag(entries);
                    if (!Take(","))
                        return null;
                }
            }

            if (Take("$f64("))
            {
                var high = Hex();
                if (high is null || !Take(","))
                    return null;
                var low = Hex();
                return low is not null && Take(")") ? BitConverter.UInt64BitsToDouble(((ulong)high.Value << 32) | low.Value) : null;
            }

            foreach (var (word, value) in new (string, object?)[] { ("NaN", double.NaN), ("Infinity", double.PositiveInfinity), ("-Infinity", double.NegativeInfinity), ("true", true), ("false", false), ("undefined", null) })
            {
                if (string.CompareOrdinal(text, at, word, 0, word.Length) == 0 && (at + word.Length >= text.Length || !char.IsAsciiLetterOrDigit(text[at + word.Length])))
                {
                    at += word.Length;
                    return value;
                }
            }

            return NumberOrBigInt();
        }

        private bool Last(string source, string word) => at >= word.Length && string.CompareOrdinal(source, at - word.Length, word, 0, word.Length) == 0;

        private void Skip()
        {
            while (at < text.Length && text[at] is ' ' or '\n' or '\r' or '\t')
                at++;
        }

        private string? Quoted(char quote)
        {
            at++;
            var value = new StringBuilder();
            while (at < text.Length && text[at] != quote)
            {
                if (text[at] == '\\' && at + 1 < text.Length)
                {
                    at++;
                    if (text[at] == 'u' && at + 4 < text.Length && int.TryParse(text.AsSpan(at + 1, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var unit))
                    {
                        value.Append((char)unit);
                        at += 5;
                        continue;
                    }
                }

                value.Append(text[at]);
                at++;
            }

            if (at >= text.Length)
                return null;
            at++;
            return value.ToString();
        }

        private uint? Hex()
        {
            if (!Take("0x"))
                return null;
            var begin = at;
            while (at < text.Length && char.IsAsciiHexDigit(text[at]))
                at++;
            return uint.TryParse(text.AsSpan(begin, at - begin), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var value) ? value : null;
        }

        private object? NumberOrBigInt()
        {
            var negative = Take("-");
            if (Take("("))
            {
                var inner = NumberOrBigInt();
                return Take(")") ? Negate(inner, negative) : null;
            }

            var begin = at;
            while (at < text.Length && (char.IsAsciiDigit(text[at]) || text[at] == '.' || ((text[at] | 0x20) == 'e' && at > begin) || (text[at] is '+' or '-' && (text[at - 1] | 0x20) == 'e')))
                at++;
            if (at == begin)
                return null;

            var literal = text[begin..at];
            object value;
            if (at < text.Length && text[at] == 'n')
            {
                at++;
                if (!BigInteger.TryParse(literal, NumberStyles.None, CultureInfo.InvariantCulture, out var big))
                    return null;
                if (Take("**"))
                {
                    if (NumberOrBigInt() is not BigInteger exponent || exponent > 5000)
                        return null;
                    big = BigInteger.Pow(big, (int)exponent);
                }

                value = big;
            }
            else
            {
                if (!double.TryParse(literal, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                    return null;
                value = number;
            }

            return Negate(value, negative);
        }

        private static object? Negate(object? value, bool negative) =>
            !negative ? value : value switch { double d => -d, BigInteger b => -b, _ => null };
    }
}
