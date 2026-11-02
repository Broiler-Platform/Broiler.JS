using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Broiler.ECMAScript.Conformance;
using Broiler.ECMAScript.Runners;

namespace Broiler.ECMAScript.Probe;

/// <summary>
/// The probe's entry point: <c>Broiler.ECMAScript.Probe [corpus-root] [--report path]</c>. It reads
/// and validates the corpus, runs every vector through each library the probe carries, checks
/// every verdict against what that library is expected to give, and exits 0 only when all hold.
/// </summary>
/// <remarks>
/// From milestone SJL-3 the library it runs is <c>Broiler.ECMAScript.Algorithms</c>, through the
/// runners' <c>AlgorithmsLibrary</c>: every number vector must match, and a vector of any area the
/// library does not implement yet must be "not implemented". The contract types are exercised
/// directly as well. From milestone SJL-9.3a it also runs <c>Broiler.ECMAScript.Intl</c>, through the
/// same runner, on the Intl area's sub-area (a), and from stage SJL-9.3b on the operations of sub-area (b) the runner
/// answers, whose vectors it reads whole (<c>IVectorRecordLibrary</c>), and from stage SJL-9.3c on those of sub-area (c), read
/// whole the same way, and from stage SJL-9.3d on those of sub-area (d), read whole too. From milestone SJL-13.2a it also runs
/// <c>Broiler.ECMAScript.Frontend</c>, through the runner's frontend adapter, on the <c>syntax</c> area, whose outcomes it
/// recorded and did not judge until stage SJL-13.2d, and judges from there (<see cref="IsParseJudgedArea"/>): from stage SJL-13.2c the parser answers
/// every vector, a refusal counted among the matches where the vector expects one and an acceptance among them where it
/// expects any other outcome, and a vector only a static-semantic early error refuses, accepted until stage 4d, counted
/// among those that differ. <i>(Revised 2026-11-01, at stage SJL-13.2c; it read "records and does not judge until the
/// library parses (<see cref="IsRecordedArea"/>): from stage SJL-13.2b a vector whose source the tokenizer refuses is
/// refused, and is counted among the matches, and every other is not implemented; a refusal of a vector that expects an
/// acceptance would be counted among those that differ.")</i> <i>(Revised 2026-11-02, at stage SJL-13.2d; it read "records and
/// does not judge until stage SJL-13.2d (<see cref="IsRecordedArea"/>)".)</i> What the probe proves is that the library, the reader, the validator, the
/// runner and the report compile into a Native AOT image under Broiler.VM's root settings and give
/// there, over the whole corpus, the answers they give under the JIT. Until SJL-3 it ran the empty
/// library, whose every answer was <c>status: not-implemented</c>.
/// </remarks>
internal static class Program
{
    /// <summary>
    /// The areas the probe judges at the parse (milestone SJL-13.2a, judged from stage SJL-13.2d; the plan's check for stage 4d,
    /// "Every <c>syntax</c> row's vectors pass"): each vector must agree at the parse
    /// (<see cref="AlgorithmsLibrary.MatchesAtTheParse"/>), refused where it expects <c>error: SyntaxError</c> and accepted
    /// where it expects any other outcome, since a parse knows that a source is accepted and not what it completes with.
    /// </summary>
    /// <remarks>
    /// Every such vector is run through the same runner as every other, its line written to the report like any other's, so
    /// that the JIT and Native AOT reports are compared over it byte for byte; its verdicts are counted on a line of their
    /// own. A vector that does not agree at the parse is a failure, and an area with no vector is a failure.
    /// <i>(Revised 2026-11-02, at stage SJL-13.2d, which reports the static-semantic early errors, so that the area is judged;
    /// it was <c>IsRecordedArea</c>, "The areas whose vectors the probe runs and records but does not judge (milestone
    /// SJL-13.2a; the plan's stage 4a: "the probe runs the syntax vectors (all expected to fail until 4c, recorded, not
    /// judged)")", whose verdicts added no failure, and whose remarks said "The syntax area leaves this set at stage 4d, and
    /// is judged from there by <see cref="AlgorithmsLibrary.MatchesAtTheParse"/>".)</i>
    /// </remarks>
    internal static bool IsParseJudgedArea(string? area) => area is ParseJudgedAreas;

    /// <summary>The areas judged at the parse, as the probe's summary line names them.</summary>
    internal const string ParseJudgedAreas = "syntax";

    internal static int Main(string[] args)
    {
        string? root = null;
        string? reportPath = null;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--report" && i + 1 < args.Length)
                reportPath = args[++i];
            else if (root is null)
                root = args[i];
            else
                return Usage();
        }

        root ??= CorpusDirectory.Home(AppContext.BaseDirectory);
        var failures = new List<string>();

        var corpus = CorpusDirectory.Load(root);
        foreach (var diagnostic in corpus.Diagnostics)
            failures.Add("corpus: " + diagnostic);
        if (corpus.Records.Count == 0)
            failures.Add("corpus: no vector was read from " + root);

        // Every area the algorithms library implements must pass; every other area is not implemented. In
        // the numerics area the operations the library does not share and the width-policy vectors are
        // engine-level, and not implemented here either; in the text area, the vectors whose outcome is an
        // engine's conversions, options, validation, raw-JSON brand or string ceiling; in the Date area, the
        // setters' conversions and their order, and the sequences of local setter calls. Every time-zone vector is
        // the library's, over the data home's generated tables, which the runner reads from disk (milestone SJL-8.4).
        // Every Temporal vector is the library's through the runner's Temporal adapter, over the same tables, but a
        // built-in's length, the properties an engine reads, and the calendar an object lends (milestone SJL-10.3). Of the
        // Intl area, sub-area (a)'s vectors are the Intl library's, over the Intl data home's tables and Broiler.VM's
        // providers, but the engine-level ones AlgorithmsLibrary.IsEngineLevelIntl names; an impl-defined one may give
        // Broiler.VM's per-engine expectation, the engine whose providers the runner takes (milestone SJL-9.3a). So are
        // sub-area (b)'s of the operations AlgorithmsLibrary.IntlValuesOperations names, read whole with their scripts
        // (stage SJL-9.3b). So are sub-area (c)'s, numbers-and-text's operations AlgorithmsLibrary.IntlNumbersOperations names and
        // values-and-plurals' format and formatToParts, but the engine-level script forms (stage SJL-9.3c). So are sub-area
        // (d)'s, date-times' operations AlgorithmsLibrary.IntlDateTimesOperations names, but the engine-level script forms
        // AlgorithmsLibrary.IsEngineLevelDateTimesScript names (stage SJL-9.3d). Every other Intl vector is not implemented.
        // The syntax area's vectors are the frontend's, through the runner's frontend adapter, judged at the parse from stage
        // 4d, which reports the static-semantic early errors (IsParseJudgedArea; recorded, not judged, from milestone SJL-13.2a
        // to stage 4c).
        var library = new AlgorithmsLibrary();
        var results = LibraryRun.Run(corpus.Records, library);
        var recorded = new int[5];
        foreach (var result in results)
        {
            if (IsParseJudgedArea(result.Vector["area"]))
            {
                recorded[(int)result.Verdict]++;
                if (AlgorithmsLibrary.MatchesAtTheParse(result))
                    recorded[4]++;
                else
                    failures.Add(result.Vector.Id + ": " + library.Name + " gave " + result.Rendered + ", which does not agree at the parse");
                continue;
            }

            var implemented = result.Vector["area"] switch
            {
                "numbers" => true,
                "numerics" => !IsEngineLevel(result.Vector),
                "text" => !AlgorithmsLibrary.IsEngineLevelText(result.Vector["op"] ?? string.Empty),
                "date" => !AlgorithmsLibrary.IsEngineLevelDate(result.Vector["op"] ?? string.Empty),
                "time-zones" => true,
                "temporal" => !AlgorithmsLibrary.IsEngineLevelTemporal(result.Vector["op"] ?? string.Empty),
                "intl" => AlgorithmsLibrary.IsLibraryIntl(result.Vector),
                _ => false,
            };
            var passes = result.Verdict == Verdict.Match || AlgorithmsLibrary.MatchesBroilerVmExpectation(result);
            if (implemented ? !passes : result.Verdict != Verdict.NotImplemented)
                failures.Add(result.Vector.Id + ": " + library.Name + " gave " + VectorRun.Name(result.Verdict));
        }

        // A second run, over the same corpus, must give the same report (rule SL9's question, asked again under Native AOT).
        var report = VectorRun.Report(results, library.Name, corpus.Sources);
        if (report != VectorRun.Report(LibraryRun.Run(corpus.Records, library), library.Name, corpus.Sources))
            failures.Add("the report of a second run differs from the first");

        failures.AddRange(ContractTypes.Check());
        var recordedCount = recorded[0] + recorded[1] + recorded[2] + recorded[3];
        if (recordedCount == 0)
            failures.Add("corpus: no vector of the areas judged at the parse (" + ParseJudgedAreas + ") was read");

        if (reportPath is not null)
            File.WriteAllText(reportPath, report);

        Console.Out.Write("Broiler.ECMAScript.Probe: corpus revision " + Decimal(corpus.Sources.Revision) + ", "
            + Decimal(corpus.Files.Count) + " files, " + Decimal(results.Count) + " vectors, "
            + Decimal(failures.Count) + " failures\n");
        Console.Out.Write("Broiler.ECMAScript.Probe: judged at the parse (" + ParseJudgedAreas + "): "
            + Decimal(recordedCount) + " vectors, " + Decimal(recorded[(int)Verdict.Match]) + " match, "
            + Decimal(recorded[(int)Verdict.Difference] + recorded[(int)Verdict.ClassifiedDivergence]) + " differ, "
            + Decimal(recorded[(int)Verdict.NotImplemented]) + " not implemented, " + Decimal(recorded[4]) + " agree at the parse\n");
        foreach (var failure in failures)
            Console.Error.Write(failure + "\n");
        return failures.Count == 0 ? 0 : 1;
    }

    internal static string Decimal(int value) => value.ToString(NumberFormatInfo.InvariantInfo);

    /// <summary>
    /// A numerics vector the library does not answer: an operation it does not share, or a
    /// width-policy vector, which pins each engine's policy.
    /// </summary>
    private static bool IsEngineLevel(VectorRecord vector) =>
        vector["op"] is "Math.fround" or "Math.clz32" or "Math.imul" or "ToPropertyKey"
        || (vector["op"] ?? string.Empty).EndsWith(" (width policy)", StringComparison.Ordinal);

    private static int Usage()
    {
        Console.Error.Write("usage: Broiler.ECMAScript.Probe [corpus-root] [--report path]\n");
        return 2;
    }
}
