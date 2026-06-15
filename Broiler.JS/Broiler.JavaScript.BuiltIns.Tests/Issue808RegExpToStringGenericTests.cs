using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// §22.2.6.13 RegExp.prototype.toString is generic: it requires only that the receiver be an Object and
// builds its result from the receiver's "source" and "flags" properties (each coerced with ToString),
// reading source before flags. It therefore works on RegExp.prototype itself and on non-RegExp objects
// rather than throwing for an "incompatible receiver". Issue #808 problem 41.
public class Issue808RegExpToStringGenericTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact]
    public void ToString_OnPrototype_ReturnsEmptyPattern()
        => Assert.Equal("/(?:)/", Eval("RegExp.prototype.toString();"));

    [Fact]
    public void ToString_OnRegExpInstance_Works()
        => Assert.Equal("/ab+c/gi", Eval("/ab+c/gi.toString();"));

    [Fact]
    public void ToString_OnGenericObject_UsesSourceAndFlags()
        => Assert.Equal("/abc/g", Eval("RegExp.prototype.toString.call({ source: 'abc', flags: 'g' });"));

    [Fact]
    public void ToString_ReadsSourceBeforeFlags()
    {
        var actual = Eval("""
            var log = [];
            var o = {
                get source() { log.push('source'); return 'x'; },
                get flags() { log.push('flags'); return 'y'; },
            };
            var s = RegExp.prototype.toString.call(o);
            s + '|' + log.join(',');
        """);
        Assert.Equal("/x/y|source,flags", actual);
    }

    [Fact]
    public void ToString_OnNonObject_Throws()
        => Assert.Equal("TypeError", Eval("""
            var err = "none";
            try { RegExp.prototype.toString.call(undefined); }
            catch (e) { err = e.constructor.name; }
            err;
        """));
}
