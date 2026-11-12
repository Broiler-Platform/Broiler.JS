using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Broiler.JavaScript.Architecture.Tests;

/// <summary>A product project compiled in memory from its own sources, and the readers found in them.</summary>
internal sealed record ProjectReading(
    EvaluatedProject Project,
    int Sources,
    int Generated,
    IReadOnlyList<string> Errors,
    IReadOnlyList<ReaderFinding> Findings,
    IReadOnlyList<string> Kept,
    IReadOnlyList<string> Suppressions,
    IReadOnlyList<string> OtherSuppressions,
    IReadOnlyList<string> ReadInOtherConfiguration);

/// <summary>
/// Compiles a product project in memory as its build does: its <c>Compile</c> items, language version, preprocessor
/// symbols, nullable context, unsafe switch, assembly name and source generators, all as MSBuild evaluates them, against
/// the platform's assemblies and the compile assets of its restore (the test project references every product project of
/// the solution and the source generator, so that their builds exist when it runs). A source whose code differs under the
/// other configuration's preprocessor symbols (<see cref="HostCultureProjects.OtherConfiguration"/>, as MSBuild evaluates
/// them) is read under those too, in a second compilation that differs from the first by those sources alone.
/// </summary>
internal static class HostCultureCompilation
{
    private static readonly ConcurrentDictionary<string, IReadOnlyList<ISourceGenerator>> GeneratorsByPath = new(StringComparer.Ordinal);

    internal static ProjectReading Read(EvaluatedProject project, IReadOnlyList<EvaluatedProject> all)
    {
        var parse = new CSharpParseOptions(Language(project.Property("LangVersion")), preprocessorSymbols: HostCultureProjects.Defines(project.Property("DefineConstants")));
        var sources = project.FullPaths("Compile")
            .Where(static p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)
            .Select(path =>
            {
                using var stream = File.OpenRead(path);
                return CSharpSyntaxTree.ParseText(SourceText.From(stream), parse, path);
            })
            .ToList();

        var name = project.Property("AssemblyName");
        var options = new CSharpCompilationOptions(
            string.Equals(project.Property("OutputType").Trim(), "Exe", StringComparison.OrdinalIgnoreCase)
                ? OutputKind.ConsoleApplication
                : OutputKind.DynamicallyLinkedLibrary,
            nullableContextOptions: Nullable(project.Property("Nullable")),
            allowUnsafe: HostCultureProjects.IsTrue(project.Property("AllowUnsafeBlocks")));
        var source = CSharpCompilation.Create(name, sources, References(project, all), options);
        var generators = Generators(project, all);
        GeneratorDriver? driver = generators.Count > 0 ? CSharpGeneratorDriver.Create(generators, parseOptions: parse) : null;
        var compilation = Generate(ref driver, source);

        // The errors asserted are the compilation's syntax and declaration errors, every source and generated source's, and
        // the errors of each source the reading binds, bodies included; a source the reading does not bind, which holds no
        // node a reader could be, is not bound for its bodies' errors (the engine's generated Unicode tables alone would
        // take most of the test's time).
        static string PathOf(SyntaxTree tree) => HostCultureProjects.Relative(tree.FilePath);
        var findings = HostCultureReaders.Read(compilation, sources, PathOf, out var boundErrors);
        var errors = compilation.GetParseDiagnostics().Concat(compilation.GetDeclarationDiagnostics())
            .Where(static d => d.Severity == DiagnosticSeverity.Error)
            .Select(static d => d.ToString())
            .Concat(boundErrors)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var kept = new List<string>();
        var suppressions = new List<string>();
        var others = new List<string>();
        foreach (var tree in sources)
        {
            var path = PathOf(tree);
            var root = tree.GetRoot();
            kept.AddRange(HostCultureReaders.KeptRegions(root).Select(r => path + ":" + r.Start.ToString(CultureInfo.InvariantCulture)));
            suppressions.AddRange(HostCultureReaders.Rs0030Disables(root).Select(line => path + ":" + line.ToString(CultureInfo.InvariantCulture)));
            others.AddRange(HostCultureReaders.OtherRs0030Suppressions(root).Select(s => path + ":" + s.Line.ToString(CultureInfo.InvariantCulture) + ": " + s.Form));
        }

        // The other configuration: only a source with a conditional directive can read differently, and only those whose
        // code (their tokens, not their trivia) differs are parsed again and read again.
        var differing = new List<(SyntaxTree Release, SyntaxTree Other)>();
        if (sources.Any(static t => HasConditional(t)))
        {
            var otherParse = parse.WithPreprocessorSymbols(HostCultureProjects.DefinesIn(project, HostCultureProjects.OtherConfiguration));
            foreach (var tree in sources.Where(static t => HasConditional(t)))
            {
                var other = CSharpSyntaxTree.ParseText(tree.GetText(), otherParse, tree.FilePath);
                if (!other.GetRoot().DescendantTokens().Select(static t => t.Text).SequenceEqual(tree.GetRoot().DescendantTokens().Select(static t => t.Text), StringComparer.Ordinal))
                    differing.Add((tree, other));
            }

            if (differing.Count > 0)
            {
                var otherSource = differing.Aggregate((Compilation)source, static (c, d) => c.ReplaceSyntaxTree(d.Release, d.Other));
                driver = driver?.WithUpdatedParseOptions(otherParse);
                var otherCompilation = Generate(ref driver, otherSource);
                var configuration = " (in " + HostCultureProjects.OtherConfiguration + ")";
                var known = findings.Select(static f => (f.Path, f.Line, f.Kind)).ToHashSet();
                findings.AddRange(HostCultureReaders.Read(otherCompilation, differing.Select(static d => d.Other), PathOf, out var otherErrors)
                    .Where(f => !known.Contains((f.Path, f.Line, f.Kind)))
                    .Select(f => f with { Detail = f.Detail + configuration }));
                errors.AddRange(otherCompilation.GetDeclarationDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error)
                    .Select(static d => d.ToString()).Concat(otherErrors).Distinct(StringComparer.Ordinal).Except(errors, StringComparer.Ordinal)
                    .Select(e => e + configuration).ToList());
            }
        }

        return new ProjectReading(project, sources.Count, compilation.SyntaxTrees.Count() - sources.Count, errors, findings, kept, suppressions, others,
            [.. differing.Select(d => PathOf(d.Release))]);
    }

    private static Compilation Generate(ref GeneratorDriver? driver, Compilation compilation)
    {
        if (driver is null)
            return compilation;
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updated, out _);
        return updated;
    }

    private static bool HasConditional(SyntaxTree tree) =>
        HostCultureReaders.Directives(tree.GetRoot()).Any(static d => d is IfDirectiveTriviaSyntax or ElifDirectiveTriviaSyntax);

    private static LanguageVersion Language(string value) =>
        LanguageVersionFacts.TryParse(value, out var version) ? version : LanguageVersion.Latest;

    private static NullableContextOptions Nullable(string value) => value.Trim().ToLowerInvariant() switch
    {
        "enable" => NullableContextOptions.Enable,
        "warnings" => NullableContextOptions.Warnings,
        "annotations" => NullableContextOptions.Annotations,
        _ => NullableContextOptions.Disable,
    };

    /// <summary>
    /// The assemblies the project's build compiles against: the platform's, and the compile assets its restore wrote to
    /// <c>project.assets.json</c>, each package's from the package folder and each project's from that project's build.
    /// </summary>
    private static List<MetadataReference> References(EvaluatedProject project, IReadOnlyList<EvaluatedProject> all)
    {
        var platform = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(p => string.Equals(Path.GetDirectoryName(p), platform, StringComparison.Ordinal))
            .ToList();

        var assets = project.Property("ProjectAssetsFile");
        if (!File.Exists(assets))
        {
            // A project in no solution is not restored by the solution's build (Broiler.JavaScript.Network and
            // Broiler.JavaScript.NodePollyfill, which do not compile): it is read against the builds of the projects it
            // references and what they compile against, and its errors are shown, not asserted.
            var closure = new List<MetadataReference>(paths.Select(static p => (MetadataReference)MetadataReference.CreateFromFile(p)));
            foreach (var reference in project.ItemsOf("ProjectReference"))
            {
                if (reference.TryGetValue("OutputItemType", out var itemType) && string.Equals(itemType, "Analyzer", StringComparison.OrdinalIgnoreCase))
                    continue;
                var referencePath = Path.GetFullPath(reference.TryGetValue("FullPath", out var full) ? full : Path.Combine(Path.GetDirectoryName(project.Path)!, reference["Identity"]));
                if (all.FirstOrDefault(p => p.Path == referencePath) is not { } referenced)
                    continue;
                if (File.Exists(referenced.Property("TargetPath")))
                    closure.Add(MetadataReference.CreateFromFile(referenced.Property("TargetPath")));
                closure.AddRange(References(referenced, all));
            }

            return [.. closure.OfType<PortableExecutableReference>().DistinctBy(static r => r.FilePath, StringComparer.Ordinal)
                .OrderBy(static r => r.FilePath, StringComparer.Ordinal)];
        }

        using var json = JsonDocument.Parse(File.ReadAllText(assets));
        var folder = json.RootElement.GetProperty("packageFolders").EnumerateObject().First().Name;
        var libraries = json.RootElement.GetProperty("libraries");
        var target = json.RootElement.GetProperty("targets").EnumerateObject()
            .First(t => string.Equals(t.Name, project.Property("TargetFramework"), StringComparison.OrdinalIgnoreCase) || t.Name.StartsWith(".NETCoreApp", StringComparison.Ordinal));
        foreach (var library in target.Value.EnumerateObject())
        {
            if (!library.Value.TryGetProperty("compile", out var compile))
                continue;
            var type = library.Value.GetProperty("type").GetString();
            foreach (var asset in compile.EnumerateObject().Select(static a => a.Name).Where(static a => a.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)))
            {
                if (type == "package")
                {
                    paths.Add(Path.GetFullPath(Path.Combine(folder, libraries.GetProperty(library.Name).GetProperty("path").GetString()!, asset)));
                }
                else
                {
                    var name = Path.GetFileNameWithoutExtension(asset);
                    var built = all.FirstOrDefault(p => p.Property("AssemblyName") == name)?.Property("TargetPath");
                    paths.Add(built is { Length: > 0 } && File.Exists(built) ? built : Path.Combine(AppContext.BaseDirectory, name + ".dll"));
                }
            }
        }

        var missing = paths.Where(static p => !File.Exists(p)).ToList();
        if (missing.Count > 0)
            throw new FileNotFoundException(HostCultureProjects.Relative(project.Path) + " compiles against assemblies that are not built or restored:\n" + string.Join("\n", missing));
        return [.. paths.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).Select(static p => (MetadataReference)MetadataReference.CreateFromFile(p))];
    }

    /// <summary>The source generators the project's build runs: each project it references as an analyzer, from that project's build output.</summary>
    private static List<ISourceGenerator> Generators(EvaluatedProject project, IReadOnlyList<EvaluatedProject> all)
    {
        var generators = new List<ISourceGenerator>();
        foreach (var reference in project.ItemsOf("ProjectReference"))
        {
            if (!reference.TryGetValue("OutputItemType", out var itemType) || !string.Equals(itemType, "Analyzer", StringComparison.OrdinalIgnoreCase))
                continue;
            var path = Path.GetFullPath(reference.TryGetValue("FullPath", out var full) ? full : Path.Combine(Path.GetDirectoryName(project.Path)!, reference["Identity"]));
            var generatorProject = all.FirstOrDefault(p => p.Path == path)
                ?? throw new InvalidOperationException(HostCultureProjects.Relative(project.Path) + " runs the generator " + HostCultureProjects.Relative(path) + ", which the scope did not evaluate");
            var assembly = generatorProject.Property("TargetPath");
            if (!File.Exists(assembly))
                throw new FileNotFoundException("the source generator " + generatorProject.Name + " is not built; the test project references it so that it is", assembly);
            generators.AddRange(GeneratorsByPath.GetOrAdd(assembly, static a => Load(a)));
        }

        return generators;
    }

    private static IReadOnlyList<ISourceGenerator> Load(string path)
    {
        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
        var generators = new List<ISourceGenerator>();
        foreach (var type in assembly.GetTypes().Where(static t => !t.IsAbstract && t.GetCustomAttribute<GeneratorAttribute>() is not null))
        {
            var instance = Activator.CreateInstance(type);
            if (instance is IIncrementalGenerator incremental)
                generators.Add(incremental.AsSourceGenerator());
            else if (instance is ISourceGenerator source)
                generators.Add(source);
        }

        return generators;
    }
}
