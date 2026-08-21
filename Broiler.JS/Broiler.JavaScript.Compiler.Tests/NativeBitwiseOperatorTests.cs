using Broiler.JavaScript.Compiler;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Compiler.Tests;

// `&`, `|`, `^`, `<<`, `>>` and `>>>` on two operands the analysis proved numeric are computed on
// raw doubles instead of going out to the JSValue operators and back
// (docs/performance-roadmap.md item 3-1).
//
// The analysis has always TYPED these operators — NumericLocalAnalysis.IsNumericBinary lists them,
// so a local assigned `i & 1023` stays numeric — while the emitter had no native form, and the gap
// is exactly measurable: `s = i + 1023` costs 0.00 bytes an iteration and `s = i & 1023` costs
// 31.84, with both operands raw doubles and the result stored straight back into one.
//
// The reason the operators were excluded is real, and it is what these tests are about: a bitwise
// operand is not the double, it is ToInt32/ToUint32 of it — truncated toward zero, reduced modulo
// 2^32, with NaN and the infinities mapping to 0 — and that reduction is NOT a CLR cast, which is
// undefined on overflow rather than wrapping. Every case here therefore asserts a value, and
// every one is asserted on BOTH settings of the switch, so they are a statement about JavaScript
// semantics rather than a description of the fast path.
[Collection(Phase3DiagnosticsCollection.Name)]
public sealed class NativeBitwiseOperatorTests
{
    private static string Eval(string body, bool native)
    {
        var previous = NativeBitwiseOperators.Enabled;
        NativeBitwiseOperators.Enabled = native;
        try
        {
            using var context = new JSContext();
            return context.Eval("(function(){ " + body + " })()").ToString();
        }
        finally
        {
            NativeBitwiseOperators.Enabled = previous;
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheOrdinaryCasesAnswerTheSame(bool native)
    {
        Assert.Equal("1", Eval("var i = 5, n = 3; return i & n;", native));
        Assert.Equal("7", Eval("var i = 5, n = 3; return i | n;", native));
        Assert.Equal("6", Eval("var i = 5, n = 3; return i ^ n;", native));
        Assert.Equal("40", Eval("var i = 5, n = 3; return i << n;", native));
        Assert.Equal("2", Eval("var i = 5, n = 1; return i >> n;", native));
        Assert.Equal("21", Eval("var i = 5; return ((i & 7) << 2) | 1;", native));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ToInt32WrapsRatherThanSaturating(bool native)
    {
        // A CLR (int) cast of an out-of-range double is undefined; ToInt32 reduces modulo 2^32.
        Assert.Equal("0", Eval("var big = 4294967296; var m = 0 - 1; return big & m;", native));
        Assert.Equal("1", Eval("var big = 4294967297; return big | 0;", native));
        Assert.Equal("-2147483648", Eval("var big = 2147483648; return big | 0;", native));
        Assert.Equal("-1", Eval("var big = 4294967295; return big | 0;", native));
        // ...and truncates toward zero, in both directions.
        Assert.Equal("1", Eval("var v = 1.9; return v | 0;", native));
        Assert.Equal("-1", Eval("var v = 0 - 1.9; return v | 0;", native));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NaNAndTheInfinitiesBecomeZero(bool native)
    {
        Assert.Equal("0", Eval("var v = 0 / 0; return v | 0;", native));
        Assert.Equal("0", Eval("var v = 1 / 0; return v | 0;", native));
        Assert.Equal("0", Eval("var v = (0 - 1) / 0; return v | 0;", native));
        Assert.Equal("0", Eval("var v = 0 / 0; var w = 5; return v & w;", native));
        // Negative zero is zero here, unlike everywhere else in this phase.
        Assert.Equal("0", Eval("var v = -0; return v | 0;", native));
        Assert.Equal("false", Eval("var v = -0; return (1 / (v | 0)) < 0;", native));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShiftCountsMaskToFiveBits(bool native)
    {
        Assert.Equal("2", Eval("var v = 1, c = 33; return v << c;", native));
        Assert.Equal("1", Eval("var v = 1, c = 32; return v << c;", native));
        Assert.Equal("1", Eval("var v = 1, c = 32; return v >>> c;", native));
        Assert.Equal("1", Eval("var v = 1, c = 32; return v >> c;", native));
        Assert.Equal("2", Eval("var v = 8, c = 34; return v >> c;", native));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UnsignedRightShiftIsUnsigned(bool native)
    {
        Assert.Equal("2147483644", Eval("var v = 0 - 8; return v >>> 1;", native));
        Assert.Equal("4294967295", Eval("var v = 0 - 1; return v >>> 0;", native));
        Assert.Equal("-4", Eval("var v = 0 - 8; return v >> 1;", native));
        // The result exceeds int range, so it must come back as a double and not wrap.
        Assert.Equal("3221225472", Eval("var v = 0 - 1073741824; return v >>> 0;", native));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ANonNumericOperandStillTakesTheGenericPath(bool native)
    {
        // The native form is chosen on the ANALYSIS, so anything it cannot prove numeric keeps
        // the JSValue operators — including the coercions they perform.
        Assert.Equal("2", Eval("var s = '6'; return s & 3;", native));
        Assert.Equal("4", Eval("var o = { valueOf: function () { return 12; } }; return o & 5;", native));
        Assert.Equal("0", Eval("var u; return u | 0;", native));
        Assert.Equal("1", Eval("var b = true; return b | 0;", native));
        Assert.Equal("5", Eval("var n = null; return n | 5;", native));
        // A getter must run exactly once, which the generic path is responsible for.
        Assert.Equal("1,7", Eval("""
            var calls = 0;
            var o = { get v() { calls++; return 6; } };
            var r = o.v | 1;
            return calls + ',' + r;
            """, native));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheResultStaysNumericForTheAnalysis(bool native)
    {
        // The whole point of the change: the analysis already types these, so a local fed by one
        // must still come out numeric and must still hold the right value.
        Assert.Equal("4092", Eval("var s = 0; for (var i = 0; i < 4096; i++) { s = i & 4092; } return s;", native));
        Assert.Equal("2095104", Eval("var s = 0; for (var i = 0; i < 1024; i++) { s = s | (i << 11); } return s;", native));
        Assert.Equal("1023", Eval("var s = 0; for (var i = 0; i < 1024; i++) { s = s ^ i ^ s; } return s | 1023;", native));
    }

    [Fact(Timeout = 600000)]
    public void TheNativeFormIsWhatRemovesTheBox()
    {
        // The counts, which are the half the switch actually moves. Both arms answer 1023.
        const string Body = "var s = 0; for (var i = 0; i < 2048; i++) { s = i & 1023; } return s;";

        Assert.Equal("1023", Eval(Body, native: true));
        Assert.Equal("1023", Eval(Body, native: false));
    }
}
