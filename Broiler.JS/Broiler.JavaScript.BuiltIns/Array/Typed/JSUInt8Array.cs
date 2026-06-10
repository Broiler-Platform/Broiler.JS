using Broiler.JavaScript.ExpressionCompiler;
using System;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Array.Typed;

[JSClassGenerator("Uint8Array"), JSBaseClass("TypedArray")]
public partial class JSUInt8Array : JSTypedArray
{
    private const string Base64Alphabet = "base64";
    private const string Base64UrlAlphabet = "base64url";

    [JSExport("BYTES_PER_ELEMENT")]
    internal static readonly int BYTES_PER_ELENENT = 1;

    [JSExport(Length = 3)]
    public JSUInt8Array(in Arguments a) : base(new TypedArrayParameters(a, BYTES_PER_ELENENT)) { }

    private JSUInt8Array(TypedArrayParameters a) : base(a) { }

    internal JSUInt8Array(byte[] data) : base(new TypedArrayParameters(data, BYTES_PER_ELENENT)) { }

    public override JSValue GetValue(uint index, JSValue receiver, bool throwError = true)
    {
        if (index < 0 || index >= length)
            return JSUndefined.Value;
        return new JSNumber(buffer.buffer[byteOffset + index]);
    }

    public override bool SetValue(uint index, JSValue value, JSValue receiver, bool throwError = true)
    {
        if (TrySetForeignReceiver(index, value, receiver, throwError, out var foreign))
            return foreign;

        var intValue = (value ?? JSUndefined.Value).IntValue;
        if (index >= length)
            return true; // out-of-bounds element write is a successful no-op (spec [[Set]] returns true)
        buffer.buffer[byteOffset + index] = (byte)(uint)intValue;
        return true;
    }


    /// <summary>
    /// ES2026 §4.3.1 — Uint8Array.fromBase64(str)
    /// Creates a new Uint8Array from a Base64-encoded string.
    /// </summary>
    [JSExport("fromBase64", Length = 1)]
    public static JSValue FromBase64(in Arguments a)
    {
        var str = a.Get1();
        if (!str.IsString)
            throw JSEngine.NewTypeError("Uint8Array.fromBase64 requires a string argument");
        var result = DecodeBase64(str.ToString(), a.Length > 1 ? a[1] : JSUndefined.Value, int.MaxValue);
        if (result.Error != null)
            throw result.Error;
        return new JSUInt8Array(result.Bytes);
    }

    /// <summary>
    /// ES2026 §4.3.3 — Uint8Array.fromHex(str)
    /// Creates a new Uint8Array from a hex-encoded string.
    /// </summary>
    [JSExport("fromHex")]
    public static JSValue FromHex(in Arguments a)
    {
        var str = a.Get1();
        if (!str.IsString)
            throw JSEngine.NewTypeError("Uint8Array.fromHex requires a string argument");
        var hex = str.ToString();
        if (hex.Length % 2 != 0)
            throw JSEngine.NewSyntaxError("Invalid hex string length");
        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            if (!TryParseHexByte(hex, i * 2, out var b))
                throw JSEngine.NewSyntaxError("Invalid hex string");
            bytes[i] = b;
        }
        return new JSUInt8Array(bytes);
    }

    /// <summary>
    /// ES2026 §4.3.2 — Uint8Array.prototype.toBase64()
    /// Returns a Base64-encoded string of the typed array content.
    /// </summary>
    [JSExport("toBase64", Length = 0)]
    public JSValue ToBase64(in Arguments a)
    {
        var alphabet = GetBase64Alphabet(a.Length > 0 ? a[0] : JSUndefined.Value);
        var src = new byte[length];
        System.Array.Copy(buffer.buffer, byteOffset, src, 0, length);
        var text = System.Convert.ToBase64String(src);
        if (alphabet == Base64UrlAlphabet)
            text = text.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return new JSString(text);
    }

    /// <summary>
    /// ES2026 §4.3.4 — Uint8Array.prototype.toHex()
    /// Returns a hex-encoded string of the typed array content.
    /// </summary>
    [JSExport("toHex")]
    public JSValue ToHex(in Arguments a)
    {
        var src = new byte[length];
        System.Array.Copy(buffer.buffer, byteOffset, src, 0, length);
        return new JSString(System.Convert.ToHexString(src).ToLowerInvariant());
    }

    /// <summary>
    /// ES2026 §4.3.5 — Uint8Array.prototype.setFromBase64(str)
    /// Decodes a Base64 string and writes bytes into this typed array.
    /// Returns an object { read, written }.
    /// </summary>
    [JSExport("setFromBase64", Length = 1)]
    public JSValue SetFromBase64(in Arguments a)
    {
        var str = a.Get1();
        if (!str.IsString)
            throw JSEngine.NewTypeError("setFromBase64 requires a string argument");
        if (buffer.isImmutable)
            throw JSEngine.NewTypeError("Cannot write into a Uint8Array backed by an immutable ArrayBuffer");
        // Per spec, maxLength is the byte capacity of the target; FromBase64 never
        // produces more bytes than that, and any successfully decoded bytes are
        // written even when decoding ultimately fails (writes-up-to-error).
        var result = DecodeBase64(str.ToString(), a.Length > 1 ? a[1] : JSUndefined.Value, length);
        int written = Math.Min(result.Bytes.Length, length);
        System.Array.Copy(result.Bytes, 0, buffer.buffer, byteOffset, written);
        if (result.Error != null)
            throw result.Error;
        var obj = new JSObject();
        obj["read"] = new JSNumber(result.Read);
        obj["written"] = new JSNumber(written);
        return obj;
    }

    /// <summary>
    /// Result of decoding a Base64 string per the ES2026 FromBase64 abstract
    /// operation: the bytes decoded so far, the number of input code units
    /// consumed, and a deferred SyntaxError (null on success). Bytes are always
    /// populated, even when <see cref="Error"/> is non-null, so callers such as
    /// setFromBase64 can write the successfully decoded prefix before throwing.
    /// </summary>
    private readonly struct Base64DecodeResult(byte[] bytes, int read, JSException error)
    {
        public readonly byte[] Bytes = bytes;
        public readonly int Read = read;
        public readonly JSException Error = error;
    }

    /// <summary>
    /// ES2026 FromBase64 abstract operation. Decodes <paramref name="text"/>
    /// up to <paramref name="maxLength"/> bytes, honouring ASCII whitespace,
    /// '=' padding, the alphabet ("base64"/"base64url") and lastChunkHandling
    /// ("loose"/"strict"/"stop-before-partial") options. Errors are reported via
    /// the returned record's <c>Error</c> rather than thrown so partial output
    /// is preserved.
    /// </summary>
    private static Base64DecodeResult DecodeBase64(string text, JSValue options, int maxLength)
    {
        var alphabet = GetBase64Alphabet(options);
        var lastChunkHandling = GetBase64LastChunkHandling(options);

        if (maxLength == 0)
            return new Base64DecodeResult(System.Array.Empty<byte>(), 0, null);

        var read = 0;
        var bytes = new System.Collections.Generic.List<byte>();
        Span<int> chunk = stackalloc int[4];
        var chunkLength = 0;
        var index = 0;
        var length = text.Length;

        Base64DecodeResult Fail() => new(bytes.ToArray(), read, JSEngine.NewSyntaxError("Invalid base64 string"));

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
    private static bool DecodeBase64Chunk(System.Collections.Generic.List<byte> bytes, Span<int> chunk, int chunkLength, bool throwOnExtraBits, out JSException error)
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
                error = JSEngine.NewSyntaxError("Base64 chunk has unused bits set");
                return false;
            }

            bytes.Add(b0);
        }
        else if (chunkLength == 3)
        {
            if (throwOnExtraBits && b2 != 0)
            {
                error = JSEngine.NewSyntaxError("Base64 chunk has unused bits set");
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

    private static int SkipAsciiWhitespace(string text, int index)
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

    private static int Base64Value(char c) => c switch
    {
        >= 'A' and <= 'Z' => c - 'A',
        >= 'a' and <= 'z' => c - 'a' + 26,
        >= '0' and <= '9' => c - '0' + 52,
        '+' => 62,
        '/' => 63,
        _ => -1,
    };

    private static string GetBase64Alphabet(JSValue options)
    {
        if (options is JSObject @object)
        {
            var alphabet = @object["alphabet"];
            if (!alphabet.IsNullOrUndefined)
            {
                if (!alphabet.IsString)
                    throw JSEngine.NewTypeError("alphabet option must be a string");

                var value = alphabet.StringValue;
                if (value != Base64Alphabet && value != Base64UrlAlphabet)
                    throw JSEngine.NewTypeError($"Invalid alphabet option {value}");

                return value;
            }
        }

        return Base64Alphabet;
    }

    private static string GetBase64LastChunkHandling(JSValue options)
    {
        if (options is JSObject @object)
        {
            var lastChunkHandling = @object["lastChunkHandling"];
            if (!lastChunkHandling.IsNullOrUndefined)
            {
                if (!lastChunkHandling.IsString)
                    throw JSEngine.NewTypeError("lastChunkHandling option must be a string");

                var value = lastChunkHandling.StringValue;
                if (value != "loose" && value != "strict" && value != "stop-before-partial")
                    throw JSEngine.NewTypeError($"Invalid lastChunkHandling option {value}");

                return value;
            }
        }

        return "loose";
    }

    /// <summary>
    /// ES2026 §4.3.5 — Uint8Array.prototype.setFromHex(str)
    /// Decodes a hex string and writes bytes into this typed array.
    /// Returns an object { read, written }.
    /// </summary>
    [JSExport("setFromHex", Length = 1)]
    public JSValue SetFromHex(in Arguments a)
    {
        var str = a.Get1();
        if (!str.IsString)
            throw JSEngine.NewTypeError("setFromHex requires a string argument");
        if (buffer.isImmutable)
            throw JSEngine.NewTypeError("Cannot write into a Uint8Array backed by an immutable ArrayBuffer");
        var hex = str.ToString();
        if (hex.Length % 2 != 0)
            throw JSEngine.NewSyntaxError("Invalid hex string length");
        var bytes = new byte[hex.Length / 2];
        int parsed = 0;
        for (int i = 0; i < bytes.Length; i++)
        {
            if (!TryParseHexByte(hex, i * 2, out var b))
            {
                // Write successfully parsed bytes before throwing
                int written = Math.Min(parsed, length);
                System.Array.Copy(bytes, 0, buffer.buffer, byteOffset, written);
                throw JSEngine.NewSyntaxError("Invalid hex string");
            }
            bytes[i] = b;
            parsed++;
        }
        int totalWritten = Math.Min(parsed, length);
        System.Array.Copy(bytes, 0, buffer.buffer, byteOffset, totalWritten);
        var result = new JSObject();
        result["read"] = new JSNumber(totalWritten * 2);
        result["written"] = new JSNumber(totalWritten);
        return result;
    }

    private static bool TryParseHexByte(string hex, int offset, out byte value)
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

    private static int HexDigitValue(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => -1,
    };
}
