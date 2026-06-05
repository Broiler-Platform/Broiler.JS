using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

/// <summary>
/// A `return` inside a control-flow statement (if/while/for/switch) that is
/// nested in a finally block previously produced invalid IL: such statements
/// lower to a Block that ends with a completion variable, so the finally body's
/// non-void value was left on the evaluation stack at endfinally. These run in
/// ordinary (non script-host) mode to lock the general fix, independent of
/// proper-tail-call handling.
/// </summary>
public class TryFinallyReturnTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    [Fact]
    public void Return_In_If_In_Finally()
        => Assert.Equal("c", Eval("function f(){ try {} finally { if (true) return 'c'; } } f();"));

    [Fact]
    public void Return_In_While_In_Finally()
        => Assert.Equal("w", Eval("function f(){ try {} finally { while (true) return 'w'; } } f();"));

    [Fact]
    public void Return_In_If_In_Finally_Overrides_Try()
        => Assert.Equal("F,T", Eval("function f(x){ try { return 'T'; } finally { if (x) return 'F'; } } '' + f(true) + ',' + f(false);"));

    [Fact]
    public void Finally_Without_Return_Falls_Through_To_Try_Value()
        => Assert.Equal("T", Eval("function f(){ try { return 'T'; } finally { if (false) return 'X'; } } f();"));

    [Fact]
    public void Return_In_If_In_Finally_With_Catch()
        => Assert.Equal("F", Eval("function f(){ try { throw 1; } catch(e){} finally { if (true) return 'F'; } } f();"));
}
