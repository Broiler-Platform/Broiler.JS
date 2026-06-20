using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Regression coverage for three test262 clusters reported in issue #853:
//   * Object.prototype.toString tag for BigInt/Symbol wrappers (§20.1.3.6).
//   * String.prototype.trim treatment of U+180E (no longer WhiteSpace).
//   * Well-formed JSON.stringify escaping of lone surrogates (ES2019).
public class Issue853Tests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    // ── Cluster: Object.prototype.toString and @@toStringTag ──────────────────
    // BigInt/Symbol are not in the §20.1.3.6 builtin-tag list, so a wrapper tags
    // as "Object" for the builtin tag. The "[object BigInt]"/"[object Symbol]"
    // display comes only from the string @@toStringTag on their prototypes, which
    // a non-string override must NOT be able to fall back to.

    [Fact]
    public void ObjectToString_BigIntPrimitive_UsesPrototypeToStringTag()
        => Assert.Equal("[object BigInt]", Eval("Object.prototype.toString.call(1n);"));

    [Fact]
    public void ObjectToString_SymbolPrimitive_UsesPrototypeToStringTag()
        => Assert.Equal("[object Symbol]", Eval("Object.prototype.toString.call(Symbol());"));

    [Fact]
    public void ObjectToString_BigIntWrapper_DefaultIsBigInt()
        => Assert.Equal("[object BigInt]", Eval("Object.prototype.toString.call(Object(1n));"));

    [Fact]
    public void ObjectToString_BigIntWrapper_NonStringTagFallsBackToObject()
        => Assert.Equal("[object Object]", Eval("""
            var b = Object(1n);
            Object.defineProperty(b, Symbol.toStringTag, { value: 1n });
            Object.prototype.toString.call(b);
        """));

    [Fact]
    public void ObjectToString_SymbolWrapper_NonStringTagFallsBackToObject()
        => Assert.Equal("[object Object]", Eval("""
            var s = Object(Symbol());
            Object.defineProperty(s, Symbol.toStringTag, { value: 123 });
            Object.prototype.toString.call(s);
        """));

    // ── Cluster: String.prototype.trim and U+180E ────────────────────────────
    // U+180E (MONGOLIAN VOWEL SEPARATOR) was reclassified from Zs to Cf in
    // Unicode 6.3, so it is no longer WhiteSpace and must not be trimmed.

    [Fact]
    public void Trim_DoesNotStripMongolianVowelSeparator()
        => Assert.Equal("1", Eval("'\\u180E'.trim().length.toString();"));

    [Fact]
    public void Trim_StillStripsRealWhitespace()
        => Assert.Equal("x", Eval("'\\u0020\\u00A0\\t x \\u3000'.trim();"));

    // ── Cluster: well-formed JSON.stringify (lone surrogate escaping) ─────────
    // Lone (unpaired) surrogates are escaped as \uXXXX; well-formed pairs stay
    // verbatim.

    [Fact]
    public void JsonStringify_EscapesLoneHighSurrogate()
        => Assert.Equal("\"\\ud800\"", Eval("JSON.stringify('\\uD800');"));

    [Fact]
    public void JsonStringify_EscapesLoneLowSurrogate()
        => Assert.Equal("\"\\udc00\"", Eval("JSON.stringify('\\uDC00');"));

    [Fact]
    public void JsonStringify_EscapesReversedSurrogatePair()
        => Assert.Equal("\"\\udc00\\ud800\"", Eval("JSON.stringify('\\uDC00\\uD800');"));

    [Fact]
    public void JsonStringify_PreservesWellFormedSurrogatePair()
        => Assert.Equal("\"\\ud83d\\ude00\"", Eval("""
            var s = JSON.stringify('😀');
            // Re-encode each code unit so the test asserts on the exact bytes.
            var out = '';
            for (var i = 0; i < s.length; i++) {
                var c = s.charCodeAt(i);
                out += (c === 0x22) ? '"' : '\\u' + ('0000' + c.toString(16)).slice(-4);
            }
            out;
        """));
}
