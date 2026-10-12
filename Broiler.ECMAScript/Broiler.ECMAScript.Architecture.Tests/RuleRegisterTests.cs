using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Broiler.ECMAScript.Architecture.Tests;

/// <summary>
/// The register is itself under test, as Broiler.VM's is: nothing may drift between the rows, the
/// tests that run and the witnesses on disk.
/// </summary>
public class RuleRegisterTests
{
    private static readonly JsonElement[] Rows = Load();

    private static readonly string[] Statuses = ["Active", "Vacuous", "Deferred"];

    [Fact]
    public void The_register_holds_SL1_to_SL10_each_Active()
    {
        Assert.Equal(Enumerable.Range(1, 10).Select(static n => "SL" + n), Rows.Select(static r => Field(r, "id")));
        Assert.All(Rows, static r => Assert.Contains(Field(r, "status"), Statuses));
        Assert.All(Rows, static r => Assert.True(Field(r, "status") == "Active", Field(r, "id") + " is not Active"));
        Assert.All(Rows, static r =>
        {
            foreach (var field in (string[])["statement", "evidence", "nonVacuousWhen", "test", "witness"])
                Assert.False(string.IsNullOrWhiteSpace(Field(r, field)), Field(r, "id") + " has no " + field);
        });
    }

    [Fact]
    public void Every_row_names_a_test_that_exists_and_every_rule_test_has_a_row()
    {
        var tests = typeof(RuleRegisterTests).Assembly.GetTypes()
            .SelectMany(static t => t.GetMethods())
            .Where(static m => m.GetCustomAttributes<FactAttribute>(inherit: true).Any())
            .Select(static m => m.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var row in Rows)
        {
            Assert.Contains(Field(row, "test"), tests);
            Assert.StartsWith(Field(row, "id") + "_", Field(row, "test"), StringComparison.Ordinal);
        }

        var ids = Rows.Select(static r => Field(r, "id")).ToHashSet(StringComparer.Ordinal);
        foreach (var test in tests.Where(static t => t.StartsWith("SL", StringComparison.Ordinal)))
            Assert.Contains(test[..test.IndexOf('_', StringComparison.Ordinal)], ids);
    }

    [Fact]
    public void Every_witness_a_row_names_exists_and_every_witness_file_is_named()
    {
        var named = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in Rows)
        {
            foreach (var witness in Field(row, "witness").Split(", "))
            {
                if (witness.StartsWith("witnesses/", StringComparison.Ordinal))
                {
                    Assert.True(File.Exists(Path.Combine(SharedTree.TestProjectDirectory, witness)), witness + " does not exist");
                    Assert.StartsWith(Field(row, "id") + "-", Path.GetFileName(witness), StringComparison.Ordinal);
                    named.Add(Path.GetFileName(witness));
                }
                else
                {
                    // A witness compiled into this assembly, as Broiler.VM's DynamicLoadingWitness is.
                    var type = witness.Split(' ')[0];
                    Assert.Contains(typeof(RuleRegisterTests).Assembly.GetTypes(), t => t.Name == type);
                }
            }
        }

        foreach (var file in Directory.EnumerateFiles(SharedTree.Witnesses))
            Assert.Contains(Path.GetFileName(file), named);
    }

    [Fact]
    public void No_row_carries_a_run_figure_beyond_what_its_rule_names()
    {
        // The register states rules; a bundle retains runs. A count of vectors, files or warnings
        // typed into a row would go stale the day a run changes it.
        foreach (var row in Rows)
        {
            var text = Field(row, "statement") + " " + Field(row, "nonVacuousWhen");
            Assert.DoesNotMatch(@"\b\d{2,}\s+(vectors|files|units|warnings|types|members)\b", text);
        }
    }

    private static string Field(JsonElement row, string name) =>
        row.TryGetProperty(name, out var value) ? value.GetString() ?? string.Empty : string.Empty;

    private static JsonElement[] Load()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(SharedTree.TestProjectDirectory, "rules.register.json")));
        return document.RootElement.GetProperty("rules").EnumerateArray().Select(static r => r.Clone()).ToArray();
    }
}
