using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Ast.Statements;

[System.Obsolete(ObsoleteMarks.Ast, DiagnosticId = ObsoleteMarks.DiagnosticId)]
public class AstBreakStatement(FastToken token, FastToken previousToken, AstIdentifier? label = null) : AstStatement(token, FastNodeType.BreakStatement, previousToken)
{
    public readonly AstIdentifier? Label = label;

    public override string ToString() => Label != null ? $"break {Label};" : $"break;";
}
