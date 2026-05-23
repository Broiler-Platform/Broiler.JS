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
    protected override YExpression VisitLabeledStatement(AstLabeledStatement labeledStatement)
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
                    var breakTarget = YExpression.Label();
                    var label = labeledStatement.Label.Span.Value;
                    var completionVar = YExpression.Variable(typeof(JSValue), "#cv");
                    var loopScope = new LoopScope(breakTarget, null, false, label) { CompletionVariable = completionVar };
                    using var s = scope.Top.Loop.Push(loopScope);
                    var body = VisitStatement(labeledStatement.Body);
                    return YExpression.Block(
                        new Sequence<YParameterExpression> { completionVar },
                        YExpression.Assign(completionVar, JSUndefinedBuilder.Value),
                        body,
                        YExpression.Label(breakTarget),
                        completionVar);
                }
        }

        throw new NotImplementedException();
    }
}
