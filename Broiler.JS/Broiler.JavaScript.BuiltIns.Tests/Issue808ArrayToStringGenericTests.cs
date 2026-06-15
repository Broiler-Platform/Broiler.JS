using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// §23.1.3.36 Array.prototype.toString does ToObject(this), then Get "join"; if "join" is not callable
// it falls back to %Object.prototype.toString%. It is therefore generic (works on non-array and boxed
// primitive receivers) and honours an overridden/non-callable "join" instead of throwing. Issue #808
// problems 85 and 88.
public class Issue808ArrayToStringGenericTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact]
    public void ToString_NormalArray_JoinsElements()
        => Assert.Equal("1,2,3", Eval("[1, 2, 3].toString();"));

    [Fact]
    public void ToString_OnBoolean_FallsBackToObjectToString()
        => Assert.Equal("[object Boolean]", Eval("Array.prototype.toString.call(true);"));

    [Fact]
    public void ToString_OnPlainObject_FallsBackToObjectToString()
        => Assert.Equal("[object Object]", Eval("Array.prototype.toString.call({});"));

    [Fact]
    public void ToString_NonCallableJoin_UsesObjectToStringWithTag()
        => Assert.Equal("[object Foo]", Eval("""
            var a = [];
            a.join = null;
            a[Symbol.toStringTag] = "Foo";
            Array.prototype.toString.call(a);
        """));

    [Fact]
    public void ToString_OverriddenCallableJoin_IsUsed()
        => Assert.Equal("custom", Eval("""
            var a = [1, 2];
            a.join = function () { return "custom"; };
            a.toString();
        """));

    [Fact]
    public void ToString_OnNullReceiver_Throws()
        => Assert.Equal("TypeError", Eval("""
            var err = "none";
            try { Array.prototype.toString.call(null); }
            catch (e) { err = e.constructor.name; }
            err;
        """));
}
