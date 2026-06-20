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

        var (year, month) = ResolveYearMonth(obj, calendarId, overflow, typeName);
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
        var (year, month) = ResolveYearMonth(obj, calendarId, overflow, typeName);
        // A PlainYearMonth is bounded by ISOYearMonthWithinLimits (year/month), validated by the
        // caller; its day-1 reference may sit a few days past the ISO date boundary.
        return RegulateToIso(calendarId, year, month, 1, overflow, enforceIsoDateRange: false);
    }

    // ── Temporal.PlainMonthDay reference-date resolution (non-Gregorian calendars) ──
    // A PlainMonthDay stores an ISO reference date whose calendar projection has the requested
    // (monthCode, day). The reference year mirrors the ISO 1972: the *latest* calendar year on or
    // before 1972 in which the month-day is representable, or — when the month code never occurs in
    // that range (e.g. a leap month whose first occurrence is in the future) — the *earliest* year
    // after 1972. NOTE this matches the test262 reference years across the .NET-backed chinese span
    // for the common cases; a handful of rare lunisolar leap edge cases (where ICU4X uses calendar
    // data we do not reproduce — e.g. the disputed 2033 leap month) are not guaranteed to match.
    private const int MonthDayReferenceYear = 1972;
    // The reference search is bounded to the span over which the lunisolar back ends agree with ICU
    // (1900 onward; before that ICU's chinese/dangi data is not "accurately calculable" and a rare
    // leap month is taken from its first *future* occurrence instead). The arithmetic calendars are
    // deterministic, so the bound only ever excludes far-past lunisolar leap months.
    private const int MonthDaySearchLowYear = 1900;
    private const int MonthDaySearchHighYear = 2100;

    // The reference search loop iterates *calendar* years, but the span above is expressed in ISO
    // years. For a calendar whose year numbering differs from the ISO year (the arithmetic islamic /
    // hebrew / coptic / ethiopic families), iterating 1900..2100 as calendar years would search the
    // wrong ISO window entirely (e.g. islamic year 1900 ≈ ISO 2461), so project the ISO endpoints onto
    // the calendar's own years. A one-year margin absorbs the new-year offset at each endpoint.
    private static (int low, int high) MonthDaySearchYears(string calendarId)
        => (CalendarYmd(calendarId, MonthDaySearchLowYear, 1, 1).y - 1,
            CalendarYmd(calendarId, MonthDaySearchHighYear, 12, 31).y + 1);

    // The ordinal of (codeNumber, isLeap) in calendar year `y`, or -1 when that month code does not
    // occur that year (absent leap month, an out-of-range number such as "M15", or a year outside the
    // back end's supported span).
    private static int MonthDayOrdinal(string calendarId, int y, int codeNumber, bool isLeap)
    {
        try
        {
            var ord = TemporalCalendarMath.OrdinalFromMonthCode(calendarId, y, codeNumber, isLeap);
            return ord < 1 || ord > TemporalCalendarMath.MonthsInYear(calendarId, y) ? -1 : ord;
        }
        catch (JSException) { return -1; }
        catch (ArgumentOutOfRangeException) { return -1; }
    }

    // The reference ISO date for (codeNumber, isLeap, targetDay): the date whose calendar projection
    // is that month-day and whose ISO value is the *latest on or before* ISO 1972-12-31, or — when no
    // occurrence falls in that range (e.g. a leap month whose first occurrence is in the future) — the
    // *earliest after* it. (The reference *year* is therefore the ISO year of that date, which can
    // differ from the calendar year — a late-calendar-month day rolls into the next ISO year.) Null
    // when the month-day never occurs in the searched span.
    private static (int y, int m, int d)? MonthDayExact(string calendarId, int codeNumber, bool isLeap, int targetDay)
    {
        var threshold = DaysFromCivil(MonthDayReferenceYear, 12, 31);
        var bestBelow = long.MinValue;
        var bestAbove = long.MaxValue;
        var (searchLow, searchHigh) = MonthDaySearchYears(calendarId);
        for (var y = searchLow; y <= searchHigh; y++)
        {
            var ord = MonthDayOrdinal(calendarId, y, codeNumber, isLeap);
            if (ord < 0 || targetDay > TemporalCalendarMath.DaysInMonth(calendarId, y, ord)) continue;
            var epoch = TemporalCalendarMath.EpochDaysFromYmd(calendarId, y, ord, targetDay);
            if (epoch <= threshold) { if (epoch > bestBelow) bestBelow = epoch; }
            else if (epoch < bestAbove) bestAbove = epoch;
        }

        var chosen = bestBelow != long.MinValue ? bestBelow : (bestAbove != long.MaxValue ? bestAbove : (long?)null);
        if (chosen == null) return null;
        if (chosen < MinEpochDays || chosen > MaxEpochDays)
            throw JSEngine.NewRangeError("Temporal.PlainMonthDay: month-day is out of range");
        var (iy, im, id) = CivilFromDays(chosen.Value);
        return ((int)iy, (int)im, (int)id);
    }

    // The greatest month length (codeNumber, isLeap) ever attains across the search range; 0 when
    // the month code never occurs.
    private static int MonthDayMaxLength(string calendarId, int codeNumber, bool isLeap)
    {
        var max = 0;
        var (searchLow, searchHigh) = MonthDaySearchYears(calendarId);
        for (var y = searchLow; y <= searchHigh; y++)
        {
            var ord = MonthDayOrdinal(calendarId, y, codeNumber, isLeap);
            if (ord >= 0) max = Math.Max(max, TemporalCalendarMath.DaysInMonth(calendarId, y, ord));
        }
        return max;
    }

    // Resolves a (monthCode, day) to its ISO reference date in a non-Gregorian calendar. Under
    // "constrain" a day past the month's length is clamped to the longest the month code reaches,
    // and a leap month code whose length cannot reach the day falls back to its regular month
    // (dropping the "L"); under "reject" an unrepresentable month-day is a RangeError.
    internal static (int y, int m, int d) MonthDayFromCode(string calendarId, string monthCode, int day, string overflow)
    {
        var (codeNumber, isLeap) = ParseMonthCode(monthCode);
        return MonthDayCore(calendarId, codeNumber, isLeap, day, overflow);
    }

    // Resolves a (year, month-ordinal, day) bag to a PlainMonthDay reference date: the calendar
    // (year, month, day) is regulated, projected to its month code, then the year is dropped.
    internal static (int y, int m, int d) MonthDayFromYearMonth(string calendarId, int year, int month, int day, string overflow)
    {
        var (iy, im, id) = RegulateToIso(calendarId, year, month, day, overflow);
        var (cy, cm, cd) = CalendarYmd(calendarId, iy, im, id);
        var (codeNumber, isLeap) = ParseMonthCode(TemporalCalendarMath.MonthCode(calendarId, cy, cm));
        return MonthDayCore(calendarId, codeNumber, isLeap, cd, overflow);
    }

    // Resolves a Temporal.PlainMonthDay property bag for a non-Gregorian calendar to an ISO
    // reference date. A monthCode without a year resolves directly through the reference search; a
    // year (or era/eraYear) present resolves the calendar (year, month/monthCode, day) first and
    // then drops the year.
    internal static (int y, int m, int d) MonthDayFromBag(JSObject obj, string calendarId, string overflow, string typeName)
    {
        if (obj[KeyStrings.GetOrCreate("day")].IsUndefined)
            throw JSEngine.NewTypeError($"{typeName}: missing day");
        var day = ToPositiveIntegerWithTruncation(obj[KeyStrings.GetOrCreate("day")], typeName);

        var yearValue = obj[KeyStrings.GetOrCreate("year")];
        var eraValue = obj[KeyStrings.GetOrCreate("era")];
        var eraYearValue = obj[KeyStrings.GetOrCreate("eraYear")];
        var monthValue = obj[KeyStrings.GetOrCreate("month")];
        var monthCodeValue = obj[KeyStrings.GetOrCreate("monthCode")];

        var hasYear = !yearValue.IsUndefined
            || (TemporalCalendarMath.HasEra(calendarId) && !eraValue.IsUndefined && !eraYearValue.IsUndefined);

        if (monthValue.IsUndefined && monthCodeValue.IsUndefined)
            throw JSEngine.NewTypeError($"{typeName}: missing month / monthCode");
        // A numeric `month` is calendar/year-dependent in these calendars, so it is only meaningful
        // with a `year`; supplying `month` without a year (even alongside a `monthCode`) is a
        // TypeError — a required-field check that precedes any month/monthCode conflict or
        // out-of-range value, which are RangeErrors.
        if (!hasYear && !monthValue.IsUndefined)
            throw JSEngine.NewTypeError($"{typeName}: month requires a year");

        if (hasYear)
        {
            var (year, month) = ResolveYearMonth(obj, calendarId, overflow, typeName);
            return MonthDayFromYearMonth(calendarId, year, month, day, overflow);
        }

        return MonthDayFromCode(calendarId, monthCodeValue.ToString(), day, overflow);
    }

    private static (int y, int m, int d) MonthDayCore(string calendarId, int codeNumber, bool isLeap, int day, string overflow)
    {
        if (day < 1)
            throw JSEngine.NewRangeError("Temporal.PlainMonthDay: day must be positive");

        var exact = MonthDayExact(calendarId, codeNumber, isLeap, day);
        if (exact != null) return exact.Value;

        if (overflow == "reject")
            throw JSEngine.NewRangeError("Temporal.PlainMonthDay: month-day is out of range");

        if (!isLeap)
        {
            var rmax = MonthDayMaxLength(calendarId, codeNumber, false);
            if (rmax == 0) throw JSEngine.NewRangeError("Temporal.PlainMonthDay: month-day is out of range");
            return MonthDayExact(calendarId, codeNumber, false, Math.Min(day, rmax))
                   ?? throw JSEngine.NewRangeError("Temporal.PlainMonthDay: month-day is out of range");
        }

        // Leap month constrain: keep the leap month while it can hold the (clamped) day, otherwise
        // fall back to the regular month with the same number.
        var lmax = MonthDayMaxLength(calendarId, codeNumber, true);
        var regMax = MonthDayMaxLength(calendarId, codeNumber, false);
        if (regMax == 0)
            throw JSEngine.NewRangeError("Temporal.PlainMonthDay: month-day is out of range");

        // A leap month code naming a leap month a *fixed-leap-month* calendar never has is invalid
        // regardless of overflow — it must NOT silently fall back to the regular month. Hebrew has
        // only the leap month "M05L" (Adar I), so "M01L".."M12L" (≠ M05L) never exist and are a
        // RangeError (test262 intl402/.../PlainMonthDay/from/invalid-month-codes-hebrew). The
        // lunisolar chinese/dangi calendars place their leap month on a year-dependent position — any
        // "MnnL" is structurally possible (some rare ones merely fall outside our search range) — so
        // they keep the constrain fallback below rather than being wrongly rejected.
        if (lmax == 0 && calendarId is not ("chinese" or "dangi"))
            throw JSEngine.NewRangeError("Temporal.PlainMonthDay: month-day is out of range");

        var clamped = Math.Min(day, Math.Max(lmax, regMax));
        if (lmax > 0 && clamped <= lmax)
        {
            var asLeap = MonthDayExact(calendarId, codeNumber, true, clamped);
            if (asLeap != null) return asLeap.Value;
        }
        return MonthDayExact(calendarId, codeNumber, false, Math.Min(clamped, regMax))
               ?? throw JSEngine.NewRangeError("Temporal.PlainMonthDay: month-day is out of range");
    }

    // The (monthCode, day) of a stored ISO reference date in a non-Gregorian calendar.
    internal static (string monthCode, int day) MonthDayFields(string calendarId, int isoYear, int isoMonth, int isoDay)
    {
        var (cy, cm, cd) = CalendarYmd(calendarId, isoYear, isoMonth, isoDay);
        return (TemporalCalendarMath.MonthCode(calendarId, cy, cm), cd);
    }

    // Temporal.PlainMonthDay.prototype.toPlainDate for a non-Gregorian calendar: combine the stored
    // month code + day with the supplied year (constraining the day into that calendar month).
    internal static (int y, int m, int d) MonthDayToPlainDate(string calendarId, int year, string monthCode, int day)
    {
        var (codeNumber, isLeap) = ParseMonthCode(monthCode);
        var ordinal = TemporalCalendarMath.OrdinalFromMonthCode(calendarId, year, codeNumber, isLeap);
        return RegulateToIso(calendarId, year, ordinal, day, "constrain");
    }

    // Resolves the (calendar year, calendar month-ordinal) from a property bag: the year comes from
    // `year` and/or { era, eraYear } (era-less for the lunisolar calendars), the month from `month`
    // and/or `monthCode` (with an optional leap "L" suffix).
    private static (int year, int month) ResolveYearMonth(JSObject obj, string calendarId, string overflow, string typeName)
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
            // A leap month code ("MnnL") exists only in a leap year that carries that particular leap
            // month. Under overflow "constrain" a year without it falls back to the regular month with
            // the same number ("Mnn") — except hebrew, whose leap month Adar I ("M05L") sits before the
            // regular Adar ("M06"), so it collapses onto M06. Under "reject" (and for a non-leap code)
            // OrdinalFromMonthCode throws when the month code is absent. (Mirrors the year-shift path
            // in ResolveMonthAfterYearShift.) This constrain only applies to calendars that actually
            // have leap months; for a solar calendar (e.g. persian) a leap code is always invalid.
            if (leapMonth && overflow != "reject" && TemporalCalendarMath.HasLeapMonths(calendarId))
            {
                try { month = TemporalCalendarMath.OrdinalFromMonthCode(calendarId, year, codeNumber, true); }
                catch (JSException)
                {
                    var fallbackNum = (calendarId == "hebrew" && codeNumber == 5) ? 6 : codeNumber;
                    month = TemporalCalendarMath.OrdinalFromMonthCode(calendarId, year, fallbackNum, false);
                }
            }
            else
                month = TemporalCalendarMath.OrdinalFromMonthCode(calendarId, year, codeNumber, leapMonth);
        }
        if (!monthValue.IsUndefined && !monthCodeValue.IsUndefined && ToPositiveIntegerWithTruncation(monthValue, typeName) != month)
            throw JSEngine.NewRangeError($"{typeName}: month and monthCode disagree");

        return (year, month);
    }

    // Constrains/validates a (year, month, day) in a non-Gregorian calendar and returns it as ISO.
    // `enforceIsoDateRange` rejects a result outside the representable ISO *date* range
    // (the limit for a PlainDate, whose day matters). A PlainYearMonth is bounded instead
    // by ISOYearMonthWithinLimits — year/month only — so its reference day-1 conversion may
    // legitimately land a few days past the boundary date (e.g. the first day of a calendar's
    // maximum month falling in +275760-09 after the 13th); such callers pass false and let the
    // year-month limit decide.
    internal static (int y, int m, int d) RegulateToIso(string calendarId, int year, int month, int day, string overflow, bool enforceIsoDateRange = true)
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
        if (enforceIsoDateRange && (epoch < MinEpochDays || epoch > MaxEpochDays))
            throw JSEngine.NewRangeError("Temporal: date is out of range");

        var (iy, im, id) = CivilFromDays(epoch);
        return ((int)iy, (int)im, (int)id);
    }

    // After shifting the year by a whole number of years, the spec preserves the *month code* (not
    // the bare ordinal): a leap month (e.g. chinese "M03L") re-resolves to the same leap month in the
    // target year, or — when that year has no such leap month — constrains to the matching common
    // month ("M03") under overflow "constrain" and is a RangeError under "reject". A common month
    // re-resolves by code too, so its ordinal shifts across a leap month present in only one of the
    // years. Calendars without leap months map code↔ordinal identically.
    private static int ResolveMonthAfterYearShift(string calendarId, int year, int month, int targetYear, string overflow)
    {
        if (year == targetYear)
            return month;

        var (num, leap) = ParseMonthCode(TemporalCalendarMath.MonthCode(calendarId, year, month));
        if (!leap)
        {
            var ord = TemporalCalendarMath.OrdinalFromMonthCode(calendarId, targetYear, num, false);
            return Math.Min(ord, TemporalCalendarMath.MonthsInYear(calendarId, targetYear));
        }

        if (overflow == "reject")
            return TemporalCalendarMath.OrdinalFromMonthCode(calendarId, targetYear, num, true); // throws if absent
        try { return TemporalCalendarMath.OrdinalFromMonthCode(calendarId, targetYear, num, true); }
        catch (JSException)
        {
            // The leap month does not exist in the target year. The lunisolar calendars constrain a
            // leap month MnnL back onto the regular month with the same number (Mnn). The Hebrew
            // calendar is different: its leap month is Adar I (M05L), which sits *before* the regular
            // Adar (M06); when a year has only one Adar both collapse onto M06, so M05L constrains to
            // M06 rather than to M05 (Shevat).
            var fallbackNum = (calendarId == "hebrew" && num == 5) ? 6 : num;
            return TemporalCalendarMath.OrdinalFromMonthCode(calendarId, targetYear, fallbackNum, false);
        }
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
        var newMonth = ResolveMonthAfterYearShift(calendarId, year, month, newYear, overflow);
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

        var step = -CompareDate(ay, am, ad, by, bm, bd); // +1 when end is after start
        if (step == 0) return (0, 0, 0, 0);

        // The year/month counting anchors on the *unconstrained* start day (`ad`): a whole month is
        // only counted when the start day-of-month does not have to be clamped into the target month
        // (so e.g. the 30th of a 30-day month "surpasses" the 29th of the following 29-day month and
        // does not count as a full month). The residual days are then measured from the day-clamped
        // intermediate. Mirrors the reference calendar untilCalendar.
        //
        // The whole-year step projects the start *month* into the candidate year preserving its
        // monthCode (ResolveMonthAfterYearShift), not its raw ordinal: across a leap-month boundary
        // the lunisolar/Hebrew calendars give the same monthCode a different ordinal from year to
        // year, so anchoring on the bare ordinal `am` would miscount a whole-year difference by a
        // spurious ±1 month.
        long years = 0;
        int cy, cm;
        if (largestUnit == "year")
        {
            // The whole-year count compares the month *codes* (not bare ordinals): a leap month
            // (e.g. chinese "M04L") is ordered just after its regular month ("M04") and before the
            // next ("M05"), and the same code maps to a different ordinal from year to year across a
            // leap-month boundary. When the target's month-day lies before the start's *within the
            // year* (its code — or, for equal codes, its day — is smaller in the step direction) one
            // fewer whole year has elapsed. Mirrors the reference calendar's untilCalendar.
            long diffYears = by - ay;
            var oneCode = TemporalCalendarMath.MonthCode(calendarId, ay, am);
            var twoCode = TemporalCalendarMath.MonthCode(calendarId, by, bm);
            var codeCmp = string.CompareOrdinal(twoCode, oneCode);
            var diffInYearSign = codeCmp != 0 ? Math.Sign(codeCmp) : Math.Sign(bd - ad);
            years = diffInYearSign * step < 0 ? diffYears - step : diffYears;

            cy = ay + (int)years;
            cm = ResolveMonthAfterYearShift(calendarId, ay, am, cy, "constrain");

            // A day-constrained whole-year step may still surpass the target (e.g. a month-end day
            // landing in a shorter month); roll back one more year.
            if (CompareDate(cy, cm, ad, by, bm, bd) == step)
            {
                years -= step;
                cy = ay + (int)years;
                cm = ResolveMonthAfterYearShift(calendarId, ay, am, cy, "constrain");
            }
        }
        else
        {
            cy = ay; cm = am;
        }

        long months = 0;
        while (true)
        {
            var (ny, nm) = AddCalendarMonths(calendarId, cy, cm, step);
            var cmp = CompareDate(ny, nm, ad, by, bm, bd);
            if (cmp == step) break; // adding one more month would surpass the end
            months += step; cy = ny; cm = nm;
            if (cmp == 0) break;
        }

        var curDay = Math.Min(ad, TemporalCalendarMath.DaysInMonth(calendarId, cy, cm));
        var daysOut = TemporalCalendarMath.EpochDaysFromYmd(calendarId, by, bm, bd)
                    - TemporalCalendarMath.EpochDaysFromYmd(calendarId, cy, cm, curDay);

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
