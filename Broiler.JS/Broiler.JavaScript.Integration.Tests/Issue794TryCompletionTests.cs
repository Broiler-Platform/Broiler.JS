using Broiler.JavaScript.Engine;
using Xunit;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/794 — Problem 17.
//
// A TryStatement's completion value is the value of its Block (or its Catch, when the Block throws),
// with the Finally block's value always discarded and an empty completion replaced by undefined
// (spec TryStatement evaluation + UpdateEmpty). Previously try/catch/finally did not participate in
// completion-value tracking, so e.g. `eval("2; try { 3; } catch {}")` returned undefined instead of 3
// (and `try { } finally { }` did not override the preceding statement's value with undefined).
public class Issue794TryCompletionTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    [Theory]
    // cptn-try: try/catch, catch only runs on throw
    [InlineData("1; try { } catch (err) { }", "undefined")]
    [InlineData("2; try { 3; } catch (err) { }", "3")]
    [InlineData("4; try { } catch (err) { 5; }", "undefined")]
    [InlineData("6; try { 7; } catch (err) { 8; }", "7")]
    // cptn-catch: catch runs (Block throws)
    [InlineData("1; try { throw null; } catch (err) { }", "undefined")]
    [InlineData("2; try { throw null; } catch (err) { 3; }", "3")]
    // cptn-finally-wo-catch: finally value discarded
    [InlineData("1; try { } finally { }", "undefined")]
    [InlineData("2; try { 3; } finally { }", "3")]
    [InlineData("4; try { } finally { 5; }", "undefined")]
    [InlineData("6; try { 7; } finally { 8; }", "7")]
    // cptn-finally-from-catch: caught, then finally discarded
    [InlineData("4; try { throw null; } catch (err) { } finally { 5; }", "undefined")]
    [InlineData("6; try { throw null; } catch (err) { 7; } finally { 8; }", "7")]
    // cptn-finally-skip-catch: not thrown, catch skipped, finally discarded
    [InlineData("2; try { } catch (err) { 3; } finally { }", "undefined")]
    [InlineData("9; try { 10; } catch (err) { } finally { }", "10")]
    [InlineData("17; try { 18; } catch (err) { 19; } finally { 20; }", "18")]
    public void TryStatement_CompletionValue(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    [Fact] // the try statement's undefined completion overrides the preceding statement
    public void EmptyTry_OverridesPreviousValue()
        => Assert.Equal("undefined", Eval("42; try { } finally { }"));

    [Fact] // completion propagates out of nested value-producing statements inside the try block
    public void NestedCompletionInsideTryBlock()
        => Assert.Equal("3", Eval("1; try { 2; if (true) { 3; } } catch (e) { }"));

    [Fact] // a finally value must not leak into an enclosing completion
    public void FinallyValueDoesNotLeak()
        => Assert.Equal("7", Eval("5; try { 7; } finally { 6; }"));

    [Fact] // try inside a loop body still tracks its block value
    public void TryInsideLoopBody()
        => Assert.Equal("99", Eval("for (var i = 0; i < 1; i++) { try { 99; } catch (e) { } }"));
}
