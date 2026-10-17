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
