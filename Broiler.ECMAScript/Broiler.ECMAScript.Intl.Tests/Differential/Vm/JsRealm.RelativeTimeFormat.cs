// DIFFERENTIAL CHECK, BROILER.VM'S ARM: a test-only copy of Broiler.VM's code at its SJL-9 rule commit, the
// reference the differential check of Broiler.VM's JSD-0082, decision D22, compares the library with. It is compiled
// into this test project and nowhere else, never ships, and runs under the JIT only; see Differential/Vm/README.md.
//
// Copied from Broiler.VM at 4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f:
//   src/Broiler.VM.Profile.JavaScript/JsRealm.RelativeTimeFormat.cs
//     blob d59be7e2e974fa1083e4750db657bb723dc949b2: JsRealm's Intl.RelativeTimeFormat part and JsRelativeTimeFormatObject, of which the constructor's reads, SingularRelativeTimeUnit and PartitionRelativeTimePattern are kept
//
// Changes from the source, none of which changes a result:
//   - the namespace;
//   - the generated SPDX and assurance header and the per-member assurance annotations removed;
//   - removed: the members that build the prototype and its methods (`SetupRelativeTimeFormat`, `RelativeTimeFormatOfThis`).

namespace Broiler.ECMAScript.Intl.Tests.Differential.Vm;

/// <summary>
/// <c>Intl.RelativeTimeFormat</c> (ECMA-402 s18; JSD-0049): a value of a unit written as CLDR's
/// relative time pattern for its tense and plural category, or as CLDR's literal for it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The number is the format's own <c>Intl.NumberFormat</c> and the category its own
/// <c>Intl.PluralRules</c></b>, each made as the constructor makes them, so a relative time writes
/// its number as a decimal format of the locale and numbering system writes it.
/// </para>
/// <para>
/// <b>The number written is the value's magnitude.</b> The tense carries the sign: CLDR's past
/// patterns say "ago", and every engine and test262 write <c>-1</c> days as "1 day ago". The
/// draft's PartitionRelativeTimePattern passes the signed value to PartitionNumberPattern, which would
/// write the minus sign as well; JSD-0049 section 3 records the reading.
/// </para>
/// </remarks>
internal sealed partial class JsRealm
{
    /// <summary><c>%Intl.RelativeTimeFormat.prototype%</c>, or nothing where the surface is not built.</summary>
    internal JsObject? RelativeTimeFormatPrototype { get; private set; }

    /// <summary><c>%Intl.RelativeTimeFormat%</c>, or nothing where the surface is not built.</summary>
    internal JsNativeFunction? RelativeTimeFormatConstructor { get; private set; }

    /// <summary>ECMA-402's <c>Intl.RelativeTimeFormat</c> constructor (s18.1.1): the options read in its order.</summary>
    internal static JsValue NewRelativeTimeFormat(JsEngine engine, JsValue newTarget, JsValue[] arguments)
    {
        var realm = engine.Realm;
        var prototype = engine.PrototypeFromConstructor(newTarget, realm.RelativeTimeFormatPrototype!);

        // RESOLVEOPTIONS: the locales, the options coerced, the matcher, and the one relevant key,
        // nu, read from numberingSystem.
        var requested = CanonicalizeLocaleList(engine, Argument(arguments, 0));
        var options = CoerceOptionsToObject(engine, Argument(arguments, 1));
        _ = GetStringOption(engine, options, "localeMatcher", ["lookup", "best fit"], "best fit");
        var numberingSystem = GetStringOption(engine, options, "numberingSystem", null, null);

        if (numberingSystem is not null && !IsTypeSequence(numberingSystem))
        {
            return engine.ThrowRangeError("Intl.RelativeTimeFormat: the numberingSystem option is not a well-formed type: " + numberingSystem);
        }

        var systems = new System.Collections.Generic.List<string?> { "latn" };

        foreach (var name in engine.Intl!.Numbers.NumberingSystems.Keys)
        {
            if (name != "latn")
            {
                systems.Add(name);
            }
        }

        systems.Sort(1, systems.Count - 1, System.StringComparer.Ordinal);
        var nuValues = systems.ToArray();

        var resolved = ResolveLocale(
            engine,
            requested,
            new System.Collections.Generic.Dictionary<string, string?>(System.StringComparer.Ordinal) { ["nu"] = numberingSystem },
            ["nu"],
            (locale, key) => key == "nu" ? nuValues : [null]);

        var style = GetStringOption(engine, options, "style", ["long", "short", "narrow"], "long")!;
        var numeric = GetStringOption(engine, options, "numeric", ["always", "auto"], "always")!;
        var nu = resolved.Keys["nu"] ?? "latn";

        // [[NUMBERFORMAT]] AND [[PLURALRULES]], CONSTRUCTED AS THE DRAFT CONSTRUCTS THEM.
        var nfOptions = new JsObject(null);
        nfOptions.DefineOrdinary("numberingSystem", JsValue.String(nu));
        var numberFormat = (JsNumberFormatObject)NewNumberFormat(
            engine,
            JsValue.Object(realm.NumberFormatConstructor!),
            [JsValue.String(resolved.Locale), JsValue.Object(nfOptions)],
            JsValue.Undefined,
            called: false).AsObject();
        var pluralRules = (JsPluralRulesObject)NewPluralRules(
            engine, JsValue.Object(realm.PluralRulesConstructor!), [JsValue.String(resolved.Locale)]).AsObject();

        var language = JsLocaleTag.Parse(resolved.DataLocale)?.Language ?? "en";
        return JsValue.Object(new JsRelativeTimeFormatObject(
            prototype, resolved.Locale, language, style, numeric, nu, numberFormat.Formatter, pluralRules));
    }

    /// <summary>ECMA-402's SingularRelativeTimeUnit (s18.5.1): a unit, or its plural, or a <c>RangeError</c>.</summary>
    private static string SingularRelativeTimeUnit(JsEngine engine, string unit) => unit switch
    {
        "second" or "seconds" => "second",
        "minute" or "minutes" => "minute",
        "hour" or "hours" => "hour",
        "day" or "days" => "day",
        "week" or "weeks" => "week",
        "month" or "months" => "month",
        "quarter" or "quarters" => "quarter",
        "year" or "years" => "year",
        _ => throw engine.Error("RangeError", "Intl.RelativeTimeFormat: invalid unit: " + unit),
    };

    /// <summary>
    /// ECMA-402's PartitionRelativeTimePattern (s18.5.2): the literal for the value where the numeric
    /// option is <c>auto</c> and CLDR names one, and otherwise the tense's pattern for the plural
    /// category, its placeable the formatted number's parts, each with the unit.
    /// </summary>
    private static System.Collections.Generic.List<(string Type, string Value, string? Unit)> PartitionRelativeTimePattern(
        JsEngine engine, JsRelativeTimeFormatObject format, double value, string unit)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw engine.Error("RangeError", "Intl.RelativeTimeFormat: the value must be finite");
        }

        unit = SingularRelativeTimeUnit(engine, unit);
        var patterns = engine.Intl!.LocaleInfo.RelativeTimes;
        var field = unit;

        if (format.Style != "long" && patterns.ContainsKey(format.Language + "|" + unit + "-" + format.Style + "|future.other"))
        {
            field = unit + "-" + format.Style;
        }

        var prefix = format.Language + "|" + field + "|";
        var parts = new System.Collections.Generic.List<(string Type, string Value, string? Unit)>();

        if (format.Numeric == "auto" &&
            patterns.TryGetValue(prefix + engine.ToStringValue(JsValue.Number(value)), out var literal))
        {
            parts.Add(("literal", literal, null));
            return parts;
        }

        var tense = value < 0 || (value == 0 && double.IsNegative(value)) ? "past" : "future";
        var magnitude = JsDecimal.FromNumber(System.Math.Abs(value));
        var category = ResolvePlural(engine, format.PluralRules, magnitude).Category;

        if (!patterns.TryGetValue(prefix + tense + "." + category, out var pattern))
        {
            pattern = patterns[prefix + tense + ".other"];
        }

        // MAKEPARTSLIST (s18.5.3): the literals of the pattern, and the number's parts in its place.
        var at = 0;

        while (at < pattern.Length)
        {
            var open = pattern.IndexOf("{0}", at, System.StringComparison.Ordinal);

            if (open < 0)
            {
                parts.Add(("literal", pattern[at..], null));
                break;
            }

            if (open > at)
            {
                parts.Add(("literal", pattern[at..open], null));
            }

            foreach (var part in format.NumberFormatter.Parts(engine, magnitude))
            {
                parts.Add((part.Type, part.Value, unit));
            }

            at = open + 3;
        }

        return parts;
    }
}

/// <summary>An <c>Intl.RelativeTimeFormat</c>: its resolved options, and the number format and plural rules it writes with.</summary>
internal sealed class JsRelativeTimeFormatObject : JsObject
{
    internal JsRelativeTimeFormatObject(
        JsObject prototype,
        string locale,
        string language,
        string style,
        string numeric,
        string numberingSystem,
        JsNumberFormatter numberFormatter,
        JsPluralRulesObject pluralRules)
        : base(prototype)
    {
        Locale = locale;
        Language = language;
        Style = style;
        Numeric = numeric;
        NumberingSystem = numberingSystem;
        NumberFormatter = numberFormatter;
        PluralRules = pluralRules;
    }

    /// <summary>The resolved locale.</summary>
    internal string Locale { get; }

    /// <summary>The language whose patterns are read.</summary>
    internal string Language { get; }

    /// <summary><c>long</c>, <c>short</c> or <c>narrow</c>.</summary>
    internal string Style { get; }

    /// <summary><c>always</c> or <c>auto</c>.</summary>
    internal string Numeric { get; }

    /// <summary>The resolved numbering system.</summary>
    internal string NumberingSystem { get; }

    /// <summary>[[NumberFormat]]'s formatter.</summary>
    internal JsNumberFormatter NumberFormatter { get; }

    /// <summary>[[PluralRules]].</summary>
    internal JsPluralRulesObject PluralRules { get; }
}
