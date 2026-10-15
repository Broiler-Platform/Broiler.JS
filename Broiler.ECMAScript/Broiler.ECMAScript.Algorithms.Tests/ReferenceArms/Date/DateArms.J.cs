using System;
using System.Collections.Generic;
using System.Globalization;
using Broiler.ECMAScript.Time;
using J = Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Date.J;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Date;

/// <summary>
/// Arm J's variants: <c>JSDateMath</c>'s arithmetic and local time, <c>Date.UTC</c>'s inline
/// <c>MakeFullYear</c>, <c>Date.parse</c> and the renderings, and the civil members of
/// <c>JSTemporalPlainDate</c> (arm J) and of the eight other types (arms J2 to J9), each called with the CLR
/// values its engine passes.
/// </summary>
/// <remarks>
/// A civil member that takes an <see cref="int"/> year is called only for the years an <see cref="int"/>
/// holds. <c>JSTemporalPlainDate.IsValidISODate</c> also checks Temporal's limits, which stay in the engine
/// (JSD-0072), so its S is <see cref="IsoCalendarMath.IsValidIsoDate"/> with the same limits, the engine's own
/// constants, applied to S's epoch days.
/// </remarks>
internal static partial class DateArms
{
    private static bool IntYear(DateInput i) => i.Year is >= int.MinValue and <= int.MaxValue;

    private static bool ValidIntYear(DateInput i) => i.Flag && IntYear(i);

    // ---- JSDateMath's arithmetic ---------------------------------------------------------------

    private static string JDay(double t) => DateRender.Num(J.JSDateMath.Day(t));

    private static string JTimeWithinDay(double t) => DateRender.Num(J.JSDateMath.TimeWithinDay(t));

    private static string JYearFromTime(double t) => DateRender.Num(J.JSDateMath.YearFromTime(t));

    private static string JMonthFromTime(double t) => DateRender.Num(J.JSDateMath.MonthFromTime(t));

    private static string JDateFromTime(double t) => DateRender.Num(J.JSDateMath.DateFromTime(t));

    private static string JWeekDay(double t) => DateRender.Num(J.JSDateMath.WeekDay(t));

    private static string JHourFromTime(double t) => DateRender.Num(J.JSDateMath.HourFromTime(t));

    private static string JMinFromTime(double t) => DateRender.Num(J.JSDateMath.MinFromTime(t));

    private static string JSecFromTime(double t) => DateRender.Num(J.JSDateMath.SecFromTime(t));

    private static string JMsFromTime(double t) => DateRender.Num(J.JSDateMath.MsFromTime(t));

    private static string JDaysInYear(long y) => DateRender.Num(J.JSDateMath.DaysInYear(y));

    private static string JDayFromYear(long y) => DateRender.Num(J.JSDateMath.DayFromYear(y));

    private static string JMakeTime(double h, double m, double s, double ms) => DateRender.Num(J.JSDateMath.MakeTime(h, m, s, ms));

    private static string JMakeFullYear(double y) => DateRender.Num(J.JSDate.MakeFullYear(y));

    private static string JMakeDay(double y, double m, double d) => DateRender.Num(J.JSDateMath.MakeDay(y, m, d));

    private static string JMakeDate(double day, double time) => DateRender.Num(J.JSDateMath.MakeDate(day, time));

    private static string JTimeClip(double t) => DateRender.Num(J.JSDateMath.TimeClip(t));

    private static string JCore(double[] c) =>
        DateRender.Num(J.JSDateMath.TimeClip(J.JSDateMath.MakeDate(J.JSDateMath.MakeDay(c[0], c[1], c[2]), J.JSDateMath.MakeTime(c[3], c[4], c[5], c[6]))));

    private static string JLocalTime(double t, TimeZoneInfo zone) => DateRender.Num(J.JSDateMath.LocalTime(t, zone));

    private static string JUtc(double t, TimeZoneInfo zone) => DateRender.Num(J.JSDateMath.UTC(t, zone));

    // ---- The civil copies, J to J9 -------------------------------------------------------------

    private static IEnumerable<DateVariant> JLeapYear(Func<DateInput, string> s)
    {
        static string B(bool b) => DateRender.Bool(b);
        yield return new("J", "JSDateMath", i => B(J.JSDateMath.IsLeapYear(i.Year)), s);
        yield return new("J", "JSTemporalPlainDate", i => B(J.JSTemporalPlainDateCivil.IsLeapYear(i.Year)), s);
        yield return new("J2", "JSTemporalInstant", i => B(J.JSTemporalInstantCivil.IsLeapYear(i.Year)), s);
        yield return new("J3", "JSTemporalPlainDateTime", i => B(J.JSTemporalPlainDateTimeCivil.IsLeapYear(i.Year)), s);
        yield return new("J4", "JSTemporalPlainMonthDay", i => B(J.JSTemporalPlainMonthDayCivil.IsLeapYear(i.Year)), s);
        yield return new("J5", "JSTemporalPlainYearMonth", i => B(J.JSTemporalPlainYearMonthCivil.IsLeapYear(i.Year)), s);
        yield return new("J6", "JSTemporalZonedDateTime", i => B(J.JSTemporalZonedDateTimeCivil.IsLeapYear(i.Year)), s);
        yield return new("J7", "TemporalIsoString", i => B(J.TemporalIsoStringCivil.IsLeapYear((int)i.Year)), s, IntYear);
        yield return new("J8", "TemporalArithmeticCalendar", i => B(J.TemporalArithmeticCalendarCivil.GregorianLeap((int)i.Year)), s, IntYear);
    }

    private static IEnumerable<DateVariant> JDaysInIsoYear(Func<DateInput, string> s) => [];

    private static IEnumerable<DateVariant> JMonthLength(Func<DateInput, string> s)
    {
        yield return new("J", "JSDate.DaysInMonth", i => DateRender.Num(J.JSDate.DaysInMonth(i.Year, i.Month)), s);
        yield return new("J", "JSTemporalPlainDate", i => DateRender.Num(J.JSTemporalPlainDateCivil.DaysInMonthOf(i.Year, i.Month)), s);
        yield return new("J2", "JSTemporalInstant", i => DateRender.Num(J.JSTemporalInstantCivil.DaysInMonthOf(i.Year, i.Month)), s);
        yield return new("J3", "JSTemporalPlainDateTime", i => DateRender.Num(J.JSTemporalPlainDateTimeCivil.DaysInMonthOf(i.Year, i.Month)), s);
        yield return new("J4", "JSTemporalPlainMonthDay", i => DateRender.Num(J.JSTemporalPlainMonthDayCivil.DaysInMonthOf(i.Year, i.Month)), s);
        yield return new("J5", "JSTemporalPlainYearMonth", i => DateRender.Num(J.JSTemporalPlainYearMonthCivil.DaysInMonthOf(i.Year, i.Month)), s);
        yield return new("J6", "JSTemporalZonedDateTime", i => DateRender.Num(J.JSTemporalZonedDateTimeCivil.DaysInMonthOf(i.Year, i.Month)), s);
        yield return new("J7", "TemporalIsoString", i => DateRender.Num(J.TemporalIsoStringCivil.DaysInMonth((int)i.Year, i.Month)), s, IntYear);
    }

    private static IEnumerable<DateVariant> JDaysFromCivil(Func<DateInput, string> s)
    {
        static Func<DateInput, string> R(Func<long, long, long, long> f) => i => DateRender.Num(f(i.Year, i.Month, i.Day));
        yield return new("J", "JSTemporalPlainDate", R(J.JSTemporalPlainDateCivil.DaysFromCivil), s, Valid);
        yield return new("J2", "JSTemporalInstant", R(J.JSTemporalInstantCivil.DaysFromCivil), s, Valid);
        yield return new("J3", "JSTemporalPlainDateTime", R(J.JSTemporalPlainDateTimeCivil.DaysFromCivil), s, Valid);
        yield return new("J5", "JSTemporalPlainYearMonth", R(J.JSTemporalPlainYearMonthCivil.DaysFromCivil), s, Valid);
        yield return new("J6", "JSTemporalZonedDateTime", R(J.JSTemporalZonedDateTimeCivil.DaysFromCivil), s, Valid);
        yield return new("J8", "TemporalArithmeticCalendar", R(J.TemporalArithmeticCalendarCivil.DaysFromCivil), s, Valid);
        yield return new("J9", "TemporalNonIso", R(J.TemporalNonIsoCivil.DaysFromCivil), s, Valid);
    }

    private static IEnumerable<DateVariant> JCivilFromDays(Func<DateInput, string> s)
    {
        static Func<DateInput, string> R(Func<long, (long y, long m, long d)> f) => i => { var d = f(i.Year); return DateRender.Civil(d.y, d.m, d.d); };
        yield return new("J", "JSTemporalPlainDate", R(J.JSTemporalPlainDateCivil.CivilFromDays), s);
        yield return new("J2", "JSTemporalInstant", R(J.JSTemporalInstantCivil.CivilFromDays), s);
        yield return new("J3", "JSTemporalPlainDateTime", R(J.JSTemporalPlainDateTimeCivil.CivilFromDays), s);
        yield return new("J5", "JSTemporalPlainYearMonth", R(J.JSTemporalPlainYearMonthCivil.CivilFromDays), s);
        yield return new("J6", "JSTemporalZonedDateTime", R(J.JSTemporalZonedDateTimeCivil.CivilFromDays), s);
        yield return new("J8", "TemporalArithmeticCalendar", R(J.TemporalArithmeticCalendarCivil.CivilFromDays), s);
        yield return new("J9", "TemporalNonIso", R(J.TemporalNonIsoCivil.CivilFromDays), s);
    }

    private static IEnumerable<DateVariant> JDayOfWeek(Func<DateInput, string> s)
    {
        static Func<DateInput, string> R(Func<int, int, int, int> f) => i => DateRender.Num(f((int)i.Year, i.Month, i.Day));
        yield return new("J", "JSTemporalPlainDate", R(J.JSTemporalPlainDateCivil.IsoDayOfWeek), s, ValidIntYear);
        yield return new("J3", "JSTemporalPlainDateTime", R(J.JSTemporalPlainDateTimeCivil.IsoDayOfWeek), s, ValidIntYear);
        yield return new("J6", "JSTemporalZonedDateTime", R(J.JSTemporalZonedDateTimeCivil.IsoDayOfWeek), s, ValidIntYear);
    }

    private static IEnumerable<DateVariant> JDayOfYear(Func<DateInput, string> s)
    {
        yield return new("J", "JSTemporalPlainDate", i => DateRender.Num(J.JSTemporalPlainDateCivil.DayOfYear((int)i.Year, i.Month, i.Day)), s, ValidIntYear);
        yield return new("J3", "JSTemporalPlainDateTime", i => DateRender.Num(J.JSTemporalPlainDateTimeCivil.DayOfYear((int)i.Year, i.Month, i.Day)), s, ValidIntYear);
        yield return new("J6", "JSTemporalZonedDateTime", i => DateRender.Num(J.JSTemporalZonedDateTimeCivil.DayOfYear(((int)i.Year, i.Month, i.Day))), s, ValidIntYear);
    }

    private static IEnumerable<DateVariant> JIsoWeek(Func<DateInput, string> s)
    {
        static Func<DateInput, string> R(Func<int, int, int, (int week, int year)> f) =>
            i => { var w = f((int)i.Year, i.Month, i.Day); return DateRender.Pair(w.week, w.year); };
        yield return new("J", "JSTemporalPlainDate", R(J.JSTemporalPlainDateCivil.IsoWeek), s, ValidIntYear);
        yield return new("J3", "JSTemporalPlainDateTime", R(J.JSTemporalPlainDateTimeCivil.IsoWeek), s, ValidIntYear);
        yield return new("J6", "JSTemporalZonedDateTime", R(J.JSTemporalZonedDateTimeCivil.IsoWeek), s, ValidIntYear);
    }

    private static IEnumerable<DateVariant> JValidity(Func<DateInput, string> s)
    {
        static Func<DateInput, string> R(Func<int, int, int, bool> f) => i => DateRender.Bool(f((int)i.Year, i.Month, i.Day));

        // JSTemporalPlainDate's validity holds Temporal's limits, which the engine keeps around S's.
        static string SWithLimits(DateInput i)
        {
            if (!IsoCalendarMath.IsValidIsoDate(i.Year, i.Month, i.Day))
                return DateRender.Bool(false);
            var epoch = IsoCalendarMath.EpochDays(i.Year, i.Month, i.Day);
            return DateRender.Bool(epoch >= J.JSTemporalPlainDateCivil.MinEpochDays && epoch <= J.JSTemporalPlainDateCivil.MaxEpochDays);
        }

        yield return new("J", "JSTemporalPlainDate (with Temporal's limits)", R(J.JSTemporalPlainDateCivil.IsValidISODate), SWithLimits, IntYear);
        yield return new("J3", "JSTemporalPlainDateTime", R(J.JSTemporalPlainDateTimeCivil.IsValidISODate), s, IntYear);
        yield return new("J4", "JSTemporalPlainMonthDay", R(J.JSTemporalPlainMonthDayCivil.IsValidISODate), s, IntYear);
        yield return new("J5", "JSTemporalPlainYearMonth", R(J.JSTemporalPlainYearMonthCivil.IsValidISODate), s, IntYear);
        yield return new("J6", "JSTemporalZonedDateTime", R(J.JSTemporalZonedDateTimeCivil.IsValidISODate), s, IntYear);
    }

    // ---- Parsing and the renderings ------------------------------------------------------------

    /// <summary>Broiler.JS's renderings of a time value that E5 reads back: toString in UTC and in New York, and toUTCString.</summary>
    private static IEnumerable<(string Form, string Text)> JRenderings(double t)
    {
        yield return ("toString UTC", J.JSDate.ToString(t, Zone("UTC")));
        yield return ("toString America/New_York", J.JSDate.ToString(t, Zone("America/New_York")));
        yield return ("toUTCString", J.JSDate.ToUTCString(t));
    }

    private static IEnumerable<DateVariant> JParse(Func<DateInput, string> s, Func<DateInput, bool>? applies = null)
    {
        yield return new("J", "Date.parse", i => DateRender.Num(J.JSDate.Parse(i.Text, CultureInfo.CurrentCulture, TimeZoneInfo.Local)), s, applies);
    }

    private static bool Named(DateInput i) => i.Name is not null;

    private static IEnumerable<DateVariant> JDateString(Func<DateInput, string> s)
    {
        yield return new("J", "DateString", i => DateRender.Str(J.JSDate.DateString(i.Number)), s);
    }

    private static IEnumerable<DateVariant> JTimeString(Func<DateInput, string> s)
    {
        yield return new("J", "ClockString and \" GMT\"", i => DateRender.Str(J.JSDate.ClockString(i.Number) + " GMT"), s);
    }

    private static IEnumerable<DateVariant> JTimeZoneString(Func<DateInput, string> s)
    {
        yield return new("J", "ToTimeZoneString", i => DateRender.Str(J.JSDate.ToTimeZoneString(i.Offset, i.Name!)), s, Named);
    }

    /// <summary>toString's body (JSDatePrototype.Formatters.cs :199-213) at a fixed offset and name: its LocalTime(t) - t is the offset.</summary>
    private static IEnumerable<DateVariant> JToStringAtOffset(Func<DateInput, string> s)
    {
        yield return new("J", "toString's body", i =>
        {
            var local = i.Number + i.Offset;
            return DateRender.Str($"{J.JSDate.DateString(local)} {J.JSDate.ClockString(local)} {J.JSDate.ToTimeZoneString(local - i.Number, i.Name!)}");
        }, s, Named);
    }

    private static IEnumerable<DateVariant> JToStringInZone(Func<DateInput, string> s)
    {
        yield return new("J", "toString", i => DateRender.Str(J.JSDate.ToString(i.Number, Zone(i.Zone))), s);
    }

    private static IEnumerable<DateVariant> JToUtcString(Func<DateInput, string> s)
    {
        yield return new("J", "toUTCString", i => DateRender.Str(J.JSDate.ToUTCString(i.Number)), s);
    }

    private static IEnumerable<DateVariant> JToIsoString(Func<DateInput, string> s)
    {
        yield return new("J", "toISOString", i => DateRender.Str(J.JSDate.ToISOString(i.Number)), s);
    }
}
