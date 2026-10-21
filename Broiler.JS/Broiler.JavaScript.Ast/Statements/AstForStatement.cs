using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Ast.Statements;


public class AstForStatement(FastToken token, FastToken previousToken, AstNode beginNode, AstExpression test, AstExpression preTest, AstStatement statement) : 
    AstStatement(token, FastNodeType.ForStatement, previousToken)
{
    public readonly AstNode Init = beginNode;
    public readonly AstExpression Test = test;
    public readonly AstExpression Update = preTest;
    public readonly AstStatement Body = statement;

    /// <summary>
    /// Whether the parser parsed a <c>continue</c> statement while it parsed <see cref="Body"/>, at any
    /// depth and in any statement position, a <c>switch</c> case's statements among them. The parser
    /// records it as it parses, because a search of the finished tree can miss a position, and the
    /// compiler's for-loop pass reads it to decide where a per-iteration copy goes (milestone
    /// SJL-12.5, item 5; Broiler.VM's JSD-0080, decision D22).
    /// </summary>
    internal bool ContainsContinue;

    public override string ToString() => $"for ({Init};{Update};{Test}) {{ {Body} }}";
}
