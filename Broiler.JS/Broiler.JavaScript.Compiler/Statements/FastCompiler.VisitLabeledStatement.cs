using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using System;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;

namespace Broiler.JavaScript.Compiler;

partial class FastCompiler
{
    protected override BExpression VisitLabeledStatement(AstLabeledStatement labeledStatement)
    {
        switch (labeledStatement.Body.Type)
        {
            case FastNodeType.ForStatement:
                return VisitForStatement(labeledStatement.Body as AstForStatement, labeledStatement.Label.Span.Value);

            case FastNodeType.ForOfStatement:
                return VisitForOfStatement(labeledStatement.Body as AstForOfStatement, labeledStatement.Label.Span.Value);

            case FastNodeType.ForInStatement:
                return VisitForInStatement(labeledStatement.Body as AstForInStatement, labeledStatement.Label.Span.Value);

            case FastNodeType.WhileStatement:
                return VisitWhileStatement(labeledStatement.Body as AstWhileStatement, labeledStatement.Label.Span.Value);

            case FastNodeType.DoWhileStatement:
                return VisitDoWhileStatement(labeledStatement.Body as AstDoWhileStatement, labeledStatement.Label.Span.Value);

            default:
                {
                    var breakTarget = BExpression.Label();
                    var label = labeledStatement.Label.Span.Value;
                    var prefixesLoop = PrefixesLoop(labeledStatement);

                    // A LabelledStatement completes with its item's completion, an empty one
                    // included: unlike if, the loops, switch, try and with it applies no
                    // UpdateEmpty(…, undefined), and a `break` to its own label only turns the
                    // break into a normal completion carrying the break's value. Both values are
                    // in the enclosing completion variables already when the body ends or breaks
                    // (its statements track themselves, and a nested statement that completes
                    // with undefined propagates that on its way out), so the body is emitted
                    // plainly, with no completion variable of its own. One reset to undefined
                    // and propagated outwards made `1; L: {}` and `1; L: var x = 2;` complete
                    // with undefined, not 1.
                    //
                    // That also keeps a generator free of the try/finally wrapper the variable
                    // needed: a generator's rewritten try region whose `finally` is SKIPPED by a
                    // `break <label>` out of the body leaves a stale region on the generator's
                    // try-region stack and corrupts the pending-completion unwinding of an
                    // enclosing finally.
                    using var s = scope.Top.Loop.Push(new LoopScope(breakTarget, null, false, label) { IsLabelOnly = true, PrefixesLoop = prefixesLoop });
                    return BExpression.Block(
                        VisitStatement(labeledStatement.Body),
                        BExpression.Label(breakTarget),
                        JSUndefinedBuilder.Value);
                }
        }

        throw new NotImplementedException();
    }

    // True when the label is an outer label of a loop's label set (`L` in `L: M: while`):
    // its body is a chain of labels that ends at an IterationStatement. `continue L` then
    // continues that loop (LoopScope.GetContinue), while `break L` still leaves the whole
    // labelled statement through this label's own break target.
    private static bool PrefixesLoop(AstLabeledStatement labeledStatement)
    {
        if (labeledStatement.Body is not AstLabeledStatement nested)
            return false;

        while (nested.Body is AstLabeledStatement inner)
            nested = inner;

        return nested.Body.Type is FastNodeType.ForStatement
            or FastNodeType.ForOfStatement
            or FastNodeType.ForInStatement
            or FastNodeType.WhileStatement
            or FastNodeType.DoWhileStatement;
    }
}
