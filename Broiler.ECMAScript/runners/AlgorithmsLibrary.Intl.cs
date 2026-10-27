using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Broiler.ECMAScript.Conformance;
using Broiler.ECMAScript.Intl;

namespace Broiler.ECMAScript.Runners;

/// <summary>
/// The Intl area's sub-area (a), locale identifiers, likely subtags, negotiation, Locale info and
/// <c>supportedValuesOf</c> (corpus revision 27, <c>conformance/vectors/intl/locales.vectors</c>; milestone SJL-9.3a): each
/// operation as an engine's adapter calls <c>Broiler.ECMAScript.Intl</c>, over the data home's generated tables, with the
/// script's own reading of the result where a vector's script shows it through a built-in.
/// </summary>
/// <remarks>
/// <para>
/// <b>The tables are the data home's</b>, <c>Broiler.ECMAScript/data/intl/generated/</c>, Broiler.JS's embedding, read from
/// disk as committed and held to the lengths and SHA-256 their manifest records, as the time tables are. Their global
/// tables - likely subtags, aliases, the BCP 47 data, time, week and script data - are Broiler.VM's (Broiler.VM's JSD-0082,
/// decision D5), and no answer of the sub-area reads a locale's main data but the matched locale's default numbering system.
/// </para>
/// <para>
/// <b>The engine's parameters are Broiler.VM's</b> (decision D7, rows L28, L33 and L40): the default locale <c>en-US</c>, the
/// available locales of its data, <c>de</c>, <c>de-DE</c>, <c>en</c> and <c>en-US</c>, and its <c>Intl.Collator</c>'s data,
/// German's <c>phonebk</c> alone (JSD-0043); and each service's relevant keys and their values as its constructor hands them
/// to ResolveLocale. They are an engine's policy, which ECMA-402 leaves to the implementation, so a vector that pins one
/// is held to Broiler.VM's <c>per-engine</c> expectation (<see cref="MatchesBroilerVmExpectation"/>). Broiler.VM's are the
/// ones its plan states as fixed lists; Broiler.JS's follow the host's cultures.
/// </para>
/// <para>
/// <b>What the vector's script does around the call is written here once more</b>, as an engine's adapter does it: the
/// elements of the locales argument as Strings, the options read, each status mapped to a <c>RangeError</c>, and the
/// script's <c>JSON.stringify</c>, <c>join</c> and <c>typeof</c>. Every step of the sub-area is the library's.
/// </para>
/// <para>
/// <b>The operation names collide with another area's in one place</b>: <c>String.prototype.toLocaleLowerCase</c> is also
/// the Unicode area's (<c>unicode/casing.vectors</c>), whose vectors no library this runner calls answers, and
/// <see cref="IVectorLibrary.Call"/> sees no area. The Intl area's vector of it is therefore not answered here either
/// (<see cref="IsEngineLevelIntl"/>); casing is Broiler.Unicode's, and the default locale it reads is the engine's.
/// </para>
/// </remarks>
internal sealed partial class AlgorithmsLibrary
{
    /// <summary>Broiler.VM's default locale (decision D7).</summary>
    internal const string IntlDefaultLocale = "en-US";

    /// <summary>The identifier prefix of the Intl area's sub-area (a) vectors, the file <c>intl/locales.vectors</c>.</summary>
    internal const string IntlLocalesPrefix = "intl/locales/";

    private static readonly Lazy<TableLocaleData> IntlTables = new(LoadIntlTables);

    /// <summary>The locale data every Intl vector reads.</summary>
    private static TableLocaleData IntlData => IntlTables.Value;

    /// <summary>Broiler.VM's available locales (decision D7): its data's locales without <c>und</c>.</summary>
    private static IReadOnlyList<string> IntlAvailable { get; } = ["de", "de-DE", "en", "en-US"];

    private static readonly VmCollatorData IntlCollator = new();

    /// <summary>
    /// Whether a sub-area (a) vector is engine-level: answered by no library, because its outcome is an engine's alone or
    /// another sub-area's.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><c>String.prototype.toLocaleLowerCase</c>: the Unicode area's operation too (see the type's remarks); casing is
    /// Broiler.Unicode's, and with no locale it reads the engine's default-locale provider (row L35).</item>
    /// <item><c>Number.prototype.toLocaleString</c>: formatting a number, sub-area (c)'s, with the engine's default locale
    /// (row L33).</item>
    /// <item><c>Intl.NumberFormat.supportedLocalesOf</c>: both vectors pin the engine's available-locale provider (rows L28 and
    /// L44): one names locales of the host's cultures, the other prepends the default locale an engine resolves, which the
    /// vector's inputs do not carry.</item>
    /// <item><c>Intl.DisplayNames.supportedLocalesOf</c> with no argument: <c>typeof</c> of the built-in, whether the engine
    /// defines it (row L37).</item>
    /// </list>
    /// </remarks>
    internal static bool IsEngineLevelIntl(string operation, string input) =>
        operation is "String.prototype.toLocaleLowerCase" or "Number.prototype.toLocaleString" or "Intl.NumberFormat.supportedLocalesOf"
        || (operation == "Intl.DisplayNames.supportedLocalesOf" && input == "undefined");

    /// <summary>Whether a vector is the Intl area's that this runner answers: sub-area (a)'s, but the engine-level ones.</summary>
    internal static bool IsLibraryIntl(VectorRecord vector) =>
        vector.Id.StartsWith(IntlLocalesPrefix, StringComparison.Ordinal) && !IsEngineLevelIntl(vector["op"] ?? string.Empty, vector["in"] ?? string.Empty);

    /// <summary>
    /// Whether a difference is the answer Broiler.VM's plan commits it to, the engine whose parameters this runner takes: an
    /// <c>impl-defined</c> vector of the Intl area whose <c>per-engine.broiler-vm</c> expectation the library gives.
    /// </summary>
    internal static bool MatchesBroilerVmExpectation(VectorResult result) =>
        result.Verdict == Verdict.Difference
        && result.Vector["area"] == "intl"
        && result.Vector.Class == "impl-defined"
        && result.Vector["per-engine.broiler-vm"] is { } expected
        && Outcome.TryParseAlternatives(expected, out var alternatives, out _, out _)
        && Outcome.AnyIs(alternatives, result.Rendered);

    private static string IntlArea(string operation, IReadOnlyList<TypedValue> inputs)
    {
        if (IsEngineLevelIntl(operation, inputs.Count == 1 && inputs[0].Kind == TypedValueKind.Undefined ? "undefined" : string.Empty))
            return EmptyLibrary.NotImplemented;

        switch (operation)
        {
            case "Intl.getCanonicalLocales":
                return Requested(inputs) is { } canonical ? Text(string.Join(",", canonical)) : RangeError;

            // The Locale constructor, and its toString.
            case "Intl.Locale":
            case "Intl.Locale.prototype.toString":
                return NewLocale(inputs[0].Chars, LocaleOptions.None) is { } created ? Text(created.Locale) : RangeError;
            case "Intl.Locale (language option)":
                return LocaleWith(inputs, new LocaleOptions { Language = inputs[1].Chars });
            case "Intl.Locale (region option)":
                return LocaleWith(inputs, new LocaleOptions { Region = inputs[1].Chars });
            case "Intl.Locale (variants option)":
                return LocaleWith(inputs, new LocaleOptions { Variants = inputs[1].Chars });
            case "Intl.Locale (calendar option)":
                return LocaleWith(inputs, new LocaleOptions { Calendar = inputs[1].Chars });
            case "Intl.Locale (collation option)":
                return LocaleWith(inputs, new LocaleOptions { Collation = inputs[1].Chars });
            case "Intl.Locale (firstDayOfWeek option)":
                return LocaleWith(inputs, new LocaleOptions { FirstDayOfWeek = inputs[1].Chars });
            case "Intl.Locale (numberingSystem option)":
                return LocaleWith(inputs, new LocaleOptions { NumberingSystem = inputs[1].Chars });

            // Add and Remove Likely Subtags: a new Locale of the result, as maximize and minimize make it.
            case "Intl.Locale.prototype.maximize":
            case "Intl.Locale.prototype.minimize":
            {
                if (NewLocale(inputs[0].Chars, LocaleOptions.None) is not { } locale || LocaleId.TryParse(locale.Locale, out var id) != IntlStatus.Completed)
                    return RangeError;

                var changed = operation.EndsWith("maximize", StringComparison.Ordinal) ? id!.Maximize(IntlData, out _) : id!.Minimize(IntlData, out _);
                return NewLocale(changed.ToString(), LocaleOptions.None) is { } result ? Text(result.Locale) : RangeError;
            }

            // The information methods, through JSON.stringify where the script uses it.
            case "Intl.Locale.prototype.getCalendars":
                return LocaleInfoOf(inputs, static locale => Json(LocaleInfo.Calendars(locale)));
            case "Intl.Locale.prototype.getCollations":
                return LocaleInfoOf(inputs, static locale => Json(LocaleInfo.Collations(locale, IntlCollator, IntlDefaultLocale)));
            case "Intl.Locale.prototype.getHourCycles":
                return LocaleInfoOf(inputs, static locale => Json(LocaleInfo.HourCycles(locale, IntlData)));
            case "Intl.Locale.prototype.getNumberingSystems":
                return LocaleInfoOf(inputs, static locale => Json(LocaleInfo.NumberingSystems(locale, IntlAvailable, IntlData)));
            case "Intl.Locale.prototype.getTextInfo":
                return LocaleInfoOf(inputs, static locale => LocaleInfo.TextDirection(locale, IntlData) is { } direction ? Text(direction) : "value: undefined");
            case "Intl.Locale.prototype.getWeekInfo":
                return LocaleInfoOf(inputs, static locale =>
                {
                    var week = LocaleInfo.WeekInfo(locale, IntlData);
                    var text = new StringBuilder("[").Append(Decimal(week.FirstDay)).Append(",[");
                    for (var i = 0; i < week.Weekend.Count; i++)
                        text.Append(i == 0 ? string.Empty : ",").Append(Decimal(week.Weekend[i]));
                    return Text(text.Append("]]").ToString());
                });
            case "Intl.Locale.prototype.getTimeZones, JSON":
                return LocaleInfoOf(inputs, static locale => LocaleInfo.TimeZones(locale, Zones) is { } zones ? Json(zones) : "value: undefined");
            case "Intl.Locale.prototype.getTimeZones, typeof":
                return LocaleInfoOf(inputs, static locale => Text(LocaleInfo.TimeZones(locale, Zones) is null ? "undefined" : "object"));

            // Each service's resolved locale.
            case "Intl.NumberFormat":
            case "Intl.NumberFormat, resolvedOptions().locale":
            case "Intl.DurationFormat, resolvedOptions().locale":
            case "Intl.RelativeTimeFormat, resolvedOptions().locale":
                return Resolved(inputs, static _ => [new RelevantKey("nu", NumberingSystemValues())]) is { } nu ? Text(nu.Locale) : RangeError;
            case "Intl.DateTimeFormat":
            case "Intl.DateTimeFormat, resolvedOptions().locale":
                return Resolved(inputs, static _ =>
                [
                    new RelevantKey("ca", CalendarValues),
                    new RelevantKey("hc", HourCycleValues),
                    new RelevantKey("nu", NumberingSystemValues()),
                ]) is { } dtf ? Text(dtf.Locale) : RangeError;
            case "Intl.PluralRules":
            case "Intl.PluralRules, resolvedOptions().locale":
            case "Intl.ListFormat":
            case "Intl.ListFormat, resolvedOptions().locale":
            case "Intl.DisplayNames, resolvedOptions().locale":
            case "Intl.Segmenter, resolvedOptions().locale":
                return Resolved(inputs, static _ => []) is { } plain ? Text(plain.Locale) : RangeError;
            case "Intl.Collator, resolvedOptions().locale":
                return Resolved(inputs, static locale => CollatorKeys(locale, null)) is { } collator ? Text(collator.Locale) : RangeError;
            case "Intl.Collator, resolvedOptions()":
            {
                // new Intl.Collator(tag, {collation}): the option is checked as the constructor reads it.
                if (!LocaleId.IsUnicodeType(inputs[1].Chars))
                    return RangeError;

                return Resolved([inputs[0]], locale => CollatorKeys(locale, inputs[1].Chars)) is { } resolved
                    ? Json([resolved.Locale, resolved.KeyValues["co"] ?? "default"])
                    : RangeError;
            }

            case "Intl.NumberFormat (numberingSystem option)":
            {
                if (Requested([inputs[0]]) is null || !LocaleId.IsUnicodeType(inputs[1].Chars))
                    return RangeError;

                return Resolved([inputs[0]], _ => [new RelevantKey("nu", NumberingSystemValues()) { Option = inputs[1].Chars }]) is { } resolved
                    ? Text(resolved.KeyValues["nu"] ?? "latn")
                    : RangeError;
            }

            case "Intl.DisplayNames.supportedLocalesOf":
                return Requested(inputs) is { } requested ? Text(string.Join(",", LocaleNegotiation.FilterLocales(IntlAvailable, requested))) : RangeError;

            case "Intl.supportedValuesOf":
                return SupportedValues.Of(inputs[0].Chars, IntlData, Zones, IntlCollator, out var values) == IntlStatus.Completed ? Json(values) : RangeError;
        }

        return EmptyLibrary.NotImplemented;
    }

    /// <summary>CanonicalizeLocaleList over the inputs, each a String element; <c>undefined</c> alone is no argument.</summary>
    private static IReadOnlyList<string>? Requested(IReadOnlyList<TypedValue> inputs)
    {
        var tags = new List<string>();
        foreach (var input in inputs)
        {
            if (input.Kind == TypedValueKind.Undefined)
                continue;
            if (input.Kind != TypedValueKind.String)
                throw new NotSupportedException("No locale vector gives a locale as a " + input.Kind + ".");
            tags.Add(input.Chars);
        }

        return LocaleNegotiation.CanonicalizeLocaleList(tags, IntlData, out var locales, out _) == IntlStatus.Completed ? locales : null;
    }

    /// <summary>A service's ResolveLocale over Broiler.VM's providers, with its relevant keys for the matched locale.</summary>
    private static ResolvedLocale? Resolved(IReadOnlyList<TypedValue> inputs, Func<string, IReadOnlyList<RelevantKey>> keys)
    {
        if (Requested(inputs) is not { } requested)
            return null;

        var match = LocaleNegotiation.MatchLocale(IntlAvailable, requested, IntlDefaultLocale);
        return LocaleNegotiation.ResolveLocale(match, keys(match.Locale), IntlData);
    }

    /// <summary>The Locale a tag and options make, or <see langword="null"/> for the constructor's <c>RangeError</c>.</summary>
    private static LocaleRecord? NewLocale(string tag, LocaleOptions options) =>
        LocaleRecord.TryCreate(tag, options, IntlData, out var record) == IntlStatus.Completed ? record : null;

    private static string LocaleWith(IReadOnlyList<TypedValue> inputs, LocaleOptions options) =>
        NewLocale(inputs[0].Chars, options) is { } locale ? Text(locale.Locale) : RangeError;

    private static string LocaleInfoOf(IReadOnlyList<TypedValue> inputs, Func<LocaleRecord, string> read) =>
        NewLocale(inputs[0].Chars, LocaleOptions.None) is { } locale ? read(locale) : RangeError;

    /// <summary>
    /// Broiler.VM's NumberFormat, DateTimeFormat, RelativeTimeFormat and DurationFormat <c>nu</c> values: <c>latn</c> first,
    /// then the data's other numbering systems in code-unit order.
    /// </summary>
    private static IReadOnlyList<string?> NumberingSystemValues()
    {
        var others = new List<string>();
        foreach (var system in IntlData.NumberingSystems)
        {
            if (system != "latn")
                others.Add(system);
        }

        others.Sort(StringComparer.Ordinal);
        var values = new List<string?> { "latn" };
        values.AddRange(others);
        return values;
    }

    /// <summary>Broiler.VM's DateTimeFormat <c>ca</c> values: the Gregorian calendar first, then the others (its <c>CalendarKeyValues</c>).</summary>
    private static readonly IReadOnlyList<string?> CalendarValues =
    [
        "gregory", "buddhist", "chinese", "coptic", "dangi", "ethioaa", "ethiopic", "hebrew", "indian", "islamic",
        "islamic-civil", "islamic-rgsa", "islamic-tbla", "islamic-umalqura", "iso8601", "japanese", "persian", "roc",
    ];

    /// <summary>Broiler.VM's DateTimeFormat <c>hc</c> values.</summary>
    private static readonly IReadOnlyList<string?> HourCycleValues = [null, "h11", "h12", "h23", "h24"];

    /// <summary>Broiler.VM's Collator relevant keys for a matched locale, sort usage (its <c>CollatorKeyData</c>).</summary>
    private static IReadOnlyList<RelevantKey> CollatorKeys(string locale, string? collation) =>
    [
        new RelevantKey("co", IntlCollator.SortLocaleData(locale)) { Option = collation },
        new RelevantKey("kf", ["false", "lower", "upper"]),
        new RelevantKey("kn", ["false", "true"]),
    ];

    /// <summary>JSON.stringify of a list of ASCII Strings.</summary>
    private static string Json(IReadOnlyList<string> values)
    {
        var text = new StringBuilder("[");
        for (var i = 0; i < values.Count; i++)
        {
            text.Append(i == 0 ? "\"" : ",\"");
            foreach (var c in values[i])
            {
                if (c is '"' or '\\' || c < ' ')
                    text.Append("\\u").Append(((int)c).ToString("x4", NumberFormatInfo.InvariantInfo));
                else
                    text.Append(c);
            }

            text.Append('"');
        }

        return Text(text.Append(']').ToString());
    }

    private static string Decimal(int value) => value.ToString(NumberFormatInfo.InvariantInfo);

    /// <summary>
    /// Broiler.VM's <c>Intl.Collator</c> data (JSD-0043): its available locales, which are its data's, and German's
    /// <c>phonebk</c> alone (<c>CollatorKeyData</c> for sort usage), which <c>supportedValuesOf("collation")</c> lists.
    /// </summary>
    private sealed class VmCollatorData : ICollatorLocaleData
    {
        public IReadOnlyList<string> AvailableLocales => IntlAvailable;

        public IReadOnlyList<string> Collations { get; } = ["phonebk"];

        public IReadOnlyList<string?> SortLocaleData(string locale) =>
            LocaleId.TryParse(locale, out var id) == IntlStatus.Completed && id!.Language == "de" ? [null, "phonebk"] : [null];
    }

    /// <summary>The data home's Broiler.JS tables, each file held to its manifest's length and SHA-256.</summary>
    private static TableLocaleData LoadIntlTables()
    {
        var generated = IntlTablesDirectory(AppContext.BaseDirectory);
        string? cldrVersion = null;
        var recorded = new Dictionary<string, (long Length, string Sha256)>(StringComparer.Ordinal);
        foreach (var line in File.ReadAllLines(Path.Combine(generated, "tables.manifest")))
        {
            var fields = line.Split(' ');
            if (fields.Length == 2 && fields[0] == "cldr-version")
                cldrVersion = fields[1];
            else if (fields.Length == 4 && fields[0] == "table")
                recorded[fields[1]] = (long.Parse(fields[2], NumberStyles.None, NumberFormatInfo.InvariantInfo), fields[3]);
        }

        var tables = new Dictionary<IntlTable, byte[]>();
        foreach (var table in Enum.GetValues<IntlTable>())
        {
            foreach (var extension in new[] { ".txt", ".bin" })
            {
                var name = IntlTableFileName(table) + extension;
                if (!recorded.TryGetValue(name, out var expected))
                    continue;

                var bytes = File.ReadAllBytes(Path.Combine(generated, name));
                if (expected.Length != bytes.Length || expected.Sha256 != Convert.ToHexStringLower(SHA256.HashData(bytes)))
                    throw new InvalidDataException(name + " is not the table its manifest records");
                tables[table] = bytes;
            }
        }

        return new TableLocaleData(new DiskIntlData(cldrVersion ?? throw new InvalidDataException("the Intl tables' manifest names no CLDR version"), tables));
    }

    /// <summary>A table's file name as the generator writes it, without its extension: <c>LikelySubtags</c> as <c>likely-subtags</c>.</summary>
    private static string IntlTableFileName(IntlTable table)
    {
        var name = new StringBuilder();
        foreach (var c in table.ToString())
        {
            if (c is >= 'A' and <= 'Z' && name.Length > 0)
                name.Append('-');
            name.Append(c is >= 'A' and <= 'Z' ? (char)(c + 32) : c);
        }

        return name.ToString();
    }

    /// <summary>The Intl data home's generated area, found from <paramref name="start"/> upwards.</summary>
    private static string IntlTablesDirectory(string start)
    {
        for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "Broiler.ECMAScript", "data", "intl", "generated");
            if (File.Exists(Path.Combine(candidate, "tables.manifest")))
                return candidate;
        }

        throw new DirectoryNotFoundException("no Broiler.ECMAScript/data/intl/generated/tables.manifest above " + start);
    }

    /// <summary>The tables as an engine hands them in (decision SJL-D-6 (c)).</summary>
    private sealed class DiskIntlData(string cldrVersion, Dictionary<IntlTable, byte[]> tables) : IIntlDataSet
    {
        public string CldrVersion { get; } = cldrVersion;

        public ReadOnlySpan<byte> Table(IntlTable table) => tables.TryGetValue(table, out var bytes) ? bytes : default;
    }
}
