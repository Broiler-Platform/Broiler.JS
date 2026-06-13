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
// only for round-tripping. Only the ISO 8601 calendar is supported. Registered under the
// Temporal namespace via Register = false.
[JSClassGenerator("PlainMonthDay", Register = false)]
public partial class JSTemporalPlainMonthDay : JSObject
{
    private const int DefaultReferenceYear = 1972;

    internal readonly int isoMonth, isoDay, referenceISOYear;

    [JSExport(Length = 2)]
    public JSTemporalPlainMonthDay(in Arguments a) : base(ResolvePrototype())
    {
        isoMonth = ToIntegerWithTruncation(a.GetAt(0));
        isoDay = ToIntegerWithTruncation(a.GetAt(1));
        RequireCalendar(a.GetAt(2));
        var refYear = a.GetAt(3);
        referenceISOYear = refYear == null || refYear.IsUndefined ? DefaultReferenceYear : ToIntegerWithTruncation(refYear);

        if (!IsValidISODate(referenceISOYear, isoMonth, isoDay))
            throw JSEngine.NewRangeError("Temporal.PlainMonthDay: invalid ISO month-day");
    }

    internal JSTemporalPlainMonthDay(int isoMonth, int isoDay, int referenceISOYear, JSObject prototype) : base(prototype)
    {
        this.isoMonth = isoMonth; this.isoDay = isoDay; this.referenceISOYear = referenceISOYear;
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

    [JSExport("calendarId")] public JSValue CalendarId => new JSString("iso8601");
    [JSExport("monthCode")] public JSValue MonthCode => new JSString($"M{isoMonth:00}");
    [JSExport("day")] public double DayValue => isoDay;

    // ── statics ─────────────────────────────────────────────────────────────────

    [JSExport("from", Length = 1)]
    internal static JSValue From(in Arguments a)
    {
        var item = a.GetAt(0);
        var overflow = ReadOverflow(a.GetAt(1));

        if (item is JSTemporalPlainMonthDay md)
            return new JSTemporalPlainMonthDay(md.isoMonth, md.isoDay, md.referenceISOYear, PlainMonthDayPrototype);

        return ToTemporalMonthDay(item, overflow);
    }

    // ── methods ─────────────────────────────────────────────────────────────────

    [JSExport("with", Length = 1)]
    public JSValue With(in Arguments a)
    {
        if (a.GetAt(0) is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.PlainMonthDay.prototype.with requires an object");
        if (!obj[KeyStrings.GetOrCreate("calendar")].IsUndefined)
            throw JSEngine.NewTypeError("Temporal.PlainMonthDay.prototype.with does not accept a calendar field");

        var overflow = ReadOverflow(a.GetAt(1));

        var any = false;
        var month = isoMonth; var day = isoDay;

        var monthCodeValue = obj[KeyStrings.GetOrCreate("monthCode")];
        var monthValue = obj[KeyStrings.GetOrCreate("month")];
        if (!monthCodeValue.IsUndefined) { month = MonthFromCode(monthCodeValue.ToString()); any = true; }
        if (!monthValue.IsUndefined)
        {
            var m = ToIntegerWithTruncation(monthValue);
            if (!monthCodeValue.IsUndefined && m != month)
                throw JSEngine.NewRangeError("Temporal.PlainMonthDay.with: month and monthCode disagree");
            month = m; any = true;
        }

        var dayValue = obj[KeyStrings.GetOrCreate("day")];
        if (!dayValue.IsUndefined) { day = ToIntegerWithTruncation(dayValue); any = true; }

        if (!any)
            throw JSEngine.NewTypeError("Temporal.PlainMonthDay.prototype.with requires at least one field");

        return RegulateMonthDay(month, day, overflow);
    }

    [JSExport("equals", Length = 1)]
    public JSValue Equals(in Arguments a)
    {
        var other = Require(ToTemporalMonthDay(a.GetAt(0), "constrain"));
        return isoMonth == other.isoMonth && isoDay == other.isoDay && referenceISOYear == other.referenceISOYear
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
        var day = Math.Clamp(isoDay, 1, DaysInMonthOf(year, isoMonth));
        return new JSTemporalPlainDate(year, isoMonth, day, JSTemporalPlainDate.PlainDatePrototype);
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
    public JSValue ToLocaleString(in Arguments a) => new JSString(ToISOString());

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

    private static void RequireCalendar(JSValue calendar)
    {
        if (calendar == null || calendar.IsUndefined) return;
        var id = TemporalCalendar.ToSlotValue(calendar, includeArithmetic: true);
        if (id != "iso8601")
            throw JSEngine.NewRangeError($"Temporal.PlainMonthDay: unsupported calendar \"{id}\" (only iso8601 is implemented)");
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

    private static JSValue ToTemporalMonthDay(JSValue item, string overflow)
    {
        if (item is JSTemporalPlainMonthDay md)
            return new JSTemporalPlainMonthDay(md.isoMonth, md.isoDay, md.referenceISOYear, PlainMonthDayPrototype);

        if (item.IsString)
            return ParseTemporalMonthDayString(item.ToString());

        if (item is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.PlainMonthDay: invalid value");

        RequireCalendar(obj[KeyStrings.GetOrCreate("calendar")]);

        var monthValue = obj[KeyStrings.GetOrCreate("month")];
        var monthCodeValue = obj[KeyStrings.GetOrCreate("monthCode")];
        var dayValue = obj[KeyStrings.GetOrCreate("day")];
        var yearValue = obj[KeyStrings.GetOrCreate("year")];

        if (dayValue.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.PlainMonthDay: missing day");
        if (monthValue.IsUndefined && monthCodeValue.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.PlainMonthDay: missing month / monthCode");
        // A month (not monthCode) without a year cannot resolve a reference year for 02-29 etc.
        if (monthCodeValue.IsUndefined && yearValue.IsUndefined)
            throw JSEngine.NewTypeError("Temporal.PlainMonthDay: month requires either monthCode or year");

        var month = monthCodeValue.IsUndefined ? ToIntegerWithTruncation(monthValue) : MonthFromCode(monthCodeValue.ToString());
        if (!monthValue.IsUndefined && !monthCodeValue.IsUndefined && ToIntegerWithTruncation(monthValue) != month)
            throw JSEngine.NewRangeError("Temporal.PlainMonthDay: month and monthCode disagree");

        return RegulateMonthDay(month, ToIntegerWithTruncation(dayValue), overflow);
    }

    private static readonly Regex MonthDayPattern = new(
        @"^(?:--)?(\d{2})-?(\d{2})(?:\[[^\]]*\])*$",
        RegexOptions.CultureInvariant);

    private static readonly Regex FullDatePattern = new(
        @"^(\d{4}|\+\d{6}|-(?!000000)\d{6})-(\d{2})-(\d{2})(?:[Tt ].*)?(?:\[[^\]]*\])*$",
        RegexOptions.CultureInvariant);

    private static JSValue ParseTemporalMonthDayString(string text)
    {
        // Only the ASCII hyphen-minus is a valid sign; reject the U+2212 variant the lenient
        // time/offset tail would otherwise accept.
        if (text.Contains('−'))
            throw JSEngine.NewRangeError($"Cannot parse Temporal.PlainMonthDay from \"{text}\"");

        var full = FullDatePattern.Match(text);
        if (full.Success)
        {
            var fm = int.Parse(full.Groups[2].Value, CultureInfo.InvariantCulture);
            var fd = int.Parse(full.Groups[3].Value, CultureInfo.InvariantCulture);
            if (!IsValidISODate(DefaultReferenceYear, fm, fd))
                throw JSEngine.NewRangeError($"Cannot parse Temporal.PlainMonthDay from \"{text}\"");
            return new JSTemporalPlainMonthDay(fm, fd, DefaultReferenceYear, PlainMonthDayPrototype);
        }

        var match = MonthDayPattern.Match(text);
        if (!match.Success)
            throw JSEngine.NewRangeError($"Cannot parse Temporal.PlainMonthDay from \"{text}\"");

        var month = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var day = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        if (!IsValidISODate(DefaultReferenceYear, month, day))
            throw JSEngine.NewRangeError($"Cannot parse Temporal.PlainMonthDay from \"{text}\"");

        return new JSTemporalPlainMonthDay(month, day, DefaultReferenceYear, PlainMonthDayPrototype);
    }

    private static JSValue RegulateMonthDay(int month, int day, string overflow)
    {
        if (overflow == "reject")
        {
            if (!IsValidISODate(DefaultReferenceYear, month, day))
                throw JSEngine.NewRangeError("Temporal.PlainMonthDay: month-day is out of range");
        }
        else
        {
            month = Math.Clamp(month, 1, 12);
            day = Math.Clamp(day, 1, DaysInMonthOf(DefaultReferenceYear, month));
        }

        return new JSTemporalPlainMonthDay(month, day, DefaultReferenceYear, PlainMonthDayPrototype);
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

    // MM-DD for the ISO calendar; a non-1972 reference year is prefixed as YYYY-MM-DD. The calendar
    // (always iso8601) is annotated only when showCalendar forces it.
    private string ToISOString(string showCalendar)
    {
        var sb = new StringBuilder();
        if (referenceISOYear != DefaultReferenceYear)
        {
            if (referenceISOYear < 0 || referenceISOYear > 9999)
                sb.Append(referenceISOYear < 0 ? '-' : '+').Append(Math.Abs(referenceISOYear).ToString("000000", CultureInfo.InvariantCulture));
            else
                sb.Append(referenceISOYear.ToString("0000", CultureInfo.InvariantCulture));
            sb.Append('-');
        }

        sb.Append(isoMonth.ToString("00", CultureInfo.InvariantCulture))
          .Append('-').Append(isoDay.ToString("00", CultureInfo.InvariantCulture));

        // iso8601 is the implied calendar, so it is shown only when explicitly requested.
        if (showCalendar is "always" or "critical")
            sb.Append(showCalendar == "critical" ? "[!u-ca=iso8601]" : "[u-ca=iso8601]");
        return sb.ToString();
    }
}
