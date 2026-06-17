using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Broiler.JavaScript.BuiltIns.Date;
using UnicodeCldr.LocaleData;

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
        // The pre-rendered time-zone display name (computed where the time zone + style are known),
        // emitted by the 'z' token. Null for a wall clock that carries no zone (plain types).
        public readonly string TimeZoneName;
        // The pre-rendered flexible day-period name (CLDR, e.g. "in the morning"), emitted by the 'B'
        // token for the ECMA-402 dayPeriod option. Null when no dayPeriod is requested.
        public readonly string DayPeriod;
        public Fields(double wallClockMs, string timeZoneName = null, string dayPeriod = null)
        {
            Year = (int)JSDateMath.YearFromTime(wallClockMs);
            Month = JSDateMath.MonthFromTime(wallClockMs) + 1; // 0-indexed -> 1-indexed
            Day = JSDateMath.DateFromTime(wallClockMs);
            Hour = JSDateMath.HourFromTime(wallClockMs);
            Minute = JSDateMath.MinFromTime(wallClockMs);
            Second = JSDateMath.SecFromTime(wallClockMs);
            Millisecond = JSDateMath.MsFromTime(wallClockMs);
            TimeZoneName = timeZoneName;
            DayPeriod = dayPeriod;
        }

        // Explicit wall-clock fields, used when formatting a Temporal plain type: its own clock is
        // formatted directly, with no time-zone conversion (the formatter's time zone is ignored).
        public Fields(int year, int month, int day, int hour, int minute, int second, int millisecond, string timeZoneName = null, string dayPeriod = null)
        {
            Year = year; Month = month; Day = day;
            Hour = hour; Minute = minute; Second = second; Millisecond = millisecond;
            TimeZoneName = timeZoneName;
            DayPeriod = dayPeriod;
        }
    }

    // The localized time-zone display name for the ECMA-402 timeZoneName option. UTC carries its CLDR
    // names; the long/short/generic styles use the bundled CLDR metazone names (English) when the zone
    // is covered; everything else (offset styles, uncovered zones/locales) uses the GMT offset.
    internal static string FormatTimeZoneName(string localeTag, string timeZone, string style, double epochMs)
    {
        if (!string.IsNullOrEmpty(timeZone) && timeZone.Equals("UTC", StringComparison.OrdinalIgnoreCase))
            return style switch
            {
                "long" or "longGeneric" => "Coordinated Universal Time",
                "longOffset" or "shortOffset" => "GMT",
                _ => "UTC",
            };

        if (style is not ("shortOffset" or "longOffset"))
        {
            var name = CldrLocaleData.GetTimeZoneName(localeTag, timeZone, style, IsDaylight(timeZone, epochMs));
            if (!string.IsNullOrEmpty(name))
                return name;
        }

        var offsetMs = ToZone(epochMs, timeZone) - epochMs;
        return GmtOffset(offsetMs, style is "longOffset" or "long" or "longGeneric");
    }

    // Whether daylight-saving time is in effect for a named IANA zone at the instant (false for an
    // offset/UTC identifier or an unknown zone), selecting the standard vs daylight metazone name.
    private static bool IsDaylight(string timeZone, double epochMs)
    {
        if (string.IsNullOrEmpty(timeZone))
            return false;
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            var clamped = Math.Clamp(epochMs,
                DateTimeOffset.MinValue.ToUnixTimeMilliseconds(), DateTimeOffset.MaxValue.ToUnixTimeMilliseconds());
            return tz.IsDaylightSavingTime(DateTimeOffset.FromUnixTimeMilliseconds((long)clamped));
        }
        catch { return false; }
    }

    // "GMT", "GMT-8", "GMT+5:30" (short) / "GMT-08:00", "GMT+05:30" (long); zero offset is "GMT".
    private static string GmtOffset(double offsetMs, bool longForm)
    {
        var minutes = (int)(Math.Round(offsetMs / 60000.0));
        if (minutes == 0) return "GMT";
        var sign = minutes < 0 ? "-" : "+";
        minutes = Math.Abs(minutes);
        var h = minutes / 60;
        var m = minutes % 60;
        if (longForm) return $"GMT{sign}{h:D2}:{m:D2}";
        return m == 0 ? $"GMT{sign}{h}" : $"GMT{sign}{h}:{m:D2}";
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
        // The resolved calendar (e.g. "buddhist"); null/"gregory" for the default.
        public string Calendar;
    }

    // Era-using calendars derived from the proleptic Gregorian year by a fixed
    // offset, with a single modern era. (Era-boundary calendars such as japanese
    // are not modelled.) Maps calendar -> (year offset, en era).
    private static readonly Dictionary<string, (int YearOffset, string Era)> EraCalendars =
        new(StringComparer.Ordinal)
        {
            ["buddhist"] = (543, "BE"),
        };

    // Calendars that display the year as a related (Gregorian) year plus a
    // sexagenary cycle year-name (e.g. chinese "2017(丁酉)"). The lunisolar month/day
    // arithmetic is not modelled; only the year presentation is.
    private static readonly HashSet<string> CyclicYearCalendars =
        new(StringComparer.Ordinal) { "chinese", "dangi" };

    private static readonly string[] SexagenaryStems =
        { "甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸" };
    private static readonly string[] SexagenaryBranches =
        { "子", "丑", "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥" };

    // The sexagenary (stem+branch) year name for a Gregorian year, e.g. 2017 → 丁酉.
    private static string SexagenaryYearName(int year)
    {
        var index = ((year - 4) % 60 + 60) % 60;
        return SexagenaryStems[index % 10] + SexagenaryBranches[index % 12];
    }

    // The islamic (Hijri) calendars share month/era names; the formatter converts the ISO date to the
    // calendar's own year/month/day (TemporalNonIso) and renders these names rather than the Gregorian
    // ones. The day/month arithmetic of the specific variant (civil / tbla / umalqura) is handled by
    // the calendar conversion; only the (English) display names live here.
    private static readonly HashSet<string> IslamicCalendars = new(StringComparer.Ordinal)
        { "islamic", "islamic-civil", "islamic-rgsa", "islamic-tbla", "islamic-umalqura" };

    private static bool NeedsCalendarConversion(string calendar) => calendar != null && IslamicCalendars.Contains(calendar);

    private static readonly string[] IslamicMonthWide =
        { "Muharram", "Safar", "Rabiʻ I", "Rabiʻ II", "Jumada I", "Jumada II", "Rajab", "Shaʻban", "Ramadan", "Shawwal", "Dhuʻl-Qiʻdah", "Dhuʻl-Hijjah" };
    private static readonly string[] IslamicMonthShort =
        { "Muh.", "Saf.", "Rab. I", "Rab. II", "Jum. I", "Jum. II", "Raj.", "Sha.", "Ram.", "Shaw.", "Dhuʻl-Q.", "Dhuʻl-H." };

    internal static bool IsSupportedCalendar(string calendar)
        => calendar != null && (EraCalendars.ContainsKey(calendar) || CyclicYearCalendars.Contains(calendar)
            || IslamicCalendars.Contains(calendar));

    // ── en locale data ──
    private static readonly string[] MonthShort =
        { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
    private static readonly string[] MonthWide =
        { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
    private static readonly string[] MonthNarrow =
        { "J", "F", "M", "A", "M", "J", "J", "A", "S", "O", "N", "D" };
    private static readonly string[] WeekdayWide =
        { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
    private static readonly string[] WeekdayShort =
        { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
    private static readonly string[] WeekdayNarrow =
        { "S", "M", "T", "W", "T", "F", "S" };

    // 0 = Sunday .. 6 = Saturday for a Gregorian/ISO date, valid across the full ±275760-year range.
    private static int DayOfWeek(in Fields f)
        => JSDateMath.WeekDay(JSDateMath.MakeDate(JSDateMath.MakeDay(f.Year, f.Month - 1, f.Day), 0));

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
        string dateStyle, string timeStyle, bool hour12, string calendar = null,
        bool hasWeekday = false, string weekdayStyle = null, bool hasTimeZoneName = false,
        string hourCycle = null, bool hasEra = false, string eraStyle = null)
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

            // The hour token follows the resolved hour cycle: h12 → "h" (1-12), h23 → "HH" (0-23),
            // h11 → "K" (0-11), h24 → "k" (1-24). The 12-hour cycles (h11/h12) also show a dayPeriod.
            var cycle = hourCycle ?? (hour12 ? "h12" : "h23");
            var (hourTok, showDayPeriod) = cycle switch
            {
                "h11" => ("K", true),
                "h24" => ("k", false),
                "h23" => ("HH", false),
                _ => ("h", true), // h12
            };

            // The dayPeriod option renders a flexible day-period name ('B') in place of AM/PM and
            // implies a 12-hour hour.
            string ap;
            if (hasDayPeriodField) { hourTok = "h"; ap = " B"; }
            else ap = showDayPeriod ? " a" : "";

            var time = new StringBuilder();
            if (hasHour && hasMinute && hasSecond)
                time.Append($"{hourTok}:mm:ss{ap}");
            else if (hasHour && hasMinute)
                time.Append($"{hourTok}:mm{ap}");
            else if (hasMinute && hasSecond)
                time.Append("mm:ss");
            else if (hasHour)
                time.Append($"{hourTok}{ap}");
            else if (hasDayPeriodField)
                time.Append("B"); // dayPeriod with no hour: the period name alone
            else if (hasMinute)
                time.Append("mm");
            else if (hasSecond)
                time.Append("ss");

            if (fractionalSecondDigits >= 1 && fractionalSecondDigits <= 3)
            {
                // A fractional-seconds field without a seconds field still renders (the digits alone),
                // so a lone fractionalSecondDigits option does not collapse to the default date.
                if (hasSecond) time.Append('.');
                time.Append(new string('S', fractionalSecondDigits));
            }

            datePattern = date.Length > 0 ? date.ToString() : null;
            timePattern = time.Length > 0 ? time.ToString() : null;

            // A weekday is prefixed to the date (en CLDR layout): "EEEE, <date>", or stands alone.
            if (hasWeekday)
            {
                var weekdayTok = weekdayStyle switch { "short" => "EEE", "narrow" => "EEEEE", _ => "EEEE" };
                datePattern = datePattern != null ? weekdayTok + ", " + datePattern : weekdayTok;
            }
        }

        // No component or style options: ECMA-402 defaults to numeric
        // year/month/day, laid out in the locale's short-date order.
        if (datePattern == null && timePattern == null)
            datePattern = DefaultNumericDate(localeTag);

        if (datePattern != null && datePattern.IndexOf('y') >= 0)
        {
            // An era-using calendar (e.g. buddhist) appends the era after a date that
            // shows a year, e.g. "M/d/y" -> "M/d/y G". The explicit `era` option does the
            // same for any calendar (including gregorian), with the requested width:
            // narrow -> "GGGGG", long -> "GGGG", short -> "G". A cyclic-year calendar
            // (chinese, dangi) shows the year as the related (Gregorian) year plus the cyclic
            // year name. CLDR's Chinese-language pattern is "rU年" (e.g. "2019己亥年"); the root
            // form used elsewhere wraps the name in parentheses, "r(U)". Each makes the pattern
            // structurally distinct.
            var eraCalendar = EraCalendars.ContainsKey(calendar ?? string.Empty) || IslamicCalendars.Contains(calendar ?? string.Empty);
            if (eraCalendar || hasEra)
                datePattern += eraStyle switch { "narrow" => " GGGGG", "long" => " GGGG", _ => " G" };
            else if (CyclicYearCalendars.Contains(calendar ?? string.Empty))
                datePattern = ReplaceYearField(datePattern,
                    CldrLocaleData.LanguageOf(localeTag) == "zh" ? "rU年" : "r(U)");
        }

        string combined = (datePattern, timePattern) switch
        {
            (null, null) => DefaultNumericDate(localeTag),
            (not null, null) => datePattern,
            (null, not null) => timePattern,
            _ => datePattern + ", " + timePattern,
        };

        // A time-zone name follows the date/time, separated by a space ("… z"). It is added for the
        // explicit timeZoneName option and for the long/full time styles (which include the zone).
        if (hasTimeZoneName || timeStyle is "long" or "full")
            combined = combined.Length > 0 ? combined + " z" : "z";

        var tokens = Parse(combined);
        return new Pattern { Tokens = tokens, Skeleton = SkeletonOf(tokens), Calendar = calendar };
    }

    // Derives the numeric-date field order (a permutation of 'd','M','y') and the
    // separator between fields from the locale's short-date pattern. Falls back to
    // the en convention (M/d/y) for an unknown/complex pattern, so en and locales
    // whose pattern can't be parsed are unchanged.
    // Replaces the run of year letters ('y') in a pattern with a replacement field,
    // skipping quoted literals. Used to swap a numeric year for the chinese
    // relatedYear(yearName) form.
    private static string ReplaceYearField(string pattern, string replacement)
    {
        var sb = new StringBuilder(pattern.Length + replacement.Length);
        var inQuote = false;
        var replaced = false;
        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];
            if (c == '\'')
            {
                inQuote = !inQuote;
                sb.Append(c);
                continue;
            }
            if (!inQuote && c == 'y')
            {
                while (i + 1 < pattern.Length && pattern[i + 1] == 'y')
                    i++;
                if (!replaced)
                {
                    sb.Append(replacement);
                    replaced = true;
                }
                continue;
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

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
    private static (string type, string value) FormatField(in Token token, in Fields f, int fractionalSecondDigits, string calendar)
    {
        switch (token.Field)
        {
            case 'G':
                // Era. Emitted by era-using calendars and by the explicit `era` option.
                if (NeedsCalendarConversion(calendar))
                    return ("era", "AH");
                if (EraCalendars.TryGetValue(calendar ?? string.Empty, out var era))
                    return ("era", era.Era);
                // Proleptic Gregorian: AD for year > 0, BC otherwise, rendered at the
                // requested width (GGGGG narrow, GGGG long, otherwise short).
                var beforeCommon = f.Year <= 0;
                return ("era", token.Count switch
                {
                    5 => beforeCommon ? "B" : "A",
                    4 => beforeCommon ? "Before Christ" : "Anno Domini",
                    _ => beforeCommon ? "BC" : "AD",
                });
            case 'U':
                // Cyclic year name (sexagenary), used by chinese/dangi.
                return ("yearName", SexagenaryYearName(f.Year));
            case 'r':
                // Related (Gregorian) year, used by chinese/dangi.
                return ("relatedYear", f.Year.ToString(CultureInfo.InvariantCulture));
            case 'y':
                var year = f.Year;
                if (calendar != null && EraCalendars.TryGetValue(calendar, out var cal))
                    year += cal.YearOffset;
                return ("year", token.Count == 2
                    ? (year % 100).ToString("D2", CultureInfo.InvariantCulture)
                    : year.ToString(CultureInfo.InvariantCulture));
            case 'M':
            case 'L':
                var islamic = NeedsCalendarConversion(calendar);
                return ("month", token.Count switch
                {
                    >= 4 when token.Count == 4 => (islamic ? IslamicMonthWide : MonthWide)[f.Month - 1],
                    5 => islamic ? f.Month.ToString(CultureInfo.InvariantCulture) : MonthNarrow[f.Month - 1],
                    3 => (islamic ? IslamicMonthShort : MonthShort)[f.Month - 1],
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
            case 'E':
            case 'c':
                var dow = DayOfWeek(in f);
                return ("weekday", token.Count switch
                {
                    5 => WeekdayNarrow[dow],
                    >= 4 => WeekdayWide[dow],
                    _ => WeekdayShort[dow],
                });
            case 'z':
            case 'O':
            case 'v':
            case 'V':
                return ("timeZoneName", f.TimeZoneName ?? string.Empty);
            case 'a':
            case 'b':
                return ("dayPeriod", f.Hour < 12 ? "AM" : "PM");
            case 'B':
                // Flexible day period (ECMA-402 dayPeriod option), pre-rendered from CLDR.
                return ("dayPeriod", f.DayPeriod ?? string.Empty);
            default:
                return ("literal", new string(token.Field, token.Count));
        }
    }

    internal static List<Part> FormatToParts(Pattern pattern, in Fields f, int fractionalSecondDigits, string source)
    {
        // A calendar with its own months (the islamic family) renders the date's ISO year/month/day
        // projected into that calendar; the time-of-day is unchanged.
        var fields = NeedsCalendarConversion(pattern.Calendar) ? ConvertToCalendar(in f, pattern.Calendar) : f;

        var parts = new List<Part>(pattern.Tokens.Count);
        foreach (var token in pattern.Tokens)
        {
            if (!token.IsField)
            {
                parts.Add(new Part("literal", token.Literal, source));
                continue;
            }
            var (type, value) = FormatField(in token, in fields, fractionalSecondDigits, pattern.Calendar);
            parts.Add(new Part(type, value, source));
        }
        return parts;
    }

    private static Fields ConvertToCalendar(in Fields f, string calendar)
    {
        var (cy, cm, cd) = Temporal.TemporalNonIso.CalendarYmd(calendar, f.Year, f.Month, f.Day);
        return new Fields(cy, cm, cd, f.Hour, f.Minute, f.Second, f.Millisecond, f.TimeZoneName, f.DayPeriod);
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
            return FormatIntervalPattern(intervalTokens, in start, in end, fractionalSecondDigits, pattern.Calendar);
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
        List<Token> tokens, in Fields start, in Fields end, int fractionalSecondDigits, string calendar)
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
                ? FormatField(in token, in end, fractionalSecondDigits, calendar)
                : FormatField(in token, in fields, fractionalSecondDigits, calendar);
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
