// REFERENCE ARM J: a test-only copy of Broiler.JS's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-6-001 (docs/evidence/sjl-6-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.JS at bc3597f (the commit the area verdict's correspondence reads):
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Date/JSDatePrototype.Formatters.cs
//     blob 280311a6ae8554153b8bf85ecb5b4190d21e6748: the body of toISOString (:55-63), ToIsoString (:90-117),
//     the bodies of toString (:199-213), toTimeString (:215-225) and toUTCString (:227-239), WeekDayNames and
//     MonthNames (:251-253), DateString (:256-263), ClockString (:267-272), HumanYearString (:276-278) and
//     ToTimeZoneString (:280-300)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Date/JSDate.cs
//     blob be56a30aac91b881b79f5ba713cba620449a3b50: Local (:25) and SetTimeValue (:81-115), which decide the
//     stored value and rawTimeMs (:27, :34) ToTimeZoneString reads; InvalidDate (:16) is copied in JSDateParse.cs
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Date/JSDatePrototype.cs
//     blob 26e8b8d3bef79f3d29a1f16004c7bed9c0015fd5: MinTime and MaxTime (:11-12), which SetTimeValue reads
//
// Changes from the source, none of which changes a result:
//   - the sources' line endings are LF already; the byte-order marks of JSDatePrototype.Formatters.cs and
//     JSDate.cs dropped;
//   - the namespace; the members sit in the static class JSDate (a JSObject partial class in the source);
//     private and public members are internal;
//   - the [JSExport...] attributes are not copied;
//   - ToString, ToTimeString and ToUTCString are the bodies of toString, toTimeString and toUTCString as static
//     functions of the time value: `var time = GetTimeMs()` is the parameter `time`, and the String the source
//     wraps in a JSString is returned as it is ("Invalid Date" for NaN included). ToString and ToTimeString also
//     take a TimeZoneInfo zone, which they pass to the copied JSDateMath.LocalTime (whose source reads
//     TimeZoneInfo.Local) and to ZoneName;
//   - ToISOString is the body of toISOString as a static function of the time value, its
//     JSEngine.NewRangeError("Invalid time value") the recorded ArmError("RangeError", "Invalid time value");
//     ToIsoString itself takes the time value in the source and throws nothing, and is unchanged;
//   - ToTimeZoneString is static and takes (double offsetMs, string zoneName). Its zone-name lookup, the lines
//     :282 (`var timeZone = TimeZoneInfo.Local`) and :288-294 (`instant`, with its rawTimeMs/DateTimeOffset.UtcNow
//     fallback, and IsDaylightSavingTime), moves verbatim into ZoneName(TimeZoneInfo zone, double time), with
//     `var timeZone = zone`, and returns zoneName; ToString and ToTimeString pass ZoneName(zone, time) to it,
//     where the source's ToTimeZoneString computes the name itself. The source reads `value` and `rawTimeMs`,
//     the Date's stored fields; ZoneName computes them by the copied SetTimeValue(zone, time, ...), the store a
//     Date holding the time value `time` went through, its two fields out parameters: `instant` is the stored
//     DateTimeOffset, FromUnixTimeMilliseconds((long)time) re-expressed at the zone's base offset, when time is
//     within [MinTime, MaxTime] (DateTimeOffset's millisecond range), is not MinTime itself (the instant equal
//     to InvalidDate, which SetTimeValue keeps raw) and the re-expression does not overflow .NET's range (which
//     happens within one base offset of either end); otherwise it is the fallback, DateTimeOffset.UtcNow, the current
//     instant. IsDaylightSavingTime(DateTimeOffset) reads the instant's UTC time only, so the stored offset
//     decides nothing but the overflow;
//   - Local, the source's TimeZoneInfo.Local.BaseUtcOffset property, is a function of the zone, zone.BaseUtcOffset;
//   - ToTimeZoneString's `$"GMT{hhmm:d4} ({zoneName})"` and `$"GMT+{hhmm:d4} ({zoneName})"` format hhmm with the
//     current culture, as the source does; the comparison sets it.

using System;
using System.Globalization;
using Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Text;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Date.J;

/// <summary>Broiler.JS's Date members, as copied: here, its renderings.</summary>
internal static partial class JSDate
{
    internal static long MinTime = DateTimeOffset.MinValue.ToUnixTimeMilliseconds();
    internal static long MaxTime = DateTimeOffset.MaxValue.ToUnixTimeMilliseconds();

    internal static TimeSpan Local(TimeZoneInfo zone) => zone.BaseUtcOffset;

    // SetTimeValue (JSDate.cs:81-115): the Date's fields `value` and `rawTimeMs` are out parameters.
    internal static double SetTimeValue(TimeZoneInfo zone, double ms, out DateTimeOffset value, out double rawTimeMs)
    {
        if (double.IsNaN(ms))
        {
            value = InvalidDate;
            rawTimeMs = double.NaN;
            return double.NaN;
        }

        if (ms < MinTime || ms > MaxTime)
        {
            value = DateTimeOffset.MinValue;
            rawTimeMs = ms;
            return ms;
        }

        try
        {
            value = DateTimeOffset.FromUnixTimeMilliseconds((long)ms).ToOffset(Local(zone));
            // The MinValue sentinel doubles as the "invalid date" marker, so a real
            // time value that lands exactly on it (0001-01-01T00:00:00Z, the .NET
            // boundary) must keep its raw representation to avoid being read as NaN.
            rawTimeMs = value == InvalidDate ? ms : double.NaN;
        }
        catch (ArgumentOutOfRangeException)
        {
            // Re-expressing the instant in the local offset overflowed .NET's range
            // at the very edge of the 1–9999 window; keep the raw time value, which
            // the getters/toISOString handle via ECMAScript date math.
            value = DateTimeOffset.MinValue;
            rawTimeMs = ms;
        }

        return ms;
    }

    // toISOString's body (:55-63).
    internal static string ToISOString(double time)
    {
        if (double.IsNaN(time))
            throw new ArmError("RangeError", "Invalid time value");

        return ToIsoString(time);
    }

    internal static string ToIsoString(double time)
    {
        var year = JSDateMath.YearFromTime(time);
        var month = JSDateMath.MonthFromTime(time) + 1;
        var day = JSDateMath.DateFromTime(time);
        var hour = JSDateMath.HourFromTime(time);
        var minute = JSDateMath.MinFromTime(time);
        var second = JSDateMath.SecFromTime(time);
        var millisecond = JSDateMath.MsFromTime(time);
        var yearText = year >= 0 && year <= 9999
            ? year.ToString("D4", DateTimeFormatInfo.InvariantInfo)
            : string.Format(
                DateTimeFormatInfo.InvariantInfo,
                "{0}{1:D6}",
                year < 0 ? "-" : "+",
                Math.Abs(year));

        return string.Format(
            DateTimeFormatInfo.InvariantInfo,
            "{0}-{1:D2}-{2:D2}T{3:D2}:{4:D2}:{5:D2}.{6:D3}Z",
            yearText,
            month,
            day,
            hour,
            minute,
            second,
            millisecond);
    }

    // toString's body (:199-213).
    internal static string ToString(double time, TimeZoneInfo zone)
    {
        if (double.IsNaN(time))
            return "Invalid Date";

        // ToDateString (ECMA-262 §21.4.4.41.4): DateString(LocalTime(t)) + " " +
        // TimeString(LocalTime(t)) + TimeZoneString(t). Computed from the ECMAScript
        // time value (not the .NET DateTimeOffset) so the full Date range — including
        // year 0 and negative years that fall outside .NET's 1–9999 window — renders.
        var local = JSDateMath.LocalTime(time, zone);
        var date = $"{DateString(local)} {ClockString(local)} {ToTimeZoneString(local - time, ZoneName(zone, time))}";
        return date;
    }

    // toTimeString's body (:215-225).
    internal static string ToTimeString(double time, TimeZoneInfo zone)
    {
        if (double.IsNaN(time))
            return "Invalid Date";

        var local = JSDateMath.LocalTime(time, zone);
        var date = $"{ClockString(local)} {ToTimeZoneString(local - time, ZoneName(zone, time))}";
        return date;
    }

    // toUTCString's body (:227-239).
    internal static string ToUTCString(double time)
    {
        if (double.IsNaN(time))
            return "Invalid Date";

        var weekday = WeekDayNames[JSDateMath.WeekDay(time)];
        var day = JSDateMath.DateFromTime(time).ToString("D2", DateTimeFormatInfo.InvariantInfo);
        var month = MonthNames[JSDateMath.MonthFromTime(time)];
        var year = HumanYearString(JSDateMath.YearFromTime(time));
        return $"{weekday}, {day} {month} {year} {ClockString(time)} GMT";
    }

    internal static readonly string[] WeekDayNames = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];
    internal static readonly string[] MonthNames =
        ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

    internal static string DateString(double t)
    {
        var weekday = WeekDayNames[JSDateMath.WeekDay(t)];
        var month = MonthNames[JSDateMath.MonthFromTime(t)];
        var day = JSDateMath.DateFromTime(t).ToString("D2", DateTimeFormatInfo.InvariantInfo);
        var year = HumanYearString(JSDateMath.YearFromTime(t));
        return $"{weekday} {month} {day} {year}";
    }

    internal static string ClockString(double t) => string.Format(
        DateTimeFormatInfo.InvariantInfo,
        "{0:D2}:{1:D2}:{2:D2}",
        JSDateMath.HourFromTime(t),
        JSDateMath.MinFromTime(t),
        JSDateMath.SecFromTime(t));

    internal static string HumanYearString(long year) => year >= 0
        ? year.ToString("D4", DateTimeFormatInfo.InvariantInfo)
        : "-" + Math.Abs(year).ToString("D4", DateTimeFormatInfo.InvariantInfo);

    internal static string ToTimeZoneString(double offsetMs, string zoneName)
    {
        // Compute the time zone offset in hours-minutes from the supplied offset (already
        // resolved for this instant by JSDateMath, including the out-of-.NET-range fallback).
        int offsetInMinutes = (int)(offsetMs / 60000);
        int hhmm = offsetInMinutes / 60 * 100 + offsetInMinutes % 60;

        if (hhmm < 0)
            return $"GMT{hhmm:d4} ({zoneName})";
        else
            return $"GMT+{hhmm:d4} ({zoneName})";
    }

    // ToTimeZoneString's zone-name lookup (:282, :288-294); `value` and `rawTimeMs` are what SetTimeValue stores
    // for the time value `time`.
    internal static string ZoneName(TimeZoneInfo zone, double time)
    {
        var timeZone = zone;
        SetTimeValue(zone, time, out var value, out var rawTimeMs);

        // Get the time zone name. For dates outside .NET's range (rawTimeMs set) the
        // DateTimeOffset value is the MinValue sentinel, so fall back to the current
        // instant for the implementation-defined zone name — matching the offset fallback.
        var instant = double.IsNaN(rawTimeMs) ? value : DateTimeOffset.UtcNow;
        string zoneName = timeZone.IsDaylightSavingTime(instant)
            ? timeZone.DaylightName
            : timeZone.StandardName;
        return zoneName;
    }
}
