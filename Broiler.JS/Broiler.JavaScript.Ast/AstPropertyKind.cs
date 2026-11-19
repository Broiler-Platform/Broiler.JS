namespace Broiler.JavaScript.Ast;

[System.Obsolete(ObsoleteMarks.Ast, DiagnosticId = ObsoleteMarks.DiagnosticId)]
public enum AstPropertyKind
{
    Get,
    Set,
    Data,
    Init,
    Constructor,
    Method,
    None
}
