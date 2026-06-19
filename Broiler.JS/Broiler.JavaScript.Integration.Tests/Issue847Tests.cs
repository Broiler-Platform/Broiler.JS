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

    // Problem 100: an async (non-generator) function's Function.prototype.toString
    // reported the "[native code]" placeholder (and the internal "native" name)
    // because the async wrapper dropped the underlying generator's source text.
    [Theory]
    [InlineData("async function f1(a, b, c) { await a; } f1.toString()", "async function f1(a, b, c) { await a; }")]
    [InlineData("(async () => 1).toString()", "async () => 1")]
    [InlineData("(async function named(y){ await y; }).toString()", "async function named(y){ await y; }")]
    [InlineData("var o={ async m(){ await 1; } }; o.m.toString()", "async m(){ await 1; }")]
    public void AsyncFunctionToStringReportsSource(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }

    // The async-generator and ordinary function source text must remain unaffected.
    [Theory]
    [InlineData("async function* g(){ yield 1; } g.toString()", "async function* g(){ yield 1; }")]
    [InlineData("function plain(x){ return x; } plain.toString()", "function plain(x){ return x; }")]
    public void OtherFunctionToStringUnaffected(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }

    // Problem 98: ToNumeric (arithmetic/bitwise operators, unary +/-, Number()) is
    // ToPrimitive(value, NUMBER), so a user @@toPrimitive must receive the "number"
    // hint — not "default". Addition (`+`) and template/string coercion keep their
    // own hints. The toPrimitive returns the received hint string for inspection.
    [Theory]
    [InlineData("var h; var o={[Symbol.toPrimitive](x){h=x;return 1;}}; Number(o); h", "number")]
    [InlineData("var h; var o={[Symbol.toPrimitive](x){h=x;return 1;}}; o*2; h", "number")]
    [InlineData("var h; var o={[Symbol.toPrimitive](x){h=x;return 1;}}; o-1; h", "number")]
    [InlineData("var h; var o={[Symbol.toPrimitive](x){h=x;return 1;}}; -o; h", "number")]
    [InlineData("var h; var o={[Symbol.toPrimitive](x){h=x;return 1;}}; o&1; h", "number")]
    [InlineData("var h; var o={[Symbol.toPrimitive](x){h=x;return 1;}}; o+''; h", "default")]
    [InlineData("var h; var o={[Symbol.toPrimitive](x){h=x;return 1;}}; String(o); h", "string")]
    public void ToNumericUsesNumberHint(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }

    // The coerced numeric result must still be correct (valueOf-style @@toPrimitive).
    [Theory]
    [InlineData("var o={[Symbol.toPrimitive](){return 21;}}; Number(o)*2", "42")]
    [InlineData("var o={[Symbol.toPrimitive](){return 5;}}; o*3", "15")]
    public void ToNumericCoercionResultIsCorrect(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }

    // Problem 63: a lexical (let/const) declaration whose initializer is an array
    // destructuring ASSIGNMENT lowered to a value-producing try/finally (iterator
    // close) assigned straight into the binding's value setter, which emitted invalid
    // IL (InvalidProgramException). The value is now spilled into a local first.
    [Theory]
    [InlineData("(()=>{ var a; let z = [a] = [5]; return z+','+a; })()", "5,5")]
    [InlineData("(()=>{ var a; const z = [a] = [5]; return z+','+a; })()", "5,5")]
    [InlineData("(()=>{ var a,b; let z = [a,[b]] = [1,[2]]; return a+','+b; })()", "1,2")]
    [InlineData("(()=>{ var a; let z = [a,...r] = [1,2,3]; return a; })()", "1")]
    public void LexicalArrayDestructuringInitializerCompiles(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }

    // A TDZ access in such an initializer (`let y = [y] = []`, where `y` is read as the
    // destructuring target before the let binding is initialized) now throws a proper
    // ReferenceError instead of crashing with InvalidProgramException.
    [Theory]
    [InlineData("(()=>{ try{ let y = [y] = []; }catch(e){ return e.constructor.name; } })()", "ReferenceError")]
    [InlineData("(()=>{ try{ let y = [y] = [,]; }catch(e){ return e.constructor.name; } })()", "ReferenceError")]
    public void LexicalArrayDestructuringInitializerTdzThrowsReferenceError(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }
}
