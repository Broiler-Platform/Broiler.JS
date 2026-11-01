// DIFFERENTIAL CHECK, BROILER.VM'S ARM: a test-only copy of Broiler.VM's code at its SJL-9 rule commit, the
// reference the differential check of Broiler.VM's JSD-0082, decision D22, compares the library with. It is compiled
// into this test project and nowhere else, never ships, and runs under the JIT only; see Differential/Vm/README.md.
//
// Copied from Broiler.VM at 4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f:
//   src/Broiler.VM.Profile.JavaScript/JsRealm.Intl.cs
//     blob b75e44c777594b41d36a06ddaa366708b83c4159: JsRealm's Intl namespace part: CanonicalizeLocaleList, the options readers, ResolveLocale, LookupMatchingLocale, FilterLocales, AvailableLocales and CollatorKeyData
//
// Changes from the source, none of which changes a result:
//   - the namespace;
//   - the generated SPDX and assurance header and the per-member assurance annotations removed;
//   - removed: the members that build the Intl namespace and Intl.Collator, which reach the collator milestone SJL-17 owns and the realm's other services (`CollatorPrototype`, `CollatorConstructor`, `SetupIntl`, `SetupCollator`, `CollatorOfThis`, `NewCollator`), and the type `JsCollatorObject`.

using System;
using System.Collections.Generic;

namespace Broiler.ECMAScript.Intl.Tests.Differential.Vm;

/// <summary>
/// The <c>Intl</c> namespace and its first constructor, <c>Intl.Collator</c>, over the tables the
/// composition handed over (ECMA-402; JSD-0043).
/// </summary>
/// <remarks>
/// <para>
/// <b>Built only where the surface is admitted and the data is present</b>, which a descriptor
/// guarantees together: a door handed no data does not admit the surface. A realm without it has no
/// <c>Intl</c> and its locale-sensitive methods keep the fixed answers JSD-0027 section 1 states.
/// </para>
/// <para>
/// <b>The locales are the data's</b>: <c>de</c>, <c>de-DE</c>, <c>en</c> and <c>en-US</c>, with
/// <c>en-US</c> the default, so the answer does not depend on the host's culture. Every locale
/// collates by the CLDR root, German's <c>phonebk</c> and <c>search</c> tailorings aside, and the root's
/// <c>search</c> tailoring serves every other search.
/// </para>
/// </remarks>
internal sealed partial class JsRealm
{
    /// <summary>The locale every service falls back to (JSD-0027 section 5 item 1).</summary>
    internal const string DefaultLocale = "en-US";

    // ---- Intl.Collator ---------------------------------------------------------------------------

    /// <summary>
    /// The values a collator supports for one relevant extension key in one locale, the default
    /// first: ECMA-402's <c>[[SortLocaleData]]</c> and <c>[[SearchLocaleData]]</c>.
    /// </summary>
    private static string?[] CollatorKeyData(string locale, string key, string usage) => key switch
    {
        // THE FIRST VALUE OF `co` IS NULL, and neither `standard` nor `search` is ever one (s10.2.3).
        "co" => usage == "sort" && JsLocaleTag.Parse(locale)?.Language == "de" ? [null, "phonebk"] : [null],
        "kn" => ["false", "true"],
        "kf" => ["false", "lower", "upper"],
        _ => [null],
    };

    /// <summary>The locales the data supports, as every service offers them: the data's list without <c>und</c>.</summary>
    private static List<string> AvailableLocales(JsEngine engine)
    {
        var available = new List<string>();

        foreach (var locale in engine.Intl!.Locales)
        {
            if (locale != "und")
            {
                available.Add(locale);
            }
        }

        return available;
    }

    // ---- the abstract operations ----------------------------------------------------------------

    /// <summary>ECMA-402's <c>CanonicalizeLocaleList</c>.</summary>
    internal static List<string> CanonicalizeLocaleList(JsEngine engine, JsValue locales)
    {
        var seen = new List<string>();

        if (locales.Type == JsType.Undefined)
        {
            return seen;
        }

        var list = locales.IsString || locales.AsObjectOrNull() is JsLocaleObject ? engine.Realm.NewArray([locales]) : engine.ToObject(locales);
        var length = ArrayLengthOf(engine, JsValue.Object(list));

        for (var index = 0.0; index < length; index++)
        {
            engine.Charge(1);
            var key = ArrayKeyOf(index);

            if (!JsEngine.HasProperty(list, key))
            {
                continue;
            }

            var value = engine.GetProperty(JsValue.Object(list), key);

            if (!value.IsString && !value.IsObject)
            {
                return ThrowList(engine, "TypeError", "Intl: a locale in the list is neither a String nor an Object");
            }

            var tag = value.AsObjectOrNull() is JsLocaleObject locale ? locale.Locale : engine.ToStringValue(value);

            if (JsLocaleTag.Parse(tag) is not { } parsed)
            {
                return ThrowList(engine, "RangeError", "Incorrect locale information provided: " + tag);
            }

            var canonical = parsed.Canonicalize(engine.Intl!).ToString();

            if (!seen.Contains(canonical))
            {
                seen.Add(canonical);
            }
        }

        return seen;
    }

    private static List<string> ThrowList(JsEngine engine, string kind, string message) =>
        throw engine.Error(kind, message);

    /// <summary>ECMA-402's <c>CoerceOptionsToObject</c>: <c>undefined</c> is an empty object with no prototype.</summary>
    internal static JsValue CoerceOptionsToObject(JsEngine engine, JsValue options) =>
        options.Type == JsType.Undefined ? JsValue.Object(new JsObject(null)) : JsValue.Object(engine.ToObject(options));

    /// <summary>ECMA-402's <c>GetOption</c> for a String: read, convert, and check against the values when there are any.</summary>
    internal static string? GetStringOption(JsEngine engine, JsValue options, string property, string[]? values, string? fallback)
    {
        var value = engine.GetProperty(options, property);

        if (value.Type == JsType.Undefined)
        {
            return fallback;
        }

        var text = engine.ToStringValue(value);

        if (values is not null && Array.IndexOf(values, text) < 0)
        {
            throw engine.Error("RangeError", "Value " + text + " out of range for option " + property);
        }

        return text;
    }

    /// <summary>ECMA-402's <c>GetOption</c> for a Boolean.</summary>
    internal static bool? GetBooleanOption(JsEngine engine, JsValue options, string property, bool? fallback)
    {
        var value = engine.GetProperty(options, property);
        return value.Type == JsType.Undefined ? fallback : value.ToBooleanValue();
    }

    /// <summary>Whether a String is a BCP 47 type: subtags of three to eight letters and digits.</summary>
    private static bool IsTypeSequence(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        foreach (var part in text.Split('-'))
        {
            if (part.Length is < 3 or > 8 || !JsLocaleTag.IsAlphanumeric(part))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>What <see cref="ResolveLocale"/> answers.</summary>
    internal sealed record JsResolvedLocale(string Locale, string DataLocale, Dictionary<string, string?> Keys);

    /// <summary>
    /// An option ResolveLocale reads as <c>null</c> rather than as absent: the value a keyword had is
    /// dropped and the key resolves to the data's <c>null</c>.
    /// </summary>
    internal const string NullOption = "\u0000null";

    /// <summary>
    /// ECMA-402's <c>ResolveLocale</c> with the lookup matcher, which also serves <c>best fit</c>: the
    /// first requested locale a prefix of which is available, its supported <c>-u-</c> keywords kept,
    /// and an option overriding a keyword it names.
    /// </summary>
    internal static JsResolvedLocale ResolveLocale(JsEngine engine, List<string> requested, Dictionary<string, string?> options, string[] relevantKeys, Func<string, string, string?[]> keyData)
    {
        var available = AvailableLocales(engine);
        var (found, extension) = LookupMatchingLocale(available, requested) ?? (DefaultLocale, null);

        var keywords = new List<(string Key, string Type)>();

        if (extension is not null)
        {
            keywords.AddRange(extension.Keywords);
        }

        var supported = new List<(string Key, string Type)>();
        var result = new Dictionary<string, string?>(StringComparer.Ordinal);

        foreach (var key in relevantKeys)
        {
            var values = keyData(found, key);
            var value = values[0];
            (string Key, string Type)? keyword = null;

            var entry = keywords.FindIndex(pair => pair.Key == key);

            if (entry >= 0)
            {
                var requestedValue = keywords[entry].Type;

                if (requestedValue.Length != 0)
                {
                    if (Array.IndexOf(values, requestedValue) >= 0)
                    {
                        value = requestedValue;
                        keyword = (key, value);
                    }
                }
                else if (Array.IndexOf(values, "true") >= 0)
                {
                    value = "true";
                    keyword = (key, string.Empty);
                }
            }

            if (options.TryGetValue(key, out var optionValue) && optionValue == NullOption)
            {
                if (value is not null && Array.IndexOf(values, null) >= 0)
                {
                    value = null;
                    keyword = null;
                }
            }
            else if (optionValue is not null)
            {
                var canonical = optionValue.ToLowerInvariant();

                if (engine.Intl!.TryType('u', key, canonical, out var preferred))
                {
                    canonical = preferred;
                }

                if (canonical.Length == 0)
                {
                    canonical = "true";
                }

                if (!string.Equals(canonical, value, StringComparison.Ordinal) && Array.IndexOf(values, canonical) >= 0)
                {
                    value = canonical;
                    keyword = null;
                }
            }

            if (keyword is { } kept)
            {
                supported.Add(kept);
            }

            result[key] = value;
        }

        var locale = found;

        if (supported.Count != 0)
        {
            var tag = JsLocaleTag.Parse(found)!;
            tag.HasUnicodeExtension = true;
            tag.Keywords.AddRange(supported);
            locale = tag.Canonicalize(engine.Intl!).ToString();
        }

        return new JsResolvedLocale(locale, found, result);
    }

    /// <summary>
    /// ECMA-402's <c>LookupMatchingLocaleByPrefix</c>: for each requested locale, its <c>-u-</c>
    /// extension set aside, the longest available prefix, truncated a subtag (and a singleton before
    /// it) at a time.
    /// </summary>
    internal static (string Locale, JsLocaleTag? Extension)? LookupMatchingLocale(List<string> available, List<string> requested)
    {
        foreach (var locale in requested)
        {
            var tag = JsLocaleTag.Parse(locale);

            if (tag is null)
            {
                continue;
            }

            var extension = tag.HasUnicodeExtension ? tag : null;
            var prefix = tag.WithoutUnicodeExtension();

            while (prefix.Length != 0)
            {
                if (available.Contains(prefix))
                {
                    return (prefix, extension);
                }

                var dash = prefix.LastIndexOf('-');

                if (dash < 0)
                {
                    break;
                }

                prefix = prefix[..dash];

                if (prefix.Length >= 2 && prefix[^2] == '-')
                {
                    prefix = prefix[..^2];
                }
            }
        }

        return null;
    }

    /// <summary>ECMA-402's <c>FilterLocales</c>: the requested locales a prefix of which is available, in the order requested.</summary>
    internal static List<string> FilterLocales(JsEngine engine, List<string> available, List<string> requested, JsValue options)
    {
        var coerced = CoerceOptionsToObject(engine, options);
        _ = GetStringOption(engine, coerced, "localeMatcher", ["lookup", "best fit"], "best fit");

        var subset = new List<string>();

        foreach (var locale in requested)
        {
            engine.Charge(1);

            if (LookupMatchingLocale(available, [locale]) is not null)
            {
                subset.Add(locale);
            }
        }

        return subset;
    }

    /// <summary>The argument at <paramref name="index"/>, or <c>undefined</c>.</summary>
    private static JsValue Argument(JsValue[] arguments, int index) =>
        index < arguments.Length ? arguments[index] : JsValue.Undefined;
}
