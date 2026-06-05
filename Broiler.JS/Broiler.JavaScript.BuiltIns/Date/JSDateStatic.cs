extern alias ExtendedDateTime;
using System;
using System.Runtime.CompilerServices;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Date;

internal static class JSDateStatic
{
    internal static JSDate AsJSDate(this JSValue v, [CallerMemberName] string helper = null)
    {
        if (v is not JSDate date)
            throw JSEngine.NewTypeError($"Date.prototype.{helper} called on non date");

        return date;
    }

    /// <summary>
    /// Converts a .NET date into a javascript date.
    /// </summary>
    /// <param name="dateTime"> The .NET date. </param>
    /// <returns> The number of milliseconds since January 1, 1970, 00:00:00 UTC </returns>
    internal static double ToJSDate(this DateTimeOffset dateTime)
    {
        if (dateTime == JSDate.InvalidDate)
            return double.NaN;

        return dateTime.ToUniversalTime().ToUnixTimeMilliseconds();
    }
}

partial class JSDate
{
    public static long epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;

    [JSExport("UTC")]
    internal static JSValue UTC(in Arguments a)
    {
        // Compute the time value with ECMAScript date math (ms since epoch) so the
        // full Date range is supported, including years outside .NET's 1–9999 window.
        for (var i = 0; i < Math.Min(a.Length, 7); i++)
        {
            var part = a.GetAt(i).DoubleValue;
            if (double.IsNaN(part) || double.IsInfinity(part))
                return JSNumber.NaN;
        }

        var (year, month, day, hour, minute, second, millisecond) = a.Get7Int();
        year = year >= 0 && year < 100 ? year + 1900 : year;

        double time = JSDateMath.MakeTime(hour, minute, second, millisecond);
        double dayValue = JSDateMath.MakeDay(year, month, day);
        double val = JSDateMath.TimeClip(JSDateMath.MakeDate(dayValue, time));

        return new JSNumber(val);
    }

    [JSExport("now")]
    internal static JSValue Now(in Arguments a)
    {
        var result = DateTimeOffset.Now.ToJSDate();
        return new JSNumber(result);
    }

    /// <summary>
    /// Jint - private JsValue Parse(JsValue thisObj, JsValue[] arguments), but we changed 
    ///  DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal to DateTimeStyles.AssumeLocal
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
    [JSExport("parse", Length = 1)]
    internal static JSValue Parse(in Arguments a)
    {
        var text = a.Get1().ToString();

        var val = DateParser.Parse(text).ToJSDate();

        // The .NET-backed parser can't represent ISO-8601 expanded years (e.g.
        // "+275760-09-13T..." / "-000001-...") or years outside 1–9999. Fall back to
        // the extended ISO parser so toISOString output round-trips across the full range.
        if (double.IsNaN(val) && TryParseExtendedIso(text, out var extended))
            val = extended;

        return new JSNumber(val);
    }

    /// <summary>
    /// Parses an ISO-8601 / RFC-3339 string whose year may use the expanded form
    /// (e.g. "+275760-..." / "-000001-...") or fall outside .NET's 1–9999 range,
    /// returning the ECMAScript time value (ms since epoch). Returns false when the
    /// text is not a valid extended ISO date-time.
    /// </summary>
    internal static bool TryParseExtendedIso(string text, out double ms)
    {
        ms = double.NaN;

        if (!ExtendedDateTime::Broiler.DateTime.ExtendedIsoDateTime.TryParse(text, out var v) || v is null)
            return false;

        int milli = v.Nanosecond / 1_000_000;
        double wallClock = JSDateMath.MakeDate(
            JSDateMath.MakeDay(v.Year, v.Month - 1, v.Day),
            JSDateMath.MakeTime(v.Hour, v.Minute, v.Second, milli));

        // A string with an explicit offset (including "Z") denotes a fixed instant;
        // without one, ECMAScript interprets the wall-clock time as local time.
        double utcMs = v.HasOffset
            ? wallClock - v.Offset.Value.TotalMilliseconds
            : JSDateMath.UTC(wallClock);

        ms = JSDateMath.TimeClip(utcMs);
        return !double.IsNaN(ms);
    }

    /// <summary>
    /// Given the components of a date, returns the equivalent .NET date.
    /// </summary>
    /// <param name="year"> The full year. </param>
    /// <param name="month"> The month as an integer between 0 and 11 (january to december). </param>
    /// <param name="day"> The day of the month, from 1 to 31.  Defaults to 1. </param>
    /// <param name="hour"> The number of hours since midnight, from 0 to 23.  Defaults to 0. </param>
    /// <param name="minute"> The number of minutes, from 0 to 59.  Defaults to 0. </param>
    /// <param name="second"> The number of seconds, from 0 to 59.  Defaults to 0. </param>
    /// <param name="millisecond"> The number of milliseconds, from 0 to 999.  Defaults to 0. </param>
    /// <param name="kind"> Indicates whether the components are in UTC or local time. </param>
    /// <returns> The equivalent .NET date. </returns>
    internal static DateTimeOffset ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, TimeSpan offset)
    {
        // DateTime doesn't support years below year 1.
        if (year < 0)
            return InvalidDate;

        // This step was missing from Jurrasic, add 1900 to year < 2000 to get full year. 
        if (0 <= year && year <= 99)
            year += 1900;

        // var offset = TimeZoneInfo.Local.BaseUtcOffset;

        if (month >= 0 && month < 12 &&
            day >= 1 && day <= DateTime.DaysInMonth(year, month + 1) &&
            hour >= 0 && hour < 24 &&
            minute >= 0 && minute < 60 &&
            second >= 0 && second < 60 &&
            millisecond >= 0 && millisecond < 1000)
        {
            // All parameters are in range.
            return new DateTimeOffset(year, month + 1, day, hour, minute, second, millisecond, offset);
        }
        else
        {
            // One or more parameters are out of range.
            try
            {
                DateTimeOffset value = new(year, 1, 1, 0, 0, 0, offset);
                value = value.AddMonths(month);

                if (day != 1)
                    value = value.AddDays(day - 1);

                if (hour != 0)
                    value = value.AddHours(hour);

                if (minute != 0)
                    value = value.AddMinutes(minute);

                if (second != 0)
                    value = value.AddSeconds(second);

                if (millisecond != 0)
                    value = value.AddMilliseconds(millisecond);

                return value;
            }
            catch (ArgumentOutOfRangeException)
            {
                // One or more of the parameters was NaN or way too big or way too small.
                // Return a sentinel invalid date.
                return InvalidDate;
            }
        }
    }
}
