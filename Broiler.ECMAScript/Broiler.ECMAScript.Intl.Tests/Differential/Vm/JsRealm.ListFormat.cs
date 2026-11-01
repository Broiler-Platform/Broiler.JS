// DIFFERENTIAL CHECK, BROILER.VM'S ARM: a test-only copy of Broiler.VM's code at its SJL-9 rule commit, the
// reference the differential check of Broiler.VM's JSD-0082, decision D22, compares the library with. It is compiled
// into this test project and nowhere else, never ships, and runs under the JIT only; see Differential/Vm/README.md.
//
// Copied from Broiler.VM at 4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f:
//   src/Broiler.VM.Profile.JavaScript/JsRealm.ListFormat.cs
//     blob df3aac41e14ff7637c3670fb863b07b151e05664: JsRealm's Intl.ListFormat part and JsListFormatObject, of which the constructor's reads, the templates, CreatePartsFromList and DeconstructPattern are kept
//
// Changes from the source, none of which changes a result:
//   - the namespace;
//   - the generated SPDX and assurance header and the per-member assurance annotations removed;
//   - removed: the members that build the prototype and its methods (`SetupListFormat`, `ListFormatOfThis`) and the engine's iteration of a script value (`StringListFromIterable`).

using System;
using System.Collections.Generic;

namespace Broiler.ECMAScript.Intl.Tests.Differential.Vm;

/// <summary>
/// <c>Intl.ListFormat</c> (ECMA-402 s14; JSD-0048): a list of strings joined by CLDR's list patterns
/// for the format's language, type and style.
/// </summary>
/// <remarks>
/// <b>One template per type and style.</b> ECMA-402 lets an implementation choose among several
/// templates by the elements, as Spanish chooses <c>y</c> or <c>e</c> by the next word. German and
/// English have no such choice, so the index into [[Templates]] is always the one template CLDR
/// gives.
/// </remarks>
internal sealed partial class JsRealm
{
    /// <summary><c>%Intl.ListFormat.prototype%</c>, or nothing where the surface is not built.</summary>
    internal JsObject? ListFormatPrototype { get; private set; }

    /// <summary><c>%Intl.ListFormat%</c>, or nothing where the surface is not built.</summary>
    internal JsNativeFunction? ListFormatConstructor { get; private set; }

    /// <summary>ECMA-402's <c>Intl.ListFormat</c> constructor (s14.1.1): the options read in its order.</summary>
    internal static JsValue NewListFormat(JsEngine engine, JsValue newTarget, JsValue[] arguments)
    {
        var prototype = engine.PrototypeFromConstructor(newTarget, engine.Realm.ListFormatPrototype!);

        // RESOLVEOPTIONS: the locales, the options as an object (undefined allowed, no coercion of
        // primitives, GetOptionsObject), the matcher, and no relevant keys.
        var requested = CanonicalizeLocaleList(engine, Argument(arguments, 0));
        var options = JsValue.Object(Base64Options(engine, Argument(arguments, 1)));
        _ = GetStringOption(engine, options, "localeMatcher", ["lookup", "best fit"], "best fit");

        var resolved = ResolveLocale(engine, requested, new Dictionary<string, string?>(StringComparer.Ordinal), [], static (locale, key) => [null]);

        var type = GetStringOption(engine, options, "type", ["conjunction", "disjunction", "unit"], "conjunction")!;
        var style = GetStringOption(engine, options, "style", ["long", "short", "narrow"], "long")!;
        var language = JsLocaleTag.Parse(resolved.DataLocale)?.Language ?? "en";

        // [[TEMPLATES]]: CLDR's type for the type and style.
        var key = language + "|" + type switch { "disjunction" => "or", "unit" => "unit", _ => "standard" } +
            style switch { "short" => "-short", "narrow" => "-narrow", _ => string.Empty };

        var templates = engine.Intl!.LocaleInfo.ListPatterns.TryGetValue(key, out var found)
            ? found
            : ("{0}, {1}", "{0}, {1}", "{0}, {1}", "{0}, {1}");

        return JsValue.Object(new JsListFormatObject(prototype, resolved.Locale, type, style, templates));
    }

    /// <summary>
    /// ECMA-402's CreatePartsFromList (s14.5.2): nothing, the element, the pair pattern, or the end
    /// pattern folded leftward through the middle patterns to the start pattern.
    /// </summary>
    private static List<(string Type, string Value)> CreatePartsFromList(JsEngine engine, JsListFormatObject format, List<string> list)
    {
        var parts = new List<(string Type, string Value)>();
        var size = list.Count;

        if (size == 0)
        {
            return parts;
        }

        if (size == 1)
        {
            parts.Add(("element", list[0]));
            return parts;
        }

        if (size == 2)
        {
            return DeconstructPattern(format.Templates.Pair, [("element", list[0])], [("element", list[1])]);
        }

        parts.Add(("element", list[size - 1]));

        for (var i = size - 2; i >= 0; i--)
        {
            engine.Charge(1);
            var pattern = i == 0 ? format.Templates.Start : i < size - 2 ? format.Templates.Middle : format.Templates.End;
            parts = DeconstructPattern(pattern, [("element", list[i])], parts);
        }

        return parts;
    }

    /// <summary>
    /// ECMA-402's DeconstructPattern (s14.5.1) over a list pattern: each non-empty literal between the
    /// placeables, and the parts of <c>{0}</c> and <c>{1}</c> in their places.
    /// </summary>
    private static List<(string Type, string Value)> DeconstructPattern(string pattern, List<(string Type, string Value)> zero, List<(string Type, string Value)> one)
    {
        var result = new List<(string Type, string Value)>();
        var at = 0;

        while (at < pattern.Length)
        {
            var open = pattern.IndexOf('{', at);

            if (open < 0)
            {
                result.Add(("literal", pattern[at..]));
                break;
            }

            var close = pattern.IndexOf('}', open);

            if (open > at)
            {
                result.Add(("literal", pattern[at..open]));
            }

            result.AddRange(pattern[(open + 1)..close] == "0" ? zero : one);
            at = close + 1;
        }

        return result;
    }
}

/// <summary>An <c>Intl.ListFormat</c>: its locale, type, style, and the patterns they select.</summary>
internal sealed class JsListFormatObject : JsObject
{
    internal JsListFormatObject(JsObject prototype, string locale, string type, string style, (string Start, string Middle, string End, string Pair) templates) : base(prototype)
    {
        Locale = locale;
        Type = type;
        Style = style;
        Templates = templates;
    }

    /// <summary>The resolved locale.</summary>
    internal string Locale { get; }

    /// <summary><c>conjunction</c>, <c>disjunction</c> or <c>unit</c>.</summary>
    internal string Type { get; }

    /// <summary><c>long</c>, <c>short</c> or <c>narrow</c>.</summary>
    internal string Style { get; }

    /// <summary>CLDR's start, middle, end and two-element patterns.</summary>
    internal (string Start, string Middle, string End, string Pair) Templates { get; }
}
