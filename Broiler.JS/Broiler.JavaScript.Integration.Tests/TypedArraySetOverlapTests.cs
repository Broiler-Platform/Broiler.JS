using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// The correctness pin for %TypedArray%.prototype.set (ES2024 23.2.3.26 — SetTypedArrayFrom
// TypedArray / SetTypedArrayFromArrayLike), written for the roadmap's "immediate correctness
// gate": modernization MOD-M0-8 recorded a SUSPECTED overlap/offset wrong answer, and the gate
// requires the narrowest regression for it before any bulk-copy optimization is priced.
//
// It does not reproduce. Every case below already answers as the specification requires, so
// these tests pin the behaviour rather than fix it — which is exactly what the gate needs them
// for: a fast copy path (MOD-M8-5) may only be introduced while they keep passing.
//
// The cases that actually constrain an implementation are the three DISCRIMINATING overlap
// tests. When source and target share a buffer, SetTypedArrayFromTypedArray clones the source
// data first, so the result must be what a copy-then-write produces; a naive element-by-element
// in-place loop reads bytes it has already overwritten and answers differently. Each of those
// three states both answers, so a regression names itself.
public class TypedArraySetOverlapTests
{
    private static string Eval(string body)
    {
        using var ctx = new JSContext(options: new JSContextOptions { ScriptHostMode = true });
        return ctx.Eval("(function () {" + body + "})()").ToString();
    }

    private const string Bytes12345678 =
        "var b = new ArrayBuffer(8); var u8 = new Uint8Array(b); u8.set([1, 2, 3, 4, 5, 6, 7, 8]);";

    // ---- Discriminating overlap: source and target share a buffer, different element types ----

    // Uint16 source over bytes 0..5, Uint8 target over bytes 3..5.
    // clone-then-write: 1,2,3,1,3,5,7,8   naive in-place: 1,2,3,1,3,3,7,8
    [Fact(Timeout = 600000)]
    public void AUint16SourceCopiedOverAnOverlappingUint8TargetClonesFirst()
        => Assert.Equal("1,2,3,1,3,5,7,8", Eval(
            Bytes12345678
            + "new Uint8Array(b, 3, 3).set(new Uint16Array(b, 0, 3));"
            + "return Array.from(u8).join(',');"));

    // Uint32 source over bytes 0..7, Uint8 target over bytes 4..5.
    // clone-then-write: 1,2,3,4,1,5,7,8   naive in-place: 1,2,3,4,1,1,7,8
    [Fact(Timeout = 600000)]
    public void AUint32SourceCopiedOverAnOverlappingUint8TargetClonesFirst()
        => Assert.Equal("1,2,3,4,1,5,7,8", Eval(
            Bytes12345678
            + "new Uint8Array(b, 4, 2).set(new Uint32Array(b, 0, 2));"
            + "return Array.from(u8).join(',');"));

    // Int16 source over bytes 0..5, Uint8 target over bytes 2..4.
    // clone-then-write: 1,2,1,3,5,6,7,8   naive in-place: 1,2,1,1,5,6,7,8
    [Fact(Timeout = 600000)]
    public void AnInt16SourceCopiedOverAnOverlappingUint8TargetClonesFirst()
        => Assert.Equal("1,2,1,3,5,6,7,8", Eval(
            Bytes12345678
            + "new Uint8Array(b, 2, 3).set(new Int16Array(b, 0, 3));"
            + "return Array.from(u8).join(',');"));

    // ---- Same-type overlap in both directions (the plain memmove hazard) ----

    [Fact(Timeout = 600000)]
    public void ASameTypeOverlapShiftingForwardCopiesEveryOriginalElement()
        => Assert.Equal("1,1,2,3,4", Eval(
            "var b = new ArrayBuffer(5); var a = new Uint8Array(b); a.set([1, 2, 3, 4, 5]);"
            + "new Uint8Array(b, 1, 4).set(new Uint8Array(b, 0, 4));"
            + "return Array.from(a).join(',');"));

    [Fact(Timeout = 600000)]
    public void ASameTypeOverlapShiftingBackwardCopiesEveryOriginalElement()
        => Assert.Equal("2,3,4,5,5", Eval(
            "var b = new ArrayBuffer(5); var a = new Uint8Array(b); a.set([1, 2, 3, 4, 5]);"
            + "new Uint8Array(b, 0, 4).set(new Uint8Array(b, 1, 4));"
            + "return Array.from(a).join(',');"));

    [Fact(Timeout = 600000)]
    public void AFourByteOverlapAtAByteOffsetCopiesEveryOriginalElement()
        => Assert.Equal("1,2,1,2,3,4,7,8", Eval(
            Bytes12345678
            + "new Uint8Array(b, 2, 4).set(new Uint8Array(b, 0, 4));"
            + "return Array.from(u8).join(',');"));

    // Setting a typed array from itself leaves it unchanged.
    [Fact(Timeout = 600000)]
    public void SettingATypedArrayFromItselfIsIdentity()
        => Assert.Equal("1,2,3", Eval("var t = new Uint8Array([1, 2, 3]); t.set(t); return Array.from(t).join(',');"));

    // ---- Offset handling ----

    [Fact(Timeout = 600000)]
    public void AnOffsetPlacesATypedArraySourceAtThatIndex()
        => Assert.Equal("0,0,0,9,9,9", Eval(
            "var t = new Uint8Array(6); t.set(new Uint8Array([9, 9, 9]), 3); return Array.from(t).join(',');"));

    [Fact(Timeout = 600000)]
    public void AnOffsetPlacesAnArrayLikeSourceAtThatIndex()
        => Assert.Equal("0,0,7,8", Eval(
            "var t = new Uint8Array(4); t.set([7, 8], 2); return Array.from(t).join(',');"));

    // ToIntegerOrInfinity truncates the offset toward zero; 1.7 is index 1, not a RangeError.
    [Fact(Timeout = 600000)]
    public void AFractionalOffsetIsTruncatedTowardZero()
        => Assert.Equal("0,9,0,0", Eval(
            "var t = new Uint8Array(4); t.set([9], 1.7); return Array.from(t).join(',');"));

    // ---- Range validation ----

    [Theory(Timeout = 600000)]
    [InlineData("t.set(new Uint8Array([1, 2]), 3)")]  // typed source runs past the end
    [InlineData("t.set([1, 2, 3, 4, 5])")]            // array-like source longer than the target
    [InlineData("t.set([1], -1)")]                    // negative offset
    public void ASourceThatWouldNotFitThrowsARangeError(string call)
        => Assert.Equal("RangeError", Eval(
            "var t = new Uint8Array(4); try { " + call + "; return 'no throw'; } catch (e) { return e.name; }"));

    // The offset is validated BEFORE any source element is read, so an out-of-range offset
    // leaves an array-like source completely unobserved.
    [Fact(Timeout = 600000)]
    public void AnOutOfRangeOffsetIsRejectedBeforeAnySourceElementIsRead()
        => Assert.Equal("false", Eval(
            "var read = false; var src = { length: 1, get 0() { read = true; return 5; } };"
            + "var t = new Uint8Array(1); try { t.set(src, 5); } catch (e) {} return String(read);"));

    // ---- Element conversion ----

    [Fact(Timeout = 600000)]
    public void ValuesAreConvertedWithTheTargetsElementType()
        => Assert.Equal("-1,-128,1", Eval(
            "var t = new Int8Array(3); t.set(new Uint8Array([255, 128, 1])); return Array.from(t).join(',');"));

    [Fact(Timeout = 600000)]
    public void AClampedTargetClampsAndRoundsHalfToEven()
        => Assert.Equal("255,0,2", Eval(
            "var t = new Uint8ClampedArray(3); t.set([300, -5, 1.6]); return Array.from(t).join(',');"));

    [Fact(Timeout = 600000)]
    public void AFloatSourceIsTruncatedForAnIntegerTarget()
        => Assert.Equal("1,2", Eval(
            "var b = new ArrayBuffer(16); var f = new Float64Array(b); f[0] = 1.9; f[1] = 2.9;"
            + "var t = new Int32Array(b, 0, 2); t.set(f); return Array.from(t).join(',');"));

    // An array-like hole reads as undefined, so it converts to NaN — 0 in an integer target and
    // NaN in a float one.
    [Fact(Timeout = 600000)]
    public void AHoleInAnArrayLikeSourceConvertsThroughUndefined()
        => Assert.Equal("1,0,3", Eval("var t = new Uint8Array(3); t.set([1, , 3]); return Array.from(t).join(',');"));

    [Fact(Timeout = 600000)]
    public void UndefinedInAnArrayLikeSourceBecomesNaNInAFloatTarget()
        => Assert.Equal("1,NaN", Eval(
            "var t = new Float32Array(2); t.set([1, undefined]); return Array.from(t).join(',');"));

    // Mixing BigInt and Number content is a TypeError, whichever side is which.
    [Theory(Timeout = 600000)]
    [InlineData("var t = new BigInt64Array(2); t.set(new Uint8Array([1, 2]))")]
    [InlineData("var t = new Uint8Array(2); t.set(new BigInt64Array(2))")]
    public void MixingBigIntAndNumberContentThrowsATypeError(string code)
        => Assert.Equal("TypeError", Eval(
            "try { " + code + "; return 'no throw'; } catch (e) { return e.name; }"));
}
