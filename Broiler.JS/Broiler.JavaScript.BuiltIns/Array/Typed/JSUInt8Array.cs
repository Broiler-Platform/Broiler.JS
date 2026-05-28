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
        var intValue = (value ?? JSUndefined.Value).IntValue;
        if (index >= length)
            return false;
        buffer.buffer[byteOffset + index] = (byte)(uint)intValue;
        return true;
    }

    [JSExport(Length = 1)]
    public static JSValue From(in Arguments a)
    {
        var temp = new JSUInt8Array(TypedArrayParameters.From(in a, BYTES_PER_ELENENT));
        var result = CreateTypedArrayFromConstructor(a.This, temp.Length);
        for (uint i = 0; i < temp.Length; i++)
            result[i] = temp[i];

        return result;
    }

    [JSExport]
    public static JSValue Of(in Arguments a)
    {
        var r = CreateTypedArrayFromConstructor(a.This, a.Length);
        for (int i = 0; i < a.Length; i++)
        {
            r[(uint)i] = a[i];
        }
        return r;
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
        var bytes = DecodeBase64(str.ToString(), a.Length > 1 ? a[1] : JSUndefined.Value);
        return new JSUInt8Array(bytes);
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
        var bytes = DecodeBase64(str.ToString(), a.Length > 1 ? a[1] : JSUndefined.Value);
        int written = Math.Min(bytes.Length, length);
        System.Array.Copy(bytes, 0, buffer.buffer, byteOffset, written);
        var result = new JSObject();
        result["read"] = new JSNumber(str.ToString().Length);
        result["written"] = new JSNumber(written);
        return result;
    }

    private static byte[] DecodeBase64(string text, JSValue options)
    {
        var alphabet = GetBase64Alphabet(options);
        _ = GetBase64LastChunkHandling(options);

        if (alphabet == Base64UrlAlphabet)
        {
            if (text.IndexOfAny(['+', '/']) >= 0)
                throw JSEngine.NewSyntaxError("Invalid base64url alphabet");

            text = text.Replace('-', '+').Replace('_', '/');
        }
        else if (text.IndexOfAny(['-', '_']) >= 0)
        {
            throw JSEngine.NewSyntaxError("Invalid base64 alphabet");
        }

        try
        {
            return System.Convert.FromBase64String(text);
        }
        catch (FormatException ex)
        {
            throw JSEngine.NewSyntaxError(ex.Message);
        }
    }

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
