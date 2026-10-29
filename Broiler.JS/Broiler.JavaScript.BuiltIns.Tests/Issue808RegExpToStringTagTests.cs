using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// §20.1.3.6 Object.prototype.toString: an object with a [[RegExpMatcher]] internal slot (a real RegExp
// instance) has the builtin tag "RegExp", so Object.prototype.toString.call(/x/) is "[object RegExp]".
// %RegExp.prototype% is an ordinary object (no matcher) and tags as "Object". A string-valued
// @@toStringTag still overrides the builtin tag. Issue #808 problem 85.
public class Issue808RegExpToStringTagTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact]
    public void ObjectToString_OnRegExpInstance_IsRegExp()
        => Assert.Equal("[object RegExp]", Eval("Object.prototype.toString.call(/x/);"));

    [Fact]
    public void ObjectToString_OnRegExpPrototype_IsObject()
        => Assert.Equal("[object Object]", Eval("Object.prototype.toString.call(RegExp.prototype);"));

    [Fact]
    public void ArrayToString_FallbackOnRegExp_IsRegExp()
        // Array.prototype.toString on a RegExp (no callable "join") falls back to Object.prototype.toString.
        => Assert.Equal("[object RegExp]", Eval("Array.prototype.toString.call(new RegExp('a'));"));

    [Fact]
    public void ObjectToString_RegExpWithToStringTag_Overrides()
        => Assert.Equal("[object Foo]", Eval("""
            var re = /x/;
            re[Symbol.toStringTag] = "Foo";
            Object.prototype.toString.call(re);
        """));
}
