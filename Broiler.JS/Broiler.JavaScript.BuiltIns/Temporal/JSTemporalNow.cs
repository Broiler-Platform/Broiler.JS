using System;
using System.Numerics;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// Temporal.Now (Temporal proposal §2): a namespace exposing the current instant / date / time.
// The host clock is DateTimeOffset.UtcNow; the default time zone is SystemTimeZoneIdentifier, the
// host's zone resolved through the shared time zone database to its primary identifier, or UTC
// where the database does not hold it (milestone SJL-8.6): the zone Date's local time and the
// default Intl.DateTimeFormat zone read too. The zoned/plain ISO variants are derived by
// interpreting the current instant in the requested (or system) zone via Temporal.ZonedDateTime.
internal static class JSTemporalNow
{
    private static BigInteger CurrentEpochNanoseconds()
    {
        // DateTimeOffset ticks are 100 ns; convert to ns since the Unix epoch.
        var ticks = DateTimeOffset.UtcNow.UtcTicks - 621355968000000000L;
        return new BigInteger(ticks) * 100;
    }

    internal static string SystemTimeZoneId() => TemporalTimeZones.SystemTimeZoneIdentifier();

    private static string ResolveTimeZoneArg(JSValue arg)
    {
        if (arg == null || arg.IsUndefined)
            return SystemTimeZoneId();
        if (arg is JSTemporalZonedDateTime zdt)
            return zdt.timeZoneId;
        if (arg.IsString)
            return arg.ToString();
        throw JSEngine.NewTypeError("Temporal.Now: time zone must be a string");
    }

    internal static JSValue TimeZoneId(in Arguments a)
        => new JSString(SystemTimeZoneId());

    internal static JSValue Instant(in Arguments a)
        => new JSTemporalInstant(CurrentEpochNanoseconds(), JSTemporalInstant.InstantPrototype);

    internal static JSValue ZonedDateTimeISO(in Arguments a)
        => JSTemporalZonedDateTime.CreateChecked(CurrentEpochNanoseconds(), ResolveTimeZoneArg(a.GetAt(0)));

    internal static JSValue PlainDateTimeISO(in Arguments a)
        => CurrentZoned(a.GetAt(0)).ToPlainDateTime(Arguments.Empty);

    internal static JSValue PlainDateISO(in Arguments a)
        => CurrentZoned(a.GetAt(0)).ToPlainDate(Arguments.Empty);

    internal static JSValue PlainTimeISO(in Arguments a)
        => CurrentZoned(a.GetAt(0)).ToPlainTime(Arguments.Empty);

    private static JSTemporalZonedDateTime CurrentZoned(JSValue timeZoneArg)
        => (JSTemporalZonedDateTime)JSTemporalZonedDateTime.CreateChecked(
            CurrentEpochNanoseconds(), ResolveTimeZoneArg(timeZoneArg));
}
