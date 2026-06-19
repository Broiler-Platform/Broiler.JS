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

    // Problem 4: super() invoked from inside an arrow function in a derived constructor
    // produced an instance with the immediate superclass's prototype (the arrow's own
    // call-stack frame carries no new.target, so the runtime fallback was undefined).
    // The lexically-captured new.target (inherited across arrows) is now threaded in.
    [Theory]
    [InlineData("class A{}; class B extends A{ constructor(){ var f=()=>super(); f(); return this; } }; new B() instanceof B", "true")]
    [InlineData("class A{constructor(){this.nt=new.target;}}; class B extends A{ constructor(){ var f=()=>super(); f(); return this; } }; (new B().nt)===B", "true")]
    [InlineData("class A{}; class B extends A{ constructor(){ (()=>(()=>super())())(); return this; } }; new B() instanceof B", "true")]
    [InlineData("class A{}; class B extends A{}; class C extends B{ constructor(){ var f=()=>super(); f(); return this; } }; var c=new C(); (c instanceof C)+','+(c instanceof B)", "true,true")]
    public void ArrowSuperCallUsesEnclosingNewTarget(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }

    // Direct super() and the default derived constructor must keep working.
    [Theory]
    [InlineData("class A{}; class B extends A{ constructor(){ super(); } }; new B() instanceof B", "true")]
    [InlineData("class A{constructor(x){this.x=x;}}; class B extends A{}; new B(7).x", "7")]
    [InlineData("class A{constructor(){this.nt=new.target.name;}}; class B extends A{}; new B().nt", "B")]
    public void DirectSuperAndDefaultConstructorUnaffected(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }

    // Invalid assignment targets are early SyntaxErrors. Previously these lowered to an
    // Expression.Assign onto a non-assignable node and leaked a CLR
    // NotImplementedException / InvalidProgramException (and `this = x` silently wrote
    // to the captured this-binding).
    [Theory]
    [InlineData("(1+2)=5")]
    [InlineData("1=2")]
    [InlineData("(x>0?x:0)=5")]
    [InlineData("(a+b)+=5")]
    [InlineData("null=5")]
    [InlineData("(a,b)=5")]
    [InlineData("(function(){})=5")]
    [InlineData("this=5")]
    [InlineData("this+=5")]
    public void InvalidAssignmentTargetIsSyntaxError(string source)
    {
        var code = "try { eval(" + Quote(source) + "); 'no-error'; } catch (e) { e.constructor.name; }";
        Assert.Equal("SyntaxError", Eval(code).ToString());
    }

    // Valid assignment targets must keep working.
    [Theory]
    [InlineData("var o={}; o.a=5; o.a", "5")]
    [InlineData("var a=[0]; a[0]=9; a[0]", "9")]
    [InlineData("var o={a:1}; o.a+=10; o.a", "11")]
    [InlineData("var x; x=3; x", "3")]
    [InlineData("class A{}; class B extends A{ m(){return 1;} }; class C extends B{ m(){ super.m(); return 2; } }; new C().m()", "2")]
    [InlineData("var o={f(){return this.v;},v:7}; o.f()", "7")]
    public void ValidAssignmentTargetsStillWork(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }

    // A BindingRestElement must be the last formal parameter and may not have a default
    // initializer; these forms are early SyntaxErrors (previously silently accepted).
    [Theory]
    [InlineData("function f(...a,...b){}")]
    [InlineData("function f(...a, b){}")]
    [InlineData("function f(...a=1){}")]
    [InlineData("(...a, b)=>{}")]
    [InlineData("var o={ m(...a, b){} }")]
    [InlineData("class C{ m(...a, b){} }")]
    [InlineData("function* g(...a, b){}")]
    [InlineData("async function h(...a, b){}")]
    public void InvalidRestParameterIsSyntaxError(string source)
    {
        var code = "try { eval(" + Quote(source) + "); 'no-error'; } catch (e) { e.constructor.name; }";
        Assert.Equal("SyntaxError", Eval(code).ToString());
    }

    [Fact]
    public void DynamicFunctionWithInvalidRestParameterIsSyntaxError()
    {
        var code = "try { new Function('...a,...b',''); 'no-error'; } catch (e) { e.constructor.name; }";
        Assert.Equal("SyntaxError", Eval(code).ToString());
    }

    // Valid rest parameters (last position, destructuring rest, methods, arrows) work.
    [Theory]
    [InlineData("function f(a, ...b){ return b.length; }; f(1,2,3)", "2")]
    [InlineData("(function(...a){return a.length;})(1,2,3)", "3")]
    [InlineData("function f(...[a,b]){ return a+b; }; f(1,2)", "3")]
    [InlineData("var o={ m(a, ...rest){ return rest.join(''); } }; o.m(1,2,3)", "23")]
    [InlineData("((...xs)=>xs.length)(1,2,3,4)", "4")]
    [InlineData("function f(...rest){return rest;}; f.length", "0")]
    public void ValidRestParametersStillWork(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }

    // A LabelledStatement may not reuse a label already in its enclosing label set
    // (the set propagates through blocks/loops/switch but resets at function
    // boundaries). These duplicate labels were silently accepted.
    [Theory]
    [InlineData("x: x: 1")]
    [InlineData("x: y: x: 1")]
    [InlineData("a: { a: 1 }")]
    [InlineData("a: { b: { a: 1 } }")]
    [InlineData("a: for(;;){ a: for(;;){} }")]
    public void DuplicateLabelIsSyntaxError(string source)
    {
        var code = "try { eval(" + Quote(source) + "); 'no-error'; } catch (e) { e.constructor.name; }";
        Assert.Equal("SyntaxError", Eval(code).ToString());
    }

    // Distinct / sibling / cross-function labels remain valid.
    [Theory]
    [InlineData("a: b: c: 1", "no-error")]
    [InlineData("a: 1; a: 2;", "no-error")]
    [InlineData("function f(){ a: 1; } a: 2;", "no-error")]
    [InlineData("a: { b: 1 } b: { a: 1 }", "no-error")]
    public void ValidLabelsStillWork(string source, string expected)
    {
        var code = "try { eval(" + Quote(source) + "); 'no-error'; } catch (e) { e.constructor.name; }";
        Assert.Equal(expected, Eval(code).ToString());
    }

    [Fact]
    public void LabelledLoopBreakStillWorks()
        => Assert.Equal("7", Eval("foo: for(var i=0;i<10;i++){ if(i===7){ break foo; } } i").ToString());

    // Class member-name early errors: a static element named "prototype", a
    // generator/async method named "constructor", and a field named "constructor"
    // (static or not) are early SyntaxErrors. Several were silently accepted (or
    // surfaced as a runtime TypeError).
    [Theory]
    [InlineData("class C{ static prototype(){} }")]
    [InlineData("class C{ static get prototype(){} }")]
    [InlineData("class C{ static prototype = 1 }")]
    [InlineData("class C{ *constructor(){} }")]
    [InlineData("class C{ async constructor(){} }")]
    [InlineData("class C{ async *constructor(){} }")]
    [InlineData("class C{ constructor = 1 }")]
    [InlineData("class C{ static constructor = 1 }")]
    public void InvalidClassMemberNameIsSyntaxError(string source)
    {
        var code = "try { eval(" + Quote(source) + "); 'no-error'; } catch (e) { e.constructor.name; }";
        Assert.Equal("SyntaxError", Eval(code).ToString());
    }

    // Valid class members (instance prototype, static method named constructor,
    // computed keys, fields) must still work.
    [Theory]
    [InlineData("class C{ prototype(){} }; 1", "1")]
    [InlineData("class C{ prototype = 1 }; 1", "1")]
    [InlineData("class C{ static constructor(){} }; 1", "1")]
    [InlineData("class C{ static async constructor(){} }; 1", "1")]
    [InlineData("class C{ static x = 42; }; C.x", "42")]
    [InlineData("class A{constructor(){this.v=3;}}; class B extends A{ constructor(){super();} }; new B().v", "3")]
    public void ValidClassMembersStillWork(string code, string expected)
        => Assert.Equal(expected, Eval(code).ToString());

    // A BindingRestElement/BindingRestProperty must be the last element, with no
    // default; an object rest must be a bare identifier (no nested pattern). These
    // were silently accepted (or surfaced as a runtime TypeError).
    [Theory]
    [InlineData("let {...a, b} = {};")]
    [InlineData("let [...a, b] = [];")]
    [InlineData("let {...a = 1} = {};")]
    [InlineData("let {...{a}} = {};")]
    [InlineData("let {...[a]} = {};")]
    [InlineData("let [...a,] = [];")]
    [InlineData("const [...x, y] = [];")]
    [InlineData("var {...a, b} = {};")]
    [InlineData("function f([...a, b]){}")]
    [InlineData("function f({...a, b}){}")]
    public void InvalidPatternRestIsSyntaxError(string source)
    {
        var code = "try { eval(" + Quote(source) + "); 'no-error'; } catch (e) { e.constructor.name; }";
        Assert.Equal("SyntaxError", Eval(code).ToString());
    }

    // Valid rest patterns (rest last, nested array-rest pattern, object rest) work.
    [Theory]
    [InlineData("var [a, ...b] = [1,2,3]; a+','+b.join(',')", "1,2,3")]
    [InlineData("var {a, ...r} = {a:1,b:2,c:3}; Object.values(r).join(',')", "2,3")]
    [InlineData("var [...[x, y]] = [3,4]; x+','+y", "3,4")]
    [InlineData("var [a, [b, ...c]] = [1,[2,3,4]]; c.join(',')", "3,4")]
    [InlineData("function f(a, ...rest){ return a + rest.length; }; f(7,1,2)", "9")]
    public void ValidPatternRestStillWorks(string code, string expected)
        => Assert.Equal(expected, Eval(code).ToString());

    private static string Quote(string code) => "\"" + code.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
