using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Xunit;

namespace Broiler.ECMAScript.Intl.Tests.Differential;

/// <summary>
/// The pinned copies of Broiler.VM's code (<c>Differential/Vm/</c>) against their table in that folder's README: every copy
/// has its row, every row its copy, and each copy the SHA-256 its row records, so that a copy is never edited in place.
/// </summary>
public sealed class VmCopiesTests
{
    [Fact]
    public void Every_copy_is_the_one_its_row_records()
    {
        var folder = Folder();
        var rows = Rows(File.ReadAllLines(Path.Combine(folder, "README.md")));
        var files = Directory.EnumerateFiles(folder, "*.cs").Select(static p => Path.GetFileName(p)).Order(StringComparer.Ordinal).ToList();

        Assert.NotEmpty(files);
        Assert.Equal(files, rows.Keys.Order(StringComparer.Ordinal));
        foreach (var file in files)
        {
            var bytes = File.ReadAllBytes(Path.Combine(folder, file));
            Assert.Equal(rows[file], Convert.ToHexStringLower(SHA256.HashData(bytes)));
            Assert.Contains("Copied from Broiler.VM at " + DifferentialCheck.VmCommit, File.ReadAllText(Path.Combine(folder, file)), StringComparison.Ordinal);
        }
    }

    /// <summary>The README's table: each copy's file name and the SHA-256 its last column records.</summary>
    private static Dictionary<string, string> Rows(IEnumerable<string> lines) =>
        lines.Where(static l => l.StartsWith("| `", StringComparison.Ordinal) && l.Contains(".cs`", StringComparison.Ordinal))
            .Select(static l => l.Split('|', StringSplitOptions.TrimEntries))
            .ToDictionary(static c => c[1].Trim('`'), static c => c[^2].Trim('`'), StringComparer.Ordinal);

    private static string Folder()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "Broiler.ECMAScript", "Broiler.ECMAScript.Intl.Tests", "Differential", "Vm");
            if (File.Exists(Path.Combine(candidate, "README.md")))
                return candidate;
        }

        throw new DirectoryNotFoundException("no Broiler.ECMAScript.Intl.Tests/Differential/Vm/README.md above " + AppContext.BaseDirectory);
    }
}
