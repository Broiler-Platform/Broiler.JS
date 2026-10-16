using Broiler.ECMAScript.Time;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Date;

// The getters of ECMA-262 21.4.4: each reads the time value, or LocalTime of it over the host's zone,
// through the shared library's field operations; NaN for an invalid date.
public partial class JSDate
{
    // LocalTime(t) (21.4.1.25) of this date's time value; NaN for an invalid date.
    private double LocalTimeValue => EcmaLocalTime.LocalTime(timeValue, Zone);

    // A Number result, the shared NaN for NaN.
    private static JSValue NumberOf(double value) => double.IsNaN(value) ? JSNumber.NaN : new JSNumber(value);

    [JSExport("getDate", Length = 0)]
    internal JSValue GetDate(in Arguments a) => NumberOf(EcmaTime.DateFromTime(LocalTimeValue));

    [JSExport("getDay", Length = 0)]
    internal JSValue GetDay(in Arguments a) => NumberOf(EcmaTime.WeekDay(LocalTimeValue));

    [JSExport("getFullYear", Length = 0)]
    internal JSValue GetFullYear(in Arguments a) => NumberOf(EcmaTime.YearFromTime(LocalTimeValue));

    // getYear (B.2.3.1): YearFromTime(LocalTime(t)) - 1900.
    [JSExport("getYear", Length = 0)]
    internal JSValue GetYear(in Arguments a) => NumberOf(EcmaTime.YearFromTime(LocalTimeValue) - 1900);

    internal static JSValue GetYearLegacy(in Arguments a)
    {
        if (a.This is JSDate date)
            return date.GetYear(in a);

        throw JSEngine.NewTypeError("Date.prototype.getYear called on incompatible receiver");
    }

    [JSExport("getHours", Length = 0)]
    internal JSValue GetHours(in Arguments a) => NumberOf(EcmaTime.HourFromTime(LocalTimeValue));

    [JSExport("getMilliseconds", Length = 0)]
    internal JSValue GetMilliSeconds(in Arguments a) => NumberOf(EcmaTime.MsFromTime(LocalTimeValue));

    [JSExport("getMinutes", Length = 0)]
    internal JSValue GetMinutes(in Arguments a) => NumberOf(EcmaTime.MinFromTime(LocalTimeValue));

    [JSExport("getMonth", Length = 0)]
    internal JSValue GetMonth(in Arguments a) => NumberOf(EcmaTime.MonthFromTime(LocalTimeValue));

    [JSExport("getSeconds", Length = 0)]
    internal JSValue GetSeconds(in Arguments a) => NumberOf(EcmaTime.SecFromTime(LocalTimeValue));

    [JSExport("getTime", Length = 0)]
    internal JSValue GetTime(in Arguments a) => NumberOf(timeValue);

    // getTimezoneOffset (21.4.4.11): (t - LocalTime(t)) / msPerMinute.
    [JSExport("getTimezoneOffset", Length = 0)]
    internal JSValue GetTimezoneOffset(in Arguments a) => NumberOf((timeValue - LocalTimeValue) / EcmaTime.MsPerMinute);

    [JSExport("getUTCDate", Length = 0)]
    internal JSValue GetUTCDate(in Arguments a) => NumberOf(EcmaTime.DateFromTime(timeValue));

    [JSExport("getUTCDay", Length = 0)]
    internal JSValue GetUTCDay(in Arguments a) => NumberOf(EcmaTime.WeekDay(timeValue));

    [JSExport("getUTCFullYear", Length = 0)]
    internal JSValue GetUTCFullYear(in Arguments a) => NumberOf(EcmaTime.YearFromTime(timeValue));

    [JSExport("getUTCHours", Length = 0)]
    internal JSValue GetUTCHours(in Arguments a) => NumberOf(EcmaTime.HourFromTime(timeValue));

    [JSExport("getUTCMilliseconds", Length = 0)]
    internal JSValue GetUTCMilliseconds(in Arguments a) => NumberOf(EcmaTime.MsFromTime(timeValue));

    [JSExport("getUTCMinutes", Length = 0)]
    internal JSValue GetUTCMinutes(in Arguments a) => NumberOf(EcmaTime.MinFromTime(timeValue));

    [JSExport("getUTCMonth", Length = 0)]
    internal JSValue GetUTCMonth(in Arguments a) => NumberOf(EcmaTime.MonthFromTime(timeValue));

    [JSExport("getUTCSeconds", Length = 0)]
    internal JSValue GetUTCSeconds(in Arguments a) => NumberOf(EcmaTime.SecFromTime(timeValue));
}
