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

    // A trailing optional component argument that is *present* — even when its value
    // is `undefined` — is "specified" per the spec (the spec keys "if X is not
    // specified" off the argument count, not off undefined-ness). A specified arg is
    // coerced with ToNumber (so undefined → NaN), and a non-finite specified
    // component makes MakeDate/MakeTime → NaN → TimeClip → NaN. Only a genuinely
    // absent argument falls back to the current field value.
    private static bool Specified(in Arguments a, int index) => a.Length > index;

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
            double mv = Specified(a, 1) ? _m.DoubleValue : JSDateMath.MonthFromTime(t);
            double dv = Specified(a, 2) ? _d.DoubleValue : JSDateMath.DateFromTime(t);
            if (double.IsNaN(year) || double.IsInfinity(year)
                || double.IsNaN(mv) || double.IsInfinity(mv)
                || double.IsNaN(dv) || double.IsInfinity(dv))
                return new JSNumber(SetTimeValue(double.NaN));

            double newDate = JSDateMath.MakeDate(JSDateMath.MakeDay(Math.Truncate(year), Math.Truncate(mv), Math.Truncate(dv)), JSDateMath.TimeWithinDay(t));
            return new JSNumber(SetTimeValue(JSDateMath.TimeClip(JSDateMath.UTC(newDate))));
        }

        // Decompose the current time value into its local month/day and
        // time-within-day fields. When the date falls outside .NET's
        // DateTimeOffset range it is stored in rawTimeMs (with `value` pinned to
        // the MinValue placeholder), so the defaults for an omitted month/date and
        // the preserved time-of-day must come from the ECMAScript local-time
        // decomposition of rawTimeMs rather than from the `value` placeholder —
        // otherwise a follow-up setFullYear on a year-0/negative date would reset
        // the month, day and time to the placeholder's (Jan 1, 00:00:00).
        int curMonth0, curDay, curHour, curMin, curSec, curMs;
        if (double.IsNaN(rawTimeMs))
        {
            curMonth0 = date.Month - 1;
            curDay = date.Day;
            curHour = date.Hour;
            curMin = date.Minute;
            curSec = date.Second;
            curMs = date.Millisecond;
        }
        else
        {
            double localMs = JSDateMath.LocalTime(rawTimeMs);
            curMonth0 = JSDateMath.MonthFromTime(localMs);
            curDay = JSDateMath.DateFromTime(localMs);
            curHour = JSDateMath.HourFromTime(localMs);
            curMin = JSDateMath.MinFromTime(localMs);
            curSec = JSDateMath.SecFromTime(localMs);
            curMs = JSDateMath.MsFromTime(localMs);
        }

        // Coerce month then day (ToNumber, valueOf once each) before the NaN-year
        // check so their side effects run in spec order. A present-but-undefined
        // optional arg is "specified" → ToNumber(undefined)=NaN → result NaN.
        var (_year, _month, _day) = a.Get3();
        bool monthGiven = Specified(a, 1), dayGiven = Specified(a, 2);
        double monthCoerced = monthGiven ? _month.DoubleValue : double.NaN;
        double dayCoerced = dayGiven ? _day.DoubleValue : double.NaN;

        if (double.IsNaN(year)
            || (monthGiven && !double.IsFinite(monthCoerced))
            || (dayGiven && !double.IsFinite(dayCoerced)))
            return new JSNumber(SetTimeValue(double.NaN));

        var month = monthGiven ? CoercedIntValue(monthCoerced) : curMonth0;
        var day = dayGiven ? CoercedIntValue(dayCoerced) : curDay;

        // For years that .NET DateTimeOffset can handle (1–9999), use the fast path.
        if (year >= 1 && year <= 9999)
        {
            rawTimeMs = double.NaN; // clear any raw override

            try
            {
                date = new DateTimeOffset((int)year, 1, 1, curHour, curMin, curSec, curMs, value.Offset);
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
        double timeWithinDay = JSDateMath.MakeTime(curHour, curMin, curSec, curMs);
        double dayValue = JSDateMath.MakeDay(Math.Truncate(year), month, day);
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
        bool minsGiven = Specified(a, 1), secGiven = Specified(a, 2), msGiven = Specified(a, 3);
        double minsCoerced = minsGiven ? _mins.DoubleValue : double.NaN;
        double secCoerced = secGiven ? _seconds.DoubleValue : double.NaN;
        double msCoerced = msGiven ? _millis.DoubleValue : double.NaN;

        if (!valid)
            return JSNumber.NaN;

        // A specified-but-non-finite component (e.g. an explicit `undefined`) → NaN.
        if ((minsGiven && !double.IsFinite(minsCoerced)) || (secGiven && !double.IsFinite(secCoerced)) || (msGiven && !double.IsFinite(msCoerced)))
            return new JSNumber(SetTimeValue(double.NaN));

        var hrs = CoercedIntValue(hours);
        var mins = minsGiven ? CoercedIntValue(minsCoerced) : date.Minute;
        var seconds = secGiven ? CoercedIntValue(secCoerced) : date.Second;
        var millis = msGiven ? CoercedIntValue(msCoerced) : date.Millisecond;

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
        bool secGiven = Specified(a, 1), msGiven = Specified(a, 2);
        double secCoerced = secGiven ? _seconds.DoubleValue : double.NaN;
        double msCoerced = msGiven ? _millis.DoubleValue : double.NaN;

        if (!valid)
            return JSNumber.NaN;

        if ((secGiven && !double.IsFinite(secCoerced)) || (msGiven && !double.IsFinite(msCoerced)))
            return new JSNumber(SetTimeValue(double.NaN));

        var mins = CoercedIntValue(minutes);
        var seconds = secGiven ? CoercedIntValue(secCoerced) : date.Second;
        var millis = msGiven ? CoercedIntValue(msCoerced) : date.Millisecond;

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
        bool daysGiven = Specified(a, 1);
        double daysCoerced = daysGiven ? _days.DoubleValue : double.NaN;

        if (!valid)
            return JSNumber.NaN;

        if (daysGiven && !double.IsFinite(daysCoerced))
            return new JSNumber(SetTimeValue(double.NaN));

        var month = CoercedIntValue(mnth);
        var days = (daysGiven ? CoercedIntValue(daysCoerced) : date.Day) - 1;

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
        bool msGiven = Specified(a, 1);
        double msCoerced = msGiven ? _millis.DoubleValue : double.NaN;

        if (!valid)
            return JSNumber.NaN;

        if (msGiven && !double.IsFinite(msCoerced))
            return new JSNumber(SetTimeValue(double.NaN));

        var seconds = CoercedIntValue(secs);
        var millis = msGiven ? CoercedIntValue(msCoerced) : date.Millisecond;

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

        bool monthGiven = Specified(a, 1), dayGiven = Specified(a, 2);
        double yearValue = _year.DoubleValue;
        double monthArg = monthGiven ? _month.DoubleValue : double.NaN;
        double dayArg = dayGiven ? _day.DoubleValue : double.NaN;

        // setUTCFullYear revives an invalid date: when the stored time value is NaN
        // it is treated as +0 (spec step 4) rather than returning NaN.
        if (double.IsNaN(t))
            t = 0;

        double monthValue = monthGiven ? monthArg : JSDateMath.MonthFromTime(t);
        double dayValue = dayGiven ? dayArg : JSDateMath.DateFromTime(t);

        if (double.IsNaN(yearValue) || double.IsInfinity(yearValue)
            || double.IsNaN(monthValue) || double.IsInfinity(monthValue)
            || double.IsNaN(dayValue) || double.IsInfinity(dayValue))
        {
            return new JSNumber(SetTimeValue(double.NaN));
        }

        // Pass the truncated-but-full-magnitude doubles to MakeDay (which guards the
        // out-of-range case); a (long) cast would wrap a huge year such as MAX_VALUE.
        double newDay = JSDateMath.MakeDay(Math.Truncate(yearValue), Math.Truncate(monthValue), Math.Truncate(dayValue));
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

        bool minsGiven = Specified(a, 1), secGiven = Specified(a, 2), msGiven = Specified(a, 3);
        double hrs = _hours.DoubleValue;
        double minsArg = minsGiven ? _mins.DoubleValue : double.NaN;
        double secondsArg = secGiven ? _seconds.DoubleValue : double.NaN;
        double millisArg = msGiven ? _millis.DoubleValue : double.NaN;

        if (double.IsNaN(t))
            return JSNumber.NaN;

        double mins = minsGiven ? minsArg : JSDateMath.MinFromTime(t);
        double seconds = secGiven ? secondsArg : JSDateMath.SecFromTime(t);
        double millis = msGiven ? millisArg : JSDateMath.MsFromTime(t);

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
        bool secGiven = Specified(a, 1), msGiven = Specified(a, 2);
        double secCoerced = secGiven ? _seconds.DoubleValue : double.NaN;
        double msCoerced = msGiven ? _millis.DoubleValue : double.NaN;

        if (!valid)
            return JSNumber.NaN;

        if ((secGiven && !double.IsFinite(secCoerced)) || (msGiven && !double.IsFinite(msCoerced)))
            return new JSNumber(SetTimeValue(double.NaN));

        var mins = CoercedIntValue(minutes);
        var seconds = secGiven ? CoercedIntValue(secCoerced) : utc.Second;
        var millis = msGiven ? CoercedIntValue(msCoerced) : utc.Millisecond;

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
        bool daysGiven = Specified(a, 1);
        double daysCoerced = daysGiven ? _days.DoubleValue : double.NaN;

        if (!valid)
            return JSNumber.NaN;

        if (daysGiven && !double.IsFinite(daysCoerced))
            return new JSNumber(SetTimeValue(double.NaN));

        var month = CoercedIntValue(mnth);
        var days = (daysGiven ? CoercedIntValue(daysCoerced) : utc.Day) - 1;

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
        bool msGiven = Specified(a, 1);
        double msCoerced = msGiven ? _millis.DoubleValue : double.NaN;

        if (!valid)
            return JSNumber.NaN;

        if (msGiven && !double.IsFinite(msCoerced))
            return new JSNumber(SetTimeValue(double.NaN));

        var seconds = CoercedIntValue(secs);
        var millis = msGiven ? CoercedIntValue(msCoerced) : utc.Millisecond;

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
