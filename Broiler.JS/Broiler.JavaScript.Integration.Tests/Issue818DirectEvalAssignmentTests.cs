using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/818 — Problem 12
// (test/language/expressions/call/eval-spread-empty-leading.js and
//  test/language/expressions/call/eval-spread-empty-trailing.js):
//   Test262Error: Expected SameValue(«0», «"global"») to be true.
//
// A *plain assignment* inside a direct eval (`eval("x = 0;")`) to a name that the
// enclosing function declares as a local `var`/parameter wrote through to a
// same-named program-level global as well as the (correct) function-local binding.
// The direct-eval overlay published the captured local into globalVars while the
// real global-object property still existed, so AssignIdentifier's dual-binding
// write hit BOTH. The captured function-owned bindings are now passed as the
// direct-eval scope's shadowed subset (mirroring the `with`-fallback path), so
// their eval-body writes stay local and never leak to the shadowed global.
public class Issue818DirectEvalAssignmentTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    [Fact]
    public void DirectEvalAssignToLocalVarDoesNotLeakToGlobal()
        => Assert.Equal("global", Eval(
            "var x = 'global'; (function () { var x = 'local'; eval('x = 0;'); })(); '' + x"));

    [Fact]
    public void DirectEvalAssignToLocalVarUpdatesTheLocalBinding()
        => Assert.Equal("0", Eval(
            "var x = 'global'; (function () { var x = 'local'; eval('x = 0;'); return '' + x; })()"));

    [Fact]
    public void DirectEvalAssignToParameterDoesNotLeakToGlobal()
        => Assert.Equal("global", Eval(
            "var x = 'global'; (function (x) { eval('x = 0;'); })('param'); '' + x"));

    [Fact]
    public void DirectEvalAssignToGenuineGlobalStillWritesGlobal()
        => Assert.Equal("2", Eval(
            "var x = 1; (function () { eval('x = 2;'); })(); '' + x"));

    [Fact]
    public void DirectEvalVarRedeclarationDoesNotLeakToGlobal()
        => Assert.Equal("global", Eval(
            "var x = 'global'; (function () { var x = 'local'; eval('var x = 9;'); })(); '' + x"));

    // The exact eval-spread-empty-leading.js shape: an empty leading spread followed
    // by the eval text makes a direct eval whose only argument is the source string.
    [Fact]
    public void DirectEvalEmptyLeadingSpreadAssignsLocalOnly()
        => Assert.Equal("0:global", Eval(
            "var nextCount = 0;" +
            "var iter = {};" +
            "iter[Symbol.iterator] = function () {" +
            "  return { next: function () { var i = nextCount++; return { done: true, value: undefined }; } };" +
            "};" +
            "var x = 'global';" +
            "var inner;" +
            "(function () { var x = 'local'; eval(...iter, 'x = 0;'); inner = x; })();" +
            "inner + ':' + x"));
}
