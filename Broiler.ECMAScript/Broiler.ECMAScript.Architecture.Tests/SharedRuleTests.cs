using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Broiler.ECMAScript.Conformance;
using Broiler.ECMAScript.Runners;
using Xunit;

namespace Broiler.ECMAScript.Architecture.Tests;

/// <summary>
/// SL1 to SL10. Each rule has a clean direction over the shared folder as it stands, which also
/// asserts that the rule read a real subject, and a witness direction over an input made to break
/// it. A test method is named for its rule, so the register can find it.
/// </summary>
public class SharedRuleTests
{
    private static IReadOnlyList<AssemblyFacts> LibraryAssemblies { get; } =
        SharedTree.Libraries.Select(static p => AssemblyFacts.Read(SharedTree.AssemblyPath(p.Name))).ToList();

    [Fact]
    public void The_rules_read_every_shared_library_and_the_probe()
    {
        Assert.Equal(["Broiler.ECMAScript.Algorithms", "Broiler.ECMAScript.Conformance"], SharedTree.Libraries.Select(static p => p.Name));
        Assert.Single(SharedTree.Projects, static p => p.Kind == SharedProjectKind.Probe);
        Assert.All(LibraryAssemblies, static a => Assert.NotEmpty(a.TypeReferences));
    }

    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void SL1_No_shared_library_references_an_engine_Broiler_VM_or_a_package_outside_the_allow_list()
    {
        var violations = new List<string>();
        foreach (var library in SharedTree.Libraries)
        {
            violations.AddRange(SharedRules.SL1(library.Name, library.Directory, library.Document));
            using var assets = JsonDocument.Parse(File.ReadAllText(Path.Combine(library.Directory, "obj", "project.assets.json")));
            violations.AddRange(SharedRules.SL1Restore(library.Name, assets));
        }

        violations.AddRange(LibraryAssemblies.SelectMany(SharedRules.SL1Assembly));
        Assert.Empty(violations);
    }

    [Theory]
    [InlineData("SL1-engine-project-reference.csproj.witness", "references the project ..\\..\\Broiler.JS")]
    [InlineData("SL1-broiler-vm-package-reference.csproj.witness", "references the package Broiler.VM.Abstractions")]
    [InlineData("SL1-against-the-layering.csproj.witness", "references the package Broiler.Regex")]
    [InlineData("SL1-against-the-layering.csproj.witness", "references the package System.Text.Json")]
    [InlineData("SL1-against-the-layering.csproj.witness", "references Broiler.ECMAScript.Conformance, against the layering")]
    public void SL1_A_witness_project_is_refused(string witness, string expected)
    {
        var algorithms = SharedTree.Project("Broiler.ECMAScript.Algorithms");
        var violations = SharedRules.SL1(algorithms.Name, algorithms.Directory, XDocument.Parse(Witness.Text(witness))).ToList();
        Assert.Contains(violations, v => v.Contains(expected, StringComparison.Ordinal));
    }

    [Fact]
    public void SL1_The_assembly_half_refuses_a_reference_outside_the_framework_and_the_layering()
    {
        // This test assembly references xunit and both libraries, none of which the layering gives
        // a library, so it is the witness for the assembly half as Broiler.VM's is for B1.
        var self = AssemblyFacts.Read(typeof(SharedRuleTests).Assembly.Location);
        var violations = SharedRules.SL1Assembly(self).ToList();
        Assert.Contains(violations, static v => v.EndsWith("references the assembly xunit.core", StringComparison.Ordinal));
        Assert.Contains(violations, static v => v.EndsWith("references the assembly Broiler.ECMAScript.Conformance", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void SL2_No_shared_library_holds_mutable_static_state()
    {
        Assert.Empty(LibraryAssemblies.SelectMany(SharedRules.SL2));

        // Not vacuous: the Conformance library declares static data, immutable, which SL2 reads and passes.
        Assert.Contains(LibraryAssemblies, static a => a.StaticFields.Any(static f => f.IsReadOnly));
    }

    [Fact]
    public void SL2_The_witness_is_refused_for_each_kind_of_state()
    {
        var violations = SharedRules.SL2(Witness.Compile("SL2-mutable-static-state.cs.witness")).ToList();
        Assert.Contains(violations, static v => v.Contains("AmbientState.Calls is a mutable static field", StringComparison.Ordinal));
        Assert.Contains(violations, static v => v.Contains("AmbientState.Table is a static array", StringComparison.Ordinal));
        Assert.Contains(violations, static v => v.Contains("AmbientState.Cache is a static System.Collections.Generic.Dictionary`2", StringComparison.Ordinal));
        Assert.Contains(violations, static v => v.Contains("AmbientState.Mode is a settable static property", StringComparison.Ordinal));
        Assert.Contains(violations, static v => v.Contains("[ThreadStatic]", StringComparison.Ordinal));
        Assert.Contains(violations, static v => v.EndsWith("names System.Threading.ThreadLocal`1", StringComparison.Ordinal));
        Assert.Contains(violations, static v => v.EndsWith("names System.Threading.AsyncLocal`1", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void SL3_No_shared_library_reaches_dynamic_code_hidden_machinery_IO_or_the_environment()
    {
        Assert.Empty(LibraryAssemblies.SelectMany(static a => SharedRules.SL3(a)));
    }

    /// <summary>
    /// The probe is an executable, not a library, and reads the corpus from disk; SL2, SL3 but for
    /// I/O, and SL4 hold it all the same, since it is the code that runs under Native AOT.
    /// </summary>
    [Fact]
    public void SL3_The_probe_reaches_nothing_but_I_O()
    {
        var probe = SharedTree.Projects.Single(static p => p.Kind == SharedProjectKind.Probe);
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var image = AssemblyFacts.Read(Path.Combine(probe.Directory, "bin", configuration, "net10.0", probe.Name + ".dll"));
        Assert.Contains("System.IO.File", image.TypeReferences);
        Assert.Empty(SharedRules.SL2(image).Concat(SharedRules.SL3(image, allowIo: true)).Concat(SharedRules.SL4(image)));
    }

    [Fact]
    public void SL3_The_witness_is_refused_for_each_kind_of_machinery()
    {
        var violations = SharedRules.SL3(Witness.Compile("SL3-dynamic-code-and-io.cs.witness")).ToList();
        string[] expected =
        [
            "calls System.Activator.CreateInstance(System.Type)", "calls System.Type.GetType(System.String)",
            "calls System.Type.MakeGenericType(System.Type[])", "calls System.Enum.GetValues(System.Type)",
            "calls System.Reflection.MethodBase.Invoke(System.Object,System.Object[])", "names System.IO.File",
            "calls System.Environment.GetEnvironmentVariable(System.String): an environment read", "applies a module initializer",
            "declares the platform invoke libc!getpid",
        ];
        foreach (var fragment in expected)
            Assert.Contains(violations, v => v.Contains(fragment, StringComparison.Ordinal));

        // The probe's exemption is for I/O only: the same witness under it still fails on the rest.
        var asProbe = SharedRules.SL3(Witness.Compile("SL3-dynamic-code-and-io.cs.witness"), allowIo: true).ToList();
        Assert.DoesNotContain(asProbe, static v => v.Contains("System.IO.File", StringComparison.Ordinal));
        Assert.Contains(asProbe, static v => v.Contains("applies a module initializer", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void SL4_No_shared_library_uses_platform_globalization_regular_expressions_or_a_culture()
    {
        Assert.Empty(LibraryAssemblies.SelectMany(SharedRules.SL4));

        // Not vacuous: the Conformance library formats and parses numbers, through the invariant
        // provider, and compares strings, ordinally. SL4 reads those references and passes them.
        var conformance = LibraryAssemblies.Single(static a => a.Name == "Broiler.ECMAScript.Conformance");
        Assert.Contains("System.Globalization.NumberFormatInfo.get_InvariantInfo()", conformance.MemberReferences);
        Assert.Contains(conformance.MemberReferences, static m => m.StartsWith("System.String.StartsWith(System.String,System.StringComparison", StringComparison.Ordinal));
    }

    [Fact]
    public void SL4_The_witness_is_refused_for_each_platform_dependence()
    {
        var violations = SharedRules.SL4(Witness.Compile("SL4-platform-globalization.cs.witness")).ToList();
        string[] expected =
        [
            "names System.Text.RegularExpressions.Regex", "calls System.String.Normalize(System.Text.NormalizationForm)",
            "calls System.String.ToUpper()", "calls System.Char.IsLetter(System.Char)",
            "names System.Globalization.CultureInfo: platform globalization", "calls System.Double.Parse(System.String), which parses by the current culture",
            "calls System.String.IndexOf(System.String), which compares by the current culture",
            "interpolates a string without a format provider", "calls System.DateTime.get_Now()", "names System.TimeZoneInfo",
        ];
        foreach (var fragment in expected)
            Assert.Contains(violations, v => v.Contains(fragment, StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void SL5_Every_shared_library_and_the_probe_hold_to_AOT_and_trimming_with_nothing_suppressed()
    {
        var violations = new List<string>();
        foreach (var library in SharedTree.Libraries)
        {
            var evaluated = Tools.Evaluate(library.Path, SharedRules.LibraryProperties.Select(static p => p.Property)
                .Concat(["NoWarn", "WarningsNotAsErrors", "SuppressTrimAnalysisWarnings"]));
            violations.AddRange(SharedRules.SL5Properties(library.Name, evaluated, SharedRules.LibraryProperties));
        }

        var probe = SharedTree.Projects.Single(static p => p.Kind == SharedProjectKind.Probe);
        var probeProperties = Tools.Evaluate(probe.Path, SharedRules.ProbeProperties.Select(static p => p.Property)
            .Concat(["NoWarn", "WarningsNotAsErrors", "SuppressTrimAnalysisWarnings"]));
        violations.AddRange(SharedRules.SL5Properties(probe.Name, probeProperties, SharedRules.ProbeProperties));

        violations.AddRange(LibraryAssemblies.SelectMany(SharedRules.SL5Assembly));
        Assert.Empty(violations);
    }

    /// <summary>
    /// The inventory, in the shape of Broiler.VM's N36: every suppression in the shared folder's
    /// sources and project files is listed by file in docs/suppressions.md, and when there is none,
    /// the inventory says None.
    /// </summary>
    [Fact]
    public void SL5_The_suppression_inventory_lists_every_suppression_and_says_None_when_there_is_none()
    {
        // The shipped code and the code the probe compiles: the libraries, the probe, the runner
        // sources they link, and the folder's own build files. The test projects are the rules'
        // own sources, whose text names every marker the scan looks for.
        var scanned = SharedTree.Projects.Where(static p => p.Kind != SharedProjectKind.Test).Select(static p => p.Directory + Path.DirectorySeparatorChar)
            .Append(Path.Combine(SharedTree.Folder, "runners") + Path.DirectorySeparatorChar)
            .ToList();
        var found = SharedTree.SourceFiles("*.cs")
            .Concat(SharedTree.SourceFiles("*.csproj"))
            .Where(path => scanned.Any(directory => path.StartsWith(directory, StringComparison.Ordinal)))
            .Concat(SharedTree.SourceFiles("*.props"))
            .Concat(SharedTree.SourceFiles("*.targets"))
            .SelectMany(static path => SharedRules.Suppressions(SharedTree.Relative(path), File.ReadAllText(path)))
            .ToList();
        var inventory = File.ReadAllText(Path.Combine(SharedTree.Folder, "docs", "suppressions.md"));
        var section = inventory[inventory.IndexOf("## Inventory", StringComparison.Ordinal)..];

        if (found.Count == 0)
            Assert.Contains("\nNone.\n", section, StringComparison.Ordinal);
        foreach (var suppression in found)
            Assert.Contains(suppression.Split(':')[0], section, StringComparison.Ordinal);

        // Not vacuous: the scan read the libraries' sources and the folder's own props.
        Assert.Contains(SharedTree.SourceFiles("*.props"), static p => p.EndsWith("Directory.Build.props", StringComparison.Ordinal));
    }

    [Fact]
    public void SL5_The_witnesses_are_refused_in_the_project_in_metadata_and_in_the_inventory()
    {
        var scratch = Directory.CreateTempSubdirectory("sl5-witness-");
        try
        {
            var project = Path.Combine(scratch.FullName, "Witness.csproj");
            File.WriteAllText(project, Witness.Text("SL5-library-suppresses-a-trim-warning.csproj.witness"));
            var evaluated = Tools.Evaluate(project, SharedRules.LibraryProperties.Select(static p => p.Property)
                .Concat(["NoWarn", "WarningsNotAsErrors", "SuppressTrimAnalysisWarnings"]));
            var violations = SharedRules.SL5Properties("Witness", evaluated, SharedRules.LibraryProperties).ToList();
            Assert.Contains("Witness: NoWarn suppresses IL2026", violations);
            Assert.Contains("Witness: WarningsNotAsErrors exempts IL3050", violations);
            Assert.DoesNotContain(violations, static v => v.Contains("is '", StringComparison.Ordinal));
        }
        finally
        {
            scratch.Delete(recursive: true);
        }

        Assert.NotEmpty(SharedRules.SL5Assembly(Witness.Compile("SL5-source-suppresses-a-trim-warning.cs.witness")));
        Assert.NotEmpty(SharedRules.Suppressions("x.cs", Witness.Text("SL5-source-suppresses-a-trim-warning.cs.witness")));
        Assert.NotEmpty(SharedRules.Suppressions("x.csproj", Witness.Text("SL5-library-suppresses-a-trim-warning.csproj.witness")));
    }

    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void SL6_The_public_API_matches_the_baseline_in_both_directions()
    {
        var update = Environment.GetEnvironmentVariable(ApiListing.UpdateVariable) == "1";
        var violations = new List<string>();
        foreach (var library in SharedTree.Libraries)
        {
            var listing = ApiListing.Read(SharedTree.AssemblyPath(library.Name));
            Assert.NotEmpty(listing);
            var baseline = ApiListing.BaselinePath(library);
            if (update)
                File.WriteAllText(baseline, ApiListing.Render(library.Name, listing));
            violations.AddRange(SharedRules.SL6(library.Name, listing, ApiListing.ReadBaseline(baseline)));
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void SL6_Package_validation_is_on_for_every_packed_shared_library()
    {
        foreach (var library in SharedTree.Libraries)
        {
            var evaluated = Tools.Evaluate(library.Path, ["IsPackable", "EnablePackageValidation"]);
            if (evaluated["IsPackable"] == "true")
                Assert.True(evaluated["EnablePackageValidation"] == "true", library.Name + " is packed without package validation");
        }
    }

    [Fact]
    public void SL6_A_drift_in_either_direction_is_refused()
    {
        var listing = ApiListing.Read(Witness.Emit("SL6-api-drift.cs.witness"));
        var baseline = Witness.Text("SL6-api-drift.PublicAPI.txt.witness").Split('\n').Where(static l => l.Length > 0 && !l.StartsWith('#')).ToList();
        var violations = SharedRules.SL6("Witness", listing, baseline).ToList();
        Assert.Contains("Witness has public API the baseline does not record: Witness.Status.Overflow = 3", violations);
        Assert.Contains("Witness's baseline records public API the assembly does not have: Witness.Status.Malformed = 1", violations);
        Assert.Equal(2, violations.Count);
    }

    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void SL7_Generated_data_is_byte_identical_to_what_its_generator_writes()
    {
        var committed = Read(Path.Combine(SharedTree.Root, "conformance", "vectors", "numbers"));
        var scratch = Directory.CreateTempSubdirectory("sl7-generated-");
        try
        {
            Tools.Python(Path.Combine(SharedTree.Root, "scripts", "conformance", "generate_number_vectors.py"), ["--out", scratch.FullName]);
            var generated = Read(scratch.FullName);
            Assert.NotEmpty(generated);
            Assert.Empty(SharedRules.SL7(committed, generated));

            // The witness: a file written by hand into the generated area, and one byte changed in a generated file.
            var edited = new Dictionary<string, byte[]>(committed, StringComparer.Ordinal)
            {
                ["hand-written.vectors"] = File.ReadAllBytes(Witness.PathOf("SL7-hand-written-file-in-a-generated-area.vectors.witness")),
            };
            var first = committed.Keys.Order(StringComparer.Ordinal).First();
            var changed = (byte[])committed[first].Clone();
            changed[^2] ^= 1;
            edited[first] = changed;
            var violations = SharedRules.SL7(edited, generated).ToList();
            Assert.Contains("hand-written.vectors is committed in a generated area, and the generator does not write it", violations);
            Assert.Contains(first + " differs from what the generator writes", violations);
        }
        finally
        {
            scratch.Delete(recursive: true);
        }

        static Dictionary<string, byte[]> Read(string directory) =>
            Directory.EnumerateFiles(directory, "*.vectors").ToDictionary(static p => Path.GetFileName(p), File.ReadAllBytes, StringComparer.Ordinal);
    }

    // ---------------------------------------------------------------------------------------------

    private static readonly JsonDocumentOptions Lenient = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

    private static IEnumerable<string> LibraryProjectPaths =>
        SharedTree.Libraries.Select(static p => Path.GetRelativePath(SharedTree.Folder, p.Path).Replace(Path.DirectorySeparatorChar, '/'));

    [Fact]
    public void SL8_The_assurance_scheme_covers_every_shared_library_and_CI_gates_on_it()
    {
        using var config = JsonDocument.Parse(File.ReadAllText(Path.Combine(SharedTree.Folder, "assurance.config.json")), Lenient);
        Assert.Empty(SharedRules.SL8(config, LibraryProjectPaths));

        foreach (var artefact in (string[])["CODE-ASSURANCE.md", "HUMAN_REVIEW.md", "assurance.manifest.json"])
            Assert.True(File.Exists(Path.Combine(SharedTree.Folder, artefact)), artefact + " has not been generated");

        // Every source file of every library is in the manifest's files array, which is what makes
        // the record complete: a file the tool did not cover would be absent.
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(SharedTree.Folder, "assurance.manifest.json")));
        var files = manifest.RootElement.GetProperty("files").EnumerateArray().Select(static f => f.GetProperty("file").GetString()).ToHashSet(StringComparer.Ordinal);
        foreach (var library in SharedTree.Libraries)
        {
            foreach (var source in Directory.EnumerateFiles(library.Directory, "*.cs").Where(static p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
                Assert.Contains(Path.GetRelativePath(SharedTree.Folder, source).Replace(Path.DirectorySeparatorChar, '/'), files);
        }

        var workflow = File.ReadAllText(Path.Combine(SharedTree.Root, ".github", "workflows", "shared-libraries.yml"));
        Assert.Contains("assurance check", workflow, StringComparison.Ordinal);
        Assert.Contains("--root Broiler.JS/Broiler.ECMAScript", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void SL8_The_witness_configuration_is_refused()
    {
        using var config = JsonDocument.Parse(Witness.Text("SL8-config-omits-a-library.json.witness"), Lenient);
        var violations = SharedRules.SL8(config, LibraryProjectPaths).ToList();
        Assert.Contains("assurance.config.json does not cover Broiler.ECMAScript.Conformance/Broiler.ECMAScript.Conformance.csproj", violations);
        Assert.Contains("assurance.config.json does not forbid preprocessor directives", violations);
        Assert.Contains("assurance.config.json does not use the strict exemption predicate", violations);
    }

    // ---------------------------------------------------------------------------------------------

    /// <summary>One run of the corpus through a library: the corpus read and validated afresh, then the library-level runner's report.</summary>
    private static string Run(IVectorLibrary library)
    {
        var corpus = CorpusDirectory.Load(CorpusDirectory.Home(AppContext.BaseDirectory));
        var builder = new StringBuilder();
        foreach (var diagnostic in corpus.Diagnostics)
            builder.Append(diagnostic).Append('\n');
        builder.Append(VectorRun.Report(LibraryRun.Run(corpus.Records, library), library.Name, corpus.Sources));
        return builder.ToString();
    }

    [Fact]
    public async Task SL9_Two_runs_and_two_threads_over_the_corpus_give_identical_output()
    {
        string[] sequential = [Run(new EchoLibrary()), Run(new EchoLibrary())];
        var threads = await Task.WhenAll(Task.Run(() => Run(new EchoLibrary())), Task.Run(() => Run(new EchoLibrary())));
        string[] runs = [.. sequential, .. threads];

        Assert.Empty(SharedRules.SL9(runs));
        Assert.True(runs[0].Length > 10_000, "the runs are over the whole corpus");
    }

    [Fact]
    public void SL9_The_witness_library_is_refused()
    {
        var witness = new NondeterministicLibraryWitness();
        Assert.NotEmpty(SharedRules.SL9([Run(witness), Run(witness)]));
    }

    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void SL10_No_shared_source_holds_a_preprocessor_directive_or_a_CR()
    {
        var sources = SharedTree.SourceFiles("*.cs").ToList();
        Assert.Contains(sources, static s => s.EndsWith("WorkCharge.cs", StringComparison.Ordinal));
        Assert.Empty(sources.SelectMany(static p => SharedRules.SL10Directives(SharedTree.Relative(p), File.ReadAllText(p))));

        var text = SharedTree.SourceFiles("*").Where(static p => !p.EndsWith(".png", StringComparison.Ordinal)).ToList();
        Assert.Empty(text.SelectMany(static p => SharedRules.SL10LineEndings(SharedTree.Relative(p), File.ReadAllBytes(p))));

        // Read by line: Git checks .gitattributes out with the platform's line endings.
        var attributes = File.ReadAllLines(Path.Combine(SharedTree.Root, ".gitattributes"));
        Assert.Contains("Broiler.ECMAScript/** text eol=lf", attributes);
    }

    [Fact]
    public void SL10_The_witnesses_are_refused()
    {
        var directives = SharedRules.SL10Directives("w.cs", Witness.Text("SL10-preprocessor-directive.cs.witness")).ToList();
        Assert.Equal(["w.cs:7: #if DEBUG", "w.cs:9: #else", "w.cs:11: #endif", "w.cs:13: #pragma warning disable CS0618", "w.cs:15: #pragma warning restore CS0618"], directives);

        Assert.NotEmpty(SharedRules.SL10LineEndings("w.cs", File.ReadAllBytes(Witness.PathOf("SL10-crlf-line-endings.cs.witness"))));
    }
}

/// <summary>A deterministic library for SL9: it renders its first input back, so the run exercises the decoder and the renderer on every vector.</summary>
internal sealed class EchoLibrary : IVectorLibrary
{
    public string Name => "echo";

    public string Call(string operation, IReadOnlyList<TypedValue> inputs) =>
        inputs.Count == 0 ? "status: no-input" : "value: " + inputs[0].Kind switch
        {
            TypedValueKind.Number or TypedValueKind.Integer => TypedValue.RenderNumber(inputs[0].Kind == TypedValueKind.Number ? inputs[0].Number : (double)inputs[0].Integer),
            TypedValueKind.String => TypedValue.RenderString(inputs[0].Chars),
            TypedValueKind.BigInt => TypedValue.RenderBigInt(inputs[0].Integer),
            TypedValueKind.Boolean => TypedValue.RenderBoolean(inputs[0].Boolean),
            _ => inputs[0].Text,
        };
}

/// <summary>
/// SL9's witness: a library whose answer depends on process history, a counter, which is what
/// convention 6 forbids. Two runs through it differ.
/// </summary>
internal sealed class NondeterministicLibraryWitness : IVectorLibrary
{
    private int calls;

    public string Name => "nondeterministic";

    public string Call(string operation, IReadOnlyList<TypedValue> inputs) =>
        "value: " + TypedValue.RenderNumber(++calls);
}
