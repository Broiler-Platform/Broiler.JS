
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Ast.Statements;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.Parser;

partial class FastParser
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool NonDeclarativeStatement(out AstStatement statement)
    {
        if (!Statement(out statement))
            return false;

        if(statement.Type == FastNodeType.ExpressionStatement && statement is AstExpressionStatement exp)
        {
            switch (exp.Expression.Type)
            {
                // Function declarations are allowed as single-statement bodies
                // in sloppy mode per Annex B (B.3.2, B.3.4). The strict-mode
                // rejection is handled later by SyntaxValidation.
                case FastNodeType.ClassStatement:
                    throw new FastParseException(exp.Start, $"Unexpected declaration");
            }
        }

        return true;
    }
}
