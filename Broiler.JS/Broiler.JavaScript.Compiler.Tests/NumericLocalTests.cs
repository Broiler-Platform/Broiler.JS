using Broiler.JavaScript.Compiler;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Compiler.Tests;

// A `var` local the compiler can prove only ever holds a number is kept in a CLR double
// rather than a heap-allocated JSValue, and arithmetic over such locals stays unboxed
// (docs/performance-roadmap.md P2-2 item 3).
//
// The optimization is only sound if the proof is, so the tests come in two halves. One half
// is the shapes that SHOULD be specialized, checked for ordinary arithmetic correctness
// including the values that make doubles awkward — NaN, both zeroes, the infinities. The
// other half is everything the analysis has to REFUSE: a name that later holds a string or an
// object, one that can be read before its initializer runs, one a closure or a direct eval
// can reach, and the heads of for-in/for-of. A miss in the second half is a miscompilation,
// not a lost optimization.
//
// Shares Phase 3's collection because CompilerSpecializationDiagnostics is a process-wide
// counter set.
[Collection(Phase3DiagnosticsCollection.Name)]
public class NumericLocalTests
{
    private static string Eval(string source)
    {
        using var context = new JSContext();
        return context.Eval(source).ToString();
    }

    /// <summary>Runs a function body and reports how many locals were held as doubles.</summary>
    private static (string Result, long NumericLocals) Compile(string body)
    {
        using var context = new JSContext();
        CompilerSpecializationDiagnostics.Reset();
        var result = context.Eval("(function(){ " + body + " })()").ToString();
        return (result, CompilerSpecializationDiagnostics.Snapshot().NumericLocals);
    }

    private static string Fn(string body) => Eval("(function(){ " + body + " })()");

    // ── the shapes that should specialize ─────────────────────────────────────────────

    [Fact]
    public void ACountedLoopSpecializesItsCounterAndAccumulator()
    {
        var (result, numericLocals) = Compile("var s = 0; for (var i = 0; i < 10; i++) s += i; return s;");

        Assert.Equal("45", result);
        Assert.True(numericLocals >= 2, $"expected both locals to be held as doubles, got {numericLocals}");
    }

    [Theory]
    [InlineData("var s = 0; for (var i = 0; i < 10; i++) s += i; return s;", "45")]
    [InlineData("var t = 0; for (var i = 0; i < 5; i++) { for (var j = 0; j < 5; j++) t += i * j; } return t;", "100")]
    [InlineData("var i = 0, s = 0; while (i < 10) { s += i; i++; } return s;", "45")]
    [InlineData("var i = 0, s = 0; do { s += i; i++; } while (i < 5); return s;", "10")]
    [InlineData("var s = 0; for (var i = 5; i > 0; i--) s += i; return s;", "15")]
    [InlineData("var s = 0; for (var i = 0, j = 10; i < j; i++, j--) s++; return s;", "5")]
    [InlineData("var s = 0; for (var i = 0; i < 10; i++) { if (i == 5) break; s += i; } return s + ',' + i;", "10,5")]
    [InlineData("var s = 0; for (var i = 0; i < 5; i++) { if (i % 2 == 0) continue; s += i; } return s;", "4")]
    [InlineData("for (var i = 0; i < 3; i++) {} return i;", "3")]
    [InlineData("var x = 0; for (var i = 0; i < 100000; i++) x += 1; return x;", "100000")]
    public void LoopsComputeTheSameAnswer(string body, string expected)
        => Assert.Equal(expected, Fn(body));

    [Theory]
    [InlineData("var i = 0; var r = ++i; return r + ',' + i;", "1,1")]
    [InlineData("var i = 0; var r = i++; return r + ',' + i;", "0,1")]
    [InlineData("var i = 5; var r = --i; return r + ',' + i;", "4,4")]
    [InlineData("var i = 5; var r = i--; return r + ',' + i;", "5,4")]
    [InlineData("var i = 0; var a = []; a.push(i++); a.push(i++); return a.join(',') + ',' + i;", "0,1,2")]
    public void UpdateExpressionsKeepTheirValueSemantics(string body, string expected)
        => Assert.Equal(expected, Fn(body));

    [Theory]
    [InlineData("var x = 10; x += 5; x -= 3; x *= 2; x /= 4; return x;", "6")]
    [InlineData("var x = 2; x **= 3; return x;", "8")]
    [InlineData("var x = 17; x %= 5; return x;", "2")]
    [InlineData("var x = 12; x &= 10; return x;", "8")]
    [InlineData("var x = 12; x |= 3; return x;", "15")]
    [InlineData("var x = 12; x ^= 5; return x;", "9")]
    [InlineData("var x = 4; x <<= 2; return x;", "16")]
    [InlineData("var x = 16; x >>= 2; return x;", "4")]
    [InlineData("var x = 0 - 16; x >>>= 28; return x;", "15")]
    [InlineData("var x = 0; x ||= 5; return x;", "5")]
    [InlineData("var x = 1; x &&= 7; return x;", "7")]
    [InlineData("var x = 3; x ??= 9; return x;", "3")]
    public void EveryCompoundAssignmentOperatorStillWorks(string body, string expected)
        // Each operator either has a native double form or falls back through the JSValue
        // one. An operator missed by BOTH would write through the binding's boxing read.
        => Assert.Equal(expected, Fn(body));

    [Theory]
    [InlineData("var a = 2, b = 3, c = 0; c = a * b + a / b * 0 + (a - b); return c;", "5")]
    [InlineData("var a = 12, b = 10; return (a & b) + ',' + (a | b) + ',' + (a ^ b) + ',' + (a << 1) + ',' + (~a);", "8,14,6,24,-13")]
    [InlineData("var a = 7, b = 3; return a % b;", "1")]
    [InlineData("var a = 2, b = 10; return a ** b;", "1024")]
    [InlineData("var a = 10, b = 3, c = 2; return (a - b - c) + ',' + (a - (b - c));", "5,9")]
    [InlineData("var a = 100, b = 5, c = 2; return (a / b / c) + ',' + (a / (b / c));", "10,40")]
    [InlineData("var s = 0; for (var i = 0; i < 4; i++) s += i * i / 2 - i * 2 + 1; return s;", "-1")]
    public void ArithmeticOverSpecializedLocalsIsUnchanged(string body, string expected)
        => Assert.Equal(expected, Fn(body));

    // ── the values that make a double representation awkward ──────────────────────────

    [Theory]
    // Every relational comparison involving NaN is false. A CLR ordered compare answers true
    // for <= and >=, which is why those two are not lowered natively.
    [InlineData("var n = 0 / 0, x = 5; return String(n < x);", "false")]
    [InlineData("var n = 0 / 0, x = 5; return String(n > x);", "false")]
    [InlineData("var n = 0 / 0, x = 5; return String(n <= x);", "false")]
    [InlineData("var n = 0 / 0, x = 5; return String(n >= x);", "false")]
    [InlineData("var n = 0 / 0, x = 5; return String(x <= n);", "false")]
    [InlineData("var n = 0 / 0, x = 5; return String(x >= n);", "false")]
    [InlineData("var n = 0 / 0; return String(n <= n);", "false")]
    [InlineData("var n = 0 / 0; return String(n >= n);", "false")]
    [InlineData("var n = 0 / 0; return String(n == n) + ',' + String(n != n);", "false,true")]
    [InlineData("var x = 5; return String(x <= x) + ',' + String(x >= x);", "true,true")]
    public void NaNComparesFalseAgainstEverything(string body, string expected)
        => Assert.Equal(expected, Fn(body));

    [Theory]
    [InlineData("var one = 1; var z = 0 * (0 - one); return (1 / z) + ',' + Object.is(z, -0);", "-Infinity,true")]
    [InlineData("var one = 1; var z = 0 * (0 - one); return String(1 / (z + 0));", "Infinity")]
    [InlineData("var z = 0, nz = 0 * (0 - 1); return String(z <= nz) + ',' + String(z == nz);", "true,true")]
    [InlineData("var a = 1, b = 0; return String(a / b) + ',' + String((0 - a) / b);", "Infinity,-Infinity")]
    [InlineData("var a = 5, b = 0; return String(a % b);", "NaN")]
    [InlineData("var a = 5, b = 3, c = 0 - 5; return (a % b) + ',' + (c % b);", "2,-2")]
    [InlineData("var a = 0 - 1, b = 0.5; return String(a ** b);", "NaN")]
    [InlineData("var a = 1e308; var b = a * 10; return String(b);", "Infinity")]
    [InlineData("var a = 1, b = 3; return (a / b).toFixed(4);", "0.3333")]
    [InlineData("var x = 0; for (var i = 0; i < 10; i++) x += 0.1; return x.toFixed(4);", "1.0000")]
    public void SpecialValuesSurviveNativeArithmetic(string body, string expected)
        => Assert.Equal(expected, Fn(body));

    // ── the value still behaves as a JavaScript number everywhere it escapes ──────────

    [Theory]
    [InlineData("var i = 5; var j = i; return typeof j;", "number")]
    [InlineData("var i = 5; var a = [i]; return a[0] + ',' + typeof a[0];", "5,number")]
    [InlineData("var i = 5; var o = { v: i }; return o.v + ',' + typeof o.v;", "5,number")]
    [InlineData("var i = 5; return 'n=' + i;", "n=5")]
    [InlineData("var i = 255; return i.toString(16);", "ff")]
    [InlineData("var i = 5; return JSON.stringify({ a: i });", "{\"a\":5}")]
    [InlineData("var i = 42; return i;", "42")]
    [InlineData("var i = 1; var a = ['a', 'b', 'c']; return a[i];", "b")]
    [InlineData("var z = 0; return Object.is(z, 0) + ',' + Object.is(z, -0);", "true,false")]
    [InlineData("var a = 1, b = 2; return (a < b) + ',' + (a == 1) + ',' + (a === 1) + ',' + (a != b);", "true,true,true,true")]
    [InlineData("var o = { v: 3 }; var i = 1; return String(i < o.v) + ',' + (i + o.v);", "true,4")]
    [InlineData("var i = 1; return String(i < '2') + ',' + (i + '2');", "true,12")]
    [InlineData("var i = 1; return String(i > true) + ',' + String(i >= true);", "false,true")]
    public void ASpecializedLocalIsIndistinguishableFromAnOrdinaryNumber(string body, string expected)
        => Assert.Equal(expected, Fn(body));

    // ── everything the analysis has to refuse ─────────────────────────────────────────

    [Theory]
    [InlineData("var x = 1; x = 's'; return typeof x + ':' + x;", "string:s")]
    [InlineData("var x = 1; for (var i = 0; i < 2; i++) { if (i == 1) x = 's'; } return typeof x;", "string")]
    [InlineData("var x = 1; x = x + 'a'; return typeof x + ':' + x;", "string:1a")]
    [InlineData("var x = 1; x = {}; return typeof x;", "object")]
    [InlineData("var x = 1; x = undefined; return String(x);", "undefined")]
    [InlineData("var x = 1; x = null; return String(x);", "null")]
    [InlineData("var o = { v: 's' }; var x = 1; x = o.v; return typeof x;", "string")]
    [InlineData("var x = 1n; var y = x + 1n; return typeof y + ':' + y;", "bigint:2")]
    [InlineData("var x = '1'; x++; return typeof x + ':' + x;", "number:2")]
    public void ANameThatCanHoldSomethingElseIsNotSpecialized(string body, string expected)
        => Assert.Equal(expected, Fn(body));

    [Theory]
    // A `var` is observably undefined until its initializer runs, which a double cannot
    // represent — so anything that can be read before that point must be refused.
    [InlineData("var x; return String(x);", "undefined")]
    [InlineData("var r = String(x); var x = 5; return r + ',' + x;", "undefined,5")]
    [InlineData("var c = 0; if (c) { var x = 5; } return String(x);", "undefined")]
    [InlineData("var c = 1; if (c) { var x = 5; } return x + ',' + typeof x;", "5,number")]
    [InlineData("var s = 0; for (var i = 0; i < 3; i++) { var q = i; } return String(q);", "2")]
    public void AVarThatCanBeReadBeforeItsInitializerIsNotSpecialized(string body, string expected)
        => Assert.Equal(expected, Fn(body));

    [Fact]
    public void ANameACloSureCanReachIsNotSpecialized()
    {
        Assert.Equal("6", Fn("var i = 5; var f = function () { return i; }; i = 6; return f();"));
        Assert.Equal("2", Fn("var i = 0; var f = function () { i++; }; f(); f(); return i;"));
    }

    [Theory]
    [InlineData("(function(){ var x = 1; with ({ x: 5 }) { return x; } })()", "5")]
    [InlineData("(function(){ var x = 1; return eval('x'); })()", "1")]
    [InlineData("(function(){ var x = 1; eval('x = 9'); return x; })()", "9")]
    public void WithAndDirectEvalDisableIt(string source, string expected)
        => Assert.Equal(expected, Eval(source));

    [Theory]
    [InlineData("var s = ''; for (var k in { a: 1, b: 2 }) s += k; return s;", "ab")]
    [InlineData("var s = 0; for (var v of [1, 2, 3]) s += v; return s;", "6")]
    [InlineData("var s = 0; for (var v of [1, 2, 3]) s += v; s += 1; return s;", "7")]
    [InlineData("var s = ''; for (var k in { a: 1 }) s += typeof k; return s;", "string")]
    public void AForInOrForOfHeadIsNotSpecialized(string body, string expected)
        // The head binding takes whatever the enumeration yields — a string key for for-in.
        => Assert.Equal(expected, Fn(body));

    [Theory]
    [InlineData("var x = 1; return String(delete x) + ',' + x;", "false,1")]
    [InlineData("var x = 1; return typeof x;", "number")]
    [InlineData("var x = 1; var x = 2; return x;", "2")]
    public void OperatorsThatNeedTheBindingItselfStillWork(string body, string expected)
        => Assert.Equal(expected, Fn(body));

    [Fact]
    public void AParameterIsNeverSpecialized()
    {
        // A parameter's value arrives as a JSValue and nothing proves it is a number.
        Assert.Equal("5", Fn("function g(a) { a = 5; return arguments[0]; } return g(1);"));
        Assert.Equal("number:1", Fn("function g(a) { return typeof a + ':' + a; } return g(1);"));
    }

    // ── control flow and scoping still hold ───────────────────────────────────────────

    [Theory]
    [InlineData("var s = 0; try { for (var i = 0; i < 3; i++) s += i; throw 1; } catch (e) { s += 10; } return s;", "13")]
    [InlineData("var s = 0; try { for (var i = 0; i < 5; i++) { s += i; if (i == 2) throw 1; } } catch (e) {} return s + ',' + i;", "3,2")]
    [InlineData("var x = 2, s = 0; switch (x) { case 1: s = 10; break; case 2: s = 20; break; } return s;", "20")]
    [InlineData("var s = 0; outer: for (var i = 0; i < 3; i++) { for (var j = 0; j < 3; j++) { if (j == 1) continue outer; s++; } } return s;", "3")]
    [InlineData("'use strict'; var s = 0; for (var i = 0; i < 5; i++) s += i; return s;", "10")]
    [InlineData("function g() { var k = 0; for (var i = 0; i < 3; i++) k += i; return k; } return g() + g();", "6")]
    [InlineData("function g() { var s = 0; for (var i = 0; i < arguments.length; i++) s += arguments[i]; return s; } return g(1, 2, 3);", "6")]
    public void ControlFlowIsUnaffected(string body, string expected)
        => Assert.Equal(expected, Fn(body));

    [Fact]
    public void RecursionAcrossFunctionBoundariesIsUnaffected()
        => Assert.Equal("610", Fn("function f(n) { return n < 2 ? n : f(n - 1) + f(n - 2); } return f(15);"));

    // ── which locals a nested function costs ──────────────────────────────────────────
    //
    // Eligibility used to be refused outright to any function containing a nested function,
    // on the grounds that a closure captures through a cell. The relevant question is not
    // whether the function has a closure but whether that closure names the binding, and the
    // difference is most of the ordinary code there is: a counted loop in a function that also
    // defines a helper was boxing its counter every iteration (docs/performance-roadmap.md
    // §6.6). These assert the counts rather than results, because the counts are what the
    // rule changes — a behavioural test passes either way.

    [Theory]
    // The helper mentions neither local, so neither pays for it.
    [InlineData("function h() { return 1; } var s = 0; for (var i = 0; i < 10; i++) s += i; return s;")]
    [InlineData("var h = function () { return 1; }; var s = 0; for (var i = 0; i < 10; i++) s += i; return s;")]
    [InlineData("var h = () => 1; var s = 0; for (var i = 0; i < 10; i++) s += i; return s;")]
    // Never referenced, and declared after the loop: neither changes the answer.
    [InlineData("var s = 0; for (var i = 0; i < 10; i++) s += i; function h() { return 1; } return s;")]
    // A helper that names something else entirely.
    [InlineData("var q = 1; function h() { return q; } var s = 0; for (var i = 0; i < 10; i++) s += i; return s;")]
    public void ANestedFunctionThatNamesNeitherLocalCostsNeither(string body)
    {
        var (result, numericLocals) = Compile(body);
        Assert.Equal("45", result);
        Assert.Equal(2, numericLocals);
    }

    [Fact]
    public void OnlyTheNamesANestedFunctionMentionsAreRefused()
    {
        // `s` is named by the helper and keeps a cell; `i` is not and stays a double.
        var (result, numericLocals) = Compile("function h() { return s; } var s = 0; for (var i = 0; i < 10; i++) s += i; return s;");
        Assert.Equal("45", result);
        Assert.Equal(1, numericLocals);
    }

    [Theory]
    // A nested function that can resolve a name it never mentions gives nothing away safely,
    // so the enclosing function is refused entirely, exactly as before.
    [InlineData("function h() { return eval('1'); } var s = 0; for (var i = 0; i < 10; i++) s += i; return s;")]
    [InlineData("function h() { var o = {}; with (o) { return 1; } } var s = 0; for (var i = 0; i < 10; i++) s += i; return s;")]
    [InlineData("function h() { debugger; } var s = 0; for (var i = 0; i < 10; i++) s += i; return s;")]
    public void ANestedFunctionWithDynamicScopeAccessStillRefusesEverything(string body)
    {
        var (result, numericLocals) = Compile(body);
        Assert.Equal("45", result);
        Assert.Equal(0, numericLocals);
    }

    // The scanner has to see through the same containers the rest of the analysis does.
    // Missing one is not a lost optimization but a miscompile, so each is checked to leave the
    // named binding un-specialized.
    [Theory]
    [InlineData("var h = function () { var q = s; return q; };")]              // variable initializer
    [InlineData("var h = { m: function () { return s; } };")]                  // object literal value
    [InlineData("var h = { get m() { return s; } };")]                         // accessor
    [InlineData("var h = function (a) { switch (a) { case 1: return s; } };")] // switch clause
    [InlineData("var h = function (a) { try { return s; } catch (e) {} };")]   // try block
    [InlineData("var h = function (a) { try { a(); } catch (e) { return s; } };")] // catch block
    [InlineData("var h = function (a = s) { return a; };")]                    // default parameter
    [InlineData("var h = function ({ a = s }) { return a; };")]                // destructuring default
    [InlineData("var h = function () { return `${s}`; };")]                    // template literal
    [InlineData("var h = function () { return () => s; };")]                   // through a further nesting
    [InlineData("class H { m() { return s; } }")]                              // class method
    [InlineData("class H { f = s; }")]                                         // class field initializer
    public void ANameReachedThroughAnyContainerIsRefused(string helper)
    {
        var (result, numericLocals) = Compile(helper + " var s = 0; for (var i = 0; i < 10; i++) s += i; return s;");
        Assert.Equal("45", result);
        // `i` still specializes; `s` must not.
        Assert.Equal(1, numericLocals);
    }

    // ...and the values stay right whichever side of the line a binding falls on.
    [Theory]
    [InlineData("var s = 0; for (var i = 0; i < 10; i++) s = i; var g = function () { return s; }; return g();", "9")]
    [InlineData("var s = 0; for (var i = 0; i < 10; i++) s = i; var g = function () { s = 42; }; g(); return s;", "42")]
    [InlineData("var s = 0; for (var i = 0; i < 10; i++) s = i; var g = function () { s = 'x'; }; g(); return typeof s;", "string")]
    [InlineData("var s = 0; for (var i = 0; i < 10; i++) s = i; var g = function () { s++; }; g(); return s;", "10")]
    [InlineData("var s = 0; var r = function () { return s; }; var w = function (v) { s = v; }; w(7); return r();", "7")]
    [InlineData("var s = 0; var fs = []; for (var i = 0; i < 3; i++) { fs.push(function () { return s; }); s = i; } return fs[0]() + ',' + fs[2]();", "2,2")]
    [InlineData("function h() { return 1; } var s = 0; for (var i = 0; i < 10; i++) s += i; return s + ',' + h();", "45,1")]
    public void CaptureStillObservesTheLiveValue(string body, string expected)
        => Assert.Equal(expected, Fn(body));
}
