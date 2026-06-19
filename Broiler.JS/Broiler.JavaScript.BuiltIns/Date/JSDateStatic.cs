using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
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
        // Each argument must be coerced to Number exactly once (Get7Double), then the
        // NaN/Infinity scan and the integer reduction both read those cached doubles.
        var (yD, moD, dD, hD, miD, sD, msD) = a.Get7Double();
        if (double.IsNaN(yD) || double.IsInfinity(yD) ||
            double.IsNaN(moD) || double.IsInfinity(moD) ||
            double.IsNaN(dD) || double.IsInfinity(dD) ||
            double.IsNaN(hD) || double.IsInfinity(hD) ||
            double.IsNaN(miD) || double.IsInfinity(miD) ||
            double.IsNaN(sD) || double.IsInfinity(sD) ||
            double.IsNaN(msD) || double.IsInfinity(msD))
            return JSNumber.NaN;

        int year = unchecked((int)JSValue.ToUint32(yD)), month = unchecked((int)JSValue.ToUint32(moD)),
            day = unchecked((int)JSValue.ToUint32(dD)), hour = unchecked((int)JSValue.ToUint32(hD)),
            minute = unchecked((int)JSValue.ToUint32(miD)), second = unchecked((int)JSValue.ToUint32(sD)),
            millisecond = unchecked((int)JSValue.ToUint32(msD));
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

    // ECMAScript Date Time String Format (ES §21.4.1.18), used as the fallback for the
    // strings the .NET-backed DateParser cannot represent: ISO expanded (signed six-digit)
    // years such as "+275760-..." / "-000001-...", the astronomical year "0000"/"+000000",
    // and any year outside .NET's 1–9999 window. The grammar is:
    //   Date:      YYYY | YYYY-MM | YYYY-MM-DD       (year also expanded ±YYYYYY)
    //   DateTime:  Date 'T' HH:mm[:ss[.sss]] [TimeZone]
    //   TimeZone:  'Z' | ±HH:mm
    private static readonly Regex EcmaIsoDateTime = new(
        @"^([+-]\d{6}|\d{4})(?:-(\d{2})(?:-(\d{2}))?)?(?:T(\d{2}):(\d{2})(?::(\d{2})(?:\.(\d{1,9}))?)?(Z|[+-]\d{2}:\d{2})?)?$",
        RegexOptions.CultureInvariant);

    /// <summary>
    /// Parses an ISO-8601 string in the ECMAScript Date Time String Format whose year may use
    /// the expanded form (e.g. "+275760-..." / "-000001-...") or fall outside .NET's 1–9999
    /// range, returning the ECMAScript time value (ms since epoch). Returns false when the text
    /// is not a valid extended ISO date-time.
    /// </summary>
    internal static bool TryParseExtendedIso(string text, out double ms)
    {
        ms = double.NaN;
        if (string.IsNullOrEmpty(text))
            return false;

        var match = EcmaIsoDateTime.Match(text);
        if (!match.Success)
            return false;

        var yearText = match.Groups[1].Value;
        if (!long.TryParse(yearText, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long year))
            return false;

        // An ISO-8601 expanded year carries a mandatory sign; the year 0 is written "+000000".
        // "-000000" (negative zero) is explicitly invalid (Date.parse → NaN).
        if (year == 0 && yearText[0] == '-')
            return false;

        int month = match.Groups[2].Success ? int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) : 1;
        int day = match.Groups[3].Success ? int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture) : 1;

        var hasTime = match.Groups[4].Success;
        int hour = hasTime ? int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture) : 0;
        int minute = match.Groups[5].Success ? int.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture) : 0;
        int second = match.Groups[6].Success ? int.Parse(match.Groups[6].Value, CultureInfo.InvariantCulture) : 0;

        int millisecond = 0;
        if (match.Groups[7].Success)
        {
            // Fractional seconds: interpret the digits as a fraction of a second and keep
            // millisecond precision (".5" → 500 ms, ".1234" → 123 ms).
            var frac = match.Groups[7].Value;
            var msText = frac.Length >= 3 ? frac.Substring(0, 3) : frac.PadRight(3, '0');
            millisecond = int.Parse(msText, CultureInfo.InvariantCulture);
        }

        // ECMAScript rejects out-of-range components. Hour 24 is permitted only when the
        // remainder of the time-of-day is zero.
        if (month is < 1 or > 12)
            return false;
        if (day < 1 || day > DaysInMonth(year, month))
            return false;
        if (minute is < 0 or > 59 || second is < 0 or > 59)
            return false;
        if (hour > 24 || (hour == 24 && (minute != 0 || second != 0 || millisecond != 0)))
            return false;

        bool hasOffset = match.Groups[8].Success;
        double offsetMs = 0;
        if (hasOffset && match.Groups[8].Value != "Z")
        {
            var tz = match.Groups[8].Value;
            int offSign = tz[0] == '-' ? -1 : 1;
            int offHours = int.Parse(tz.Substring(1, 2), CultureInfo.InvariantCulture);
            int offMinutes = int.Parse(tz.Substring(4, 2), CultureInfo.InvariantCulture);
            if (offHours > 23 || offMinutes > 59)
                return false;
            offsetMs = offSign * (offHours * 60 + offMinutes) * 60000.0;
        }

        double wallClock = JSDateMath.MakeDate(
            JSDateMath.MakeDay(year, month - 1, day),
            JSDateMath.MakeTime(hour, minute, second, millisecond));

        // Per spec: a date-only form is interpreted as UTC; a date-time form with an explicit
        // offset (including "Z") denotes a fixed instant; a date-time without an offset is
        // interpreted as local wall-clock time.
        double utcMs;
        if (hasOffset)
            utcMs = wallClock - offsetMs;
        else if (hasTime)
            utcMs = JSDateMath.UTC(wallClock);
        else
            utcMs = wallClock;

        ms = JSDateMath.TimeClip(utcMs);
        return !double.IsNaN(ms);
    }

    // Days in a month under the proleptic Gregorian calendar (valid for year 0 and
    // negative/astronomical years), used to reject impossible dates such as 2021-02-30.
    private static int DaysInMonth(long year, int month)
    {
        switch (month)
        {
            case 2:
                var leap = (year % 4 == 0 && year % 100 != 0) || year % 400 == 0;
                return leap ? 29 : 28;
            case 4:
            case 6:
            case 9:
            case 11:
                return 30;
            default:
                return 31;
        }
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
