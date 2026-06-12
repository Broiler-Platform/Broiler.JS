using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// Temporal.PlainDate (Temporal proposal §3): a calendar date with no time or zone. Only the
// ISO 8601 calendar is supported — its arithmetic (leap years, month lengths, date add /
// difference) is pure, data-free math, so the full date surface is implementable. The
// CLDR submodule has no calendar arithmetic (only day-period formatting strings), and the
// non-ISO calendars / localized output it could feed are out of scope.
//
// Registered under the Temporal namespace (not as a global) via Register = false.
[JSClassGenerator("PlainDate", Register = false)]
public partial class JSTemporalPlainDate : JSObject
{
    // ISODateTimeWithinLimits bounds: -271821-04-19 … +275760-09-13 (±~10^8 days).
    private static readonly long MinEpochDays = DaysFromCivil(-271821, 4, 19);
    private static readonly long MaxEpochDays = DaysFromCivil(275760, 9, 13);

    internal readonly int isoYear, isoMonth, isoDay;

    [JSExport(Length = 3)]
    public JSTemporalPlainDate(in Arguments a) : base(ResolvePrototype())
    {
        isoYear = ToIntegerWithTruncation(a.GetAt(0));
        isoMonth = ToIntegerWithTruncation(a.GetAt(1));
        isoDay = ToIntegerWithTruncation(a.GetAt(2));
        RequireCalendar(a.GetAt(3));

        if (!IsValidISODate(isoYear, isoMonth, isoDay))
            throw JSEngine.NewRangeError("Temporal.PlainDate: invalid ISO date");
    }

    internal JSTemporalPlainDate(int isoYear, int isoMonth, int isoDay, JSObject prototype) : base(prototype)
    {
        this.isoYear = isoYear;
        this.isoMonth = isoMonth;
        this.isoDay = isoDay;
    }

    private static JSObject ResolvePrototype()
    {
        if (JSEngine.NewTarget == null && (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
            throw JSEngine.NewTypeError("Constructor Temporal.PlainDate requires 'new'");

        return JSEngine.NewTargetPrototype ?? PlainDatePrototype;
    }

    internal static JSObject PlainDatePrototype
    {
        get
        {
            var temporal = (JSEngine.Current as JSObject)?[KeyStrings.GetOrCreate("Temporal")] as JSObject;
            return (temporal?[KeyStrings.GetOrCreate("PlainDate")] as JSFunction)?.prototype;
        }
    }

    // ── accessors ───────────────────────────────────────────────────────────────

    [JSExport("calendarId")] public JSValue CalendarId => new JSString("iso8601");
    [JSExport("year")] public double YearValue => isoYear;
    [JSExport("month")] public double MonthValue => isoMonth;
    [JSExport("monthCode")] public JSValue MonthCode => new JSString($"M{isoMonth:00}");
    [JSExport("day")] public double DayValue => isoDay;

    [JSExport("dayOfWeek")] public double DayOfWeek => IsoDayOfWeek(isoYear, isoMonth, isoDay);
    [JSExport("dayOfYear")] public double DayOfYear => (int)(DaysFromCivil(isoYear, isoMonth, isoDay) - DaysFromCivil(isoYear, 1, 1)) + 1;
    [JSExport("daysInWeek")] public double DaysInWeek => 7;
    [JSExport("daysInMonth")] public double DaysInMonth => DaysInMonthOf(isoYear, isoMonth);
    [JSExport("daysInYear")] public double DaysInYear => IsLeapYear(isoYear) ? 366 : 365;
    [JSExport("monthsInYear")] public double MonthsInYear => 12;
    [JSExport("inLeapYear")] public bool InLeapYear => IsLeapYear(isoYear);

    [JSExport("weekOfYear")] public double WeekOfYear => IsoWeek(isoYear, isoMonth, isoDay).week;
    [JSExport("yearOfWeek")] public double YearOfWeek => IsoWeek(isoYear, isoMonth, isoDay).year;

    // ── statics ─────────────────────────────────────────────────────────────────

    [JSExport("from", Length = 1)]
    internal static JSValue From(in Arguments a)
    {
        var item = a.GetAt(0);
        var overflow = ReadOverflow(a.GetAt(1));

        if (item is JSTemporalPlainDate d)
            return new JSTemporalPlainDate(d.isoYear, d.isoMonth, d.isoDay, PlainDatePrototype);

        return ToTemporalDate(item, overflow);
    }

    [JSExport("compare", Length = 2)]
    internal static JSValue Compare(in Arguments a)
    {
        var one = RequireDate(ToTemporalDate(a.GetAt(0), "constrain"));
        var two = RequireDate(ToTemporalDate(a.GetAt(1), "constrain"));
        return new JSNumber(CompareISODate(one.isoYear, one.isoMonth, one.isoDay, two.isoYear, two.isoMonth, two.isoDay));
    }

    // ── methods ─────────────────────────────────────────────────────────────────

    [JSExport("with", Length = 1)]
    public JSValue With(in Arguments a)
    {
        if (a.GetAt(0) is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.PlainDate.prototype.with requires an object");

        // A calendar field is not allowed in a with() bag.
        if (!obj[KeyStrings.GetOrCreate("calendar")].IsUndefined)
            throw JSEngine.NewTypeError("Temporal.PlainDate.prototype.with does not accept a calendar field");

        var overflow = ReadOverflow(a.GetAt(1));

        var any = false;
        var year = isoYear;
        var month = isoMonth;
        var day = isoDay;

        var yearValue = obj[KeyStrings.GetOrCreate("year")];
        if (!yearValue.IsUndefined) { year = ToIntegerWithTruncation(yearValue); any = true; }

        var monthCodeValue = obj[KeyStrings.GetOrCreate("monthCode")];
        var monthValue = obj[KeyStrings.GetOrCreate("month")];
        if (!monthCodeValue.IsUndefined) { month = MonthFromCode(monthCodeValue.ToString()); any = true; }
        if (!monthValue.IsUndefined)
        {
            var m = ToIntegerWithTruncation(monthValue);
            if (!monthCodeValue.IsUndefined && m != month)
                throw JSEngine.NewRangeError("Temporal.PlainDate.with: month and monthCode disagree");
            month = m;
            any = true;
        }

        var dayValue = obj[KeyStrings.GetOrCreate("day")];
        if (!dayValue.IsUndefined) { day = ToIntegerWithTruncation(dayValue); any = true; }

        if (!any)
            throw JSEngine.NewTypeError("Temporal.PlainDate.prototype.with requires at least one date property");

        return RegulateISODate(year, month, day, overflow);
    }

    [JSExport("withCalendar", Length = 1)]
    public JSValue WithCalendar(in Arguments a)
    {
        RequireCalendar(a.GetAt(0));
        return new JSTemporalPlainDate(isoYear, isoMonth, isoDay, PlainDatePrototype);
    }

    [JSExport("add", Length = 1)]
    public JSValue Add(in Arguments a) => AddDuration(a.GetAt(0), a.GetAt(1), 1);

    [JSExport("subtract", Length = 1)]
    public JSValue Subtract(in Arguments a) => AddDuration(a.GetAt(0), a.GetAt(1), -1);

    private JSValue AddDuration(JSValue durationLike, JSValue options, int sign)
    {
        var overflow = ReadOverflow(options);
        var d = (JSTemporalDuration)JSTemporalDuration.ToTemporalDuration(durationLike);

        // A PlainDate balances the duration's time components down to whole (24 h) days,
        // then performs calendar date arithmetic.
        var timeNanoseconds =
            (long)d.HoursValue * 3_600_000_000_000 + (long)d.MinutesValue * 60_000_000_000
            + (long)d.SecondsValue * 1_000_000_000 + (long)d.MillisecondsValue * 1_000_000
            + (long)d.MicrosecondsValue * 1_000 + (long)d.NanosecondsValue;
        var extraDays = (long)(timeNanoseconds / 86_400_000_000_000);

        return AddISODate(isoYear, isoMonth, isoDay,
            sign * (long)d.YearsValue, sign * (long)d.MonthsValue,
            sign * (long)d.WeeksValue, sign * ((long)d.DaysValue + extraDays), overflow);
    }

    [JSExport("until", Length = 1)]
    public JSValue Until(in Arguments a) => Difference(a.GetAt(0), a.GetAt(1), 1);

    [JSExport("since", Length = 1)]
    public JSValue Since(in Arguments a) => Difference(a.GetAt(0), a.GetAt(1), -1);

    // TODO: until/since honor only the `largestUnit` option (day[default]/week/month/year);
    // smallestUnit/roundingIncrement/roundingMode rounding is not yet applied.
    private JSValue Difference(JSValue other, JSValue options, int sign)
    {
        var target = RequireDate(ToTemporalDate(other, "constrain"));
        var largestUnit = ReadLargestUnit(options, "day");

        // `since` is `until` with the operands swapped.
        var (ay, am, ad, by, bm, bd) = sign == 1
            ? (isoYear, isoMonth, isoDay, target.isoYear, target.isoMonth, target.isoDay)
            : (target.isoYear, target.isoMonth, target.isoDay, isoYear, isoMonth, isoDay);

        var (years, months, weeks, days) = DifferenceISODate(ay, am, ad, by, bm, bd, largestUnit);
        return new JSTemporalDuration(years, months, weeks, days, 0, 0, 0, 0, 0, 0, JSTemporalDuration.DurationPrototype);
    }

    [JSExport("equals", Length = 1)]
    public JSValue Equals(in Arguments a)
    {
        var other = RequireDate(ToTemporalDate(a.GetAt(0), "constrain"));
        return isoYear == other.isoYear && isoMonth == other.isoMonth && isoDay == other.isoDay
            ? JSValue.BooleanTrue : JSValue.BooleanFalse;
    }

    [JSExport("toString", Length = 0)]
    public JSValue ToStringMethod(in Arguments a) => new JSString(ToISOString());

    [JSExport("toJSON", Length = 0)]
    public JSValue ToJSON(in Arguments a) => new JSString(ToISOString());

    [JSExport("toLocaleString", Length = 0)]
    public JSValue ToLocaleString(in Arguments a) => new JSString(ToISOString());

    [JSExport("valueOf", Length = 0)]
    public JSValue ValueOf(in Arguments a)
        => throw JSEngine.NewTypeError("Called Temporal.PlainDate.prototype.valueOf, which is not supported. Use Temporal.PlainDate.compare for comparison.");

    // TODO: toPlainDateTime / toPlainYearMonth / toPlainMonthDay / toZonedDateTime depend on
    // Temporal.PlainDateTime / PlainYearMonth / PlainMonthDay / ZonedDateTime, still stubs.
    [JSExport("toPlainDateTime", Length = 0)]
    public JSValue ToPlainDateTime(in Arguments a)
        => throw JSEngine.NewError("Temporal.PlainDate.prototype.toPlainDateTime is not yet implemented (needs Temporal.PlainDateTime)");

    [JSExport("toPlainYearMonth", Length = 0)]
    public JSValue ToPlainYearMonth(in Arguments a)
        => throw JSEngine.NewError("Temporal.PlainDate.prototype.toPlainYearMonth is not yet implemented (needs Temporal.PlainYearMonth)");

    [JSExport("toPlainMonthDay", Length = 0)]
    public JSValue ToPlainMonthDay(in Arguments a)
        => throw JSEngine.NewError("Temporal.PlainDate.prototype.toPlainMonthDay is not yet implemented (needs Temporal.PlainMonthDay)");

    [JSExport("toZonedDateTime", Length = 1)]
    public JSValue ToZonedDateTime(in Arguments a)
        => throw JSEngine.NewError("Temporal.PlainDate.prototype.toZonedDateTime is not yet implemented (needs Temporal.ZonedDateTime)");

    // ── value coercion / validation ─────────────────────────────────────────────

    private static JSTemporalPlainDate RequireDate(JSValue value)
        => value as JSTemporalPlainDate ?? throw JSEngine.NewTypeError("expected a Temporal.PlainDate");

    private static int ToIntegerWithTruncation(JSValue value)
    {
        if (value == null || value.IsUndefined)
            return 0;

        var number = value.DoubleValue; // ToNumber (throws TypeError for BigInt/Symbol)
        if (double.IsNaN(number) || double.IsInfinity(number))
            throw JSEngine.NewRangeError("Temporal.PlainDate: date component must be finite");

        return (int)Math.Truncate(number);
    }

    // Only the ISO 8601 calendar is supported.
    private static void RequireCalendar(JSValue calendar)
    {
        if (calendar == null || calendar.IsUndefined)
            return;

        var id = calendar.ToString();
        if (!string.Equals(id, "iso8601", StringComparison.OrdinalIgnoreCase))
            throw JSEngine.NewRangeError($"Temporal.PlainDate: unsupported calendar \"{id}\" (only iso8601 is implemented)");
    }

    private static int MonthFromCode(string code)
    {
        var match = Regex.Match(code, @"^M(\d{2})$");
        if (!match.Success)
            throw JSEngine.NewRangeError($"Temporal: invalid monthCode \"{code}\"");

        return int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    private static string ReadOverflow(JSValue options)
    {
        if (options == null || options.IsUndefined)
            return "constrain";

        if (options is not JSObject optionsObject)
            throw JSEngine.NewTypeError("Temporal options must be an object or undefined");

        var v = optionsObject[KeyStrings.GetOrCreate("overflow")];
        if (v.IsUndefined) return "constrain";

        var overflow = v.ToString();
        if (overflow is not ("constrain" or "reject"))
            throw JSEngine.NewRangeError($"Temporal: invalid overflow \"{overflow}\"");

        return overflow;
    }

    private static string ReadLargestUnit(JSValue options, string defaultUnit)
    {
        if (options == null || options.IsUndefined)
            return defaultUnit;

        if (options is not JSObject optionsObject)
            throw JSEngine.NewTypeError("Temporal options must be an object or undefined");

        var v = optionsObject[KeyStrings.GetOrCreate("largestUnit")];
        if (v.IsUndefined || v.ToString() == "auto")
            return defaultUnit;

        return v.ToString() switch
        {
            "year" or "years" => "year",
            "month" or "months" => "month",
            "week" or "weeks" => "week",
            "day" or "days" => "day",
            var other => throw JSEngine.NewRangeError($"Temporal.PlainDate: invalid largestUnit \"{other}\""),
        };
    }

    private static JSValue ToTemporalDate(JSValue item, string overflow)
    {
        if (item is JSTemporalPlainDate d)
            return new JSTemporalPlainDate(d.isoYear, d.isoMonth, d.isoDay, PlainDatePrototype);

        if (item.IsString)
            return ParseTemporalDateString(item.ToString());

        if (item is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.PlainDate: invalid value");

        RequireCalendar(obj[KeyStrings.GetOrCreate("calendar")]);

        var yearValue = obj[KeyStrings.GetOrCreate("year")];
        var monthValue = obj[KeyStrings.GetOrCreate("month")];
        var monthCodeValue = obj[KeyStrings.GetOrCreate("monthCode")];
        var dayValue = obj[KeyStrings.GetOrCreate("day")];

        if (yearValue.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.PlainDate: missing year");
        if (dayValue.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.PlainDate: missing day");
        if (monthValue.IsUndefined && monthCodeValue.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.PlainDate: missing month / monthCode");

        var month = monthCodeValue.IsUndefined ? ToIntegerWithTruncation(monthValue) : MonthFromCode(monthCodeValue.ToString());
        if (!monthValue.IsUndefined && !monthCodeValue.IsUndefined && ToIntegerWithTruncation(monthValue) != month)
            throw JSEngine.NewRangeError("Temporal.PlainDate: month and monthCode disagree");

        return RegulateISODate(ToIntegerWithTruncation(yearValue), month, ToIntegerWithTruncation(dayValue), overflow);
    }

    private static readonly Regex DatePattern = new(
        @"^(\d{4}|[+-−]\d{6})-(\d{2})-(\d{2})(?:[Tt ].*)?$",
        RegexOptions.CultureInvariant);

    private static JSValue ParseTemporalDateString(string text)
    {
        var match = DatePattern.Match(text);
        if (!match.Success)
            throw JSEngine.NewRangeError($"Cannot parse Temporal.PlainDate from \"{text}\"");

        var year = int.Parse(match.Groups[1].Value.Replace('−', '-'), CultureInfo.InvariantCulture);
        var month = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        var day = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

        if (!IsValidISODate(year, month, day))
            throw JSEngine.NewRangeError($"Cannot parse Temporal.PlainDate from \"{text}\"");

        return new JSTemporalPlainDate(year, month, day, PlainDatePrototype);
    }

    // RegulateISODate: constrain each field into range, or reject an out-of-range value.
    private static JSValue RegulateISODate(int year, int month, int day, string overflow)
    {
        if (overflow == "reject")
        {
            if (!IsValidISODate(year, month, day))
                throw JSEngine.NewRangeError("Temporal.PlainDate: date is out of range");
        }
        else
        {
            month = Math.Clamp(month, 1, 12);
            day = Math.Clamp(day, 1, DaysInMonthOf(year, month));
            if (!IsValidISODate(year, month, day))
                throw JSEngine.NewRangeError("Temporal.PlainDate: date is out of range");
        }

        return new JSTemporalPlainDate(year, month, day, PlainDatePrototype);
    }

    // ── ISO 8601 calendar arithmetic ────────────────────────────────────────────

    private static bool IsLeapYear(long y) => (y % 4 == 0 && y % 100 != 0) || y % 400 == 0;

    private static int DaysInMonthOf(long year, int month) => month switch
    {
        1 or 3 or 5 or 7 or 8 or 10 or 12 => 31,
        4 or 6 or 9 or 11 => 30,
        2 => IsLeapYear(year) ? 29 : 28,
        _ => 0,
    };

    private static bool IsValidISODate(int year, int month, int day)
    {
        if (month is < 1 or > 12) return false;
        if (day < 1 || day > DaysInMonthOf(year, month)) return false;

        var epoch = DaysFromCivil(year, month, day);
        return epoch >= MinEpochDays && epoch <= MaxEpochDays;
    }

    private static int CompareISODate(int y1, int m1, int d1, int y2, int m2, int d2)
    {
        if (y1 != y2) return y1 < y2 ? -1 : 1;
        if (m1 != m2) return m1 < m2 ? -1 : 1;
        if (d1 != d2) return d1 < d2 ? -1 : 1;
        return 0;
    }

    private static JSValue AddISODate(int year, int month, int day, long years, long months, long weeks, long days, string overflow)
    {
        // Add years and months, balancing the month into 1..12, then constrain/reject the day.
        var total = (long)month - 1 + months;
        var newYear = year + years + FloorDiv(total, 12);
        var newMonth = (int)(((total % 12) + 12) % 12) + 1;

        var maxDay = DaysInMonthOf(newYear, newMonth);
        var regulatedDay = day;
        if (overflow == "reject")
        {
            if (day > maxDay)
                throw JSEngine.NewRangeError("Temporal.PlainDate: day out of range for resulting month");
        }
        else
        {
            regulatedDay = Math.Min(day, maxDay);
        }

        // Add weeks + days as a plain day offset via the epoch-day axis.
        var epoch = DaysFromCivil(newYear, newMonth, regulatedDay) + days + weeks * 7;
        if (epoch < MinEpochDays || epoch > MaxEpochDays)
            throw JSEngine.NewRangeError("Temporal.PlainDate: result is out of range");

        var (ry, rm, rd) = CivilFromDays(epoch);
        return new JSTemporalPlainDate((int)ry, (int)rm, (int)rd, PlainDatePrototype);
    }

    // DifferenceISODate from a (start) to b (end), both with a >= b or a <= b resolved by sign.
    private static (double years, double months, double weeks, double days) DifferenceISODate(
        int ay, int am, int ad, int by, int bm, int bd, string largestUnit)
    {
        var startEpoch = DaysFromCivil(ay, am, ad);
        var endEpoch = DaysFromCivil(by, bm, bd);

        if (largestUnit is "day" or "week")
        {
            var totalDays = endEpoch - startEpoch;
            if (largestUnit == "week")
                return (0, 0, totalDays / 7, totalDays % 7);
            return (0, 0, 0, totalDays);
        }

        // largestUnit year or month: step whole years, then whole months, then count days.
        var sign = CompareISODate(ay, am, ad, by, bm, bd); // -1 if a<b, +1 if a>b
        if (sign == 0) return (0, 0, 0, 0);
        var step = -sign; // +1 when end is after start

        long years = by - ay;
        var (my, mm, md) = AddYearsMonths(ay, am, ad, years, 0);
        if (CompareISODate(my, mm, md, by, bm, bd) == step)
        {
            // overshot — back off one year
            years -= step;
            (my, mm, md) = AddYearsMonths(ay, am, ad, years, 0);
        }

        long months = 0;
        while (true)
        {
            var (ny, nm, nd) = AddYearsMonths(my, mm, md, 0, step);
            var cmp = CompareISODate(ny, nm, nd, by, bm, bd);
            if (cmp == step)
                break; // one more month would overshoot the end

            months += step; my = ny; mm = nm; md = nd;
            if (cmp == 0)
                break; // landed exactly on the end
        }

        var days = DaysFromCivil(by, bm, bd) - DaysFromCivil(my, mm, md);

        if (largestUnit == "month")
        {
            months += years * 12;
            years = 0;
        }

        return (years, months, 0, days);
    }

    // Adds whole years and months with the ISO "constrain" overflow (clamp day to month).
    private static (int y, int m, int d) AddYearsMonths(int year, int month, int day, long years, long months)
    {
        var total = (long)month - 1 + months;
        var newYear = (int)(year + years + FloorDiv(total, 12));
        var newMonth = (int)(((total % 12) + 12) % 12) + 1;
        var newDay = Math.Min(day, DaysInMonthOf(newYear, newMonth));
        return (newYear, newMonth, newDay);
    }

    private static int IsoDayOfWeek(int year, int month, int day)
    {
        var epoch = DaysFromCivil(year, month, day);
        // 1970-01-01 (epoch day 0) is a Thursday (ISO weekday 4).
        return (int)(((epoch + 3) % 7 + 7) % 7) + 1;
    }

    // ISO 8601 week-of-year and its associated week-numbering year.
    private static (int week, int year) IsoWeek(int year, int month, int day)
    {
        var ordinal = (int)(DaysFromCivil(year, month, day) - DaysFromCivil(year, 1, 1)) + 1;
        var weekday = IsoDayOfWeek(year, month, day);
        var week = (ordinal - weekday + 10) / 7;

        if (week < 1)
            return (WeeksInIsoYear(year - 1), year - 1);

        if (week > WeeksInIsoYear(year))
            return (1, year + 1);

        return (week, year);
    }

    private static int WeeksInIsoYear(int year)
    {
        int P(int y) => (y + y / 4 - y / 100 + y / 400) % 7;
        return P(year) == 4 || P(year - 1) == 3 ? 53 : 52;
    }

    private static long FloorDiv(long a, long b)
    {
        var q = a / b;
        if (a % b != 0 && (a < 0) != (b < 0)) q -= 1;
        return q;
    }

    // Days from 1970-01-01 (Howard Hinnant's days_from_civil).
    private static long DaysFromCivil(long y, long m, long d)
    {
        y -= m <= 2 ? 1 : 0;
        var era = (y >= 0 ? y : y - 399) / 400;
        var yoe = y - era * 400;
        var doy = (153 * (m > 2 ? m - 3 : m + 9) + 2) / 5 + d - 1;
        var doe = yoe * 365 + yoe / 4 - yoe / 100 + doy;
        return era * 146097 + doe - 719468;
    }

    private static (long y, long m, long d) CivilFromDays(long z)
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

    // YYYY-MM-DD, with a ±YYYYYY expanded year outside 0000–9999. The ISO calendar gets no
    // "[u-ca=…]" annotation under the default calendarName "auto".
    private string ToISOString()
    {
        var sb = new StringBuilder();
        if (isoYear < 0 || isoYear > 9999)
            sb.Append(isoYear < 0 ? '-' : '+').Append(Math.Abs(isoYear).ToString("000000", CultureInfo.InvariantCulture));
        else
            sb.Append(isoYear.ToString("0000", CultureInfo.InvariantCulture));

        sb.Append('-').Append(isoMonth.ToString("00", CultureInfo.InvariantCulture))
          .Append('-').Append(isoDay.ToString("00", CultureInfo.InvariantCulture));
        return sb.ToString();
    }
}
