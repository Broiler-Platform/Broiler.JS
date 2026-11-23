using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Ast;


[System.Obsolete(ObsoleteMarks.Ast, DiagnosticId = ObsoleteMarks.DiagnosticId)]
public class AstProgram(FastToken token, FastToken end, IFastEnumerable<AstStatement> statements, bool isAsync) : AstBlock(token, FastNodeType.Program, end, statements)
{
    public readonly bool IsAsync = isAsync;
}
