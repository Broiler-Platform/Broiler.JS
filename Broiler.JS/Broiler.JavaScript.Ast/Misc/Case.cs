using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Statements;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Ast.Misc;

// Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=0; Fingerprint=TBF
// Broiler-Human:        PENDING
[System.Obsolete(ObsoleteMarks.Ast, DiagnosticId = ObsoleteMarks.DiagnosticId)]
public readonly struct Case(AstExpression test, IFastEnumerable<AstStatement> last)
{
    public readonly AstExpression Test = test;
    public readonly IFastEnumerable<AstStatement> Statements = last;
}
