using System;
using Broiler.ECMAScript.Temporal;
using Broiler.ECMAScript.Time;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// Temporal.PlainMonthDay (Temporal proposal §10): a calendar month and day (e.g. "--06-15"),
// carrying an ISO reference year (1972 in ISO 8601, a leap year so 02-29 is representable; the
// calendar's own reference year elsewhere) used only for round-tripping. Its calendar fields, field
// resolution and reference dates are the shared library's calendars, through TemporalAdapter.
// Registered under the Temporal namespace via Register = false.
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
        calendarId = TemporalAdapter.CalendarArgument(a.GetAt(2), "Temporal.PlainMonthDay");
        var refYear = a.GetAt(3);
        referenceISOYear = refYear == null || refYear.IsUndefined ? DefaultReferenceYear : ToIntegerWithTruncation(refYear);

        // CreateTemporalMonthDay also runs ISODateWithinLimits on the reference date: a
        // referenceISOYear that pushes referenceISOYear-isoMonth-isoDay past the representable
        // ISO date range (after +275760-09-13 or before -271821-04-19) is a RangeError.
        var status = TemporalLimits.CheckIsoDate(IsoDate);
        if (status == TemporalStatus.InvalidDate)
            throw JSEngine.NewRangeError("Temporal.PlainMonthDay: invalid ISO month-day");
        TemporalAdapter.Check(status, "Temporal.PlainMonthDay: reference date is out of range");
    }

    internal JSTemporalPlainMonthDay(int isoMonth, int isoDay, int referenceISOYear, JSObject prototype)
        : this(isoMonth, isoDay, referenceISOYear, "iso8601", prototype) { }

    internal JSTemporalPlainMonthDay(int isoMonth, int isoDay, int referenceISOYear, string calendarId, JSObject prototype) : base(prototype)
    {
        this.isoMonth = isoMonth; this.isoDay = isoDay; this.referenceISOYear = referenceISOYear; this.calendarId = calendarId;
    }

    internal JSTemporalPlainMonthDay(IsoDate date, string calendarId, JSObject prototype)
        : this(date.Month, date.Day, (int)date.Year, calendarId, prototype) { }

    // The month-day's ISO date, its year the reference year, as the shared library's record.
    internal IsoDate IsoDate => new(referenceISOYear, isoMonth, isoDay);

    private static JSObject ResolvePrototype()
    {
        if (JSEngine.NewTarget == null && (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
            throw JSEngine.NewTypeError("Constructor Temporal.PlainMonthDay requires 'new'");

        return JSEngine.NewTargetPrototype ?? PlainMonthDayPrototype;
    }

    private static readonly KeyString PlainMonthDayIntrinsicKey = KeyStrings.GetOrCreate("Temporal.PlainMonthDay");

    // %Temporal.PlainMonthDay.prototype% of the current realm, never the prototype of whatever
    // `globalThis.Temporal.PlainMonthDay` holds now: guest code may replace the namespace or its members.
    internal static JSObject PlainMonthDayPrototype => Intrinsics.TemporalPrototype(PlainMonthDayIntrinsicKey);

    // ── accessors ───────────────────────────────────────────────────────────────

    // CalendarISOToDate of the reference date, the shared library's: the calendar's month code and day.
    private CalendarDate CalendarDate => TemporalAdapter.CalendarDateOf(calendarId, IsoDate);

    [JSExport("calendarId")] public JSValue CalendarId => new JSString(calendarId);
    [JSExport("monthCode")] public JSValue MonthCode => new JSString(CalendarDate.MonthCode.ToString());
    [JSExport("day")] public double DayValue => CalendarDate.Day;

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
        TemporalAdapter.RejectObjectWithCalendarOrTimeZone(obj);

        // ISODateToFields as a month-day, then PrepareCalendarFields of the partial bag in property order -
        // day, [era, eraYear,] month, monthCode, year - each converted as it is read; then the overflow option;
        // then CalendarMergeFields and CalendarMonthDayFromFields, the shared library's. A year only regulates
        // the day (row C12: its leap year is kept at any magnitude); it is never stored.
        var fields = TemporalAdapter.DateToFields(calendarId, IsoDate, CalendarFieldsType.MonthDay);
        var partial = new TemporalAdapter.CalendarFieldReader(obj, calendarId, "Temporal.PlainMonthDay");
        partial.DateFields();
        if (!partial.Any)
            throw JSEngine.NewTypeError("Temporal.PlainMonthDay.prototype.with requires at least one field");

        var overflow = ReadOverflow(a.GetAt(1));
        var date = TemporalAdapter.MonthDayFromFields(calendarId, TemporalAdapter.MergeFields(calendarId, fields, partial.Fields), overflow,
            "Temporal.PlainMonthDay");
        return new JSTemporalPlainMonthDay(date, calendarId, PlainMonthDayPrototype);
    }

    [JSExport("equals", Length = 1)]
    public JSValue Equals(in Arguments a)
    {
        var other = Require(ToTemporalMonthDay(a.GetAt(0)));
        return isoMonth == other.isoMonth && isoDay == other.isoDay && referenceISOYear == other.referenceISOYear
            && calendarId == other.calendarId
            ? BooleanTrue : BooleanFalse;
    }

    [JSExport("toPlainDate", Length = 1)]
    public JSValue ToPlainDate(in Arguments a)
    {
        if (a.GetAt(0) is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.PlainMonthDay.prototype.toPlainDate requires an object");

        // PrepareCalendarFields of the argument's « year » - with era and eraYear before it where the calendar
        // has eras - each converted as it is read; then the month-day's fields merged with them and
        // CalendarDateFromFields under constrain, the shared library's. The year is the calendar's, never an
        // ISO year (row C17).
        var fields = new TemporalAdapter.CalendarFieldReader(obj, calendarId, "Temporal.PlainMonthDay.prototype.toPlainDate");
        fields.Era();
        fields.Year();
        return new JSTemporalPlainDate(TemporalAdapter.MonthDayToDate(calendarId, IsoDate, fields.Fields), calendarId,
            JSTemporalPlainDate.PlainDatePrototype);
    }

    // TemporalMonthDayToString, the shared library's, with the calendarName option: the reference year is
    // written where the calendar is not iso8601 or the annotation is forced.
    [JSExport("toString", Length = 0)]
    public JSValue ToStringMethod(in Arguments a)
        => new JSString(TemporalFormat.MonthDayToString(IsoDate, calendarId, TemporalAdapter.ReadShowCalendarName(a.GetAt(0))));

    [JSExport("toJSON", Length = 0)]
    public JSValue ToJSON(in Arguments a) => new JSString(TemporalFormat.MonthDayToString(IsoDate, calendarId, ShowCalendarName.Auto));

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
        return (int)TemporalRoundingOptions.ToIntegerWithTruncation(value, "Temporal.PlainMonthDay: component must be finite");
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

        // GetTemporalCalendarIdentifierWithISODefault, then PrepareCalendarFields in property order - day,
        // [era, eraYear,] month, monthCode, year - each converted as it is read; then the overflow option; then
        // CalendarMonthDayFromFields, the shared library's: ISO 8601's reference year 1972, the month and day
        // regulated against the year given (row C12), and elsewhere the calendar's reference year (Table 6 and
        // the 1900 to 2035 rule, row C10), a year given checked against the limits first.
        var calendarId = TemporalAdapter.CalendarWithIsoDefault(obj);
        var fields = new TemporalAdapter.CalendarFieldReader(obj, calendarId, "Temporal.PlainMonthDay");
        fields.DateFields();

        var overflow = ReadOverflow(options);
        return new JSTemporalPlainMonthDay(TemporalAdapter.MonthDayFromFields(calendarId, fields.Fields, overflow, "Temporal.PlainMonthDay"),
            calendarId, PlainMonthDayPrototype);
    }

    // ParseISODateTime with the TemporalMonthDayString goal (the shared library's grammar, which refuses a
    // month-day without a year in a calendar other than iso8601), then the calendar annotation canonicalized.
    private static JSValue ParseTemporalMonthDayString(string text)
    {
        var parsed = TemporalAdapter.ParseIsoString(text, [TemporalGoal.MonthDay], "Temporal.PlainMonthDay");
        var calendarId = TemporalAdapter.CalendarOf(parsed);

        // The iso8601 calendar discards a parsed year and anchors against the leap-year reference (1972).
        if (parsed.Year is null || calendarId == "iso8601")
            return new JSTemporalPlainMonthDay(parsed.Month, parsed.Day, DefaultReferenceYear, calendarId, PlainMonthDayPrototype);

        // Any other calendar keeps the parsed date as its reference (ToTemporalMonthDay string path:
        // CreateTemporalMonthDay with referenceISOYear), so the date must be representable before
        // being projected to the calendar's month-day.
        var (fy, fm, fd) = ((int)parsed.Year.Value, parsed.Month, parsed.Day);
        if (!TemporalAdapter.IsoDateWithinLimits(fy, fm, fd))
            throw JSEngine.NewRangeError($"Cannot parse Temporal.PlainMonthDay from \"{text}\"");

        // ISODateToFields as a month-day, then CalendarMonthDayFromFields under constrain, the shared library's:
        // the calendar's own reference date for the parsed date's month code and day.
        return new JSTemporalPlainMonthDay(TemporalAdapter.MonthDayOfDate(calendarId, new IsoDate(fy, fm, fd)), calendarId,
            PlainMonthDayPrototype);
    }
}
