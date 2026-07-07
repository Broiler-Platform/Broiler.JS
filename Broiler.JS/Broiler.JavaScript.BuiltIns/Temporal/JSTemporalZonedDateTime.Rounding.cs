using System;
using System.Numerics;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// Temporal.Duration round / total / compare with a Temporal.ZonedDateTime relativeTo. Implements the
// proposal's RoundRelativeDuration / TotalRelativeDuration over a zoned timeline, where a calendar day
// may be 23 h / 24 h / 25 h across a DST transition. The duration's date part is added to the relativeTo
// in wall-clock space (re-resolving the zone offset) and the time part on the exact epoch-nanosecond
// timeline; rounding then nudges between the two surrounding calendar (or zoned-day) boundaries.
public partial class JSTemporalZonedDateTime
{
    // A calendar date part (years/months/weeks/days) plus a signed nanosecond time part.
    private readonly struct InternalDuration
    {
        public readonly long Years, Months, Weeks, Days;
        public readonly BigInteger Time;
        public InternalDuration(long y, long mo, long w, long d, BigInteger t) { Years = y; Months = mo; Weeks = w; Days = d; Time = t; }
    }

    private static int DateDurationSign(long y, long mo, long w, long d)
        => y != 0 ? Math.Sign(y) : mo != 0 ? Math.Sign(mo) : w != 0 ? Math.Sign(w) : d != 0 ? Math.Sign(d) : 0;

    private static int InternalDurationSign(in InternalDuration x)
    {
        var ds = DateDurationSign(x.Years, x.Months, x.Weeks, x.Days);
        return ds != 0 ? ds : x.Time.Sign;
    }

    private static bool IsCalendarUnit(string u) => u is "year" or "month" or "week";

    private static long TimeUnitNs(string unit) => unit switch
    {
        "day" => NanosecondsPerDay,
        "hour" => 3_600_000_000_000,
        "minute" => 60_000_000_000,
        "second" => 1_000_000_000,
        "millisecond" => 1_000_000,
        "microsecond" => 1_000,
        _ => 1, // nanosecond
    };

    private static string LargerUnit(string a, string b) => UnitRank(a) <= UnitRank(b) ? a : b;

    // ── entry points (called from Temporal.Duration) ────────────────────────────

    // AddZonedDateTime(this, duration): the resulting instant, in epoch-nanoseconds.
    internal BigInteger AddDurationEpochNs(JSTemporalDuration d)
    {
        var years = (long)d.YearsValue; var months = (long)d.MonthsValue;
        var weeks = (long)d.WeeksValue; var days = (long)d.DaysValue;
        var timeNs = DurationTimeNanoseconds(d);

        BigInteger result;
        if (years == 0 && months == 0 && weeks == 0 && days == 0)
            result = epochNanoseconds + timeNs;
        else
        {
            var l = Local();
            var (ry, rm, rd) = AddISODate(l.y, l.mo, l.d, years, months, weeks, days, "constrain");
            result = EpochNsForLocal(ry, rm, rd, l.h, l.mi, l.s, l.ms, l.us, l.ns) + timeNs;
        }
        if (!IsValid(result))
            throw JSEngine.NewRangeError("Temporal.Duration: result is out of range");
        return result;
    }

    internal JSValue RoundDurationRelative(JSTemporalDuration d, string smallestUnit, string largestUnit, long increment, string roundingMode)
    {
        var target = AddDurationEpochNs(d);
        var diff = DifferenceInternal(epochNanoseconds, target, largestUnit);
        var rounded = RoundRelativeDuration(diff, epochNanoseconds, target, largestUnit, increment, smallestUnit, roundingMode);
        return BuildDuration(rounded, largestUnit);
    }

    internal double TotalDurationRelative(JSTemporalDuration d, string unit)
    {
        var target = AddDurationEpochNs(d);
        var diff = DifferenceInternal(epochNanoseconds, target, unit);

        if (IsCalendarUnit(unit) || unit == "day")
        {
            var sign = InternalDurationSign(diff) < 0 ? -1 : 1;
            var o = Local();
            var n = NudgeToCalendarUnit(sign, diff, epochNanoseconds, target, o, 1, unit, "trunc");
            return n.total!.Value;
        }

        // A time unit: the whole elapsed nanoseconds (date part is zero for a time-unit largestUnit)
        // divided by the unit length. Round the EXACT rational once (RatioToDouble) rather than
        // converting both operands to double and dividing, which rounds twice and can be one ULP off
        // (test262 Duration/prototype/total/relativeto-total-of-each-unit "hours"/"minutes").
        var totalNs = diff.Time + (BigInteger)diff.Days * NanosecondsPerDay;
        return JSTemporalDuration.RatioToDouble(totalNs, TimeUnitNs(unit));
    }

    // The signed difference between two instants in this zone as an InternalDuration. For a time-unit
    // largestUnit the date part is zero and the whole nanosecond difference is the time part; otherwise
    // it is the DST-aware calendar difference plus the residual sub-day nanoseconds.
    private InternalDuration DifferenceInternal(BigInteger ns1, BigInteger ns2, string largestUnit)
    {
        if (IsTimeUnit(largestUnit) || ns1 == ns2)
            return new InternalDuration(0, 0, 0, 0, ns2 - ns1);

        var start = LocalAt(ns1);
        var end = LocalAt(ns2);
        var sign = ns2 > ns1 ? 1 : -1;

        int iy = end.y, im = end.mo, id = end.d;
        BigInteger residual;

        // Same calendar day: the date difference is zero and the whole difference is the
        // epoch-nanosecond remainder. Running the day-correction loop here would re-resolve the
        // wall clock through EpochNsForLocal — which, across a DST fall-back, disambiguates the
        // repeated hour to a different offset than the operands and spuriously adds a day
        // (proposal-temporal #3141: same ISO date, opposite-sign wall-clock vs epoch deltas).
        if (start.y == end.y && start.mo == end.mo && start.d == end.d)
        {
            residual = ns2 - ns1;
        }
        else
        {
            var timeNs = TimeOfDayNs(end) - TimeOfDayNs(start);
            var dayCorrection = Math.Sign(timeNs) == -sign ? 1 : 0;
            while (true)
            {
                var epochDay = DaysFromCivil(end.y, end.mo, end.d) - (long)dayCorrection * sign;
                var (cy, cm, cd) = CivilFromDays(epochDay);
                iy = (int)cy; im = (int)cm; id = (int)cd;

                var intermediateNs = EpochNsForLocal(iy, im, id, start.h, start.mi, start.s, start.ms, start.us, start.ns);
                residual = ns2 - intermediateNs;
                if (residual == 0 || residual.Sign == sign)
                    break;

                dayCorrection++;
                if (dayCorrection > 3)
                    break;
            }
        }

        var (years, months, weeks, days) = DifferenceISODate(start.y, start.mo, start.d, iy, im, id, largestUnit);
        return new InternalDuration((long)years, (long)months, (long)weeks, (long)days, residual);
    }

    // ── RoundRelativeDuration ───────────────────────────────────────────────────

    private InternalDuration RoundRelativeDuration(InternalDuration dur, BigInteger originEpochNs, BigInteger destEpochNs,
        string largestUnit, long increment, string smallestUnit, string roundingMode)
    {
        var irregular = IsCalendarUnit(smallestUnit) || smallestUnit == "day";
        var sign = InternalDurationSign(dur) < 0 ? -1 : 1;
        var o = Local();

        InternalDuration result;
        BigInteger nudgedEpochNs;
        bool didExpand;

        if (irregular)
        {
            var n = NudgeToCalendarUnit(sign, dur, originEpochNs, destEpochNs, o, increment, smallestUnit, roundingMode);
            result = n.duration; nudgedEpochNs = n.nudgedEpochNs; didExpand = n.didExpand;
        }
        else
        {
            var n = NudgeToZonedTime(sign, dur, o, increment, smallestUnit, largestUnit, roundingMode);
            result = n.duration; nudgedEpochNs = n.nudgedEpochNs; didExpand = n.didExpand;
        }

        if (didExpand && smallestUnit != "week")
            result = BubbleRelativeDuration(sign, result, nudgedEpochNs, o, largestUnit, LargerUnit(smallestUnit, "day"));

        return result;
    }

    private readonly struct NudgeResult
    {
        public readonly InternalDuration duration;
        public readonly BigInteger nudgedEpochNs;
        public readonly bool didExpand;
        public readonly double? total;
        public NudgeResult(InternalDuration d, BigInteger n, bool e, double? t) { duration = d; nudgedEpochNs = n; didExpand = e; total = t; }
    }

    private NudgeResult NudgeToCalendarUnit(int sign, in InternalDuration dur, BigInteger originEpochNs, BigInteger destEpochNs,
        (int y, int mo, int d, int h, int mi, int s, int ms, int us, int ns) o, long increment, string unit, string roundingMode)
    {
        var didExpand = false;
        var w = ComputeNudgeWindow(sign, dur, originEpochNs, o, increment, unit, false);

        var inBounds = sign == 1
            ? w.StartEpochNs <= destEpochNs && destEpochNs <= w.EndEpochNs
            : w.EndEpochNs <= destEpochNs && destEpochNs <= w.StartEpochNs;
        if (!inBounds && unit is "year" or "month")
        {
            w = ComputeNudgeWindow(sign, dur, originEpochNs, o, increment, unit, true);
            didExpand = true;
        }

        var numerator = destEpochNs - w.StartEpochNs;
        var denominator = w.EndEpochNs - w.StartEpochNs;
        var absNum = BigInteger.Abs(numerator);
        var absDen = BigInteger.Abs(denominator);

        bool isEnd;
        if (numerator.IsZero) isEnd = false;
        else if (absNum == absDen) isEnd = true;
        else
        {
            var cmp = (absNum * 2).CompareTo(absDen);
            var even = (Math.Abs(w.R1) / increment) % 2 == 0;
            isEnd = ApplyRoundingPicksEnd(cmp, even, UnsignedRoundingMode(roundingMode, sign < 0));
        }

        double? total = null;
        if (increment == 1 && !absDen.IsZero)
            total = (double)w.R1 + sign * (double)absNum / (double)absDen;

        didExpand = didExpand || isEnd;
        var chosen = isEnd ? w.EndDur : w.StartDur;
        var resultDur = new InternalDuration(chosen.y, chosen.mo, chosen.w, chosen.d, BigInteger.Zero);
        var nudgedEpochNs = didExpand ? w.EndEpochNs : w.StartEpochNs;
        return new NudgeResult(resultDur, nudgedEpochNs, didExpand, total);
    }

    private readonly struct NudgeWindow
    {
        public readonly long R1, R2;
        public readonly BigInteger StartEpochNs, EndEpochNs;
        public readonly (long y, long mo, long w, long d) StartDur, EndDur;
        public NudgeWindow(long r1, long r2, BigInteger s, BigInteger e, (long, long, long, long) sd, (long, long, long, long) ed)
        { R1 = r1; R2 = r2; StartEpochNs = s; EndEpochNs = e; StartDur = sd; EndDur = ed; }
    }

    private NudgeWindow ComputeNudgeWindow(int sign, in InternalDuration dur, BigInteger originEpochNs,
        (int y, int mo, int d, int h, int mi, int s, int ms, int us, int ns) o, long increment, string unit, bool additionalShift)
    {
        long r1, r2;
        (long y, long mo, long w, long d) startDur, endDur;
        switch (unit)
        {
            case "year":
            {
                var years = dur.Years / increment * increment;
                r1 = additionalShift ? years + increment * sign : years;
                r2 = r1 + increment * sign;
                startDur = (r1, 0, 0, 0); endDur = (r2, 0, 0, 0);
                break;
            }
            case "month":
            {
                var months = dur.Months / increment * increment;
                r1 = additionalShift ? months + increment * sign : months;
                r2 = r1 + increment * sign;
                startDur = (dur.Years, r1, 0, 0); endDur = (dur.Years, r2, 0, 0);
                break;
            }
            case "week":
            {
                var (ws_y, ws_mo, ws_d) = AddISODate(o.y, o.mo, o.d, dur.Years, dur.Months, 0, 0, "constrain");
                var (we_y, we_mo, we_d) = CivilFromDays(DaysFromCivil(ws_y, ws_mo, ws_d) + dur.Days);
                var until = DifferenceISODate(ws_y, ws_mo, ws_d, (int)we_y, (int)we_mo, (int)we_d, "week");
                var weeks = (dur.Weeks + (long)until.weeks) / increment * increment;
                r1 = weeks;
                r2 = r1 + increment * sign;
                startDur = (dur.Years, dur.Months, r1, 0); endDur = (dur.Years, dur.Months, r2, 0);
                break;
            }
            default: // "day"
            {
                var days = dur.Days / increment * increment;
                r1 = days;
                r2 = r1 + increment * sign;
                startDur = (dur.Years, dur.Months, dur.Weeks, r1); endDur = (dur.Years, dur.Months, dur.Weeks, r2);
                break;
            }
        }

        var startEpochNs = DateDurationSign(startDur.y, startDur.mo, startDur.w, startDur.d) == 0
            ? originEpochNs
            : DateAddEpochNs(o, startDur.y, startDur.mo, startDur.w, startDur.d);
        var endEpochNs = DateAddEpochNs(o, endDur.y, endDur.mo, endDur.w, endDur.d);

        return new NudgeWindow(r1, r2, startEpochNs, endEpochNs, startDur, endDur);
    }

    private NudgeResult NudgeToZonedTime(int sign, in InternalDuration dur,
        (int y, int mo, int d, int h, int mi, int s, int ms, int us, int ns) o, long increment, string unit, string largestUnit, string roundingMode)
    {
        // When the largest unit is itself a time unit the result is expressed purely as
        // time (the date difference was already projected onto the elapsed nanoseconds by
        // DifferenceInternal), so the rounded time must NOT roll into a day — e.g. a
        // 1-day duration over a 25-hour DST day rounds to 36 hours, not 1 day (#818 P9).
        if (IsTimeUnit(largestUnit))
        {
            var unitNsTime = (BigInteger)TimeUnitNs(unit) * increment;
            var roundedTime = TemporalRoundingOptions.RoundToIncrement(dur.Time, unitNsTime, roundingMode);
            var (ty, tm, td) = AddISODate(o.y, o.mo, o.d, dur.Years, dur.Months, dur.Weeks, dur.Days, "constrain");
            var startNsTime = EpochNsForLocal(ty, tm, td, o.h, o.mi, o.s, o.ms, o.us, o.ns);
            return new NudgeResult(
                new InternalDuration(dur.Years, dur.Months, dur.Weeks, dur.Days, roundedTime),
                startNsTime + roundedTime, false, null);
        }

        var (sy, sm, sd) = AddISODate(o.y, o.mo, o.d, dur.Years, dur.Months, dur.Weeks, dur.Days, "constrain");
        var startEpochNs = EpochNsForLocal(sy, sm, sd, o.h, o.mi, o.s, o.ms, o.us, o.ns);
        var (ey, em, ed) = CivilFromDays(DaysFromCivil(sy, sm, sd) + sign);
        var endEpochNs = EpochNsForLocal((int)ey, (int)em, (int)ed, o.h, o.mi, o.s, o.ms, o.us, o.ns);

        var daySpan = endEpochNs - startEpochNs;
        if (daySpan.Sign != sign)
            throw JSEngine.NewRangeError("Temporal.Duration: time zone returned inconsistent Instants");

        var unitNs = (BigInteger)TimeUnitNs(unit) * increment;
        var rounded = TemporalRoundingOptions.RoundToIncrement(dur.Time, unitNs, roundingMode);
        var beyond = rounded - daySpan;
        // The rounded time rolls into the next day when it reaches OR passes the day length.
        // Reaching it exactly (beyond == 0) must still roll here: largestUnit is always a
        // calendar unit in this branch (a time largestUnit returned early above and keeps the
        // overflow as time per #818 P9), so a time that rounds up to a whole day has to become
        // a day and bubble up — e.g. since/until with a calendar largestUnit and a time
        // smallestUnit (round-cross-unit-boundary: 23:59:59.999999999 expands to 1 day → 2 years).
        var didRoundBeyondDay = beyond.Sign == sign || beyond.IsZero;

        // The next-day boundary used to measure the day length is the result of
        // AddDaysToZonedDateTime and must be a representable instant whenever it actually contributes
        // to the result: when rounding TO days (largestUnit "day", the day length is the output unit)
        // or when the rounded time rolls across the boundary into the next day. A relativeTo within
        // one day of the maximum instant then pushes this boundary past nsMaxInstant — a RangeError
        // (test262 round/next-day-out-of-range). A larger largestUnit whose time does not roll over
        // (e.g. rounding an empty duration with largestUnit "years" relative to the max date) never
        // consumes the boundary and must NOT throw (test262 round/relativeto-date-limits).
        if ((largestUnit == "day" || didRoundBeyondDay) && !IsValid(endEpochNs))
            throw JSEngine.NewRangeError("Temporal.Duration: day boundary is out of range");

        long dayDelta;
        BigInteger nudgedEpochNs, roundedFinal;
        if (didRoundBeyondDay)
        {
            dayDelta = sign;
            roundedFinal = TemporalRoundingOptions.RoundToIncrement(beyond, unitNs, roundingMode);
            nudgedEpochNs = endEpochNs + roundedFinal;
        }
        else
        {
            dayDelta = 0;
            roundedFinal = rounded;
            nudgedEpochNs = startEpochNs + rounded;
        }

        var resultDur = new InternalDuration(dur.Years, dur.Months, dur.Weeks, dur.Days + dayDelta, roundedFinal);
        return new NudgeResult(resultDur, nudgedEpochNs, didRoundBeyondDay, null);
    }

    // Bubbles an expanded smaller unit up into the larger calendar units (weeks do not bubble into
    // months unless largestUnit is week).
    private InternalDuration BubbleRelativeDuration(int sign, InternalDuration dur, BigInteger nudgedEpochNs,
        (int y, int mo, int d, int h, int mi, int s, int ms, int us, int ns) o, string largestUnit, string smallestUnit)
    {
        if (smallestUnit == largestUnit) return dur;

        var largestIdx = UnitRank(largestUnit);
        var smallestIdx = UnitRank(smallestUnit);
        for (var unitIdx = smallestIdx - 1; unitIdx >= largestIdx; unitIdx--)
        {
            var unit = UnitRankOrder[unitIdx];
            if (unit == "week" && largestUnit != "week") continue;

            (long y, long mo, long w, long d) endDur;
            switch (unit)
            {
                case "year": endDur = (dur.Years + sign, 0, 0, 0); break;
                case "month": endDur = (dur.Years, dur.Months + sign, 0, 0); break;
                case "week": endDur = (dur.Years, dur.Months, dur.Weeks + sign, 0); break;
                default: continue;
            }

            var endEpochNs = DateAddEpochNs(o, endDur.y, endDur.mo, endDur.w, endDur.d);
            var didExpandToEnd = nudgedEpochNs.CompareTo(endEpochNs) != -sign;
            if (didExpandToEnd)
                dur = new InternalDuration(endDur.y, endDur.mo, endDur.w, endDur.d, BigInteger.Zero);
            else
                break;
        }
        return dur;
    }

    // Adds a calendar date duration to the relativeTo wall clock and resolves the resulting instant.
    private BigInteger DateAddEpochNs((int y, int mo, int d, int h, int mi, int s, int ms, int us, int ns) o,
        long years, long months, long weeks, long days)
    {
        var (ny, nm, nd) = AddISODate(o.y, o.mo, o.d, years, months, weeks, days, "constrain");
        var result = EpochNsForLocal(ny, nm, nd, o.h, o.mi, o.s, o.ms, o.us, o.ns);
        // A nudge-window boundary (e.g. the next-day start used to measure a calendar/day
        // unit) is the result of AddZonedDateTime and must be a representable instant. A
        // relativeTo within one day of the maximum instant pushes the following day's
        // boundary past nsMaxInstant, which is a RangeError (test262 total/relativeto-date-limits:
        // +275760-09-12T00:00:01+00:00[UTC] is out of range because the next day overflows).
        if (!IsValid(result))
            throw JSEngine.NewRangeError("Temporal.Duration: relativeTo is out of range");
        return result;
    }

    // ── rounding-mode helpers ───────────────────────────────────────────────────

    private static string UnsignedRoundingMode(string mode, bool negative) => mode switch
    {
        "ceil" => negative ? "zero" : "infinity",
        "floor" => negative ? "infinity" : "zero",
        "expand" => "infinity",
        "trunc" => "zero",
        "halfCeil" => negative ? "half-zero" : "half-infinity",
        "halfFloor" => negative ? "half-infinity" : "half-zero",
        "halfExpand" => "half-infinity",
        "halfTrunc" => "half-zero",
        "halfEven" => "half-even",
        _ => "half-infinity",
    };

    // Given cmp = sign(2·|numerator| − |denominator|), whether to pick the END (r2) boundary.
    private static bool ApplyRoundingPicksEnd(int cmp, bool even, string unsignedMode) => unsignedMode switch
    {
        "zero" => false,
        "infinity" => true,
        "half-zero" => cmp > 0,
        "half-infinity" => cmp >= 0,
        "half-even" => cmp > 0 || (cmp == 0 && !even),
        _ => false,
    };

    // ── TemporalDurationFromInternal ────────────────────────────────────────────

    private static JSValue BuildDuration(in InternalDuration x, string largestUnit)
    {
        var sign = x.Time.Sign;
        var t = BigInteger.Abs(x.Time);

        // Distribute the sub-day nanoseconds across the time components with `largestUnit` as the
        // coarsest bucket. When largestUnit is finer than a second (milli/micro/nanosecond) the
        // larger components must fold INTO that bucket rather than populating seconds/minutes/hours
        // — e.g. largestUnit "millisecond" expresses a 3660-second remainder as 3_660_000
        // milliseconds, not 3660 seconds (test262 Duration/round/
        // relativeto-largestunit-smallestunit-combinations). The day count comes from x.Days (the
        // DST-aware calendar difference); x.Time is the sub-day remainder, whose hours must NOT be
        // re-split into 24-hour days — in a 25-hour day a 24-hour remainder stays 24h (#818 P9).
        BigInteger hours = 0, minutes = 0, seconds = 0, milliseconds = 0, microseconds = 0, nanoseconds = 0;
        var rank = UnitRank(largestUnit);
        if (rank <= UnitRank("second"))
        {
            seconds = t / 1_000_000_000;
            var sub = t % 1_000_000_000;
            milliseconds = sub / 1_000_000;
            microseconds = sub / 1_000 % 1_000;
            nanoseconds = sub % 1_000;
            if (rank <= UnitRank("minute")) { minutes = seconds / 60; seconds %= 60; }
            if (rank <= UnitRank("hour")) { hours = minutes / 60; minutes %= 60; }
        }
        else if (rank == UnitRank("millisecond"))
        {
            milliseconds = t / 1_000_000;
            microseconds = t / 1_000 % 1_000;
            nanoseconds = t % 1_000;
        }
        else if (rank == UnitRank("microsecond"))
        {
            microseconds = t / 1_000;
            nanoseconds = t % 1_000;
        }
        else // nanosecond
        {
            nanoseconds = t;
        }

        // ℝ→𝔽 nearest double (ties to even) for the BigInteger components, not .NET's
        // truncating (double)BigInteger (#818 Problems 18/19).
        double S(BigInteger v) => sign * JSTemporalDuration.NearestDouble(v);
        return new JSTemporalDuration(
            x.Years, x.Months, x.Weeks, x.Days,
            S(hours), S(minutes), S(seconds), S(milliseconds), S(microseconds), S(nanoseconds),
            JSTemporalDuration.DurationPrototype);
    }

    // ToRelativeTemporalObject's ZonedDateTime branch. Only an actual Temporal.ZonedDateTime object is
    // routed through the DST-aware machinery here; a property bag carrying a timeZone field or a string
    // with a [TimeZone] annotation falls back to its ISO date (a 24-hour day — correct whenever the
    // duration spans no DST transition, which the bag / string relativeTo cases exercised do not).
    internal static JSTemporalZonedDateTime ToZonedRelative(JSValue rel)
    {
        if (rel is JSTemporalZonedDateTime z)
            return new JSTemporalZonedDateTime(z.epochNanoseconds, z.timeZoneId, z.calendarId, ZonedDateTimePrototype);
        return null;
    }

    // A Temporal.Duration relativeTo string that carries a time-zone annotation is a ZonedDateTime
    // string: parse it as a full ZonedDateTime relativeTo (DST-aware, and validating the representable
    // instant range), so that adding the duration to the boundary instant overflows exactly as the
    // spec's AddZonedDateTime requires. Used by Temporal.Duration round/total/compare.
    internal static JSTemporalZonedDateTime ParseRelativeZoned(string text)
        => (JSTemporalZonedDateTime)ParseZonedDateTimeString(text);
}
