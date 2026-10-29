using System.Threading.Tasks;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;
using Xunit;

namespace Broiler.JavaScript.Core.Tests;

/// <summary>
/// Regression guard for a generator-rewriter codegen bug: after a top-level-<c>await</c>
/// resume, statements that resolve an identifier or member through the script's
/// <c>ScriptInfo.Indices</c> key table used to dereference null (the box-load prologue
/// re-seeded the persisted <c>ScriptInfo</c> box with a null body-local on every resume).
/// A constant receiver or a bare global read resolves via constant <c>KeyStrings</c> and so
/// always survived, which made the fault look receiver-shaped. These pin the corrected
/// behaviour and guard that ordinary async functions / generators still work.
/// </summary>
public class TopLevelAwaitResumeTests
{
    private static async Task<JSValue> Tla(string code)
    {
        using var ctx = new JSContext();
        return await ctx.EvalWithTopLevelAwaitAsync(code);
    }

    [Fact]
    public async Task Local_Read_After_Await()
        => Assert.Equal(5.0, (await Tla("await Promise.resolve(); var x = 5; x;")).DoubleValue);

    [Fact]
    public async Task Member_Get_On_Identifier_Receiver_After_Await()
        => Assert.Equal(3.141592653589793, (await Tla("await Promise.resolve(); Math.PI;")).DoubleValue, 12);

    [Fact]
    public async Task Member_Call_On_Identifier_Receiver_After_Await()
        => Assert.Equal(2.0, (await Tla("await Promise.resolve(); Math.max(1, 2);")).DoubleValue);

    [Fact]
    public async Task Member_Assign_After_Await()
        => Assert.Equal(2.0, (await Tla("await Promise.resolve(); globalThis.__a = 2; globalThis.__a;")).DoubleValue);

    [Fact]
    public async Task Member_On_Awaited_Value()
        => Assert.Equal(7.0, (await Tla("var o = await Promise.resolve({ m: 7 }); o.m;")).DoubleValue);

    [Fact]
    public async Task Sequential_Awaits_Keep_Bindings()
        => Assert.Equal(3.0, (await Tla("var a = await Promise.resolve(1); var b = await Promise.resolve(2); a + b;")).DoubleValue);

    [Fact]
    public async Task Constant_Receiver_Member_After_Await_Still_Works()
        => Assert.Equal(2.0, (await Tla("await Promise.resolve(); 'hi'.length;")).DoubleValue);

    [Fact]
    public async Task Nested_Async_Function_Member_After_Await_Unregressed()
    {
        using var ctx = new JSContext();
        var r = await ctx.ExecuteAsync(
            "async function f(){ await Promise.resolve(); var q = Math.max(5, 6); return q + globalThis.__z; }\n" +
            "globalThis.__z = 100;\n" +
            "f();");
        Assert.Equal(106.0, r.DoubleValue);
    }

    [Fact]
    public void Generator_Member_After_Yield_Unregressed()
    {
        using var ctx = new JSContext();
        var r = ctx.Eval(
            "function* g(){ yield 1; var q = Math.max(3, 4); return q + 10; }\n" +
            "var it = g(); it.next(); it.next().value;");
        Assert.Equal(14.0, r.DoubleValue);
    }
}
