using System;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms;

/// <summary>
/// The entry point of the test assembly when it is run rather than tested: bundle sjl-3-001's
/// reference-arm comparison (<c>compare</c>), from milestone SJL-4.3 bundle sjl-4-001's
/// (<c>compare-numerics</c>), and from SJL-5.3 bundle sjl-5-001's (<c>compare-text</c>), in Release
/// under the JIT, with the invariant culture as the rules ask.
/// </summary>
/// <remarks>
/// <code>
/// dotnet Broiler.ECMAScript.Algorithms.Tests.dll compare --population DIR --out DIR [--limit LINES] [--operations NAME,NAME]
/// </code>
/// From milestone SJL-5.3, <c>compare-text</c> runs bundle sjl-5-001's.
/// The test host never calls this: it loads the assembly and runs its tests, which is why the
/// project turns the generated entry point off.
/// </remarks>
internal static class Program
{
    public static int Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        if (args.Length == 0 || args[0] is not ("compare" or "compare-numerics" or "compare-text"))
        {
            Console.Error.WriteLine("usage: compare --population DIR --out DIR [--limit LINES] [--operations NAME,NAME]");
            Console.Error.WriteLine("       compare-numerics --population DIR --out DIR [--limit LINES] [--operations NAME|NAME]");
            Console.Error.WriteLine("       compare-text --population DIR --out DIR [--limit LINES] [--operations NAME|NAME] [--controls] [--culture NAME]");
            return 2;
        }

        string? Option(string name)
        {
            var at = Array.IndexOf(args, name);
            return at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
        }

        if (args[0] == "compare-numerics")
            return CompareNumerics(Option);

        if (args[0] == "compare-text")
            return CompareText(Option, args.Contains("--controls"));

        var population = new Population(Option("--population") ?? throw new ArgumentException("--population is required"));
        var output = Option("--out") ?? throw new ArgumentException("--out is required");
        int? limit = Option("--limit") is { } text ? int.Parse(text, CultureInfo.InvariantCulture) : null;
        var names = Option("--operations")?.Split(',');
        var operations = Arms.Operations.Where(o => names is null || names.Contains(o.Name)).ToList();
        new Comparison(population, operations, limit, Console.Out).Run(output);
        return 0;
    }

    /// <summary>
    /// Bundle sjl-5-001's comparison, or with <c>--controls</c> its two negative controls alone. The
    /// operation names hold spaces, so a filter separates them with "|"; a filtered run, like a limited
    /// one, is void.
    /// </summary>
    private static int CompareText(Func<string, string?> option, bool controls)
    {
        // Every thread of the run, the parallel workers' included, under one culture: the invariant one, or
        // the one --culture names for the rule's second run of arm J's string operations.
        var culture = option("--culture") is { } name ? CultureInfo.GetCultureInfo(name) : CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        Console.Out.WriteLine("culture: " + (culture.Name.Length == 0 ? "invariant" : culture.Name));
        var population = new Text.TextPopulation(option("--population") ?? throw new ArgumentException("--population is required"));
        var output = option("--out") ?? throw new ArgumentException("--out is required");
        long? limit = option("--limit") is { } text ? long.Parse(text, CultureInfo.InvariantCulture) : null;
        var names = option("--operations")?.Split('|');
        var all = controls ? Text.TextArms.NegativeControls : Text.TextArms.Operations;
        var operations = all.Where(o => names is null || names.Contains(o.Name)).ToList();
        new Text.TextComparison(population, operations, limit, names is not null, Console.Out)
            .Run(output, "docs/evidence/sjl-5-001/reference-arms.md", "docs/evidence/sjl-5-001/reference-arms-revised-2026-10-14.md");
        return 0;
    }

    /// <summary>
    /// Bundle sjl-4-001's comparison. The operation names hold commas, so a filter separates them with
    /// "|"; a filtered run, like a limited one, is void.
    /// </summary>
    private static int CompareNumerics(Func<string, string?> option)
    {
        var population = new Numerics.NumericsPopulation(option("--population") ?? throw new ArgumentException("--population is required"));
        var output = option("--out") ?? throw new ArgumentException("--out is required");
        long? limit = option("--limit") is { } text ? long.Parse(text, CultureInfo.InvariantCulture) : null;
        var names = option("--operations")?.Split('|');
        var operations = Numerics.NumericsArms.Operations.Where(o => names is null || names.Contains(o.Name)).ToList();
        new Numerics.NumericsComparison(population, operations, limit, names is not null, Console.Out).Run(output);
        return 0;
    }
}
