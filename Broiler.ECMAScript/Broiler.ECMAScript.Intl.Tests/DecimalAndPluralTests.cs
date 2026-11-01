using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Broiler.ECMAScript.Numbers;
using Xunit;

namespace Broiler.ECMAScript.Intl.Tests;

/// <summary>
/// Sub-area (b) of the Intl library (stage SJL-9.3b): the Intl mathematical value, SetNumberFormatDigitOptions' decisions,
/// FormatNumericToString, and plural rules and ranges, each held to the answers Broiler.VM gives at its SJL-9 rule commit as
/// the correspondence of bundle <c>sjl-9-001</c> records them, row by row, over the data home's Broiler.JS tables.
/// </summary>
public sealed class DecimalAndPluralTests
{
    private static readonly TableLocaleData Data = new(HomeDataSet.Current);

    /// <summary>Broiler.VM's BigInt policy is its engine's; these tests read no prefixed literal past a few digits.</summary>
    private static readonly BigIntPolicy Policy = BigIntPolicy.Unbounded;

    // ---- the engine's side, as an adapter does it ------------------------------------------------------------------------

    /// <summary>
    /// SetNumberFormatDigitOptions as an engine's adapter runs it over an options bag of plain values: the plan, each option
    /// converted at its step through DefaultNumberOption, then the library's decisions.
    /// </summary>
    private static IntlStatus TryDigits(out DigitOptions? options, int mnfdDefault = 0, int mxfdDefault = 3, NumberNotation notation = NumberNotation.Standard,
        double? mnfd = null, double? mxfd = null, double? mnsd = null, double? mxsd = null, int increment = 1,
        RoundingMode mode = RoundingMode.HalfExpand, RoundingPriority priority = RoundingPriority.Auto,
        TrailingZeroDisplay trailingZeroDisplay = TrailingZeroDisplay.Auto, int mnid = 1)
    {
        options = null;

        if (!DigitOptionRules.IsAdmittedRoundingIncrement(increment))
        {
            return IntlStatus.RoundingIncrementNotAdmitted;
        }

        var hasSd = mnsd is not null || mxsd is not null;
        var hasFd = mnfd is not null || mxfd is not null;
        var plan = DigitOptionRules.Plan(hasSd, hasFd, priority, notation);
        int? minSd = null, maxSd = null, minFd = null, maxFd = null;

        if (plan.NeedSignificantDigits && hasSd)
        {
            var status = Number(mnsd, 1, 21, 1, out minSd);
            if (status != IntlStatus.Completed)
            {
                return status;
            }

            status = Number(mxsd, minSd!.Value, 21, 21, out maxSd);
            if (status != IntlStatus.Completed)
            {
                return status;
            }
        }

        if (plan.NeedFractionDigits && hasFd)
        {
            var status = Number(mnfd, 0, 100, null, out minFd);
            if (status != IntlStatus.Completed)
            {
                return status;
            }

            status = Number(mxfd, 0, 100, null, out maxFd);
            if (status != IntlStatus.Completed)
            {
                return status;
            }
        }

        var values = new DigitOptionValues(mnid, hasSd, minSd, maxSd, hasFd, minFd, maxFd, increment, mode, priority, trailingZeroDisplay);
        return DigitOptionRules.SetNumberFormatDigitOptions(values, new FractionDigitDefaults(mnfdDefault, mxfdDefault), notation, out options);
    }

    private static IntlStatus Number(double? value, int minimum, int maximum, int? fallback, out int? result)
    {
        if (value is null)
        {
            result = fallback;
            return IntlStatus.Completed;
        }

        var status = DigitOptionRules.DefaultNumberOption(value.Value, minimum, maximum, out var converted);
        result = converted;
        return status;
    }

    private static DigitOptions Digits(int mnfdDefault = 0, int mxfdDefault = 3, NumberNotation notation = NumberNotation.Standard,
        double? mnfd = null, double? mxfd = null, double? mnsd = null, double? mxsd = null, int increment = 1,
        RoundingMode mode = RoundingMode.HalfExpand, RoundingPriority priority = RoundingPriority.Auto,
        TrailingZeroDisplay trailingZeroDisplay = TrailingZeroDisplay.Auto, int mnid = 1)
    {
        Assert.Equal(IntlStatus.Completed, TryDigits(out var options, mnfdDefault, mxfdDefault, notation, mnfd, mxfd, mnsd, mxsd, increment, mode, priority, trailingZeroDisplay, mnid));
        return options!;
    }

    private static string Format(DigitOptions options, IntlDecimal x)
    {
        var result = NumericFormat.FormatNumericToString(options, x);
        return (result.RoundedNumber.IsNegative ? "-" : string.Empty) + result.FormattedString;
    }

    private static string Format(DigitOptions options, double x) => Format(options, IntlDecimal.FromNumber(x));

    private static PluralRulesRecord Rules(string language, PluralRuleType type = PluralRuleType.Cardinal, NumberNotation notation = NumberNotation.Standard,
        DigitOptions? digits = null, CompactDisplay compactDisplay = CompactDisplay.Short) =>
        PluralRulesRecord.Create(Data, language, language, type, notation, compactDisplay, digits ?? Digits(notation: notation));

    private static string Select(PluralRulesRecord rules, double x) => rules.ResolvePlural(IntlDecimal.FromNumber(x)).PluralCategory;

    private static string Range(PluralRulesRecord rules, IntlDecimal x, IntlDecimal y)
    {
        Assert.Equal(IntlStatus.Completed, rules.ResolvePluralRange(x, y, out var category));
        return category;
    }

    // ---- M: the Intl mathematical value, digit options and FormatNumericToString -----------------------------------------

    /// <summary>Row M1a: a Number is its Number::toString digits, so its decimal value rounds, not its binary one.</summary>
    [Fact]
    public void Row_M1a_a_Number_rounds_by_its_shortest_decimal_digits()
    {
        var one = Digits(mxfd: 1);
        Assert.Equal(["1.2", "1.3", "1.4", "1.5", "-1.2"], new[] { 1.15, 1.25, 1.35, 1.45, -1.15 }.Select(x => Format(one, x)));

        var two = Digits(mxfd: 2);
        Assert.Equal(["1.01", "2.68", "1.26", "8.35", "0.62"], new[] { 1.005, 2.675, 1.255, 8.345, 0.615 }.Select(x => Format(two, x)));

        var even = Digits(mxfd: 2, mode: RoundingMode.HalfEven);
        Assert.Equal(["1.02", "1.02", "0.12"], new[] { 1.015, 1.025, 0.125 }.Select(x => Format(even, x)));

        Assert.Equal("1.005", Format(Digits(mnsd: 4), 1.005));
        Assert.Equal("0.30000000000000004", Format(Digits(mxsd: 21), 0.1 + 0.2));
        Assert.Equal("0.1", Format(Digits(mxfd: 20), 0.1));
        Assert.Equal("10.0", Format(Digits(mnsd: 3, mxsd: 3), 9.995));
        Assert.Equal("10", Format(Digits(mxsd: 3), 9.995));
    }

    /// <summary>Rows M2a and M2b: a String is read exactly, and held to the Number range as RoundMVResult holds it.</summary>
    [Fact]
    public void Rows_M2a_and_M2b_a_String_is_exact_and_held_to_the_Number_range()
    {
        IntlDecimal Of(string text) => IntlDecimal.FromString(text, Policy);

        Assert.Equal(IntlDecimal.Finite(false, "115", 1), Of("1.15"));
        Assert.Equal(IntlDecimal.Finite(false, "31", 2), Of("0x1F"));
        Assert.Equal(IntlDecimal.Finite(false, "1", 4), Of(" 1e3 "));
        Assert.Equal(IntlDecimal.Finite(false, "5", 0), Of(".5"));
        Assert.Equal(IntlDecimal.Finite(false, "5", 1), Of("5."));
        Assert.Equal(IntlDecimal.Finite(false, "5", 1), Of("+.5e1"));
        Assert.Equal(IntlDecimal.Finite(true, "5", -1), Of("-.5E-1"));
        Assert.Equal(IntlDecimal.Zero, Of(""));
        Assert.Equal(IntlDecimal.Zero, Of("\u00A0\uFEFF\u2028\u2029"));
        Assert.Equal(IntlDecimal.NegativeZero, Of("-0"));
        Assert.Equal(IntlDecimal.PositiveInfinity, Of("Infinity"));
        Assert.Equal(IntlDecimal.NegativeInfinity, Of("-Infinity"));

        foreach (var refused in new[] { "-0x1F", "0x", "1_000", "1e", "1n", "infinity", "\u0085" + "1" })
        {
            Assert.Equal(IntlDecimalKind.NaN, Of(refused).Kind);
        }

        Assert.Equal(IntlDecimal.PositiveInfinity, Of("1e309"));
        Assert.Equal(IntlDecimal.PositiveInfinity, Of("1.7976931348623159e308"));
        Assert.True(Of("1.7976931348623158e308").IsFinite);
        Assert.True(Of("2.4703282292062328e-324").IsFinite);
        Assert.Equal(IntlDecimal.Zero, Of("2.4703282292062327e-324"));
        Assert.Equal(IntlDecimal.Zero, Of("1e-400"));
        Assert.Equal(IntlDecimal.NegativeZero, Of("-1e-400"));

        Assert.Equal(["0", "0", "-0"], new[] { "2.4703282292062327e-324", "1e-400", "-1e-400" }.Select(t => Format(Digits(mxsd: 2), Of(t))));
        Assert.Equal("0", Format(Digits(mxfd: 1, mode: RoundingMode.Ceil), Of("1e-400")));
        Assert.Equal("9007199254740993", Format(Digits(), Of("9007199254740993")));
        Assert.Equal("0.123", Format(Digits(mxfd: 3), Of("0.1234999999999999999999")));
    }

    /// <summary>Row M3: a BigInt is exact, in every notation and past the Number range.</summary>
    [Fact]
    public void Row_M3_a_BigInt_is_exact()
    {
        var power = BigInteger.Pow(10, 400);
        var value = IntlDecimal.FromBigInt(power);
        Assert.Equal(IntlDecimal.Finite(false, "1", 401), value);
        Assert.Equal("1" + new string('0', 400), Format(Digits(), value));
        Assert.Equal("-1" + new string('0', 400), Format(Digits(), IntlDecimal.FromBigInt(-power)));
        Assert.Equal("123456789012345678901", Format(Digits(mxsd: 21), IntlDecimal.FromBigInt(BigInteger.Parse("123456789012345678901", System.Globalization.NumberFormatInfo.InvariantInfo))));
        Assert.Equal("5", Format(Digits(), IntlDecimal.FromBigInt(5)));
    }

    /// <summary>Row M4: SetNumberFormatDigitOptions' refusals, each a status.</summary>
    [Fact]
    public void Row_M4_digit_option_refusals_are_statuses()
    {
        Assert.Equal(IntlStatus.FractionDigitsOutOfOrder, TryDigits(out _, mnfd: 3, mxfd: 2));
        Assert.Equal(IntlStatus.NumberOptionOutOfRange, TryDigits(out _, mnsd: 5, mxsd: 4));
        Assert.Equal(IntlStatus.NumberOptionOutOfRange, TryDigits(out _, mxfd: 101));
        Assert.Equal(IntlStatus.NumberOptionOutOfRange, TryDigits(out _, mxsd: 22));
        Assert.Equal(IntlStatus.NumberOptionOutOfRange, TryDigits(out _, mnfd: double.NaN));
        Assert.Equal(IntlStatus.RoundingIncrementNotAdmitted, TryDigits(out _, increment: 3));
        Assert.Equal(IntlStatus.RoundingIncrementNeedsFractionDigits, TryDigits(out _, increment: 5, mxsd: 2));
        Assert.Equal(IntlStatus.RoundingIncrementNeedsFractionDigits, TryDigits(out _, increment: 5, priority: RoundingPriority.MorePrecision));
        Assert.Equal(IntlStatus.RoundingIncrementNeedsEqualFractionDigits, TryDigits(out _, increment: 5, mnfd: 2, mxfd: 3));
        Assert.Equal(IntlStatus.NumberOptionOutOfRange, DigitOptionRules.DefaultNumberOption(0, 1, 21, out _));
        Assert.Equal(IntlStatus.Completed, DigitOptionRules.DefaultNumberOption(2.9, 1, 21, out var floor));
        Assert.Equal(2, floor);

        // Significant digits win under "auto", so the fraction pair is neither converted nor checked.
        Assert.Equal(new DigitOptionsPlan(true, false), DigitOptionRules.Plan(true, true, RoundingPriority.Auto, NumberNotation.Standard));
        Assert.Equal(IntlStatus.Completed, TryDigits(out _, mnfd: 3, mxfd: 2, mxsd: 2));
    }

    /// <summary>Row M5: the defaults SetNumberFormatDigitOptions is handed, and what it resolves from them.</summary>
    [Fact]
    public void Row_M5_the_defaults_and_their_resolution()
    {
        Assert.Equal(new FractionDigitDefaults(0, 3), DigitOptionRules.NumberFormatDefaults(NumberStyle.Decimal, null, NumberNotation.Standard, Data));
        Assert.Equal(new FractionDigitDefaults(0, 0), DigitOptionRules.NumberFormatDefaults(NumberStyle.Percent, null, NumberNotation.Standard, Data));
        Assert.Equal(new FractionDigitDefaults(0, 0), DigitOptionRules.NumberFormatDefaults(NumberStyle.Currency, "jpy", NumberNotation.Standard, Data));
        Assert.Equal(new FractionDigitDefaults(3, 3), DigitOptionRules.NumberFormatDefaults(NumberStyle.Currency, "KWD", NumberNotation.Standard, Data));
        Assert.Equal(new FractionDigitDefaults(2, 2), DigitOptionRules.NumberFormatDefaults(NumberStyle.Currency, "EUR", NumberNotation.Standard, Data));
        Assert.Equal(new FractionDigitDefaults(0, 3), DigitOptionRules.NumberFormatDefaults(NumberStyle.Currency, "EUR", NumberNotation.Scientific, Data));

        Assert.Equal(new DigitOptions(1, 0, 0, 1, 2, NumberRoundingType.MorePrecision, RoundingPriority.MorePrecision, 1, RoundingMode.HalfExpand, TrailingZeroDisplay.Auto),
            Digits(notation: NumberNotation.Compact));
        Assert.Equal(new DigitOptions(1, 0, 1, null, null, NumberRoundingType.FractionDigits, RoundingPriority.Auto, 1, RoundingMode.HalfExpand, TrailingZeroDisplay.Auto),
            Digits(notation: NumberNotation.Compact, mxfd: 1));
        Assert.Equal(new DigitOptions(1, null, null, 1, 3, NumberRoundingType.SignificantDigits, RoundingPriority.Auto, 1, RoundingMode.HalfExpand, TrailingZeroDisplay.Auto),
            Digits(mxsd: 3));
        Assert.Equal(new DigitOptions(1, 0, 3, 1, 21, NumberRoundingType.LessPrecision, RoundingPriority.LessPrecision, 1, RoundingMode.HalfExpand, TrailingZeroDisplay.Auto),
            Digits(priority: RoundingPriority.LessPrecision));
        Assert.Equal(new DigitOptions(1, 0, 0, null, null, NumberRoundingType.FractionDigits, RoundingPriority.Auto, 1, RoundingMode.HalfExpand, TrailingZeroDisplay.Auto),
            Digits(mxfdDefault: 0));

        Assert.Equal(["13", "50", "0"], new[] { 12.5, 50, 0.49 }.Select(x => Format(Digits(mxfdDefault: 0), x)));

        var twentyFive = Digits(increment: 25, mnfd: 2);
        Assert.Equal(2, twentyFive.MaximumFractionDigits);
        Assert.Equal("1.25", Format(twentyFive, 1.13));
        Assert.Equal("5", Format(Digits(increment: 5), 7));
    }

    /// <summary>Row M6: every rounding mode at ties, both signs, with an increment and both priorities.</summary>
    [Fact]
    public void Row_M6_rounding_modes_increments_and_priorities()
    {
        var expected = new Dictionary<RoundingMode, string[]>
        {
            [RoundingMode.Ceil] = ["1", "-0", "1", "-0", "2", "-1", "3", "-2"],
            [RoundingMode.Floor] = ["0", "-1", "0", "-1", "1", "-2", "2", "-3"],
            [RoundingMode.Expand] = ["1", "-1", "1", "-1", "2", "-2", "3", "-3"],
            [RoundingMode.Trunc] = ["0", "-0", "0", "-0", "1", "-1", "2", "-2"],
            [RoundingMode.HalfCeil] = ["0", "-0", "1", "-0", "2", "-1", "3", "-2"],
            [RoundingMode.HalfFloor] = ["0", "-0", "0", "-1", "1", "-2", "2", "-3"],
            [RoundingMode.HalfExpand] = ["0", "-0", "1", "-1", "2", "-2", "3", "-3"],
            [RoundingMode.HalfTrunc] = ["0", "-0", "0", "-0", "1", "-1", "2", "-2"],
            [RoundingMode.HalfEven] = ["0", "-0", "0", "-0", "2", "-2", "2", "-2"],
        };

        foreach (var (mode, answers) in expected)
        {
            var options = Digits(mxfd: 0, mode: mode);
            Assert.Equal(answers, new[] { 0.4, -0.4, 0.5, -0.5, 1.5, -1.5, 2.5, -2.5 }.Select(x => Format(options, x)));
        }

        Assert.Equal("1.25", Format(Digits(increment: 25, mnfd: 2, mxfd: 2), 1.125));
        Assert.Equal("1.0000", Format(Digits(increment: 5000, mnfd: 4, mxfd: 4), 1.24));
        Assert.Equal("1.5000", Format(Digits(increment: 5000, mnfd: 4, mxfd: 4), 1.25));
        Assert.Equal("0.00", Format(Digits(mnsd: 3, mxsd: 3), 0));
        Assert.Equal("0.000123", Format(Digits(mnsd: 3, mxsd: 3), 0.000123456));
        Assert.Equal("0.000123", Format(Digits(priority: RoundingPriority.MorePrecision, mxsd: 3), 0.000123456));
        Assert.Equal("0", Format(Digits(priority: RoundingPriority.LessPrecision, mxsd: 3), 0.000123456));
        Assert.Equal("007", Format(Digits(mnid: 3), 7));
    }

    /// <summary>Row M7: <c>trailingZeroDisplay: "stripIfInteger"</c> cuts the fraction of a rounded integer.</summary>
    [Fact]
    public void Row_M7_stripIfInteger()
    {
        var options = Digits(mnfd: 2, trailingZeroDisplay: TrailingZeroDisplay.StripIfInteger);
        Assert.Equal(["5", "5.10", "5", "5.001"], new[] { 5, 5.1, 4.9999, 5.001 }.Select(x => Format(options, x)));
        Assert.Equal("1", Format(Digits(mnsd: 2, mxsd: 2, trailingZeroDisplay: TrailingZeroDisplay.StripIfInteger), 1));
    }

    /// <summary>Row M8: negative zero keeps its sign through rounding, and a negative value that rounds to zero is negative zero.</summary>
    [Fact]
    public void Row_M8_negative_zero()
    {
        Assert.Equal(IntlDecimal.NegativeZero, IntlDecimal.FromNumber(-0.0));
        Assert.Equal("-0", Format(Digits(), -0.0));
        Assert.Equal("-0", Format(Digits(), -0.0001));
        Assert.Equal("-0", Format(Digits(mxfd: 0), -0.4));
        Assert.Equal("other", Rules("en").ResolvePlural(IntlDecimal.NegativeZero).PluralCategory);
    }

    /// <summary>Row M9 (decision D18): the String's and FormatNumericToString's charges, in Broiler.VM's units, before the work.</summary>
    [Fact]
    public void Row_M9_the_charges_are_Broiler_VM_s()
    {
        ulong charged = 0;
        WorkCharge meter = units => charged += units;
        var text = "0." + new string('1', 1_000_000);
        var value = IntlDecimal.FromString(text, Policy, meter);
        Assert.Equal((ulong)text.Length / 8 + 1, charged);

        charged = 0;
        var result = NumericFormat.FormatNumericToString(Digits(mxfd: 100), value, meter);
        Assert.Equal((ulong)(1_000_000 / 16) + 1, charged);
        Assert.Equal(102, result.FormattedString.Length);

        // A meter that throws leaves nothing done.
        Assert.Throws<InvalidOperationException>(() => IntlDecimal.FromString(text, Policy, _ => throw new InvalidOperationException()));
    }

    // ---- P: plural rules and plural ranges -------------------------------------------------------------------------------

    /// <summary>Row P1: select reads FormatNumericToString's String, with the digit options.</summary>
    [Fact]
    public void Row_P1_select_reads_the_formatted_String()
    {
        Assert.Equal("other", Select(Rules("en", digits: Digits(mnfd: 1)), 1));
        Assert.Equal("other", Select(Rules("de", digits: Digits(mnfd: 1)), 1));
        Assert.Equal(["one", "one"], new[] { 1.4, 0.6 }.Select(x => Select(Rules("en", digits: Digits(mxfd: 0)), x)));
        Assert.Equal(["one", "one", "other"], new[] { 1.0001, 0.9999, 1.0005 }.Select(x => Select(Rules("en"), x)));
        Assert.Equal("other", Select(Rules("en", digits: Digits(mxsd: 1)), 1.5));
        Assert.Equal("other", Select(Rules("en", digits: Digits(mnsd: 2)), 1));
        Assert.Equal("one", Select(Rules("en", PluralRuleType.Ordinal, digits: Digits(mxfd: 0)), 1.4));
        Assert.Equal(["one", "one"], new[] { 0.999, 1.004 }.Select(x => Select(Rules("en", notation: NumberNotation.Compact), x)));
        Assert.Equal("1.001", Rules("en").ResolvePlural(IntlDecimal.FromNumber(1.0005)).FormattedString);
    }

    /// <summary>Row P2: a BigInt selects by its exact value.</summary>
    [Fact]
    public void Row_P2_a_BigInt_selects()
    {
        var en = Rules("en");
        Assert.Equal(["one", "other", "other", "one"], new BigInteger[] { 1, 2, 0, -1 }.Select(x => en.ResolvePlural(IntlDecimal.FromBigInt(x)).PluralCategory));
        var ordinal = Rules("en", PluralRuleType.Ordinal);
        Assert.Equal(["two", "few"], new BigInteger[] { 2, 3 }.Select(x => ordinal.ResolvePlural(IntlDecimal.FromBigInt(x)).PluralCategory));
        Assert.Equal("other", en.ResolvePlural(IntlDecimal.FromBigInt(BigInteger.Pow(10, 30))).PluralCategory);
        Assert.Equal("other", en.ResolvePlural(IntlDecimal.FromBigInt(BigInteger.Pow(10, 400))).PluralCategory);
        Assert.Equal("other", Rules("en", digits: Digits(mnfd: 1)).ResolvePlural(IntlDecimal.FromBigInt(1)).PluralCategory);
        Assert.Equal("one", Range(Rules("de"), IntlDecimal.FromBigInt(0), IntlDecimal.FromBigInt(1)));
    }

    /// <summary>Row P3: every CLDR language's rules in Broiler.JS's embedding; the categories in resolvedOptions' order.</summary>
    [Fact]
    public void Row_P3_the_languages_rules_and_categories()
    {
        Assert.Equal(["one", "many", "other"], Rules("fr").PluralCategories);
        Assert.Equal(["one", "few", "many", "other"], Rules("ru").PluralCategories);
        Assert.Equal(["zero", "one", "two", "few", "many", "other"], Rules("ar").PluralCategories);
        Assert.Equal(["other"], Rules("ja").PluralCategories);
        Assert.Equal(["one", "other"], Rules("en").PluralCategories);
        Assert.Equal(["one", "two", "few", "other"], Rules("en", PluralRuleType.Ordinal).PluralCategories);
        Assert.Equal(["other"], Rules("zz").PluralCategories);
        Assert.Equal(["one", "few", "many", "one", "other"], new[] { 1, 2, 5, 21, 1.5 }.Select(x => Select(Rules("ru"), x)));
    }

    /// <summary>Row P4 and decision D20: the compact exponent reaches the operands c and e; en and de read neither.</summary>
    [Fact]
    public void Row_P4_the_compact_exponent()
    {
        Assert.Equal(["one", "other", "other", "other"], new[] { 1, 1000, 1000000, 1200000 }.Select(x => Select(Rules("en", notation: NumberNotation.Compact), x)));
        Assert.Equal(["one", "other", "other", "other"], new[] { 1, 1000, 1000000, 1200000 }.Select(x => Select(Rules("de", notation: NumberNotation.Compact), x)));
        Assert.Equal(["many", "many", "other"], new[] { 1000000, 1200000, 2000 }.Select(x => Select(Rules("fr", notation: NumberNotation.Compact), x)));
        Assert.Equal(["many", "other"], new[] { 1000000, 1200000 }.Select(x => Select(Rules("fr"), x)));
        Assert.Equal("many", Select(Rules("fr", notation: NumberNotation.Compact, compactDisplay: CompactDisplay.Long), 1000000));

        // The operands themselves, through the engine.
        var fr = PluralEngine.Create(Data, "fr", PluralRuleType.Cardinal);
        Assert.Equal("many", fr.Select("1000000", 0));
        Assert.Equal("many", fr.Select("1200000", 6));
        Assert.Equal("other", fr.Select("1200000", 0));
        Assert.Equal("one", fr.Select("1.5"));
        Assert.Throws<ArgumentException>(() => fr.Select("-1"));
    }

    /// <summary>Rows P5a, P5b and P5c: a range's category.</summary>
    [Fact]
    public void Rows_P5a_P5b_P5c_ranges()
    {
        IntlDecimal N(double x) => IntlDecimal.FromNumber(x);
        var en = Rules("en");
        Assert.Equal(["one", "one", "one"], new[] { (1.0, 1.0), (1, 1.0001), (-1, 1) }.Select(p => Range(en, N(p.Item1), N(p.Item2))));
        Assert.Equal("one", Range(en, N(1), IntlDecimal.FromString("1.0", Policy)));
        Assert.Equal("one", Range(Rules("de"), N(1), N(1)));
        Assert.Equal("one", Range(Rules("en", PluralRuleType.Ordinal), N(1), N(1)));
        Assert.Equal("other", Range(Rules("en", digits: Digits(mnfd: 1)), N(1), N(1)));

        var de = Rules("de");
        Assert.Equal(["one", "other", "one", "other"], new[] { (0.0, 1.0), (1, 2), (2, 1), (1, 1.5) }.Select(p => Range(de, N(p.Item1), N(p.Item2))));
        Assert.Equal(["other", "other", "other"], new[] { (0.0, 1.0), (1, 2), (2, 1) }.Select(p => Range(en, N(p.Item1), N(p.Item2))));
        var ordinal = Rules("en", PluralRuleType.Ordinal);
        Assert.Equal(["other", "other"], new[] { (1.0, 2.0), (2, 3) }.Select(p => Range(ordinal, N(p.Item1), N(p.Item2))));

        Assert.Equal(IntlStatus.RangeIsNotANumber, en.ResolvePluralRange(IntlDecimal.NaN, N(1), out _));
        Assert.Equal(IntlStatus.RangeIsNotANumber, en.ResolvePluralRange(N(1), IntlDecimal.NaN, out _));
        Assert.Equal("other", Range(en, N(2), N(1)));
        Assert.Equal("other", Range(en, IntlDecimal.NegativeInfinity, IntlDecimal.PositiveInfinity));
    }

    /// <summary>Row P8: unrounded categories, the non-finite values and Strings.</summary>
    [Fact]
    public void Row_P8_unrounded_categories()
    {
        var ordinal = Rules("en", PluralRuleType.Ordinal);
        Assert.Equal(
            ["one", "two", "few", "other", "other", "other", "other", "one", "two", "few", "one", "other", "other"],
            new[] { 1, 2, 3, 4, 11, 12, 13, 21, 22, 23, 101, 111, 1.5 }.Select(x => Select(ordinal, x)));
        var de = Rules("de");
        Assert.Equal(["other", "one", "other", "other"], new[] { 0, 1, 2, 1.5 }.Select(x => Select(de, x)));
        Assert.Equal("other", Select(Rules("de", PluralRuleType.Ordinal), 1));

        var en = Rules("en");
        Assert.Equal(new ResolvedPlural("other", "NaN"), en.ResolvePlural(IntlDecimal.NaN));
        Assert.Equal(new ResolvedPlural("other", "Infinity"), en.ResolvePlural(IntlDecimal.PositiveInfinity));
        Assert.Equal(new ResolvedPlural("other", "-Infinity"), en.ResolvePlural(IntlDecimal.NegativeInfinity));
        Assert.Equal(["other", "one", "one", "other"], new[] { 1e21, -1, 1.0, 1.5 }.Select(x => Select(en, x)));
        Assert.Equal(["one", "one", "other"], new[] { "1.0", " 1 ", "1.50" }.Select(t => en.ResolvePlural(IntlDecimal.FromString(t, Policy)).PluralCategory));
    }

    /// <summary>The engine's own reading of rule text, in UTS #35's syntax, and its refusal of text that is not.</summary>
    [Fact]
    public void The_rule_text_reader()
    {
        var data = new TableLocaleData(new TextDataSet(new Dictionary<IntlTable, string>
        {
            [IntlTable.Plurals] = "xx|one|n % 10 = 1 and n % 100 != 11\nxx|few|v = 0 and i % 10 = 2..4,7 or f = 5\nxx|other|\nyy|one|i = \n",
        }));

        var xx = PluralEngine.Create(data, "xx", PluralRuleType.Cardinal);
        Assert.Equal(["one", "few", "other"], xx.Categories);
        Assert.Equal(["one", "other", "few", "few", "few", "other"], new[] { "21", "11", "3", "7", "1.5", "5" }.Select(t => xx.Select(t)));
        Assert.Equal("one", xx.Select("1000000000000000000001"));
        Assert.Throws<System.IO.InvalidDataException>(() => PluralEngine.Create(data, "yy", PluralRuleType.Cardinal));

        ulong charged = 0;
        _ = xx.Select("12345", 0, units => charged += units);
        Assert.Equal(5UL, charged);
    }
}
