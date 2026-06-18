using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Ast.Expressions;

/// <summary>
/// Wraps the root of an optional chain (any member/index/call chain containing at least one
/// <c>?.</c> link). Its sole job is to evaluate the inner chain and convert the optional-chain
/// short-circuit sentinel back to <c>undefined</c> (see JSOptionalChainSkip): when an earlier
/// <c>?.</c> link short-circuits, the sentinel propagates up the chain and is unwrapped here.
/// </summary>
public class AstOptionalChain(AstExpression expression) :
    AstExpression(expression.Start, FastNodeType.OptionalChain, expression.End)
{
    public readonly AstExpression Expression = expression;

    public override string ToString() => Expression.ToString();
}
