using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for the switch-statement numeric/string "fast path" type
// soundness. A switch matches with the Strict Equality Comparison (===), but
// when every `case` value is a number or a string the compiler coerces the
// discriminant with a typed IL comparison (int Beq / double Beq / string hash)
// for speed. Previously the discriminant was coerced unconditionally and
// lossily, so a wrong-typed discriminant could match (e.g. `switch(true)` hit
// `case 1` because true coerced to 1), and NaN/Infinity/fractional values
// mis-matched integer cases via truncation. A pure-double switch additionally
// threw NotSupportedException in ConvertToNumber, and the IL generator had no
// double comparison path (it emitted a call to a null CompareMethod).
//
// The dispatch is now guarded with a strict type check: the typed switch is only
// entered when the discriminant is a Number (and, for the integer path, an exact
// in-range integer) or a String respectively; otherwise control jumps straight
// to the default/break. This makes the fast path agree with ===.
public class SwitchTypeSoundnessTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    private const string IntSwitch =
        "function f(v){ switch(v){ case 1: return 'one'; case 2: return 'two'; default: return 'd'; } } ";
    private const string DoubleSwitch =
        "function f(v){ switch(v){ case 1.5: return 'a'; case 2.5: return 'b'; default: return 'd'; } } ";
    private const string StringSwitch =
        "function f(v){ switch(v){ case 'x': return 'X'; case 'y': return 'Y'; default: return 'd'; } } ";

    // ---- integer-case switch: only an exact integer Number matches ----

    [Fact]
    public void IntSwitchMatchesNumber() => Assert.Equal("one", Eval(IntSwitch + "f(1)"));

    [Fact]
    public void IntSwitchBooleanDoesNotMatch() => Assert.Equal("d", Eval(IntSwitch + "f(true)"));

    [Fact]
    public void IntSwitchNullDoesNotMatch() => Assert.Equal("d", Eval(IntSwitch + "f(null)"));

    [Fact]
    public void IntSwitchUndefinedDoesNotMatch() => Assert.Equal("d", Eval(IntSwitch + "f(undefined)"));

    [Fact]
    public void IntSwitchNaNDoesNotMatch() => Assert.Equal("d", Eval(IntSwitch + "f(NaN)"));

    [Fact]
    public void IntSwitchInfinityDoesNotMatch() => Assert.Equal("d", Eval(IntSwitch + "f(Infinity)"));

    [Fact]
    public void IntSwitchFractionDoesNotMatch() => Assert.Equal("d", Eval(IntSwitch + "f(1.5)"));

    [Fact]
    public void IntSwitchNumericStringDoesNotMatch() => Assert.Equal("d", Eval(IntSwitch + "f('1')"));

    [Fact]
    public void IntSwitchBoxedNumberDoesNotMatch()
        => Assert.Equal("d", Eval(IntSwitch + "f(new Number(1))"));

    [Fact]
    public void IntSwitchBigIntDoesNotMatch()
        // 1n !== 1; reading the numeric value of a BigInt would throw, so the
        // type guard must short-circuit before coercing.
        => Assert.Equal("d", Eval(IntSwitch + "f(1n)"));

    [Fact]
    public void IntSwitchOutOfRangeIntegerDoesNotMatch()
        => Assert.Equal("d", Eval(IntSwitch + "f(3000000000)"));

    [Fact]
    public void IntSwitchNegativeZeroMatchesZero()
        => Assert.Equal("zero", Eval(
            "function f(v){ switch(v){ case 0: return 'zero'; default: return 'd'; } } f(-0)"));

    // ---- fractional-case switch (the previously-broken double path) ----

    [Fact]
    public void DoubleSwitchMatchesFraction() => Assert.Equal("a", Eval(DoubleSwitch + "f(1.5)"));

    [Fact]
    public void DoubleSwitchMatchesSecondFraction() => Assert.Equal("b", Eval(DoubleSwitch + "f(2.5)"));

    [Fact]
    public void DoubleSwitchNoMatch() => Assert.Equal("d", Eval(DoubleSwitch + "f(9)"));

    [Fact]
    public void DoubleSwitchBooleanDoesNotMatch() => Assert.Equal("d", Eval(DoubleSwitch + "f(true)"));

    [Fact]
    public void DoubleSwitchNaNDoesNotMatch() => Assert.Equal("d", Eval(DoubleSwitch + "f(NaN)"));

    // ---- string-case switch: only a String primitive matches ----

    [Fact]
    public void StringSwitchMatches() => Assert.Equal("X", Eval(StringSwitch + "f('x')"));

    [Fact]
    public void StringSwitchNoMatch() => Assert.Equal("d", Eval(StringSwitch + "f('z')"));

    [Fact]
    public void StringSwitchNumberDoesNotMatch() => Assert.Equal("d", Eval(StringSwitch + "f(1)"));

    [Fact]
    public void StringSwitchBooleanDoesNotMatch() => Assert.Equal("d", Eval(StringSwitch + "f(true)"));

    [Fact]
    public void StringSwitchBoxedStringDoesNotMatch()
        => Assert.Equal("d", Eval(StringSwitch + "f(new String('x'))"));

    // ---- the discriminant is still evaluated exactly once ----

    [Fact]
    public void DiscriminantEvaluatedOnce()
        => Assert.Equal("1", Eval(
            "var n = 0; function side(){ n++; return true; }"
            + " switch(side()){ case 1: break; default: break; } n"));
}
