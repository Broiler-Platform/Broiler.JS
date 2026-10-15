// REFERENCE ARM J: a test-only copy of Broiler.JS's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-6-001 (docs/evidence/sjl-6-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.JS at bc3597f (the commit the area verdict's correspondence reads):
//   Broiler.JS/Broiler.JavaScript.Runtime/DateParser.cs
//     blob 3fb67b6b96ba0a6eb29dfd5f6c4711352581c81e: the whole class DateParser (:7-222): GmtOffsetWithoutColon
//     and TrailingZoneName (:14-17), NormalizeToStringZone (:19-27), ExtendedYearPrefix (:34),
//     NormalizeExtendedYear (:36-47), DefaultFormats (:49-56), StrictIsoTUtcFormats (:69-73),
//     StrictIsoTLocalFormats (:77-84), IsoTSeparator (:88), NumericSlashDate (:93), TryParseNumericSlashDate
//     (:100-139), SecondaryFormatsUTC (:141-145), SecondaryFormats (:147-178) and Parse (:180-221)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Date/JSDateStatic.cs
//     blob 9ffcbb962518ee7e6e1b27fc648c58983e0cbdfd: JSDateStatic.ToJSDate (:26-32), the body of Date.parse
//     (:90-104), EcmaIsoDateTime (:113-115), TryParseExtendedIso (:123-201) and DaysInMonth (:205-220)
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Date/JSDate.cs
//     blob be56a30aac91b881b79f5ba713cba620449a3b50: InvalidDate (:16), which ToJSDate reads
//
// Changes from the source, none of which changes a result:
//   - the sources' line endings are LF already; their byte-order marks dropped;
//   - the namespace; DateParser and JSDateStatic stay static classes, and the JSDate members sit in the static
//     class JSDate (a JSObject partial class in the source); private and public members are internal;
//   - the [JSExport...] attribute of Date.parse is not copied;
//   - DateParser.Parse takes a CultureInfo culture, read where the source reads CultureInfo.CurrentCulture (the
//     fourth of its five parses, :213); every other parse names CultureInfo.InvariantCulture and is unchanged.
//     DateTimeStyles.AssumeLocal (:199, :204, :210, :213, :216) and TryParseNumericSlashDate's
//     DateTimeKind.Local (:132) read the process's zone implicitly and are kept as they are: the comparison
//     runs with the process's zone set to UTC, and passes that zone to Parse below;
//   - TryParseExtendedIso takes a TimeZoneInfo zone, before its out parameter, which it passes to the copied
//     JSDateMath.UTC (whose source reads TimeZoneInfo.Local) for a date-time without an offset (:195);
//   - Parse is the body of Date.parse (:90-104) as a function of the argument's String: the source's first line,
//     `var text = a.Get1().ToString()`, is the engine's conversion and the parameter `text` holds its result;
//     the result is the time value the source wraps in a JSNumber, NaN when both parsers fail. It takes the
//     culture DateParser.Parse reads and the zone TryParseExtendedIso reads. Its conversion of the
//     DateTimeOffset, DateParser.Parse(text).ToJSDate(), is the copied ToJSDate, verbatim: InvalidDate
//     (DateTimeOffset.MinValue, the parser's failure) is NaN, and any other value is
//     ToUniversalTime().ToUnixTimeMilliseconds(), with no TimeClip.

using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Date.J;

internal static class DateParser
{
    // Date.prototype.toString() / toTimeString() emit the implementation-defined shape
    // "… GMT+0000 (Coordinated Universal Time)". .NET's offset specifier (K / zzz) needs a
    // colon ("+00:00") and does not understand the trailing parenthesised zone name, so the
    // engine's own toString output did not round-trip through Date.parse. Normalise that
    // shape — insert the colon and drop the zone-name parenthetical — before parsing.
    internal static readonly Regex GmtOffsetWithoutColon =
        new(@"GMT([+-]\d{2})(\d{2})", RegexOptions.Compiled);
    internal static readonly Regex TrailingZoneName =
        new(@"\s*\([^)]*\)\s*$", RegexOptions.Compiled);

    internal static string NormalizeToStringZone(string text)
    {
        if (text.IndexOf("GMT", StringComparison.Ordinal) < 0)
            return text;

        text = TrailingZoneName.Replace(text, string.Empty);
        text = GmtOffsetWithoutColon.Replace(text, "GMT$1:$2");
        return text;
    }

    // ES extended year: a date may begin with a signed six-digit year (±YYYYYY). The
    // underlying .NET list parser does not accept the leading sign or six-digit field, so
    // collapse a positive extended year that fits in four digits (e.g. "+001997-3-8" ->
    // "1997-3-8") before parsing (test262 sm/Date/non-iso). Negative or >4-digit extended
    // years are left untouched (out of range for the list parser, and not exercised here).
    internal static readonly Regex ExtendedYearPrefix = new(@"^([+-])(\d{6})(?=[-T])", RegexOptions.Compiled);

    internal static string NormalizeExtendedYear(string text)
    {
        var m = ExtendedYearPrefix.Match(text);
        if (!m.Success)
            return text;

        var year = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
        if (m.Groups[1].Value == "+" && year is >= 1 and <= 9999)
            return year.ToString("D4", CultureInfo.InvariantCulture) + text.Substring(m.Length);

        return text;
    }

    internal static readonly string[] DefaultFormats = [
        "yyyy-MM-ddTHH:mm:ss.FFF",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm",
        "yyyy-MM-dd",
        "yyyy-MM",
        "yyyy"
    ];

    // The ES Date Time String Format proper: a 'T' date/time separator requires
    // zero-padded fields and a Z or ±HH:mm offset (no bare ±HH). A relaxed,
    // single-digit / bare-offset form is accepted only with a SPACE separator
    // (SpiderMonkey extension). So any string whose date and time are joined by 'T'
    // must match these strict patterns exactly — it must NOT fall through to the
    // relaxed format lists or the general parser (test262 sm/Date/non-iso:
    // "1997-3-8T11:19:20" and "1997-03-08T11:19:10-07" are NaN, while the
    // space-separated equivalents parse).
    // Z-suffixed strict ISO is UTC: parsed with AssumeUniversal so the wall clock is read as
    // UTC (a 'Z' literal under AssumeLocal would wrongly apply the local offset — Date.parse of
    // a toISOString() round-trip must be exact).
    internal static readonly string[] StrictIsoTUtcFormats = [
        "yyyy-MM-ddTHH:mm:ss.FFF'Z'",
        "yyyy-MM-ddTHH:mm:ss'Z'",
        "yyyy-MM-ddTHH:mm'Z'",
    ];

    // Strict ISO with an explicit ±HH:mm offset (zzz rejects a bare ±HH) or with no offset
    // (a no-offset date-time is local time).
    internal static readonly string[] StrictIsoTLocalFormats = [
        "yyyy-MM-ddTHH:mm:ss.FFFzzz",
        "yyyy-MM-ddTHH:mm:sszzz",
        "yyyy-MM-ddTHH:mmzzz",
        "yyyy-MM-ddTHH:mm:ss.FFF",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm",
    ];

    // The ISO 'T' date/time separator: a 'T' immediately preceded by a digit (the day).
    // This does not match the 'T' in "GMT"/"UTC" zone designators (preceded by a letter).
    internal static readonly Regex IsoTSeparator = new(@"\dT", RegexOptions.Compiled);

    // A bare numeric, slash-separated date "N/N/N" (no time part). SpiderMonkey applies a
    // two-digit-year / field-order heuristic that .NET's parser does not replicate, so this
    // form is interpreted here (test262 sm/Date/two-digit-years).
    internal static readonly Regex NumericSlashDate = new(@"^\s*(\d{1,6})/(\d{1,2})/(\d{1,4})\s*$", RegexOptions.Compiled);

    // Disambiguate "N/N/N": a leading field that cannot be a month or day (> 31) is the year
    // (yy/mm/dd); otherwise the US month-first order applies (mm/dd/yy). A 1- or 2-digit year
    // maps 50..99 -> 1950..1999 and 00..49 -> 2000..2049. Returns false (the caller yields an
    // invalid Date) for an out-of-range field — and the caller treats ANY N/N/N string this
    // way, so it never falls through to a more lenient parse.
    internal static bool TryParseNumericSlashDate(string text, out DateTimeOffset result)
    {
        result = DateTimeOffset.MinValue;

        var m = NumericSlashDate.Match(text);
        if (!m.Success)
            return false;

        int n1 = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        int n2 = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
        int n3 = int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);

        int year, month, day;
        if (n1 > 31)
        {
            year = n1; month = n2; day = n3;   // yy/mm/dd (year first)
        }
        else
        {
            month = n1; day = n2; year = n3;   // mm/dd/yy (US, month first)
        }

        if (year < 100)
            year = year >= 50 ? year + 1900 : year + 2000;

        if (month is < 1 or > 12 || day is < 1 or > 31 || year is < 1 or > 9999)
            return false;

        try
        {
            // Bare numeric dates are interpreted in local time, like the Date(y, m, d) form
            // (not the UTC ISO date-only form).
            result = new DateTimeOffset(new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Local));
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    internal static readonly string[] SecondaryFormatsUTC = [
        "d MMMM yyyy HH:mm \\U\\T\\CK",
        "MMMM dd, yyyy, HH:mm:ss \\U\\T\\CK",
        "MMMM dd, yyyy HH:mm:ss \\U\\T\\CK",
    ];

    internal static readonly string[] SecondaryFormats = [
        // Formats used in DatePrototype toString methods
        "ddd MMM dd yyyy HH:mm:ss 'GMT'K",
        "ddd MMM dd yyyy",
        // ES Date Format
        "MMMM dd, yyyy HH:mm:ss \\G\\M\\TK",
        "MMMM dd, yyyy, HH:mm:ss \\G\\M\\TK",
        "d MMMM yyyy HH:mm:ss \\G\\M\\TK",
        "HH:mm:ss 'GMT'K",

        // standard formats
         "yyyy-M-dTH:m:s.FFFK",
        "yyyy/M/dTH:m:s.FFFK",
        "yyyy-M-dTH:m:sK", 
         "yyyy/M/dTH:m:sK",
         "yyyy-M-d H:m:s.FFFK",
         "yyyy/M/d H:m:s.FFFK",
         "yyyy-M-d H:m:sK",
        "yyyy/M/d H:m:sK",
        "yyyy-M-d H:mK",
        "yyyy/M/d H:mK",
        "yyyy-M-dK",
        "yyyy/M/dK",
        "yyyy-MK",
        "yyyy/MK",
        "yyyyK",
        "THH:mm:ss.FFFK",
        "THH:mm:ssK",
        "THHK",
        "yyyyTH:m"

    ];

    internal static DateTimeOffset Parse(string text, CultureInfo culture)
    {
        text = NormalizeToStringZone(text);
        text = NormalizeExtendedYear(text);

        // A bare numeric "N/N/N" date is resolved by the SpiderMonkey field-order / two-digit
        // year heuristic, and any such string is handled exclusively here (an out-of-range
        // field is an invalid Date, never re-parsed leniently — test262 sm/Date/two-digit-years).
        if (NumericSlashDate.IsMatch(text))
            return TryParseNumericSlashDate(text, out var slash) ? slash : DateTimeOffset.MinValue;

        // A 'T'-joined date/time is the strict ES Date Time String Format: it must match the
        // zero-padded patterns exactly, and an unrecognised 'T' string is NaN rather than being
        // re-parsed leniently (test262 sm/Date/non-iso). The relaxed single-digit / bare-offset
        // forms below apply only to the space-separated and other non-'T' shapes.
        if (IsoTSeparator.IsMatch(text))
        {
            if (DateTimeOffset.TryParseExact(text, StrictIsoTUtcFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var iso))
                return iso;
            if (DateTimeOffset.TryParseExact(text, StrictIsoTLocalFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out iso))
                return iso;
            return DateTimeOffset.MinValue;
        }

        if (DateTimeOffset.TryParseExact(text, DefaultFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var result))
            return result;

        if (DateTimeOffset.TryParseExact(text, SecondaryFormatsUTC, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out result))
            return result;

        if (DateTimeOffset.TryParseExact(text, SecondaryFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out result))
            return result;

        if (DateTimeOffset.TryParse(text, culture, DateTimeStyles.AssumeLocal, out result))
            return result;

        if (!DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out result))
            // unrecognized dates should return NaN (15.9.4.2)
            return DateTimeOffset.MinValue;

        return result;
    }
}

internal static class JSDateStatic
{
    internal static double ToJSDate(this DateTimeOffset dateTime)
    {
        if (dateTime == JSDate.InvalidDate)
            return double.NaN;

        return dateTime.ToUniversalTime().ToUnixTimeMilliseconds();
    }
}

/// <summary>Broiler.JS's Date members, as copied: here, Date.parse and its extended ISO parser.</summary>
internal static partial class JSDate
{
    internal static readonly DateTimeOffset InvalidDate = DateTimeOffset.MinValue;

    // Date.parse (JSDateStatic.cs:90-104) as a function of the argument's String.
    internal static double Parse(string text, CultureInfo culture, TimeZoneInfo zone)
    {
        var val = DateParser.Parse(text, culture).ToJSDate();

        // The .NET-backed parser can't represent ISO-8601 expanded years (e.g.
        // "+275760-09-13T..." / "-000001-...") or years outside 1–9999. Fall back to
        // the extended ISO parser so toISOString output round-trips across the full range.
        if (double.IsNaN(val) && TryParseExtendedIso(text, zone, out var extended))
            val = extended;

        return val;
    }

    internal static readonly System.Text.RegularExpressions.Regex EcmaIsoDateTime = new(
        @"^([+-]\d{6}|\d{4})(?:-(\d{2})(?:-(\d{2}))?)?(?:T(\d{2}):(\d{2})(?::(\d{2})(?:\.(\d{1,9}))?)?(Z|[+-]\d{2}:\d{2})?)?$",
        RegexOptions.CultureInvariant);

    internal static bool TryParseExtendedIso(string text, TimeZoneInfo zone, out double ms)
    {
        ms = double.NaN;
        if (string.IsNullOrEmpty(text))
            return false;

        var match = EcmaIsoDateTime.Match(text);
        if (!match.Success)
            return false;

        var yearText = match.Groups[1].Value;
        if (!long.TryParse(yearText, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long year))
            return false;

        // An ISO-8601 expanded year carries a mandatory sign; the year 0 is written "+000000".
        // "-000000" (negative zero) is explicitly invalid (Date.parse → NaN).
        if (year == 0 && yearText[0] == '-')
            return false;

        int month = match.Groups[2].Success ? int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) : 1;
        int day = match.Groups[3].Success ? int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture) : 1;

        var hasTime = match.Groups[4].Success;
        int hour = hasTime ? int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture) : 0;
        int minute = match.Groups[5].Success ? int.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture) : 0;
        int second = match.Groups[6].Success ? int.Parse(match.Groups[6].Value, CultureInfo.InvariantCulture) : 0;

        int millisecond = 0;
        if (match.Groups[7].Success)
        {
            // Fractional seconds: interpret the digits as a fraction of a second and keep
            // millisecond precision (".5" → 500 ms, ".1234" → 123 ms).
            var frac = match.Groups[7].Value;
            var msText = frac.Length >= 3 ? frac.Substring(0, 3) : frac.PadRight(3, '0');
            millisecond = int.Parse(msText, CultureInfo.InvariantCulture);
        }

        // ECMAScript rejects out-of-range components. Hour 24 is permitted only when the
        // remainder of the time-of-day is zero.
        if (month is < 1 or > 12)
            return false;
        if (day < 1 || day > DaysInMonth(year, month))
            return false;
        if (minute is < 0 or > 59 || second is < 0 or > 59)
            return false;
        if (hour > 24 || (hour == 24 && (minute != 0 || second != 0 || millisecond != 0)))
            return false;

        bool hasOffset = match.Groups[8].Success;
        double offsetMs = 0;
        if (hasOffset && match.Groups[8].Value != "Z")
        {
            var tz = match.Groups[8].Value;
            int offSign = tz[0] == '-' ? -1 : 1;
            int offHours = int.Parse(tz.Substring(1, 2), CultureInfo.InvariantCulture);
            int offMinutes = int.Parse(tz.Substring(4, 2), CultureInfo.InvariantCulture);
            if (offHours > 23 || offMinutes > 59)
                return false;
            offsetMs = offSign * (offHours * 60 + offMinutes) * 60000.0;
        }

        double wallClock = JSDateMath.MakeDate(
            JSDateMath.MakeDay(year, month - 1, day),
            JSDateMath.MakeTime(hour, minute, second, millisecond));

        // Per spec: a date-only form is interpreted as UTC; a date-time form with an explicit
        // offset (including "Z") denotes a fixed instant; a date-time without an offset is
        // interpreted as local wall-clock time.
        double utcMs;
        if (hasOffset)
            utcMs = wallClock - offsetMs;
        else if (hasTime)
            utcMs = JSDateMath.UTC(wallClock, zone);
        else
            utcMs = wallClock;

        ms = JSDateMath.TimeClip(utcMs);
        return !double.IsNaN(ms);
    }

    internal static int DaysInMonth(long year, int month)
    {
        switch (month)
        {
            case 2:
                var leap = (year % 4 == 0 && year % 100 != 0) || year % 400 == 0;
                return leap ? 29 : 28;
            case 4:
            case 6:
            case 9:
            case 11:
                return 30;
            default:
                return 31;
        }
    }
}
