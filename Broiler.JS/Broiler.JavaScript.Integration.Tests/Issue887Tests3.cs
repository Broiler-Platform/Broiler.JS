using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/887 — two engine crashes
// from the issue's crash cluster:
//
//   * Cluster G (issue Problem 145): BigInt `>>` / `<<` overflowed the shift-count cast
//     (`(byte)` / `(int)`), throwing a host OverflowException for large or negative counts.
//   * Cluster H (issue Problem 144): a `super()` nested in an arrow inside a derived
//     constructor that declares instance members NullReferenced at compile time in
//     FastCompiler.InitMembers (the arrow scope's MemberInits is null).
public class Issue887Tests3
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code)?.ToString();
    }

    private static string ErrorName(string body) =>
        Eval("let t='NONE'; try { " + body + " } catch (e) { t = e.constructor.name; } t");

    // ── Cluster G — BigInt signed shift count handling ────────────────────────────

    [Theory]
    // Negative right shift is a left shift; large counts must not overflow the cast.
    [InlineData("0b101n >> -1n", "10")]
    [InlineData("0b101n >> 1n", "2")]
    [InlineData("0b101n >> 3n", "0")]
    [InlineData("-5n >> 1n", "-3")]
    [InlineData("-5n >> -1n", "-10")]
    [InlineData("0x246n >> 128n", "0")]
    [InlineData("-0x246n >> 128n", "-1")]
    [InlineData("123n >> 9999999999999n", "0")]      // huge positive right shift → 0 (was OverflowException)
    [InlineData("-7n >> 9999999999999n", "-1")]      // huge positive right shift of negative → -1
    [InlineData("1n << 64n", "18446744073709551616")]
    [InlineData("8n << -2n", "2")]                    // negative left shift is a right shift
    public void BigIntShift(string expr, string expected)
        => Assert.Equal(expected, Eval($"String({expr})"));

    [Fact]
    public void BigIntHugeLeftShiftThrowsRangeError()
        // `5n >> -1e12n` == `5n << 1e12n` exceeds any representable BigInt → RangeError, not a crash.
        => Assert.Equal("RangeError", ErrorName("5n >> -1000000000000n"));

    // ── Cluster H — super() in an arrow inside a derived constructor ───────────────

    [Fact]
    public void PrivateFieldDestructuringTargetBeforeSuperThrowsReferenceError()
        // Accessing `this.#field` (a destructuring target) before `super()` runs must throw a
        // ReferenceError (this-TDZ) — it previously NullReferenced in the compiler.
        => Assert.Equal("ReferenceError", ErrorName(
            "class C extends class {} {" +
            "  #field;" +
            "  constructor() { var init = () => super(); var o = { get a() { init(); } }; ({ a: this.#field } = o); }" +
            "} new C();"));

    [Fact]
    public void ArrowSuperWithDefaultedFieldDoesNotCrash()
        // A `super()` reached through an arrow in a class that declares a field must compile and run.
        => Assert.Equal("7", Eval(
            "class B { constructor(x) { this.x = x; } }" +
            "class C extends B { constructor() { var go = () => super(7); go(); } } " +
            "'' + new C().x"));

    [Fact]
    public void DirectSuperWithFieldsStillInitializes()
        => Assert.Equal("3", Eval(
            "class B {} class C extends B { a = 1; b = 2; constructor() { super(); } } " +
            "var c = new C(); '' + (c.a + c.b)"));

    [Fact]
    public void PrivateMethodWithArrowSuperStillWorks()
        => Assert.Equal("3", Eval(
            "class B {} class C extends B { #m() { return 3; } constructor() { (() => super())(); } call() { return this.#m(); } } " +
            "'' + new C().call()"));
}
