using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/640
// Covers the cleanly-reproducible subset:
//   Problem 6      - global object inherits Object.prototype (propertyIsEnumerable
//                    and other inherited methods callable on top-level `this`).
//   Problem 8      - Object(value) / new Object(value) returns ToObject(value).
//   Problem 10     - new.target is a SyntaxError in a direct eval that is not
//                    inside ordinary function code (global / top-level arrow).
public class Issue640Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problem 6: global object's [[Prototype]] is Object.prototype ----

    [Fact]
    public void GlobalObjectInheritsObjectPrototype()
        => Assert.Equal("true", Eval("'' + (Object.getPrototypeOf(this) === Object.prototype)"));

    // Inherited Object.prototype methods are callable on the top-level `this`.
    [Fact]
    public void GlobalThisHasInheritedMethods()
        => Assert.Equal("function function", Eval("typeof this.hasOwnProperty + ' ' + typeof this.propertyIsEnumerable"));

    // The inherited propertyIsEnumerable is functional on the global object.
    [Fact]
    public void GlobalThisPropertyIsEnumerableIsFunctional()
        => Assert.Equal("true false", Eval("this.hasOwnProperty('Object') + ' ' + this.propertyIsEnumerable('thisPropertyDoesNotExist')"));

    // ---- Problem 8: Object constructor follows ToObject(value) ----

    // new Object(fn) returns the function itself (an object is returned unchanged).
    [Fact]
    public void NewObjectReturnsFunctionArgument()
        => Assert.Equal("true 1", Eval("var f=function(){return 1;}; (new Object(f)===f) + ' ' + new Object(f)()"));

    // new Object(obj) returns the same object.
    [Fact]
    public void NewObjectReturnsObjectArgument()
        => Assert.Equal("true", Eval("var o={a:1}; '' + (new Object(o)===o)"));

    // new Object() with no argument still yields a fresh object with Object.prototype.
    [Fact]
    public void NewObjectNoArgumentCreatesOrdinaryObject()
        => Assert.Equal("true", Eval("var n=new Object(); '' + (Object.getPrototypeOf(n)===Object.prototype)"));

    // ---- Object [[Construct]] with a SUBCLASS new.target ignores the value argument ----
    // (ES §20.1.1.1 step 1 / OrdinaryCreateFromConstructor). test262
    // built-ins/Object/subclass-object-arg.js.

    // `new (class extends Object {})(value)` creates a fresh object with the subclass prototype and
    // does NOT adopt the argument's properties.
    [Fact]
    public void NewSubclassOfObjectIgnoresArgument()
        => Assert.Equal("undefined true", Eval(
            "class O extends Object {} var o=new O({a:1}); typeof o.a + ' ' + (Object.getPrototypeOf(o)===O.prototype)"));

    // Reflect.construct(Object, [value], Subclass) behaves the same.
    [Fact]
    public void ReflectConstructObjectWithSubclassNewTargetIgnoresArgument()
        => Assert.Equal("undefined true", Eval(
            "class O extends Object {} var o=Reflect.construct(Object,[{b:2}],O); typeof o.b + ' ' + (Object.getPrototypeOf(o)===O.prototype)"));

    // The argument object's own prototype must NOT be mutated by the subclass construction.
    [Fact]
    public void SubclassConstructionDoesNotMutateArgumentPrototype()
        => Assert.Equal("true", Eval(
            "class O extends Object {} var inp={q:9}; new O(inp); '' + (Object.getPrototypeOf(inp)===Object.prototype)"));

    // ---- Problem 10: new.target in eval outside function code is a SyntaxError ----

    [Fact]
    public void NewTargetInGlobalDirectEvalIsSyntaxError()
        => Assert.Equal("SyntaxError", Eval("var c='no throw'; try { eval('new.target;'); } catch (e) { c = e.constructor.name; } c"));

    // A top-level arrow is not ordinary function code: new.target is still rejected.
    [Fact]
    public void NewTargetInTopLevelArrowDirectEvalIsSyntaxError()
        => Assert.Equal("SyntaxError", Eval("var c='no throw'; var f=()=>eval('new.target;'); try { f(); } catch (e) { c = e.constructor.name; } c"));

    // Inside ordinary function code new.target in a direct eval is permitted (no SyntaxError).
    [Fact]
    public void NewTargetInFunctionDirectEvalIsAllowed()
        => Assert.Equal("no syntax error", Eval("function F(){ try { eval('new.target;'); return 'no syntax error'; } catch (e) { return e.constructor.name; } } F()"));
}
