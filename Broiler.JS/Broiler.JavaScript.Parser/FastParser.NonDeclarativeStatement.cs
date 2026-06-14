
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Statements;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.Parser;

partial class FastParser
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool NonDeclarativeStatement(out AstStatement statement)
    {
        singleStatementContext = true;
        if (!Statement(out statement))
            return false;

        if(statement.Type == FastNodeType.ExpressionStatement && statement is AstExpressionStatement exp)
        {
            // The body of a while/do/for/with statement is a Statement, never a
            // Declaration. A FunctionDeclaration is only permitted as a single-
            // statement body by the Annex B IfStatement/LabelledStatement extensions
            // (handled via NestedStatement / the label parser), so reject it — and a
            // ClassDeclaration — here in both sloppy and strict mode.
            if (exp.Expression.Type == FastNodeType.ClassStatement)
                throw new FastParseException(exp.Start, "Unexpected declaration");

            if (exp.Expression is AstFunctionExpression { IsStatement: true })
                throw new FastParseException(exp.Start, "Function declarations are not allowed as the body of a loop or with statement");
        }

        return true;
    }
}
