// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   68
// Annotated:        68/68
// Exempt:           15
// Human-reviewed:   0/68
// IP risk:          Low
// Security risk:    High
// Criteria:         3/3
// Resource impact:  2/10 max
// Unverified:       68
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace Broiler.ECMAScript.Intl;

/// <summary>
/// The pattern a skeleton asks for in one language, chosen the way ICU's DateTimePatternGenerator
/// chooses it (JSD-0045): ECMA-402's BestFitFormatMatcher is implementation-defined, and ICU's is the
/// one every engine's answers come from.
/// </summary>
/// <remarks>
/// <para>
/// <b>The generator holds the language's patterns as ICU holds them</b>: one per field letter, plus
/// the language's date and time styles and its available formats, each keyed by its skeleton, in the
/// order ICU adds them. A skeleton is matched by the distance between the fields' types, a missing
/// field costing more than a different width and an extra one more than a missing one. What is left
/// over is appended by the language's append items, and a date half and a time half are joined by
/// the language's date-time pattern for the width the month asks for.
/// </para>
/// <para>
/// <b>Field widths are adjusted as ICU adjusts them</b>: to the skeleton's, but for a numeric minute
/// or second, whose width the pattern keeps, and for an hour only when the caller asks.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=05ED9C
// Broiler-Human:        PENDING
internal sealed class DateTimePatternGenerator
{
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=D48789
    // Broiler-Human:        PENDING
    internal const int Era = 0;
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=6322C7
    // Broiler-Human:        PENDING
    internal const int Year = 1;
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=B40D99
    // Broiler-Human:        PENDING
    internal const int Quarter = 2;
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=9370C0
    // Broiler-Human:        PENDING
    internal const int Month = 3;
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=404AE2
    // Broiler-Human:        PENDING
    internal const int WeekOfYear = 4;
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=3D19F6
    // Broiler-Human:        PENDING
    internal const int WeekOfMonth = 5;
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=0582C9
    // Broiler-Human:        PENDING
    internal const int Weekday = 6;
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=002C5F
    // Broiler-Human:        PENDING
    internal const int DayOfYear = 7;
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=D962B2
    // Broiler-Human:        PENDING
    internal const int DayOfWeekInMonth = 8;
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=F5CEDE
    // Broiler-Human:        PENDING
    internal const int Day = 9;
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=BAAE41
    // Broiler-Human:        PENDING
    internal const int DayPeriod = 10;
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=0B0998
    // Broiler-Human:        PENDING
    internal const int Hour = 11;
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=FC736B
    // Broiler-Human:        PENDING
    internal const int Minute = 12;
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=8C256D
    // Broiler-Human:        PENDING
    internal const int Second = 13;
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=9A5D8C
    // Broiler-Human:        PENDING
    internal const int FractionalSecond = 14;
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=F2D3E4
    // Broiler-Human:        PENDING
    internal const int Zone = 15;
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=F4119A
    // Broiler-Human:        PENDING
    internal const int FieldCount = 16;

    /// <summary>ICU's <c>UDATPG_MATCH_HOUR_FIELD_LENGTH</c>: the hour keeps the skeleton's width.</summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=971A3B
    // Broiler-Human:        PENDING
    internal const int MatchHourLength = 1 << Hour;

    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=B28560
    // Broiler-Human:        PENDING
    private const int MatchMinuteLength = 1 << Minute;
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=B0060D
    // Broiler-Human:        PENDING
    private const int MatchSecondLength = 1 << Second;

    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=25D536
    // Broiler-Human:        PENDING
    private const int Narrow = -0x101;
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=4754DD
    // Broiler-Human:        PENDING
    private const int Shorter = -0x102;
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=ABF2A9
    // Broiler-Human:        PENDING
    private const int Short = -0x103;
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=935270
    // Broiler-Human:        PENDING
    private const int Long = -0x104;
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=242FEC
    // Broiler-Human:        PENDING
    private const int Numeric = 0x100;
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=C47522
    // Broiler-Human:        PENDING
    private const int Delta = 0x10;

    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=98877B
    // Broiler-Human:        PENDING
    private const int ExtraField = 0x10000;
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=F435B9
    // Broiler-Human:        PENDING
    private const int MissingField = 0x1000;

    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=EBB30F
    // Broiler-Human:        PENDING
    private const int UsesCapJ = 1;
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=CF31DA
    // Broiler-Human:        PENDING
    private const int FixFractionalSeconds = 2;

    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=C6BD54
    // Broiler-Human:        PENDING
    private const int DateMask = (1 << DayPeriod) - 1;
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=0A200C
    // Broiler-Human:        PENDING
    private const int TimeMask = ((1 << FieldCount) - 1) & ~DateMask;
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=A756FD
    // Broiler-Human:        PENDING
    private const int FractionalMask = 1 << FractionalSecond;
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=F855F2
    // Broiler-Human:        PENDING
    private const int SecondAndFractionalMask = (1 << Second) | (1 << FractionalSecond);

    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=0F2409
    // Broiler-Human:        PENDING
    private readonly record struct Row(char Char, int Field, int Type, int MinLength);

    /// <summary>ICU's field table: each pattern letter's field, type and least width.</summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=3452F7
    // Broiler-Human:        PENDING
    private static readonly ImmutableArray<Row> Rows =
    [
        new('G', Era, Short, 1), new('G', Era, Long, 4), new('G', Era, Narrow, 5),
        new('y', Year, Numeric, 1), new('Y', Year, Numeric + Delta, 1), new('u', Year, Numeric + (2 * Delta), 1),
        new('r', Year, Numeric + (3 * Delta), 1),
        new('U', Year, Short, 1), new('U', Year, Long, 4), new('U', Year, Narrow, 5),
        new('Q', Quarter, Numeric, 1), new('Q', Quarter, Short, 3), new('Q', Quarter, Long, 4), new('Q', Quarter, Narrow, 5),
        new('q', Quarter, Numeric + Delta, 1), new('q', Quarter, Short - Delta, 3), new('q', Quarter, Long - Delta, 4),
        new('q', Quarter, Narrow - Delta, 5),
        new('M', Month, Numeric, 1), new('M', Month, Short, 3), new('M', Month, Long, 4), new('M', Month, Narrow, 5),
        new('L', Month, Numeric + Delta, 1), new('L', Month, Short - Delta, 3), new('L', Month, Long - Delta, 4),
        new('L', Month, Narrow - Delta, 5),
        new('l', Month, Numeric + Delta, 1),
        new('w', WeekOfYear, Numeric, 1),
        new('W', WeekOfMonth, Numeric, 1),
        new('E', Weekday, Short, 1), new('E', Weekday, Long, 4), new('E', Weekday, Narrow, 5), new('E', Weekday, Shorter, 6),
        new('c', Weekday, Numeric + (2 * Delta), 1), new('c', Weekday, Short - (2 * Delta), 3),
        new('c', Weekday, Long - (2 * Delta), 4), new('c', Weekday, Narrow - (2 * Delta), 5),
        new('c', Weekday, Shorter - (2 * Delta), 6),
        new('e', Weekday, Numeric + Delta, 1), new('e', Weekday, Short - Delta, 3), new('e', Weekday, Long - Delta, 4),
        new('e', Weekday, Narrow - Delta, 5), new('e', Weekday, Shorter - Delta, 6),
        new('d', Day, Numeric, 1), new('g', Day, Numeric + Delta, 1),
        new('D', DayOfYear, Numeric, 1),
        new('F', DayOfWeekInMonth, Numeric, 1),
        new('a', DayPeriod, Short, 1), new('a', DayPeriod, Long, 4), new('a', DayPeriod, Narrow, 5),
        new('b', DayPeriod, Short - Delta, 1), new('b', DayPeriod, Long - Delta, 4), new('b', DayPeriod, Narrow - Delta, 5),
        new('B', DayPeriod, Short - (3 * Delta), 1), new('B', DayPeriod, Long - (3 * Delta), 4),
        new('B', DayPeriod, Narrow - (3 * Delta), 5),
        new('H', Hour, Numeric + (10 * Delta), 1), new('k', Hour, Numeric + (11 * Delta), 1), new('h', Hour, Numeric, 1),
        new('K', Hour, Numeric + Delta, 1),
        new('J', Hour, Numeric + (5 * Delta), 1), new('j', Hour, Numeric + (6 * Delta), 1), new('C', Hour, Numeric + (7 * Delta), 1),
        new('m', Minute, Numeric, 1),
        new('s', Second, Numeric, 1), new('A', Second, Numeric + Delta, 1),
        new('S', FractionalSecond, Numeric, 1),
        new('v', Zone, Short - (2 * Delta), 1), new('v', Zone, Long - (2 * Delta), 4),
        new('z', Zone, Short, 1), new('z', Zone, Long, 4),
        new('Z', Zone, Narrow - Delta, 1), new('Z', Zone, Long - Delta, 4), new('Z', Zone, Short - Delta, 5),
        new('O', Zone, Short - (2 * Delta), 1), new('O', Zone, Long - (2 * Delta), 4),
        new('V', Zone, Short - Delta, 1), new('V', Zone, Long - Delta, 2), new('V', Zone, Long - 1 - Delta, 3),
        new('V', Zone, Long - 2 - Delta, 4),
        new('X', Zone, Narrow - Delta, 1), new('X', Zone, Short - Delta, 2), new('X', Zone, Long - Delta, 4),
        new('x', Zone, Narrow - Delta, 1), new('x', Zone, Short - Delta, 2), new('x', Zone, Long - Delta, 4),
    ];

    /// <summary>The append item each field takes, by CLDR's name; <c>*</c> takes ICU's default.</summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=BC4C77
    // Broiler-Human:        PENDING
    private static readonly ImmutableArray<string> AppendItems =
        ["Era", "Year", "Quarter", "Month", "Week", "*", "Day-Of-Week", "*", "*", "Day", "*", "Hour", "Minute", "Second", "*", "Timezone"];

    /// <summary>The date field whose display name an append item quotes.</summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=D74347
    // Broiler-Human:        PENDING
    private static readonly ImmutableArray<string> FieldNames =
        ["era", "year", "quarter", "month", "week", "weekOfMonth", "weekday", "dayOfYear", "weekdayOfMonth", "day", "dayperiod", "hour", "minute", "second", "*", "zone"];

    /// <summary>The fields of a skeleton or a pattern, as ICU's <c>PtnSkeleton</c> holds them.</summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=77ADE3
    // Broiler-Human:        PENDING
    private sealed class Skeleton
    {
        internal readonly int[] Type = new int[FieldCount];
        internal readonly char[] Chars = new char[FieldCount];
        internal readonly int[] Lengths = new int[FieldCount];
        internal readonly char[] BaseChars = new char[FieldCount];
        internal readonly int[] BaseLengths = new int[FieldCount];
        internal bool AddedDefaultDayPeriod;

        // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=7468AE
        // Broiler-Human:        PENDING
        internal string Original => Text(Chars, Lengths);

        // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=88E450
        // Broiler-Human:        PENDING
        internal string Base => Text(BaseChars, BaseLengths);

        // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=04C35E
        // Broiler-Human:        PENDING
        internal int FieldMask
        {
            get
            {
                var mask = 0;

                for (var index = 0; index < FieldCount; index++)
                {
                    if (Type[index] != 0)
                    {
                        mask |= 1 << index;
                    }
                }

                return mask;
            }
        }

        // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=1; Fingerprint=604C12
        // Broiler-Human:        PENDING
        private static string Text(char[] chars, int[] lengths)
        {
            var text = new StringBuilder();

            for (var field = 0; field < FieldCount; field++)
            {
                text.Append(chars[field], lengths[field]);
            }

            return text.ToString();
        }
    }

    /// <summary>A pattern the generator holds, as ICU's <c>PtnElem</c>.</summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=15D69B
    // Broiler-Human:        PENDING
    private sealed class Entry(string basePattern, Skeleton skeleton, string pattern, bool specified)
    {
        internal string Base { get; } = basePattern;

        internal Skeleton Skeleton { get; } = skeleton;

        internal string Pattern { get; set; } = pattern;

        internal bool Specified { get; set; } = specified;
    }

    private readonly List<Entry>?[] boot = new List<Entry>?[52];

    private readonly string[] appendFormats = new string[FieldCount];

    private readonly string[] fieldNames = new string[FieldCount];

    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=0; Fingerprint=355213
    // Broiler-Human:        PENDING
    private readonly Dictionary<string, string> glue = new(StringComparer.Ordinal);

    private readonly char defaultHourChar;

    private readonly string decimalSymbol;

    /// <summary>Makes the generator of <paramref name="language"/> as ICU initializes one.</summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Medium; Resources=2; Fingerprint=1BABFD
    // Broiler-Human:        PENDING
    internal DateTimePatternGenerator(DateData data, string language, char hourChar, string decimalSymbol)
    {
        defaultHourChar = hourChar;
        this.decimalSymbol = decimalSymbol;

        // THE CANONICAL ITEMS, THEN THE STYLES (TIMES FIRST), THEN THE AVAILABLE FORMATS, IN ICU'S
        // ORDER. Which of two equally distant patterns wins depends on the order they were added in.
        foreach (var item in "GyQMwWEDFdaHmsSv")
        {
            AddPattern(item.ToString(), null, overrideExisting: false);
        }

        foreach (var kind in new[] { "timeFormats", "dateFormats" })
        {
            foreach (var style in new[] { "full", "long", "medium", "short" })
            {
                if (data.Value(language, kind + "." + style) is { } pattern)
                {
                    AddPattern(pattern, null, overrideExisting: false);
                }
            }
        }

        if (data.Values(language) is { } values)
        {
            var available = new List<string>();

            foreach (var key in values.Keys)
            {
                if (key.StartsWith("available.", StringComparison.Ordinal))
                {
                    available.Add(key["available.".Length..]);
                }
            }

            available.Sort(StringComparer.Ordinal);

            foreach (var skeleton in available)
            {
                AddPattern(values["available." + skeleton], skeleton, overrideExisting: true);
            }
        }

        for (var field = 0; field < FieldCount; field++)
        {
            appendFormats[field] = data.Value(language, "append." + AppendItems[field]) ?? "{0} \u251C{2}: {1}\u2524";
            fieldNames[field] = data.Value(language, "field." + FieldNames[field]) ?? "F" + field.ToString(NumberFormatInfo.InvariantInfo);
        }

        foreach (var style in new[] { "full", "long", "medium", "short" })
        {
            glue[style] = data.Value(language, "atTime." + style) ?? "{1} {0}";
        }
    }

    /// <summary>
    /// ICU's <c>getBestPattern</c>: the pattern for <paramref name="skeletonText"/>, with the hour's
    /// width the skeleton's when <paramref name="options"/> carries <see cref="MatchHourLength"/>.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=High; Resources=2; Fingerprint=4E6042
    // Broiler-Falsified-If: a skeleton's pattern differs from ICU DateTimePatternGenerator getBestPattern's over the locale's CLDR availableFormats and appendItems
    // Broiler-Human:        PENDING
    internal string BestPattern(string skeletonText, int options)
    {
        var flags = 0;
        var request = Parse(MapMetacharacters(skeletonText, ref flags));
        var best = BestRaw(request, -1, out var missing, out var extra);

        if (missing == 0 && extra == 0)
        {
            return AdjustFieldTypes(best.Pattern, best.Specified ? best.Skeleton : null, flags, options, request);
        }

        var needed = request.FieldMask;
        var date = BestAppending(needed & DateMask, flags, options, request);
        var time = BestAppending(needed & TimeMask, flags, options, request);

        if (date.Length == 0)
        {
            return time;
        }

        if (time.Length == 0)
        {
            return date;
        }

        // THE JOINING PATTERN IS CHOSEN BY THE MONTH: a wide month with a weekday is full, a wide
        // month alone long, an abbreviated one medium, and anything else short.
        var month = request.BaseLengths[Month];
        var style = month == 4
            ? request.BaseLengths[Weekday] > 0 ? "full" : "long"
            : month == 3 ? "medium" : "short";

        return Substitute(glue[style], time, date, string.Empty);
    }

    /// <summary>ICU's <c>staticGetSkeleton</c>: a pattern's fields in canonical order, its literal text dropped.</summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Medium; Resources=1; Fingerprint=FC4DBA
    // Broiler-Human:        PENDING
    internal static string SkeletonOf(string pattern)
    {
        var skeleton = Parse(pattern);
        var text = skeleton.Original;

        if (skeleton.AddedDefaultDayPeriod)
        {
            var at = text.IndexOf('a');

            if (at >= 0)
            {
                text = text.Remove(at, 1);
            }
        }

        return text;
    }

    /// <summary>A pattern's tokens as ICU's <c>FormatParser</c> splits it: runs of one letter, and every other character alone.</summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Medium; Resources=2; Fingerprint=92863F
    // Broiler-Human:        PENDING
    internal static List<string> Tokens(string pattern)
    {
        var tokens = new List<string>();
        var at = 0;

        while (at < pattern.Length)
        {
            var c = pattern[at];

            if (!IsLetter(c))
            {
                tokens.Add(c.ToString());
                at++;
                continue;
            }

            var end = at + 1;

            while (end < pattern.Length && pattern[end] == c)
            {
                end++;
            }

            tokens.Add(pattern[at..end]);
            at = end;
        }

        return tokens;
    }

    /// <summary>
    /// ICU's <c>getQuoteLiteral</c>: from the quote token at <paramref name="index"/>, the index of the
    /// token that closes it, with the quoted tokens, the quotes included, appended to <paramref name="into"/>.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=1; Fingerprint=2A0B57
    // Broiler-Human:        PENDING
    internal static int QuoteEnd(List<string> tokens, int index, StringBuilder? into)
    {
        if (tokens[index][0] == '\'')
        {
            into?.Append(tokens[index]);
            index++;
        }

        while (index < tokens.Count)
        {
            if (tokens[index][0] == '\'')
            {
                if (index + 1 < tokens.Count && tokens[index + 1][0] == '\'')
                {
                    into?.Append(tokens[index]).Append(tokens[index + 1]);
                    index += 2;
                    continue;
                }

                into?.Append(tokens[index]);
                break;
            }

            into?.Append(tokens[index]);
            index++;
        }

        return index;
    }

    /// <summary>Whether <paramref name="c"/> is an ASCII letter, which a pattern reads as a field.</summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=1; Fingerprint=5FED4A
    // Broiler-Human:        PENDING
    internal static bool IsLetter(char c) => c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z');

    /// <summary>The field a run of one pattern letter is, or -1.</summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=1; Fingerprint=D84E40
    // Broiler-Human:        PENDING
    internal static int FieldOf(string token)
    {
        var index = CanonicalIndex(token, strict: true);
        return index < 0 ? -1 : Rows[index].Field;
    }

    /// <summary>Whether a run of one pattern letter is numeric in ICU's table.</summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=1; Fingerprint=94BF83
    // Broiler-Human:        PENDING
    internal static bool IsNumeric(string token)
    {
        var index = CanonicalIndex(token, strict: true);
        return index >= 0 && Rows[index].Type > 0;
    }

    // ---- ICU's structures ------------------------------------------------------------------------

    /// <summary>ICU's <c>getCanonicalIndex</c>: the row of a run of one letter, by its width.</summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Medium; Resources=1; Fingerprint=E5C8BD
    // Broiler-Human:        PENDING
    private static int CanonicalIndex(string token, bool strict)
    {
        if (token.Length == 0)
        {
            return -1;
        }

        var c = token[0];

        for (var at = 1; at < token.Length; at++)
        {
            if (token[at] != c)
            {
                return -1;
            }
        }

        var best = -1;
        var index = 0;

        while (index < Rows.Length)
        {
            if (Rows[index].Char != c)
            {
                index++;
                continue;
            }

            best = index;

            if (index + 1 >= Rows.Length || Rows[index + 1].Char != c)
            {
                return index;
            }

            if (Rows[index + 1].MinLength <= token.Length)
            {
                index++;
                continue;
            }

            return index;
        }

        return strict ? -1 : best;
    }

    /// <summary>ICU's <c>DateTimeMatcher::set</c>: the fields of a skeleton or a pattern.</summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Medium; Resources=2; Fingerprint=4C2345
    // Broiler-Human:        PENDING
    private static Skeleton Parse(string pattern)
    {
        var skeleton = new Skeleton();
        var tokens = Tokens(pattern);

        for (var index = 0; index < tokens.Count; index++)
        {
            var value = tokens[index];

            if (value[0] == '\'')
            {
                index = QuoteEnd(tokens, index, null);
                continue;
            }

            var row = CanonicalIndex(value, strict: true);

            if (row < 0)
            {
                continue;
            }

            var entry = Rows[row];
            skeleton.Chars[entry.Field] = value[0];
            skeleton.Lengths[entry.Field] = value.Length;
            skeleton.BaseChars[entry.Field] = entry.Char;
            skeleton.BaseLengths[entry.Field] = entry.MinLength;
            skeleton.Type[entry.Field] = entry.Type > 0 ? entry.Type + value.Length : entry.Type;
        }

        // MINUTES AND A FRACTION OF A SECOND WITHOUT THE SECOND BRING THE SECOND (ICU-20739).
        if (skeleton.Lengths[Minute] != 0 && skeleton.Lengths[FractionalSecond] != 0 && skeleton.Lengths[Second] == 0)
        {
            skeleton.Chars[Second] = 's';
            skeleton.Lengths[Second] = 1;
            skeleton.BaseChars[Second] = 's';
            skeleton.BaseLengths[Second] = 1;
            skeleton.Type[Second] = Numeric + 1;
        }

        // A TWELVE-HOUR CLOCK BRINGS ITS DAY PERIOD, AND A TWENTY-FOUR-HOUR CLOCK DROPS ONE.
        if (skeleton.Lengths[Hour] != 0)
        {
            if (skeleton.Chars[Hour] is 'h' or 'K')
            {
                if (skeleton.Lengths[DayPeriod] == 0)
                {
                    skeleton.Chars[DayPeriod] = 'a';
                    skeleton.Lengths[DayPeriod] = 1;
                    skeleton.BaseChars[DayPeriod] = 'a';
                    skeleton.BaseLengths[DayPeriod] = 1;
                    skeleton.Type[DayPeriod] = Short;
                    skeleton.AddedDefaultDayPeriod = true;
                }
            }
            else if (skeleton.Lengths[DayPeriod] != 0)
            {
                skeleton.Chars[DayPeriod] = '\0';
                skeleton.Lengths[DayPeriod] = 0;
                skeleton.BaseChars[DayPeriod] = '\0';
                skeleton.BaseLengths[DayPeriod] = 0;
                skeleton.Type[DayPeriod] = 0;
            }
        }

        return skeleton;
    }

    /// <summary>ICU's <c>mapSkeletonMetacharacters</c>: <c>j</c>, <c>C</c> and <c>J</c> replaced by the language's hour and day period.</summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Medium; Resources=1; Fingerprint=A05509
    // Broiler-Human:        PENDING
    private string MapMetacharacters(string skeleton, ref int flags)
    {
        var mapped = new StringBuilder(skeleton.Length);
        var quoted = false;

        for (var at = 0; at < skeleton.Length; at++)
        {
            var c = skeleton[at];

            if (c == '\'')
            {
                quoted = !quoted;
                continue;
            }

            if (quoted)
            {
                continue;
            }

            if (c is 'j' or 'C')
            {
                var extra = 0;

                while (at + 1 < skeleton.Length && skeleton[at + 1] == c)
                {
                    extra++;
                    at++;
                }

                var hourLength = 1 + (extra & 1);
                var dayPeriodLength = extra < 2 ? 1 : 3 + (extra >> 1);
                var hourChar = defaultHourChar;

                if (hourChar is 'H' or 'k')
                {
                    dayPeriodLength = 0;
                }

                mapped.Append('a', dayPeriodLength).Append(hourChar, hourLength);
                continue;
            }

            if (c == 'J')
            {
                mapped.Append('H');
                flags |= UsesCapJ;
                continue;
            }

            mapped.Append(c);
        }

        return mapped.ToString();
    }

    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=1; Fingerprint=CAE71E
    // Broiler-Human:        PENDING
    private static int BootIndex(char c) =>
        c is >= 'A' and <= 'Z' ? c - 'A' : c is >= 'a' and <= 'z' ? 26 + c - 'a' : -1;

    /// <summary>ICU's <c>addPatternWithSkeleton</c>.</summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Medium; Resources=1; Fingerprint=A1579A
    // Broiler-Human:        PENDING
    private void AddPattern(string pattern, string? skeletonKey, bool overrideExisting)
    {
        var skeleton = Parse(skeletonKey ?? pattern);
        var basePattern = skeleton.Base;

        if (basePattern.Length == 0)
        {
            return;
        }

        // A BASE ALREADY HELD by a pattern no skeleton named, or any base when this one may not
        // override, is a conflict; only an overriding pattern goes on.
        if (FromBase(basePattern) is { } byBase && (!byBase.Specified || (skeletonKey is not null && !overrideExisting)) && !overrideExisting)
        {
            return;
        }

        if (FromSkeleton(skeleton) is { } bySkeleton && (!overrideExisting || (skeletonKey is not null && bySkeleton.Specified)))
        {
            return;
        }

        var index = BootIndex(basePattern[0]);

        if (index < 0)
        {
            return;
        }

        var list = boot[index] ??= [];

        foreach (var entry in list)
        {
            if (entry.Base == basePattern && SameTypes(entry.Skeleton, skeleton))
            {
                entry.Pattern = pattern;
                entry.Specified = skeletonKey is not null;
                return;
            }
        }

        list.Add(new Entry(basePattern, skeleton, pattern, skeletonKey is not null));
    }

    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=1; Fingerprint=79669E
    // Broiler-Human:        PENDING
    private Entry? FromBase(string basePattern)
    {
        var index = BootIndex(basePattern[0]);

        if (index < 0 || boot[index] is not { } list)
        {
            return null;
        }

        foreach (var entry in list)
        {
            if (entry.Base == basePattern)
            {
                return entry;
            }
        }

        return null;
    }

    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=1; Fingerprint=A6B11F
    // Broiler-Human:        PENDING
    private Entry? FromSkeleton(Skeleton skeleton)
    {
        var basePattern = skeleton.Base;

        if (basePattern.Length == 0)
        {
            return null;
        }

        var index = BootIndex(basePattern[0]);

        if (index < 0 || boot[index] is not { } list)
        {
            return null;
        }

        var original = skeleton.Original;

        foreach (var entry in list)
        {
            if (entry.Skeleton.Original == original)
            {
                return entry;
            }
        }

        return null;
    }

    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=1; Fingerprint=60DD36
    // Broiler-Human:        PENDING
    private static bool SameTypes(Skeleton left, Skeleton right)
    {
        for (var field = 0; field < FieldCount; field++)
        {
            if (left.Type[field] != right.Type[field])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>ICU's <c>getDistance</c>.</summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=High; Resources=2; Fingerprint=531AC5
    // Broiler-Falsified-If: a pattern's distance to a skeleton differs from ICU's, so another availableFormats entry is chosen
    // Broiler-Human:        PENDING
    private static int Distance(Skeleton request, Skeleton other, int includeMask, out int missing, out int extra)
    {
        var result = 0;
        missing = 0;
        extra = 0;

        for (var field = 0; field < FieldCount; field++)
        {
            var mine = (includeMask & (1 << field)) == 0 ? 0 : request.Type[field];
            var theirs = other.Type[field];

            if (mine == theirs)
            {
                continue;
            }

            if (mine == 0)
            {
                result += ExtraField;
                extra |= 1 << field;
            }
            else if (theirs == 0)
            {
                result += MissingField;
                missing |= 1 << field;
            }
            else
            {
                result += Math.Abs(mine - theirs);
            }
        }

        return result;
    }

    /// <summary>
    /// ICU's <c>getBestRaw</c>: the nearest pattern, and of two as near the one whose missing fields
    /// are the higher bits.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Medium; Resources=2; Fingerprint=9815B6
    // Broiler-Human:        PENDING
    private Entry BestRaw(Skeleton request, int includeMask, out int missing, out int extra)
    {
        var bestDistance = int.MaxValue;
        var bestMissing = -1;
        Entry? best = null;
        missing = 0;
        extra = 0;

        foreach (var list in boot)
        {
            if (list is null)
            {
                continue;
            }

            foreach (var entry in list)
            {
                var distance = Distance(request, entry.Skeleton, includeMask, out var entryMissing, out var entryExtra);

                if (distance < bestDistance || (distance == bestDistance && bestMissing < entryMissing))
                {
                    bestDistance = distance;
                    bestMissing = entryMissing;
                    best = entry;
                    missing = entryMissing;
                    extra = entryExtra;

                    if (distance == 0)
                    {
                        return best;
                    }
                }
            }
        }

        return best!;
    }

    /// <summary>ICU's <c>getBestAppending</c>: the fields of <paramref name="fields"/>, with what no pattern holds appended.</summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Medium; Resources=2; Fingerprint=EEA8A0
    // Broiler-Human:        PENDING
    private string BestAppending(int fields, int flags, int options, Skeleton request)
    {
        if (fields == 0)
        {
            return string.Empty;
        }

        var raw = BestRaw(request, fields, out var missing, out _);
        var specified = raw.Specified ? raw.Skeleton : null;
        var result = AdjustFieldTypes(raw.Pattern, specified, flags, options, request);
        var lastMissing = 0;

        while (missing != 0)
        {
            if (lastMissing == missing)
            {
                break;
            }

            if ((missing & SecondAndFractionalMask) == FractionalMask && (fields & SecondAndFractionalMask) == SecondAndFractionalMask)
            {
                result = AdjustFieldTypes(result, specified, flags | FixFractionalSeconds, options, request);
                missing &= ~FractionalMask;
                continue;
            }

            var starting = missing;
            var next = BestRaw(request, missing, out missing, out _);
            specified = next.Specified ? next.Skeleton : null;
            var appended = AdjustFieldTypes(next.Pattern, specified, flags, options, request);
            var top = TopBit(starting & ~missing);

            if (appendFormats[top].Length != 0)
            {
                result = Substitute(appendFormats[top], result, appended, "'" + fieldNames[top] + "'");
            }

            lastMissing = missing;
        }

        return result;
    }

    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=1; Fingerprint=9235BC
    // Broiler-Human:        PENDING
    private static int TopBit(int mask)
    {
        if (mask == 0)
        {
            return 0;
        }

        var bit = 0;

        while (mask != 0)
        {
            mask >>>= 1;
            bit++;
        }

        return bit - 1 > Zone ? Zone : bit - 1;
    }

    /// <summary>ICU's <c>adjustFieldTypes</c>: a found pattern's fields given the skeleton's letters and widths.</summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=High; Resources=2; Fingerprint=4D9929
    // Broiler-Falsified-If: a matched pattern's field widths differ from ICU adjustFieldTypes' for the requested skeleton
    // Broiler-Human:        PENDING
    private string AdjustFieldTypes(string pattern, Skeleton? specified, int flags, int options, Skeleton request)
    {
        var tokens = Tokens(pattern);
        var result = new StringBuilder(pattern.Length + 8);

        for (var index = 0; index < tokens.Count; index++)
        {
            var field = tokens[index];

            if (field[0] == '\'')
            {
                index = QuoteEnd(tokens, index, result);
                continue;
            }

            var row = CanonicalIndex(field, strict: true);

            if (row < 0)
            {
                result.Append(field);
                continue;
            }

            var entry = Rows[row];
            var type = entry.Field;

            if (type == DayPeriod && (flags & UsesCapJ) != 0)
            {
                continue;
            }

            if ((flags & FixFractionalSeconds) != 0 && type == Second)
            {
                result.Append(field).Append(decimalSymbol).Append(request.Chars[FractionalSecond], request.Lengths[FractionalSecond]);
                continue;
            }

            if (request.Type[type] != 0)
            {
                var requestChar = request.Chars[type];
                var requestLength = request.Lengths[type];

                if (requestChar == 'E' && requestLength < 3)
                {
                    requestLength = 3;
                }

                var length = requestLength;

                if ((type == Hour && (options & MatchHourLength) == 0) ||
                    (type == Minute && (options & MatchMinuteLength) == 0) ||
                    (type == Second && (options & MatchSecondLength) == 0))
                {
                    length = field.Length;
                }
                else if (specified is not null && requestChar != 'c' && requestChar != 'e')
                {
                    var skeletonLength = specified.Lengths[type];
                    var patternNumeric = entry.Type > 0;
                    var skeletonNumeric = specified.Type[type] > 0;

                    if (skeletonLength == requestLength || patternNumeric != skeletonNumeric)
                    {
                        length = field.Length;
                    }
                }

                var c = type != Hour && type != Month && type != Weekday && (type != Year || requestChar == 'Y')
                    ? requestChar
                    : field[0];

                if (type == Hour && defaultHourChar != '\0')
                {
                    if ((flags & UsesCapJ) != 0 || requestChar == defaultHourChar)
                    {
                        c = defaultHourChar;
                    }
                    else if (requestChar == 'h' && defaultHourChar == 'K')
                    {
                        c = 'K';
                    }
                    else if (requestChar == 'H' && defaultHourChar == 'k')
                    {
                        c = 'k';
                    }
                    else if (requestChar == 'k' && defaultHourChar == 'H')
                    {
                        c = 'H';
                    }
                    else if (requestChar == 'K' && defaultHourChar == 'h')
                    {
                        c = 'h';
                    }
                }

                field = new string(c, length);
            }

            result.Append(field);
        }

        return result.ToString();
    }

    /// <summary>A CLDR message pattern with <c>{0}</c>, <c>{1}</c> and <c>{2}</c> replaced.</summary>
    // Broiler-AI:           Origin=Ported; Spec=UTS35 Part 4 section 2.6 availableFormats and appendItems via Broiler.VM JsDatePatternGenerator (ICU DateTimePatternGenerator); IP=Low; Security=Low; Resources=1; Fingerprint=337911
    // Broiler-Human:        PENDING
    internal static string Substitute(string pattern, string zero, string one, string two)
    {
        var text = new StringBuilder(pattern.Length + zero.Length + one.Length + two.Length);

        for (var at = 0; at < pattern.Length; at++)
        {
            if (pattern[at] == '{' && at + 2 < pattern.Length && pattern[at + 2] == '}' && pattern[at + 1] is '0' or '1' or '2')
            {
                text.Append(pattern[at + 1] switch { '0' => zero, '1' => one, _ => two });
                at += 2;
                continue;
            }

            text.Append(pattern[at]);
        }

        return text.ToString();
    }
}
