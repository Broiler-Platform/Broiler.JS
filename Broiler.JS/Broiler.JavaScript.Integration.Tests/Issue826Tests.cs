using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/826
//
// Problems 9 / 32 / 33 — A simple assignment must call PutValue against the
// Reference resolved from the LeftHandSideExpression BEFORE the right-hand side
// runs (S11.13.1_A6). Broiler resolved the target binding AFTER the RHS, so an
// assignment whose RHS introduced a more local binding of the same name (a direct
// eval declaring `var x`, or a `with` object gaining the property) wrongly
// retargeted the write — and the eval-shadow case additionally threw
// "Cannot access 'x' before initialization" because the as-yet-uninitialized
// shadow was read by the eval's own `var x` declaration.
//
//   T1: `x = (eval("var x;"), 1)`        -> outer x === 1, inner x === undefined
//   T2: `x = (eval("var x = 2;"), 1)`    -> outer x === 1, inner x === 2
//   T3: `with (scope) x = (scope.x=2,1)` -> outer x === 1, scope.x === 2
public class Issue826Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // S11.13.1_A6_T1: the eval introduces an (uninitialized) inner `var x`; the
    // assignment still writes to the outer x via the initially resolved reference,
    // and `return x` observes the inner binding (undefined).
    [Fact]
    public void SimpleAssignment_EvalVarInRhs_WritesOuterReference()
        => Assert.Equal("undefined|1", Eval(
            "function t(){" +
            "  var x = 0;" +
            "  var innerX = (function(){ x = (eval('var x;'), 1); return x; })();" +
            "  return typeof innerX + '|' + x;" +
            "} t();"));

    // S11.13.1_A6_T2: same, but the eval initializes the inner binding to 2.
    [Fact]
    public void SimpleAssignment_EvalVarWithInitInRhs_WritesOuterReference()
        => Assert.Equal("2|1", Eval(
            "function t(){" +
            "  var x = 0;" +
            "  var innerX = (function(){ x = (eval('var x = 2;'), 1); return x; })();" +
            "  return innerX + '|' + x;" +
            "} t();"));

    // S11.13.1_A6_T3: the RHS adds `x` to the with object, which must NOT redirect
    // the already-resolved write to the outer x.
    [Fact]
    public void SimpleAssignment_WithObjectGainsProperty_WritesOuterReference()
        => Assert.Equal("2|1", Eval(
            "function t(){" +
            "  var x = 0;" +
            "  var scope = {};" +
            "  with (scope) { x = (scope.x = 2, 1); }" +
            "  return scope.x + '|' + x;" +
            "} t();"));

    // Once a direct eval has introduced `var x`, a subsequent assignment targets the
    // new inner binding (the shadow now owns its value), leaving the outer untouched.
    [Fact]
    public void AssignmentAfterEvalVar_TargetsInnerBinding()
        => Assert.Equal("9|5", Eval(
            "var x = 5;" +
            "var inner = (function(){ eval('var x;'); x = 9; return x; })();" +
            "inner + '|' + x;"));

    // A closed-over outer variable is read correctly inside a function that contains
    // a body direct eval introducing the same name, before the eval runs.
    [Fact]
    public void ReadOuterBeforeEvalIntroducesVar()
        => Assert.Equal("5", Eval(
            "var x = 5;" +
            "(function(){ var r = x; eval('var x;'); return r; })();"));
}
