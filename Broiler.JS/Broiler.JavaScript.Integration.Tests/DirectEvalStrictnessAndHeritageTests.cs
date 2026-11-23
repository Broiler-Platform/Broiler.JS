using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

/// <summary>
/// Window W2 of milestone SJL-14 (bundle <c>sjl-14-001</c>; decisions D41 and D44, as Broiler.VM's JSD-0083 revised
/// them): direct eval code inherits its caller's strictness as the shared library's forced strictness, in the validating
/// parse and in the compile, instead of a <c>"use strict";</c> line put before its text, and the derived-constructor flag
/// the eval inherits comes from the class's heritage (correction R24 of K10).
/// </summary>
public class DirectEvalStrictnessAndHeritageTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // The constructor name of what evaluating the eval code throws, or "none".
    private static string Thrown(string strictness, string evalCode)
        => Eval(strictness + "var name; try { eval(" + evalCode + "); name = 'none'; } catch (e) { name = e.constructor.name; } name");

    [Fact]
    public void AStrictCallersEvalCodeIsStrict()
        // Strict code's this in a function called plainly is undefined; sloppy code's is the global object.
        => Assert.Equal("undefined", Eval("'use strict'; eval('(function () { return typeof this; })()')"));

    [Fact]
    public void ASloppyCallersEvalCodeIsSloppy()
        => Assert.Equal("object", Eval("eval('(function () { return typeof this; })()')"));

    [Fact]
    public void AStrictCallersEvalCodeRefusesALegacyOctalLiteral()
        => Assert.Equal("SyntaxError", Thrown("'use strict'; ", "'010'"));

    [Fact]
    public void AStrictCallersEvalCodeRefusesAStrictReservedWordAsABinding()
        => Assert.Equal("SyntaxError", Thrown("'use strict'; ", "'var implements = 1'"));

    [Fact]
    public void ASloppyCallersEvalCodeAdmitsTheSame()
        => Assert.Equal("none", Thrown("", "'var implements = 010'"));

    [Fact]
    public void AStrictCallersInertEvalCodeCompletesWithItsOwnValue()
        // Until W2 the inert path skipped the prefixed directive; the completion is the code's own literal.
        => Assert.Equal("x", Eval("'use strict'; eval('\"x\"')"));

    [Fact]
    public void AStrictCallersEmptyEvalCodeIsUndefined()
        => Assert.Equal("undefined", Eval("'use strict'; '' + eval('')"));

    [Fact]
    public void AStrictCallersEvalCodeDeclaresInItsOwnVariableEnvironment()
        => Assert.Equal("undefined", Eval("'use strict'; eval('var leaked = 1'); typeof leaked"));

    [Fact]
    public void ASuperCallInABaseClassConstructorsEvalIsASyntaxError()
        // Row R24: the specification refuses it as an early error of the eval code; until W2 it was admitted at the parse
        // and threw a TypeError at run time.
        => Assert.Equal("SyntaxError", Eval(
            "class A { constructor() { eval('super()'); } }" +
            "var name; try { new A(); name = 'none'; } catch (e) { name = e.constructor.name; } name"));

    [Fact]
    public void ASuperCallInADerivedClassConstructorsEvalStillRuns()
        => Assert.Equal("3", Eval(
            "class A { constructor() { this.x = 3; } }" +
            "class B extends A { constructor() { eval('super()'); } }" +
            "'' + new B().x"));

    [Fact]
    public void ASuperCallInANestedEvalOfADerivedConstructorStillRuns()
        => Assert.Equal("4", Eval(
            "class A { constructor() { this.x = 4; } }" +
            "class B extends A { constructor() { eval(\"eval('super()')\"); } }" +
            "'' + new B().x"));

    [Fact]
    public void ABaseClassConstructorsEvalReachedBySuperFromADerivedConstructorsEvalReadsItsOwnThis()
        // Found in review of window W2: the base class's constructor shares no binding, so its eval code read the
        // enclosing derived constructor's `this`, still uninitialised, and threw a ReferenceError. The innermost shared
        // binding is now the running constructor's own. (Inside a function: at a sloppy script's top level the same
        // source throws "Cannot assign to read only variable" at the base commit too, which is not this row.)
        => Assert.Equal("E:true|E:true|F:true", Eval(
            "(function () { var r = [];" +
            "class E { constructor() { r.push('E:' + (eval('this') === this)); } }" +
            "new E();" +
            "class F extends E { constructor() { eval('super()'); r.push('F:' + (this instanceof F)); } }" +
            "try { new F(); } catch (e) { r.push('F-threw:' + e.constructor.name); }" +
            "return r.join('|'); })()"));

    [Fact]
    public void ANestedEvalInABaseClassConstructorInheritsNoSuperCallFromADerivedConstructorsEval()
        // Found in review of window W2: the derived constructor's super-call grant reached the base constructor's nested
        // eval, whose super() ran the base constructor again. It is the base constructor's eval code, where super() is
        // an early error (row R24).
        => Assert.Equal("A:SyntaxError|B:true", Eval(
            "(function () { var r = [], n = 0;" +
            "class A { constructor() { if (++n > 3) { r.push('A:re-entered ' + n + ' times'); return; }" +
            " try { var s = 'super()'; eval('eval(s)'); r.push('A:none'); } catch (e) { r.push('A:' + e.constructor.name); } } }" +
            "class B extends A { constructor() { var s = 'super()'; eval('eval(s)'); r.push('B:' + (this instanceof B)); } }" +
            "try { new B(); } catch (e) { r.push('outer:' + e.constructor.name); }" +
            "return r.join('|'); })()"));
}
