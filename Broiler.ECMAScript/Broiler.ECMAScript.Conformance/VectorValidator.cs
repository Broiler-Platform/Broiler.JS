// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   20
// Annotated:        20/20
// Exempt:           4
// Human-reviewed:   0/20
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  9/10 max
// Unverified:       20
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Broiler.ECMAScript.Conformance;

/// <summary>
/// Checks records against the schema (conformance/vectors/SCHEMA.md): the fields, the typed values
/// and outcomes, the classes, the provenance, and the revision. It refuses any vector whose
/// expected value has no source independent of both engines.
/// </summary>
/// <remarks>
/// One validator checks one corpus: identifiers are unique across every file it has seen, so a
/// caller validates each corpus with its own instance. An instance is not safe to share between
/// threads while it validates.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=9; Fingerprint=E7A033
// Broiler-Human:        PENDING
public sealed class VectorValidator
{
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=C119F4
    // Broiler-Human:        PENDING
    private static readonly ImmutableArray<string> Required = ["id", "area", "op", "in", "out", "class", "prov", "script", "since"];

    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=990747
    // Broiler-Human:        PENDING
    private static readonly FrozenSet<string> Known = FrozenSet.Create(
        StringComparer.Ordinal, "id", "area", "op", "in", "out", "class", "prov", "script", "since", "note");

    private readonly VectorSources sources;

    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=B89F2C
    // Broiler-Human:        PENDING
    private readonly Dictionary<string, VectorRecord> ids = new(StringComparer.Ordinal);

    private readonly List<VectorDiagnostic> diagnostics = [];

    /// <summary>Creates a validator for a corpus at the revision and pins <paramref name="sources"/> gives.</summary>
    /// <param name="sources">What the corpus's own documents pin.</param>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=769E7F
    // Broiler-Human:        PENDING
    public VectorValidator(VectorSources sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        this.sources = sources;
    }

    /// <summary>The engines a per-engine or divergence field may name.</summary>
    public static ImmutableArray<string> Engines { get; } = ["broiler-js", "broiler-vm"];

    /// <summary>The areas the schema defines, the first segment of every identifier.</summary>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=D9548D
    // Broiler-Human:        PENDING
    public static FrozenSet<string> Areas { get; } = FrozenSet.Create(
        StringComparer.Ordinal,
        "numbers", "numerics", "text", "date", "unicode", "time-zones", "intl", "temporal", "regexp", "syntax", "semantics");

    /// <summary>The classes the schema's section 5 defines.</summary>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=FA6E87
    // Broiler-Human:        PENDING
    public static FrozenSet<string> Classes { get; } = FrozenSet.Create(
        StringComparer.Ordinal, "normative", "impl-defined", "divergence");

    /// <summary>Every defect found so far, the reader's and the validator's, in the order found.</summary>
    public IReadOnlyList<VectorDiagnostic> Diagnostics => diagnostics;

    /// <summary>Validates a file; identifiers are checked for uniqueness across every file this validator has seen.</summary>
    /// <param name="file">A file as the reader read it; its structural defects are carried over.</param>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=369403
    // Broiler-Human:        PENDING
    public void Validate(VectorFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        diagnostics.AddRange(file.Diagnostics);
        foreach (var record in file.Records)
            Validate(record);
    }

    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=7; Fingerprint=BCEF55
    // Broiler-Human:        PENDING
    private void Validate(VectorRecord record)
    {
        void Report(string code, int line, string message) => diagnostics.Add(new(code, record.File, line, message));

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in record.Fields)
        {
            if (!seen.Add(field.Key))
                Report("duplicate-key", field.Line, "'" + field.Key + "' appears more than once in the record");
            if (Known.Contains(field.Key))
                continue;
            if (EngineField(field.Key) is { } engine)
            {
                if (!Engines.Contains(engine.Name))
                    Report("unknown-engine", field.Line, "'" + engine.Name + "' is not broiler-js or broiler-vm");
                continue;
            }

            Report("unknown-key", field.Line, "'" + field.Key + "' is not a key the schema defines");
        }

        foreach (var key in Required)
        {
            if (record[key] is null)
                Report("missing-field", record.Line, "the record has no '" + key + "'");
        }

        int LineOf(string key) => record.Find(key)?.Line ?? record.Line;

        var area = record["area"];
        if (area is not null && !Areas.Contains(area))
            Report("bad-area", LineOf("area"), "'" + area + "' is not an area the schema defines");

        if (record["id"] is { } id)
        {
            if (!IsIdentifier(id))
                Report("bad-id", LineOf("id"), "'" + id + "' is not lowercase segments separated by /");
            else if (area is not null && id[..id.IndexOf('/', StringComparison.Ordinal)] != area)
                Report("bad-id", LineOf("id"), "'" + id + "' does not start with its area, '" + area + "'");
            if (ids.TryGetValue(id, out var first))
                Report("duplicate-id", LineOf("id"), "'" + id + "' is also the identifier at " + first.File + ":" + Ascii.Decimal(first.Line));
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
            Report("bad-class", LineOf("class"), "'" + @class + "' is not normative, impl-defined or divergence");

        var perEngine = 0;
        var divergences = 0;
        foreach (var field in record.Fields)
        {
            switch (EngineField(field.Key)?.Kind)
            {
                case "per-engine":
                    perEngine++;
                    if (@class != "impl-defined")
                        Report("misplaced-field", field.Line, "'" + field.Key + "' belongs to an impl-defined vector only");
                    ValidateOutcome(field.Value, field.Line, Report);
                    break;
                case "divergence":
                    divergences++;

                    // On an impl-defined vector the field classifies a difference from the engine's
                    // own per-engine expectation, so that expectation must be there (SCHEMA.md
                    // section 5).
                    if (@class == "impl-defined" ? record["per-engine." + field.Key["divergence.".Length..]] is null : @class != "divergence")
                        Report("misplaced-field", field.Line, "'" + field.Key + "' belongs to a divergence vector, or to an impl-defined one beside its per-engine field");
                    if (string.IsNullOrWhiteSpace(field.Value))
                        Report("bad-divergence", field.Line, "'" + field.Key + "' gives no reason");
                    break;
            }
        }

        if (@class == "impl-defined" && perEngine == 0)
            Report("impl-defined-without-expectation", LineOf("class"), "an impl-defined vector names no per-engine expectation");
        if (@class == "divergence" && divergences == 0)
            Report("divergence-without-engine", LineOf("class"), "a divergence vector names no engine");

        if (record["prov"] is { } provenance)
            ValidateProvenance(provenance, LineOf("prov"), Report);

        if (record["script"] is { } script && string.IsNullOrWhiteSpace(script))
            Report("bad-script", LineOf("script"), "the script is empty");

        if (record["since"] is { } since)
        {
            if (!Ascii.IsPositiveInteger(since))
                Report("bad-since", LineOf("since"), "'" + since + "' is not a positive integer");
            else if (!Ascii.TryParsePositive(since, out var revision) || revision > sources.Revision)
                Report("future-revision", LineOf("since"), "'" + since + "' is beyond the corpus's revision, " + Ascii.Decimal(sources.Revision));
        }
    }

    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=2; Fingerprint=75D1B5
    // Broiler-Human:        PENDING
    private static void ValidateInputs(string inputs, int line, Action<string, int, string> report)
    {
        var position = 0;
        while (true)
        {
            if (!TypedValue.TryReadToken(inputs, ref position, out var token))
            {
                report("bad-value", line, "'" + inputs + "' is not typed values separated by single spaces");
                return;
            }

            if (!TypedValue.TryParse(token, out var value, out var code, out var message))
                report(code, line, message);
            else if (value.Kind is TypedValueKind.Symbol or TypedValueKind.Object)
                report("bad-value", line, "'" + token + "' is an outcome notation, not an input");

            if (position == inputs.Length)
                return;
            if (inputs[position] != ' ')
            {
                report("bad-value", line, "'" + inputs + "' does not separate its values by single spaces");
                return;
            }

            position++;
        }
    }

    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=B9699B
    // Broiler-Human:        PENDING
    private static void ValidateOutcome(string text, int line, Action<string, int, string> report)
    {
        if (!Outcome.TryParseAlternatives(text, out _, out var code, out var message))
            report(code, line, message);
    }

    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=3; Fingerprint=E26139
    // Broiler-Human:        PENDING
    private void ValidateProvenance(string provenance, int line, Action<string, int, string> report)
    {
        var independent = false;
        var observed = false;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in provenance.Split(' '))
        {
            if (!seen.Add(entry))
            {
                report("bad-provenance", line, "'" + entry + "' is listed twice");
                continue;
            }

            if (!TryParseProvenance(entry, out var kind, out var detail))
            {
                report("bad-provenance", line, "'" + entry + "' is not a provenance entry the schema defines");
                continue;
            }

            if (kind == "spec" && !sources.SpecificationRevision.StartsWith(detail, StringComparison.Ordinal))
                report("bad-provenance", line, "'" + entry + "' does not name the pinned specification revision, " + sources.SpecificationRevision);
            else if (kind == "ucd" && detail != sources.UnicodeVersion)
                report("bad-provenance", line, "'" + entry + "' does not name the pinned Unicode version, " + sources.UnicodeVersion);
            else if (kind == "observed" && !Engines.Contains(detail))
                report("bad-provenance", line, "'" + entry + "' does not name broiler-js or broiler-vm");

            // An overruled reference records an answer the vector does not take, so it is no source of the
            // expected value (SCHEMA.md section 6, corpus revision 24).
            if (kind == "observed")
                observed = true;
            else if (kind != "overruled")
                independent = true;
        }

        if (!independent && observed)
            report("engine-observed", line, "the expected value has no source independent of both engines");
        else if (!independent)
            report("no-source", line, "the expected value has no source: an overruled reference is not one");
    }

    /// <summary>
    /// One provenance entry of the schema's section 6, and the part of it the validator compares
    /// with a pin: a specification revision, a Unicode version or an engine.
    /// </summary>
    /// <remarks>
    /// The grammar, as the regular expression it replaces wrote it:
    /// <c>spec:[a-z0-9]+@(?&lt;revision&gt;[0-9a-f]{7,40})#[a-z0-9.%-]+</c>,
    /// <c>test262:[A-Za-z0-9_./-]+\.js</c>, <c>ref:[a-z0-9-]+@[0-9]+</c>,
    /// <c>ucd:(?&lt;version&gt;[0-9]+\.[0-9]+\.[0-9]+)/[A-Za-z0-9_.-]+</c> and
    /// <c>observed:(?&lt;engine&gt;[a-z0-9-]+)</c>. Corpus revision 24 adds
    /// <c>proposal:[a-z0-9-]+@[0-9a-f]{7,40}#[a-z0-9.%-]+</c>, a clause of a pinned TC39 proposal's text, and
    /// <c>overruled:[a-z0-9-]+@[0-9]+=</c> followed by an outcome written without the space after its kind, the
    /// answer of a reference computation the vector does not take.
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=4; Fingerprint=AB4BFB
    // Broiler-Human:        PENDING
    private static bool TryParseProvenance(string entry, out string kind, out string detail)
    {
        kind = string.Empty;
        detail = string.Empty;
        var colon = entry.IndexOf(':', StringComparison.Ordinal);
        if (colon < 0)
            return false;
        kind = entry[..colon];
        var body = entry.AsSpan(colon + 1);
        switch (kind)
        {
            case "spec":
            {
                var at = body.IndexOf('@');
                if (at < 0 || !Ascii.All(body[..at], IsLowerAlphanumeric))
                    return false;
                var rest = body[(at + 1)..];
                var hash = rest.IndexOf('#');
                if (hash < 7 || hash > 40 || !Ascii.All(rest[..hash], Ascii.IsLowerHex) || !Ascii.All(rest[(hash + 1)..], IsClauseCharacter))
                    return false;
                detail = rest[..hash].ToString();
                return true;
            }

            case "test262":
                return body.Length > 3 && body.EndsWith(".js", StringComparison.Ordinal)
                    && Ascii.All(body, static c => Ascii.IsLetter(c) || Ascii.IsDigit(c) || c is '_' or '.' or '/' or '-');
            case "ref":
            {
                var at = body.IndexOf('@');
                return at >= 0 && Ascii.All(body[..at], static c => IsLowerAlphanumeric(c) || c == '-') && Ascii.All(body[(at + 1)..], Ascii.IsDigit);
            }

            case "ucd":
            {
                var slash = body.IndexOf('/');
                if (slash < 0 || !IsVersion(body[..slash]) || !Ascii.All(body[(slash + 1)..], static c => Ascii.IsLetter(c) || Ascii.IsDigit(c) || c is '_' or '.' or '-'))
                    return false;
                detail = body[..slash].ToString();
                return true;
            }

            case "observed":
                if (!Ascii.All(body, static c => IsLowerAlphanumeric(c) || c == '-'))
                    return false;
                detail = body.ToString();
                return true;
            case "proposal":
            {
                var at = body.IndexOf('@');
                if (at < 0 || !Ascii.All(body[..at], static c => IsLowerAlphanumeric(c) || c == '-'))
                    return false;
                var rest = body[(at + 1)..];
                var hash = rest.IndexOf('#');
                return hash >= 7 && hash <= 40 && Ascii.All(rest[..hash], Ascii.IsLowerHex) && Ascii.All(rest[(hash + 1)..], IsClauseCharacter);
            }

            case "overruled":
            {
                var equals = body.IndexOf('=');
                if (equals < 0)
                    return false;
                var reference = body[..equals];
                var at = reference.IndexOf('@');
                if (at < 0 || !Ascii.All(reference[..at], static c => IsLowerAlphanumeric(c) || c == '-') || !Ascii.All(reference[(at + 1)..], Ascii.IsDigit))
                    return false;
                return IsCompactOutcome(body[(equals + 1)..].ToString());
            }

            default:
                return false;
        }
    }

    /// <summary>
    /// One outcome of the schema's section 4 written without the space after its kind, as an <c>overruled:</c>
    /// entry holds it: <c>value:str:"P1Y"</c> for <c>value: str:"P1Y"</c>.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=B94301
    // Broiler-Human:        PENDING
    private static bool IsCompactOutcome(string text)
    {
        var colon = text.IndexOf(':', StringComparison.Ordinal);
        if (colon <= 0 || colon == text.Length - 1 || text[colon + 1] == ' ')
            return false;
        return Outcome.TryParseAlternatives(text[..colon] + ": " + text[(colon + 1)..], out var outcomes, out _, out _) && outcomes.Count == 1;
    }

    /// <summary><c>[0-9]+\.[0-9]+\.[0-9]+</c>.</summary>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=43D78D
    // Broiler-Human:        PENDING
    private static bool IsVersion(ReadOnlySpan<char> text)
    {
        for (var part = 0; part < 3; part++)
        {
            var dot = text.IndexOf('.');
            var digits = part < 2 ? (dot < 0 ? [] : text[..dot]) : text;
            if (!Ascii.All(digits, Ascii.IsDigit))
                return false;
            text = part < 2 ? text[(dot + 1)..] : [];
        }

        return true;
    }

    /// <summary><c>[a-z0-9.-]+(/[a-z0-9.-]+)+</c>.</summary>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=69B0F5
    // Broiler-Human:        PENDING
    private static bool IsIdentifier(string id)
    {
        var segments = id.Split('/');
        if (segments.Length < 2)
            return false;
        foreach (var segment in segments)
        {
            if (!Ascii.All(segment, IsIdentifierCharacter))
                return false;
        }

        return true;
    }

    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=F77F3B
    // Broiler-Human:        PENDING
    private static bool IsLowerAlphanumeric(char c) => Ascii.IsLower(c) || Ascii.IsDigit(c);

    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=D7BDFC
    // Broiler-Human:        PENDING
    private static bool IsIdentifierCharacter(char c) => IsLowerAlphanumeric(c) || c is '.' or '-';

    /// <summary>
    /// A character of a clause id: an identifier character, or <c>%</c>, which ECMA-262 writes in the
    /// ids of the intrinsics' clauses, such as <c>sec-%typedarray%.prototype.slice</c> (corpus revision 11).
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=156678
    // Broiler-Human:        PENDING
    private static bool IsClauseCharacter(char c) => IsIdentifierCharacter(c) || c == '%';

    // Broiler-AI:           Origin=AI; Spec=conformance/vectors/SCHEMA.md; IP=Low; Security=Low; Resources=1; Fingerprint=E0D475
    // Broiler-Human:        PENDING
    private static (string Kind, string Name)? EngineField(string key)
    {
        foreach (var kind in (ReadOnlySpan<string>)["per-engine", "divergence"])
        {
            if (key.Length > kind.Length + 1 && key.StartsWith(kind, StringComparison.Ordinal) && key[kind.Length] == '.')
                return (kind, key[(kind.Length + 1)..]);
        }

        return null;
    }
}
