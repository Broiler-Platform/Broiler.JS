using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// Shared calendar-date math for the non-Gregorian (non-ISO) calendars, used by both
// Temporal.PlainDate and Temporal.PlainDateTime. Every operation works in the active calendar's
// (year, month, day) space via TemporalCalendarMath and returns a proleptic-Gregorian (ISO) date —
// the storage form for all Temporal types — leaving the caller to attach the calendar id and any
// time-of-day component. Keeping this in one place ensures PlainDate and PlainDateTime resolve,
// regulate, add and difference non-ISO dates identically.
internal static class TemporalNonIso
{
    // ISODateWithinLimits bounds, matching Temporal.PlainDate: −271821-04-19 … +275760-09-13.
    internal static readonly long MinEpochDays = DaysFromCivil(-271821, 4, 19);
    internal static readonly long MaxEpochDays = DaysFromCivil(275760, 9, 13);

    // The stored ISO date projected into the active calendar's (year, month, day).
    internal static (int y, int m, int d) CalendarYmd(string calendarId, int isoYear, int isoMonth, int isoDay)
        => TemporalCalendarMath.YmdFromEpochDays(calendarId, DaysFromCivil(isoYear, isoMonth, isoDay));

    // Resolves a property bag for one of the non-Gregorian calendars: the year comes from `year`
    // and/or { era, eraYear } (era-less for the lunisolar calendars), the month from `month` /
    // `monthCode` (with an optional leap "L" suffix), then the calendar (year, month, day) is
    // regulated and projected onto an ISO date.
    internal static (int y, int m, int d) ToIsoFromBag(JSObject obj, string calendarId, string overflow, string typeName)
    {
        if (obj[KeyStrings.GetOrCreate("day")].IsUndefined)
            throw JSEngine.NewTypeError($"{typeName}: missing day");

        var (year, month) = ResolveYearMonth(obj, calendarId, typeName);
        var day = ToPositiveIntegerWithTruncation(obj[KeyStrings.GetOrCreate("day")], typeName);
        return RegulateToIso(calendarId, year, month, day, overflow);
    }

    // CalendarMergeFields + the calendar's date-from-fields for a non-Gregorian `.with()`: the
    // partial fields from the `bag` override the receiver's current calendar fields, then the merged
    // set is resolved to an ISO date. Per CalendarFieldKeysToIgnore the year/era/eraYear fields form
    // one mutually-overriding group and month/monthCode another, so supplying any member of a group
    // drops the receiver's whole group (e.g. a new `year` re-resolves the receiver's monthCode against
    // it, and a new `monthCode` ignores the receiver's month). Consistency between supplied fields
    // (year vs era/eraYear, month vs monthCode) is validated by the shared ToIsoFromBag resolution.
    internal static (int y, int m, int d) WithToIso(
        JSObject bag, string calendarId, string overflow, string typeName, int isoYear, int isoMonth, int isoDay)
    {
        var (curY, curM, curD) = CalendarYmd(calendarId, isoYear, isoMonth, isoDay);

        var bagYear = bag[KeyStrings.GetOrCreate("year")];
        var bagEra = bag[KeyStrings.GetOrCreate("era")];
        var bagEraYear = bag[KeyStrings.GetOrCreate("eraYear")];
        var bagMonth = bag[KeyStrings.GetOrCreate("month")];
        var bagMonthCode = bag[KeyStrings.GetOrCreate("monthCode")];
        var bagDay = bag[KeyStrings.GetOrCreate("day")];

        var hasYearGroup = !bagYear.IsUndefined || !bagEra.IsUndefined || !bagEraYear.IsUndefined;
        var hasMonthGroup = !bagMonth.IsUndefined || !bagMonthCode.IsUndefined;
        if (!hasYearGroup && !hasMonthGroup && bagDay.IsUndefined)
            throw JSEngine.NewTypeError($"{typeName}.with requires at least one date property");

        var merged = new JSObject();
        if (hasYearGroup)
        {
            if (!bagYear.IsUndefined) merged[KeyStrings.GetOrCreate("year")] = bagYear;
            if (!bagEra.IsUndefined) merged[KeyStrings.GetOrCreate("era")] = bagEra;
            if (!bagEraYear.IsUndefined) merged[KeyStrings.GetOrCreate("eraYear")] = bagEraYear;
        }
        else merged[KeyStrings.GetOrCreate("year")] = new JSNumber(curY);

        if (hasMonthGroup)
        {
            if (!bagMonth.IsUndefined) merged[KeyStrings.GetOrCreate("month")] = bagMonth;
            if (!bagMonthCode.IsUndefined) merged[KeyStrings.GetOrCreate("monthCode")] = bagMonthCode;
        }
        else merged[KeyStrings.GetOrCreate("monthCode")] = new JSString(TemporalCalendarMath.MonthCode(calendarId, curY, curM));

        merged[KeyStrings.GetOrCreate("day")] = bagDay.IsUndefined ? new JSNumber(curD) : bagDay;

        return ToIsoFromBag(merged, calendarId, overflow, typeName);
    }

    // Resolves a Temporal.PlainYearMonth property bag (year + month / monthCode, no day) for a
    // non-Gregorian calendar to the stored ISO date of day 1 of that calendar month.
    internal static (int y, int m, int d) YearMonthToIso(JSObject obj, string calendarId, string overflow, string typeName)
    {
        var (year, month) = ResolveYearMonth(obj, calendarId, typeName);
        return RegulateToIso(calendarId, year, month, 1, overflow);
    }

    // Resolves the (calendar year, calendar month-ordinal) from a property bag: the year comes from
    // `year` and/or { era, eraYear } (era-less for the lunisolar calendars), the month from `month`
    // and/or `monthCode` (with an optional leap "L" suffix).
    private static (int year, int month) ResolveYearMonth(JSObject obj, string calendarId, string typeName)
    {
        var yearValue = obj[KeyStrings.GetOrCreate("year")];
        var eraValue = obj[KeyStrings.GetOrCreate("era")];
        var eraYearValue = obj[KeyStrings.GetOrCreate("eraYear")];
        var monthValue = obj[KeyStrings.GetOrCreate("month")];
        var monthCodeValue = obj[KeyStrings.GetOrCreate("monthCode")];

        var hasYear = !yearValue.IsUndefined;
        var hasEra = !eraValue.IsUndefined;
        var hasEraYear = !eraYearValue.IsUndefined;
        var hasEraSupport = TemporalCalendarMath.HasEra(calendarId);

        if (!hasYear && !(hasEraSupport && hasEra && hasEraYear))
            throw JSEngine.NewTypeError($"{typeName}: missing year (or era and eraYear)");
        if (monthValue.IsUndefined && monthCodeValue.IsUndefined)
            throw JSEngine.NewTypeError($"{typeName}: missing month / monthCode");

        int year;
        if (hasEraSupport)
        {
            if (hasEra != hasEraYear)
                throw JSEngine.NewTypeError("Temporal: era and eraYear must be provided together");
            int? fromYear = hasYear ? ToIntegerWithTruncation(yearValue) : null;
            int? fromEra = hasEra
                ? TemporalCalendarMath.YearFromEra(calendarId, eraValue.StringValue, ToIntegerWithTruncation(eraYearValue))
                : null;
            if (fromYear != null && fromEra != null && fromYear.Value != fromEra.Value)
                throw JSEngine.NewRangeError("Temporal: year and era/eraYear do not agree");
            year = fromYear ?? fromEra.Value;
        }
        else
        {
            year = ToIntegerWithTruncation(yearValue); // era-less (lunisolar) calendars use `year`
        }

        int month;
        if (monthCodeValue.IsUndefined)
            month = ToPositiveIntegerWithTruncation(monthValue, typeName);
        else
        {
            var (codeNumber, leapMonth) = ParseMonthCode(monthCodeValue.ToString());
            month = TemporalCalendarMath.OrdinalFromMonthCode(calendarId, year, codeNumber, leapMonth);
        }
        if (!monthValue.IsUndefined && !monthCodeValue.IsUndefined && ToPositiveIntegerWithTruncation(monthValue, typeName) != month)
            throw JSEngine.NewRangeError($"{typeName}: month and monthCode disagree");

        return (year, month);
    }

    // Constrains/validates a (year, month, day) in a non-Gregorian calendar and returns it as ISO.
    internal static (int y, int m, int d) RegulateToIso(string calendarId, int year, int month, int day, string overflow)
    {
        var monthsInYear = TemporalCalendarMath.MonthsInYear(calendarId, year);
        if (overflow == "reject")
        {
            if (month < 1 || month > monthsInYear || day < 1 || day > TemporalCalendarMath.DaysInMonth(calendarId, year, month))
                throw JSEngine.NewRangeError("Temporal: date is out of range");
        }
        else
        {
            month = Math.Clamp(month, 1, monthsInYear);
            day = Math.Clamp(day, 1, TemporalCalendarMath.DaysInMonth(calendarId, year, month));
        }

        var epoch = TemporalCalendarMath.EpochDaysFromYmd(calendarId, year, month, day);
        if (epoch < MinEpochDays || epoch > MaxEpochDays)
            throw JSEngine.NewRangeError("Temporal: date is out of range");

        var (iy, im, id) = CivilFromDays(epoch);
        return ((int)iy, (int)im, (int)id);
    }

    // Steps `monthsToAdd` whole months from (year, month) honouring each year's own month count
    // (12 or 13), which varies year-to-year for the lunisolar and Hebrew calendars.
    private static (int year, int month) AddCalendarMonths(string calendarId, int year, long month, long monthsToAdd)
    {
        month += monthsToAdd;
        while (month > TemporalCalendarMath.MonthsInYear(calendarId, year))
            month -= TemporalCalendarMath.MonthsInYear(calendarId, year++);
        while (month < 1)
            month += TemporalCalendarMath.MonthsInYear(calendarId, --year);
        return (year, (int)month);
    }

    // Adds whole years then whole months in calendar space, clamping the month into the resulting
    // year and the day into the resulting month.
    private static (int y, int m, int d) AddCalendarYearsMonths(string calendarId, int year, int month, int day, long years, long months)
    {
        var y = (int)(year + years);
        var m = Math.Min(month, TemporalCalendarMath.MonthsInYear(calendarId, y));
        (y, m) = AddCalendarMonths(calendarId, y, m, months);
        var d = Math.Min(day, TemporalCalendarMath.DaysInMonth(calendarId, y, m));
        return (y, m, d);
    }

    // Adds a duration in a non-Gregorian calendar: years + months in calendar space (with the day
    // constrained to the resulting month), then weeks + days on the epoch-day axis. Inputs are the
    // active-calendar (year, month, day); the result is an ISO date.
    internal static (int y, int m, int d) AddToIso(string calendarId, int year, int month, int day,
        long years, long months, long weeks, long days, string overflow)
    {
        var newYear = (int)(year + years);
        var newMonth = Math.Min(month, TemporalCalendarMath.MonthsInYear(calendarId, newYear));
        (newYear, newMonth) = AddCalendarMonths(calendarId, newYear, newMonth, months);

        var maxDay = TemporalCalendarMath.DaysInMonth(calendarId, newYear, newMonth);
        var regulatedDay = day;
        if (overflow == "reject")
        {
            if (day > maxDay)
                throw JSEngine.NewRangeError("Temporal: day out of range for resulting month");
        }
        else regulatedDay = Math.Min(day, maxDay);

        var epoch = TemporalCalendarMath.EpochDaysFromYmd(calendarId, newYear, newMonth, regulatedDay) + days + weeks * 7;
        if (epoch < MinEpochDays || epoch > MaxEpochDays)
            throw JSEngine.NewRangeError("Temporal: result is out of range");

        var (ry, rm, rd) = CivilFromDays(epoch);
        return ((int)ry, (int)rm, (int)rd);
    }

    // Calendar-space date difference: step whole years (only for largestUnit "year") then whole
    // months, then count the remaining days on the epoch axis. Inputs are active-calendar dates.
    internal static (double years, double months, double weeks, double days) Difference(
        string calendarId, int ay, int am, int ad, int by, int bm, int bd, string largestUnit)
    {
        var startEpoch = TemporalCalendarMath.EpochDaysFromYmd(calendarId, ay, am, ad);
        var endEpoch = TemporalCalendarMath.EpochDaysFromYmd(calendarId, by, bm, bd);

        if (largestUnit is "day" or "week")
        {
            var totalDays = endEpoch - startEpoch;
            if (largestUnit == "week")
                return (0, 0, totalDays / 7, totalDays % 7);
            return (0, 0, 0, totalDays);
        }

        var sign = CompareDate(ay, am, ad, by, bm, bd); // -1 if a<b, +1 if a>b
        if (sign == 0) return (0, 0, 0, 0);
        var step = -sign;

        long years = 0;
        var (my, mm, md) = (ay, am, ad);
        if (largestUnit == "year")
        {
            years = by - ay;
            (my, mm, md) = AddCalendarYearsMonths(calendarId, ay, am, ad, years, 0);
            if (CompareDate(my, mm, md, by, bm, bd) == step)
            {
                years -= step;
                (my, mm, md) = AddCalendarYearsMonths(calendarId, ay, am, ad, years, 0);
            }
        }

        long months = 0;
        while (true)
        {
            var (ny, nm, nd) = AddCalendarYearsMonths(calendarId, my, mm, md, 0, step);
            var cmp = CompareDate(ny, nm, nd, by, bm, bd);
            if (cmp == step) break;
            months += step; my = ny; mm = nm; md = nd;
            if (cmp == 0) break;
        }

        var daysOut = TemporalCalendarMath.EpochDaysFromYmd(calendarId, by, bm, bd)
                    - TemporalCalendarMath.EpochDaysFromYmd(calendarId, my, mm, md);

        return (years, months, 0, daysOut);
    }

    // ── shared value coercion ─────────────────────────────────────────────────────

    private static int ToIntegerWithTruncation(JSValue value)
    {
        if (value == null || value.IsUndefined) return 0;
        var number = value.DoubleValue; // ToNumber (throws TypeError for BigInt/Symbol)
        if (double.IsNaN(number) || double.IsInfinity(number))
            throw JSEngine.NewRangeError("Temporal: date component must be finite");
        return (int)Math.Truncate(number);
    }

    // ToPositiveIntegerWithTruncation: month / day fields must be a positive (≥ 1) integer.
    private static int ToPositiveIntegerWithTruncation(JSValue value, string typeName)
    {
        var n = ToIntegerWithTruncation(value);
        if (n < 1)
            throw JSEngine.NewRangeError($"{typeName}: month and day must be positive");
        return n;
    }

    // Parses "M01".."M13" with an optional trailing "L" leap-month marker.
    private static (int number, bool leap) ParseMonthCode(string code)
    {
        var match = Regex.Match(code, @"^M(\d{2})(L?)$");
        if (!match.Success)
            throw JSEngine.NewRangeError($"Temporal: invalid monthCode \"{code}\"");
        return (int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), match.Groups[2].Value == "L");
    }

    private static int CompareDate(int y1, int m1, int d1, int y2, int m2, int d2)
    {
        if (y1 != y2) return y1 < y2 ? -1 : 1;
        if (m1 != m2) return m1 < m2 ? -1 : 1;
        if (d1 != d2) return d1 < d2 ? -1 : 1;
        return 0;
    }

    // Howard Hinnant's days_from_civil / civil_from_days about the ISO 1970-01-01 epoch.
    internal static long DaysFromCivil(long y, long m, long d)
    {
        y -= m <= 2 ? 1 : 0;
        var era = (y >= 0 ? y : y - 399) / 400;
        var yoe = y - era * 400;
        var doy = (153 * (m > 2 ? m - 3 : m + 9) + 2) / 5 + d - 1;
        var doe = yoe * 365 + yoe / 4 - yoe / 100 + doy;
        return era * 146097 + doe - 719468;
    }

    internal static (long y, long m, long d) CivilFromDays(long z)
    {
        z += 719468;
        var era = (z >= 0 ? z : z - 146096) / 146097;
        var doe = z - era * 146097;
        var yoe = (doe - doe / 1460 + doe / 36524 - doe / 146096) / 365;
        var y = yoe + era * 400;
        var doy = doe - (365 * yoe + yoe / 4 - yoe / 100);
        var mp = (5 * doy + 2) / 153;
        var d = doy - (153 * mp + 2) / 5 + 1;
        var m = mp < 10 ? mp + 3 : mp - 9;
        return (m <= 2 ? y + 1 : y, m, d);
    }
}
