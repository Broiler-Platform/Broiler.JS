using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression for #912 Problem 6 (the contained part):
// a labelled FunctionDeclaration is an early SyntaxError in strict mode in every
// context, including a switch CaseClause. The validator's base VisitCase is a no-op,
// so case-nested labelled functions were not being rejected in strict mode.
//
// NOTE: this does NOT fully green block-scoped-functions-annex-b-label.js — that test
// runs every snippet through eval(), and a block-level function called BEFORE its
// declaration within the same block is not value-hoisted to block-top under eval/program
// compilation (only function-body compilation hoists it, via CreateFunction's InitList).
// That direct-eval block-function hoisting is the remaining architectural blocker.
public class Issue912LabeledFunctionTests
{
    private static string Outcome(string code)
    {
        using var ctx = new JSContext();
        try { ctx.Eval(code); return "ok"; }
        catch (JSException e) { return e.Message.Contains("strict mode") || e.Message.Contains("declared") ? "SyntaxError" : "other:" + e.Message; }
    }

    [Theory]
    [InlineData("'use strict'; l: function f(){}")]
    [InlineData("'use strict'; { l: function f(){} }")]
    [InlineData("'use strict'; switch (1) { case 1: l: function f(){} }")]
    [InlineData("'use strict'; switch (1) { case 1: break; default: l: function f(){} }")]
    [InlineData("'use strict'; l0: l: function f(){}")]
    public void StrictLabeledFunctionIsSyntaxError(string code)
        => Assert.Equal("SyntaxError", Outcome(code));

    [Theory]
    // sloppy: a labelled function in a StatementListItem position is allowed (Annex B)
    [InlineData("l: function f(){}")]
    [InlineData("{ l: function f(){} }")]
    [InlineData("switch (1) { case 1: l: function f(){} }")]
    public void SloppyLabeledFunctionAllowed(string code)
        => Assert.Equal("ok", Outcome(code));
}
