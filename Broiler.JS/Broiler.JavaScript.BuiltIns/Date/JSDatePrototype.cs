using Broiler.JavaScript.Runtime;
using System;
using System.Globalization;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.BuiltIns.String;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Date;

public partial class JSDate
{
    static long MinTime = DateTimeOffset.MinValue.ToUnixTimeMilliseconds();
    static long MaxTime = DateTimeOffset.MaxValue.ToUnixTimeMilliseconds();

    /// <summary>
    /// Factory delegate for formatting a date using Intl.DateTimeFormat.
    /// Wired by the BuiltIns assembly via <c>[ModuleInitializer]</c>.
    /// Signature: (CultureInfo culture, DateTimeOffset value, JSObject options) → JSValue
    /// </summary>
    internal static Func<CultureInfo, DateTimeOffset, JSObject, JSValue> IntlDateFormatter { get; set; }

    [JSExport(Length = 7)]
    JSDate(in Arguments a)
    {
        static JSValue ToPrimitive(JSValue value)
        {
            if (value is not JSObject @object)
                return value;

            var toPrimitive = @object[(IJSSymbol)JSSymbol.toPrimitive];
            if (!toPrimitive.IsUndefined)
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
            if (primitive.IsNumber)
            {
                var ticks = primitive.BigIntValue;
                ticks = Math.Max(MinTime, ticks);
                ticks = Math.Min(MaxTime, ticks);
                date = DateTimeOffset.FromUnixTimeMilliseconds(ticks);

                if (ticks == MinTime || ticks == MaxTime)
                {
                    value = date;
                    return;
                }

                value = date.ToOffset(Local);
                return;
            }

            date = DateParser.Parse(primitive.StringValue);

            if (date == DateTimeOffset.MinValue)
            {
                value = date;
                return;
            }

            value = date.ToLocalTime();
            return;
        }

        var (year, month, day, hours, minutes, seconds, millis) = a.Get7Int();

        day = day - 1;
        try
        {
            year = year >= 0 && year < 100 ? year + 1900 : year;
            date = new DateTimeOffset(year, 1, 1, 0, 0, 0, 0, Local);
            date = date.AddMilliseconds(millis);
            date = date.AddSeconds(seconds);
            date = date.AddMinutes(minutes);
            date = date.AddHours(hours);
            date = date.AddDays(day);
            date = date.AddMonths(month);
            value = date;

            return;
        }
        catch (ArgumentOutOfRangeException)
        {
            value = DateTimeOffset.MinValue;
            return;
        }
    }

}
