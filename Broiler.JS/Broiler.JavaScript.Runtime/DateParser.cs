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

    internal static readonly string[] DefaultFormats = [
        "yyyy-MM-ddTHH:mm:ss.FFF",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm",
        "yyyy-MM-dd",
        "yyyy-MM",
        "yyyy"
    ];

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
