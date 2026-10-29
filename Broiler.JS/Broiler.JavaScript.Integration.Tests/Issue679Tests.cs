using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/679
//
// Fixed here (subset of the problems grouped in the issue by their Test262Error
// message):
//
//   * Derived/base constructor [[Construct]] return-value semantics
//     (OrdinaryCallEvaluateBody on a return completion). An object return passes
//     through; a base constructor discards any other value in favour of `this`;
//     a derived constructor throws a TypeError for a non-undefined non-object
//     return and a ReferenceError for an undefined return when `super` was never
//     called. Covers P1 derived-return-val and several P7 derivedConstructor*
//     cases.
//
//   * super(...) BindThisValue: the derived constructor `this` binding may be
//     initialized only once, so a second super() call is a ReferenceError. The
//     superclass [[Construct]] still runs on every call (it precedes the bind) and
//     instance field initializers run only on the successful bind. Covers P7
//     fields-run-once-on-double-super and superCallThisInit.
//
//   * `delete super.x` / `delete super[expr]` is always a ReferenceError (delete
//     operator runtime semantics step 5a, IsSuperReference). Covers P7
//     delete/super-property* and the sm superPropDelete/superElemDelete tests.
//
//   * DataView.prototype.setBigInt64/setBigUint64 coerce the value with ToBigInt
//     BEFORE the out-of-bounds check, so `setBigInt64(0)` with no value argument
//     is a TypeError (ToBigInt(undefined)); the stored 64-bit pattern is taken
//     from the full BigInteger so magnitudes outside the signed-64 range no longer
//     overflow. Covers P1 DataView setBigInt64/no-value-arg.
//
//   * Number.prototype.toPrecision coerces its precision argument with ToNumber
//     (TypeError for a Symbol or BigInt) instead of silently ignoring a
//     non-Number argument. Covers P1 toPrecision return-abrupt-tointeger-symbol.
//
// Out of scope (triaged in the issue): P8 private-method/getter/setter brand check
// across multiple class evaluations (private names are modelled as shared string
// keys rather than per-evaluation brands — architectural); the remaining
// message-grouped P1/P3/P4/P6/P9/P10 cases (Intl/CLDR, Object.defineProperty
// accessor ordering, dstr IteratorClose, AnnexB eval binding re-init) each have
// distinct unrelated root causes.
public class Issue679Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // Returns the constructor name of whatever `expr` throws, or "no-throw".
    private static string ThrowKind(string expr)
        => Eval("(function(){ try { " + expr + "; return 'no-throw'; }"
              + " catch (e) { return e && e.constructor ? e.constructor.name : String(e); } })()");

    // ---- Constructor return-value semantics ----

    [Fact]
    public void BaseConstructorReturnPrimitiveIsIgnored()
        => Assert.Equal("true", Eval(
            "class B { constructor(){ return null; } } (new B() instanceof B)"));

    [Fact]
    public void BaseConstructorReturnObjectIsUsed()
        => Assert.Equal("9", Eval(
            "class B { constructor(){ return { z: 9 }; } } String(new B().z)"));

    [Fact]
    public void DerivedConstructorReturnPrimitiveThrowsTypeError()
        => Assert.Equal("TypeError", ThrowKind(
            "class C extends Object { constructor(){ return null; } } new C()"));

    [Fact]
    public void DerivedConstructorReturnUndefinedWithoutSuperThrowsReferenceError()
        => Assert.Equal("ReferenceError", ThrowKind(
            "class C extends Object { constructor(){ return undefined; } } new C()"));

    [Fact]
    public void DerivedConstructorReturnObjectWithoutSuperIsAllowed()
        => Assert.Equal("1", Eval(
            "class C extends Object { constructor(){ return { ok: 1 }; } } String(new C().ok)"));

    [Fact]
    public void DerivedConstructorImplicitReturnUsesSuperThis()
        => Assert.Equal("true", Eval(
            "class C extends Object { constructor(){ super(); } } (new C() instanceof C)"));

    // ---- super(...) BindThisValue (single call) ----

    [Fact]
    public void SecondSuperCallThrowsReferenceError()
        => Assert.Equal("ReferenceError", ThrowKind(
            "class B {} class C extends B { constructor(){ super(); super(); } } new C()"));

    [Fact]
    public void DoubleSuperRunsBaseTwiceButFieldsOnce()
        => Assert.Equal("2,1", Eval(
            "var base=0, field=0;"
            + " class B { constructor(){ ++base; } }"
            + " var C = class extends B { f = ++field;"
            + "   constructor(){ super();"
            + "     try { super(); } catch (e) {} } };"
            + " new C(); base + ',' + field"));

    [Fact]
    public void AccessThisBeforeSuperThrowsReferenceError()
        => Assert.Equal("ReferenceError", ThrowKind(
            "class B {} class C extends B { constructor(){ this; super(); } } new C()"));

    // ---- delete super reference ----

    [Fact]
    public void DeleteSuperPropertyThrowsReferenceError()
        => Assert.Equal("ReferenceError", ThrowKind(
            "class C extends Object { constructor(){ super(); delete super.x; } } new C()"));

    [Fact]
    public void DeleteSuperComputedThrowsReferenceError()
        => Assert.Equal("ReferenceError", ThrowKind(
            "class C extends Object { constructor(){ super(); delete super['x']; } } new C()"));

    [Fact]
    public void DeleteSuperInMethodThrowsReferenceError()
        => Assert.Equal("ReferenceError", ThrowKind(
            "class C extends Object { m(){ delete super.x; } } new C().m()"));

    // ---- DataView setBigInt64 / setBigUint64 ----

    [Fact]
    public void SetBigInt64WithoutValueThrowsTypeError()
        => Assert.Equal("TypeError", ThrowKind(
            "new DataView(new ArrayBuffer(8)).setBigInt64(0)"));

    [Fact]
    public void SetBigUint64WithoutValueThrowsTypeError()
        => Assert.Equal("TypeError", ThrowKind(
            "new DataView(new ArrayBuffer(8)).setBigUint64(0)"));

    [Fact]
    public void SetBigInt64RoundTripsNegative()
        => Assert.Equal("true", Eval(
            "var dv = new DataView(new ArrayBuffer(8));"
            + " dv.setBigInt64(0, -2n); (dv.getBigInt64(0) === -2n)"));

    [Fact]
    public void SetBigUint64RoundTripsLargeMagnitude()
        => Assert.Equal("true", Eval(
            "var dv = new DataView(new ArrayBuffer(8));"
            + " dv.setBigUint64(0, 18446744073709551614n);"
            + " (dv.getBigUint64(0) === 18446744073709551614n)"));

    // ---- Number.prototype.toPrecision argument coercion ----

    [Fact]
    public void ToPrecisionWithSymbolThrowsTypeError()
        => Assert.Equal("TypeError", ThrowKind(
            "Number.prototype.toPrecision(Symbol('1'))"));

    [Fact]
    public void ToPrecisionWithUndefinedReturnsToString()
        => Assert.Equal("123", Eval("(123).toPrecision(undefined)"));

    [Fact]
    public void ToPrecisionWithNumberStillFormats()
        => Assert.Equal("1.0", Eval("(1).toPrecision(2)"));
}
