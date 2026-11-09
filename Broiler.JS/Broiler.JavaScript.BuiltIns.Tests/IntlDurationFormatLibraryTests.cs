using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Intl.DurationFormat over the shared Intl library's DurationFormatCore (SJL-9.5, task 8.6, sub-area (e), 2026-11-09): the
// answers the cut-over corrects or takes from the data, each with its row of Broiler.VM's bundle sjl-9-001 correspondence.
// - Row U7 (a correction): a property bag's fields are read once each, in alphabetical order, by ToTemporalDuration.
// - Row U9 (a correction): a String is parsed into fields; Temporal.Duration.prototype's getters are never called.
// - Rows U1, U2 and U3 (corrections): GetDurationUnitOptions' display defaults after a numeric unit, a sub-second numeric
//   unit as "fractional" (reported "numeric"), and ValidateDurationUnitStyle's RangeError for a fraction always displayed.
// - Row U4 (a correction, noted 2026-11-09 at the review of task 8.6): a unit's display option is read before
//   ValidateDurationUnitStyle's RangeError for a word style after a numeric unit; the former engine threw before the read.
// - Row U13 (data policy): the digital clock's separator is CLDR's timeSeparator, "." for da.
// - Row U16 with decision D21 (data policy): arab digits with arab's own symbols, U+061C before the minus.
public class IntlDurationFormatLibraryTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact]
    public void A_property_bag_is_read_once_in_alphabetical_order()
        => Assert.Equal("days,hours,minutes|1 hr, 2 min", Eval(
            "var r = []; var o = { get minutes() { r.push('minutes'); return 2; }, get hours() { r.push('hours'); return 1; },"
            + " get days() { r.push('days'); return undefined; } };"
            + "var s = new Intl.DurationFormat('en').format(o); r.join() + '|' + s"));

    [Fact]
    public void A_string_is_parsed_without_the_prototype_s_getters()
        => Assert.Equal("1 hr, 2 min", Eval(
            "Object.defineProperty(Temporal.Duration.prototype, 'hours', { get() { throw new EvalError('tainted'); } });"
            + "new Intl.DurationFormat('en').format('PT1H2M')"));

    [Fact]
    public void Minutes_after_numeric_hours_are_always_displayed()
        => Assert.Equal("5:00", Eval("new Intl.DurationFormat('en', { style: 'long', minutes: 'numeric' }).format({ minutes: 5 })"));

    [Fact]
    public void A_numeric_sub_second_unit_is_reported_numeric()
        => Assert.Equal("numeric/auto", Eval(
            "var o = new Intl.DurationFormat('en', { style: 'short', milliseconds: 'numeric' }).resolvedOptions();"
            + "o.milliseconds + '/' + o.millisecondsDisplay"));

    [Fact]
    public void A_fraction_always_displayed_is_a_RangeError()
        => Assert.Equal("RangeError", Eval(
            "try { new Intl.DurationFormat('en', { milliseconds: 'numeric', millisecondsDisplay: 'always' }); 'none' } catch (e) { e.name }"));

    [Fact]
    public void The_display_option_is_read_before_a_word_style_after_a_numeric_unit_is_refused()
        => Assert.Equal("minutes,minutesDisplay|RangeError", Eval(
            "var r = []; var o = { hours: 'numeric', get minutes() { r.push('minutes'); return 'long'; },"
            + " get minutesDisplay() { r.push('minutesDisplay'); return 'auto'; } };"
            + "var n; try { new Intl.DurationFormat('en', o); n = 'none'; } catch (e) { n = e.name; } r.join() + '|' + n"));

    [Theory]
    [InlineData("en", "1:02:03")]
    [InlineData("da", "1.02.03")]
    public void The_digital_separator_is_the_locale_s(string locale, string expected)
        => Assert.Equal(expected, Eval(
            $"new Intl.DurationFormat('{locale}', {{ style: 'digital' }}).format({{ hours: 1, minutes: 2, seconds: 3 }})"));

    [Fact]
    public void Arab_digits_take_arab_s_own_symbols()
        => Assert.Equal("؜-١ hr, ٢ min, ٣ sec", Eval(
            "new Intl.DurationFormat('en', { numberingSystem: 'arab' }).format({ hours: -1, minutes: -2, seconds: -3 })"));
}
