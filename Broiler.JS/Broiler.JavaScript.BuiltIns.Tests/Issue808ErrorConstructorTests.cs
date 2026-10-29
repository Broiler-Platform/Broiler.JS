using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// SuppressedError is an ordinary error constructor: it is callable without `new` (problem 86) and its
// [[Prototype]] is the Error constructor. Error message coercion uses ToString, so an object message
// observes its toString (a thrown value propagates) and a Symbol message — directly or via toString —
// is a TypeError rather than being silently stringified (problems 37 & 38). Issue #808.
public class Issue808ErrorConstructorTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Fact]
    public void SuppressedError_CallableWithoutNew()
        => Assert.Equal("true|m|1|2", Eval("""
            var e = SuppressedError(1, 2, "m");
            [e instanceof SuppressedError, e.message, e.error, e.suppressed].join("|");
        """));

    [Fact]
    public void SuppressedError_PrototypeIsError()
        => Assert.Equal("true", Eval("String(Object.getPrototypeOf(SuppressedError) === Error);"));

    [Fact]
    public void SuppressedError_LengthAndName()
        => Assert.Equal("3|SuppressedError", Eval("SuppressedError.length + '|' + SuppressedError.name;"));

    [Fact]
    public void SuppressedError_NewStillWorks()
        => Assert.Equal("true|true", Eval("""
            var e = new SuppressedError(1, 2, "m");
            (e instanceof SuppressedError) + "|" + (e instanceof Error);
        """));

    [Theory]
    [InlineData("Symbol()")]
    [InlineData("{ toString() { return Symbol(); } }")]
    public void Error_SymbolMessage_Throws(string message)
        => Assert.Equal("TypeError", Eval($$"""
            var err = "none";
            try { new Error({{message}}); }
            catch (e) { err = e.constructor.name; }
            err;
        """));

    [Fact]
    public void Error_MessageToStringThrow_Propagates()
        => Assert.Equal("42", Eval("""
            var caught = "none";
            try { new SuppressedError(1, 2, { toString() { throw { custom: 42 }; } }); }
            catch (e) { caught = String(e && e.custom); }
            caught;
        """));

    [Fact]
    public void Error_ObjectMessage_UsesToString()
        => Assert.Equal("hello", Eval("new Error({ toString() { return 'hello'; } }).message;"));
}
