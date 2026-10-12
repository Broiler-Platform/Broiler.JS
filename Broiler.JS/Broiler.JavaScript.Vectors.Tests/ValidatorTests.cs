using Broiler.JavaScript.Vectors.Tests.Corpus;

namespace Broiler.JavaScript.Vectors.Tests;

/// <summary>The corpus in its home, loaded and validated once for every test that reads it.</summary>
public static class Home
{
    private static readonly Lazy<VectorCorpus> Loaded = new(() => VectorCorpus.Load(VectorCorpus.Home()));

    public static VectorCorpus Corpus => Loaded.Value;
}

public class ValidatorTests
{
    [Fact]
    public void The_corpus_validates()
    {
        var corpus = Home.Corpus;
        Assert.True(corpus.Diagnostics.Count == 0, string.Join("\n", corpus.Diagnostics.Take(20)));
        Assert.NotEmpty(corpus.Records);
        Assert.All(corpus.Files, file => Assert.NotEmpty(file.Records));
    }

    [Fact]
    public void Every_vector_has_a_source_independent_of_both_engines()
    {
        // The validator refuses the opposite; this states the property of the corpus as it stands.
        Assert.All(Home.Corpus.Records, vector =>
            Assert.Contains(vector["prov"]!.Split(' '), entry => !entry.StartsWith("observed:", StringComparison.Ordinal)));
    }

    public static TheoryData<string> Controls()
    {
        var data = new TheoryData<string>();
        foreach (var path in Directory.EnumerateFiles(Path.Combine(VectorCorpus.Home(), VectorCorpus.ControlsDirectory), "*.vectors").Order(StringComparer.Ordinal))
            data.Add(Path.GetFileName(path));
        return data;
    }

    /// <summary>
    /// Each negative control is malformed in one way, and its file name is the code of the one
    /// defect the validator must report: refused, and for the stated reason only.
    /// </summary>
    [Theory]
    [MemberData(nameof(Controls))]
    public void A_negative_control_is_refused_for_its_stated_reason(string control)
    {
        var diagnostics = ValidateControl(control);
        var reason = Path.GetFileNameWithoutExtension(control);
        Assert.True(diagnostics.Count > 0, $"{control} was accepted");
        Assert.All(diagnostics, d => Assert.True(d.Code == reason, $"{control}: {d}"));
    }

    [Fact]
    public void The_controls_README_lists_every_control()
    {
        var directory = Path.Combine(VectorCorpus.Home(), VectorCorpus.ControlsDirectory);
        var readme = File.ReadAllText(Path.Combine(directory, "README.md"));
        foreach (var path in Directory.EnumerateFiles(directory, "*.vectors"))
            Assert.Contains($"| `{Path.GetFileName(path)}` |", readme);
    }

    [Fact]
    public void A_vector_whose_only_provenance_is_engine_observed_is_refused()
    {
        var diagnostics = ValidateControl("engine-observed.vectors");
        Assert.Contains(diagnostics, d => d.Code == "engine-observed");
    }

    /// <summary>
    /// One record of the corpus, changed one field at a time, is refused for each change: the
    /// validator reads the fields of a real vector, not only the shape of the controls.
    /// </summary>
    [Theory]
    [InlineData("class", "optional", "bad-class")]
    [InlineData("since", "0", "bad-since")]
    [InlineData("prov", "observed:broiler-vm", "engine-observed")]
    [InlineData("prov", "spec:es2026@deadbeef#sec-x", "bad-provenance")]
    [InlineData("prov", "ucd:16.0.0/PropList.txt", "bad-provenance")]
    [InlineData("area", "number", "bad-area")]
    [InlineData("id", "Numbers/x", "bad-id")]
    [InlineData("out", "value: 1", "bad-value")]
    [InlineData("out", "error: Oops", "bad-outcome")]
    [InlineData("in", "f64:0x3FF0000000000000  int:1", "bad-value")]
    public void A_changed_field_of_a_real_vector_is_refused(string key, string value, string code)
    {
        var record = Home.Corpus.Records.First().With(key, value);
        var validator = new VectorValidator(Home.Corpus.Sources);
        validator.Validate(new VectorFile("changed.vectors", [record], []));
        Assert.Contains(validator.Diagnostics, d => d.Code == code);
    }

    private static List<VectorDiagnostic> ValidateControl(string control)
    {
        var path = Path.Combine(VectorCorpus.Home(), VectorCorpus.ControlsDirectory, control);
        var validator = new VectorValidator(Home.Corpus.Sources);
        validator.Validate(VectorReader.Read(path, VectorCorpus.ControlsDirectory + "/" + control));
        return validator.Diagnostics;
    }
}
