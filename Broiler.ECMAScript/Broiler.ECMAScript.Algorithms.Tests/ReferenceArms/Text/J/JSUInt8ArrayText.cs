// REFERENCE ARM J: a test-only copy of Broiler.JS's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-5-001 (docs/evidence/sjl-5-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.JS at 62c42e3 (SJL-4's merge, the commit the area verdict's correspondence reads):
//   Broiler.JS/Broiler.JavaScript.BuiltIns/Array/Typed/JSUInt8Array.cs
//     blob 715ebd3af8a660ed10dd5110e78b7760b1e7640a: Base64Alphabet and Base64UrlAlphabet (:14-15), FromHex
//     (:64-85), ToBase64's encoding (:87-105), ToHex (:107-117), Base64DecodeResult (:146-158), DecodeBase64
//     (:160-273), DecodeBase64Chunk (:284-325), SkipAsciiWhitespace (:327-339), Base64Value (:341-349), SetFromHex
//     (:405-441), TryParseHexByte and HexDigitValue (:443-462)
//
// Changes from the source, none of which changes a result:
//   - line endings normalised to LF where the source has CRLF, and a byte-order mark dropped;
//   - the namespace; the members sit in a static class of their own; private and public members are internal;
//   - the [JSExport...] and [JSPrototypeMethod] attributes are not copied;
//   - each built-in takes what its adapter lines compute and returns the CLR value its result wraps: FromHex
//     takes the String (its non-String TypeError is the adapter's) and returns the bytes where the source
//     builds a JSUInt8Array; ToBase64 takes the view's bytes, the alphabet and omitPadding, which the source
//     reads from its options with GetBase64Alphabet and GetBase64OmitPadding; ToHex takes the view's bytes;
//     SetFromHex takes the String and the destination's bytes (byteOffset 0, length its length) and returns
//     (read, written) where the source builds the result object, its non-String and immutable-buffer
//     TypeErrors being the adapter's;
//   - DecodeBase64 takes the alphabet and the last-chunk handling as the option readers return them, where the
//     source takes the options value and calls GetBase64Alphabet and GetBase64LastChunkHandling first;
//   - every JSEngine.NewSyntaxError(message) is the recorded ArmError("SyntaxError", message), and
//     Base64DecodeResult's and DecodeBase64Chunk's JSException is ArmError.

using System;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Text.J;

/// <summary>Broiler.JS's base64 and hex members, as copied.</summary>
internal static class JSUInt8ArrayText
{
    internal const string Base64Alphabet = "base64";

    internal const string Base64UrlAlphabet = "base64url";

    /// <summary>
    /// ES2026 §4.3.3 — Uint8Array.fromHex(str)
    /// Creates a new Uint8Array from a hex-encoded string.
    /// </summary>
    internal static byte[] FromHex(string hex)
    {
        if (hex.Length % 2 != 0)
            throw new ArmError("SyntaxError", "Invalid hex string length");
        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            if (!TryParseHexByte(hex, i * 2, out var b))
                throw new ArmError("SyntaxError", "Invalid hex string");
            bytes[i] = b;
        }
        return bytes;
    }

    /// <summary>
    /// ES2026 §4.3.2 — Uint8Array.prototype.toBase64()
    /// Returns a Base64-encoded string of the typed array content.
    /// </summary>
    internal static string ToBase64(byte[] src, string alphabet, bool omitPadding)
    {
        var text = System.Convert.ToBase64String(src);
        if (alphabet == Base64UrlAlphabet)
            text = text.Replace('+', '-').Replace('/', '_');
        if (omitPadding)
            text = text.TrimEnd('=');
        return text;
    }

    /// <summary>
    /// ES2026 §4.3.4 — Uint8Array.prototype.toHex()
    /// Returns a hex-encoded string of the typed array content.
    /// </summary>
    internal static string ToHex(byte[] src)
    {
        return System.Convert.ToHexString(src).ToLowerInvariant();
    }

    /// <summary>
    /// Result of decoding a Base64 string per the ES2026 FromBase64 abstract
    /// operation: the bytes decoded so far, the number of input code units
    /// consumed, and a deferred SyntaxError (null on success). Bytes are always
    /// populated, even when <see cref="Error"/> is non-null, so callers such as
    /// setFromBase64 can write the successfully decoded prefix before throwing.
    /// </summary>
    internal readonly struct Base64DecodeResult(byte[] bytes, int read, ArmError? error)
    {
        public readonly byte[] Bytes = bytes;
        public readonly int Read = read;
        public readonly ArmError? Error = error;
    }

    /// <summary>
    /// ES2026 FromBase64 abstract operation. Decodes <paramref name="text"/>
    /// up to <paramref name="maxLength"/> bytes, honouring ASCII whitespace,
    /// '=' padding, the alphabet ("base64"/"base64url") and lastChunkHandling
    /// ("loose"/"strict"/"stop-before-partial") options. Errors are reported via
    /// the returned record's <c>Error</c> rather than thrown so partial output
    /// is preserved.
    /// </summary>
    internal static Base64DecodeResult DecodeBase64(string text, string alphabet, string lastChunkHandling, int maxLength)
    {

        if (maxLength == 0)
            return new Base64DecodeResult(System.Array.Empty<byte>(), 0, null);

        var read = 0;
        var bytes = new System.Collections.Generic.List<byte>();
        Span<int> chunk = stackalloc int[4];
        var chunkLength = 0;
        var index = 0;
        var length = text.Length;

        Base64DecodeResult Fail() => new(bytes.ToArray(), read, new ArmError("SyntaxError", "Invalid base64 string"));

        while (true)
        {
            index = SkipAsciiWhitespace(text, index);
            if (index == length)
            {
                if (chunkLength > 0)
                {
                    if (lastChunkHandling == "stop-before-partial")
                        return new Base64DecodeResult(bytes.ToArray(), read, null);

                    if (lastChunkHandling == "loose")
                    {
                        if (chunkLength == 1)
                            return Fail();

                        DecodeBase64Chunk(bytes, chunk, chunkLength, throwOnExtraBits: false, out _);
                    }
                    else
                    {
                        return Fail();
                    }
                }

                return new Base64DecodeResult(bytes.ToArray(), length, null);
            }

            var ch = text[index];
            index++;

            if (ch == '=')
            {
                if (chunkLength < 2)
                    return Fail();

                index = SkipAsciiWhitespace(text, index);
                if (chunkLength == 2)
                {
                    if (index == length)
                    {
                        if (lastChunkHandling == "stop-before-partial")
                            return new Base64DecodeResult(bytes.ToArray(), read, null);

                        return Fail();
                    }

                    if (text[index] == '=')
                        index = SkipAsciiWhitespace(text, index + 1);
                }

                if (index < length)
                    return Fail();

                var throwOnExtraBits = lastChunkHandling == "strict";
                if (!DecodeBase64Chunk(bytes, chunk, chunkLength, throwOnExtraBits, out var extraBitsError))
                    return new Base64DecodeResult(bytes.ToArray(), read, extraBitsError);

                return new Base64DecodeResult(bytes.ToArray(), length, null);
            }

            if (alphabet == Base64UrlAlphabet)
            {
                if (ch is '+' or '/')
                    return Fail();
                if (ch == '-')
                    ch = '+';
                else if (ch == '_')
                    ch = '/';
            }

            var value = Base64Value(ch);
            if (value < 0)
                return Fail();

            var remaining = maxLength - bytes.Count;
            if ((remaining == 1 && chunkLength == 2) || (remaining == 2 && chunkLength == 3))
                return new Base64DecodeResult(bytes.ToArray(), read, null);

            chunk[chunkLength] = value;
            chunkLength++;
            if (chunkLength == 4)
            {
                DecodeBase64Chunk(bytes, chunk, 4, throwOnExtraBits: false, out _);
                chunkLength = 0;
                read = index;
                if (bytes.Count == maxLength)
                    return new Base64DecodeResult(bytes.ToArray(), read, null);
            }
        }
    }

    /// <summary>
    /// ES2026 DecodeBase64Chunk. Appends the bytes decoded from the first
    /// <paramref name="chunkLength"/> six-bit values of <paramref name="chunk"/>
    /// to <paramref name="bytes"/>. A 2-value chunk yields one byte, a 3-value
    /// chunk two bytes, a 4-value chunk three bytes. When
    /// <paramref name="throwOnExtraBits"/> is set, a non-zero trailing partial
    /// byte yields a SyntaxError (returned via <paramref name="error"/>).
    /// Returns false only when such an error is produced.
    /// </summary>
    internal static bool DecodeBase64Chunk(System.Collections.Generic.List<byte> bytes, Span<int> chunk, int chunkLength, bool throwOnExtraBits, out ArmError? error)
    {
        error = null;
        var v0 = chunk[0];
        var v1 = chunk[1];
        var v2 = chunkLength > 2 ? chunk[2] : 0;
        var v3 = chunkLength > 3 ? chunk[3] : 0;

        var b0 = (byte)((v0 << 2) | (v1 >> 4));
        var b1 = (byte)(((v1 & 0xF) << 4) | (v2 >> 2));
        var b2 = (byte)(((v2 & 0x3) << 6) | v3);

        if (chunkLength == 2)
        {
            if (throwOnExtraBits && b1 != 0)
            {
                error = new ArmError("SyntaxError", "Base64 chunk has unused bits set");
                return false;
            }

            bytes.Add(b0);
        }
        else if (chunkLength == 3)
        {
            if (throwOnExtraBits && b2 != 0)
            {
                error = new ArmError("SyntaxError", "Base64 chunk has unused bits set");
                return false;
            }

            bytes.Add(b0);
            bytes.Add(b1);
        }
        else
        {
            bytes.Add(b0);
            bytes.Add(b1);
            bytes.Add(b2);
        }

        return true;
    }

    internal static int SkipAsciiWhitespace(string text, int index)
    {
        while (index < text.Length)
        {
            var c = text[index];
            if (c != '\t' && c != '\n' && c != '\f' && c != '\r' && c != ' ')
                return index;

            index++;
        }

        return index;
    }

    internal static int Base64Value(char c) => c switch
    {
        >= 'A' and <= 'Z' => c - 'A',
        >= 'a' and <= 'z' => c - 'a' + 26,
        >= '0' and <= '9' => c - '0' + 52,
        '+' => 62,
        '/' => 63,
        _ => -1,
    };

    internal static bool TryParseHexByte(string hex, int offset, out byte value)
    {
        var hi = HexDigitValue(hex[offset]);
        var lo = HexDigitValue(hex[offset + 1]);
        if (hi < 0 || lo < 0)
        {
            value = 0;
            return false;
        }
        value = (byte)((hi << 4) | lo);
        return true;
    }

    internal static int HexDigitValue(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => -1,
    };

    /// <summary>
    /// ES2026 §4.3.5 — Uint8Array.prototype.setFromHex(str)
    /// Decodes a hex string and writes bytes into this typed array.
    /// Returns an object { read, written }.
    /// </summary>
    internal static (int Read, int Written) SetFromHex(string hex, byte[] buffer)
    {
        var length = buffer.Length;
        var byteOffset = 0;
        if (hex.Length % 2 != 0)
            throw new ArmError("SyntaxError", "Invalid hex string length");
        var bytes = new byte[hex.Length / 2];
        int parsed = 0;
        for (int i = 0; i < bytes.Length; i++)
        {
            if (!TryParseHexByte(hex, i * 2, out var b))
            {
                // Write successfully parsed bytes before throwing
                int written = Math.Min(parsed, length);
                System.Array.Copy(bytes, 0, buffer, byteOffset, written);
                throw new ArmError("SyntaxError", "Invalid hex string");
            }
            bytes[i] = b;
            parsed++;
        }
        int totalWritten = Math.Min(parsed, length);
        System.Array.Copy(bytes, 0, buffer, byteOffset, totalWritten);
        return (totalWritten * 2, totalWritten);
    }
}
