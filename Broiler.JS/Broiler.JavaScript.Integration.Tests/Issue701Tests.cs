using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/701
//
// Fixed here:
//
// Problem 2 (subset) — installing an instance private field did not enforce the
//   PrivateFieldAdd invariants (ECMA-262 § 7.3.28). The field was stored with an
//   unconditional FastAddValue, so neither of the two TypeError cases fired:
//   re-adding a private name an object already carries (step 4, reachable when a
//   derived constructor's `return`-override hands the same object to a second
//   field initialization), and adding a field to a non-extensible object (the
//   `nonextensible-applies-to-private` refinement). Instance private fields now
//   route through JSObject.PrivateFieldAdd, which performs both checks before
//   storing the slot. Ordinary field initialization on a fresh, extensible object
//   is unchanged.
//
// Problem 5 (subset) — a class element written as a `key: value` colon pair
//   (object-literal syntax) was accepted as a data property instead of being
//   rejected. A colon is never a valid ClassElement, so `class X { x: 1 }` is now
//   a SyntaxError. Object-literal and destructuring colons are unaffected.
//
// Problem 2 (private methods/accessors) — instance private methods and accessors
//   were installed once on the prototype, which broke the per-instance brand: a
//   `return`-override object (whose prototype is not the class prototype) could
//   not call them, and re-running a constructor over the same object did not throw.
//   They are now created once at class evaluation and installed PER INSTANCE (via
//   PrivateMethodAdd / PrivateAccessorAdd, before the field initializers), matching
//   InitializeInstanceElements: a second installation or a non-extensible target
//   throws, and the override object carries the element. (Fixes the
//   private-method-double-initialisation* and the method/accessor portions of the
//   return-override non-extensible tests; covered by a full base-vs-fix run of the
//   class/elements + class/subclass test262 trees with zero regressions.)
//
// Problem 2 (static fields) — static data field initializers ran while the
//   constructor object was being built, before its name binding was set, so an
//   initializer that referenced the class name saw `undefined`. They now run after
//   the class binding (ClassDefinitionEvaluation evaluates static field
//   initializers last, after the methods), and a static private field uses
//   PrivateFieldAdd — so a self-sealed constructor throws. (Fixes
//   private-class-field-on-nonextensible-objects.js.)
//
// Problem 2 (private members on a Proxy) — a private member access (`this.#x`)
//   whose receiver is a Proxy forwarded to the target (or the get/set trap)
//   instead of operating on the proxy's own private elements. A private member is
//   not a property lookup and never consults the target or a trap, so a proxy that
//   does not itself carry the name fails the brand check (TypeError). JSProxy now
//   routes private-name reads/writes to the base object. (Fixes the
//   *-proxy-default-handler-throws and static-private-methods-proxy tests.)
public class Issue701Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code);
    }

    // Run `source`, reporting the thrown error's constructor name or "ok".
    private static string Catch(string source)
        => Eval("var r; try { " + source + " r = 'ok'; } catch (e) { r = e.constructor.name; } r;").ToString();

    // ---- PrivateFieldAdd: re-adding a private name is a TypeError ----

    // A base constructor `return`-override hands the same object to two separate
    // field initializations; the second PrivateFieldAdd of `#x` throws.
    [Fact]
    public void DuplicatePrivateFieldThrows()
        => Assert.Equal("TypeError", Catch(
            "class A { constructor(a) { return a; } }" +
            "class C extends A { #x; constructor(a) { super(a); } }" +
            "var o = new C(); new C(o);"));

    // ---- PrivateFieldAdd: a non-extensible target is a TypeError ----

    // A derived instance whose base constructor sealed `this` cannot receive the
    // subclass private field.
    [Fact]
    public void PrivateFieldOnNonExtensibleDerivedInstanceThrows()
        => Assert.Equal("TypeError", Catch(
            "class B { constructor(seal) { if (seal) Object.preventExtensions(this); } }" +
            "class C extends B { #v; constructor(seal) { super(seal); this.#v = 42; } }" +
            "new C(true);"));

    // A base-class field initializer that makes `this` non-extensible before the
    // field is stored throws when the field is added.
    [Fact]
    public void PrivateFieldOnSelfSealedBaseThrows()
        => Assert.Equal("TypeError", Catch(
            "class T { #g = (Object.preventExtensions(this), 'Test262'); } new T();"));

    // ---- Ordinary private-field initialization is unaffected ----

    // A field on a fresh, extensible instance is readable.
    [Fact]
    public void PrivateFieldOnExtensibleInstanceReads()
        => Assert.Equal("5", Eval(
            "class C { #x = 5; get() { return this.#x; } } new C().get();").ToString());

    // An extensible derived instance still constructs and reads its field.
    [Fact]
    public void PrivateFieldOnExtensibleDerivedInstanceReads()
        => Assert.Equal("42", Eval(
            "class B { constructor(seal) { if (seal) Object.preventExtensions(this); } }" +
            "class C extends B { #v; constructor(seal) { super(seal); this.#v = 42; } v() { return this.#v; } }" +
            "new C(false).v();").ToString());

    // Two independent instances each get their own private field (the duplicate
    // check is per object, not per private name).
    [Fact]
    public void DistinctInstancesEachGetTheirOwnField()
        => Assert.Equal("1,1", Eval(
            "class C { #x = 1; v() { return this.#x; } } new C().v() + ',' + new C().v();").ToString());

    // ---- A `key: value` colon is never a valid ClassElement ----

    // The plain, static, and computed-key colon forms are all SyntaxErrors.
    [Theory]
    [InlineData("eval('class X { x: 1 }');")]
    [InlineData("eval('class X { static x: 1 }');")]
    [InlineData("eval('class X { [\\'y\\']: 1 }');")]
    public void ClassElementColonIsSyntaxError(string source)
        => Assert.Equal("SyntaxError", Catch(source));

    // Valid ClassElement forms still parse and run: field, method, accessor pair,
    // static block, and a computed-key method.
    [Fact]
    public void ValidClassElementsStillParse()
        => Assert.Equal("5,7,9,3,4", Eval(
            "class CF { x = 5; }" +
            "class CM { m() { return 7; } }" +
            "class CA { #v = 1; get x() { return this.#v; } set x(n) { this.#v = n; } }" +
            "class CS { static y; static { CS.y = 3; } }" +
            "class CC { ['m']() { return 4; } }" +
            "var a = new CA(); a.x = 9;" +
            "[new CF().x, new CM().m(), a.x, CS.y, new CC().m()].join(',');").ToString());

    // The object-literal and destructuring colons are untouched.
    [Fact]
    public void ObjectLiteralColonStillWorks()
        => Assert.Equal("1", Eval("({ a: 1 }).a;").ToString());

    [Fact]
    public void DestructuringDefaultStillWorks()
        => Assert.Equal("5", Eval("var { a = 5 } = {}; a;").ToString());

    // ---- Instance private methods/accessors install per instance ----

    // Re-running a constructor over the same `return`-override object installs the
    // private method a second time and throws (PrivateMethodOrAccessorAdd).
    [Fact]
    public void PrivateMethodDoubleInstallThrows()
        => Assert.Equal("TypeError", Catch(
            "class B { constructor(o) { return o; } }" +
            "class C extends B { #m() {} }" +
            "var o = {}; new C(o); new C(o);"));

    // The same for a private accessor sharing a getter and setter.
    [Fact]
    public void PrivateAccessorDoubleInstallThrows()
        => Assert.Equal("TypeError", Catch(
            "class B { constructor(o) { return o; } }" +
            "class C extends B { get #x() {} set #x(v) {} }" +
            "var o = {}; new C(o); new C(o);"));

    // Installing a private method on a non-extensible instance throws.
    [Fact]
    public void PrivateMethodOnNonExtensibleThrows()
        => Assert.Equal("TypeError", Catch(
            "class B { constructor(seal) { if (seal) Object.preventExtensions(this); } }" +
            "class C extends B { constructor(seal) { super(seal); } #m() { return 1; } }" +
            "new C(true);"));

    // A `return`-override object carries the private method and can call it — the
    // method is on the instance, not (only) the class prototype.
    [Fact]
    public void ReturnOverrideObjectCanCallPrivateMethod()
        => Assert.Equal("7", Eval(
            "class B { constructor(o) { return o; } }" +
            "class C extends B { #m() { return 7; } static call(o) { return o.#m(); } }" +
            "var o = {}; var inst = new C(o); C.call(inst);").ToString());

    // A private method is callable on an ordinary instance (the common case).
    [Fact]
    public void PrivateMethodOnOrdinaryInstanceWorks()
        => Assert.Equal("3", Eval(
            "class C { #m() { return 3; } call() { return this.#m(); } } new C().call();").ToString());

    // A private accessor (getter + setter) round-trips on an ordinary instance.
    [Fact]
    public void PrivateAccessorRoundTrips()
        => Assert.Equal("9", Eval(
            "class C { #v = 1; get #x() { return this.#v; } set #x(n) { this.#v = n; }" +
            "          run() { this.#x = 9; return this.#x; } } new C().run();").ToString());

    // Private methods install before field initializers, so a field initializer
    // may call one.
    [Fact]
    public void FieldInitializerCanCallPrivateMethod()
        => Assert.Equal("5", Eval(
            "class C { #m() { return 5; } x = this.#m(); } new C().x;").ToString());

    // A private method is not an enumerable own property.
    [Fact]
    public void PrivateMethodIsNotEnumerable()
        => Assert.Equal("0", Eval(
            "class C { #m() {} f = 1; }" +
            "var n = 0; for (var k in new C()) if (k !== 'f') n++; n;").ToString());

    // A class with private methods but no fields and no explicit constructor still
    // installs them (the synthetic constructor runs InitMembers).
    [Fact]
    public void SyntheticConstructorInstallsPrivateMethod()
        => Assert.Equal("4", Eval(
            "class C { #m() { return 4; } call() { return this.#m(); } } new C().call();").ToString());

    // ---- Static data field initializers run after the class binding is set ----

    // A static private field added to a constructor that an earlier initializer
    // sealed is a TypeError.
    [Fact]
    public void StaticPrivateFieldOnSelfSealedConstructorThrows()
        => Assert.Equal("TypeError", Catch(
            "class T { static #g = (Object.preventExtensions(T), 'x'); }"));

    // The class name resolves to the constructor inside a static field initializer
    // (it was previously undefined).
    [Fact]
    public void ClassNameVisibleInStaticFieldInitializer()
        => Assert.Equal("function", Eval(
            "class C { static #t = (typeof C); static get() { return C.#t; } } C.get();").ToString());

    [Fact]
    public void ClassNameVisibleInPublicStaticFieldInitializer()
        => Assert.Equal("function", Eval("class C { static t = typeof C; } C.t;").ToString());

    // Static methods install before static field initializers, so a static field
    // initializer may call one.
    [Fact]
    public void StaticFieldInitializerCanCallStaticMethod()
        => Assert.Equal("3", Eval("class C { static m() { return 3; } static x = C.m(); } C.x;").ToString());

    // Ordinary static private and public fields still read back.
    [Fact]
    public void StaticPrivateFieldStillReads()
        => Assert.Equal("5", Eval("class C { static #x = 5; static get() { return C.#x; } } C.get();").ToString());

    [Fact]
    public void StaticPublicFieldStillReads()
        => Assert.Equal("7", Eval("class C { static y = 7; } C.y;").ToString());

    // ---- Private member access does not forward through a Proxy ----

    // Calling a method through a Proxy runs it with the proxy as `this`; the
    // proxy does not carry the target's private field, so `this.#x` is a TypeError.
    [Fact]
    public void PrivateFieldDoesNotLeakThroughProxy()
        => Assert.Equal("TypeError", Catch(
            "class C { #x = 1; x() { return this.#x; } }" +
            "var c = new C(); var p = new Proxy(c, {}); p.x();"));

    // The original object still reads its own private field.
    [Fact]
    public void PrivateFieldReadsOnRealReceiver()
        => Assert.Equal("1", Eval(
            "class C { #x = 1; x() { return this.#x; } } new C().x();").ToString());

    // A proxy handed a private field by a constructor return-override holds it as
    // its own element and can read it.
    [Fact]
    public void ProxyWithOwnPrivateFieldReads()
        => Assert.Equal("7", Eval(
            "class B { constructor(o) { return o; } }" +
            "class C extends B { #x = 7; static get(o) { return o.#x; } }" +
            "var p = new Proxy({}, {}); var inst = new C(p); C.get(p);").ToString());

    // Ordinary (non-private) property access through a Proxy is unaffected.
    [Fact]
    public void ProxyOrdinaryGetTrapStillWorks()
        => Assert.Equal("42", Eval(
            "var p = new Proxy({ a: 1 }, { get(t, k) { return k === 'a' ? 42 : t[k]; } }); p.a;").ToString());
}
