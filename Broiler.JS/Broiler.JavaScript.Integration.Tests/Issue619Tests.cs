using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/619
public class Issue619Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code);
    }

    // Problem 1: under proper tail calls (BROILER_SCRIPT_HOST), the trailing
    // expression of an eval body is a tail call and was emitted as an unforced
    // JSTailCall sentinel. eval is a syntactic boundary: its completion value
    // must be materialized, so the call runs even when the eval result is
    // discarded. Previously the call was silently skipped, so a later reference
    // to the (never-initialized) binding raised "undefined is not a function".
    [Theory]
    [InlineData("var ran=0; (function(){ eval('{ function f(){ ran++; } } f();'); })(); ran;")]
    [InlineData("var ran=0; eval('{ function f(){ ran++; } } f();'); ran;")]
    [InlineData("var ran=0; (function(){ eval('function f(){ ran++; } f();'); })(); ran;")]
    [InlineData("var ran=0; function g(){ ran++; } eval('g()'); ran;")]
    public void TrailingCallInEvalRunsEvenWhenResultDiscarded(string code)
    {
        Assert.Equal(1.0, Eval(code).DoubleValue);
    }

    // Problem 1 (continued): a block-scoped function declaration inside direct
    // eval binds a *mutable* self-name (it is a declaration, not a named function
    // expression). Reassigning the name inside the body must be observable to a
    // subsequent read in the same body. Previously the self-name was bound
    // read-only, so the write was silently ignored.
    [Fact]
    public void EvalBlockFunctionSelfNameIsMutable()
    {
        var code = "var cBV;"
            + "(function(){ eval('{ function f(){ f = 123; cBV = f; return 7; } } f();'); })();"
            + "cBV;";
        Assert.Equal(123.0, Eval(code).DoubleValue);
    }

    // Problem 1 (continued): the exact annexB eval-func-block-scoping case body.
    // initialBV() must return the function's value ('decl'), the block-scoped
    // binding must be mutable (currentBV === 123), and the var-scoped binding
    // captured before the call is independent (varBinding() === 'decl').
    [Fact]
    public void AnnexBEvalFuncBlockScoping()
    {
        var code = "var initialBV, currentBV, varBinding;"
            + "(function() {"
            + "  eval("
            + "    '{ function f() { initialBV = f; f = 123; currentBV = f; return \"decl\"; } }varBinding = f; f();'"
            + "  );"
            + "}());"
            + "initialBV() + '|' + currentBV + '|' + varBinding();";
        Assert.Equal("decl|123|decl", Eval(code).ToString());
    }

    // Problem 2: %TypedArray%.from with a source that has no @@iterator method is
    // treated as an array-like (ToObject, then ToLength(Get(source, "length"))).
    // A missing/undefined "length" yields 0 rather than throwing
    // "<source> is not iterable".
    [Theory]
    [InlineData("Int8Array.from({}).length;", 0.0)]
    [InlineData("Int8Array.from(1).length;", 0.0)]
    [InlineData("Int8Array.from(true).length;", 0.0)]
    [InlineData("Int8Array.from(Symbol()).length;", 0.0)]
    [InlineData("Int8Array.from({length:2, 0:'0', 1:'1'}).length;", 2.0)]
    public void TypedArrayFromNonIterableArrayLike(string code, double expected)
    {
        Assert.Equal(expected, Eval(code).DoubleValue);
    }

    [Fact]
    public void TypedArrayFromArrayLikeCopiesByIndex()
    {
        var code = "Array.prototype.join.call(Int8Array.from({0:'0',1:'1',2:'two',9:'n',length:2}), ',');";
        Assert.Equal("0,1", Eval(code).ToString());
    }

    // Problem 1 (continued): property access on a primitive Symbol must consult
    // Symbol.prototype (not Object.prototype). Promise.prototype.catch reads
    // `this.then`; with a Symbol receiver this resolved to undefined and threw
    // "undefined is not a function". A Symbol's wrapper prototype is now
    // Symbol.prototype, mirroring Boolean/Number/String primitives.
    [Fact]
    public void SymbolPrimitiveConsultsSymbolPrototype()
    {
        var code = "var count = 0;"
            + "Symbol.prototype.then = function(){ count += 1; };"
            + "Promise.prototype.catch.call(Symbol());"
            + "count;";
        Assert.Equal(1.0, Eval(code).DoubleValue);
    }

    // Problem 3: ArrayBuffer.prototype.byteLength getter returns +0 for a detached
    // buffer (e.g. after transfer) rather than throwing a TypeError.
    [Fact]
    public void DetachedArrayBufferByteLengthIsZero()
    {
        var code = "var s = new ArrayBuffer(4); s.transfer(8);"
            + "[s.detached, s.byteLength].join('|');";
        Assert.Equal("true|0", Eval(code).ToString());
    }

    [Fact]
    public void TransferToFixedLengthDetachesSource()
    {
        var code = "var s = new ArrayBuffer(4); var d = s.transferToFixedLength(2);"
            + "[s.byteLength, d.byteLength].join('|');";
        Assert.Equal("0|2", Eval(code).ToString());
    }
}
