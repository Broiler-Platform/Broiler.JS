using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/739
//
// Fixed here:
//
//   Problem 1 (two of the three sm negative-syntax files) —
//     * methDefnGen.js: object-literal method-definition edge cases now reject as
//       SyntaxErrors. A generator marker `*name` must introduce a method, so
//       `{*a : 1}` (and `{*a}` / `{*a = 1}`) are errors; a getter/setter is never a
//       generator, so `{get *a(){}}` / `{set *a(c){}}` are errors; and a leading `*`
//       no longer begins an expression (`{a :* 1}`, `x = * 1`) — it was silently
//       consumed as a phantom generator prefix in the prefix-expression parser.
//     * invalid-parameter-list.js: Function() now parses the parameter text and the
//       body text independently (per §20.2.1.1.1) so a comment / template / stray `)`
//       in one part can no longer escape into the other (e.g.
//       `new Function("/*", "*/){")` used to assemble into a valid empty function).
//   (generators/syntax.js, the third file, still needs full yield-context tracking
//   for `yield` as a label / binding name in generator vs non-generator scopes — out
//   of scope.)
//
//   Problem 13 ("Failed to convert this to JSRegExp") — RegExp.prototype.test is
//   generic (§22.2.6.16): it only requires `this` to be an Object and then performs
//   RegExpExec, which calls the receiver's own (callable) `exec` property rather than
//   assuming a real RegExp. It was an instance method, so the generated wrapper cast
//   `this` to JSRegExp and threw for a plain object carrying an `exec` method. It is
//   now a static [JSPrototypeMethod] that runs RegExpExec.
//
//   Problem 19 / 20 (duplicate named capture groups + an iterated \k<name>
//   backreference) — ECMAScript clears a group's captures on each repetition of a
//   quantified group, so /(?:(?:(?<x>a)|(?<x>b)|c)\k<x>){2}/ matches "aac" with
//   groups.x === undefined. .NET's regex retains the previous capture across
//   repetitions, so the conditional backreference (BuildNamedBackref) fired on a
//   stale group and the match was lost ("Cannot get property groups of null"). The
//   capture-group rewrite now prepends a balancing-group reset
//   "(?(bjsgN)(?<-bjsgN>))" to each quantified group body, popping any stale capture
//   at the start of every iteration. Applied only to patterns with duplicate named
//   groups (the ES2025 feature that requires it), so other patterns are untouched.
//
//   Problem 15 / 16 (Reflect.setPrototypeOf must return false, not throw) — the
//   ordinary [[SetPrototypeOf]] (§10.1.2) returns false for the not-extensible and
//   cyclic cases; only Object.setPrototypeOf / the __proto__ setter turn that into a
//   TypeError. The runtime threw unconditionally. JSValue now exposes
//   TrySetPrototypeOf returning the boolean (SetPrototypeOf keeps throwing on top of
//   it), and Reflect.setPrototypeOf surfaces the boolean.
//
// Out of scope (unchanged): P1 generators/syntax.js yield-context tracking and P2
// eval ReferenceError (parser/architectural); P3-P12 class decorators + `accessor`
// auto-accessors (Stage-3); P14 super-call-in-arrow-eval this-init, P17 for-of head
// var-environment, P18 Annex-B eval block-scoped function hoisting (architectural).
public class Issue739Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // Evaluates `code` via indirect eval inside a JS try/catch and returns the JS error
    // constructor name (e.g. "SyntaxError"), or "NOTHROW". Compile-time syntax errors
    // are surfaced this way because a bare parse failure isn't catchable in-script.
    private static string SyntaxCheck(string code)
    {
        using var ctx = new JSContext();
        var src = System.Text.Json.JsonSerializer.Serialize(code);
        return ctx.Eval(
            "(function(){try{var e=eval;e(" + src + ");return 'NOTHROW';}" +
            "catch(err){return err && err.constructor ? err.constructor.name : String(err);}})()").ToString();
    }

    // ---- Problem 13: RegExp.prototype.test is generic over the receiver's exec ----

    [Fact]
    public void RegExpTestUsesReceiverExecOnPlainObject()
        => Assert.Equal("true", Eval(
            "var obj={exec(){return function(){};}};" +
            "''+RegExp.prototype.test.call(obj,'')"));

    [Fact]
    public void RegExpTestReturnsFalseWhenReceiverExecReturnsNull()
        => Assert.Equal("false", Eval(
            "var obj={exec(){return null;}};''+RegExp.prototype.test.call(obj,'x')"));

    [Fact]
    public void RegExpTestStillWorksOnRealRegExp()
        => Assert.Equal("true,false", Eval("[/ab/.test('zabz'),/ab/.test('zz')].join(',')"));

    [Fact]
    public void RegExpTestOnNonObjectThrowsTypeError()
        => Assert.Equal("TypeError", Eval(
            "try{RegExp.prototype.test.call(5,'');'no'}catch(e){e.constructor.name}"));

    // ---- Problem 19 / 20: duplicate named groups with an iterated backreference ----

    [Fact]
    public void DuplicateNamedGroupsExecGroupsObject()
        => Assert.Equal("b,a,c|x,y,z", Eval(
            "var m=/(?:(?<x>a)|(?<y>a)(?<x>b))(?:(?<z>c)|(?<z>d))/;var r=m.exec('abc');" +
            "[r.groups.x,r.groups.y,r.groups.z].join(',')+'|'+Object.keys(r.groups).join(',')"));

    [Fact]
    public void DuplicateNamedGroupsNonParticipatingIsUndefined()
        => Assert.Equal("a,undefined,d", Eval(
            "var m=/(?:(?<x>a)|(?<y>a)(?<x>b))(?:(?<z>c)|(?<z>d))/;var r=m.exec('ad');" +
            "[r.groups.x,String(r.groups.y),r.groups.z].join(',')"));

    [Fact]
    public void DuplicateNamedGroupIteratedBackrefMatchesWithResetCapture()
        => Assert.Equal("aac|undefined", Eval(
            "var it=/(?:(?:(?<x>a)|(?<x>b)|c)\\k<x>){2}/;var r=it.exec('aac');" +
            "r[0]+'|'+String(r.groups.x)"));

    [Fact]
    public void DuplicateNamedGroupIteratedBackrefKeepsLastIterationCapture()
        => Assert.Equal("b,a", Eval(
            "var it=/(?:(?:(?<x>a)|(?<x>b)|c)\\k<x>){2}/;" +
            "[it.exec('aabb').groups.x, it.exec('aaaa').groups.x].join(',')"));

    [Fact]
    public void DuplicateNamedGroupIndicesGroupsObject()
        => Assert.Equal("1,2|0,1|2,3", Eval(
            "var m=/(?:(?<x>a)|(?<y>a)(?<x>b))(?:(?<z>c)|(?<z>d))/d;var r=m.exec('abc');" +
            "[r.indices.groups.x.join(','),r.indices.groups.y.join(','),r.indices.groups.z.join(',')].join('|')"));

    // ---- Problem 15: Reflect.setPrototypeOf returns false on a cyclic change ----

    [Fact]
    public void ReflectSetPrototypeOfSameTargetReturnsFalse()
        => Assert.Equal("false,true", Eval(
            "var o={};" +
            "[Reflect.setPrototypeOf(o,o), Object.getPrototypeOf(o)===Object.prototype].join(',')"));

    [Fact]
    public void ReflectSetPrototypeOfCyclicReturnsFalse()
        => Assert.Equal("false", Eval(
            "var a={};var b=Object.create(a);''+Reflect.setPrototypeOf(a,b)"));

    // ---- Problem 16: Reflect.setPrototypeOf returns false on a non-extensible target ----

    [Fact]
    public void ReflectSetPrototypeOfNonExtensibleReturnsFalse()
        => Assert.Equal("false,false,false", Eval(
            "var o1={};Object.preventExtensions(o1);" +
            "var o2={};Object.preventExtensions(o2);" +
            "var o3=Object.create(null);Object.preventExtensions(o3);" +
            "[Reflect.setPrototypeOf(o1,{}),Reflect.setPrototypeOf(o2,null),Reflect.setPrototypeOf(o3,{})].join(',')"));

    [Fact]
    public void ReflectSetPrototypeOfNonExtensibleLeavesPrototypeUnchanged()
        => Assert.Equal("true", Eval(
            "var o=Object.create(null);Object.preventExtensions(o);" +
            "Reflect.setPrototypeOf(o,{});''+(Object.getPrototypeOf(o)===null)"));

    // Object.setPrototypeOf still throws for the same conditions.
    [Fact]
    public void ObjectSetPrototypeOfNonExtensibleStillThrows()
        => Assert.Equal("TypeError", Eval(
            "var o={};Object.preventExtensions(o);" +
            "try{Object.setPrototypeOf(o,{});'no'}catch(e){e.constructor.name}"));

    // Reflect.setPrototypeOf still succeeds (true) on a normal change.
    [Fact]
    public void ReflectSetPrototypeOfNormalReturnsTrue()
        => Assert.Equal("true,true", Eval(
            "var o={};var p={};" +
            "[Reflect.setPrototypeOf(o,p), Object.getPrototypeOf(o)===p].join(',')"));

    // ---- Problem 1 (methDefnGen.js): object-literal method-definition negatives ----

    [Theory]
    [InlineData("({*a : 1})")]      // a generator marker requires a method body
    [InlineData("({*a})")]
    [InlineData("({*a = 1})")]
    [InlineData("({a :* 1})")]      // a leading * is not an expression
    [InlineData("({get *a(){}})")]  // a getter cannot be a generator
    [InlineData("({set *a(c){}})")] // a setter cannot be a generator
    public void ObjectLiteralGeneratorAndStarNegativesAreSyntaxErrors(string code)
        => Assert.Equal("SyntaxError", SyntaxCheck(code));

    // ---- Problem 1: a leading `*` no longer starts an expression ----

    [Theory]
    [InlineData("x = * 1")]
    [InlineData("var y = * 1")]
    [InlineData("var y = (* 1)")]
    [InlineData("var y = 1 + * 1")]
    public void LeadingStarIsNotAnExpression(string code)
        => Assert.Equal("SyntaxError", SyntaxCheck(code));

    [Fact]
    public void MultiplicationAndGeneratorMethodsStillWork()
    {
        Assert.Equal("42", Eval("''+(6*7)"));
        // generator method with yield in computed keys still runs
        Assert.Equal("1,2", Eval(
            "var b={*g(){yield 1;yield 2;}};var it=b.g();[it.next().value,it.next().value].join(',')"));
        // a generator method is not constructable
        Assert.Equal("TypeError", Eval(
            "var a={*g(){yield 1;}};try{new a.g;'no'}catch(e){e.constructor.name}"));
        // yield* delegation still parses and runs
        Assert.Equal("1,2", Eval(
            "function* inner(){yield 1;yield 2;}function* outer(){yield* inner();}" +
            "var it=outer();[it.next().value,it.next().value].join(',')"));
    }

    // ---- Problem 1 (invalid-parameter-list.js): Function() validates parts apart ----

    [Theory]
    [InlineData("/*", "*/) {")]
    [InlineData("//", ") {")]
    [InlineData("a = `", "` ) {")]
    [InlineData(") { var x = function (", "} ")]
    [InlineData("x = function (", "}) {")]
    public void FunctionConstructorRejectsParameterListInjection(string p, string b)
        => Assert.Equal("SyntaxError", SyntaxCheck(
            $"new Function({System.Text.Json.JsonSerializer.Serialize(p)},{System.Text.Json.JsonSerializer.Serialize(b)})"));

    [Fact]
    public void FunctionConstructorStillBuildsValidFunctions()
    {
        Assert.Equal("5", Eval("''+new Function('a','b','return a+b')(2,3)"));
        Assert.Equal("7", Eval("''+new Function('a','// c\\nreturn a')(7)"));   // comment body needs the newline
        Assert.Equal("5", Eval("''+new Function('a=5','return a')()"));
        Assert.Equal("3", Eval("''+new Function('...a','return a.length')(1,2,3)"));
        Assert.Equal("9", Eval("''+new Function('{x}','return x')({x:9})"));
        Assert.Equal("function", Eval("typeof new Function('//only a comment')"));
    }
}
