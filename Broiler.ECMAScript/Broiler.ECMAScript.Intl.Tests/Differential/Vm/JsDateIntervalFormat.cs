// DIFFERENTIAL CHECK, BROILER.VM'S ARM: a test-only copy of Broiler.VM's code at its SJL-9 rule commit, the
// reference the differential check of Broiler.VM's JSD-0082, decision D22, compares the library with. It is compiled
// into this test project and nowhere else, never ships, and runs under the JIT only; see Differential/Vm/README.md.
//
// Copied from Broiler.VM at 4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f:
//   src/Broiler.VM.Profile.JavaScript/JsDateIntervalFormat.cs
//     blob a0e77e35d49e86461a3e7d79085b537e777e9e11: the whole file: JsDateIntervalFormat
//
// Changes from the source, none of which changes a result:
//   - the namespace;
//   - the generated SPDX and assurance header and the per-member assurance annotations removed.

using System;
using System.Collections.Generic;
using System.Text;

namespace Broiler.ECMAScript.Intl.Tests.Differential.Vm;

/// <summary>
/// The patterns a date range is written in, chosen as ICU's DateIntervalFormat chooses them
/// (JSD-0045): ECMA-402's range patterns are implementation-defined, and ICU's are every engine's.
/// </summary>
/// <remarks>
/// <para>
/// <b>A range is written by the largest field its ends differ in.</b> The skeleton of the format's
/// pattern is split into a date half and a time half, each normalized, and the nearest CLDR interval
/// skeleton gives a pattern for each field that may differ. A field no interval pattern serves falls
/// back to both ends in the format's own pattern, joined by CLDR's fallback; ends on one day whose
/// format has a date and a time write the date once and the times as a range.
/// </para>
/// </remarks>
internal sealed class JsDateIntervalFormat
{
    /// <summary>The calendar fields a range compares, largest first, as ICU's interval indexes.</summary>
    internal const int Era = 0;
    internal const int Year = 1;
    internal const int Month = 2;
    internal const int Date = 3;
    internal const int AmPm = 4;
    internal const int Hour = 5;
    internal const int Minute = 6;
    internal const int Second = 7;
    internal const int Millisecond = 8;
    private const int Count = 9;

    private static readonly int[] Levels = [0, 10, 20, 30, 40, 50, 60, 70, 80];

    private static readonly char[] Letters = ['G', 'y', 'M', 'd', 'a', 'h', 'm', 's', 'S'];

    private readonly string?[] firstParts = new string?[Count];

    private readonly string?[] secondParts = new string?[Count];

    private readonly Dictionary<string, string?[]> intervals = new(StringComparer.Ordinal);

    private readonly List<string> skeletons = [];

    private readonly JsDatePatternGenerator generator;

    private readonly string skeleton;

    private string? datePattern;

    private string? timePattern;

    private readonly string? dateTimeFormat;

    /// <summary>Prepares the patterns of a format whose pattern's skeleton is <paramref name="skeleton"/>.</summary>
    internal JsDateIntervalFormat(JsDateData data, string language, JsDatePatternGenerator generator, string skeleton)
    {
        this.generator = generator;
        this.skeleton = skeleton;
        Fallback = data.Value(language, "interval.fallback") ?? "{0} \u2013 {1}";

        if (data.Locales.TryGetValue(language, out var values))
        {
            foreach (var (key, value) in values)
            {
                if (!key.StartsWith("interval.", StringComparison.Ordinal) || key == "interval.fallback")
                {
                    continue;
                }

                var dot = key.LastIndexOf('.');
                var name = key["interval.".Length..dot];
                var index = key[(dot + 1)..] switch
                {
                    "G" => Era,
                    "y" => Year,
                    "M" => Month,
                    "d" => Date,
                    "a" or "B" => AmPm,
                    "h" or "H" => Hour,
                    "m" => Minute,
                    _ => -1,
                };

                if (index < 0)
                {
                    continue;
                }

                if (!intervals.TryGetValue(name, out var patterns))
                {
                    patterns = new string?[Count];
                    intervals[name] = patterns;
                    skeletons.Add(name);
                }

                patterns[index] ??= value;
            }
        }

        skeletons.Sort(StringComparer.Ordinal);
        FullPattern = generator.BestPattern(skeleton, 0);

        Split(NormalizeHour(skeleton), out var dateSkeleton, out var normalizedDate, out var timeSkeleton, out var normalizedTime);

        if (timeSkeleton.Length != 0 && dateSkeleton.Length != 0)
        {
            dateTimeFormat = data.Value(language, "dateTime.medium");
        }

        var found = SetSeparateDateTimePattern(normalizedDate, normalizedTime);

        if (!found || (timeSkeleton.Length != 0 && dateSkeleton.Length == 0))
        {
            // A TIME WITHOUT A DATE falls back, for a date difference, to the short date before it.
            if (timeSkeleton.Length != 0 && dateSkeleton.Length == 0)
            {
                var withDate = "yMd" + timeSkeleton;
                var pattern = generator.BestPattern(withDate, 0);
                SetPatternInfo(Date, null, pattern);
                SetPatternInfo(Month, null, pattern);
                SetPatternInfo(Year, null, pattern);
                SetPatternInfo(Era, null, generator.BestPattern("G" + withDate, 0));
            }

            return;
        }

        if (timeSkeleton.Length == 0)
        {
            return;
        }

        // A DATE AND A TIME: a date difference falls back to both ends whole, and a time difference
        // writes the date once before the time range.
        var extended = skeleton;

        foreach (var (field, letter) in new[] { (Date, 'd'), (Month, 'M'), (Year, 'y'), (Era, 'G') })
        {
            if (dateSkeleton.IndexOf(letter) < 0)
            {
                extended = letter + extended;
                SetPatternInfo(field, null, generator.BestPattern(extended, 0));
            }
        }

        if (dateTimeFormat is null)
        {
            return;
        }

        var dateOnce = generator.BestPattern(dateSkeleton, 0);

        foreach (var field in new[] { AmPm, Hour, Minute })
        {
            if (!string.IsNullOrEmpty(firstParts[field]))
            {
                SetIntervalPattern(field, JsDatePatternGenerator.Substitute(dateTimeFormat, firstParts[field] + secondParts[field], dateOnce, string.Empty));
            }
        }
    }

    /// <summary>The pattern each end is written in when the range falls back: the format's skeleton's own.</summary>
    internal string FullPattern { get; }

    /// <summary>CLDR's fallback range pattern.</summary>
    internal string Fallback { get; }

    /// <summary>
    /// The range from <paramref name="from"/> to <paramref name="to"/> as ICU's <c>formatImpl</c>
    /// writes it, with its fields; the first index is -1 when it is one date.
    /// </summary>
    internal int Format(JsLocalTime from, JsLocalTime to, JsDateTimeFormatter formatter, StringBuilder text, List<JsDateField> fields)
    {
        var field = Largest(from, to);

        if (field < 0)
        {
            formatter.FormatPattern(FullPattern, from, text, fields);
            return -1;
        }

        var sameDay = field >= AmPm;

        if (string.IsNullOrEmpty(firstParts[field]) && string.IsNullOrEmpty(secondParts[field]))
        {
            if (IsFieldUnitIgnored(FullPattern, field))
            {
                formatter.FormatPattern(FullPattern, from, text, fields);
                return -1;
            }

            return FallbackFormat(FullPattern, from, to, sameDay, formatter, text, fields);
        }

        if (string.IsNullOrEmpty(firstParts[field]))
        {
            return FallbackFormat(secondParts[field]!, from, to, sameDay, formatter, text, fields);
        }

        formatter.FormatPattern(firstParts[field]!, from, text, fields);

        if (!string.IsNullOrEmpty(secondParts[field]))
        {
            formatter.FormatPattern(secondParts[field]!, to, text, fields);
        }

        return 0;
    }

    /// <summary>The largest calendar field two times differ in, or -1.</summary>
    private static int Largest(JsLocalTime from, JsLocalTime to)
    {
        if (from.Era != to.Era)
        {
            return Era;
        }

        if (from.EraYear != to.EraYear)
        {
            return Year;
        }

        if (from.Month != to.Month)
        {
            return Month;
        }

        if (from.Day != to.Day)
        {
            return Date;
        }

        if ((from.Hour >= 12) != (to.Hour >= 12))
        {
            return AmPm;
        }

        if (from.Hour % 12 != to.Hour % 12)
        {
            return Hour;
        }

        if (from.Minute != to.Minute)
        {
            return Minute;
        }

        if (from.Second != to.Second)
        {
            return Second;
        }

        return from.Millisecond != to.Millisecond ? Millisecond : -1;
    }

    /// <summary>ICU's <c>fallbackFormat</c>.</summary>
    private int FallbackFormat(string pattern, JsLocalTime from, JsLocalTime to, bool sameDay, JsDateTimeFormatter formatter, StringBuilder text, List<JsDateField> fields)
    {
        if (sameDay && datePattern is not null && timePattern is not null && dateTimeFormat is not null)
        {
            var zero = dateTimeFormat.IndexOf("{0}", StringComparison.Ordinal);
            var one = dateTimeFormat.IndexOf("{1}", StringComparison.Ordinal);

            if (zero < one)
            {
                text.Append(dateTimeFormat, 0, zero);
                var first = FallbackRange(timePattern, from, to, formatter, text, fields);
                text.Append(dateTimeFormat, zero + 3, one - zero - 3);
                formatter.FormatPattern(datePattern, from, text, fields);
                text.Append(dateTimeFormat, one + 3, dateTimeFormat.Length - one - 3);
                return first;
            }

            text.Append(dateTimeFormat, 0, one);
            formatter.FormatPattern(datePattern, from, text, fields);
            text.Append(dateTimeFormat, one + 3, zero - one - 3);
            var index = FallbackRange(timePattern, from, to, formatter, text, fields);
            text.Append(dateTimeFormat, zero + 3, dateTimeFormat.Length - zero - 3);
            return index;
        }

        return FallbackRange(pattern, from, to, formatter, text, fields);
    }

    /// <summary>ICU's <c>fallbackRange</c>: both ends in one pattern, joined by CLDR's fallback.</summary>
    private int FallbackRange(string pattern, JsLocalTime from, JsLocalTime to, JsDateTimeFormatter formatter, StringBuilder text, List<JsDateField> fields)
    {
        var zero = Fallback.IndexOf("{0}", StringComparison.Ordinal);
        var one = Fallback.IndexOf("{1}", StringComparison.Ordinal);

        if (zero < one)
        {
            text.Append(Fallback, 0, zero);
            formatter.FormatPattern(pattern, from, text, fields);
            text.Append(Fallback, zero + 3, one - zero - 3);
            formatter.FormatPattern(pattern, to, text, fields);
            text.Append(Fallback, one + 3, Fallback.Length - one - 3);
            return 0;
        }

        text.Append(Fallback, 0, one);
        formatter.FormatPattern(pattern, to, text, fields);
        text.Append(Fallback, one + 3, zero - one - 3);
        formatter.FormatPattern(pattern, from, text, fields);
        text.Append(Fallback, zero + 3, Fallback.Length - zero - 3);
        return 1;
    }

    // ---- initialization ---------------------------------------------------------------------------

    /// <summary>
    /// ICU's <c>normalizeHourMetacharacters</c>: the hour field becomes one hour letter of the cycle
    /// the locale writes it in, followed on a twelve-hour clock by its day period.
    /// </summary>
    private string NormalizeHour(string text)
    {
        var hourChar = '\0';
        var dayPeriodChar = '\0';
        var hourStart = 0;
        var hourLength = 0;
        var dayPeriodStart = 0;
        var dayPeriodLength = 0;

        for (var at = 0; at < text.Length; at++)
        {
            var c = text[at];

            if (c is 'j' or 'J' or 'C' or 'h' or 'H' or 'k' or 'K')
            {
                if (hourChar == '\0')
                {
                    hourChar = c;
                    hourStart = at;
                }

                hourLength++;
            }
            else if (c is 'a' or 'b' or 'B')
            {
                if (dayPeriodChar == '\0')
                {
                    dayPeriodChar = c;
                    dayPeriodStart = at;
                }

                dayPeriodLength++;
            }
            else if (hourChar != '\0' && dayPeriodChar != '\0')
            {
                break;
            }
        }

        if (hourChar == '\0')
        {
            return text;
        }

        var converted = generator.BestPattern(hourChar.ToString(), 0);
        var letters = new StringBuilder();
        var quoted = false;

        foreach (var c in converted)
        {
            if (c == '\'')
            {
                quoted = !quoted;
            }
            else if (!quoted && JsDatePatternGenerator.IsLetter(c))
            {
                letters.Append(c);
            }
        }

        var stripped = letters.ToString();
        var hour = stripped.Contains('h') ? 'h' : stripped.Contains('K') ? 'K' : stripped.Contains('k') ? 'k' : 'H';
        var replacement = new StringBuilder().Append(hour);

        if (hour is 'h' or 'K')
        {
            if (dayPeriodChar == '\0')
            {
                dayPeriodChar = 'a';
            }

            var length = dayPeriodLength >= 5 || hourLength >= 5 ? 5 : dayPeriodLength >= 3 || hourLength >= 3 ? 3 : 1;
            replacement.Append(dayPeriodChar, length);
        }

        var result = text.Remove(hourStart, hourLength).Insert(hourStart, replacement.ToString());

        if (dayPeriodLength != 0)
        {
            if (dayPeriodStart > hourStart)
            {
                dayPeriodStart += replacement.Length - hourLength;
            }

            result = result.Remove(dayPeriodStart, dayPeriodLength);
        }

        return result;
    }

    /// <summary>ICU's <c>getDateTimeSkeleton</c>: a skeleton's date and time halves, each also normalized.</summary>
    private static void Split(string text, out string date, out string normalizedDate, out string time, out string normalizedTime)
    {
        var dateText = new StringBuilder();
        var normalizedDateText = new StringBuilder();
        var timeText = new StringBuilder();
        var normalizedTimeText = new StringBuilder();
        int weekdays = 0, days = 0, months = 0, years = 0, minutes = 0, generics = 0, specifics = 0;
        var hourChar = '\0';

        foreach (var c in text)
        {
            switch (c)
            {
                case 'E':
                    dateText.Append(c);
                    weekdays++;
                    break;
                case 'd':
                    dateText.Append(c);
                    days++;
                    break;
                case 'M':
                    dateText.Append(c);
                    months++;
                    break;
                case 'y':
                    dateText.Append(c);
                    years++;
                    break;
                case 'G' or 'Y' or 'u' or 'Q' or 'q' or 'L' or 'l' or 'W' or 'w' or 'D' or 'F' or 'g' or 'e' or 'c' or 'U' or 'r':
                    normalizedDateText.Append(c);
                    dateText.Append(c);
                    break;
                case 'h' or 'H' or 'k' or 'K' or 'j' or 'J':
                    timeText.Append(c);

                    if (hourChar == '\0')
                    {
                        hourChar = c;
                    }

                    break;
                case 'm':
                    timeText.Append(c);
                    minutes++;
                    break;
                case 'z':
                    specifics++;
                    timeText.Append(c);
                    break;
                case 'v':
                    generics++;
                    timeText.Append(c);
                    break;
                case 'a' or 'V' or 'Z' or 's' or 'S' or 'A' or 'b' or 'B':
                    timeText.Append(c);
                    normalizedTimeText.Append(c);
                    break;
            }
        }

        normalizedDateText.Append('y', years);

        if (months != 0)
        {
            normalizedDateText.Append('M', months < 3 ? 1 : Math.Min(months, 5));
        }

        if (weekdays != 0)
        {
            normalizedDateText.Append('E', weekdays <= 3 ? 1 : Math.Min(weekdays, 5));
        }

        if (days != 0)
        {
            normalizedDateText.Append('d');
        }

        if (hourChar != '\0')
        {
            normalizedTimeText.Append(hourChar);
        }

        if (minutes != 0)
        {
            normalizedTimeText.Append('m');
        }

        if (specifics != 0)
        {
            normalizedTimeText.Append('z');
        }

        if (generics != 0)
        {
            normalizedTimeText.Append('v');
        }

        date = dateText.ToString();
        normalizedDate = normalizedDateText.ToString();
        time = timeText.ToString();
        normalizedTime = normalizedTimeText.ToString();
    }

    /// <summary>ICU's <c>setSeparateDateTimePtn</c>.</summary>
    private bool SetSeparateDateTimePattern(string dateSkeleton, string timeSkeleton)
    {
        var current = timeSkeleton.Length != 0 ? timeSkeleton : dateSkeleton;
        var best = BestSkeleton(current, out var difference);

        if (best is null)
        {
            return false;
        }

        if (dateSkeleton.Length != 0)
        {
            datePattern = generator.BestPattern(dateSkeleton, 0);
        }

        if (timeSkeleton.Length != 0)
        {
            timePattern = generator.BestPattern(timeSkeleton, 0);
        }

        if (difference == -1)
        {
            return false;
        }

        if (timeSkeleton.Length == 0)
        {
            SetIntervalPattern(Date, current, best, difference, extend: true, out _, out _);

            if (SetIntervalPattern(Month, current, best, difference, extend: true, out var extendedSkeleton, out var extendedBest))
            {
                current = extendedSkeleton!;
                best = extendedBest!;
            }

            SetIntervalPattern(Year, current, best, difference, extend: true, out _, out _);
            SetIntervalPattern(Era, current, best, difference, extend: true, out _, out _);
        }
        else
        {
            SetIntervalPattern(Minute, current, best, difference, extend: false, out _, out _);
            SetIntervalPattern(Hour, current, best, difference, extend: false, out _, out _);
            SetIntervalPattern(AmPm, current, best, difference, extend: false, out _, out _);
        }

        return true;
    }

    /// <summary>ICU's <c>setIntervalPattern</c> for one field: whether it extended the skeleton by the field's letter.</summary>
    private bool SetIntervalPattern(int field, string current, string best, int difference, bool extend, out string? extendedSkeleton, out string? extendedBest)
    {
        extendedSkeleton = null;
        extendedBest = null;
        var pattern = Pattern(best, field);

        if (pattern is null)
        {
            if (IsFieldUnitIgnored(best, field))
            {
                return false;
            }

            // A DAY PERIOD DIFFERENCE IS AN HOUR DIFFERENCE where the data names none.
            if (field == AmPm)
            {
                if (Pattern(best, Hour) is { } hourPattern)
                {
                    SetIntervalPattern(field, AdjustFieldWidth(current, best, hourPattern, difference));
                }

                return false;
            }

            if (extend)
            {
                extendedSkeleton = Letters[field] + current;
                extendedBest = Letters[field] + best;
                pattern = Pattern(extendedBest, field);

                if (pattern is null && difference == 0)
                {
                    var nearest = BestSkeleton(extendedBest, out difference);

                    if (nearest is not null && difference != -1)
                    {
                        pattern = Pattern(nearest, field);
                        best = nearest;
                    }
                }
            }
        }

        if (pattern is not null)
        {
            SetIntervalPattern(field, difference != 0 ? AdjustFieldWidth(current, best, pattern, difference) : pattern);
            return extendedSkeleton is not null;
        }

        return false;
    }

    private string? Pattern(string name, int field) => intervals.TryGetValue(name, out var patterns) ? patterns[field] : null;

    private void SetPatternInfo(int field, string? first, string? second)
    {
        if (first is not null)
        {
            firstParts[field] = first;
        }

        if (second is not null)
        {
            secondParts[field] = second;
        }
    }

    /// <summary>An interval pattern split where its first repeated field begins.</summary>
    private void SetIntervalPattern(int field, string pattern)
    {
        var split = SplitPoint(pattern);
        firstParts[field] = pattern[..split];
        secondParts[field] = split < pattern.Length ? pattern[split..] : string.Empty;
    }

    /// <summary>ICU's <c>splitPatternInto2Part</c>.</summary>
    private static int SplitPoint(string pattern)
    {
        var seen = new bool[58];
        var quoted = false;
        var previous = '\0';
        var count = 0;
        var repeated = false;
        var at = 0;

        for (; at < pattern.Length; at++)
        {
            var c = pattern[at];

            if (c != previous && count > 0)
            {
                if (!seen[previous - 'A'])
                {
                    seen[previous - 'A'] = true;
                }
                else
                {
                    repeated = true;
                    break;
                }

                count = 0;
            }

            if (c == '\'')
            {
                if (at + 1 < pattern.Length && pattern[at + 1] == '\'')
                {
                    at++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (!quoted && JsDatePatternGenerator.IsLetter(c))
            {
                previous = c;
                count++;
            }
        }

        if (count > 0 && !repeated && !seen[previous - 'A'])
        {
            count = 0;
        }

        return at - count;
    }

    /// <summary>ICU's <c>getBestSkeleton</c> over the interval skeletons.</summary>
    private string? BestSkeleton(string text, out int difference)
    {
        var input = text;
        var replaced = false;

        if (text.IndexOfAny(['z', 'k', 'K', 'a', 'b']) >= 0)
        {
            input = text.Replace('z', 'v').Replace('k', 'H').Replace('K', 'h').Replace("a", string.Empty, StringComparison.Ordinal).Replace("b", string.Empty, StringComparison.Ordinal);
            replaced = true;
        }

        var inputWidths = Widths(input);
        string? best = null;
        var bestDistance = int.MaxValue;
        difference = 0;

        foreach (var candidate in skeletons)
        {
            var widths = Widths(candidate);
            var distance = 0;
            var fieldDifference = 1;

            for (var index = 0; index < widths.Length; index++)
            {
                var mine = inputWidths[index];
                var theirs = widths[index];

                if (mine == theirs)
                {
                    continue;
                }

                if (mine == 0 || theirs == 0)
                {
                    fieldDifference = -1;
                    distance += 0x1000;
                }
                else if (index + 'A' == 'M' && ((mine <= 2 && theirs > 2) || (mine > 2 && theirs <= 2)))
                {
                    distance += 0x100;
                }
                else
                {
                    distance += Math.Abs(mine - theirs);
                }
            }

            if (distance < bestDistance)
            {
                best = candidate;
                bestDistance = distance;
                difference = fieldDifference;
            }

            if (distance == 0)
            {
                difference = 0;
                break;
            }
        }

        if (replaced && difference != -1)
        {
            difference = 2;
        }

        return best;
    }

    private static int[] Widths(string text)
    {
        var widths = new int[58];

        foreach (var c in text)
        {
            if (c is >= 'A' and <= 'z')
            {
                widths[c - 'A']++;
            }
        }

        return widths;
    }

    /// <summary>ICU's <c>adjustFieldWidth</c>: an interval pattern's fields widened to the skeleton's.</summary>
    private static string AdjustFieldWidth(string input, string best, string pattern, int difference)
    {
        var adjusted = new StringBuilder(pattern);
        var inputWidths = Widths(input);
        var bestWidths = Widths(best);

        if (difference == 2)
        {
            if (input.Contains('z'))
            {
                bestWidths['z' - 'A'] = bestWidths['v' - 'A'];
                ReplaceInPattern(adjusted, 'v', 'z');
            }

            if (input.Contains('K'))
            {
                ReplaceInPattern(adjusted, 'h', 'K');
            }

            if (input.Contains('k'))
            {
                ReplaceInPattern(adjusted, 'H', 'k');
            }

            if (input.Contains('b'))
            {
                ReplaceInPattern(adjusted, 'a', 'b');
            }
        }

        var text = adjusted.ToString();

        if (text.Contains('a') && bestWidths['a' - 'A'] == 0)
        {
            bestWidths['a' - 'A'] = 1;
        }

        if (text.Contains('b') && bestWidths['b' - 'A'] == 0)
        {
            bestWidths['b' - 'A'] = 1;
        }

        var quoted = false;
        var previous = '\0';
        var count = 0;

        void Widen(int at)
        {
            var letter = previous == 'L' ? 'M' : previous;
            var bestCount = bestWidths[letter - 'A'];
            var inputCount = inputWidths[letter - 'A'];

            if (bestCount == count && inputCount > bestCount)
            {
                adjusted.Insert(at, new string(previous, inputCount - bestCount));
            }
        }

        for (var at = 0; at < adjusted.Length; at++)
        {
            var c = adjusted[at];

            if (c != previous && count > 0)
            {
                var before = adjusted.Length;
                Widen(at);
                at += adjusted.Length - before;
                count = 0;
                c = adjusted[at];
            }

            if (c == '\'')
            {
                if (at + 1 < adjusted.Length && adjusted[at + 1] == '\'')
                {
                    at++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (!quoted && JsDatePatternGenerator.IsLetter(c))
            {
                previous = c;
                count++;
            }
        }

        if (count > 0)
        {
            Widen(adjusted.Length);
        }

        return adjusted.ToString();
    }

    private static void ReplaceInPattern(StringBuilder pattern, char from, char to)
    {
        var quoted = false;

        for (var at = 0; at < pattern.Length; at++)
        {
            if (pattern[at] == '\'')
            {
                quoted = !quoted;
            }
            else if (!quoted && pattern[at] == from)
            {
                pattern[at] = to;
            }
        }
    }

    /// <summary>
    /// ICU's <c>isFieldUnitIgnored</c>: whether every field of a pattern is larger than
    /// <paramref name="field"/>, so that a difference in it does not show.
    /// </summary>
    internal static bool IsFieldUnitIgnored(string pattern, int field)
    {
        var fieldLevel = Levels[field];
        var quoted = false;
        var previous = '\0';
        var count = 0;

        for (var at = 0; at < pattern.Length; at++)
        {
            var c = pattern[at];

            if (c != previous && count > 0)
            {
                if (fieldLevel <= Level(previous))
                {
                    return false;
                }

                count = 0;
            }

            if (c == '\'')
            {
                if (at + 1 < pattern.Length && pattern[at + 1] == '\'')
                {
                    at++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (!quoted && JsDatePatternGenerator.IsLetter(c))
            {
                previous = c;
                count++;
            }
        }

        return count <= 0 || fieldLevel > Level(previous);
    }

    /// <summary>ICU's level of a pattern letter: the larger, the smaller the unit; -1 for a letter with none.</summary>
    private static int Level(char c) => c switch
    {
        'G' or 'O' or 'V' or 'X' or 'Z' or 'g' or 'l' or 'v' or 'x' or 'z' => 0,
        'U' or 'Y' or 'r' or 'u' or 'y' => 10,
        'D' or 'L' or 'M' or 'Q' or 'q' or 'w' => 20,
        'E' or 'F' or 'W' or 'c' or 'd' or 'e' => 30,
        'A' or 'a' => 40,
        'H' or 'K' or 'h' or 'k' => 50,
        'm' => 60,
        's' => 70,
        'S' => 80,
        _ => -1,
    };
}
