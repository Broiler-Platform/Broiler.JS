using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Broiler.ECMAScript.Temporal;
using Broiler.ECMAScript.Time;
using Xunit;
using static Broiler.ECMAScript.Algorithms.Tests.CalendarTestSupport;

namespace Broiler.ECMAScript.Algorithms.Tests;

/// <summary>
/// The calendars' public contract (milestone SJL-10.3, part B), beyond the correspondence's lines: the identifiers,
/// month codes and statuses, field merging, the preconditions each member throws on, the arithmetic through
/// <see cref="ICalendarArithmetic"/> as the durations sub-area calls it, its round trip in every calendar, and
/// concurrent use.
/// </summary>
public class CalendarApiTests
{
    private static TemporalCalendars Cal => Calendars;

    // ---- identifiers -------------------------------------------------------------------------------------

    [Theory]
    [InlineData("iso8601", CalendarId.Iso8601)]
    [InlineData("ISO8601", CalendarId.Iso8601)]
    [InlineData("Islamic-Umalqura", CalendarId.IslamicUmalqura)]
    [InlineData("ethiopic-amete-alem", CalendarId.Ethioaa)]
    [InlineData("ETHIOPIC-AMETE-ALEM", CalendarId.Ethioaa)]
    [InlineData("islamicc", CalendarId.IslamicCivil)]
    [InlineData("IslamicC", CalendarId.IslamicCivil)]
    [InlineData("roc", CalendarId.Roc)]
    public void Canonicalize_folds_ASCII_case_and_the_two_aliases(string identifier, CalendarId expected)
    {
        Assert.Equal(CalendarStatus.Completed, TemporalCalendars.Canonicalize(identifier, out var calendar));
        Assert.Equal(expected, calendar);
    }

    [Theory]
    [InlineData("gregorian")] // C2: CLDR's legacy alias, not Table 1's (JSD-0078, decision D5)
    [InlineData("GREGORIAN")]
    [InlineData("minguo")]
    [InlineData("islamic")] // C3: no row of Table 1
    [InlineData("islamic-rgsa")]
    [InlineData("iso-8601")]
    [InlineData("iso8601 ")]
    [InlineData("chinese\0")]
    [InlineData("İso8601")] // a capital I with a dot does not fold to i
    [InlineData("japаnese")] // a Cyrillic a
    [InlineData("")]
    [InlineData("ethiopic-amete-alem-")]
    public void Canonicalize_refuses_what_Table_1_does_not_name(string identifier)
    {
        Assert.Equal(CalendarStatus.UnknownCalendar, TemporalCalendars.Canonicalize(identifier, out var calendar));
        Assert.Equal(default, calendar);
    }

    [Fact]
    public void Every_calendar_has_one_canonical_identifier_and_era_support_as_CalendarSupportsEra_states()
    {
        var identifiers = new HashSet<string>(StringComparer.Ordinal);
        for (var id = CalendarId.Iso8601; id <= CalendarId.Roc; id++)
        {
            Assert.True(identifiers.Add(TemporalCalendars.Identifier(id)));
            Assert.Equal(id is not (CalendarId.Iso8601 or CalendarId.Chinese or CalendarId.Dangi), TemporalCalendars.SupportsEra(id));
        }

        Assert.Equal(16, identifiers.Count);
        Assert.Throws<ArgumentOutOfRangeException>(() => TemporalCalendars.Identifier((CalendarId)16));
        Assert.Throws<ArgumentOutOfRangeException>(() => TemporalCalendars.SupportsEra((CalendarId)(-1)));
    }

    [Fact]
    public void The_four_missing_field_statuses_are_TypeErrors_and_the_others_RangeErrors()
    {
        var typeErrors = new[] { CalendarStatus.MissingYear, CalendarStatus.MissingMonth, CalendarStatus.MissingDay, CalendarStatus.IncompleteEra };
        foreach (var status in Enum.GetValues<CalendarStatus>())
            Assert.Equal(typeErrors.Contains(status), TemporalCalendars.IsTypeError(status));
    }

    // ---- month codes -------------------------------------------------------------------------------------

    [Theory]
    [InlineData("M01", 1, false)]
    [InlineData("M12", 12, false)]
    [InlineData("M13", 13, false)]
    [InlineData("M99", 99, false)]
    [InlineData("M05L", 5, true)]
    [InlineData("M00L", 0, true)] // the grammar's; no calendar has it
    public void A_month_code_is_parsed_and_written_back_as_the_grammar_has_it(string text, int number, bool leap)
    {
        Assert.Equal(CalendarStatus.Completed, MonthCode.Parse(text, out var code));
        Assert.Equal(new MonthCode(number, leap), code);
        Assert.Equal(text, code.ToString());
    }

    [Theory]
    [InlineData("M00")]
    [InlineData("m01")]
    [InlineData("M1")]
    [InlineData("M001")]
    [InlineData("M01l")]
    [InlineData("L01M")]
    [InlineData("M01 ")]
    [InlineData(" M01")]
    [InlineData("")]
    [InlineData("M٠١")] // C6: Arabic-Indic digits
    [InlineData("M0١")]
    [InlineData("M０１")] // fullwidth digits
    [InlineData("M01ʟ")] // a small capital L
    [InlineData("M01LL")]
    public void A_month_code_off_the_grammar_is_refused(string text)
    {
        Assert.Equal(CalendarStatus.InvalidMonthCode, MonthCode.Parse(text, out var code));
        Assert.Equal(default, code);
    }

    [Fact]
    public void A_month_code_outside_the_grammar_cannot_be_made()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MonthCode(0, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MonthCode(100, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MonthCode(-1, true));
    }

    [Theory]
    [InlineData("hebrew", 5783, "M05L", "M06")] // C7: Hebrew skips forward to Adar
    [InlineData("chinese", 2024, "M02L", "M02")] // skips back to the common month
    [InlineData("dangi", 2024, "M02L", "M02")]
    public void A_leap_month_the_year_lacks_is_constrained_or_refused(string name, long year, string code, string constrained)
    {
        var calendar = Id(name);
        MonthCode.Parse(code, out var monthCode);
        var fields = new CalendarFields { Year = year, MonthCode = monthCode, Day = 1 };
        Assert.Equal(CalendarStatus.Completed, Cal.DateFromFields(calendar, fields, CalendarOverflow.Constrain, out var date));
        Assert.Equal(constrained, Parts(calendar, date).MonthCode.ToString());
        Assert.Equal(CalendarStatus.MonthNotInYear, Cal.DateFromFields(calendar, fields, CalendarOverflow.Reject, out var none));
        Assert.Equal(default, none);
    }

    [Theory]
    [InlineData("coptic", "M00L")]
    [InlineData("coptic", "M14")]
    [InlineData("persian", "M13")] // C5: constrained silently by Broiler.JS before the correction
    [InlineData("indian", "M13")]
    [InlineData("islamic-umalqura", "M13")]
    [InlineData("hebrew", "M06L")]
    [InlineData("chinese", "M13")]
    [InlineData("gregory", "M01L")]
    [InlineData("iso8601", "M13")]
    [InlineData("iso8601", "M05L")]
    public void A_month_code_the_calendar_lacks_is_refused_under_either_overflow(string name, string code)
    {
        var calendar = Id(name);
        MonthCode.Parse(code, out var monthCode);
        var fields = new CalendarFields { Year = LeapYear[name], MonthCode = monthCode, Day = 1 };
        Assert.Equal(CalendarStatus.InvalidMonthCode, Cal.DateFromFields(calendar, fields, CalendarOverflow.Constrain, out _));
        Assert.Equal(CalendarStatus.InvalidMonthCode, Cal.DateFromFields(calendar, fields, CalendarOverflow.Reject, out _));
    }

    // ---- fields ------------------------------------------------------------------------------------------

    [Fact]
    public void Missing_fields_answer_their_TypeError_statuses_in_the_specifications_order()
    {
        var m01 = new MonthCode(1, false);
        Assert.Equal(CalendarStatus.MissingYear, Cal.DateFromFields(CalendarId.Iso8601, new CalendarFields { MonthCode = m01 }, CalendarOverflow.Constrain, out _));
        Assert.Equal(CalendarStatus.MissingDay, Cal.DateFromFields(CalendarId.Iso8601, new CalendarFields { Year = 2020, MonthCode = m01 }, CalendarOverflow.Constrain, out _));
        Assert.Equal(CalendarStatus.MissingMonth, Cal.DateFromFields(CalendarId.Iso8601, new CalendarFields { Year = 2020, Day = 1 }, CalendarOverflow.Constrain, out _));

        // AN ERA WITHOUT ITS YEAR stands for no year, and a year with half an era is incomplete.
        Assert.Equal(CalendarStatus.MissingYear, Cal.DateFromFields(CalendarId.Gregory, new CalendarFields { Era = "ce", MonthCode = m01, Day = 1 }, CalendarOverflow.Constrain, out _));
        Assert.Equal(CalendarStatus.IncompleteEra, Cal.DateFromFields(CalendarId.Gregory, new CalendarFields { Year = 2020, Era = "ce", MonthCode = m01, Day = 1 }, CalendarOverflow.Constrain, out _));
        Assert.Equal(CalendarStatus.IncompleteEra, Cal.DateFromFields(CalendarId.Gregory, new CalendarFields { Year = 2020, EraYear = 2020, MonthCode = m01, Day = 1 }, CalendarOverflow.Constrain, out _));

        // A CALENDAR WITHOUT ERAS IGNORES THEM: chinese, as iso8601 does.
        Assert.Equal(CalendarStatus.MissingYear, Cal.DateFromFields(CalendarId.Chinese, new CalendarFields { Era = "ce", EraYear = 2020, MonthCode = m01, Day = 1 }, CalendarOverflow.Constrain, out _));

        // A MONTH-DAY OUTSIDE ISO 8601 NEEDS A YEAR WITH A MONTH NUMBER, and none with a month code.
        Assert.Equal(CalendarStatus.MissingYear, Cal.MonthDayFromFields(CalendarId.Hebrew, new CalendarFields { Month = 6, Day = 1 }, CalendarOverflow.Constrain, out _));
        Assert.Equal(CalendarStatus.Completed, Cal.MonthDayFromFields(CalendarId.Iso8601, new CalendarFields { Month = 2, Day = 29 }, CalendarOverflow.Reject, out var leapDay));
        Assert.Equal(new IsoDate(1972, 2, 29), leapDay);
    }

    [Fact]
    public void Eras_resolve_to_their_years_and_older_names_are_unknown()
    {
        var m01 = new MonthCode(1, false);
        Assert.Equal(CalendarStatus.Completed, Cal.DateFromFields(CalendarId.Japanese, new CalendarFields { Era = "reiwa", EraYear = 5, MonthCode = m01, Day = 1 }, CalendarOverflow.Constrain, out var reiwa));
        Assert.Equal(new IsoDate(2023, 1, 1), reiwa);
        Assert.Equal(CalendarStatus.Completed, Cal.DateFromFields(CalendarId.Gregory, new CalendarFields { Era = "bc", EraYear = 5, MonthCode = m01, Day = 1 }, CalendarOverflow.Constrain, out var bc));
        Assert.Equal(new IsoDate(-4, 1, 1), bc);
        Assert.Equal(CalendarStatus.EraYearMismatch, Cal.DateFromFields(CalendarId.Gregory, new CalendarFields { Era = "ce", EraYear = 2019, Year = 2020, MonthCode = m01, Day = 1 }, CalendarOverflow.Constrain, out _));

        // C9: names of earlier drafts and other cases are not Table 2's (JSD-0078, decision D4).
        foreach (var era in new[] { "CE", "gregory", "gregory-inverse", "incarnation", "mundi", "saka", "minguo", "before-roc" })
            Assert.Equal(CalendarStatus.UnknownEra, Cal.DateFromFields(CalendarId.Gregory, new CalendarFields { Era = era, EraYear = 5, MonthCode = m01, Day = 1 }, CalendarOverflow.Constrain, out _));
    }

    [Fact]
    public void MergeFields_drops_what_each_additional_field_entails()
    {
        var m03 = new MonthCode(3, false);
        var date = new CalendarFields { Era = "reiwa", EraYear = 5, Year = 2023, Month = 3, MonthCode = m03, Day = 15 };

        // A MONTH DROPS THE MONTH CODE, and in japanese the era with it.
        var month = TemporalCalendars.MergeFields(CalendarId.Japanese, date, new CalendarFields { Month = 1 });
        Assert.Equal(new CalendarFields { Year = 2023, Month = 1, Day = 15 }, month);

        // A YEAR DROPS THE ERA in a calendar with eras, and not in one without.
        Assert.Equal(new CalendarFields { Year = 2020, Month = 3, MonthCode = m03, Day = 15 }, TemporalCalendars.MergeFields(CalendarId.Gregory, date, new CalendarFields { Year = 2020 }));
        Assert.Equal(date with { Year = 2020 }, TemporalCalendars.MergeFields(CalendarId.Chinese, date, new CalendarFields { Year = 2020 }));

        // AN ERA DROPS THE YEAR; A DAY DROPS NOTHING BUT ITSELF outside japanese.
        Assert.Equal(new CalendarFields { Era = "ce", EraYear = 1, Month = 3, MonthCode = m03, Day = 15 }, TemporalCalendars.MergeFields(CalendarId.Gregory, date, new CalendarFields { Era = "ce", EraYear = 1 }));
        Assert.Equal(date with { Day = 1 }, TemporalCalendars.MergeFields(CalendarId.Gregory, date, new CalendarFields { Day = 1 }));
        Assert.Equal(new CalendarFields { Year = 2023, Month = 3, MonthCode = m03, Day = 1 }, TemporalCalendars.MergeFields(CalendarId.Japanese, date, new CalendarFields { Day = 1 }));

        // NOTHING ADDITIONAL KEEPS EVERYTHING.
        Assert.Equal(date, TemporalCalendars.MergeFields(CalendarId.Iso8601, date, default));
    }

    [Fact]
    public void A_field_outside_its_conversions_range_is_a_broken_precondition()
    {
        var m01 = new MonthCode(1, false);
        var good = new CalendarFields { Year = 2020, MonthCode = m01, Day = 1 };
        foreach (var bad in new[]
        {
            good with { Year = 2020.5 },
            good with { Year = double.NaN },
            good with { Year = double.PositiveInfinity },
            good with { Day = 0 },
            good with { Day = -1 },
            good with { Month = 0.5 },
            good with { EraYear = 1.5, Era = "ce" },
            good with { MonthCode = default(MonthCode) },
        })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Cal.DateFromFields(CalendarId.Gregory, bad, CalendarOverflow.Constrain, out _));
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => Cal.DateFromFields((CalendarId)99, good, CalendarOverflow.Constrain, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cal.DateFromFields(CalendarId.Gregory, good, (CalendarOverflow)2, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cal.DateToFields(CalendarId.Gregory, new IsoDate(2020, 1, 1), (CalendarFieldsType)3, out _));
        Assert.Throws<ArgumentNullException>(() => new TemporalCalendars(null!));
    }

    [Fact]
    public void An_ISO_date_outside_the_years_the_calendars_compute_is_out_of_range_and_one_that_is_no_date_throws()
    {
        // THE YEAR-MONTH LIMIT'S FIRST REFERENCE DATE is before ISODateWithinLimits and is still a calendar's date.
        Assert.Equal(CalendarStatus.Completed, Cal.IsoToDate(CalendarId.Gregory, new IsoDate(-271821, 4, 1), out var first));
        Assert.Equal("bce", first.Era);
        Assert.Equal(CalendarStatus.OutOfRange, Cal.IsoToDate(CalendarId.Hebrew, new IsoDate(-271822, 12, 31), out _));
        Assert.Equal(CalendarStatus.OutOfRange, Cal.IsoToDate(CalendarId.Iso8601, new IsoDate(275761, 1, 1), out _));
        Assert.Equal(CalendarStatus.OutOfRange, Cal.IsoToDate(CalendarId.Chinese, new IsoDate(long.MaxValue, 1, 1), out _));

        Assert.Throws<ArgumentOutOfRangeException>(() => Cal.IsoToDate(CalendarId.Gregory, new IsoDate(2021, 2, 29), out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cal.IsoToDate(CalendarId.Gregory, new IsoDate(2021, 13, 1), out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cal.DateAdd(CalendarId.Gregory, new IsoDate(2021, 4, 31), default, CalendarOverflow.Constrain, out _));
    }

    // ---- arithmetic through the provider interface -------------------------------------------------------

    [Fact]
    public void The_provider_interface_adds_and_differences_in_each_calendar()
    {
        ICalendarArithmetic arithmetic = Cal;

        // GREGORY: a month from 31 January is constrained, or refused.
        Assert.Equal(CalendarStatus.Completed, arithmetic.DateAdd(CalendarId.Gregory, new IsoDate(2023, 1, 31), new CalendarDuration(0, 1, 0, 0), CalendarOverflow.Constrain, out var february));
        Assert.Equal(new IsoDate(2023, 2, 28), february);
        Assert.Equal(CalendarStatus.FieldOutOfRange, arithmetic.DateAdd(CalendarId.Gregory, new IsoDate(2023, 1, 31), new CalendarDuration(0, 1, 0, 0), CalendarOverflow.Reject, out _));
        Assert.Equal(CalendarStatus.FieldOutOfRange, arithmetic.DateAdd(CalendarId.Iso8601, new IsoDate(2023, 1, 31), new CalendarDuration(0, 1, 0, 0), CalendarOverflow.Reject, out _));

        // CHINESE: the leap month M02L of 2023 is constrained to M02 a year on, or refused.
        Assert.Equal(CalendarStatus.Completed, arithmetic.DateAdd(CalendarId.Chinese, new IsoDate(2023, 3, 22), new CalendarDuration(1, 0, 0, 0), CalendarOverflow.Constrain, out var nextYear));
        Assert.Equal("M02", Parts(CalendarId.Chinese, nextYear).MonthCode.ToString());
        Assert.Equal(CalendarStatus.MonthNotInYear, arithmetic.DateAdd(CalendarId.Chinese, new IsoDate(2023, 3, 22), new CalendarDuration(1, 0, 0, 0), CalendarOverflow.Reject, out _));

        // THE EARLY REFUSALS AND THE LIMITS.
        Assert.Equal(CalendarStatus.OutOfRange, arithmetic.DateAdd(CalendarId.Iso8601, new IsoDate(2020, 1, 1), new CalendarDuration(1_000_000_001, 0, 0, 0), CalendarOverflow.Constrain, out _));
        Assert.Equal(CalendarStatus.OutOfRange, arithmetic.DateAdd(CalendarId.Hebrew, new IsoDate(2020, 1, 1), new CalendarDuration(300_000, 0, 0, 0), CalendarOverflow.Constrain, out _));
        Assert.Equal(CalendarStatus.OutOfRange, arithmetic.DateAdd(CalendarId.Persian, new IsoDate(275760, 9, 13), new CalendarDuration(0, 0, 0, 1), CalendarOverflow.Constrain, out _));
        Assert.Equal(CalendarStatus.Completed, arithmetic.DateAdd(CalendarId.Dangi, new IsoDate(275760, 9, 12), new CalendarDuration(0, 0, 0, 1), CalendarOverflow.Constrain, out var last));
        Assert.Equal(new IsoDate(275760, 9, 13), last);

        // DIFFERENCES: the largest units first, of one sign, and zero between equal dates.
        Assert.Equal(CalendarStatus.Completed, arithmetic.DateUntil(CalendarId.Hebrew, new IsoDate(2023, 3, 15), new IsoDate(2024, 3, 15), CalendarUnit.Month, out var months));
        Assert.Equal(new CalendarDuration(0, 12, 0, 13), months);
        Assert.Equal(CalendarStatus.Completed, arithmetic.DateUntil(CalendarId.Iso8601, new IsoDate(2024, 3, 15), new IsoDate(2023, 1, 1), CalendarUnit.Week, out var weeks));
        Assert.Equal(new CalendarDuration(0, 0, -62, -5), weeks);
        Assert.Equal(CalendarStatus.Completed, arithmetic.DateUntil(CalendarId.Coptic, new IsoDate(2023, 3, 15), new IsoDate(2023, 3, 15), CalendarUnit.Year, out var none));
        Assert.Equal(default, none);
        Assert.Throws<ArgumentOutOfRangeException>(() => arithmetic.DateUntil(CalendarId.Coptic, new IsoDate(2023, 3, 15), new IsoDate(2023, 3, 15), (CalendarUnit)4, out _));
    }

    [Fact]
    public void A_difference_added_to_its_first_date_gives_the_second_in_every_calendar()
    {
        // THE PROPOSAL'S DIFFERENCE IS THE LARGEST that does not pass the second date, so adding it back under
        // constrain lands there: a property of CalendarDateUntil the durations sub-area's nudging relies on.
        var random = new Random(20261018);
        for (var id = CalendarId.Iso8601; id <= CalendarId.Roc; id++)
        {
            for (var i = 0; i < 200; i++)
            {
                var one = FromDays(random.NextInt64(-80_000, 120_000));
                var two = FromDays(IsoCalendarMath.EpochDays(one.Year, one.Month, one.Day) + random.NextInt64(-4_000, 4_000));
                foreach (var unit in new[] { CalendarUnit.Year, CalendarUnit.Month, CalendarUnit.Week, CalendarUnit.Day })
                {
                    Assert.Equal(CalendarStatus.Completed, Cal.DateUntil(id, one, two, unit, out var difference));
                    var signs = new[] { difference.Years, difference.Months, difference.Weeks, difference.Days }.Select(Math.Sign).Where(static s => s != 0).Distinct().ToArray();
                    Assert.True(signs.Length <= 1, id + " " + Iso(one) + " " + Iso(two) + " " + difference);
                    Assert.Equal(CalendarStatus.Completed, Cal.DateAdd(id, one, difference, CalendarOverflow.Constrain, out var back));
                    Assert.True(two == back, id + " " + Iso(one) + " + " + DurationString(difference) + " = " + Iso(back) + ", not " + Iso(two));
                }
            }
        }
    }

    [Fact]
    public void A_date_converts_to_fields_and_back_in_every_calendar()
    {
        var random = new Random(181020);
        for (var id = CalendarId.Iso8601; id <= CalendarId.Roc; id++)
        {
            for (var i = 0; i < 300; i++)
            {
                var date = FromDays(random.NextInt64(-100_000_001, 100_000_001));
                Assert.Equal(CalendarStatus.Completed, Cal.DateToFields(id, date, CalendarFieldsType.Date, out var fields));
                Assert.Equal(CalendarStatus.Completed, Cal.DateFromFields(id, fields, CalendarOverflow.Reject, out var back));
                Assert.Equal(date, back);

                var parts = Parts(id, date);
                Assert.Equal(CalendarStatus.Completed, Cal.DateFromFields(id, new CalendarFields { Year = parts.Year, Month = parts.Month, Day = parts.Day }, CalendarOverflow.Reject, out var byOrdinal));
                Assert.Equal(date, byOrdinal);
                Assert.InRange(parts.DayOfYear, 1, parts.DaysInYear);
                Assert.InRange(parts.Day, 1, parts.DaysInMonth);
                Assert.InRange(parts.Month, 1, parts.MonthsInYear);
            }
        }
    }

    [Fact]
    public void The_calendars_answer_alike_from_many_threads()
    {
        var dates = Enumerable.Range(0, 400).Select(static i => FromDays((i * 250_007L) - 50_000_000)).ToArray();
        string Answer(CalendarId id) => string.Join('|', dates.Select(d => Cal.IsoToDate(id, d, out var parts) + " " + parts));
        var expected = Enumerable.Range(0, 16).Select(i => Answer((CalendarId)i)).ToArray();

        Parallel.For(0, 64, n =>
        {
            var id = (CalendarId)(n % 16);
            Assert.Equal(expected[(int)id], Answer(id));
        });
    }

    private static IsoDate FromDays(long days)
    {
        var (year, month, day) = IsoCalendarMath.FromEpochDays(days);
        return new IsoDate(year, month, day);
    }
}
