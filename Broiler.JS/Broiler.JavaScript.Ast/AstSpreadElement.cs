using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Ast;


[System.Obsolete(ObsoleteMarks.Ast, DiagnosticId = ObsoleteMarks.DiagnosticId)]
public class AstSpreadElement(FastToken start, FastToken end, AstExpression element) : AstExpression(start, FastNodeType.SpreadElement, end)
{
    public readonly AstExpression Argument = element;
}
