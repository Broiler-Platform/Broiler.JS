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
/// directly as well. What the probe proves is that the library, the reader, the validator, the
/// runner and the report compile into a Native AOT image under Broiler.VM's root settings and give
/// there, over the whole corpus, the answers they give under the JIT. Until SJL-3 it ran the empty
/// library, whose every answer was <c>status: not-implemented</c>.
/// </remarks>
internal static class Program
{
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
        // engine's conversions, options, validation, raw-JSON brand or string ceiling.
        var library = new AlgorithmsLibrary();
        var results = LibraryRun.Run(corpus.Records, library);
        foreach (var result in results)
        {
            var implemented = result.Vector["area"] switch
            {
                "numbers" => true,
                "numerics" => !IsEngineLevel(result.Vector),
                "text" => !AlgorithmsLibrary.IsEngineLevelText(result.Vector["op"] ?? string.Empty),
                _ => false,
            };
            if (implemented ? result.Verdict != Verdict.Match : result.Verdict != Verdict.NotImplemented)
                failures.Add(result.Vector.Id + ": " + library.Name + " gave " + VectorRun.Name(result.Verdict));
        }

        // A second run, over the same corpus, must give the same report (rule SL9's question, asked again under Native AOT).
        var report = VectorRun.Report(results, library.Name, corpus.Sources);
        if (report != VectorRun.Report(LibraryRun.Run(corpus.Records, library), library.Name, corpus.Sources))
            failures.Add("the report of a second run differs from the first");

        failures.AddRange(ContractTypes.Check());

        if (reportPath is not null)
            File.WriteAllText(reportPath, report);

        Console.Out.Write("Broiler.ECMAScript.Probe: corpus revision " + Decimal(corpus.Sources.Revision) + ", "
            + Decimal(corpus.Files.Count) + " files, " + Decimal(results.Count) + " vectors, "
            + Decimal(failures.Count) + " failures\n");
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
