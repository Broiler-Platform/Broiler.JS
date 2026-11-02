using System;
using System.Collections.Generic;
using System.Linq;
using Broiler.ECMAScript.Temporal;
using Xunit;

namespace Broiler.ECMAScript.Intl.Tests;

/// <summary>
/// Sub-area (e) of the Intl library (stage SJL-9.3e): duration formatting, held to the answers Broiler.VM gives at its SJL-9 rule
/// commit as the correspondence of bundle <c>sjl-9-001</c> records them, rows U1 to U22, over the data home's Broiler.JS tables in
/// the composite's order.
/// </summary>
public sealed class DurationTests
{
    private static readonly CompositeLocaleData Data = new(new TableLocaleData(HomeDataSet.Current));

    // ---- the engine's side, as an adapter does it ------------------------------------------------------------------------

    /// <summary>The unit options a constructor reads, by unit name: a style or <c>Display</c> value as the option's String.</summary>
    private static IntlStatus Options(DurationFormatStyle style, IReadOnlyDictionary<string, string> given, bool twoDigitHours, out List<DurationUnitOptions> units)
    {
        units = [];
        DurationUnitStyle? previous = null;

        foreach (var unit in Enum.GetValues<DurationUnit>())
        {
            var name = DurationFormatCore.UnitName(unit);
            DurationUnitStyle? unitStyle = given.TryGetValue(name, out var s) ? s switch
            {
                "long" => DurationUnitStyle.Long,
                "short" => DurationUnitStyle.Short,
                "narrow" => DurationUnitStyle.Narrow,
                "numeric" => DurationUnitStyle.Numeric,
                _ => DurationUnitStyle.TwoDigit,
            } : null;
            DurationUnitDisplay? display = given.TryGetValue(name + "Display", out var d) ? d == "always" ? DurationUnitDisplay.Always : DurationUnitDisplay.Auto : null;
            var status = DurationFormatCore.GetDurationUnitOptions(unit, unitStyle, display, style, previous, twoDigitHours, out var options);
            if (status != IntlStatus.Completed)
                return status;
            units.Add(options);
            if (unit is DurationUnit.Hours or DurationUnit.Minutes or DurationUnit.Seconds or DurationUnit.Milliseconds or DurationUnit.Microseconds)
                previous = options.Style;
        }

        return IntlStatus.Completed;
    }

    private static DurationFormatCore Format(string locale, DurationFormatStyle style = DurationFormatStyle.Short, string numberingSystem = "latn",
        int? fractionalDigits = null, params (string Key, string Value)[] given)
    {
        var digital = DurationFormatCore.DigitalFormat(Data, locale);
        Assert.Equal(IntlStatus.Completed, Options(style, given.ToDictionary(static g => g.Key, static g => g.Value), digital.TwoDigitHours, out var units));
        return DurationFormatCore.Create(Data, locale, locale, numberingSystem, style, units, fractionalDigits);
    }

    private static DurationFields D(double years = 0, double months = 0, double weeks = 0, double days = 0, double hours = 0, double minutes = 0,
        double seconds = 0, double milliseconds = 0, double microseconds = 0, double nanoseconds = 0) =>
        new(years, months, weeks, days, hours, minutes, seconds, milliseconds, microseconds, nanoseconds);

    private static string Resolved(DurationFormatCore format, params DurationUnit[] which) =>
        string.Join(" ", which.Select(u => DurationFormatCore.UnitName(u) + "=" + format.Units[(int)u].Style switch
        {
            DurationUnitStyle.Long => "long",
            DurationUnitStyle.Short => "short",
            DurationUnitStyle.Narrow => "narrow",
            DurationUnitStyle.TwoDigit => "2-digit",
            DurationUnitStyle.Fractional => "fractional",
            _ => "numeric",
        } + "/" + (format.Units[(int)u].Display == DurationUnitDisplay.Always ? "always" : "auto")));

    // ---- U1: the display default after a numeric unit ----------------------------------------------------------------------

    [Fact]
    public void U1_minutes_and_seconds_after_a_numeric_unit_are_displayed_always()
    {
        var minutes = Format("en", DurationFormatStyle.Long, given: ("minutes", "numeric"));
        Assert.Equal("minutes=numeric/always seconds=2-digit/always milliseconds=fractional/auto",
            Resolved(minutes, DurationUnit.Minutes, DurationUnit.Seconds, DurationUnit.Milliseconds));
        Assert.Equal("5:00", minutes.Format(D(minutes: 5)));
        Assert.Equal("0:00", minutes.Format(D(seconds: 0)));
        Assert.Equal("1 hour, 2:00", minutes.Format(D(hours: 1, minutes: 2)));
        Assert.Equal("1 day, 0:00", minutes.Format(D(days: 1)));
        Assert.Equal("-5:00", minutes.Format(D(minutes: -5, seconds: -0.0)));

        var hours = Format("en", DurationFormatStyle.Long, given: ("hours", "numeric"));
        Assert.Equal("1:00:00", hours.Format(D(hours: 1)));
        Assert.Equal("0:03:00", hours.Format(D(minutes: 3)));
        Assert.Equal("2 days, 1:00:00", hours.Format(D(days: 2, hours: 1)));
        Assert.Equal("0:00:00", hours.Format(D(hours: 0)));
        Assert.Equal("2 hr, 0:00", Format("en", given: ("minutes", "numeric")).Format(D(hours: 2)));
        Assert.Equal("2:03:00", Format("en", given: ("hours", "numeric")).Format(D(hours: 2, minutes: 3)));
    }

    // ---- U2 and U3: fractional units --------------------------------------------------------------------------------------

    [Fact]
    public void U2_a_sub_second_unit_given_numeric_is_fractional_and_displayed_auto()
    {
        Assert.Equal("milliseconds=fractional/auto", Resolved(Format("en", given: ("milliseconds", "numeric")), DurationUnit.Milliseconds));
        Assert.Equal("microseconds=fractional/auto", Resolved(Format("en", DurationFormatStyle.Narrow, fractionalDigits: 0, given: ("microseconds", "numeric")), DurationUnit.Microseconds));
    }

    [Fact]
    public void U3_and_ValidateDurationUnitStyle_refusals_are_statuses()
    {
        static IntlStatus Status(DurationFormatStyle style, params (string, string)[] given) =>
            Options(style, given.ToDictionary(static g => g.Item1, static g => g.Item2), twoDigitHours: false, out _);

        Assert.Equal(IntlStatus.FractionalDurationUnitAlwaysDisplayed, Status(DurationFormatStyle.Short, ("milliseconds", "numeric"), ("millisecondsDisplay", "always")));
        Assert.Equal(IntlStatus.FractionalDurationUnitAlwaysDisplayed, Status(DurationFormatStyle.Digital, ("millisecondsDisplay", "always")));
        Assert.Equal(IntlStatus.FractionalDurationUnitAlwaysDisplayed, Status(DurationFormatStyle.Short, ("hours", "numeric"), ("nanosecondsDisplay", "always")));
        Assert.Equal(IntlStatus.DurationUnitAfterNumeric, Status(DurationFormatStyle.Short, ("hours", "numeric"), ("minutes", "long")));
        Assert.Equal(IntlStatus.DurationUnitAfterFractional, Status(DurationFormatStyle.Short, ("milliseconds", "numeric"), ("microseconds", "long")));
        Assert.Equal(IntlStatus.DurationUnitAfterNumeric, Status(DurationFormatStyle.Short, ("seconds", "2-digit"), ("milliseconds", "short")));
    }

    [Fact]
    public void A_style_Table_20_does_not_admit_is_a_broken_precondition()
    {
        Assert.Throws<ArgumentException>(() => DurationFormatCore.GetDurationUnitOptions(DurationUnit.Days, DurationUnitStyle.Numeric, null,
            DurationFormatStyle.Short, null, false, out _));
        Assert.Throws<ArgumentException>(() => DurationFormatCore.GetDurationUnitOptions(DurationUnit.Milliseconds, DurationUnitStyle.Fractional, null,
            DurationFormatStyle.Short, null, false, out _));
        Assert.Equal(["long", "short", "narrow", "numeric", "2-digit"],
            DurationFormatCore.UnitStyles(DurationUnit.Hours).Select(static s => s switch
            {
                DurationUnitStyle.Long => "long", DurationUnitStyle.Short => "short", DurationUnitStyle.Narrow => "narrow",
                DurationUnitStyle.Numeric => "numeric", _ => "2-digit",
            }));
        Assert.Equal(4, DurationFormatCore.UnitStyles(DurationUnit.Nanoseconds).Count);
        Assert.Throws<ArgumentOutOfRangeException>(() => DurationFormatCore.UnitName((DurationUnit)10));
    }

    // ---- U4: the other option results -------------------------------------------------------------------------------------

    [Fact]
    public void U4_the_digital_defaults_and_a_numeric_minute_after_a_word_hour()
    {
        var digital = Format("en", DurationFormatStyle.Digital);
        Assert.Equal("years=short/auto days=short/auto hours=numeric/always minutes=2-digit/always seconds=2-digit/always milliseconds=fractional/auto nanoseconds=fractional/auto",
            Resolved(digital, DurationUnit.Years, DurationUnit.Days, DurationUnit.Hours, DurationUnit.Minutes, DurationUnit.Seconds, DurationUnit.Milliseconds, DurationUnit.Nanoseconds));
        Assert.Equal("hours=long/always minutes=numeric/always", Resolved(Format("en", DurationFormatStyle.Digital, given: ("hours", "long")), DurationUnit.Hours, DurationUnit.Minutes));
    }

    // ---- U5 and U6: the clock and the sign ---------------------------------------------------------------------------------

    [Fact]
    public void U5_the_clock()
    {
        var digital = Format("en", DurationFormatStyle.Digital);
        Assert.Equal("1:02:03", digital.Format(D(hours: 1, minutes: 2, seconds: 3)));
        Assert.Equal("0:05:00", digital.Format(D(minutes: 5)));
        Assert.Equal("12345:01:00", digital.Format(D(hours: 12345, minutes: 1)));
        Assert.Equal("1 day, 25:00:00", digital.Format(D(days: 1, hours: 25)));
        Assert.Equal("0:00:07.99", Format("en", DurationFormatStyle.Digital, fractionalDigits: 2).Format(D(seconds: 7, milliseconds: 999)));
        Assert.Equal("01:00:00", Format("en", DurationFormatStyle.Digital, given: ("hours", "2-digit")).Format(D(hours: 1)));
        Assert.Equal("1:00:05", Format("en", DurationFormatStyle.Digital, given: ("hoursDisplay", "auto")).Format(D(hours: 1, seconds: 5)));
        Assert.Equal("00:05", Format("en", DurationFormatStyle.Digital, given: ("hoursDisplay", "auto")).Format(D(seconds: 5)));
    }

    [Fact]
    public void U6_only_the_first_unit_written_is_signed()
    {
        Assert.Equal("-1 hr, 2 min, 3 sec", Format("en").Format(D(hours: -1, minutes: -2, seconds: -3)));
        Assert.Equal("-1:02:03", Format("en", DurationFormatStyle.Digital).Format(D(hours: -1, minutes: -2, seconds: -3)));
        Assert.Equal("-0 hr, 5 min", Format("en", given: ("hoursDisplay", "always")).Format(D(hours: 0, minutes: -5)));
        Assert.Equal("-0:05:00", Format("en", DurationFormatStyle.Digital).Format(D(hours: 0, minutes: -5)));
        Assert.Equal("-1 yr, 0 days", Format("en", given: ("daysDisplay", "always")).Format(D(years: -1, days: 0)));
        Assert.Equal("-0:00:01.5", Format("en", DurationFormatStyle.Digital).Format(D(seconds: -1, milliseconds: -500)));
        Assert.Equal("-1.5 sec", Format("en", given: ("milliseconds", "numeric")).Format(D(seconds: -1, milliseconds: -500)));
        Assert.Equal(string.Empty, Format("en").Format(D(seconds: -0.0)));
        Assert.Equal("0:00:00", Format("en", DurationFormatStyle.Digital).Format(D(seconds: -0.0)));
        Assert.Equal("-0 yrs, 1 sec", Format("en", given: ("yearsDisplay", "always")).Format(D(seconds: -1)));
    }

    // ---- U11, U20 and U21: exact fractions ----------------------------------------------------------------------------------

    [Fact]
    public void U11_U20_and_U21_fractions_are_exact_and_truncated()
    {
        var digital = Format("en", DurationFormatStyle.Digital);
        Assert.Equal("0:00:4503599627370497.000000001", digital.Format(D(seconds: 4503599627370497, nanoseconds: 1)));
        Assert.Equal("0:00:01.000000005", digital.Format(D(seconds: 1, nanoseconds: 5)));
        Assert.Equal("0:00:00.000000001", digital.Format(D(seconds: 0, nanoseconds: 1)));
        Assert.Equal("0:00:07", Format("en", DurationFormatStyle.Digital, fractionalDigits: 0).Format(D(seconds: 7, milliseconds: 999, microseconds: 999, nanoseconds: 999)));
        Assert.Equal("0:00:07.999", Format("en", DurationFormatStyle.Digital, fractionalDigits: 3).Format(D(seconds: 7, milliseconds: 999, microseconds: 999, nanoseconds: 999)));
        Assert.Equal("0:00:07.000000000", Format("en", DurationFormatStyle.Digital, fractionalDigits: 9).Format(D(seconds: 7)));
        Assert.Equal("1.5 sec", Format("en", given: ("milliseconds", "numeric")).Format(D(milliseconds: 1500)));
        Assert.Equal("1 sec, 2.003004 ms", Format("en", given: ("microseconds", "numeric")).Format(D(seconds: 1, milliseconds: 2, microseconds: 3, nanoseconds: 4)));
        Assert.Equal("0.000000001 seconds", Format("en", DurationFormatStyle.Long, given: ("milliseconds", "numeric")).Format(D(nanoseconds: 1)));
    }

    // ---- U12: the parts ------------------------------------------------------------------------------------------------------

    [Fact]
    public void U12_number_parts_carry_the_singular_unit_and_the_list_s_literals_none()
    {
        var parts = Format("en", DurationFormatStyle.Long).FormatToParts(D(hours: 1, minutes: 2));
        Assert.Equal("integer:1:hour|literal: :hour|unit:hour:hour|literal:, :-|integer:2:minute|literal: :minute|unit:minutes:minute",
            string.Join("|", parts.Select(static p => p.Type + ":" + p.Value + ":" + (p.Unit ?? "-"))));
        Assert.Empty(Format("en").FormatToParts(D(seconds: 0)));

        var clock = Format("en", DurationFormatStyle.Digital).FormatToParts(D(hours: 1, minutes: 2, seconds: 3, milliseconds: 500));
        Assert.Equal("integer:1:hour|literal:::-|integer:02:minute|literal:::-|integer:03:second|decimal:.:second|fraction:5:second",
            string.Join("|", clock.Select(static p => p.Type + ":" + p.Value + ":" + (p.Unit ?? "-"))));
        Assert.Equal(39, Format("en").FormatToParts(D(1, 2, 3, 4, 5, 6, 7, 8, 9, 10)).Count);
    }

    // ---- U13, U14, U15 and U16: the data -------------------------------------------------------------------------------------

    [Fact]
    public void U13_and_U14_the_digital_format_is_CLDR_s_at_the_data_locale()
    {
        Assert.Equal(new DurationDigitalFormat(":", ":", false), DurationFormatCore.DigitalFormat(Data, "en"));
        Assert.Equal(new DurationDigitalFormat(":", ":", false), DurationFormatCore.DigitalFormat(Data, "de"));
        Assert.Equal(new DurationDigitalFormat(".", ".", false), DurationFormatCore.DigitalFormat(Data, "da"));
        Assert.Equal(new DurationDigitalFormat(".", ".", false), DurationFormatCore.DigitalFormat(Data, "en-DK"));
        Assert.Equal(new DurationDigitalFormat(":", ":", true), DurationFormatCore.DigitalFormat(Data, "af"));
        Assert.Equal("1.02.03", Format("da", DurationFormatStyle.Digital).Format(D(hours: 1, minutes: 2, seconds: 3)));
        Assert.Equal("01:02:03", Format("lt", DurationFormatStyle.Digital).Format(D(hours: 1, minutes: 2, seconds: 3)));
        Assert.Equal("1:02:03", Format("en", DurationFormatStyle.Digital).Format(D(hours: 1, minutes: 2, seconds: 3)));
    }

    /// <summary>
    /// GetDurationUnitOptions makes the hours <c>2-digit</c> wherever [[TwoDigitHours]] is true, whatever the style resolved
    /// (ECMA-402's step, as Broiler.VM ports it), so that with decision D34's reading of Broiler.JS's data the hours of every
    /// style become <c>2-digit</c> in <c>af</c> and <c>lt</c>, the minutes and seconds after them too, and a duration with hours
    /// is written as a clock in the <c>long</c>, <c>short</c> and <c>narrow</c> styles as well (where Node writes
    /// <c>"1 val. 2 min."</c>). The design note's dated 9.3e note records it; Broiler.VM's tables never reach the step.
    /// </summary>
    [Fact]
    public void U14_two_digit_hours_make_every_style_s_hours_a_clock_in_af_and_lt()
    {
        foreach (var locale in new[] { "af", "lt" })
        {
            foreach (var style in new[] { DurationFormatStyle.Long, DurationFormatStyle.Short, DurationFormatStyle.Narrow })
            {
                var format = Format(locale, style);
                Assert.Equal("hours=2-digit/auto minutes=2-digit/always seconds=2-digit/always",
                    Resolved(format, DurationUnit.Hours, DurationUnit.Minutes, DurationUnit.Seconds));
                Assert.Equal("01:02:00", format.Format(D(hours: 1, minutes: 2)));
            }
        }

        Assert.Equal("1 hr, 2 min", Format("en").Format(D(hours: 1, minutes: 2)));
    }

    [Fact]
    public void U15_German_units_and_lists()
    {
        Assert.Equal("1 Stunde, 2 Minuten und 3 Sekunden", Format("de", DurationFormatStyle.Long).Format(D(hours: 1, minutes: 2, seconds: 3)));
        Assert.Equal("1 ns", Format("de").Format(D(nanoseconds: 1)));
        Assert.Equal("1 ms, 1 \u03BCs", Format("de").Format(D(milliseconds: 1, microseconds: 1)));
        Assert.Equal("1h, 1ms", Format("de", DurationFormatStyle.Narrow).Format(D(hours: 1, milliseconds: 1)));
        Assert.Equal("1:02:03,456", Format("de", DurationFormatStyle.Digital).Format(D(hours: 1, minutes: 2, seconds: 3, milliseconds: 456)));
    }

    [Fact]
    public void U16_a_numbering_system_s_digits_and_its_own_symbols()
    {
        var arab = Format("en", numberingSystem: "arab");
        Assert.Equal("\u0661\u066C\u0660\u0660\u0660 days", arab.Format(D(days: 1000)));
        Assert.StartsWith("\u061C-\u0661 hr", arab.Format(D(hours: -1, minutes: -2, seconds: -3)), StringComparison.Ordinal);
    }

    // ---- U19: the charge ---------------------------------------------------------------------------------------------------

    [Fact]
    public void U19_the_charge_is_one_per_unit_visited_and_the_formats_it_constructs()
    {
        ulong charged = 0;
        var format = Format("en");
        format.Format(D(1, 2, 3, 4, 5, 6, 7, 8, 9, 10), units => charged += units);

        // Ten units visited; ten number formats, each one unit for the locale its constructor canonicalises and one rounding of a
        // value of one or two digits; one list format, one unit for its locale and nine template steps over ten elements
        // (Broiler.VM's DF :597, JsRealm.Intl.cs:282, JsNumberFormatter.cs:821, JsRealm.ListFormat.cs:217).
        Assert.Equal(10UL + (10UL * 2) + 1 + 9, charged);

        // formatToParts charges the same in the library; the one unit per part is the engine's (DF :124).
        ulong parts = 0;
        format.FormatToParts(D(1, 2, 3, 4, 5, 6, 7, 8, 9, 10), units => parts += units);
        Assert.Equal(charged, parts);

        // A fraction's String is read as a decimal and charged length / 8 + 1: "1.002003000", eleven code units, is two units.
        ulong fraction = 0;
        Format("en", given: ("milliseconds", "numeric")).Format(D(seconds: 1, milliseconds: 2, microseconds: 3), units => fraction += units);
        Assert.Equal(7UL + 2 + 1 + 1 + 1, fraction);
    }

    [Fact]
    public void Format_is_the_parts_values_joined()
    {
        foreach (var style in Enum.GetValues<DurationFormatStyle>())
        {
            var format = Format("de", style);
            var duration = D(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            Assert.Equal(string.Concat(format.FormatToParts(duration).Select(static p => p.Value)), format.Format(duration));
        }
    }

    // ---- preconditions -------------------------------------------------------------------------------------------------------

    [Fact]
    public void A_record_that_is_not_a_valid_duration_is_a_broken_precondition()
    {
        var format = Format("en");
        Assert.Throws<ArgumentException>(() => format.Format(D(hours: 1.5)));
        Assert.Throws<ArgumentException>(() => format.Format(D(hours: 1, minutes: -1)));
        Assert.Throws<ArgumentException>(() => format.Format(D(years: 4294967296)));
        Assert.Throws<ArgumentException>(() => format.Format(D(hours: double.NaN)));
        Assert.Equal("4,294,967,295 yrs", format.Format(D(years: 4294967295)));
        Assert.Equal("9,007,199,254,740,991 sec", format.Format(D(seconds: 9007199254740991)));
        Assert.Throws<ArgumentException>(() => DurationFormatCore.Create(Data, "en", "en", "latn", DurationFormatStyle.Short, [], null));
        Assert.Throws<ArgumentException>(() => DurationFormatCore.Create(Data, "en", "en", "latn", DurationFormatStyle.Short, format.Units, 10));
    }
}
