// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

namespace Broiler.ECMAScript.Intl;

/// <summary>How a currency is written: [[CurrencyDisplay]] of an <c>Intl.NumberFormat</c> (<c>sec-setnumberformatunitoptions</c>).</summary>
public enum CurrencyDisplay
{
    /// <summary><c>symbol</c>: the locale's symbol, such as <c>$</c> or <c>US$</c>.</summary>
    Symbol = 0,

    /// <summary><c>narrowSymbol</c>: the locale's narrow symbol, such as <c>$</c>.</summary>
    NarrowSymbol = 1,

    /// <summary><c>code</c>: the ISO 4217 code.</summary>
    Code = 2,

    /// <summary><c>name</c>: the locale's name for the currency, by the plural of the number.</summary>
    Name = 3,
}

/// <summary>[[CurrencySign]] of an <c>Intl.NumberFormat</c>: how a negative currency amount is written.</summary>
public enum CurrencySign
{
    /// <summary><c>standard</c>: the minus sign.</summary>
    Standard = 0,

    /// <summary><c>accounting</c>: the locale's accounting pattern, such as parentheses.</summary>
    Accounting = 1,
}

/// <summary>[[UnitDisplay]] of an <c>Intl.NumberFormat</c>: the width of a unit's pattern.</summary>
public enum UnitDisplay
{
    /// <summary><c>short</c>.</summary>
    Short = 0,

    /// <summary><c>narrow</c>.</summary>
    Narrow = 1,

    /// <summary><c>long</c>.</summary>
    Long = 2,
}

/// <summary>[[UseGrouping]] of an <c>Intl.NumberFormat</c> (<c>sec-intl.numberformat</c>, GetBooleanOrStringNumberFormatOption).</summary>
public enum NumberGrouping
{
    /// <summary><c>auto</c>: the locale's minimum grouping digits.</summary>
    Auto = 0,

    /// <summary><c>always</c>.</summary>
    Always = 1,

    /// <summary><c>min2</c>: only when the leading group holds two digits or more.</summary>
    Min2 = 2,

    /// <summary><see langword="false"/>: no grouping separator.</summary>
    Off = 3,
}

/// <summary>[[SignDisplay]] of an <c>Intl.NumberFormat</c>.</summary>
public enum SignDisplay
{
    /// <summary><c>auto</c>: the sign of negative numbers, negative zero included.</summary>
    Auto = 0,

    /// <summary><c>never</c>.</summary>
    Never = 1,

    /// <summary><c>always</c>.</summary>
    Always = 2,

    /// <summary><c>exceptZero</c>: a sign for every number but zero.</summary>
    ExceptZero = 3,

    /// <summary><c>negative</c>: the sign of negative numbers, not of negative zero.</summary>
    Negative = 4,
}

/// <summary>
/// What an <c>Intl.NumberFormat</c> constructor resolved from its options (<c>sec-intl.numberformat</c>, Table 15's internal
/// slots), the record <see cref="NumberFormatter.Create"/> takes. The engine reads the options in the constructor's order,
/// checks each as the specification checks it (<see cref="NumberFormatter.IsWellFormedCurrencyCode"/>,
/// <see cref="NumberFormatter.IsWellFormedUnitIdentifier"/>, <see cref="DigitOptionRules"/>), and hands the library what it
/// read (convention 2: resolved options as records, never an options object).
/// </summary>
/// <remarks>
/// A slot the style does not use is ignored: <see cref="Currency"/>, <see cref="CurrencyDisplay"/> and
/// <see cref="CurrencySign"/> outside the currency style, <see cref="Unit"/> and <see cref="UnitDisplay"/> outside the unit
/// style, and <see cref="CompactDisplay"/> outside compact notation, as the constructor leaves those slots unset.
/// </remarks>
public sealed record NumberFormatOptions
{
    /// <summary>[[Style]].</summary>
    public NumberStyle Style { get; init; }

    /// <summary>[[Currency]]: a well-formed currency code, in either case; required by the currency style.</summary>
    public string? Currency { get; init; }

    /// <summary>[[CurrencyDisplay]].</summary>
    public CurrencyDisplay CurrencyDisplay { get; init; }

    /// <summary>[[CurrencySign]].</summary>
    public CurrencySign CurrencySign { get; init; }

    /// <summary>[[Unit]]: a well-formed unit identifier; required by the unit style.</summary>
    public string? Unit { get; init; }

    /// <summary>[[UnitDisplay]].</summary>
    public UnitDisplay UnitDisplay { get; init; }

    /// <summary>What SetNumberFormatDigitOptions resolved, with the defaults <see cref="DigitOptionRules.NumberFormatDefaults"/> gives.</summary>
    public required DigitOptions DigitOptions { get; init; }

    /// <summary>[[Notation]].</summary>
    public NumberNotation Notation { get; init; }

    /// <summary>[[CompactDisplay]].</summary>
    public CompactDisplay CompactDisplay { get; init; }

    /// <summary>[[UseGrouping]]: <see cref="NumberGrouping.Min2"/> is the constructor's default in compact notation, <see cref="NumberGrouping.Auto"/> otherwise.</summary>
    public NumberGrouping UseGrouping { get; init; }

    /// <summary>[[SignDisplay]].</summary>
    public SignDisplay SignDisplay { get; init; }
}

/// <summary>One part of a formatted number or range (<c>sec-formatnumericToparts</c>, <c>sec-formatnumericrangetoparts</c>).</summary>
/// <param name="Type">The part's type: <c>integer</c>, <c>group</c>, <c>decimal</c>, <c>fraction</c>, <c>minusSign</c>,
/// <c>plusSign</c>, <c>percentSign</c>, <c>currency</c>, <c>unit</c>, <c>compact</c>, <c>exponentSeparator</c>,
/// <c>exponentMinusSign</c>, <c>exponentInteger</c>, <c>approximatelySign</c>, <c>nan</c>, <c>infinity</c> or <c>literal</c>.</param>
/// <param name="Value">The part's text.</param>
/// <param name="Source">In a range, <c>startRange</c>, <c>endRange</c> or <c>shared</c>; <see langword="null"/> for one number.</param>
public readonly record struct NumberPart(string Type, string Value, string? Source);
