using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.ExpressionCompiler.Core;

namespace Broiler.JavaScript.Ast.Patterns;

[System.Obsolete(ObsoleteMarks.Ast, DiagnosticId = ObsoleteMarks.DiagnosticId)]
public class AstArrayPattern(FastToken start, FastToken end, IFastEnumerable<AstExpression> elements) : AstBindingPattern(start, FastNodeType.ArrayPattern, end)
{
    public readonly IFastEnumerable<AstExpression> Elements = elements;

    public override string ToString() => $"[{Elements.Join()}]";
}
