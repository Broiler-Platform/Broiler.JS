// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   29
// Annotated:        29/29
// Exempt:           3
// Human-reviewed:   0/29
// IP risk:          Low
// Security risk:    High
// Criteria:         2/2
// Resource impact:  5/10 max
// Unverified:       29
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Broiler.ECMAScript.Generation.Time;

/// <summary>
/// The calendar year table, as <see cref="CalendarTableGenerator.Generate"/> writes it: the bytes an engine
/// embeds, whatever file it embeds them in.
/// </summary>
/// <remarks>
/// <b>The layout</b> is Broiler.VM's, which its <c>JsCalendarTables</c> holds at Broiler.VM <c>9508bd5</c>
/// byte for byte (Broiler.VM's decision JSD-0056). All numbers are little-endian. Five sections follow one
/// another: the Chinese years (1912 to 2102), the Korean years (1912 to 2102), the Qing-era years both
/// calendars share (1900 to 1911), the Umm al-Qura years (1300 to 1600 AH), and the Persian corrections.
/// <list type="bullet">
/// <item>An East Asian section is its first related ISO year and its count of years, 16 bits each, then
/// three bytes per year: bits 0 to 12 each month's length, set for 30 days; bits 13 to 16 the leap month,
/// 0 for none, otherwise the leap month's ordinal (2 to 13); bits 17 to 22 the new year's distance in days
/// from 19 January of the related ISO year.</item>
/// <item>The Umm al-Qura section is its first year and count, then 16 bits per year: bits 0 to 11 each
/// month's length, set for 30 days, and bits 12 to 15 the new year's distance in days from the tabular
/// Islamic (civil) new year, plus 8.</item>
/// <item>The Persian section is a count, then each year, 16 bits, from 1502 AP, that the 33-year rule makes
/// leap and the astronomical calendar does not, ascending.</item>
/// </list>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar tables via Broiler.VM CalendarTableGenerator; IP=Low; Security=Low; Resources=1; Fingerprint=80D336
// Broiler-Human:        PENDING
public sealed class CalendarTables
{
    /// <summary>Holds the table the generator wrote.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar tables via Broiler.VM CalendarTableGenerator; IP=Low; Security=Low; Resources=1; Fingerprint=6A4064
    // Broiler-Human:        PENDING
    internal CalendarTables(byte[] calendars)
    {
        Calendars = calendars;
    }

    /// <summary>The Chinese, Korean and Qing years, the Umm al-Qura years, and the Persian calendar's non-leap corrections.</summary>
    public ReadOnlyMemory<byte> Calendars { get; }
}

/// <summary>
/// Writes the calendar year table from the crate members a <see cref="CalendarPin"/> names: one function,
/// pure and deterministic.
/// </summary>
/// <remarks>
/// <para>
/// <b>PORTED from Broiler.VM's <c>CalendarTableGenerator</c></b> (its rule N32; Broiler.VM's JSD-0076
/// moves it to the data home), its data half with its behaviour unchanged: the table is the bytes it
/// writes into <c>JsCalendarTables</c>. Its source half is <see cref="CalendarTableSource"/>.
/// </para>
/// <para>
/// <b>It reads Rust source as data, and nothing else of it.</b> ICU4X states each published year as one
/// constructor call - <c>PackedEastAsianTraditionalYearData::new(year, [month lengths], leap month,
/// gregorian(new year))</c> and <c>PackedHijriYearData::try_new(year, [month lengths], gregorian(new
/// year)).unwrap()</c> - and the Persian corrections as one array literal. Broiler.VM matched each with a
/// regular expression; this port matches the same text by hand, character for character (convention 9
/// admits no regular expression), over the pinned sources, which are ASCII. Each table's years must run on
/// from its <c>STARTING_YEAR</c> without a gap, and each new year must fit the layout
/// <see cref="CalendarTables"/> states, or nothing is written.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar tables via Broiler.VM CalendarTableGenerator; IP=Low; Security=High; Resources=5; Fingerprint=CDF266
// Broiler-Falsified-If: the table is written from a crate member that failed its pin's check, or a year that does not follow the one before it, or whose new year falls outside the layout, is written instead of refused
// Broiler-Human:        PENDING
public static class CalendarTableGenerator
{
    /// <summary>ICU4X's Chinese years, 1912 to 2102.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar tables via Broiler.VM CalendarTableGenerator; IP=Low; Security=Low; Resources=0; Fingerprint=0D1A26
    // Broiler-Human:        PENDING
    public const string China = "icu_calendar-2.3.0/src/cal/east_asian_traditional/china_data.rs";

    /// <summary>ICU4X's Korean years, 1912 to 2102.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar tables via Broiler.VM CalendarTableGenerator; IP=Low; Security=Low; Resources=0; Fingerprint=C038BE
    // Broiler-Human:        PENDING
    public const string Korea = "icu_calendar-2.3.0/src/cal/east_asian_traditional/korea_data.rs";

    /// <summary>ICU4X's Qing-era years, 1900 to 1911, which the Chinese and Korean calendars share.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar tables via Broiler.VM CalendarTableGenerator; IP=Low; Security=Low; Resources=0; Fingerprint=362332
    // Broiler-Human:        PENDING
    public const string Qing = "icu_calendar-2.3.0/src/cal/east_asian_traditional/qing_data.rs";

    /// <summary>ICU4X's Umm al-Qura years, 1300 to 1600 AH.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar tables via Broiler.VM CalendarTableGenerator; IP=Low; Security=Low; Resources=0; Fingerprint=EAF8EB
    // Broiler-Human:        PENDING
    public const string UmmAlQura = "icu_calendar-2.3.0/src/cal/hijri/ummalqura_data.rs";

    /// <summary><c>calendrical_calculations</c>' Persian calendar, whose <c>NON_LEAP_CORRECTION</c> array the table carries.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar tables via Broiler.VM CalendarTableGenerator; IP=Low; Security=Low; Resources=0; Fingerprint=FCA8AC
    // Broiler-Human:        PENDING
    public const string Persian = "calendrical_calculations-0.2.4/src/persian.rs";

    /// <summary>R.D. 227015, the civil Hijri epoch, in days since 1970-01-01.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar tables via Broiler.VM CalendarTableGenerator; IP=Low; Security=Low; Resources=0; Fingerprint=F561B8
    // Broiler-Human:        PENDING
    private const long CivilHijriEpoch = 227015 - 719163;

    /// <summary>
    /// The calendar table, from the verified crate members. Throws when they are not the pinned ones or a
    /// year table does not fit the layout; nothing is generated from input that failed a check.
    /// </summary>
    /// <param name="pin">The calendar pin.</param>
    /// <param name="members">The crate members, as <see cref="CalendarPin.Extract"/> answers them.</param>
    /// <exception cref="InvalidDataException">The members are not the pinned ones, or a year table is malformed or does not fit the layout.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar tables via Broiler.VM CalendarTableGenerator; IP=Low; Security=High; Resources=5; Fingerprint=5E7CCA
    // Broiler-Falsified-If: a table is answered for a crate member whose length or hash differs from the pin
    // Broiler-Human:        PENDING
    public static CalendarTables Generate(CalendarPin pin, IReadOnlyDictionary<string, byte[]> members)
    {
        ArgumentNullException.ThrowIfNull(pin);

        pin.Verified(members);

        var bytes = new List<byte>();
        EastAsianSection(bytes, EastAsianYears(CalendarPin.Decode(members[China]), China));
        EastAsianSection(bytes, EastAsianYears(CalendarPin.Decode(members[Korea]), Korea));
        EastAsianSection(bytes, EastAsianYears(CalendarPin.Decode(members[Qing]), Qing));
        UmmAlQuraSection(bytes, HijriYears(CalendarPin.Decode(members[UmmAlQura])));

        var corrections = PersianCorrections(CalendarPin.Decode(members[Persian]));
        U16(bytes, corrections.Count);

        foreach (var year in corrections)
        {
            U16(bytes, year);
        }

        return new CalendarTables([.. bytes]);
    }

    /// <summary>One published East Asian year: its related ISO year, month lengths, leap month and new year.</summary>
    /// <param name="Year">The related ISO year.</param>
    /// <param name="Long">Each of the thirteen months' lengths, true for 30 days.</param>
    /// <param name="LeapMonth">The leap month's ordinal, or 0 for none.</param>
    /// <param name="NewYear">The new year, in days since 1970-01-01.</param>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar tables via Broiler.VM CalendarTableGenerator; IP=Low; Security=Low; Resources=1; Fingerprint=33F77C
    // Broiler-Human:        PENDING
    internal sealed record EastAsianYear(int Year, bool[] Long, int LeapMonth, long NewYear);

    /// <summary>One published Umm al-Qura year: its year, month lengths and new year.</summary>
    /// <param name="Year">The Hijri year.</param>
    /// <param name="Long">Each of the twelve months' lengths, true for 30 days.</param>
    /// <param name="NewYear">The new year, in days since 1970-01-01.</param>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar tables via Broiler.VM CalendarTableGenerator; IP=Low; Security=Low; Resources=1; Fingerprint=8C51B2
    // Broiler-Human:        PENDING
    internal sealed record HijriYear(int Year, bool[] Long, long NewYear);

    /// <summary>The years one East Asian table states, checked to run on from its starting year.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar tables via Broiler.VM CalendarTableGenerator; IP=Low; Security=Medium; Resources=4; Fingerprint=4C77D6
    // Broiler-Human:        PENDING
    internal static IReadOnlyList<EastAsianYear> EastAsianYears(string source, string file)
    {
        var start = StartingYear(source, file);
        var years = new List<EastAsianYear>();

        foreach (var match in Matches(source, "PackedEastAsianTraditionalYearData::new(", EastAsianRow))
        {
            var year = Int(match[0]);
            var lengths = Lengths(match[1], 13, file);
            var leap = match[2].Length > 0 ? Int(match[2]) : 0;
            var newYear = TzdbCompiler.DaysFromCivil(Int(match[3]), Int(match[4]), Int(match[5]));

            if (year != start + years.Count)
            {
                throw new InvalidDataException(string.Create(NumberFormatInfo.InvariantInfo, $"{file}: the year {year} does not follow {start + years.Count - 1}"));
            }

            if (leap is 1 or > 13 || (leap == 0 && lengths[12]))
            {
                throw new InvalidDataException(string.Create(NumberFormatInfo.InvariantInfo, $"{file}: the year {year} has a leap month {leap} the layout cannot state"));
            }

            years.Add(new EastAsianYear(year, lengths, leap, newYear));
        }

        if (years.Count == 0)
        {
            throw new InvalidDataException(file + " states no years");
        }

        return years;
    }

    /// <summary>The Umm al-Qura years, checked to run on from the table's starting year.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar tables via Broiler.VM CalendarTableGenerator; IP=Low; Security=Medium; Resources=4; Fingerprint=8F1023
    // Broiler-Human:        PENDING
    internal static IReadOnlyList<HijriYear> HijriYears(string source)
    {
        var start = StartingYear(source, UmmAlQura);
        var years = new List<HijriYear>();

        foreach (var match in Matches(source, "PackedHijriYearData::try_new(", HijriRow))
        {
            var year = Int(match[0]);

            if (year != start + years.Count)
            {
                throw new InvalidDataException(string.Create(NumberFormatInfo.InvariantInfo, $"{UmmAlQura}: the year {year} does not follow {start + years.Count - 1}"));
            }

            years.Add(new HijriYear(
                year,
                Lengths(match[1], 12, UmmAlQura),
                TzdbCompiler.DaysFromCivil(Int(match[2]), Int(match[3]), Int(match[4]))));
        }

        if (years.Count == 0)
        {
            throw new InvalidDataException(UmmAlQura + " states no years");
        }

        return years;
    }

    /// <summary>The Persian years the 33-year rule makes leap and are not, ascending.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar tables via Broiler.VM CalendarTableGenerator; IP=Low; Security=Medium; Resources=4; Fingerprint=DF16C3
    // Broiler-Human:        PENDING
    internal static IReadOnlyList<int> PersianCorrections(string source)
    {
        var match = Matches(source, "const NON_LEAP_CORRECTION: [i32; ", PersianTable).FirstOrDefault()
            ?? throw new InvalidDataException(Persian + " has no NON_LEAP_CORRECTION array");

        var years = match[1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(Int).ToList();

        if (years.Count != Int(match[0]) || years.Zip(years.Skip(1)).Any(static pair => pair.First >= pair.Second))
        {
            throw new InvalidDataException(Persian + ": NON_LEAP_CORRECTION is not " + match[0] + " ascending years");
        }

        return years;
    }

    /// <summary>Appends one East Asian section: its first year and count, then three bytes per year.</summary>
    /// <exception cref="InvalidDataException">A new year falls outside the layout's 64 days from 19 January.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar tables via Broiler.VM CalendarTableGenerator; IP=Low; Security=Medium; Resources=3; Fingerprint=657256
    // Broiler-Human:        PENDING
    private static void EastAsianSection(List<byte> bytes, IReadOnlyList<EastAsianYear> years)
    {
        U16(bytes, years[0].Year);
        U16(bytes, years.Count);

        foreach (var year in years)
        {
            var offset = year.NewYear - TzdbCompiler.DaysFromCivil(year.Year, 1, 19);

            if (offset is < 0 or > 63)
            {
                throw new InvalidDataException(string.Create(NumberFormatInfo.InvariantInfo, $"the new year of {year.Year} is {offset} days from 19 January, outside the layout"));
            }

            var packed = 0;

            for (var month = 0; month < 13; month++)
            {
                packed |= year.Long[month] ? 1 << month : 0;
            }

            packed |= year.LeapMonth << 13;
            packed |= (int)offset << 17;
            bytes.Add((byte)packed);
            bytes.Add((byte)(packed >> 8));
            bytes.Add((byte)(packed >> 16));
        }
    }

    /// <summary>Appends the Umm al-Qura section: its first year and count, then 16 bits per year.</summary>
    /// <exception cref="InvalidDataException">A new year is more than five days from the tabular one.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar tables via Broiler.VM CalendarTableGenerator; IP=Low; Security=Medium; Resources=3; Fingerprint=D3F8EF
    // Broiler-Human:        PENDING
    private static void UmmAlQuraSection(List<byte> bytes, IReadOnlyList<HijriYear> years)
    {
        U16(bytes, years[0].Year);
        U16(bytes, years.Count);

        foreach (var year in years)
        {
            var tabular = CivilHijriEpoch + ((year.Year - 1L) * 354) + Math.Floor((3 + (11.0 * year.Year)) / 30);
            var offset = year.NewYear - (long)tabular;

            if (offset is < -5 or > 5)
            {
                throw new InvalidDataException(string.Create(NumberFormatInfo.InvariantInfo, $"the Umm al-Qura new year of {year.Year} is {offset} days from the tabular one, outside the layout"));
            }

            var packed = 0;

            for (var month = 0; month < 12; month++)
            {
                packed |= year.Long[month] ? 1 << month : 0;
            }

            packed |= (int)(offset + 8) << 12;
            U16(bytes, packed);
        }
    }

    /// <summary>A table's <c>pub const STARTING_YEAR: i32 = N;</c>.</summary>
    /// <exception cref="InvalidDataException">The source states no starting year.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar tables via Broiler.VM CalendarTableGenerator; IP=Low; Security=Medium; Resources=3; Fingerprint=40324F
    // Broiler-Human:        PENDING
    private static int StartingYear(string source, string file) =>
        Matches(source, "pub const STARTING_YEAR: i32 = ", StartingYearLine).FirstOrDefault() is { } match
            ? Int(match[0])
            : throw new InvalidDataException(file + " states no STARTING_YEAR");

    /// <summary>A year's month lengths, <c>l</c> long and <c>s</c> short, separated by commas.</summary>
    /// <exception cref="InvalidDataException">A length is neither, or the year states another number of months.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar tables via Broiler.VM CalendarTableGenerator; IP=Low; Security=Medium; Resources=2; Fingerprint=316193
    // Broiler-Human:        PENDING
    private static bool[] Lengths(string list, int count, string file)
    {
        var lengths = list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(length => length switch
            {
                "l" => true,
                "s" => false,
                _ => throw new InvalidDataException(file + ": `" + length + "` is not a month length"),
            })
            .ToArray();

        return lengths.Length == count
            ? lengths
            : throw new InvalidDataException(string.Create(NumberFormatInfo.InvariantInfo, $"{file}: a year states {lengths.Length} month lengths, not {count}"));
    }

    /// <summary>Appends a 16-bit value, little-endian.</summary>
    /// <exception cref="InvalidDataException">The value does not fit 16 bits.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar tables via Broiler.VM CalendarTableGenerator; IP=Low; Security=Low; Resources=1; Fingerprint=CE8BD0
    // Broiler-Human:        PENDING
    private static void U16(List<byte> bytes, int value)
    {
        if (value is < 0 or > 0xFFFF)
        {
            throw new InvalidDataException(string.Create(NumberFormatInfo.InvariantInfo, $"{value} does not fit the table's 16 bits"));
        }

        bytes.Add((byte)value);
        bytes.Add((byte)(value >> 8));
    }

    /// <summary>A decimal integer with an optional leading sign, read through the invariant format.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar tables via Broiler.VM CalendarTableGenerator; IP=Low; Security=Medium; Resources=1; Fingerprint=AA26C9
    // Broiler-Human:        PENDING
    private static int Int(string text) => int.Parse(text, NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo);

    /// <summary>
    /// Every match of one row's shape in <paramref name="source"/>, left to right and not overlapping, as
    /// a regular expression's matches are: each occurrence of <paramref name="prefix"/> where
    /// <paramref name="rest"/> reads the rest of the row, its groups in order.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0056 calendar tables; IP=Low; Security=Medium; Resources=4; Fingerprint=31C8C6
    // Broiler-Human:        PENDING
    private static IEnumerable<string[]> Matches(string source, string prefix, Func<RowReader, string[]?> rest)
    {
        for (var at = source.IndexOf(prefix, StringComparison.Ordinal); at >= 0; at = source.IndexOf(prefix, at, StringComparison.Ordinal))
        {
            var reader = new RowReader(source, at + prefix.Length);

            if (rest(reader) is { } groups)
            {
                yield return groups;
                at = reader.Position;
            }
            else
            {
                at++;
            }
        }
    }

    /// <summary>
    /// The rest of <c>PackedEastAsianTraditionalYearData::new\((\d+), \[([ls, ]+)\], (None|Some\((\d+)\)),
    /// gregorian\((\d+), (\d+), (\d+)\)\)</c>: the year, the lengths, the leap month or the empty string, and
    /// the new year's year, month and day.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0056 calendar tables; IP=Low; Security=Medium; Resources=2; Fingerprint=D5516E
    // Broiler-Human:        PENDING
    private static string[]? EastAsianRow(RowReader row)
    {
        if (row.Digits() is not { } year || !row.Literal(", [") || row.Run("ls, ") is not { } lengths || !row.Literal("], "))
        {
            return null;
        }

        var leap = string.Empty;

        if (!row.Literal("None"))
        {
            if (!row.Literal("Some(") || row.Digits() is not { } month || !row.Literal(")"))
            {
                return null;
            }

            leap = month;
        }

        return row.Literal(", gregorian(") && row.Digits() is { } y && row.Literal(", ") && row.Digits() is { } m && row.Literal(", ")
            && row.Digits() is { } d && row.Literal("))")
            ? [year, lengths, leap, y, m, d]
            : null;
    }

    /// <summary>
    /// The rest of <c>PackedHijriYearData::try_new\((\d+), \[([ls, ]+)\], gregorian\((\d+), (\d+),
    /// (\d+)\)\)\.unwrap\(\)</c>: the year, the lengths, and the new year's year, month and day.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0056 calendar tables; IP=Low; Security=Medium; Resources=2; Fingerprint=09BA8C
    // Broiler-Human:        PENDING
    private static string[]? HijriRow(RowReader row) =>
        row.Digits() is { } year && row.Literal(", [") && row.Run("ls, ") is { } lengths && row.Literal("], gregorian(")
        && row.Digits() is { } y && row.Literal(", ") && row.Digits() is { } m && row.Literal(", ") && row.Digits() is { } d
        && row.Literal(")).unwrap()")
            ? [year, lengths, y, m, d]
            : null;

    /// <summary>The rest of <c>const NON_LEAP_CORRECTION: \[i32; (\d+)\] = \[([0-9,\s]+)\];</c>: the count and the list.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0056 calendar tables; IP=Low; Security=Medium; Resources=2; Fingerprint=8567C5
    // Broiler-Human:        PENDING
    private static string[]? PersianTable(RowReader row) =>
        row.Digits() is { } count && row.Literal("] = [") && row.Run("0123456789, \t\n\v\f\r") is { } list && row.Literal("];")
            ? [count, list]
            : null;

    /// <summary>The rest of <c>pub const STARTING_YEAR: i32 = (\d+);</c>: the year.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0056 calendar tables; IP=Low; Security=Medium; Resources=1; Fingerprint=242DFF
    // Broiler-Human:        PENDING
    private static string[]? StartingYearLine(RowReader row) =>
        row.Digits() is { } year && row.Literal(";") ? [year] : null;

    /// <summary>
    /// A cursor over one row of Rust source: each read consumes what it matches, greedily, and answers null
    /// or false, consuming nothing more, where the text does not match. ASCII digits only, as the pinned
    /// sources write them.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0056 calendar tables; IP=Low; Security=Medium; Resources=2; Fingerprint=CC5D67
    // Broiler-Human:        PENDING
    private sealed class RowReader(string source, int position)
    {
        /// <summary>Where the next read starts.</summary>
        // Broiler-AI:           Origin=AI; Spec=JSD-0056 calendar tables; IP=Low; Security=Low; Resources=0; Fingerprint=AC61E8
        // Broiler-Human:        PENDING
        internal int Position { get; private set; } = position;

        /// <summary>Consumes <paramref name="text"/> where it is next.</summary>
        // Broiler-AI:           Origin=AI; Spec=JSD-0056 calendar tables; IP=Low; Security=Medium; Resources=1; Fingerprint=742D5C
        // Broiler-Human:        PENDING
        internal bool Literal(string text)
        {
            if (!source.AsSpan(Position).StartsWith(text, StringComparison.Ordinal))
            {
                return false;
            }

            Position += text.Length;
            return true;
        }

        /// <summary>Consumes one or more ASCII digits.</summary>
        // Broiler-AI:           Origin=AI; Spec=JSD-0056 calendar tables; IP=Low; Security=Medium; Resources=1; Fingerprint=BBE62F
        // Broiler-Human:        PENDING
        internal string? Digits() => Run("0123456789");

        /// <summary>Consumes one or more characters of <paramref name="set"/>.</summary>
        // Broiler-AI:           Origin=AI; Spec=JSD-0056 calendar tables; IP=Low; Security=Medium; Resources=2; Fingerprint=263DD5
        // Broiler-Human:        PENDING
        internal string? Run(string set)
        {
            var end = Position;

            while (end < source.Length && set.Contains(source[end], StringComparison.Ordinal))
            {
                end++;
            }

            if (end == Position)
            {
                return null;
            }

            var run = source[Position..end];
            Position = end;
            return run;
        }
    }
}
