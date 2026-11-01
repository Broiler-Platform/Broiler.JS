// DIFFERENTIAL CHECK, BROILER.VM'S ARM: a test-only copy of Broiler.VM's code at its SJL-9 rule commit, the
// reference the differential check of Broiler.VM's JSD-0082, decision D22, compares the library with. It is compiled
// into this test project and nowhere else, never ships, and runs under the JIT only; see Differential/Vm/README.md.
//
// Copied from Broiler.VM at 4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f:
//   src/Broiler.VM.Profile.JavaScript/JsRealm.DisplayNames.cs
//     blob a49be69ade789b2aeea5b422bd9221d78828c174: JsRealm's Intl.DisplayNames part and JsDisplayNamesObject, of which the constructor's reads, CanonicalCodeForDisplayNames, DisplayName and LanguageName are kept
//
// Changes from the source, none of which changes a result:
//   - the namespace;
//   - the generated SPDX and assurance header and the per-member assurance annotations removed;
//   - removed: the members that build the prototype and its methods (`SetupDisplayNames`, `DisplayNamesOfThis`).

using System;
using System.Collections.Generic;

namespace Broiler.ECMAScript.Intl.Tests.Differential.Vm;

/// <summary>
/// <c>Intl.DisplayNames</c> (ECMA-402 s12; JSD-0052): the names CLDR gives languages, regions,
/// scripts, currencies, calendars and date-time fields in the format's language.
/// </summary>
/// <remarks>
/// <para>
/// <b>[[Fields]] holds a name for every code whose every part CLDR names.</b> A language's name is
/// composed as ICU composes it: a dialect name for the whole code where the data has one (<c>en-GB</c>
/// is "British English"), and otherwise the language's name with its script, region and variants in
/// CLDR's locale pattern. A code with a part CLDR does not name has no field, so
/// <c>Intl.DisplayNames.prototype.of</c> answers the code itself or <c>undefined</c>, as the draft
/// states.
/// </para>
/// </remarks>
internal sealed partial class JsRealm
{
    /// <summary><c>%Intl.DisplayNames.prototype%</c>, or nothing where the surface is not built.</summary>
    internal JsObject? DisplayNamesPrototype { get; private set; }

    /// <summary><c>%Intl.DisplayNames%</c>, or nothing where the surface is not built.</summary>
    internal JsNativeFunction? DisplayNamesConstructor { get; private set; }

    /// <summary>The date-time field codes of table 19.</summary>
    private static readonly string[] DateTimeFieldCodes = ["era", "year", "quarter", "month", "weekOfYear", "weekday", "day", "dayPeriod", "hour", "minute", "second", "timeZoneName"];

    /// <summary>ECMA-402's <c>Intl.DisplayNames</c> constructor (s12.1.1): the options required, and read in its order.</summary>
    internal static JsValue NewDisplayNames(JsEngine engine, JsValue newTarget, JsValue[] arguments)
    {
        var prototype = engine.PrototypeFromConstructor(newTarget, engine.Realm.DisplayNamesPrototype!);

        // RESOLVEOPTIONS WITH REQUIRE-OPTIONS: the locales, then options that must not be undefined,
        // through GetOptionsObject, the matcher, and no relevant keys.
        var requested = CanonicalizeLocaleList(engine, Argument(arguments, 0));
        var optionsArgument = Argument(arguments, 1);

        if (optionsArgument.Type == JsType.Undefined)
        {
            throw engine.Error("TypeError", "Intl.DisplayNames: the options are required");
        }

        var options = JsValue.Object(Base64Options(engine, optionsArgument));
        _ = GetStringOption(engine, options, "localeMatcher", ["lookup", "best fit"], "best fit");

        var resolved = ResolveLocale(engine, requested, new Dictionary<string, string?>(StringComparer.Ordinal), [], static (locale, key) => [null]);

        var style = GetStringOption(engine, options, "style", ["narrow", "short", "long"], "long")!;
        var type = GetStringOption(engine, options, "type", ["language", "region", "script", "currency", "calendar", "dateTimeField"], null) ??
            throw engine.Error("TypeError", "Intl.DisplayNames: the type option is required");
        var fallback = GetStringOption(engine, options, "fallback", ["code", "none"], "code")!;
        var languageDisplay = GetStringOption(engine, options, "languageDisplay", ["dialect", "standard"], "dialect")!;

        return JsValue.Object(new JsDisplayNamesObject(prototype)
        {
            Locale = resolved.Locale,
            Language = JsLocaleTag.Parse(resolved.DataLocale)?.Language ?? "en",
            Style = style,
            Type = type,
            Fallback = fallback,
            LanguageDisplay = type == "language" ? languageDisplay : null,
        });
    }

    /// <summary>ECMA-402's CanonicalCodeForDisplayNames (s12.5.1): a well-formed code of the type, case-regularized, or a <c>RangeError</c>.</summary>
    private static string CanonicalCodeForDisplayNames(JsEngine engine, string type, string code)
    {
        static bool Letters(string text) => text.Length > 0 && JsLocaleTag.IsAlpha(text);

        static bool Digits(string text)
        {
            foreach (var c in text)
            {
                if (!char.IsAsciiDigit(c))
                {
                    return false;
                }
            }

            return text.Length > 0;
        }

        switch (type)
        {
            case "language":
                {
                    // UNICODE_LANGUAGE_ID IN ITS BCP 47 FORM: no extension, no private use, no underscore.
                    if (code.Contains('_', System.StringComparison.Ordinal) ||
                        JsLocaleTag.Parse(code) is not { } tag ||
                        tag.LanguageId().Length != code.Length)
                    {
                        throw engine.Error("RangeError", "Intl.DisplayNames: invalid language code: " + code);
                    }

                    return tag.Canonicalize(engine.Intl!).ToString();
                }

            case "region":
                if (!(code.Length == 2 && Letters(code)) && !(code.Length == 3 && Digits(code)))
                {
                    throw engine.Error("RangeError", "Intl.DisplayNames: invalid region code: " + code);
                }

                return code.ToUpperInvariant();

            case "script":
                if (code.Length != 4 || !Letters(code))
                {
                    throw engine.Error("RangeError", "Intl.DisplayNames: invalid script code: " + code);
                }

                return char.ToUpperInvariant(code[0]) + code[1..].ToLowerInvariant();

            case "calendar":
                if (!IsTypeSequence(code) || code.Contains('_', System.StringComparison.Ordinal))
                {
                    throw engine.Error("RangeError", "Intl.DisplayNames: invalid calendar code: " + code);
                }

                return code.ToLowerInvariant();

            case "dateTimeField":
                if (System.Array.IndexOf(DateTimeFieldCodes, code) < 0)
                {
                    throw engine.Error("RangeError", "Intl.DisplayNames: invalid date-time field code: " + code);
                }

                return code;

            default:
                if (!IsWellFormedCurrencyCode(code))
                {
                    throw engine.Error("RangeError", "Intl.DisplayNames: invalid currency code: " + code);
                }

                return code.ToUpperInvariant();
        }
    }

    /// <summary>The name [[Fields]] holds for a canonical code, or nothing.</summary>
    private static string? DisplayName(JsEngine engine, JsDisplayNamesObject names, string code)
    {
        var data = engine.Intl!.LocaleInfo.DisplayNames;
        var prefix = names.Language + "|";
        var shortened = names.Style != "long";

        string? Named(string kind, string key) =>
            (shortened && data.TryGetValue(prefix + kind + "|" + key + "-alt-short", out var alternate)) ? alternate :
            data.TryGetValue(prefix + kind + "|" + key, out var name) ? name : null;

        switch (names.Type)
        {
            case "region":
                return Named("region", code);

            case "script":
                return data.TryGetValue(prefix + "script|" + code, out var script) ? script : null;

            case "calendar":
                return data.TryGetValue(prefix + "calendar|" + code, out var calendar) ? calendar : null;

            case "dateTimeField":
                return data.TryGetValue(prefix + "field|" + code + "-" + names.Style, out var field) ? field : null;

            case "currency":
                return engine.Intl!.Numbers.Currencies.TryGetValue(names.Language + "|" + code, out var currency) && currency.Name.Length != 0
                    ? currency.Name
                    : null;

            default:
                return LanguageName(engine, names, code, Named);
        }
    }

    /// <summary>
    /// A language code's name, composed as ICU's LocaleDisplayNames composes it: under the dialect
    /// display the whole code's own name where CLDR has one, then the language's name, and the script,
    /// region and variants it did not cover in CLDR's locale pattern. An unnamed part is its code under
    /// the code fallback, as ICU writes "xyz (Germany)", and makes the whole name nothing otherwise.
    /// </summary>
    private static string? LanguageName(JsEngine engine, JsDisplayNamesObject names, string code, Func<string, string, string?> named)
    {
        var tag = JsLocaleTag.Parse(code)!;
        var data = engine.Intl!.LocaleInfo.DisplayNames;
        var language = tag.Language;
        var script = tag.Script;
        var region = tag.Region;
        string? name = null;

        if (names.LanguageDisplay == "dialect")
        {
            // ONE DIALECT KEY, THE WHOLE CODE'S: a script and a region together are never split.
            if (script is not null && region is not null)
            {
                if (named("language", language + "-" + script + "-" + region) is { } full)
                {
                    (name, script, region) = (full, null, null);
                }
            }
            else if (script is not null)
            {
                if (named("language", language + "-" + script) is { } withScript)
                {
                    (name, script) = (withScript, null);
                }
            }
            else if (region is not null && named("language", language + "-" + region) is { } withRegion)
            {
                (name, region) = (withRegion, null);
            }
        }

        var substitute = names.Fallback == "code";
        name ??= named("language", language) ?? (substitute ? language : null);

        if (name is null)
        {
            return null;
        }

        var details = new System.Collections.Generic.List<string>();

        if (script is not null)
        {
            if (!data.TryGetValue(names.Language + "|script|" + script, out var scriptName))
            {
                if (!substitute)
                {
                    return null;
                }

                scriptName = script;
            }

            details.Add(scriptName);
        }

        if (region is not null)
        {
            if (named("region", region) is not { } regionName)
            {
                if (!substitute)
                {
                    return null;
                }

                regionName = region;
            }

            details.Add(regionName);
        }

        foreach (var variant in tag.Variants)
        {
            if (!data.TryGetValue(names.Language + "|variant|" + variant.ToUpperInvariant(), out var variantName))
            {
                if (!substitute)
                {
                    return null;
                }

                variantName = variant.ToUpperInvariant();
            }

            details.Add(variantName);
        }

        if (details.Count == 0)
        {
            return name;
        }

        var separator = data.TryGetValue(names.Language + "|pattern|localeSeparator", out var s) ? s : "{0}, {1}";
        var pattern = data.TryGetValue(names.Language + "|pattern|localePattern", out var p) ? p : "{0} ({1})";
        var joined = details[0];

        for (var i = 1; i < details.Count; i++)
        {
            joined = separator.Replace("{0}", joined, StringComparison.Ordinal).Replace("{1}", details[i], StringComparison.Ordinal);
        }

        return pattern.Replace("{0}", name, StringComparison.Ordinal).Replace("{1}", joined, StringComparison.Ordinal);
    }
}

/// <summary>An <c>Intl.DisplayNames</c>: its resolved options and the language whose names it reads.</summary>
internal sealed class JsDisplayNamesObject : JsObject
{
    internal JsDisplayNamesObject(JsObject prototype) : base(prototype) { }

    /// <summary>The resolved locale.</summary>
    internal string Locale { get; init; } = string.Empty;

    /// <summary>The language whose names are read.</summary>
    internal string Language { get; init; } = "en";

    /// <summary><c>narrow</c>, <c>short</c> or <c>long</c>.</summary>
    internal string Style { get; init; } = "long";

    /// <summary>The display name type.</summary>
    internal string Type { get; init; } = "language";

    /// <summary><c>code</c> or <c>none</c>.</summary>
    internal string Fallback { get; init; } = "code";

    /// <summary><c>dialect</c> or <c>standard</c> for the language type, and nothing otherwise.</summary>
    internal string? LanguageDisplay { get; init; }
}
