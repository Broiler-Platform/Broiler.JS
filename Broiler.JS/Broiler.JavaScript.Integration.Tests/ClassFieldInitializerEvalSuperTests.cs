using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// A class field initializer is a method-like function with its own [[HomeObject]], so
// `super.x` resolves inside it, and a direct eval within it inherits that home object —
// as does an arrow function closed over it, at any nesting depth.
//
// Issue814DerivedConstructorEvalSuperTests covers the neighbouring case, `super(...)` and
// `super.m()` inside a *derived constructor*. The field-initializer case is not the same
// binding: there is no super *constructor* call involved, only super property lookup
// through the field initializer's home object. These probes previously lived in a
// ReproTests scratch file that wrote its results to a hard-coded D:\ path and asserted
// nothing, so a regression in any of them was invisible.
public class ClassFieldInitializerEvalSuperTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    [Fact]
    public void DirectEvalInFieldInitializerCallsSuperMethod()
        => Assert.Equal("1", Eval(
            "class B { foo() { return 1; } }" +
            "class C extends B { x = eval('super.foo()'); }" +
            "'' + new C().x"));

    [Fact]
    public void ArrowInsideEvalInFieldInitializerCallsSuperMethod()
        => Assert.Equal("2", Eval(
            "class B { foo() { return 2; } }" +
            "class C extends B { x = eval('(() => super.foo())()'); }" +
            "'' + new C().x"));

    [Fact]
    public void ArrowInsideEvalInFieldInitializerReadsSuperMethodAsValue()
        // Reading `super.foo` without calling it yields the method itself, so the field
        // holds a function rather than its result.
        => Assert.Equal("function", Eval(
            "class B { foo() { return 3; } }" +
            "class C extends B { x = eval('(() => super.foo)()'); }" +
            "typeof new C().x"));

    [Fact]
    public void ArrowInFieldInitializerCallsSuperMethodWithoutEval()
        // The no-eval baseline: whatever the eval cases do, this must keep working, or
        // the home object is being lost before eval is even involved.
        => Assert.Equal("6", Eval(
            "class B { foo() { return 6; } }" +
            "class C extends B { x = (() => super.foo())(); }" +
            "'' + new C().x"));

    [Fact]
    public void FunctionDeclaredInsideEvalInFieldInitializerCallsSuperMethod()
        // The arrow is declared in one eval statement and invoked in the next, so the
        // home object has to survive on the closure rather than on the call site.
        => Assert.Equal("7", Eval(
            "class B { foo() { return 7; } }" +
            "class C extends B { x = eval('var f = () => super.foo(); f();'); }" +
            "'' + new C().x"));

    [Fact]
    public void ArrowInsideEvalInMethodCallsSuperMethod()
        // The same shape in an ordinary method rather than a field initializer.
        => Assert.Equal("5", Eval(
            "class B { foo() { return 5; } }" +
            "class C extends B { m() { return eval('(() => super.foo())()'); } }" +
            "'' + new C().m()"));
}
