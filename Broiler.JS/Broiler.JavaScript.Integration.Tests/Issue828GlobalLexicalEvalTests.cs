using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/828 Problem 5.
//
// A top-level script `let`/`const` declaration creates a binding in the global lexical
// environment: it is resolvable by name from code that runs in the global environment —
// notably an indirect eval, whose new declarative environment's outer environment is the
// global environment (test262 language/eval-code/indirect/lex-env-heritage) — yet it never
// becomes a property of the global object. The binding used to live only in the running
// script's own scope, so an indirect eval (which is compiled as a fresh global program)
// could not see it and threw "x is not defined".
public class Issue828GlobalLexicalEvalTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // Indirect eval resolves a top-level `let` through the global environment.
    [Fact]
    public void IndirectEvalSeesGlobalLet()
        => Assert.Equal("outside", Eval("""
            let x = 'outside';
            (0, eval)('x');
        """));

    [Fact]
    public void IndirectEvalSeesGlobalConst()
        => Assert.Equal("42", Eval("""
            const k = 42;
            '' + (0, eval)('k');
        """));

    // A block-scoped `let` does NOT leak to an indirect eval, which sees the outer global one
    // (lex-env-heritage: indirect eval's outer environment is the global environment).
    [Fact]
    public void IndirectEvalIgnoresBlockScopedShadow()
        => Assert.Equal("outside,outside", Eval("""
            let x = 'outside';
            var nonStrict, strict;
            {
              let x = 'inside';
              nonStrict = (0, eval)('x;');
              strict = (0, eval)('"use strict"; x;');
            }
            nonStrict + ',' + strict;
        """));

    // A top-level `let` is a lexical binding, never a property of the global object.
    [Fact]
    public void GlobalLetIsNotAGlobalObjectProperty()
        => Assert.Equal("value,undefined,false", Eval("""
            let x = 'value';
            x + ',' + (typeof globalThis.x) + ',' + ('x' in globalThis);
        """));
}
