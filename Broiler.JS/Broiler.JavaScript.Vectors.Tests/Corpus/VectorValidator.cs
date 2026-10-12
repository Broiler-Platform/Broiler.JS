using System.Text.RegularExpressions;

namespace Broiler.JavaScript.Vectors.Tests.Corpus;

/// <summary>
/// Checks records against the schema (conformance/vectors/SCHEMA.md): the fields, the typed values
/// and outcomes, the classes, the provenance, and the revision. It refuses any vector whose
/// expected value has no source independent of both engines.
/// </summary>
public sealed partial class VectorValidator(VectorSources sources)
{
    public static readonly IReadOnlyList<string> Engines = ["broiler-js", "broiler-vm"];

    public static readonly IReadOnlySet<string> Areas = new HashSet<string>(StringComparer.Ordinal)
    {
        "numbers", "numerics", "text", "date", "unicode", "time-zones", "intl", "temporal", "regexp", "syntax", "semantics",
    };

    public static readonly IReadOnlySet<string> Classes = new HashSet<string>(StringComparer.Ordinal)
    {
        "normative", "impl-defined", "divergence",
    };

    private static readonly string[] Required = ["id", "area", "op", "in", "out", "class", "prov", "script", "since"];

    private static readonly HashSet<string> Known = [.. Required, "note"];

    private readonly Dictionary<string, VectorRecord> ids = new(StringComparer.Ordinal);

    public List<VectorDiagnostic> Diagnostics { get; } = [];

    /// <summary>Validates a file; identifiers are checked for uniqueness across every file this validator has seen.</summary>
    public void Validate(VectorFile file)
    {
        Diagnostics.AddRange(file.Diagnostics);
        foreach (var record in file.Records)
            Validate(record);
    }

    private void Validate(VectorRecord record)
    {
        void Report(string code, int line, string message) => Diagnostics.Add(new(code, record.File, line, message));

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in record.Fields)
        {
            if (!seen.Add(field.Key))
                Report("duplicate-key", field.Line, $"'{field.Key}' appears more than once in the record");
            if (Known.Contains(field.Key))
                continue;
            if (EngineField(field.Key) is { } engine)
            {
                if (!Engines.Contains(engine.Name))
                    Report("unknown-engine", field.Line, $"'{engine.Name}' is not broiler-js or broiler-vm");
                continue;
            }

            Report("unknown-key", field.Line, $"'{field.Key}' is not a key the schema defines");
        }

        foreach (var key in Required)
        {
            if (record[key] is null)
                Report("missing-field", record.Line, $"the record has no '{key}'");
        }

        int LineOf(string key) => record.Fields.FirstOrDefault(f => f.Key == key)?.Line ?? record.Line;

        var area = record["area"];
        if (area is not null && !Areas.Contains(area))
            Report("bad-area", LineOf("area"), $"'{area}' is not an area the schema defines");

        if (record["id"] is { } id)
        {
            if (!IdPattern().IsMatch(id))
                Report("bad-id", LineOf("id"), $"'{id}' is not lowercase segments separated by /");
            else if (area is not null && id[..id.IndexOf('/')] != area)
                Report("bad-id", LineOf("id"), $"'{id}' does not start with its area, '{area}'");
            if (ids.TryGetValue(id, out var first))
                Report("duplicate-id", LineOf("id"), $"'{id}' is also the identifier at {first.File}:{first.Line}");
            else
                ids[id] = record;
        }

        if (record["op"] is { Length: 0 })
            Report("bad-op", LineOf("op"), "the operation is empty");

        if (record["in"] is { } inputs)
            ValidateInputs(inputs, LineOf("in"), Report);

        if (record["out"] is { } expected)
            ValidateOutcome(expected, LineOf("out"), Report);

        var @class = record["class"];
        if (@class is not null && !Classes.Contains(@class))
            Report("bad-class", LineOf("class"), $"'{@class}' is not normative, impl-defined or divergence");

        var perEngine = record.Fields.Where(f => EngineField(f.Key) is { Kind: "per-engine" }).ToList();
        var divergences = record.Fields.Where(f => EngineField(f.Key) is { Kind: "divergence" }).ToList();
        foreach (var field in perEngine)
        {
            if (@class != "impl-defined")
                Report("misplaced-field", field.Line, $"'{field.Key}' belongs to an impl-defined vector only");
            ValidateOutcome(field.Value, field.Line, Report);
        }

        foreach (var field in divergences)
        {
            // On an impl-defined vector the field classifies a difference from the engine's own
            // per-engine expectation, so that expectation must be there (SCHEMA.md section 5).
            if (@class == "impl-defined" ? record["per-engine." + field.Key["divergence.".Length..]] is null : @class != "divergence")
                Report("misplaced-field", field.Line, $"'{field.Key}' belongs to a divergence vector, or to an impl-defined one beside its per-engine field");
            if (string.IsNullOrWhiteSpace(field.Value))
                Report("bad-divergence", field.Line, $"'{field.Key}' gives no reason");
        }

        if (@class == "impl-defined" && perEngine.Count == 0)
            Report("impl-defined-without-expectation", LineOf("class"), "an impl-defined vector names no per-engine expectation");
        if (@class == "divergence" && divergences.Count == 0)
            Report("divergence-without-engine", LineOf("class"), "a divergence vector names no engine");

        if (record["prov"] is { } provenance)
            ValidateProvenance(provenance, LineOf("prov"), Report);

        if (record["script"] is { } script && string.IsNullOrWhiteSpace(script))
            Report("bad-script", LineOf("script"), "the script is empty");

        if (record["since"] is { } since)
        {
            if (!SincePattern().IsMatch(since))
                Report("bad-since", LineOf("since"), $"'{since}' is not a positive integer");
            else if (!int.TryParse(since, out var revision) || revision > sources.Revision)
                Report("future-revision", LineOf("since"), $"'{since}' is beyond the corpus's revision, {sources.Revision}");
        }
    }

    private static void ValidateInputs(string inputs, int line, Action<string, int, string> report)
    {
        var position = 0;
        while (true)
        {
            if (!TypedValue.TryReadToken(inputs, ref position, out var token))
            {
                report("bad-value", line, $"'{inputs}' is not typed values separated by single spaces");
                return;
            }

            if (!TypedValue.TryParse(token, out var value, out var code, out var message))
                report(code, line, message);
            else if (value.Kind is TypedValueKind.Symbol or TypedValueKind.Object)
                report("bad-value", line, $"'{token}' is an outcome notation, not an input");

            if (position == inputs.Length)
                return;
            if (inputs[position] != ' ')
            {
                report("bad-value", line, $"'{inputs}' does not separate its values by single spaces");
                return;
            }

            position++;
        }
    }

    private static void ValidateOutcome(string text, int line, Action<string, int, string> report)
    {
        if (!Outcome.TryParseAlternatives(text, out _, out var code, out var message))
            report(code, line, message);
    }

    private void ValidateProvenance(string provenance, int line, Action<string, int, string> report)
    {
        var entries = provenance.Split(' ');
        var independent = false;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            if (!seen.Add(entry))
            {
                report("bad-provenance", line, $"'{entry}' is listed twice");
                continue;
            }

            var match = ProvenancePattern().Match(entry);
            if (!match.Success)
            {
                report("bad-provenance", line, $"'{entry}' is not a provenance entry the schema defines");
                continue;
            }

            var kind = match.Groups["kind"].Value;
            if (kind == "spec" && !sources.SpecificationRevision.StartsWith(match.Groups["revision"].Value, StringComparison.Ordinal))
                report("bad-provenance", line, $"'{entry}' does not name the pinned specification revision, {sources.SpecificationRevision}");
            else if (kind == "ucd" && match.Groups["version"].Value != sources.UnicodeVersion)
                report("bad-provenance", line, $"'{entry}' does not name the pinned Unicode version, {sources.UnicodeVersion}");
            else if (kind == "observed" && !Engines.Contains(match.Groups["engine"].Value))
                report("bad-provenance", line, $"'{entry}' does not name broiler-js or broiler-vm");

            if (kind != "observed")
                independent = true;
        }

        if (!independent)
            report("engine-observed", line, "the expected value has no source independent of both engines");
    }

    private static (string Kind, string Name)? EngineField(string key)
    {
        foreach (var kind in (ReadOnlySpan<string>)["per-engine", "divergence"])
        {
            if (key.Length > kind.Length + 1 && key.StartsWith(kind, StringComparison.Ordinal) && key[kind.Length] == '.')
                return (kind, key[(kind.Length + 1)..]);
        }

        return null;
    }

    [GeneratedRegex("^[a-z0-9.-]+(/[a-z0-9.-]+)+$")]
    private static partial System.Text.RegularExpressions.Regex IdPattern();

    [GeneratedRegex("^[1-9][0-9]*$")]
    private static partial System.Text.RegularExpressions.Regex SincePattern();

    [GeneratedRegex(
        "^(?:(?<kind>spec):[a-z0-9]+@(?<revision>[0-9a-f]{7,40})#[a-z0-9.-]+"
        + "|(?<kind>test262):[A-Za-z0-9_./-]+\\.js"
        + "|(?<kind>ref):[a-z0-9-]+@[0-9]+"
        + "|(?<kind>ucd):(?<version>[0-9]+\\.[0-9]+\\.[0-9]+)/[A-Za-z0-9_.-]+"
        + "|(?<kind>observed):(?<engine>[a-z0-9-]+))$")]
    private static partial System.Text.RegularExpressions.Regex ProvenancePattern();
}
