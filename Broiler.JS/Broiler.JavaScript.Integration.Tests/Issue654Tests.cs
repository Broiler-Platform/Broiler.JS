using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/654
public class Issue654Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problem 6: toFixed maps NaN fraction digits to 0 (ToIntegerOrInfinity) ----

    [Fact]
    public void ToFixedNaNDigits() => Assert.Equal("0", Eval("Number.prototype.toFixed(Number.NaN)"));

    [Fact]
    public void ToFixedStringDigits() => Assert.Equal("0", Eval("Number.prototype.toFixed('some string')"));

    [Fact]
    public void ToFixedTruncatedDigits() => Assert.Equal("0.0", Eval("Number.prototype.toFixed(1.1)"));

    [Fact]
    public void ToFixedNoArg() => Assert.Equal("0", Eval("Number.prototype.toFixed()"));

    [Fact]
    public void ToFixedStillRejectsInfinity()
        => Assert.Equal("RangeError", Eval("var c='no throw'; try { (0).toFixed(Infinity); } catch (e) { c = e.constructor.name; } c"));

    // ---- Problem 2: new.target permitted in direct eval inside a class field initializer ----

    [Fact]
    public void NewTargetInFieldInitializerDirectEval()
        => Assert.Equal("true undefined", Eval(
            "var executed=false; class C { x = eval('executed=true; new.target;'); } var c=new C(); '' + executed + ' ' + c.x"));

    [Fact]
    public void NewTargetInPrivateFieldInitializerDirectEval()
        => Assert.Equal("true", Eval(
            "var executed=false; class C { #x = eval('executed=true; new.target;'); } new C(); '' + executed"));

    [Fact]
    public void NewTargetInArrowBodyFieldInitializerDirectEval()
        => Assert.Equal("undefined", Eval(
            "class C { x = eval('() => new.target;'); } var c=new C(); '' + c.x()"));

    // Still a SyntaxError at global scope (no regression).
    [Fact]
    public void NewTargetInGlobalDirectEvalStillSyntaxError()
        => Assert.Equal("SyntaxError", Eval("var c='no throw'; try { eval('new.target;'); } catch (e) { c = e.constructor.name; } c"));

    // ---- Problem 8: `static` is a valid instance field/method name ----

    [Fact]
    public void StaticAsBareInstanceField()
        => Assert.Equal("true undefined", Eval(
            "class C { static; } var c=new C(); c.hasOwnProperty('static') + ' ' + c.static"));

    [Fact]
    public void StaticAsAssignedInstanceField()
        => Assert.Equal("1", Eval("class C { static = 1; } var c=new C(); '' + c.static"));

    [Fact]
    public void StaticAsInstanceMethod()
        => Assert.Equal("42", Eval("class C { static() { return 42; } } var c=new C(); '' + c.static()"));

    // The `static` modifier still works in all its real positions (no regression).
    [Fact]
    public void StaticModifierStillWorks()
        => Assert.Equal("1 2 3 4", Eval(
            "class C { static f = 1; static m() { return 2; } static get g() { return 3; } static { C.b = 4; } }"
            + " '' + C.f + ' ' + C.m() + ' ' + C.g + ' ' + C.b"));

    // ---- Problem 9: BigInt of empty/whitespace string is 0n ----

    [Fact]
    public void BigIntEmptyStringIsZero() => Assert.Equal("0", Eval("'' + BigInt('')"));

    [Fact]
    public void BigInt64ArrayEmptyStringElement()
        => Assert.Equal("0 1", Eval("var a=new BigInt64Array(['', '1']); '' + a[0] + ' ' + a[1]"));

    [Fact]
    public void BigIntInvalidStringStillThrows()
        => Assert.Equal("SyntaxError", Eval("var c='no throw'; try { BigInt('foo'); } catch (e) { c = e.constructor.name; } c"));

    // ---- Problem 10: %TypedArray%.prototype.at exists with length 1 ----

    [Fact]
    public void TypedArrayPrototypeAtExists() => Assert.Equal("function 1", Eval(
        "var TA=Object.getPrototypeOf(Int8Array); typeof TA.prototype.at + ' ' + TA.prototype.at.length"));

    [Fact]
    public void TypedArrayAtPositiveAndNegative() => Assert.Equal("10 30 undefined", Eval(
        "var a=new Int8Array([10,20,30]); '' + a.at(0) + ' ' + a.at(-1) + ' ' + a.at(3)"));

    [Fact]
    public void TypedArrayAtIsInheritedBySubclasses() => Assert.Equal("3", Eval(
        "'' + new Uint16Array([1,2,3]).at(-1)"));
}
