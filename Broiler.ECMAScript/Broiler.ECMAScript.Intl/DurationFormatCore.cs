// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using System.Text;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Temporal;

namespace Broiler.ECMAScript.Intl;

/// <summary>A unit of a duration, in Table 20's order (ECMA-402 <c>sec-Intl.DurationFormat</c>, Table 20).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-Intl.DurationFormat Table 20 units and styles; IP=Low; Security=Low; Resources=0; Fingerprint=TBF
// Broiler-Human:        PENDING
public enum DurationUnit
{
    /// <summary><c>years</c>.</summary>
    Years = 0,

    /// <summary><c>months</c>.</summary>
    Months = 1,

    /// <summary><c>weeks</c>.</summary>
    Weeks = 2,

    /// <summary><c>days</c>.</summary>
    Days = 3,

    /// <summary><c>hours</c>.</summary>
    Hours = 4,

    /// <summary><c>minutes</c>.</summary>
    Minutes = 5,

    /// <summary><c>seconds</c>.</summary>
    Seconds = 6,

    /// <summary><c>milliseconds</c>.</summary>
    Milliseconds = 7,

    /// <summary><c>microseconds</c>.</summary>
    Microseconds = 8,

    /// <summary><c>nanoseconds</c>.</summary>
    Nanoseconds = 9,
}

/// <summary>[[Style]] of an <c>Intl.DurationFormat</c>, its base style (the <c>style</c> option).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-Intl.DurationFormat Table 20 units and styles; IP=Low; Security=Low; Resources=0; Fingerprint=TBF
// Broiler-Human:        PENDING
public enum DurationFormatStyle
{
    /// <summary><c>long</c>.</summary>
    Long = 0,

    /// <summary><c>short</c>, the option's default.</summary>
    Short = 1,

    /// <summary><c>narrow</c>.</summary>
    Narrow = 2,

    /// <summary><c>digital</c>: the time units as a clock.</summary>
    Digital = 3,
}

/// <summary>The style of one unit of a duration format: a unit option's value, or <see cref="Fractional"/>, which GetDurationUnitOptions makes of a sub-second unit's <c>numeric</c>.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-Intl.DurationFormat Table 20 units and styles; IP=Low; Security=Low; Resources=0; Fingerprint=TBF
// Broiler-Human:        PENDING
public enum DurationUnitStyle
{
    /// <summary><c>long</c>.</summary>
    Long = 0,

    /// <summary><c>short</c>.</summary>
    Short = 1,

    /// <summary><c>narrow</c>.</summary>
    Narrow = 2,

    /// <summary><c>numeric</c>.</summary>
    Numeric = 3,

    /// <summary><c>2-digit</c>.</summary>
    TwoDigit = 4,

    /// <summary>
    /// <c>fractional</c>: a sub-second unit written as the fraction of the unit before it (GetDurationUnitOptions,
    /// <c>sec-getdurationunitoptions</c>, step 4); <c>resolvedOptions</c> reports it as <c>numeric</c>.
    /// </summary>
    Fractional = 5,
}

/// <summary>The display of one unit of a duration format (the <c><i>unit</i>Display</c> option).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-Intl.DurationFormat Table 20 units and styles; IP=Low; Security=Low; Resources=0; Fingerprint=TBF
// Broiler-Human:        PENDING
public enum DurationUnitDisplay
{
    /// <summary><c>auto</c>: a zero value is not written.</summary>
    Auto = 0,

    /// <summary><c>always</c>.</summary>
    Always = 1,
}

/// <summary>One unit's [[<i>Unit</i>Options]]: what GetDurationUnitOptions resolved (<c>sec-getdurationunitoptions</c>).</summary>
/// <param name="Style">The unit's style.</param>
/// <param name="Display">The unit's display.</param>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-Intl.DurationFormat Table 20 units and styles; IP=Low; Security=Low; Resources=0; Fingerprint=TBF
// Broiler-Human:        PENDING
public readonly record struct DurationUnitOptions(DurationUnitStyle Style, DurationUnitDisplay Display);

/// <summary>
/// A locale's [[DigitalFormat]] (<c>sec-Intl.DurationFormat-internal-slots</c>): the clock's separators and whether its hours
/// are always written with two digits, as CLDR's number data states them (Broiler.VM's JSD-0082, decision D34).
/// </summary>
/// <param name="HourMinuteSeparator">[[HourMinuteSeparator]]: CLDR's <c>timeSeparator</c> symbol.</param>
/// <param name="MinuteSecondSeparator">[[MinuteSecondSeparator]]: the same symbol.</param>
/// <param name="TwoDigitHours">[[TwoDigitHours]]: whether CLDR's <c>durationUnit-type-hms</c> pattern begins <c>hh</c> (JSD-0051 §2).</param>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-Intl.DurationFormat-internal-slots DigitalFormat, JSD-0082 D34; IP=Low; Security=Low; Resources=0; Fingerprint=TBF
// Broiler-Human:        PENDING
public readonly record struct DurationDigitalFormat(string HourMinuteSeparator, string MinuteSecondSeparator, bool TwoDigitHours);

/// <summary>One part of a formatted duration (<c>sec-Intl.DurationFormat.prototype.formatToParts</c>).</summary>
/// <param name="Type">A number part's type, or <c>literal</c>.</param>
/// <param name="Value">The part's text.</param>
/// <param name="Unit">The singular unit, for a part of a unit's number; <see langword="null"/> for the list's literals and the clock's separators.</param>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-Intl.DurationFormat.prototype.formatToParts parts; IP=Low; Security=Low; Resources=0; Fingerprint=TBF
// Broiler-Human:        PENDING
public readonly record struct DurationPart(string Type, string Value, string? Unit);

/// <summary>
/// One configured <c>Intl.DurationFormat</c>'s formatting: PartitionDurationFormatPattern (<c>sec-partitiondurationformatpattern</c>)
/// over the library's number and list formatters (Broiler.VM's JSD-0082, sub-area (e), rows U1 to U22), with the constructor's
/// GetDurationUnitOptions and the locale's [[DigitalFormat]].
/// </summary>
/// <remarks>
/// <para>
/// <b>Every number is a <see cref="NumberFormatter"/> and the list a <see cref="ListFormatter"/></b>, each made as Broiler.VM's realm
/// makes them through its own constructors, with the options the text names: a unit format of the unit's style for each unit
/// before the clock, a decimal format without grouping for each field of the clock, and a unit list of the base style
/// (<c>short</c> for <c>digital</c>). So a duration writes each unit as those formatters write it, in the format's numbering
/// system, with that system's own symbols where the data carries them (decision D21, row U16).
/// </para>
/// <para>
/// <b>A fractional value is exact</b> (rows U11, U20 and U21). ComputeFractionalDigits adds the sub-second units to the unit before
/// them as a mathematical value; here the sum is a <see cref="BigInteger"/> of the smallest unit, written in ASCII digits and read
/// as a decimal String, which the number format writes exactly, truncated to <c>fractionalDigits</c> or nine digits.
/// </para>
/// <para>
/// <b>The data is the data locale's</b> (rows U13 to U15; decision D34): [[DigitalFormat]] from the number data's
/// <c>symbols.timeSeparator</c> and <c>duration.hms</c>, and the unit and list patterns, all read through the
/// <see cref="ILocaleData"/> handed in, where Broiler.VM read the data locale's language; for Broiler.VM's tables the two are the
/// same lines.
/// </para>
/// <para>
/// <b>Ported from Broiler.VM's <c>JsRealm.DurationFormat</c></b> at its SJL-9 rule commit <c>4cf9ca44</c>: GetDurationUnitOptions
/// with ValidateDurationUnitStyle, [[DigitalFormat]], DurationSign, ComputeFractionalDigits, FormatNumericHours, -Minutes and
/// -Seconds, FormatNumericUnits, ListFormatParts and PartitionDurationFormatPattern. The three changes of the stage: a status where
/// ValidateDurationUnitStyle threw; ASCII digits from <see cref="BigIntText"/> where the fraction was written with the invariant
/// culture (decision D19, row U20); and the caller's <see cref="WorkCharge"/> where Broiler.VM charged its engine's meter, in its
/// units (decision D18, row U19): one per unit PartitionDurationFormatPattern visits, and the charges of the number and list
/// formats it makes - one for the locale each one's constructor canonicalises, each number's FormatNumericToString, a fraction's
/// String read as a decimal, and each step of the list's template. The reads of the argument - ToTemporalDuration, or ECMA-402's
/// own ToDurationRecord where Temporal is not composed (row U22) - the reads of the options, the parts arrays with their per-part
/// charge and <c>resolvedOptions</c>' object are the engine's (decision D24): the core takes a valid duration record and reads no
/// script value.
/// </para>
/// <para><b>Immutable</b>: one may be shared between threads (convention 10).</para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-partitiondurationformatpattern sec-getdurationunitoptions via Broiler.VM JsRealm.DurationFormat; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
// Broiler-Human:        PENDING
public sealed class DurationFormatCore
{
    /// <summary>Table 20: each unit, its singular, its styles and its digital default.</summary>
    private static readonly ImmutableArray<UnitRow> Table =
    [
        new("years", "year", [DurationUnitStyle.Long, DurationUnitStyle.Short, DurationUnitStyle.Narrow], DurationUnitStyle.Short),
        new("months", "month", [DurationUnitStyle.Long, DurationUnitStyle.Short, DurationUnitStyle.Narrow], DurationUnitStyle.Short),
        new("weeks", "week", [DurationUnitStyle.Long, DurationUnitStyle.Short, DurationUnitStyle.Narrow], DurationUnitStyle.Short),
        new("days", "day", [DurationUnitStyle.Long, DurationUnitStyle.Short, DurationUnitStyle.Narrow], DurationUnitStyle.Short),
        new("hours", "hour", [DurationUnitStyle.Long, DurationUnitStyle.Short, DurationUnitStyle.Narrow, DurationUnitStyle.Numeric, DurationUnitStyle.TwoDigit], DurationUnitStyle.Numeric),
        new("minutes", "minute", [DurationUnitStyle.Long, DurationUnitStyle.Short, DurationUnitStyle.Narrow, DurationUnitStyle.Numeric, DurationUnitStyle.TwoDigit], DurationUnitStyle.Numeric),
        new("seconds", "second", [DurationUnitStyle.Long, DurationUnitStyle.Short, DurationUnitStyle.Narrow, DurationUnitStyle.Numeric, DurationUnitStyle.TwoDigit], DurationUnitStyle.Numeric),
        new("milliseconds", "millisecond", [DurationUnitStyle.Long, DurationUnitStyle.Short, DurationUnitStyle.Narrow, DurationUnitStyle.Numeric], DurationUnitStyle.Numeric),
        new("microseconds", "microsecond", [DurationUnitStyle.Long, DurationUnitStyle.Short, DurationUnitStyle.Narrow, DurationUnitStyle.Numeric], DurationUnitStyle.Numeric),
        new("nanoseconds", "nanosecond", [DurationUnitStyle.Long, DurationUnitStyle.Short, DurationUnitStyle.Narrow, DurationUnitStyle.Numeric], DurationUnitStyle.Numeric),
    ];

    private const int UnitCount = 10;

    private const int HoursIndex = (int)DurationUnit.Hours;

    private const int MinutesIndex = (int)DurationUnit.Minutes;

    private const int SecondsIndex = (int)DurationUnit.Seconds;

    private readonly ILocaleData data;

    private readonly string dataLocale;

    private readonly DurationUnitOptions[] units;

    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-partitiondurationformatpattern sec-getdurationunitoptions via Broiler.VM JsRealm.DurationFormat; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private DurationFormatCore(ILocaleData data, string locale, string dataLocale, string numberingSystem, DurationFormatStyle style,
        DurationUnitOptions[] units, int? fractionalDigits, DurationDigitalFormat digital)
    {
        this.data = data;
        this.dataLocale = dataLocale;
        this.units = units;
        Units = Array.AsReadOnly(units);
        Locale = locale;
        NumberingSystem = numberingSystem;
        Style = style;
        FractionalDigits = fractionalDigits;
        HourMinuteSeparator = digital.HourMinuteSeparator;
        MinuteSecondSeparator = digital.MinuteSecondSeparator;
    }

    /// <summary>[[Locale]].</summary>
    public string Locale { get; }

    /// <summary>[[NumberingSystem]].</summary>
    public string NumberingSystem { get; }

    /// <summary>[[Style]].</summary>
    public DurationFormatStyle Style { get; }

    /// <summary>Each unit's [[<i>Unit</i>Options]], in Table 20's order, indexed by <see cref="DurationUnit"/>.</summary>
    public IReadOnlyList<DurationUnitOptions> Units { get; }

    /// <summary>[[FractionalDigits]], or <see langword="null"/> where the option was not given.</summary>
    public int? FractionalDigits { get; }

    /// <summary>[[HourMinuteSeparator]].</summary>
    public string HourMinuteSeparator { get; }

    /// <summary>[[MinuteSecondSeparator]].</summary>
    public string MinuteSecondSeparator { get; }

    /// <summary>A unit's name, Table 20's [[Unit]] column and the option a constructor reads: <c>years</c> to <c>nanoseconds</c>.</summary>
    /// <param name="unit">The unit.</param>
    /// <returns>The name.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="unit"/> is not a unit.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-Intl.DurationFormat Table 20 units and styles; IP=Low; Security=Low; Resources=0; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public static string UnitName(DurationUnit unit) => Row(unit).Unit;

    /// <summary>A unit's singular, the unit its number format writes and its parts carry: <c>year</c> to <c>nanosecond</c>.</summary>
    /// <param name="unit">The unit.</param>
    /// <returns>The singular.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="unit"/> is not a unit.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-Intl.DurationFormat Table 20 units and styles; IP=Low; Security=Low; Resources=0; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public static string UnitSingular(DurationUnit unit) => Row(unit).Singular;

    /// <summary>The styles a unit's option admits, Table 20's [[Values]] column, in its order: the list GetOption checks the option against.</summary>
    /// <param name="unit">The unit.</param>
    /// <returns>The styles, never <see cref="DurationUnitStyle.Fractional"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="unit"/> is not a unit.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-Intl.DurationFormat Table 20 units and styles; IP=Low; Security=Low; Resources=0; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public static IReadOnlyList<DurationUnitStyle> UnitStyles(DurationUnit unit) => Row(unit).Styles;

    /// <summary>
    /// A locale's [[DigitalFormat]] (decision D34): CLDR's <c>symbols.timeSeparator</c> as both separators, <c>:</c> where the data
    /// has none, and two-digit hours where its <c>duration.hms</c> pattern begins <c>hh</c>. The constructor reads it before the
    /// units' options, whose hours it makes <c>2-digit</c> (<see cref="GetDurationUnitOptions"/>).
    /// </summary>
    /// <param name="data">The locale data, ordinarily a <see cref="CompositeLocaleData"/>.</param>
    /// <param name="dataLocale">ResolveLocale's [[DataLocale]].</param>
    /// <returns>The digital format.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-Intl.DurationFormat-internal-slots DigitalFormat, JSD-0082 D34 via Broiler.VM JsRealm.DurationFormat; IP=Low; Security=Low; Resources=0; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public static DurationDigitalFormat DigitalFormat(ILocaleData data, string dataLocale)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(dataLocale);
        var separator = data.NumberValue(dataLocale, "symbols.timeSeparator") ?? ":";
        var twoDigitHours = (data.NumberValue(dataLocale, "duration.hms") ?? "h:mm:ss").StartsWith("hh", StringComparison.Ordinal);
        return new DurationDigitalFormat(separator, separator, twoDigitHours);
    }

    /// <summary>
    /// GetDurationUnitOptions (<c>sec-getdurationunitoptions</c>) with ValidateDurationUnitStyle (<c>sec-validatedurationunitstyle</c>),
    /// after an engine has read the unit's two options: the style's default from the base style and the style before (rows U1 and
    /// U4), a sub-second <c>numeric</c> made <c>fractional</c> with the display default <c>auto</c> (row U2), the checks (row U3),
    /// and the hours of a locale with two-digit hours and the minutes and seconds after a <c>numeric</c> or <c>2-digit</c> unit made
    /// <c>2-digit</c>.
    /// </summary>
    /// <param name="unit">The unit, taken in Table 20's order.</param>
    /// <param name="style">The <c><i>unit</i></c> option, one of <see cref="UnitStyles"/>; <see langword="null"/> where it is undefined.</param>
    /// <param name="display">The <c><i>unit</i>Display</c> option; <see langword="null"/> where it is undefined, for the default.</param>
    /// <param name="baseStyle">[[Style]].</param>
    /// <param name="previous">The style of the unit before, as the constructor keeps it (the last of hours to microseconds); <see langword="null"/> for the empty String.</param>
    /// <param name="twoDigitHours">[[DigitalFormat]]'s two-digit hours (<see cref="DigitalFormat"/>).</param>
    /// <param name="options">The unit's options; <c>default</c> where refused.</param>
    /// <returns><see cref="IntlStatus.Completed"/>, or a <c>RangeError</c>: <see cref="IntlStatus.FractionalDurationUnitAlwaysDisplayed"/>,
    /// <see cref="IntlStatus.DurationUnitAfterFractional"/> or <see cref="IntlStatus.DurationUnitAfterNumeric"/>, in that order of the checks.</returns>
    /// <exception cref="ArgumentException"><paramref name="style"/> is a style the unit's option does not admit, which GetOption refuses first.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="unit"/> is not a unit.</exception>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-getdurationunitoptions sec-validatedurationunitstyle via Broiler.VM JsRealm.DurationFormat; IP=Low; Security=High; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: a unit's resolved style or display differs from GetDurationUnitOptions' for the options and the unit before, or a refusal is another status
    // Broiler-Human:        PENDING
    public static IntlStatus GetDurationUnitOptions(DurationUnit unit, DurationUnitStyle? style, DurationUnitDisplay? display, DurationFormatStyle baseStyle,
        DurationUnitStyle? previous, bool twoDigitHours, out DurationUnitOptions options)
    {
        var row = Row(unit);
        if (style is { } given && row.Styles.IndexOf(given) < 0)
        {
            throw new ArgumentException("The " + row.Unit + " option admits no style " + given + ".", nameof(style));
        }

        options = default;
        var resolved = style ?? DurationUnitStyle.Long;
        var displayDefault = DurationUnitDisplay.Always;

        if (style is null)
        {
            if (baseStyle == DurationFormatStyle.Digital)
            {
                resolved = row.Digital;

                if (unit is not (DurationUnit.Hours or DurationUnit.Minutes or DurationUnit.Seconds))
                {
                    displayDefault = DurationUnitDisplay.Auto;
                }
            }
            else if (previous is DurationUnitStyle.Fractional or DurationUnitStyle.Numeric or DurationUnitStyle.TwoDigit)
            {
                resolved = DurationUnitStyle.Numeric;

                if (unit is not (DurationUnit.Minutes or DurationUnit.Seconds))
                {
                    displayDefault = DurationUnitDisplay.Auto;
                }
            }
            else
            {
                resolved = baseStyle switch { DurationFormatStyle.Long => DurationUnitStyle.Long, DurationFormatStyle.Narrow => DurationUnitStyle.Narrow, _ => DurationUnitStyle.Short };
                displayDefault = DurationUnitDisplay.Auto;
            }
        }

        if (resolved == DurationUnitStyle.Numeric && IsFractionalSecondUnit(unit))
        {
            resolved = DurationUnitStyle.Fractional;
            displayDefault = DurationUnitDisplay.Auto;
        }

        var resolvedDisplay = display ?? displayDefault;

        // VALIDATEDURATIONUNITSTYLE, its three steps in order.
        if (resolvedDisplay == DurationUnitDisplay.Always && resolved == DurationUnitStyle.Fractional)
        {
            return IntlStatus.FractionalDurationUnitAlwaysDisplayed;
        }

        if (previous == DurationUnitStyle.Fractional && resolved != DurationUnitStyle.Fractional)
        {
            return IntlStatus.DurationUnitAfterFractional;
        }

        if (previous is DurationUnitStyle.Numeric or DurationUnitStyle.TwoDigit
            && resolved is not (DurationUnitStyle.Fractional or DurationUnitStyle.Numeric or DurationUnitStyle.TwoDigit))
        {
            return IntlStatus.DurationUnitAfterNumeric;
        }

        if (unit == DurationUnit.Hours && twoDigitHours)
        {
            resolved = DurationUnitStyle.TwoDigit;
        }

        if (unit is DurationUnit.Minutes or DurationUnit.Seconds && previous is DurationUnitStyle.Numeric or DurationUnitStyle.TwoDigit)
        {
            resolved = DurationUnitStyle.TwoDigit;
        }

        options = new DurationUnitOptions(resolved, resolvedDisplay);
        return IntlStatus.Completed;
    }

    /// <summary>
    /// The formatter an <c>Intl.DurationFormat</c> constructor makes from what it resolved and read (<c>sec-Intl.DurationFormat</c>),
    /// with [[DigitalFormat]]'s separators read at the data locale.
    /// </summary>
    /// <param name="data">The locale data, ordinarily a <see cref="CompositeLocaleData"/>.</param>
    /// <param name="locale">ResolveLocale's [[Locale]], which the number and list formats are made for.</param>
    /// <param name="dataLocale">ResolveLocale's [[DataLocale]], at which the data is read.</param>
    /// <param name="numberingSystem">The resolved <c>nu</c> key's value.</param>
    /// <param name="style">[[Style]].</param>
    /// <param name="units">The ten units' options, in Table 20's order, as <see cref="GetDurationUnitOptions"/> resolved them.</param>
    /// <param name="fractionalDigits">GetNumberOption's <c>fractionalDigits</c>, 0 to 9, or <see langword="null"/>.</param>
    /// <returns>The formatter.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="units"/> does not hold ten units, or <paramref name="fractionalDigits"/> is outside 0 to 9.</exception>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-Intl.DurationFormat via Broiler.VM JsRealm.DurationFormat; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public static DurationFormatCore Create(ILocaleData data, string locale, string dataLocale, string numberingSystem, DurationFormatStyle style,
        IReadOnlyList<DurationUnitOptions> units, int? fractionalDigits)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(locale);
        ArgumentNullException.ThrowIfNull(dataLocale);
        ArgumentNullException.ThrowIfNull(numberingSystem);
        ArgumentNullException.ThrowIfNull(units);

        if (units.Count != UnitCount)
        {
            throw new ArgumentException("A duration format has the options of Table 20's ten units.", nameof(units));
        }

        if (fractionalDigits is < 0 or > 9)
        {
            throw new ArgumentException("fractionalDigits is 0 to 9, as GetNumberOption admits it.", nameof(fractionalDigits));
        }

        var copy = new DurationUnitOptions[UnitCount];
        for (var i = 0; i < UnitCount; i++)
        {
            copy[i] = units[i];
        }

        return new DurationFormatCore(data, locale, dataLocale, numberingSystem, style, copy, fractionalDigits, DigitalFormat(data, dataLocale));
    }

    /// <summary>
    /// <c>Intl.DurationFormat.prototype.format</c>'s String (<c>sec-Intl.DurationFormat.prototype.format</c>): the parts'
    /// values, joined.
    /// </summary>
    /// <param name="duration">The duration record the engine's ToTemporalDuration made.</param>
    /// <param name="charge">The caller's meter, as for <see cref="FormatToParts"/>.</param>
    /// <returns>The String.</returns>
    /// <exception cref="ArgumentException"><paramref name="duration"/> is not a valid duration of integral fields.</exception>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-partitiondurationformatpattern via Broiler.VM JsRealm.DurationFormat; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public string Format(DurationFields duration, WorkCharge? charge = null)
    {
        var text = new StringBuilder();

        foreach (var part in FormatToParts(duration, charge))
        {
            text.Append(part.Value);
        }

        return text.ToString();
    }

    /// <summary>
    /// PartitionDurationFormatPattern (<c>sec-partitiondurationformatpattern</c>): each unit before the clock written by a unit
    /// number format of its style where it is displayed, the first written signed and a negative duration's zero first unit
    /// <c>-0</c> (row U6); the clock from its first numeric unit (FormatNumericUnits, row U5); the sub-second units added to the unit
    /// before them where they are fractional (row U11); and the whole joined by a unit list format (ListFormatParts), each number's
    /// parts carrying the singular unit and the list's literals none (row U12).
    /// </summary>
    /// <param name="duration">The duration record the engine's ToTemporalDuration made.</param>
    /// <param name="charge">The caller's meter (decision D18, row U19): one unit for each unit visited, one for the locale each number
    /// and list format canonicalises as it is made, each FormatNumericToString's <c>digits / 16 + 1</c>, a fraction's
    /// <c>length / 8 + 1</c> as its String is read, and one for each step of the list's template.</param>
    /// <returns>The parts.</returns>
    /// <exception cref="ArgumentException"><paramref name="duration"/> is not a valid duration of integral fields.</exception>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-partitiondurationformatpattern sec-listformatparts via Broiler.VM JsRealm.DurationFormat; IP=Low; Security=High; Resources=2; Fingerprint=TBF
    // Broiler-Falsified-If: the parts differ from PartitionDurationFormatPattern's for the record, or the charge from Broiler.VM's for the same duration and format
    // Broiler-Human:        PENDING
    public IReadOnlyList<DurationPart> FormatToParts(DurationFields duration, WorkCharge? charge = null)
    {
        var record = RecordOf(duration);
        var lists = new List<List<DurationPart>>();
        var signDisplayed = true;

        for (var i = 0; i < UnitCount; i++)
        {
            charge?.Invoke(1);
            var (style, display) = units[i];

            if (style is DurationUnitStyle.Numeric or DurationUnitStyle.TwoDigit)
            {
                var numeric = FormatNumericUnits(record, i, signDisplayed, charge);

                if (numeric.Count != 0)
                {
                    lists.Add(numeric);
                }

                break;
            }

            var nextFractional = i + 1 < UnitCount && i >= SecondsIndex && units[i + 1].Style == DurationUnitStyle.Fractional;
            var value = nextFractional ? WithFractions(record, i, charge) : IntlDecimal.FromNumber(record[i]);
            var fractions = nextFractional;
            var signNever = false;

            if (display == DurationUnitDisplay.Always || !value.IsZero)
            {
                if (signDisplayed)
                {
                    signDisplayed = false;

                    if (value.IsZero && DurationSign(record) == -1)
                    {
                        value = IntlDecimal.NegativeZero;
                    }
                }
                else
                {
                    signNever = true;
                }

                var options = new NumberFormatOptions
                {
                    Style = NumberStyle.Unit,
                    Unit = Table[i].Singular,
                    UnitDisplay = style switch { DurationUnitStyle.Long => UnitDisplay.Long, DurationUnitStyle.Narrow => UnitDisplay.Narrow, _ => UnitDisplay.Short },
                    DigitOptions = Digits(NumberStyle.Unit, 1, fractions),
                    UseGrouping = NumberGrouping.Auto,
                    SignDisplay = signNever ? SignDisplay.Never : SignDisplay.Auto,
                };
                var list = new List<DurationPart>();

                foreach (var part in NumberFormat(options, charge).FormatToParts(value, charge))
                {
                    list.Add(new DurationPart(part.Type, part.Value, Table[i].Singular));
                }

                lists.Add(list);
            }

            if (nextFractional)
            {
                break;
            }
        }

        return ListFormatParts(lists, charge);
    }

    /// <summary>
    /// FormatNumericUnits (<c>sec-formatnumericunits</c>): which of hours, minutes and seconds the clock writes - the minutes between
    /// written hours and seconds - from the first numeric unit, each by FormatNumericHours, FormatNumericMinutes or
    /// FormatNumericSeconds, the first written carrying the sign and a negative duration's zero hours or minutes <c>-0</c>.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatnumericunits via Broiler.VM JsRealm.DurationFormat; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private List<DurationPart> FormatNumericUnits(double[] record, int first, bool signDisplayed, WorkCharge? charge)
    {
        var result = new List<DurationPart>();
        var hours = record[HoursIndex];
        var minutes = record[MinutesIndex];
        var seconds = record[7] != 0 || record[8] != 0 || record[9] != 0
            ? WithFractions(record, SecondsIndex, charge)
            : IntlDecimal.FromNumber(record[SecondsIndex]);

        var hoursFormatted = first == HoursIndex && (hours != 0 || units[HoursIndex].Display == DurationUnitDisplay.Always);
        var secondsFormatted = !seconds.IsZero || units[SecondsIndex].Display == DurationUnitDisplay.Always;
        var minutesFormatted = first is HoursIndex or MinutesIndex &&
            ((hoursFormatted && secondsFormatted) || minutes != 0 || units[MinutesIndex].Display == DurationUnitDisplay.Always);

        if (hoursFormatted)
        {
            var value = signDisplayed && hours == 0 && DurationSign(record) == -1 ? -0.0 : hours;
            FormatNumericField(HoursIndex, IntlDecimal.FromNumber(value), null, signDisplayed, result, charge);
            signDisplayed = false;
        }

        if (minutesFormatted)
        {
            var value = signDisplayed && minutes == 0 && DurationSign(record) == -1 ? -0.0 : minutes;
            FormatNumericField(MinutesIndex, IntlDecimal.FromNumber(value), hoursFormatted ? HourMinuteSeparator : null, signDisplayed, result, charge);
            signDisplayed = false;
        }

        if (secondsFormatted)
        {
            FormatNumericField(SecondsIndex, seconds, minutesFormatted ? MinuteSecondSeparator : null, signDisplayed, result, charge);
        }

        return result;
    }

    /// <summary>
    /// FormatNumericHours, FormatNumericMinutes and FormatNumericSeconds (<c>sec-formatnumerichours</c> and its siblings): one clock
    /// field after its separator where a field precedes it, by a decimal format without grouping, two integer digits for
    /// <c>2-digit</c>, the sign never but on the first field, and the seconds' fraction truncated to <c>fractionalDigits</c> or nine
    /// digits.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-formatnumerichours sec-formatnumericminutes sec-formatnumericseconds via Broiler.VM JsRealm.DurationFormat; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private void FormatNumericField(int index, IntlDecimal value, string? separator, bool signDisplayed, List<DurationPart> result, WorkCharge? charge)
    {
        if (separator is not null)
        {
            result.Add(new DurationPart("literal", separator, null));
        }

        var options = new NumberFormatOptions
        {
            Style = NumberStyle.Decimal,
            DigitOptions = Digits(NumberStyle.Decimal, units[index].Style == DurationUnitStyle.TwoDigit ? 2 : 1, index == SecondsIndex),
            UseGrouping = NumberGrouping.Off,
            SignDisplay = signDisplayed ? SignDisplay.Auto : SignDisplay.Never,
        };

        foreach (var part in NumberFormat(options, charge).FormatToParts(value, charge))
        {
            result.Add(new DurationPart(part.Type, part.Value, Table[index].Singular));
        }
    }

    /// <summary>
    /// ListFormatParts (<c>sec-listformatparts</c>): a unit list format of the base style, <c>short</c> for <c>digital</c>, over each
    /// element's text, and each element's parts in its place, the list's literals carrying no unit.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-listformatparts via Broiler.VM JsRealm.DurationFormat; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private List<DurationPart> ListFormatParts(List<List<DurationPart>> lists, WorkCharge? charge)
    {
        // THE LIST FORMAT, MADE AS ITS CONSTRUCTOR MAKES IT: one unit for the locale CanonicalizeLocaleList reads.
        charge?.Invoke(1);
        var listStyle = Style switch { DurationFormatStyle.Long => FormatStyle.Long, DurationFormatStyle.Narrow => FormatStyle.Narrow, _ => FormatStyle.Short };
        var listFormat = ListFormatter.Create(data, Locale, dataLocale, ListFormatType.Unit, listStyle);
        var strings = new List<string>(lists.Count);

        foreach (var parts in lists)
        {
            var text = new StringBuilder();

            foreach (var part in parts)
            {
                text.Append(part.Value);
            }

            strings.Add(text.ToString());
        }

        var result = new List<DurationPart>();
        var element = 0;

        foreach (var part in listFormat.FormatToParts(strings, charge))
        {
            if (part.Type == "element")
            {
                result.AddRange(lists[element]);
                element++;
            }
            else
            {
                result.Add(new DurationPart("literal", part.Value, null));
            }
        }

        return result;
    }

    /// <summary>
    /// A number format of the duration format's locale and numbering system, made as its constructor makes it from
    /// <paramref name="options"/>: one unit charged for the locale CanonicalizeLocaleList reads (Broiler.VM's <c>JsRealm.Intl.cs:282</c>).
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-partitiondurationformatpattern NumberFormat construction via Broiler.VM JsRealm.DurationFormat; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private NumberFormatter NumberFormat(NumberFormatOptions options, WorkCharge? charge)
    {
        charge?.Invoke(1);
        return NumberFormatter.Create(data, Locale, dataLocale, NumberingSystem, options);
    }

    /// <summary>
    /// SetNumberFormatDigitOptions over the options a duration's number format is made with: the minimum integer digits, and, for a
    /// value with a fraction, up to nine fraction digits or exactly <c>fractionalDigits</c>, truncated; otherwise the style's defaults.
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-setnumberformatdigitoptions for DurationFormat's number formats via Broiler.VM JsRealm.DurationFormat; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private DigitOptions Digits(NumberStyle style, int minimumIntegerDigits, bool fractions)
    {
        var values = fractions
            ? new DigitOptionValues(minimumIntegerDigits, false, null, null, true, FractionalDigits ?? 0, FractionalDigits ?? 9, 1, RoundingMode.Trunc, RoundingPriority.Auto, TrailingZeroDisplay.Auto)
            : new DigitOptionValues(minimumIntegerDigits, false, null, null, false, null, null, 1, RoundingMode.HalfExpand, RoundingPriority.Auto, TrailingZeroDisplay.Auto);
        var status = DigitOptionRules.SetNumberFormatDigitOptions(values, DigitOptionRules.NumberFormatDefaults(style, null, NumberNotation.Standard, data), NumberNotation.Standard, out var digits);

        // NO INPUT REACHES A REFUSAL: the digits are 0 to 9 and the minimum never exceeds the maximum.
        return status == IntlStatus.Completed ? digits! : throw new InvalidOperationException("SetNumberFormatDigitOptions refused a duration's number options: " + status);
    }

    /// <summary>
    /// The value of unit <paramref name="index"/> with ComputeFractionalDigits (<c>sec-computefractionaldigits</c>) added: the
    /// fractional units that follow it, each a thousandth of the one before, as an exact decimal (rows U11, U20 and U21).
    /// </summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-computefractionaldigits sec-nextunitfractional via Broiler.VM JsRealm.DurationFormat; IP=Low; Security=High; Resources=1; Fingerprint=TBF
    // Broiler-Falsified-If: a unit with its fractional units differs from the exact sum ComputeFractionalDigits makes, in digits or sign
    // Broiler-Human:        PENDING
    private IntlDecimal WithFractions(double[] record, int index, WorkCharge? charge)
    {
        var total = new BigInteger(record[index]);
        var digits = 0;

        for (var i = index + 1; i < UnitCount && units[i].Style == DurationUnitStyle.Fractional; i++)
        {
            total = (total * 1_000) + new BigInteger(record[i]);
            digits += 3;
        }

        if (digits == 0)
        {
            return IntlDecimal.FromNumber(record[index]);
        }

        // ASCII DIGITS (decision D19, row U20), where Broiler.VM wrote the integer with the invariant culture.
        var negative = total.Sign < 0;
        var text = BigIntText.ToString(BigInteger.Abs(total), 10, null).PadLeft(digits + 1, '0');
        var decimalText = (negative ? "-" : string.Empty) + text[..^digits] + "." + text[^digits..];
        return IntlDecimal.FromString(decimalText, BigIntPolicy.Unbounded, charge);
    }

    /// <summary>DurationSign (<c>sec-durationsign</c>): the sign of the first field that is not zero, or 0.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-durationsign via Broiler.VM JsRealm.DurationFormat; IP=Low; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private static int DurationSign(double[] record)
    {
        foreach (var value in record)
        {
            if (value < 0)
            {
                return -1;
            }

            if (value > 0)
            {
                return 1;
            }
        }

        return 0;
    }

    /// <summary>
    /// The record's ten fields in Table 20's order, once it is held to be what ToTemporalDuration makes: every field an integral
    /// Number and the whole a valid duration (the proposal's IsValidDuration, <see cref="TemporalLimits.CheckDuration"/>).
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=proposal-temporal sec-temporal-totemporalduration record precondition; IP=Low; Security=Medium; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private static double[] RecordOf(DurationFields duration)
    {
        double[] record =
        [
            duration.Years, duration.Months, duration.Weeks, duration.Days, duration.Hours, duration.Minutes, duration.Seconds,
            duration.Milliseconds, duration.Microseconds, duration.Nanoseconds,
        ];

        foreach (var value in record)
        {
            if (!double.IsFinite(value) || Math.Truncate(value) != value)
            {
                throw new ArgumentException("A duration's fields are integral Numbers, as ToTemporalDuration makes them.", nameof(duration));
            }
        }

        if (TemporalLimits.CheckDuration(duration) != TemporalStatus.Completed)
        {
            throw new ArgumentException("The duration is not valid (IsValidDuration); ToTemporalDuration refuses it before it is formatted.", nameof(duration));
        }

        return record;
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-isfractionalsecondunitname; IP=Low; Security=Low; Resources=0; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private static bool IsFractionalSecondUnit(DurationUnit unit) => unit is DurationUnit.Milliseconds or DurationUnit.Microseconds or DurationUnit.Nanoseconds;

    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-Intl.DurationFormat Table 20 units and styles; IP=Low; Security=Low; Resources=0; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private static UnitRow Row(DurationUnit unit) =>
        unit is >= DurationUnit.Years and <= DurationUnit.Nanoseconds
            ? Table[(int)unit]
            : throw new ArgumentOutOfRangeException(nameof(unit), unit, "Not a unit of Table 20.");

    /// <summary>One row of Table 20.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-Intl.DurationFormat Table 20 units and styles; IP=Low; Security=Low; Resources=0; Fingerprint=TBF
    // Broiler-Human:        PENDING
    private readonly record struct UnitRow(string Unit, string Singular, ImmutableArray<DurationUnitStyle> Styles, DurationUnitStyle Digital);
}
