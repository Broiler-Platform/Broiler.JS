using System.Threading.Tasks;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

/// <summary>
/// Locks in that two live <see cref="JSContext"/> instances are isolated — each evaluates against its own
/// global object regardless of creation order, concurrent access, or a stored callback being invoked while
/// another context is the "current" one. This is the engine-level guarantee a multi-session host (e.g. the
/// HtmlBridge, whose Phase-2 exit criterion is "two simultaneous sessions cannot see each other's state")
/// relies on. Isolation comes from the <c>[ThreadStatic]</c> + <c>AsyncLocal</c> current-context flow plus
/// the realm scope entered by <c>Eval</c> and <c>InvokeFunction</c>.
/// </summary>
public class SessionIsolationTests
{
    [Fact]
    public void Interleaved_Evals_On_Two_Contexts_Do_Not_Share_Globals()
    {
        using var a = new JSContext();
        using var b = new JSContext();

        a.Eval("globalThis.x = 'A';");
        b.Eval("globalThis.x = 'B';");
        a.Eval("globalThis.y = 'A2';");

        Assert.Equal("A", a.Eval("''+globalThis.x").ToString());
        Assert.Equal("B", b.Eval("''+globalThis.x").ToString());
        Assert.Equal("A2", a.Eval("''+globalThis.y").ToString());
        Assert.Equal("undefined", b.Eval("typeof globalThis.y").ToString()); // not leaked from a
    }

    [Fact]
    public void A_Stored_Callback_Resolves_Its_Own_Context_After_Another_Is_Created()
    {
        using var a = new JSContext();
        a.Eval("globalThis.v = 'A'; globalThis.getV = function () { return globalThis.v; };");
        var getV = a.Eval("globalThis.getV");

        // b is created after a, so the constructor's last-wins `CurrentContext = this` points at b.
        using var b = new JSContext();
        b.Eval("globalThis.v = 'B';");

        // Invoking a's stored function directly (no a.Eval re-entry) must still resolve a's globalThis.
        Assert.Equal("A", getV.InvokeFunction(new Arguments(JSUndefined.Value)).ToString());
    }

    [Fact]
    public async Task Concurrent_Evals_On_Two_Contexts_Never_Clobber_Each_Other()
    {
        using var a = new JSContext();
        using var b = new JSContext();
        a.Eval("globalThis.name = 'A';");
        b.Eval("globalThis.name = 'B';");

        int aWrong = 0, bWrong = 0;
        var ta = Task.Run(() =>
        {
            for (int i = 0; i < 2000; i++)
                if (a.Eval("''+globalThis.name").ToString() != "A") aWrong++;
        });
        var tb = Task.Run(() =>
        {
            for (int i = 0; i < 2000; i++)
                if (b.Eval("''+globalThis.name").ToString() != "B") bWrong++;
        });
        await Task.WhenAll(ta, tb);

        Assert.Equal(0, aWrong);
        Assert.Equal(0, bWrong);
    }
}
