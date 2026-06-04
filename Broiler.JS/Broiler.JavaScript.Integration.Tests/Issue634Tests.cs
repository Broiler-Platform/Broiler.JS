using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/634
// Problem 1 (Annex B.3.3 var-binding initialization) and Problem 4 (Annex B.3.4
// implicit-block scoping) for `if`-clause FunctionDeclarations.
public class Issue634Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // Problem 1: per B.3.3.2 a sloppy-mode `if`-clause FunctionDeclaration creates
    // a var-scoped binding in the enclosing global var environment, initialized to
    // `undefined` at GlobalDeclarationInstantiation time. Reading the name before
    // the declaration executes must yield `undefined`, not throw "f is not defined".
    [Theory]
    [InlineData("var before = f; if (true) function f() {} '' + before;")]
    [InlineData("var before = f; if (true) function f() {} else function _f(){} '' + before;")]
    [InlineData("var before = f; if (false) function _f() {} else function f(){} '' + before;")]
    [InlineData("var before = f; if (true) function f() {} else ; '' + before;")]
    [InlineData("var before = f; if (false) ; else function f() {} '' + before;")]
    public void GlobalIfClauseFunctionBindingInitializedToUndefined(string code)
        => Assert.Equal("undefined", Eval(code));

    // Problem 1 (function scope, B.3.3.1): the var-scoped binding is created in the
    // enclosing function's var environment, initialized to `undefined`, and is
    // mutable before the declaration executes.
    [Fact]
    public void FunctionIfClauseFunctionBindingInitializedAndMutable()
    {
        var code = "(function(){ var init = f; f = 123; var changed = f;"
            + " if (true) function f() {} return init + ',' + changed; })();";
        Assert.Equal("undefined,123", Eval(code));
    }

    // Problem 1: the hoisted global binding has CreateGlobalFunctionBinding
    // semantics — enumerable, writable, non-configurable.
    [Fact]
    public void GlobalIfClauseFunctionBindingIsNonConfigurable()
    {
        var code = "if (true) function f() {} var d = Object.getOwnPropertyDescriptor(this, 'f');"
            + " '' + d.enumerable + ',' + d.writable + ',' + d.configurable;";
        Assert.Equal("true,true,false", Eval(code));
    }

    // Problem 4: per B.3.4 the `if`-clause function lives in its own implicit block.
    // The body's own-name references resolve to that block-scoped binding, so a
    // self-reassignment does not clobber the var-scoped (global) binding that the
    // function value was copied out to.
    [Theory]
    // if-decl-no-else
    [InlineData("var initialBV, currentBV;"
        + "if (true) function f() { initialBV = f; f = 123; currentBV = f; return 'decl'; }"
        + "f(); '' + initialBV() + ',' + currentBV + ',' + f();")]
    // if-stmt-else-decl (function in the else branch)
    [InlineData("var initialBV, currentBV;"
        + "if (false) ; else function f() { initialBV = f; f = 123; currentBV = f; return 'decl'; }"
        + "f(); '' + initialBV() + ',' + currentBV + ',' + f();")]
    public void GlobalIfClauseFunctionIsBlockScoped(string code)
        => Assert.Equal("decl,123,decl", Eval(code));

    // Problem 4 (function scope): same block-scoping at function var-environment level.
    [Fact]
    public void FunctionIfClauseFunctionIsBlockScoped()
    {
        var code = "(function(){ var initialBV, currentBV;"
            + "if (true) function f() { initialBV = f; f = 123; currentBV = f; return 'decl'; }"
            + "f(); return '' + initialBV() + ',' + currentBV + ',' + f(); })();";
        Assert.Equal("decl,123,decl", Eval(code));
    }

    // A nested block containing an `if`-clause function still hoists the Annex B
    // var binding to the enclosing body (read before the inner block -> undefined).
    [Fact]
    public void NestedBlockIfClauseFunctionStillHoists()
        => Assert.Equal("undefined,function", Eval("var b; { b = f; if (true) function f(){} } typeof b + ',' + typeof f;"));

    // A blocking lexical sibling at the same (non-eval) block level prevents the
    // Annex B var-hoist, so nothing leaks to the global scope.
    [Fact]
    public void IfClauseFunctionBlockedByLexicalSiblingDoesNotLeak()
        => Assert.Equal("undefined", Eval("{ let f; if (true) function f(){} } typeof f;"));
}
