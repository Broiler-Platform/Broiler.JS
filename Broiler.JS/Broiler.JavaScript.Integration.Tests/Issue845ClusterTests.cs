using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/845 — three further
// clusters beyond the Number-formatting fix:
//   * %TypedArray%.prototype.join / toLocaleString capture the length before any
//     side-effecting coercion, reading now-out-of-bounds indices as the empty string.
//   * String.prototype.replace treats "$<...>" literally when the pattern has no named
//     groups.
//   * JSON.parse exposes context.source only while a slot still holds its parsed value.
public class Issue845ClusterTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- TypedArray join/toLocaleString with a resizable buffer (Problems 80, 81, 91-93) ----

    [Theory]
    // Shrinking a fixed-length view during separator coercion leaves it out of bounds;
    // all four original elements stringify to "" → "...".
    [InlineData(@"
        const rab = new ArrayBuffer(4, { maxByteLength: 8 });
        const ta = new Int8Array(rab, 0, 4);
        const evil = { toString() { rab.resize(2); return '.'; } };
        ta.join(evil);", "...")]
    // Length-tracking view: elements past the new length read as "".
    [InlineData(@"
        const rab = new ArrayBuffer(4, { maxByteLength: 8 });
        const ta = new Int8Array(rab);
        const evil = { toString() { rab.resize(2); return '.'; } };
        ta.join(evil);", "0.0..")]
    public void TypedArrayJoinCapturesLengthBeforeCoercion(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    [Fact]
    public void TypedArrayJoinBasicUnaffected()
        => Assert.Equal("1-2-3", Eval("new Int8Array([1,2,3]).join('-')"));

    // ---- String.prototype.replace: "$<name>" literal without named groups (Problems 84, 85) ----

    [Theory]
    [InlineData("'abcd'.replace(/(.)(.)|(x)/, '$<snd>$<fst>')", "$<snd>$<fst>cd")]
    [InlineData("'abcd'.replace(/(.)(.)|(x)/, '$<42$1>')", "$<42a>cd")]
    [InlineData("'a'.replace(/./, '$<a>')", "$<a>")]
    [InlineData("'abcd'.replace(/(.)(.)|(x)/, '$<$1>')", "$<a>cd")]
    public void ReplaceTreatsNamedRefAsLiteralWithoutNamedGroups(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    [Fact]
    public void ReplaceNamedGroupStillExpands()
        => Assert.Equal("01/2024", Eval("'2024-01'.replace(/(?<y>\\d+)-(?<m>\\d+)/, '$<m>/$<y>')"));
}
