using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #871 (fourth batch): constructor proper-tail-call resolution.
//
// Under the script host, a function body ending in `return g()` compiles to a JSTailCall sentinel
// (the proper-tail-call trampoline that [[Call]]/InvokeFunction unwraps in its loop). [[Construct]]
// invoked the body delegate directly and did NOT unwrap the sentinel, so a constructor whose body
// tail-returns an object leaked the raw JSTailCall instead of honouring the explicit object return
// (e.g. `new (function(){ return {a:1}; })().a` was undefined). This surfaced via test262
// staging/sm/class/newTargetArrow.js (which additionally needs new.target closure capture).
public class Issue871FourthTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext(experimentalFeatures: JavaScriptFeatureFlags.AllExperimentalEs2026);
        return ctx.Eval(source).ToString();
    }

    [Theory]
    // A constructor that tail-returns an object must yield that object (explicit object return).
    [InlineData("new (function(){ return { a: 9 }; })().a", "9")]
    [InlineData("(function G(){ return { a: 9 }; }, new (function F(){ return (function(){ return { a: 9 }; })(); })().a)", "9")]
    [InlineData("new (function(){ this.z = 7; return (function(){ return 5; })(); })().z", "7")] // primitive tail-return ignored
    [InlineData("new (function F(){ return (() => new.target)(); })() === undefined", "false")]  // arrow tail-call sees new.target
    public void Constructor_TailReturn_HonoursObjectReturn(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr})"));

    [Theory]
    // Ordinary (non-constructor) tail calls and ordinary constructors are unaffected.
    [InlineData("(function G(){ return 3; }, (function F(){ return (function(){ return 3; })(); })())", "3")]
    [InlineData("new (function(){ this.x = 1; })().x", "1")]
    [InlineData("typeof new (function(){ this.x = 1; return 5; })()", "object")] // primitive return -> new instance
    [InlineData("new (function(){ this.x = 1; return 5; })().x", "1")]
    public void Constructor_OrdinaryReturnsUnchanged(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr})"));
}
