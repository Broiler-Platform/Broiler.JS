using System;
using System.Collections.Generic;
using System.Numerics;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Temporal;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Runners;

// The Temporal objects' conversions and methods, each as Broiler.VM's realm files write it at 14072b0: the reads in the
// specification's order, then the library's operation, then the object.
internal sealed partial class TemporalAdapter
{
    // ---- Property bags (PrepareCalendarFields, s12.3.3) -----------------------------------------------------------

    /// <summary>The fields PrepareCalendarFields read from a bag, with the time, offset and zone fields beside the calendar's.</summary>
    internal sealed class Fields
    {
        internal CalendarFields Calendar;
        internal double? Hour, Minute, Second, Millisecond, Microsecond, Nanosecond;
        internal string? Offset;
        internal string? TimeZone;
        internal bool Any;
    }

    /// <summary>
    /// PrepareCalendarFields (s12.3.3): the named fields read in code unit order, each converted by Table 19's conversion,
    /// the era fields only where the calendar has eras; for a partial read, a TypeError where none is present.
    /// </summary>
    internal Fields PrepareFields(CalendarId calendar, Bag bag, bool date, bool time, bool offset, bool zone, bool yearMonth = false, bool partial = false)
    {
        var fields = new Fields();
        var names = new List<string>();
        if (date)
        {
            if (!yearMonth)
                names.Add("day");
            names.Add("month");
            names.Add("monthCode");
            names.Add("year");

            // CALENDAREXTRAFIELDS: the era fields where the calendar has eras and the year is read.
            if (TemporalCalendars.SupportsEra(calendar))
            {
                names.Add("era");
                names.Add("eraYear");
            }
        }

        if (time)
            names.AddRange(["hour", "microsecond", "millisecond", "minute", "nanosecond", "second"]);
        if (offset)
            names.Add("offset");
        if (zone)
            names.Add("timeZone");
        names.Sort(StringComparer.Ordinal);

        foreach (var name in names)
        {
            var value = bag.Get(name);
            if (value is null)
                continue;

            fields.Any = true;
            switch (name)
            {
                case "day":
                    fields.Calendar = fields.Calendar with { Day = ToPositiveIntegerWithTruncation(value) };
                    break;
                case "era":
                    fields.Calendar = fields.Calendar with { Era = ToStringValue(value) };
                    break;
                case "eraYear":
                    fields.Calendar = fields.Calendar with { EraYear = ToIntegerWithTruncation(value) };
                    break;
                case "year":
                    fields.Calendar = fields.Calendar with { Year = ToIntegerWithTruncation(value) };
                    break;
                case "month":
                    fields.Calendar = fields.Calendar with { Month = ToPositiveIntegerWithTruncation(value) };
                    break;
                case "monthCode":
                    // TOPRIMITIVE, then a TypeError for a value that is not a string, then ParseMonthCode.
                    if (value is not string code)
                        throw new TypeError();
                    Check(MonthCode.Parse(code, out var parsed));
                    fields.Calendar = fields.Calendar with { MonthCode = parsed };
                    break;
                case "hour":
                    fields.Hour = ToIntegerWithTruncation(value);
                    break;
                case "minute":
                    fields.Minute = ToIntegerWithTruncation(value);
                    break;
                case "second":
                    fields.Second = ToIntegerWithTruncation(value);
                    break;
                case "millisecond":
                    fields.Millisecond = ToIntegerWithTruncation(value);
                    break;
                case "microsecond":
                    fields.Microsecond = ToIntegerWithTruncation(value);
                    break;
                case "nanosecond":
                    fields.Nanosecond = ToIntegerWithTruncation(value);
                    break;
                case "offset":
                    fields.Offset = ToOffsetString(value);
                    break;
                case "timeZone":
                    fields.TimeZone = ToTemporalTimeZoneIdentifier(value);
                    break;
            }
        }

        if (partial && !fields.Any)
            throw new TypeError();

        return fields;
    }

    /// <summary>ToOffsetString (s13.41): a string the grammar reads as a UTC offset.</summary>
    internal static string ToOffsetString(object? value)
    {
        // TOPRIMITIVE, then a TypeError for a value that is not a string.
        if (value is not string text)
            throw new TypeError();
        if (!TemporalGrammar.TryParseOffsetString(text, out _))
            throw new RangeError();

        return text;
    }

    /// <summary>GetTemporalCalendarIdentifierWithISODefault (s12.3.11): a bag's calendar.</summary>
    internal static CalendarId BagCalendar(Bag bag) => bag.Get("calendar") is { } value ? ToTemporalCalendarIdentifier(value) : CalendarId.Iso8601;

    /// <summary>A bag's time fields, regulated (RegulateTime, s4.5.8).</summary>
    private static IsoTime RegulateTime(Fields fields, TemporalOverflow overflow)
    {
        Check(IsoArithmetic.RegulateTime(fields.Hour ?? 0, fields.Minute ?? 0, fields.Second ?? 0, fields.Millisecond ?? 0, fields.Microsecond ?? 0, fields.Nanosecond ?? 0, overflow, out var time));
        return time;
    }

    /// <summary>InterpretTemporalDateTimeFields (s5.5.5).</summary>
    private IsoDateTime InterpretDateTimeFields(CalendarId calendar, Fields fields, TemporalOverflow overflow)
    {
        Check(Calendars.DateFromFields(calendar, fields.Calendar, overflow == TemporalOverflow.Reject ? CalendarOverflow.Reject : CalendarOverflow.Constrain, out var date));
        return new IsoDateTime(date, RegulateTime(fields, overflow));
    }

    private static CalendarOverflow ToCalendar(TemporalOverflow overflow) => overflow == TemporalOverflow.Reject ? CalendarOverflow.Reject : CalendarOverflow.Constrain;

    private static IsoParseResult Parse(string text, params TemporalGoal[] goals) =>
        TemporalGrammar.TryParse(text, goals, out var result, out _) ? result : throw new RangeError();

    // ---- ToTemporal… conversions -------------------------------------------------------------------------------

    /// <summary>ToTemporalDuration (s7.5.12).</summary>
    internal Duration ToDuration(object? item)
    {
        if (item is Duration duration)
            return duration;

        if (item is string text)
        {
            if (!TemporalGrammar.TryParseDuration(text, out var parsed, out _))
                throw new RangeError();
            return CreateDuration(parsed);
        }

        if (item is not Bag bag)
            throw new TypeError();

        var partial = ToPartialDuration(bag);
        return CreateDuration(new DurationFields(
            partial[0] ?? 0, partial[1] ?? 0, partial[2] ?? 0, partial[3] ?? 0, partial[4] ?? 0, partial[5] ?? 0, partial[6] ?? 0, partial[7] ?? 0,
            partial[8] ?? 0, partial[9] ?? 0));
    }

    private static readonly string[] DurationKeys = ["days", "hours", "microseconds", "milliseconds", "minutes", "months", "nanoseconds", "seconds", "weeks", "years"];

    private static readonly int[] DurationKeyIndex = [3, 4, 8, 7, 5, 1, 9, 6, 2, 0];

    /// <summary>ToTemporalPartialDurationRecord (s7.5.18): the ten properties in code unit order.</summary>
    internal static double?[] ToPartialDuration(Bag bag)
    {
        var result = new double?[10];
        var any = false;
        for (var i = 0; i < DurationKeys.Length; i++)
        {
            if (bag.Get(DurationKeys[i]) is { } value)
            {
                result[DurationKeyIndex[i]] = ToIntegerIfIntegral(value);
                any = true;
            }
        }

        if (!any)
            throw new TypeError();

        return result;
    }

    /// <summary>CreateTemporalDuration (s7.5.19)'s validity test.</summary>
    internal static Duration CreateDuration(DurationFields fields)
    {
        Check(TemporalLimits.CheckDuration(fields));
        return new Duration(new DurationFields(
            fields.Years + 0.0, fields.Months + 0.0, fields.Weeks + 0.0, fields.Days + 0.0, fields.Hours + 0.0, fields.Minutes + 0.0,
            fields.Seconds + 0.0, fields.Milliseconds + 0.0, fields.Microseconds + 0.0, fields.Nanoseconds + 0.0));
    }

    /// <summary>ToTemporalDate (s3.5.4).</summary>
    internal PlainDate ToPlainDate(object? item, object? options = null)
    {
        switch (item)
        {
            case PlainDate date:
                OverflowOption(OptionsObject(options));
                return date;
            case ZonedDateTime zoned:
                OverflowOption(OptionsObject(options));
                return new PlainDate(IsoDateTimeFor(zoned.TimeZone, zoned.EpochNanoseconds).Date, zoned.Calendar);
            case PlainDateTime dateTime:
                OverflowOption(OptionsObject(options));
                return new PlainDate(dateTime.DateTime.Date, dateTime.Calendar);
            case Bag bag:
            {
                var calendar = BagCalendar(bag);
                var fields = PrepareFields(calendar, bag, date: true, time: false, offset: false, zone: false);
                var overflow = OverflowOption(OptionsObject(options));
                Check(Calendars.DateFromFields(calendar, fields.Calendar, ToCalendar(overflow), out var result));
                return new PlainDate(result, calendar);
            }

            case string text:
            {
                var parsed = Parse(text, TemporalGoal.DateTime);
                var calendar = AnnotationCalendar(parsed.Calendar);
                OverflowOption(OptionsObject(options));
                var date = new IsoDate(parsed.Year!.Value, parsed.Month, parsed.Day);
                Check(TemporalLimits.CheckIsoDate(date));
                return new PlainDate(date, calendar);
            }

            default:
                throw new TypeError();
        }
    }

    /// <summary>ToTemporalDateTime (s5.5.4).</summary>
    internal PlainDateTime ToPlainDateTime(object? item, object? options = null)
    {
        switch (item)
        {
            case PlainDateTime dateTime:
                OverflowOption(OptionsObject(options));
                return dateTime;
            case ZonedDateTime zoned:
                OverflowOption(OptionsObject(options));
                return new PlainDateTime(IsoDateTimeFor(zoned.TimeZone, zoned.EpochNanoseconds), zoned.Calendar);
            case PlainDate date:
                OverflowOption(OptionsObject(options));
                return CreatePlainDateTime(new IsoDateTime(date.Date, default), date.Calendar);
            case Bag bag:
            {
                var calendar = BagCalendar(bag);
                var fields = PrepareFields(calendar, bag, date: true, time: true, offset: false, zone: false);
                var overflow = OverflowOption(OptionsObject(options));
                return CreatePlainDateTime(InterpretDateTimeFields(calendar, fields, overflow), calendar);
            }

            case string text:
            {
                var parsed = Parse(text, TemporalGoal.DateTime);
                var calendar = AnnotationCalendar(parsed.Calendar);
                OverflowOption(OptionsObject(options));
                return CreatePlainDateTime(new IsoDateTime(new IsoDate(parsed.Year!.Value, parsed.Month, parsed.Day), parsed.Time ?? default), calendar);
            }

            default:
                throw new TypeError();
        }
    }

    /// <summary>CreateTemporalDateTime (s5.5.3)'s limit check.</summary>
    internal static PlainDateTime CreatePlainDateTime(IsoDateTime dateTime, CalendarId calendar)
    {
        Check(TemporalLimits.CheckIsoDateTime(dateTime));
        return new PlainDateTime(dateTime, calendar);
    }

    /// <summary>ToTemporalTime (s4.5.6).</summary>
    internal PlainTime ToPlainTime(object? item, object? options = null)
    {
        switch (item)
        {
            case PlainTime time:
                OverflowOption(OptionsObject(options));
                return time;
            case PlainDateTime dateTime:
                OverflowOption(OptionsObject(options));
                return new PlainTime(dateTime.DateTime.Time);
            case ZonedDateTime zoned:
                OverflowOption(OptionsObject(options));
                return new PlainTime(IsoDateTimeFor(zoned.TimeZone, zoned.EpochNanoseconds).Time);
            case Bag bag:
            {
                var fields = PrepareFields(CalendarId.Iso8601, bag, date: false, time: true, offset: false, zone: false, partial: true);
                var overflow = OverflowOption(OptionsObject(options));
                return new PlainTime(RegulateTime(fields, overflow));
            }

            case string text:
            {
                var parsed = Parse(text, TemporalGoal.Time);
                OverflowOption(OptionsObject(options));
                return new PlainTime(parsed.Time!.Value);
            }

            default:
                throw new TypeError();
        }
    }

    /// <summary>ToTemporalYearMonth (s9.5.2).</summary>
    internal PlainYearMonth ToPlainYearMonth(object? item, object? options = null)
    {
        switch (item)
        {
            case PlainYearMonth yearMonth:
                OverflowOption(OptionsObject(options));
                return yearMonth;
            case Bag bag:
            {
                var calendar = BagCalendar(bag);
                var fields = PrepareFields(calendar, bag, date: true, time: false, offset: false, zone: false, yearMonth: true);
                var overflow = OverflowOption(OptionsObject(options));
                Check(Calendars.YearMonthFromFields(calendar, fields.Calendar, ToCalendar(overflow), out var result));
                return new PlainYearMonth(result, calendar);
            }

            case string text:
            {
                var parsed = Parse(text, TemporalGoal.YearMonth);
                var calendar = AnnotationCalendar(parsed.Calendar);
                OverflowOption(OptionsObject(options));
                var date = new IsoDate(parsed.Year!.Value, parsed.Month, parsed.Day);
                Check(TemporalLimits.CheckIsoYearMonth(date));
                Check(Calendars.DateToFields(calendar, date, CalendarFieldsType.YearMonth, out var fields));
                Check(Calendars.YearMonthFromFields(calendar, fields, CalendarOverflow.Constrain, out var reference));
                return new PlainYearMonth(reference, calendar);
            }

            default:
                throw new TypeError();
        }
    }

    /// <summary>ToTemporalMonthDay (s10.5.1).</summary>
    internal PlainMonthDay ToPlainMonthDay(object? item, object? options = null)
    {
        switch (item)
        {
            case PlainMonthDay monthDay:
                OverflowOption(OptionsObject(options));
                return monthDay;
            case Bag bag:
            {
                var calendar = BagCalendar(bag);
                var fields = PrepareFields(calendar, bag, date: true, time: false, offset: false, zone: false);
                var overflow = OverflowOption(OptionsObject(options));
                Check(Calendars.MonthDayFromFields(calendar, fields.Calendar, ToCalendar(overflow), out var result));
                return new PlainMonthDay(result, calendar);
            }

            case string text:
            {
                var parsed = Parse(text, TemporalGoal.MonthDay);
                var calendar = AnnotationCalendar(parsed.Calendar);
                OverflowOption(OptionsObject(options));
                if (calendar == CalendarId.Iso8601)
                    return new PlainMonthDay(new IsoDate(1972, parsed.Month, parsed.Day), calendar);

                var date = new IsoDate(parsed.Year!.Value, parsed.Month, parsed.Day);
                Check(TemporalLimits.CheckIsoDate(date));
                Check(Calendars.DateToFields(calendar, date, CalendarFieldsType.MonthDay, out var fields));
                Check(Calendars.MonthDayFromFields(calendar, fields, CalendarOverflow.Constrain, out var reference));
                return new PlainMonthDay(reference, calendar);
            }

            default:
                throw new TypeError();
        }
    }

    /// <summary>ToTemporalInstant (s8.5.2).</summary>
    internal Instant ToInstant(object? item)
    {
        switch (item)
        {
            case Instant instant:
                return instant;
            case ZonedDateTime zoned:
                return new Instant(zoned.EpochNanoseconds);
            case string text:
                if (!TemporalGrammar.TryParseInstant(text, out var epoch, out _))
                    throw new RangeError();
                Check(TemporalLimits.CheckEpochNanoseconds(epoch));
                return new Instant(epoch);
            case Bag:
                throw new RangeError(); // ToPrimitive gives "[object Object]", which no grammar reads
            default:
                throw new TypeError();
        }
    }

    /// <summary>ToTemporalZonedDateTime (s6.5.2).</summary>
    internal ZonedDateTime ToZonedDateTime(object? item, object? options = null)
    {
        switch (item)
        {
            case ZonedDateTime zoned:
            {
                var resolved = OptionsObject(options);
                DisambiguationOption(resolved);
                OffsetOption(resolved, "reject");
                OverflowOption(resolved);
                return zoned;
            }

            case Bag bag:
            {
                var calendar = BagCalendar(bag);
                var fields = PrepareFields(calendar, bag, date: true, time: true, offset: true, zone: true);
                if (fields.TimeZone is null)
                    throw new TypeError();
                var resolved = OptionsObject(options);
                var disambiguation = DisambiguationOption(resolved);
                var offsetOption = OffsetOption(resolved, "reject");
                var overflow = OverflowOption(resolved);
                var dateTime = InterpretDateTimeFields(calendar, fields, overflow);
                var behaviour = fields.Offset is null ? "wall" : "option";
                long offsetNs = 0;
                if (fields.Offset is { } offsetText)
                    TemporalGrammar.TryParseOffsetString(offsetText, out offsetNs);
                var epoch = InterpretIsoDateTimeOffset(dateTime.Date, dateTime.Time, behaviour, offsetNs, fields.TimeZone, disambiguation, offsetOption, matchMinutes: true);
                return new ZonedDateTime(epoch, fields.TimeZone, calendar);
            }

            case string text:
            {
                var parsed = Parse(text, TemporalGoal.ZonedDateTime);
                if (!TemporalGrammar.TryParseTimeZoneIdentifier(parsed.TimeZone!, out var record))
                    throw new RangeError();
                var zone = ZoneIdentifier(record);
                var calendar = AnnotationCalendar(parsed.Calendar);
                var resolved = OptionsObject(options);
                var disambiguation = DisambiguationOption(resolved);
                var offsetOption = OffsetOption(resolved, "reject");
                OverflowOption(resolved);
                var behaviour = parsed.Z ? "exact" : parsed.Offset is null ? "wall" : "option";
                var date = new IsoDate(parsed.Year!.Value, parsed.Month, parsed.Day);
                var epoch = InterpretIsoDateTimeOffset(date, parsed.Time, behaviour, parsed.Z ? 0 : parsed.OffsetNanoseconds, zone, disambiguation, offsetOption,
                    matchMinutes: !parsed.OffsetHasSeconds);
                return new ZonedDateTime(epoch, zone, calendar);
            }

            default:
                throw new TypeError();
        }
    }

    /// <summary>
    /// GetTemporalRelativeToOption (s13.19)'s value resolved: a plain date or a zoned instant, as the library's record.
    /// </summary>
    internal RelativeTo ToRelativeTo(object? value)
    {
        switch (value)
        {
            case null:
                return RelativeTo.None;
            case ZonedDateTime zoned:
                return RelativeTo.FromZoned(zoned.EpochNanoseconds, Zone(zoned.TimeZone), Calendars, zoned.Calendar);
            case PlainDate date:
                return RelativeTo.FromPlainDate(date.Date, Calendars, date.Calendar);
            case PlainDateTime dateTime:
                return RelativeTo.FromPlainDate(dateTime.DateTime.Date, Calendars, dateTime.Calendar);
            case Bag bag:
            {
                var calendar = BagCalendar(bag);
                var fields = PrepareFields(calendar, bag, date: true, time: true, offset: true, zone: true);
                var dateTime = InterpretDateTimeFields(calendar, fields, TemporalOverflow.Constrain);
                if (fields.TimeZone is null)
                {
                    Check(TemporalLimits.CheckIsoDate(dateTime.Date));
                    return RelativeTo.FromPlainDate(dateTime.Date, Calendars, calendar);
                }

                long offsetNs = 0;
                if (fields.Offset is { } offsetText)
                    TemporalGrammar.TryParseOffsetString(offsetText, out offsetNs);
                var epoch = InterpretIsoDateTimeOffset(dateTime.Date, dateTime.Time, fields.Offset is null ? "wall" : "option", offsetNs, fields.TimeZone,
                    Disambiguation.Compatible, "reject", matchMinutes: true);
                return RelativeTo.FromZoned(epoch, Zone(fields.TimeZone), Calendars, calendar);
            }

            case string text:
            {
                var parsed = Parse(text, TemporalGoal.ZonedDateTime, TemporalGoal.DateTime);
                string? zone = null;
                if (parsed.TimeZone is { } annotation)
                {
                    if (!TemporalGrammar.TryParseTimeZoneIdentifier(annotation, out var record))
                        throw new RangeError();
                    zone = ZoneIdentifier(record);
                }

                var calendar = AnnotationCalendar(parsed.Calendar);
                var date = new IsoDate(parsed.Year!.Value, parsed.Month, parsed.Day);
                if (zone is null)
                {
                    Check(TemporalLimits.CheckIsoDate(date));
                    return RelativeTo.FromPlainDate(date, Calendars, calendar);
                }

                var behaviour = parsed.Z ? "exact" : parsed.Offset is null ? "wall" : "option";
                var epoch = InterpretIsoDateTimeOffset(date, parsed.Time, behaviour, parsed.Z ? 0 : parsed.OffsetNanoseconds, zone, Disambiguation.Compatible, "reject",
                    matchMinutes: !parsed.OffsetHasSeconds);
                return RelativeTo.FromZoned(epoch, Zone(zone), Calendars, calendar);
            }

            default:
                throw new TypeError();
        }
    }

    // ---- Temporal.Duration --------------------------------------------------------------------------------------

    /// <summary>new Temporal.Duration(…): each argument ToIntegerIfIntegral, undefined as 0.</summary>
    internal static Duration NewDuration(params object?[] arguments)
    {
        var fields = new double[10];
        for (var i = 0; i < 10; i++)
            fields[i] = i < arguments.Length && arguments[i] is { } value ? ToIntegerIfIntegral(value) : 0;

        return CreateDuration(new DurationFields(fields[0], fields[1], fields[2], fields[3], fields[4], fields[5], fields[6], fields[7], fields[8], fields[9]));
    }

    /// <summary>Temporal.Duration.prototype.with.</summary>
    internal static Duration With(Duration duration, object? like)
    {
        if (like is not Bag bag)
            throw new TypeError();

        var partial = ToPartialDuration(bag);
        var f = duration.Fields;
        return CreateDuration(new DurationFields(
            partial[0] ?? f.Years, partial[1] ?? f.Months, partial[2] ?? f.Weeks, partial[3] ?? f.Days, partial[4] ?? f.Hours, partial[5] ?? f.Minutes,
            partial[6] ?? f.Seconds, partial[7] ?? f.Milliseconds, partial[8] ?? f.Microseconds, partial[9] ?? f.Nanoseconds));
    }

    /// <summary>Temporal.Duration.prototype.add and subtract: AddDurations (s7.5.41).</summary>
    internal Duration AddDurations(Duration duration, object? other, bool subtract)
    {
        var fields = ToDuration(other).Fields;
        if (subtract)
            fields = DurationMath.Negate(fields);

        Check(DurationMath.AddDurations(duration.Fields, fields, out var result));
        return new Duration(result);
    }

    // A roundTo or totalOf argument: a string names the one option, undefined is a TypeError, anything else the options.
    private static Bag StringOrOptions(object? value, string key) => value switch
    {
        null => throw new TypeError(),
        string text => new Bag((key, text)),
        _ => OptionsObject(value),
    };

    /// <summary>Temporal.Duration.prototype.round (s7.3.20).</summary>
    internal Duration Round(Duration duration, object? roundToValue)
    {
        var roundTo = StringOrOptions(roundToValue, "smallestUnit");
        var largest = UnitOption(roundTo, "largestUnit");
        var relativeTo = ToRelativeTo(roundTo.Get("relativeTo"));
        var increment = RoundingIncrementOption(roundTo);
        var mode = RoundingModeOption(roundTo, TemporalOptionRules.DefaultRoundRoundingMode);
        var smallest = UnitOption(roundTo, "smallestUnit");
        if (!TemporalOptionRules.TryGetDurationRoundUnits(duration.Fields, largest, increment, smallest, out var resolvedLargest, out var resolvedSmallest))
            throw new RangeError();

        Check(DurationMath.Round(duration.Fields, resolvedLargest, increment, resolvedSmallest, mode, relativeTo, out var result));
        return new Duration(result);
    }

    /// <summary>Temporal.Duration.prototype.total (s7.3.21).</summary>
    internal double Total(Duration duration, object? totalOfValue)
    {
        var totalOf = StringOrOptions(totalOfValue, "unit");
        var relativeTo = ToRelativeTo(totalOf.Get("relativeTo"));
        var unit = UnitOption(totalOf, "unit", required: true);
        if (!TemporalOptionRules.IsValidTotalUnit(unit))
            throw new RangeError();

        Check(DurationMath.Total(duration.Fields, unit, relativeTo, out var total));
        return total;
    }

    /// <summary>Temporal.Duration.compare (s7.2.3).</summary>
    internal int Compare(object? one, object? two, object? options = null)
    {
        var first = ToDuration(one);
        var second = ToDuration(two);
        var resolved = OptionsObject(options);
        var relativeTo = ToRelativeTo(resolved.Get("relativeTo"));
        Check(DurationMath.Compare(first.Fields, second.Fields, relativeTo, out var result));
        return result;
    }

    /// <summary>Temporal.Duration.prototype.toString (s7.3.22).</summary>
    internal string DurationToString(Duration duration, object? options)
    {
        var resolved = OptionsObject(options);
        var digits = FractionalSecondDigitsOption(resolved);
        var mode = RoundingModeOption(resolved, TemporalOptionRules.DefaultToStringRoundingMode);
        var smallest = UnitOption(resolved, "smallestUnit");
        if (!TemporalOptionRules.IsValidToStringSmallestUnit(smallest, duration: true))
            throw new RangeError();

        Check(TemporalFormat.TryDurationToString(duration.Fields, TemporalOptionRules.ToSecondsStringPrecisionRecord(smallest, digits), mode, out var text));
        return text!;
    }

    /// <summary>GetTemporalFractionalSecondDigitsOption (s13.15).</summary>
    internal int FractionalSecondDigitsOption(Bag options)
    {
        var value = options.Get("fractionalSecondDigits");
        if (value is null)
            return TemporalFormat.AutoPrecision;
        if (value is not double number)
            return TemporalOptionRules.IsFractionalSecondDigitsAuto(ToStringValue(value)) ? TemporalFormat.AutoPrecision : throw new RangeError();

        return TemporalOptionRules.TryParseFractionalSecondDigits(number, out var digits) ? digits : throw new RangeError();
    }

    // ---- Differences ----------------------------------------------------------------------------------------------

    // GetDifferenceSettings's four reads in code unit order, then the type's rules.
    private DifferenceSettings DifferenceSettingsOf(TemporalType type, DifferenceOperation operation, object? options)
    {
        var resolved = OptionsObject(options);
        var largest = UnitOption(resolved, "largestUnit");
        var increment = RoundingIncrementOption(resolved);
        var mode = RoundingModeOption(resolved, TemporalOptionRules.DefaultDifferenceRoundingMode);
        var smallest = UnitOption(resolved, "smallestUnit");
        return TemporalOptionRules.TryGetDifferenceSettings(type, operation, largest, increment, mode, smallest, out var settings) ? settings : throw new RangeError();
    }

    /// <summary>Temporal.PlainDate.prototype.until and since (DifferenceTemporalPlainDate, s3.5.14).</summary>
    internal Duration Difference(DifferenceOperation operation, PlainDate date, object? otherValue, object? options)
    {
        var other = ToPlainDate(otherValue);
        if (other.Calendar != date.Calendar)
            throw new RangeError();

        var settings = DifferenceSettingsOf(TemporalType.PlainDate, operation, options);
        Check(TemporalDifferences.DifferenceTemporalPlainDate(operation, Calendars, date.Calendar, date.Date, other.Date, settings, out var result));
        return new Duration(result);
    }

    /// <summary>Temporal.PlainDateTime.prototype.until and since (DifferenceTemporalPlainDateTime, s5.5.15).</summary>
    internal Duration Difference(DifferenceOperation operation, PlainDateTime dateTime, object? otherValue, object? options)
    {
        var other = ToPlainDateTime(otherValue);
        if (other.Calendar != dateTime.Calendar)
            throw new RangeError();

        var settings = DifferenceSettingsOf(TemporalType.PlainDateTime, operation, options);
        Check(TemporalDifferences.DifferenceTemporalPlainDateTime(operation, Calendars, dateTime.Calendar, dateTime.DateTime, other.DateTime, settings, out var result));
        return new Duration(result);
    }

    /// <summary>Temporal.PlainTime.prototype.until and since (DifferenceTemporalPlainTime, s4.5.17).</summary>
    internal Duration Difference(DifferenceOperation operation, PlainTime time, object? otherValue, object? options)
    {
        var other = ToPlainTime(otherValue);
        var settings = DifferenceSettingsOf(TemporalType.PlainTime, operation, options);
        Check(TemporalDifferences.DifferenceTemporalPlainTime(operation, time.Time, other.Time, settings, out var result));
        return new Duration(result);
    }

    /// <summary>Temporal.Instant.prototype.until and since (DifferenceTemporalInstant, s8.5.9).</summary>
    internal Duration Difference(DifferenceOperation operation, Instant instant, object? otherValue, object? options)
    {
        var other = ToInstant(otherValue);
        var settings = DifferenceSettingsOf(TemporalType.Instant, operation, options);
        Check(TemporalDifferences.DifferenceTemporalInstant(operation, instant.EpochNanoseconds, other.EpochNanoseconds, settings, out var result));
        return new Duration(result);
    }

    /// <summary>Temporal.ZonedDateTime.prototype.until and since (DifferenceTemporalZonedDateTime, s6.5.9).</summary>
    internal Duration Difference(DifferenceOperation operation, ZonedDateTime zoned, object? otherValue, object? options)
    {
        var other = ToZonedDateTime(otherValue);
        if (other.Calendar != zoned.Calendar)
            throw new RangeError();

        var settings = DifferenceSettingsOf(TemporalType.ZonedDateTime, operation, options);
        if (TemporalOptionRules.IsDateUnit(settings.LargestUnit) && !TimeZoneEquals(zoned.TimeZone, other.TimeZone))
            throw new RangeError();

        Check(TemporalDifferences.DifferenceTemporalZonedDateTime(
            operation, zoned.EpochNanoseconds, other.EpochNanoseconds, Zone(zoned.TimeZone), Calendars, zoned.Calendar, settings, out var result));
        return new Duration(result);
    }

    /// <summary>Temporal.PlainYearMonth.prototype.until and since (DifferenceTemporalPlainYearMonth, s9.5.7).</summary>
    internal Duration Difference(DifferenceOperation operation, PlainYearMonth yearMonth, object? otherValue, object? options)
    {
        var other = ToPlainYearMonth(otherValue);
        if (other.Calendar != yearMonth.Calendar)
            throw new RangeError();

        var settings = DifferenceSettingsOf(TemporalType.PlainYearMonth, operation, options);
        Check(TemporalDifferences.DifferenceTemporalPlainYearMonth(operation, Calendars, yearMonth.Calendar, yearMonth.Date, other.Date, settings, out var result));
        return new Duration(result);
    }

    // ---- Addition of points (balancing's compositions, the adapter guide's) -------------------------------------

    // A duration argument, negated for subtract.
    private Duration SignedDuration(object? like, bool subtract)
    {
        var duration = ToDuration(like);
        return subtract ? new Duration(DurationMath.Negate(duration.Fields)) : duration;
    }

    /// <summary>Temporal.PlainDate.prototype.add and subtract (AddDurationToDate, s3.5.15).</summary>
    internal PlainDate Add(PlainDate date, object? like, object? options, bool subtract)
    {
        var duration = SignedDuration(like, subtract);
        var internalDuration = DurationMath.ToInternalWith24HourDays(duration.Fields);
        var days = BigInteger.Divide(internalDuration.TimeNanoseconds, new BigInteger(TemporalLimits.NanosecondsPerDay));
        Check(DurationMath.CreateDateDurationRecord(
            internalDuration.Date.Years, internalDuration.Date.Months, internalDuration.Date.Weeks, ExactRounding.ToDouble(days, BigInteger.One), out var dateDuration));
        var overflow = OverflowOption(OptionsObject(options));
        return new PlainDate(DateAdd(date.Calendar, date.Date, dateDuration, overflow), date.Calendar);
    }

    // CalendarDateAdd through the calendars, its status the error.
    private IsoDate DateAdd(CalendarId calendar, IsoDate date, DateDuration duration, TemporalOverflow overflow)
    {
        if (Math.Abs(duration.Years) > 4e18 || Math.Abs(duration.Months) > 4e18 || Math.Abs(duration.Weeks) > 4e18 || Math.Abs(duration.Days) > 4e18)
            throw new RangeError();

        Check(Calendars.DateAdd(
            calendar, date, new CalendarDuration((long)duration.Years, (long)duration.Months, (long)duration.Weeks, (long)duration.Days), ToCalendar(overflow), out var result));
        return result;
    }

    /// <summary>Temporal.PlainDateTime.prototype.add and subtract (AddDurationToDateTime, s5.5.16).</summary>
    internal PlainDateTime Add(PlainDateTime dateTime, object? like, object? options, bool subtract)
    {
        var duration = SignedDuration(like, subtract);
        var overflow = OverflowOption(OptionsObject(options));
        var internalDuration = DurationMath.ToInternalWith24HourDays(duration.Fields);
        var time = IsoArithmetic.AddTime(dateTime.DateTime.Time, internalDuration.TimeNanoseconds, out var days);
        Check(DurationMath.CreateDateDurationRecord(internalDuration.Date.Years, internalDuration.Date.Months, internalDuration.Date.Weeks, days, out var dateDuration));
        var added = DateAdd(dateTime.Calendar, dateTime.DateTime.Date, dateDuration, overflow);
        return CreatePlainDateTime(new IsoDateTime(added, time), dateTime.Calendar);
    }

    /// <summary>Temporal.PlainTime.prototype.add and subtract (AddDurationToTime, s4.5.18).</summary>
    internal PlainTime Add(PlainTime time, object? like, bool subtract)
    {
        var duration = SignedDuration(like, subtract);
        return new PlainTime(IsoArithmetic.AddTime(time.Time, DurationMath.ToInternal(duration.Fields).TimeNanoseconds, out _));
    }

    /// <summary>Temporal.Instant.prototype.add and subtract (AddDurationToInstant, s8.5.10).</summary>
    internal Instant Add(Instant instant, object? like, bool subtract)
    {
        var duration = SignedDuration(like, subtract);
        if (TemporalOptionRules.IsDateUnit(DurationMath.DefaultLargestUnit(duration.Fields)))
            throw new RangeError();

        Check(IsoArithmetic.AddInstant(instant.EpochNanoseconds, DurationMath.ToInternalWith24HourDays(duration.Fields).TimeNanoseconds, out var result));
        return new Instant(result);
    }

    /// <summary>Temporal.ZonedDateTime.prototype.add and subtract (AddDurationToZonedDateTime, s6.5.10).</summary>
    internal ZonedDateTime Add(ZonedDateTime zoned, object? like, object? options, bool subtract)
    {
        var duration = SignedDuration(like, subtract);
        var overflow = OverflowOption(OptionsObject(options));
        Check(DurationMath.AddZonedDateTime(
            zoned.EpochNanoseconds, Zone(zoned.TimeZone), Calendars, zoned.Calendar, DurationMath.ToInternal(duration.Fields), overflow, out var result));
        return new ZonedDateTime(result, zoned.TimeZone, zoned.Calendar);
    }

    /// <summary>Temporal.PlainYearMonth.prototype.add and subtract (AddDurationToYearMonth, s9.5.8).</summary>
    internal PlainYearMonth Add(PlainYearMonth yearMonth, object? like, object? options, bool subtract)
    {
        var duration = SignedDuration(like, subtract);
        var internalDuration = DurationMath.ToInternal(duration.Fields);
        var overflow = OverflowOption(OptionsObject(options));
        var date = internalDuration.Date;
        if (date.Weeks != 0 || date.Days != 0 || !internalDuration.TimeNanoseconds.IsZero)
            throw new RangeError();

        Check(Calendars.DateToFields(yearMonth.Calendar, yearMonth.Date, CalendarFieldsType.YearMonth, out var fields));
        Check(Calendars.DateFromFields(yearMonth.Calendar, fields with { Day = 1 }, CalendarOverflow.Constrain, out var first));
        var added = DateAdd(yearMonth.Calendar, first, date, overflow);
        Check(Calendars.DateToFields(yearMonth.Calendar, added, CalendarFieldsType.YearMonth, out var addedFields));
        Check(Calendars.YearMonthFromFields(yearMonth.Calendar, addedFields, ToCalendar(overflow), out var result));
        return new PlainYearMonth(result, yearMonth.Calendar);
    }

    // ---- Rounding of points ---------------------------------------------------------------------------------------

    // A round argument: a string names smallestUnit, undefined is a TypeError, anything else the options.
    private (long Increment, RoundingMode Mode, TemporalUnit Smallest) RoundOptions(TemporalType type, object? roundToValue)
    {
        var roundTo = StringOrOptions(roundToValue, "smallestUnit");
        var increment = RoundingIncrementOption(roundTo);
        var mode = RoundingModeOption(roundTo, TemporalOptionRules.DefaultRoundRoundingMode);
        var smallest = UnitOption(roundTo, "smallestUnit", required: true);
        return TemporalOptionRules.IsValidRoundOptions(type, smallest, increment) ? (increment, mode, smallest) : throw new RangeError();
    }

    /// <summary>Temporal.PlainTime.prototype.round.</summary>
    internal PlainTime Round(PlainTime time, object? roundTo)
    {
        var (increment, mode, smallest) = RoundOptions(TemporalType.PlainTime, roundTo);
        return new PlainTime(IsoArithmetic.RoundTime(time.Time, increment, smallest, mode, out _));
    }

    /// <summary>Temporal.PlainDateTime.prototype.round.</summary>
    internal PlainDateTime Round(PlainDateTime dateTime, object? roundTo)
    {
        var (increment, mode, smallest) = RoundOptions(TemporalType.PlainDateTime, roundTo);
        if (smallest == TemporalUnit.Nanosecond && increment == 1)
            return dateTime;

        return CreatePlainDateTime(IsoArithmetic.RoundIsoDateTime(dateTime.DateTime, increment, smallest, mode), dateTime.Calendar);
    }

    /// <summary>Temporal.Instant.prototype.round.</summary>
    internal Instant Round(Instant instant, object? roundTo)
    {
        var (increment, mode, smallest) = RoundOptions(TemporalType.Instant, roundTo);
        var rounded = IsoArithmetic.RoundTemporalInstant(instant.EpochNanoseconds, increment, smallest, mode);
        Check(TemporalLimits.CheckEpochNanoseconds(rounded));
        return new Instant(rounded);
    }

    /// <summary>Temporal.ZonedDateTime.prototype.round (s6.3.39).</summary>
    internal ZonedDateTime Round(ZonedDateTime zoned, object? roundTo)
    {
        var (increment, mode, smallest) = RoundOptions(TemporalType.ZonedDateTime, roundTo);
        if (smallest == TemporalUnit.Nanosecond && increment == 1)
            return zoned;

        var dateTime = IsoDateTimeFor(zoned.TimeZone, zoned.EpochNanoseconds);
        BigInteger epoch;
        if (smallest == TemporalUnit.Day)
        {
            var start = StartOfDay(zoned.TimeZone, dateTime.Date);
            var end = StartOfDay(zoned.TimeZone, IsoArithmetic.AddDaysToIsoDate(dateTime.Date, 1));
            Check(DurationMath.RoundTimeDurationToIncrement(zoned.EpochNanoseconds - start, end - start, mode, out var rounded));
            epoch = start + rounded;
        }
        else
        {
            var rounded = IsoArithmetic.RoundIsoDateTime(dateTime, increment, smallest, mode);
            var offset = OffsetNanosecondsFor(zoned.TimeZone, zoned.EpochNanoseconds);
            epoch = InterpretIsoDateTimeOffset(rounded.Date, rounded.Time, "option", offset, zoned.TimeZone, Disambiguation.Compatible, "prefer", matchMinutes: false);
        }

        Check(TemporalLimits.CheckEpochNanoseconds(epoch));
        return new ZonedDateTime(epoch, zoned.TimeZone, zoned.Calendar);
    }

    /// <summary>Temporal.ZonedDateTime.prototype.hoursInDay.</summary>
    internal double HoursInDay(ZonedDateTime zoned)
    {
        var today = IsoDateTimeFor(zoned.TimeZone, zoned.EpochNanoseconds).Date;
        var tomorrow = IsoArithmetic.AddDaysToIsoDate(today, 1);
        return DurationMath.TotalTimeDuration(StartOfDay(zoned.TimeZone, tomorrow) - StartOfDay(zoned.TimeZone, today), TemporalUnit.Hour);
    }

    // ---- Constructors and conversions between the types -----------------------------------------------------------

    /// <summary>new Temporal.ZonedDateTime(epochNanoseconds, timeZone, calendar).</summary>
    internal ZonedDateTime NewZonedDateTime(object? epochNanoseconds, object? timeZone, object? calendar = null)
    {
        if (epochNanoseconds is not BigInteger epoch)
            throw new TypeError();

        Check(TemporalLimits.CheckEpochNanoseconds(epoch));
        var zone = TimeZoneFromIdentifier(timeZone);
        return new ZonedDateTime(epoch, zone, CalendarArgument(calendar));
    }

    /// <summary>Temporal.PlainDate.prototype.withCalendar.</summary>
    internal static PlainDate WithCalendar(PlainDate date, object? calendar) => new(date.Date, ToTemporalCalendarIdentifier(calendar));

    /// <summary>Temporal.PlainDate.prototype.toPlainYearMonth: the calendar's year-month of the date (s3.3.31).</summary>
    internal PlainYearMonth DateToPlainYearMonth(PlainDate date)
    {
        Check(Calendars.DateToYearMonth(date.Calendar, date.Date, out var result));
        return new PlainYearMonth(result, date.Calendar);
    }

    /// <summary>Temporal.PlainYearMonth.prototype.toPlainDate (s9.3.20): the year-month with a bag's day.</summary>
    internal PlainDate YearMonthToPlainDate(PlainYearMonth yearMonth, object? item)
    {
        if (item is not Bag bag)
            throw new TypeError();

        var fields = PrepareFields(yearMonth.Calendar, bag, date: false, time: false, offset: false, zone: false);
        var day = bag.Get("day") is { } dayValue ? ToPositiveIntegerWithTruncation(dayValue) : (double?)null;
        Check(Calendars.YearMonthToDate(yearMonth.Calendar, yearMonth.Date, fields.Calendar with { Day = day }, out var result));
        return new PlainDate(result, yearMonth.Calendar);
    }

    /// <summary>Temporal.PlainMonthDay.prototype.toPlainDate: the month-day with a bag's year (and era fields where the calendar has eras).</summary>
    internal PlainDate MonthDayToPlainDate(PlainMonthDay monthDay, object? item)
    {
        if (item is not Bag bag)
            throw new TypeError();

        // PrepareCalendarFields over « year » and the calendar's extra fields, in code unit order.
        var fields = default(CalendarFields);
        if (TemporalCalendars.SupportsEra(monthDay.Calendar))
        {
            if (bag.Get("era") is { } era)
                fields = fields with { Era = ToStringValue(era) };
            if (bag.Get("eraYear") is { } eraYear)
                fields = fields with { EraYear = ToIntegerWithTruncation(eraYear) };
        }

        if (bag.Get("year") is { } year)
            fields = fields with { Year = ToIntegerWithTruncation(year) };

        Check(Calendars.MonthDayToDate(monthDay.Calendar, monthDay.Date, fields, out var result));
        return new PlainDate(result, monthDay.Calendar);
    }

    /// <summary>
    /// Temporal.ZonedDateTime.prototype.with: the zoned value's wall fields and offset merged with a partial bag's, then
    /// interpreted with the offset option's default <c>prefer</c>, the offset matched exactly.
    /// </summary>
    internal ZonedDateTime With(ZonedDateTime zoned, object? like, object? options)
    {
        if (like is not Bag bag)
            throw new TypeError();
        if (bag.Get("calendar") is not null || bag.Get("timeZone") is not null)
            throw new TypeError(); // RejectTemporalLikeObject

        var offsetNanoseconds = OffsetNanosecondsFor(zoned.TimeZone, zoned.EpochNanoseconds);
        var dateTime = IsoDateTimeFor(zoned.TimeZone, zoned.EpochNanoseconds);
        Check(Calendars.DateToFields(zoned.Calendar, dateTime.Date, CalendarFieldsType.Date, out var own));
        var partial = PrepareFields(zoned.Calendar, bag, date: true, time: true, offset: true, zone: false, partial: true);
        var merged = new Fields
        {
            Calendar = TemporalCalendars.MergeFields(zoned.Calendar, own, partial.Calendar),
            Hour = partial.Hour ?? dateTime.Time.Hour,
            Minute = partial.Minute ?? dateTime.Time.Minute,
            Second = partial.Second ?? dateTime.Time.Second,
            Millisecond = partial.Millisecond ?? dateTime.Time.Millisecond,
            Microsecond = partial.Microsecond ?? dateTime.Time.Microsecond,
            Nanosecond = partial.Nanosecond ?? dateTime.Time.Nanosecond,
        };
        var resolved = OptionsObject(options);
        var disambiguation = DisambiguationOption(resolved);
        var offsetOption = OffsetOption(resolved, "prefer");
        var overflow = OverflowOption(resolved);
        var result = InterpretDateTimeFields(zoned.Calendar, merged, overflow);
        if (partial.Offset is { } offsetText)
            TemporalGrammar.TryParseOffsetString(offsetText, out offsetNanoseconds);

        var epoch = InterpretIsoDateTimeOffset(result.Date, result.Time, "option", offsetNanoseconds, zoned.TimeZone, disambiguation, offsetOption, matchMinutes: false);
        return new ZonedDateTime(epoch, zoned.TimeZone, zoned.Calendar);
    }

    /// <summary>Temporal.PlainDate.prototype.toZonedDateTime with a time zone string: the start of the day in the zone.</summary>
    internal ZonedDateTime ToZonedDateTime(PlainDate date, object? item)
    {
        if (item is Bag bag)
        {
            var zone = ToTemporalTimeZoneIdentifier(bag.Get("timeZone") ?? throw new TypeError());
            if (bag.Get("plainTime") is { } timeValue)
            {
                var time = ToPlainTime(timeValue).Time;
                var dateTime = new IsoDateTime(date.Date, time);
                Check(TemporalLimits.CheckIsoDateTime(dateTime));
                return new ZonedDateTime(EpochNanosecondsFor(zone, dateTime, Disambiguation.Compatible), zone, date.Calendar);
            }

            return new ZonedDateTime(StartOfDay(zone, date.Date), zone, date.Calendar);
        }

        var identifier = ToTemporalTimeZoneIdentifier(item);
        return new ZonedDateTime(StartOfDay(identifier, date.Date), identifier, date.Calendar);
    }

    /// <summary>Temporal.PlainDateTime.prototype.toZonedDateTime: GetEpochNanosecondsFor under the disambiguation option.</summary>
    internal ZonedDateTime ToZonedDateTime(PlainDateTime dateTime, object? timeZone, object? options)
    {
        var zone = ToTemporalTimeZoneIdentifier(timeZone);
        var disambiguation = DisambiguationOption(OptionsObject(options));
        return new ZonedDateTime(EpochNanosecondsFor(zone, dateTime.DateTime, disambiguation), zone, dateTime.Calendar);
    }

    /// <summary>Temporal.Instant.prototype.toZonedDateTimeISO.</summary>
    internal ZonedDateTime ToZonedDateTimeIso(Instant instant, object? timeZone) =>
        new(instant.EpochNanoseconds, ToTemporalTimeZoneIdentifier(timeZone), CalendarId.Iso8601);

    /// <summary>Temporal.ZonedDateTime.prototype.getTimeZoneTransition: SJL-8's next or previous transition, or null.</summary>
    internal ZonedDateTime? Transition(ZonedDateTime zoned, object? direction)
    {
        var text = direction switch
        {
            string s => s,
            Bag bag => bag.Get("direction") is { } value ? ToStringValue(value) : throw new RangeError(),
            null => throw new TypeError(),
            _ => throw new TypeError(),
        };
        var rules = ((TemporalTimeZone)Zone(zoned.TimeZone)).Rules;
        var found = text switch
        {
            "next" => ZonedTime.GetNextTransition(rules, zoned.EpochNanoseconds),
            "previous" => ZonedTime.GetPreviousTransition(rules, zoned.EpochNanoseconds),
            _ => throw new RangeError(),
        };
        return found is { } epoch ? new ZonedDateTime(epoch, zoned.TimeZone, zoned.Calendar) : null;
    }

    /// <summary>The calendar's fields of a date: the getters' values (CalendarISOToDate).</summary>
    internal CalendarDate CalendarFieldsOf(IsoDate date, CalendarId calendar)
    {
        Check(Calendars.IsoToDate(calendar, date, out var fields));
        return fields;
    }

    /// <summary>Temporal.PlainDate.prototype.with (s3.3.24): the date's fields merged with a partial bag's, then regulated.</summary>
    internal PlainDate With(PlainDate date, object? like, object? options)
    {
        if (like is not Bag bag)
            throw new TypeError();
        if (bag.Get("calendar") is not null || bag.Get("timeZone") is not null)
            throw new TypeError(); // RejectTemporalLikeObject

        Check(Calendars.DateToFields(date.Calendar, date.Date, CalendarFieldsType.Date, out var own));
        var partial = PrepareFields(date.Calendar, bag, date: true, time: false, offset: false, zone: false, partial: true);
        var merged = TemporalCalendars.MergeFields(date.Calendar, own, partial.Calendar);
        var overflow = OverflowOption(OptionsObject(options));
        Check(Calendars.DateFromFields(date.Calendar, merged, ToCalendar(overflow), out var result));
        return new PlainDate(result, date.Calendar);
    }

    // ---- String forms with options -------------------------------------------------------------------------------

    // The toString options of the time-bearing types, read in code unit order: the digits, the mode, the smallest unit.
    private SecondsStringPrecision PrecisionOptions(Bag options, out RoundingMode mode, bool allowMinute = true)
    {
        var digits = FractionalSecondDigitsOption(options);
        mode = RoundingModeOption(options, TemporalOptionRules.DefaultToStringRoundingMode);
        var smallest = UnitOption(options, "smallestUnit");
        if (!TemporalOptionRules.IsValidToStringSmallestUnit(smallest, duration: !allowMinute))
            throw new RangeError();

        return TemporalOptionRules.ToSecondsStringPrecisionRecord(smallest, digits);
    }

    /// <summary>Temporal.PlainDate.prototype.toString with options: calendarName.</summary>
    internal string ToString(PlainDate date, object? options)
    {
        var resolved = OptionsObject(options);
        var show = ShowCalendarOption(resolved);
        return TemporalFormat.DateToString(date.Date, TemporalCalendars.Identifier(date.Calendar), show);
    }

    /// <summary>Temporal.PlainDateTime.prototype.toString with options.</summary>
    internal string ToString(PlainDateTime dateTime, object? options)
    {
        var resolved = OptionsObject(options);
        var show = ShowCalendarOption(resolved);
        var precision = PrecisionOptions(resolved, out var mode);
        Check(TemporalFormat.TryPlainDateTimeToString(dateTime.DateTime, TemporalCalendars.Identifier(dateTime.Calendar), precision, mode, show, out var text));
        return text!;
    }

    /// <summary>Temporal.PlainTime.prototype.toString with options.</summary>
    internal string ToString(PlainTime time, object? options)
    {
        var precision = PrecisionOptions(OptionsObject(options), out var mode);
        return TemporalFormat.PlainTimeToString(time.Time, precision, mode);
    }

    /// <summary>Temporal.Instant.prototype.toString with options: the instant rounded, then the offset at the rounded instant.</summary>
    internal string ToString(Instant instant, object? options)
    {
        var resolved = OptionsObject(options);
        var precision = PrecisionOptions(resolved, out var mode);
        var zoneValue = resolved.Get("timeZone");
        var zone = zoneValue is null ? null : ToTemporalTimeZoneIdentifier(zoneValue);
        var rounded = IsoArithmetic.RoundTemporalInstant(instant.EpochNanoseconds, precision.Increment, precision.Unit, mode);
        Check(TemporalLimits.CheckEpochNanoseconds(rounded));
        return TemporalFormat.InstantToString(rounded, zone is null ? null : OffsetNanosecondsFor(zone, rounded), precision.Precision);
    }

    /// <summary>Temporal.ZonedDateTime.prototype.toString with options.</summary>
    internal string ToString(ZonedDateTime zoned, object? options)
    {
        var resolved = OptionsObject(options);
        var show = ShowCalendarOption(resolved);
        var digits = FractionalSecondDigitsOption(resolved);
        var offsetShow = StringOption(resolved, "offset") is { } offsetText
            ? TemporalOptionRules.TryParseShowOffset(offsetText, out var parsedOffset) ? parsedOffset : throw new RangeError()
            : ShowOffset.Auto;
        var mode = RoundingModeOption(resolved, TemporalOptionRules.DefaultToStringRoundingMode);
        var smallest = UnitOption(resolved, "smallestUnit");
        var zoneShow = StringOption(resolved, "timeZoneName") is { } zoneText
            ? TemporalOptionRules.TryParseShowTimeZoneName(zoneText, out var parsedZone) ? parsedZone : throw new RangeError()
            : ShowTimeZoneName.Auto;
        if (!TemporalOptionRules.IsValidToStringSmallestUnit(smallest, duration: false))
            throw new RangeError();

        var precision = TemporalOptionRules.ToSecondsStringPrecisionRecord(smallest, digits);
        var rounded = IsoArithmetic.RoundTemporalInstant(zoned.EpochNanoseconds, precision.Increment, precision.Unit, mode);
        Check(TemporalLimits.CheckEpochNanoseconds(rounded));
        return TemporalFormat.ZonedDateTimeToString(
            rounded, OffsetNanosecondsFor(zoned.TimeZone, rounded), zoned.TimeZone, TemporalCalendars.Identifier(zoned.Calendar), precision.Precision, show, zoneShow, offsetShow);
    }

    // GetTemporalShowCalendarNameOption (s13.10).
    private ShowCalendarName ShowCalendarOption(Bag options)
    {
        if (StringOption(options, "calendarName") is not { } text)
            return ShowCalendarName.Auto;

        return TemporalOptionRules.TryParseShowCalendarName(text, out var show) ? show : throw new RangeError();
    }

    /// <summary>Temporal.ZonedDateTime.prototype.startOfDay.</summary>
    internal ZonedDateTime StartOfDay(ZonedDateTime zoned) =>
        new(StartOfDay(zoned.TimeZone, IsoDateTimeFor(zoned.TimeZone, zoned.EpochNanoseconds).Date), zoned.TimeZone, zoned.Calendar);
}
