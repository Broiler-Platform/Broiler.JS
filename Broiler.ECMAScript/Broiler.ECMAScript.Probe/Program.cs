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
/// Until milestone SJL-3 moves the first algorithm, the library it runs is the empty one, whose
/// every answer is <c>status: not-implemented</c>, and the contract types of the algorithms
/// library are exercised directly. What the probe proves at this milestone is that the reader, the
/// validator, the runner, the report and the contract types compile into a Native AOT image under
/// Broiler.VM's root settings and run there over the whole corpus.
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

        var results = LibraryRun.Run(corpus.Records, new EmptyLibrary());
        foreach (var result in results)
        {
            if (result.Verdict != Verdict.NotImplemented)
                failures.Add(result.Vector.Id + ": the empty library gave " + VectorRun.Name(result.Verdict));
        }

        // A second run, over the same corpus, must give the same report (rule SL9's question, asked again under Native AOT).
        var report = VectorRun.Report(results, "empty", corpus.Sources);
        if (report != VectorRun.Report(LibraryRun.Run(corpus.Records, new EmptyLibrary()), "empty", corpus.Sources))
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

    private static int Usage()
    {
        Console.Error.Write("usage: Broiler.ECMAScript.Probe [corpus-root] [--report path]\n");
        return 2;
    }
}
