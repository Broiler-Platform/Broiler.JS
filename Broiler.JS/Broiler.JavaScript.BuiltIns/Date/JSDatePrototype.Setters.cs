using System;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Date;

public partial class JSDate
{
    // IsValid already performed ToNumber on the first component argument (which may
    // invoke valueOf). Multi-argument setters must reuse that coerced value for the
    // first slot instead of reading IntValue again — a second read would call
    // valueOf a second time, which the spec forbids (each argument is coerced
    // exactly once). The (int)(long) truncation reproduces JSValue.IntValue exactly,
    // including its low-32-bit wraparound for out-of-range / infinite operands.
    private static int CoercedIntValue(double coerced) => unchecked((int)(long)coerced);

    /// <summary>
    /// The setDate() method sets the day of the Date object relative to the beginning of the currently set month.
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
    [JSExport("setDate", Length = 1)]
    internal JSValue SetDate(in Arguments a)
    {
        var date = value;
        if (!IsValid(date, a.Get1(), out var diffValue))
            return JSNumber.NaN;

        try
        {
            value = date.AddDays(-date.Day + diffValue);
        }
        catch (ArgumentOutOfRangeException)
        {
            value = DateTimeOffset.MinValue;
        }

        return new JSNumber(value.ToJSDate());
    }

    [JSExport("setFullYear", Length = 3)]
    internal JSValue SetFullYear(in Arguments a)
    {
        // Capture validity from the snapshot BEFORE coercing the argument: a valueOf
        // side effect may revive the date, but the spec reads [[DateValue]] first.
        bool wasInvalid = !IsValidDate();
        var date = value;

        // setFullYear does not early-return on an invalid date: per the spec, when
        // the stored time value is NaN it is treated as +0 (so the call revives the
        // date) rather than returning NaN. ToNumber(year) is still performed (a
        // single valueOf), and only a NaN year makes the result NaN.
        double year = a.Get1().DoubleValue;

        if (wasInvalid)
        {
            // The revived date uses t = +0 (local). Compute purely with ECMAScript
            // date math so the year-1 boundary doesn't underflow .NET's range when
            // converting to/from a local-offset DateTimeOffset.
            var (_, _m, _d) = a.Get3();
            double t = 0;
            double mv = _m.IsUndefined ? JSDateMath.MonthFromTime(t) : _m.DoubleValue;
            double dv = _d.IsUndefined ? JSDateMath.DateFromTime(t) : _d.DoubleValue;
            if (double.IsNaN(year) || double.IsInfinity(year)
                || double.IsNaN(mv) || double.IsInfinity(mv)
                || double.IsNaN(dv) || double.IsInfinity(dv))
                return new JSNumber(SetTimeValue(double.NaN));

            double newDate = JSDateMath.MakeDate(JSDateMath.MakeDay((long)year, (long)mv, (long)dv), JSDateMath.TimeWithinDay(t));
            return new JSNumber(SetTimeValue(JSDateMath.TimeClip(JSDateMath.UTC(newDate))));
        }

        if (double.IsNaN(year))
            return new JSNumber(SetTimeValue(double.NaN));

        var (_year, _month, _day) = a.Get3();

        var month = _month.IsUndefined ? date.Month - 1 : _month.IntValue;
        var day = (_day.IsUndefined ? date.Day : _day.IntValue);

        // For years that .NET DateTimeOffset can handle (1–9999), use the fast path.
        if (year >= 1 && year <= 9999)
        {
            rawTimeMs = double.NaN; // clear any raw override

            try
            {
                date = new DateTimeOffset((int)year, 1, 1, date.Hour, date.Minute, date.Second, date.Millisecond, value.Offset);
                // Apply the month offset before the day offset: ECMAScript MakeDay
                // resolves the (year, month) to the first of that month and then adds
                // (date - 1) days, so day overflow rolls into the next month. Doing
                // AddDays first then AddMonths would let .NET's AddMonths clamp the
                // day-of-month to the target month's last valid day (e.g. setMonth(5,31)
                // would wrongly land on Jun 30 instead of Jul 1).
                date = date.AddMonths(month);
                date = date.AddDays(day - 1);
                value = date;
                // Disambiguate a real year-1 boundary date from the MinValue sentinel
                // that marks an invalid date (otherwise it would be read back as NaN).
                rawTimeMs = value == InvalidDate ? value.ToUniversalTime().ToUnixTimeMilliseconds() : double.NaN;
            }
            catch (ArgumentOutOfRangeException)
            {
                value = DateTimeOffset.MinValue;
            }

            return new JSNumber(GetTimeMs());
        }

        // For year 0 or negative years, use ECMAScript date math directly.
        // This handles the proleptic Gregorian calendar correctly.
        double timeWithinDay = JSDateMath.MakeTime(date.Hour, date.Minute, date.Second, date.Millisecond);
        double dayValue = JSDateMath.MakeDay((long)year, month, day);
        double utcMs = JSDateMath.UTC(JSDateMath.MakeDate(dayValue, timeWithinDay));
        double result = JSDateMath.TimeClip(utcMs);

        rawTimeMs = result;
        value = DateTimeOffset.MinValue;

        return new JSNumber(result);
    }

    [JSExport("setHours", Length = 4)]
    internal JSValue SetHours(in Arguments a)
    {
        var date = value;
        bool valid = IsValid(date, a.Get1(), out var hours);

        var (_hours, _mins, _seconds, _millis) = a.Get4();

        // Coerce every present argument (ToNumber, possibly invoking valueOf) BEFORE
        // returning on an invalid date: the spec performs all the ToNumber conversions
        // in order before the "if t is NaN, return NaN" step. Each value is read once
        // and reused via CoercedIntValue so valueOf is invoked exactly once per arg.
        double minsCoerced = _mins.IsUndefined ? double.NaN : _mins.DoubleValue;
        double secCoerced = _seconds.IsUndefined ? double.NaN : _seconds.DoubleValue;
        double msCoerced = _millis.IsUndefined ? double.NaN : _millis.DoubleValue;

        if (!valid)
            return JSNumber.NaN;

        var hrs = _hours.IsUndefined ? date.Hour : CoercedIntValue(hours);
        var mins = _mins.IsUndefined ? date.Minute : CoercedIntValue(minsCoerced);
        var seconds = _seconds.IsUndefined ? date.Second : CoercedIntValue(secCoerced);
        var millis = _millis.IsUndefined ? date.Millisecond : CoercedIntValue(msCoerced);

        try
        {
            date = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, 0, date.Offset);
            date = date.AddMilliseconds(millis);
            date = date.AddSeconds(seconds);
            date = date.AddMinutes(mins);
            date = date.AddHours(hrs);
            value = date;
        }
        catch (ArgumentOutOfRangeException)
        {
            value = DateTimeOffset.MinValue;
        }

        return new JSNumber(value.ToJSDate());
    }

    [JSExport("setMilliseconds", Length = 1)]
    internal JSValue SetMilliseconds(in Arguments a)
    {
        var date = value;
        if (!IsValid(date, a.Get1(), out var _millis))
            return JSNumber.NaN;

        try
        {
            date = new DateTimeOffset(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, 0, date.Offset);
            date = date.AddMilliseconds(_millis);
            value = date;
        }
        catch (ArgumentOutOfRangeException)
        {
            value = DateTimeOffset.MinValue;
        }

        return new JSNumber(value.ToJSDate());
    }

    [JSExport("setMinutes", Length = 3)]
    internal JSValue SetMinutes(in Arguments a)
    {
        var date = value;
        bool valid = IsValid(date, a.Get1(), out var minutes);

        var (_mins, _seconds, _millis) = a.Get3();

        // Coerce every present argument (ToNumber) before returning on an invalid
        // date so all valueOf side effects run, in order. See SetHours for rationale.
        double secCoerced = _seconds.IsUndefined ? double.NaN : _seconds.DoubleValue;
        double msCoerced = _millis.IsUndefined ? double.NaN : _millis.DoubleValue;

        if (!valid)
            return JSNumber.NaN;

        var mins = _mins.IsUndefined ? date.Minute : CoercedIntValue(minutes);
        var seconds = _seconds.IsUndefined ? date.Second : CoercedIntValue(secCoerced);
        var millis = _millis.IsUndefined ? date.Millisecond : CoercedIntValue(msCoerced);

        try
        {
            date = new DateTimeOffset(date.Year, date.Month, date.Day, date.Hour, 0, 0, 0, date.Offset);
            date = date.AddMilliseconds(millis);
            date = date.AddSeconds(seconds);
            date = date.AddMinutes(mins);
            value = date;
        }
        catch (ArgumentOutOfRangeException)
        {
            value = DateTimeOffset.MinValue;
        }

        return new JSNumber(value.ToJSDate());
    }

    [JSExport("setMonth", Length = 2)]
    internal JSValue SetMonth(in Arguments a)
    {
        var date = value;
        bool valid = IsValid(date, a.Get1(), out var mnth);

        var (_month, _days) = a.Get2();

        // Coerce a present date argument (ToNumber) before returning on an invalid
        // date so its valueOf side effect runs. See SetHours for rationale.
        double daysCoerced = _days.IsUndefined ? double.NaN : _days.DoubleValue;

        if (!valid)
            return JSNumber.NaN;

        var month = _month.IsUndefined ? date.Month : CoercedIntValue(mnth);
        var days = (_days.IsUndefined ? date.Day : CoercedIntValue(daysCoerced)) - 1;

        try
        {
            // Month offset before day offset so day overflow rolls into the next month
            // (ECMAScript MakeDay), instead of .NET AddMonths clamping the day to the
            // target month's last valid day. See SetFullYear for the detailed rationale.
            date = new DateTimeOffset(date.Year, 1, 1, date.Hour, date.Minute, date.Second, date.Millisecond, date.Offset);
            date = date.AddMonths(month);
            date = date.AddDays(days);
            value = date;

        }
        catch (ArgumentOutOfRangeException)
        {
            value = DateTimeOffset.MinValue;
        }

        return new JSNumber(value.ToJSDate());
    }

    [JSExport("setSeconds", Length = 2)]
    internal JSValue SetSeconds(in Arguments a)
    {
        var date = value;
        bool valid = IsValid(date, a.Get1(), out var secs);

        var (_seconds, _millis) = a.Get2();

        // Coerce a present ms argument (ToNumber) before returning on an invalid date
        // so its valueOf side effect runs, in order. See SetHours for rationale.
        double msCoerced = _millis.IsUndefined ? double.NaN : _millis.DoubleValue;

        if (!valid)
            return JSNumber.NaN;

        var seconds = _seconds.IsUndefined ? date.Second : CoercedIntValue(secs);
        var millis = _millis.IsUndefined ? date.Millisecond : CoercedIntValue(msCoerced);

        try
        {
            date = new DateTimeOffset(date.Year, date.Month, date.Day, date.Hour, date.Minute, 0, 0, date.Offset);
            date = date.AddMilliseconds(millis);
            date = date.AddSeconds(seconds);
            value = date;
        }
        catch (ArgumentOutOfRangeException)
        {
            value = DateTimeOffset.MinValue;
        }

        return new JSNumber(value.ToJSDate());
    }


    [JSExport("setTime", Length = 1)]
    internal JSValue SetTime(in Arguments a)
    {
        // setTime does not depend on the current [[DateValue]]: it always coerces
        // its argument (ToNumber) and stores TimeClip of the result, so it works
        // even when the receiver is currently an invalid date.
        double t = a.Get1().DoubleValue;
        return new JSNumber(SetTimeValue(JSDateMath.TimeClip(t)));
    }

    internal static JSValue SetYearLegacy(in Arguments a)
    {
        var date = a.This.AsJSDate();
        var yearValue = a.Get1().DoubleValue;
        var time = date.GetTimeMs();

        if (double.IsNaN(time))
            time = 0;
        else
            time = JSDateMath.LocalTime(time);

        if (double.IsNaN(yearValue) || double.IsInfinity(yearValue) || yearValue < long.MinValue || yearValue > long.MaxValue)
        {
            date.rawTimeMs = double.NaN;
            date.value = InvalidDate;
            return JSNumber.NaN;
        }

        var year = (long)Math.Truncate(yearValue);
        if (year >= 0 && year <= 99)
            year += 1900;

        var newDate = JSDateMath.MakeDate(
            JSDateMath.MakeDay(year, JSDateMath.MonthFromTime(time), JSDateMath.DateFromTime(time)),
            JSDateMath.TimeWithinDay(time));
        var result = JSDateMath.TimeClip(JSDateMath.UTC(newDate));

        if (double.IsNaN(result))
        {
            date.rawTimeMs = double.NaN;
            date.value = InvalidDate;
            return JSNumber.NaN;
        }

        var minMs = DateTimeOffset.MinValue.ToUnixTimeMilliseconds();
        var maxMs = DateTimeOffset.MaxValue.ToUnixTimeMilliseconds();

        if (result >= minMs && result <= maxMs)
        {
            date.rawTimeMs = double.NaN;
            date.value = DateTimeOffset.FromUnixTimeMilliseconds((long)result).ToLocalTime();
        }
        else
        {
            date.rawTimeMs = result;
            date.value = InvalidDate;
        }

        return new JSNumber(result);
    }

    [JSExport("setUTCDate", Length = 1)]
    internal JSValue setUTCDate(in Arguments a)
    {
        var date = value;
        if (!IsValid(date, a.Get1(), out var _date))
            return JSNumber.NaN;

        try
        {
            var offset = date.Offset;
            var utc = date.ToUniversalTime();

            utc = utc.AddDays(-utc.Day + _date);
            value = utc.ToOffset(offset);
        }
        catch (ArgumentOutOfRangeException)
        {
            value = DateTimeOffset.MinValue;
        }
        return new JSNumber(value.ToJSDate());
    }

    [JSExport("setUTCFullYear", Length = 3)]
    internal JSValue setUTCFullYear(in Arguments a)
    {
        // Operate on the ECMAScript time value (ms since epoch) so the full Date
        // range is supported, including years outside .NET's 1–9999 window. Read the
        // receiver's time value BEFORE coercing the arguments (their valueOf may
        // mutate this date). Like setFullYear, an invalid date is revived (t = +0).
        double t = GetTimeMs();

        // Coerce the arguments (ToNumber, possibly invoking valueOf) BEFORE the
        // "if t is NaN, return NaN" step, matching the spec's evaluation order.
        var (_year, _month, _day) = a.Get3();

        double yearValue = _year.DoubleValue;
        double monthArg = _month.IsUndefined ? double.NaN : _month.DoubleValue;
        double dayArg = _day.IsUndefined ? double.NaN : _day.DoubleValue;

        // setUTCFullYear revives an invalid date: when the stored time value is NaN
        // it is treated as +0 (spec step 4) rather than returning NaN.
        if (double.IsNaN(t))
            t = 0;

        double monthValue = _month.IsUndefined ? JSDateMath.MonthFromTime(t) : monthArg;
        double dayValue = _day.IsUndefined ? JSDateMath.DateFromTime(t) : dayArg;

        if (double.IsNaN(yearValue) || double.IsInfinity(yearValue)
            || double.IsNaN(monthValue) || double.IsInfinity(monthValue)
            || double.IsNaN(dayValue) || double.IsInfinity(dayValue))
        {
            return new JSNumber(SetTimeValue(double.NaN));
        }

        double newDay = JSDateMath.MakeDay((long)yearValue, (long)monthValue, (long)dayValue);
        double newDate = JSDateMath.MakeDate(newDay, JSDateMath.TimeWithinDay(t));
        return new JSNumber(SetTimeValue(JSDateMath.TimeClip(newDate)));
    }

    [JSExport("setUTCHours", Length = 4)]
    internal JSValue SetUTCHours(in Arguments a)
    {
        // Operate on the ECMAScript time value (ms since epoch) so dates outside
        // .NET's 1–9999 year range keep working. An already-invalid date stays NaN.
        double t = GetTimeMs();

        // Coerce the arguments (ToNumber, possibly invoking valueOf) BEFORE the
        // "if t is NaN, return NaN" step, matching the spec's evaluation order.
        var (_hours, _mins, _seconds, _millis) = a.Get4();

        double hrs = _hours.DoubleValue;
        double minsArg = _mins.IsUndefined ? double.NaN : _mins.DoubleValue;
        double secondsArg = _seconds.IsUndefined ? double.NaN : _seconds.DoubleValue;
        double millisArg = _millis.IsUndefined ? double.NaN : _millis.DoubleValue;

        if (double.IsNaN(t))
            return JSNumber.NaN;

        double mins = _mins.IsUndefined ? JSDateMath.MinFromTime(t) : minsArg;
        double seconds = _seconds.IsUndefined ? JSDateMath.SecFromTime(t) : secondsArg;
        double millis = _millis.IsUndefined ? JSDateMath.MsFromTime(t) : millisArg;

        if (double.IsNaN(hrs) || double.IsInfinity(hrs)
            || double.IsNaN(mins) || double.IsInfinity(mins)
            || double.IsNaN(seconds) || double.IsInfinity(seconds)
            || double.IsNaN(millis) || double.IsInfinity(millis))
        {
            return new JSNumber(SetTimeValue(double.NaN));
        }

        double newTime = JSDateMath.MakeTime(hrs, mins, seconds, millis);
        double newDate = JSDateMath.MakeDate(JSDateMath.Day(t), newTime);
        return new JSNumber(SetTimeValue(JSDateMath.TimeClip(newDate)));
    }

    [JSExport("setUTCMilliseconds", Length = 1)]
    internal JSValue SetUTCMilliseconds(in Arguments a)
    {
        var date = value;
        if (!IsValid(date, a.Get1(), out var _millis))
            return JSNumber.NaN;

        var offset = date.Offset;
        var utc = date.ToUniversalTime();

        try
        {
            utc = new DateTimeOffset(utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute, utc.Second, 0, utc.Offset);
            utc = utc.AddMilliseconds(_millis);

            value = utc.ToOffset(offset);
        }
        catch (ArgumentOutOfRangeException)
        {
            value = DateTimeOffset.MinValue;
        }

        return new JSNumber(value.ToJSDate());
    }

    [JSExport("setUTCMinutes", Length = 3)]
    internal JSValue SetUTCMinutes(in Arguments a)
    {
        var date = value;
        bool valid = IsValid(date, a.Get1(), out var minutes);

        var offset = date.Offset;
        var utc = date.ToUniversalTime();

        var (_mins, _seconds, _millis) = a.Get3();

        // Coerce every present argument (ToNumber) before returning on an invalid
        // date so all valueOf side effects run, in order. See SetHours for rationale.
        double secCoerced = _seconds.IsUndefined ? double.NaN : _seconds.DoubleValue;
        double msCoerced = _millis.IsUndefined ? double.NaN : _millis.DoubleValue;

        if (!valid)
            return JSNumber.NaN;

        var mins = _mins.IsUndefined ? utc.Minute : CoercedIntValue(minutes);
        var seconds = _seconds.IsUndefined ? utc.Second : CoercedIntValue(secCoerced);
        var millis = _millis.IsUndefined ? utc.Millisecond : CoercedIntValue(msCoerced);

        try
        {
            utc = new DateTimeOffset(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, 0, utc.Offset);
            utc = utc.AddMilliseconds(millis);
            utc = utc.AddSeconds(seconds);
            utc = utc.AddMinutes(mins);

            value = utc.ToOffset(offset);
        }
        catch (ArgumentOutOfRangeException)
        {
            value = DateTimeOffset.MinValue;
        }

        return new JSNumber(value.ToJSDate());
    }

    [JSExport("setUTCMonth", Length = 2)]
    internal JSValue SetUTCMonth(in Arguments a)
    {
        var date = value;
        bool valid = IsValid(date, a.Get1(), out var mnth);

        var offset = date.Offset;
        var utc = date.ToUniversalTime();

        var (_month, _days) = a.Get2();

        // Coerce a present date argument (ToNumber) before returning on an invalid
        // date so its valueOf side effect runs. See SetHours for rationale.
        double daysCoerced = _days.IsUndefined ? double.NaN : _days.DoubleValue;

        if (!valid)
            return JSNumber.NaN;

        var month = _month.IsUndefined ? utc.Month : CoercedIntValue(mnth);
        var days = (_days.IsUndefined ? utc.Day : CoercedIntValue(daysCoerced)) - 1;

        try
        {
            // Month offset before day offset so day overflow rolls into the next month
            // (ECMAScript MakeDay), instead of .NET AddMonths clamping. See SetFullYear.
            utc = new DateTimeOffset(utc.Year, 1, 1, utc.Hour, utc.Minute, utc.Second, utc.Millisecond, utc.Offset);
            utc = utc.AddMonths(month);
            utc = utc.AddDays(days);

            value = utc.ToOffset(offset);
        }
        catch (ArgumentOutOfRangeException)
        {
            value = DateTimeOffset.MinValue;
        }

        return new JSNumber(value.ToJSDate());
    }

    [JSExport("setUTCSeconds", Length = 2)]
    internal JSValue SetUTCSeconds(in Arguments a)
    {
        var date = value;
        bool valid = IsValid(date, a.Get1(), out var secs);

        var offset = date.Offset;
        var utc = date.ToUniversalTime();

        var (_seconds, _millis) = a.Get2();

        // Coerce a present ms argument (ToNumber) before returning on an invalid date
        // so its valueOf side effect runs, in order. See SetHours for rationale.
        double msCoerced = _millis.IsUndefined ? double.NaN : _millis.DoubleValue;

        if (!valid)
            return JSNumber.NaN;

        var seconds = _seconds.IsUndefined ? utc.Second : CoercedIntValue(secs);
        var millis = _millis.IsUndefined ? utc.Millisecond : CoercedIntValue(msCoerced);

        try
        {
            utc = new DateTimeOffset(utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute, 0, 0, utc.Offset);
            utc = utc.AddMilliseconds(millis);
            utc = utc.AddSeconds(seconds);

            value = utc.ToOffset(offset);
        }
        catch (ArgumentOutOfRangeException)
        {
            value = DateTimeOffset.MinValue;
        }

        return new JSNumber(value.ToJSDate());
    }
}
