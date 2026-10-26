// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   13
// Annotated:        13/13
// Exempt:           4
// Human-reviewed:   0/13
// IP risk:          Low
// Security risk:    High
// Criteria:         7/7
// Resource impact:  3/10 max
// Unverified:       13
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Broiler.ECMAScript.Generation.Time;

namespace Broiler.ECMAScript.Generation.Intl;

/// <summary>One file an Intl data pin names: its path below the pin's directory, its length and its SHA-256.</summary>
/// <param name="Name">The path as the pin writes it, relative to the pin's directory, e.g. <c>cldr-48.2.0/json/cldr-core/supplemental/plurals.json</c>.</param>
/// <param name="Bytes">The file's length.</param>
/// <param name="Sha256">The file's SHA-256, in lower-case hexadecimal.</param>
// Broiler-AI:           Origin=Ported; Spec=JSD-0082 D10 CLDR pin via Broiler.VM CldrPin; IP=Low; Security=Low; Resources=1; Fingerprint=7F0BCB
// Broiler-Human:        PENDING
public sealed record IntlPinEntry(string Name, long Bytes, string Sha256);

/// <summary>
/// The CLDR data pin of the Intl data home - <c>Broiler.ECMAScript/data/intl/cldr/cldr.pin</c>, or Broiler.VM's
/// <c>src/tests/cldr/pins/cldr.pin</c>, whose lines it holds unchanged - parsed, and the checks that stand
/// between an archived byte and a table made from it.
/// </summary>
/// <remarks>
/// <para>
/// <b>PORTED from Broiler.VM's <c>CldrPin</c></b> (its rule N27) at Broiler.VM <c>4cf9ca44</c>: the pin's
/// shape, the version line, the archived flag, each file's length and SHA-256, and the version a file states
/// where it states one - a supplemental file in its <c>version</c> object's <c>_cldrVersion</c>, a
/// <c>package.json</c> in its <c>version</c>, the UCA files in a <c># UCA Version:</c> line - each compared
/// before anything reads the file, a failure an exception naming every violation. The main-data files, the
/// <c>bcp47</c> files, the collation XML and the licence state none and are held by their hashes alone.
/// </para>
/// <para>
/// <b>The UCA version is tied to the Unicode version</b> (Broiler.VM's decision JSD-0082, decision D12):
/// the pin's <c>uca-version</c> line, and every UCA file's header, must name
/// <see cref="IntlDataVersions.UnicodeVersion"/>, the version of the UCD files the generator reads beside
/// them. Broiler.VM's reader took its <c>PinnedUcaVersion</c> from its Unicode pin, and its rule N27 checked
/// the line; here the reader checks both.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=JSD-0082 D10 D12 CLDR pin via Broiler.VM CldrPin; IP=Low; Security=High; Resources=3; Fingerprint=574FE4
// Broiler-Falsified-If: a file whose length, SHA-256 or stated version differs from the pin, or a pin of another CLDR or UCA version, is returned as the pinned one without an exception
// Broiler-Human:        PENDING
public sealed class IntlCldrPin
{
    /// <summary>The one CLDR release the Intl data is taken from.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D10 CLDR pin via Broiler.VM CldrPin; IP=Low; Security=Low; Resources=0; Fingerprint=2BDACF
    // Broiler-Human:        PENDING
    public const string PinnedVersion = IntlDataVersions.CldrVersion;

    /// <summary>The CLDR major version the supplemental files state.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D10 CLDR pin via Broiler.VM CldrPin; IP=Low; Security=Low; Resources=0; Fingerprint=884F22
    // Broiler-Human:        PENDING
    public const string PinnedMajor = IntlDataVersions.CldrMajor;

    /// <summary>The UCA version the collation files state, which is the Unicode version the generator reads.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D12 CLDR pin via Broiler.VM CldrPin; IP=Low; Security=Low; Resources=0; Fingerprint=4C2813
    // Broiler-Human:        PENDING
    public const string PinnedUcaVersion = IntlDataVersions.UcaVersion;

    private IntlCldrPin(IReadOnlyDictionary<string, string> values, IReadOnlyList<IntlPinEntry> entries, IReadOnlyList<string> integrities)
    {
        Values = values;
        Entries = entries;
        Integrities = integrities;
    }

    /// <summary>The single-valued keys: <c>version</c>, <c>uca-version</c>, <c>archived</c> and the rest.</summary>
    public IReadOnlyDictionary<string, string> Values { get; }

    /// <summary>Every file the pin names, in the order it names them.</summary>
    public IReadOnlyList<IntlPinEntry> Entries { get; }

    /// <summary>The <c>json-integrity</c> lines, one per npm package the JSON files came from, without the key.</summary>
    public IReadOnlyList<string> Integrities { get; }

    /// <summary>The version line, or the empty string when there is none.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D10 CLDR pin via Broiler.VM CldrPin; IP=Low; Security=Medium; Resources=1; Fingerprint=7391BA
    // Broiler-Human:        PENDING
    public string Version => Values.TryGetValue("version", out var version) ? version : string.Empty;

    /// <summary>Parses pin text: <c>key value</c> lines, <c>#</c> comments, repeated <c>file</c> and <c>json-integrity</c> lines.</summary>
    /// <param name="text">The pin's text.</param>
    /// <exception cref="InvalidDataException">A <c>file</c> line is malformed, or a line is empty of a value or repeats a key.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D10 CLDR pin via Broiler.VM CldrPin; IP=Low; Security=High; Resources=3; Fingerprint=871D66
    // Broiler-Falsified-If: a malformed file line or a repeated key is accepted
    // Broiler-Human:        PENDING
    public static IntlCldrPin Parse(string text)
    {
        var (values, entries, integrities) = IntlPinText.Parse(text, "cldr.pin");
        return new IntlCldrPin(values, entries, integrities);
    }

    /// <summary>The entry the pin writes as <paramref name="name"/>.</summary>
    /// <param name="name">The path as the pin writes it.</param>
    /// <exception cref="InvalidDataException">The pin names no such file.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D10 CLDR pin via Broiler.VM CldrPin; IP=Low; Security=Medium; Resources=2; Fingerprint=8DFD48
    // Broiler-Human:        PENDING
    public IntlPinEntry Entry(string name) =>
        Entries.SingleOrDefault(entry => string.Equals(entry.Name, name, StringComparison.Ordinal))
        ?? throw new InvalidDataException("cldr.pin names no file `" + name + "`");

    /// <summary>
    /// Every way the given files differ from what the pin records: the version and UCA version lines, the
    /// archived flag, a missing file, a length, a hash, or a stated version that is not the pinned one. Empty
    /// when they are the files the pin describes.
    /// </summary>
    /// <param name="archive">Each file's bytes, by the path the pin writes it under.</param>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D10 D12 CLDR pin via Broiler.VM CldrPin; IP=Low; Security=High; Resources=3; Fingerprint=874F2C
    // Broiler-Falsified-If: a file whose length, hash or stated version differs from the pin, or a pin of another version or UCA version, yields no violation
    // Broiler-Human:        PENDING
    public IEnumerable<string> Violations(IReadOnlyDictionary<string, byte[]> archive)
    {
        ArgumentNullException.ThrowIfNull(archive);

        if (!string.Equals(Version, PinnedVersion, StringComparison.Ordinal))
        {
            yield return "cldr.pin names version `" + Version + "`, and the Intl data is taken from " + PinnedVersion + " only";
        }

        if (!Values.TryGetValue("uca-version", out var uca) || !string.Equals(uca, PinnedUcaVersion, StringComparison.Ordinal))
        {
            yield return "cldr.pin names UCA version `" + uca + "`, and the Unicode data the generator reads is " + PinnedUcaVersion;
        }

        if (!Values.TryGetValue("archived", out var archived) || archived != "yes")
        {
            yield return "cldr.pin does not say `archived yes`, and nothing reads unarchived bytes";
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

            if (VersionViolation(entry.Name, bytes) is { } stated)
            {
                yield return entry.Name + ": " + stated;
            }
        }
    }

    /// <summary>The files, verified, or an exception naming every way they are not the pinned ones.</summary>
    /// <param name="archive">Each file's bytes, by the path the pin writes it under.</param>
    /// <exception cref="InvalidDataException">The files are not the ones the pin describes.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D10 CLDR pin via Broiler.VM CldrPin; IP=Low; Security=High; Resources=3; Fingerprint=22A1C6
    // Broiler-Falsified-If: files with a violation are returned instead of refused
    // Broiler-Human:        PENDING
    public IReadOnlyDictionary<string, byte[]> Verified(IReadOnlyDictionary<string, byte[]> archive)
    {
        var violations = Violations(archive).ToArray();

        if (violations.Length > 0)
        {
            throw new InvalidDataException(
                "The CLDR archive is not the one cldr.pin describes, so nothing is generated from it:\n  " +
                string.Join("\n  ", violations));
        }

        return archive;
    }

    /// <summary>
    /// Whether a file states a version other than the pinned one, or null when it states the pinned one or
    /// none at all: a <c>package.json</c>'s <c>version</c>, a supplemental file's <c>_cldrVersion</c>, a UCA
    /// file's <c># UCA Version:</c> line among its first eleven.
    /// </summary>
    /// <param name="name">The path as the pin writes it.</param>
    /// <param name="bytes">The file's bytes.</param>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D10 D12 CLDR pin via Broiler.VM CldrPin; IP=Low; Security=High; Resources=3; Fingerprint=13894D
    // Broiler-Falsified-If: a package, supplemental or UCA file of another release is answered null
    // Broiler-Human:        PENDING
    public static string? VersionViolation(string name, byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(bytes);

        var fileName = name[(name.LastIndexOf('/') + 1)..];

        if (string.Equals(fileName, "package.json", StringComparison.Ordinal))
        {
            using var package = JsonDocument.Parse(bytes);

            var version = package.RootElement.TryGetProperty("version", out var stated) ? stated.GetString() : null;

            return string.Equals(version, PinnedVersion, StringComparison.Ordinal)
                ? null
                : "the package states version `" + version + "`, and the pin is " + PinnedVersion;
        }

        if (name.Contains("/supplemental/", StringComparison.Ordinal))
        {
            using var document = JsonDocument.Parse(bytes);

            var major = document.RootElement.TryGetProperty("supplemental", out var supplemental) &&
                supplemental.TryGetProperty("version", out var version) &&
                version.TryGetProperty("_cldrVersion", out var cldr)
                    ? cldr.GetString()
                    : null;

            return string.Equals(major, PinnedMajor, StringComparison.Ordinal)
                ? null
                : "the file states CLDR version `" + major + "`, and the pin is CLDR " + PinnedMajor;
        }

        if (name.Contains("/common/uca/", StringComparison.Ordinal))
        {
            var expected = "# UCA Version: " + PinnedUcaVersion;
            var lines = ArchiveReading.Decode(bytes).Split('\n', 12);

            return lines.Take(11).Any(line => string.Equals(line.TrimEnd('\r'), expected, StringComparison.Ordinal))
                ? null
                : "the header does not state `" + expected + "`";
        }

        return null;
    }
}

/// <summary>The grammar both Intl data pins share: <c>key value</c> lines, <c>#</c> comments, repeated <c>file</c> and <c>json-integrity</c> lines.</summary>
/// <remarks><b>PORTED from Broiler.VM's <c>CldrPin.Parse</c></b>, its grammar unchanged; Broiler.VM's <c>UnicodePin</c> reads the same <c>file</c> lines.</remarks>
// Broiler-AI:           Origin=Ported; Spec=JSD-0082 D10 D12 pin grammar via Broiler.VM CldrPin; IP=Low; Security=High; Resources=3; Fingerprint=6A20EE
// Broiler-Falsified-If: a file line that is not a path, a decimal length and a digest, or a single-valued key that repeats or has no value, is parsed without an exception
// Broiler-Human:        PENDING
internal static class IntlPinText
{
    /// <summary>The single-valued keys, the <c>file</c> entries in order and the <c>json-integrity</c> lines of a pin's text.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0082 D10 D12 pin grammar via Broiler.VM CldrPin; IP=Low; Security=High; Resources=3; Fingerprint=333008
    // Broiler-Falsified-If: a malformed file line or a repeated key is accepted
    // Broiler-Human:        PENDING
    internal static (IReadOnlyDictionary<string, string> Values, IReadOnlyList<IntlPinEntry> Entries, IReadOnlyList<string> Integrities) Parse(string text, string pinName)
    {
        ArgumentNullException.ThrowIfNull(text);

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var entries = new List<IntlPinEntry>();
        var integrities = new List<string>();

        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();

            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts[0] == "file")
            {
                if (parts.Length != 4 || !ArchiveReading.TryParseLength(parts[2], out var bytes))
                {
                    throw new InvalidDataException(pinName + ": `" + line + "` is not `file <path> <bytes> <sha256>`");
                }

                entries.Add(new IntlPinEntry(parts[1], bytes, ArchiveReading.AsciiLower(parts[3])));
                continue;
            }

            if (parts[0] == "json-integrity")
            {
                integrities.Add(string.Join(' ', parts.Skip(1)));
                continue;
            }

            if (parts.Length < 2 || !values.TryAdd(parts[0], string.Join(' ', parts.Skip(1))))
            {
                throw new InvalidDataException(pinName + ": `" + line + "` is empty or repeats a key");
            }
        }

        return (values.ToFrozenDictionary(StringComparer.Ordinal), entries.AsReadOnly(), integrities.AsReadOnly());
    }
}
