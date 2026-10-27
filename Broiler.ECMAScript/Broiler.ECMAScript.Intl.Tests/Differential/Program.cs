using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace Broiler.ECMAScript.Intl.Tests.Differential;

/// <summary>
/// The entry point of the test assembly when it is run rather than tested: the differential check of Broiler.VM's JSD-0082,
/// decision D22, in Release under the JIT, with the invariant culture.
/// </summary>
/// <remarks>
/// <code>
/// dotnet Broiler.ECMAScript.Intl.Tests.dll differential [--seed N] [--scale N] [--sections D0,L1,...] [--classified-examples N] [--out FILE]
/// </code>
/// It prints the report and writes it to <c>--out</c> when given, and exits 1 when a difference is unclassified.
/// <c>--classified-examples</c> lists up to that many classified differences of each row and section, three by default, so
/// that every one can be audited against its row. The test
/// host never calls this: it loads the assembly and runs its tests, which is why the project turns the generated entry
/// point off.
/// </remarks>
internal static class Program
{
    public static int Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

        if (args.Length == 0 || args[0] != "differential")
        {
            Console.Error.WriteLine("usage: differential [--seed N] [--scale N] [--sections D0,L1,...] [--classified-examples N] [--out FILE]");
            return 2;
        }

        string? Option(string name)
        {
            var at = Array.IndexOf(args, name);
            return at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
        }

        var seed = Option("--seed") is { } s ? ulong.Parse(s, CultureInfo.InvariantCulture) : DifferentialCheck.DefaultSeed;
        var scale = Option("--scale") is { } n ? int.Parse(n, CultureInfo.InvariantCulture) : 1;
        var only = Option("--sections")?.Split(',');
        if (Option("--classified-examples") is { } examples)
            Section.ClassifiedExampleLimit = int.Parse(examples, CultureInfo.InvariantCulture);
        var sections = DifferentialCheck.Run(seed, scale, only);
        var report = DifferentialCheck.Report(sections, seed, scale);
        Console.Write(report);

        if (Option("--out") is { } output)
            File.WriteAllText(output, report);

        return sections.Any(static section => section.Unclassified > 0) ? 1 : 0;
    }
}
