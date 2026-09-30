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
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: a real time value of 0001-01-01T00:00:00Z is reported as NaN because it equals this sentinel
    // Broiler-Human:        PENDING
    internal static readonly DateTimeOffset InvalidDate = DateTimeOffset.MinValue;
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: script obtains this shared instance and a setter call on it changes the date every other holder sees
    // Broiler-Human:        PENDING
    internal static readonly JSDate invalidDate = new(DateTimeOffset.MinValue);

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: an instant inside daylight-saving time is shifted by the zone's standard offset instead of the offset in force at that instant
    // Broiler-Human:        PENDING
    internal static TimeSpan Local => TimeZoneInfo.Local.BaseUtcOffset;

    internal DateTimeOffset value;

    /// <summary>
    /// Raw ECMAScript time value (ms since epoch) for dates outside
    /// .NET DateTimeOffset range (e.g., year 0 or negative years).
    /// When NaN, the <see cref="value"/> field is authoritative.
    /// </summary>
    internal double rawTimeMs = double.NaN;

    public DateTimeOffset Value
    {
        get => value;
        set => this.value = value;
    }

    public DateTime DateTime => Value.DateTime;

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: the created date carries a prototype other than the one passed in
    // Broiler-Human:        PENDING
    internal JSDate(JSObject prototype, DateTimeOffset time) : base(prototype) => value = time;

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public JSDate(DateTimeOffset time) : this() => value = time;

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public override string ToDetailString() => value.ToString();

    /// <summary>
    /// Returns the ECMAScript time value for this date (ms since epoch).
    /// Uses <see cref="rawTimeMs"/> when set (for dates outside .NET range),
    /// otherwise delegates to <see cref="JSDateStatic.ToJSDate"/>.
    /// </summary>
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: a date holding rawTimeMs returns the .NET sentinel's time instead of rawTimeMs, or an invalid date returns a number
    // Broiler-Human:        PENDING
    internal double GetTimeMs()
    {
        if (!double.IsNaN(rawTimeMs))
            return rawTimeMs;

        return value.ToJSDate();
    }

    /// <summary>
    /// Stores an ECMAScript time value (ms since epoch) as this date's value,
    /// keeping the raw <see cref="rawTimeMs"/> representation when the value falls
    /// outside .NET DateTimeOffset's 1–9999 year range. Returns the stored value.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: a time value inside daylight-saving time is stored at the zone's standard offset, so getHours on new Date(ms) is one hour off the local wall-clock hour
    // Broiler-Human:        PENDING
    internal double SetTimeValue(double ms)
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
            value = DateTimeOffset.FromUnixTimeMilliseconds((long)ms).ToOffset(Local);
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

    /// <summary>
    /// Returns true if this date is valid (not NaN / invalid).
    /// </summary>
    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: a date set to exactly 0001-01-01T00:00:00Z through SetTimeValue reports itself invalid
    // Broiler-Human:        PENDING
    internal bool IsValidDate()
    {
        if (!double.IsNaN(rawTimeMs))
            return true;

        return value != InvalidDate;
    }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: a valid Date outside years 1 to 9999, held only in rawTimeMs, converts to a CLR DateTime of 0001-01-01 and the conversion reports success
    // Broiler-Human:        PENDING
    public override bool ConvertTo(Type type, out object value)
    {
        if (type == typeof(DateTime))
        {
            value = this.value.LocalDateTime;
            return true;
        }

        if (type == typeof(DateTimeOffset))
        {
            value = this.value;
            return true;
        }

        if (type.IsAssignableFrom(typeof(JSDate)))
        {
            value = this;
            return true;
        }

        if (type == typeof(object))
        {
            value = this.value;
            return true;
        }

        return base.ConvertTo(type, out value);
    }
}
