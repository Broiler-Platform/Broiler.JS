using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Broiler.JavaScript.Architecture.Tests;

/// <summary>What a project is, for the host-culture rules' scope: read from what the project declares, not from its name.</summary>
internal enum ProjectKind
{
    /// <summary>A project that ships engine code: every project the other kinds do not take. The rules hold it.</summary>
    Product,

    /// <summary>A test project: <c>IsTestProject</c> is true.</summary>
    Test,

    /// <summary>A benchmark: it references BenchmarkDotNet.</summary>
    Benchmark,

    /// <summary>A sample, named in <see cref="HostCultureProjects.Samples"/>, which no product project references.</summary>
    Sample,

    /// <summary>A tool: a compiler plug-in (<c>EnforceExtendedAnalyzerRules</c> is true), such as the source generator.</summary>
    Tool,

    /// <summary>A shared library or its test, below Broiler.ECMAScript/: held by the shared libraries' own rules, SL1 to SL10.</summary>
    Shared,
}

/// <summary>One project as MSBuild evaluates it: its properties and items with every import applied.</summary>
internal sealed record EvaluatedProject(
    string Path,
    IReadOnlyDictionary<string, string> Properties,
    IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, string>>> Items)
{
    internal string Name => System.IO.Path.GetFileNameWithoutExtension(Path);

    internal string Property(string name) => Properties.TryGetValue(name, out var value) ? value : string.Empty;

    internal IReadOnlyList<IReadOnlyDictionary<string, string>> ItemsOf(string type) =>
        Items.TryGetValue(type, out var items) ? items : [];

    internal IEnumerable<string> FullPaths(string type) =>
        ItemsOf(type).Select(static item => item.TryGetValue("FullPath", out var path) ? path : item["Identity"]);

    internal IEnumerable<string> Identities(string type) => ItemsOf(type).Select(static item => item["Identity"]);
}

/// <summary>
/// The engine's projects and their evaluation for milestone SJL-9's clause 8 (the owner's ruling of 2026-11-12, "Follow-up
/// arch test"): every project of Broiler.JS.slnx and every project file below Broiler.JS/, each evaluated by MSBuild itself
/// (<c>dotnet msbuild -getProperty -getItem</c>, as rules SL5 and SL6 evaluate a shared project), so that the scope the
/// test reads is the one <c>Broiler.JS/Directory.Build.props</c> gives the build, with every import and condition applied.
/// </summary>
internal static class HostCultureProjects
{
    internal const string SolutionFile = "Broiler.JS.slnx";

    internal const string EngineFolder = "Broiler.JS";

    internal const string SharedFolder = "Broiler.ECMAScript";

    /// <summary>The samples, by repository path: a sample declares nothing that tells it from a product, so this list does.</summary>
    internal static readonly string[] Samples =
    [
        "Broiler.JS/Broiler.JavaScript.Feature.Sample/Broiler.JavaScript.Feature.Sample.csproj",
        "Broiler.JS/samples/Broiler.JavaScript.NativeAotSample/Broiler.JavaScript.NativeAotSample.csproj",
        "Broiler.JS/samples/Broiler.JavaScript.StartupHost/Broiler.JavaScript.StartupHost.csproj",
    ];

    internal static readonly string[] EvaluatedProperties =
    [
        "BroilerHostCultureRules", "IsTestProject", "EnforceExtendedAnalyzerRules", "AssemblyName", "OutputType",
        "LangVersion", "DefineConstants", "Nullable", "AllowUnsafeBlocks", "TargetPath", "TargetFramework", "ProjectAssetsFile",
        "NoWarn", "RunAnalyzers", "RunAnalyzersDuringBuild",
    ];

    /// <summary>The rules the build holds over the product projects, which no product project may silence by its own configuration.</summary>
    internal static readonly string[] HostCultureRules = ["CA1304", "CA1305", "CA1309", "CA1310", "CA1311", "RS0030"];

    internal static readonly string[] EvaluatedItems =
    [
        "Compile", "GlobalAnalyzerConfigFiles", "AdditionalFiles", "PackageReference", "ProjectReference",
    ];

    /// <summary>The repository root: the first directory above the test assembly that holds Broiler.JS.slnx.</summary>
    internal static string Root { get; } = FindRoot();

    /// <summary>The configuration this test assembly was built in, which the evaluation and the read build outputs use.</summary>
    internal static string Configuration { get; } = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;

    /// <summary>
    /// The configuration whose preprocessor symbols the reading also parses each product project's sources with: Debug when
    /// the test is built in Release and Release when it is built in Debug (Debug-Linux for Release-Linux, and so on), so that
    /// code under <c>#if DEBUG</c> or <c>#if RELEASE</c> is read whichever the test is built in.
    /// </summary>
    internal static string OtherConfiguration { get; } = Other(Configuration);

    private static string Other(string configuration) =>
        configuration.StartsWith("Release", StringComparison.OrdinalIgnoreCase) ? "Debug" + configuration["Release".Length..]
        : configuration.StartsWith("Debug", StringComparison.OrdinalIgnoreCase) ? "Release" + configuration["Debug".Length..]
        : "Debug";

    /// <summary>A project's preprocessor symbols in <paramref name="configuration"/>, as MSBuild evaluates them.</summary>
    internal static string[] DefinesIn(EvaluatedProject project, string configuration) =>
        Defines(EvaluateOne(project.Path, configuration, ["DefineConstants", "BroilerHostCultureRules"], []).Property("DefineConstants"));

    internal static string[] Defines(string value) =>
        [.. value.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
    internal static string GlobalConfig => System.IO.Path.Combine(Root, EngineFolder, "host-culture.globalconfig");

    internal static string BannedSymbols => System.IO.Path.Combine(Root, EngineFolder, "BannedSymbols.HostCulture.txt");

    private static readonly Lazy<IReadOnlyList<EvaluatedProject>> All = new(static () => Evaluate(Discover()));

    /// <summary>Every project of the solution and every project file below Broiler.JS/, evaluated, in ordinal order of path.</summary>
    internal static IReadOnlyList<EvaluatedProject> Projects => All.Value;

    internal static string Relative(string path) =>
        System.IO.Path.GetRelativePath(Root, path).Replace(System.IO.Path.DirectorySeparatorChar, '/');

    /// <summary>The project paths the scope reads: the solution's projects and every project file below Broiler.JS/.</summary>
    internal static IReadOnlyList<string> Discover()
    {
        var paths = new SortedSet<string>(StringComparer.Ordinal);
        var solution = XDocument.Load(System.IO.Path.Combine(Root, SolutionFile));
        foreach (var project in solution.Descendants("Project"))
        {
            var path = (string?)project.Attribute("Path");
            if (path is not null)
                paths.Add(System.IO.Path.GetFullPath(System.IO.Path.Combine(Root, path.Replace('\\', System.IO.Path.DirectorySeparatorChar))));
        }

        foreach (var path in Directory.EnumerateFiles(System.IO.Path.Combine(Root, EngineFolder), "*.csproj", SearchOption.AllDirectories))
        {
            var segments = Relative(path).Split('/');
            if (!segments.Any(static s => s is "bin" or "obj"))
                paths.Add(System.IO.Path.GetFullPath(path));
        }

        return [.. paths];
    }

    /// <summary>What a project is, from what it declares (<see cref="ProjectKind"/>); never from the property the scope sets.</summary>
    internal static ProjectKind Classify(EvaluatedProject project, IReadOnlyCollection<string> samples)
    {
        var relative = Relative(project.Path);
        if (relative.StartsWith(SharedFolder + "/", StringComparison.Ordinal))
            return ProjectKind.Shared;
        if (IsTrue(project.Property("IsTestProject")))
            return ProjectKind.Test;
        if (project.Identities("PackageReference").Contains("BenchmarkDotNet", StringComparer.OrdinalIgnoreCase))
            return ProjectKind.Benchmark;
        if (IsTrue(project.Property("EnforceExtendedAnalyzerRules")))
            return ProjectKind.Tool;
        if (samples.Contains(relative))
            return ProjectKind.Sample;
        return ProjectKind.Product;
    }

    /// <summary>
    /// Every way the evaluated projects depart from the scope: a product project the rules do not hold, a project of any
    /// other kind they hold, a listed sample that does not exist, and a sample a product project references.
    /// </summary>
    internal static IEnumerable<string> ScopeViolations(IReadOnlyList<EvaluatedProject> projects, IReadOnlyCollection<string> samples, string globalConfig, string bannedSymbols)
    {
        foreach (var sample in samples)
        {
            if (!projects.Any(p => Relative(p.Path) == sample))
                yield return sample + " is listed as a sample and is not a project of " + SolutionFile + " or below " + EngineFolder + "/";
        }

        foreach (var project in projects)
        {
            var kind = Classify(project, samples);
            var relative = Relative(project.Path);
            var rules = project.Property("BroilerHostCultureRules");
            var config = project.FullPaths("GlobalAnalyzerConfigFiles").Contains(globalConfig, StringComparer.Ordinal);
            var banned = project.FullPaths("AdditionalFiles").Contains(bannedSymbols, StringComparer.Ordinal);
            var analyzer = project.Identities("PackageReference").Contains("Microsoft.CodeAnalysis.BannedApiAnalyzers", StringComparer.OrdinalIgnoreCase);
            if (kind == ProjectKind.Product)
            {
                if (!IsTrue(rules))
                    yield return relative + " is a product project and evaluates BroilerHostCultureRules=" + Shown(rules);
                if (!config)
                    yield return relative + " is a product project and the compiler is not given host-culture.globalconfig";
                if (!banned || !analyzer)
                    yield return relative + " is a product project and the banned-API analyzer does not read BannedSymbols.HostCulture.txt";
                foreach (var silenced in Silenced(project, globalConfig))
                    yield return relative + " is a product project and " + silenced;
                continue;
            }

            if (IsTrue(rules) || config || banned)
                yield return relative + " is a " + kind.ToString().ToLowerInvariant() + " project and the host-culture rules hold it (BroilerHostCultureRules=" + Shown(rules) + ")";

            if (kind == ProjectKind.Sample)
            {
                foreach (var referrer in projects.Where(p => Classify(p, samples) == ProjectKind.Product))
                {
                    foreach (var reference in referrer.FullPaths("ProjectReference"))
                    {
                        if (string.Equals(System.IO.Path.GetFullPath(reference), project.Path, StringComparison.Ordinal))
                            yield return relative + " is listed as a sample and the product project " + Relative(referrer.Path) + " references it";
                    }
                }
            }
        }

    }

    /// <summary>
    /// Each way a product project's own configuration silences a host-culture rule: <c>NoWarn</c> naming it, analyzers turned
    /// off, or a severity for it in an analyzer config other than <c>host-culture.globalconfig</c>, a global one the project
    /// is given or an <c>.editorconfig</c> at or above its folder.
    /// </summary>
    internal static IEnumerable<string> Silenced(EvaluatedProject project, string globalConfig)
    {
        var noWarn = Defines(project.Property("NoWarn"));
        foreach (var rule in HostCultureRules.Where(r => noWarn.Contains(r, StringComparer.OrdinalIgnoreCase)))
            yield return "NoWarn names " + rule;
        foreach (var switchName in (string[])["RunAnalyzers", "RunAnalyzersDuringBuild"])
        {
            if (string.Equals(project.Property(switchName).Trim(), "false", StringComparison.OrdinalIgnoreCase))
                yield return switchName + " is false";
        }

        var configs = project.FullPaths("GlobalAnalyzerConfigFiles").Where(p => !string.Equals(p, globalConfig, StringComparison.Ordinal)).ToList();
        for (var directory = new DirectoryInfo(System.IO.Path.GetDirectoryName(project.Path)!); directory is not null; directory = directory.Parent)
        {
            var editorConfig = System.IO.Path.Combine(directory.FullName, ".editorconfig");
            if (!File.Exists(editorConfig))
                continue;
            configs.Add(editorConfig);
            if (File.ReadLines(editorConfig).Any(static l => Regex.IsMatch(l, @"^\s*root\s*=\s*true\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)))
                break;
        }

        foreach (var config in configs.Where(File.Exists))
        {
            var number = 0;
            foreach (var line in File.ReadLines(config))
            {
                number++;
                var entry = Regex.Match(line, @"^\s*dotnet_(analyzer_)?diagnostic\.([^.\s]+)\.severity\s*=", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (entry.Success && (HostCultureRules.Contains(entry.Groups[2].Value, StringComparer.OrdinalIgnoreCase)
                    || string.Equals(entry.Groups[2].Value, "category-Globalization", StringComparison.OrdinalIgnoreCase)))
                    yield return Relative(config) + ":" + number.ToString(System.Globalization.CultureInfo.InvariantCulture) + " sets a severity for " + entry.Groups[2].Value;
            }
        }
    }

    internal static bool IsTrue(string value) => string.Equals(value.Trim(), "true", StringComparison.OrdinalIgnoreCase);

    private static string Shown(string value) => value.Length == 0 ? "(empty)" : value;

    /// <summary>Evaluates each project with MSBuild, a few at a time.</summary>
    internal static IReadOnlyList<EvaluatedProject> Evaluate(IReadOnlyList<string> paths)
    {
        var results = new ConcurrentDictionary<string, EvaluatedProject>(StringComparer.Ordinal);
        var failures = new ConcurrentBag<string>();
        Parallel.ForEach(paths, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Math.Min(4, Environment.ProcessorCount)) }, path =>
        {
            try
            {
                results[path] = EvaluateOne(path, Configuration, EvaluatedProperties, EvaluatedItems);
            }
            catch (InvalidOperationException exception)
            {
                failures.Add(Relative(path) + ": " + exception.Message);
            }
        });
        if (!failures.IsEmpty)
            throw new InvalidOperationException("MSBuild did not evaluate every project:\n" + string.Join("\n", failures.Order(StringComparer.Ordinal)));
        return [.. paths.Select(p => results[p])];
    }

    /// <summary>One project's evaluation; at least two properties are asked for, so that MSBuild answers in JSON.</summary>
    private static EvaluatedProject EvaluateOne(string path, string configuration, IReadOnlyList<string> properties, IReadOnlyList<string> itemNames)
    {
        var arguments = new List<string> { "msbuild", path, "-nologo", "-p:Configuration=" + configuration };
        arguments.AddRange(properties.Select(static p => "-getProperty:" + p));
        arguments.AddRange(itemNames.Select(static i => "-getItem:" + i));
        var output = Run(arguments, System.IO.Path.GetDirectoryName(path)!);
        using var json = JsonDocument.Parse(output);
        var values = json.RootElement.GetProperty("Properties").EnumerateObject()
            .ToDictionary(static p => p.Name, static p => p.Value.GetString() ?? string.Empty, StringComparer.Ordinal);
        var items = new Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, string>>>(StringComparer.Ordinal);
        if (json.RootElement.TryGetProperty("Items", out var itemTypes))
        {
            foreach (var type in itemTypes.EnumerateObject())
            {
                items[type.Name] = [.. type.Value.EnumerateArray().Select(static item => (IReadOnlyDictionary<string, string>)item.EnumerateObject()
                    .ToDictionary(static m => m.Name, static m => m.Value.ValueKind == JsonValueKind.String ? m.Value.GetString() ?? string.Empty : m.Value.ToString(), StringComparer.Ordinal))];
            }
        }

        return new EvaluatedProject(path, values, items);
    }

    private static string Dotnet => Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } host ? host : "dotnet";

    private static string Run(IEnumerable<string> arguments, string directory)
    {
        var start = new ProcessStartInfo(Dotnet)
        {
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);
        start.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        start.Environment["DOTNET_NOLOGO"] = "1";
        using var process = Process.Start(start)!;
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException("dotnet " + string.Join(' ', arguments) + " exited " + process.ExitCode + ":\n" + output + error.Result);
        return output;
    }

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(System.IO.Path.Combine(directory.FullName, SolutionFile)))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException("no " + SolutionFile + " above " + AppContext.BaseDirectory);
    }
}
