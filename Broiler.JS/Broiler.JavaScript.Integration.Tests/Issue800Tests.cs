using Broiler.JavaScript.Engine;
using Xunit;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/800
//
// Covered here (the cross-cutting ones that exercise the compiler / whole engine; the many
// Temporal-only fixes have unit tests in Broiler.JavaScript.BuiltIns.Tests):
//   * Problem 23 — a `using` declaration whose initializer is not a disposable resource throws a
//     TypeError at the declaration (when the disposable resource is created and its Symbol.dispose
//     method is resolved), not a SuppressedError at end of block. Valid resources still dispose
//     last-in-first-out, and null/undefined initializers are no-ops.
//   * Problem 1 — Temporal.PlainMonthDay.from accepts a bare numeric month/day without a year.
//   * Problem 9 — a basic-form ISO date string (YYYYMMDD) parses.
public class Issue800Tests
{
    private static string ErrorName(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval($"let __t='NONE'; try {{ {code} }} catch (e) {{ __t = e.constructor.name; }} __t").ToString();
    }

    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ── Problem 23: `using` declaration rejects a non-disposable at declaration time ─────────────

    [Theory]
    [InlineData("{ using x = {}; }")]
    [InlineData("{ using x = { [Symbol.dispose]: null }; }")]
    [InlineData("{ using x = { [Symbol.dispose]: undefined }; }")]
    [InlineData("{ using x = { [Symbol.dispose]: 42 }; }")]
    [InlineData("{ using x = 5; }")]
    public void Using_NonDisposable_ThrowsTypeError(string code)
    {
        Assert.Equal("TypeError", ErrorName(code));
    }

    [Fact]
    public void Using_ValidResources_DisposeLastInFirstOut()
    {
        Assert.Equal("body,b,a", Eval("""
            let order = [];
            {
              using a = { [Symbol.dispose]() { order.push('a'); } };
              using b = { [Symbol.dispose]() { order.push('b'); } };
              order.push('body');
            }
            order.join(',');
        """));
    }

    [Fact]
    public void Using_NullOrUndefined_IsNoOp()
    {
        Assert.Equal("ok", Eval("{ using x = null; using y = undefined; } 'ok';"));
    }

    [Fact]
    public void Using_SingleDisposalError_IsNotWrappedInSuppressedError()
    {
        // A lone error thrown during disposal propagates as-is (not a SuppressedError chain).
        Assert.Equal("RangeError", ErrorName("""
            { using x = { [Symbol.dispose]() { throw new RangeError('boom'); } }; }
        """));
    }

    // ── Problems 1 & 9: Temporal parsing completeness ────────────────────────────────────────────

    [Fact]
    public void PlainMonthDay_From_NumericMonthWithoutYear()
        => Assert.Equal("M02|29", Eval("const d = Temporal.PlainMonthDay.from({ month: 2, day: 29 }); d.monthCode + '|' + d.day;"));

    [Fact]
    public void PlainDate_From_BasicDateForm()
        => Assert.Equal("2020-01-01", Eval("Temporal.PlainDate.from('20200101').toString();"));
}
