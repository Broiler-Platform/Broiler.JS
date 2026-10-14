// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   13
// Annotated:        13/13
// Exempt:           7
// Human-reviewed:   0/13
// IP risk:          Low
// Security risk:    High
// Criteria:         5/2
// Resource impact:  3/10 max
// Unverified:       13
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.ECMAScript.Text;

/// <summary>Which base64 alphabet a codec reads or writes: RFC 4648 section 4 or section 5.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 Uint8Array base64; IP=Low; Security=Low; Resources=1; Fingerprint=F5EB23
// Broiler-Human:        PENDING
public enum Base64Alphabet
{
    /// <summary><c>"base64"</c>: <c>+</c> and <c>/</c>.</summary>
    Base64 = 0,

    /// <summary><c>"base64url"</c>: <c>-</c> and <c>_</c>.</summary>
    Base64Url = 1,
}

/// <summary>What <c>FromBase64</c> does with a final chunk of fewer than four characters (the option <c>lastChunkHandling</c>).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 Uint8Array base64; IP=Low; Security=Low; Resources=1; Fingerprint=036834
// Broiler-Human:        PENDING
public enum LastChunkHandling
{
    /// <summary><c>"loose"</c>: decoded, padded or not, its unused bits ignored.</summary>
    Loose = 0,

    /// <summary><c>"strict"</c>: it must be padded and its unused bits zero.</summary>
    Strict = 1,

    /// <summary><c>"stop-before-partial"</c>: an unpadded one is not decoded, and reading stops before it.</summary>
    StopBeforePartial = 2,
}

/// <summary>
/// The base64 and hex codecs of <c>Uint8Array</c>: <c>FromBase64</c> with its last-chunk modes,
/// <c>FromHex</c>, and the encodings of <c>toBase64</c> and <c>toHex</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The adapters keep everything a script can observe around the codec</b> (contract convention
/// 2): <c>GetOptionsObject</c> and the option reads, which an engine's Intl constructors share,
/// receiver validation, detachment and bounds, the result object and the errors. A decode writes into
/// the caller's span, whose length is the clauses' <c>maxLength</c>, and answers how much of the text
/// it read and how many bytes it wrote; <b>the bytes written before an error are written</b>, as
/// <c>setFromBase64</c> and <c>setFromHex</c> store them before they throw. A caller with no
/// destination passes one of <see cref="DecodedLengthBound"/> bytes, which never limits the decode.
/// </para>
/// <para>
/// A decode is charged the text's length plus one before its loop, and nothing when the destination
/// is empty or, for hex, when the text's length is odd; an encode is charged before it writes. Those
/// are Broiler.VM's units before the cut-over (JSD-0070, decision 3).
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 Uint8Array base64; IP=Low; Security=Medium; Resources=2; Fingerprint=6E8BCD
// Broiler-Human:        PENDING
public static class Base64Codec
{
    /// <summary>A destination length no decode of a text of <paramref name="textLength"/> code units can fill, so that it decodes as if <c>maxLength</c> were 2^53 - 1.</summary>
    /// <param name="textLength">The text's length.</param>
    /// <returns>Three bytes for each four code units of the text, and three more.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The length is negative.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 Uint8Array base64; IP=Low; Security=Medium; Resources=1; Fingerprint=AD66B3
    // Broiler-Falsified-If: a decode into a destination of this length stops, or reads less, than it would with no limit
    // Broiler-Human:        PENDING
    public static int DecodedLengthBound(int textLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(textLength);
        return (int)Math.Min(((long)textLength / 4 * 3) + 3, int.MaxValue);
    }

    /// <summary><c>FromBase64</c> (ECMA-262, <c>Uint8Array</c>'s base64 clauses): decodes at most the destination's length.</summary>
    /// <param name="text">The String.</param>
    /// <param name="alphabet">The alphabet.</param>
    /// <param name="lastChunkHandling">What to do with a final partial chunk.</param>
    /// <param name="destination">Where the bytes go; its length is <c>maxLength</c>.</param>
    /// <param name="read">How many code units of the text the written bytes account for.</param>
    /// <param name="written">How many bytes were written, error or not.</param>
    /// <param name="failure">Why the decode stopped, when the status is not <see cref="ResultStatus.Completed"/>.</param>
    /// <param name="charge">The caller's meter: the text's length plus one, first; nothing when the destination is empty. <see langword="null"/> for none.</param>
    /// <returns>
    /// <see cref="ResultStatus.Completed"/>, or <see cref="ResultStatus.Malformed"/>, which is the
    /// clauses' <c>SyntaxError</c>: a character outside the alphabet (<c>base64.invalid-character</c>,
    /// or <c>base64.wrong-alphabet</c> for <c>+</c> or <c>/</c> under base64url), padding after fewer
    /// than two characters (<c>base64.misplaced-padding</c>), one <c>=</c> where two are needed at the end
    /// (<c>base64.incomplete-padding</c>), text after the padding (<c>base64.text-after-padding</c>),
    /// unused bits that are set under strict handling (<c>base64.extra-bits</c>), an unpadded final chunk
    /// under strict handling (<c>base64.unpadded-chunk</c>), or a final chunk of one character
    /// (<c>base64.single-character-chunk</c>).
    /// </returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 Uint8Array base64; IP=Low; Security=High; Resources=3; Fingerprint=2974B4
    // Broiler-Falsified-If: a decode writes past the destination, reports a read count other than the clauses', or completes or fails where the clauses do not
    // Broiler-Human:        PENDING
    public static ResultStatus FromBase64(ReadOnlySpan<char> text, Base64Alphabet alphabet, LastChunkHandling lastChunkHandling, Span<byte> destination, out int read, out int written, out Failure failure, WorkCharge? charge = null)
    {
        read = 0;
        written = 0;
        failure = default;
        if (destination.IsEmpty)
            return ResultStatus.Completed;

        charge?.Invoke((ulong)text.Length + 1);
        Span<int> chunk = stackalloc int[4];
        var chunkLength = 0;
        var at = 0;
        while (true)
        {
            at = SkipAsciiWhitespace(text, at);
            if (at == text.Length)
            {
                if (chunkLength > 0)
                {
                    if (lastChunkHandling == LastChunkHandling.StopBeforePartial)
                        return ResultStatus.Completed;

                    if (lastChunkHandling == LastChunkHandling.Strict)
                        return Refuse(at, "base64.unpadded-chunk", out failure);

                    if (chunkLength == 1)
                        return Refuse(at, "base64.single-character-chunk", out failure);

                    WriteFinal(chunk, chunkLength, destination, ref written);
                }

                read = text.Length;
                return ResultStatus.Completed;
            }

            var unit = text[at];
            at++;
            if (unit == '=')
            {
                if (chunkLength < 2)
                    return Refuse(at - 1, "base64.misplaced-padding", out failure);

                at = SkipAsciiWhitespace(text, at);
                if (chunkLength == 2)
                {
                    if (at == text.Length)
                    {
                        if (lastChunkHandling == LastChunkHandling.StopBeforePartial)
                            return ResultStatus.Completed;

                        return Refuse(at, "base64.incomplete-padding", out failure);
                    }

                    if (text[at] == '=')
                        at = SkipAsciiWhitespace(text, at + 1);
                }

                if (at < text.Length)
                    return Refuse(at, "base64.text-after-padding", out failure);

                if (lastChunkHandling == LastChunkHandling.Strict && FinalHasExtraBits(chunk, chunkLength))
                    return Refuse(at, "base64.extra-bits", out failure);

                WriteFinal(chunk, chunkLength, destination, ref written);
                read = text.Length;
                return ResultStatus.Completed;
            }

            if (alphabet == Base64Alphabet.Base64Url)
            {
                if (unit is '+' or '/')
                    return Refuse(at - 1, "base64.wrong-alphabet", out failure);

                unit = unit switch
                {
                    '-' => '+',
                    '_' => '/',
                    _ => unit,
                };
            }

            var value = SextetOf(unit);
            if (value < 0)
                return Refuse(at - 1, "base64.invalid-character", out failure);

            var remaining = destination.Length - written;
            if ((remaining == 1 && chunkLength == 2) || (remaining == 2 && chunkLength == 3))
                return ResultStatus.Completed;

            chunk[chunkLength++] = value;
            if (chunkLength == 4)
            {
                var bits = (chunk[0] << 18) | (chunk[1] << 12) | (chunk[2] << 6) | chunk[3];
                destination[written++] = (byte)(bits >> 16);
                destination[written++] = (byte)(bits >> 8);
                destination[written++] = (byte)bits;
                chunkLength = 0;
                read = at;
                if (written == destination.Length)
                    return ResultStatus.Completed;
            }
        }
    }

    /// <summary><c>FromHex</c> (ECMA-262, <c>Uint8Array</c>'s base64 clauses): pairs of hexadecimal digits, at most the destination's length.</summary>
    /// <param name="text">The String.</param>
    /// <param name="destination">Where the bytes go; its length is <c>maxLength</c>.</param>
    /// <param name="read">How many code units of the text the written bytes account for.</param>
    /// <param name="written">How many bytes were written, error or not.</param>
    /// <param name="failure">Why the decode stopped, when the status is not <see cref="ResultStatus.Completed"/>.</param>
    /// <param name="charge">The caller's meter: the text's length plus one, after the length is found even; <see langword="null"/> for none.</param>
    /// <returns>
    /// <see cref="ResultStatus.Completed"/>, or <see cref="ResultStatus.Malformed"/>: an odd length
    /// (<c>hex.odd-length</c>), or a pair that is not two hexadecimal digits (<c>hex.invalid-digit</c>),
    /// with the bytes before it written.
    /// </returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 Uint8Array base64; IP=Low; Security=High; Resources=2; Fingerprint=C63508
    // Broiler-Falsified-If: a decode writes past the destination, reads beyond what it wrote, or completes over a text the clauses refuse
    // Broiler-Human:        PENDING
    public static ResultStatus FromHex(ReadOnlySpan<char> text, Span<byte> destination, out int read, out int written, out Failure failure, WorkCharge? charge = null)
    {
        read = 0;
        written = 0;
        failure = default;
        if (text.Length % 2 != 0)
            return Refuse(0, "hex.odd-length", out failure);

        charge?.Invoke((ulong)text.Length + 1);
        while (read < text.Length && written < destination.Length)
        {
            var value = (UriCodec.HexDigitValue(text[read]) << 4) | UriCodec.HexDigitValue(text[read + 1]);
            if (value < 0)
                return Refuse(read, "hex.invalid-digit", out failure);

            read += 2;
            destination[written++] = (byte)value;
        }

        return ResultStatus.Completed;
    }

    /// <summary><c>Uint8Array.prototype.toBase64</c>'s encoding.</summary>
    /// <param name="bytes">The bytes.</param>
    /// <param name="alphabet">The alphabet.</param>
    /// <param name="omitPadding">Whether the final chunk is left unpadded.</param>
    /// <param name="charge">The caller's meter: the byte count times four thirds, plus four, first; <see langword="null"/> for none.</param>
    /// <returns>The base64 text.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The text would be longer than the longest String the runtime can allocate.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 Uint8Array base64; IP=Low; Security=Medium; Resources=2; Fingerprint=CF4207
    // Broiler-Falsified-If: the text is not RFC 4648's encoding of the bytes in the alphabet asked for, or is padded when padding is omitted
    // Broiler-Human:        PENDING
    public static string ToBase64(ReadOnlySpan<byte> bytes, Base64Alphabet alphabet, bool omitPadding, WorkCharge? charge = null)
    {
        var whole = bytes.Length / 3;
        var tail = bytes.Length % 3;
        var longLength = ((long)whole * 4) + (tail == 0 ? 0 : omitPadding ? tail + 1 : 4);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(longLength, StringPadding.MaximumDotNetString, nameof(bytes));
        var length = (int)longLength;
        charge?.Invoke(((ulong)bytes.Length * 4 / 3) + 4);
        var table = alphabet == Base64Alphabet.Base64Url ? Base64UrlTable : Base64Table;
        Span<char> output = length <= 1024 ? stackalloc char[length] : new char[length];
        var to = 0;
        var from = 0;
        for (var group = 0; group < whole; group++, from += 3)
        {
            var bits = (bytes[from] << 16) | (bytes[from + 1] << 8) | bytes[from + 2];
            output[to++] = table[(bits >> 18) & 0x3F];
            output[to++] = table[(bits >> 12) & 0x3F];
            output[to++] = table[(bits >> 6) & 0x3F];
            output[to++] = table[bits & 0x3F];
        }

        if (tail != 0)
        {
            var bits = (bytes[from] << 16) | (tail == 2 ? bytes[from + 1] << 8 : 0);
            output[to++] = table[(bits >> 18) & 0x3F];
            output[to++] = table[(bits >> 12) & 0x3F];
            if (tail == 2)
                output[to++] = table[(bits >> 6) & 0x3F];
            else if (!omitPadding)
                output[to++] = '=';

            if (!omitPadding)
                output[to++] = '=';
        }

        return new string(output[..to]);
    }

    /// <summary><c>Uint8Array.prototype.toHex</c>'s encoding: two lower-case hexadecimal digits per byte.</summary>
    /// <param name="bytes">The bytes.</param>
    /// <param name="charge">The caller's meter: twice the byte count plus one, first; <see langword="null"/> for none.</param>
    /// <returns>The hexadecimal text.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The text would be longer than the longest String the runtime can allocate.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 Uint8Array base64; IP=Low; Security=Medium; Resources=1; Fingerprint=DFFABF
    // Broiler-Falsified-If: a byte is written other than as two lower-case hexadecimal digits
    // Broiler-Human:        PENDING
    public static string ToHex(ReadOnlySpan<byte> bytes, WorkCharge? charge = null)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((long)bytes.Length * 2, StringPadding.MaximumDotNetString, nameof(bytes));
        charge?.Invoke(((ulong)bytes.Length * 2) + 1);
        return Convert.ToHexStringLower(bytes);
    }

    /// <summary><c>SkipAsciiWhitespace</c>: tab, line feed, form feed, carriage return and space.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 Uint8Array base64; IP=Low; Security=Low; Resources=1; Fingerprint=5EBE89
    // Broiler-Human:        PENDING
    private static int SkipAsciiWhitespace(ReadOnlySpan<char> text, int at)
    {
        while (at < text.Length && text[at] is '\t' or '\n' or '\f' or '\r' or ' ')
            at++;

        return at;
    }

    /// <summary>The value of one character of the standard alphabet, or -1.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 Uint8Array base64; IP=Low; Security=Low; Resources=1; Fingerprint=7DAAB8
    // Broiler-Human:        PENDING
    private static int SextetOf(char unit) => unit switch
    {
        >= 'A' and <= 'Z' => unit - 'A',
        >= 'a' and <= 'z' => unit - 'a' + 26,
        >= '0' and <= '9' => unit - '0' + 52,
        '+' => 62,
        '/' => 63,
        _ => -1,
    };

    /// <summary>Whether the bits a final chunk of two or three characters leaves unused are set (<c>DecodeBase64Chunk</c>'s <c>throwOnExtraBits</c>).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 Uint8Array base64; IP=Low; Security=Medium; Resources=1; Fingerprint=CC0BF1
    // Broiler-Human:        PENDING
    private static bool FinalHasExtraBits(ReadOnlySpan<int> chunk, int length) =>
        length == 2 ? (chunk[1] & 0x0F) != 0 : (chunk[2] & 0x03) != 0;

    /// <summary><c>DecodeBase64Chunk</c> for a final chunk of two or three characters: one or two bytes.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 Uint8Array base64; IP=Low; Security=Medium; Resources=1; Fingerprint=0AA078
    // Broiler-Human:        PENDING
    private static void WriteFinal(ReadOnlySpan<int> chunk, int length, Span<byte> destination, ref int written)
    {
        var bits = (chunk[0] << 18) | (chunk[1] << 12) | (length == 3 ? chunk[2] << 6 : 0);
        destination[written++] = (byte)(bits >> 16);
        if (length == 3)
            destination[written++] = (byte)(bits >> 8);
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 Uint8Array base64; IP=Low; Security=Low; Resources=1; Fingerprint=7407F6
    // Broiler-Human:        PENDING
    private static ReadOnlySpan<char> Base64Table => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 Uint8Array base64; IP=Low; Security=Low; Resources=1; Fingerprint=D1BE52
    // Broiler-Human:        PENDING
    private static ReadOnlySpan<char> Base64UrlTable => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 Uint8Array base64; IP=Low; Security=Low; Resources=1; Fingerprint=9DDC75
    // Broiler-Human:        PENDING
    private static ResultStatus Refuse(int position, string messageId, out Failure failure)
    {
        failure = new Failure(ResultStatus.Malformed, position, messageId);
        return ResultStatus.Malformed;
    }
}
