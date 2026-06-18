using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Statements;

namespace Broiler.JavaScript.Ast;

/// <summary>
/// Detects whether an expression that is about to be reinterpreted as arrow-function
/// parameters Contains an AwaitExpression or YieldExpression (ECMA-262 early error:
/// "ArrowParameters Contains AwaitExpression / YieldExpression"). The "Contains" static
/// semantic does not descend into nested function or class boundaries — an
/// await/yield there belongs to that inner scope — so those nodes are not traversed.
/// </summary>
public sealed class AwaitYieldParameterDetector : AstReduce
{
    private bool found;

    public static bool Contains(AstNode node)
    {
        if (node == null)
            return false;

        var detector = new AwaitYieldParameterDetector();
        detector.Visit(node);
        return detector.found;
    }

    protected override AstNode VisitAwaitExpression(AstAwaitExpression node)
    {
        found = true;
        return node;
    }

    protected override AstNode VisitYieldExpression(AstYieldExpression yieldExpression)
    {
        found = true;
        return yieldExpression;
    }

    // Nested function / arrow / class bodies open their own [Await]/[Yield] scope, so an
    // await/yield inside them is not part of the enclosing arrow's parameters: stop here.
    protected override AstNode VisitFunctionExpression(AstFunctionExpression functionExpression)
        => functionExpression;

    protected override AstNode VisitClassStatement(AstClassExpression classStatement)
        => classStatement;
}
