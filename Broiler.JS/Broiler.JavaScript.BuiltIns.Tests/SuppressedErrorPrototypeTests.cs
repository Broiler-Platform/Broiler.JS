using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// SuppressedError.prototype is a plain object without an [[ErrorData]] slot: `error` and `suppressed`
// are own data properties created on each instance, absent on the prototype. Issue #805 problem 35.
public class SuppressedErrorPrototypeTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);
    private static string E(string e) { Load(); using var c = new JSContext(); return c.Eval(e).ToString(); }

    [Theory]
    [InlineData("SuppressedError.prototype.hasOwnProperty('error')", "false")]
    [InlineData("SuppressedError.prototype.hasOwnProperty('suppressed')", "false")]
    public void Prototype_DoesNotOwnErrorOrSuppressed(string expr, string expected)
        => Assert.Equal(expected, E(expr));

    [Theory]
    [InlineData("new SuppressedError(1, 2, 'm').hasOwnProperty('error')", "true")]
    [InlineData("new SuppressedError(1, 2, 'm').hasOwnProperty('suppressed')", "true")]
    [InlineData("new SuppressedError(1, 2, 'm').error", "1")]
    [InlineData("new SuppressedError(1, 2, 'm').suppressed", "2")]
    [InlineData("new SuppressedError(1, 2, 'm').message", "m")]
    public void Instance_HasOwnErrorAndSuppressed(string expr, string expected)
        => Assert.Equal(expected, E(expr));

    [Fact]
    public void Instance_ErrorAndSuppressed_AreNonEnumerable()
        => Assert.Equal("", E("Object.keys(new SuppressedError(1, 2, 'm')).join(',')"));

    [Fact]
    public void Instance_ErrorIsWritableAndConfigurable()
        => Assert.Equal("writable:true,configurable:true,enumerable:false", E("""
            var d = Object.getOwnPropertyDescriptor(new SuppressedError(1, 2, 'm'), 'error');
            'writable:' + d.writable + ',configurable:' + d.configurable + ',enumerable:' + d.enumerable;
        """));
}
