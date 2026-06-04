using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/636
// Covers the cleanly-isolated subset:
//   Problem 2  - Object.freeze / Object.seal on a String wrapper object.
//   Problem 8  - Symbol.prototype[Symbol.toPrimitive] with a boxed Symbol receiver.
//   Problem 9  - Number.prototype.toExponential spec ordering / range / undefined.
//   Problem 10 - Intl.NumberFormat / DateTimeFormat / Collator callable without `new`.
public class Issue636Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problem 9: Number.prototype.toExponential ----

    // A non-finite value returns "NaN"/"Infinity"/"-Infinity" *before* the
    // fractionDigits range check, so an out-of-range argument on NaN does not throw.
    [Theory]
    [InlineData("NaN.toExponential()", "NaN")]
    [InlineData("NaN.toExponential(0)", "NaN")]
    [InlineData("NaN.toExponential(-1)", "NaN")]
    [InlineData("NaN.toExponential(500)", "NaN")]
    [InlineData("(Infinity).toExponential()", "Infinity")]
    [InlineData("(-Infinity).toExponential(2)", "-Infinity")]
    public void ToExponentialNonFinite(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    // An explicit `undefined` fractionDigits behaves like an omitted argument
    // (minimal significant digits), instead of being coerced to NaN and throwing.
    [Theory]
    [InlineData("(123.456).toExponential()", "1.23456e+2")]
    [InlineData("(123.456).toExponential(undefined)", "1.23456e+2")]
    [InlineData("(123.456).toExponential(4)", "1.2346e+2")]
    [InlineData("(123.456).toExponential(0)", "1e+2")]
    [InlineData("(0).toExponential(4)", "0.0000e+0")]
    [InlineData("(-123.456).toExponential(2)", "-1.23e+2")]
    public void ToExponentialFormatting(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    // The valid fractionDigits range is 0..100 (raised from the old 0..20 limit),
    // checked against the truncated integer value.
    [Fact]
    public void ToExponentialAcceptsUpTo100Digits()
        => Assert.Equal("1." + new string('0', 100) + "e+0", Eval("(1).toExponential(100)"));

    [Theory]
    [InlineData("(1).toExponential(101)")]
    [InlineData("(1).toExponential(-1)")]
    public void ToExponentialOutOfRangeThrows(string code)
        => Assert.StartsWith("RangeError", Eval($"(function(){{ try {{ {code}; return 'no throw'; }} catch (e) {{ return e.constructor.name; }} }})()"));

    // ---- Problem 10: legacy Intl constructors callable without `new` ----

    [Theory]
    [InlineData("typeof Intl.NumberFormat()", "object")]
    [InlineData("typeof Intl.DateTimeFormat()", "object")]
    [InlineData("typeof Intl.Collator()", "object")]
    public void LegacyIntlConstructorsCallableWithoutNew(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    // Non-legacy Intl constructors still require `new`.
    [Fact]
    public void NonLegacyIntlConstructorStillRequiresNew()
        => Assert.Equal("TypeError", Eval("(function(){ try { Intl.PluralRules(); return 'no throw'; } catch (e) { return e.constructor.name; } })()"));

    // ---- Problem 8: Symbol.prototype[Symbol.toPrimitive] with a boxed receiver ----

    [Fact]
    public void SymbolToPrimitiveUnwrapsBoxedSymbol()
        => Assert.Equal("true", Eval("var s = Symbol('x'); '' + (Object(s)[Symbol.toPrimitive]() === s)"));

    [Fact]
    public void SymbolToPrimitiveStillRejectsNonSymbolReceiver()
        => Assert.Equal("TypeError", Eval("(function(){ try { Symbol.prototype[Symbol.toPrimitive].call({}); return 'no throw'; } catch (e) { return e.constructor.name; } })()"));

    // ---- Problem 2: Object.freeze / Object.seal on a String wrapper ----

    [Fact]
    public void FreezeStringWrapperSucceeds()
        => Assert.Equal("true", Eval("var s = new String('abc'); Object.freeze(s); '' + Object.isFrozen(s)"));

    [Fact]
    public void SealStringWrapperSucceeds()
        => Assert.Equal("true", Eval("var s = new String('abc'); Object.seal(s); '' + Object.isSealed(s)"));

    // Freezing must not corrupt the wrapper's characters or length.
    [Fact]
    public void FrozenStringWrapperRetainsCharactersAndLength()
        => Assert.Equal("a,b,c,3", Eval("var s = new String('abc'); Object.freeze(s); '' + s[0] + ',' + s[1] + ',' + s[2] + ',' + s.length"));
}
