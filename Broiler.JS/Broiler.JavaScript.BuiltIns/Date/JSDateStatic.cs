using System;
using System.Runtime.CompilerServices;
using Broiler.ECMAScript.Time;
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
}

partial class JSDate
{
    public static long epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;

    // Date.UTC (ECMA-262 21.4.3.4): each argument converted once, in order, the absent ones after the year
    // taking the defaults; the year is MakeFullYear's.
    [JSExport("UTC", Length = 7)]
    internal static JSValue UTC(in Arguments a)
    {
        // The year is required: ToNumber(undefined) is NaN.
        if (a.Length == 0)
            return JSNumber.NaN;

        var (year, month, date, hours, minutes, seconds, milliseconds) = a.Get7Double();
        var time = EcmaTime.MakeDate(
            EcmaTime.MakeDay(EcmaTime.MakeFullYear(year), month, date),
            EcmaTime.MakeTime(hours, minutes, seconds, milliseconds));
        return NumberOf(EcmaTime.TimeClip(time));
    }

    [JSExport("now")]
    internal static JSValue Now(in Arguments a) => new JSNumber(CurrentTime());

    // Date.parse (21.4.3.2).
    [JSExport("parse", Length = 1)]
    internal static JSValue Parse(in Arguments a) => NumberOf(ParseTimeValue(a.Get1().ToString()));

    /// <summary>
    /// The time value of a string, as <c>Date.parse</c> and the constructor read it (ECMA-262 21.4.3.2): the
    /// Date Time String Format, through the shared library's strict dialect; then this engine's own fallback,
    /// <see cref="DateParser"/> (decision SJL-D-17); then, for what both refuse, Broiler.VM's dialect
    /// (JSD-0072), which reads among others this engine's renderings of the years outside 0001 to 9999. A
    /// local time is read through UTC over the host's zone. NaN when no grammar reads the string.
    /// </summary>
    internal static double ParseTimeValue(string text)
    {
        var parsed = EcmaDateParser.Parse(text, DateParseDialect.Strict);
        if (parsed.Kind == EcmaParsedKind.Invalid)
            parsed = DateParser.Parse(text);
        if (parsed.Kind == EcmaParsedKind.Invalid)
            parsed = EcmaDateParser.Parse(text, DateParseDialect.BroilerVM);

        return parsed.Kind switch
        {
            EcmaParsedKind.Utc => parsed.Value,
            EcmaParsedKind.Local => EcmaTime.TimeClip(EcmaLocalTime.Utc(parsed.Value, Zone)),
            _ => double.NaN,
        };
    }
}
