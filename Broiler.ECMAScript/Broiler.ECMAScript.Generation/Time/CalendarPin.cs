// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   12
// Annotated:        12/12
// Exempt:           4
// Human-reviewed:   0/12
// IP risk:          Low
// Security risk:    High
// Criteria:         6/6
// Resource impact:  7/10 max
// Unverified:       12
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Broiler.ECMAScript.Generation.Time;

/// <summary>One archived crate a calendar pin names: its file name, length, SHA-256 and SHA-512.</summary>
/// <param name="Name">The crate's file name, e.g. <c>icu_calendar-2.3.0.crate</c>.</param>
/// <param name="Bytes">The crate's length.</param>
/// <param name="Sha256">The crate's SHA-256, in lower-case hexadecimal: the <c>cksum</c> the crates.io index states.</param>
/// <param name="Sha512">The crate's SHA-512, in base64.</param>
// Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar pin via Broiler.VM CalendarPin; IP=Low; Security=Low; Resources=1; Fingerprint=D8EB11
// Broiler-Human:        PENDING
public sealed record CalendarPinArchive(string Name, long Bytes, string Sha256, string Sha512);

/// <summary>One file inside an archived crate: the crate, its path, its length and SHA-256.</summary>
/// <param name="Archive">The crate's file name.</param>
/// <param name="Name">The file's path inside the crate, e.g. <c>icu_calendar-2.3.0/src/cal/hijri/ummalqura_data.rs</c>.</param>
/// <param name="Bytes">The file's length after extraction.</param>
/// <param name="Sha256">The file's SHA-256 after extraction, in lower-case hexadecimal.</param>
// Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar pin via Broiler.VM CalendarPin; IP=Low; Security=Low; Resources=1; Fingerprint=F69C33
// Broiler-Human:        PENDING
public sealed record CalendarPinMember(string Archive, string Name, long Bytes, string Sha256);

/// <summary>
/// A calendar data pin - the home's <c>Broiler.ECMAScript/data/time/calendars/calendars.pin</c>, or
/// Broiler.VM's <c>src/tests/calendars/pins/calendars.pin</c> it is a copy of - parsed, and the checks that
/// stand between an archived byte and anything made from it.
/// </summary>
/// <remarks>
/// <para>
/// <b>PORTED from Broiler.VM's <c>CalendarPin</c></b> (its rule N31; Broiler.VM's JSD-0076 moves it to the
/// data home), with its behaviour unchanged, but that it reads no file: the caller hands it the pin's
/// text and each crate's bytes. It is <see cref="TzdbPin"/>'s shape over two archives: crates.io
/// publishes each crate version as a gzipped tarball whose SHA-256 its index states, so each is archived
/// as published and the pin records its digests; a generator reads only the members the pin names, each
/// checked against its own length and SHA-256 after extraction.
/// </para>
/// <para>
/// <b>The versions are read where each crate states them</b>: its <c>Cargo.toml</c>'s <c>[package]</c>
/// name and version, which must be <see cref="PinnedVersions"/>' (decision SJL-D-21).
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar pin via Broiler.VM CalendarPin; IP=Low; Security=High; Resources=7; Fingerprint=BFC182
// Broiler-Falsified-If: a crate or a member whose length, digests or stated name and version differ from the pin is extracted, or returned as the pinned one, without an exception
// Broiler-Human:        PENDING
public sealed class CalendarPin
{
    /// <summary>The crate versions the calendar tables are taken from: <see cref="TimeDataVersions.IcuCalendarVersion"/> and <see cref="TimeDataVersions.CalendricalCalculationsVersion"/>, by crate name.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar pin via Broiler.VM CalendarPin; IP=Low; Security=Low; Resources=1; Fingerprint=9A1DA8
    // Broiler-Human:        PENDING
    public static IReadOnlyDictionary<string, string> PinnedVersions { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["icu_calendar"] = TimeDataVersions.IcuCalendarVersion,
        ["calendrical_calculations"] = TimeDataVersions.CalendricalCalculationsVersion,
    }.ToFrozenDictionary(StringComparer.Ordinal);

    private CalendarPin(IReadOnlyDictionary<string, string> values, IReadOnlyList<CalendarPinArchive> archives, IReadOnlyList<CalendarPinMember> members)
    {
        Values = values;
        Archives = archives;
        Members = members;
    }

    /// <summary>The single-valued keys, and the <c>upstream</c> and <c>licence</c> lines as <c>upstream &lt;archive&gt;</c> and <c>licence &lt;archive&gt;</c>.</summary>
    public IReadOnlyDictionary<string, string> Values { get; }

    /// <summary>Every archive the pin names, in its order.</summary>
    public IReadOnlyList<CalendarPinArchive> Archives { get; }

    /// <summary>Every member the pin names, in its order.</summary>
    public IReadOnlyList<CalendarPinMember> Members { get; }

    /// <summary>
    /// Parses pin text: <c>key value</c> lines, <c>#</c> comments, repeated <c>archive</c>, <c>upstream</c>,
    /// <c>licence</c> and <c>member</c> lines.
    /// </summary>
    /// <param name="text">The pin's text.</param>
    /// <exception cref="InvalidDataException">An <c>archive</c>, <c>member</c>, <c>upstream</c> or <c>licence</c> line is malformed or repeats one, or a line is empty of a value or repeats a key.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar pin via Broiler.VM CalendarPin; IP=Low; Security=High; Resources=3; Fingerprint=084A93
    // Broiler-Falsified-If: a malformed archive or member line, or a repeated key, is accepted
    // Broiler-Human:        PENDING
    public static CalendarPin Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var archives = new List<CalendarPinArchive>();
        var members = new List<CalendarPinMember>();

        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();

            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            switch (parts[0])
            {
                case "archive":
                    if (parts.Length != 5 || !ArchiveReading.TryParseLength(parts[2], out var length))
                    {
                        throw new InvalidDataException("calendars.pin: `" + line + "` is not `archive <file> <bytes> <sha256> <sha512>`");
                    }

                    archives.Add(new CalendarPinArchive(parts[1], length, ArchiveReading.AsciiLower(parts[3]), parts[4]));
                    continue;
                case "member":
                    if (parts.Length != 5 || !ArchiveReading.TryParseLength(parts[3], out var bytes))
                    {
                        throw new InvalidDataException("calendars.pin: `" + line + "` is not `member <archive> <name> <bytes> <sha256>`");
                    }

                    members.Add(new CalendarPinMember(parts[1], parts[2], bytes, ArchiveReading.AsciiLower(parts[4])));
                    continue;
                case "upstream":
                case "licence":
                    if (parts.Length != 3 || !values.TryAdd(parts[0] + " " + parts[1], parts[2]))
                    {
                        throw new InvalidDataException("calendars.pin: `" + line + "` is not `" + parts[0] + " <archive> <value>` or repeats one");
                    }

                    continue;
            }

            if (parts.Length < 2 || !values.TryAdd(parts[0], string.Join(' ', parts.Skip(1))))
            {
                throw new InvalidDataException("calendars.pin: `" + line + "` is empty or repeats a key");
            }
        }

        return new CalendarPin(values.ToFrozenDictionary(StringComparer.Ordinal), archives.AsReadOnly(), members.AsReadOnly());
    }

    /// <summary>
    /// The members of the crates the pin names, by the pin's own list and nothing else, after each crate's
    /// own length and digests are checked; or an exception naming every way a crate or a member is not the
    /// pinned one.
    /// </summary>
    /// <param name="crates">Each archived crate's bytes, by its file name.</param>
    /// <returns>Each pinned member's bytes, by its path inside its crate.</returns>
    /// <exception cref="InvalidDataException">A crate or a member is not the one the pin describes, or a member is held twice.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar pin via Broiler.VM CalendarPin; IP=Low; Security=High; Resources=7; Fingerprint=87E5C0
    // Broiler-Falsified-If: a crate whose length or digests differ from the pin is decompressed, or a member is answered without its own length and hash compared
    // Broiler-Human:        PENDING
    public IReadOnlyDictionary<string, byte[]> Extract(IReadOnlyDictionary<string, byte[]> crates)
    {
        ArgumentNullException.ThrowIfNull(crates);

        var refusals = ArchiveViolations(crates).ToArray();

        if (refusals.Length > 0)
        {
            throw Refusal(refusals);
        }

        var extracted = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        foreach (var archive in Archives)
        {
            var wanted = Members.Where(member => member.Archive == archive.Name).Select(static member => member.Name).ToHashSet(StringComparer.Ordinal);

            foreach (var (name, bytes) in ArchiveReading.RegularFiles(crates[archive.Name], wanted))
            {
                if (!extracted.TryAdd(name, bytes))
                {
                    throw Refusal([archive.Name + " holds `" + name + "` twice"]);
                }
            }
        }

        return Verified(extracted);
    }

    /// <summary>Every way the crates' bytes differ from what the pin records: the archived flag, the crates named, each crate's presence, length and digests.</summary>
    /// <param name="crates">Each archived crate's bytes, by its file name.</param>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar pin via Broiler.VM CalendarPin; IP=Low; Security=High; Resources=3; Fingerprint=5AF285
    // Broiler-Falsified-If: a crate one byte or one length away from the pin's, or a crate of another version, yields no violation
    // Broiler-Human:        PENDING
    public IEnumerable<string> ArchiveViolations(IReadOnlyDictionary<string, byte[]> crates)
    {
        ArgumentNullException.ThrowIfNull(crates);

        if (!Values.TryGetValue("archived", out var archived) || archived != "yes")
        {
            yield return "calendars.pin does not say `archived yes`, and nothing reads unarchived bytes";
        }

        if (Archives.Count != PinnedVersions.Count)
        {
            yield return string.Create(NumberFormatInfo.InvariantInfo, $"calendars.pin names {Archives.Count} archives, and the calendar data is taken from {PinnedVersions.Count}");
        }

        foreach (var archive in Archives)
        {
            if (!PinnedVersions.Any(pair => archive.Name == pair.Key + "-" + pair.Value + ".crate"))
            {
                yield return "calendars.pin names `" + archive.Name + "`, which is not a pinned crate version";
            }

            if (!crates.TryGetValue(archive.Name, out var bytes))
            {
                yield return "`" + archive.Name + "` is missing";
                continue;
            }

            if (bytes.LongLength != archive.Bytes)
            {
                yield return string.Create(NumberFormatInfo.InvariantInfo, $"{archive.Name} is {bytes.LongLength} bytes, and the pin records {archive.Bytes}");
            }

            var sha256 = ArchiveReading.Sha256(bytes);

            if (!string.Equals(sha256, archive.Sha256, StringComparison.Ordinal))
            {
                yield return archive.Name + " hashes to " + sha256 + ", and the pin records " + archive.Sha256;
            }

            var sha512 = ArchiveReading.Sha512(bytes);

            if (!string.Equals(sha512, archive.Sha512, StringComparison.Ordinal))
            {
                yield return archive.Name + "'s SHA-512 is " + sha512 + ", and the pin records " + archive.Sha512;
            }
        }
    }

    /// <summary>
    /// Every way extracted members differ from what the pin records: a missing member, a length, a hash, or
    /// a crate stating another name or version. Empty when they are the pinned ones.
    /// </summary>
    /// <param name="members">Each member's bytes, by its path inside its crate.</param>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar pin via Broiler.VM CalendarPin; IP=Low; Security=High; Resources=3; Fingerprint=1EB55D
    // Broiler-Falsified-If: a member whose length or hash differs from the pin, or a Cargo.toml stating another name or version, yields no violation
    // Broiler-Human:        PENDING
    public IEnumerable<string> Violations(IReadOnlyDictionary<string, byte[]> members)
    {
        ArgumentNullException.ThrowIfNull(members);

        foreach (var member in Members)
        {
            if (!members.TryGetValue(member.Name, out var bytes))
            {
                yield return member.Archive + " has no member `" + member.Name + "`, which the pin names";
                continue;
            }

            if (bytes.LongLength != member.Bytes)
            {
                yield return string.Create(NumberFormatInfo.InvariantInfo, $"`{member.Name}` is {bytes.LongLength} bytes, and the pin records {member.Bytes}");
            }

            var digest = ArchiveReading.Sha256(bytes);

            if (!string.Equals(digest, member.Sha256, StringComparison.Ordinal))
            {
                yield return "`" + member.Name + "` hashes to " + digest + ", and the pin records " + member.Sha256;
            }
        }

        // In the order Broiler.VM's reader states them, so that its refusals read the same.
        foreach (var crate in (string[])["icu_calendar", "calendrical_calculations"])
        {
            var version = PinnedVersions[crate];
            var manifest = crate + "-" + version + "/Cargo.toml";

            if (!members.TryGetValue(manifest, out var bytes))
            {
                continue;
            }

            var (name, stated) = PackageOf(Decode(bytes));

            if (name != crate || stated != version)
            {
                yield return manifest + " states the package `" + name + "` " + stated + ", and the pin is " + crate + " " + version;
            }
        }
    }

    /// <summary>The members, verified, or an exception naming every way they are not the pinned ones.</summary>
    /// <param name="members">Each member's bytes, by its path inside its crate.</param>
    /// <exception cref="InvalidDataException">The members are not the ones the pin describes.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar pin via Broiler.VM CalendarPin; IP=Low; Security=High; Resources=3; Fingerprint=6B0B95
    // Broiler-Falsified-If: members with a violation are returned instead of refused
    // Broiler-Human:        PENDING
    public IReadOnlyDictionary<string, byte[]> Verified(IReadOnlyDictionary<string, byte[]> members)
    {
        var violations = Violations(members).ToArray();

        if (violations.Length > 0)
        {
            throw Refusal(violations);
        }

        return members;
    }

    /// <summary>The name and version a <c>Cargo.toml</c>'s <c>[package]</c> table states, each empty when it states none.</summary>
    /// <param name="manifest">The <c>Cargo.toml</c>'s text.</param>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar pin via Broiler.VM CalendarPin; IP=Low; Security=Medium; Resources=3; Fingerprint=24EAFE
    // Broiler-Human:        PENDING
    public static (string Name, string Version) PackageOf(string manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var name = string.Empty;
        var version = string.Empty;
        var inPackage = false;

        foreach (var raw in manifest.Split('\n'))
        {
            var line = raw.Trim();

            if (line.StartsWith('['))
            {
                inPackage = line == "[package]";
                continue;
            }

            if (!inPackage)
            {
                continue;
            }

            if (line.StartsWith("name = \"", StringComparison.Ordinal))
            {
                name = line[8..^1];
            }
            else if (line.StartsWith("version = \"", StringComparison.Ordinal))
            {
                version = line[11..^1];
            }
        }

        return (name, version);
    }

    /// <summary>Strict UTF-8: a byte sequence that is not UTF-8 is a refusal, not a replacement character.</summary>
    /// <param name="bytes">The bytes.</param>
    /// <exception cref="System.Text.DecoderFallbackException">The bytes are not UTF-8.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar pin via Broiler.VM CalendarPin; IP=Low; Security=Medium; Resources=3; Fingerprint=DAAAC3
    // Broiler-Human:        PENDING
    public static string Decode(byte[] bytes) => ArchiveReading.Decode(bytes);

    /// <summary>The exception every refusal is, naming each violation on its own line.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0056 calendar pin via Broiler.VM CalendarPin; IP=Low; Security=Low; Resources=2; Fingerprint=10B312
    // Broiler-Human:        PENDING
    private static InvalidDataException Refusal(IEnumerable<string> violations) =>
        new("The calendar archives are not the ones calendars.pin describes, so nothing is generated from them:\n  " +
            string.Join("\n  ", violations));
}
