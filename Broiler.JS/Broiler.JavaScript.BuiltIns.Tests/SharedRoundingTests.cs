using System.Numerics;
using System.Runtime.CompilerServices;
using Broiler.ECMAScript.Numbers;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// The corrections milestone SJL-4.5 makes in exact rounding, where Temporal's and Intl's rounding
// modes, increment roundings, NudgeToCalendarUnit's window and the nearest double of a ratio moved to
// the shared library Broiler.ECMAScript.Algorithms (ExactRounding and RoundingMode), and the built-ins
// kept their option reading, coercions and RangeErrors. The modes themselves are judged by the shared
// corpus (conformance/vectors/numerics/rounding.vectors), which this engine's engine-level run reads;
// the cases here are the consumers the corpus does not hold, and a few that agreed before and must
// still agree.
public class SharedRoundingTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    private static string Throws(string expression)
        => Eval("try { " + expression + "; 'no error' } catch (e) { e.constructor.name }");

    private static string Format(string options, string value)
        => Eval("new Intl.NumberFormat('en', " + options + ").format(" + value + ")");

    private static string Round(string duration, string options)
        => Eval("Temporal.Duration.from(" + duration + ").round(" + options + ").toString()");

    // ECMA-402 ComputeExponent: whether the mantissa carries to the next power of ten is the
    // formatter's own rounding under roundingMode, the increment and the digit options; the
    // platform's away-from-zero Math.Round decided it before, so "floor" carried 9.99 to "0.9E1".
    [Theory]
    [InlineData("{notation: 'scientific', maximumFractionDigits: 1, roundingMode: 'floor'}", "9.99", "9.9E0")]
    [InlineData("{notation: 'scientific', maximumFractionDigits: 1, roundingMode: 'trunc'}", "-9.99", "-9.9E0")]
    [InlineData("{notation: 'engineering', maximumFractionDigits: 1, roundingMode: 'floor'}", "999.99", "999.9E0")]
    [InlineData("{notation: 'scientific', maximumSignificantDigits: 2}", "9.99", "1E1")]
    public void The_scientific_exponent_follows_the_rounding_mode(string options, string value, string expected)
        => Assert.Equal(expected, Format(options, value));

    // The carry the default mode makes is unchanged.
    [Theory]
    [InlineData("{notation: 'scientific', maximumFractionDigits: 1}", "9.99", "1E1")]
    [InlineData("{notation: 'engineering', maximumFractionDigits: 1}", "999.99", "1E3")]
    [InlineData("{notation: 'scientific'}", "123456", "1.235E5")]
    [InlineData("{notation: 'scientific', roundingMode: 'floor'}", "123456", "1.234E5")]
    public void The_scientific_exponent_still_carries_where_the_mode_rounds_up(string options, string value, string expected)
        => Assert.Equal(expected, Format(options, value));

    // The compact notation rounds its reduced value x × 10^-exponent exactly, under roundingMode,
    // where Math.Round rounded it away from zero.
    [Theory]
    [InlineData("{notation: 'compact', roundingMode: 'floor'}", "1990", "1.9K")]
    [InlineData("{notation: 'compact', roundingMode: 'floor'}", "-1910", "-2K")]
    [InlineData("{notation: 'compact', roundingMode: 'trunc'}", "-1990", "-1.9K")]
    [InlineData("{notation: 'compact', roundingMode: 'halfEven'}", "1250", "1.2K")]
    public void The_compact_notation_follows_the_rounding_mode(string options, string value, string expected)
        => Assert.Equal(expected, Format(options, value));

    [Theory]
    [InlineData("{notation: 'compact'}", "1990", "2K")]
    [InlineData("{notation: 'compact'}", "1150", "1.2K")]
    [InlineData("{notation: 'compact'}", "1450", "1.5K")]
    [InlineData("{notation: 'compact'}", "987654321", "988M")]
    public void The_compact_notation_s_default_rounding_is_unchanged(string options, string value, string expected)
        => Assert.Equal(expected, Format(options, value));

    // ToRawFixed with an increment: "halfEven" picks the even multiple of the increment.
    [Theory]
    [InlineData("halfEven", "1.00")]
    [InlineData("halfExpand", "1.25")]
    [InlineData("halfTrunc", "1.00")]
    [InlineData("ceil", "1.25")]
    public void A_rounding_increment_ties_by_the_mode(string mode, string expected)
        => Assert.Equal(expected, Format(
            "{minimumFractionDigits: 2, maximumFractionDigits: 2, roundingIncrement: 25, roundingMode: '" + mode + "'}", "1.125"));

    [Theory]
    [InlineData("floor", "-2")]
    [InlineData("ceil", "-1")]
    [InlineData("halfFloor", "-2")]
    [InlineData("halfCeil", "-1")]
    public void A_negative_value_rounds_by_the_unsigned_mode_for_its_sign(string mode, string expected)
        => Assert.Equal(expected, Format("{maximumFractionDigits: 0, roundingMode: '" + mode + "'}", "-1.5"));

    // GetOption: each of the nine names is accepted and reported; any other string is a RangeError.
    [Theory]
    [InlineData("ceil")]
    [InlineData("floor")]
    [InlineData("expand")]
    [InlineData("trunc")]
    [InlineData("halfCeil")]
    [InlineData("halfFloor")]
    [InlineData("halfExpand")]
    [InlineData("halfTrunc")]
    [InlineData("halfEven")]
    public void Every_rounding_mode_name_is_accepted_by_NumberFormat(string mode)
        => Assert.Equal(mode, Eval("new Intl.NumberFormat('en', {roundingMode: '" + mode + "'}).resolvedOptions().roundingMode"));

    [Fact]
    public void NumberFormat_reports_halfExpand_by_default()
        => Assert.Equal("halfExpand", Eval("new Intl.NumberFormat('en').resolvedOptions().roundingMode"));

    [Theory]
    [InlineData("new Intl.NumberFormat('en', {roundingMode: 'halfUp'})")]
    [InlineData("new Intl.NumberFormat('en', {roundingMode: 'HalfExpand'})")]
    [InlineData("new Intl.NumberFormat('en', {roundingMode: ''})")]
    [InlineData("new Intl.PluralRules('en', {roundingMode: 'up'})")]
    [InlineData("new Intl.NumberFormat('en', {roundingIncrement: 3})")]
    public void An_unknown_Intl_rounding_option_is_a_RangeError(string expression)
        => Assert.Equal("RangeError", Throws(expression));

    [Fact]
    public void The_rounding_mode_option_is_converted_with_ToString()
        => Assert.Equal("floor", Eval(
            "new Intl.NumberFormat('en', {roundingMode: {toString() { return 'floor'; }}}).resolvedOptions().roundingMode"));

    // NudgeToCalendarUnit rounds over the increment's window [r1, r2], not linearly inside one unit:
    // from 2023-01-01 a two-month window is January and February, and one month is past its middle.
    [Theory]
    [InlineData("'2023-01-01'", "halfTrunc", "P2M")]
    [InlineData("'2023-01-01'", "halfExpand", "P2M")]
    [InlineData("'2023-01-01'", "trunc", "PT0S")]
    [InlineData("'2023-02-01'", "halfTrunc", "PT0S")]
    [InlineData("'2023-02-01'", "ceil", "P2M")]
    [InlineData("'2023-01-01[UTC]'", "halfTrunc", "P2M")]
    public void A_calendar_increment_rounds_over_its_window(string relativeTo, string mode, string expected)
        => Assert.Equal(expected, Round("'P1M'",
            "{smallestUnit: 'months', largestUnit: 'months', roundingIncrement: 2, roundingMode: '" + mode + "', relativeTo: " + relativeTo + "}"));

    [Fact]
    public void A_date_difference_rounds_over_the_same_window()
        => Assert.Equal("P2M", Eval(
            "Temporal.PlainDate.from('2023-01-01').until('2023-02-01', {smallestUnit: 'months', roundingIncrement: 2, roundingMode: 'halfTrunc'}).toString()"));

    // Duration.prototype.total with a ZonedDateTime relativeTo is the exact ratio's nearest double,
    // rounded once; it was r1 plus a separately rounded fraction, one unit in the last place off.
    [Theory]
    [InlineData("Temporal.ZonedDateTime.from('2020-01-01T00:00[UTC]')")]
    [InlineData("'2020-01-01'")]
    public void A_calendar_total_is_the_nearest_double_of_the_exact_ratio(string relativeTo)
        => Assert.Equal("true", Eval(
            "Temporal.Duration.from({years: 5, seconds: 26368, nanoseconds: 123456789})"
            + ".total({unit: 'years', relativeTo: " + relativeTo + "}) === 5.000836127709817"));

    [Fact]
    public void A_time_total_is_the_nearest_double_of_the_exact_ratio()
        => Assert.Equal("9007199254740992", Eval(
            "String(Temporal.Duration.from({seconds: 9007199254740991, nanoseconds: 999999999}).total('seconds'))"));

    // The engine's ratio-to-double conversion was replaced by the library's, which is correctly
    // rounded over the whole range: the old one shifted by 256 bits and overflowed to Infinity near
    // 2^800, and flushed a subnormal result to zero. No Temporal input reaches either range, so the
    // conversion the engine now calls is checked directly.
    [Fact]
    public void The_nearest_double_of_a_ratio_is_correctly_rounded_over_the_whole_range()
    {
        Assert.Equal(Math.Pow(2, 800), ExactRounding.ToDouble(BigInteger.Pow(2, 800), BigInteger.One));
        Assert.Equal(Math.Pow(2, 800) / 3, ExactRounding.ToDouble(BigInteger.Pow(2, 800), 3));
        Assert.Equal(1.5e-323, ExactRounding.ToDouble(5 * BigInteger.Pow(2, 125) + 1, BigInteger.Pow(2, 1200)));
    }

    // Unchanged: each half mode on -1.5 minutes (the signed modes reduce to the unsigned ones by sign).
    [Theory]
    [InlineData("ceil", "-PT1M")]
    [InlineData("floor", "-PT2M")]
    [InlineData("expand", "-PT2M")]
    [InlineData("trunc", "-PT1M")]
    [InlineData("halfCeil", "-PT1M")]
    [InlineData("halfFloor", "-PT2M")]
    [InlineData("halfExpand", "-PT2M")]
    [InlineData("halfTrunc", "-PT1M")]
    [InlineData("halfEven", "-PT2M")]
    public void Every_rounding_mode_name_is_accepted_by_Temporal(string mode, string expected)
        => Assert.Equal(expected, Round("'-PT1M30S'", "{smallestUnit: 'minute', roundingMode: '" + mode + "'}"));

    [Theory]
    [InlineData("Temporal.Duration.from('PT1S').round({smallestUnit: 'second', roundingMode: 'halfUp'})")]
    [InlineData("Temporal.Duration.from('PT1S').round({smallestUnit: 'second', roundingMode: 'HalfExpand'})")]
    [InlineData("Temporal.Instant.fromEpochMilliseconds(0).round({smallestUnit: 'second', roundingMode: 'up'})")]
    [InlineData("Temporal.PlainTime.from('01:00').toString({roundingMode: ''})")]
    [InlineData("Temporal.PlainDate.from('2020-01-01').until('2020-02-01', {roundingMode: 'nearest'})")]
    public void An_unknown_Temporal_rounding_mode_is_a_RangeError(string expression)
        => Assert.Equal("RangeError", Throws(expression));

    [Fact]
    public void A_Symbol_rounding_mode_is_a_TypeError()
        => Assert.Equal("TypeError", Throws(
            "Temporal.Duration.from('PT1S').round({smallestUnit: 'second', roundingMode: Symbol()})"));

    // `since` negates the rounding mode (NegateRoundingMode): floor on a negative difference rounds
    // toward -Infinity.
    [Theory]
    [InlineData("Temporal.Instant.fromEpochMilliseconds(0).since(Temporal.Instant.fromEpochMilliseconds(90000), {smallestUnit: 'minute', roundingMode: 'floor'})", "-PT2M")]
    [InlineData("Temporal.Instant.fromEpochMilliseconds(0).since(Temporal.Instant.fromEpochMilliseconds(90000), {smallestUnit: 'minute', roundingMode: 'ceil'})", "-PT1M")]
    [InlineData("Temporal.Instant.fromEpochMilliseconds(0).until(Temporal.Instant.fromEpochMilliseconds(90000), {smallestUnit: 'minute', roundingMode: 'floor'})", "PT1M")]
    [InlineData("Temporal.PlainTime.from('00:00').since('00:01:30', {smallestUnit: 'minute', roundingMode: 'floor'})", "-PT2M")]
    [InlineData("Temporal.PlainTime.from('00:00').since('00:01:30', {smallestUnit: 'minute', roundingMode: 'halfCeil'})", "-PT1M")]
    [InlineData("Temporal.PlainDate.from('2020-01-01').since('2020-02-16', {smallestUnit: 'month', roundingMode: 'floor'})", "-P2M")]
    [InlineData("Temporal.PlainDate.from('2020-01-01').since('2020-02-16', {smallestUnit: 'month', roundingMode: 'ceil'})", "-P1M")]
    public void Since_negates_the_rounding_mode(string expression, string expected)
        => Assert.Equal(expected, Eval(expression + ".toString()"));

    // An increment that does not divide the next unit is a RangeError.
    [Theory]
    [InlineData("Temporal.Duration.from('PT1H').round({smallestUnit: 'minute', roundingIncrement: 7})")]
    [InlineData("Temporal.Instant.fromEpochMilliseconds(0).round({smallestUnit: 'hour', roundingIncrement: 5})")]
    [InlineData("Temporal.PlainTime.from('01:00').round({smallestUnit: 'second', roundingIncrement: 7})")]
    public void An_increment_that_does_not_divide_is_a_RangeError(string expression)
        => Assert.Equal("RangeError", Throws(expression));

    // Values beyond 2^53 round exactly.
    [Theory]
    [InlineData("halfEven", "1000000000000000000")]
    [InlineData("halfExpand", "1000000000000000010")]
    [InlineData("floor", "1000000000000000000")]
    public void An_instant_beyond_2_to_the_53_rounds_exactly(string mode, string expected)
        => Assert.Equal(expected, Eval(
            "String(Temporal.Instant.fromEpochNanoseconds(10n ** 18n + 5n).round({smallestUnit: 'nanosecond', roundingIncrement: 10, roundingMode: '" + mode + "'}).epochNanoseconds)"));

    [Theory]
    [InlineData("halfEven", "PT9007199254.00074S")]
    [InlineData("halfExpand", "PT9007199254.000741S")]
    public void A_duration_beyond_2_to_the_53_nanoseconds_rounds_exactly(string mode, string expected)
        => Assert.Equal(expected, Round("{seconds: 9007199254, nanoseconds: 740500}", "{smallestUnit: 'microsecond', roundingMode: '" + mode + "'}"));

    // InterpretISODateTimeOffset's match-minutes rounds the zone's offset to the minute, ties away
    // from zero: Africa/Monrovia's -00:44:30 matches "-00:45".
    [Fact]
    public void A_minute_offset_matches_the_zone_s_offset_rounded_to_the_minute()
        => Assert.Equal("1970-01-01T00:00:00-00:45[Africa/Monrovia]", Eval(
            "Temporal.ZonedDateTime.from('1970-01-01T00:00-00:45[Africa/Monrovia]').toString()"));
}
