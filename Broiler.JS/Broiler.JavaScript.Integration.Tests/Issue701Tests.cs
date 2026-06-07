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
}
