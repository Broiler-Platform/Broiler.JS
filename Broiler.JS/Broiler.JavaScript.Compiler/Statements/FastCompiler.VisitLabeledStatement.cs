using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using System;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.ExpressionCompiler.Core;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Runtime;

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

                    // Inside a generator the body is lowered into a state machine and the
                    // script/eval completion value is irrelevant — so emit the labeled
                    // body plainly, without the completion-tracking try/finally wrapper
                    // (mirroring VisitTryStatement's generator special-case). That wrapper
                    // is a rewritten try region whose `finally` (PropagateCompletion) would
                    // be SKIPPED by a `break <label>` that branches out of the body, leaving
                    // a stale region on the generator's try-region stack and corrupting the
                    // pending-completion unwinding of an enclosing finally.
                    if (scope.Top.Function?.Generator == true)
                    {
                        var genLoopScope = new LoopScope(breakTarget, null, false, label) { IsLabelOnly = true, PrefixesLoop = prefixesLoop };
                        using var gs = scope.Top.Loop.Push(genLoopScope);
                        return BExpression.Block(
                            VisitStatement(labeledStatement.Body),
                            BExpression.Label(breakTarget),
                            JSUndefinedBuilder.Value);
                    }

                    var completionVar = BExpression.Variable(typeof(JSValue), "#cv");
                    var outerCompletionVars = GetCompletionVariables();
                    var loopScope = new LoopScope(breakTarget, null, false, label) { IsLabelOnly = true, PrefixesLoop = prefixesLoop, CompletionVariable = completionVar };
                    using var completion = completionScopes.Push(completionVar);
                    using var s = scope.Top.Loop.Push(loopScope);
                    var body = TrackCompletion(VisitStatement(labeledStatement.Body));
                    return BExpression.Block(
                        new Sequence<BParameterExpression> { completionVar },
                        BExpression.Assign(completionVar, JSUndefinedBuilder.Value),
                        BExpression.TailCallTransparentTryFinally(body, PropagateCompletion(completionVar, outerCompletionVars)),
                        BExpression.Label(breakTarget),
                        completionVar);
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
