using Broiler.ECMAScript.Time;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using System;

namespace Broiler.JavaScript.BuiltIns.Date;

// Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=1; Fingerprint=TBF
// Broiler-Falsified-If: new Date(ms) for an instant inside daylight-saving time reports a getHours value one hour off the local hour implied by getTimezoneOffset
// Broiler-Human:        PENDING
[JSFunctionGenerator("Date")]
public partial class JSDate: JSObject
{
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 21.4.1.21; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: LocalTime, UTC or TimeZoneString reads a zone other than the host's, resolved through the shared time zone database, at the instant
    // Broiler-Human:        PENDING
    internal static readonly ILocalTimeProvider Zone = new HostLocalTimeProvider();

    // The instants a DateTimeOffset holds: 0001-01-01T00:00:00.000Z to 9999-12-31T23:59:59.999Z.
    private const double FirstViewTime = -62135596800000d;
    private const double LastViewTime = 253402300799999d;

    /// <summary>
    /// [[DateValue]] (ECMA-262 21.4): this date's time value, NaN for an invalid date. It is the date's only
    /// storage; <see cref="Value"/> and <see cref="DateTime"/> are views of it.
    /// </summary>
    internal double timeValue = double.NaN;

    /// <summary>
    /// The time value as a <see cref="DateTimeOffset"/>: the instant at the host's offset in force at it, or at
    /// offset zero where its local reading is outside <see cref="DateTimeOffset"/>'s range; and
    /// <see cref="DateTimeOffset.MinValue"/> for an invalid date and for a valid date outside that range. Setting
    /// it stores the instant's time value, and <see cref="DateTimeOffset.MinValue"/> stores an invalid date.
    /// </summary>
    public DateTimeOffset Value
    {
        get => ToView(timeValue);
        set => timeValue = FromView(value);
    }

    /// <summary>The local clock reading of <see cref="Value"/>.</summary>
    public DateTime DateTime => Value.DateTime;

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: the created date holds a time value other than the one passed in
    // Broiler-Human:        PENDING
    internal JSDate(double time) : this() => timeValue = time;

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public JSDate(DateTimeOffset time) : this() => timeValue = FromView(time);

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public override string ToDetailString() => EcmaDateFormatter.ToDateString(timeValue, Zone);

    /// <summary>The time value of the present moment.</summary>
    internal static double CurrentTime() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    private static bool InView(double time) => time >= FirstViewTime && time <= LastViewTime;

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: a valid time value inside DateTimeOffset's range is viewed as a different instant
    // Broiler-Human:        PENDING
    private static DateTimeOffset ToView(double time)
    {
        if (!InView(time))
            return DateTimeOffset.MinValue;

        var instant = DateTimeOffset.FromUnixTimeMilliseconds((long)time);
        var local = TimeZoneInfo.ConvertTime(instant, TimeZoneInfo.Local);

        // ConvertTime answers its range's edge for a local reading outside the range, in the range's first
        // and last hours; the instant at offset zero is the same instant.
        return local.UtcTicks == instant.UtcTicks ? local : instant;
    }

    private static double FromView(DateTimeOffset value) =>
        value == DateTimeOffset.MinValue ? double.NaN : value.ToUnixTimeMilliseconds();

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: a valid Date outside years 1 to 9999 converts to a CLR DateTime or DateTimeOffset and the conversion reports success
    // Broiler-Human:        PENDING
    public override bool ConvertTo(Type type, out object value)
    {
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
        {
            // A valid date the CLR types cannot hold converts to neither; an invalid one converts to the
            // MinValue sentinel, as it always has.
            if (!double.IsNaN(timeValue) && !InView(timeValue))
            {
                value = null!;
                return false;
            }

            var view = Value;
            value = type == typeof(DateTime) ? view.LocalDateTime : view;
            return true;
        }

        if (type.IsAssignableFrom(typeof(JSDate)))
        {
            value = this;
            return true;
        }

        return base.ConvertTo(type, out value);
    }
}
