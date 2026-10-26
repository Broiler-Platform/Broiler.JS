// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   7
// Annotated:        7/7
// Exempt:           3
// Human-reviewed:   0/7
// IP risk:          Low
// Security risk:    High
// Criteria:         4/4
// Resource impact:  3/10 max
// Unverified:       7
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Broiler.ECMAScript.Generation.Time;

namespace Broiler.ECMAScript.Generation.Intl;

/// <summary>
/// The Unicode Character Database pin of the Intl data home, <c>Broiler.ECMAScript/data/intl/ucd/ucd.pin</c>:
/// the two UCD files the generator reads, <see cref="CldrUcd.UnicodeDataFile"/> and
/// <see cref="CldrUcd.PropListFile"/>, and the Unicode licence, parsed, and the checks that stand between an
/// archived byte and a table made from it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The shape of Broiler.VM's and Broiler.Unicode's Unicode pins</b> (decision JSD-0082, decision D12): a
/// <c>version</c> line, an <c>archived yes</c> line, and a <c>file</c> line per file, whose length and SHA-256
/// are compared before anything reads it. Each <c>file</c> line equals the line Broiler.Unicode's own pin
/// gives the same file, which the home's tests hold.
/// </para>
/// <para>
/// <b>Broiler.VM hands its own pinned files</b> to <see cref="CldrUcd.Read"/>, verified by its own Unicode
/// pin, and does not read this one.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0082 D12 UCD pin; IP=Low; Security=High; Resources=3; Fingerprint=AEF610
// Broiler-Falsified-If: a file whose length or SHA-256 differs from the pin, a PropList.txt of another version, or a pin of another Unicode version, is returned as the pinned one without an exception
// Broiler-Human:        PENDING
public sealed class IntlUcdPin
{
    /// <summary>The one Unicode version the generator reads.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D12 UCD pin; IP=Low; Security=Low; Resources=0; Fingerprint=F480A8
    // Broiler-Human:        PENDING
    public const string PinnedVersion = IntlDataVersions.UnicodeVersion;

    private IntlUcdPin(IReadOnlyDictionary<string, string> values, IReadOnlyList<IntlPinEntry> entries)
    {
        Values = values;
        Entries = entries;
    }

    /// <summary>The single-valued keys: <c>version</c>, <c>archived</c> and the rest.</summary>
    public IReadOnlyDictionary<string, string> Values { get; }

    /// <summary>Every file the pin names, in the order it names them.</summary>
    public IReadOnlyList<IntlPinEntry> Entries { get; }

    /// <summary>The version line, or the empty string when there is none.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D12 UCD pin; IP=Low; Security=Medium; Resources=1; Fingerprint=7391BA
    // Broiler-Human:        PENDING
    public string Version => Values.TryGetValue("version", out var version) ? version : string.Empty;

    /// <summary>Parses pin text: <c>key value</c> lines, <c>#</c> comments and repeated <c>file</c> lines.</summary>
    /// <param name="text">The pin's text.</param>
    /// <exception cref="InvalidDataException">A <c>file</c> line is malformed, or a line is empty of a value or repeats a key.</exception>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D12 UCD pin; IP=Low; Security=High; Resources=3; Fingerprint=07D170
    // Broiler-Falsified-If: a malformed file line or a repeated key is accepted
    // Broiler-Human:        PENDING
    public static IntlUcdPin Parse(string text)
    {
        var (values, entries, _) = IntlPinText.Parse(text, "ucd.pin");
        return new IntlUcdPin(values, entries);
    }

    /// <summary>The entry the pin writes as <paramref name="name"/>.</summary>
    /// <param name="name">The path as the pin writes it.</param>
    /// <exception cref="InvalidDataException">The pin names no such file.</exception>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D12 UCD pin; IP=Low; Security=Medium; Resources=2; Fingerprint=9F1A62
    // Broiler-Human:        PENDING
    public IntlPinEntry Entry(string name) =>
        Entries.SingleOrDefault(entry => string.Equals(entry.Name, name, StringComparison.Ordinal))
        ?? throw new InvalidDataException("ucd.pin names no file `" + name + "`");

    /// <summary>
    /// Every way the given files differ from what the pin records: the version, the archived flag, a missing
    /// file, a length, a hash, or a <c>PropList.txt</c> whose first line names another version. Empty when they
    /// are the files the pin describes and the pin names both files the generator reads.
    /// </summary>
    /// <param name="archive">Each file's bytes, by the path the pin writes it under.</param>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D12 UCD pin; IP=Low; Security=High; Resources=3; Fingerprint=DA6542
    // Broiler-Falsified-If: a file whose length or hash differs from the pin, a pin of another version, or a pin that does not name a file the generator reads, yields no violation
    // Broiler-Human:        PENDING
    public IEnumerable<string> Violations(IReadOnlyDictionary<string, byte[]> archive)
    {
        ArgumentNullException.ThrowIfNull(archive);

        if (!string.Equals(Version, PinnedVersion, StringComparison.Ordinal))
        {
            yield return "ucd.pin names version `" + Version + "`, and the generator reads " + PinnedVersion + " only";
        }

        if (!Values.TryGetValue("archived", out var archived) || archived != "yes")
        {
            yield return "ucd.pin does not say `archived yes`, and nothing reads unarchived bytes";
        }

        foreach (var name in new[] { CldrUcd.UnicodeDataFile, CldrUcd.PropListFile })
        {
            if (!Entries.Any(entry => string.Equals(entry.Name, name, StringComparison.Ordinal)))
            {
                yield return "ucd.pin does not name " + name + ", which the generator reads";
            }
        }

        foreach (var entry in Entries)
        {
            if (!archive.TryGetValue(entry.Name, out var bytes))
            {
                yield return entry.Name + " is pinned and no file is there";
                continue;
            }

            if (bytes.LongLength != entry.Bytes)
            {
                yield return string.Create(NumberFormatInfo.InvariantInfo, $"{entry.Name} is {bytes.LongLength} bytes, and the pin records {entry.Bytes}");
            }

            var digest = ArchiveReading.Sha256(bytes);

            if (!string.Equals(digest, entry.Sha256, StringComparison.Ordinal))
            {
                yield return entry.Name + " hashes to " + digest + ", and the pin records " + entry.Sha256;
            }

            if (string.Equals(entry.Name, CldrUcd.PropListFile, StringComparison.Ordinal) &&
                !ArchiveReading.Decode(bytes).StartsWith("# PropList-" + PinnedVersion + ".txt", StringComparison.Ordinal))
            {
                yield return entry.Name + ": the header does not state `# PropList-" + PinnedVersion + ".txt`";
            }
        }
    }

    /// <summary>The files, verified, or an exception naming every way they are not the pinned ones.</summary>
    /// <param name="archive">Each file's bytes, by the path the pin writes it under.</param>
    /// <exception cref="InvalidDataException">The files are not the ones the pin describes.</exception>
    // Broiler-AI:           Origin=AI; Spec=JSD-0082 D12 UCD pin; IP=Low; Security=High; Resources=3; Fingerprint=67FB7E
    // Broiler-Falsified-If: files with a violation are returned instead of refused
    // Broiler-Human:        PENDING
    public IReadOnlyDictionary<string, byte[]> Verified(IReadOnlyDictionary<string, byte[]> archive)
    {
        var violations = Violations(archive).ToArray();

        if (violations.Length > 0)
        {
            throw new InvalidDataException(
                "The UCD files are not the ones ucd.pin describes, so nothing is generated from them:\n  " +
                string.Join("\n  ", violations));
        }

        return archive;
    }
}
