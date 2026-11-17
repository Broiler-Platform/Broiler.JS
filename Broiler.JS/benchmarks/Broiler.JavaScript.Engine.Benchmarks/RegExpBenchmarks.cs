using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

/// <summary>
/// The workloads Broiler.JS's measurement of the shared JavaScript libraries' regular-expression
/// area (milestone SJL-11) is taken over: the twelve programs committed with its decision rule at
/// SJL-11.1, before any code the rule judges existed, run here verbatim.
/// </summary>
/// <remarks>
/// <para>
/// The rule is <c>docs/evidence/sjl-11-001/decision-rule.md</c>, part B. It compares this engine at
/// its base commit with this engine after the cut-over, never with another engine, and states its
/// repetitions and its noise floor itself. <b>No figure from these workloads belongs anywhere but
/// bundle <c>docs/evidence/sjl-11-001/</c>'s <c>measurement/</c> sub-bundle.</b>
/// </para>
/// <para>
/// The programs are not written here: they are the files of
/// <c>docs/evidence/sjl-11-001/inputs/workloads/</c>, embedded in this assembly byte for byte by the
/// project file (decision D17 of Broiler.VM's JSD-0081). Workload <c>Scan</c> runs <c>scan.js</c>
/// and <c>ScanControl</c> runs <c>scan.control.js</c>, and so on for <c>Captures</c>,
/// <c>Replace</c>, <c>Split</c>, <c>Unicode</c> and <c>Backtrack</c>. Each file is checked against
/// the SHA-256 the workloads' README gives before it runs, and a file that differs stops the run.
/// </para>
/// <para>
/// <b>Each program's completion value is its answer</b>, which counts the operations that
/// completed, not their results: the cut-over corrects results this engine gets wrong today, so an
/// answer built from the results would change for a correction. <c>--regexp-answers</c> prints every
/// answer with the process's effective configuration, as one JSON object.
/// </para>
/// <para>
/// The workloads are not changed after they are committed: a change would change the population
/// the rule was written over.
/// </para>
/// </remarks>
public class RegExpBenchmarks
{
    /// <summary>The workloads, each with the file it runs and that file's SHA-256 as the workloads' README gives it.</summary>
    private static readonly (string Name, string File, string Sha256)[] Files =
    [
        ("Scan", "scan.js", "02a619abf2fcf2f443136b05cb35d8cfd55d4a6dfd746e4912c7814a681287b0"),
        ("ScanControl", "scan.control.js", "ef64a4b153c2c7df2dea77e42df2a730b4db0ca0771cfc454591c7403af32f0d"),
        ("Captures", "captures.js", "e25e7178d7bcc5fb2d8757ea71ca82f14c782fdc6834c292249b206915530652"),
        ("CapturesControl", "captures.control.js", "19df64626ca1d819ee37f9d12205c34789361f692dbb7750a848e5413412c11b"),
        ("Replace", "replace.js", "e831cd294655c95ec91e93ea2ea69a93535e5ab61dc05f82ea9bb49acd28275b"),
        ("ReplaceControl", "replace.control.js", "1dd2f4d035ed75c940b0a74bff4af4e5ee994387c0ce129e937c50fa9b713b3f"),
        ("Split", "split.js", "62530412cfba8672de165b8be897fe0ef869142d5da591285e517b50223503da"),
        ("SplitControl", "split.control.js", "8cf7d77169f7c709b08fd21eb42a93c54badf7e0b22033d182f1cf35594296a0"),
        ("Unicode", "unicode.js", "ec67a4f9353aaa4e0269b76d3173a6a6501bbc7ca3bed68244e4683cbdf6d3cf"),
        ("UnicodeControl", "unicode.control.js", "a971807c729a121e24ce0945eee9165325b901c632059e6e9c6841367dcc5169"),
        ("Backtrack", "backtrack.js", "f52f1ed6049205377d0909db043d4fa1c4570d6394f69d65c509d5a0e0593444"),
        ("BacktrackControl", "backtrack.control.js", "ba1ef3cd30f74100871175a3e0929084dc9b75d0393db33105bcab4cdae4d033"),
    ];

    private JSContext context;
    private string source;
    private string fileName;

    [Params("Scan", "ScanControl", "Captures", "CapturesControl", "Replace", "ReplaceControl",
        "Split", "SplitControl", "Unicode", "UnicodeControl", "Backtrack", "BacktrackControl")]
    public string Workload { get; set; } = "Scan";

    [GlobalSetup]
    public void Setup()
    {
        (fileName, source) = Read(Workload);
        context = BenchmarkContext.Create(new LocalDictionaryCodeCache());
        context.Eval(source, fileName);
    }

    [GlobalCleanup]
    public void Cleanup() => context?.Dispose();

    /// <summary>One operation: the workload's cached program evaluated once.</summary>
    [Benchmark]
    public object Run() => context.Eval(source, fileName);

    /// <summary>
    /// Prints each workload's answer, its program's completion value, each file's SHA-256, and the
    /// effective configuration of this process, as one JSON object.
    /// </summary>
    public static void WriteAnswers()
    {
        var answers = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var files = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (name, file, sha256) in Files)
        {
            var (_, script) = Read(name);
            files[file] = sha256;
            using var context = BenchmarkContext.Create(new NoCodeCache());
            answers[name] = context.Eval(script, file).ToString();
        }

        var configuration = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["framework"] = RuntimeInformation.FrameworkDescription,
            ["rid"] = RuntimeInformation.RuntimeIdentifier,
            ["processArchitecture"] = RuntimeInformation.ProcessArchitecture.ToString(),
            ["os"] = RuntimeInformation.OSDescription,
            ["serverGc"] = GCSettings.IsServerGC.ToString(CultureInfo.InvariantCulture),
            ["concurrentGc"] = Convert.ToString(AppContext.GetData("System.GC.Concurrent"), CultureInfo.InvariantCulture) ?? "",
            ["tieredCompilation"] = Convert.ToString(AppContext.GetData("System.Runtime.TieredCompilation"), CultureInfo.InvariantCulture) ?? "",
            ["tieredPgo"] = Convert.ToString(AppContext.GetData("System.Runtime.TieredPGO"), CultureInfo.InvariantCulture) ?? "",
            ["readyToRun"] = Convert.ToString(AppContext.GetData("System.Runtime.ReadyToRun"), CultureInfo.InvariantCulture) ?? "",
            ["vectorHardwareAccelerated"] = Vector.IsHardwareAccelerated.ToString(CultureInfo.InvariantCulture),
            ["processorCount"] = Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture),
            ["globalizationInvariantVariable"] = Environment.GetEnvironmentVariable("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT") ?? "",
            ["currentCulture"] = CultureInfo.CurrentCulture.Name is { Length: > 0 } culture ? culture : "invariant",
            ["currentUICulture"] = CultureInfo.CurrentUICulture.Name is { Length: > 0 } uiCulture ? uiCulture : "invariant",
        };

        Console.WriteLine(JsonSerializer.Serialize(new { answers, files, configuration }));
    }

    /// <summary>
    /// The workload's file name and text, read from this assembly's embedded copy of
    /// <c>docs/evidence/sjl-11-001/inputs/workloads/</c>; a file whose SHA-256 is not the README's stops the run.
    /// </summary>
    private static (string File, string Text) Read(string workload)
    {
        foreach (var (name, file, sha256) in Files)
        {
            if (!string.Equals(name, workload, StringComparison.Ordinal))
                continue;

            using var stream = typeof(RegExpBenchmarks).Assembly.GetManifestResourceStream("RegExpWorkloads." + file)
                ?? throw new InvalidOperationException("The workload file " + file + " is not embedded.");
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            var bytes = buffer.ToArray();
            var actual = Convert.ToHexStringLower(SHA256.HashData(bytes));
            if (!string.Equals(actual, sha256, StringComparison.Ordinal))
                throw new InvalidOperationException("The workload file " + file + " has SHA-256 " + actual + ", not " + sha256 + ".");

            return (file, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(bytes));
        }

        throw new ArgumentException("No regular-expression workload is named " + workload + ".", nameof(workload));
    }
}
