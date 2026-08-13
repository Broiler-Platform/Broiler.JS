using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Compiler.Tests;

// A direct eval's scope is LEXICAL, so a closure the eval creates keeps the eval site's bindings
// after that call has returned.
//
// The caller's bindings reach a direct eval as an overlay installed for the duration of the call
// and withdrawn when it returns. Reading one *inside* the eval therefore worked, and a function the
// eval created stopped resolving it the moment the eval returned - which is exactly when such a
// function is called. `eval("(function(){ return b; })")` threw "b is not defined".
//
// Google's module loader is built on that shape: `function(e){return eval(e)}(src)` where src is
// `0,function(){b(2,57,1,w)}`, the resulting function stored and invoked later by the bundle. It is
// what made this a "b is not defined" on google.com.
public class DirectEvalClosureScopeTests
{
    private static string Eval(string expression)
    {
        using var context = new JSContext();
        return context.Eval($"String({expression})", "t.js").ToString();
    }

    // The reported failure, reduced.
    [Fact]
    public void AFunctionCreatedByEval_SeesTheCallersBinding_WhenCalledLater()
    {
        Assert.Equal("42", Eval("(function(){ var b = 42; var f = eval('(function(){ return b; })'); return f(); })()"));
    }

    // google.com's shape: a comma-expression fragment evaluated by an IIFE whose whole body is the
    // eval, the result stored and called afterwards.
    [Fact]
    public void TheReportedShape_Resolves()
    {
        Assert.Equal(
            "7",
            Eval("(function(){ var b = function(a, c){ return a + c; }; var w = 5;" +
                 " var f = (function(e){ return eval(e); })('0,function(){return b(2,w);}');" +
                 " return f(); })()"));
    }

    // The binding may be several scopes above the eval, as it is in the real code.
    [Fact]
    public void ABindingSeveralScopesAbove_Resolves()
    {
        Assert.Equal(
            "43",
            Eval("(function(){ var b = 43; return (function(){ return (function(){" +
                 " return (function(e){ return eval(e); })('0,function(){return b;}'); })(); })()(); })()"));
    }

    // Stored by the eval, invoked from somewhere else entirely - the loader's pattern.
    [Fact]
    public void AStoredFunction_ResolvesWhenInvokedFromElsewhere()
    {
        Assert.Equal(
            "9",
            Eval("(function(){ var b = 9; var s = {};" +
                 " (function(e){ s.f = eval(e); })('0,function(){return b;}');" +
                 " return (function(){ return s.f(); })(); })()"));
    }

    // It is the caller's binding, not a copy of its value: a later write is visible...
    [Fact]
    public void TheBindingIsShared_NotSnapshotted()
    {
        Assert.Equal("5", Eval("(function(){ var b = 1; var f = eval('0,function(){return b;}'); b = 5; return f(); })()"));
    }

    // ...and a write from inside the closure lands on it rather than on a new global.
    [Fact]
    public void AWriteFromTheClosure_ReachesTheCallersBinding()
    {
        Assert.Equal("7", Eval("(function(){ var b = 1; var f = eval('0,function(){ b = 7; }'); f(); return b; })()"));
    }

    // Parameters and lexical declarations are bindings too.
    [Theory]
    [InlineData("(function(b){ var f = eval('0,function(){return b;}'); return f(); })(11)", "11")]
    [InlineData("(function(){ let b = 12; var f = eval('0,function(){return b;}'); return f(); })()", "12")]
    public void ParametersAndLexicalBindings_Resolve(string source, string expected)
    {
        Assert.Equal(expected, Eval(source));
    }

    // A function nested inside the evalled function reaches it too.
    [Fact]
    public void ANestedFunction_Resolves()
    {
        Assert.Equal("3", Eval("(function(){ var b = 3; var f = eval('0,function(){ return (function(){ return b; })(); }'); return f(); })()"));
    }

    // What must NOT change. The capture is consulted only after every ordinary scope has failed, so
    // nothing that resolves today resolves differently: the eval's own declaration still shadows the
    // caller's binding, a global of the same name still wins, and a genuinely free name still throws.
    [Theory]
    [InlineData("(function(){ var b = 1; var f = eval('var b = 2; 0,function(){return b;}'); return f(); })()", "2")]
    [InlineData("(function(){ globalThis.gg1 = 'global'; var f = eval('0,function(){return gg1;}'); return f(); })()", "global")]
    public void ExistingResolutionIsUnchanged(string source, string expected)
    {
        Assert.Equal(expected, Eval(source));
    }

    [Fact]
    public void AGenuinelyUndeclaredName_StillThrows()
    {
        Assert.Equal(
            "ReferenceError",
            Eval("(function(){ var b = 1; try { var f = eval('0,function(){return zzNope;}'); return f(); }" +
                 " catch (x) { return x.constructor.name; } })()"));
    }
}
