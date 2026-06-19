using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/847
public class Issue847Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code);
    }

    // Problems 72/85/86/87/95/96/99: String.prototype.padStart / padEnd built the
    // padding from only the first character of the fill string (PadLeft/PadRight
    // with fillString[0]) instead of the StringPad filler, which is the whole fill
    // string repeated and truncated to the required width.
    [Theory]
    [InlineData("'abc'.padEnd(7, 'def')", "abcdefd")]
    [InlineData("'abc'.padStart(7, 'def')", "defdabc")]
    [InlineData("'abc'.padEnd(11, 'def')", "abcdefdefde")]
    [InlineData("'abc'.padStart(11, 'def')", "defdefdeabc")]
    [InlineData("'42'.padEnd(7, 'bloop')", "42bloop")]
    [InlineData("'abc'.padEnd(10, false)", "abcfalsefa")]
    [InlineData("'abc'.padStart(10, false)", "falsefaabc")]
    public void PadRepeatsWholeFillString(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }

    // The default fill string is a single space, and a maxLength that does not
    // exceed the current length returns the string unchanged.
    [Theory]
    [InlineData("'abc'.padStart(6)", "   abc")]
    [InlineData("'abc'.padEnd(6)", "abc   ")]
    [InlineData("'abc'.padStart(2, 'x')", "abc")]
    [InlineData("'abc'.padEnd(2, 'x')", "abc")]
    [InlineData("'abc'.padEnd(7, '')", "abc")]
    public void PadEdgeCases(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }

    // maxLength is coerced (ToLength) before fillString is coerced (ToString), and
    // fillString must not be coerced at all when no padding is required.
    [Fact]
    public void FillStringNotCoercedWhenNoPaddingNeeded()
    {
        var code = "var coerced = false;"
            + "var fill = { toString() { coerced = true; return 'x'; } };"
            + "'abc'.padEnd(2, fill);"
            + "coerced;";
        Assert.False(Eval(code).BooleanValue);
    }

    // Problem 91: a computed PropertyName is evaluated — including ToPropertyKey's
    // observable ToString — before the property value expression. Here the key's
    // toString mutates `value` from "bad" to "ok" and the value read must see "ok".
    [Fact]
    public void ComputedPropertyKeyEvaluatedBeforeValue()
    {
        var code = "var value='bad';"
            + "var key={ toString() { value='ok'; return 'p'; } };"
            + "var obj={ [key]: value };"
            + "obj.p;";
        Assert.Equal("ok", Eval(code).ToString());
    }

    // The computed key's side effects ordering also applies in the object-literal
    // path that builds via a temp (a __proto__ setter / super reference present).
    [Fact]
    public void ComputedPropertyKeyEvaluatedBeforeValueWithProto()
    {
        var code = "var log=[];"
            + "var key={ toString() { log.push('key'); return 'p'; } };"
            + "var obj={ __proto__: null, [key]: (log.push('val'), 1) };"
            + "log.join(',');";
        Assert.Equal("key,val", Eval(code).ToString());
    }

    // Problem 82: NamedEvaluation of a short-circuit assignment (`||=`/`&&=`/`??=`)
    // names the anonymous function only when the LHS is a bare IdentifierReference.
    // Parenthesizing the target suppresses the name (it is no longer an IdentifierRef).
    [Theory]
    [InlineData("let a; a ??= function(){}; a.name", "a")]
    [InlineData("let a; (a) ??= function(){}; a.name", "")]
    [InlineData("let a=false; a ||= function(){}; a.name", "a")]
    [InlineData("let a=false; (a) ||= function(){}; a.name", "")]
    [InlineData("let a=true; a &&= function(){}; a.name", "a")]
    [InlineData("let a=true; (a) &&= function(){}; a.name", "")]
    [InlineData("let a=false; (a) ||= () => 1; a.name", "")]
    public void ShortCircuitAssignmentNamedEvaluationRespectsParentheses(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }

    // Plain `=` and compound logical assignment must still name a bare-identifier
    // target, and the parenthesized plain `=` must still suppress.
    [Theory]
    [InlineData("let a; a = function(){}; a.name", "a")]
    [InlineData("let a; (a) = function(){}; a.name", "")]
    public void PlainAssignmentNamedEvaluationRespectsParentheses(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }

    // Problem 93: Map and Set iterators carry the proper [[Symbol.toStringTag]]
    // ("Map Iterator" / "Set Iterator") instead of the internal "Clr Iterator".
    [Theory]
    [InlineData("Object.prototype.toString.call(new Map().entries())", "[object Map Iterator]")]
    [InlineData("Object.prototype.toString.call(new Map().keys())", "[object Map Iterator]")]
    [InlineData("Object.prototype.toString.call(new Map().values())", "[object Map Iterator]")]
    [InlineData("Object.prototype.toString.call(new Set().entries())", "[object Set Iterator]")]
    [InlineData("Object.prototype.toString.call(new Set().keys())", "[object Set Iterator]")]
    [InlineData("Object.prototype.toString.call(new Set().values())", "[object Set Iterator]")]
    [InlineData("new Map().entries()[Symbol.toStringTag]", "Map Iterator")]
    [InlineData("new Set().values()[Symbol.toStringTag]", "Set Iterator")]
    public void MapAndSetIteratorsHaveCorrectToStringTag(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }

    // The retagged Map/Set iterators must still iterate correctly (for-of, spread,
    // manual next(), and destructuring of entries).
    [Theory]
    [InlineData("var m=new Map([[1,2],[3,4]]); [...m].length", "2")]
    [InlineData("var s=new Set([1,2,3]); var t=0; for(const x of s) t+=x; t", "6")]
    [InlineData("var m=new Map([['a',1]]); var o=[]; for(const [k,v] of m.entries()) o.push(k+v); o.join()", "a1")]
    [InlineData("var m=new Map([[1,2]]); m.entries().next().value.join('-')", "1-2")]
    public void MapAndSetIterationStillWorks(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }
}
