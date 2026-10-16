using Broiler.ECMAScript.Time;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Date;

// The setters of ECMA-262 21.4.4 and setYear (B.2.3.2). Each reads the time value first, then converts each
// argument it is given, once and in order, whatever the time value is, and only then answers NaN for an
// invalid date (setFullYear, setUTCFullYear and setYear read one as +0). The arithmetic is the shared
// library's, a local setter works on LocalTime(t) and stores TimeClip(UTC(date)) over the host's zone, and
// the date is written last.
public partial class JSDate
{
    // An argument "is present" (21.4.4) when the call passes it, even as undefined, whose ToNumber is NaN.
    private static bool Specified(in Arguments a, int index) => a.Length > index;

    // ToNumber of a present argument; NaN, never read, for an absent one.
    private static double Present(in Arguments a, int index) => Specified(a, index) ? a.GetAt(index).DoubleValue : double.NaN;

    // Writes the date's new time value and answers it.
    private JSValue Store(double time)
    {
        timeValue = time;
        return NumberOf(time);
    }

    // The local time to set fields of: LocalTime(t) over the host's zone.
    private static double Local(double t) => EcmaLocalTime.LocalTime(t, Zone);

    // A local date's time value: TimeClip(UTC(date)) over the host's zone.
    private static double FromLocal(double date) => EcmaTime.TimeClip(EcmaLocalTime.Utc(date, Zone));

    // setDate (21.4.4.20).
    [JSExport("setDate", Length = 1)]
    internal JSValue SetDate(in Arguments a)
    {
        var t = timeValue;
        var dt = a.Get1().DoubleValue;
        if (double.IsNaN(t))
            return JSNumber.NaN;

        t = Local(t);
        var newDate = EcmaTime.MakeDate(
            EcmaTime.MakeDay(EcmaTime.YearFromTime(t), EcmaTime.MonthFromTime(t), dt),
            EcmaTime.TimeWithinDay(t));
        return Store(FromLocal(newDate));
    }

    // setFullYear (21.4.4.21): an invalid date is read as +0.
    [JSExport("setFullYear", Length = 3)]
    internal JSValue SetFullYear(in Arguments a)
    {
        var t = timeValue;
        var y = a.Get1().DoubleValue;
        t = double.IsNaN(t) ? 0 : Local(t);
        var m = Specified(a, 1) ? a.GetAt(1).DoubleValue : EcmaTime.MonthFromTime(t);
        var dt = Specified(a, 2) ? a.GetAt(2).DoubleValue : EcmaTime.DateFromTime(t);
        var newDate = EcmaTime.MakeDate(EcmaTime.MakeDay(y, m, dt), EcmaTime.TimeWithinDay(t));
        return Store(FromLocal(newDate));
    }

    // setHours (21.4.4.22).
    [JSExport("setHours", Length = 4)]
    internal JSValue SetHours(in Arguments a)
    {
        var t = timeValue;
        var h = a.Get1().DoubleValue;
        var m = Present(a, 1);
        var s = Present(a, 2);
        var milli = Present(a, 3);
        if (double.IsNaN(t))
            return JSNumber.NaN;

        t = Local(t);
        if (!Specified(a, 1))
            m = EcmaTime.MinFromTime(t);
        if (!Specified(a, 2))
            s = EcmaTime.SecFromTime(t);
        if (!Specified(a, 3))
            milli = EcmaTime.MsFromTime(t);

        var date = EcmaTime.MakeDate(EcmaTime.Day(t), EcmaTime.MakeTime(h, m, s, milli));
        return Store(FromLocal(date));
    }

    // setMilliseconds (21.4.4.23).
    [JSExport("setMilliseconds", Length = 1)]
    internal JSValue SetMilliseconds(in Arguments a)
    {
        var t = timeValue;
        var ms = a.Get1().DoubleValue;
        if (double.IsNaN(t))
            return JSNumber.NaN;

        t = Local(t);
        var time = EcmaTime.MakeTime(EcmaTime.HourFromTime(t), EcmaTime.MinFromTime(t), EcmaTime.SecFromTime(t), ms);
        return Store(FromLocal(EcmaTime.MakeDate(EcmaTime.Day(t), time)));
    }

    // setMinutes (21.4.4.24).
    [JSExport("setMinutes", Length = 3)]
    internal JSValue SetMinutes(in Arguments a)
    {
        var t = timeValue;
        var m = a.Get1().DoubleValue;
        var s = Present(a, 1);
        var milli = Present(a, 2);
        if (double.IsNaN(t))
            return JSNumber.NaN;

        t = Local(t);
        if (!Specified(a, 1))
            s = EcmaTime.SecFromTime(t);
        if (!Specified(a, 2))
            milli = EcmaTime.MsFromTime(t);

        var date = EcmaTime.MakeDate(EcmaTime.Day(t), EcmaTime.MakeTime(EcmaTime.HourFromTime(t), m, s, milli));
        return Store(FromLocal(date));
    }

    // setMonth (21.4.4.25).
    [JSExport("setMonth", Length = 2)]
    internal JSValue SetMonth(in Arguments a)
    {
        var t = timeValue;
        var m = a.Get1().DoubleValue;
        var dt = Present(a, 1);
        if (double.IsNaN(t))
            return JSNumber.NaN;

        t = Local(t);
        if (!Specified(a, 1))
            dt = EcmaTime.DateFromTime(t);

        var newDate = EcmaTime.MakeDate(EcmaTime.MakeDay(EcmaTime.YearFromTime(t), m, dt), EcmaTime.TimeWithinDay(t));
        return Store(FromLocal(newDate));
    }

    // setSeconds (21.4.4.26).
    [JSExport("setSeconds", Length = 2)]
    internal JSValue SetSeconds(in Arguments a)
    {
        var t = timeValue;
        var s = a.Get1().DoubleValue;
        var milli = Present(a, 1);
        if (double.IsNaN(t))
            return JSNumber.NaN;

        t = Local(t);
        if (!Specified(a, 1))
            milli = EcmaTime.MsFromTime(t);

        var date = EcmaTime.MakeDate(
            EcmaTime.Day(t),
            EcmaTime.MakeTime(EcmaTime.HourFromTime(t), EcmaTime.MinFromTime(t), s, milli));
        return Store(FromLocal(date));
    }

    // setTime (21.4.4.27): TimeClip of the argument, whatever the date held.
    [JSExport("setTime", Length = 1)]
    internal JSValue SetTime(in Arguments a) => Store(EcmaTime.TimeClip(a.Get1().DoubleValue));

    // setYear (B.2.3.2): an invalid date is read as +0, and the year is MakeFullYear's.
    internal static JSValue SetYearLegacy(in Arguments a)
    {
        var date = a.This.AsJSDate();
        var t = date.timeValue;
        var y = a.Get1().DoubleValue;
        t = double.IsNaN(t) ? 0 : Local(t);
        var d = EcmaTime.MakeDay(EcmaTime.MakeFullYear(y), EcmaTime.MonthFromTime(t), EcmaTime.DateFromTime(t));
        return date.Store(FromLocal(EcmaTime.MakeDate(d, EcmaTime.TimeWithinDay(t))));
    }

    // setUTCDate (21.4.4.28).
    [JSExport("setUTCDate", Length = 1)]
    internal JSValue setUTCDate(in Arguments a)
    {
        var t = timeValue;
        var dt = a.Get1().DoubleValue;
        if (double.IsNaN(t))
            return JSNumber.NaN;

        var newDate = EcmaTime.MakeDate(
            EcmaTime.MakeDay(EcmaTime.YearFromTime(t), EcmaTime.MonthFromTime(t), dt),
            EcmaTime.TimeWithinDay(t));
        return Store(EcmaTime.TimeClip(newDate));
    }

    // setUTCFullYear (21.4.4.29): an invalid date is read as +0.
    [JSExport("setUTCFullYear", Length = 3)]
    internal JSValue setUTCFullYear(in Arguments a)
    {
        var t = timeValue;
        if (double.IsNaN(t))
            t = 0;

        var y = a.Get1().DoubleValue;
        var m = Specified(a, 1) ? a.GetAt(1).DoubleValue : EcmaTime.MonthFromTime(t);
        var dt = Specified(a, 2) ? a.GetAt(2).DoubleValue : EcmaTime.DateFromTime(t);
        var newDate = EcmaTime.MakeDate(EcmaTime.MakeDay(y, m, dt), EcmaTime.TimeWithinDay(t));
        return Store(EcmaTime.TimeClip(newDate));
    }

    // setUTCHours (21.4.4.30).
    [JSExport("setUTCHours", Length = 4)]
    internal JSValue SetUTCHours(in Arguments a)
    {
        var t = timeValue;
        var h = a.Get1().DoubleValue;
        var m = Present(a, 1);
        var s = Present(a, 2);
        var milli = Present(a, 3);
        if (double.IsNaN(t))
            return JSNumber.NaN;

        if (!Specified(a, 1))
            m = EcmaTime.MinFromTime(t);
        if (!Specified(a, 2))
            s = EcmaTime.SecFromTime(t);
        if (!Specified(a, 3))
            milli = EcmaTime.MsFromTime(t);

        var date = EcmaTime.MakeDate(EcmaTime.Day(t), EcmaTime.MakeTime(h, m, s, milli));
        return Store(EcmaTime.TimeClip(date));
    }

    // setUTCMilliseconds (21.4.4.31).
    [JSExport("setUTCMilliseconds", Length = 1)]
    internal JSValue SetUTCMilliseconds(in Arguments a)
    {
        var t = timeValue;
        var ms = a.Get1().DoubleValue;
        if (double.IsNaN(t))
            return JSNumber.NaN;

        var time = EcmaTime.MakeTime(EcmaTime.HourFromTime(t), EcmaTime.MinFromTime(t), EcmaTime.SecFromTime(t), ms);
        return Store(EcmaTime.TimeClip(EcmaTime.MakeDate(EcmaTime.Day(t), time)));
    }

    // setUTCMinutes (21.4.4.32).
    [JSExport("setUTCMinutes", Length = 3)]
    internal JSValue SetUTCMinutes(in Arguments a)
    {
        var t = timeValue;
        var m = a.Get1().DoubleValue;
        var s = Present(a, 1);
        var milli = Present(a, 2);
        if (double.IsNaN(t))
            return JSNumber.NaN;

        if (!Specified(a, 1))
            s = EcmaTime.SecFromTime(t);
        if (!Specified(a, 2))
            milli = EcmaTime.MsFromTime(t);

        var date = EcmaTime.MakeDate(EcmaTime.Day(t), EcmaTime.MakeTime(EcmaTime.HourFromTime(t), m, s, milli));
        return Store(EcmaTime.TimeClip(date));
    }

    // setUTCMonth (21.4.4.33).
    [JSExport("setUTCMonth", Length = 2)]
    internal JSValue SetUTCMonth(in Arguments a)
    {
        var t = timeValue;
        var m = a.Get1().DoubleValue;
        var dt = Present(a, 1);
        if (double.IsNaN(t))
            return JSNumber.NaN;

        if (!Specified(a, 1))
            dt = EcmaTime.DateFromTime(t);

        var newDate = EcmaTime.MakeDate(EcmaTime.MakeDay(EcmaTime.YearFromTime(t), m, dt), EcmaTime.TimeWithinDay(t));
        return Store(EcmaTime.TimeClip(newDate));
    }

    // setUTCSeconds (21.4.4.34).
    [JSExport("setUTCSeconds", Length = 2)]
    internal JSValue SetUTCSeconds(in Arguments a)
    {
        var t = timeValue;
        var s = a.Get1().DoubleValue;
        var milli = Present(a, 1);
        if (double.IsNaN(t))
            return JSNumber.NaN;

        if (!Specified(a, 1))
            milli = EcmaTime.MsFromTime(t);

        var date = EcmaTime.MakeDate(
            EcmaTime.Day(t),
            EcmaTime.MakeTime(EcmaTime.HourFromTime(t), EcmaTime.MinFromTime(t), s, milli));
        return Store(EcmaTime.TimeClip(date));
    }
}
