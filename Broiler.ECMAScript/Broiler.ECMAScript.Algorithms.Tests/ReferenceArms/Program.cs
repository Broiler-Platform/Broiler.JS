using System;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms;

/// <summary>
/// The entry point of the test assembly when it is run rather than tested: bundle sjl-3-001's
/// reference-arm comparison, in Release under the JIT, with the invariant culture as the rule asks.
/// </summary>
/// <remarks>
/// <code>
/// dotnet Broiler.ECMAScript.Algorithms.Tests.dll compare --population DIR --out DIR [--limit LINES] [--operations NAME,NAME]
/// </code>
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
        if (args.Length == 0 || args[0] != "compare")
        {
            Console.Error.WriteLine("usage: compare --population DIR --out DIR [--limit LINES] [--operations NAME,NAME]");
            return 2;
        }

        string? Option(string name)
        {
            var at = Array.IndexOf(args, name);
            return at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
        }

        var population = new Population(Option("--population") ?? throw new ArgumentException("--population is required"));
        var output = Option("--out") ?? throw new ArgumentException("--out is required");
        int? limit = Option("--limit") is { } text ? int.Parse(text, CultureInfo.InvariantCulture) : null;
        var names = Option("--operations")?.Split(',');
        var operations = Arms.Operations.Where(o => names is null || names.Contains(o.Name)).ToList();
        new Comparison(population, operations, limit, Console.Out).Run(output);
        return 0;
    }
}
