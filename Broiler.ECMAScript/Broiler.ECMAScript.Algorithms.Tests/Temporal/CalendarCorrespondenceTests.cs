using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Broiler.ECMAScript.Temporal;
using Broiler.ECMAScript.Time;
using Xunit;
using static Broiler.ECMAScript.Algorithms.Tests.CalendarTestSupport;

namespace Broiler.ECMAScript.Algorithms.Tests;

/// <summary>
/// The calendar rows C1 to C18 of correspondence <c>sjl-10-001</c> at library level (milestone SJL-10.3, part B):
/// every line of Broiler.VM's recorded answers to the rows' probes that the library can answer is computed here with
/// <see cref="TemporalCalendars"/>, as an engine's adapter would call it, and held to Broiler.VM's answer, which the
/// correspondence records as the specification's on every calendar row. A refusal is compared by its constructor
/// alone; messages stay each engine's.
/// </summary>
/// <remarks>
/// A line the library cannot answer, because only an engine holds what it asks (a Temporal object lent as a
/// calendar, row C13), is named by its pattern and counted, so that a line no pattern matches fails the test.
/// </remarks>
public class CalendarCorrespondenceTests
{
    private const double TwoTo32 = 4294967296;

    private static TemporalCalendars Cal => Calendars;

    // Runs every line through the computation, and holds the answers and the counts.
    private static void Replay(IReadOnlyList<(string Label, string Answer)> lines, Func<string, string?> compute, int expectedComputed)
    {
        var mismatches = new List<string>();
        var computed = 0;
        foreach (var (label, answer) in lines)
        {
            var mine = compute(label);
            if (mine is null)
                continue;

            computed++;
            if (WithoutMessage(mine) != WithoutMessage(answer))
                mismatches.Add(label + " => " + answer + "   library: " + mine);
        }

        Assert.True(mismatches.Count == 0, mismatches.Count + " of " + computed + " lines differ:\n" + string.Join('\n', mismatches.Take(40)));
        Assert.Equal(expectedComputed, computed);
    }

    // A date from fields as PlainDate.from answers it, as the probes' ymd(d).
    private static string FromFields(CalendarId calendar, CalendarFields fields, CalendarOverflow overflow)
    {
        var status = Cal.DateFromFields(calendar, fields, overflow, out var date);
        return status == CalendarStatus.Completed ? Ymd(calendar, date) : Throws(status);
    }

    // The era fields an engine reads: only where the calendar supports eras (CalendarExtraFields).
    private static CalendarFields WithEra(CalendarId calendar, CalendarFields fields, string? era, double? eraYear) =>
        TemporalCalendars.SupportsEra(calendar) ? fields with { Era = era, EraYear = eraYear } : fields;

    private static CalendarOverflow Overflow(string name) => name == "reject" ? CalendarOverflow.Reject : CalendarOverflow.Constrain;

    private static double Number(string text) => double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);

    // ---- C1 to C3: identifiers ---------------------------------------------------------------------------

    [Fact]
    public void C1_to_C3_identifiers_canonicalise_as_Broiler_VM_answers()
    {
        var lines = Correspondence("c01-identifiers.vm.txt", "088c9ebfc0b535371e30f9a2aa080ae0ed1386c3ae2ca60e76b40ad2d90c4d63");
        var constructor = new Regex("^new PlainDate\\(2020, 1, 1, (?<id>\".*\")\\)\\.calendarId$");
        var annotation = new Regex("^PlainDate\\.from\\(\"2020-01-01\\[u-ca=(?<id>.*)\\]\"\\)\\.calendarId$", RegexOptions.Singleline);

        string Canonical(string identifier) =>
            TemporalCalendars.Canonicalize(identifier, out var calendar) is var status && status == CalendarStatus.Completed
                ? TemporalCalendars.Identifier(calendar)
                : Throws(status);

        Replay(lines, label =>
        {
            if (constructor.Match(label) is { Success: true } c)
                return Canonical(JsonString(c.Groups["id"].Value));

            if (annotation.Match(label) is { Success: true } a)
                return Canonical(a.Groups["id"].Value);

            // SJL-9'S LIST, read here for the record: the sixteen calendar types, in code unit order.
            Assert.StartsWith("Intl.supportedValuesOf(\"calendar\")", label, StringComparison.Ordinal);
            return string.Join(' ', Enumerable.Range(0, 16).Select(static i => TemporalCalendars.Identifier((CalendarId)i)).Order(StringComparer.Ordinal));
        }, expectedComputed: 83);
    }

    // ---- C4, C5 and C7: month codes ----------------------------------------------------------------------

    [Fact]
    public void C4_C5_C7_month_codes_resolve_constrain_and_refuse_as_Broiler_VM_answers()
    {
        var lines = Correspondence("c02-month-codes.vm.txt", "7cf61ae7cc257698e1c8a0ede90b04cf4304c884a33933d7f27c6719475778f9");
        var quoted = new Regex("^(?<cal>\\S+) (?<y>\\d+) (?<code>\".*\") (?<ov>constrain|reject)$");
        var leap = new Regex("^(?<cal>\\S+) (?<y>\\d+) (?<code>M\\d\\dL?) (?<ov>constrain|reject)$");
        var yearMonth = new Regex("^PlainYearMonth (?<cal>\\S+) (?<y>\\d+) (?<code>M\\d\\dL?) (?<ov>constrain|reject)$");
        var both = new Regex("^(?<cal>\\S+) (?<y>\\d+) month (?<m>\\d+) monthCode (?<code>\\S+)$");
        var month14 = new Regex("^(?<cal>\\S+) month 14 (?<ov>constrain|reject)$");
        var codes = new Regex("^(?<cal>\\S+) (?<y>\\d+) monthCodes$");

        Replay(lines, label =>
        {
            if (quoted.Match(label) is { Success: true } q)
            {
                var calendar = Id(q.Groups["cal"].Value);

                // THE READ: ParseMonthCode refuses before the calendar sees the code.
                var parsed = MonthCode.Parse(JsonString(q.Groups["code"].Value), out var code);
                return parsed != CalendarStatus.Completed
                    ? Throws(parsed)
                    : FromFields(calendar, new CalendarFields { Year = Number(q.Groups["y"].Value), MonthCode = code, Day = 1 }, Overflow(q.Groups["ov"].Value));
            }

            if (leap.Match(label) is { Success: true } l)
            {
                MonthCode.Parse(l.Groups["code"].Value, out var code);
                return FromFields(Id(l.Groups["cal"].Value), new CalendarFields { Year = Number(l.Groups["y"].Value), MonthCode = code, Day = 1 }, Overflow(l.Groups["ov"].Value));
            }

            if (yearMonth.Match(label) is { Success: true } ym)
            {
                var calendar = Id(ym.Groups["cal"].Value);
                MonthCode.Parse(ym.Groups["code"].Value, out var code);
                var status = Cal.YearMonthFromFields(calendar, new CalendarFields { Year = Number(ym.Groups["y"].Value), MonthCode = code }, Overflow(ym.Groups["ov"].Value), out var reference);
                if (status != CalendarStatus.Completed)
                    return Throws(status);

                var d = Parts(calendar, reference);
                return d.Year.ToString(CultureInfo.InvariantCulture) + "/" + d.MonthCode + " " + YearMonthString(reference, calendar);
            }

            if (both.Match(label) is { Success: true } b)
            {
                MonthCode.Parse(b.Groups["code"].Value, out var code);
                return FromFields(Id(b.Groups["cal"].Value), new CalendarFields { Year = Number(b.Groups["y"].Value), Month = Number(b.Groups["m"].Value), MonthCode = code, Day = 1 }, CalendarOverflow.Constrain);
            }

            if (month14.Match(label) is { Success: true } m)
            {
                var name = m.Groups["cal"].Value;
                return FromFields(Id(name), new CalendarFields { Year = LeapYear[name], Month = 14, Day = 1 }, Overflow(m.Groups["ov"].Value));
            }

            var c = codes.Match(label);
            Assert.True(c.Success, label);
            {
                var calendar = Id(c.Groups["cal"].Value);
                var year = Number(c.Groups["y"].Value);
                Assert.Equal(CalendarStatus.Completed, Cal.DateFromFields(calendar, new CalendarFields { Year = year, Month = 1, Day = 1 }, CalendarOverflow.Constrain, out var first));
                var result = new List<string>();
                for (var month = 1; month <= Parts(calendar, first).MonthsInYear; month++)
                {
                    Assert.Equal(CalendarStatus.Completed, Cal.DateFromFields(calendar, new CalendarFields { Year = year, Month = month, Day = 1 }, CalendarOverflow.Constrain, out var date));
                    result.Add(Parts(calendar, date).MonthCode.ToString());
                }

                return string.Join(' ', result);
            }
        }, expectedComputed: 704);
    }

    // ---- C6: month codes' digits -------------------------------------------------------------------------

    [Fact]
    public void C6_a_month_code_with_digits_other_than_ASCII_is_refused_as_Broiler_VM_refuses_it()
    {
        var lines = Correspondence("c10-monthcode-digits.vm.txt", "812f921f38c32a0ffad8c6172d1a3adc4fd5547c988752edeb6b40352620d5d7");
        var date = new Regex("^(?<cal>[a-z0-9-]+) monthCode (?<code>\".*\")$");
        var monthDay = new Regex("^PlainMonthDay hebrew monthCode (?<code>\".*\")$");
        var yearMonth = new Regex("^PlainYearMonth iso8601 monthCode (?<code>\".*\")$");

        Replay(lines, label =>
        {
            if (date.Match(label) is { Success: true } d)
            {
                var name = d.Groups["cal"].Value;
                var parsed = MonthCode.Parse(JsonString(d.Groups["code"].Value), out var code);
                return parsed != CalendarStatus.Completed
                    ? Throws(parsed)
                    : FromFields(Id(name), new CalendarFields { Year = LeapYear[name], MonthCode = code, Day = 1 }, CalendarOverflow.Constrain);
            }

            if (monthDay.Match(label) is { Success: true } md)
            {
                var parsed = MonthCode.Parse(JsonString(md.Groups["code"].Value), out var code);
                if (parsed != CalendarStatus.Completed)
                    return Throws(parsed);

                var status = Cal.MonthDayFromFields(CalendarId.Hebrew, new CalendarFields { MonthCode = code, Day = 1 }, CalendarOverflow.Constrain, out var reference);
                return status == CalendarStatus.Completed ? MonthDayString(reference, CalendarId.Hebrew) : Throws(status);
            }

            var ym = yearMonth.Match(label);
            Assert.True(ym.Success, label);
            {
                var parsed = MonthCode.Parse(JsonString(ym.Groups["code"].Value), out var code);
                if (parsed != CalendarStatus.Completed)
                    return Throws(parsed);

                var status = Cal.YearMonthFromFields(CalendarId.Iso8601, new CalendarFields { Year = 2020, MonthCode = code }, CalendarOverflow.Constrain, out var reference);
                return status == CalendarStatus.Completed ? YearMonthString(reference, CalendarId.Iso8601) : Throws(status);
            }
        }, expectedComputed: 88);
    }

    // ---- C8 and C9: eras ---------------------------------------------------------------------------------

    [Fact]
    public void C8_C9_eras_resolve_and_older_names_are_refused_as_Broiler_VM_answers()
    {
        var lines = Correspondence("c03-eras.vm.txt", "e13dc8f75a92bd3ced663647ff23de614f24dea9440818ba278a1088d5fa6bb0");
        var everyEra = new Regex("^(?<cal>[a-z0-9-]+) era (?<era>\".*\") eraYear 5$");
        var edge = new Regex("^(?<cal>[a-z0-9-]+) (?<iso>[-+]?\\d{4,6}-\\d\\d-\\d\\d)$");
        var eraYear = new Regex("^(?<cal>[a-z0-9-]+) era (?<era>\\S+) eraYear (?<ey>-?\\d+)$");
        var bag = new Regex("^(?<cal>[a-z0-9-]+) (?<bag>\\{.*\\})$");

        string Resolve(CalendarId calendar, CalendarFields fields)
        {
            var status = Cal.DateFromFields(calendar, fields, CalendarOverflow.Constrain, out var date);
            if (status != CalendarStatus.Completed)
                return Throws(status);

            var d = Parts(calendar, date);
            return (d.Era ?? "undefined") + " " + Js(d.EraYear) + " year " + Js(d.Year) + " iso " + Iso(date);
        }

        var m01 = new CalendarFields { MonthCode = new MonthCode(1, false), Day = 1 };
        Replay(lines, label =>
        {
            if (everyEra.Match(label) is { Success: true } e)
            {
                var calendar = Id(e.Groups["cal"].Value);
                return Resolve(calendar, WithEra(calendar, m01, JsonString(e.Groups["era"].Value), 5));
            }

            if (edge.Match(label) is { Success: true } x)
            {
                var calendar = Id(x.Groups["cal"].Value);
                var d = Parts(calendar, ParseIso(x.Groups["iso"].Value));
                return (d.Era ?? "undefined") + " " + Js(d.EraYear) + " / " + Js(d.Year) + " " + d.MonthCode + " " + Js(d.Day);
            }

            if (eraYear.Match(label) is { Success: true } y)
            {
                var calendar = Id(y.Groups["cal"].Value);
                return Resolve(calendar, WithEra(calendar, m01, y.Groups["era"].Value, Number(y.Groups["ey"].Value)));
            }

            var b = bag.Match(label);
            Assert.True(b.Success, label);
            {
                var calendar = Id(b.Groups["cal"].Value);
                using var json = JsonDocument.Parse(b.Groups["bag"].Value);
                var root = json.RootElement;
                var fields = m01 with { Year = root.TryGetProperty("year", out var year) ? year.GetDouble() : null };
                return Resolve(calendar, WithEra(
                    calendar,
                    fields,
                    root.TryGetProperty("era", out var era) ? era.GetString() : null,
                    root.TryGetProperty("eraYear", out var ey) ? ey.GetDouble() : null));
            }
        }, expectedComputed: 1060);
    }

    // ---- C10: month-day reference dates ------------------------------------------------------------------

    [Fact]
    public void C10_month_day_reference_dates_are_Table_6_and_the_1900_to_2035_rule_as_Broiler_VM_answers()
    {
        var lines = Correspondence("c04-reference-years.vm.txt", "2606ebb9d09042b72b6cb6db406d392692c0e65dd132e7be9bf6674f68358ba8");
        var plain = new Regex("^(?<cal>\\S+) (?<code>M\\d\\dL?) (?<day>\\d+) (?<ov>reject|constrain)$");
        var withYear = new Regex("^(?<cal>\\S+) year (?<y>\\d+) (?<code>M\\d\\dL?) (?<day>\\d+) (?<ov>reject|constrain)$");
        var bag = new Regex("^(?<cal>\\S+) (?<bag>\\{.*\\})$");

        string Reference(CalendarId calendar, CalendarFields fields, CalendarOverflow overflow)
        {
            var status = Cal.MonthDayFromFields(calendar, fields, overflow, out var reference);
            if (status != CalendarStatus.Completed)
                return Throws(status);

            var d = Parts(calendar, reference);
            return d.MonthCode + "/" + Js(d.Day) + " ref " + MonthDayString(reference, calendar, "never");
        }

        Replay(lines, label =>
        {
            if (plain.Match(label) is { Success: true } p)
            {
                MonthCode.Parse(p.Groups["code"].Value, out var code);
                return Reference(Id(p.Groups["cal"].Value), new CalendarFields { MonthCode = code, Day = Number(p.Groups["day"].Value) }, Overflow(p.Groups["ov"].Value));
            }

            if (withYear.Match(label) is { Success: true } y)
            {
                MonthCode.Parse(y.Groups["code"].Value, out var code);
                var fields = new CalendarFields { Year = Number(y.Groups["y"].Value), MonthCode = code, Day = Number(y.Groups["day"].Value) };
                return Reference(Id(y.Groups["cal"].Value), fields, Overflow(y.Groups["ov"].Value));
            }

            var b = bag.Match(label);
            Assert.True(b.Success, label);
            {
                using var json = JsonDocument.Parse(b.Groups["bag"].Value);
                var root = json.RootElement;
                MonthCode? code = null;
                if (root.TryGetProperty("monthCode", out var monthCode))
                {
                    MonthCode.Parse(monthCode.GetString(), out var parsed);
                    code = parsed;
                }

                var fields = new CalendarFields
                {
                    Year = root.TryGetProperty("year", out var year) ? year.GetDouble() : null,
                    Month = root.TryGetProperty("month", out var month) ? month.GetDouble() : null,
                    MonthCode = code,
                    Day = root.GetProperty("day").GetDouble(),
                };
                return Reference(Id(b.Groups["cal"].Value), fields, CalendarOverflow.Constrain);
            }
        }, expectedComputed: 1670);
    }

    // ---- C11 and C13: fields beyond 32 bits, and the calendar a month-day lends --------------------------

    [Fact]
    public void C11_fields_beyond_32_bits_are_held_exactly_as_Broiler_VM_holds_them()
    {
        var lines = Correspondence("c07-slot-and-narrowing.vm.txt", "690313068af54f2264c140729238fa3e8dc98218e8fdaaa24b0a9ca5e9c56a69");
        var slot = new Regex("^(PlainDate\\.from|PlainDate#withCalendar|PlainMonthDay\\.from)\\(.*\\)\\.calendarId$");
        var year = new Regex("^(?<cal>\\S+) year 2\\^32\\+(?<y>\\d+)$");
        var day = new Regex("^(?<cal>\\S+) day 2\\^32\\+1 (?<ov>constrain|reject)$");
        var month = new Regex("^(?<cal>\\S+) month 2\\^32\\+1 constrain$");
        var yearMonth = new Regex("^(?<cal>\\S+) PlainYearMonth year 2\\^32\\+(?<y>\\d+)$");
        var monthDay = new Regex("^(?<cal>\\S+) PlainMonthDay year 2\\^32\\+(?<y>\\d+)$");
        var era = new Regex("^(?<cal>\\S+) era (?<era>\\S+) eraYear 2\\^32\\+5$");
        var m01 = new MonthCode(1, false);
        var slots = 0;

        Replay(lines, label =>
        {
            // ROW C13: TOTEMPORALCALENDARIDENTIFIER READS AN OBJECT'S SLOT, which only an engine holds; the
            // library's part, that a month-day keeps the calendar it is made in, is the test below.
            if (slot.IsMatch(label))
            {
                slots++;
                return null;
            }

            if (year.Match(label) is { Success: true } y)
                return FromFields(Id(y.Groups["cal"].Value), new CalendarFields { Year = TwoTo32 + Number(y.Groups["y"].Value), MonthCode = m01, Day = 1 }, CalendarOverflow.Constrain);

            if (day.Match(label) is { Success: true } d)
            {
                var name = d.Groups["cal"].Value;
                return FromFields(Id(name), new CalendarFields { Year = LeapYear[name], MonthCode = m01, Day = TwoTo32 + 1 }, Overflow(d.Groups["ov"].Value));
            }

            if (month.Match(label) is { Success: true } m)
            {
                var name = m.Groups["cal"].Value;
                return FromFields(Id(name), new CalendarFields { Year = LeapYear[name], Month = TwoTo32 + 1, Day = 1 }, CalendarOverflow.Constrain);
            }

            if (yearMonth.Match(label) is { Success: true } ym)
            {
                var calendar = Id(ym.Groups["cal"].Value);
                var status = Cal.YearMonthFromFields(calendar, new CalendarFields { Year = TwoTo32 + Number(ym.Groups["y"].Value), MonthCode = m01 }, CalendarOverflow.Constrain, out var reference);
                return status == CalendarStatus.Completed ? Js(Parts(calendar, reference).Year) + " " + YearMonthString(reference, calendar) : Throws(status);
            }

            if (monthDay.Match(label) is { Success: true } md)
            {
                var calendar = Id(md.Groups["cal"].Value);
                var status = Cal.MonthDayFromFields(calendar, new CalendarFields { Year = TwoTo32 + Number(md.Groups["y"].Value), MonthCode = m01, Day = 1 }, CalendarOverflow.Constrain, out var reference);
                return status == CalendarStatus.Completed ? MonthDayString(reference, calendar) : Throws(status);
            }

            var e = era.Match(label);
            Assert.True(e.Success, label);
            var eraCalendar = Id(e.Groups["cal"].Value);
            return FromFields(eraCalendar, new CalendarFields { Era = e.Groups["era"].Value, EraYear = TwoTo32 + 5, MonthCode = m01, Day = 1 }, CalendarOverflow.Constrain);
        }, expectedComputed: 56);

        Assert.Equal(21, slots);
    }

    [Fact]
    public void C13_a_month_day_keeps_the_calendar_it_is_made_in_and_lends_it_by_its_identifier()
    {
        // THE PROBE'S MONTH-DAY: PlainMonthDay.from({ calendar: 'hebrew', monthCode: 'M05L', day: 1 }). Its
        // [[Calendar]] is the identifier the engine canonicalised, and the library answers its reference date in it.
        var hebrew = Id("hebrew");
        Assert.Equal(CalendarStatus.Completed, Cal.MonthDayFromFields(hebrew, new CalendarFields { MonthCode = new MonthCode(5, true), Day = 1 }, CalendarOverflow.Constrain, out var reference));
        Assert.Equal(new MonthCode(5, true), Parts(hebrew, reference).MonthCode);

        // LENT AS A CALENDAR, the slot's identifier canonicalises to itself: hebrew, not iso8601, for PlainDate.from,
        // withCalendar and PlainMonthDay.from alike; and so does every other calendar's.
        Assert.Equal("hebrew", TemporalCalendars.Identifier(hebrew));
        for (var id = CalendarId.Iso8601; id <= CalendarId.Roc; id++)
        {
            Assert.Equal(CalendarStatus.Completed, TemporalCalendars.Canonicalize(TemporalCalendars.Identifier(id), out var again));
            Assert.Equal(id, again);
        }

        // A DATE MADE IN THE LENT CALENDAR is a Hebrew date.
        Assert.Equal(CalendarStatus.Completed, Cal.DateFromFields(hebrew, new CalendarFields { Year = 5784, MonthCode = new MonthCode(5, true), Day = 1 }, CalendarOverflow.Constrain, out var date));
        Assert.Equal(new IsoDate(2024, 2, 10), date);
    }

    // ---- C12: an ISO month-day's year past 2^31 ----------------------------------------------------------

    [Fact]
    public void C12_an_ISO_month_days_year_keeps_its_leap_year_past_2_to_the_31_as_Broiler_VM_keeps_it()
    {
        var lines = Correspondence("c08-saturation.vm.txt", "4ea726e208deb5b40865b077c4a8656a39fbd00df8b2a13dc0e8e482f03d512f");
        var from = new Regex("^PlainMonthDay\\.from\\(\\{year: (?<y>-?\\d+), month: 2, day: 29\\}, (?<ov>reject|constrain)\\)$");

        Replay(lines, label =>
        {
            if (from.Match(label) is { Success: true } f)
            {
                var status = Cal.MonthDayFromFields(CalendarId.Iso8601, new CalendarFields { Year = Number(f.Groups["y"].Value), Month = 2, Day = 29 }, Overflow(f.Groups["ov"].Value), out var reference);
                return status == CalendarStatus.Completed ? MonthDayString(reference, CalendarId.Iso8601) : Throws(status);
            }

            // THROUGH with(): PlainMonthDay.from('02-28').with({ year: 2147483652, day: 29 }, { overflow: 'reject' }).
            Assert.StartsWith("PlainMonthDay.from({year: 2147483652, monthCode: \"M02\", day: 29}) through with()", label, StringComparison.Ordinal);
            Assert.Equal(CalendarStatus.Completed, Cal.DateToFields(CalendarId.Iso8601, new IsoDate(1972, 2, 28), CalendarFieldsType.MonthDay, out var fields));
            var merged = TemporalCalendars.MergeFields(CalendarId.Iso8601, fields, new CalendarFields { Year = 2147483652, Day = 29 });
            var withStatus = Cal.MonthDayFromFields(CalendarId.Iso8601, merged, CalendarOverflow.Reject, out var with);
            return withStatus == CalendarStatus.Completed ? MonthDayString(with, CalendarId.Iso8601) : Throws(withStatus);
        }, expectedComputed: 19);
    }

    // ---- C14: the formula calendars at their edges -------------------------------------------------------

    [Fact]
    public void C14_the_formula_calendars_answer_as_Broiler_VM_at_their_edges()
    {
        var lines = Correspondence("c05-formula-edges.vm.txt", "e663dbf8648e57821b2a7d9fc8e3377a7363d02d8badca07126dd98c5e1548ac");
        var fields = new Regex("^(?<cal>[a-z0-9-]+) (?<iso>[-+]?\\d{4,6}-\\d\\d-\\d\\d)$");
        var roundTrip = new Regex("^(?<cal>[a-z0-9-]+) round trip (?<iso>[-+]\\d{6}-\\d\\d-\\d\\d)$");
        var dayPast = new Regex("^(?<cal>[a-z0-9-]+) one day past (?<iso>[-+]\\d{6}-\\d\\d-\\d\\d)$");
        var hebrew = new Regex("^hebrew (?<y>\\d+) months$");

        Replay(lines, label =>
        {
            if (fields.Match(label) is { Success: true } f)
            {
                var d = Parts(Id(f.Groups["cal"].Value), ParseIso(f.Groups["iso"].Value));
                string[] values =
                [
                    d.Era ?? string.Empty, d.EraYear?.ToString(CultureInfo.InvariantCulture) ?? string.Empty, Js(d.Year), Js(d.Month),
                    d.MonthCode.ToString(), Js(d.Day), Js(d.DayOfYear), Js(d.DaysInMonth), Js(d.DaysInYear), Js(d.MonthsInYear),
                    Js(d.InLeapYear), d.WeekOfYear?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                    d.YearOfWeek?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                ];
                return string.Join(' ', values);
            }

            if (roundTrip.Match(label) is { Success: true } r)
            {
                var calendar = Id(r.Groups["cal"].Value);
                var d = Parts(calendar, ParseIso(r.Groups["iso"].Value));
                var status = Cal.DateFromFields(calendar, new CalendarFields { Year = d.Year, MonthCode = d.MonthCode, Day = d.Day }, CalendarOverflow.Constrain, out var date);
                return status == CalendarStatus.Completed ? DateString(date, calendar) : Throws(status);
            }

            if (dayPast.Match(label) is { Success: true } p)
            {
                var calendar = Id(p.Groups["cal"].Value);
                var iso = p.Groups["iso"].Value;
                var d = Parts(calendar, ParseIso(iso));
                var day = d.Day + (iso[0] == '+' ? 1 : -1);

                // A DAY OF 0 IS REFUSED AT THE READ, by ToPositiveIntegerWithTruncation: the engine's RangeError.
                if (day < 1)
                    return "THROWS RangeError";

                var status = Cal.DateFromFields(calendar, new CalendarFields { Year = d.Year, MonthCode = d.MonthCode, Day = day }, CalendarOverflow.Reject, out var date);
                return status == CalendarStatus.Completed ? DateString(date, calendar) : Throws(status);
            }

            var h = hebrew.Match(label);
            Assert.True(h.Success, label);
            {
                var year = Number(h.Groups["y"].Value);
                Assert.Equal(CalendarStatus.Completed, Cal.DateFromFields(CalendarId.Hebrew, new CalendarFields { Year = year, Month = 1, Day = 1 }, CalendarOverflow.Constrain, out var first));
                var parts = Parts(CalendarId.Hebrew, first);
                var lengths = new List<string>();
                for (var month = 1; month <= parts.MonthsInYear; month++)
                {
                    Assert.Equal(CalendarStatus.Completed, Cal.DateFromFields(CalendarId.Hebrew, new CalendarFields { Year = year, Month = month, Day = 1 }, CalendarOverflow.Constrain, out var date));
                    lengths.Add(Js(Parts(CalendarId.Hebrew, date).DaysInMonth));
                }

                return Js(parts.DaysInYear) + " " + Iso(first) + " " + string.Join(',', lengths);
            }
        }, expectedComputed: 558);
    }

    // ---- C15 to C17: the conversions ---------------------------------------------------------------------

    [Fact]
    public void C15_to_C17_the_conversions_keep_the_calendars_day_calendar_and_year_as_Broiler_VM_keeps_them()
    {
        var lines = Correspondence("c09-conversions.vm.txt", "6ffb13baf0f71575fca596160cc1b64bf45c1c2ed27ab0550ede76fb457dce32");
        var line = new Regex("^(?<cal>[a-z0-9-]+) (?<what>.+)$");
        var date = new IsoDate(2023, 3, 15);

        Replay(lines, label =>
        {
            var m = line.Match(label);
            Assert.True(m.Success, label);
            var calendar = Id(m.Groups["cal"].Value);
            CalendarStatus status;

            switch (m.Groups["what"].Value)
            {
                case "PlainDate#toPlainYearMonth":
                case "PlainDateTime#toPlainDate#toPlainYearMonth":
                {
                    status = Cal.DateToYearMonth(calendar, date, out var yearMonth);
                    return status == CalendarStatus.Completed ? YearMonthString(yearMonth, calendar, "always") : Throws(status);
                }

                case "PlainDate#toPlainMonthDay":
                {
                    status = Cal.DateToMonthDay(calendar, date, out var monthDay);
                    if (status != CalendarStatus.Completed)
                        return Throws(status);

                    var d = Parts(calendar, monthDay);
                    return TemporalCalendars.Identifier(calendar) + " " + d.MonthCode + "/" + Js(d.Day) + " " + MonthDayString(monthDay, calendar, "always");
                }

                case "ZonedDateTime#toPlainDate#toPlainMonthDay":
                {
                    status = Cal.DateToMonthDay(calendar, date, out var monthDay);
                    return status == CalendarStatus.Completed ? MonthDayString(monthDay, calendar, "always") : Throws(status);
                }

                case "PlainMonthDay#toPlainDate({year: the calendar year of 2001-06-01})":
                {
                    Assert.Equal(CalendarStatus.Completed, Cal.MonthDayFromFields(calendar, new CalendarFields { MonthCode = new MonthCode(2, false), Day = 10 }, CalendarOverflow.Constrain, out var monthDay));
                    var year = Parts(calendar, new IsoDate(2001, 6, 1)).Year;
                    status = Cal.MonthDayToDate(calendar, monthDay, new CalendarFields { Year = year }, out var result);
                    return status == CalendarStatus.Completed ? DateString(result, calendar, "always") : Throws(status);
                }

                case "PlainYearMonth#toPlainDate({day: 10})":
                {
                    Assert.Equal(CalendarStatus.Completed, Cal.DateToYearMonth(calendar, date, out var yearMonth));
                    status = Cal.YearMonthToDate(calendar, yearMonth, new CalendarFields { Day = 10 }, out var result);
                    return status == CalendarStatus.Completed ? DateString(result, calendar, "always") : Throws(status);
                }

                case "PlainYearMonth.from(fields) reference day":
                {
                    var d = Parts(calendar, date);
                    status = Cal.YearMonthFromFields(calendar, new CalendarFields { Year = d.Year, MonthCode = d.MonthCode }, CalendarOverflow.Constrain, out var yearMonth);
                    return status == CalendarStatus.Completed ? YearMonthString(yearMonth, calendar, "always") : Throws(status);
                }

                case "PlainYearMonth#with({monthCode: M01})":
                {
                    Assert.Equal(CalendarStatus.Completed, Cal.DateToYearMonth(calendar, date, out var yearMonth));
                    Assert.Equal(CalendarStatus.Completed, Cal.DateToFields(calendar, yearMonth, CalendarFieldsType.YearMonth, out var fields));
                    var merged = TemporalCalendars.MergeFields(calendar, fields, new CalendarFields { MonthCode = new MonthCode(1, false) });
                    status = Cal.YearMonthFromFields(calendar, merged, CalendarOverflow.Constrain, out var result);
                    return status == CalendarStatus.Completed ? YearMonthString(result, calendar, "always") : Throws(status);
                }

                default:
                {
                    Assert.Equal("PlainMonthDay#with({day: 1})", m.Groups["what"].Value);
                    Assert.Equal(CalendarStatus.Completed, Cal.DateToMonthDay(calendar, date, out var monthDay));
                    Assert.Equal(CalendarStatus.Completed, Cal.DateToFields(calendar, monthDay, CalendarFieldsType.MonthDay, out var fields));
                    var merged = TemporalCalendars.MergeFields(calendar, fields, new CalendarFields { Day = 1 });
                    status = Cal.MonthDayFromFields(calendar, merged, CalendarOverflow.Constrain, out var result);
                    return status == CalendarStatus.Completed ? MonthDayString(result, calendar, "always") : Throws(status);
                }
            }
        }, expectedComputed: 144);
    }

    // ---- C18: the table calendars at their tables' edges -------------------------------------------------

    [Fact]
    public void C18_the_table_calendars_answer_as_Broiler_VM_across_their_tables_seams()
    {
        var lines = Correspondence("c06-table-edges.vm.txt", "6e463f7121eedf7a045917735aa41bbe2b9eb996866f039112fdaa9197085cc9");
        var yearInfo = new Regex("^(?<cal>[a-z-]+) (?<y>\\d+)$");
        var add = new Regex("^(?<cal>[a-z-]+) (?<iso>\\d{4}-\\d\\d-\\d\\d) \\+ (?<dur>\\{.*\\})$");

        Replay(lines, label =>
        {
            if (yearInfo.Match(label) is { Success: true } y)
            {
                var calendar = Id(y.Groups["cal"].Value);
                var year = Number(y.Groups["y"].Value);
                Assert.Equal(CalendarStatus.Completed, Cal.DateFromFields(calendar, new CalendarFields { Year = year, Month = 1, Day = 1 }, CalendarOverflow.Constrain, out var first));
                var parts = Parts(calendar, first);
                var lengths = string.Empty;
                var leap = new List<string>();
                for (var month = 1; month <= parts.MonthsInYear; month++)
                {
                    Assert.Equal(CalendarStatus.Completed, Cal.DateFromFields(calendar, new CalendarFields { Year = year, Month = month, Day = 1 }, CalendarOverflow.Constrain, out var date));
                    var d = Parts(calendar, date);
                    lengths += d.DaysInMonth == 30 ? "l" : "s";
                    if (d.MonthCode.IsLeapMonth)
                        leap.Add(d.MonthCode + "@" + Js(month));
                }

                return Iso(first) + " " + Js(parts.DaysInYear) + " " + Js(parts.MonthsInYear) + " " + lengths + " " + string.Join(',', leap);
            }

            var a = add.Match(label);
            Assert.True(a.Success, label);
            {
                var calendar = Id(a.Groups["cal"].Value);
                var start = ParseIso(a.Groups["iso"].Value);
                var status = Cal.DateAdd(calendar, start, ParseDuration(a.Groups["dur"].Value), CalendarOverflow.Constrain, out var end);
                if (status != CalendarStatus.Completed)
                    return Throws(status);

                Assert.Equal(CalendarStatus.Completed, Cal.DateUntil(calendar, start, end, CalendarUnit.Month, out var until));
                var e = Parts(calendar, end);
                return Js(e.Year) + "/" + e.MonthCode + "/" + Js(e.Day) + " " + Iso(end) + " until months " + DurationString(until);
            }
        }, expectedComputed: 34);
    }
}
