using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Broiler.ECMAScript.Architecture.Tests;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using Xunit.Abstractions;

namespace Broiler.JavaScript.Architecture.Tests;

/// <summary>
/// The parser's architecture test: part B of bundle sjl-12-001's rule
/// (<c>docs/evidence/sjl-12-001/decision-rule.md</c>), over the <c>Broiler.JavaScript.Parser</c>
/// assembly beside this one, or the one <c>BROILER_ARCH_PARSER_ASSEMBLY</c> names.
/// </summary>
/// <remarks>
/// The test asserts the exact list of the parser's violations, <see cref="TodaysViolations"/>. Each
/// item of SJL-12.5 that removes a violation removes its line in the same change, so the list only
/// shrinks, and at the item that lands the for-loop pass it is empty and the test asserts none. A
/// violation the list does not hold fails the test, and so does a listed one the parser no longer
/// has. Every violation the run finds is written to the test's output and named in a failure.
/// </remarks>
public sealed class ParserArchitectureTests(ITestOutputHelper output)
{
    private const string Assembly = Subject.AssemblyName;

    /// <summary>
    /// The violations the parser has today, each under the item of SJL-12.5 (the milestone's
    /// numbering, Broiler.VM's JSD-0080) that removes it.
    /// </summary>
    private static readonly string[] TodaysViolations =
    [
        // Item 4 (decision D20) removed the reference to Runtime: the Unicode and regular-expression
        // types the parser read there moved to Ast, and item 1 (decision D19), the explicit options,
        // had already removed its reads of CoreScript's ambient goal and top-level-await scopes.

        // Item 5 (decision D22), the for-loop desugaring moves to the compiler's pass.
        Assembly + " names the desugaring walker Broiler.JavaScript.Ast.AstIdentifierReplacer",
        Assembly + " names the desugaring walker Broiler.JavaScript.Ast.ClosureDetector",
        Assembly + " names the desugaring walker Broiler.JavaScript.Ast.IdentifierNameCollector",
        ParserFixtures.ForLetViolation,
    ];

    private static readonly Lazy<ClauseReading> WitnessReading = new(static () =>
        ParserClauses.Read(WitnessImage.Compile("parser-ambient-state.cs.witness"), AppContext.BaseDirectory));

    [Fact]
    public void The_parser_has_exactly_the_listed_violations()
    {
        var path = Subject.Locate();
        output.WriteLine("subject: " + path + (Subject.Named is null ? "" : " (named by " + Subject.Variable + ")"));

        var reading = ParserClauses.Read(File.ReadAllBytes(path), Path.GetDirectoryName(path)!);
        Assert.Equal(Assembly, reading.Assembly);

        // The metadata clauses' violations are written before a fixture runs, and a fixture that
        // cannot run is a violation of its own rather than an exception, so the output and the
        // failure always name everything found.
        foreach (var violation in reading.Violations)
            output.WriteLine("violation: " + violation);

        var fixtures = ParserFixtures.Run(path, output.WriteLine);
        foreach (var violation in fixtures)
            output.WriteLine("violation: " + violation);

        var found = reading.Violations.Concat(fixtures).Order(StringComparer.Ordinal).ToList();
        var listed = TodaysViolations.Order(StringComparer.Ordinal).ToList();
        var unlisted = found.Except(listed, StringComparer.Ordinal).ToList();
        var gone = listed.Except(found, StringComparer.Ordinal).ToList();
        Assert.True(unlisted.Count == 0 && gone.Count == 0, Report(path, found, unlisted, gone));
    }

    /// <summary>Non-vacuity: the clauses read a real parser, and at least one InitOnly static field they accepted.</summary>
    [Fact]
    public void The_clauses_read_the_parser_and_accept_at_least_one_read_only_static_field()
    {
        var path = Subject.Locate();
        var image = File.ReadAllBytes(path);
        var reading = ParserClauses.Read(image, Path.GetDirectoryName(path)!);
        foreach (var field in reading.AcceptedReadOnlyStatics)
            output.WriteLine("accepted: " + field);

        Assert.NotEmpty(reading.AcceptedReadOnlyStatics);
        Assert.True(reading.StaticFieldsRead >= reading.AcceptedReadOnlyStatics.Count);

        var facts = AssemblyFacts.Read(image);
        Assert.Contains("Broiler.JavaScript.Ast", facts.AssemblyReferences);
        Assert.Contains(facts.TypeReferences, static t => t.StartsWith("Broiler.JavaScript.Ast.", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("a mutable static counter", "Witness.AmbientState.Parses is a mutable static field")]
    [InlineData("a static readonly field of a mutable class", "Witness.AmbientState.EndOfInput is a static readonly Broiler.JavaScript.Ast.Misc.FastToken, whose type declares the mutable instance fields Next, Previous")]
    [InlineData("a static readonly array", "Witness.AmbientState.Delimiters is a static array")]
    [InlineData("a static readonly field of a mutable collection type", "Witness.AmbientState.Seen is a static System.Collections.Generic.Dictionary`2<System.String,System.Int32>")]
    [InlineData("a [ThreadStatic] field", "Witness.AmbientState.depth is [ThreadStatic]")]
    [InlineData("an AsyncLocal", "parser_ambient_state names System.Threading.AsyncLocal`1")]
    [InlineData("a ThreadLocal", "parser_ambient_state names System.Threading.ThreadLocal`1")]
    [InlineData("a settable static property", "Witness.AmbientState.Mode is a settable static property")]
    [InlineData("a use of a Runtime type", "parser_ambient_state references the assembly Broiler.JavaScript.Runtime")]
    [InlineData("a use of a desugaring walker", "parser_ambient_state names the desugaring walker Broiler.JavaScript.Ast.AstIdentifierReplacer")]
    [InlineData("a use of a desugaring walker", "parser_ambient_state names the desugaring walker Broiler.JavaScript.Ast.IdentifierNameCollector")]
    [InlineData("a use of a desugaring walker", "parser_ambient_state names the desugaring walker Broiler.JavaScript.Ast.ClosureDetector")]
    public void The_witness_is_refused_for_each_shape(string shape, string refusal)
    {
        var violations = WitnessReading.Value.Violations;
        Assert.True(violations.Any(v => v.Contains(refusal, StringComparison.Ordinal)),
            "The witness's " + shape + " is not refused: no violation contains \"" + refusal + "\". The witness's violations:\n  "
            + string.Join("\n  ", violations));
    }

    /// <summary>
    /// <see cref="ParserClauses.MutableTypes"/> is a copy of rule SL2's list, which is private; the copy is the list.
    /// The project copies <c>SharedRules.cs</c> into its output, so the test reads it there, wherever the build put it.
    /// </summary>
    [Fact]
    public void The_list_of_mutable_types_is_rule_SL2s()
    {
        var rules = Path.Combine(AppContext.BaseDirectory, "rules", "SharedRules.cs");
        var source = File.ReadAllText(rules);
        var start = source.IndexOf("MutableTypes =", StringComparison.Ordinal);
        Assert.True(start >= 0, rules + " declares no MutableTypes");
        var end = source.IndexOf("];", start, StringComparison.Ordinal);
        var listed = Regex.Matches(source[start..end], "\"([^\"]+)\"").Select(static m => m.Groups[1].Value).ToList();
        Assert.Equal(listed, ParserClauses.MutableTypes);
    }

    private static string Report(string path, List<string> found, List<string> unlisted, List<string> gone)
    {
        var report = new StringBuilder("The parser at " + path + " does not have exactly the listed violations.\n");
        if (unlisted.Count > 0)
            report.Append("Violations the list does not hold:\n  ").AppendJoin("\n  ", unlisted).Append('\n');
        if (gone.Count > 0)
            report.Append("Listed violations the parser no longer has (remove their lines from TodaysViolations):\n  ").AppendJoin("\n  ", gone).Append('\n');
        return report.Append("Every violation found:\n  ").AppendJoin("\n  ", found).ToString();
    }
}

/// <summary>
/// Compiles a witness in memory, against the platform's assemblies and this engine's, which are beside
/// the test assembly, so that a clause that stops recognising a shape stops refusing it.
/// </summary>
internal static class WitnessImage
{
    internal static byte[] Compile(string file)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "witnesses", file);
        var name = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(file)).Replace('-', '_');
        var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), new CSharpParseOptions(LanguageVersion.Latest), path: file);
        var compilation = CSharpCompilation.Create(
            name,
            [tree],
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        if (!result.Success)
        {
            var errors = result.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error);
            throw new InvalidOperationException("the witness " + file + " does not compile:\n" + string.Join("\n", errors));
        }

        return stream.ToArray();
    }

    private static IEnumerable<MetadataReference> References()
    {
        var platform = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(static p => Path.GetFileName(p).StartsWith("System.", StringComparison.Ordinal)
                || Path.GetFileName(p) is "netstandard.dll" or "mscorlib.dll");
        var engine = Directory.EnumerateFiles(AppContext.BaseDirectory, "Broiler.*.dll")
            .Where(static p => Path.GetFileNameWithoutExtension(p) != typeof(WitnessImage).Assembly.GetName().Name);
        return platform.Concat(engine).Order(StringComparer.Ordinal).Select(static p => MetadataReference.CreateFromFile(p));
    }
}
