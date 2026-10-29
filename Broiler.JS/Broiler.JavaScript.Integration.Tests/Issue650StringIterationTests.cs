using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/650 — Problem 3
// (test/staging/sm/Array/from_string.js and friends).
//
// String iteration must yield Unicode code points, not UTF-16 code units. Spread,
// for-of, destructuring, Array.from and Set/Map construction over a string with an
// astral character (U+1D11E "𝄞" = surrogate pair 𝄞) previously produced
// two surrogate-half elements instead of one code-point element. Array-like access
// (length, indexing, Object.keys) still uses code units.
public class Issue650StringIterationTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    private const string Clef = "'\\uD834\\uDD1E'"; // "𝄞"

    [Fact]
    public void SpreadYieldsCodePoints()
        => Assert.Equal("1", Eval($"'' + [...{Clef}].length"));

    [Fact]
    public void ArrayFromYieldsCodePoints()
        => Assert.Equal("1", Eval($"'' + Array.from({Clef}).length"));

    [Fact]
    public void ArrayFromAstralElementRoundTrips()
        // The single element is the full surrogate pair (length 2), not a half.
        => Assert.Equal("2", Eval($"'' + Array.from({Clef})[0].length"));

    [Fact]
    public void ForOfYieldsCodePoints()
        => Assert.Equal("1", Eval($"var n=0; for (var c of {Clef}) n++; '' + n"));

    [Fact]
    public void MixedBmpAndAstralSplitsCorrectly()
        => Assert.Equal("3 a b", Eval($"var a=[...('a'+{Clef}+'b')]; '' + a.length + ' ' + a[0] + ' ' + a[2]"));

    [Fact]
    public void DestructuringYieldsCodePoints()
        => Assert.Equal("2 undefined", Eval($"var [x, y] = {Clef}; '' + x.length + ' ' + y"));

    [Fact]
    public void SetConstructionDeduplicatesByCodePoint()
        => Assert.Equal("1", Eval($"'' + new Set({Clef}).size"));

    [Fact]
    public void BasicMultiCharStringStillIterates()
        => Assert.Equal("a,b,c", Eval("[...'abc'].join(',')"));

    // Array-like access remains code-unit based.
    [Fact]
    public void StringLengthAndIndexingStayCodeUnits()
        => Assert.Equal("2 0,1", Eval($"var s={Clef}; '' + s.length + ' ' + Object.keys('ab').join(',')"));
}
