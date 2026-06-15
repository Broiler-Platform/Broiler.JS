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

// Temporal.PlainMonthDay (Temporal proposal §10): a calendar month and day (e.g. "--06-15"),
// carrying an ISO reference year (default 1972, a leap year so 02-29 is representable) used
// only for round-tripping. The ISO 8601 calendar and the Gregorian-family calendars (gregory,
// buddhist, roc, japanese) — which share the ISO month/day structure — are supported; the
// lunisolar / arithmetic non-ISO calendars are not yet implemented for PlainMonthDay. Registered
// under the Temporal namespace via Register = false.
[JSClassGenerator("PlainMonthDay", Register = false)]
public partial class JSTemporalPlainMonthDay : JSObject
{
    private const int DefaultReferenceYear = 1972;

    internal readonly int isoMonth, isoDay, referenceISOYear;
    internal readonly string calendarId;

    [JSExport(Length = 2)]
    public JSTemporalPlainMonthDay(in Arguments a) : base(ResolvePrototype())
    {
        isoMonth = ToIntegerWithTruncation(a.GetAt(0));
        isoDay = ToIntegerWithTruncation(a.GetAt(1));
        calendarId = ResolveCalendar(a.GetAt(2));
        var refYear = a.GetAt(3);
        referenceISOYear = refYear == null || refYear.IsUndefined ? DefaultReferenceYear : ToIntegerWithTruncation(refYear);

        if (!IsValidISODate(referenceISOYear, isoMonth, isoDay))
            throw JSEngine.NewRangeError("Temporal.PlainMonthDay: invalid ISO month-day");
    }

    internal JSTemporalPlainMonthDay(int isoMonth, int isoDay, int referenceISOYear, JSObject prototype)
        : this(isoMonth, isoDay, referenceISOYear, "iso8601", prototype) { }

    internal JSTemporalPlainMonthDay(int isoMonth, int isoDay, int referenceISOYear, string calendarId, JSObject prototype) : base(prototype)
    {
        this.isoMonth = isoMonth; this.isoDay = isoDay; this.referenceISOYear = referenceISOYear; this.calendarId = calendarId;
    }

    private static JSObject ResolvePrototype()
    {
        if (JSEngine.NewTarget == null && (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
            throw JSEngine.NewTypeError("Constructor Temporal.PlainMonthDay requires 'new'");

        return JSEngine.NewTargetPrototype ?? PlainMonthDayPrototype;
    }

    internal static JSObject PlainMonthDayPrototype
    {
        get
        {
            var temporal = (JSEngine.Current as JSObject)?[KeyStrings.GetOrCreate("Temporal")] as JSObject;
            return (temporal?[KeyStrings.GetOrCreate("PlainMonthDay")] as JSFunction)?.prototype;
        }
    }

    // ── accessors ───────────────────────────────────────────────────────────────

    private bool NonIso => TemporalCalendarMath.IsNonIso(calendarId);

    [JSExport("calendarId")] public JSValue CalendarId => new JSString(calendarId);
    [JSExport("monthCode")] public JSValue MonthCode => new JSString(
        NonIso ? TemporalNonIso.MonthDayFields(calendarId, referenceISOYear, isoMonth, isoDay).monthCode : $"M{isoMonth:00}");
    [JSExport("day")] public double DayValue =>
        NonIso ? TemporalNonIso.MonthDayFields(calendarId, referenceISOYear, isoMonth, isoDay).day : isoDay;

    // ── statics ─────────────────────────────────────────────────────────────────

    [JSExport("from", Length = 1)]
    internal static JSValue From(in Arguments a)
        => ToTemporalMonthDay(a.GetAt(0), a.GetAt(1));

    // ── methods ─────────────────────────────────────────────────────────────────

    [JSExport("with", Length = 1)]
    public JSValue With(in Arguments a)
    {
        if (a.GetAt(0) is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.PlainMonthDay.prototype.with requires an object");
        TemporalCalendar.RejectObjectWithCalendarOrTimeZone(obj);

        var monthCodeValue = obj[KeyStrings.GetOrCreate("monthCode")];
        var monthValue = obj[KeyStrings.GetOrCreate("month")];
        var dayValue = obj[KeyStrings.GetOrCreate("day")];

        if (NonIso)
        {
            var (curCode, curDay) = TemporalNonIso.MonthDayFields(calendarId, referenceISOYear, isoMonth, isoDay);
            var code = curCode;
            var day = curDay;
            var any = false;

            if (!monthCodeValue.IsUndefined) { code = monthCodeValue.ToString(); any = true; }
            else if (!monthValue.IsUndefined)
            {
                // A numeric month has no year-independent meaning in a non-ISO calendar; resolve it
                // against the stored reference year's calendar.
                var refCalYear = TemporalNonIso.CalendarYmd(calendarId, referenceISOYear, isoMonth, isoDay).y;
                code = TemporalCalendarMath.MonthCode(calendarId, refCalYear, ToPositiveIntegerWithTruncation(monthValue));
                any = true;
            }
            if (!dayValue.IsUndefined) { day = ToPositiveIntegerWithTruncation(dayValue); any = true; }

            if (!any)
                throw JSEngine.NewTypeError("Temporal.PlainMonthDay.prototype.with requires at least one field");

            var nonIsoOverflow = ReadOverflow(a.GetAt(1));
            var (ny, nm, nd) = TemporalNonIso.MonthDayFromCode(calendarId, code, day, nonIsoOverflow);
            return new JSTemporalPlainMonthDay(nm, nd, ny, calendarId, PlainMonthDayPrototype);
        }

        var anyIso = false;
        var month = isoMonth; var isoDayOut = isoDay;
        if (!monthCodeValue.IsUndefined) { month = MonthFromCode(monthCodeValue.ToString()); anyIso = true; }
        if (!monthValue.IsUndefined)
        {
            var m = ToPositiveIntegerWithTruncation(monthValue);
            if (!monthCodeValue.IsUndefined && m != month)
                throw JSEngine.NewRangeError("Temporal.PlainMonthDay.with: month and monthCode disagree");
            month = m; anyIso = true;
        }

        if (!dayValue.IsUndefined) { isoDayOut = ToPositiveIntegerWithTruncation(dayValue); anyIso = true; }

        if (!anyIso)
            throw JSEngine.NewTypeError("Temporal.PlainMonthDay.prototype.with requires at least one field");

        // GetTemporalOverflowOption runs only after the partial fields have been read and coerced.
        var overflow = ReadOverflow(a.GetAt(1));

        return RegulateMonthDay(month, isoDayOut, overflow, calendarId, referenceISOYear);
    }

    [JSExport("equals", Length = 1)]
    public JSValue Equals(in Arguments a)
    {
        var other = Require(ToTemporalMonthDay(a.GetAt(0)));
        return isoMonth == other.isoMonth && isoDay == other.isoDay && referenceISOYear == other.referenceISOYear
            && calendarId == other.calendarId
            ? JSValue.BooleanTrue : JSValue.BooleanFalse;
    }

    [JSExport("toPlainDate", Length = 1)]
    public JSValue ToPlainDate(in Arguments a)
    {
        if (a.GetAt(0) is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.PlainMonthDay.prototype.toPlainDate requires an object");
        var yearValue = obj[KeyStrings.GetOrCreate("year")];
        if (yearValue.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.PlainMonthDay.prototype.toPlainDate requires a year");

        var year = ToIntegerWithTruncation(yearValue);

        if (NonIso)
        {
            var (code, calDay) = TemporalNonIso.MonthDayFields(calendarId, referenceISOYear, isoMonth, isoDay);
            var (iy, im, id) = TemporalNonIso.MonthDayToPlainDate(calendarId, year, code, calDay);
            return new JSTemporalPlainDate(iy, im, id, calendarId, JSTemporalPlainDate.PlainDatePrototype);
        }

        var day = Math.Clamp(isoDay, 1, DaysInMonthOf(year, isoMonth));
        return new JSTemporalPlainDate(year, isoMonth, day, calendarId, JSTemporalPlainDate.PlainDatePrototype);
    }

    [JSExport("toString", Length = 0)]
    public JSValue ToStringMethod(in Arguments a) => new JSString(ToISOString(ReadCalendarName(a.GetAt(0))));

    // GetTemporalShowCalendarNameOption (shared shape with PlainDate).
    private static string ReadCalendarName(JSValue options)
    {
        if (options == null || options.IsUndefined) return "auto";
        if (options is not JSObject optionsObject)
            throw JSEngine.NewTypeError("Temporal options must be an object or undefined");

        var v = optionsObject[KeyStrings.GetOrCreate("calendarName")];
        if (v.IsUndefined) return "auto";

        var name = v.StringValue;
        return name switch
        {
            "auto" or "always" or "never" or "critical" => name,
            _ => throw JSEngine.NewRangeError($"Temporal: invalid calendarName \"{name}\""),
        };
    }

    [JSExport("toJSON", Length = 0)]
    public JSValue ToJSON(in Arguments a) => new JSString(ToISOString());

    [JSExport("toLocaleString", Length = 0)]
    public JSValue ToLocaleString(in Arguments a)
        => Intl.JSIntlDateTimeFormat.TemporalToLocaleString(this, a.GetAt(0), a.GetAt(1));

    [JSExport("valueOf", Length = 0)]
    public JSValue ValueOf(in Arguments a)
        => throw JSEngine.NewTypeError("Called Temporal.PlainMonthDay.prototype.valueOf, which is not supported.");

    // ── helpers ─────────────────────────────────────────────────────────────────

    private static JSTemporalPlainMonthDay Require(JSValue value)
        => value as JSTemporalPlainMonthDay ?? throw JSEngine.NewTypeError("expected a Temporal.PlainMonthDay");

    private static int ToIntegerWithTruncation(JSValue value)
    {
        if (value == null || value.IsUndefined) return 0;
        var number = value.DoubleValue;
        if (double.IsNaN(number) || double.IsInfinity(number))
            throw JSEngine.NewRangeError("Temporal.PlainMonthDay: component must be finite");
        return (int)Math.Truncate(number);
    }

    // month / day fields must be a positive (≥ 1) integer (RangeError otherwise) — checked when the
    // field is read, before the overflow option is processed.
    private static int ToPositiveIntegerWithTruncation(JSValue value)
    {
        var n = ToIntegerWithTruncation(value);
        if (n < 1)
            throw JSEngine.NewRangeError("Temporal.PlainMonthDay: month and day must be positive");
        return n;
    }

    // ToTemporalCalendarSlotValue: the ISO 8601 and Gregorian-family calendars share the ISO
    // month/day structure (stored directly); the arithmetic and lunisolar non-ISO calendars store an
    // ISO reference date whose calendar projection carries the requested month-day.
    private static string ResolveCalendar(JSValue calendar)
    {
        if (calendar == null || calendar.IsUndefined) return "iso8601";
        return TemporalCalendar.ToSlotValue(calendar, includeArithmetic: true);
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
        if (options == null || options.IsUndefined) return "constrain";
        if (options is not JSObject optionsObject)
            throw JSEngine.NewTypeError("Temporal options must be an object or undefined");
        var v = optionsObject[KeyStrings.GetOrCreate("overflow")];
        if (v.IsUndefined) return "constrain";
        var overflow = v.StringValue;
        if (overflow is not ("constrain" or "reject"))
            throw JSEngine.NewRangeError($"Temporal: invalid overflow \"{overflow}\"");
        return overflow;
    }

    private static JSValue ToTemporalMonthDay(JSValue item) => ToTemporalMonthDay(item, JSUndefined.Value);

    // `options` is the raw options argument: the overflow option is read at the spec-mandated point
    // (after the item's type is validated and its fields/string are read), so an invalid primitive
    // item throws a TypeError before the options bag is ever observed.
    private static JSValue ToTemporalMonthDay(JSValue item, JSValue options)
    {
        if (item is JSTemporalPlainMonthDay md)
        {
            ReadOverflow(options);
            return new JSTemporalPlainMonthDay(md.isoMonth, md.isoDay, md.referenceISOYear, md.calendarId, PlainMonthDayPrototype);
        }

        if (item.IsString)
        {
            var parsed = ParseTemporalMonthDayString(item.ToString());
            ReadOverflow(options);
            return parsed;
        }

        if (item is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.PlainMonthDay: invalid value");

        var calendarId = ResolveCalendar(obj[KeyStrings.GetOrCreate("calendar")]);

        if (TemporalCalendarMath.IsNonIso(calendarId))
        {
            var nonIsoOverflow = ReadOverflow(options);
            var (ny, nm, nd) = TemporalNonIso.MonthDayFromBag(obj, calendarId, nonIsoOverflow, "Temporal.PlainMonthDay");
            return new JSTemporalPlainMonthDay(nm, nd, ny, calendarId, PlainMonthDayPrototype);
        }

        var monthValue = obj[KeyStrings.GetOrCreate("month")];
        var monthCodeValue = obj[KeyStrings.GetOrCreate("monthCode")];
        var dayValue = obj[KeyStrings.GetOrCreate("day")];
        var yearValue = obj[KeyStrings.GetOrCreate("year")];

        if (dayValue.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.PlainMonthDay: missing day");
        if (monthValue.IsUndefined && monthCodeValue.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.PlainMonthDay: missing month / monthCode");

        var overflow = ReadOverflow(options);

        var month = monthCodeValue.IsUndefined ? ToIntegerWithTruncation(monthValue) : MonthFromCode(monthCodeValue.ToString());
        if (!monthValue.IsUndefined && !monthCodeValue.IsUndefined && ToIntegerWithTruncation(monthValue) != month)
            throw JSEngine.NewRangeError("Temporal.PlainMonthDay: month and monthCode disagree");

        // A bare month/day with no year resolves against the leap-year reference (1972) so that
        // 02-29 is representable; a supplied year is used to validate/constrain the day instead.
        var validationYear = yearValue.IsUndefined ? DefaultReferenceYear : ToIntegerWithTruncation(yearValue);

        return RegulateMonthDay(month, ToIntegerWithTruncation(dayValue), overflow, calendarId, validationYear);
    }

    private static readonly Regex MonthDayPattern = new(
        @"^(?:--)?(\d{2})-?(\d{2})(?:\[[^\]]*\])*$",
        RegexOptions.CultureInvariant);

    // A full calendar date (with an optional, discarded time tail) from which the month and day are
    // extracted. The date portion may use the extended (YYYY-MM-DD) or basic (YYYYMMDD) form — but
    // not a mix — mirroring TemporalIsoString.DateTimePattern.
    private static readonly Regex FullDatePattern = new(
        @"^(?<y>\d{4}|\+\d{6}|-(?!000000)\d{6})(?:-(?<mo>\d{2})-(?<d>\d{2})|(?<mo>\d{2})(?<d>\d{2}))" +
        TemporalIsoString.TimeAndOffsetTail + TemporalIsoString.AnnotationsTail + "$",
        RegexOptions.CultureInvariant);

    private static readonly Regex CalendarAnnotation = new(@"\[!?u-ca=([^\]]+)\]", RegexOptions.CultureInvariant);

    private static JSValue ParseTemporalMonthDayString(string text)
    {
        // Only the ASCII hyphen-minus is a valid sign; reject the U+2212 variant the time/offset
        // tail would otherwise accept.
        if (text.Contains('−'))
            throw JSEngine.NewRangeError($"Cannot parse Temporal.PlainMonthDay from \"{text}\"");

        TemporalIsoString.RejectMultipleCalendarAnnotations(text);
        TemporalIsoString.RejectMalformedAnnotations(text);
        TemporalIsoString.RejectInvalidAnnotations(text);

        var calMatch = CalendarAnnotation.Match(text);
        var calendarId = calMatch.Success ? ResolveCalendarId(calMatch.Groups[1].Value, text) : "iso8601";

        var full = FullDatePattern.Match(text);
        if (full.Success)
        {
            TemporalIsoString.RejectTimeTailForCalendarOnly(full, text);
            var fy = int.Parse(full.Groups["y"].Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            var fm = int.Parse(full.Groups["mo"].Value, CultureInfo.InvariantCulture);
            var fd = int.Parse(full.Groups["d"].Value, CultureInfo.InvariantCulture);
            if (!IsValidISODate(fy, fm, fd))
                throw JSEngine.NewRangeError($"Cannot parse Temporal.PlainMonthDay from \"{text}\"");

            // The iso8601 calendar discards the parsed year and anchors against the leap-year reference
            // (1972). Any other calendar keeps the parsed date as its reference (ToTemporalMonthDay
            // string path: CreateTemporalMonthDay with referenceISOYear), so the date must be
            // representable — RejectDateRange — before being projected to the calendar's month-day.
            if (calendarId == "iso8601")
                return new JSTemporalPlainMonthDay(fm, fd, DefaultReferenceYear, calendarId, PlainMonthDayPrototype);

            var epochDays = TemporalNonIso.DaysFromCivil(fy, fm, fd);
            if (epochDays < TemporalNonIso.MinEpochDays || epochDays > TemporalNonIso.MaxEpochDays)
                throw JSEngine.NewRangeError($"Cannot parse Temporal.PlainMonthDay from \"{text}\"");

            // A Gregorian-family calendar shares the ISO month-day (anchored to the leap-year reference);
            // a non-Gregorian calendar projects the representable ISO date to its own month-day.
            if (!TemporalCalendarMath.IsNonIso(calendarId))
                return new JSTemporalPlainMonthDay(fm, fd, DefaultReferenceYear, calendarId, PlainMonthDayPrototype);

            var (cy, cm, cd) = TemporalNonIso.CalendarYmd(calendarId, fy, fm, fd);
            var monthCode = TemporalCalendarMath.MonthCode(calendarId, cy, cm);
            var (ny, nm, nd) = TemporalNonIso.MonthDayFromCode(calendarId, monthCode, cd, "constrain");
            return new JSTemporalPlainMonthDay(nm, nd, ny, calendarId, PlainMonthDayPrototype);
        }

        var match = MonthDayPattern.Match(text);
        if (!match.Success)
            throw JSEngine.NewRangeError($"Cannot parse Temporal.PlainMonthDay from \"{text}\"");

        // A bare month-day (no year) carries no reference year, so it can only denote an iso8601
        // month-day; any other calendar annotation needs the full date format (ParseTemporalMonthDayString
        // throws "MM-DD format is only valid with iso8601 calendar").
        if (calendarId != "iso8601")
            throw JSEngine.NewRangeError($"Temporal.PlainMonthDay: a non-iso8601 calendar requires a year in \"{text}\"");

        var month = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var day = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        if (!IsValidISODate(DefaultReferenceYear, month, day))
            throw JSEngine.NewRangeError($"Cannot parse Temporal.PlainMonthDay from \"{text}\"");

        return new JSTemporalPlainMonthDay(month, day, DefaultReferenceYear, calendarId, PlainMonthDayPrototype);
    }

    // Canonicalizes a [u-ca=…] annotation value.
    private static string ResolveCalendarId(string id, string text)
        => TemporalCalendar.Canonicalize(id, includeArithmetic: true);

    private static JSValue RegulateMonthDay(int month, int day, string overflow, string calendarId, int validationYear)
    {
        // Month and day must be positive integers; a non-positive value is out of range no matter
        // the overflow handling (constrain only clamps values that are too large).
        if (month < 1 || day < 1)
            throw JSEngine.NewRangeError("Temporal.PlainMonthDay: month-day is out of range");

        if (overflow == "reject")
        {
            if (!IsValidISODate(validationYear, month, day))
                throw JSEngine.NewRangeError("Temporal.PlainMonthDay: month-day is out of range");
        }
        else
        {
            month = Math.Min(month, 12);
            day = Math.Min(day, DaysInMonthOf(validationYear, month));
        }

        // The resolved value is always stored against the canonical leap-year reference (1972).
        return new JSTemporalPlainMonthDay(month, day, DefaultReferenceYear, calendarId, PlainMonthDayPrototype);
    }

    // ── ISO helpers ───────────────────────────────────────────────────────────────

    private static bool IsLeapYear(long y) => (y % 4 == 0 && y % 100 != 0) || y % 400 == 0;

    private static int DaysInMonthOf(long year, int month) => month switch
    {
        1 or 3 or 5 or 7 or 8 or 10 or 12 => 31,
        4 or 6 or 9 or 11 => 30,
        2 => IsLeapYear(year) ? 29 : 28,
        _ => 0,
    };

    private static bool IsValidISODate(int year, int month, int day)
        => month is >= 1 and <= 12 && day >= 1 && day <= DaysInMonthOf(year, month);

    private string ToISOString() => ToISOString("auto");

    // MM-DD for the ISO calendar. Per TemporalMonthDayToString the reference year is prefixed
    // (YYYY-MM-DD) when showCalendar forces the calendar to appear (always/critical) or when a
    // non-ISO calendar is attached (so the value round-trips). The calendar is annotated when
    // showCalendar forces it (always/critical) or, for "auto", when it is not iso8601.
    private string ToISOString(string showCalendar)
    {
        var nonIsoCalendar = calendarId != "iso8601";
        var sb = new StringBuilder();
        if (showCalendar is "always" or "critical" || nonIsoCalendar)
        {
            if (referenceISOYear < 0 || referenceISOYear > 9999)
                sb.Append(referenceISOYear < 0 ? '-' : '+').Append(Math.Abs(referenceISOYear).ToString("000000", CultureInfo.InvariantCulture));
            else
                sb.Append(referenceISOYear.ToString("0000", CultureInfo.InvariantCulture));
            sb.Append('-');
        }

        sb.Append(isoMonth.ToString("00", CultureInfo.InvariantCulture))
          .Append('-').Append(isoDay.ToString("00", CultureInfo.InvariantCulture));

        if (showCalendar is "always" or "critical" || (showCalendar == "auto" && nonIsoCalendar))
            sb.Append(showCalendar == "critical" ? $"[!u-ca={calendarId}]" : $"[u-ca={calendarId}]");
        return sb.ToString();
    }
}
