// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   9
// Annotated:        9/9
// Exempt:           0
// Human-reviewed:   0/9
// IP risk:          Low
// Security risk:    High
// Criteria:         2/2
// Resource impact:  7/10 max
// Unverified:       9
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Broiler.ECMAScript.Generation.Time;

/// <summary>
/// What every pin reader of this package does to bytes: decode them as strict UTF-8, hash them, fold
/// ASCII letters, read numbers through the invariant format, and read the regular files of a gzipped
/// tarball. Pure functions over what the caller hands in; nothing here reads a file.
/// </summary>
/// <remarks>
/// <b>PORTED from Broiler.VM</b>: the tarball reading is the loop its <c>TzdbPin.Extract</c> and
/// <c>CalendarPin.Extract</c> each wrote, in memory over the archive's bytes; the ASCII folding replaces
/// their <c>ToLowerInvariant</c> and <c>ToUpperInvariant</c>, as Broiler.Unicode.Generation's port did,
/// so no platform casing is read (convention 9). On the pinned inputs, which are ASCII, the answers are
/// the same.
/// </remarks>
// Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz pin via Broiler.VM TzdbPin; IP=Low; Security=High; Resources=7; Fingerprint=CD64C8
// Broiler-Falsified-If: a member of a tarball is answered with bytes other than the ones its tar entry holds, or a link, a directory or an unnamed member is answered as a file
// Broiler-Human:        PENDING
internal static class ArchiveReading
{
    /// <summary>Strict UTF-8: a byte sequence that is not UTF-8 is a refusal, not a replacement character.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz pin via Broiler.VM TzdbPin; IP=Low; Security=Medium; Resources=3; Fingerprint=666CE6
    // Broiler-Human:        PENDING
    internal static string Decode(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(bytes);
    }

    /// <summary>A byte sequence's SHA-256, in lower-case hexadecimal.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz pin via Broiler.VM TzdbPin; IP=Low; Security=Medium; Resources=3; Fingerprint=08CC4F
    // Broiler-Human:        PENDING
    internal static string Sha256(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    /// <summary>A byte sequence's SHA-512, in the base64 form the pins write it in.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz pin via Broiler.VM TzdbPin; IP=Low; Security=Medium; Resources=3; Fingerprint=4B2DBF
    // Broiler-Human:        PENDING
    internal static string Sha512(byte[] bytes) => Convert.ToBase64String(SHA512.HashData(bytes));

    /// <summary>A pin's number: an optional sign and ASCII digits, read through the invariant format, or false.</summary>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz pin via Broiler.VM TzdbPin; IP=Low; Security=Medium; Resources=1; Fingerprint=6CADD1
    // Broiler-Human:        PENDING
    internal static bool TryParseLength(string text, out long value) =>
        long.TryParse(text, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out value);

    /// <summary>A hexadecimal digest in lower case, letter by ASCII letter: no culture's or Unicode's case mapping is read.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0053 tz pin; IP=Low; Security=Medium; Resources=2; Fingerprint=00ED57
    // Broiler-Human:        PENDING
    internal static string AsciiLower(string text) =>
        string.Create(text.Length, text, static (span, source) =>
        {
            for (var i = 0; i < source.Length; i++)
            {
                span[i] = AsciiLower(source[i]);
            }
        });

    /// <summary>An ASCII upper-case letter in lower case; every other character as it is.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0053 tz pin; IP=Low; Security=Low; Resources=0; Fingerprint=35F834
    // Broiler-Human:        PENDING
    internal static char AsciiLower(char c) => c is >= 'A' and <= 'Z' ? (char)(c + ('a' - 'A')) : c;

    /// <summary>An ASCII lower-case letter in upper case; every other character as it is.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0053 tz pin; IP=Low; Security=Low; Resources=0; Fingerprint=8B2473
    // Broiler-Human:        PENDING
    internal static char AsciiUpper(char c) => c is >= 'a' and <= 'z' ? (char)(c - ('a' - 'A')) : c;

    /// <summary>
    /// Each regular file of a gzipped tarball whose name is in <paramref name="wanted"/>, with its bytes, in
    /// the tarball's order. Every other entry - a link, a directory, a file not wanted - is skipped unread.
    /// </summary>
    /// <param name="tarball">The gzipped tarball, whose digests the caller has already checked.</param>
    /// <param name="wanted">The member names to read.</param>
    // Broiler-AI:           Origin=Ported; Spec=JSD-0053 tz pin via Broiler.VM TzdbPin; IP=Low; Security=High; Resources=7; Fingerprint=78B9DB
    // Broiler-Falsified-If: an entry that is not a regular file, or whose name is not wanted, is answered, or a wanted file's bytes differ from its tar entry's
    // Broiler-Human:        PENDING
    internal static IEnumerable<KeyValuePair<string, byte[]>> RegularFiles(byte[] tarball, IReadOnlySet<string> wanted)
    {
        using var gzip = new GZipStream(new MemoryStream(tarball, writable: false), CompressionMode.Decompress);
        using var reader = new TarReader(gzip);

        while (reader.GetNextEntry() is { } entry)
        {
            if (entry.EntryType is not (TarEntryType.RegularFile or TarEntryType.V7RegularFile) ||
                !wanted.Contains(entry.Name) || entry.DataStream is null)
            {
                continue;
            }

            using var copy = new MemoryStream();
            entry.DataStream.CopyTo(copy);
            yield return new KeyValuePair<string, byte[]>(entry.Name, copy.ToArray());
        }
    }
}
