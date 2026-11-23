using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Ast.Statements;

[System.Obsolete(ObsoleteMarks.Ast, DiagnosticId = ObsoleteMarks.DiagnosticId)]
public class AstThrowStatement(FastToken token, FastToken previousToken, AstExpression target) : AstStatement(token, FastNodeType.ThrowStatement, previousToken)
{
    public readonly AstExpression Argument = target;
}
