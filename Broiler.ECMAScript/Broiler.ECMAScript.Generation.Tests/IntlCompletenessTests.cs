using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Broiler.ECMAScript.Generation.Intl;
using Broiler.ECMAScript.Runners;
using Xunit;

namespace Broiler.ECMAScript.Generation.Tests;

/// <summary>
/// The completeness test of the milestone's exit clause 3, read per embedding (decision JSD-0082, decision D5):
/// every locale of each list has every table its group carries - its own main data, or its CLDR parent's, and its
/// language's supplemental rules where CLDR states them - with decision D34's keys for every locale of Broiler.JS's
/// list, so that none falls back to <c>:</c> and one-digit hours for want of a key.
/// </summary>
public sealed class IntlCompletenessTests
{
    /// <summary>The tables of the main-data group, each holding lines keyed by a main-data locale.</summary>
    private static readonly string[] MainTables = ["NumberLocales", "Currencies", "Units", "ListPatterns", "RelativeTimes", "DisplayNames", "DateLocales"];

    /// <summary>The supplemental tables and the CLDR file and object each covers the languages of.</summary>
    private static readonly (string Table, string File, string Object)[] SupplementalTables =
    [
        ("Plurals", "plurals.json", "plurals-type-cardinal"),
        ("Ordinals", "ordinals.json", "plurals-type-ordinal"),
        ("PluralRanges", "pluralRanges.json", "plurals"),
        ("DayPeriods", "dayPeriods.json", "dayPeriodRuleSet"),
    ];

    /// <summary>The number keys every main-data locale holds: its symbols and patterns, and decision D34's clock data.</summary>
    private static readonly string[] NumberKeys =
    [
        "defaultNumberingSystem", "symbols.decimal", "symbols.group", "symbols.minusSign", "symbols.percentSign",
        "decimal.standard", "percent.standard", "currency.standard", "symbols.timeSeparator", "duration.hm", "duration.hms", "duration.ms",
    ];

    /// <summary>The date keys every main-data locale holds.</summary>
    private static readonly string[] DateKeys =
    [
        "months.format.wide.1", "days.format.wide.sun", "dateFormats.short", "timeFormats.short", "dateTime.medium", "atTime.medium",
        "zone.gmtFormat", "zone.regionFormat", "field.year",
    ];

    private static readonly Lazy<IntlTables> BroilerJs = new(static () => IntlDataHome.GenerateBroilerJs(IntlDataHome.Current));

    private static readonly Lazy<IntlTables> BroilerVm = new(static () => IntlDataHome.GenerateBroilerVm(IntlDataHome.Current));

    [Fact]
    public void Every_locale_of_Broiler_JS_s_list_has_every_table_its_group_carries()
    {
        var list = IntlDataHome.ReadList(IntlDataHome.Current, IntlDataHome.BroilerJsList);
        Assert.Empty(Violations(BroilerJs.Value, list, rootResolvesToDefault: false));
    }

    [Fact]
    public void Every_locale_of_Broiler_VM_s_list_has_every_table_its_group_carries()
    {
        // Broiler.VM resolves a locale its data lacks, und among them, to its default locale, en-US (decision D7),
        // so its list carries no root data and und is held to nothing.
        var list = IntlDataHome.ReadList(IntlDataHome.Current, IntlDataHome.BroilerVmList);
        Assert.Empty(Violations(BroilerVm.Value, list, rootResolvesToDefault: true));
    }

    /// <summary>The control: one locale's number lines removed, and one supplemental language's rules, are each reported.</summary>
    [Fact]
    public void A_locale_without_a_table_is_reported()
    {
        var list = IntlDataHome.ReadList(IntlDataHome.Current, IntlDataHome.BroilerJsList);
        var tables = BroilerJs.Value;

        var numbers = Without(tables, "NumberLocales", static line => line.StartsWith("fr|", StringComparison.Ordinal));
        Assert.Contains("fr has no NumberLocales line", Violations(numbers, list, rootResolvesToDefault: false));
        Assert.Contains("fr-FR has no NumberLocales line through fr", Violations(numbers, list, rootResolvesToDefault: false));

        var clock = Without(tables, "NumberLocales", static line => line.StartsWith("lt|duration.hms|", StringComparison.Ordinal));
        Assert.Contains("lt has no NumberLocales key duration.hms", Violations(clock, list, rootResolvesToDefault: false));

        var plurals = Without(tables, "Plurals", static line => line.StartsWith("cy|", StringComparison.Ordinal));
        Assert.Contains("Plurals lacks cy, whose rules CLDR states", Violations(plurals, list, rootResolvesToDefault: false));

        // A default-content locale whose language has no main data in the list reaches only und, which is reported.
        var text = File.ReadAllText(IntlDataHome.ListPath(IntlDataHome.Current, IntlDataHome.BroilerJsList));
        var withKaGe = IntlLocaleList.Parse(text.Replace("locale jbo main\n", "locale jbo main\nlocale ka-GE\n", StringComparison.Ordinal));
        Assert.Contains("ka-GE", withKaGe.Locales);
        Assert.Contains("ka-GE reads its data through no main-data locale of its list", Violations(tables, withKaGe, rootResolvesToDefault: false));
    }

    /// <summary>
    /// Decision D34's data, as the decision states it for Broiler.JS's list: two-digit hours for <c>af</c> and
    /// <c>lt</c> (row U14), and <c>.</c> as the clock's separator for <c>da</c>, <c>en-DK</c>, <c>id</c> and
    /// <c>si</c> (row U13); and decision D21's root symbols for <c>arab</c>.
    /// </summary>
    [Fact]
    public void Decision_D34_s_and_D21_s_data_are_what_the_decisions_state()
    {
        var numbers = Lines(BroilerJs.Value["NumberLocales"]);
        var list = IntlDataHome.ReadList(IntlDataHome.Current, IntlDataHome.BroilerJsList);

        Assert.Equal(["af", "lt"], list.MainDataLocales.Where(locale => numbers.Contains(locale + "|duration.hms|hh:mm:ss")));
        Assert.Equal(["da", "en-DK", "id", "si"], list.MainDataLocales.Where(locale => numbers.Contains(locale + "|symbols.timeSeparator|.")));

        Assert.Contains("und|symbols@arab.decimal|\\u066B", numbers);
        Assert.Contains("und|symbols@arab.group|\\u066C", numbers);
        Assert.Contains("und|symbols@arab.minusSign|\\u061C-", numbers);
        Assert.DoesNotContain(numbers, static line => line.StartsWith("und|symbols@beng.", StringComparison.Ordinal));

        // Broiler.VM's tables carry the latn symbols alone (decision D21).
        Assert.DoesNotContain(Lines(BroilerVm.Value["NumberLocales"]), static line => line.Contains("|symbols@", StringComparison.Ordinal));
    }

    /// <summary>Every way <paramref name="tables"/> fail the completeness reading for <paramref name="list"/>.</summary>
    private static List<string> Violations(IntlTables tables, IntlLocaleList list, bool rootResolvesToDefault)
    {
        var violations = new List<string>();
        var main = list.MainDataLocales.ToHashSet(StringComparer.Ordinal);
        var lines = MainTables.ToDictionary(static name => name, name => Lines(tables[name]), StringComparer.Ordinal);
        var parents = Parents();

        // EACH MAIN-DATA LOCALE has lines in every main table, and the keys the formatters read.
        foreach (var locale in main)
        {
            foreach (var table in MainTables)
            {
                if (!lines[table].Any(line => line.StartsWith(locale + "|", StringComparison.Ordinal)))
                    violations.Add(locale + " has no " + table + " line");
            }

            foreach (var key in NumberKeys)
            {
                if (!lines["NumberLocales"].Any(line => line.StartsWith(locale + "|" + key + "|", StringComparison.Ordinal)))
                    violations.Add(locale + " has no NumberLocales key " + key);
            }

            foreach (var key in DateKeys)
            {
                if (!lines["DateLocales"].Any(line => line.StartsWith(locale + "|" + key + "|", StringComparison.Ordinal)))
                    violations.Add(locale + " has no DateLocales key " + key);
            }
        }

        // EVERY OTHER LOCALE reads its data through its CLDR parent: parentLocales.json's, or the tag truncated. One
        // that reaches root only by truncation - a default-content locale whose language has no main data in the list
        // - reads und's data, which is no locale's of its own, so it is reported; parentLocales.json naming root as the
        // locale's own parent is what makes und its data.
        foreach (var locale in list.Locales.Where(tag => !main.Contains(tag)))
        {
            if (locale == "und" && rootResolvesToDefault)
                continue;

            var parent = locale;
            while (!main.Contains(parent) && parent != "und")
                parent = parents.TryGetValue(parent, out var stated) ? stated : parent.Contains('-', StringComparison.Ordinal) ? parent[..parent.LastIndexOf('-')] : "und";

            var rootIsItsParent = parents.TryGetValue(locale, out var own) && own == "und";
            if (!main.Contains(parent) || (parent == "und" && locale != "und" && !rootIsItsParent))
            {
                violations.Add(locale + " reads its data through no main-data locale of its list");
                continue;
            }

            foreach (var table in MainTables)
            {
                if (!lines[table].Any(line => line.StartsWith(parent + "|", StringComparison.Ordinal)))
                    violations.Add(locale + " has no " + table + " line through " + parent);
            }
        }

        // THE SUPPLEMENTAL TABLES cover every language CLDR states rules for, or the list's languages.
        var archive = IntlDataHome.CldrFiles(IntlDataHome.Current, IntlDataHome.ReadCldrPin(IntlDataHome.Current));
        foreach (var (table, file, name) in SupplementalTables)
        {
            using var document = JsonDocument.Parse(archive["cldr-48.2.0/json/cldr-core/supplemental/" + file]);
            var stated = document.RootElement.GetProperty("supplemental").GetProperty(name).EnumerateObject().Select(static language => language.Name).ToHashSet(StringComparer.Ordinal);
            var written = Lines(tables[table]).Select(static line => line[..line.IndexOf('|', StringComparison.Ordinal)]).ToHashSet(StringComparer.Ordinal);
            var wanted = list.EverySupplementalLanguage ? stated : list.SupplementalLanguages.ToHashSet(StringComparer.Ordinal);

            foreach (var language in wanted.Except(written).Order(StringComparer.Ordinal))
                violations.Add(table + " lacks " + language + (stated.Contains(language) ? ", whose rules CLDR states" : ", whose rules CLDR does not state"));
            foreach (var language in written.Except(wanted).Order(StringComparer.Ordinal))
                violations.Add(table + " holds " + language + ", which the list does not name");

            // Every language of the list whose cardinal rules CLDR states has them written.
            if (table == "Plurals")
            {
                foreach (var language in list.Locales.Select(static tag => tag.Split('-')[0]).Distinct().Where(stated.Contains).Where(language => wanted.Contains(language) || list.EverySupplementalLanguage))
                {
                    if (!written.Contains(language))
                        violations.Add(language + " has no Plurals line");
                }
            }
        }

        return violations;
    }

    /// <summary>CLDR 48.2.0's parent locales, from <c>parentLocales.json</c>.</summary>
    private static Dictionary<string, string> Parents()
    {
        var archive = IntlDataHome.CldrFiles(IntlDataHome.Current, IntlDataHome.ReadCldrPin(IntlDataHome.Current));
        using var document = JsonDocument.Parse(archive["cldr-48.2.0/json/cldr-core/supplemental/parentLocales.json"]);
        return document.RootElement.GetProperty("supplemental").GetProperty("parentLocales").GetProperty("parentLocale")
            .EnumerateObject().ToDictionary(static entry => entry.Name, static entry => entry.Value.GetString()!, StringComparer.Ordinal);
    }

    private static List<string> Lines(IntlGeneratedTable table) => [.. Encoding.ASCII.GetString(table.Data.Span).Split('\n')];

    private static IntlTables Without(IntlTables tables, string name, Func<string, bool> removed)
    {
        var table = tables[name];
        var kept = Encoding.ASCII.GetBytes(string.Join('\n', Lines(table).Where(line => !removed(line))));
        return new IntlTables(tables.CldrVersion, tables.UcaVersion, tables.Tables.Select(each => each.Name == name ? each with { Data = kept } : each).ToList(), tables.AbsentFiles);
    }
}
