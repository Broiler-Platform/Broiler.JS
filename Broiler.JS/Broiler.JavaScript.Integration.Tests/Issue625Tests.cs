using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/625
public class Issue625Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext(experimentalFeatures: JavaScriptFeatureFlags.AllExperimentalEs2026);
        return ctx.Eval(code);
    }

    // Problem 1: every Intl service constructor's resolvedOptions() must expose a
    // canonical "locale" string, so that the testWithIntlConstructors idiom
    //   var l = new Constructor().resolvedOptions().locale;
    //   Constructor.supportedLocalesOf([l]);
    // round-trips instead of throwing "Locale list entries must be strings...".
    [Theory]
    [InlineData("Collator")]
    [InlineData("DateTimeFormat")]
    [InlineData("NumberFormat")]
    [InlineData("PluralRules")]
    [InlineData("RelativeTimeFormat")]
    [InlineData("ListFormat")]
    [InlineData("Segmenter")]
    public void DefaultLocaleIsSupported(string ctor)
    {
        var code =
            "var C = Intl." + ctor + ";" +
            "var l = new C().resolvedOptions().locale;" +
            "var s = C.supportedLocalesOf([l]);" +
            "(typeof l) + '|' + s.length + '|' + (s[0] === l);";
        Assert.Equal("string|1|true", Eval(code).ToString());
    }

    // supportedLocalesOf must validate the localeMatcher option.
    [Theory]
    [InlineData("'lookup'", "ok")]
    [InlineData("'best fit'", "ok")]
    [InlineData("undefined", "ok")]
    [InlineData("'invalid'", "RangeError")]
    [InlineData("null", "RangeError")]
    [InlineData("0", "RangeError")]
    public void SupportedLocalesOfValidatesLocaleMatcher(string value, string expected)
    {
        var code =
            "var l = new Intl.NumberFormat().resolvedOptions().locale;" +
            "var t;try{ Intl.NumberFormat.supportedLocalesOf([l], {localeMatcher: " + value + "}); t='ok'; }" +
            "catch(e){ t = e.constructor.name; } t;";
        Assert.Equal(expected, Eval(code).ToString());
    }

    [Fact]
    public void ListFormatResolvedOptionsHasTypeAndStyle()
    {
        var code = "var o = new Intl.ListFormat('en', {type:'disjunction'}).resolvedOptions();"
            + "o.type + '|' + o.style;";
        Assert.Equal("disjunction|long", Eval(code).ToString());
    }

    // Problem 3: Base64 decoding follows the ES2026 FromBase64 algorithm.
    [Theory]
    // ASCII whitespace is ignored between code points.
    [InlineData("' S G V s b G 8 = '", "loose", "48656c6c6f")]
    // No padding, loose: the trailing partial chunk is decoded.
    [InlineData("'SGVsbG8'", "loose", "48656c6c6f")]
    // No padding, stop-before-partial: only complete chunks are emitted.
    [InlineData("'SGVsbG8'", "stop-before-partial", "48656c")]
    // Explicit padding.
    [InlineData("'Zg=='", "loose", "66")]
    [InlineData("'Zm8='", "loose", "666f")]
    public void FromBase64Decodes(string strLiteral, string handling, string expectedHex)
    {
        var code = "Uint8Array.fromBase64(" + strLiteral + ", {lastChunkHandling:'" + handling + "'}).toHex();";
        Assert.Equal(expectedHex, Eval(code).ToString());
    }

    [Theory]
    // strict: an unpadded partial trailing chunk is an error.
    [InlineData("Uint8Array.fromBase64('SGVsbG8', {lastChunkHandling:'strict'})")]
    // Trailing garbage after padding.
    [InlineData("Uint8Array.fromBase64('Zg==junk')")]
    // A single leftover character (1 of 4) is never valid, even in loose mode.
    [InlineData("Uint8Array.fromBase64('SGVsb')")]
    public void FromBase64ThrowsOnInvalid(string expr)
    {
        var code = "var t;try{" + expr + ";t='no throw';}catch(e){t=e.constructor.name;}t;";
        Assert.Equal("SyntaxError", Eval(code).ToString());
    }

    [Fact]
    public void Base64UrlAlphabet()
    {
        // 0xfb 0xff 0xfe -> standard "+/+ " ; base64url uses '-' and '_'.
        var code = "Uint8Array.fromBase64('-_8', {alphabet:'base64url'}).toHex();";
        Assert.Equal("fbff", Eval(code).ToString());
    }

    [Fact]
    public void SetFromBase64WritesUpToError()
    {
        // "Zm9vYmFy" decodes to "foobar" (6 bytes); the trailing '!' is invalid.
        // The valid prefix must be written before the SyntaxError is thrown.
        var code =
            "var ta = new Uint8Array(10);" +
            "var threw=false;" +
            "try{ ta.setFromBase64('Zm9vYmFy!'); }catch(e){ threw = e.constructor.name === 'SyntaxError'; }" +
            "threw + '|' + ta.subarray(0,6).toHex();";
        Assert.Equal("true|666f6f626172", Eval(code).ToString());
    }

    [Fact]
    public void SetFromBase64ReportsReadAndWritten()
    {
        var code =
            "var ta = new Uint8Array(10);" +
            "var r = ta.setFromBase64('Zm9vYmFy');" +
            "r.read + '|' + r.written;";
        Assert.Equal("8|6", Eval(code).ToString());
    }

    [Fact]
    public void SetFromBase64StopsAtTargetSize()
    {
        // Target only holds 3 bytes; decoding stops after the first complete chunk.
        var code =
            "var ta = new Uint8Array(3);" +
            "var r = ta.setFromBase64('Zm9vYmFy');" +
            "r.read + '|' + r.written + '|' + ta.toHex();";
        Assert.Equal("4|3|666f6f", Eval(code).ToString());
    }
}
