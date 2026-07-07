using Broiler.JavaScript.BuiltIns.Null;
using System;
using System.Globalization;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Date;

public partial class JSDate
{
    private static JSValue ToNumberPrimitive(JSValue value)
    {
        if (value is not JSObject @object)
            return value;

        var toPrimitive = @object[(IJSSymbol)JSSymbol.toPrimitive];
        if (!toPrimitive.IsUndefined && !toPrimitive.IsNull)
        {
            var primitive = toPrimitive.InvokeFunction(new Arguments(@object, JSConstants.Number));
            if (primitive.IsObject)
                throw JSEngine.NewTypeError("Cannot convert object to primitive value");

            return primitive;
        }

        if (@object[KeyStrings.valueOf] is IJSFunction valueOf)
        {
            var primitive = valueOf.InvokeFunction(new Arguments(@object));
            if (!primitive.IsObject)
                return primitive;
        }

        if (@object[KeyStrings.toString] is IJSFunction toString)
        {
            var primitive = toString.InvokeFunction(new Arguments(@object));
            if (!primitive.IsObject)
                return primitive;
        }

        throw JSEngine.NewTypeError("Cannot convert object to primitive value");
    }

    [JSExport("toDateString", Length = 0)]
    internal JSValue ToDateString(in Arguments a)
    {
        var time = GetTimeMs();
        if (double.IsNaN(time))
            return new JSString("Invalid Date");

        return new JSString(DateString(JSDateMath.LocalTime(time)));
    }

    [JSExport("toISOString", Length = 0)]
    internal JSValue ToISOString(in Arguments a)
    {
        var time = GetTimeMs();
        if (double.IsNaN(time))
            throw JSEngine.NewRangeError("Invalid time value");

        return new JSString(ToIsoString(time));
    }

    [JSExport("toJSON", Length = 1)]
    internal JSValue ToJSON(in Arguments a)
    {
        var receiver = a.This;
        var @object = receiver as JSObject;
        if (@object == null)
        {
            if (receiver.IsNullOrUndefined)
                throw JSEngine.NewTypeError(JSException.Cannot_convert_undefined_or_null_to_object);

            @object = (JSObject)CreatePrimitiveObject(receiver);
        }

        var primitive = ToNumberPrimitive(@object);
        if (primitive.IsNumber)
        {
            var number = primitive.DoubleValue;
            if (double.IsNaN(number) || double.IsInfinity(number))
                return JSNull.Value;
        }

        var toISOString = @object[KeyStrings.GetOrCreate("toISOString")];
        return toISOString.InvokeFunction(new Arguments(@object));
    }

    private static string ToIsoString(double time)
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

    [JSExport("toLocaleDateString", Length = 0)]
    internal JSValue ToLocaleDateString(in Arguments a)
    {
        if (double.IsNaN(GetTimeMs()))
            return new JSString("Invalid Date");

        var (locale, format) = a.Get2();

        // Broiler extension: a .NET format *string* keeps the legacy CultureInfo fast path.
        if (format.IsString)
            return new JSString(FormatWithNetCulture(locale, format.ToString(), "D"));

        // Spec (§21.4.4.39): construct an Intl.DateTimeFormat with ToDateTimeOptions(options,
        // "date", "date") and format through it, so the result — and any exception — matches
        // Intl.DateTimeFormat exactly.
        return FormatThroughIntl(locale, format, required: "date", defaults: "date");
    }

    [JSExport("toLocaleString", Length = 0)]
    internal JSValue ToLocaleString(in Arguments a)
    {
        if (double.IsNaN(GetTimeMs()))
            return new JSString("Invalid Date");

        var (locale, format) = a.Get2();

        // Broiler extension: a bare .NET format *string* keeps the legacy "F"/CultureInfo path.
        if (format.IsString)
            return new JSString(FormatWithNetCulture(locale, format.ToString(), "F"));

        // Spec (§21.4.4.39): construct an Intl.DateTimeFormat with ToDateTimeOptions(options,
        // "any", "all") — so with no component/style it shows the full date+time — and format
        // through it. This makes the output and exceptions match Intl.DateTimeFormat exactly.
        return FormatThroughIntl(locale, format, required: "any", defaults: "all");
    }

    [JSExport("toLocaleTimeString", Length = 0)]
    internal JSValue ToLocaleTimeString(in Arguments a)
    {
        if (double.IsNaN(GetTimeMs()))
            return new JSString("Invalid Date");

        var (locale, format) = a.Get2();

        // Broiler extension: a .NET format *string* keeps the legacy CultureInfo fast path.
        if (format.IsString)
            return new JSString(FormatWithNetCulture(locale, format.ToString(), "T"));

        // Spec (§21.4.4.42): construct an Intl.DateTimeFormat with ToDateTimeOptions(options,
        // "time", "time") and format through it, so the result and any exception match
        // Intl.DateTimeFormat exactly.
        return FormatThroughIntl(locale, format, required: "time", defaults: "time");
    }

    // Routes Date.prototype.toLocale{,Date,Time}String through Intl.DateTimeFormat, applying the
    // method's ToDateTimeOptions(options, required, defaults). Both the formatted output and any
    // thrown exception (null locale → TypeError, malformed tag / invalid option → RangeError) are
    // therefore identical to constructing the Intl.DateTimeFormat directly.
    private JSValue FormatThroughIntl(JSValue locale, JSValue format, string required, string defaults)
    {
        var opts = Intl.JSIntlDateTimeFormat.ToDateTimeOptions(format, required, defaults);
        var dtf = new Intl.JSIntlDateTimeFormat(new Arguments(JSUndefined.Value, locale, opts));
        return dtf.Format(new Arguments(JSUndefined.Value, CreateNumber(GetTimeMs())));
    }

    // The Broiler .NET extension path: a non-spec convenience where the second argument is a .NET
    // custom/standard date format string. A nullish locale uses the current culture; otherwise the
    // locale must validate as a language tag (matching the spec's locale handling) and selects the
    // CultureInfo. <paramref name="standardFormat"/> is the .NET format used when none is supplied.
    private string FormatWithNetCulture(JSValue locale, string format, string standardFormat)
    {
        if (!locale.IsUndefined)
            Intl.JSIntl.CanonicalizeLocaleList(locale);

        var culture = locale.IsNullOrUndefined
            ? DateTimeFormatInfo.CurrentInfo
            : (IFormatProvider)CultureInfo.GetCultureInfo(locale.ToString());
        return value.ToString(string.IsNullOrEmpty(format) ? standardFormat : format, culture);
    }

    [JSExport("toString", Length = 0)]
    internal new JSValue ToString(in Arguments a)
    {
        var time = GetTimeMs();
        if (double.IsNaN(time))
            return new JSString("Invalid Date");

        // ToDateString (ECMA-262 §21.4.4.41.4): DateString(LocalTime(t)) + " " +
        // TimeString(LocalTime(t)) + TimeZoneString(t). Computed from the ECMAScript
        // time value (not the .NET DateTimeOffset) so the full Date range — including
        // year 0 and negative years that fall outside .NET's 1–9999 window — renders.
        var local = JSDateMath.LocalTime(time);
        var date = $"{DateString(local)} {ClockString(local)} {ToTimeZoneString(local - time)}";
        return new JSString(date);
    }

    [JSExport("toTimeString", Length = 0)]
    internal JSValue ToTimeString(in Arguments a)
    {
        var time = GetTimeMs();
        if (double.IsNaN(time))
            return new JSString("Invalid Date");

        var local = JSDateMath.LocalTime(time);
        var date = $"{ClockString(local)} {ToTimeZoneString(local - time)}";
        return new JSString(date);
    }

    [JSExport("toUTCString", Length = 0)]
    internal JSValue ToUTCString(in Arguments a)
    {
        var time = GetTimeMs();
        if (double.IsNaN(time))
            return new JSString("Invalid Date");

        var weekday = WeekDayNames[JSDateMath.WeekDay(time)];
        var day = JSDateMath.DateFromTime(time).ToString("D2", DateTimeFormatInfo.InvariantInfo);
        var month = MonthNames[JSDateMath.MonthFromTime(time)];
        var year = HumanYearString(JSDateMath.YearFromTime(time));
        return new JSString($"{weekday}, {day} {month} {year} {ClockString(time)} GMT");
    }

    [JSExport("valueOf", Length = 0)]
    internal new JSValue ValueOf(in Arguments a)
    {
        var result = GetTimeMs();
        return double.IsNaN(result) ? JSNumber.NaN : new JSNumber(result);
    }

    // Abbreviated weekday/month names used by the ECMAScript date/time string
    // representations (Date.prototype.toString / toUTCString / toDateString). These are
    // locale-independent: the spec mandates these exact English abbreviations.
    private static readonly string[] WeekDayNames = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];
    private static readonly string[] MonthNames =
        ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

    // DateString (ECMA-262 §21.4.4.41.2): "Thu Jan 01 1970".
    private static string DateString(double t)
    {
        var weekday = WeekDayNames[JSDateMath.WeekDay(t)];
        var month = MonthNames[JSDateMath.MonthFromTime(t)];
        var day = JSDateMath.DateFromTime(t).ToString("D2", DateTimeFormatInfo.InvariantInfo);
        var year = HumanYearString(JSDateMath.YearFromTime(t));
        return $"{weekday} {month} {day} {year}";
    }

    // The "hh:mm:ss" portion of TimeString (ECMA-262 §21.4.4.41.1), without the trailing
    // time-zone text (which differs between the local and UTC string forms).
    private static string ClockString(double t) => string.Format(
        DateTimeFormatInfo.InvariantInfo,
        "{0:D2}:{1:D2}:{2:D2}",
        JSDateMath.HourFromTime(t),
        JSDateMath.MinFromTime(t),
        JSDateMath.SecFromTime(t));

    // Year as it appears in the human-readable date strings: a minimum of four digits,
    // with a leading "-" for negative (proleptic) years — e.g. 1970, 0001, -0001, 12345.
    private static string HumanYearString(long year) => year >= 0
        ? year.ToString("D4", DateTimeFormatInfo.InvariantInfo)
        : "-" + Math.Abs(year).ToString("D4", DateTimeFormatInfo.InvariantInfo);

    internal string ToTimeZoneString(double offsetMs)
    {
        var timeZone = TimeZoneInfo.Local;
        // Compute the time zone offset in hours-minutes from the supplied offset (already
        // resolved for this instant by JSDateMath, including the out-of-.NET-range fallback).
        int offsetInMinutes = (int)(offsetMs / 60000);
        int hhmm = offsetInMinutes / 60 * 100 + offsetInMinutes % 60;

        // Get the time zone name. For dates outside .NET's range (rawTimeMs set) the
        // DateTimeOffset value is the MinValue sentinel, so fall back to the current
        // instant for the implementation-defined zone name — matching the offset fallback.
        var instant = double.IsNaN(rawTimeMs) ? value : DateTimeOffset.UtcNow;
        string zoneName = timeZone.IsDaylightSavingTime(instant)
            ? timeZone.DaylightName
            : timeZone.StandardName;

        if (hhmm < 0)
            return $"GMT{hhmm:d4} ({zoneName})";
        else
            return $"GMT+{hhmm:d4} ({zoneName})";
    }
}
