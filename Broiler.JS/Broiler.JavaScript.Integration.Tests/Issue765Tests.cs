using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/765.
public class Issue765Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // P38/P39/P40: a function whose observable `prototype` property is overwritten
    // with a primitive is still a constructor (constructorness must not be re-read
    // off the mutable prototype cache field), and the instance it builds falls back
    // to %Object.prototype% per OrdinaryCreateFromConstructor.
    [Fact]
    public void FunctionWithPrimitivePrototypeStaysConstructable()
        => Assert.Equal("true", Eval(
            "function F(){} F.prototype = 1;"
            + " var d = new F();"
            + " '' + (typeof F.prototype === 'number' && Object.prototype.isPrototypeOf(d));"));

    [Fact]
    public void FunctionExpressionWithPrimitivePrototypeStaysConstructable()
        => Assert.Equal("true", Eval(
            "var F = function(){}; F.prototype = 'x';"
            + " '' + (new F() instanceof Object);"));

    // P41: ECMAScript allows quantifier counts up to 2^53-1; .NET caps them at
    // Int32.MaxValue. Building such a regex must not throw, and the (unsatisfiable)
    // count must simply never match.
    [Fact]
    public void HugeQuantifierExactDoesNotThrowAndNeverMatches()
        => Assert.Equal("false", Eval(
            "'' + new RegExp('b{' + Number.MAX_SAFE_INTEGER + '}', 'u').test('')"));

    [Fact]
    public void HugeQuantifierOpenEndedDoesNotThrow()
        => Assert.Equal("false", Eval(
            "'' + new RegExp('b{' + Number.MAX_SAFE_INTEGER + ',}?').test('a')"));

    [Fact]
    public void HugeQuantifierRangeDoesNotThrow()
        => Assert.Equal("false", Eval(
            "'' + new RegExp('b{' + Number.MAX_SAFE_INTEGER + ',' + Number.MAX_SAFE_INTEGER + '}').test('b')"));

    [Fact]
    public void NormalQuantifierStillMatches()
        => Assert.Equal("true", Eval("'' + /b{2,3}/.test('bbb')"));

    // P36: `lastIndex` is a per-instance own data property, not a property of
    // %RegExp.prototype%. Reading it off the prototype previously threw
    // "Failed to convert this to JSRegExp".
    [Fact]
    public void RegExpPrototypeHasNoLastIndex()
        => Assert.Equal("true", Eval(
            "'' + (Object.getOwnPropertyNames(RegExp.prototype).indexOf('lastIndex') === -1)"));

    [Fact]
    public void RegExpInstanceLastIndexStillWorks()
        => Assert.Equal("2", Eval(
            "var re = /a/g; re.exec('xa'); '' + re.lastIndex;"));

    [Fact]
    public void RegExpInstanceLastIndexDescriptorIsSpecCompliant()
        => Assert.Equal("true", Eval(
            "var d = Object.getOwnPropertyDescriptor(/a/, 'lastIndex');"
            + " '' + (d.writable && !d.enumerable && !d.configurable);"));
}
