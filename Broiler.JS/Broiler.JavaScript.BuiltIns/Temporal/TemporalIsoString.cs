using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// Shared parsing of a Temporal ISO 8601 string for the *slot-value* contexts where a calendar or a
// time zone may be supplied as a full date/time string rather than a bare identifier:
//
//   • ToTemporalCalendarSlotValue → ParseTemporalCalendarString: a calendar field given as a
//     Temporal string adopts the string's [u-ca=…] annotation (defaulting to iso8601).
//   • ToTemporalTimeZoneIdentifier → ParseTemporalTimeZoneString: a timeZone field given as a
//     Temporal string adopts the string's time-zone designator — a [TimeZone] annotation, a Z
//     (UTC) designator, or a numeric UTC offset, in that order of preference.
//
// The parser validates that the input is a plausible Temporal string (date, date-time, time-only,
// year-month or month-day, each with optional annotations) and is intentionally permissive about
// the calendar/offset payloads it does not itself canonicalize — the callers run the extracted
// designator through the strict calendar / time-zone canonicalizers.
internal static class TemporalIsoString
{
    // The time-of-day + UTC-offset tail permitted after the date in a Temporal date / date-time
    // string: an optional T / space separator and HH[:MM[:SS]] with a fraction allowed only on the
    // smallest (seconds) component, then an optional Z or numeric UTC offset. Mirrors the strict
    // PlainDateTime time grammar so the date-only parsers reject a fraction on the minutes or hours.
    internal const string TimeAndOffsetTail =
        @"(?:[Tt ](?<th>\d{2})(?::?(?<tmin>\d{2})(?::?(?<tsec>\d{2})(?:[.,]\d{1,9})?)?)?" +
        @"(?<toffset>[Zz]|[+-]\d{2}(?::?\d{2}(?::?\d{2}(?:[.,]\d{1,9})?)?)?)?)?";

    // The calendar-only parsers (PlainDate / PlainYearMonth / PlainMonthDay) discard the optional
    // time tail captured by TimeAndOffsetTail, but a string is still rejected (RangeError) when it
    // carries a UTC (Z) designator — these types have no time zone — or an out-of-range wall-clock
    // time component. (Spec ToTemporalDate / ToTemporalYearMonth / ToTemporalMonthDay: the parsed
    // [[Z]] flag and an invalid time both throw, even though the time-of-day itself is unused.)
    internal static void RejectTimeTailForCalendarOnly(Match m, string text)
    {
        var offset = m.Groups["toffset"];
        if (offset.Success && (offset.Value == "Z" || offset.Value == "z"))
            throw JSEngine.NewRangeError($"Temporal: \"{text}\" has a UTC (Z) designator but no time zone");

        var h = m.Groups["th"];
        if (!h.Success) return;
        var hour = int.Parse(h.Value, CultureInfo.InvariantCulture);
        var minute = m.Groups["tmin"].Success ? int.Parse(m.Groups["tmin"].Value, CultureInfo.InvariantCulture) : 0;
        var second = m.Groups["tsec"].Success ? int.Parse(m.Groups["tsec"].Value, CultureInfo.InvariantCulture) : 0;
        if (hour > 23 || minute > 59 || second > 60)
            throw JSEngine.NewRangeError($"Temporal: \"{text}\" has an out-of-range time component");
    }

    // Trailing [..] annotations (a time-zone annotation and/or one or more key=value annotations).
    internal const string AnnotationsTail = @"(?:\[[^\]]*\])*";

    // The monthCode field of a property bag is required to resolve to a String (PrepareCalendarFields
    // treats it as a "to-monthcode" conversion: ToPrimitive(value, string) then a String type check).
    // A primitive other than a String — Number, Boolean, BigInt, … — is a TypeError before the
    // format / calendar-suitability checks that produce a RangeError. An Object is coerced through
    // ToPrimitive (its @@toPrimitive / toString / valueOf), and the resulting primitive must itself
    // be a String. Returns null for an undefined / missing field so the caller can keep its
    // "field is absent" shortcut.
    internal static string RequireMonthCodeString(JSValue value, string typeName)
        => RequireStringField(value, typeName, "monthCode");

    // The offset field has the same "to-string-or-TypeError" coercion as monthCode (test262
    // built-ins/Temporal/Duration/.../relativeto-propertybag-invalid-offset-string and friends).
    // A Number, BigInt, null, boolean, … is a TypeError; an Object goes through ToPrimitive and
    // the resulting primitive must itself be a String.
    internal static string RequireOffsetString(JSValue value, string typeName)
        => RequireStringField(value, typeName, "offset");

    private static string RequireStringField(JSValue value, string typeName, string field)
    {
        if (value == null || value.IsUndefined) return null;
        if (value is Runtime.JSObject obj)
        {
            var primitive = obj.ToStringPrimitive();
            if (!primitive.IsString)
                throw JSEngine.NewTypeError($"{typeName}: {field} must be a string");
            return primitive.StringValue;
        }
        if (!value.IsString)
            throw JSEngine.NewTypeError($"{typeName}: {field} must be a string");
        return value.StringValue;
    }

    // Group 1 captures the critical flag ("!") when present, so a calendar annotation can be
    // classified as critical or not.
    private static readonly Regex CalendarAnnotationPattern =
        new(@"\[(!?)u-ca=[^\]]+\]", RegexOptions.CultureInvariant);

    // A Temporal / RFC 9557 string may carry more than one calendar (u-ca) annotation only when
    // none of them is flagged critical ("!"); the first wins and the rest are ignored
    // (e.g. "[u-ca=iso8601][u-ca=discord]" parses as iso8601). Two or more calendar annotations
    // where any is critical — "[u-ca=iso8601][!u-ca=iso8601]" — is a RangeError (ParseISODateTime).
    internal static void RejectMultipleCalendarAnnotations(string text)
    {
        var matches = CalendarAnnotationPattern.Matches(text);
        if (matches.Count <= 1) return;

        foreach (Match m in matches)
        {
            if (m.Groups[1].Value.Length > 0) // a critical ("!") calendar annotation among several
                throw JSEngine.NewRangeError($"Temporal: more than one calendar annotation in \"{text}\"");
        }
    }

    // Each trailing [..] annotation is either a TimeZoneIdentifier (no '=') or a key=value
    // Annotation whose AnnotationKey is lowercase ASCII (a-z then a-z/0-9/-/_) and whose
    // AnnotationValue is one or more '-'-separated alphanumeric components. A malformed key (e.g.
    // the uppercase "U-CA" / "FOO") or value is a RangeError.
    private static readonly Regex AnnotationBracketPattern = new(@"\[([^\]]*)\]", RegexOptions.CultureInvariant);
    private static readonly Regex AnnotationKeyPattern = new(@"^[a-z_][a-z0-9_-]*$", RegexOptions.CultureInvariant);
    private static readonly Regex AnnotationValuePattern = new(@"^[A-Za-z0-9]+(?:-[A-Za-z0-9]+)*$", RegexOptions.CultureInvariant);

    internal static void RejectMalformedAnnotations(string text)
    {
        foreach (Match m in AnnotationBracketPattern.Matches(text))
        {
            var content = m.Groups[1].Value;
            if (content.StartsWith("!", StringComparison.Ordinal)) content = content.Substring(1);
            var eq = content.IndexOf('=');
            if (eq < 0) continue; // a time-zone annotation, validated elsewhere
            var key = content.Substring(0, eq);
            var value = content.Substring(eq + 1);
            if (!AnnotationKeyPattern.IsMatch(key) || !AnnotationValuePattern.IsMatch(value))
                throw JSEngine.NewRangeError($"Temporal: malformed annotation \"[{m.Groups[1].Value}]\"");
        }
    }

    // RFC 9557 annotation rules shared by every Temporal string parser:
    //   • At most one time-zone annotation (a bracket whose contents, after an optional critical "!",
    //     carry no '='); a second one — e.g. "...[UTC][UTC]" — is a RangeError.
    //   • An annotation flagged critical ("!") whose key is not one Temporal recognizes (only "u-ca"
    //     is) — e.g. "...[!foo=bar]" — is a RangeError. A non-critical unknown annotation is ignored.
    //   • A time-zone annotation that is a numeric UTC offset may only carry minute precision
    //     (±HH, ±HH:MM, ±HHMM); a sub-minute offset such as "[-07:00:01]" is a RangeError even though
    //     the same offset is permitted on the wall clock.
    // (RejectMultipleCalendarAnnotations / RejectMalformedAnnotations cover the u-ca-specific checks.)
    internal static void RejectInvalidAnnotations(string text)
    {
        var timeZoneCount = 0;
        foreach (Match m in AnnotationBracketPattern.Matches(text))
        {
            var content = m.Groups[1].Value;
            var critical = content.StartsWith("!", StringComparison.Ordinal);
            if (critical) content = content.Substring(1);
            var eq = content.IndexOf('=');
            if (eq < 0)
            {
                if (++timeZoneCount > 1)
                    throw JSEngine.NewRangeError($"Temporal: more than one time zone annotation in \"{text}\"");
                if ((content.StartsWith("+", StringComparison.Ordinal) || content.StartsWith("-", StringComparison.Ordinal))
                    && !MinutePrecisionOffsetName.IsMatch(content))
                    throw JSEngine.NewRangeError($"Temporal: sub-minute offset in time zone annotation \"[{m.Groups[1].Value}]\" in \"{text}\"");
                continue;
            }

            var key = content.Substring(0, eq);
            if (critical && !key.Equals("u-ca", StringComparison.Ordinal))
                throw JSEngine.NewRangeError($"Temporal: unknown annotation with critical flag \"[{m.Groups[1].Value}]\" in \"{text}\"");
        }
    }

    // A numeric UTC offset used as a *time-zone annotation* is restricted to minute precision.
    private static readonly Regex MinutePrecisionOffsetName = new(@"^[+-]\d{2}(?::?\d{2})?$", RegexOptions.CultureInvariant);

    // A date or date-time. The date portion may use the extended (YYYY-MM-DD) or basic (YYYYMMDD)
    // form — but not a mix — and is validated separately by IsValidDate. Time, fraction, and
    // Z / numeric-offset designators are all optional.
    private static readonly Regex DateTimePattern = new(
        @"^(?<y>\d{4}|\+\d{6}|-(?!000000)\d{6})(?:-(?<mo>\d{2})-(?<d>\d{2})|(?<mo>\d{2})(?<d>\d{2}))" +
        @"(?:[Tt ](?<h>\d{2})(?::?(?<mi>\d{2})(?::?(?<s>\d{2})(?:[.,](?<f>\d{1,9}))?)?)?" +
        @"(?<offset>[Zz]|[+-]\d{2}(?::?\d{2}(?::?\d{2}(?:[.,]\d{1,9})?)?)?)?)?$",
        RegexOptions.CultureInvariant);

    // A bare time-of-day string, optionally prefixed with the time designator and carrying a Z or a
    // numeric offset.
    private static readonly Regex TimePattern = new(
        @"^[Tt]?(?<h>\d{2})(?::?(?<mi>\d{2})(?::?(?<s>\d{2})(?:[.,](?<f>\d{1,9}))?)?)?" +
        @"(?<offset>[Zz]|[+-]\d{2}(?::?\d{2}(?::?\d{2}(?:[.,]\d{1,9})?)?)?)?$",
        RegexOptions.CultureInvariant);

    private static readonly Regex YearMonthPattern = new(
        @"^(?<y>\d{4}|\+\d{6}|-(?!000000)\d{6})-?(?<mo>\d{2})$", RegexOptions.CultureInvariant);

    private static readonly Regex MonthDayPattern = new(
        @"^(?:--)?(?<mo>\d{2})-?(?<d>\d{2})$", RegexOptions.CultureInvariant);

    // A trailing [key=value] / [!key=value] / [TimeZone] annotation.
    private static readonly Regex TrailingAnnotation = new(@"\[(!?)([^\]]*)\]$", RegexOptions.CultureInvariant);

    private struct Parsed
    {
        public string CalendarAnnotation; // the value of the first [u-ca=…] annotation, or null
        public string TimeZoneAnnotation; // the value of a [TimeZone] annotation, or null
        public bool HasZ;                 // a Z / z designator was present
        public string Offset;             // a numeric UTC offset (±HH[:MM[:SS]]), or null
        public bool HasTime;              // a date-time / time-only form (carries a wall clock)
    }

    // Parses a Temporal ISO string, separating its trailing annotations from the date/time core and
    // validating the core. Returns false for anything that is not a recognizable Temporal string.
    private static bool TryParse(string text, out Parsed parsed)
    {
        parsed = default;
        if (text == null) return false;

        // Peel off trailing [..] annotations, recording the calendar (u-ca) and time-zone ones. A
        // time-zone annotation is the bracket whose contents are not a key=value pair.
        var core = text;
        var annotations = new List<string>();
        while (true)
        {
            var m = TrailingAnnotation.Match(core);
            if (!m.Success || m.Index + m.Length != core.Length) break;
            annotations.Add(m.Groups[2].Value);
            core = core.Substring(0, m.Index);
        }

        foreach (var ann in annotations) // outermost-first; the leftmost time-zone annotation wins
        {
            var eq = ann.IndexOf('=');
            if (eq < 0)
            {
                parsed.TimeZoneAnnotation ??= ann;
            }
            else if (parsed.CalendarAnnotation == null &&
                     ann.Substring(0, eq).Equals("u-ca", StringComparison.Ordinal))
            {
                parsed.CalendarAnnotation = ann.Substring(eq + 1);
            }
        }

        var dt = DateTimePattern.Match(core);
        if (dt.Success)
        {
            if (!IsValidDate(dt.Groups["y"].Value, dt.Groups["mo"].Value, dt.Groups["d"].Value)) return false;
            if (dt.Groups["h"].Success && !IsValidTime(dt)) return false;
            FillTime(ref parsed, dt);
            return true;
        }

        // A no-separator 6-digit string (e.g. "152330") matches YearMonthPattern as YYYYMM and a
        // 4-digit one matches MonthDayPattern; accept them only when they form a valid date, so an
        // invalid date (month 30) falls through to the time grammar below ("152330" → 15:23:30).
        if (YearMonthPattern.Match(core) is { Success: true } ym &&
            IsValidDate(ym.Groups["y"].Value, ym.Groups["mo"].Value, "01"))
            return true;

        if (MonthDayPattern.Match(core) is { Success: true } md &&
            IsValidDate("2000", md.Groups["mo"].Value, md.Groups["d"].Value))
            return true;

        var t = TimePattern.Match(core);
        if (t.Success)
        {
            if (!IsValidTime(t)) return false;
            FillTime(ref parsed, t);
            return true;
        }

        return false;
    }

    private static void FillTime(ref Parsed parsed, Match m)
    {
        if (!m.Groups["h"].Success) return; // a date-only string carries no clock / offset
        parsed.HasTime = true;
        var offset = m.Groups["offset"];
        if (!offset.Success) return;
        var value = offset.Value;
        if (value is "Z" or "z") parsed.HasZ = true;
        else parsed.Offset = value;
    }

    private static bool IsValidDate(string yearText, string monthText, string dayText)
    {
        var year = int.Parse(yearText, CultureInfo.InvariantCulture);
        var month = int.Parse(monthText, CultureInfo.InvariantCulture);
        var day = int.Parse(dayText, CultureInfo.InvariantCulture);
        if (month is < 1 or > 12) return false;
        return day >= 1 && day <= DaysInMonth(year, month);
    }

    private static bool IsValidTime(Match m)
    {
        var hour = int.Parse(m.Groups["h"].Value, CultureInfo.InvariantCulture);
        var minute = m.Groups["mi"].Success ? int.Parse(m.Groups["mi"].Value, CultureInfo.InvariantCulture) : 0;
        var second = m.Groups["s"].Success ? int.Parse(m.Groups["s"].Value, CultureInfo.InvariantCulture) : 0;
        return hour <= 23 && minute <= 59 && second <= 60; // a leap second (60) parses, then collapses
    }

    private static bool IsLeapYear(int y) => (y % 4 == 0 && y % 100 != 0) || y % 400 == 0;

    private static int DaysInMonth(int year, int month) => month switch
    {
        1 or 3 or 5 or 7 or 8 or 10 or 12 => 31,
        4 or 6 or 9 or 11 => 30,
        2 => IsLeapYear(year) ? 29 : 28,
        _ => 0,
    };

    // ParseTemporalCalendarString: succeeds for any valid Temporal ISO string, yielding its
    // [u-ca=…] annotation value (or null, meaning the iso8601 default).
    internal static bool TryExtractCalendar(string text, out string calendar)
    {
        if (TryParse(text, out var parsed))
        {
            calendar = parsed.CalendarAnnotation;
            return true;
        }

        calendar = null;
        return false;
    }

    // The toLocaleString options of a Temporal date/time type may not request a component the type
    // does not carry: a date-only type (PlainDate / PlainYearMonth / PlainMonthDay) rejects
    // timeStyle, and the time-only PlainTime rejects dateStyle, with a TypeError (matching the spec's
    // per-type Intl.DateTimeFormat field restrictions). The options argument is only inspected when it
    // is an object; other coercion is left to the (stubbed) formatter.
    internal static void RejectIncompatibleStyle(JSValue options, bool dateAllowed, bool timeAllowed)
    {
        if (options is not JSObject o) return;
        if (!timeAllowed && !o[KeyStrings.GetOrCreate("timeStyle")].IsUndefined)
            throw JSEngine.NewTypeError("Temporal: timeStyle is not allowed for a date-only type");
        if (!dateAllowed && !o[KeyStrings.GetOrCreate("dateStyle")].IsUndefined)
            throw JSEngine.NewTypeError("Temporal: dateStyle is not allowed for a time-only type");
    }

    // ParseTemporalTimeZoneString: succeeds only when the string carries a time-zone designator —
    // a [TimeZone] annotation, a Z (UTC) designator, or a numeric UTC offset, in that order — and
    // returns it for the caller to canonicalize.
    internal static bool TryExtractTimeZone(string text, out string designator)
    {
        designator = null;
        if (!TryParse(text, out var parsed)) return false;

        if (parsed.TimeZoneAnnotation != null) designator = parsed.TimeZoneAnnotation;
        else if (parsed.HasZ) designator = "UTC";
        else if (parsed.Offset != null) designator = parsed.Offset;

        return designator != null;
    }

    // For ToRelativeTemporalObject: parse a relativeTo string and surface its time-zone annotation, the
    // presence of a UTC (Z) designator, and any numeric offset (so the caller can apply the relativeTo
    // rules — a [TimeZone] annotation → ZonedDateTime, a Z with no annotation → RangeError, etc.).
    internal static bool TryParseRelative(string text, out string timeZoneAnnotation, out bool hasZ, out string offset)
    {
        timeZoneAnnotation = null;
        hasZ = false;
        offset = null;
        if (!TryParse(text, out var parsed)) return false;
        timeZoneAnnotation = parsed.TimeZoneAnnotation;
        hasZ = parsed.HasZ;
        offset = parsed.Offset;
        return true;
    }

    // A numeric UTC offset with consistent separators: ±HH, ±HH:MM(:SS(.fff)?)?, or ±HHMM(SS(.fff)?)?.
    // A mixed-separator offset such as "+00:0000" is rejected (the lenient parse pattern accepts it).
    private static readonly Regex StrictOffsetPattern = new(
        @"^[+-]\d{2}(:\d{2}(:\d{2}([.,]\d{1,9})?)?|\d{2}(\d{2}([.,]\d{1,9})?)?)?$", RegexOptions.CultureInvariant);

    internal static bool IsStrictOffset(string offset) => StrictOffsetPattern.IsMatch(offset);
}
