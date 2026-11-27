using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Broiler.ECMAScript.Probe;
using Broiler.ECMAScript.Runners;
using Xunit;

namespace Broiler.ECMAScript.Syntax.Tests;

/// <summary>
/// The probe's corpus digest mode with the semantic model (SJL-15.2 stage 8; the session's decisions D-js-digest-model and
/// D-js-part-g): its request is the corpus reader's, <c>ParseCorpus.Request</c>, copied member by member with
/// <see cref="ParseRequest.BuildSemanticModel"/> on, and its line is the id, the variant, the outcome and the SHA-256 of
/// the canonical dump. The copy is held equal to the original in every public property but that one, by reflection, so
/// that a member added to the request later cannot be dropped silently; and the line is held to its four classes:
/// accepted (the model's section moves the digest), refused by a <c>semantics</c> row with the member on, refused by the
/// grammar after the partial walk reported a <c>semantics</c> row, and refused by the grammar with none. The probe's
/// <c>ParseCorpus.cs</c> and <c>CorpusDigestRequest.cs</c> are compiled into this project for these tests.
/// </summary>
public sealed class CorpusDigestTests
{
    private static readonly Lazy<ParseCorpus> Corpus =
        new(static () => ParseCorpus.Load(Path.Combine(UnicodeDataHome.Root(AppContext.BaseDirectory), "conformance", "parse"), null, null));

    /// <summary>One variant of each kind the reader builds in configuration V, and every eval flag set.</summary>
    public static TheoryData<string> Variants => new() { "sloppy", "strict", "module", "E0", "E1", "E2", "E3", "E4", "E5", "E6", "E7" };

    [Theory]
    [MemberData(nameof(Variants))]
    public void The_copy_is_the_corpus_reader_s_request_in_every_public_member_but_the_model_s(string variant)
    {
        var original = Corpus.Value.Request("var x = 1;", variant, "V", NoValidator.Instance);
        var copy = CorpusDigestRequest.WithModel(original);
        Assert.False(original.BuildSemanticModel);
        Assert.True(copy.BuildSemanticModel);
        var properties = typeof(ParseRequest).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Assert.Contains(properties, static p => p.Name == nameof(ParseRequest.BuildSemanticModel));
        Assert.Contains(properties, static p => p.Name == nameof(ParseRequest.Context));
        foreach (var property in properties.Where(static p => p.Name != nameof(ParseRequest.BuildSemanticModel)))
            Assert.True(Equals(property.GetValue(original), property.GetValue(copy)), variant + ": " + property.Name + " differs in the copy");

        // The request has no public field a copy of its properties would miss.
        Assert.Empty(typeof(ParseRequest).GetFields(BindingFlags.Public | BindingFlags.Instance));
    }

    [Fact]
    public void An_accepted_source_s_digest_covers_the_model_s_section()
    {
        var (line, off, on) = Digests("var a = 1; function f(b) { return a + b; }");
        Assert.Equal("id\tsloppy\taccepted\t" + SyntaxDump.Digest(on), line);
        Assert.Equal(ParseOutcome.Accepted, off.Outcome);
        Assert.NotNull(on.Model);
        Assert.NotEqual(SyntaxDump.Digest(off), SyntaxDump.Digest(on));
    }

    [Fact]
    public void A_source_a_semantics_row_refuses_reads_refused()
    {
        var (line, off, on) = Digests("({ __proto__: 1, __proto__: 2 });");
        Assert.Equal(ParseOutcome.Accepted, off.Outcome);
        Assert.Equal(ParseOutcome.Refused, on.Outcome);
        Assert.Contains(on.Diagnostics, static d => d.Phase == DiagnosticPhase.Semantics);
        Assert.Equal("id\tsloppy\trefused\t" + SyntaxDump.Digest(on), line);
    }

    [Fact]
    public void A_grammar_refusal_after_a_semantics_row_moves_the_digest()
    {
        var (line, off, on) = Digests("({ __proto__: 1, __proto__: 2 });\n)");
        Assert.Equal(ParseOutcome.Refused, off.Outcome);
        Assert.Equal(ParseOutcome.Refused, on.Outcome);
        Assert.DoesNotContain(off.Diagnostics, static d => d.Phase == DiagnosticPhase.Semantics);
        Assert.Contains(on.Diagnostics, static d => d.Phase == DiagnosticPhase.Semantics);
        Assert.Equal("id\tsloppy\trefused\t" + SyntaxDump.Digest(on), line);
        Assert.NotEqual(SyntaxDump.Digest(off), SyntaxDump.Digest(on));
    }

    [Fact]
    public void A_grammar_refusal_with_no_semantics_row_keeps_the_digest()
    {
        var (line, off, on) = Digests("var a = 1;\nvar = ;");
        Assert.Equal(ParseOutcome.Refused, off.Outcome);
        Assert.Equal(ParseOutcome.Refused, on.Outcome);
        Assert.DoesNotContain(on.Diagnostics, static d => d.Phase == DiagnosticPhase.Semantics);
        Assert.Equal("id\tsloppy\trefused\t" + SyntaxDump.Digest(off), line);
    }

    // The digest mode's line for a sloppy source, with the parses with the member off (the reader's request) and on.
    private static (string Line, ParseResult Off, ParseResult On) Digests(string text)
    {
        var request = Corpus.Value.Request(text, "sloppy", "V", NoValidator.Instance);
        var on = SyntaxParser.Parse(CorpusDigestRequest.WithModel(request));
        return (CorpusDigestRequest.Line("id", "sloppy", on), SyntaxParser.Parse(request), on);
    }
}
