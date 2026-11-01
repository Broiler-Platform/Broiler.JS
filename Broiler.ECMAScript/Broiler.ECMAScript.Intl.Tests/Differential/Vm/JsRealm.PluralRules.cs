// DIFFERENTIAL CHECK, BROILER.VM'S ARM: a test-only copy of Broiler.VM's code at its SJL-9 rule commit, the
// reference the differential check of Broiler.VM's JSD-0082, decision D22, compares the library with. It is compiled
// into this test project and nowhere else, never ships, and runs under the JIT only; see Differential/Vm/README.md.
//
// Copied from Broiler.VM at 4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f:
//   src/Broiler.VM.Profile.JavaScript/JsRealm.PluralRules.cs
//     blob a6358f49750fd30674c07b8f73e7ff34c2b2b06b: JsRealm's Intl.PluralRules part and JsPluralRulesObject, of which the constructor, ResolvePlural, ResolvePluralRange and the categories are kept
//
// Changes from the source, none of which changes a result:
//   - the namespace;
//   - the generated SPDX and assurance header and the per-member assurance annotations removed;
//   - removed: the members that build the prototype and its methods (`SetupPluralRules`, `PluralRulesOfThis`).

using System;
using System.Collections.Generic;

namespace Broiler.ECMAScript.Intl.Tests.Differential.Vm;

/// <summary>
/// <c>Intl.PluralRules</c> (ECMA-402 s17; JSD-0047): a number's cardinal or ordinal plural category,
/// and a range's, by CLDR's rules for the format's language.
/// </summary>
/// <remarks>
/// <para>
/// <b>A plural rules object is a decimal number format that selects instead of writing.</b> Its digit
/// options are SetNumberFormatDigitOptions's and its rounding is FormatNumericToString's, both
/// <see cref="JsNumberFormatter"/>'s, so <c>select</c> reads the operands of the string a decimal
/// <c>Intl.NumberFormat</c> with the same options would write.
/// </para>
/// <para>
/// <b>A range's category is CLDR's plural ranges for the language</b>, for both types. CLDR states
/// ranges for cardinals only, and ICU resolves an ordinal range by the same data.
/// </para>
/// </remarks>
internal sealed partial class JsRealm
{
    /// <summary><c>%Intl.PluralRules.prototype%</c>, or nothing where the surface is not built.</summary>
    internal JsObject? PluralRulesPrototype { get; private set; }

    /// <summary><c>%Intl.PluralRules%</c>, or nothing where the surface is not built.</summary>
    internal JsNativeFunction? PluralRulesConstructor { get; private set; }

    /// <summary>The plural categories in the order <c>resolvedOptions</c> lists them (s17.3.2).</summary>
    private static readonly string[] PluralCategoryOrder = ["zero", "one", "two", "few", "many", "other"];

    /// <summary>ECMA-402's <c>Intl.PluralRules</c> constructor (s17.1.1): the options read in its order.</summary>
    internal static JsValue NewPluralRules(JsEngine engine, JsValue newTarget, JsValue[] arguments)
    {
        var prototype = engine.PrototypeFromConstructor(newTarget, engine.Realm.PluralRulesPrototype!);

        // RESOLVEOPTIONS: the locales, the options coerced, the matcher, and no relevant keys.
        var requested = CanonicalizeLocaleList(engine, Argument(arguments, 0));
        var options = CoerceOptionsToObject(engine, Argument(arguments, 1));
        _ = GetStringOption(engine, options, "localeMatcher", ["lookup", "best fit"], "best fit");

        var resolved = ResolveLocale(engine, requested, new Dictionary<string, string?>(StringComparer.Ordinal), [], static (locale, key) => [null]);

        var type = GetStringOption(engine, options, "type", ["cardinal", "ordinal"], "cardinal")!;
        var notation = GetStringOption(engine, options, "notation", ["standard", "scientific", "engineering", "compact"], "standard")!;
        var compactDisplay = GetStringOption(engine, options, "compactDisplay", ["short", "long"], "short")!;
        var digitOptions = SetNumberFormatDigitOptions(engine, options, 0, 3, notation);
        var language = JsLocaleTag.Parse(resolved.DataLocale)?.Language ?? "en";

        var formatter = new JsNumberFormatter(engine.Intl!.Numbers, language, "latn")
        {
            Locale = resolved.Locale,
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
        };

        return JsValue.Object(new JsPluralRulesObject(prototype, formatter, type, language));
    }

    /// <summary>
    /// ECMA-402's ResolvePlural (s17.5.2): <c>other</c> for not-a-number and the infinities, and
    /// otherwise PluralRuleSelect over FormatNumericToString's string.
    /// </summary>
    /// <remarks>
    /// PluralRuleSelect reads the operands of that string. The compact and scientific exponents, the
    /// operands <c>c</c> and <c>e</c>, are 0: the string is the whole value's, and of the supported
    /// languages' rules none reads them.
    /// </remarks>
    private static (string Category, string Formatted) ResolvePlural(JsEngine engine, JsPluralRulesObject rules, JsDecimal n)
    {
        if (!n.IsFinite)
        {
            return ("other", n.Kind == JsDecimalKind.NaN ? "NaN" : n.Kind == JsDecimalKind.PositiveInfinity ? "Infinity" : "-Infinity");
        }

        var (_, formatted) = rules.Formatter.FormatNumericToString(engine, n);
        var operands = JsPluralOperands.Of(formatted, 0);
        return (engine.Intl!.Numbers.Plural(rules.Language, operands, rules.Type == "ordinal"), formatted);
    }

    /// <summary>
    /// ECMA-402's ResolvePluralRange (s17.5.4): a <c>RangeError</c> at not-a-number, the start's
    /// category where both ends write the same string, and otherwise CLDR's range rule.
    /// </summary>
    private static string ResolvePluralRange(JsEngine engine, JsPluralRulesObject rules, JsDecimal x, JsDecimal y)
    {
        if (x.Kind == JsDecimalKind.NaN || y.Kind == JsDecimalKind.NaN)
        {
            throw engine.Error("RangeError", "Intl.PluralRules: a range cannot start or end at NaN");
        }

        var (Category, Formatted) = ResolvePlural(engine, rules, x);
        var end = ResolvePlural(engine, rules, y);

        if (Formatted == end.Formatted)
        {
            return Category;
        }

        return engine.Intl!.Numbers.PluralRange(rules.Language, Category, end.Category);
    }

    /// <summary>Every category PluralRuleSelect can answer for the language and type, in s17.3.2's order.</summary>
    private static List<string> PluralCategoriesOf(JsEngine engine, JsPluralRulesObject rules)
    {
        var numbers = engine.Intl!.Numbers;
        var categories = new HashSet<string>(StringComparer.Ordinal) { "other" };

        if ((rules.Type == "ordinal" ? numbers.Ordinals : numbers.Plurals).TryGetValue(rules.Language, out var list))
        {
            foreach (var (category, _) in list)
            {
                categories.Add(category);
            }
        }

        var ordered = new List<string>();

        foreach (var category in PluralCategoryOrder)
        {
            if (categories.Contains(category))
            {
                ordered.Add(category);
            }
        }

        return ordered;
    }
}

/// <summary>An <c>Intl.PluralRules</c>: its type, its language, and the decimal format whose strings it reads.</summary>
internal sealed class JsPluralRulesObject : JsObject
{
    internal JsPluralRulesObject(JsObject prototype, JsNumberFormatter formatter, string type, string language) : base(prototype)
    {
        Formatter = formatter;
        Type = type;
        Language = language;
    }

    /// <summary>The decimal format holding the locale, the notation and the digit options.</summary>
    internal JsNumberFormatter Formatter { get; }

    /// <summary><c>cardinal</c> or <c>ordinal</c>.</summary>
    internal string Type { get; }

    /// <summary>The language whose rules select.</summary>
    internal string Language { get; }
}
