using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/814 — Problem 11
// (test/staging/sm/class/derivedConstructorArrowEvalSuperCall.js and
//  derivedConstructorArrowEvalNestedSuperCall.js).
//
// A `super(...)` inside a direct eval in a derived constructor (directly or through one
// or more arrow functions) must run the superclass [[Construct]] and initialize the SAME
// `this` binding the constructor observes afterwards. The eval previously received `this`
// only by value, so it could not bind the constructor's binding — and reading that value
// to pass it in threw "Cannot access 'this' before initialization" before the eval (hence
// its super()) ever ran. The eval now shares the constructor's `this` binding and
// superclass constructor, reading `this` lazily through the binding.
public class Issue814DerivedConstructorEvalSuperTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    [Fact]
    public void EvalSuperCallInitializesThis()
        => Assert.Equal("6", Eval(
            "class A { constructor() { this.x = 6; } }" +
            "class B extends A { constructor() { eval('super()'); this.y = this.x; } }" +
            "'' + new B().y"));

    [Fact]
    public void ArrowEvalSuperCallInitializesThis()
        => Assert.Equal("7", Eval(
            "class A { constructor() { this.x = 7; } }" +
            "class B extends A { constructor() { (() => { eval('super()'); })(); this.y = this.x; } }" +
            "'' + new B().y"));

    [Fact]
    public void NestedArrowEvalSuperCallInitializesThis()
        => Assert.Equal("8", Eval(
            "class A { constructor() { this.x = 8; } }" +
            "class B extends A { constructor() { var g = () => () => { eval('super()'); }; g()(); this.y = this.x; } }" +
            "'' + new B().y"));

    [Fact]
    public void EvalSuperForwardsArguments()
        => Assert.Equal("99", Eval(
            "class A { constructor(v) { this.v = v; } }" +
            "class B extends A { constructor() { eval('super(99)'); } }" +
            "'' + new B().v"));

    [Fact]
    public void ThisIsReadableInsideEvalAfterSuper()
        // The `this` is read inside the eval, after its super() — the assignment target
        // must be a plain local: `this.r = eval(...)` would (correctly) read the `this`
        // base of `this.r` before the eval runs super(), which is itself a TDZ error.
        => Assert.Equal("5", Eval(
            "class A { constructor() { this.x = 5; } }" +
            "class B extends A { constructor() { var t = eval('super(); this.x'); this.r = t; } }" +
            "'' + new B().r"));

    [Fact]
    public void InstanceFieldsInitializeAfterEvalSuper()
        => Assert.Equal("5", Eval(
            "class A { constructor() {} }" +
            "class B extends A { y = 5; constructor() { eval('super()'); } }" +
            "'' + new B().y"));

    [Fact]
    public void SecondSuperViaEvalThrowsReferenceError()
        => Assert.Equal("ReferenceError", Eval(
            "class A { constructor() {} }" +
            "class B extends A { constructor() { super(); try { eval('super()'); this.r = 'no-error'; } " +
            "catch (e) { this.r = e.constructor.name; } } }" +
            "new B().r"));

    [Fact]
    public void ReadingThisBeforeEvalSuperThrowsReferenceError()
        => Assert.Equal("ReferenceError", Eval(
            "class A { constructor() {} }" +
            "class B extends A { constructor() { try { var q = this.x; this.r = 'no-error'; } " +
            "catch (e) { super(); this.r = e.constructor.name; } } }" +
            "new B().r"));

    [Fact]
    public void SuperPropertyViaEvalStillWorks()
        => Assert.Equal("42", Eval(
            "class A { constructor() {} m() { return 42; } }" +
            "class B extends A { constructor() { super(); this.z = eval('super.m()'); } }" +
            "'' + new B().z"));

    [Fact]
    public void NonDerivedConstructorEvalThisUnaffected()
        => Assert.Equal("3", Eval(
            "class A { constructor() { this.x = 1; this.z = eval('this.x + 2'); } }" +
            "'' + new A().z"));
}
