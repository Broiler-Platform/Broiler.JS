using Broiler.ECMAScript.Time;
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

    // toDateString (ECMA-262 21.4.4.35): DateString of the local time.
    [JSExport("toDateString", Length = 0)]
    internal JSValue ToDateString(in Arguments a) =>
        new JSString(double.IsNaN(timeValue) ? EcmaDateFormatter.InvalidDate : EcmaDateFormatter.DateString(LocalTimeValue));

    // toISOString (21.4.4.36): a RangeError for an invalid date.
    [JSExport("toISOString", Length = 0)]
    internal JSValue ToISOString(in Arguments a)
    {
        if (!EcmaDateFormatter.TryToIsoString(timeValue, out var text))
            throw JSEngine.NewRangeError("Invalid time value");

        return new JSString(text);
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

    [JSExport("toLocaleDateString", Length = 0)]
    internal JSValue ToLocaleDateString(in Arguments a)
    {
        if (double.IsNaN(timeValue))
            return new JSString(EcmaDateFormatter.InvalidDate);

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
    internal new JSValue ToLocaleString(in Arguments a)
    {
        if (double.IsNaN(timeValue))
            return new JSString(EcmaDateFormatter.InvalidDate);

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
        if (double.IsNaN(timeValue))
            return new JSString(EcmaDateFormatter.InvalidDate);

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
        return dtf.Format(new Arguments(JSUndefined.Value, CreateNumber(timeValue)));
    }

    // The Broiler .NET extension path: a non-spec convenience where the second argument is a .NET
    // custom/standard date format string. A nullish locale uses the current culture; otherwise the
    // locale must validate as a language tag (matching the spec's locale handling) and selects the
    // CultureInfo. <paramref name="standardFormat"/> is the .NET format used when none is supplied.
    // The date is formatted as its DateTimeOffset view, at the host's offset in force at the instant;
    // a date that view cannot hold, outside the years 1 to 9999, is a RangeError (correspondence row R5).
    private string FormatWithNetCulture(JSValue locale, string format, string standardFormat)
    {
        if (!locale.IsUndefined)
            Intl.JSIntl.CanonicalizeLocaleList(locale);

        if (!InView(timeValue))
            throw JSEngine.NewRangeError("A .NET date format string formats a date from the year 1 to the year 9999 only");

        var culture = locale.IsNullOrUndefined
            ? DateTimeFormatInfo.CurrentInfo
            : (IFormatProvider)CultureInfo.GetCultureInfo(locale.ToString());
        return ToView(timeValue).ToString(string.IsNullOrEmpty(format) ? standardFormat : format, culture);
    }

    // toString (21.4.4.41): ToDateString over the host's zone.
    [JSExport("toString", Length = 0)]
    internal new JSValue ToString(in Arguments a) => new JSString(EcmaDateFormatter.ToDateString(timeValue, Zone));

    // toTimeString (21.4.4.42): TimeString of the local time, then TimeZoneString.
    [JSExport("toTimeString", Length = 0)]
    internal JSValue ToTimeString(in Arguments a) =>
        new JSString(double.IsNaN(timeValue)
            ? EcmaDateFormatter.InvalidDate
            : EcmaDateFormatter.TimeString(LocalTimeValue) + EcmaDateFormatter.TimeZoneString(timeValue, Zone));

    // toUTCString (21.4.4.43).
    [JSExport("toUTCString", Length = 0)]
    internal JSValue ToUTCString(in Arguments a) => new JSString(EcmaDateFormatter.ToUtcString(timeValue));

    [JSExport("valueOf", Length = 0)]
    internal new JSValue ValueOf(in Arguments a) => NumberOf(timeValue);
}
