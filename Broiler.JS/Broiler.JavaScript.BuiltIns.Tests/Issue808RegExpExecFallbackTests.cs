using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// §22.2.7.1 RegExpExec uses a callable "exec" property; otherwise (absent or any non-callable value
// such as null or a number) it falls back to the builtin RegExpBuiltinExec rather than throwing. The
// String methods that consume RegExpExec (match, replace, split, …) therefore still work when "exec"
// has been overwritten with a non-callable value. Issue #808 problem 100.
public class Issue808RegExpExecFallbackTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact]
    public void Match_NonCallableExec_UsesBuiltin()
        => Assert.Equal("a", Eval("""
            var re = /a/;
            re.exec = 42;
            JSON.stringify("xax".match(re));
        """).Replace("[", "").Replace("]", "").Replace("\"", ""));

    [Fact]
    public void Replace_NullExec_UsesBuiltin()
        => Assert.Equal("xYxY", Eval("""
            var re = /a/g;
            re.exec = null;
            "xaxa".replace(re, "Y");
        """));

    [Fact]
    public void Split_NonCallableExec_UsesBuiltin()
        => Assert.Equal("x|x", Eval("""
            var re = /a/;
            re.exec = "notfn";
            "xax".split(re).join("|");
        """));

    [Fact]
    public void Replace_CallableExec_IsUsed()
        => Assert.Equal("Z", Eval("""
            var re = /a/;
            re.exec = function () { return ["custom"]; };
            "xax".replace(re, "Z");
        """));

    [Fact]
    public void Replace_ExecReturnsNonObject_Throws()
        => Assert.Equal("TypeError", Eval("""
            var re = /a/;
            re.exec = function () { return 5; };
            var err = "none";
            try { "xax".replace(re, "Q"); }
            catch (e) { err = e.constructor.name; }
            err;
        """));
}
