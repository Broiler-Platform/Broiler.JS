// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   8
// Annotated:        8/8
// Exempt:           4
// Human-reviewed:   0/8
// IP risk:          Low
// Security risk:    High
// Criteria:         4/4
// Resource impact:  3/10 max
// Unverified:       8
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Broiler.ECMAScript.Generation.Time;

/// <summary>One file a CLDR pin names: its path below the pin's directory, its length and its SHA-256.</summary>
/// <param name="Name">The path as the pin writes it, relative to the pin's directory, e.g. <c>cldr-48.2.0/json/cldr-bcp47/bcp47/timezone.json</c>.</param>
/// <param name="Bytes">The file's length.</param>
/// <param name="Sha256">The file's SHA-256, in lower-case hexadecimal.</param>
// Broiler-AI:           Origin=Ported; Spec=JSD-0027 s5 CLDR pin via Broiler.VM CldrPin; IP=Low; Security=Low; Resources=1; Fingerprint=F2815E
// Broiler-Human:        PENDING
public sealed record CldrPinEntry(string Name, long Bytes, string Sha256);

/// <summary>
/// A CLDR data pin - the home's <c>Broiler.ECMAScript/data/time/cldr/cldr.pin</c>, which names the one
/// file the time zone tables read, or Broiler.VM's <c>src/tests/cldr/pins/cldr.pin</c> its line is copied
/// from - parsed, and the checks that stand between an archived byte and a table made from it.
/// </summary>
/// <remarks>
/// <b>PORTED, in part, from Broiler.VM's <c>CldrPin</c></b> (its rule N27): the pin's shape, the version
/// line, the archived flag, and each file's length and SHA-256 compared before anything reads it, a
/// failure an exception naming every violation. Broiler.VM's pin also names files that state their own
/// CLDR version, which its <c>CldrPin</c> compares; the file the time zone tables read,
/// <c>bcp47/timezone.json</c>, states none and is held by its hash alone, so this reader compares no
/// stated version, and Broiler.VM's rule N27 keeps that check for its own files.
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=JSD-0027 s5 CLDR pin via Broiler.VM CldrPin; IP=Low; Security=High; Resources=3; Fingerprint=FAF53A
// Broiler-Falsified-If: a file whose length or SHA-256 differs from the pin, or a pin of another CLDR version, is returned as the pinned one without an exception
// Broiler-Human:        PENDING
public sealed class CldrPin
{
    /// <summary>The one CLDR release the primary identifiers are taken from, <see cref="TimeDataVersions.CldrVersion"/>.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0027 s5 CLDR pin via Broiler.VM CldrPin; IP=Low; Security=Low; Resources=0; Fingerprint=9E9AC2
    // Broiler-Human:        PENDING
    public const string PinnedVersion = TimeDataVersions.CldrVersion;

    private CldrPin(IReadOnlyDictionary<string, string> values, IReadOnlyList<CldrPinEntry> entries, IReadOnlyList<string> integrities)
    {
        Values = values;
        Entries = entries;
        Integrities = integrities;
    }

    /// <summary>The single-valued keys: <c>version</c>, <c>archived</c> and the rest.</summary>
    public IReadOnlyDictionary<string, string> Values { get; }

    /// <summary>Every file the pin names, in the order it names them.</summary>
    public IReadOnlyList<CldrPinEntry> Entries { get; }

    /// <summary>The <c>json-integrity</c> lines, one per npm package the JSON files came from.</summary>
    public IReadOnlyList<string> Integrities { get; }

    /// <summary>The version line, or the empty string when there is none.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0027 s5 CLDR pin via Broiler.VM CldrPin; IP=Low; Security=Medium; Resources=1; Fingerprint=7391BA
    // Broiler-Human:        PENDING
    public string Version => Values.TryGetValue("version", out var version) ? version : string.Empty;

    /// <summary>Parses pin text: <c>key value</c> lines, <c>#</c> comments, repeated <c>file</c> and <c>json-integrity</c> lines.</summary>
    /// <param name="text">The pin's text.</param>
    /// <exception cref="InvalidDataException">A <c>file</c> line is malformed, or a line is empty of a value or repeats a key.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0027 s5 CLDR pin via Broiler.VM CldrPin; IP=Low; Security=High; Resources=3; Fingerprint=4ECE1D
    // Broiler-Falsified-If: a malformed file line or a repeated key is accepted
    // Broiler-Human:        PENDING
    public static CldrPin Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var entries = new List<CldrPinEntry>();
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
                    throw new InvalidDataException("cldr.pin: `" + line + "` is not `file <path> <bytes> <sha256>`");
                }

                entries.Add(new CldrPinEntry(parts[1], bytes, ArchiveReading.AsciiLower(parts[3])));
                continue;
            }

            if (parts[0] == "json-integrity")
            {
                integrities.Add(string.Join(' ', parts.Skip(1)));
                continue;
            }

            if (parts.Length < 2 || !values.TryAdd(parts[0], string.Join(' ', parts.Skip(1))))
            {
                throw new InvalidDataException("cldr.pin: `" + line + "` is empty or repeats a key");
            }
        }

        return new CldrPin(values.ToFrozenDictionary(StringComparer.Ordinal), entries.AsReadOnly(), integrities.AsReadOnly());
    }

    /// <summary>The entry the pin writes as <paramref name="name"/>.</summary>
    /// <param name="name">The path as the pin writes it.</param>
    /// <exception cref="InvalidDataException">The pin names no such file.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0027 s5 CLDR pin via Broiler.VM CldrPin; IP=Low; Security=Medium; Resources=2; Fingerprint=CBD84E
    // Broiler-Human:        PENDING
    public CldrPinEntry Entry(string name) =>
        Entries.SingleOrDefault(entry => string.Equals(entry.Name, name, StringComparison.Ordinal))
        ?? throw new InvalidDataException("cldr.pin names no file `" + name + "`");

    /// <summary>
    /// Every way the given files differ from what the pin records: the version, the archived flag, a missing
    /// file, a length or a hash. Empty when they are the files the pin describes.
    /// </summary>
    /// <param name="archive">Each file's bytes, by the path the pin writes it under.</param>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0027 s5 CLDR pin via Broiler.VM CldrPin; IP=Low; Security=High; Resources=3; Fingerprint=558790
    // Broiler-Falsified-If: a file whose length or hash differs from the pin, or a pin of another version, yields no violation
    // Broiler-Human:        PENDING
    public IEnumerable<string> Violations(IReadOnlyDictionary<string, byte[]> archive)
    {
        ArgumentNullException.ThrowIfNull(archive);

        if (!string.Equals(Version, PinnedVersion, StringComparison.Ordinal))
        {
            yield return "cldr.pin names version `" + Version + "`, and the time zone data is taken from " + PinnedVersion + " only";
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
        }
    }

    /// <summary>The files, verified, or an exception naming every way they are not the pinned ones.</summary>
    /// <param name="archive">Each file's bytes, by the path the pin writes it under.</param>
    /// <exception cref="InvalidDataException">The files are not the ones the pin describes.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0027 s5 CLDR pin via Broiler.VM CldrPin; IP=Low; Security=High; Resources=3; Fingerprint=22A1C6
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
}
