using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Broiler.ECMAScript.Time;

namespace Broiler.JavaScript.Runtime;

/// <summary>
/// This engine's own reading of the strings outside the Date Time String Format (decision SJL-D-17), which
/// <c>Date.parse</c> calls after the shared library's strict dialect and before Broiler.VM's (JSD-0072): the
/// numeric slash dates, the format's leniencies this engine keeps, its format lists and the culture's parses.
/// </summary>
public static class DateParser
{
    // Date.prototype.toString() / toTimeString() emit the implementation-defined shape
    // "… GMT+0000 (Coordinated Universal Time)". .NET's offset specifier (K / zzz) needs a
    // colon ("+00:00") and does not understand the trailing parenthesised zone name, so the
    // engine's own toString output did not round-trip through Date.parse. Normalise that
    // shape — insert the colon and drop the zone-name parenthetical — before parsing.
    private static readonly Regex GmtOffsetWithoutColon =
        new(@"GMT([+-]\d{2})(\d{2})", RegexOptions.Compiled);
    private static readonly Regex TrailingZoneName =
        new(@"\s*\([^)]*\)\s*$", RegexOptions.Compiled);

    private static string NormalizeToStringZone(string text)
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
    private static readonly Regex ExtendedYearPrefix = new(@"^([+-])(\d{6})(?=[-T])", RegexOptions.Compiled);

    private static string NormalizeExtendedYear(string text)
    {
        var m = ExtendedYearPrefix.Match(text);
        if (!m.Success)
            return text;

        var year = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
        if (m.Groups[1].Value == "+" && year is >= 1 and <= 9999)
            return year.ToString("D4", CultureInfo.InvariantCulture) + text.Substring(m.Length);

        return text;
    }

    // The format's leniencies this engine keeps (correspondence rows P4 and P6, decided at SJL-6.5): a
    // 'T'-joined date-time of the format whose fraction has other than three digits, none to nine, or whose
    // offset has no colon. It is read as the format with the fraction cut or padded to three digits and the
    // colon inserted, through the shared library's strict dialect.
    private static readonly Regex FormatLeniencies = new(
        @"^((?:[+-]\d{6}|\d{4})(?:-\d{2}(?:-\d{2})?)?)T(\d{2}:\d{2}(?::\d{2})?)(?:\.(\d{0,9}))?(Z|[+-]\d{2}:?\d{2})?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static EcmaParsedDate ReadFormatLeniencies(string text)
    {
        var m = FormatLeniencies.Match(text);
        if (!m.Success)
            return Invalid;

        var time = m.Groups[2].Value;
        var fraction = m.Groups[3];
        var offset = m.Groups[4].Value;

        // A fraction needs the seconds, as the format's does.
        if (fraction.Success && time.Length != 8)
            return Invalid;

        var normalized = m.Groups[1].Value + "T" + time;
        if (fraction.Success)
            normalized += "." + (fraction.Value + "000").Substring(0, 3);
        if (offset.Length == 5)
            offset = offset.Substring(0, 3) + ":" + offset.Substring(3);

        return EcmaDateParser.Parse(normalized + offset, DateParseDialect.Strict);
    }

    // The ISO 'T' date/time separator: a 'T' immediately preceded by a digit (the day).
    // This does not match the 'T' in "GMT"/"UTC" zone designators (preceded by a letter).
    private static readonly Regex IsoTSeparator = new(@"\dT", RegexOptions.Compiled);

    // A bare numeric, slash-separated date "N/N/N" (no time part). SpiderMonkey applies a
    // two-digit-year / field-order heuristic that .NET's parser does not replicate, so this
    // form is interpreted here (test262 sm/Date/two-digit-years).
    private static readonly Regex NumericSlashDate = new(@"^\s*(\d{1,6})/(\d{1,2})/(\d{1,4})\s*$", RegexOptions.Compiled);

    // Disambiguate "N/N/N": a leading field that cannot be a month or day (> 31) is the year
    // (yy/mm/dd); otherwise the US month-first order applies (mm/dd/yy). A 1- or 2-digit year
    // maps 50..99 -> 1950..1999 and 00..49 -> 2000..2049. Returns false (the caller yields an
    // invalid Date) for an out-of-range field — and the caller treats ANY N/N/N string this
    // way, so it never falls through to a more lenient parse.
    private static bool TryParseNumericSlashDate(string text, out DateTime result)
    {
        result = default;

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

        if (day > DateTime.DaysInMonth(year, month))
            return false;

        // Bare numeric dates are interpreted in local time, like the Date(y, m, d) form
        // (not the UTC ISO date-only form).
        result = new DateTime(year, month, day);
        return true;
    }

    internal static readonly string[] SecondaryFormatsUTC = [
        // The form of Date.prototype.toUTCString, read before the culture's parse, which reads it in
        // the culture's calendar (correspondence row P8)
        "ddd, dd MMM yyyy HH:mm:ss 'GMT'",
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

    private static readonly EcmaParsedDate Invalid = new(EcmaParsedKind.Invalid, double.NaN);

    // A .NET parse's reading. DateTimeOffset reads the string as this engine always has: a string with an
    // offset, or one the UTC list reads, is its instant; DateTime, which marks such a string UTC, tells it
    // from one without, whose wall clock is a local time the caller reads through UTC over the host's zone.
    private static EcmaParsedDate Reading(DateTimeOffset instant, bool marked, DateTime clock)
    {
        if (marked && clock.Kind == DateTimeKind.Unspecified)
        {
            var wallClock = new DateTimeOffset(instant.DateTime.Ticks, TimeSpan.Zero).ToUnixTimeMilliseconds();
            return new EcmaParsedDate(EcmaParsedKind.Local, wallClock);
        }

        return new EcmaParsedDate(EcmaParsedKind.Utc, EcmaTime.TimeClip(instant.ToUnixTimeMilliseconds()));
    }

    private static bool TryParseExact(string text, string[] formats, DateTimeStyles assume, out EcmaParsedDate parsed)
    {
        parsed = Invalid;
        if (!DateTimeOffset.TryParseExact(text, formats, CultureInfo.InvariantCulture, assume, out var instant))
            return false;

        var marked = DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, Marked(assume), out var clock);
        parsed = Reading(instant, marked, clock);
        return true;
    }

    private static bool TryParse(string text, CultureInfo culture, out EcmaParsedDate parsed)
    {
        parsed = Invalid;
        if (!DateTimeOffset.TryParse(text, culture, DateTimeStyles.AssumeLocal, out var instant))
            return false;

        var marked = DateTime.TryParse(text, culture, Marked(DateTimeStyles.AssumeLocal), out var clock);
        parsed = Reading(instant, marked, clock);
        return true;
    }

    // The styles under which DateTime marks a string with an offset, or one assumed UTC, as UTC, and leaves
    // the kind of one without unspecified.
    private static DateTimeStyles Marked(DateTimeStyles assume) =>
        DateTimeStyles.AdjustToUniversal | (assume & DateTimeStyles.AssumeUniversal);

    internal static EcmaParsedDate Parse(string text)
    {
        text = NormalizeToStringZone(text);
        text = NormalizeExtendedYear(text);

        // A bare numeric "N/N/N" date is resolved by the SpiderMonkey field-order / two-digit
        // year heuristic, and any such string is handled exclusively here (an out-of-range
        // field is an invalid Date, never re-parsed leniently — test262 sm/Date/two-digit-years).
        if (NumericSlashDate.IsMatch(text))
        {
            return TryParseNumericSlashDate(text, out var slash)
                ? new EcmaParsedDate(EcmaParsedKind.Local, new DateTimeOffset(slash.Ticks, TimeSpan.Zero).ToUnixTimeMilliseconds())
                : Invalid;
        }

        // A 'T'-joined date/time is the ES Date Time String Format's: this engine reads it with the
        // format's leniencies it keeps, and an unrecognised 'T' string is NaN rather than being
        // re-parsed leniently (test262 sm/Date/non-iso). The relaxed single-digit / bare-offset
        // forms below apply only to the space-separated and other non-'T' shapes.
        if (IsoTSeparator.IsMatch(text))
            return ReadFormatLeniencies(text);

        if (TryParseExact(text, SecondaryFormatsUTC, DateTimeStyles.AssumeUniversal, out var result))
            return result;

        if (TryParseExact(text, SecondaryFormats, DateTimeStyles.AssumeLocal, out result))
            return result;

        if (TryParse(text, CultureInfo.CurrentCulture, out result))
            return result;

        // unrecognized dates should return NaN (15.9.4.2)
        return TryParse(text, CultureInfo.InvariantCulture, out result) ? result : Invalid;
    }
}
