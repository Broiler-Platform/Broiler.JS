using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Ast.Statements;

[System.Obsolete(ObsoleteMarks.Ast, DiagnosticId = ObsoleteMarks.DiagnosticId)]
public class AstDebuggerStatement(FastToken token) : AstStatement(token, FastNodeType.DebuggerStatement, token)
{
    public override string ToString() => "debugger;";
}
