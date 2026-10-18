using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Temporal;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Runners;

/// <summary>
/// What an engine's adapter does around the Temporal core (milestone SJL-10.3), written once more for the library-level
/// runner and the library's correspondence and dataset tests: the Temporal objects as records, the reads of property
/// bags and options in the specification's order with their conversions, <c>relativeTo</c> resolution, the calendar and
/// zone lookups, each status mapped to the error the specification throws, and each method's steps around the library's
/// calls. None of it is a balance, a limit, a parse, a calendar's arithmetic, a duration's rounding or a difference:
/// every such step is <c>Broiler.ECMAScript.Algorithms</c>'s.
/// </summary>
/// <remarks>
/// <para>
/// A JavaScript value is an <see cref="object"/>: <see langword="null"/> for <c>undefined</c>, <see cref="JsNull"/>,
/// <see cref="bool"/>, <see cref="double"/> (a Number), <see cref="BigInteger"/> (a BigInt), <see cref="string"/>, a
/// <see cref="Bag"/> (an ordinary object with data properties) or one of the records below (a Temporal object). An
/// error the specification throws is a <see cref="RangeError"/> or a <see cref="TypeError"/>.
/// </para>
/// <para>
/// <b>This file is linked into the test projects and the Native AOT probe, never into a library</b>, as the other
/// runners' files are. Its methods follow Broiler.VM's realm files at 14072b0, the pinned specification's order.
/// </para>
/// </remarks>
internal sealed partial class TemporalAdapter
{
    private readonly TimeZoneDatabase zones;
    private readonly Dictionary<string, TemporalTimeZone> zoneCache = new(StringComparer.Ordinal);
    private readonly object gate = new();

    /// <summary>Creates the adapter over the calendars and the zone database an engine hands the library.</summary>
    internal TemporalAdapter(TemporalCalendars calendars, TimeZoneDatabase zones)
    {
        Calendars = calendars;
        this.zones = zones;
    }

    /// <summary>The calendars.</summary>
    internal TemporalCalendars Calendars { get; }

    // ---- Values -------------------------------------------------------------------------------------------------

    /// <summary>The specification's RangeError.</summary>
    internal sealed class RangeError() : Exception("RangeError");

    /// <summary>The specification's TypeError.</summary>
    internal sealed class TypeError() : Exception("TypeError");

    /// <summary>JavaScript's <c>null</c>.</summary>
    internal sealed class JsNull
    {
        internal static readonly JsNull Value = new();

        private JsNull()
        {
        }
    }

    /// <summary>An ordinary object with data properties, in creation order.</summary>
    internal sealed class Bag
    {
        private readonly List<KeyValuePair<string, object?>> entries = [];

        internal Bag()
        {
        }

        internal Bag(params (string Key, object? Value)[] entries)
        {
            foreach (var (key, value) in entries)
                Set(key, value);
        }

        internal IEnumerable<KeyValuePair<string, object?>> Entries => entries;

        internal void Set(string key, object? value)
        {
            for (var i = 0; i < entries.Count; i++)
            {
                if (entries[i].Key == key)
                {
                    entries[i] = new KeyValuePair<string, object?>(key, value);
                    return;
                }
            }

            entries.Add(new KeyValuePair<string, object?>(key, value));
        }

        /// <summary>[[Get]]: the property's value, or undefined.</summary>
        internal object? Get(string key)
        {
            foreach (var entry in entries)
            {
                if (entry.Key == key)
                    return entry.Value;
            }

            return null;
        }
    }

    internal sealed record Duration(DurationFields Fields);

    internal sealed record PlainDate(IsoDate Date, CalendarId Calendar);

    internal sealed record PlainDateTime(IsoDateTime DateTime, CalendarId Calendar);

    internal sealed record PlainTime(IsoTime Time);

    internal sealed record PlainYearMonth(IsoDate Date, CalendarId Calendar);

    internal sealed record PlainMonthDay(IsoDate Date, CalendarId Calendar);

    internal sealed record Instant(BigInteger EpochNanoseconds);

    internal sealed record ZonedDateTime(BigInteger EpochNanoseconds, string TimeZone, CalendarId Calendar);

    // ---- Statuses as errors ---------------------------------------------------------------------------------------

    /// <summary>A Temporal status as its error: every one is a RangeError.</summary>
    internal static void Check(TemporalStatus status)
    {
        if (status != TemporalStatus.Completed)
            throw new RangeError();
    }

    /// <summary>A calendar status as its error: four are TypeErrors, every other a RangeError.</summary>
    internal static void Check(CalendarStatus status)
    {
        if (status == CalendarStatus.Completed)
            return;
        if (TemporalCalendars.IsTypeError(status))
            throw new TypeError();

        throw new RangeError();
    }

    /// <summary>A time-zone status as its error: a RangeError.</summary>
    internal static void Check(TimeStatus status)
    {
        if (status != TimeStatus.Completed)
            throw new RangeError();
    }

    // ---- Conversions (ECMA-262 and the proposal's s13) -----------------------------------------------------------

    /// <summary>ToNumber of a value.</summary>
    internal static double ToNumber(object? value) => value switch
    {
        null => double.NaN,
        JsNull => 0,
        bool b => b ? 1 : 0,
        double d => d,
        int i => i,
        long l => l,
        string s => NumberParsing.StringToNumber(s),
        Bag => double.NaN, // "[object Object]"
        _ => throw new TypeError(), // a BigInt, or a Temporal object whose valueOf throws
    };

    /// <summary>ToString of a value.</summary>
    internal string ToStringValue(object? value) => value switch
    {
        null => "undefined",
        JsNull => "null",
        bool b => b ? "true" : "false",
        double d => NumberText.ToString(d),
        string s => s,
        BigInteger i => i.ToString(CultureInfo.InvariantCulture),
        Bag => "[object Object]",
        _ => Render(value),
    };

    /// <summary>The proposal's ToIntegerWithTruncation (s13.40).</summary>
    internal static double ToIntegerWithTruncation(object? value)
    {
        var number = ToNumber(value);
        if (!double.IsFinite(number))
            throw new RangeError();

        return Math.Truncate(number) + 0.0;
    }

    /// <summary>The proposal's ToPositiveIntegerWithTruncation (s13.39).</summary>
    internal static double ToPositiveIntegerWithTruncation(object? value)
    {
        var integer = ToIntegerWithTruncation(value);
        if (integer <= 0)
            throw new RangeError();

        return integer;
    }

    /// <summary>The proposal's ToIntegerIfIntegral (s14.5.1.1).</summary>
    internal static double ToIntegerIfIntegral(object? value)
    {
        var number = ToNumber(value);
        if (!double.IsFinite(number) || Math.Truncate(number) != number)
            throw new RangeError();

        return number + 0.0;
    }

    /// <summary>ECMA-402's GetOptionsObject: undefined as an empty object, an object as itself, anything else a TypeError.</summary>
    internal static Bag OptionsObject(object? value) => value switch
    {
        null => new Bag(),
        Bag bag => bag,
        _ => throw new TypeError(),
    };

    /// <summary>GetOption of type string: ToString of the property, or undefined.</summary>
    private string? StringOption(Bag options, string key) => options.Get(key) is { } value ? ToStringValue(value) : null;

    /// <summary>GetTemporalOverflowOption (s13.6).</summary>
    internal TemporalOverflow OverflowOption(Bag options)
    {
        if (StringOption(options, "overflow") is not { } text)
            return TemporalOptionRules.DefaultOverflow;

        return TemporalOptionRules.TryParseOverflow(text, out var overflow) ? overflow : throw new RangeError();
    }

    /// <summary>GetRoundingModeOption (s14.5.2.2).</summary>
    internal RoundingMode RoundingModeOption(Bag options, RoundingMode fallback)
    {
        if (StringOption(options, "roundingMode") is not { } text)
            return fallback;

        return ExactRounding.TryParseRoundingMode(text, out var mode) ? mode : throw new RangeError();
    }

    /// <summary>GetRoundingIncrementOption (s14.5.2.3).</summary>
    internal static long RoundingIncrementOption(Bag options)
    {
        if (options.Get("roundingIncrement") is not { } value)
            return TemporalOptionRules.DefaultRoundingIncrement;

        return TemporalOptionRules.TryParseRoundingIncrement(ToNumber(value), out var increment) ? increment : throw new RangeError();
    }

    /// <summary>GetTemporalUnitValuedOption (s13.17): a unit, unset or auto; a required option missing is a RangeError.</summary>
    internal TemporalUnit UnitOption(Bag options, string key, bool required = false)
    {
        if (StringOption(options, key) is not { } text)
            return required ? throw new RangeError() : TemporalUnit.Unset;

        return TemporalOptionRules.TryParseUnit(text, out var unit) ? unit : throw new RangeError();
    }

    /// <summary>GetTemporalDisambiguationOption (s13.7).</summary>
    internal Disambiguation DisambiguationOption(Bag options) => StringOption(options, "disambiguation") switch
    {
        null or "compatible" => Disambiguation.Compatible,
        "earlier" => Disambiguation.Earlier,
        "later" => Disambiguation.Later,
        "reject" => Disambiguation.Reject,
        _ => throw new RangeError(),
    };

    /// <summary>GetTemporalOffsetOption (s13.9).</summary>
    internal string OffsetOption(Bag options, string fallback) => StringOption(options, "offset") switch
    {
        null => fallback,
        "prefer" => "prefer",
        "use" => "use",
        "ignore" => "ignore",
        "reject" => "reject",
        _ => throw new RangeError(),
    };

    // ---- Calendars and zones ------------------------------------------------------------------------------------

    /// <summary>CanonicalizeCalendar (s12.1.1) of a calendar type.</summary>
    internal static CalendarId Canonical(string identifier)
    {
        Check(TemporalCalendars.Canonicalize(identifier, out var calendar));
        return calendar;
    }

    /// <summary>The constructors' calendar argument: undefined is ISO 8601, a string is canonicalized, anything else a TypeError.</summary>
    internal static CalendarId CalendarArgument(object? value) => value switch
    {
        null => CalendarId.Iso8601,
        string s => Canonical(s),
        _ => throw new TypeError(),
    };

    /// <summary>ToTemporalCalendarIdentifier (s12.3.10): a Temporal object's calendar, or a calendar string's.</summary>
    internal static CalendarId ToTemporalCalendarIdentifier(object? value)
    {
        switch (value)
        {
            case PlainDate d:
                return d.Calendar;
            case PlainDateTime dt:
                return dt.Calendar;
            case PlainYearMonth ym:
                return ym.Calendar;
            case PlainMonthDay md:
                return md.Calendar;
            case ZonedDateTime z:
                return z.Calendar;
            case string text:
                if (!TemporalGrammar.TryParseCalendarString(text, out var identifier, out _))
                    throw new RangeError();
                return Canonical(identifier!);
            default:
                throw new TypeError();
        }
    }

    /// <summary>An annotation's calendar, or ISO 8601.</summary>
    internal static CalendarId AnnotationCalendar(string? annotation) => annotation is null ? CalendarId.Iso8601 : Canonical(annotation);

    /// <summary>A zone identifier record as the identifier an engine stores: the offset's, or the available name's.</summary>
    internal string ZoneIdentifier(TimeZoneIdentifierRecord record)
    {
        if (record.Name is null)
            return OffsetTimeZones.FormatOffsetIdentifier(record.OffsetMinutes);

        return zones.TryFind(record.Name, out var id) ? id.Identifier : throw new RangeError();
    }

    /// <summary>ToTemporalTimeZoneIdentifier (s11.1.8): a ZonedDateTime's zone, or a time zone string's.</summary>
    internal string ToTemporalTimeZoneIdentifier(object? value)
    {
        if (value is ZonedDateTime zoned)
            return zoned.TimeZone;
        if (value is not string text)
            throw new TypeError();
        if (!TemporalGrammar.TryParseTimeZoneString(text, out var record, out _))
            throw new RangeError();

        return ZoneIdentifier(record);
    }

    /// <summary>The ZonedDateTime constructor's zone: ParseTimeZoneIdentifier of a string.</summary>
    internal string TimeZoneFromIdentifier(object? value)
    {
        if (value is not string text)
            throw new TypeError();

        return TemporalGrammar.TryParseTimeZoneIdentifier(text, out var record) ? ZoneIdentifier(record) : throw new RangeError();
    }

    /// <summary>A stored zone identifier as the library's time zone, over SJL-8's rules.</summary>
    internal ITimeZoneArithmetic Zone(string identifier)
    {
        lock (gate)
        {
            if (zoneCache.TryGetValue(identifier, out var cached))
                return cached;

            ITimeZoneRules rules = OffsetTimeZones.TryParseOffsetIdentifier(identifier, out var minutes)
                ? new FixedOffsetTimeZoneRules(minutes * 60)
                : zones.Rules(zones.TryFind(identifier, out var id) ? id : throw new RangeError());
            var zone = new TemporalTimeZone(rules);
            zoneCache[identifier] = zone;
            return zone;
        }
    }

    /// <summary>TimeZoneEquals (s11.1.15), SJL-8's.</summary>
    internal bool TimeZoneEquals(string one, string two) => ZonedTime.TimeZoneEquals(zones, one, two);

    /// <summary>GetOffsetNanosecondsFor (s11.1.9).</summary>
    internal long OffsetNanosecondsFor(string zone, BigInteger epochNanoseconds) => Zone(zone).GetOffsetNanosecondsFor(epochNanoseconds);

    /// <summary>GetISODateTimeFor (s11.1.10).</summary>
    internal IsoDateTime IsoDateTimeFor(string zone, BigInteger epochNanoseconds) =>
        IsoArithmetic.GetIsoPartsFromEpoch(epochNanoseconds + OffsetNanosecondsFor(zone, epochNanoseconds));

    /// <summary>GetEpochNanosecondsFor (s11.1.11).</summary>
    internal BigInteger EpochNanosecondsFor(string zone, IsoDateTime dateTime, Disambiguation disambiguation)
    {
        Check(Zone(zone).GetEpochNanosecondsFor(dateTime, disambiguation, out var epochNanoseconds));
        return epochNanoseconds;
    }

    /// <summary>GetStartOfDay (s11.1.14).</summary>
    internal BigInteger StartOfDay(string zone, IsoDate date)
    {
        Check(Zone(zone).GetStartOfDay(date, out var epochNanoseconds));
        return epochNanoseconds;
    }

    /// <summary>
    /// InterpretISODateTimeOffset (s6.5.1): a wall time with its offset as an instant, under the offset option and the
    /// disambiguation; a null time is the start of the day.
    /// </summary>
    internal BigInteger InterpretIsoDateTimeOffset(
        IsoDate date, IsoTime? time, string offsetBehaviour, long offsetNanoseconds, string zone, Disambiguation disambiguation, string offsetOption, bool matchMinutes)
    {
        // AS BROILER.VM'S InterpretDateTimeOffset: the wall time's and the day's instants are SJL-8's, which refuse an
        // instant out of range; the days are checked where an offset is matched or used.
        if (time is not { } clock)
            return StartOfDay(zone, date);

        var dateTime = new IsoDateTime(date, clock);
        if (offsetBehaviour == "wall" || (offsetBehaviour == "option" && offsetOption == "ignore"))
            return EpochNanosecondsFor(zone, dateTime, disambiguation);

        if (offsetBehaviour == "exact" || (offsetBehaviour == "option" && offsetOption == "use"))
        {
            var balanced = IsoArithmetic.BalanceIsoDateTime(date, IsoArithmetic.TimeToNanoseconds(clock) - offsetNanoseconds);
            Check(TemporalLimits.CheckIsoDaysRange(balanced.Date));
            var epoch = IsoArithmetic.GetUtcEpochNanoseconds(balanced);
            Check(TemporalLimits.CheckEpochNanoseconds(epoch));
            return epoch;
        }

        Check(TemporalLimits.CheckIsoDaysRange(date));
        var utc = IsoArithmetic.GetUtcEpochNanoseconds(dateTime);
        var possible = new BigInteger[ZonedTime.MaxPossibleEpochNanoseconds];
        var rules = ((TemporalTimeZone)Zone(zone)).Rules;
        Check(ZonedTime.GetPossibleEpochNanoseconds(rules, dateTime, possible, out var count));
        for (var i = 0; i < count; i++)
        {
            var candidateOffset = utc - possible[i];
            if (candidateOffset == offsetNanoseconds)
                return possible[i];
            if (matchMinutes &&
                ExactRounding.RoundNumberToIncrement(candidateOffset, BigInteger.One, new BigInteger(60_000_000_000L), RoundingMode.HalfExpand) == offsetNanoseconds)
                return possible[i];
        }

        if (offsetOption == "reject")
            throw new RangeError();

        Check(ZonedTime.DisambiguatePossibleEpochNanoseconds(rules, possible.AsSpan(0, count), dateTime, disambiguation, out var chosen));
        return chosen;
    }

    // ---- Rendering -----------------------------------------------------------------------------------------------

    /// <summary>A Temporal object's toString(), as the probes' and datasets' String(value) prints it.</summary>
    internal string Render(object? value) => value switch
    {
        Duration d => TemporalFormat.DurationToString(d.Fields, TemporalFormat.AutoPrecision),
        PlainDate d => TemporalFormat.DateToString(d.Date, TemporalCalendars.Identifier(d.Calendar), ShowCalendarName.Auto),
        PlainDateTime dt => TemporalFormat.DateTimeToString(dt.DateTime, TemporalCalendars.Identifier(dt.Calendar), TemporalFormat.AutoPrecision, ShowCalendarName.Auto),
        PlainTime t => TemporalFormat.TimeToString(t.Time, TemporalFormat.AutoPrecision),
        PlainYearMonth ym => TemporalFormat.YearMonthToString(ym.Date, TemporalCalendars.Identifier(ym.Calendar), ShowCalendarName.Auto),
        PlainMonthDay md => TemporalFormat.MonthDayToString(md.Date, TemporalCalendars.Identifier(md.Calendar), ShowCalendarName.Auto),
        Instant i => TemporalFormat.InstantToString(i.EpochNanoseconds, null, TemporalFormat.AutoPrecision),
        ZonedDateTime z => TemporalFormat.ZonedDateTimeToString(
            z.EpochNanoseconds, OffsetNanosecondsFor(z.TimeZone, z.EpochNanoseconds), z.TimeZone, TemporalCalendars.Identifier(z.Calendar),
            TemporalFormat.AutoPrecision, ShowCalendarName.Auto, ShowTimeZoneName.Auto, ShowOffset.Auto),
        _ => ToStringValue(value),
    };

    /// <summary>A Temporal object's [Symbol.toStringTag].</summary>
    internal static string Tag(object value) => value switch
    {
        Duration => "Temporal.Duration",
        PlainDate => "Temporal.PlainDate",
        PlainDateTime => "Temporal.PlainDateTime",
        PlainTime => "Temporal.PlainTime",
        PlainYearMonth => "Temporal.PlainYearMonth",
        PlainMonthDay => "Temporal.PlainMonthDay",
        Instant => "Temporal.Instant",
        ZonedDateTime => "Temporal.ZonedDateTime",
        _ => throw new ArgumentException("not a Temporal object", nameof(value)),
    };
}
