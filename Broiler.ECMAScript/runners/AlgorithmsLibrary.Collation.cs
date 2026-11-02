using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Broiler.ECMAScript.Conformance;
using Broiler.ECMAScript.Intl;
using Broiler.Unicode.Algorithms;

namespace Broiler.ECMAScript.Runners;

/// <summary>
/// The collation vectors, <c>conformance/vectors/collation/</c> (identifiers <c>intl/collation/</c>, corpus revisions 33 and 34;
/// milestone SJL-17.2), answered at library level from milestone SJL-17.3: each script's <c>Intl.Collator</c> or
/// <c>localeCompare</c> as an engine's adapter calls the shared collator, <see cref="UcaCollator"/>, over the data home's
/// generated tables - Broiler.JS's embedding, whose collation tables carry the tailorings of Broiler.VM's JSD-0084, decision E3.
/// </summary>
/// <remarks>
/// <para>
/// <b>What the script does around the call is written here once more</b>, as an engine's adapter does it: the Collator
/// constructor's reads in the specification's order (<c>usage</c>, <c>collation</c> checked as a type, <c>numeric</c>,
/// <c>caseFirst</c>), ResolveLocale over the relevant keys <c>co</c>, <c>kf</c> and <c>kn</c>, then <c>sensitivity</c> and
/// <c>ignorePunctuation</c>, whose default is the tailoring's data (Thai's shifted alternate, JSD-0084, decision E5); the
/// tailoring the resolved locale, usage and collation select; and the script's <c>Math.sign</c>, its stable
/// <c>Array.prototype.sort</c> with the comparator, and the relation it writes between neighbours.
/// </para>
/// <para>
/// <b>The engine's parameters</b>: the default locale is Broiler.VM's, <c>en-US</c> (decision D7, as every Intl vector of this
/// runner reads it), and the Collator's available locales are the locales whose collation the tables cover, JSD-0084's
/// decision E3: <c>de</c>, <c>en</c>, <c>es</c>, <c>sv</c> and <c>th</c>, with Broiler.VM's <c>de-DE</c> and <c>en-US</c>. So a
/// tailoring vector, whose answer for Broiler.VM is its narrower locale list's (rule N28), gives its <c>out</c>, which is
/// Broiler.JS's expectation; and Han across blocks gives UCA 17's implicit weights by block, Broiler.VM's expectation (the
/// owner's decision of 2026-11-02 on row O3). <c>LibraryRunTests</c> lists which expectation each vector meets.
/// </para>
/// <para>
/// <b>The collator charges nothing here</b>, as Broiler.JS passes no meter (decision E7), and its string-length ceiling is
/// the largest a .NET string can hold.
/// </para>
/// </remarks>
internal sealed partial class AlgorithmsLibrary
{
    /// <summary>The identifier prefix of the collation vectors.</summary>
    internal const string IntlCollationPrefix = "intl/collation/";

    /// <summary>The Collator's available locales: the locales the tables' collation data covers (JSD-0084, decision E3).</summary>
    internal static IReadOnlyList<string> CollationAvailable { get; } = ["de", "de-DE", "en", "en-US", "es", "sv", "th"];

    private static readonly Lazy<UcaCollationData> CollationData = new(static () => new UcaCollationData(IntlDataSet.Value));

    private static readonly Lazy<UnicodeNormalization> CollationNormalization = new(static () => new UnicodeNormalization(UnicodeDataHome.Tables));

    /// <summary>Whether a vector is a collation vector, which this part answers whole, script included.</summary>
    internal static bool IsLibraryIntlCollation(VectorRecord vector) =>
        vector.Id.StartsWith(IntlCollationPrefix, StringComparison.Ordinal);

    /// <summary>A collation vector's answer, from its script.</summary>
    private static string IntlCollationArea(VectorRecord vector)
    {
        var script = new CollationScript(vector.Script);
        var operation = vector["op"];

        switch (operation)
        {
            case "Intl.Collator.prototype.compare":
            {
                script.Expect("[\"<\",\"=\",\">\"][Math.sign(");
                var made = script.NewCollator();
                script.Expect(".compare(");
                var x = script.String();
                script.Expect(",");
                var y = script.String();
                script.Expect("))+1]");
                script.End();
                return made.Collator is { } collator ? Relation(Compare(collator, x, y)) : made.Refusal!;
            }

            case "String.prototype.localeCompare":
            {
                script.Expect("[\"<\",\"=\",\">\"][Math.sign(");
                var x = script.String();
                script.Expect(".localeCompare(");
                var y = script.String();
                string? locale = null;
                Dictionary<string, object> options = new(StringComparer.Ordinal);

                if (script.Next(","))
                {
                    locale = script.String();

                    if (script.Next(","))
                    {
                        options = script.Options();
                    }
                }

                script.Expect("))+1]");
                script.End();
                var made = NewCollator(locale, options);
                return made.Collator is { } collator ? Relation(Compare(collator, x, y)) : made.Refusal!;
            }

            case "Array.prototype.sort with Intl.Collator.prototype.compare":
            {
                script.Expect("(function(c,w){w.sort(c.compare);var s=w[0];for(var i=1;i<w.length;i++)s+=\" \"+[\"<\",\"=\",\">\"][Math.sign(c.compare(w[i-1],w[i]))+1]+\" \"+w[i];return s})(");
                var made = script.NewCollator();
                script.Expect(",[");
                var words = new List<string> { script.String() };

                while (script.Next(","))
                {
                    words.Add(script.String());
                }

                script.Expect("])");
                script.End();

                if (made.Collator is not { } collator)
                {
                    return made.Refusal!;
                }

                // ARRAY.PROTOTYPE.SORT IS STABLE (ECMA-262 sec-sortindexedproperties): LINQ's OrderBy is too.
                var sorted = words.OrderBy(static word => word, Comparer<string>.Create((a, b) => Compare(collator, a, b))).ToList();
                var text = new StringBuilder(sorted[0]);

                for (var i = 1; i < sorted.Count; i++)
                {
                    text.Append(' ').Append(RelationSign(Compare(collator, sorted[i - 1], sorted[i]))).Append(' ').Append(sorted[i]);
                }

                return Text(text.ToString());
            }

            case "Intl.Collator.prototype.resolvedOptions().ignorePunctuation":
            {
                var made = script.NewCollator();
                script.Expect(".resolvedOptions().ignorePunctuation");
                script.End();
                return made.Collator is { } collator ? Boolean(collator.Options.Shifted) : made.Refusal!;
            }
        }

        throw new NotSupportedException("no collation vector's operation is " + operation);
    }

    private static int Compare(UcaCollator collator, string x, string y) =>
        collator.Compare(x, y, int.MaxValue, null, out var order) == IntlStatus.Completed
            ? order
            : throw new InvalidOperationException("no collation vector's string passes the ceiling");

    private static string Relation(int order) => Text(RelationSign(order));

    private static string RelationSign(int order) => order < 0 ? "<" : order > 0 ? ">" : "=";

    /// <summary>
    /// <c>new Intl.Collator(locale, options)</c> as an engine's adapter makes it (<c>sec-intl.collator</c>): the reads in the
    /// specification's order, ResolveLocale over the covered locales, then the tailoring and the resolved options; or the
    /// constructor's <c>RangeError</c>.
    /// </summary>
    private static (UcaCollator? Collator, string? Refusal) NewCollator(string? locale, IReadOnlyDictionary<string, object> options)
    {
        string? Read(string name) => options.TryGetValue(name, out var value) ? value as string : null;
        bool? Flag(string name) => options.TryGetValue(name, out var value) ? value as bool? : null;

        if (LocaleNegotiation.CanonicalizeLocaleList(locale is null ? [] : [locale], IntlData, out var requested, out _) != IntlStatus.Completed)
            return (null, RangeError);

        var usage = Read("usage") ?? "sort";
        var collation = Read("collation");

        if (collation is not null && !LocaleId.IsUnicodeType(collation))
            return (null, RangeError);

        var numeric = Flag("numeric");
        var caseFirst = Read("caseFirst");

        var data = CollationData.Value;
        var match = LocaleNegotiation.MatchLocale(CollationAvailable, requested, IntlDefaultLocale);
        var language = LocaleId.TryParse(match.Locale, out var matched) == IntlStatus.Completed ? matched!.Language : "en";
        var resolved = LocaleNegotiation.ResolveLocale(match,
        [
            new RelevantKey("co", data.KeyValues(language, "co", usage)) { Option = collation },
            new RelevantKey("kf", data.KeyValues(language, "kf", usage)) { Option = caseFirst },
            new RelevantKey("kn", data.KeyValues(language, "kn", usage)) { Option = numeric is null ? null : numeric.Value ? "true" : "false" },
        ], IntlData);

        var dataLanguage = LocaleId.TryParse(resolved.DataLocale, out var dataLocale) == IntlStatus.Completed ? dataLocale!.Language : "en";
        var tailoring = data.TailoringFor(dataLanguage, usage, resolved.KeyValues["co"]);
        var sensitivity = Read("sensitivity") ?? "variant";
        var ignorePunctuation = Flag("ignorePunctuation") ?? data.ShiftedByDefault(tailoring);
        var record = UcaCollatorOptions.FromResolved(
            sensitivity, ignorePunctuation, resolved.KeyValues["kf"] ?? "false", resolved.KeyValues["kn"] == "true", tailoring);
        return (new UcaCollator(data, record, CollationNormalization.Value), null);
    }

    /// <summary>A reader of the few forms the collation vectors' scripts take: String literals, an options object, punctuation.</summary>
    private sealed class CollationScript(string script)
    {
        private int at;

        internal void Expect(string text)
        {
            if (string.CompareOrdinal(script, at, text, 0, text.Length) != 0)
                throw new NotSupportedException("no collation vector's script reads `" + script[at..] + "` where `" + text + "` was expected: " + script);
            at += text.Length;
        }

        internal bool Next(string text)
        {
            if (string.CompareOrdinal(script, at, text, 0, text.Length) != 0)
                return false;
            at += text.Length;
            return true;
        }

        internal void End()
        {
            if (at != script.Length)
                throw new NotSupportedException("a collation vector's script goes on after its form: " + script);
        }

        /// <summary><c>new Intl.Collator("locale")</c> or <c>new Intl.Collator("locale",{options})</c>, made.</summary>
        internal (UcaCollator? Collator, string? Refusal) NewCollator()
        {
            Expect("new Intl.Collator(");
            var locale = String();
            var options = Next(",") ? Options() : new Dictionary<string, object>(StringComparer.Ordinal);
            Expect(")");
            return AlgorithmsLibrary.NewCollator(locale, options);
        }

        /// <summary>An object literal of identifiers and String or Boolean values.</summary>
        internal Dictionary<string, object> Options()
        {
            var options = new Dictionary<string, object>(StringComparer.Ordinal);
            Expect("{");

            while (!Next("}"))
            {
                if (options.Count > 0)
                    Expect(",");

                var start = at;
                while (at < script.Length && script[at] is >= 'a' and <= 'z' or >= 'A' and <= 'Z')
                    at++;
                var name = script[start..at];
                Expect(":");
                options[name] = Next("true") ? true : Next("false") ? false : String();
            }

            return options;
        }

        /// <summary>A double-quoted String literal, its escapes resolved.</summary>
        internal string String()
        {
            Expect("\"");
            var text = new StringBuilder();

            while (script[at] != '"')
            {
                if (script[at] != '\\')
                {
                    text.Append(script[at++]);
                    continue;
                }

                var escape = script[at + 1];
                at += 2;

                switch (escape)
                {
                    case 'u':
                        text.Append((char)int.Parse(script.AsSpan(at, 4), System.Globalization.NumberStyles.AllowHexSpecifier, System.Globalization.NumberFormatInfo.InvariantInfo));
                        at += 4;
                        break;
                    case 'n': text.Append('\n'); break;
                    case 't': text.Append('\t'); break;
                    case 'r': text.Append('\r'); break;
                    case '0': text.Append('\0'); break;
                    default: text.Append(escape); break;
                }
            }

            at++;
            return text.ToString();
        }
    }
}
