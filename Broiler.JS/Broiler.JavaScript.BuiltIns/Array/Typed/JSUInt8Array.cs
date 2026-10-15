using Broiler.JavaScript.ExpressionCompiler;
using System;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Engine;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript;
using Broiler.ECMAScript.Text;

namespace Broiler.JavaScript.BuiltIns.Array.Typed;

[JSClassGenerator("Uint8Array"), JSBaseClass("TypedArray")]
public partial class JSUInt8Array : JSTypedArray
{
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

        var number = (value ?? JSUndefined.Value).DoubleValue;
        if (index >= length)
            return true; // out-of-bounds element write is a successful no-op (spec [[Set]] returns true)
        // ToUint8 is the shared library's (SJL-4.5).
        buffer.buffer[byteOffset + index] = IntegerConversions.ToUint8(number);
        return true;
    }


    /// <summary>
    /// ES2026 §4.3.1 — Uint8Array.fromBase64(str, options)
    /// Creates a new Uint8Array from a Base64-encoded string.
    /// </summary>
    /// <remarks>
    /// The codecs are the shared library's (SJL-5.5); what stays here is what a script observes around
    /// them: the receiver, GetOptionsObject and the option reads in order, bounds, the result object
    /// and the errors.
    /// </remarks>
    [JSExport("fromBase64", Length = 1, Feature = (int)JavaScriptFeatureFlags.Uint8ArrayBase64)]
    public static JSValue FromBase64(in Arguments a)
    {
        var str = a.Get1();
        if (!str.IsString)
            throw JSEngine.NewTypeError("Uint8Array.fromBase64 requires a string argument");
        var options = GetOptionsObject(a.Length > 1 ? a[1] : JSUndefined.Value);
        var alphabet = GetBase64Alphabet(options);
        var lastChunkHandling = GetBase64LastChunkHandling(options);
        var text = str.ToString();

        // No maxLength: a destination no decode of the text can fill.
        var bytes = new byte[Base64Codec.DecodedLengthBound(text.Length)];
        if (Base64Codec.FromBase64(text, alphabet, lastChunkHandling, bytes, out _, out var written, out _) != ResultStatus.Completed)
            throw JSEngine.NewSyntaxError("Invalid base64 string");

        return new JSUInt8Array(bytes.AsSpan(0, written).ToArray());
    }

    /// <summary>
    /// ES2026 §4.3.3 — Uint8Array.fromHex(str)
    /// Creates a new Uint8Array from a hex-encoded string.
    /// </summary>
    [JSExport("fromHex", Feature = (int)JavaScriptFeatureFlags.Uint8ArrayBase64)]
    public static JSValue FromHex(in Arguments a)
    {
        var str = a.Get1();
        if (!str.IsString)
            throw JSEngine.NewTypeError("Uint8Array.fromHex requires a string argument");
        var hex = str.ToString();
        var bytes = new byte[hex.Length / 2];
        if (Base64Codec.FromHex(hex, bytes, out _, out _, out var failure) != ResultStatus.Completed)
            throw HexError(failure);

        return new JSUInt8Array(bytes);
    }

    /// <summary>
    /// ES2026 §4.3.2 — Uint8Array.prototype.toBase64(options)
    /// Returns a Base64-encoded string of the typed array content.
    /// </summary>
    [JSExport("toBase64", Length = 0, Feature = (int)JavaScriptFeatureFlags.Uint8ArrayBase64)]
    public JSValue ToBase64(in Arguments a)
    {
        var options = GetOptionsObject(a.Length > 0 ? a[0] : JSUndefined.Value);
        var alphabet = GetBase64Alphabet(options);
        var omitPadding = options != null && options["omitPadding"].BooleanValue;
        return new JSString(Base64Codec.ToBase64(BytesOf(), alphabet, omitPadding));
    }

    /// <summary>
    /// ES2026 §4.3.4 — Uint8Array.prototype.toHex()
    /// Returns a hex-encoded string of the typed array content.
    /// </summary>
    [JSExport("toHex", Feature = (int)JavaScriptFeatureFlags.Uint8ArrayBase64)]
    public JSValue ToHex(in Arguments a) => new JSString(Base64Codec.ToHex(BytesOf()));

    /// <summary>
    /// ES2026 §4.3.5 — Uint8Array.prototype.setFromBase64(str, options)
    /// Decodes a Base64 string and writes bytes into this typed array.
    /// Returns an object { read, written }.
    /// </summary>
    [JSExport("setFromBase64", Length = 1, Feature = (int)JavaScriptFeatureFlags.Uint8ArrayBase64)]
    public JSValue SetFromBase64(in Arguments a)
    {
        var str = a.Get1();
        if (!str.IsString)
            throw JSEngine.NewTypeError("setFromBase64 requires a string argument");
        if (buffer.isImmutable)
            throw JSEngine.NewTypeError("Cannot write into a Uint8Array backed by an immutable ArrayBuffer");
        var options = GetOptionsObject(a.Length > 1 ? a[1] : JSUndefined.Value);
        var alphabet = GetBase64Alphabet(options);
        var lastChunkHandling = GetBase64LastChunkHandling(options);

        // The bounds are read after the options, whose getters may detach or shrink the buffer. The
        // decode writes straight into the view, at most its length, so the bytes decoded before an
        // error are in the array when the error is thrown.
        var status = Base64Codec.FromBase64(str.ToString(), alphabet, lastChunkHandling, Destination("setFromBase64"), out var read, out var written, out _);
        if (status != ResultStatus.Completed)
            throw JSEngine.NewSyntaxError("Invalid base64 string");

        return ReadAndWritten(read, written);
    }

    /// <summary>
    /// ES2026 §4.3.5 — Uint8Array.prototype.setFromHex(str)
    /// Decodes a hex string and writes bytes into this typed array.
    /// Returns an object { read, written }.
    /// </summary>
    [JSExport("setFromHex", Length = 1, Feature = (int)JavaScriptFeatureFlags.Uint8ArrayBase64)]
    public JSValue SetFromHex(in Arguments a)
    {
        var str = a.Get1();
        if (!str.IsString)
            throw JSEngine.NewTypeError("setFromHex requires a string argument");
        if (buffer.isImmutable)
            throw JSEngine.NewTypeError("Cannot write into a Uint8Array backed by an immutable ArrayBuffer");

        // FromHex stops at the view's length, so text past it is neither read nor judged; until
        // SJL-5.5 the whole text was parsed first, and a bad pair past the destination threw (B1).
        var status = Base64Codec.FromHex(str.ToString(), Destination("setFromHex"), out var read, out var written, out var failure);
        if (status != ResultStatus.Completed)
            throw HexError(failure);

        return ReadAndWritten(read, written);
    }

    /// <summary>
    /// GetOptionsObject: no options read nothing, an object is read, and anything else is a
    /// TypeError; until SJL-5.5 a non-object argument was ignored (B2).
    /// </summary>
    private static JSObject? GetOptionsObject(JSValue options)
    {
        if (options.IsUndefined)
            return null;

        return options as JSObject ?? throw JSEngine.NewTypeError("The options argument must be an object");
    }

    /// <summary>
    /// The alphabet option: only the two Strings name one, so null is a TypeError like any other
    /// value; until SJL-5.5 null read as the default (B3).
    /// </summary>
    private static Base64Alphabet GetBase64Alphabet(JSObject? options)
    {
        var alphabet = options?["alphabet"] ?? JSUndefined.Value;
        if (alphabet.IsUndefined)
            return Base64Alphabet.Base64;

        if (!alphabet.IsString)
            throw JSEngine.NewTypeError("alphabet option must be a string");

        return alphabet.StringValue switch
        {
            "base64" => Base64Alphabet.Base64,
            "base64url" => Base64Alphabet.Base64Url,
            var value => throw JSEngine.NewTypeError($"Invalid alphabet option {value}"),
        };
    }

    /// <summary>The lastChunkHandling option, read as the alphabet is (B3).</summary>
    private static LastChunkHandling GetBase64LastChunkHandling(JSObject? options)
    {
        var lastChunkHandling = options?["lastChunkHandling"] ?? JSUndefined.Value;
        if (lastChunkHandling.IsUndefined)
            return LastChunkHandling.Loose;

        if (!lastChunkHandling.IsString)
            throw JSEngine.NewTypeError("lastChunkHandling option must be a string");

        return lastChunkHandling.StringValue switch
        {
            "loose" => LastChunkHandling.Loose,
            "strict" => LastChunkHandling.Strict,
            "stop-before-partial" => LastChunkHandling.StopBeforePartial,
            var value => throw JSEngine.NewTypeError($"Invalid lastChunkHandling option {value}"),
        };
    }

    /// <summary>
    /// GetUint8ArrayBytes: the view's bytes, or a TypeError when it is detached or out of bounds;
    /// until SJL-5.5 such a view encoded as the empty string (B4).
    /// </summary>
    private ReadOnlySpan<byte> BytesOf()
    {
        if (buffer == null || buffer.isDetached || IsOutOfBounds)
            throw JSEngine.NewTypeError("The Uint8Array is detached or out of bounds");

        return new ReadOnlySpan<byte>(buffer.buffer, byteOffset, length);
    }

    /// <summary>
    /// Where a setFrom… call writes: the view's bytes, whose length is the decode's maxLength, or a
    /// TypeError when it is detached or out of bounds; until SJL-5.5 such a view answered
    /// { read: 0, written: 0 } (B4).
    /// </summary>
    private Span<byte> Destination(string method)
    {
        if (buffer == null || buffer.isDetached || IsOutOfBounds)
            throw JSEngine.NewTypeError($"Uint8Array.prototype.{method} called on a detached or out-of-bounds array");

        return new Span<byte>(buffer.buffer, byteOffset, length);
    }

    private static JSValue ReadAndWritten(int read, int written)
    {
        var result = new JSObject();
        result["read"] = new JSNumber(read);
        result["written"] = new JSNumber(written);
        return result;
    }

    private static JSException HexError(Failure failure) => failure.MessageId == "hex.odd-length"
        ? JSEngine.NewSyntaxError("Invalid hex string length")
        : JSEngine.NewSyntaxError("Invalid hex string");
}
