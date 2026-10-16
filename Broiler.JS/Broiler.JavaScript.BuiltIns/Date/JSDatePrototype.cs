using Broiler.ECMAScript.Time;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Date;

public partial class JSDate
{
    // Date called as a function (ECMA-262 21.4.2.1 step 1): ToDateString of the present moment.
    [JSExport(IsConstructor = true, Length = 7)]
    public new static JSValue Constructor(in Arguments a)
        => (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null
            ? new JSString(EcmaDateFormatter.ToDateString(CurrentTime(), Zone))
            : new JSDate(a);

    // The Date constructor (ECMA-262 21.4.2.1): the present moment, a time value, or local fields.
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

        // Step 3: no argument is the present moment.
        if (a.Length == 0)
        {
            timeValue = CurrentTime();
            return;
        }

        // Step 4: one argument is another date's time value, a String read by Date.parse's grammars, or
        // any other primitive's ToNumber; TimeClip in every case.
        if (a.Length == 1)
        {
            var value = a.Get1();
            double tv;
            if (value is JSDate other)
            {
                tv = other.timeValue;
            }
            else
            {
                var primitive = ToPrimitive(value);
                tv = primitive.IsString ? ParseTimeValue(primitive.StringValue) : primitive.DoubleValue;
            }

            timeValue = EcmaTime.TimeClip(tv);
            return;
        }

        // Step 5: local fields. Each argument is converted once, in order (Get7Double), the absent ones
        // taking the defaults; the year is MakeFullYear's, and the local time is UTC's over the host's zone.
        var (year, month, date, hours, minutes, seconds, milliseconds) = a.Get7Double();
        var finalDate = EcmaTime.MakeDate(
            EcmaTime.MakeDay(EcmaTime.MakeFullYear(year), month, date),
            EcmaTime.MakeTime(hours, minutes, seconds, milliseconds));
        timeValue = EcmaTime.TimeClip(EcmaLocalTime.Utc(finalDate, Zone));
    }
}
