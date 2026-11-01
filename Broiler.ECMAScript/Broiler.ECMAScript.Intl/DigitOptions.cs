// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   18
// Annotated:        18/18
// Exempt:           19
// Human-reviewed:   0/18
// IP risk:          Low
// Security risk:    High
// Criteria:         1/1
// Resource impact:  1/10 max
// Unverified:       18
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using Broiler.ECMAScript.Numbers;

namespace Broiler.ECMAScript.Intl;

/// <summary>The <c>style</c> option of <c>Intl.NumberFormat</c> (ECMA-402 SetNumberFormatUnitOptions, <c>sec-setnumberformatunitoptions</c>).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-setnumberformatdigitoptions records and options; IP=Low; Security=Low; Resources=0; Fingerprint=BF2772
// Broiler-Human:        PENDING
public enum NumberStyle
{
    /// <summary><c>decimal</c>.</summary>
    Decimal = 0,

    /// <summary><c>percent</c>.</summary>
    Percent = 1,

    /// <summary><c>currency</c>.</summary>
    Currency = 2,

    /// <summary><c>unit</c>.</summary>
    Unit = 3,
}

/// <summary>The <c>notation</c> option of <c>Intl.NumberFormat</c> and <c>Intl.PluralRules</c> (ECMA-402 <c>sec-intl.numberformat</c>, <c>sec-intl.pluralrules</c>).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-setnumberformatdigitoptions records and options; IP=Low; Security=Low; Resources=0; Fingerprint=1D2D87
// Broiler-Human:        PENDING
public enum NumberNotation
{
    /// <summary><c>standard</c>.</summary>
    Standard = 0,

    /// <summary><c>scientific</c>.</summary>
    Scientific = 1,

    /// <summary><c>engineering</c>.</summary>
    Engineering = 2,

    /// <summary><c>compact</c>.</summary>
    Compact = 3,
}

/// <summary>The <c>compactDisplay</c> option (ECMA-402 <c>sec-intl.numberformat</c>, <c>sec-intl.pluralrules</c>).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-setnumberformatdigitoptions records and options; IP=Low; Security=Low; Resources=0; Fingerprint=DCD319
// Broiler-Human:        PENDING
public enum CompactDisplay
{
    /// <summary><c>short</c>.</summary>
    Short = 0,

    /// <summary><c>long</c>.</summary>
    Long = 1,
}

/// <summary>[[RoundingType]], which SetNumberFormatDigitOptions sets (<c>sec-setnumberformatdigitoptions</c>).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-setnumberformatdigitoptions records and options; IP=Low; Security=Low; Resources=0; Fingerprint=E693AE
// Broiler-Human:        PENDING
public enum NumberRoundingType
{
    /// <summary><c>fraction-digits</c>.</summary>
    FractionDigits = 0,

    /// <summary><c>significant-digits</c>.</summary>
    SignificantDigits = 1,

    /// <summary><c>more-precision</c>.</summary>
    MorePrecision = 2,

    /// <summary><c>less-precision</c>.</summary>
    LessPrecision = 3,
}

/// <summary>The <c>roundingPriority</c> option, and [[ComputedRoundingPriority]] (<c>sec-setnumberformatdigitoptions</c>).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-setnumberformatdigitoptions records and options; IP=Low; Security=Low; Resources=0; Fingerprint=FD658F
// Broiler-Human:        PENDING
public enum RoundingPriority
{
    /// <summary><c>auto</c>.</summary>
    Auto = 0,

    /// <summary><c>morePrecision</c>.</summary>
    MorePrecision = 1,

    /// <summary><c>lessPrecision</c>.</summary>
    LessPrecision = 2,
}

/// <summary>The <c>trailingZeroDisplay</c> option (<c>sec-setnumberformatdigitoptions</c>).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-setnumberformatdigitoptions records and options; IP=Low; Security=Low; Resources=0; Fingerprint=9FFA49
// Broiler-Human:        PENDING
public enum TrailingZeroDisplay
{
    /// <summary><c>auto</c>.</summary>
    Auto = 0,

    /// <summary><c>stripIfInteger</c>.</summary>
    StripIfInteger = 1,
}

/// <summary>
/// The option values SetNumberFormatDigitOptions (<c>sec-setnumberformatdigitoptions</c>) works on, once the engine has read
/// them: the digit options it converted at the step that converts them, and whether the significant-digit and fraction-digit
/// options were given.
/// </summary>
/// <param name="MinimumIntegerDigits">GetNumberOption's <c>minimumIntegerDigits</c>, 1 to 21.</param>
/// <param name="HasSignificantDigits">Whether <c>minimumSignificantDigits</c> or <c>maximumSignificantDigits</c> is not <c>undefined</c> (_hasSd_).</param>
/// <param name="MinimumSignificantDigits">DefaultNumberOption's <c>minimumSignificantDigits</c> (1 to 21, fallback 1), where <see cref="DigitOptionRules.Plan"/> says it is converted and <paramref name="HasSignificantDigits"/>; otherwise ignored.</param>
/// <param name="MaximumSignificantDigits">DefaultNumberOption's <c>maximumSignificantDigits</c> (the minimum to 21, fallback 21), likewise.</param>
/// <param name="HasFractionDigits">Whether <c>minimumFractionDigits</c> or <c>maximumFractionDigits</c> is not <c>undefined</c> (_hasFd_).</param>
/// <param name="MinimumFractionDigits">DefaultNumberOption's <c>minimumFractionDigits</c> (0 to 100, fallback <c>undefined</c>, here <see langword="null"/>), where it is converted; otherwise ignored.</param>
/// <param name="MaximumFractionDigits">DefaultNumberOption's <c>maximumFractionDigits</c>, likewise.</param>
/// <param name="RoundingIncrement">GetNumberOption's <c>roundingIncrement</c>, an admitted increment (<see cref="DigitOptionRules.IsAdmittedRoundingIncrement"/>).</param>
/// <param name="RoundingMode">GetOption's <c>roundingMode</c>.</param>
/// <param name="RoundingPriority">GetOption's <c>roundingPriority</c>.</param>
/// <param name="TrailingZeroDisplay">GetOption's <c>trailingZeroDisplay</c>.</param>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-setnumberformatdigitoptions records and options; IP=Low; Security=Low; Resources=0; Fingerprint=6F7EEC
// Broiler-Human:        PENDING
public sealed record DigitOptionValues(
    int MinimumIntegerDigits,
    bool HasSignificantDigits,
    int? MinimumSignificantDigits,
    int? MaximumSignificantDigits,
    bool HasFractionDigits,
    int? MinimumFractionDigits,
    int? MaximumFractionDigits,
    int RoundingIncrement,
    RoundingMode RoundingMode,
    RoundingPriority RoundingPriority,
    TrailingZeroDisplay TrailingZeroDisplay);

/// <summary>Which digit options SetNumberFormatDigitOptions converts: _needSd_ and _needFd_ (<c>sec-setnumberformatdigitoptions</c>).</summary>
/// <param name="NeedSignificantDigits">Whether the significant-digit options are converted and resolved.</param>
/// <param name="NeedFractionDigits">Whether the fraction-digit options are converted and resolved.</param>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-setnumberformatdigitoptions records and options; IP=Low; Security=Low; Resources=0; Fingerprint=E42C3F
// Broiler-Human:        PENDING
public readonly record struct DigitOptionsPlan(bool NeedSignificantDigits, bool NeedFractionDigits);

/// <summary>The fraction-digit defaults the constructors hand SetNumberFormatDigitOptions: _mnfdDefault_ and _mxfdDefault_.</summary>
/// <param name="Minimum">_mnfdDefault_.</param>
/// <param name="Maximum">_mxfdDefault_.</param>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-setnumberformatdigitoptions records and options; IP=Low; Security=Low; Resources=0; Fingerprint=554E25
// Broiler-Human:        PENDING
public readonly record struct FractionDigitDefaults(int Minimum, int Maximum);

/// <summary>
/// The digit options SetNumberFormatDigitOptions (<c>sec-setnumberformatdigitoptions</c>) resolves, the internal slots of an
/// <c>Intl.NumberFormat</c> or an <c>Intl.PluralRules</c> that FormatNumericToString reads (rows M4 to M7, P6 and P7).
/// </summary>
/// <param name="MinimumIntegerDigits">[[MinimumIntegerDigits]].</param>
/// <param name="MinimumFractionDigits">[[MinimumFractionDigits]], or <see langword="null"/> where it is not set.</param>
/// <param name="MaximumFractionDigits">[[MaximumFractionDigits]], or <see langword="null"/>.</param>
/// <param name="MinimumSignificantDigits">[[MinimumSignificantDigits]], or <see langword="null"/>.</param>
/// <param name="MaximumSignificantDigits">[[MaximumSignificantDigits]], or <see langword="null"/>.</param>
/// <param name="RoundingType">[[RoundingType]].</param>
/// <param name="ComputedRoundingPriority">[[ComputedRoundingPriority]].</param>
/// <param name="RoundingIncrement">[[RoundingIncrement]].</param>
/// <param name="RoundingMode">[[RoundingMode]].</param>
/// <param name="TrailingZeroDisplay">[[TrailingZeroDisplay]].</param>
// Broiler-AI:           Origin=AI; Spec=ECMA-402 sec-setnumberformatdigitoptions records and options; IP=Low; Security=Low; Resources=0; Fingerprint=FA03B2
// Broiler-Human:        PENDING
public sealed record DigitOptions(
    int MinimumIntegerDigits,
    int? MinimumFractionDigits,
    int? MaximumFractionDigits,
    int? MinimumSignificantDigits,
    int? MaximumSignificantDigits,
    NumberRoundingType RoundingType,
    RoundingPriority ComputedRoundingPriority,
    int RoundingIncrement,
    RoundingMode RoundingMode,
    TrailingZeroDisplay TrailingZeroDisplay);

/// <summary>
/// ECMA-402's SetNumberFormatDigitOptions (<c>sec-setnumberformatdigitoptions</c>) and DefaultNumberOption
/// (<c>sec-defaultnumberoption</c>) after the engine's reads: the steps that decide, as the record they resolve, with each
/// refusal a status (rows M4 to M7, P6 and P7).
/// </summary>
/// <remarks>
/// <para>
/// <b>The reads are the engine's</b> (convention 2): SetNumberFormatDigitOptions reads <c>minimumIntegerDigits</c> and converts
/// it, reads the four digit options without converting them, reads and converts <c>roundingIncrement</c>, and reads the three
/// string options, in that order; then it converts only the digit options <see cref="Plan"/> names, the minimum before the
/// maximum. The engine runs each read and each ToNumber at its step, hands each converted Number to
/// <see cref="DefaultNumberOption"/> for its range, and hands the values to <see cref="SetNumberFormatDigitOptions"/>, which
/// takes the decisions after them. So no option is read twice or after construction (row M4).
/// </para>
/// <para>
/// <b>Ported from Broiler.VM's <c>JsRealm.SetNumberFormatDigitOptions</c>, <c>DefaultNumberOption</c> and
/// <c>NewNumberFormat</c>'s defaults</b> at its SJL-9 rule commit <c>4cf9ca44</c>, with statuses where it threw and ASCII
/// casing where it called <c>ToUpperInvariant</c> (decision D19). Nothing here grows with its input, so nothing takes a meter.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-setnumberformatdigitoptions sec-defaultnumberoption via Broiler.VM JsRealm.NumberFormat; IP=Low; Security=Low; Resources=0; Fingerprint=521DB1
// Broiler-Human:        PENDING
public static class DigitOptionRules
{
    /// <summary>The increments SetNumberFormatDigitOptions admits, in order.</summary>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-setnumberformatdigitoptions sec-defaultnumberoption via Broiler.VM JsRealm.NumberFormat; IP=Low; Security=Low; Resources=0; Fingerprint=71A11F
    // Broiler-Human:        PENDING
    private static ReadOnlySpan<int> AdmittedIncrements => [1, 2, 5, 10, 20, 25, 50, 100, 200, 250, 500, 1000, 2000, 2500, 5000];

    /// <summary>
    /// DefaultNumberOption's check of a converted value (<c>sec-defaultnumberoption</c>, steps 2 to 4): not-a-number or outside
    /// the range is refused; otherwise the value's floor.
    /// </summary>
    /// <param name="value">ToNumber of the option, which the engine computed.</param>
    /// <param name="minimum">The least value admitted.</param>
    /// <param name="maximum">The greatest value admitted.</param>
    /// <param name="result">The floor of <paramref name="value"/>, or 0.</param>
    /// <returns><see cref="IntlStatus.Completed"/>, or <see cref="IntlStatus.NumberOptionOutOfRange"/>, a <c>RangeError</c>.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-setnumberformatdigitoptions sec-defaultnumberoption via Broiler.VM JsRealm.NumberFormat; IP=Low; Security=Low; Resources=1; Fingerprint=53EAF5
    // Broiler-Human:        PENDING
    public static IntlStatus DefaultNumberOption(double value, int minimum, int maximum, out int result)
    {
        if (double.IsNaN(value) || value < minimum || value > maximum)
        {
            result = 0;
            return IntlStatus.NumberOptionOutOfRange;
        }

        result = (int)Math.Floor(value);
        return IntlStatus.Completed;
    }

    /// <summary>
    /// Whether SetNumberFormatDigitOptions admits <paramref name="increment"/> as <c>roundingIncrement</c>: its step 8, which an
    /// engine asks right after GetNumberOption reads the increment and before it reads <c>roundingMode</c>, so that the
    /// <c>RangeError</c> comes before the later reads a script can observe. When it is <see langword="false"/>, the engine
    /// throws the <c>RangeError</c> <see cref="IntlStatus.RoundingIncrementNotAdmitted"/> names;
    /// <see cref="SetNumberFormatDigitOptions"/> returns that status too, for an increment no engine checked.
    /// </summary>
    /// <param name="increment">The increment GetNumberOption read, 1 to 5000.</param>
    /// <returns><see langword="true"/> for 1, 2, 5, 10, 20, 25, 50, 100, 200, 250, 500, 1000, 2000, 2500 and 5000.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-setnumberformatdigitoptions sec-defaultnumberoption via Broiler.VM JsRealm.NumberFormat; IP=Low; Security=Low; Resources=1; Fingerprint=D8C802
    // Broiler-Human:        PENDING
    public static bool IsAdmittedRoundingIncrement(int increment) => AdmittedIncrements.Contains(increment);

    /// <summary>
    /// Which digit options SetNumberFormatDigitOptions goes on to convert: _needSd_ and _needFd_, from whether each pair was
    /// given, the rounding priority and the notation.
    /// </summary>
    /// <param name="hasSignificantDigits">_hasSd_.</param>
    /// <param name="hasFractionDigits">_hasFd_.</param>
    /// <param name="roundingPriority">The <c>roundingPriority</c> option.</param>
    /// <param name="notation">The notation.</param>
    /// <returns>The plan.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-setnumberformatdigitoptions sec-defaultnumberoption via Broiler.VM JsRealm.NumberFormat; IP=Low; Security=Low; Resources=1; Fingerprint=932E15
    // Broiler-Human:        PENDING
    public static DigitOptionsPlan Plan(bool hasSignificantDigits, bool hasFractionDigits, RoundingPriority roundingPriority, NumberNotation notation)
    {
        var needSd = true;
        var needFd = true;

        if (roundingPriority == RoundingPriority.Auto)
        {
            needSd = hasSignificantDigits;

            if (needSd || (!hasFractionDigits && notation == NumberNotation.Compact))
            {
                needFd = false;
            }
        }

        return new DigitOptionsPlan(needSd, needFd);
    }

    /// <summary>
    /// The fraction-digit defaults <c>Intl.NumberFormat</c> hands SetNumberFormatDigitOptions (<c>sec-intl.numberformat</c>):
    /// the currency's digits for a currency in standard notation (CurrencyDigits, <c>sec-currencydigits</c>, 2 where the data
    /// states none), 0 and 0 for a percent, and 0 and 3 otherwise (row M5). <c>Intl.PluralRules</c> hands 0 and 3.
    /// </summary>
    /// <param name="style">The style.</param>
    /// <param name="currency">The currency code, three ASCII letters in either case, for the currency style; otherwise ignored.</param>
    /// <param name="notation">The notation.</param>
    /// <param name="data">The locale data whose currency digits are read.</param>
    /// <returns>The defaults.</returns>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-setnumberformatdigitoptions sec-defaultnumberoption via Broiler.VM JsRealm.NumberFormat; IP=Low; Security=Low; Resources=1; Fingerprint=2DBAD6
    // Broiler-Human:        PENDING
    public static FractionDigitDefaults NumberFormatDefaults(NumberStyle style, string? currency, NumberNotation notation, ILocaleData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (style == NumberStyle.Currency && notation == NumberNotation.Standard)
        {
            ArgumentNullException.ThrowIfNull(currency);
            var digits = data.CurrencyDigits(AsciiUpper(currency)) ?? 2;
            return new FractionDigitDefaults(digits, digits);
        }

        return new FractionDigitDefaults(0, style == NumberStyle.Percent ? 0 : 3);
    }

    /// <summary>
    /// SetNumberFormatDigitOptions' decisions (<c>sec-setnumberformatdigitoptions</c>) over the values the engine read and
    /// converted: the fraction-digit default moved by an increment, the pairs resolved under _needSd_ and _needFd_, the rounding
    /// type and computed priority, and the increment's refusals.
    /// </summary>
    /// <param name="values">The values, as the engine read and converted them.</param>
    /// <param name="defaults">_mnfdDefault_ and _mxfdDefault_.</param>
    /// <param name="notation">The notation.</param>
    /// <param name="options">The resolved options, or <see langword="null"/>.</param>
    /// <returns>
    /// <see cref="IntlStatus.Completed"/>; or <see cref="IntlStatus.RoundingIncrementNotAdmitted"/> (step 8, which an engine
    /// asks first with <see cref="IsAdmittedRoundingIncrement"/>, at its own step), <see cref="IntlStatus.FractionDigitsOutOfOrder"/>
    /// and <see cref="IntlStatus.RoundingIncrementNeedsEqualFractionDigits"/>, each a <c>RangeError</c>, or
    /// <see cref="IntlStatus.RoundingIncrementNeedsFractionDigits"/>, a <c>TypeError</c>.
    /// </returns>
    /// <exception cref="ArgumentException">A significant-digit value is missing where the plan converts it.</exception>
    // Broiler-AI:           Origin=Ported; Spec=ECMA-402 sec-setnumberformatdigitoptions sec-defaultnumberoption via Broiler.VM JsRealm.NumberFormat; IP=Low; Security=High; Resources=1; Fingerprint=5E6F7D
    // Broiler-Falsified-If: a resolved slot differs from SetNumberFormatDigitOptions' for the same option values, or a refusal is another status or none
    // Broiler-Human:        PENDING
    public static IntlStatus SetNumberFormatDigitOptions(DigitOptionValues values, FractionDigitDefaults defaults, NumberNotation notation, out DigitOptions? options)
    {
        ArgumentNullException.ThrowIfNull(values);
        options = null;

        if (!IsAdmittedRoundingIncrement(values.RoundingIncrement))
        {
            return IntlStatus.RoundingIncrementNotAdmitted;
        }

        var mnfdDefault = defaults.Minimum;
        var mxfdDefault = values.RoundingIncrement != 1 ? mnfdDefault : defaults.Maximum;
        var plan = Plan(values.HasSignificantDigits, values.HasFractionDigits, values.RoundingPriority, notation);

        int? minSd = null, maxSd = null, minFd = null, maxFd = null;

        if (plan.NeedSignificantDigits)
        {
            if (values.HasSignificantDigits)
            {
                minSd = values.MinimumSignificantDigits ?? throw new ArgumentException("the minimum significant digits are converted here", nameof(values));
                maxSd = values.MaximumSignificantDigits ?? throw new ArgumentException("the maximum significant digits are converted here", nameof(values));
            }
            else
            {
                minSd = 1;
                maxSd = 21;
            }
        }

        if (plan.NeedFractionDigits)
        {
            if (values.HasFractionDigits)
            {
                var min = values.MinimumFractionDigits;
                var max = values.MaximumFractionDigits;

                if (min is null)
                {
                    min = Math.Min(mnfdDefault, max!.Value);
                }
                else if (max is null)
                {
                    max = Math.Max(mxfdDefault, min.Value);
                }
                else if (min > max)
                {
                    return IntlStatus.FractionDigitsOutOfOrder;
                }

                minFd = min;
                maxFd = max;
            }
            else
            {
                minFd = mnfdDefault;
                maxFd = mxfdDefault;
            }
        }

        NumberRoundingType roundingType;
        RoundingPriority computed;

        if (!plan.NeedSignificantDigits && !plan.NeedFractionDigits)
        {
            minFd = 0;
            maxFd = 0;
            minSd = 1;
            maxSd = 2;
            roundingType = NumberRoundingType.MorePrecision;
            computed = RoundingPriority.MorePrecision;
        }
        else if (values.RoundingPriority == RoundingPriority.MorePrecision)
        {
            roundingType = NumberRoundingType.MorePrecision;
            computed = RoundingPriority.MorePrecision;
        }
        else if (values.RoundingPriority == RoundingPriority.LessPrecision)
        {
            roundingType = NumberRoundingType.LessPrecision;
            computed = RoundingPriority.LessPrecision;
        }
        else if (values.HasSignificantDigits)
        {
            roundingType = NumberRoundingType.SignificantDigits;
            computed = RoundingPriority.Auto;
        }
        else
        {
            roundingType = NumberRoundingType.FractionDigits;
            computed = RoundingPriority.Auto;
        }

        if (values.RoundingIncrement != 1)
        {
            if (roundingType != NumberRoundingType.FractionDigits)
            {
                return IntlStatus.RoundingIncrementNeedsFractionDigits;
            }

            if (maxFd != minFd)
            {
                return IntlStatus.RoundingIncrementNeedsEqualFractionDigits;
            }
        }

        options = new DigitOptions(values.MinimumIntegerDigits, minFd, maxFd, minSd, maxSd, roundingType, computed,
            values.RoundingIncrement, values.RoundingMode, values.TrailingZeroDisplay);
        return IntlStatus.Completed;
    }

    /// <summary>ASCII upper casing of a currency code (decision D19, where Broiler.VM called <c>ToUpperInvariant</c>).</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D19 ASCII casing; IP=Low; Security=Low; Resources=1; Fingerprint=FFD669
    // Broiler-Human:        PENDING
    internal static string AsciiUpper(string code)
    {
        var chars = code.ToCharArray();

        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] is >= 'a' and <= 'z')
            {
                chars[i] = (char)(chars[i] - 32);
            }
        }

        return new string(chars);
    }
}
