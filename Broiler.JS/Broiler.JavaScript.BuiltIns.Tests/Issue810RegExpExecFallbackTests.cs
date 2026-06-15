using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// The RegExpExec abstract operation only invokes a "exec" property that is callable; any other value
// (undefined, null, or a non-callable primitive) falls back to the built-in RegExpBuiltinExec rather
// than throwing. Issue #810 problem 74.
public class Issue810RegExpExecFallbackTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Theory]
    [InlineData("undefined")]
    [InlineData("null")]
    [InlineData("5")]
    [InlineData("true")]
    [InlineData("Symbol()")]
    public void MatchAll_NonCallableExec_FallsBackToBuiltin(string execValue)
        => Assert.Equal("a,b", Eval($$"""
            var re = /\w/g;
            Object.defineProperty(re, "exec", { value: {{execValue}}, configurable: true });
            var out = [];
            for (var m of "a*b".matchAll(re)) out.push(m[0]);
            out.join(",");
        """));

    [Theory]
    [InlineData("null")]
    [InlineData("5")]
    public void Match_NonCallableExec_FallsBackToBuiltin(string execValue)
        => Assert.Equal("a", Eval($$"""
            var re = /\w/;
            Object.defineProperty(re, "exec", { value: {{execValue}}, configurable: true });
            var m = "a*b".match(re);
            m[0];
        """));
}
