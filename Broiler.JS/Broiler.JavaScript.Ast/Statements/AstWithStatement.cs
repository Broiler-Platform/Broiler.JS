using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Ast.Statements;

[System.Obsolete(ObsoleteMarks.Ast, DiagnosticId = ObsoleteMarks.DiagnosticId)]
public class AstWithStatement(FastToken start, FastToken end, AstExpression @object, AstStatement body) : AstStatement(start, FastNodeType.WithStatement, end)
{
    public readonly AstExpression Object = @object;
    public readonly AstStatement Body = body;
}
