using System;
using System.Linq;
using System.Text;
using Xunit;

namespace Broiler.ECMAScript.Conformance.Tests;

/// <summary>
/// The schema's grammars the validator decides by hand. Until milestone SJL-2 they were regular
/// expressions; the shared libraries use none (convention 9), so each one's accepting and refusing
/// cases are stated here against the expression it replaced.
/// </summary>
public class GrammarTests
{
    private static VectorSources Sources => Home.Corpus.Sources;

    [Theory]
    [InlineData("numbers/to-fixed/0001")]
    [InlineData("numbers/a.b-c/d")]
    public void An_identifier_of_lowercase_segments_is_accepted(string id)
        => Assert.DoesNotContain(Validate("id", id), d => d.Code == "bad-id");

    [Theory]
    [InlineData("numbers")]
    [InlineData("numbers/")]
    [InlineData("numbers//x")]
    [InlineData("numbers/X")]
    [InlineData("numbers/a b")]
    public void An_identifier_that_is_not_lowercase_segments_is_refused(string id)
        => Assert.Contains(Validate("id", id), d => d.Code == "bad-id");

    [Theory]
    [InlineData("test262:test/built-ins/Number/prototype/toFixed/S15.7.4.5_A1.1_T01.js")]
    [InlineData("ref:numref@1")]
    [InlineData("ref:a-b@0")]
    [InlineData("observed:broiler-js ref:numref@1")]
    public void A_provenance_entry_the_schema_defines_is_accepted(string provenance)
        => Assert.DoesNotContain(Validate("prov", provenance), d => d.Code is "bad-provenance" or "engine-observed");

    [Fact]
    public void The_pinned_specification_revision_and_Unicode_version_are_accepted()
    {
        Assert.DoesNotContain(Validate("prov", "spec:es2026@" + Sources.SpecificationRevision[..7] + "#sec-x.y"), d => d.Code == "bad-provenance");
        Assert.DoesNotContain(Validate("prov", "spec:es2026@" + Sources.SpecificationRevision + "#sec-x"), d => d.Code == "bad-provenance");
        Assert.DoesNotContain(Validate("prov", "ucd:" + Sources.UnicodeVersion + "/PropList.txt"), d => d.Code == "bad-provenance");
    }

    [Theory]
    [InlineData("spec:es2026@abcdef#sec-x")] // six hexadecimal digits
    [InlineData("spec:es2026@ABCDEF0#sec-x")] // uppercase
    [InlineData("spec:ES2026@abcdef0#sec-x")]
    [InlineData("spec:es2026@abcdef0#")]
    [InlineData("spec:es2026@abcdef0")]
    [InlineData("spec:@abcdef0#sec-x")]
    [InlineData("test262:.js")]
    [InlineData("test262:a.mjs")]
    [InlineData("test262:a b.js")]
    [InlineData("ref:numref@")]
    [InlineData("ref:@1")]
    [InlineData("ref:numref@1a")]
    [InlineData("ucd:17.0/PropList.txt")]
    [InlineData("ucd:17.0.0.0/PropList.txt")]
    [InlineData("ucd:17.0.0/")]
    [InlineData("ucd:17..0/PropList.txt")]
    [InlineData("observed:")]
    [InlineData("observed:Broiler-JS")]
    [InlineData("unknown:x")]
    [InlineData("spec")]
    public void A_provenance_entry_the_schema_does_not_define_is_refused(string provenance)
        => Assert.Contains(Validate("prov", provenance + " ref:numref@1"), d => d.Code == "bad-provenance");

    [Theory]
    [InlineData("1")]
    [InlineData("10")]
    public void A_revision_is_a_positive_integer(string since)
        => Assert.DoesNotContain(Validate("since", since), d => d.Code == "bad-since");

    [Theory]
    [InlineData("0")]
    [InlineData("01")]
    [InlineData("-1")]
    [InlineData("+1")]
    [InlineData("1.0")]
    [InlineData("")]
    public void A_revision_that_is_not_a_positive_integer_is_refused(string since)
        => Assert.Contains(Validate("since", since), d => d.Code == "bad-since");

    [Fact]
    public void A_revision_beyond_the_corpus_and_beyond_an_int_is_a_future_revision()
    {
        Assert.Contains(Validate("since", (Sources.Revision + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)), d => d.Code == "future-revision");
        Assert.Contains(Validate("since", "99999999999999999999"), d => d.Code == "future-revision");
    }

    [Theory]
    [InlineData("status: not-implemented")]
    [InlineData("status: a1-b")]
    public void A_status_word_is_accepted(string outcome)
        => Assert.True(Outcome.TryParseAlternatives(outcome, out _, out _, out var message), message);

    [Theory]
    [InlineData("status: Not")]
    [InlineData("status: 1a")]
    [InlineData("status: -a")]
    [InlineData("status: a_b")]
    public void A_status_that_is_not_a_word_is_refused(string outcome)
        => Assert.False(Outcome.TryParseAlternatives(outcome, out _, out _, out _));

    [Fact]
    public void The_sources_are_read_from_REVISIONS_md_and_the_pins()
    {
        const string revisions = "# Revisions\n\n| Revision | Date | Change |\n|---|---|---|\n| 1 | 2026-10-11 | first |\r\n| 2 | 2026-10-12 | second |\n";
        var sources = VectorSources.Parse(revisions, "# pin\nrevision abc123\n", "version 17.0.0\n");
        Assert.Equal(new VectorSources(2, "abc123", "17.0.0"), sources);

        Assert.Throws<FormatException>(() => VectorSources.Parse("| 2 | 2026-10-12 | x |\n", "revision a\n", "version b\n"));
        Assert.Throws<FormatException>(() => VectorSources.Parse("| 1 | 2026-10-11 | x |\n| 3 | 2026-10-12 | x |\n", "revision a\n", "version b\n"));
        Assert.Throws<FormatException>(() => VectorSources.Parse("no table\n", "revision a\n", "version b\n"));
        Assert.Throws<FormatException>(() => VectorSources.Parse("| 1 | 2026-10-11 | x |\n", "# revision a\n", "version b\n"));
    }

    [Fact]
    public void The_reader_refuses_a_byte_order_mark_bytes_that_are_not_UTF_8_and_a_CR()
    {
        static string Code(byte[] content) => Assert.Single(VectorReader.Read(content, "x.vectors").Diagnostics).Code;

        Assert.Equal("encoding", Code([0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("schema: 1\n")]));
        Assert.Equal("encoding", Code([.. Encoding.UTF8.GetBytes("schema: 1\n\nid: "), 0xC3, 0x28, (byte)'\n']));
        Assert.Equal("encoding", Code(Encoding.UTF8.GetBytes("schema: 1\r\n")));
        Assert.Equal("no-schema", Code(Encoding.UTF8.GetBytes("# only a comment\n")));
        Assert.Equal("syntax", Code(Encoding.UTF8.GetBytes("schema: 1\n\nid:x\n")));
    }

    [Fact]
    public void A_corpus_reads_its_vector_files_in_ordinal_order_and_skips_the_controls()
    {
        var file = Encoding.UTF8.GetBytes("schema: 1\n");
        var corpus = VectorCorpus.Load(Sources, [new("b.vectors", file), new("controls/x.vectors", file), new("A.vectors", file), new("README.md", file)]);
        Assert.Equal(["A.vectors", "b.vectors"], corpus.Files.Select(f => f.Path));
    }

    private static System.Collections.Generic.IReadOnlyList<VectorDiagnostic> Validate(string key, string value)
    {
        var validator = new VectorValidator(Sources);
        validator.Validate(new VectorFile("changed.vectors", [Home.Corpus.Records.First().With(key, value)], []));
        return validator.Diagnostics;
    }
}
