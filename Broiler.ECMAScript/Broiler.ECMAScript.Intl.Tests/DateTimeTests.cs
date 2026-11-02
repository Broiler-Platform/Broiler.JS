using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Broiler.ECMAScript.Temporal;
using Broiler.ECMAScript.Time;
using Xunit;

namespace Broiler.ECMAScript.Intl.Tests;

/// <summary>
/// Sub-area (d) of the Intl library (stage SJL-9.3d): the date-time format, its pattern generator, interval formatter, day
/// periods, hour cycles and zone names, and Intl over Temporal, each held to the answers Broiler.VM gives at its SJL-9 rule commit
/// as the correspondence of bundle <c>sjl-9-001</c> records them, row by row (section T), over the data home's Broiler.JS tables in
/// the composite's order; and row Z8's correction, the zone's identifier as given (decision D15).
/// </summary>
public sealed class DateTimeTests
{
    private static readonly CompositeLocaleData Data = new(new TableLocaleData(HomeDataSet.Current));

    private static readonly DateTimeData Dates = new(Data, HomeTimeZones.Current, HomeCalendars.Current);

    /// <summary>2024-01-05T15:07:09.123Z, the correspondence's probes' instant.</summary>
    private const double Instant = 1704467229123;

    // ---- the engine's side, as an adapter does it ------------------------------------------------------------------------

    private static ResolvedLocale Resolve(string locale, string? calendar = null, string? hourCycle = null, bool? hour12 = null, string? numberingSystem = null)
    {
        Assert.Equal(IntlStatus.Completed, LocaleNegotiation.CanonicalizeLocaleList([locale], Data, out var requested, out _));
        var match = LocaleNegotiation.MatchLocale(["de", "de-DE", "en", "en-US", "fr", "ja"], requested, "en-US");
        return LocaleNegotiation.ResolveLocale(match,
        [
            new RelevantKey("ca", DateTimeFormat.CalendarKeyValues) { Option = calendar },
            new RelevantKey("hc", DateTimeFormat.HourCycleKeyValues) { Option = hour12 is null ? hourCycle : null, OptionIsNull = hour12 is not null },
            new RelevantKey("nu", DateTimeFormat.NumberingSystemKeyValues(Data, match.Locale)) { Option = numberingSystem },
        ], Data);
    }

    private static ResolvedTimeZone Zone(string name)
    {
        Assert.Equal(IntlStatus.Completed, ResolvedTimeZone.Resolve(name, HomeTimeZones.Current, out var zone));
        return zone!;
    }

    private static DateTimeFormat Format(string locale, DateTimeComponents? components = null, string? timeZone = "UTC", string? dateStyle = null,
        string? timeStyle = null, string? calendar = null, string? hourCycle = null, bool? hour12 = null, string? numberingSystem = null,
        DateTimeRequired required = DateTimeRequired.Any, DateTimeDefaults defaults = DateTimeDefaults.Date)
    {
        var status = DateTimeFormat.Create(Dates, Resolve(locale, calendar, hourCycle, hour12, numberingSystem), new DateTimeFormatOptions
        {
            Hour12 = hour12,
            TimeZone = timeZone is null ? ResolvedTimeZone.Utc : Zone(timeZone),
            Components = components ?? DateTimeComponents.None,
            DateStyle = dateStyle,
            TimeStyle = timeStyle,
            Required = required,
            Defaults = defaults,
        }, out var format);
        Assert.Equal(IntlStatus.Completed, status);
        return format!;
    }

    private static string Write(DateTimeFormat format, double time) => format.Formatter.Format(time);

    private static double Utc(int year, int month, int day, int hour = 0, int minute = 0) =>
        EcmaTime.MakeDate(EcmaTime.MakeDay(year, month - 1, day), EcmaTime.MakeTime(hour, minute, 0, 0));

    // ---- T7 to T13: resolution, hour cycles and patterns -----------------------------------------------------------------

    [Fact]
    public void T7_resolvedOptions_reports_the_selected_pattern_s_widths()
    {
        var format = Format("en", new DateTimeComponents { Hour = "numeric", Minute = "numeric" });
        Assert.Contains(new KeyValuePair<string, string>("minute", "2-digit"), format.ResolvedComponents);
        Assert.Equal("h12", format.HourCycle);
        Assert.True(format.Hour12);
    }

    [Fact]
    public void T8_a_format_without_an_hour_reports_no_hour_cycle()
    {
        Assert.Null(Format("en", new DateTimeComponents { Year = "numeric" }).HourCycle);
        Assert.Null(Format("en", dateStyle: "long").HourCycle);
        Assert.Null(Format("en", dateStyle: "long").Hour12);
        Assert.Empty(Format("en", dateStyle: "long").ResolvedComponents);
    }

    [Fact]
    public void T9_hour12_wins_over_hourCycle_and_h24_writes_24()
    {
        Assert.Equal("h12", Format("en", new DateTimeComponents { Hour = "numeric" }, hourCycle: "h23", hour12: true).HourCycle);
        Assert.Equal("24:05", Write(Format("en", new DateTimeComponents { Hour = "2-digit", Minute = "2-digit" }, hourCycle: "h24"), Utc(2024, 1, 5, 0, 5)));
    }

    [Fact]
    public void T9_h11_writes_0_to_11_and_h23_writes_00_to_23()
    {
        var components = new DateTimeComponents { Hour = "numeric", Minute = "2-digit" };
        var h11 = Format("en", components, hourCycle: "h11");
        Assert.Equal("h11", h11.HourCycle);
        Assert.Contains('K', h11.Formatter.Pattern);
        Assert.Equal("0:05\u202FAM", Write(h11, Utc(2024, 1, 5, 0, 5)));
        Assert.Equal("0:05\u202FPM", Write(h11, Utc(2024, 1, 5, 12, 5)));
        Assert.Equal("11:05\u202FPM", Write(h11, Utc(2024, 1, 5, 23, 5)));

        var h12 = Format("en", components, hourCycle: "h12");
        Assert.Equal("12:05\u202FAM", Write(h12, Utc(2024, 1, 5, 0, 5)));
        Assert.Equal("12:05\u202FPM", Write(h12, Utc(2024, 1, 5, 12, 5)));

        var h23 = Format("en", new DateTimeComponents { Hour = "2-digit", Minute = "2-digit" }, hourCycle: "h23");
        Assert.Equal("h23", h23.HourCycle);
        Assert.Equal("00:05", Write(h23, Utc(2024, 1, 5, 0, 5)));
        Assert.Equal("23:05", Write(h23, Utc(2024, 1, 5, 23, 5)));
    }

    [Fact]
    public void T10_each_locale_s_default_twelve_and_twenty_four_hour_cycles()
    {
        // CLDR's preferred hour of the data locale's region gives the default; hour12 takes the default where it is of its kind,
        // and otherwise h12 or h23, as Broiler.VM's CreateDateTimeFormat does.
        string? Cycle(string locale, bool? hour12 = null) => Format(locale, new DateTimeComponents { Hour = "numeric" }, hour12: hour12).HourCycle;

        Assert.Equal("h12", Cycle("en"));
        Assert.Equal("h23", Cycle("de"));
        Assert.Equal("h23", Cycle("fr"));
        Assert.Equal("h23", Cycle("ja"));
        Assert.Equal("h12", Cycle("de", hour12: true));
        Assert.Equal("h12", Cycle("en", hour12: true));
        Assert.Equal("h23", Cycle("en", hour12: false));
        Assert.Equal("h23", Cycle("ja", hour12: false));
        Assert.Equal("h23", Cycle("de-u-hc-h11", hour12: false));
    }

    [Fact]
    public void T12_CLDR_s_patterns_write_U_202F_and_the_atTime_join()
    {
        Assert.Equal("3:07 PM", Write(Format("en", timeStyle: "short"), Instant));
        Assert.StartsWith("Friday, January 5, 2024 at 3:07:09 PM", Write(Format("en", dateStyle: "full", timeStyle: "full"), Instant), StringComparison.Ordinal);
    }

    [Fact]
    public void T13_the_pattern_generator_takes_CLDR_s_skeletons()
    {
        Assert.Equal("5 Friday", Write(Format("en", new DateTimeComponents { Weekday = "long", Day = "numeric" }), Instant));
        Assert.Equal("7", Write(Format("en", new DateTimeComponents { Minute = "2-digit" }), Instant));
        Assert.Equal("1/5/2024, UTC", Write(Format("en", new DateTimeComponents { TimeZoneName = "short" }), Instant));
    }

    [Fact]
    public void T14_names_are_the_data_locale_s()
    {
        Assert.Equal("Januar", Write(Format("de", new DateTimeComponents { Month = "long" }), Instant));
        Assert.Equal("Freitag", Write(Format("de", new DateTimeComponents { Weekday = "long" }), Instant));
        Assert.Equal("5.1.2024", Write(Format("de"), Instant));
    }

    [Fact]
    public void T15_and_T16_layouts_and_locales_are_CLDR_s()
    {
        Assert.Equal("5. Januar 2024", Write(Format("de", dateStyle: "long"), Instant));
        Assert.Equal("05.01.24", Write(Format("de", dateStyle: "short"), Instant));
        Assert.Equal("Fr., 5. Jan.", Write(Format("de", new DateTimeComponents { Weekday = "short", Day = "numeric", Month = "short" }), Instant));
        Assert.Equal("5. Januar 2024 um 15:07", Write(Format("de", dateStyle: "long", timeStyle: "short"), Instant));
        Assert.Equal("15:07:09,123", Write(Format("de", new DateTimeComponents { Hour = "numeric", Minute = "numeric", Second = "numeric", FractionalSecondDigits = 3 }), Instant));
        Assert.Equal("05/01/2024", Write(Format("fr"), Instant));
        Assert.Equal("2024/1/5", Write(Format("ja"), Instant));
    }

    // ---- T17 to T20: day periods ----------------------------------------------------------------------------------------

    [Fact]
    public void T17_to_T20_day_periods_follow_CLDR_s_rules()
    {
        var periods = Format("en", new DateTimeComponents { DayPeriod = "long" });
        Assert.Equal("in the morning", Write(periods, Utc(2024, 1, 5, 0, 0)));
        Assert.Equal("noon", Write(periods, Utc(2024, 1, 5, 12, 1)));
        Assert.Equal("12:01 in the afternoon", Write(Format("en", new DateTimeComponents { DayPeriod = "long", Hour = "numeric", Minute = "numeric" }), Utc(2024, 1, 5, 12, 1)));
        Assert.Equal("15 Uhr", Write(Format("de", new DateTimeComponents { DayPeriod = "long", Hour = "numeric" }), Utc(2024, 1, 5, 15)));
    }

    // ---- Z8 and T21 to T27: zones ----------------------------------------------------------------------------------------

    [Fact]
    public void Z8_the_resolved_zone_is_the_identifier_as_given_and_its_names_are_the_primary_s()
    {
        var calcutta = Zone("asia/calcutta");
        Assert.Equal("Asia/Calcutta", calcutta.Identifier);
        Assert.Equal("Asia/Kolkata", calcutta.PrimaryIdentifier);
        Assert.Equal(TimeZoneKind.Named, calcutta.Kind);

        var gmt = Zone("Etc/GMT");
        Assert.Equal("Etc/GMT", gmt.Identifier);
        Assert.Equal("UTC", gmt.PrimaryIdentifier);
        Assert.Equal(TimeZoneKind.Utc, gmt.Kind);

        // ROW T26: Etc/GMT keeps UTC's names, its primary's (decision D15).
        var format = Format("en", new DateTimeComponents { TimeZoneName = "long" }, timeZone: "Etc/GMT");
        Assert.Equal("Etc/GMT", format.TimeZone);
        Assert.EndsWith("Coordinated Universal Time", Write(format, Instant), StringComparison.Ordinal);
        Assert.Equal(Write(Format("en", new DateTimeComponents { TimeZoneName = "long" }, timeZone: "Asia/Kolkata"), Instant),
            Write(Format("en", new DateTimeComponents { TimeZoneName = "long" }, timeZone: "Asia/Calcutta"), Instant));
    }

    [Fact]
    public void T21_offsets_and_refusals()
    {
        Assert.Equal("+05:30", Zone("+0530").Identifier);
        Assert.Equal(TimeZoneKind.Gmt, Zone("-00:00").Kind);
        Assert.Equal(TimeZoneKind.Offset, Zone("Etc/GMT-14").Kind);
        Assert.Equal(840, Zone("Etc/GMT-14").OffsetMinutes);
        Assert.Equal(IntlStatus.TimeZoneOffsetHasSeconds, ResolvedTimeZone.Resolve("+05:30:00", HomeTimeZones.Current, out var refused));
        Assert.Null(refused);
        Assert.Equal(IntlStatus.InvalidTimeZone, ResolvedTimeZone.Resolve("Mars/Phobos", HomeTimeZones.Current, out _));
        Assert.Equal(IntlStatus.InvalidTimeZone, ResolvedTimeZone.Resolve("Asia/Kolkatı", HomeTimeZones.Current, out _));
        ulong charged = 0;
        ResolvedTimeZone.Resolve("Europe/Berlin", HomeTimeZones.Current, out _, units => charged += units);
        Assert.Equal(13UL, charged);
    }

    [Fact]
    public void T22_to_T25_zone_names_with_metazone_history()
    {
        string Name(string locale, string zone, string style, double time) =>
            Format(locale, new DateTimeComponents { TimeZoneName = style }, timeZone: zone).Formatter.FormatToParts(time).Single(static p => p.Type == "timeZoneName").Value;

        Assert.Equal("Nordamerikanische Ostküsten-Normalzeit", Name("de", "America/New_York", "long", Instant));
        Assert.Equal("British Summer Time", Name("en", "Europe/London", "long", Utc(2024, 7, 15, 12)));
        Assert.Equal("Eastern Standard Time", Name("en", "America/Indiana/Knox", "long", Utc(1995, 1, 5)));
        Assert.Equal("Central Standard Time", Name("en", "America/Indiana/Knox", "long", Utc(2010, 1, 5)));
        Assert.Equal("GMT-0:44:30", Name("en", "Africa/Monrovia", "shortOffset", Utc(1970, 1, 5)));
        Assert.Equal("Germany Time", Name("en", "Europe/Berlin", "shortGeneric", Instant));
        Assert.Equal("Central European Time", Name("en", "Europe/Berlin", "longGeneric", Instant));
    }

    [Fact]
    public void The_metazone_questions_are_the_provider_s()
    {
        var tables = new TableLocaleData(HomeDataSet.Current);
        Assert.NotNull(tables.MetazonePeriods("America/Indiana/Knox"));
        Assert.Contains(tables.MetazonePeriods("America/Indiana/Knox")!, static p => p.Metazone == "America_Eastern");
        Assert.Null(tables.MetazonePeriods("Mars/Phobos"));
        Assert.Equal("America/New_York", tables.GoldenZone("America_Eastern", "001"));
        Assert.Null(tables.GoldenZone("America_Eastern", "ZZ"));
        Assert.Equal("Europe/Berlin", tables.PrimaryZone("DE"));
        Assert.Equal("Europe/Berlin", Data.PrimaryZone("DE"));
    }

    // ---- T29 to T31: ranges ----------------------------------------------------------------------------------------------

    [Fact]
    public void T29_a_range_compares_fields_the_format_does_not_display()
    {
        var weekday = Format("en", new DateTimeComponents { Weekday = "short" });
        Assert.Equal("Fri – Sun", weekday.Formatter.FormatRange(Instant, Utc(2024, 1, 7, 10)));
        var parts = weekday.Formatter.FormatRangeToParts(Utc(2024, 1, 2), Utc(2024, 1, 3));
        Assert.Equal([("weekday", "Tue", "startRange"), ("literal", " – ", "shared"), ("weekday", "Wed", "endRange")],
            parts.Select(static p => (p.Type, p.Value, p.Source)));
        Assert.Equal("Jan 5 – 7, 2024",
            Format("en", new DateTimeComponents { Year = "numeric", Month = "short", Day = "numeric" }).Formatter.FormatRange(Utc(2024, 1, 5), Utc(2024, 1, 7)));
        var same = Format("en", dateStyle: "long").Formatter.FormatRangeToParts(Instant, Instant);
        Assert.All(same, static p => Assert.Equal("shared", p.Source));
    }

    [Fact]
    public void T28_parts_eras_and_TimeClip()
    {
        var full = Format("en", dateStyle: "full", timeStyle: "long");
        Assert.Equal(["weekday", "literal", "month", "literal", "day", "literal", "year", "literal", "hour", "literal", "minute", "literal", "second", "literal", "dayPeriod", "literal", "timeZoneName"],
            full.Formatter.FormatToParts(Instant).Select(static p => p.Type));
        Assert.All(full.Formatter.FormatToParts(Instant), static p => Assert.Null(p.Source));

        var era = Format("en", new DateTimeComponents { Era = "short", Year = "numeric" });
        Assert.Equal("2024 AD", Write(era, Instant));
        Assert.Equal("1 BC", Write(era, Utc(0, 6, 1)));
        Assert.Equal("2 BC", Write(era, Utc(-1, 6, 1)));
        Assert.Equal([("year", "1"), ("literal", " "), ("era", "BC")], era.Formatter.FormatToParts(Utc(0, 6, 1)).Select(static p => (p.Type, p.Value)));
        Assert.Equal("24", Write(Format("en", new DateTimeComponents { Year = "2-digit" }), Instant));

        var format = Format("en");
        Assert.Equal(IntlStatus.Completed, format.HandleDateTimeValue(DateTimeValue.Number(8.64e15), out var formatter, out var time));
        Assert.Equal("9/13/275760", formatter!.Format(time));
        Assert.Equal(IntlStatus.Completed, format.HandleDateTimeValue(DateTimeValue.Number(-0.5), out _, out time));
        Assert.Equal(0, time);
        Assert.Equal("1/1/1970", format.Formatter.Format(time));
    }

    [Fact]
    public void T30_and_T31_interval_patterns_and_the_range_separator()
    {
        Assert.Equal("January 5\u2009\u2013\u2009February 7",
            Format("en", new DateTimeComponents { Month = "long", Day = "numeric" }).Formatter.FormatRange(Utc(2024, 1, 5), Utc(2024, 2, 7)));
        Assert.Equal("05.\u201307.01.2024",
            Format("de", new DateTimeComponents { Year = "numeric", Month = "2-digit", Day = "2-digit" }).Formatter.FormatRange(Utc(2024, 1, 5), Utc(2024, 1, 7)));
        Assert.Equal("4 BC\u2009\u2013\u20092024 AD",
            Format("en", new DateTimeComponents { Year = "numeric" }).Formatter.FormatRange(Utc(-3, 6, 1), Instant));

        // THE FALLBACK: CLDR's intervalFormatFallback joins two full dates where no interval pattern applies.
        var separator = Format("en", dateStyle: "short", timeStyle: "short").Formatter.FormatRangeToParts(Instant, Instant + 400 * 86_400_000.0)
            .Single(static p => p.Source == "shared");
        Assert.Equal(("literal", "\u2009\u2013\u2009"), (separator.Type, separator.Value));
    }

    [Fact]
    public void T32_formatRange_s_checks_and_order()
    {
        var format = Format("en");
        Assert.Equal(IntlStatus.TimeValueOutOfRange, format.HandleDateTimeRange(DateTimeValue.Number(double.NaN), DateTimeValue.Number(0), out var formatter, out _, out _));
        Assert.Null(formatter);
        Assert.Equal(IntlStatus.TimeValueOutOfRange, format.HandleDateTimeRange(DateTimeValue.Number(0), DateTimeValue.Number(8.64e15 + 1), out _, out _, out _));
        Assert.Equal(IntlStatus.RangeOfDifferentTypes, format.HandleDateTimeRange(
            DateTimeValue.PlainDate(new IsoDate(2024, 1, 5), "iso8601"),
            DateTimeValue.PlainDateTime(new IsoDateTime(new IsoDate(2024, 1, 7), new IsoTime(0, 0, 0, 0, 0, 0)), "iso8601"), out _, out _, out _));
        Assert.Equal(IntlStatus.RangeOfDifferentTypes,
            format.HandleDateTimeRange(DateTimeValue.Number(0), DateTimeValue.PlainDate(new IsoDate(2024, 1, 5), "iso8601"), out _, out _, out _));

        // X AFTER Y: the range is written from x to y, as given.
        Assert.Equal(IntlStatus.Completed, format.HandleDateTimeRange(DateTimeValue.Number(Utc(2024, 1, 7)), DateTimeValue.Number(Utc(2024, 1, 5)), out formatter, out var x, out var y));
        Assert.Equal("1/7/2024\u2009\u2013\u20091/5/2024", formatter!.FormatRange(x, y));
    }

    // ---- T33 to T37: calendars and numbering systems ---------------------------------------------------------------------

    [Fact]
    public void T33_the_deprecated_Hijri_calendars_fall_back_to_islamic_tbla()
    {
        var format = Format("en-u-ca-islamic");
        Assert.Equal("islamic-tbla", format.Calendar);
        Assert.Equal("en-u-ca-islamic", format.Locale);
        Assert.Equal("coptic", Format("en", calendar: "coptic").Calendar);
    }

    [Fact]
    public void T34_and_T35_another_calendar_s_weekday_and_names()
    {
        Assert.Equal("Friday", Write(Format("en", new DateTimeComponents { Weekday = "long" }, calendar: "coptic"), Instant));
        Assert.StartsWith("Kiahk 26, 1740", Write(Format("en", new DateTimeComponents { Year = "numeric", Month = "long", Day = "numeric" }, calendar: "coptic"), Instant), StringComparison.Ordinal);
    }

    [Fact]
    public void T36_the_japanese_calendar_writes_its_era()
    {
        var japanese = Format("en-u-ca-japanese");
        Assert.Equal("japanese", japanese.Calendar);
        Assert.Equal("1/5/6 R", Write(japanese, Instant));
        Assert.Equal("January 5, 6 Reiwa", Write(Format("en-u-ca-japanese", dateStyle: "long"), Instant));
        Assert.Equal([("year", "6"), ("literal", " "), ("era", "Reiwa")],
            Format("en", new DateTimeComponents { Era = "long", Year = "numeric" }, calendar: "japanese").Formatter.FormatToParts(Instant).Select(static p => (p.Type, p.Value)));
        Assert.Equal("1/5/31 H", Write(japanese, Utc(2019, 1, 5)));
        Assert.Equal("2024-01-05", Write(Format("en", dateStyle: "short", calendar: "iso8601"), Instant));
    }

    [Fact]
    public void T37_a_numbering_system_s_digits()
    {
        Assert.Equal("١/٥/٢٠٢٤", Write(Format("en", numberingSystem: "arab"), Instant));
        Assert.Equal("arab", Format("en", numberingSystem: "arab").NumberingSystem);
    }

    // ---- T38 and T39: Intl over Temporal ---------------------------------------------------------------------------------

    [Fact]
    public void T38_a_plain_value_is_written_at_UTC_so_no_zone_moves_it()
    {
        var format = Format("en", new DateTimeComponents { Hour = "numeric", Minute = "numeric" }, timeZone: "America/New_York");
        var gap = DateTimeValue.PlainDateTime(new IsoDateTime(new IsoDate(2024, 3, 10), new IsoTime(2, 30, 0, 0, 0, 0)), "iso8601");
        Assert.Equal(IntlStatus.Completed, format.HandleDateTimeValue(gap, out var formatter, out var time));
        Assert.Equal("2:30 AM", formatter!.Format(time));

        var instant = DateTimeValue.Instant(new BigInteger(Instant) * 1_000_000);
        Assert.Equal(IntlStatus.Completed, format.HandleDateTimeValue(instant, out formatter, out time));
        Assert.Equal("10:07 AM", formatter!.Format(time));
    }

    [Fact]
    public void T38_a_Temporal_value_beyond_TimeClip_is_written_unclipped()
    {
        // TEST262'S temporal-objects-no-time-clip: HandleDateTimeValue does not clip a Temporal value, so the limits' noons and
        // date-times lie beyond TimeClip's 8.64e15 and are written as Broiler.VM writes them.
        foreach (var calendar in new[] { "iso8601", "gregory" })
        {
            var format = Format("en", calendar: "gregory");
            string Plain(DateTimeValue value, out double time)
            {
                Assert.Equal(IntlStatus.Completed, format.HandleDateTimeValue(value, out var formatter, out time));
                return formatter!.Format(time);
            }

            Assert.Equal("4/19/271822", Plain(DateTimeValue.PlainDate(new IsoDate(-271821, 4, 19), calendar), out var time));
            Assert.Equal(-8.64e15 - 43_200_000, time);
            Assert.Equal("9/13/275760", Plain(DateTimeValue.PlainDate(new IsoDate(275760, 9, 13), calendar), out time));
            Assert.Equal(8.64e15 + 43_200_000, time);
            Assert.StartsWith("4/19/271822, 12:00:00", Plain(DateTimeValue.PlainDateTime(new IsoDateTime(new IsoDate(-271821, 4, 19), new IsoTime(0, 0, 0, 0, 0, 1)), calendar), out time), StringComparison.Ordinal);
            Assert.Equal(-8.64e15 - 86_400_000, time);
            Assert.StartsWith("9/13/275760, 11:59:59", Plain(DateTimeValue.PlainDateTime(new IsoDateTime(new IsoDate(275760, 9, 13), new IsoTime(23, 59, 59, 999, 999, 999)), calendar), out time), StringComparison.Ordinal);
            Assert.Equal(8.64e15 + 86_399_999, time);
            if (calendar == "gregory")
            {
                Assert.Equal("4/271822", Plain(DateTimeValue.PlainYearMonth(new IsoDate(-271821, 4, 1), calendar), out time));
                Assert.Equal(DateTimeFormatter.MinTime + 43_200_000, time);
                Assert.Equal("9/275760", Plain(DateTimeValue.PlainYearMonth(new IsoDate(275760, 9, 1), calendar), out _));
            }

            Assert.Equal(IntlStatus.Completed, format.HandleDateTimeRange(DateTimeValue.PlainDate(new IsoDate(-271821, 4, 19), calendar),
                DateTimeValue.PlainDate(new IsoDate(275760, 9, 13), calendar), out var range, out var x, out var y));
            Assert.Equal("4/19/271822 BC\u2009\u2013\u20099/13/275760 AD", range!.FormatRange(x, y));
        }
    }

    [Fact]
    public void T39_a_plain_year_month_s_calendar_is_checked_before_its_fields()
    {
        var format = Format("en", new DateTimeComponents { Hour = "numeric" });
        var yearMonth = DateTimeValue.PlainYearMonth(new IsoDate(2024, 3, 1), "iso8601");
        Assert.Equal(IntlStatus.TemporalCalendarMismatch, format.HandleDateTimeValue(yearMonth, out var formatter, out _));
        Assert.Null(formatter);

        var gregorian = DateTimeValue.PlainYearMonth(new IsoDate(2024, 3, 1), "gregory");
        Assert.Equal(IntlStatus.TemporalTypeNotFormatted, format.HandleDateTimeValue(gregorian, out _, out _));
        Assert.Equal(IntlStatus.Completed, Format("en").HandleDateTimeValue(gregorian, out formatter, out var time));
        Assert.Equal("3/2024", formatter!.Format(time));
    }

    // ---- the statuses ----------------------------------------------------------------------------------------------------

    [Fact]
    public void CreateDateTimeFormat_s_TypeErrors_are_statuses()
    {
        DateTimeFormatOptions Options(DateTimeComponents? components = null, string? dateStyle = null, string? timeStyle = null, DateTimeRequired required = DateTimeRequired.Any) =>
            new() { Components = components ?? DateTimeComponents.None, DateStyle = dateStyle, TimeStyle = timeStyle, Required = required };

        Assert.Equal(IntlStatus.StyleWithComponents, DateTimeFormat.Create(Dates, Resolve("en"), Options(new DateTimeComponents { Month = "long" }, dateStyle: "short"), out var format));
        Assert.Null(format);
        Assert.Equal(IntlStatus.DateFormatWithTimeStyle, DateTimeFormat.Create(Dates, Resolve("en"), Options(timeStyle: "short", required: DateTimeRequired.Date), out _));
        Assert.Equal(IntlStatus.TimeFormatWithDateStyle, DateTimeFormat.Create(Dates, Resolve("en"), Options(dateStyle: "short", required: DateTimeRequired.Time), out _));
        Assert.Throws<ArgumentException>(() => DateTimeFormat.Create(Dates, Resolve("en"), Options(new DateTimeComponents { Month = "LONG" }), out _));
        Assert.Throws<ArgumentException>(() => DateTimeFormat.Create(Dates, Resolve("en"), Options(new DateTimeComponents { FractionalSecondDigits = 4 }), out _));
    }

    [Fact]
    public void HandleDateTimeValue_s_refusals_are_statuses()
    {
        var format = Format("en");
        Assert.Equal(IntlStatus.TimeValueOutOfRange, format.HandleDateTimeValue(DateTimeValue.Number(8.64e15 + 1), out _, out _));
        Assert.Equal(IntlStatus.TimeValueOutOfRange, format.HandleDateTimeValue(DateTimeValue.Number(double.NaN), out _, out _));
        Assert.Equal(IntlStatus.Completed, format.HandleDateTimeValue(DateTimeValue.Number(1.9), out _, out var clipped));
        Assert.Equal(1, clipped);
        Assert.Equal(IntlStatus.ZonedDateTimeNotFormatted, format.HandleDateTimeValue(DateTimeValue.ZonedDateTime(BigInteger.Zero, "iso8601"), out _, out _));
        Assert.Equal(IntlStatus.RangeOfDifferentTypes,
            format.HandleDateTimeRange(DateTimeValue.PlainDate(new IsoDate(2024, 1, 5), "iso8601"), DateTimeValue.Number(0), out _, out _, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => format.Formatter.Format(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => format.Formatter.Format(0.5));

        // THE FORMATTER'S PRECONDITION is the ISO dates of Temporal's values, not TimeClip's range, which HandleDateTimeValue
        // keeps for a Number alone (row T38).
        Assert.Equal("4/1/271822", format.Formatter.Format(DateTimeFormatter.MinTime));
        Assert.Equal("9/30/275760", format.Formatter.Format(DateTimeFormatter.MaxTime));
        Assert.Throws<ArgumentOutOfRangeException>(() => format.Formatter.Format(DateTimeFormatter.MinTime - 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => format.Formatter.FormatToParts(DateTimeFormatter.MaxTime + 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => format.Formatter.FormatRange(0, double.PositiveInfinity));
    }

    // ---- T40: the charge -------------------------------------------------------------------------------------------------

    [Fact]
    public void T40_the_formatter_charges_its_output_s_length()
    {
        var format = Format("en", dateStyle: "full", timeStyle: "long");
        ulong charged = 0;
        var text = format.Formatter.Format(Instant, units => charged += units);
        Assert.Equal((ulong)text.Length, charged);

        charged = 0;
        var parts = format.Formatter.FormatToParts(Instant, units => charged += units);
        Assert.Equal((ulong)parts.Sum(static p => p.Value.Length), charged);

        charged = 0;
        var range = format.Formatter.FormatRange(Instant, Instant + 86_400_000, units => charged += units);
        Assert.Equal((ulong)range.Length, charged);
    }

    // ---- the shared state ------------------------------------------------------------------------------------------------

    [Fact]
    public void A_format_and_its_data_may_be_shared_between_threads()
    {
        var format = Format("de", new DateTimeComponents { Year = "numeric", Month = "long", Day = "numeric", TimeZoneName = "long" }, timeZone: "Europe/Berlin");
        var expected = format.Formatter.FormatRange(Instant, Instant + 3 * 86_400_000.0);
        var answers = new string[64];
        Parallel.For(0, answers.Length, i =>
        {
            var plain = DateTimeValue.PlainDate(new IsoDate(2024, 1, 5), "iso8601");
            Assert.Equal(IntlStatus.Completed, format.HandleDateTimeValue(plain, out _, out _));
            answers[i] = format.Formatter.FormatRange(Instant, Instant + 3 * 86_400_000.0);
        });
        Assert.All(answers, a => Assert.Equal(expected, a));
    }
}
