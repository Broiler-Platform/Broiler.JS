// DIFFERENTIAL CHECK, BROILER.VM'S ARM: a test-only copy of Broiler.VM's code at its SJL-9 rule commit, the
// reference the differential check of Broiler.VM's JSD-0082, decision D22, compares the library with. It is compiled
// into this test project and nowhere else, never ships, and runs under the JIT only; see Differential/Vm/README.md.
//
// Copied from Broiler.VM at 4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f:
//   src/Broiler.VM.Profile.JavaScript/JsRealm.NumberFormat.cs
//     blob 7ae56e7fcc1a01aa976edb86b137b30bb30fd309: JsRealm's Intl.NumberFormat part, of which the constructor's reads and SetNumberFormatDigitOptions are kept; a second copy of the file at the same commit beside JsRealm.NumberFormat.cs, which keeps the sanctioned units, the two holding no member in common
//
// Changes from the source, none of which changes a result:
//   - the namespace;
//   - the generated SPDX and assurance header and the per-member assurance annotations removed;
//   - removed: `SanctionedUnits`, which the file's other copy keeps, and the members that build the prototype and its methods or read a script value's mathematical value, sub-area (c)'s (`SetupNumberFormat`, `RangeValues`, `PartsArray`, `ToIntlMathematicalValue`, `NumberFormatOfThis`, `UnwrapNumberFormat`, `ToLocaleNumberString`).

using System;
using System.Collections.Generic;

namespace Broiler.ECMAScript.Intl.Tests.Differential.Vm;

/// <summary>
/// <c>Intl.NumberFormat</c>, and <c>Number.prototype.toLocaleString</c> and
/// <c>BigInt.prototype.toLocaleString</c> through it (ECMA-402 s16; JSD-0044).
/// </summary>
/// <remarks>
/// <para>
/// <b>The constructor reads its options in ECMA-402's order</b> and keeps what it resolved; the
/// formatting is <see cref="JsNumberFormatter"/>'s. Called as a function on an object that inherits
/// from <c>Intl.NumberFormat.prototype</c>, it takes ECMA-402's normative optional legacy path: the
/// new formatter is stored on that object under the realm's <c>IntlLegacyConstructedSymbol</c>, and
/// the methods that unwrap read it from there.
/// </para>
/// </remarks>
internal sealed partial class JsRealm
{
    /// <summary><c>%Intl.NumberFormat.prototype%</c>, or nothing where the surface is not built.</summary>
    internal JsObject? NumberFormatPrototype { get; private set; }

    /// <summary><c>%Intl.NumberFormat%</c>, or nothing where the surface is not built.</summary>
    internal JsNativeFunction? NumberFormatConstructor { get; private set; }

    /// <summary><c>%Intl%.[[FallbackSymbol]]</c>, the legacy constructor's key.</summary>
    internal JsSymbol? IntlFallbackSymbol { get; private set; }

    /// <summary>
    /// ECMA-402's <c>Intl.NumberFormat</c> constructor: the prototype from the new target, then
    /// InitializeNumberFormat, then the legacy path when called as a function.
    /// </summary>
    internal static JsValue NewNumberFormat(JsEngine engine, JsValue newTarget, JsValue[] arguments, JsValue thisValue, bool called)
    {
        var realm = engine.Realm;
        var prototype = engine.PrototypeFromConstructor(newTarget, realm.NumberFormatPrototype!);
        var requested = CanonicalizeLocaleList(engine, Argument(arguments, 0));
        var options = CoerceOptionsToObject(engine, Argument(arguments, 1));

        _ = GetStringOption(engine, options, "localeMatcher", ["lookup", "best fit"], "best fit");
        var numberingSystem = GetStringOption(engine, options, "numberingSystem", null, null);

        if (numberingSystem is not null && !IsTypeSequence(numberingSystem))
        {
            return engine.ThrowRangeError("Intl.NumberFormat: the numberingSystem option is not a well-formed type: " + numberingSystem);
        }

        var numbers = engine.Intl!.Numbers;
        var systems = new List<string?> { "latn" };

        foreach (var name in numbers.NumberingSystems.Keys)
        {
            if (name != "latn")
            {
                systems.Add(name);
            }
        }

        systems.Sort(1, systems.Count - 1, StringComparer.Ordinal);
        var nuValues = systems.ToArray();

        var resolved = ResolveLocale(engine, requested, new Dictionary<string, string?>(StringComparer.Ordinal) { ["nu"] = numberingSystem }, ["nu"], (locale, key) => key == "nu" ? nuValues : [null]);

        var language = JsLocaleTag.Parse(resolved.DataLocale)?.Language ?? "en";

        // SetNumberFormatUnitOptions.
        var style = GetStringOption(engine, options, "style", ["decimal", "percent", "currency", "unit"], "decimal")!;
        var currency = GetStringOption(engine, options, "currency", null, null);

        if (currency is null)
        {
            if (style == "currency")
            {
                throw engine.Error("TypeError", "Intl.NumberFormat: the currency style needs a currency");
            }
        }
        else if (!IsWellFormedCurrencyCode(currency))
        {
            return engine.ThrowRangeError("Intl.NumberFormat: invalid currency code: " + currency);
        }

        var currencyDisplay = GetStringOption(engine, options, "currencyDisplay", ["code", "symbol", "narrowSymbol", "name"], "symbol")!;
        var currencySign = GetStringOption(engine, options, "currencySign", ["standard", "accounting"], "standard")!;
        var unit = GetStringOption(engine, options, "unit", null, null);

        if (unit is null)
        {
            if (style == "unit")
            {
                throw engine.Error("TypeError", "Intl.NumberFormat: the unit style needs a unit");
            }
        }
        else if (!IsWellFormedUnitIdentifier(unit))
        {
            return engine.ThrowRangeError("Intl.NumberFormat: invalid unit: " + unit);
        }

        var unitDisplay = GetStringOption(engine, options, "unitDisplay", ["short", "narrow", "long"], "short")!;
        var notation = GetStringOption(engine, options, "notation", ["standard", "scientific", "engineering", "compact"], "standard")!;

        int mnfdDefault;
        int mxfdDefault;

        if (style == "currency" && notation == "standard")
        {
            var upper = currency!.ToUpperInvariant();
            mnfdDefault = numbers.CurrencyDigits.TryGetValue(upper, out var digits) ? digits : 2;
            mxfdDefault = mnfdDefault;
        }
        else
        {
            mnfdDefault = 0;
            mxfdDefault = style == "percent" ? 0 : 3;
        }

        var digitOptions = SetNumberFormatDigitOptions(engine, options, mnfdDefault, mxfdDefault, notation);

        var compactDisplay = GetStringOption(engine, options, "compactDisplay", ["short", "long"], "short")!;
        var defaultUseGrouping = notation == "compact" ? "min2" : "auto";
        var useGrouping = GetBooleanOrStringNumberFormatOption(engine, options, "useGrouping", ["min2", "auto", "always", "true", "false"], defaultUseGrouping);

        var signDisplay = GetStringOption(engine, options, "signDisplay", ["auto", "never", "always", "exceptZero", "negative"], "auto")!;

        var formatter = new JsNumberFormatter(numbers, language, resolved.Keys["nu"] ?? "latn")
        {
            Locale = resolved.Locale,
            Style = style,
            Currency = style == "currency" ? currency!.ToUpperInvariant() : null,
            CurrencyDisplay = style == "currency" ? currencyDisplay : null,
            CurrencySign = style == "currency" ? currencySign : null,
            Unit = style == "unit" ? unit : null,
            UnitDisplay = style == "unit" ? unitDisplay : null,
            MinimumIntegerDigits = digitOptions.MinimumIntegerDigits,
            MinimumFractionDigits = digitOptions.MinimumFractionDigits,
            MaximumFractionDigits = digitOptions.MaximumFractionDigits,
            MinimumSignificantDigits = digitOptions.MinimumSignificantDigits,
            MaximumSignificantDigits = digitOptions.MaximumSignificantDigits,
            RoundingType = digitOptions.RoundingType,
            ComputedRoundingPriority = digitOptions.ComputedRoundingPriority,
            RoundingIncrement = digitOptions.RoundingIncrement,
            RoundingMode = digitOptions.RoundingMode,
            TrailingZeroDisplay = digitOptions.TrailingZeroDisplay,
            Notation = notation,
            CompactDisplay = notation == "compact" ? compactDisplay : null,
            UseGrouping = useGrouping,
            SignDisplay = signDisplay,
        };

        var made = new JsNumberFormatObject(prototype, formatter);

        // CHAINNUMBERFORMAT, ECMA-402'S NORMATIVE OPTIONAL LEGACY PATH: called as a function on an
        // object that is an Intl.NumberFormat by its prototype chain, the new format is stored on it.
        if (called && thisValue.IsObject &&
            engine.OrdinaryHasInstance(JsValue.Object(realm.NumberFormatConstructor!), thisValue))
        {
            var fields = new ObjectDescriptorFields
            {
                HasValue = true,
                Value = JsValue.Object(made),
                HasWritable = true,
                Writable = false,
                HasEnumerable = true,
                Enumerable = false,
                HasConfigurable = true,
                Configurable = false,
            };

            ObjectApplyDescriptorAt(engine, thisValue.AsObject(), JsValue.Symbol(realm.IntlFallbackSymbol!), fields);
            return thisValue;
        }

        return JsValue.Object(made);
    }

    /// <summary>What SetNumberFormatDigitOptions resolves.</summary>
    internal sealed record JsDigitOptions(int MinimumIntegerDigits, int? MinimumFractionDigits, int? MaximumFractionDigits, int? MinimumSignificantDigits, int? MaximumSignificantDigits,
        string RoundingType, string ComputedRoundingPriority, int RoundingIncrement, string RoundingMode, string TrailingZeroDisplay);

    /// <summary>ECMA-402's SetNumberFormatDigitOptions.</summary>
    internal static JsDigitOptions SetNumberFormatDigitOptions(JsEngine engine, JsValue options, int mnfdDefault, int mxfdDefault, string notation)
    {
        var mnid = GetNumberOption(engine, options, "minimumIntegerDigits", 1, 21, 1)!.Value;
        var mnfd = engine.GetProperty(options, "minimumFractionDigits");
        var mxfd = engine.GetProperty(options, "maximumFractionDigits");
        var mnsd = engine.GetProperty(options, "minimumSignificantDigits");
        var mxsd = engine.GetProperty(options, "maximumSignificantDigits");
        var roundingIncrement = GetNumberOption(engine, options, "roundingIncrement", 1, 5000, 1)!.Value;

        if (Array.IndexOf([1, 2, 5, 10, 20, 25, 50, 100, 200, 250, 500, 1000, 2000, 2500, 5000], roundingIncrement) < 0)
        {
            throw engine.Error("RangeError", "Intl.NumberFormat: roundingIncrement must be one of the admitted increments");
        }

        var roundingMode = GetStringOption(engine, options, "roundingMode",
            ["ceil", "floor", "expand", "trunc", "halfCeil", "halfFloor", "halfExpand", "halfTrunc", "halfEven"], "halfExpand")!;
        var roundingPriority = GetStringOption(engine, options, "roundingPriority", ["auto", "morePrecision", "lessPrecision"], "auto")!;
        var trailingZeroDisplay = GetStringOption(engine, options, "trailingZeroDisplay", ["auto", "stripIfInteger"], "auto")!;

        if (roundingIncrement != 1)
        {
            mxfdDefault = mnfdDefault;
        }

        var hasSd = mnsd.Type != JsType.Undefined || mxsd.Type != JsType.Undefined;
        var hasFd = mnfd.Type != JsType.Undefined || mxfd.Type != JsType.Undefined;
        var needSd = true;
        var needFd = true;

        if (roundingPriority == "auto")
        {
            needSd = hasSd;

            if (needSd || (!hasFd && notation == "compact"))
            {
                needFd = false;
            }
        }

        int? minSd = null, maxSd = null, minFd = null, maxFd = null;

        if (needSd)
        {
            if (hasSd)
            {
                minSd = DefaultNumberOption(engine, mnsd, 1, 21, 1, "minimumSignificantDigits");
                maxSd = DefaultNumberOption(engine, mxsd, minSd!.Value, 21, 21, "maximumSignificantDigits");
            }
            else
            {
                minSd = 1;
                maxSd = 21;
            }
        }

        if (needFd)
        {
            if (hasFd)
            {
                var min = DefaultNumberOption(engine, mnfd, 0, 100, null, "minimumFractionDigits");
                var max = DefaultNumberOption(engine, mxfd, 0, 100, null, "maximumFractionDigits");

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
                    throw engine.Error("RangeError", "Intl.NumberFormat: minimumFractionDigits is greater than maximumFractionDigits");
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

        string roundingType;
        string computed;

        if (!needSd && !needFd)
        {
            minFd = 0;
            maxFd = 0;
            minSd = 1;
            maxSd = 2;
            roundingType = "morePrecision";
            computed = "morePrecision";
        }
        else if (roundingPriority is "morePrecision" or "lessPrecision")
        {
            roundingType = roundingPriority;
            computed = roundingPriority;
        }
        else if (hasSd)
        {
            roundingType = "significantDigits";
            computed = "auto";
        }
        else
        {
            roundingType = "fractionDigits";
            computed = "auto";
        }

        if (roundingIncrement != 1)
        {
            if (roundingType != "fractionDigits")
            {
                throw engine.Error("TypeError", "Intl.NumberFormat: roundingIncrement needs fraction-digit rounding");
            }

            if (maxFd != minFd)
            {
                throw engine.Error("RangeError", "Intl.NumberFormat: roundingIncrement needs equal minimum and maximum fraction digits");
            }
        }

        return new JsDigitOptions(mnid, minFd, maxFd, minSd, maxSd, roundingType, computed, roundingIncrement, roundingMode, trailingZeroDisplay);
    }

    /// <summary>ECMA-402's GetNumberOption.</summary>
    internal static int? GetNumberOption(JsEngine engine, JsValue options, string property, int minimum, int maximum, int? fallback) =>
        DefaultNumberOption(engine, engine.GetProperty(options, property), minimum, maximum, fallback, property);

    /// <summary>ECMA-402's DefaultNumberOption.</summary>
    internal static int? DefaultNumberOption(JsEngine engine, JsValue value, int minimum, int maximum, int? fallback, string property)
    {
        if (value.Type == JsType.Undefined)
        {
            return fallback;
        }

        var number = engine.ToNumber(value);

        if (double.IsNaN(number) || number < minimum || number > maximum)
        {
            throw engine.Error("RangeError", property + " value is out of range.");
        }

        return (int)Math.Floor(number);
    }

    /// <summary>
    /// ECMA-402's GetBooleanOrStringNumberFormatOption, with <see langword="true"/> already read as
    /// <c>always</c> and <see langword="false"/> answered as <see langword="null"/>; the strings
    /// <c>"true"</c> and <c>"false"</c> answer the fallback, as the step after it requires.
    /// </summary>
    private static string? GetBooleanOrStringNumberFormatOption(JsEngine engine, JsValue options, string property, string[] values, string fallback)
    {
        var value = engine.GetProperty(options, property);

        if (value.Type == JsType.Undefined)
        {
            return fallback;
        }

        if (value.Type == JsType.Boolean && value.ToBooleanValue())
        {
            return "always";
        }

        if (!value.ToBooleanValue())
        {
            return null;
        }

        var text = engine.ToStringValue(value);

        if (text is "true" or "false")
        {
            return fallback;
        }

        if (Array.IndexOf(values, text) < 0)
        {
            throw engine.Error("RangeError", "Value " + text + " out of range for option " + property);
        }

        return text;
    }

    /// <summary>ECMA-402's IsWellFormedCurrencyCode: three ASCII letters.</summary>
    private static bool IsWellFormedCurrencyCode(string code) =>
        code.Length == 3 && char.IsAsciiLetter(code[0]) && char.IsAsciiLetter(code[1]) && char.IsAsciiLetter(code[2]);

    /// <summary>ECMA-402's IsWellFormedUnitIdentifier: a sanctioned unit, or two joined by <c>-per-</c>.</summary>
    private static bool IsWellFormedUnitIdentifier(string unit)
    {
        if (Array.IndexOf(SanctionedUnits, unit) >= 0)
        {
            return true;
        }

        var per = unit.IndexOf("-per-", StringComparison.Ordinal);
        return per > 0 && Array.IndexOf(SanctionedUnits, unit[..per]) >= 0 && Array.IndexOf(SanctionedUnits, unit[(per + 5)..]) >= 0;
    }
}

/// <summary>An <c>Intl.NumberFormat</c>: its formatter and its bound <c>format</c>.</summary>
internal sealed class JsNumberFormatObject : JsObject
{
    internal JsNumberFormatObject(JsObject prototype, JsNumberFormatter formatter) : base(prototype) => Formatter = formatter;

    internal JsNumberFormatter Formatter { get; }

    /// <summary>The <c>format</c> getter's function, made the first time it is read.</summary>
    internal JsNativeFunction? BoundFormat { get; set; }
}
