using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;
using Xunit;

namespace Broiler.JavaScript.Integration.Tests;

// #919 P1 — a top-level FunctionDeclaration in a function-local direct eval is
// instantiated once at eval entry; its textual statement is a runtime no-op and must
// not re-read the (deletable) binding. Previously `delete f` before the textual site
// made the textual statement throw "f is not defined".
// (test262 language/eval-code/direct/var-env-func-init-local-new-delete.js)
public class Issue919Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code);
    }

    [Fact]
    public void NewlyCreatedLocalEvalFunctionBindingMayBeDeleted()
    {
        var result = Eval(@"
var initial, postDeletion;
(function() {
  eval('initial = f; delete f; postDeletion = function() { f; }; function f() { return 33; }');
}());
var threw = false;
try { postDeletion(); } catch (e) { threw = e instanceof ReferenceError; }
[typeof initial, initial(), threw].join(',');");
        Assert.Equal("function,33,true", result.ToString());
    }

    [Fact]
    public void TopLevelEvalFunctionIsCallableBeforeAndAfterTextualSite()
    {
        // Hoisted at eval entry: readable before the textual declaration, and a stable
        // single object (identity) — the textual statement does not re-instantiate it.
        Assert.Equal("33", Eval(@"(function(){ eval('function f(){return 33}'); return f(); })()").ToString());
        Assert.Equal("function", Eval(@"(function(){ return eval('function f(){return 1}; typeof f;'); })()").ToString());
        Assert.Equal("function", Eval(@"(function(){ return eval('var a=typeof f; function f(){return 1}; a;'); })()").ToString());
        Assert.True(Eval(@"(function(){ return eval('var a=f; function f(){}; var b=f; a===b'); })()").BooleanValue);
    }

    [Fact]
    public void EvalFunctionDeclarationCollidingWithParamOrVarUpdatesBinding()
    {
        Assert.Equal("99", Eval(@"(function(p){ eval('function p(){return 99}'); return typeof p==='function' ? p() : p; })(5)").ToString());
        Assert.Equal("8", Eval(@"(function(){ var v=1; eval('function v(){return 8}'); return typeof v==='function'?v():v; })()").ToString());
    }

    [Fact]
    public void GlobalAndStrictEvalFunctionDeclarationsAreUnaffected()
    {
        // Global/indirect eval funcdecls still become global bindings; strict eval stays local.
        Assert.Equal("9", Eval(@"eval('function gg2(){return 9}'); gg2()").ToString());
        Assert.Equal("11", Eval(@"(0,eval)('function gi(){return 11}'); gi()").ToString());
        Assert.Equal("undefined", Eval(@"(function(){ eval('""use strict""; function sf(){return 1}'); return typeof sf; })()").ToString());
    }
}
