using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/857 — three test262
// script-host failures fixed together:
//
//  • Problem 4: a class declared inside a generator with two `[yield …]` computed property
//    names made the generator rewriter lift the same reused compiler temp twice, so the
//    original→box ToDictionary threw "An item with the same key has already been added".
//  • Problem 7: a `using` LexicalDeclaration in a C-style for head (`for (using x = …; …; …)`)
//    was rejected as a SyntaxError even though it is valid Explicit Resource Management syntax.
//  • Problems 5/8/9/10: multiple non-critical calendar annotations
//    ("[u-ca=iso8601][u-ca=discord]") were rejected; they are valid (first wins) and only a
//    critical ("!") flag among several makes them a RangeError. (Covered in Issue781Tests.)
public class Issue857Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code);
    }

    // Problem 4: two `[yield …]` computed property names of a class inside a generator share one
    // reused compiler temp; the rewriter must box it once instead of faulting on a duplicate key.
    [Fact]
    public void ClassWithMultipleYieldComputedNamesInsideGenerator()
    {
        var code =
            "(function () {"
            + "  function* g() {"
            + "    let C = class {"
            + "      [yield 9]() { return 'a'; }"
            + "      static [yield 9]() { return 'b'; }"
            + "    };"
            + "    let c = new C();"
            + "    return c[yield 9]() + C[yield 9]();"
            + "  }"
            + "  var it = g();"
            + "  while (!it.next().done) ;"
            + "  return 'ok';"
            + "})()";
        Assert.Equal("ok", Eval(code).ToString());
    }

    // The doubled object-literal computed-name variant exercises the same shared-temp path.
    [Fact]
    public void ObjectLiteralWithMultipleYieldComputedNamesInsideGenerator()
    {
        var code =
            "(function () {"
            + "  function* g() { return { [yield 1]: 'x', [yield 2]: 'y' }; }"
            + "  var it = g();"
            + "  it.next(); it.next('a'); var r = it.next('b');"
            + "  return r.value.a + r.value.b;"
            + "})()";
        Assert.Equal("xy", Eval(code).ToString());
    }

    // Problem 7: a sync `using` declaration is a valid C-style for-head LexicalDeclaration.
    [Fact]
    public void SyncUsingInCStyleForHeadIsDisposed()
    {
        var code =
            "(function () {"
            + "  var log = [];"
            + "  for (using x = { [Symbol.dispose]() { log.push('disposed'); } }; false;) {}"
            + "  return log.join(',');"
            + "})()";
        Assert.Equal("disposed", Eval(code).ToString());
    }
}
