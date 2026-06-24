using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Broiler.JavaScript.Runtime;

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
    private static readonly string[] StrictIsoTUtcFormats = [
        "yyyy-MM-ddTHH:mm:ss.FFF'Z'",
        "yyyy-MM-ddTHH:mm:ss'Z'",
        "yyyy-MM-ddTHH:mm'Z'",
    ];

    // Strict ISO with an explicit ±HH:mm offset (zzz rejects a bare ±HH) or with no offset
    // (a no-offset date-time is local time).
    private static readonly string[] StrictIsoTLocalFormats = [
        "yyyy-MM-ddTHH:mm:ss.FFFzzz",
        "yyyy-MM-ddTHH:mm:sszzz",
        "yyyy-MM-ddTHH:mmzzz",
        "yyyy-MM-ddTHH:mm:ss.FFF",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm",
    ];

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
    private static bool TryParseNumericSlashDate(string text, out DateTimeOffset result)
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

    internal static DateTimeOffset Parse(string text)
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

        if (DateTimeOffset.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out result))
            return result;

        if (!DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out result))
            // unrecognized dates should return NaN (15.9.4.2)
            return DateTimeOffset.MinValue;

        return result;
    }
}
