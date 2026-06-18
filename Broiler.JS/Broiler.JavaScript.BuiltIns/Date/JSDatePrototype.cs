using Broiler.JavaScript.Runtime;
using System;
using System.Globalization;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.BuiltIns.String;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Date;

public partial class JSDate
{
    static long MinTime = DateTimeOffset.MinValue.ToUnixTimeMilliseconds();
    static long MaxTime = DateTimeOffset.MaxValue.ToUnixTimeMilliseconds();

    [JSExport(IsConstructor = true, Length = 7)]
    public new static JSValue Constructor(in Arguments a)
        => (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null
            ? new JSDate(DateTimeOffset.Now).ToString(Arguments.Empty)
            : new JSDate(a);

    [JSExport(Length = 7)]
    JSDate(in Arguments a)
    {
        static JSValue ToPrimitive(JSValue value)
        {
            if (value is not JSObject @object)
                return value;

            var toPrimitive = @object[(IJSSymbol)JSSymbol.toPrimitive];
            if (!toPrimitive.IsUndefined && !toPrimitive.IsNull)
            {
                var primitive = toPrimitive.InvokeFunction(new Arguments(@object, new JSString("default")));
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

        DateTimeOffset date;

        if (a.Length == 0)
        {
            value = DateTimeOffset.Now;
            return;
        }

        var dateString = a.Get1();

        if (dateString.IsNumber && double.IsNaN(dateString.DoubleValue))
        {
            value = DateTimeOffset.MinValue;
            return;
        }

        if (a.Length == 1)
        {
            if (dateString is JSDate dateObject)
            {
                value = dateObject.value;
                rawTimeMs = dateObject.rawTimeMs;
                return;
            }

            var primitive = ToPrimitive(dateString);

            // §21.4.2.1: only a String is date-parsed; every other primitive is run
            // through ToNumber. DoubleValue implements ToNumber, so boolean true → 1,
            // false → 0, null → 0 and undefined → NaN, rather than being stringified
            // and mis-parsed (test262 Date/value-to-primitive-result-non-string-prim).
            if (!primitive.IsString)
            {
                SetTimeValue(JSDateMath.TimeClip(primitive.DoubleValue));
                return;
            }

            var text = primitive.StringValue;
            date = DateParser.Parse(text);

            if (date == DateTimeOffset.MinValue)
            {
                // Fall back to the extended ISO parser for expanded years / years
                // outside .NET's 1–9999 range (e.g. "+275760-09-13T00:00:00Z").
                if (TryParseExtendedIso(text, out var extended))
                {
                    SetTimeValue(extended);
                    return;
                }

                value = date;
                return;
            }

            value = date.ToLocalTime();
            return;
        }

        // Coerce each supplied component to Number exactly once (Get7Double); the
        // NaN/Infinity scan and the integer reduction both read the cached doubles so
        // a valueOf side effect is observed a single time per argument, in order.
        var (yD, moD, dD, hD, miD, sD, msD) = a.Get7Double();
        if (double.IsNaN(yD) || double.IsInfinity(yD) ||
            double.IsNaN(moD) || double.IsInfinity(moD) ||
            double.IsNaN(dD) || double.IsInfinity(dD) ||
            double.IsNaN(hD) || double.IsInfinity(hD) ||
            double.IsNaN(miD) || double.IsInfinity(miD) ||
            double.IsNaN(sD) || double.IsInfinity(sD) ||
            double.IsNaN(msD) || double.IsInfinity(msD))
        {
            value = InvalidDate;
            rawTimeMs = double.NaN;
            return;
        }

        int year = unchecked((int)JSValue.ToUint32(yD)), month = unchecked((int)JSValue.ToUint32(moD)),
            day = unchecked((int)JSValue.ToUint32(dD)), hours = unchecked((int)JSValue.ToUint32(hD)),
            minutes = unchecked((int)JSValue.ToUint32(miD)), seconds = unchecked((int)JSValue.ToUint32(sD)),
            millis = unchecked((int)JSValue.ToUint32(msD));

        year = year >= 0 && year < 100 ? year + 1900 : year;

        try
        {
            date = new DateTimeOffset(year, 1, 1, 0, 0, 0, 0, Local);
            date = date.AddMilliseconds(millis);
            date = date.AddSeconds(seconds);
            date = date.AddMinutes(minutes);
            date = date.AddHours(hours);
            date = date.AddDays(day - 1);
            date = date.AddMonths(month);
            value = date;
            rawTimeMs = double.NaN;

            return;
        }
        catch (ArgumentOutOfRangeException)
        {
            // The local date/time is outside .NET DateTimeOffset's 1–9999 year range.
            // Fall back to ECMAScript time-value math (ms since epoch), which spans the
            // full Date range, so e.g. new Date(1970, 0, -99999999) stays a valid date
            // and toISOString() emits an expanded-year string instead of throwing.
            double timeWithinDay = JSDateMath.MakeTime(hours, minutes, seconds, millis);
            double dayValue = JSDateMath.MakeDay(year, month, day);
            double result = JSDateMath.TimeClip(JSDateMath.UTC(JSDateMath.MakeDate(dayValue, timeWithinDay)));

            value = DateTimeOffset.MinValue;
            rawTimeMs = result;
            return;
        }
    }

}
