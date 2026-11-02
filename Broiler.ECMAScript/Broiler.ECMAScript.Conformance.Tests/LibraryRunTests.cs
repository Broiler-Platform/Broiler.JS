using System.Linq;
using Broiler.ECMAScript.Runners;
using Xunit;

namespace Broiler.ECMAScript.Conformance.Tests;

/// <summary>
/// The library-level runner: against an empty library every vector is "not implemented", and
/// against <c>Broiler.ECMAScript.Algorithms</c> (from milestone SJL-3) every number vector passes.
/// </summary>
public class LibraryRunTests
{
    [Fact]
    public void Every_number_vector_passes_against_the_algorithms_library()
    {
        var vectors = Home.Corpus.Records.Where(static v => v["area"] == "numbers").ToList();
        var results = LibraryRun.Run(vectors, new AlgorithmsLibrary());

        Assert.NotEmpty(vectors);
        var differences = results.Where(static r => r.Verdict != Verdict.Match)
            .Select(static r => r.Vector.Id + " " + r.Vector["op"] + " in: " + r.Vector["in"] + " expected: " + r.Expected + " got: " + r.Rendered)
            .ToList();
        Assert.True(differences.Count == 0, differences.Count + " differences:\n" + string.Join("\n", differences.Take(60)));
    }

    /// <summary>
    /// Every numerics vector passes against the library (milestone SJL-4.3), but those the library
    /// does not answer: the operations it does not share (<c>fround</c>, <c>clz32</c>, <c>imul</c>,
    /// property keys), whose vectors are engine-level, and the width-policy vectors, which pin each
    /// engine's policy and give their operands as descriptions.
    /// </summary>
    [Fact]
    public void Every_numerics_vector_passes_against_the_algorithms_library()
    {
        var vectors = Home.Corpus.Records.Where(static v => v["area"] == "numerics").ToList();
        var results = LibraryRun.Run(vectors, new AlgorithmsLibrary());

        Assert.NotEmpty(vectors);
        var differences = results.Where(static r => r.Verdict == Verdict.Difference)
            .Select(static r => r.Vector.Id + " " + r.Vector["op"] + " in: " + r.Vector["in"] + " expected: " + r.Expected + " got: " + r.Rendered)
            .ToList();
        Assert.True(differences.Count == 0, differences.Count + " differences:\n" + string.Join("\n", differences.Take(60)));

        var engineLevel = results.Where(static r => r.Verdict == Verdict.NotImplemented).Select(static r => r.Vector).ToList();
        Assert.All(engineLevel, static v => Assert.True(
            v["op"] is "Math.fround" or "Math.clz32" or "Math.imul" or "ToPropertyKey" || v["op"]!.EndsWith(" (width policy)", System.StringComparison.Ordinal),
            v.Id + " is not answered by the library"));
    }

    /// <summary>
    /// Every text vector passes against the library (milestone SJL-5.3), but the engine-level ones: those
    /// whose outcome is an engine's conversion order, option reading, typed-array validation or raw-JSON
    /// brand, and those that pin an engine's string ceiling.
    /// </summary>
    [Fact]
    public void Every_text_vector_passes_against_the_algorithms_library()
    {
        var vectors = Home.Corpus.Records.Where(static v => v["area"] == "text").ToList();
        var results = LibraryRun.Run(vectors, new AlgorithmsLibrary());

        Assert.NotEmpty(vectors);
        var differences = results.Where(static r => r.Verdict == Verdict.Difference)
            .Select(static r => r.Vector.Id + " " + r.Vector["op"] + " in: " + r.Vector["in"] + " expected: " + r.Expected + " got: " + r.Rendered)
            .ToList();
        Assert.True(differences.Count == 0, differences.Count + " differences:\n" + string.Join("\n", differences.Take(60)));

        var engineLevel = results.Where(static r => r.Verdict == Verdict.NotImplemented).Select(static r => r.Vector).ToList();
        Assert.All(engineLevel, static v => Assert.True(AlgorithmsLibrary.IsEngineLevelText(v["op"]!), v.Id + " is not answered by the library"));
    }

    /// <summary>
    /// Every Date vector passes against the library (milestone SJL-6.3), but the engine-level ones: those
    /// whose outcome is the setters' argument conversion and its order, and those whose input is a
    /// sequence of local setter calls.
    /// </summary>
    [Fact]
    public void Every_date_vector_passes_against_the_algorithms_library()
    {
        var vectors = Home.Corpus.Records.Where(static v => v["area"] == "date").ToList();
        var results = LibraryRun.Run(vectors, new AlgorithmsLibrary());

        Assert.NotEmpty(vectors);
        var differences = results.Where(static r => r.Verdict == Verdict.Difference)
            .Select(static r => r.Vector.Id + " " + r.Vector["op"] + " in: " + r.Vector["in"] + " expected: " + r.Expected + " got: " + r.Rendered)
            .ToList();
        Assert.True(differences.Count == 0, differences.Count + " differences:\n" + string.Join("\n", differences.Take(60)));

        var engineLevel = results.Where(static r => r.Verdict == Verdict.NotImplemented).Select(static r => r.Vector).ToList();
        Assert.All(engineLevel, static v => Assert.True(AlgorithmsLibrary.IsEngineLevelDate(v["op"]!), v.Id + " is not answered by the library"));
    }

    /// <summary>
    /// Every time-zone vector passes against the library (milestone SJL-8.4), over the data home's generated tables,
    /// and the library answers every one: the area has no engine-level vector. The library is Broiler.VM's behaviour
    /// (the differential check of milestone SJL-8.3), and the vectors' expected values are the independent reference
    /// computation's, <c>scripts/conformance/tzref.py</c>, so a difference here is a defect of one or the other.
    /// </summary>
    [Fact]
    public void Every_time_zone_vector_passes_against_the_algorithms_library()
    {
        var vectors = Home.Corpus.Records.Where(static v => v["area"] == "time-zones").ToList();
        var results = LibraryRun.Run(vectors, new AlgorithmsLibrary());

        Assert.NotEmpty(vectors);
        var differences = results.Where(static r => r.Verdict != Verdict.Match)
            .Select(static r => r.Vector.Id + " " + r.Vector["op"] + " in: " + r.Vector["in"] + " expected: " + r.Expected + " got: " + r.Rendered)
            .ToList();
        Assert.True(differences.Count == 0, differences.Count + " differences:\n" + string.Join("\n", differences.Take(60)));
    }

    /// <summary>
    /// Every Temporal vector passes against the library (milestone SJL-10.3), through the runner's Temporal adapter over
    /// the data home's generated tables, but the engine-level ones: a built-in's <c>length</c>, the properties an engine
    /// reads, and the calendar an object lends from its slot (row C13). The expected values are the reference
    /// polyfill's, with the pinned text's where decision D7 rules, so a difference here is a defect of the library, the
    /// adapter or the reference.
    /// </summary>
    [Fact]
    public void Every_Temporal_vector_passes_against_the_algorithms_library()
    {
        var vectors = Home.Corpus.Records.Where(static v => v["area"] == "temporal").ToList();
        var results = LibraryRun.Run(vectors, new AlgorithmsLibrary());

        Assert.NotEmpty(vectors);
        var differences = results.Where(static r => r.Verdict == Verdict.Difference)
            .Select(static r => r.Vector.Id + " " + r.Vector["op"] + " in: " + r.Vector["in"] + " expected: " + r.Expected + " got: " + r.Rendered)
            .ToList();
        Assert.True(differences.Count == 0, differences.Count + " differences:\n" + string.Join("\n", differences.Take(60)));

        var engineLevel = results.Where(static r => r.Verdict == Verdict.NotImplemented).Select(static r => r.Vector).ToList();
        Assert.All(engineLevel, static v => Assert.True(AlgorithmsLibrary.IsEngineLevelTemporal(v["op"]!), v.Id + " is not answered by the library"));
        Assert.Equal(27, engineLevel.Count);
        Assert.Equal(10878 - 27, results.Count(static r => r.Verdict == Verdict.Match));
    }

    /// <summary>
    /// Every vector of the Intl area's sub-area (a), <c>intl/locales.vectors</c>, passes against the Intl library (milestone
    /// SJL-9.3a), over the Intl data home's tables and Broiler.VM's providers, or gives Broiler.VM's <c>per-engine</c>
    /// expectation, the engine whose providers the runner takes; but the engine-level ones, whose outcome is an engine's or
    /// another sub-area's. Every other Intl vector is not implemented yet, but sub-area (b)'s (from stage SJL-9.3b, the next
    /// test) and sub-area (c)'s (from stage SJL-9.3c), and no other area's verdict moves: the Unicode
    /// area's <c>String.prototype.toLocaleLowerCase</c>, the one operation name the two areas share, stays unanswered.
    /// </summary>
    [Fact]
    public void Every_locale_vector_passes_against_the_Intl_library_or_gives_Broiler_VM_s_expectation()
    {
        var vectors = Home.Corpus.Records.Where(static v => v["area"] == "intl").ToList();
        var results = LibraryRun.Run(vectors, new AlgorithmsLibrary());
        var locales = results.Where(static r => r.Vector.Id.StartsWith(AlgorithmsLibrary.IntlLocalesPrefix, System.StringComparison.Ordinal)).ToList();

        Assert.NotEmpty(locales);
        var differences = locales.Where(static r => r.Verdict == Verdict.Difference && !AlgorithmsLibrary.MatchesBroilerVmExpectation(r))
            .Select(static r => r.Vector.Id + " " + r.Vector["op"] + " in: " + r.Vector["in"] + " expected: " + r.Expected + " got: " + r.Rendered)
            .ToList();
        Assert.True(differences.Count == 0, differences.Count + " differences:\n" + string.Join("\n", differences.Take(60)));

        var engineLevel = locales.Where(static r => r.Verdict == Verdict.NotImplemented).Select(static r => r.Vector).ToList();
        Assert.All(engineLevel, static v => Assert.False(AlgorithmsLibrary.IsLibraryIntl(v), v.Id + " is not answered by the library"));
        Assert.Equal(
            ["intl/locales/negotiation/0021", "intl/locales/providers/0012", "intl/locales/providers/0013", "intl/locales/providers/0014"],
            engineLevel.Select(static v => v.Id).Order(System.StringComparer.Ordinal));

        Assert.All(results.Except(locales).Where(static r => !AlgorithmsLibrary.IsLibraryIntlValues(r.Vector) && !AlgorithmsLibrary.IsLibraryIntlNumbers(r.Vector)),
            static r => Assert.Equal(Verdict.NotImplemented, r.Verdict));

        // Intl.NumberFormat.supportedLocalesOf: providers/0007 is the library's FilterLocales, held to Broiler.VM's per-engine
        // expectation; providers/0014, whose inputs lack the default locale its script prepends, is not answered.
        var filtered = Assert.Single(locales, static r => r.Vector.Id == "intl/locales/providers/0007");
        Assert.True(AlgorithmsLibrary.MatchesBroilerVmExpectation(filtered), filtered.Rendered);
        Assert.Equal("value: str:\"de-CH,en-US-u-ca-gregory,de-DE-x-foo,en-GB,de-1996\"", filtered.Rendered);

        var unicode = LibraryRun.Run(Home.Corpus.Records.Where(static v => v["op"] == "String.prototype.toLocaleLowerCase").ToList(), new AlgorithmsLibrary());
        Assert.Contains(unicode, static r => r.Vector["area"] == "unicode");
        Assert.All(unicode, static r => Assert.Equal(Verdict.NotImplemented, r.Verdict));
    }

    /// <summary>
    /// Every vector of the Intl area's sub-area (b), <c>intl/values-and-plurals.vectors</c>, of an operation the runner answers
    /// passes against the Intl library (stage SJL-9.3b), or gives Broiler.VM's <c>per-engine</c> expectation; every other vector
    /// of the file is not implemented: the formatting ones, sub-area (c)'s, and the engine's reads and coercions. <i>(Revised
    /// 2026-11-01, at stage SJL-9.3c: the formatting ones are sub-area (c)'s part's since that stage, which its own test holds,
    /// and of them the one whose outcome is an object's <c>valueOf</c> count stays not implemented. The list of operations not
    /// answered read "Intl.NumberFormat (option coercions)", "Intl.NumberFormat (option reads)", "Intl.NumberFormat
    /// (resolvedOptions)", "Intl.NumberFormat.prototype.format", "Intl.NumberFormat.prototype.formatToParts" and
    /// "Intl.PluralRules (option reads)", over every vector the part does not answer.)</i>
    /// </summary>
    [Fact]
    public void Every_values_and_plurals_vector_the_runner_answers_passes_against_the_Intl_library()
    {
        var vectors = Home.Corpus.Records.Where(static v => v.Id.StartsWith(AlgorithmsLibrary.IntlValuesPrefix, System.StringComparison.Ordinal)).ToList();
        var results = LibraryRun.Run(vectors, new AlgorithmsLibrary());

        Assert.NotEmpty(vectors);
        var answered = results.Where(static r => AlgorithmsLibrary.IsLibraryIntlValues(r.Vector)).ToList();
        var differences = answered.Where(static r => r.Verdict != Verdict.Match && !AlgorithmsLibrary.MatchesBroilerVmExpectation(r))
            .Select(static r => r.Vector.Id + " " + r.Vector.Script + " expected: " + r.Expected + " got: " + r.Rendered)
            .ToList();
        Assert.True(differences.Count == 0, differences.Count + " differences:\n" + string.Join("\n", differences.Take(60)));
        Assert.Equal(results.Count - 97, answered.Count);

        var notAnswered = results.Except(answered).Where(static r => !AlgorithmsLibrary.IsLibraryIntlNumbers(r.Vector)).ToList();
        Assert.All(notAnswered, static r => Assert.Equal(Verdict.NotImplemented, r.Verdict));
        Assert.Equal(
            ["Intl.NumberFormat (option coercions)", "Intl.NumberFormat (option reads)", "Intl.NumberFormat (resolvedOptions)",
             "Intl.NumberFormat.prototype.format", "Intl.PluralRules (option reads)"],
            notAnswered.Select(static r => r.Vector["op"]!).Distinct().Order(System.StringComparer.Ordinal));
        Assert.Equal(5, notAnswered.Count);
    }

    /// <summary>
    /// The operation names sub-area (b)'s part of the runner answers keep every other file's outcome: each vector of the corpus
    /// outside <c>intl/values-and-plurals.vectors</c> with one of those names gives what the runner's operation route gives it,
    /// as before the part was added (<c>IVectorLibrary.Call</c> sees no area). <i>(Revised 2026-11-01, at stage SJL-9.3c: one of
    /// those names, <c>Intl.NumberFormat.prototype.resolvedOptions</c>, is also sub-area (c)'s in <c>intl/numbers-and-text.vectors</c>,
    /// whose vectors that stage's part answers; so a vector of that file is held only not to be sub-area (b)'s, and every other
    /// file's as before: the corpus that is loaded holds no other.)</i>
    /// </summary>
    [Fact]
    public void Sub_area_b_s_operation_names_move_no_other_area_s_vector()
    {
        var library = new AlgorithmsLibrary();
        var others = Home.Corpus.Records
            .Where(static v => AlgorithmsLibrary.IntlValuesOperations.Contains(v["op"]!) && !v.Id.StartsWith(AlgorithmsLibrary.IntlValuesPrefix, System.StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(others);
        Assert.All(others, static v => Assert.False(AlgorithmsLibrary.IsLibraryIntlValues(v), v.Id));
        var outside = others.Where(static v => !v.Id.StartsWith(AlgorithmsLibrary.IntlNumbersPrefix, System.StringComparison.Ordinal)).ToList();
        Assert.All(outside, v => Assert.Equal(((IVectorLibrary)library).Call(v["op"]!, LibraryRun.Inputs(v)), library.Call(v)));
        Assert.All(LibraryRun.Run(outside, library), static r => Assert.Equal(Verdict.NotImplemented, r.Verdict));
        Assert.All(outside, static v => Assert.False(AlgorithmsLibrary.IsLibraryIntl(v), v.Id));
    }

    /// <summary>
    /// Every vector of the Intl area's sub-area (c) the runner answers (stage SJL-9.3c) - <c>intl/numbers-and-text.vectors</c>'
    /// operations but the engine-level script forms, and the <c>format</c> and <c>formatToParts</c> vectors of
    /// <c>intl/values-and-plurals.vectors</c> - passes against the Intl library, or gives Broiler.VM's <c>per-engine</c>
    /// expectation; every other vector of <c>numbers-and-text.vectors</c> is not implemented: the bound <c>format</c>, the
    /// legacy constructor, the reads after construction, a constructor called without <c>new</c> and the lists
    /// StringListFromIterable refuses, each an engine's.
    /// </summary>
    [Fact]
    public void Every_numbers_and_text_vector_the_runner_answers_passes_against_the_Intl_library()
    {
        var vectors = Home.Corpus.Records.Where(static v => v.Id.StartsWith(AlgorithmsLibrary.IntlNumbersPrefix, System.StringComparison.Ordinal)
            || (v.Id.StartsWith(AlgorithmsLibrary.IntlValuesPrefix, System.StringComparison.Ordinal) && AlgorithmsLibrary.IntlValuesFormatOperations.Contains(v["op"]!))).ToList();
        var results = LibraryRun.Run(vectors, new AlgorithmsLibrary());

        Assert.NotEmpty(vectors);
        var answered = results.Where(static r => AlgorithmsLibrary.IsLibraryIntlNumbers(r.Vector)).ToList();
        var differences = answered.Where(static r => r.Verdict != Verdict.Match && !AlgorithmsLibrary.MatchesBroilerVmExpectation(r))
            .Select(static r => r.Vector.Id + " " + r.Vector.Script + " expected: " + r.Expected + " got: " + r.Rendered)
            .ToList();
        Assert.True(differences.Count == 0, differences.Count + " differences:\n" + string.Join("\n", differences.Take(80)));

        var values = answered.Count(static r => r.Vector.Id.StartsWith(AlgorithmsLibrary.IntlValuesPrefix, System.StringComparison.Ordinal));
        Assert.Equal(92, values);

        var notAnswered = results.Except(answered).ToList();
        Assert.All(notAnswered, static r => Assert.Equal(Verdict.NotImplemented, r.Verdict));
        Assert.All(notAnswered, static r => Assert.True(AlgorithmsLibrary.IsEngineLevelNumbersScript(r.Vector.Script), r.Vector.Id));
        Assert.Equal(
            ["intl/numbers-and-text/options/0001", "intl/numbers-and-text/options/0002", "intl/numbers-and-text/options/0003",
             "intl/numbers-and-text/options/0004", "intl/numbers-and-text/options/0005", "intl/numbers-and-text/options/0006"],
            notAnswered.Where(static r => r.Vector["op"]!.StartsWith("Intl.NumberFormat", System.StringComparison.Ordinal)
                    || r.Vector["op"] == "Number.prototype.toLocaleString (reads)")
                .Where(static r => r.Vector.Id.StartsWith(AlgorithmsLibrary.IntlNumbersPrefix, System.StringComparison.Ordinal))
                .Select(static r => r.Vector.Id).Order(System.StringComparer.Ordinal));
    }

    /// <summary>
    /// The operation names sub-area (c)'s part of the runner answers keep every other file's outcome. At corpus revision 30 no
    /// vector outside the two Intl files it reads carries one of those names, and the test holds that: a vector that comes to
    /// carry one fails it, so that the test is revisited to check that such a vector gives what the runner's operation route
    /// gives it and is not answered as sub-area (c)'s (<c>IVectorLibrary.Call</c> sees no area). Within the two files, a name
    /// answers only its own file's vectors.
    /// </summary>
    [Fact]
    public void Sub_area_c_s_operation_names_move_no_other_area_s_vector()
    {
        var names = AlgorithmsLibrary.IntlNumbersOperations.Concat(AlgorithmsLibrary.IntlValuesFormatOperations).ToHashSet(System.StringComparer.Ordinal);
        var others = Home.Corpus.Records
            .Where(v => names.Contains(v["op"]!) && !v.Id.StartsWith(AlgorithmsLibrary.IntlNumbersPrefix, System.StringComparison.Ordinal)
                && !v.Id.StartsWith(AlgorithmsLibrary.IntlValuesPrefix, System.StringComparison.Ordinal))
            .ToList();

        // NO OTHER FILE CARRIES THESE NAMES TODAY. A vector that comes to carry one fails here; the check it is then to pass is
        // the one sub-area (b)'s test makes: Call(op, inputs) == Call(vector), and IsLibraryIntlNumbers false.
        Assert.Empty(others.Select(static v => v.Id));

        // Within the two files, an operation name of sub-area (c) answers only its own file's vectors.
        var values = Home.Corpus.Records.Where(static v => v.Id.StartsWith(AlgorithmsLibrary.IntlValuesPrefix, System.StringComparison.Ordinal)
            && AlgorithmsLibrary.IsLibraryIntlNumbers(v)).ToList();
        Assert.All(values, static v => Assert.Contains(v["op"]!, AlgorithmsLibrary.IntlValuesFormatOperations));
        Assert.All(values, static v => Assert.False(AlgorithmsLibrary.IsLibraryIntlValues(v), v.Id));
    }

    [Fact]
    public void Against_the_empty_library_every_vector_is_not_implemented()
    {
        var vectors = Home.Corpus.Records.ToList();
        var results = LibraryRun.Run(vectors, new EmptyLibrary());

        Assert.Equal(vectors.Count, results.Count);
        Assert.All(results, r => Assert.Equal(Verdict.NotImplemented, r.Verdict));
    }

    [Fact]
    public void Every_vector_s_inputs_decode()
    {
        Assert.All(Home.Corpus.Records, vector => Assert.NotEmpty(LibraryRun.Inputs(vector)));
    }

    [Fact]
    public void The_report_counts_every_verdict_and_lists_every_vector()
    {
        var results = LibraryRun.Run(Home.Corpus.Records, new EmptyLibrary());
        var lines = VectorRun.Report(results, "empty", Home.Corpus.Sources).Split('\n');

        Assert.Equal("# engine: empty", lines[0]);
        Assert.Equal("# corpus revision: " + Home.Corpus.Sources.Revision, lines[1]);
        Assert.Equal("# not-implemented: " + results.Count, lines[6]);
        Assert.Equal("id\tclass\texpected\trendered\tverdict", lines[8]);
        Assert.Equal(results.Count, lines.Skip(9).Count(line => line.Length > 0));
    }
}
