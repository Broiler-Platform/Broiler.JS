using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// Stubs for the remaining Temporal types. Each constructor exists (so `typeof
// Temporal.PlainDate === "function"` and feature detection work) but throws until the
// type is implemented. They are attached to the Temporal namespace by
// BuiltInsAssemblyInitializer.PatchTemporal (Register = false → not globals).
//
// Temporal.Duration, Temporal.Instant, Temporal.PlainDate and Temporal.PlainTime are
// implemented; the calendar- and time-zone-dependent types below are the remaining work.

// Temporal.PlainDate is implemented in JSTemporalPlainDate.cs.
// Temporal.PlainTime is implemented in JSTemporalPlainTime.cs.

// TODO: Temporal.PlainDateTime (proposal §5) — a date + wall-clock time, no zone.
//   Constructor(isoYear, isoMonth, isoDay, hour, …, ns [, calendar]); from/compare.
//   Combines PlainDate + PlainTime accessors/methods; needs calendar arithmetic.
[JSClassGenerator("PlainDateTime", Register = false)]
public partial class JSTemporalPlainDateTime : JSObject
{
    [JSExport(Length = 3)]
    public JSTemporalPlainDateTime(in Arguments a) : base(JSEngine.NewTargetPrototype)
        => throw JSEngine.NewError("Temporal.PlainDateTime is not yet implemented");
}

// TODO: Temporal.PlainYearMonth (proposal §9) — a calendar year+month (e.g. "2024-06").
//   Constructor(isoYear, isoMonth [, calendar, referenceISODay]); from/compare.
//   Accessors: year, month, monthCode, calendarId, daysInMonth/Year, monthsInYear,
//     inLeapYear. Methods: with, add, subtract, until, since, equals, toPlainDate,
//     toString/toJSON/toLocaleString, valueOf (throws).
[JSClassGenerator("PlainYearMonth", Register = false)]
public partial class JSTemporalPlainYearMonth : JSObject
{
    [JSExport(Length = 2)]
    public JSTemporalPlainYearMonth(in Arguments a) : base(JSEngine.NewTargetPrototype)
        => throw JSEngine.NewError("Temporal.PlainYearMonth is not yet implemented");
}

// TODO: Temporal.PlainMonthDay (proposal §10) — a calendar month+day (e.g. "--06-15").
//   Constructor(isoMonth, isoDay [, calendar, referenceISOYear]); from.
//   Accessors: monthCode, day, calendarId. Methods: with, equals, toPlainDate,
//     toString/toJSON/toLocaleString, valueOf (throws).
[JSClassGenerator("PlainMonthDay", Register = false)]
public partial class JSTemporalPlainMonthDay : JSObject
{
    [JSExport(Length = 2)]
    public JSTemporalPlainMonthDay(in Arguments a) : base(JSEngine.NewTargetPrototype)
        => throw JSEngine.NewError("Temporal.PlainMonthDay is not yet implemented");
}

// TODO: Temporal.ZonedDateTime (proposal §6) — an exact instant in a named time zone with
//   a calendar. The most complex type: needs a time-zone database (offset transitions /
//   DST), GetNamedTimeZoneOffsetNanoseconds, disambiguation, and full calendar arithmetic.
//   Constructor(epochNanoseconds, timeZone [, calendar]); from/compare; the union of
//   Instant + PlainDateTime accessors plus offset/timeZoneId/hoursInDay and the
//   with*/round/until/since/start-of-day/toInstant/toPlainDate(Time) methods.
[JSClassGenerator("ZonedDateTime", Register = false)]
public partial class JSTemporalZonedDateTime : JSObject
{
    [JSExport(Length = 2)]
    public JSTemporalZonedDateTime(in Arguments a) : base(JSEngine.NewTargetPrototype)
        => throw JSEngine.NewError("Temporal.ZonedDateTime is not yet implemented");
}
