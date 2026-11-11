using System;
using System.Globalization;

namespace Broiler.JavaScript.BuiltIns.Tests;

// The `m` literal's Decimal reads and writes its digits with the invariant culture, under every host culture (milestone
// SJL-9's clause 8, on the owner's ruling of 2026-11-11, "Fix in this PR"): an `m` literal's digits are source text, whose
// decimal point is `.` under every host. Until 2026-11-11 the literal's factory, the Decimal function, toString, toFixed,
// the concatenations and the equality with a string literal named no culture and read the host's: under de-DE, whose
// group separator is `.`, `1234567.891m` was 1234567891 and `(1.5m).toFixed(1)` was "15,0"; under sv-SE, whose decimal
// point is `,`, an `m` literal with a fraction was a TypeError. Each case runs under the invariant culture, whose answers
// are the ones the engine gave there before the change and every host now gives, and under de-DE, sv-SE and tr-TR,
// applied as DecimalToLocaleStringTests applies a culture.
public class DecimalCultureTests
{
    private static string Eval(string source) => SharedDateAlgorithmTests.Eval(source);

    private static T UnderCulture<T>(string culture, Func<T> body)
    {
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            return body();
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }

    public static TheoryData<string> Cultures => new() { "", "de-DE", "sv-SE", "tr-TR" };

    private static void Holds(string culture, string expected, string source)
        => Assert.Equal(expected, UnderCulture(culture, () => Eval(source)));

    [Theory]
    [MemberData(nameof(Cultures))]
    public void The_literal_s_digits_are_read_with_the_invariant_culture(string culture)
    {
        Holds(culture, "1234567.891", "(1234567.891m).toString()");
        Holds(culture, "1.5", "(1.5m).toString()");
        Holds(culture, "-0.5", "(-0.5m).toString()");
        Holds(culture, "0.5", "(.5m).toString()");
        Holds(culture, "1000000.25", "(1_000_000.25m).toString()");
        Holds(culture, "true", "1234567.891m === Decimal('1234567891') / 1000m");
        Holds(culture, "true", "1.5m * 2m === 3m");
    }

    [Theory]
    [MemberData(nameof(Cultures))]
    public void ToFixed_writes_the_invariant_culture_s_digits(string culture)
    {
        Holds(culture, "1.5", "(1.5m).toFixed(1)");
        Holds(culture, "1234567.89", "(1234567.891m).toFixed(2)");
        Holds(culture, "1234567", "(1234567.091m).toFixed()");
        Holds(culture, "-0.50", "(-0.5m).toFixed(2)");
        // Above 999999999999999, with at most 15 digits asked, toFixed writes the general format, as it did.
        Holds(culture, "1234567890123456.5", "(1234567890123456.5m).toFixed(1)");
    }

    // The `n` after a Decimal's digits in three of these is the engine's own text of a Decimal value (JSDecimal.ToString),
    // which the change leaves as it was; only the digits' culture is under test.
    [Theory]
    [MemberData(nameof(Cultures))]
    public void Concatenation_writes_the_invariant_culture_s_digits(string culture)
    {
        Holds(culture, "1.5x", "1.5m + 'x'");
        Holds(culture, "1.5nx", "var s = 'x'; 1.5m + s");
        Holds(culture, "x1.5n", "'x' + 1.5m");
        Holds(culture, "1.5n", "String(1.5m)");
        Holds(culture, "1234567.891x", "1234567.891m + 'x'");
    }

    [Theory]
    [MemberData(nameof(Cultures))]
    public void Equality_with_a_string_literal_reads_the_invariant_culture_s_digits(string culture)
    {
        Holds(culture, "true", "1.5m == '1.5'");
        Holds(culture, "false", "1.5m == '1,5'");
        Holds(culture, "false", "1.5m != '1.5'");
        Holds(culture, "true", "1234567.891m == '1234567.891'");
    }

    [Theory]
    [MemberData(nameof(Cultures))]
    public void The_Decimal_function_reads_a_String_with_the_invariant_culture(string culture)
    {
        Holds(culture, "1234567.891", "Decimal('1234567.891').toString()");
        Holds(culture, "1.5", "Decimal('1.5m').toString()");
        Holds(culture, "-1.5", "Decimal('-1.5').toString()");
        Holds(culture, "1.5", "Decimal(' 1.5 ').toString()");
        Holds(culture, "-5", "Decimal(-5n).toString()");
        // The invariant culture's group separator is `,`, which the function read under the invariant culture before.
        Holds(culture, "15", "Decimal('1,5').toString()");
        Holds(culture, "TypeError", "var e = 'none'; try { Decimal('1.5.5'); } catch (x) { e = x.name; } e");
        // U+2212, sv-SE's minus sign in .NET, is not the invariant culture's.
        Holds(culture, "TypeError", "var e = 'none'; try { Decimal('−5'); } catch (x) { e = x.name; } e");
    }

    // The type keeps a negative zero's sign: toString writes it as the invariant culture does, "0", and toLocaleString
    // writes "-0", as for a Number's -0.
    [Theory]
    [MemberData(nameof(Cultures))]
    public void A_negative_zero_is_written_as_under_the_invariant_culture(string culture)
    {
        Holds(culture, "0", "(-0m).toString()");
        Holds(culture, "0", "(0m * -1m).toString()");
        Holds(culture, "-0", "(-0m).toLocaleString('en')");
        Holds(culture, "-0", "(0m * -1m).toLocaleString('en')");
    }

    // DecimalToLocaleStringTests' cases, with the literal itself where they build the value from a String.
    [Theory]
    [MemberData(nameof(Cultures))]
    public void ToLocaleString_formats_the_literal_s_value(string culture)
    {
        Holds(culture, "1,234,567.891", "(1234567.891m).toLocaleString('en')");
        Holds(culture, "1.234.567,891", "(1234567.891m).toLocaleString('de')");
        Holds(culture, "1,234,567.9", "(1234567.891m).toLocaleString('en', { maximumFractionDigits: 1 })");
        Holds(culture, "1.234.567,89 €", "(1234567.891m).toLocaleString('de', { style: 'currency', currency: 'EUR' })");
        Holds(culture, "1.2M", "(1234567.891m).toLocaleString('en', { notation: 'compact' })");
        Holds(culture, "-1", "(-0.5m).toLocaleString('en', { maximumFractionDigits: 0 })");
        Holds(culture, "12,345,678,901,234,567,890.123456789",
            "(12345678901234567890.123456789m).toLocaleString('en', { maximumFractionDigits: 20 })");
        Holds(culture, "true", "(1234567.891m).toLocaleString() === new Intl.NumberFormat().format('1234567.891')");
    }

    // With no locales the default-locale provider decides (decision D7, row L35), and the literal is the same value under
    // every host.
    [Theory]
    [InlineData("", "1,234,567.891")]
    [InlineData("de-DE", "1.234.567,891")]
    [InlineData("sv-SE", "1 234 567,891")]
    [InlineData("tr-TR", "1.234.567,891")]
    public void ToLocaleString_with_no_locales_formats_the_literal_s_value_in_the_default_locale(string culture, string expected)
        => Holds(culture, expected, "(1234567.891m).toLocaleString()");
}
