using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.Benchmarks;

/// <summary>
/// The workloads Broiler.JS's measurement of the shared JavaScript libraries' Unicode area
/// (milestone SJL-7) is taken over, committed with its decision rule at SJL-7.1, before any code
/// the rule judges exists.
/// </summary>
/// <remarks>
/// <para>
/// The rule is <c>docs/evidence/sjl-7-001/decision-rule.md</c>, part B. It compares this engine at
/// its base commit with this engine after the cut-over, never with another engine, and states its
/// repetitions and its tolerance itself. <b>No figure from these workloads belongs anywhere but an
/// evidence bundle.</b>
/// </para>
/// <para>
/// The three workloads are the ones milestone SJL-7 names: <c>normalize</c>, casing and identifier
/// scanning. Their strings are the ones Broiler.VM's workloads of the same milestone use, written
/// here as escapes so that no editor or tool can change their form. Each workload has a control:
/// the same program minus the operations measured. For the two runtime workloads the strings,
/// loops and accumulation are the same and each operation is replaced by a type test of its input;
/// for the identifier workload the same source is compiled with every identifier written in ASCII
/// of the same length in code points. A difference between a workload and its control attributes
/// cost to the operations; the rule decides on the workload alone.
/// </para>
/// <para>
/// <b>The answers count completed operations, not their results.</b> The cut-over corrects results
/// this engine gets wrong today (normalization that depends on the host's ICU, lone surrogates that
/// throw, casing that depends on the host culture, identifiers that do not subtract
/// Pattern_Syntax), so an answer built from the results would change for a correction, as the
/// answer of SJL-3's measurement of Broiler.VM did. Counting each completed operation keeps the
/// answer the same at the base and after, and a lane that throws or stops early still fails it.
/// <c>--unicode-answers</c> prints every answer with the process's effective configuration,
/// including its culture and two probes of the host's globalization data, which this engine reads
/// at the base: under invariant globalization its locale casing throws, so the answers are taken
/// with the host's ICU loaded, and the probes show it.
/// </para>
/// <para>
/// The workloads are not changed after they are committed: a change would change the population
/// the rule was written over.
/// </para>
/// </remarks>
public class UnicodeBenchmarks
{
    /// <summary>
    /// <c>normalize</c> in its four forms, over strings already in each form and strings that are
    /// not: Latin with combining marks, Hangul, compatibility characters, Greek with diacritics,
    /// supplementary-plane mappings and ASCII.
    /// </summary>
    private const string NormalizeWorkload = """
        (function () {
            var samples = ["été café", "été café", "가나다라",
                "가나다", "ﬁﬂ Ω Å ㌀", "ᾀᾳΐ άέ",
                "Ǻ q̣̇ ẛ̣", "𝅗𝅥𝅘𝅥", "plain ascii text 0123",
                "ｶﾞﾊﾟ ½ ①"];
            var forms = ["NFC", "NFD", "NFKC", "NFKD"];
            var total = 0;
            for (var i = 0; i < 600; i++) {
                for (var j = 0; j < samples.length; j++) {
                    for (var f = 0; f < forms.length; f++) {
                        total += typeof samples[j].normalize(forms[f]) === "string" ? 1 : 0;
                    }
                }
            }
            return total;
        })()
        """;

    private const string NormalizeControl = """
        (function () {
            var samples = ["été café", "été café", "가나다라",
                "가나다", "ﬁﬂ Ω Å ㌀", "ᾀᾳΐ άέ",
                "Ǻ q̣̇ ẛ̣", "𝅗𝅥𝅘𝅥", "plain ascii text 0123",
                "ｶﾞﾊﾟ ½ ①"];
            var forms = ["NFC", "NFD", "NFKC", "NFKD"];
            var total = 0;
            for (var i = 0; i < 600; i++) {
                for (var j = 0; j < samples.length; j++) {
                    for (var f = 0; f < forms.length; f++) {
                        total += typeof samples[j] === "string" ? 1 : 0;
                    }
                }
            }
            return total;
        })()
        """;

    /// <summary>
    /// <c>toUpperCase</c> and <c>toLowerCase</c>, and <c>toLocaleUpperCase</c> and
    /// <c>toLocaleLowerCase</c> with a Turkic and a Lithuanian locale, over ASCII, Latin with
    /// diacritics, German sharp s, Greek with final sigma, Cyrillic, Armenian ligatures, title-case
    /// letters and supplementary-plane letters.
    /// </summary>
    private const string CasingWorkload = """
        (function () {
            var samples = ["The Quick Brown Fox", "Straße ÄÖÜ äöü",
                "ΑΣΤΡΟΣ λόγος", "Привет мир",
                "և ﬀﬁ ŉ", "𐐀𐐁 𐵐𐵑", "İstanbul ışık",
                "İ J̀ ÌÍ", "plain ascii text 0123", "ǅǈǋ ᾀᾈ"];
            var total = 0;
            for (var i = 0; i < 600; i++) {
                for (var j = 0; j < samples.length; j++) {
                    var s = samples[j];
                    total += typeof s.toUpperCase() === "string" ? 1 : 0;
                    total += typeof s.toLowerCase() === "string" ? 1 : 0;
                    total += typeof s.toLocaleUpperCase("tr") === "string" ? 1 : 0;
                    total += typeof s.toLocaleLowerCase("lt") === "string" ? 1 : 0;
                }
            }
            return total;
        })()
        """;

    private const string CasingControl = """
        (function () {
            var samples = ["The Quick Brown Fox", "Straße ÄÖÜ äöü",
                "ΑΣΤΡΟΣ λόγος", "Привет мир",
                "և ﬀﬁ ŉ", "𐐀𐐁 𐵐𐵑", "İstanbul ışık",
                "İ J̀ ÌÍ", "plain ascii text 0123", "ǅǈǋ ᾀᾈ"];
            var total = 0;
            for (var i = 0; i < 600; i++) {
                for (var j = 0; j < samples.length; j++) {
                    var s = samples[j];
                    total += typeof s === "string" ? 1 : 0;
                    total += typeof s === "string" ? 1 : 0;
                    total += typeof s === "string" ? 1 : 0;
                    total += typeof s === "string" ? 1 : 0;
                }
            }
            return total;
        })()
        """;

    /// <summary>
    /// Identifiers outside ASCII: Greek, Cyrillic, Han, Hiragana, Katakana, Other_ID_Start and
    /// Other_ID_Continue characters, a joiner, supplementary-plane letters and Devanagari with a
    /// virama. The source holds them literally, so the scanner classifies every one.
    /// </summary>
    private static readonly string[] IdentifierNames =
    [
        "αβγ", "жук", "日本語", "ひらがな",
        "カタカナ", "℘℮", "x··", "a‌b", "\U0001D465\U0001D466",
        "क्ष",
    ];

    /// <summary>The same names in ASCII, each of the same length in code points.</summary>
    private static readonly string[] IdentifierControlNames =
    [
        "abc", "zhk", "nih", "hira", "kata", "pe", "xmm", "azb", "xy", "ksa",
    ];

    private static readonly string IdentifiersWorkload = IdentifierSource(IdentifierNames);

    private static readonly string IdentifiersControl = IdentifierSource(IdentifierControlNames);

    internal static readonly IReadOnlyDictionary<string, string> Workloads = new Dictionary<string, string>
    {
        ["Normalize"] = NormalizeWorkload,
        ["NormalizeControl"] = NormalizeControl,
        ["Casing"] = CasingWorkload,
        ["CasingControl"] = CasingControl,
        ["Identifiers"] = IdentifiersWorkload,
        ["IdentifiersControl"] = IdentifiersControl,
    };

    private JSContext context;
    private string source;
    private bool compileOnly;

    [Params("Normalize", "NormalizeControl", "Casing", "CasingControl", "Identifiers", "IdentifiersControl")]
    public string Workload { get; set; } = "Normalize";

    [GlobalSetup]
    public void Setup()
    {
        source = Workloads[Workload];
        compileOnly = Workload.StartsWith("Identifiers", StringComparison.Ordinal);
        context = BenchmarkContext.Create(compileOnly ? new NoCodeCache() : new LocalDictionaryCodeCache());
        if (!compileOnly)
            context.Eval(source, Workload + ".js");
    }

    [GlobalCleanup]
    public void Cleanup() => context?.Dispose();

    /// <summary>
    /// One operation: the runtime workloads evaluate their cached script once; the identifier
    /// workloads compile their source once, without a cache, and do not run it.
    /// </summary>
    [Benchmark]
    public object Run()
        => compileOnly
            ? CoreScript.Compile(source, Workload + ".js", codeCache: new NoCodeCache())
            : context.Eval(source, Workload + ".js");

    /// <summary>
    /// Prints each workload's answer, and the effective configuration of this process, as one JSON
    /// object. The identifier workloads are compiled and then run once to produce theirs.
    /// </summary>
    public static void WriteAnswers()
    {
        var answers = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (name, script) in Workloads)
        {
            using var context = BenchmarkContext.Create(new NoCodeCache());
            JSValue value;
            if (name.StartsWith("Identifiers", StringComparison.Ordinal))
            {
                var compiled = CoreScript.Compile(script, name + ".js", codeCache: new NoCodeCache());
                var arguments = new Arguments(context);
                value = compiled(in arguments);
            }
            else
            {
                value = context.Eval(script, name + ".js");
            }

            answers[name] = value.ToString();
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
            ["hostTurkicUpperOfI"] = HostProbe(() => ((int)CultureInfo.GetCultureInfo("tr").TextInfo.ToUpper('i')).ToString("X4", CultureInfo.InvariantCulture)),
            ["hostNfcLengthOfDecomposedE"] = HostProbe(() => "é".Normalize(NormalizationForm.FormC).Length.ToString(CultureInfo.InvariantCulture)),
        };

        Console.WriteLine(JsonSerializer.Serialize(new { answers, configuration }));
    }

    /// <summary>
    /// A fact about the host's globalization data, which this engine's casing and normalization read
    /// at the base: the probe's value, or the name of the exception it threw.
    /// </summary>
    private static string HostProbe(Func<string> probe)
    {
        try
        {
            return probe();
        }
        catch (Exception exception)
        {
            return exception.GetType().Name;
        }
    }

    /// <summary>
    /// A function that declares each name twenty times with a numeric suffix, reads each
    /// declaration once, and returns the number of reads that completed.
    /// </summary>
    private static string IdentifierSource(string[] names)
    {
        var builder = new StringBuilder("(function () { var t = 0;");
        for (var k = 0; k < 20; k++)
        {
            foreach (var name in names)
            {
                var identifier = name + k.ToString(CultureInfo.InvariantCulture);
                builder.Append(" var ").Append(identifier).Append(" = 1; t += ").Append(identifier).Append(';');
            }
        }

        return builder.Append(" return t; })()").ToString();
    }
}
