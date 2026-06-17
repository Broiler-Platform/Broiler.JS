using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// Temporal.Duration (Temporal proposal §7). A Duration is an immutable record of ten signed
// integer components (years … nanoseconds) sharing one sign. This implements construction,
// from()/compare(), the component/sign/blank accessors, with()/negated()/abs(), and ISO-8601
// string round-tripping. Calendar-dependent arithmetic (round/total/add with a relativeTo
// that needs a calendar) is only supported for the calendar-independent (time + days) case;
// otherwise a RangeError is raised, matching the spec's "relativeTo is required" paths.
//
// Registered under the Temporal namespace (not as a global) via Register = false.
[JSClassGenerator("Duration", Register = false)]
public partial class JSTemporalDuration : JSObject
{
    internal readonly double years, months, weeks, days, hours, minutes, seconds, milliseconds, microseconds, nanoseconds;

    [JSExport(Length = 0)]
    public JSTemporalDuration(in Arguments a) : base(ResolvePrototype())
    {
        years = ToIntegerIfIntegral(a.GetAt(0));
        months = ToIntegerIfIntegral(a.GetAt(1));
        weeks = ToIntegerIfIntegral(a.GetAt(2));
        days = ToIntegerIfIntegral(a.GetAt(3));
        hours = ToIntegerIfIntegral(a.GetAt(4));
        minutes = ToIntegerIfIntegral(a.GetAt(5));
        seconds = ToIntegerIfIntegral(a.GetAt(6));
        milliseconds = ToIntegerIfIntegral(a.GetAt(7));
        microseconds = ToIntegerIfIntegral(a.GetAt(8));
        nanoseconds = ToIntegerIfIntegral(a.GetAt(9));

        RejectDuration(years, months, weeks, days, hours, minutes, seconds, milliseconds, microseconds, nanoseconds);
    }

    internal JSTemporalDuration(
        double years, double months, double weeks, double days, double hours,
        double minutes, double seconds, double milliseconds, double microseconds, double nanoseconds,
        JSObject prototype) : base(prototype)
    {
        // A Temporal.Duration field is never negative zero (CreateDurationRecord stores mathematical
        // values); operations like negated() / since / balancing can produce -0 from `-1 * 0`, so
        // canonicalize every component here.
        this.years = NoNegativeZero(years); this.months = NoNegativeZero(months); this.weeks = NoNegativeZero(weeks);
        this.days = NoNegativeZero(days); this.hours = NoNegativeZero(hours); this.minutes = NoNegativeZero(minutes);
        this.seconds = NoNegativeZero(seconds); this.milliseconds = NoNegativeZero(milliseconds);
        this.microseconds = NoNegativeZero(microseconds); this.nanoseconds = NoNegativeZero(nanoseconds);
    }

    private static double NoNegativeZero(double v) => v == 0 ? 0.0 : v;

    private static JSObject ResolvePrototype()
    {
        if (JSEngine.NewTarget == null && (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget == null)
            throw JSEngine.NewTypeError("Constructor Temporal.Duration requires 'new'");

        return JSEngine.NewTargetPrototype ?? DurationPrototype;
    }

    // The realm's %Temporal.Duration.prototype%, for instances created internally.
    internal static JSObject DurationPrototype
    {
        get
        {
            var temporal = (JSEngine.Current as JSObject)?[KeyStrings.GetOrCreate("Temporal")] as JSObject;
            return (temporal?[KeyStrings.GetOrCreate("Duration")] as JSFunction)?.prototype;
        }
    }

    // ── accessors ───────────────────────────────────────────────────────────────

    [JSExport("years")] public double YearsValue => years;
    [JSExport("months")] public double MonthsValue => months;
    [JSExport("weeks")] public double WeeksValue => weeks;
    [JSExport("days")] public double DaysValue => days;
    [JSExport("hours")] public double HoursValue => hours;
    [JSExport("minutes")] public double MinutesValue => minutes;
    [JSExport("seconds")] public double SecondsValue => seconds;
    [JSExport("milliseconds")] public double MillisecondsValue => milliseconds;
    [JSExport("microseconds")] public double MicrosecondsValue => microseconds;
    [JSExport("nanoseconds")] public double NanosecondsValue => nanoseconds;

    [JSExport("sign")] public double Sign => DurationSign();
    [JSExport("blank")] public bool Blank => DurationSign() == 0;

    private int DurationSign() => DurationSign(years, months, weeks, days, hours, minutes, seconds, milliseconds, microseconds, nanoseconds);

    // ── methods ─────────────────────────────────────────────────────────────────

    [JSExport("negated", Length = 0)]
    public JSValue Negated(in Arguments a)
        => new JSTemporalDuration(-years, -months, -weeks, -days, -hours, -minutes, -seconds, -milliseconds, -microseconds, -nanoseconds, DurationPrototype);

    [JSExport("abs", Length = 0)]
    public JSValue Abs(in Arguments a)
        => new JSTemporalDuration(Math.Abs(years), Math.Abs(months), Math.Abs(weeks), Math.Abs(days), Math.Abs(hours), Math.Abs(minutes), Math.Abs(seconds), Math.Abs(milliseconds), Math.Abs(microseconds), Math.Abs(nanoseconds), DurationPrototype);

    [JSExport("with", Length = 1)]
    public JSValue With(in Arguments a)
    {
        if (a.GetAt(0) is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.Duration.prototype.with requires an object");

        // At least one recognized field must be present (PrepareTemporalFields with a
        // partial record rejects a wholly empty bag).
        var any = false;
        double Read(string name, double current)
        {
            var v = obj[KeyStrings.GetOrCreate(name)];
            if (v.IsUndefined) return current;
            any = true;
            return ToIntegerIfIntegral(v);
        }

        // ToTemporalPartialDurationRecord reads the fields in alphabetical order.
        var d = Read("days", days);
        var h = Read("hours", hours);
        var us = Read("microseconds", microseconds);
        var ms = Read("milliseconds", milliseconds);
        var mi = Read("minutes", minutes);
        var mo = Read("months", months);
        var ns = Read("nanoseconds", nanoseconds);
        var s = Read("seconds", seconds);
        var w = Read("weeks", weeks);
        var y = Read("years", years);

        if (!any)
            throw JSEngine.NewTypeError("Temporal.Duration.prototype.with requires at least one duration property");

        RejectDuration(y, mo, w, d, h, mi, s, ms, us, ns);
        return new JSTemporalDuration(y, mo, w, d, h, mi, s, ms, us, ns, DurationPrototype);
    }

    [JSExport("toString", Length = 0)]
    public JSValue ToStringMethod(in Arguments a)
    {
        var optionsArg = a.GetAt(0);
        var digits = -1; // "auto"
        var roundingMode = "trunc";
        string smallestUnit = null;

        if (optionsArg != null && !optionsArg.IsUndefined)
        {
            if (optionsArg is not JSObject options)
                throw JSEngine.NewTypeError("Temporal.Duration.toString options must be an object or undefined");

            digits = TemporalRoundingOptions.GetFractionalSecondDigits(options);
            roundingMode = TemporalRoundingOptions.GetRoundingMode(options, "trunc");

            var su = options[KeyStrings.GetOrCreate("smallestUnit")];
            if (!su.IsUndefined)
            {
                smallestUnit = TemporalRoundingOptions.NormalizeTimeUnit(su.StringValue, allowAuto: false);
                if (smallestUnit is "hour" or "minute")
                    throw JSEngine.NewRangeError($"Temporal.Duration.toString: smallestUnit \"{smallestUnit}\" is not supported");
            }
        }

        // ToSecondsStringPrecisionRecord: precision -1 = auto, 0..9 = fixed fractional-second
        // digits; incrementNs is the rounding step applied to the seconds field.
        int precision;
        long incrementNs;
        if (smallestUnit != null)
        {
            (precision, incrementNs) = smallestUnit switch
            {
                "second" => (0, 1_000_000_000L),
                "millisecond" => (3, 1_000_000L),
                "microsecond" => (6, 1_000L),
                _ => (9, 1L), // nanosecond
            };
        }
        else if (digits == -1) { precision = -1; incrementNs = 1; }
        else { precision = digits; incrementNs = TemporalRoundingOptions.Pow10(9 - digits); }

        return new JSString(ToISOString(precision, incrementNs, roundingMode));
    }

    [JSExport("toJSON", Length = 0)]
    public JSValue ToJSON(in Arguments a) => new JSString(ToISOString(-1, 1, "trunc"));

    [JSExport("toLocaleString", Length = 0)]
    public JSValue ToLocaleString(in Arguments a) => new JSString(ToISOString(-1, 1, "trunc"));

    [JSExport("valueOf", Length = 0)]
    public JSValue ValueOf(in Arguments a)
        => throw JSEngine.NewTypeError("Called Temporal.Duration.prototype.valueOf, which is not supported. Use Temporal.Duration.compare for comparison.");

    // add / subtract / round / total: the calendar-independent (time + 24h-day) cases are
    // implemented in JSTemporalDuration.Arithmetic.cs; calendar-unit / relativeTo cases throw.
    [JSExport("add", Length = 1)]
    public JSValue Add(in Arguments a) => AddSubtract(a.GetAt(0), a.GetAt(1), 1);

    [JSExport("subtract", Length = 1)]
    public JSValue Subtract(in Arguments a) => AddSubtract(a.GetAt(0), a.GetAt(1), -1);

    [JSExport("round", Length = 1)]
    public JSValue Round(in Arguments a) => RoundImpl(a.GetAt(0));

    [JSExport("total", Length = 1)]
    public JSValue Total(in Arguments a) => TotalImpl(a.GetAt(0));

    // ── statics ─────────────────────────────────────────────────────────────────

    [JSExport("from", Length = 1)]
    internal static JSValue From(in Arguments a)
    {
        var item = a.GetAt(0);
        if (item is JSTemporalDuration d)
            return new JSTemporalDuration(d.years, d.months, d.weeks, d.days, d.hours, d.minutes, d.seconds, d.milliseconds, d.microseconds, d.nanoseconds, DurationPrototype);

        return ToTemporalDuration(item);
    }

    [JSExport("compare", Length = 2)]
    internal static JSValue Compare(in Arguments a)
    {
        var one = ToTemporalDuration(a.GetAt(0));
        var two = ToTemporalDuration(a.GetAt(1));

        var d1 = (JSTemporalDuration)one;
        var d2 = (JSTemporalDuration)two;

        // GetTemporalRelativeToOption: only the presence is observed here.
        var options = a.GetAt(2);
        var relativeTo = JSUndefined.Value;
        if (!options.IsUndefined)
        {
            if (options is not JSObject optionsObject)
                throw JSEngine.NewTypeError("Temporal.Duration.compare options must be an object");
            relativeTo = optionsObject[KeyStrings.GetOrCreate("relativeTo")];
        }

        var calendarUnits = d1.years != 0 || d1.months != 0 || d1.weeks != 0
            || d2.years != 0 || d2.months != 0 || d2.weeks != 0;

        // ToRelativeTemporalObject resolves (and validates) relativeTo whenever it is present, even when
        // the comparison itself does not need it — an unparsable or otherwise invalid relativeTo string
        // is a RangeError regardless of the durations' units. The spec resolves relativeTo before the
        // identical-durations short-circuit below, so an invalid relativeTo still throws here.
        JSTemporalZonedDateTime relZoned = null;
        JSTemporalPlainDate relDate = null;
        if (!relativeTo.IsNullOrUndefined)
            TryResolveRelativeTo(options, out relZoned, out relDate);

        // If the two durations are identical in every field, the result is +0 regardless of relativeTo.
        // The spec applies this short-circuit before the calendar-units requirement, so comparing a
        // duration that has calendar units to itself returns 0 rather than throwing for a missing relativeTo.
        if (d1.years == d2.years && d1.months == d2.months && d1.weeks == d2.weeks
            && d1.days == d2.days && d1.hours == d2.hours && d1.minutes == d2.minutes
            && d1.seconds == d2.seconds && d1.milliseconds == d2.milliseconds
            && d1.microseconds == d2.microseconds && d1.nanoseconds == d2.nanoseconds)
            return new JSNumber(0);

        if (calendarUnits && relativeTo.IsNullOrUndefined)
            throw JSEngine.NewRangeError("Temporal.Duration.compare with calendar units requires a relativeTo option");

        // The spec only adds each duration to the zoned relativeTo when one of the durations has a
        // date-category largest unit (a non-zero year/month/week/day); a purely time-based comparison
        // never touches the relativeTo, so a boundary relativeTo whose instant is representable does not
        // overflow even though adding the time would (test262 compare/relativeto-string-limits).
        var dateUnits = calendarUnits || d1.days != 0 || d2.days != 0;

        if (relZoned != null && dateUnits)
        {
            // Add each duration to the (shared) zoned relativeTo and compare the resulting instants,
            // so a day spans its real DST-adjusted length.
            var e1 = relZoned.AddDurationEpochNs(d1);
            var e2 = relZoned.AddDurationEpochNs(d2);
            return new JSNumber(e1 < e2 ? -1 : e1 > e2 ? 1 : 0);
        }

        if (calendarUnits)
        {
            // Add each duration's date part to the (shared) relativeTo date and compare the resulting
            // instants on the 24-hour-day timeline.
            var (end1, _, _) = d1.RelativeEndpoints(relDate);
            var (end2, _, _) = d2.RelativeEndpoints(relDate);
            return new JSNumber(end1 < end2 ? -1 : end1 > end2 ? 1 : 0);
        }

        var ns1 = d1.TotalNanoseconds();
        var ns2 = d2.TotalNanoseconds();
        return new JSNumber(ns1 < ns2 ? -1 : ns1 > ns2 ? 1 : 0);
    }

    // ── helpers ─────────────────────────────────────────────────────────────────

    // Total nanoseconds for the calendar-independent part (days treated as 24h).
    private BigInteger TotalNanoseconds()
    {
        BigInteger ToBig(double v) => new(v);
        return ToBig(days) * 86_400_000_000_000
            + ToBig(hours) * 3_600_000_000_000
            + ToBig(minutes) * 60_000_000_000
            + ToBig(seconds) * 1_000_000_000
            + ToBig(milliseconds) * 1_000_000
            + ToBig(microseconds) * 1_000
            + ToBig(nanoseconds);
    }

    private static double ToIntegerIfIntegral(JSValue value)
    {
        if (value == null || value.IsUndefined)
            return 0;

        var number = value.DoubleValue; // ToNumber (throws TypeError for BigInt/Symbol)
        if (double.IsNaN(number) || double.IsInfinity(number))
            throw JSEngine.NewRangeError("Temporal.Duration field must be a finite integer");

        if (Math.Truncate(number) != number)
            throw JSEngine.NewRangeError("Temporal.Duration field must be an integer");

        return number == 0 ? 0 : number; // normalize -0 → 0
    }

    private static int DurationSign(params double[] components)
    {
        foreach (var v in components)
        {
            if (v < 0) return -1;
            if (v > 0) return 1;
        }
        return 0;
    }

    // RejectDuration / IsValidDuration: every component must share the duration's sign, and
    // the magnitudes must stay within the spec limits (years…days < 2^32, and the total
    // time in seconds < 2^53).
    private static void RejectDuration(
        double years, double months, double weeks, double days, double hours,
        double minutes, double seconds, double milliseconds, double microseconds, double nanoseconds)
    {
        var sign = DurationSign(years, months, weeks, days, hours, minutes, seconds, milliseconds, microseconds, nanoseconds);

        void Check(double v)
        {
            if (!double.IsFinite(v))
                throw JSEngine.NewRangeError("Temporal.Duration field must be finite");
            if (v < 0 && sign > 0)
                throw JSEngine.NewRangeError("Temporal.Duration fields must have a consistent sign");
            if (v > 0 && sign < 0)
                throw JSEngine.NewRangeError("Temporal.Duration fields must have a consistent sign");
        }

        Check(years); Check(months); Check(weeks); Check(days); Check(hours);
        Check(minutes); Check(seconds); Check(milliseconds); Check(microseconds); Check(nanoseconds);

        // Only years, months and weeks are bounded by 2^32; days are limited only by the total-time
        // bound below (a duration may carry up to ~1.04e11 days — 2^53 seconds — of elapsed time).
        if (Math.Abs(years) >= 4294967296d || Math.Abs(months) >= 4294967296d || Math.Abs(weeks) >= 4294967296d)
            throw JSEngine.NewRangeError("Temporal.Duration date fields are out of range");

        // The total elapsed time (days + the time components) must be under 2^53 seconds. The
        // components are integer-valued, so the sum is computed exactly in nanoseconds via BigInteger
        // to honour the precise mathematical boundary (a floating-point sum would round near 2^53).
        var totalNs = (BigInteger)days * 86_400_000_000_000
            + (BigInteger)hours * 3_600_000_000_000
            + (BigInteger)minutes * 60_000_000_000
            + (BigInteger)seconds * 1_000_000_000
            + (BigInteger)milliseconds * 1_000_000
            + (BigInteger)microseconds * 1_000
            + (BigInteger)nanoseconds;
        if (BigInteger.Abs(totalNs) >= (BigInteger)9007199254740992d * 1_000_000_000) // 2^53 seconds
            throw JSEngine.NewRangeError("Temporal.Duration is out of range");
    }

    internal static JSValue ToTemporalDuration(JSValue item)
    {
        if (item is JSTemporalDuration d)
            return new JSTemporalDuration(d.years, d.months, d.weeks, d.days, d.hours, d.minutes, d.seconds, d.milliseconds, d.microseconds, d.nanoseconds, DurationPrototype);

        if (item.IsString)
            return ParseTemporalDurationString(item.ToString());

        if (item is not JSObject obj)
            throw JSEngine.NewTypeError("Temporal.Duration: invalid duration value");

        // ToTemporalDurationRecord: read each field; at least one must be present.
        var any = false;
        double Field(string name)
        {
            var v = obj[KeyStrings.GetOrCreate(name)];
            if (v.IsUndefined) return 0;
            any = true;
            return ToIntegerIfIntegral(v);
        }

        // ToTemporalDurationRecord reads the fields in alphabetical order.
        var dd = Field("days"); var h = Field("hours"); var us = Field("microseconds");
        var ms = Field("milliseconds"); var mi = Field("minutes"); var mo = Field("months");
        var ns = Field("nanoseconds"); var s = Field("seconds"); var w = Field("weeks");
        var y = Field("years");

        if (!any)
            throw JSEngine.NewTypeError("Temporal.Duration: object has no duration properties");

        RejectDuration(y, mo, w, dd, h, mi, s, ms, us, ns);
        return new JSTemporalDuration(y, mo, w, dd, h, mi, s, ms, us, ns, DurationPrototype);
    }

    private static readonly Regex DurationPattern = new(
        @"^([+-])?[Pp](?:(\d+)[Yy])?(?:(\d+)[Mm])?(?:(\d+)[Ww])?(?:(\d+)[Dd])?(?:[Tt](?:(\d+)(?:[.,](\d{1,9}))?[Hh])?(?:(\d+)(?:[.,](\d{1,9}))?[Mm])?(?:(\d+)(?:[.,](\d{1,9}))?[Ss])?)?$",
        RegexOptions.CultureInvariant);

    private static JSValue ParseTemporalDurationString(string text)
    {
        var match = DurationPattern.Match(text);
        if (!match.Success)
            throw JSEngine.NewRangeError($"Cannot parse Temporal.Duration from \"{text}\"");

        double G(int i) => match.Groups[i].Success ? double.Parse(match.Groups[i].Value, CultureInfo.InvariantCulture) : 0;
        bool Has(int i) => match.Groups[i].Success;

        // At least one component must be present, and a `T` with no time component is invalid.
        if (!Has(2) && !Has(3) && !Has(4) && !Has(5) && !Has(6) && !Has(8) && !Has(10))
            throw JSEngine.NewRangeError($"Cannot parse Temporal.Duration from \"{text}\"");

        // A fraction may only appear on the smallest provided time unit; cascade it down.
        if ((Has(7) && (Has(8) || Has(10))) || (Has(9) && Has(10)))
            throw JSEngine.NewRangeError($"Cannot parse Temporal.Duration from \"{text}\"");

        var sign = match.Groups[1].Value is "-" or "−" ? -1 : 1;

        var years = G(2); var months = G(3); var weeks = G(4); var days = G(5);
        var hours = G(6); var minutes = G(8); var seconds = G(10);
        double milliseconds = 0, microseconds = 0, nanoseconds = 0;

        // Distribute the (single) fractional part to nanosecond resolution.
        BigInteger fractionNs = 0;
        if (Has(7)) fractionNs = FractionToNanoseconds(match.Groups[7].Value, 3_600_000_000_000);
        else if (Has(9)) fractionNs = FractionToNanoseconds(match.Groups[9].Value, 60_000_000_000);
        else if (Has(11)) fractionNs = FractionToNanoseconds(match.Groups[11].Value, 1_000_000_000);

        if (fractionNs != 0)
        {
            minutes += (double)(fractionNs / 60_000_000_000); fractionNs %= 60_000_000_000;
            seconds += (double)(fractionNs / 1_000_000_000); fractionNs %= 1_000_000_000;
            milliseconds = (double)(fractionNs / 1_000_000); fractionNs %= 1_000_000;
            microseconds = (double)(fractionNs / 1_000); fractionNs %= 1_000;
            nanoseconds = (double)fractionNs;
        }

        RejectDuration(sign * years, sign * months, sign * weeks, sign * days, sign * hours,
            sign * minutes, sign * seconds, sign * milliseconds, sign * microseconds, sign * nanoseconds);

        return new JSTemporalDuration(sign * years, sign * months, sign * weeks, sign * days, sign * hours,
            sign * minutes, sign * seconds, sign * milliseconds, sign * microseconds, sign * nanoseconds, DurationPrototype);
    }

    // digits/10^len of unitNs, as an exact nanosecond count.
    private static BigInteger FractionToNanoseconds(string digits, long unitNs)
    {
        var numerator = BigInteger.Parse(digits, CultureInfo.InvariantCulture) * unitNs;
        var denominator = BigInteger.Pow(10, digits.Length);
        return numerator / denominator;
    }

    // TemporalDurationToString: ISO-8601 with the seconds field assembled from seconds/ms/us/ns.
    // `precision` controls the fractional-second digits (-1 = auto, 0..9 = fixed); `incrementNs`
    // and `roundingMode` round the combined seconds value to that precision.
    private string ToISOString(int precision, long incrementNs, string roundingMode)
    {
        var sign = DurationSign();
        var sb = new StringBuilder();
        if (sign < 0) sb.Append('-');
        sb.Append('P');

        void DatePart(BigInteger v, char unit)
        {
            if (!v.IsZero) sb.Append(v.ToString(CultureInfo.InvariantCulture)).Append(unit);
        }

        var absDays = new BigInteger(Math.Abs(days));

        // The whole-seconds and sub-second components combined into a nanosecond magnitude.
        var secondsNs = new BigInteger(Math.Abs(seconds)) * 1_000_000_000
            + new BigInteger(Math.Abs(milliseconds)) * 1_000_000
            + new BigInteger(Math.Abs(microseconds)) * 1_000
            + new BigInteger(Math.Abs(nanoseconds));

        BigInteger hoursOut = new(Math.Abs(hours));
        BigInteger minutesOut = new(Math.Abs(minutes));
        BigInteger wholeSeconds = secondsNs / 1_000_000_000;
        var fraction = (long)(secondsNs % 1_000_000_000);
        var daysOut = absDays;

        if (incrementNs > 1)
        {
            // Round the WHOLE time portion (hours + minutes + seconds + sub-seconds) to the
            // requested precision, then balance the result back up to (at most) the
            // duration's largest unit, floored at "second" — TemporalDurationToString uses
            // roundedLargestUnit = LargerOfTwoTemporalUnits(DefaultTemporalLargestUnit, "second").
            // So rounding can cross unit boundaries: seconds into minutes/hours and, when the
            // largest unit is day or larger, into days — but never up into weeks/months/years.
            var timeNs = hoursOut * 3_600_000_000_000
                + minutesOut * 60_000_000_000
                + secondsNs;

            var signed = sign < 0 ? -timeNs : timeNs;
            signed = TemporalRoundingOptions.RoundToIncrement(signed, incrementNs, roundingMode);
            timeNs = BigInteger.Abs(signed);

            // TemporalDurationFromInternal: the rounded duration must still be valid — rounding the
            // time up (ceil/expand) can push the total elapsed time to/over the 2^53-second limit.
            var roundedTimeNs = absDays * 86_400_000_000_000 + timeNs;
            if (roundedTimeNs >= (BigInteger)9007199254740992d * 1_000_000_000)
                throw JSEngine.NewRangeError("Temporal.Duration: rounded duration is out of range");

            fraction = (long)(timeNs % 1_000_000_000);
            var totalSeconds = timeNs / 1_000_000_000;

            // cap = the rank of LargerOfTwoTemporalUnits(DefaultTemporalLargestUnit, "second").
            // Ranks: day=6, hour=5, minute=4, second(+finer)=3.
            var cap = Math.Max(DefaultLargestUnitRank(), 3);
            if (cap >= 6) // day or larger: time balances all the way into days
            {
                wholeSeconds = totalSeconds % 60;
                var totalMinutes = totalSeconds / 60;
                minutesOut = totalMinutes % 60;
                var totalHours = totalMinutes / 60;
                hoursOut = totalHours % 24;
                daysOut = absDays + totalHours / 24;
            }
            else if (cap == 5) // hour: balance up to (uncapped) hours
            {
                wholeSeconds = totalSeconds % 60;
                var totalMinutes = totalSeconds / 60;
                minutesOut = totalMinutes % 60;
                hoursOut = totalMinutes / 60;
            }
            else if (cap == 4) // minute: balance up to (uncapped) minutes
            {
                wholeSeconds = totalSeconds % 60;
                minutesOut = totalSeconds / 60;
                hoursOut = BigInteger.Zero;
            }
            else // second or finer: seconds stay as a single (possibly >= 60) value
            {
                wholeSeconds = totalSeconds;
                minutesOut = BigInteger.Zero;
                hoursOut = BigInteger.Zero;
            }
        }

        DatePart(new BigInteger(Math.Abs(years)), 'Y');
        DatePart(new BigInteger(Math.Abs(months)), 'M');
        DatePart(new BigInteger(Math.Abs(weeks)), 'W');
        DatePart(daysOut, 'D');

        // With a fixed precision the seconds field is always emitted; with auto it appears only
        // when nonzero.
        var showSeconds = !wholeSeconds.IsZero || fraction != 0 || precision >= 0;
        var hasTime = !hoursOut.IsZero || !minutesOut.IsZero || showSeconds;
        if (hasTime)
        {
            sb.Append('T');
            if (!hoursOut.IsZero) sb.Append(hoursOut.ToString(CultureInfo.InvariantCulture)).Append('H');
            if (!minutesOut.IsZero) sb.Append(minutesOut.ToString(CultureInfo.InvariantCulture)).Append('M');
            if (showSeconds)
            {
                sb.Append(wholeSeconds.ToString(CultureInfo.InvariantCulture));
                JSTemporalInstant.AppendFraction(sb, fraction, precision);
                sb.Append('S');
            }
        }

        // The zero duration is "PT0S".
        if (sb.Length == (sign < 0 ? 2 : 1))
            sb.Append("T0S");

        return sb.ToString();
    }

    // DefaultTemporalLargestUnit rank: the largest unit with a nonzero value.
    // year=9, month=8, week=7, day=6, hour=5, minute=4, second=3, ms=2, us=1, ns=0.
    private int DefaultLargestUnitRank()
    {
        if (years != 0) return 9;
        if (months != 0) return 8;
        if (weeks != 0) return 7;
        if (days != 0) return 6;
        if (hours != 0) return 5;
        if (minutes != 0) return 4;
        if (seconds != 0) return 3;
        if (milliseconds != 0) return 2;
        if (microseconds != 0) return 1;
        return 0;
    }
}
