using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Ast.Statements;

[System.Obsolete(ObsoleteMarks.Ast, DiagnosticId = ObsoleteMarks.DiagnosticId)]
public class AstSwitchStatement(FastToken start, FastToken end, AstExpression target, IFastEnumerable<Case> astCases) : AstStatement(start, FastNodeType.SwitchStatement, end)
{
    public IFastEnumerable<StringSpan>? HoistingScope;
    public readonly AstExpression Target = target;
    public readonly IFastEnumerable<Case> Cases = astCases;
}
