// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   12
// Annotated:        12/12
// Exempt:           3
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

/// <summary>One file inside the archived tzdb tarball a pin names: its name, length and SHA-256.</summary>
/// <param name="Name">The member's path inside the tarball, e.g. <c>europe</c> or <c>zone.tab</c>.</param>
/// <param name="Bytes">The member's length after extraction.</param>
/// <param name="Sha256">The member's SHA-256 after extraction, in lower-case hexadecimal.</param>
// Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz pin via Broiler.VM TzdbPin; IP=Low; Security=Low; Resources=1; Fingerprint=25B4A3
// Broiler-Human:        PENDING
public sealed record TzdbPinMember(string Name, long Bytes, string Sha256);

/// <summary>
/// An IANA Time Zone Database pin - the home's <c>Broiler.ECMAScript/data/time/tzdb/tzdb.pin</c>, or
/// Broiler.VM's <c>src/tests/tzdb/pins/tzdb.pin</c> it is a copy of - parsed, and the checks that stand
/// between an archived byte and anything made from it.
/// </summary>
/// <remarks>
/// <para>
/// <b>PORTED from Broiler.VM's <c>TzdbPin</c></b> (its rule N29; Broiler.VM's JSD-0076 moves it to the
/// data home), with its behaviour unchanged, but that it reads no file: the caller hands it the pin's
/// text and the tarball's bytes. IANA publishes each release as a data tarball, so the tarball is
/// archived as published and the pin records its length and digests; a generator then reads only the
/// members the pin names, each checked against its own length and SHA-256 after extraction.
/// </para>
/// <para>
/// <b>The version is read where the release states it</b>: the <c>version</c> member, one line, which
/// must be <see cref="PinnedVersion"/>. A failure is an exception naming every violation, never a
/// warning, and nothing is generated from bytes that failed a check.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz pin via Broiler.VM TzdbPin; IP=Low; Security=High; Resources=7; Fingerprint=790869
// Broiler-Falsified-If: a tarball or a member whose length, SHA-256, SHA-512 or stated version differs from the pin is extracted, or returned as the pinned one, without an exception
// Broiler-Human:        PENDING
public sealed class TzdbPin
{
    /// <summary>The one tzdb release the time zone tables are taken from, <see cref="TimeDataVersions.TzdbRelease"/>.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz pin via Broiler.VM TzdbPin; IP=Low; Security=Low; Resources=0; Fingerprint=769382
    // Broiler-Human:        PENDING
    public const string PinnedVersion = TimeDataVersions.TzdbRelease;

    private TzdbPin(IReadOnlyDictionary<string, string> values, IReadOnlyList<TzdbPinMember> members)
    {
        Values = values;
        Members = members;
    }

    /// <summary>The single-valued keys: <c>version</c>, <c>archive</c>, <c>archived</c> and the rest.</summary>
    public IReadOnlyDictionary<string, string> Values { get; }

    /// <summary>Every member the pin names, in the order it names them.</summary>
    public IReadOnlyList<TzdbPinMember> Members { get; }

    /// <summary>The version line, or the empty string when there is none.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz pin via Broiler.VM TzdbPin; IP=Low; Security=Medium; Resources=1; Fingerprint=7391BA
    // Broiler-Human:        PENDING
    public string Version => Values.TryGetValue("version", out var version) ? version : string.Empty;

    /// <summary>The archive's file name, which the pin's <c>archive</c> line states beside its length.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz pin via Broiler.VM TzdbPin; IP=Low; Security=Medium; Resources=1; Fingerprint=ACF122
    // Broiler-Human:        PENDING
    public string ArchiveName => Values.TryGetValue("archive", out var archive) ? archive.Split(' ')[0] : string.Empty;

    /// <summary>Parses pin text: <c>key value</c> lines, <c>#</c> comments, repeated <c>member</c> lines.</summary>
    /// <param name="text">The pin's text.</param>
    /// <exception cref="InvalidDataException">A <c>member</c> line is malformed, or a line is empty of a value or repeats a key.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz pin via Broiler.VM TzdbPin; IP=Low; Security=High; Resources=3; Fingerprint=72E0AE
    // Broiler-Falsified-If: a malformed member line or a repeated key is accepted
    // Broiler-Human:        PENDING
    public static TzdbPin Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var members = new List<TzdbPinMember>();

        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();

            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts[0] == "member")
            {
                if (parts.Length != 4 || !ArchiveReading.TryParseLength(parts[2], out var bytes))
                {
                    throw new InvalidDataException("tzdb.pin: `" + line + "` is not `member <name> <bytes> <sha256>`");
                }

                members.Add(new TzdbPinMember(parts[1], bytes, ArchiveReading.AsciiLower(parts[3])));
                continue;
            }

            if (parts.Length < 2 || !values.TryAdd(parts[0], string.Join(' ', parts.Skip(1))))
            {
                throw new InvalidDataException("tzdb.pin: `" + line + "` is empty or repeats a key");
            }
        }

        return new TzdbPin(values.ToFrozenDictionary(StringComparer.Ordinal), members.AsReadOnly());
    }

    /// <summary>
    /// The members of a tarball the pin names, by the pin's own list and nothing else, after the tarball's
    /// own length and digests are checked; or an exception naming every way the tarball or a member is not
    /// the pinned one.
    /// </summary>
    /// <param name="tarball">The archived tarball's bytes.</param>
    /// <returns>Each pinned member's bytes, by its name in the tarball.</returns>
    /// <exception cref="InvalidDataException">The tarball or a member is not the one the pin describes, or the tarball holds a member twice.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz pin via Broiler.VM TzdbPin; IP=Low; Security=High; Resources=7; Fingerprint=CF0796
    // Broiler-Falsified-If: a tarball whose length or digests differ from the pin is decompressed, or a member is answered without its own length and hash compared
    // Broiler-Human:        PENDING
    public IReadOnlyDictionary<string, byte[]> Extract(byte[] tarball)
    {
        ArgumentNullException.ThrowIfNull(tarball);

        var refusals = TarballViolations(tarball).ToArray();

        if (refusals.Length > 0)
        {
            throw Refusal(refusals);
        }

        var wanted = Members.Select(static member => member.Name).ToHashSet(StringComparer.Ordinal);
        var archive = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        foreach (var (name, bytes) in ArchiveReading.RegularFiles(tarball, wanted))
        {
            if (!archive.TryAdd(name, bytes))
            {
                throw Refusal([ArchiveName + " holds `" + name + "` twice"]);
            }
        }

        return Verified(archive);
    }

    /// <summary>Every way the tarball's bytes differ from what the pin records: the version, the archived flag, the length and both digests.</summary>
    /// <param name="tarball">The archived tarball's bytes.</param>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz pin via Broiler.VM TzdbPin; IP=Low; Security=High; Resources=3; Fingerprint=394CF4
    // Broiler-Falsified-If: a tarball one byte or one length away from the pin's yields no violation
    // Broiler-Human:        PENDING
    public IEnumerable<string> TarballViolations(byte[] tarball)
    {
        ArgumentNullException.ThrowIfNull(tarball);

        if (!string.Equals(Version, PinnedVersion, StringComparison.Ordinal))
        {
            yield return "tzdb.pin names version `" + Version + "`, and the time zone data is taken from " + PinnedVersion + " only";
        }

        if (!Values.TryGetValue("archived", out var archived) || archived != "yes")
        {
            yield return "tzdb.pin does not say `archived yes`, and nothing reads unarchived bytes";
        }

        var recorded = Values.TryGetValue("archive", out var archive) ? archive.Split(' ') : [];

        if (recorded.Length != 2 || !ArchiveReading.TryParseLength(recorded[1], out var length))
        {
            yield return "tzdb.pin does not record `archive <file> <bytes>`";
            yield break;
        }

        if (tarball.LongLength != length)
        {
            yield return string.Create(NumberFormatInfo.InvariantInfo, $"{ArchiveName} is {tarball.LongLength} bytes, and the pin records {length}");
        }

        var digest = ArchiveReading.Sha256(tarball);

        if (!Values.TryGetValue("archive-sha256", out var sha256) || !string.Equals(digest, sha256, StringComparison.Ordinal))
        {
            yield return ArchiveName + " hashes to " + digest + ", and the pin records " + sha256;
        }

        var sha512 = ArchiveReading.Sha512(tarball);

        if (!Values.TryGetValue("archive-sha512", out var pinned512) || !string.Equals(sha512, pinned512, StringComparison.Ordinal))
        {
            yield return ArchiveName + "'s SHA-512 is " + sha512 + ", and the pin records " + pinned512;
        }
    }

    /// <summary>
    /// Every way extracted members differ from what the pin records: a missing member, a length, a hash,
    /// or a stated version that is not the pinned one. Empty when they are the pinned ones.
    /// </summary>
    /// <param name="archive">Each member's bytes, by its name in the tarball.</param>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz pin via Broiler.VM TzdbPin; IP=Low; Security=High; Resources=3; Fingerprint=4DB748
    // Broiler-Falsified-If: a member whose length, hash or stated version differs from the pin yields no violation
    // Broiler-Human:        PENDING
    public IEnumerable<string> Violations(IReadOnlyDictionary<string, byte[]> archive)
    {
        ArgumentNullException.ThrowIfNull(archive);

        foreach (var member in Members)
        {
            if (!archive.TryGetValue(member.Name, out var bytes))
            {
                yield return ArchiveName + " has no member `" + member.Name + "`, which the pin names";
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

        if (archive.TryGetValue("version", out var version) &&
            !string.Equals(Decode(version).Trim(), PinnedVersion, StringComparison.Ordinal))
        {
            yield return "the release states version `" + Decode(version).Trim() + "`, and the pin is " + PinnedVersion;
        }
    }

    /// <summary>The members, verified, or an exception naming every way they are not the pinned ones.</summary>
    /// <param name="archive">Each member's bytes, by its name in the tarball.</param>
    /// <exception cref="InvalidDataException">The members are not the ones the pin describes.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz pin via Broiler.VM TzdbPin; IP=Low; Security=High; Resources=3; Fingerprint=FE7B8A
    // Broiler-Falsified-If: members with a violation are returned instead of refused
    // Broiler-Human:        PENDING
    public IReadOnlyDictionary<string, byte[]> Verified(IReadOnlyDictionary<string, byte[]> archive)
    {
        var violations = Violations(archive).ToArray();

        if (violations.Length > 0)
        {
            throw Refusal(violations);
        }

        return archive;
    }

    /// <summary>Strict UTF-8: a byte sequence that is not UTF-8 is a refusal, not a replacement character.</summary>
    /// <param name="bytes">The bytes.</param>
    /// <exception cref="System.Text.DecoderFallbackException">The bytes are not UTF-8.</exception>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz pin via Broiler.VM TzdbPin; IP=Low; Security=Medium; Resources=3; Fingerprint=DAAAC3
    // Broiler-Human:        PENDING
    public static string Decode(byte[] bytes) => ArchiveReading.Decode(bytes);

    /// <summary>The exception every refusal is, naming each violation on its own line.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz pin via Broiler.VM TzdbPin; IP=Low; Security=Low; Resources=2; Fingerprint=D1088C
    // Broiler-Human:        PENDING
    private static InvalidDataException Refusal(IEnumerable<string> violations) =>
        new("The tzdb archive is not the one tzdb.pin describes, so nothing is generated from it:\n  " +
            string.Join("\n  ", violations));
}
