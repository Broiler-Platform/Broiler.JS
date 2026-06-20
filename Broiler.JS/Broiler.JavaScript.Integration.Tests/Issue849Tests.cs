using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/849
public class Issue849Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code);
    }

    // Problem 88: a parenthesized class expression's toString reported the JSFunction
    // "native" fallback ("function native() { [native code] }") because the compiler
    // never captured the original source text. AstNode.Code already spans the class
    // keyword through the closing brace and excludes surrounding parentheses, so the
    // captured substring matches the spec's NativeFunction-vs-class branching.
    [Theory]
    [InlineData("(class {}).toString()", "class {}")]
    [InlineData("((class {})).toString()", "class {}")]
    [InlineData("class C{};C.toString()", "class C{}")]
    [InlineData("(class Named { m(){} }).toString()", "class Named { m(){} }")]
    [InlineData("(class extends Object {}).toString()", "class extends Object {}")]
    public void ClassExpressionToStringReportsSource(string code, string expected)
    {
        Assert.Equal(expected, Eval(code).ToString());
    }

    // The class's own prototype.constructor identity is the same class object, so the
    // constructor accessed via the prototype must report the same source text.
    [Fact]
    public void ClassPrototypeConstructorToStringMatchesClass()
    {
        Assert.Equal("class C { m(){ return 1; } }",
            Eval("class C { m(){ return 1; } }; C.prototype.constructor.toString()").ToString());
    }

    // Native functions still report the "[native code]" placeholder.
    [Fact]
    public void NativeFunctionToStringUnaffected()
    {
        Assert.Equal("function parseInt() { [native code] }",
            Eval("parseInt.toString()").ToString());
    }

    // Problem 95: %RegExpStringIteratorPrototype% must expose its own @@toStringTag
    // ("RegExp String Iterator"). Without it the prototype inherited the
    // %IteratorPrototype% accessor and reported "Iterator" instead.
    [Fact]
    public void RegExpStringIteratorPrototypeHasToStringTag()
    {
        Assert.Equal("RegExp String Iterator",
            Eval("Object.getPrototypeOf(/./[Symbol.matchAll](''))[Symbol.toStringTag]").ToString());
    }

    // The descriptor is { writable: false, enumerable: false, configurable: true }
    // (verifyProperty in the test262 case checks each attribute independently).
    [Fact]
    public void RegExpStringIteratorPrototypeToStringTagDescriptor()
    {
        var code = "var p = Object.getPrototypeOf(/./[Symbol.matchAll](''));"
            + "var d = Object.getOwnPropertyDescriptor(p, Symbol.toStringTag);"
            + "[d.writable, d.enumerable, d.configurable].join(',')";
        Assert.Equal("false,false,true", Eval(code).ToString());
    }

    // Object.prototype.toString picks up the new tag on RegExp String Iterators.
    [Fact]
    public void ObjectToStringUsesRegExpStringIteratorTag()
    {
        Assert.Equal("[object RegExp String Iterator]",
            Eval("Object.prototype.toString.call(/./[Symbol.matchAll](''))").ToString());
    }
}
