using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Broiler.JavaScript.BuiltIns.Date;

namespace Broiler.JavaScript.BuiltIns.Intl;

// A focused Intl.DateTimeFormat formatting engine covering the en / en-US locale:
// component- and preset-based pattern resolution, field formatting, formatToParts,
// and the interval (formatRange / formatRangeToParts) algorithm
// (PartitionDateTimeRangePattern). Locale data is embedded for `en`; broader locale
// coverage would source patterns from the CLDR data pipeline.
internal static class JSIntlDateTimeFormatEngine
{
    // ── Resolved per-format calendar fields, in a specific time zone ──
    // Computed with ECMAScript date math from a wall-clock time value (ms) so the full
    // valid date range (±8.64e15 ms / ±275760 years) is supported — DateTime/DateTimeOffset
    // are limited to years 1..9999.
    internal readonly struct Fields
    {
        public readonly int Year, Month, Day, Hour, Minute, Second, Millisecond;
        public Fields(double wallClockMs)
        {
            Year = (int)JSDateMath.YearFromTime(wallClockMs);
            Month = JSDateMath.MonthFromTime(wallClockMs) + 1; // 0-indexed -> 1-indexed
            Day = JSDateMath.DateFromTime(wallClockMs);
            Hour = JSDateMath.HourFromTime(wallClockMs);
            Minute = JSDateMath.MinFromTime(wallClockMs);
            Second = JSDateMath.SecFromTime(wallClockMs);
            Millisecond = JSDateMath.MsFromTime(wallClockMs);
        }
    }

    internal readonly struct Token
    {
        public readonly bool IsField;
        public readonly char Field;
        public readonly int Count;
        public readonly string Literal;
        public Token(char field, int count) { IsField = true; Field = field; Count = count; Literal = null; }
        public Token(string literal) { IsField = false; Field = '\0'; Count = 0; Literal = literal; }
    }

    internal readonly struct Part
    {
        public readonly string Type;
        public readonly string Value;
        public readonly string Source;
        public Part(string type, string value, string source) { Type = type; Value = value; Source = source; }
    }

    internal sealed class Pattern
    {
        public List<Token> Tokens;
        // The CLDR skeleton key (e.g. "yMMMd"), used to find interval patterns.
        public string Skeleton;
    }

    // ── en locale data ──
    private static readonly string[] MonthShort =
        { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
    private static readonly string[] MonthWide =
        { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
    private static readonly string[] MonthNarrow =
        { "J", "F", "M", "A", "M", "J", "J", "A", "S", "O", "N", "D" };

    // CLDR en intervalFormats fallback: "{0} – {1}" (U+2013 with surrounding spaces).
    internal const string RangeSeparator = " – ";

    // Specific en interval patterns by skeleton+greatestDifference. Any (skeleton, field)
    // not listed falls back to the "{0} – {1}" pattern (format both fully, join).
    private static readonly Dictionary<string, string> IntervalPatterns = new(StringComparer.Ordinal)
    {
        ["yMMMd|M"] = "MMM d – MMM d, y",
        ["yMMMd|d"] = "MMM d – d, y",
        ["yMMMd|y"] = "MMM d, y – MMM d, y",
        ["yMMMd|h"] = "MMM d, y", // date-only skeleton: time diff is irrelevant
    };

    // Significance order for greatest-difference detection (higher index = lower
    // significance). The first (most significant) differing field type wins.
    private static readonly string[] FieldSignificance =
        { "era", "year", "month", "day", "dayPeriod", "hour", "minute", "second", "fractionalSecond" };

    // ── Pattern resolution ──
    internal static Pattern ResolvePattern(
        string localeTag,
        bool hasYear, string yearStyle, bool hasMonth, string monthStyle, bool hasDay, string dayStyle,
        bool hasHour, bool hasMinute, bool hasSecond, int fractionalSecondDigits, bool hasDayPeriodField,
        string dateStyle, string timeStyle, bool hour12)
    {
        string datePattern = null;
        string timePattern = null;

        if (dateStyle != null)
        {
            datePattern = dateStyle switch
            {
                "full" => "EEEE, MMMM d, y",
                "long" => "MMMM d, y",
                "medium" => "MMM d, y",
                _ => "M/d/yy", // short
            };
        }

        if (timeStyle != null)
        {
            timePattern = timeStyle switch
            {
                "full" or "long" => "h:mm:ss a",
                "medium" => "h:mm:ss a",
                _ => "h:mm a", // short
            };
        }

        if (datePattern == null && timePattern == null)
        {
            // Build from component options, respecting each field's width.
            var monthTok = monthStyle switch { "long" => "MMMM", "short" => "MMM", "narrow" => "MMMMM", "2-digit" => "MM", _ => "M" };
            var dayTok = dayStyle == "2-digit" ? "dd" : "d";
            var yearTok = yearStyle == "2-digit" ? "yy" : "y";
            var alphaMonth = monthStyle is "long" or "short" or "narrow";

            var date = new StringBuilder();
            if (alphaMonth)
            {
                // Spelled-out month: keep the en layout (locale word order for named
                // months is not modelled here).
                if (hasMonth && hasDay && hasYear)
                    date.Append($"{monthTok} {dayTok}, {yearTok}");
                else if (hasMonth && hasDay)
                    date.Append($"{monthTok} {dayTok}");
                else if (hasYear && hasMonth)
                    date.Append($"{monthTok} {yearTok}");
                else if (hasYear) date.Append(yearTok);
                else if (hasMonth) date.Append(monthTok);
                else if (hasDay) date.Append(dayTok);
            }
            else
            {
                // Numeric date: order the present fields per the locale's short-date
                // convention (e.g. de → d.M.y, ja → y/M/d) instead of always M/d/y.
                var (order, sep) = NumericDateLayout(localeTag);
                var first = true;
                foreach (var field in order)
                {
                    string tok = field switch
                    {
                        'd' => hasDay ? dayTok : null,
                        'M' => hasMonth ? monthTok : null,
                        'y' => hasYear ? yearTok : null,
                        _ => null,
                    };
                    if (tok == null)
                        continue;
                    if (!first)
                        date.Append(sep);
                    date.Append(tok);
                    first = false;
                }
            }

            var time = new StringBuilder();
            if (hasHour && hasMinute && hasSecond)
                time.Append(hour12 ? "h:mm:ss a" : "HH:mm:ss");
            else if (hasHour && hasMinute)
                time.Append(hour12 ? "h:mm a" : "HH:mm");
            else if (hasMinute && hasSecond)
                time.Append("mm:ss");
            else if (hasHour)
                time.Append(hour12 ? "h a" : "HH");
            else if (hasMinute)
                time.Append("mm");
            else if (hasSecond)
                time.Append("ss");

            if (fractionalSecondDigits >= 1 && fractionalSecondDigits <= 3 && (hasSecond))
                time.Append('.').Append(new string('S', fractionalSecondDigits));

            datePattern = date.Length > 0 ? date.ToString() : null;
            timePattern = time.Length > 0 ? time.ToString() : null;
        }

        string combined = (datePattern, timePattern) switch
        {
            // No component or style options: ECMA-402 defaults to numeric
            // year/month/day, laid out in the locale's short-date order.
            (null, null) => DefaultNumericDate(localeTag),
            (not null, null) => datePattern,
            (null, not null) => timePattern,
            _ => datePattern + ", " + timePattern,
        };

        var tokens = Parse(combined);
        return new Pattern { Tokens = tokens, Skeleton = SkeletonOf(tokens) };
    }

    // Derives the numeric-date field order (a permutation of 'd','M','y') and the
    // separator between fields from the locale's short-date pattern. Falls back to
    // the en convention (M/d/y) for an unknown/complex pattern, so en and locales
    // whose pattern can't be parsed are unchanged.
    // The default all-numeric date pattern (year/month/day) for a locale, e.g.
    // "M/d/y" for en, "d.M.y" for de, "y/M/d" for ja.
    private static string DefaultNumericDate(string localeTag)
    {
        var (order, sep) = NumericDateLayout(localeTag);
        var sb = new StringBuilder();
        for (var i = 0; i < order.Length; i++)
        {
            if (i > 0)
                sb.Append(sep);
            sb.Append(order[i]);
        }
        return sb.ToString();
    }

    private static (string Order, string Separator) NumericDateLayout(string localeTag)
    {
        var fallback = ("Mdy", "/");
        if (string.IsNullOrEmpty(localeTag))
            return fallback;

        var tag = localeTag;
        var uPos = tag.IndexOf("-u-", StringComparison.OrdinalIgnoreCase);
        if (uPos >= 0)
            tag = tag[..uPos];

        CultureInfo culture;
        try
        {
            culture = CultureInfo.GetCultureInfo(tag);
        }
        catch (CultureNotFoundException)
        {
            return fallback;
        }

        if (culture.IsNeutralCulture && tag.Equals("en", StringComparison.OrdinalIgnoreCase))
            return fallback;

        var shortPattern = culture.DateTimeFormat.ShortDatePattern;
        if (string.IsNullOrEmpty(shortPattern))
            return fallback;

        var order = new StringBuilder(3);
        string separator = null;
        var inQuote = false;
        for (var i = 0; i < shortPattern.Length; i++)
        {
            var c = shortPattern[i];
            if (c == '\'')
            {
                inQuote = !inQuote;
                continue;
            }
            if (inQuote)
                continue;

            var field = c is 'd' ? 'd' : c is 'M' ? 'M' : c is 'y' ? 'y' : '\0';
            if (field != '\0')
            {
                if (order.Length == 0 || order[^1] != field)
                {
                    if (order.ToString().IndexOf(field) >= 0)
                        return fallback; // repeated field group → too complex, keep en
                    order.Append(field);
                }
            }
            else if (order.Length == 1 && separator == null && !char.IsWhiteSpace(c))
            {
                // First separator run, immediately after the first field.
                separator = c.ToString();
            }
        }

        // Only use a clean d/M/y layout with a single-character separator.
        if (order.Length != 3 || separator == null)
            return fallback;

        return (order.ToString(), separator);
    }

    private static List<Token> Parse(string pattern)
    {
        var tokens = new List<Token>();
        var i = 0;
        var literal = new StringBuilder();
        void FlushLiteral()
        {
            if (literal.Length > 0) { tokens.Add(new Token(literal.ToString())); literal.Clear(); }
        }
        while (i < pattern.Length)
        {
            var c = pattern[i];
            if (c == '\'')
            {
                // Quoted literal text.
                i++;
                if (i < pattern.Length && pattern[i] == '\'') { literal.Append('\''); i++; continue; }
                while (i < pattern.Length && pattern[i] != '\'') { literal.Append(pattern[i]); i++; }
                i++;
                continue;
            }
            if (char.IsLetter(c))
            {
                FlushLiteral();
                var start = i;
                while (i < pattern.Length && pattern[i] == c) i++;
                tokens.Add(new Token(c, i - start));
                continue;
            }
            literal.Append(c);
            i++;
        }
        FlushLiteral();
        return tokens;
    }

    // A normalized skeleton key (field letters in canonical order) for interval lookup.
    private static string SkeletonOf(List<Token> tokens)
    {
        bool y = false, h = false, dayP = false;
        var monthCount = 0; bool d = false, hour = false, minute = false, second = false, frac = false;
        foreach (var t in tokens)
        {
            if (!t.IsField) continue;
            switch (t.Field)
            {
                case 'y': y = true; break;
                case 'M': case 'L': monthCount = Math.Max(monthCount, t.Count); break;
                case 'd': d = true; break;
                case 'h': case 'H': case 'k': case 'K': hour = true; break;
                case 'm': minute = true; break;
                case 's': second = true; break;
                case 'S': frac = true; break;
                case 'a': case 'b': dayP = true; break;
            }
        }
        var sb = new StringBuilder();
        if (y) sb.Append('y');
        if (monthCount >= 3) sb.Append("MMM"); else if (monthCount == 2) sb.Append("MM"); else if (monthCount == 1) sb.Append('M');
        if (d) sb.Append('d');
        if (hour) sb.Append('h');
        if (minute) sb.Append('m');
        if (second) sb.Append('s');
        if (frac) sb.Append('S');
        _ = h; _ = dayP;
        return sb.ToString();
    }

    // ── Time-zone resolution ──
    // Returns the wall-clock time value (ms) in the requested zone, computed by offset
    // arithmetic so the full ECMAScript date range is supported and offsets beyond ±14h
    // are permitted (DateTimeOffset rejects those).
    internal static double ToZone(double clippedMs, string timeZone)
    {
        if (string.IsNullOrEmpty(timeZone))
            return JSDateMath.LocalTime(clippedMs);

        if (string.Equals(timeZone, "UTC", StringComparison.OrdinalIgnoreCase))
            return clippedMs;

        if (TryParseOffset(timeZone, out var offset))
            return clippedMs + offset.TotalMilliseconds;

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            // Only valid within DateTime range; clamp lookups outside it to the offset at
            // the nearest representable instant.
            var clamped = Math.Clamp(clippedMs,
                DateTimeOffset.MinValue.ToUnixTimeMilliseconds(),
                DateTimeOffset.MaxValue.ToUnixTimeMilliseconds());
            var tzOffset = tz.GetUtcOffset(DateTimeOffset.FromUnixTimeMilliseconds((long)clamped));
            return clippedMs + tzOffset.TotalMilliseconds;
        }
        catch
        {
            return JSDateMath.LocalTime(clippedMs);
        }
    }

    // Parses an ECMAScript UTC-offset time-zone identifier such as "+0301", "+02",
    // "+13:49", "-0914" into a TimeSpan.
    private static bool TryParseOffset(string tz, out TimeSpan offset)
    {
        offset = default;
        if (tz.Length < 3 || (tz[0] != '+' && tz[0] != '-'))
            return false;

        var sign = tz[0] == '-' ? -1 : 1;
        var body = tz.Substring(1).Replace(":", string.Empty);
        if (body.Length != 2 && body.Length != 4)
            return false;
        if (!int.TryParse(body.Substring(0, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var hours))
            return false;
        var minutes = 0;
        if (body.Length == 4 && !int.TryParse(body.Substring(2, 2), NumberStyles.None, CultureInfo.InvariantCulture, out minutes))
            return false;
        if (hours > 23 || minutes > 59)
            return false;

        offset = new TimeSpan(sign * hours, sign * minutes, 0);
        return true;
    }

    // ── Field formatting ──
    private static (string type, string value) FormatField(in Token token, in Fields f, int fractionalSecondDigits)
    {
        switch (token.Field)
        {
            case 'y':
                var year = f.Year;
                return ("year", token.Count == 2
                    ? (year % 100).ToString("D2", CultureInfo.InvariantCulture)
                    : year.ToString(CultureInfo.InvariantCulture));
            case 'M':
            case 'L':
                return ("month", token.Count switch
                {
                    >= 4 when token.Count == 4 => MonthWide[f.Month - 1],
                    5 => MonthNarrow[f.Month - 1],
                    3 => MonthShort[f.Month - 1],
                    2 => f.Month.ToString("D2", CultureInfo.InvariantCulture),
                    _ => f.Month.ToString(CultureInfo.InvariantCulture),
                });
            case 'd':
                return ("day", token.Count == 2
                    ? f.Day.ToString("D2", CultureInfo.InvariantCulture)
                    : f.Day.ToString(CultureInfo.InvariantCulture));
            case 'h':
                var h12 = f.Hour % 12; if (h12 == 0) h12 = 12;
                return ("hour", token.Count == 2 ? h12.ToString("D2", CultureInfo.InvariantCulture) : h12.ToString(CultureInfo.InvariantCulture));
            case 'H':
                return ("hour", token.Count == 2 ? f.Hour.ToString("D2", CultureInfo.InvariantCulture) : f.Hour.ToString(CultureInfo.InvariantCulture));
            case 'K':
                var k = f.Hour % 12;
                return ("hour", token.Count == 2 ? k.ToString("D2", CultureInfo.InvariantCulture) : k.ToString(CultureInfo.InvariantCulture));
            case 'k':
                var kk = f.Hour == 0 ? 24 : f.Hour;
                return ("hour", token.Count == 2 ? kk.ToString("D2", CultureInfo.InvariantCulture) : kk.ToString(CultureInfo.InvariantCulture));
            case 'm':
                return ("minute", token.Count == 2 ? f.Minute.ToString("D2", CultureInfo.InvariantCulture) : f.Minute.ToString(CultureInfo.InvariantCulture));
            case 's':
                return ("second", token.Count == 2 ? f.Second.ToString("D2", CultureInfo.InvariantCulture) : f.Second.ToString(CultureInfo.InvariantCulture));
            case 'S':
                var digits = token.Count;
                var ms = f.Millisecond.ToString("D3", CultureInfo.InvariantCulture);
                return ("fractionalSecond", digits <= 3 ? ms.Substring(0, digits) : ms.PadRight(digits, '0'));
            case 'a':
            case 'b':
                return ("dayPeriod", f.Hour < 12 ? "AM" : "PM");
            default:
                return ("literal", new string(token.Field, token.Count));
        }
    }

    internal static List<Part> FormatToParts(Pattern pattern, in Fields f, int fractionalSecondDigits, string source)
    {
        var parts = new List<Part>(pattern.Tokens.Count);
        foreach (var token in pattern.Tokens)
        {
            if (!token.IsField)
            {
                parts.Add(new Part("literal", token.Literal, source));
                continue;
            }
            var (type, value) = FormatField(in token, in f, fractionalSecondDigits);
            parts.Add(new Part(type, value, source));
        }
        return parts;
    }

    internal static string PartsToString(List<Part> parts)
    {
        var sb = new StringBuilder();
        foreach (var p in parts) sb.Append(p.Value);
        return sb.ToString();
    }

    // ── Interval (range) formatting ──

    // Determines the most significant field type whose formatted value differs between
    // the two dates, restricted to field types present in the pattern. Returns null when
    // every displayed field is equal (the two endpoints render identically).
    private static string GreatestDifference(Pattern pattern, in Fields a, in Fields b, int fractionalSecondDigits)
    {
        var startParts = FormatToParts(pattern, in a, fractionalSecondDigits, "shared");
        var endParts = FormatToParts(pattern, in b, fractionalSecondDigits, "shared");
        foreach (var fieldType in FieldSignificance)
        {
            var sv = FindFieldValue(startParts, fieldType);
            if (sv == null) continue;
            var ev = FindFieldValue(endParts, fieldType);
            if (!string.Equals(sv, ev, StringComparison.Ordinal))
                return fieldType;
        }
        return null;
    }

    private static string FindFieldValue(List<Part> parts, string type)
    {
        foreach (var p in parts)
            if (p.Type == type) return p.Value;
        return null;
    }

    private static char FieldLetterFor(string type) => type switch
    {
        "year" => 'y',
        "month" => 'M',
        "day" => 'd',
        "hour" => 'h',
        "minute" => 'm',
        "second" => 's',
        "fractionalSecond" => 'S',
        "dayPeriod" => 'a',
        _ => '\0',
    };

    internal static List<Part> FormatRangeToParts(
        Pattern pattern, in Fields start, in Fields end, int fractionalSecondDigits)
    {
        var greatest = GreatestDifference(pattern, in start, in end, fractionalSecondDigits);
        if (greatest == null)
            return FormatToParts(pattern, in start, fractionalSecondDigits, "shared");

        var fieldLetter = FieldLetterFor(greatest);
        if (pattern.Skeleton != null
            && IntervalPatterns.TryGetValue($"{pattern.Skeleton}|{fieldLetter}", out var intervalPatternText))
        {
            var intervalTokens = Parse(intervalPatternText);
            return FormatIntervalPattern(intervalTokens, in start, in end, fractionalSecondDigits);
        }

        // Fallback "{0} – {1}".
        var result = new List<Part>();
        result.AddRange(FormatToParts(pattern, in start, fractionalSecondDigits, "startRange"));
        result.Add(new Part("literal", RangeSeparator, "shared"));
        result.AddRange(FormatToParts(pattern, in end, fractionalSecondDigits, "endRange"));
        return result;
    }

    // Formats a CLDR interval pattern (e.g. "MMM d – MMM d, y"): a field letter that
    // appears twice is per-range (first occurrence -> startRange/date0, second ->
    // endRange/date1); a letter appearing once is shared. A literal between two fields of
    // the same source takes that source, otherwise it is shared.
    private static List<Part> FormatIntervalPattern(
        List<Token> tokens, in Fields start, in Fields end, int fractionalSecondDigits)
    {
        // Count field-letter occurrences.
        var counts = new Dictionary<char, int>();
        foreach (var t in tokens)
            if (t.IsField)
                counts[t.Field] = counts.TryGetValue(t.Field, out var c) ? c + 1 : 1;

        // First pass: assign a source to each field token.
        var seen = new Dictionary<char, int>();
        var sources = new string[tokens.Count];
        for (var i = 0; i < tokens.Count; i++)
        {
            if (!tokens[i].IsField) continue;
            var letter = tokens[i].Field;
            if (counts[letter] == 1) { sources[i] = "shared"; continue; }
            var occ = seen.TryGetValue(letter, out var s) ? s : 0;
            sources[i] = occ == 0 ? "startRange" : "endRange";
            seen[letter] = occ + 1;
        }

        // Second pass: literals inherit a source from their field neighbours.
        for (var i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].IsField) continue;
            var prev = PrevFieldSource(sources, tokens, i);
            var next = NextFieldSource(sources, tokens, i);
            sources[i] = (prev, next) switch
            {
                (null, null) => "shared",
                (null, var n) => n,
                (var p, null) => p,
                var (p, n) => p == n ? p : "shared",
            };
        }

        var parts = new List<Part>(tokens.Count);
        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            var source = sources[i];
            if (!token.IsField)
            {
                parts.Add(new Part("literal", token.Literal, source));
                continue;
            }
            ref readonly var fields = ref start;
            var (type, value) = source == "endRange"
                ? FormatField(in token, in end, fractionalSecondDigits)
                : FormatField(in token, in fields, fractionalSecondDigits);
            parts.Add(new Part(type, value, source));
        }
        return parts;
    }

    private static string PrevFieldSource(string[] sources, List<Token> tokens, int i)
    {
        for (var j = i - 1; j >= 0; j--)
            if (tokens[j].IsField) return sources[j];
        return null;
    }

    private static string NextFieldSource(string[] sources, List<Token> tokens, int i)
    {
        for (var j = i + 1; j < tokens.Count; j++)
            if (tokens[j].IsField) return sources[j];
        return null;
    }
}
