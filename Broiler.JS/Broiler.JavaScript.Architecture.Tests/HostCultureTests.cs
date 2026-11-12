using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Broiler.JavaScript.Architecture.Tests;

/// <summary>
/// Milestone SJL-9's clause 8, "Host globalization remains only behind the optional providers", on the owner's ruling of
/// 2026-11-12, "Follow-up arch test": the readers of the host's culture that no build rule sees, and the scope of the rules
/// the build holds.
/// </summary>
/// <remarks>
/// The build holds CA1304, CA1305, CA1309, CA1310, CA1311 and RS0030 as errors in the engine's product projects
/// (<c>Broiler.JS/host-culture.globalconfig</c>, scoped by <c>Broiler.JS/Directory.Build.props</c>). Three kinds of reader
/// escape those rules: a numeric or date <c>Parse</c> or <c>TryParse</c> with no format provider, a .NET <c>Regex</c> that
/// folds case without <c>RegexOptions.CultureInvariant</c>, and an interpolated string, <c>string.Format</c> or
/// <c>StringBuilder.AppendFormat</c> that makes a number, a date or another formattable value text with no provider, culture
/// or invariant format. This test finds them by
/// Roslyn's semantic model over each product project's own sources, compiled in memory as its build compiles them, and
/// read again under the other configuration's preprocessor symbols where a source's code differs there (Debug when the
/// test is built in Release); a kept path is exempt only inside the <c>#pragma warning disable RS0030 // Kept: …</c>
/// region the RS0030 suppressions use, and no product source may silence RS0030 another way. It also asserts, by
/// MSBuild's own evaluation of each project, that every product project is in the rules' scope, silencing none of the
/// rules by its own configuration, and that every test, benchmark, sample and tool project is outside it. The reading
/// costs most of the engine architecture tests' time: one <c>dotnet msbuild</c> process per project, four at a time, and an
/// in-memory compilation of every product project with its source generator.
/// </remarks>
public sealed class HostCultureTests(ITestOutputHelper output)
{
    private static readonly Lazy<IReadOnlyList<ProjectReading>> Readings = new(static () =>
    {
        var projects = HostCultureProjects.Projects;
        return [.. projects
            .Where(p => HostCultureProjects.Classify(p, HostCultureProjects.Samples) == ProjectKind.Product)
            .AsParallel().AsOrdered().WithDegreeOfParallelism(Math.Max(1, Math.Min(4, Environment.ProcessorCount)))
            .Select(p => HostCultureCompilation.Read(p, projects))];
    });

    private static readonly Lazy<IReadOnlyList<ReaderFinding>> WitnessFindings = new(static () => ReadWitness("host-culture-readers.cs.witness"));

    [Fact]
    public void No_product_project_reads_the_hosts_culture_where_no_build_rule_sees()
    {
        var findings = Readings.Value.SelectMany(static r => r.Findings).ToList();
        foreach (var finding in findings.Where(static f => f.Kept))
            output.WriteLine("kept: " + finding);
        var found = findings.Where(static f => !f.Kept).Select(static f => f.ToString()).Order(StringComparer.Ordinal).ToList();
        foreach (var finding in found)
            output.WriteLine("reader: " + finding);

        Assert.True(found.Count == 0, new StringBuilder("The product projects read the host's culture where no build rule sees, at " + found.Count + " place(s):\n  ")
            .AppendJoin("\n  ", found)
            .Append("\nName the invariant culture (string.Create(CultureInfo.InvariantCulture, ...), a provider argument) or "
                + "RegexOptions.CultureInvariant, or, on a path the owner keeps on the host's culture, put the line inside "
                + "\"#pragma warning disable RS0030 // Kept: <reason> (<decision>)\" and its restore.")
            .ToString());
    }

    /// <summary>
    /// Non-vacuity: the reading compiles each product project of the solution as its build does, with no syntax or
    /// declaration error and no error in a source it binds (a source it does not bind holds no node a reader could be), and
    /// reads in the other configuration too each source whose code differs there.
    /// </summary>
    [Fact]
    public void The_reading_compiles_every_product_project_of_the_solution_without_an_error()
    {
        var inSolution = SolutionProjects();
        var failures = new List<string>();
        foreach (var reading in Readings.Value)
        {
            var relative = HostCultureProjects.Relative(reading.Project.Path);
            output.WriteLine(relative + ": " + reading.Sources + " source(s), " + reading.Generated + " generated, "
                + reading.Errors.Count + " error(s), " + reading.Findings.Count + " reader(s)");
            foreach (var other in reading.ReadInOtherConfiguration)
                output.WriteLine("  read in " + HostCultureProjects.OtherConfiguration + " too: " + other);
            if (inSolution.Contains(reading.Project.Path))
            {
                if (reading.Errors.Count > 0)
                    failures.Add(relative + " does not compile in memory: " + string.Join("\n    ", reading.Errors.Take(10)));
            }
            else
            {
                foreach (var error in reading.Errors.Take(5))
                    output.WriteLine("  outside the solution, as its build: " + error);
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n  ", failures));
        Assert.Contains(Readings.Value, static r => r.Project.Name == "Broiler.JavaScript.BuiltIns" && r.Sources > 0 && r.Generated > 0);
        Assert.Contains(Readings.Value, static r => r.Project.Name == "Broiler.JavaScript.Runtime" && r.Sources > 0);
        Assert.Contains(Readings.Value, static r => r.ReadInOtherConfiguration.Count > 0);
    }

    /// <summary>
    /// The kept paths the reading exempts are the RS0030 suppressions of the paths the ruling of 2026-11-11 keeps, in the
    /// form the owner accepted on 2026-11-12 ("Accept RS0030 form"): every <c>#pragma warning disable RS0030</c> in a product
    /// project opens a kept path, naming its reason, and no product source silences RS0030 any other way (a
    /// <c>#pragma warning disable</c> that names no code, or a <c>SuppressMessage</c> of RS0030).
    /// </summary>
    [Fact]
    public void Every_RS0030_suppression_in_a_product_project_is_a_kept_path_and_the_reading_finds_them()
    {
        var kept = Readings.Value.SelectMany(static r => r.Kept).Order(StringComparer.Ordinal).ToList();
        var suppressions = Readings.Value.SelectMany(static r => r.Suppressions).Order(StringComparer.Ordinal).ToList();
        foreach (var path in kept)
            output.WriteLine("kept path: " + path);
        Assert.NotEmpty(kept);
        Assert.Equal(suppressions, kept);
        var others = Readings.Value.SelectMany(static r => r.OtherSuppressions).Order(StringComparer.Ordinal).ToList();
        Assert.True(others.Count == 0, "Product sources silence RS0030 outside the kept-path form:\n  " + string.Join("\n  ", others));
    }

    [Theory]
    [InlineData("Parse")]
    [InlineData("Regex")]
    [InlineData("Format")]
    public void The_witness_is_refused_for_each_kind_on_exactly_its_marked_lines(string shown)
    {
        var kind = Enum.Parse<ReaderKind>(shown);
        var (refused, kept) = Marks("host-culture-readers.cs.witness");
        var found = WitnessFindings.Value.Where(f => f.Kind == kind).ToList();
        foreach (var finding in found)
            output.WriteLine((finding.Kept ? "kept: " : "reader: ") + finding);

        Assert.NotEmpty(refused[kind]);
        Assert.Equal(refused[kind], found.Where(static f => !f.Kept).Select(static f => f.Line).Distinct().Order());
        Assert.Equal(kept[kind], found.Where(static f => f.Kept).Select(static f => f.Line).Distinct().Order());
    }

    [Fact]
    public void The_witness_silencing_RS0030_outside_the_kept_path_form_is_refused_on_exactly_its_marked_lines()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "witnesses", "host-culture-readers.cs.witness");
        var lines = File.ReadAllLines(path);
        var marked = Enumerable.Range(1, lines.Length).Where(n => lines[n - 1].TrimEnd().EndsWith("// silences: RS0030", StringComparison.Ordinal)).ToList();
        var found = HostCultureReaders.OtherRs0030Suppressions(CSharpSyntaxTree.ParseText(File.ReadAllText(path), cancellationToken: TestContext.Current.CancellationToken).GetRoot(TestContext.Current.CancellationToken)).ToList();
        foreach (var (line, form) in found)
            output.WriteLine(line + ": " + form);
        Assert.NotEmpty(marked);
        Assert.Equal(marked, found.Select(static f => f.Line).Order());
    }

    [Fact]
    public void Every_product_project_is_in_the_host_culture_rules_scope_and_no_other_project_is()
    {
        var projects = HostCultureProjects.Projects;
        foreach (var project in projects)
        {
            output.WriteLine(HostCultureProjects.Relative(project.Path) + ": " + HostCultureProjects.Classify(project, HostCultureProjects.Samples)
                + ", BroilerHostCultureRules=" + project.Property("BroilerHostCultureRules"));
        }

        var violations = HostCultureProjects.ScopeViolations(projects, HostCultureProjects.Samples, HostCultureProjects.GlobalConfig, HostCultureProjects.BannedSymbols).ToList();
        Assert.True(violations.Count == 0, "The host-culture rules' scope departs from the projects' kinds:\n  " + string.Join("\n  ", violations));

        // Non-vacuity: the scope read every kind, and the solution's projects.
        var kinds = projects.Select(static p => HostCultureProjects.Classify(p, HostCultureProjects.Samples)).ToHashSet();
        foreach (var kind in Enum.GetValues<ProjectKind>())
            Assert.Contains(kind, kinds);
        Assert.Superset(SolutionProjects(), projects.Select(static p => p.Path).ToHashSet(StringComparer.Ordinal));
        Assert.Contains(projects, static p => p.Name == "Broiler.JavaScript.Runtime" && HostCultureProjects.IsTrue(p.Property("BroilerHostCultureRules")));
    }

    [Theory]
    [InlineData("a product project named like a test", "Broiler.JS/Broiler.JavaScript.Widget.Tests/Broiler.JavaScript.Widget.Tests.csproj", "false", false, "is a product project and evaluates BroilerHostCultureRules=false")]
    [InlineData("a product project outside Broiler.JS/", "Broiler.Widget/Broiler.Widget.csproj", "", false, "is a product project and evaluates BroilerHostCultureRules=(empty)")]
    [InlineData("a product project the compiler is not given the rules", "Broiler.JS/Broiler.JavaScript.Widget/Broiler.JavaScript.Widget.csproj", "true", false, "the compiler is not given host-culture.globalconfig")]
    [InlineData("a test project the rules hold", "Broiler.JS/Broiler.JavaScript.Widget.Tests/Broiler.JavaScript.Widget.Tests.csproj", "true", true, "is a test project and the host-culture rules hold it")]
    public void The_scope_refuses_each_departure(string shape, string path, string rules, bool isTest, string refusal)
    {
        var project = Synthetic(path, rules, isTest, given: false);
        var violations = HostCultureProjects.ScopeViolations([project], [], HostCultureProjects.GlobalConfig, HostCultureProjects.BannedSymbols).ToList();
        Assert.True(violations.Any(v => v.Contains(refusal, StringComparison.Ordinal)),
            "The scope does not refuse " + shape + ": no violation contains \"" + refusal + "\". Its violations:\n  " + string.Join("\n  ", violations));
    }

    [Fact]
    public void The_scope_refuses_a_sample_a_product_project_references_and_a_listed_sample_that_does_not_exist()
    {
        var sample = Synthetic("Broiler.JS/samples/Widget/Widget.csproj", "false", isTest: false, given: false);
        var product = Synthetic("Broiler.JS/Broiler.JavaScript.Widget/Broiler.JavaScript.Widget.csproj", "true", isTest: false, given: true, references: sample.Path);
        string[] samples = ["Broiler.JS/samples/Widget/Widget.csproj", "Broiler.JS/samples/Gone/Gone.csproj"];
        var violations = HostCultureProjects.ScopeViolations([product, sample], samples, HostCultureProjects.GlobalConfig, HostCultureProjects.BannedSymbols).ToList();
        foreach (var violation in violations)
            output.WriteLine(violation);
        Assert.Equal(
            [
                "Broiler.JS/samples/Gone/Gone.csproj is listed as a sample and is not a project of Broiler.JS.slnx or below Broiler.JS/",
                "Broiler.JS/samples/Widget/Widget.csproj is listed as a sample and the product project Broiler.JS/Broiler.JavaScript.Widget/Broiler.JavaScript.Widget.csproj references it",
            ],
            violations);
    }

    [Fact]
    public void The_scope_refuses_a_product_project_that_silences_a_host_culture_rule()
    {
        var config = Path.Combine(Path.GetTempPath(), "host-culture-silenced-" + Guid.NewGuid().ToString("N") + ".globalconfig");
        File.WriteAllLines(config, ["is_global = true", "dotnet_diagnostic.RS0030.severity = none"]);
        try
        {
            var given = Synthetic("Broiler.JS/Broiler.JavaScript.Widget/Broiler.JavaScript.Widget.csproj", "true", isTest: false, given: true);
            var properties = new Dictionary<string, string>(given.Properties, StringComparer.Ordinal) { ["NoWarn"] = "CS1591;CA1305", ["RunAnalyzersDuringBuild"] = "false" };
            var items = new Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, string>>>(given.Items, StringComparer.Ordinal)
            {
                ["GlobalAnalyzerConfigFiles"] =
                [
                    new Dictionary<string, string>(StringComparer.Ordinal) { ["Identity"] = "host-culture.globalconfig", ["FullPath"] = HostCultureProjects.GlobalConfig },
                    new Dictionary<string, string>(StringComparer.Ordinal) { ["Identity"] = config, ["FullPath"] = config },
                ],
            };
            var violations = HostCultureProjects.ScopeViolations([given with { Properties = properties, Items = items }], [], HostCultureProjects.GlobalConfig, HostCultureProjects.BannedSymbols).ToList();
            foreach (var violation in violations)
                output.WriteLine(violation);
            Assert.Equal(3, violations.Count);
            Assert.Contains(violations, static v => v.EndsWith("is a product project and NoWarn names CA1305", StringComparison.Ordinal));
            Assert.Contains(violations, static v => v.EndsWith("is a product project and RunAnalyzersDuringBuild is false", StringComparison.Ordinal));
            Assert.Contains(violations, static v => v.EndsWith(":2 sets a severity for RS0030", StringComparison.Ordinal));
        }
        finally
        {
            File.Delete(config);
        }
    }

    private static EvaluatedProject Synthetic(string relative, string rules, bool isTest, bool given, string? references = null)
    {
        var path = Path.GetFullPath(Path.Combine(HostCultureProjects.Root, relative));
        var properties = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["BroilerHostCultureRules"] = rules,
            ["IsTestProject"] = isTest ? "true" : "",
        };
        IReadOnlyList<IReadOnlyDictionary<string, string>> Item(string identity, string fullPath) =>
            [new Dictionary<string, string>(StringComparer.Ordinal) { ["Identity"] = identity, ["FullPath"] = fullPath }];
        var items = new Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, string>>>(StringComparer.Ordinal);
        if (given || isTest)
        {
            items["GlobalAnalyzerConfigFiles"] = Item("host-culture.globalconfig", HostCultureProjects.GlobalConfig);
            items["AdditionalFiles"] = Item("BannedSymbols.HostCulture.txt", HostCultureProjects.BannedSymbols);
            items["PackageReference"] = Item("Microsoft.CodeAnalysis.BannedApiAnalyzers", "Microsoft.CodeAnalysis.BannedApiAnalyzers");
        }

        if (references is not null)
            items["ProjectReference"] = Item(references, references);
        return new EvaluatedProject(path, properties, items);
    }

    private static HashSet<string> SolutionProjects()
    {
        var solution = System.Xml.Linq.XDocument.Load(Path.Combine(HostCultureProjects.Root, HostCultureProjects.SolutionFile));
        return solution.Descendants("Project")
            .Select(static p => (string?)p.Attribute("Path"))
            .Where(static p => p is not null)
            .Select(static p => Path.GetFullPath(Path.Combine(HostCultureProjects.Root, p!.Replace('\\', Path.DirectorySeparatorChar))))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static (Dictionary<ReaderKind, List<int>> Refused, Dictionary<ReaderKind, List<int>> Kept) Marks(string file)
    {
        var refused = Enum.GetValues<ReaderKind>().ToDictionary(static k => k, static _ => new List<int>());
        var kept = Enum.GetValues<ReaderKind>().ToDictionary(static k => k, static _ => new List<int>());
        var lines = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "witnesses", file));
        for (var i = 0; i < lines.Length; i++)
        {
            var mark = System.Text.RegularExpressions.Regex.Match(lines[i], @"// (refused|kept): (\w+)\s*$");
            if (!mark.Success)
                continue;
            var kind = Enum.Parse<ReaderKind>(mark.Groups[2].Value);
            (mark.Groups[1].Value == "refused" ? refused : kept)[kind].Add(i + 1);
        }

        return (refused, kept);
    }

    private static List<ReaderFinding> ReadWitness(string file)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "witnesses", file);
        var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), new CSharpParseOptions(LanguageVersion.Latest), path: file);
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(static p => Path.GetFileName(p).StartsWith("System.", StringComparison.Ordinal) || Path.GetFileName(p) is "netstandard.dll" or "mscorlib.dll")
            .Order(StringComparer.Ordinal)
            .Select(static p => MetadataReference.CreateFromFile(p));
        var compilation = CSharpCompilation.Create("host_culture_readers", [tree], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        var errors = compilation.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Count > 0)
            throw new InvalidOperationException("the witness " + file + " does not compile:\n" + string.Join("\n", errors));
        return HostCultureReaders.Read(compilation, [tree], static t => t.FilePath);
    }
}
