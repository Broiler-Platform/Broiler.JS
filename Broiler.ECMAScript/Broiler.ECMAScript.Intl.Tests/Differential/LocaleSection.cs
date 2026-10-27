using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Broiler.ECMAScript.Intl.Tests.Differential.Vm;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Intl.Tests.Differential;

/// <summary>
/// Sub-area (a)'s sections (the plan's task 5.a, stage SJL-9.3a): Broiler.VM's <c>JsLocaleTag</c> and its realm's Intl.Locale,
/// locale-list and <c>Intl.supportedValuesOf</c> code, run as its engine runs them over the stand-ins of
/// <c>VmEngine.cs</c>, against <see cref="LocaleId"/>, <see cref="LocaleNegotiation"/>, <see cref="LocaleRecord"/>,
/// <see cref="LocaleInfo"/> and <see cref="SupportedValues"/>, called as an engine's adapter calls them.
/// </summary>
/// <remarks>
/// <para>
/// <b>Both arms take the same engine parameters</b>, Broiler.VM's (decision D7): the default locale <c>en-US</c>; the available
/// locales of the embedding's data (Broiler.VM's <c>AvailableLocales</c>, the data's list without <c>und</c>), which on the
/// <c>broiler-js</c> embedding is Broiler.JS's list; its <c>Intl.Collator</c>'s data (<c>CollatorKeyData</c>, SJL-17's), which
/// the library arm reads through <see cref="ICollatorLocaleData"/>; each service's relevant keys and values; and the shared
/// time zone database of milestone SJL-8. Each arm reads the tables through its own decoder.
/// </para>
/// <para>
/// <b>An answer is rendered as the script would see it</b>: a String as it is, a list as <c>[a,b]</c>, <c>undefined</c> as
/// such, and a refusal as <c>error: RangeError</c> (Broiler.VM's thrown error, the library's status other than
/// <see cref="IntlStatus.Completed"/>). An exception that is no script error is rendered by its type, so that it differs.
/// </para>
/// </remarks>
internal static partial class LocaleSection
{
    /// <summary>Broiler.VM's DefaultLocale (decision D7), both arms'.</summary>
    private const string DefaultLocale = "en-US";

    private const string Refused = "error: RangeError";

    internal static IEnumerable<Section> Run(Embedding embedding, SeededRandom random, int scale, IReadOnlyCollection<string>? only)
    {
        var arms = new Arms(embedding);
        var population = LocalePopulation.Build(embedding, random, scale);
        var suffix = " (" + embedding.Name + ")";

        if (only is null || only.Contains("L1"))
            yield return Identifiers(arms, population, "L1 identifiers: parse, canonicalise, aliases, likely subtags" + suffix);
        if (only is null || only.Contains("L2"))
            yield return Negotiation(arms, population, "L2 negotiation: locale lists, lookup, filter, ResolveLocale" + suffix);
        if (only is null || only.Contains("L3"))
            yield return Locales(arms, population, "L3 Intl.Locale: constructor, parts, likely subtags, Locale info" + suffix);
        if (only is null || only.Contains("L4"))
            yield return Values(arms, population, "L4 Intl.supportedValuesOf" + suffix);
    }

    // ---- L1 --------------------------------------------------------------------------------------------------------------

    private static Section Identifiers(Arms arms, LocalePopulation population, string name)
    {
        var section = new Section(name);
        var tables = arms.Tables;
        var data = arms.Data;

        foreach (var tag in population.Tags)
        {
            var label = Quote(tag);
            section.Compare("canonical " + label,
                VmCall(() => JsLocaleTag.Parse(tag) is { } t ? t.Canonicalize(tables).ToString() : Refused),
                LibraryCall(() => LocaleId.TryCanonicalize(tag, data, out var c) == IntlStatus.Completed ? c : Refused));

            section.Compare("raw maximize " + label,
                VmCall(() => JsLocaleTag.Parse(tag) is { } t ? Found(t.Maximize(tables)) + " " + t : Refused),
                LibraryCall(() => LocaleId.TryParse(tag, out var id) == IntlStatus.Completed ? Likely(id!.Maximize(data, out var found), found) : Refused));

            if (JsLocaleTag.Parse(tag) is not { } parsed)
                continue;

            var vmCanonical = parsed.Canonicalize(tables).ToString();
            var libraryCanonical = LocaleId.TryCanonicalize(tag, data, out var c) == IntlStatus.Completed ? c : null;
            if (libraryCanonical is null)
                continue;

            section.Compare("canonical twice " + label,
                VmCall(() => JsLocaleTag.Parse(vmCanonical) is { } t ? t.Canonicalize(tables).ToString() : Refused),
                LibraryCall(() => LocaleId.TryCanonicalize(libraryCanonical, data, out var again) == IntlStatus.Completed ? again : Refused));

            var vm = JsLocaleTag.Parse(vmCanonical);
            LocaleId.TryParse(libraryCanonical, out var id);

            section.Compare("parts " + label,
                VmCall(() => vm is null ? Refused : Parts(vm.LanguageId(), vm.Language, vm.Script, vm.Region, vm.Variants, vm.Keywords.Select(static k => k.Key + "=" + k.Type))),
                LibraryCall(() => id is null ? Refused : Parts(id.BaseName, id.Language, id.Script, id.Region, id.Variants, id.Keywords.Select(static k => k.Key + "=" + k.Value))));
            section.Compare("maximize " + label,
                VmCall(() => JsLocaleTag.Parse(vmCanonical) is { } t ? Found(t.Maximize(tables)) + " " + t : Refused),
                LibraryCall(() => id is null ? Refused : Likely(id.Maximize(data, out var found), found)));
            section.Compare("minimize " + label,
                VmCall(() => JsLocaleTag.Parse(vmCanonical) is { } t ? Found(t.Minimize(tables)) + " " + t : Refused),
                LibraryCall(() => id is null ? Refused : Likely(id.Minimize(data, out var found), found)));
            section.Compare("without -u- " + label,
                VmCall(() => vm is null ? Refused : vm.WithoutUnicodeExtension()),
                LibraryCall(() => id is null ? Refused : id.WithoutUnicodeExtension().ToString()));
        }

        return section;
    }

    private static string Found(bool found) => found ? "found" : "not-found";

    private static string Likely(LocaleId changed, bool found) => Found(found) + " " + changed;

    private static string Parts(string baseName, string language, string? script, string? region, IEnumerable<string> variants, IEnumerable<string> keywords) =>
        baseName + " | " + language + " | " + (script ?? "undefined") + " | " + (region ?? "undefined") + " | " + string.Join("-", variants) + " | " + string.Join(";", keywords);

    // ---- L2 --------------------------------------------------------------------------------------------------------------

    private static Section Negotiation(Arms arms, LocalePopulation population, string name)
    {
        var section = new Section(name);

        foreach (var list in population.Lists)
        {
            var label = List(list);
            var vm = VmList(arms, list);
            var library = LibraryList(arms, list);
            section.Compare("canonicalize-locale-list " + label, vm.Rendered, library.Rendered);

            if (vm.Locales is null || library.Locales is null)
                continue;

            section.Compare("lookup " + label,
                VmCall(() => JsRealm.LookupMatchingLocale(arms.VmAvailable, vm.Locales) is { } match ? match.Locale + " " + Keywords(match.Extension?.Keywords) : "<none>"),
                LibraryCall(() => LocaleNegotiation.LookupMatchingLocaleByPrefix(arms.Available, library.Locales) is { } match ? match.Locale + " " + string.Join(";", match.ExtensionKeywords.Select(static k => k.Key + "=" + k.Value)) : "<none>"));

            long vmCharged = 0;
            ulong libraryCharged = 0;
            section.Compare("filter " + label,
                VmCall(() =>
                {
                    var before = arms.Engine.Charged;
                    var subset = JsRealm.FilterLocales(arms.Engine, arms.VmAvailable, vm.Locales, JsValue.Undefined);
                    vmCharged = arms.Engine.Charged - before;
                    return "[" + string.Join(",", subset) + "]";
                }),
                LibraryCall(() => "[" + string.Join(",", LocaleNegotiation.FilterLocales(arms.Available, library.Locales, units => libraryCharged += units)) + "]"));

            // D18: FilterLocales charges one unit per requested locale in both, the one place both meter alike (row L43).
            section.Compare("filter charge " + label, vmCharged.ToString(CultureInfo.InvariantCulture), libraryCharged.ToString(CultureInfo.InvariantCulture));
        }

        foreach (var resolution in population.Resolutions)
        {
            var label = resolution.Service + " " + List(resolution.Requested) + " " + string.Join(";", resolution.Options.Select(static o => o.Key + "=" + (o.Value == JsRealm.NullOption ? "<null>" : o.Value)));
            var vm = VmList(arms, resolution.Requested);
            var library = LibraryList(arms, resolution.Requested);

            if (vm.Locales is null || library.Locales is null)
            {
                section.Compare("resolve " + label, vm.Rendered, library.Rendered);
                continue;
            }

            section.Compare("resolve " + label, VmCall(() => VmResolve(arms, resolution, vm.Locales)), LibraryCall(() => LibraryResolve(arms, resolution, library.Locales)));
        }

        return section;
    }

    private static (string Rendered, List<string>? Locales) VmList(Arms arms, IReadOnlyList<string> tags)
    {
        try
        {
            var argument = tags.Count == 0 ? JsValue.Undefined : JsValue.Object(arms.Engine.Realm.NewArray(tags.Select(JsValue.String).ToList()));
            var locales = JsRealm.VmCanonicalizeLocaleList(arms.Engine, argument);
            return ("[" + string.Join(",", locales) + "]", locales);
        }
        catch (VmError error)
        {
            return ("error: " + error.Kind, null);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return ("exception: " + exception.GetType().Name, null);
        }
    }

    private static (string Rendered, IReadOnlyList<string>? Locales) LibraryList(Arms arms, IReadOnlyList<string> tags)
    {
        try
        {
            return LocaleNegotiation.CanonicalizeLocaleList(tags, arms.Data, out var locales, out _) == IntlStatus.Completed
                ? ("[" + string.Join(",", locales) + "]", locales)
                : (Refused, null);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return ("exception: " + exception.GetType().Name, null);
        }
    }

    /// <summary>Broiler.VM's ResolveLocale as each service's constructor calls it.</summary>
    private static string VmResolve(Arms arms, Resolution resolution, List<string> requested)
    {
        var options = new Dictionary<string, string?>(resolution.Options, StringComparer.Ordinal);
        var keys = Keys(resolution.Service);
        var nu = arms.VmNumberingSystems;
        var resolved = JsRealm.ResolveLocale(arms.Engine, requested, options, keys, (locale, key) => resolution.Service switch
        {
            "Collator" => JsRealm.VmCollatorKeyData(locale, key),
            "NumberFormat" => key == "nu" ? nu : [null],
            "DateTimeFormat" => key switch
            {
                "ca" => [.. JsRealm.VmCalendarKeyValues],
                "hc" => [null, "h11", "h12", "h23", "h24"],
                "nu" => nu,
                _ => [null],
            },
            _ => [null],
        });

        return resolved.Locale + " " + resolved.DataLocale + " " + string.Join(";", keys.Select(k => k + "=" + (resolved.Keys[k] ?? "<null>")));
    }

    /// <summary>The library's MatchLocale and ResolveLocale, with the same engine's key data and options.</summary>
    private static string LibraryResolve(Arms arms, Resolution resolution, IReadOnlyList<string> requested)
    {
        var match = LocaleNegotiation.MatchLocale(arms.Available, requested, DefaultLocale);
        var keys = Keys(resolution.Service);
        var relevant = new List<RelevantKey>();

        foreach (var key in keys)
        {
            IReadOnlyList<string?> values = resolution.Service switch
            {
                "Collator" => arms.Collator.KeyData(match.Locale, key),
                "NumberFormat" => arms.LibraryNumberingSystems,
                "DateTimeFormat" => key switch
                {
                    "ca" => JsRealm.VmCalendarKeyValues,
                    "hc" => [null, "h11", "h12", "h23", "h24"],
                    _ => arms.LibraryNumberingSystems,
                },
                _ => [null],
            };

            resolution.Options.TryGetValue(key, out var option);
            relevant.Add(new RelevantKey(key, values) { Option = option == JsRealm.NullOption ? null : option, OptionIsNull = option == JsRealm.NullOption });
        }

        var resolved = LocaleNegotiation.ResolveLocale(match, relevant, arms.Data);
        return resolved.Locale + " " + resolved.DataLocale + " " + string.Join(";", keys.Select(k => k + "=" + (resolved.KeyValues[k] ?? "<null>")));
    }

    private static string[] Keys(string service) => service switch
    {
        "Collator" => ["co", "kf", "kn"],
        "NumberFormat" => ["nu"],
        "DateTimeFormat" => ["ca", "hc", "nu"],
        _ => [],
    };

    private static string Keywords(List<(string Key, string Type)>? keywords) =>
        keywords is null ? string.Empty : string.Join(";", keywords.Select(static k => k.Key + "=" + k.Type));

    // ---- L3 --------------------------------------------------------------------------------------------------------------

    private static readonly string[] Getters = ["baseName", "language", "script", "region", "variants", "calendar", "caseFirst", "collation", "firstDayOfWeek", "hourCycle", "numberingSystem", "numeric"];

    private static readonly string[] Methods = ["toString", "maximize", "minimize", "getCalendars", "getCollations", "getHourCycles", "getNumberingSystems", "getTimeZones", "getTextInfo", "getWeekInfo"];

    private static Section Locales(Arms arms, LocalePopulation population, string name)
    {
        var section = new Section(name);

        foreach (var (tag, options) in population.Locales)
        {
            var label = Quote(tag) + (options is null ? string.Empty : " " + options);

            JsValue vmLocale = default;
            var vmCreated = VmCall(() =>
            {
                vmLocale = JsRealm.NewLocale(arms.Engine, JsValue.Object(arms.Engine.Realm.LocaleConstructor!), [JsValue.String(tag), VmOptions(arms, options)]);
                return arms.Engine.ToStringValue(vmLocale);
            });

            LocaleRecord? record = null;
            var libraryCreated = LibraryCall(() => LibraryLocale(arms, tag, options, out record));
            section.Compare("new Intl.Locale " + label, vmCreated, libraryCreated);

            if (vmLocale.Type != JsType.Object || record is null)
                continue;

            LocaleId.TryParse(record.Locale, out var id);

            foreach (var getter in Getters)
                section.Compare(getter + " " + label, VmCall(() => VmRender(arms.Engine, arms.Engine.GetProperty(vmLocale, getter))), LibraryCall(() => LibraryGetter(record, id!, getter)));

            foreach (var method in Methods)
            {
                section.Compare(method + " " + record.Locale + " of " + label,
                    VmCall(() =>
                    {
                        var function = (JsNativeFunction)arms.Engine.GetProperty(vmLocale, method).AsObjectOrNull()!;
                        return VmRender(arms.Engine, function.Call(arms.Engine, vmLocale, []));
                    }),
                    LibraryCall(() => LibraryMethod(arms, record, id!, method)));
            }
        }

        return section;
    }

    private static JsValue VmOptions(Arms arms, LocaleOptionsInput? options)
    {
        if (options is null)
            return JsValue.Undefined;

        var bag = new JsObject(arms.Engine.Realm.ObjectPrototype);
        foreach (var (property, value) in options.Present())
            bag.DefineOrdinary(property, value is bool b ? JsValue.Boolean(b) : JsValue.String((string)value));
        return JsValue.Object(bag);
    }

    /// <summary>The constructor as an engine's adapter calls the library: GetOption's value lists for <c>hourCycle</c> and <c>caseFirst</c>, then <see cref="LocaleRecord.TryCreate"/>.</summary>
    private static string LibraryLocale(Arms arms, string tag, LocaleOptionsInput? options, out LocaleRecord? record)
    {
        record = null;
        options ??= new LocaleOptionsInput();

        if (options.HourCycle is { } hc && hc is not ("h11" or "h12" or "h23" or "h24"))
            return Refused;
        if (options.CaseFirst is { } kf && kf is not ("upper" or "lower" or "false"))
            return Refused;

        var status = LocaleRecord.TryCreate(tag, new LocaleOptions
        {
            Language = options.Language,
            Script = options.Script,
            Region = options.Region,
            Variants = options.Variants,
            Calendar = options.Calendar,
            Collation = options.Collation,
            FirstDayOfWeek = options.FirstDayOfWeek,
            HourCycle = options.HourCycle,
            CaseFirst = options.CaseFirst,
            Numeric = options.Numeric,
            NumberingSystem = options.NumberingSystem,
        }, arms.Data, out record);

        return status == IntlStatus.Completed ? record!.Locale : Refused;
    }

    private static string LibraryGetter(LocaleRecord record, LocaleId id, string getter) => getter switch
    {
        "baseName" => id.BaseName,
        "language" => id.Language,
        "script" => id.Script ?? "undefined",
        "region" => id.Region ?? "undefined",
        "variants" => id.Variants.Count > 0 ? string.Join("-", id.Variants) : "undefined",
        "calendar" => record.Calendar ?? "undefined",
        "caseFirst" => record.CaseFirst ?? "undefined",
        "collation" => record.Collation ?? "undefined",
        "firstDayOfWeek" => record.FirstDayOfWeek ?? "undefined",
        "hourCycle" => record.HourCycle ?? "undefined",
        "numberingSystem" => record.NumberingSystem ?? "undefined",
        "numeric" => record.Numeric ? "true" : "false",
        _ => throw new ArgumentOutOfRangeException(nameof(getter)),
    };

    private static string LibraryMethod(Arms arms, LocaleRecord record, LocaleId id, string method)
    {
        switch (method)
        {
            case "toString":
                return record.Locale;
            case "maximize":
            case "minimize":
                var changed = method == "maximize" ? id.Maximize(arms.Data, out _) : id.Minimize(arms.Data, out _);
                return LocaleRecord.TryCreate(changed.ToString(), LocaleOptions.None, arms.Data, out var created) == IntlStatus.Completed ? created!.Locale : Refused;
            case "getCalendars":
                return Array(LocaleInfo.Calendars(record));
            case "getCollations":
                return Array(LocaleInfo.Collations(record, arms.Collator, DefaultLocale));
            case "getHourCycles":
                return Array(LocaleInfo.HourCycles(record, arms.Data));
            case "getNumberingSystems":
                return Array(LocaleInfo.NumberingSystems(record, arms.Available, arms.Data));
            case "getTimeZones":
                return LocaleInfo.TimeZones(record, arms.TimeZones) is { } zones ? Array(zones) : "undefined";
            case "getTextInfo":
                return "{direction:" + (LocaleInfo.TextDirection(record, arms.Data) ?? "undefined") + "}";
            case "getWeekInfo":
                var week = LocaleInfo.WeekInfo(record, arms.Data);
                return "{firstDay:" + week.FirstDay.ToString(CultureInfo.InvariantCulture) + ",weekend:[" + string.Join(",", week.Weekend.Select(static d => d.ToString(CultureInfo.InvariantCulture))) + "]}";
            default:
                throw new ArgumentOutOfRangeException(nameof(method));
        }
    }

    /// <summary>A value the copied realm code answered, as the script would see it.</summary>
    private static string VmRender(JsEngine engine, JsValue value)
    {
        switch (value.Type)
        {
            case JsType.Undefined: return "undefined";
            case JsType.Null: return "null";
            case JsType.String: return value.AsString();
            case JsType.Boolean: return value.AsBoolean() ? "true" : "false";
            case JsType.Number: return value.AsNumber().ToString("R", CultureInfo.InvariantCulture);
        }

        var o = value.AsObjectOrNull()!;
        if (o is JsLocaleObject locale)
            return locale.Locale;

        if (o.Properties.ContainsKey("length"))
        {
            var length = (int)engine.GetProperty(value, "length").AsNumber();
            var items = new List<string>();
            for (var i = 0; i < length; i++)
                items.Add(VmRender(engine, engine.GetProperty(value, i.ToString(CultureInfo.InvariantCulture))));
            return "[" + string.Join(",", items) + "]";
        }

        return "{" + string.Join(",", o.Properties.Select(p => p.Key + ":" + VmRender(engine, p.Value.Value))) + "}";
    }

    private static string Array(IReadOnlyList<string> values) => "[" + string.Join(",", values) + "]";

    // ---- L4 --------------------------------------------------------------------------------------------------------------

    private static Section Values(Arms arms, LocalePopulation population, string name)
    {
        var section = new Section(name);

        foreach (var key in population.SupportedKeys)
        {
            section.Compare("supportedValuesOf " + Quote(key),
                VmCall(() => VmRender(arms.Engine, JsRealm.VmSupportedValuesOf(arms.Engine, JsValue.String(key)))),
                LibraryCall(() => SupportedValues.Of(key, arms.Data, arms.TimeZones, arms.Collator, out var values) == IntlStatus.Completed ? Array(values) : Refused));
        }

        return section;
    }

    // ---- both arms ---------------------------------------------------------------------------------------------------------

    private static string VmCall(Func<string> call)
    {
        try
        {
            return call();
        }
        catch (VmError error)
        {
            return "error: " + error.Kind;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return "exception: " + exception.GetType().Name;
        }
    }

    private static string LibraryCall(Func<string> call)
    {
        try
        {
            return call();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return "exception: " + exception.GetType().Name;
        }
    }

    private static string List(IReadOnlyList<string> tags) => "[" + string.Join(",", tags.Select(Quote)) + "]";

    /// <summary>A tag as a difference's report names it: quoted, with what is not printable ASCII escaped.</summary>
    internal static string Quote(string text)
    {
        var quoted = new StringBuilder("\"");
        foreach (var c in text)
        {
            if (c is >= ' ' and <= '~' and not '"' and not '\\')
                quoted.Append(c);
            else
                quoted.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
        }

        return quoted.Append('"').ToString();
    }

    /// <summary>One embedding's two arms and the engine parameters they share.</summary>
    private sealed class Arms
    {
        internal Arms(Embedding embedding)
        {
            Tables = new JsIntlTables(embedding.Vm);
            TimeZones = HomeTimeZones.Current;
            Engine = new JsEngine(new VmIntl(Tables, TimeZones));
            Data = new TableLocaleData(embedding.Library);
            VmAvailable = JsRealm.VmAvailableLocales(Engine);
            Available = Data.Locales.Where(static l => l != "und").ToList().AsReadOnly();
            Collator = new EngineCollator(Available);
            VmNumberingSystems = NumberingSystemValues(Tables.Numbers.NumberingSystems.Keys);
            LibraryNumberingSystems = NumberingSystemValues(Data.NumberingSystems);
        }

        internal JsIntlTables Tables { get; }

        internal ITimeZoneDatabase TimeZones { get; }

        internal JsEngine Engine { get; }

        internal TableLocaleData Data { get; }

        /// <summary>Broiler.VM's available locales, as its <c>AvailableLocales</c> reads them from the embedding's data.</summary>
        internal List<string> VmAvailable { get; }

        /// <summary>The same provider for the library arm, from the library's decoder.</summary>
        internal IReadOnlyList<string> Available { get; }

        internal EngineCollator Collator { get; }

        /// <summary>Broiler.VM's NumberFormat <c>nu</c> values over its decoder: <c>latn</c> first, then the rest by code unit.</summary>
        internal string?[] VmNumberingSystems { get; }

        /// <summary>The same values over the library's decoder.</summary>
        internal string?[] LibraryNumberingSystems { get; }

        private static string?[] NumberingSystemValues(IEnumerable<string> systems)
        {
            var others = systems.Where(static s => s != "latn").Order(StringComparer.Ordinal);
            return ["latn", .. others];
        }
    }

    /// <summary>
    /// Broiler.VM's <c>Intl.Collator</c> data (JSD-0043; SJL-17's), the engine parameter both arms share: its available locales,
    /// the embedding's; its sort data, Broiler.VM's own <c>CollatorKeyData</c>; and its collations, <c>phonebk</c> alone.
    /// </summary>
    private sealed class EngineCollator(IReadOnlyList<string> available) : ICollatorLocaleData
    {
        public IReadOnlyList<string> AvailableLocales { get; } = available;

        public IReadOnlyList<string> Collations { get; } = ["phonebk"];

        public IReadOnlyList<string?> SortLocaleData(string locale) => JsRealm.VmCollatorKeyData(locale, "co");

        internal IReadOnlyList<string?> KeyData(string locale, string key) => JsRealm.VmCollatorKeyData(locale, key);
    }

    // ---- the correspondence's rows ------------------------------------------------------------------------------------------

    /// <summary>
    /// The differences of sub-area (a) that Broiler.VM's correspondence classes: L9 and L18, corrections of Broiler.VM the
    /// library makes (decision D32), and L22, each engine's own numbering-system data (data policy C5, decision D5).
    /// </summary>
    internal static IEnumerable<KnownDifference> Known()
    {
        // ROW L9: a -u-sd or -u-rg value whose subdivision alias is a region. Broiler.VM writes the region as given
        // (`en-u-sd-AX`), which is no canonical tag; the library writes UTS #35 Annex C's `axzzzz`. Every answer that reads
        // such a tag differs, through Intl.Locale's keywords, week and hour cycles too.
        static bool NamesSubdivisionWithARegion(string input)
        {
            foreach (Match match in SubdivisionKeyword().Matches(input.ToLowerInvariant()))
            {
                if (SubdivisionsWithARegion.Value.Contains(match.Groups[1].Value))
                    return true;
            }

            return false;
        }

        yield return new KnownDifference("L1", "L9", (input, vm, library) => NamesSubdivisionWithARegion(input));
        yield return new KnownDifference("L2", "L9", (input, vm, library) => NamesSubdivisionWithARegion(input));
        yield return new KnownDifference("L3", "L9", (input, vm, library) => NamesSubdivisionWithARegion(input));

        // ROW L18: getCollations of a Locale with no -u-co-, whose language is not und and which no Collator locale matches:
        // Broiler.VM answers emoji and eor; the library DefaultLocale's collations, en-US's none.
        yield return new KnownDifference("L3", "L18", (input, vm, library) =>
            input.StartsWith("getCollations ", StringComparison.Ordinal)
            && vm == "[emoji,eor]" && library == "[]"
            && LocaleOf(input) is { } locale && locale.Language != "und" && locale.UnicodeExtensionValue("co") is null);

        // ROW L22: getNumberingSystems of a Locale with no -u-nu-: Broiler.VM answers latn whatever the data; the library the
        // matched NumberFormat locale's default from the data, which differs only where the data names one other than latn
        // (Broiler.JS's embedding).
        yield return new KnownDifference("L3", "L22", (input, vm, library) =>
            input.StartsWith("getNumberingSystems ", StringComparison.Ordinal)
            && vm == "[latn]" && SingleSystem().IsMatch(library) && library != "[latn]"
            && LocaleOf(input) is { } locale && locale.UnicodeExtensionValue("nu") is null);
    }

    /// <summary>The Locale a method's input names: the text after the method's name, up to " of ".</summary>
    private static LocaleId? LocaleOf(string input)
    {
        var start = input.IndexOf(' ', StringComparison.Ordinal) + 1;
        var end = input.IndexOf(" of ", start, StringComparison.Ordinal);
        return end > start && LocaleId.TryParse(input[start..end], out var id) == IntlStatus.Completed ? id : null;
    }

    /// <summary>The subdivision aliases whose first replacement is a region (CLDR's <c>subdivisionAlias</c>), the same in both embeddings.</summary>
    private static readonly Lazy<HashSet<string>> SubdivisionsWithARegion = new(static () =>
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var embedding in Embedding.Both())
        {
            foreach (var fields in embedding.Lines(IntlTable.Aliases).Select(static l => l.Split('|')).Where(static f => f[0] == "subdivision"))
            {
                var replacement = fields[2].Split(' ')[0];
                if (replacement.Length == 2 && replacement.All(char.IsAsciiLetter) || replacement.Length == 3 && replacement.All(char.IsAsciiDigit))
                    set.Add(fields[1]);
            }
        }

        return set;
    });

    [GeneratedRegex("(?:^|-|\")(?:sd|rg)-([a-z0-9]{3,8})(?=-|\"|$| |,|\\]|_)", RegexOptions.CultureInvariant)]
    private static partial Regex SubdivisionKeyword();

    [GeneratedRegex("^\\[[a-z0-9]+\\]$", RegexOptions.CultureInvariant)]
    private static partial Regex SingleSystem();
}
