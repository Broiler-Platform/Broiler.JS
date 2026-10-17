// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   25
// Annotated:        25/25
// Exempt:           1
// Human-reviewed:   0/25
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  5/10 max
// Unverified:       25
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Broiler.ECMAScript.Generation.Time;

/// <summary>A tzdb <c>Rule</c> line: the years it covers, the day and time it falls on, and its save.</summary>
/// <param name="From">The first year.</param>
/// <param name="To">The last year, or null for <c>max</c>.</param>
/// <param name="Month">The month, January 1.</param>
/// <param name="On">The ON field as written: a day, <c>lastSun</c>, <c>Sun&gt;=8</c> or <c>Sun&lt;=25</c>.</param>
/// <param name="At">The AT time, in seconds after midnight.</param>
/// <param name="AtKind">How AT is read: <c>w</c> wall clock, <c>s</c> standard time, <c>u</c> universal time.</param>
/// <param name="Save">The SAVE amount, in seconds.</param>
// Broiler-AI:           Origin=Ported; Spec=JSD-0053 zic compile via Broiler.VM TzdbCompiler; IP=Low; Security=Low; Resources=1; Fingerprint=18F04A
// Broiler-Human:        PENDING
public sealed record TzdbRule(int From, int? To, int Month, string On, int At, char AtKind, int Save);

/// <summary>A tzdb <c>Zone</c> line or continuation: its standard offset, its rules, and when it ends.</summary>
/// <param name="StandardOffset">The STDOFF field, in seconds.</param>
/// <param name="Rules">The RULES field: <c>-</c>, a fixed save such as <c>1:00</c>, or a rule set's name.</param>
/// <param name="Until">The UNTIL fields, or none on the zone's last line.</param>
// Broiler-AI:           Origin=Ported; Spec=JSD-0053 zic compile via Broiler.VM TzdbCompiler; IP=Low; Security=Low; Resources=1; Fingerprint=A46890
// Broiler-Human:        PENDING
public sealed record TzdbZoneLine(int StandardOffset, string Rules, IReadOnlyList<string> Until);

/// <summary>A zone's recurring rules after the last year its transitions are listed for.</summary>
/// <param name="StandardOffset">The standard offset of the zone's last line, in seconds.</param>
/// <param name="LastListedYear">The last year whose transitions are listed; the rules give every later year's.</param>
/// <param name="Rules">The rules without a last year, in the order the source gives them.</param>
// Broiler-AI:           Origin=Ported; Spec=JSD-0053 zic compile via Broiler.VM TzdbCompiler; IP=Low; Security=Low; Resources=1; Fingerprint=C98CAE
// Broiler-Human:        PENDING
public sealed record TzdbFinalRules(int StandardOffset, int LastListedYear, IReadOnlyList<TzdbRule> Rules);

/// <summary>A change of a zone's UTC offset, or of its daylight flag alone.</summary>
/// <param name="At">The instant, in seconds since the epoch.</param>
/// <param name="Offset">The UTC offset from that instant, in seconds.</param>
/// <param name="Daylight">
/// Whether that offset is daylight saving time as CLDR names it: the rearguard reading, in which a negative
/// save makes the other time of the year the daylight one.
/// </param>
// Broiler-AI:           Origin=Ported; Spec=JSD-0053 zic compile via Broiler.VM TzdbCompiler; IP=Low; Security=Low; Resources=0; Fingerprint=CB36F0
// Broiler-Human:        PENDING
public readonly record struct TzdbTransition(long At, int Offset, bool Daylight);

/// <summary>A compiled zone: its first offset, each later change of offset, and the rules that continue it.</summary>
/// <param name="InitialOffset">The offset before the first transition, in seconds.</param>
/// <param name="Transitions">Each change of offset or of the daylight flag, in order of time.</param>
/// <param name="Final">The rules for every year after the listed transitions, or null when the last offset holds for ever.</param>
// Broiler-AI:           Origin=Ported; Spec=JSD-0053 zic compile via Broiler.VM TzdbCompiler; IP=Low; Security=Low; Resources=1; Fingerprint=92C3AE
// Broiler-Human:        PENDING
public sealed record TzdbZone(int InitialOffset, IReadOnlyList<TzdbTransition> Transitions, TzdbFinalRules? Final);

/// <summary>The rule sets, zones and links of the tzdb source files, by name.</summary>
/// <param name="Rules">Each rule set's lines, by its name, in the order the files give them.</param>
/// <param name="Zones">Each Zone's lines, by its name, in the order the files give them.</param>
/// <param name="Links">Each Link's target, by the Link's name.</param>
// Broiler-AI:           Origin=Ported; Spec=JSD-0053 zic compile via Broiler.VM TzdbCompiler; IP=Low; Security=Low; Resources=1; Fingerprint=B44F82
// Broiler-Human:        PENDING
public sealed record TzdbSource(
    IReadOnlyDictionary<string, IReadOnlyList<TzdbRule>> Rules,
    IReadOnlyDictionary<string, IReadOnlyList<TzdbZoneLine>> Zones,
    IReadOnlyDictionary<string, string> Links);

/// <summary>
/// A compiler of tzdb source into UTC offsets, as <c>zic</c> compiles it (Broiler.VM's decision JSD-0053):
/// each zone's lines in order, each line's rules in chronological order, and <c>zic</c>'s own merge of a
/// transition whose wall clock does not advance into the one before it.
/// </summary>
/// <remarks>
/// <para>
/// <b>PORTED from Broiler.VM's <c>TzdbCompiler</c></b> (Broiler.VM's JSD-0076 moves it to the data home),
/// with its behaviour unchanged. Where it read a month or a weekday through the invariant culture's case
/// mapping it folds ASCII letters (convention 9), and its numbers are read through the invariant format,
/// which on the pinned ASCII source gives the same answers.
/// </para>
/// <para>
/// <b>Offsets, and the daylight flag CLDR's names read.</b> Temporal and <c>Intl.DateTimeFormat</c> read a
/// zone's UTC offset at an instant and the instants at which it changes; abbreviations are not kept. A
/// change of the daylight flag alone is kept as a transition for the names, and a reader skips it where
/// Temporal asks for the next or previous change of offset (GetNamedTimeZoneNextTransition). <c>zic</c>'s
/// own flag, a save other than zero, is tracked while compiling, because its merge compares whole types;
/// the flag kept is the rearguard one ICU and CLDR's names read (Broiler.VM's JSD-0058): where a line's
/// rules save a negative amount, the larger save is the daylight one.
/// </para>
/// <para>
/// <b>Checked against <c>zic</c></b>: the home's <c>data/time/tzdb/check-zic.py</c> compiles the nine
/// pinned files with the host's <c>zic -b fat</c> and compares every identifier's offsets, 1800 to 2037,
/// with the generated table (milestone SJL-8's data-home exit clause 3).
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=JSD-0053 zic compile via Broiler.VM TzdbCompiler; IP=Low; Security=Medium; Resources=5; Fingerprint=8A4293
// Broiler-Human:        PENDING
public static class TzdbCompiler
{
    /// <summary>The source files <c>zic</c> compiles in the pinned default build, in the order IANA's Makefile lists them; <c>factory</c> is left out (SJL-D-20).</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 zic compile via Broiler.VM TzdbCompiler; IP=Low; Security=Low; Resources=1; Fingerprint=DA3CEF
    // Broiler-Human:        PENDING
    public static ImmutableArray<string> SourceFiles { get; } =
        ["africa", "antarctica", "asia", "australasia", "europe", "northamerica", "southamerica", "etcetera", "backward"];

    /// <summary>Reads the source files' Rule, Zone and Link lines.</summary>
    /// <param name="archive">The verified members of the tzdb tarball, by name; each of <see cref="SourceFiles"/> must be among them.</param>
    /// <exception cref="InvalidDataException">A line is neither a Rule, a Zone, a Link nor a continuation, or a field cannot be read.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 zic compile via Broiler.VM TzdbCompiler; IP=Low; Security=Medium; Resources=4; Fingerprint=531868
    // Broiler-Human:        PENDING
    public static TzdbSource Parse(IReadOnlyDictionary<string, byte[]> archive)
    {
        ArgumentNullException.ThrowIfNull(archive);

        var rules = new Dictionary<string, List<TzdbRule>>(StringComparer.Ordinal);
        var zones = new Dictionary<string, List<TzdbZoneLine>>(StringComparer.Ordinal);
        var links = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var file in SourceFiles)
        {
            List<TzdbZoneLine>? zone = null;

            foreach (var raw in TzdbPin.Decode(archive[file]).Split('\n'))
            {
                var hash = raw.IndexOf('#', StringComparison.Ordinal);
                var line = (hash < 0 ? raw : raw[..hash]).TrimEnd();

                if (line.Trim().Length == 0)
                {
                    continue;
                }

                var fields = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                var continuation = line[0] is ' ' or '\t';

                if (!continuation && fields[0] == "Rule")
                {
                    var from = Integer(fields[2]);
                    int? to = fields[3].StartsWith('o') ? from : fields[3].StartsWith("ma", StringComparison.Ordinal) ? null : Integer(fields[3]);
                    var (at, kind) = Time(fields[7]);

                    if (!rules.TryGetValue(fields[1], out var set))
                    {
                        rules[fields[1]] = set = [];
                    }

                    set.Add(new TzdbRule(from, to, Month(fields[5]), fields[6], at, kind, Time(fields[8]).Seconds));
                    zone = null;
                }
                else if (!continuation && fields[0] == "Link")
                {
                    links[fields[2]] = fields[1];
                    zone = null;
                }
                else if (!continuation && fields[0] == "Zone")
                {
                    zone = [];
                    zones[fields[1]] = zone;
                    zone.Add(ZoneLine(fields[2..]));
                }
                else if (continuation && zone is not null)
                {
                    zone.Add(ZoneLine(fields));
                }
                else
                {
                    throw new InvalidDataException("tzdb " + file + ": `" + line + "` is neither a Rule, a Zone, a Link nor a continuation");
                }
            }
        }

        return new TzdbSource(
            rules.ToDictionary(static pair => pair.Key, static pair => (IReadOnlyList<TzdbRule>)pair.Value.AsReadOnly(), StringComparer.Ordinal),
            zones.ToDictionary(static pair => pair.Key, static pair => (IReadOnlyList<TzdbZoneLine>)pair.Value.AsReadOnly(), StringComparer.Ordinal),
            links);
    }

    /// <summary>A zone line's fields from its STDOFF on.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 zic compile via Broiler.VM TzdbCompiler; IP=Low; Security=Medium; Resources=1; Fingerprint=19B167
    // Broiler-Human:        PENDING
    private static TzdbZoneLine ZoneLine(string[] fields) =>
        new(Time(fields[0]).Seconds, fields[1], fields.Length > 3 ? fields[3..] : []);

    /// <summary>A decimal integer field, read through the invariant format.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 zic compile via Broiler.VM TzdbCompiler; IP=Low; Security=Medium; Resources=1; Fingerprint=5F5DEF
    // Broiler-Human:        PENDING
    internal static int Integer(string text) => int.Parse(text, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);

    /// <summary>A three-letter name with its first letter in upper case and the next two in lower case, ASCII letters folded.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 zic compile via Broiler.VM TzdbCompiler; IP=Low; Security=Medium; Resources=1; Fingerprint=986094
    // Broiler-Human:        PENDING
    private static string Capitalized(string text) =>
        string.Concat(ArchiveReading.AsciiUpper(text[0]).ToString(), ArchiveReading.AsciiLower(text[1..3]));

    /// <summary>A month field, January 1, by its first three letters.</summary>
    /// <exception cref="InvalidDataException">The field names no month.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 zic compile via Broiler.VM TzdbCompiler; IP=Low; Security=Medium; Resources=1; Fingerprint=68614F
    // Broiler-Human:        PENDING
    internal static int Month(string text) =>
        Capitalized(text) switch
        {
            "Jan" => 1, "Feb" => 2, "Mar" => 3, "Apr" => 4, "May" => 5, "Jun" => 6,
            "Jul" => 7, "Aug" => 8, "Sep" => 9, "Oct" => 10, "Nov" => 11, "Dec" => 12,
            _ => throw new InvalidDataException("tzdb: `" + text + "` is not a month"),
        };

    /// <summary>A weekday, Monday 0, by its first three letters.</summary>
    /// <exception cref="InvalidDataException">The field names no weekday.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 zic compile via Broiler.VM TzdbCompiler; IP=Low; Security=Medium; Resources=1; Fingerprint=92E117
    // Broiler-Human:        PENDING
    internal static int Weekday(string text) =>
        Capitalized(text) switch
        {
            "Mon" => 0, "Tue" => 1, "Wed" => 2, "Thu" => 3, "Fri" => 4, "Sat" => 5, "Sun" => 6,
            _ => throw new InvalidDataException("tzdb: `" + text + "` is not a day"),
        };

    /// <summary>A time field, <c>[-]h[:mm[:ss]]</c> with an optional suffix: seconds, and <c>w</c>, <c>s</c> or <c>u</c>.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 zic compile via Broiler.VM TzdbCompiler; IP=Low; Security=Medium; Resources=1; Fingerprint=BAD60F
    // Broiler-Human:        PENDING
    internal static (int Seconds, char Kind) Time(string text)
    {
        var kind = 'w';

        if (text.Length > 0 && text[^1] is 'w' or 's' or 'u' or 'g' or 'z' or 'd')
        {
            kind = text[^1] switch { 'g' or 'z' => 'u', 'd' => 'w', var c => c };
            text = text[..^1];
        }

        if (text is "-" or "")
        {
            return (0, kind);
        }

        var sign = 1;

        if (text[0] == '-')
        {
            sign = -1;
            text = text[1..];
        }

        var parts = text.Split(':');
        var seconds = Integer(parts[0]) * 3600;

        if (parts.Length > 1)
        {
            seconds += Integer(parts[1]) * 60;
        }

        if (parts.Length > 2)
        {
            seconds += (int)double.Parse(parts[2], NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.InvariantInfo);
        }

        return (sign * seconds, kind);
    }

    /// <summary>Days since 1970-01-01 of a proleptic Gregorian date: Howard Hinnant's <c>days_from_civil</c>.</summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month, January 1.</param>
    /// <param name="day">The day of the month.</param>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 zic compile via Broiler.VM TzdbCompiler; IP=Low; Security=Low; Resources=0; Fingerprint=6178AC
    // Broiler-Human:        PENDING
    public static long DaysFromCivil(long year, int month, int day)
    {
        year -= month <= 2 ? 1 : 0;
        var era = (year >= 0 ? year : year - 399) / 400;
        var yearOfEra = year - (era * 400);
        var dayOfYear = ((153 * (month + (month > 2 ? -3 : 9))) + 2) / 5 + day - 1;
        var dayOfEra = (yearOfEra * 365) + (yearOfEra / 4) - (yearOfEra / 100) + dayOfYear;
        return (era * 146097) + dayOfEra - 719468;
    }

    /// <summary>The length of a month of the proleptic Gregorian calendar.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 zic compile via Broiler.VM TzdbCompiler; IP=Low; Security=Low; Resources=0; Fingerprint=A3ED65
    // Broiler-Human:        PENDING
    private static int MonthLength(long year, int month) =>
        month == 2
            ? (year % 4 == 0 && (year % 100 != 0 || year % 400 == 0) ? 29 : 28)
            : month is 4 or 6 or 9 or 11 ? 30 : 31;

    /// <summary>Monday 0 to Sunday 6, of a day since the epoch, which was a Thursday.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 zic compile via Broiler.VM TzdbCompiler; IP=Low; Security=Low; Resources=0; Fingerprint=8A80C4
    // Broiler-Human:        PENDING
    private static int DayOfWeek(long days) => (int)(((days + 3) % 7 + 7) % 7);

    /// <summary>The day, since the epoch, an ON field names in a year and month.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 zic compile via Broiler.VM TzdbCompiler; IP=Low; Security=Medium; Resources=1; Fingerprint=D85065
    // Broiler-Human:        PENDING
    internal static long OnDay(long year, int month, string on)
    {
        if (on.StartsWith("last", StringComparison.Ordinal))
        {
            var weekday = Weekday(on[4..]);
            var day = DaysFromCivil(year, month, MonthLength(year, month));

            while (DayOfWeek(day) != weekday)
            {
                day--;
            }

            return day;
        }

        var after = on.IndexOf(">=", StringComparison.Ordinal);
        var before = on.IndexOf("<=", StringComparison.Ordinal);

        if (after > 0 || before > 0)
        {
            var at = after > 0 ? after : before;
            var weekday = Weekday(on[..at]);
            var day = DaysFromCivil(year, month, Integer(on[(at + 2)..]));

            while (DayOfWeek(day) != weekday)
            {
                day += after > 0 ? 1 : -1;
            }

            return day;
        }

        return DaysFromCivil(year, month, Integer(on));
    }

    /// <summary>The year an instant falls in, by the mean Gregorian year, as the compiler and its readers decide when the recurring rules apply.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 zic compile via Broiler.VM TzdbCompiler; IP=Low; Security=Low; Resources=0; Fingerprint=951EC2
    // Broiler-Human:        PENDING
    internal static int YearOf(long seconds) => 1970 + (int)Math.Floor(seconds / 31556952.0);

    /// <summary>An UNTIL field's local instant, in seconds, and how it is read.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 zic compile via Broiler.VM TzdbCompiler; IP=Low; Security=Medium; Resources=1; Fingerprint=34CE0A
    // Broiler-Human:        PENDING
    private static (long Local, char Kind) UntilLocal(IReadOnlyList<string> until)
    {
        var year = Integer(until[0]);
        var month = until.Count > 1 ? Month(until[1]) : 1;
        var day = until.Count > 2 ? OnDay(year, month, until[2]) : DaysFromCivil(year, month, 1);
        var (time, kind) = until.Count > 3 ? Time(until[3]) : (0, 'w');
        return ((day * 86400) + time, kind);
    }

    /// <summary>A local instant as universal time, by how it is read.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 zic compile via Broiler.VM TzdbCompiler; IP=Low; Security=Low; Resources=0; Fingerprint=93CA82
    // Broiler-Human:        PENDING
    private static long ToUniversal((long Local, char Kind) at, int standard, int save) =>
        at.Kind switch
        {
            'u' => at.Local,
            's' => at.Local - standard,
            _ => at.Local - standard - save,
        };

    /// <summary>Each rule's occurrence in the years given, in chronological order of local time.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 zic compile via Broiler.VM TzdbCompiler; IP=Low; Security=Medium; Resources=4; Fingerprint=FF803F
    // Broiler-Human:        PENDING
    internal static List<(long Local, char Kind, int Save)> Events(IEnumerable<TzdbRule> rules, long firstYear, long lastYear)
    {
        var events = new List<(long Local, char Kind, int Save)>();

        for (var year = firstYear; year <= lastYear; year++)
        {
            foreach (var rule in rules)
            {
                if (rule.From <= year && (rule.To is null || year <= rule.To))
                {
                    events.Add(((OnDay(year, rule.Month, rule.On) * 86400) + rule.At, rule.AtKind, rule.Save));
                }
            }
        }

        // STABLE, so two rules at one local time keep the order the file gives them.
        return [.. events.Select(static (e, i) => (e, i)).OrderBy(static p => p.e.Local).ThenBy(static p => p.i).Select(static p => p.e)];
    }

    /// <summary>Compiles one zone's lines against the rule sets.</summary>
    /// <param name="lines">The zone's lines, in the order the source gives them.</param>
    /// <param name="rules">Every rule set, by name.</param>
    /// <exception cref="InvalidDataException">The zone has no lines.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 zic compile via Broiler.VM TzdbCompiler; IP=Low; Security=Medium; Resources=5; Fingerprint=15DD73
    // Broiler-Human:        PENDING
    public static TzdbZone Compile(IReadOnlyList<TzdbZoneLine> lines, IReadOnlyDictionary<string, IReadOnlyList<TzdbRule>> rules)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(rules);

        var raw = new List<(long At, int Offset, bool Daylight, bool Named)>();
        int? initial = null;
        long? start = null;
        TzdbFinalRules? final = null;
        var fixedForever = false;

        void Emit(long at, int offset, int standard, bool named)
        {
            if (initial is null)
            {
                initial = offset;
                return;
            }

            raw.Add((at, offset, offset != standard, named));
        }

        foreach (var line in lines)
        {
            var standard = line.StandardOffset;
            var last = line.Until.Count == 0;

            if (line.Rules == "-" || char.IsAsciiDigit(line.Rules[0]) || line.Rules[0] == '-')
            {
                var save = line.Rules == "-" ? 0 : Time(line.Rules).Seconds;
                Emit(start ?? long.MinValue, standard + save, standard, save > 0);

                if (last)
                {
                    fixedForever = true;
                    break;
                }

                start = ToUniversal(UntilLocal(line.Until), standard, save);
                continue;
            }

            var set = rules[line.Rules];
            var firstYear = set.Min(static rule => rule.From);
            int lastYear;
            var cut = 0;

            if (last)
            {
                var finite = set.Max(static rule => rule.To ?? rule.From);
                cut = Math.Max(finite, start is { } from ? YearOf(from) : 0) + 1;
                lastYear = cut;
            }
            else
            {
                lastYear = YearOf(UntilLocal(line.Until).Local) + 1;
            }

            var events = Events(set, firstYear, lastYear);

            // THE REARGUARD DAYLIGHT FLAG: a negative save still to come on the line is its standard
            // time, and a larger save until then is daylight time (Ireland's summer, Namibia's until
            // 2017); a save with no negative one after it is daylight only where it is positive.
            // Only the line's own events count: one after its UNTIL is another line's.
            var recurringLeast = last ? set.Where(static rule => rule.To is null).Select(static rule => rule.Save).DefaultIfEmpty(0).Min() : 0;
            var least = new int[events.Count + 1];
            least[events.Count] = Math.Min(0, recurringLeast);
            var untilLocal = last ? long.MaxValue : UntilLocal(line.Until).Local;

            for (var i = events.Count - 1; i >= 0; i--)
            {
                least[i] = events[i].Local < untilLocal ? Math.Min(least[i + 1], events[i].Save) : least[i + 1];
            }

            // THE SAVE IN FORCE AT THE LINE'S START is the latest rule's at or before it, however
            // many years before, as zic's start offset is; with none, standard time.
            var current = 0;
            var next = 0;

            if (start is { } begin)
            {
                for (; next < events.Count; next++)
                {
                    if (ToUniversal((events[next].Local, events[next].Kind), standard, current) > begin)
                    {
                        break;
                    }

                    current = events[next].Save;
                }
            }

            Emit(start ?? long.MinValue, standard + current, standard, current > least[next]);

            for (; next < events.Count; next++)
            {
                var at = ToUniversal((events[next].Local, events[next].Kind), standard, current);

                if (!last && at >= ToUniversal(UntilLocal(line.Until), standard, current))
                {
                    break;
                }

                Emit(at, standard + events[next].Save, standard, events[next].Save > least[next]);
                current = events[next].Save;
            }

            if (last)
            {
                var recurring = set.Where(static rule => rule.To is null).ToArray();

                if (recurring.Length > 0)
                {
                    final = new TzdbFinalRules(standard, cut, Array.AsReadOnly(recurring));
                }
                else
                {
                    fixedForever = true;
                }

                break;
            }

            start = ToUniversal(UntilLocal(line.Until), standard, current);
        }

        // ZIC'S MERGE (writezone): a transition whose wall clock, in the offset before it, is not
        // after the previous transition's wall clock, in the offset before that, gives the previous
        // transition its type and is dropped; and a transition to the type already in force is no
        // transition.
        var first = initial ?? throw new InvalidDataException("a zone without lines");
        var merged = new List<(long At, int Offset, bool Daylight, bool Named)>();

        foreach (var entry in raw.Select(static (e, i) => (e, i)).OrderBy(static p => p.e.At).ThenBy(static p => p.i).Select(static p => p.e))
        {
            if (merged.Count > 0)
            {
                var before = merged[^1].Offset;
                var beforePrevious = merged.Count > 1 ? merged[^2].Offset : first;

                if (entry.At + before <= merged[^1].At + beforePrevious)
                {
                    merged[^1] = (merged[^1].At, entry.Offset, entry.Daylight, entry.Named);
                    continue;
                }
            }

            var previous = merged.Count > 0 ? (merged[^1].Offset, merged[^1].Daylight) : (first, false);

            if ((entry.Offset, entry.Daylight) != previous)
            {
                merged.Add(entry);
            }
        }

        var transitions = new List<TzdbTransition>();

        // A CHANGE OF OFFSET OR OF THE NAMED DAYLIGHT FLAG is kept: a zone that moves to standard time
        // at its daylight offset (America/Chihuahua in 2022) is named for standard time after it.
        foreach (var (at, offset, _, named) in merged)
        {
            if ((offset, named) != (transitions.Count > 0 ? (transitions[^1].Offset, transitions[^1].Daylight) : (first, false)))
            {
                transitions.Add(new TzdbTransition(at, offset, named));
            }
        }

        return new TzdbZone(first, transitions.AsReadOnly(), fixedForever ? null : final);
    }

    /// <summary>
    /// A compiled zone's offset at an instant: the listed transitions, then the recurring rules, the way a
    /// reader of the generated table computes it; the generator's own check of what it encodes.
    /// </summary>
    /// <param name="zone">The compiled zone.</param>
    /// <param name="at">The instant, in seconds since the epoch.</param>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 zic compile via Broiler.VM TzdbCompiler; IP=Low; Security=Medium; Resources=2; Fingerprint=353C12
    // Broiler-Human:        PENDING
    public static int OffsetAt(TzdbZone zone, long at)
    {
        ArgumentNullException.ThrowIfNull(zone);

        if (zone.Final is { } final && YearOf(at) > final.LastListedYear)
        {
            return RecurringOffsetAt(final, at);
        }

        var low = 0;
        var high = zone.Transitions.Count;

        while (low < high)
        {
            var middle = (low + high) / 2;

            if (zone.Transitions[middle].At <= at)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low == 0 ? zone.InitialOffset : zone.Transitions[low - 1].Offset;
    }

    /// <summary>The offset the recurring rules give in a year after the listed ones.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 zic compile via Broiler.VM TzdbCompiler; IP=Low; Security=Medium; Resources=2; Fingerprint=324A9F
    // Broiler-Human:        PENDING
    internal static int RecurringOffsetAt(TzdbFinalRules final, long at)
    {
        var year = YearOf(at);
        var save = Events(final.Rules, year - 2, year - 2)[^1].Save;
        var offset = final.StandardOffset + save;

        foreach (var (local, kind, next) in Events(final.Rules, year - 1, year + 1))
        {
            if (ToUniversal((local, kind), final.StandardOffset, save) > at)
            {
                break;
            }

            offset = final.StandardOffset + next;
            save = next;
        }

        return offset;
    }
}
